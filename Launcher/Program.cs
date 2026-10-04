using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new LauncherForm());
    }
}

// ---------------- config ----------------

sealed class LauncherConfig
{
    public string RepoOwner  { get; set; } = "YOUR_GITHUB_USER";
    public string RepoName   { get; set; } = "YOUR_REPO";
    public string GameFolder { get; set; } = "Game";                 // install dir next to Launcher.exe
    public string GameExe    { get; set; } = "MyGame.exe";           // relative to zip root
    public string GameAssetPattern     { get; set; } = "*windows*.zip"; // which release asset is the game
    public string LauncherAssetPattern { get; set; } = "Launcher*.zip"; // asset used to self-update
    public bool   AutoLaunch  { get; set; } = true;
    public string Token       { get; set; } = ""; // PAT — private repos only, never ship publicly

    public static LauncherConfig Load(string dir)
    {
        string path = Path.Combine(dir, "launcher.json");
        if (File.Exists(path))
        {
            var cfg = JsonSerializer.Deserialize<LauncherConfig>(File.ReadAllText(path));
            if (cfg != null) return cfg;
        }
        var fresh = new LauncherConfig(); // first run: write a template to fill in
        File.WriteAllText(path, JsonSerializer.Serialize(fresh, new JsonSerializerOptions { WriteIndented = true }));
        return fresh;
    }
}

// ---------------- GitHub API DTOs ----------------

sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")] public string Tag { get; set; } = "";
    [JsonPropertyName("name")]     public string Name { get; set; } = "";
    [JsonPropertyName("assets")]   public List<ReleaseAsset> Assets { get; set; } = new();
}

sealed class ReleaseAsset
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("url")]  public string Url { get; set; } = "";
}

// ---------------- GitHub client ----------------

sealed class ReleaseClient : IDisposable
{
    readonly HttpClient _http;
    readonly string _apiBase;

    public ReleaseClient(LauncherConfig cfg)
    {
        _http = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        { Timeout = TimeSpan.FromMinutes(30) }; // default 100s is too short for big zips

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MyGame-Launcher/1.0"); // GitHub requires a UA
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        if (!string.IsNullOrEmpty(cfg.Token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cfg.Token);

        _apiBase = $"https://api.github.com/repos/{cfg.RepoOwner}/{cfg.RepoName}";
    }

    public async Task<GitHubRelease> GetLatestReleaseAsync(CancellationToken ct)
    {
        using var req  = new HttpRequestMessage(HttpMethod.Get, _apiBase + "/releases/latest");
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<GitHubRelease>(await resp.Content.ReadAsStringAsync(ct))
               ?? throw new InvalidOperationException("Empty response from GitHub.");
    }

    public async Task DownloadAssetAsync(ReleaseAsset asset, string destPath, IProgress<int> progress, CancellationToken ct)
    {
        // asset.Url + octet-stream => 302 to the signed CDN link (works with or without a token)
        using var req  = new HttpRequestMessage(HttpMethod.Get, asset.Url);
        req.Headers.Accept.ParseAdd("application/octet-stream");
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        long total = resp.Content.Headers.ContentLength ?? asset.Size;
        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true);

        var buf = new byte[1 << 16];
        long received = 0;
        while (true)
        {
            int n = await src.ReadAsync(buf, ct);
            if (n == 0) break;
            await dst.WriteAsync(buf.AsMemory(0, n), ct);
            received += n;
            if (total > 0) progress?.Report((int)(100 * received / total));
        }
    }

    public void Dispose() => _http.Dispose();
}

// ---------------- UI + update logic ----------------

sealed class LauncherForm : Form
{
    readonly LauncherConfig _cfg;
    readonly ReleaseClient _github;
    readonly CancellationTokenSource _cts = new();
    readonly Progress<int> _progress;

    readonly Label _status;
    readonly ProgressBar _bar;
    readonly Button _play;

    readonly string _root, _gameDir, _gameVersionFile, _launcherVersionFile;
    GitHubRelease _latest;

    public LauncherForm()
    {
        Text = "My Game — Launcher";
        ClientSize = new Size(480, 190);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        _status = new Label { AutoSize = false, Location = new Point(12, 18), Size = new Size(456, 20),
                              TextAlign = ContentAlignment.MiddleCenter, Text = "Checking for updates…" };
        _bar    = new ProgressBar { Location = new Point(12, 46), Size = new Size(456, 20) };
        _play   = new Button { Location = new Point(12, 130), Size = new Size(456, 44), Text = "Play", Enabled = false };
        _play.Click += (s, e) => LaunchGame();
        Controls.Add(_status);
        Controls.Add(_bar);
        Controls.Add(_play);

        _cfg      = LauncherConfig.Load(AppContext.BaseDirectory);
        _github   = new ReleaseClient(_cfg);
        _progress = new Progress<int>(p => _bar.Value = Math.Min(100, p));

        _root                = AppContext.BaseDirectory;
        _gameDir             = Path.Combine(_root, _cfg.GameFolder);
        _gameVersionFile     = Path.Combine(_root, ".game-version");
        _launcherVersionFile = Path.Combine(_root, ".launcher-version");

        Load       += async (s, e) => await StartupAsync();
        FormClosing += (s, e) => _cts.Cancel();
        FormClosed  += (s, e) => { _cts.Dispose(); _github.Dispose(); };
    }

    async Task StartupAsync()
    {
        try
        {
            if (_cfg.RepoOwner.Contains("YOUR_"))
            {
                MessageBox.Show(this, "Open launcher.json next to Launcher.exe and set RepoOwner / RepoName.", "Configuration");
                Close();
                return;
            }

            CleanupLeftovers();
            _latest = await _github.GetLatestReleaseAsync(_cts.Token);

            await SelfUpdateLauncherAsync();   // see "self-update" section

            string installed = File.Exists(_gameVersionFile) ? File.ReadAllText(_gameVersionFile).Trim() : "";
            if (IsGameInstalled() && string.Equals(installed, _latest.Tag, StringComparison.OrdinalIgnoreCase))
                _status.Text = $"Up to date — {_latest.Tag}";
            else
                await InstallGameAsync(_cts.Token);

            if (_cfg.AutoLaunch) LaunchGame();
            else { _status.Text = $"Ready — {_latest.Tag}"; _play.Enabled = true; }
        }
        catch (OperationCanceledException) { Close(); }
        catch (Exception ex)
        {
            // Offline / rate-limited: if a game is already installed, let them play anyway.
            if (IsGameInstalled())
            {
                _status.Text = "Update check failed — launching installed version.";
                if (_cfg.AutoLaunch) LaunchGame(); else _play.Enabled = true;
            }
            else
            {
                MessageBox.Show(this, "Could not fetch or install the latest release:\n\n" + ex.Message,
                                "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }
    }

    async Task InstallGameAsync(CancellationToken ct)
    {
        var asset = _latest.Assets.FirstOrDefault(a => WildcardMatch(a.Name, _cfg.GameAssetPattern))
            ?? throw new InvalidOperationException($"Release {_latest.Tag} has no asset matching '{_cfg.GameAssetPattern}'.");

        EnsureGameNotRunning(); // files are locked while the game runs

        _status.Text = $"Downloading {_latest.Tag} ({asset.Size / (1024 * 1024)} MB)…";
        string zipPath = Path.Combine(_root, "game-update.zip");
        await _github.DownloadAssetAsync(asset, zipPath, _progress, ct);

        _status.Text = "Extracting…";
        string staging = _gameDir + ".new";
        TryDeleteDirectory(staging);
        ExtractZip(zipPath, staging, _progress, ct);
        TryDelete(zipPath);

        _status.Text = "Installing…";
        string old = _gameDir + ".old";
        TryDeleteDirectory(old);
        if (Directory.Exists(_gameDir)) Directory.Move(_gameDir, old); // rename on same volume = fast, near-atomic
        Directory.Move(staging, _gameDir);
        File.WriteAllText(_gameVersionFile, _latest.Tag);
        TryDeleteDirectory(old); // may fail (AV scan) — swept again on next launch

        _status.Text = $"Installed {_latest.Tag}";
    }

    async Task SelfUpdateLauncherAsync()
    {
        var asset = _latest.Assets.FirstOrDefault(a => WildcardMatch(a.Name, _cfg.LauncherAssetPattern));
        if (asset == null) return; // this release ships no launcher build — nothing to do

        string installed = File.Exists(_launcherVersionFile) ? File.ReadAllText(_launcherVersionFile).Trim() : "";
        if (string.Equals(installed, _latest.Tag, StringComparison.OrdinalIgnoreCase)) return;

        _status.Text = "Updating launcher…";
        string zipPath = Path.Combine(_root, "launcher-update.zip");
        await _github.DownloadAssetAsync(asset, zipPath, _progress, _cts.Token);

        string staging = Path.Combine(_root, ".launcher-new"); // same volume as the exe — required
        TryDeleteDirectory(staging);
        ExtractZip(zipPath, staging, null, _cts.Token);
        TryDelete(zipPath);

        string runningExe = Environment.ProcessPath!;
        string myName = Path.GetFileName(runningExe);
        string newExe = Directory.EnumerateFiles(staging, myName).FirstOrDefault()
            ?? throw new IOException($"Launcher update zip must contain an exe named '{myName}'.");

        // Windows won't let you overwrite/delete a running exe, but it WILL let you rename it.
        string oldExe = runningExe + ".old";
        TryDelete(oldExe);
        File.Move(runningExe, oldExe); // process keeps running from the renamed file
        File.Move(newExe, runningExe); // new version is in place for the next start
        File.WriteAllText(_launcherVersionFile, _latest.Tag);
        TryDeleteDirectory(staging);

        // Optional: restart into the new version immediately instead of continuing:
        // Process.Start(new ProcessStartInfo(runningExe) { UseShellExecute = true });
        // Environment.Exit(0);
    }

    void LaunchGame()
    {
        string exe = FindGameExe();
        if (exe == null) { MessageBox.Show(this, $"Could not find {_cfg.GameExe} inside {_gameDir}.", "Error"); return; }
        Process.Start(new ProcessStartInfo { FileName = exe, WorkingDirectory = Path.GetDirectoryName(exe)!, UseShellExecute = true });
        Close();
    }

    // ---------------- helpers ----------------

    string FindGameExe()
    {
        string direct = Path.Combine(_gameDir, _cfg.GameExe);
        if (File.Exists(direct)) return direct;
        // fallback: handle zips that contain a wrapping folder
        return Directory.Exists(_gameDir)
            ? Directory.EnumerateFiles(_gameDir, Path.GetFileName(_cfg.GameExe), SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    bool IsGameInstalled() => FindGameExe() != null;

    void EnsureGameNotRunning()
    {
        while (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_cfg.GameExe)).Length > 0)
        {
            var r = MessageBox.Show(this, $"{_cfg.GameExe} is still running. Close it and press Retry.",
                "Game is running", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);
            if (r != DialogResult.Retry) throw new OperationCanceledException("Cancelled by user.");
        }
    }

    void CleanupLeftovers()
    {
        foreach (string d in new[] { _gameDir + ".old", _gameDir + ".new", Path.Combine(_root, ".launcher-new") })
            TryDeleteDirectory(d);
        foreach (string f in new[] { "game-update.zip", "launcher-update.zip" })
            TryDelete(Path.Combine(_root, f));
        foreach (string f in Directory.EnumerateFiles(_root, "*.exe.old")) // old launcher exes
            TryDelete(f);
    }

    static void ExtractZip(string zipPath, string destDir, IProgress<int> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);
        string root = Path.GetFullPath(destDir + Path.DirectorySeparatorChar);
        using var zip = ZipFile.OpenRead(zipPath);
        for (int i = 0; i < zip.Entries.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var entry = zip.Entries[i];
            string dest = Path.GetFullPath(Path.Combine(destDir, entry.FullName));
            if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase)) // zip-slip guard
                throw new IOException($"Unsafe entry in zip: {entry.FullName}");
            if (string.IsNullOrEmpty(entry.Name)) Directory.CreateDirectory(dest);
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                entry.ExtractToFile(dest, overwrite: true);
            }
            progress?.Report((int)(100L * (i + 1) / zip.Entries.Count));
        }
    }

    static bool WildcardMatch(string name, string pattern) // supports a single '*'
    {
        pattern = pattern.Trim();
        int star = pattern.IndexOf('*');
        if (star < 0) return string.Equals(name, pattern, StringComparison.OrdinalIgnoreCase);
        string pre = pattern[..star], post = pattern[(star + 1)..];
        return name.Length >= pre.Length + post.Length
            && name.StartsWith(pre, StringComparison.OrdinalIgnoreCase)
            && name.EndsWith(post, StringComparison.OrdinalIgnoreCase);
    }

    static void TryDelete(string path)        { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    static void TryDeleteDirectory(string p)  { try { if (Directory.Exists(p)) Directory.Delete(p, true); } catch { } }
}
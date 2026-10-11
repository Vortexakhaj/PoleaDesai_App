using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class DpiAndResolutionFix : MonoBehaviour
{
#if UNITY_STANDALONE_WIN 
    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    private static readonly IntPtr PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void EarlyInit()
    {
        try { SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2); } catch { }
    }

    void Awake()
    {
        // 1. Tell Windows this process handles its own DPI scaling.
        try
        {
            SetProcessDpiAwarenessContext(PER_MONITOR_AWARE_V2);
        }
        catch (Exception e)
        {
            Debug.LogWarning("DPI awareness call failed: " + e.Message);
        }

        // 2. Query the actual native resolution of the display.
        //    After setting DPI awareness, Unity reports the true pixel dimensions.
        int nativeWidth = Display.main.systemWidth;
        int nativeHeight = Display.main.systemHeight;

        // Sanity fallback: if detection fails, use the highest available resolution.
        if (nativeWidth <= 0 || nativeHeight <= 0)
        {
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions.Length > 0)
            {
                Resolution highest = resolutions[resolutions.Length - 1];
                nativeWidth = highest.width;
                nativeHeight = highest.height;
            }
            else
            {
                // Last-resort fallback
                nativeWidth = Screen.width;
                nativeHeight = Screen.height;
            }
        }

        Debug.Log($"Detected native resolution: {nativeWidth}x{nativeHeight}");

        // 3. Overwrite the stale registry values with the correct native resolution.
        PlayerPrefs.SetInt("Screenmanager Resolution Width", nativeWidth);
        PlayerPrefs.SetInt("Screenmanager Resolution Height", nativeHeight);
        PlayerPrefs.SetInt("Screenmanager Is Fullscreen mode", 1); // 1 = fullscreen
        PlayerPrefs.Save();

        // 4. Apply immediately for the current session.
        Screen.SetResolution(nativeWidth, nativeHeight, FullScreenMode.FullScreenWindow);
    }
#endif
}
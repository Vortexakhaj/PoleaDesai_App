using UnityEditor;
using UnityEngine;

/// <summary>
/// Moves a UI element horizontally across its parent, from one edge to the other,
/// with a random-feeling pattern (random direction, speed, height, drift, bobbing).
/// When it fully exits the far edge, it waits a random delay and makes a fresh pass.
/// Attach to Clouds1, Clouds2, Birds1, Birds2... and tweak each one.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIHorizontalWanderer : MonoBehaviour
{
    [Header("Direction")]
    [Tooltip("Pick a new random direction (left/right) on every pass.")]
    [SerializeField] private bool randomDirection = true;

    [Tooltip("Fixed direction when 'Random Direction' is off. True = left to right.")]
    [SerializeField] private bool moveRight = true;

    [Tooltip("Mirror the sprite so it faces the way it travels (art should face right by default). Handy for birds.")]
    [SerializeField] private bool flipToFaceDirection = false;

    [Header("Speed (parent pixels per second)")]
    [SerializeField] private float minSpeed = 40f;
    [SerializeField] private float maxSpeed = 90f;

    [Tooltip("How much the speed randomly rises/falls mid-flight. 0 = constant, 1 = wild.")]
    [Range(0f, 1f)]
    [SerializeField] private float speedVariation = 0.3f;

    [Header("Vertical Motion (this is where the random feel comes from)")]
    [Tooltip("Smooth random up/down wandering in pixels. 0 = none (typical for clouds).")]
    [SerializeField] private float verticalDrift = 15f;

    [Tooltip("How quickly the wandering/speed variation changes. Higher = twitchier.")]
    [SerializeField] private float driftFrequency = 0.4f;

    [Tooltip("Steady sine-wave bobbing in pixels. 0 = none (nice for birds).")]
    [SerializeField] private float bobAmplitude = 5f;

    [Tooltip("How fast it bobs.")]
    [SerializeField] private float bobFrequency = 3f;

    [Tooltip("Pick a new random height every pass. If off, keeps the Y you placed it at in the editor.")]
    [SerializeField] private bool randomHeightEachPass = true;

    [Header("Bounds")]
    [Tooltip("Extra horizontal gap between the sprite and the edge before it counts as fully outside.")]
    [SerializeField] private float horizontalPadding = 20f;

    [Tooltip("Minimum gap from the parent's top/bottom edges when picking a height.")]
    [SerializeField] private float verticalPadding = 30f;

    [Tooltip("Optional: use a different RectTransform as the travel area (e.g. drag 'Parallax Panel' here). Defaults to the direct parent.")]
    [SerializeField] private RectTransform boundsOverride = null;

    [Header("Respawn")]
    [Tooltip("Random pause off-screen between passes. Set both to 0 for back-to-back passes.")]
    [SerializeField] private float minRespawnDelay = 1f;
    [SerializeField] private float maxRespawnDelay = 4f;

    public enum StartMode
    {
        AtCurrentPosition,   // begin wherever the object is placed in the editor
        FromEdge,            // classic: spawn just off-screen and cross
        RandomAlongPath      // begin at a random point along the crossing
    }

    [Header("Start")]
    [Tooltip("Where the object should be when the scene starts. Only affects the first pass — every pass after starts off-screen.")]
    [SerializeField] private StartMode startMode = StartMode.AtCurrentPosition;

    [Header("Misc")]
    [Tooltip("Keep moving while the game is paused (Time.timeScale = 0). Handy for menu backgrounds.")]
    [SerializeField] private bool useUnscaledTime = false;

    // ---------------- runtime ----------------
    private RectTransform rect;
    private Vector3 originalScale;
    private readonly Vector3[] corners = new Vector3[4];

    private float dir;               // +1 = right, -1 = left
    private float speed;             // base speed for the current pass
    private float startX, endX;      // off-screen positions for the current pass
    private float offLeftX, offRightX;
    private float minSpawnY, maxSpawnY;
    private float minClampY, maxClampY;
    private float baseY, editorX, editorY;
    private float seedX, seedY, bobSeed;
    private float travelTime, waitTimer;
    private bool waiting;

    // --- Read by CloudFloatJiggle (or any idle-animation script) ---
    private Vector3 basePos;    // last position this script wrote (before jiggle offsets)
    private Vector3 baseScale;  // last scale this script wrote (direction flip included)
    private float currentX;     // our own travel X, immune to offsets added by other scripts

    public Vector3 BaseLocalPosition => basePos;
    public Vector3 BaseLocalScale => baseScale;

    private void Awake()
    {
        rect = (RectTransform)transform;
        originalScale = transform.localScale;
        baseScale = originalScale;
        Vector3 p = rect.localPosition;
        editorX = p.x;   
        editorY = p.y;
    }

    private void Start()
    {
        BeginPass(firstPass: true);
    }

    // Starts a new pass with fresh random values.
    // Starts a new pass with fresh random values.
    // firstPass == true only on scene start; that's the only time Start Mode applies.
    private void BeginPass(bool firstPass = false)
    {
        RecalculateBounds();

        dir = randomDirection ? (Random.value < 0.5f ? -1f : 1f)
                              : (moveRight ? 1f : -1);

        // Start just outside one edge, finish just outside the other.
        startX = dir > 0f ? offLeftX : offRightX;
        endX = dir > 0f ? offRightX : offLeftX;

        speed = Random.Range(Mathf.Min(minSpeed, maxSpeed), Mathf.Max(minSpeed, maxSpeed));

        // Fresh seeds so every element and every pass moves differently.
        seedX = Random.Range(0f, 1000f);
        seedY = Random.Range(0f, 1000f);
        bobSeed = Random.Range(0f, Mathf.PI * 2f);

        // ---- Where does this pass begin? ----
        float x = startX;
        bool keepEditorHeight = false;

        if (firstPass)
        {
            switch (startMode)
            {
                case StartMode.AtCurrentPosition:
                    // Begin exactly where the object was placed in the editor...
                    x = editorX;
                    // ...and keep the editor height for this first pass too.
                    keepEditorHeight = true;
                    break;

                case StartMode.RandomAlongPath:
                    x = Random.Range(Mathf.Min(startX, endX), Mathf.Max(startX, endX));
                    break;

                case StartMode.FromEdge:
                default:
                    x = startX;
                    break;
            }
        }

        baseY = randomHeightEachPass && !keepEditorHeight
            ? Random.Range(minSpawnY, maxSpawnY)
            : editorY;

        if (flipToFaceDirection)
        {
            baseScale = new Vector3(Mathf.Abs(originalScale.x) * dir,
                                    originalScale.y, originalScale.z);
            rect.localScale = baseScale;
        }

        travelTime = 0f;
        waiting = false;

        currentX = x;
        basePos = new Vector3(x, baseY, rect.localPosition.z);
        rect.localPosition = basePos;
    }
    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Waiting off-screen between passes.
        if (waiting)
        {
            waitTimer -= dt;
            if (waitTimer <= 0f) BeginPass();
            return;
        }

        travelTime += dt;

        // ---- Horizontal: base speed + smooth random variation (Perlin noise) ----
        float speedNoise = Mathf.PerlinNoise(travelTime * driftFrequency + seedX, seedX) * 2f - 1f;
        float currentSpeed = Mathf.Max(0f, speed * (1f + speedNoise * speedVariation));
        currentX += dir * currentSpeed * dt;

        // ---- Vertical: smooth wandering + steady bobbing ----
        float drift = Mathf.PerlinNoise(travelTime * driftFrequency + seedY, seedY) * 2f - 1f;
        float bob = Mathf.Sin(travelTime * bobFrequency + bobSeed) * bobAmplitude;
        float y = Mathf.Clamp(baseY + drift * verticalDrift + bob, minClampY, maxClampY);

        basePos = new Vector3(currentX, y, basePos.z);
        rect.localPosition = basePos;

        // ---- Fully exited the far edge? Wait, then go again. ----
        bool reachedEnd = (dir > 0f && currentX >= endX) || (dir < 0f && currentX <= endX);
        if (reachedEnd)
        {
            waiting = true;
            waitTimer = Random.Range(Mathf.Min(minRespawnDelay, maxRespawnDelay), Mathf.Max(minRespawnDelay, maxRespawnDelay));
        }
    }

    // Works out where "fully outside left/right" is and the safe height range,
    // based on the bounds rect (direct parent by default). Re-runs every pass,
    // so resolution/layout changes are picked up.
    private void RecalculateBounds()
    {
        RectTransform bounds = boundsOverride != null ? boundsOverride : transform.parent as RectTransform;

        if (bounds == null || transform.parent == null)
        {
            Debug.LogWarning($"{name} ({nameof(UIHorizontalWanderer)}): needs a parent RectTransform to move across. Disabling.", this);
            enabled = false;
            return;
        }

        // Bounds edges in world space, converted into the space we move in
        // (the direct parent's local space). Works with any anchors/pivots.
        bounds.GetWorldCorners(corners);
        Vector3 bottomLeft = transform.parent.InverseTransformPoint(corners[0]);
        Vector3 topRight = transform.parent.InverseTransformPoint(corners[2]);

        // My own extents around my own pivot.
        float myLeft = rect.rect.xMin;
        float myRight = rect.rect.xMax;
        float myBottom = rect.rect.yMin;
        float myTop = rect.rect.yMax;

        // X positions where the sprite is completely outside the bounds.
        offLeftX = bottomLeft.x - horizontalPadding - myRight;
        offRightX = topRight.x + horizontalPadding - myLeft;

        // Height range: padded range for spawning, hard limits so drift/bob
        // can never push the sprite outside the parent.
        minSpawnY = bottomLeft.y + verticalPadding - myBottom;
        maxSpawnY = topRight.y - verticalPadding - myTop;
        if (minSpawnY > maxSpawnY)
            minSpawnY = maxSpawnY = (minSpawnY + maxSpawnY) * 0.5f;

        minClampY = bottomLeft.y - myBottom;
        maxClampY = topRight.y - myTop;
        if (minClampY > maxClampY)
            minClampY = maxClampY = (minClampY + maxClampY) * 0.5f;
    }

    private void OnValidate()
    {
        if (maxSpeed < minSpeed) maxSpeed = minSpeed;
        if (maxRespawnDelay < minRespawnDelay) maxRespawnDelay = minRespawnDelay;
    }
}
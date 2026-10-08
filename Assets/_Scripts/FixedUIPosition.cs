using UnityEngine;

/// <summary>
/// Pins a UI element to a fixed point on the screen regardless of its hierarchy —
/// works with any depth of parents, scaled/rotated/animated parents,
/// any Canvas render mode, with or without a Canvas Scaler.
///
/// Note: the element's PIVOT is what lands on the target point
/// (set the pivot in the RectTransform to choose which part gets pinned).
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
[ExecuteAlways]
public class FixedUIPosition : MonoBehaviour
{
    [Header("Target point (screen pixels, origin = bottom-left)")]
    [Tooltip("(0,0) is the bottom-left corner of the screen.")]
    [SerializeField] private Vector2 screenPosition = new Vector2(100f, 100f);

    [Header("Use normalized 0-1 coordinates instead of pixels")]
    [SerializeField] private bool useNormalized = false;
    [SerializeField, Range(0f, 1f)] private float normalizedX = 0.5f;
    [SerializeField, Range(0f, 1f)] private float normalizedY = 0.5f;

    [Header("Behaviour")]
    [Tooltip("Keep re-pinning every frame. Leave ON if parents move/animate or resolution can change.")]
    [SerializeField] private bool pinEveryFrame = true;

    [Tooltip("Optional. Only needed for World Space canvases with multiple cameras.")]
    [SerializeField] private Camera cameraOverride;

    private RectTransform rectTransform;
    private Canvas rootCanvas;

    // ---------------------------------------------------------------- setup

    private void Awake() => CacheReferences();

    private void OnEnable()
    {
        CacheReferences();
        Apply();
    }

    private void OnTransformParentChanged()
    {
        CacheReferences();
        Apply();
    }

    private void LateUpdate()
    {
        if (pinEveryFrame) Apply();
    }

    // ---------------------------------------------------------------- core

    [ContextMenu("Apply Position Now")]
    public void Apply()
    {
        if (rootCanvas == null && !CacheReferences())
        {
            Debug.LogWarning($"[PinToScreenPoint] '{name}' has no Canvas in its parents.", this);
            return;
        }

        Vector2 screenPos = useNormalized
            ? new Vector2(normalizedX * Screen.width, normalizedY * Screen.height)
            : screenPosition;

        // Convert the screen point into a world position on the canvas plane...
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rootCanvas.transform as RectTransform,
                screenPos,
                GetUICamera(),
                out Vector3 worldPoint))
        {
            // ...then assign it directly as a WORLD position.
            // Going through world space bypasses every parent's
            // anchors, offsets, rotations and scales automatically.
            rectTransform.position = worldPoint;
        }
    }

    /// <summary>Pin to a position in screen pixels (origin = bottom-left).</summary>
    public void SetScreenPosition(Vector2 pixels)
    {
        screenPosition = pixels;
        useNormalized = false;
        Apply();
    }

    /// <summary>Pin to a position as fractions of the screen (0-1, bottom-left origin).</summary>
    public void SetNormalizedPosition(float x, float y)
    {
        normalizedX = Mathf.Clamp01(x);
        normalizedY = Mathf.Clamp01(y);
        useNormalized = true;
        Apply();
    }

    // ---------------------------------------------------------------- helpers

    private bool CacheReferences()
    {
        rectTransform = (RectTransform)transform;
        Canvas canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;
        return rootCanvas != null;
    }

    private Camera GetUICamera()
    {
        // Screen Space Overlay MUST pass null as the camera.
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (cameraOverride != null)
            return cameraOverride;

        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
    }
}
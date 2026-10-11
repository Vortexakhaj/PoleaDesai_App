using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class DynamicCanvasScaler : MonoBehaviour
{
    [Header("Target Design Dimensions")]
    [Tooltip("The width you designed your UI around (e.g., 2160 for Portrait, 3840 for Landscape)")]
    public float targetWidth = 2160f;
    [Tooltip("The height you designed your UI around (e.g., 3840 for Portrait, 2160 for Landscape)")]
    public float targetHeight = 3840f;

    [Header("References")]
    [Tooltip("The main container holding all your game UI elements")]
    public RectTransform holderObject;

    private CanvasScaler canvasScaler;
    private AspectRatioFitter aspectFitter;

    // Tracker variables to monitor window adjustments
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();

        // Initialize base canvas scaler properties
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(targetWidth, targetHeight);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        InitializeHolderLayout();

        // Force the first-pass calculation on boot
        UpdateLayoutDimensions();
    }

    void Update()
    {
        // Actively check if the window properties changed this frame
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateLayoutDimensions();
        }
    }

    private void InitializeHolderLayout()
    {
        if (holderObject == null) return;

        aspectFitter = holderObject.GetComponent<AspectRatioFitter>();
        if (aspectFitter == null)
        {
            aspectFitter = holderObject.gameObject.AddComponent<AspectRatioFitter>();
        }

        // Maintain aspect ratio inside parent constraints
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = targetWidth / targetHeight;

        // Force the layout anchors to center-stretch and reset manual properties
        holderObject.anchorMin = new Vector2(0.5f, 0.5f);
        holderObject.anchorMax = new Vector2(0.5f, 0.5f);
        holderObject.pivot = new Vector2(0.5f, 0.5f);
        holderObject.sizeDelta = Vector2.zero;
    }

    private void UpdateLayoutDimensions()
    {
        // Cache current frame configurations to stop redundant execution
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        // Safety fallback check
        if (lastScreenHeight <= 0 || targetHeight <= 0) return;

        float physicalAspect = (float)lastScreenWidth / lastScreenHeight;
        float targetAspect = targetWidth / targetHeight;
        bool isTargetLandscape = targetAspect >= 1f;

        if (isTargetLandscape)
        {
            // Landscape Design: 
            // Narrow physical display (e.g., portrait phone) -> Match Width (0) to fit side boundaries
            // Wide physical display (e.g., ultra-wide monitor) -> Match Height (1) to keep scale uniform
            canvasScaler.matchWidthOrHeight = (physicalAspect < targetAspect) ? 0f : 1f;
        }
        else
        {
            // Portrait Design (Your 2160x3840 setup):
            // Wide physical display (e.g., landscape monitor) -> Match Height (1) to fit vertical boundaries
            // Taller physical display (e.g., long aspect phone) -> Match Width (0) to protect side space
            canvasScaler.matchWidthOrHeight = (physicalAspect > targetAspect) ? 1f : 0f;
        }

        // Force the Canvas system to immediately re-evaluate layout matrices 
        // to prevent a one-frame visual pop during window snapping
        Canvas.ForceUpdateCanvases();
    }
}

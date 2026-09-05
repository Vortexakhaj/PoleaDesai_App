using UnityEngine;
using UnityEngine.UI;

public static class UIBlocker
{
    /// <summary>
    /// Creates a full-screen blocker.
    /// If a popup is provided, it ensures the popup renders above the blocker (like TMP_Dropdown).
    /// If no popup is provided, it acts as a full-screen input lock (e.g., for loading screens).
    /// </summary>
    /// <param name="rootCanvas">The canvas to overlay. If null, it will try to find it from the popup.</param>
    /// <param name="popupToElevate">Optional. The panel you want to keep above the blocker.</param>
    /// <param name="onBlockerClicked">Callback invoked when the blocker is clicked.</param>
    /// <param name="sortingOrderOffset">How much higher than the root canvas the blocker should be.</param>
    /// <param name="tintColor">Optional background tint.</param>
    public static GameObject CreateBlocker(
        Canvas rootCanvas = null,
        RectTransform popupToElevate = null,
        System.Action onBlockerClicked = null,
        int sortingOrderOffset = 1,
        Color? tintColor = null)
    {
        // 1. Resolve Root Canvas
        if (rootCanvas == null && popupToElevate != null)
            rootCanvas = GetRootCanvas(popupToElevate);

        if (rootCanvas == null)
        {
            Debug.LogError("UIBlocker: Could not find a root Canvas. Pass one explicitly or via a popup.");
            return null;
        }

        // 2. Create the Blocker GameObject
        GameObject blocker = new GameObject("Blocker");
        RectTransform blockerRect = blocker.AddComponent<RectTransform>();

        blockerRect.SetParent(rootCanvas.transform, false);
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        Canvas blockerCanvas = blocker.AddComponent<Canvas>();
        blockerCanvas.sortingOrder = rootCanvas.sortingOrder + sortingOrderOffset;
        blockerCanvas.pixelPerfect = rootCanvas.pixelPerfect;

        blocker.AddComponent<GraphicRaycaster>();

        Image blockerImage = blocker.AddComponent<Image>();
        blockerImage.color = tintColor ?? Color.clear;
        blockerImage.raycastTarget = true;

        Button blockerButton = blocker.AddComponent<Button>();
        blockerButton.transition = Selectable.Transition.None;

        if (onBlockerClicked != null)
            blockerButton.onClick.AddListener(() => onBlockerClicked.Invoke());

        // 3. Handle Popup Elevation (Only if a popup was provided)
        if (popupToElevate != null)
        {
            EnsurePopupRendersAboveBlocker(popupToElevate, rootCanvas, sortingOrderOffset);
        }

        return blocker;
    }

    public static void DestroyBlocker(GameObject blocker)
    {
        if (blocker != null)
            Object.Destroy(blocker);
    }

    private static Canvas GetRootCanvas(RectTransform rect)
    {
        Canvas[] canvases = rect.GetComponentsInParent<Canvas>();
        if (canvases.Length > 0)
            return canvases[canvases.Length - 1];
        return null;
    }

    private static void EnsurePopupRendersAboveBlocker(RectTransform popup, Canvas rootCanvas, int offset)
    {
        Canvas popupCanvas = popup.GetComponent<Canvas>();
        bool wasJustAdded = false;

        // If popup lacks a Canvas, add one dynamically
        if (popupCanvas == null)
        {
            popupCanvas = popup.gameObject.AddComponent<Canvas>();

            // Ensure the popup has a raycaster so its buttons work
            if (popup.GetComponent<GraphicRaycaster>() == null)
                popup.gameObject.AddComponent<GraphicRaycaster>();

            wasJustAdded = true;
        }

        // Force the popup to sort above the blocker
        popupCanvas.overrideSorting = true;
        popupCanvas.sortingOrder = rootCanvas.sortingOrder + offset + 1;

        if (wasJustAdded)
            Debug.Log($"UIBlocker: Automatically added Canvas to '{popup.name}' to ensure it renders above the blocker.");
    }
}
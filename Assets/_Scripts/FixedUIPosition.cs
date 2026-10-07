using UnityEngine;

/// <summary>
/// Keeps this RectTransform visually locked in its initial world position, 
/// rotation, and scale, even if its parent moves.
/// </summary>
[ExecuteAlways] // Allows it to work in Edit Mode if you want to preview it
public class FixedUIPosition : MonoBehaviour
{
    [Tooltip("Lock the UI element to its starting world position.")]
    public bool lockPosition = true;

    [Tooltip("Lock the UI element to its starting world rotation.")]
    public bool lockRotation = true;

    private RectTransform rectTransform;
    private Vector3 initialWorldPosition;
    private Quaternion initialWorldRotation;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        StoreInitialState();
    }

    void OnEnable()
    {
        // Recapture state in case the object was moved while disabled
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        StoreInitialState();
    }

    void LateUpdate()
    {
        if (rectTransform == null) return;

        // If the parent has moved, force this rect back to its world position
        if (lockPosition && rectTransform.position != initialWorldPosition)
        {
            rectTransform.position = initialWorldPosition;
        }

        // If the parent has rotated, force this rect back to its world rotation
        if (lockRotation && rectTransform.rotation != initialWorldRotation)
        {
            rectTransform.rotation = initialWorldRotation;
        }
    }

    private void StoreInitialState()
    {
        if (rectTransform != null)
        {
            initialWorldPosition = rectTransform.position;
            initialWorldRotation = rectTransform.rotation;
        }
    }

    /// <summary>
    /// Call this method if you intentionally want to move the UI element 
    /// to a new "locked" position via code.
    /// </summary>
    public void UpdateLockedPosition(Vector3 newWorldPosition)
    {
        initialWorldPosition = newWorldPosition;
        rectTransform.position = newWorldPosition;
    }
}
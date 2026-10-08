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

    private Vector3 initialWorldPosition;
    private Quaternion initialWorldRotation;

    void Awake()
    {
        StoreInitialState();
    }

    void OnEnable()
    {
        StoreInitialState();
    }

    void LateUpdate()
    {

        // If the parent has moved, force this rect back to its world position
        if (lockPosition && transform.position != initialWorldPosition)
        {
            transform.position = initialWorldPosition;
        }

        // If the parent has rotated, force this rect back to its world rotation
        if (lockRotation && transform.rotation != initialWorldRotation)
        {
            transform.rotation = initialWorldRotation;
        }
    }

    private void StoreInitialState()
    {
        initialWorldPosition = transform.position;
        initialWorldRotation = transform.rotation;
    }

    /// <summary>
    /// Call this method if you intentionally want to move the UI element 
    /// to a new "locked" position via code.
    /// </summary>
    public void UpdateLockedPosition(Vector3 newWorldPosition)
    {
        transform.position = initialWorldPosition = newWorldPosition;
    }
}
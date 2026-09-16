using UnityEngine;
using UnityEngine.UI;

public class UIScrollImage : MonoBehaviour
{
    public enum ScrollType { TransformMovement, UVScrolling }
    public enum Direction { Up, Down, Left, Right, Custom }

    [Header("Scroll Settings")]
    [Tooltip("TransformMovement moves the UI element. UVScrolling moves the texture inside the UI element (requires a RawImage or repeating texture).")]
    public ScrollType scrollType = ScrollType.TransformMovement;

    public Direction direction = Direction.Left;
    public float speed = 100f;

    [Tooltip("Used only if Direction is set to Custom.")]
    public Vector2 customDirection = new Vector2(-1f, 0f);

    private RectTransform rectTransform;
    private RawImage rawImage;
    private Material imageMaterial;
    private Vector2 defaultPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();

        // Store the starting position so we can reset it if needed
        defaultPosition = rectTransform.anchoredPosition;

        // If using UV Scrolling on a standard Image, we need to instance the material so we don't change the shared asset
        Image img = GetComponent<Image>();
        if (img != null && scrollType == ScrollType.UVScrolling)
        {
            imageMaterial = Instantiate(img.material);
            img.material = imageMaterial;
        }
    }

    void Update()
    {
        Vector2 dir = GetDirectionVector();

        if (scrollType == ScrollType.TransformMovement)
        {
            // Move the UI element continuously
            rectTransform.anchoredPosition += dir * speed * Time.deltaTime;
        }
        else if (scrollType == ScrollType.UVScrolling)
        {
            // Move the texture inside the UI element continuously
            if (rawImage != null)
            {
                Rect uvRect = rawImage.uvRect;
                uvRect.position += dir * speed * Time.deltaTime;
                rawImage.uvRect = uvRect;
            }
            else if (imageMaterial != null)
            {
                Vector2 offset = imageMaterial.mainTextureOffset;
                offset += dir * speed * Time.deltaTime;
                imageMaterial.mainTextureOffset = offset;
            }
        }
    }

    private Vector2 GetDirectionVector()
    {
        switch (direction)
        {
            case Direction.Up: return Vector2.up;
            case Direction.Down: return Vector2.down;
            case Direction.Left: return Vector2.left;
            case Direction.Right: return Vector2.right;
            case Direction.Custom: return customDirection.normalized; // Normalized so speed isn't doubled on diagonals
            default: return Vector2.zero;
        }
    }

    /// <summary>
    /// Call this method from other scripts or UI buttons to reset the image to its starting position.
    /// </summary>
    public void ResetPosition()
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = defaultPosition;

        if (rawImage != null)
            rawImage.uvRect = new Rect(0, 0, 1, 1);

        if (imageMaterial != null)
            imageMaterial.mainTextureOffset = Vector2.zero;
    }
}
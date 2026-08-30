using ThisOtherThing.UI.Shapes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class MemoryBubble : MonoBehaviour, IPointerClickHandler
{
    [Header("Size Settings")]
    public float minSize = 1.0f;
    public float maxSize = 3.0f;
    public float growthStep = 0.2f;
    public float scaleSpeed = 2f;

    [Header("Selection Count Settings")]
    public int thresholdClickCount = 100;

    [Header("Selection Visuals")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.8f, 0.4f);

    [HideInInspector]
    public int selectionCount = 0;

    [SerializeField] private bool isCurrentlySelected = false;
    private Vector2 targetScale;
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Rectangle bubbleImage;
    private BubbleManager manager;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        bubbleImage = GetComponent<Rectangle>();
        manager = FindFirstObjectByType<BubbleManager>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (bubbleImage != null)
        {
            bubbleImage.ShapeProperties.DrawOutline = false;
            bubbleImage.ForceMeshUpdate();
        }
        UpdateTargetScale();
        transform.localScale = targetScale;
    }

    void Update()
    {
        if (transform.localScale.x != targetScale.x)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
            //col.radius = Mathf.Lerp(col.radius, targetScale.x * 0.5f, Time.deltaTime * scaleSpeed);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isCurrentlySelected)
        {
            // DESELECT LOGIC (User clicked a bubble they already selected)
            ForceDeselect(); // Reuse this method to avoid duplicate code
            if (manager != null) manager.RegisterDeselection(this);
        }
        else
        {
            // SELECT LOGIC: Ask manager for permission and pass "this" bubble
            if (manager != null && manager.RequestSelection(this))
            {
                ActivateSelectionVisuals();
            }
            else
            {
                // Permission denied (e.g., maxConcurrentSelections is set to 0)
                Debug.Log("Cannot select this bubble. Selections disabled.");
            }
        }
    }

    // Turns on the visual selection state
    private void ActivateSelectionVisuals()
    {
        isCurrentlySelected = true;
        if (bubbleImage != null)
        {
            bubbleImage.ShapeProperties.DrawOutline = true;
            bubbleImage.ForceMeshUpdate();
        }
    }

    // The Manager calls this to remotely kick the oldest bubble out of the selection
    public void ForceDeselect()
    {
        isCurrentlySelected = false;
        if (bubbleImage != null)
        {
            bubbleImage.ShapeProperties.DrawOutline = false;
            bubbleImage.ForceMeshUpdate();
        }
    }

    public void ConfirmSelection()
    {
        if (isCurrentlySelected)
        {
            selectionCount++;
            if (selectionCount % thresholdClickCount == 0)
            {
                GrowBubble();
            }
        }
        // Reset the visual selection state for the next visitor
        ForceDeselect();
    }

    private void GrowBubble()
    {
        float newScale = targetScale.x + growthStep;
        if (newScale > maxSize) newScale = maxSize;
        targetScale = new Vector2(newScale, newScale);
    }

    public void ResetBubble()
    {
        selectionCount = 0;
        UpdateTargetScale();
    }

    public void InitializeFromData(int loadedCount)
    {
        selectionCount = loadedCount;
        UpdateTargetScale();
    }

    private void UpdateTargetScale()
    {
        int steps = selectionCount / thresholdClickCount;
        float calculatedSize = minSize + (steps * growthStep);

        if (calculatedSize > maxSize) calculatedSize = maxSize;
        targetScale = new Vector2(calculatedSize, calculatedSize);
    }
}
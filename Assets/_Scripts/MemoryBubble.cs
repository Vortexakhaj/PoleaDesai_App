using ThisOtherThing.UI.Shapes;
using TMPro;
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
    public TextMeshProUGUI counterText;

    [Header("Selection Visuals")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.8f, 0.4f);

    [HideInInspector]
    private int selectionCount = 0;
    public int SelectionCount
    {
        get { return selectionCount; }
        set
        {
            selectionCount = value;
            counterText.text = selectionCount.ToString();
        }
    }

    [SerializeField] private bool isCurrentlySelected = false;
    public Vector2 targetScale;
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Rectangle bubbleImage;
    private BubbleManager manager;

    private void Awake()
    {
        manager = FindFirstObjectByType<BubbleManager>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        bubbleImage = GetComponent<Rectangle>();

        normalColor = bubbleImage.ShapeProperties.FillColor;
        selectedColor = DarkenViaHSV(normalColor, 0, 0.3f, 1f);

        counterText.color = DarkenViaHSV(normalColor, 0f, 0f, -0.2f);

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Start()
    {
        if (bubbleImage != null)
        {
            FillColor(normalColor);
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

    Color DarkenViaHSV(Color color, float h_reduceValueBy = 0, float s_reduceValueBy = 0, float v_reduceValueBy = 0)
    {
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);

        // Subtract from the brightness component
        h = Mathf.Clamp01(h + h_reduceValueBy);
        s = Mathf.Clamp01(s + s_reduceValueBy);
        v = Mathf.Clamp01(v + v_reduceValueBy);

        return Color.HSVToRGB(h, s, v);
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

    private void FillColor(Color color)
    {
        bubbleImage.ShapeProperties.FillColor = color;
        bubbleImage.ForceMeshUpdate();
    }

    // Turns on the visual selection state
    private void ActivateSelectionVisuals()
    {
        isCurrentlySelected = true;
        SelectionCount++;
        if (bubbleImage != null)
        {
            FillColor(selectedColor);
        }
    }

    // The Manager calls this to remotely kick the oldest bubble out of the selection
    public void ForceDeselect()
    {
        isCurrentlySelected = false;
        SelectionCount--;
        if (bubbleImage != null)
        {
            FillColor(normalColor);
        }
    }

    public void ConfirmSelection()
    {
        if (isCurrentlySelected)
        {
            if (SelectionCount % Mathf.Max(thresholdClickCount, 1) == 0)
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
        SelectionCount = 0;
        UpdateTargetScale();
    }

    public void InitializeFromData(int loadedCount)
    {
        SelectionCount = loadedCount;
        UpdateTargetScale();
    }

    private void UpdateTargetScale()
    {
        int steps = SelectionCount / Mathf.Max(thresholdClickCount, 1);
        float calculatedSize = minSize + (steps * growthStep);

        if (calculatedSize > maxSize) calculatedSize = maxSize;
        targetScale = new Vector2(calculatedSize, calculatedSize);
    }
}
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WindEffectController : MonoBehaviour
{
    // --- DIRECTION DROPDOWN ---
    public enum WindDirection
    {
        LeftToRight = 0,
        RightToLeft = 1,
        BottomToTop = 2,
        TopToBottom = 3,
        DiagBL_To_TR = 4,
        DiagTL_To_BR = 5,
        DiagBR_To_TL = 6,
        DiagTR_To_BL = 7
    }

    [Header("Direction Settings")]
    [Tooltip("Select which way the wind blows.")]
    public WindDirection direction = WindDirection.TopToBottom;

    [Header("Animation Settings")]
    [Tooltip("How many seconds the reveal/hide takes.")]
    public float duration = 3f;

    [Tooltip("If true, it will continuously Reveal -> Hide -> Reveal. If false, it plays Reveal -> Hide once and stops.")]
    public bool loop = false;

    private Image targetImage;
    private Material instancedMat;
    private float currentTime = 0f;
    private bool isHiding = false;
    private bool isFinished = false;

    private void Awake()
    {
        targetImage = GetComponent<Image>();

        // Create an instance of the material so every Image animates independently
        if (targetImage.material != null)
        {
            instancedMat = new Material(targetImage.material);
            targetImage.material = instancedMat;
        }
        else
        {
            Debug.LogError("No material assigned to the Image component!", this);
        }
    }

    private void OnEnable()
    {
        // Reset everything when the object is enabled
        currentTime = 0f;
        isHiding = false;
        isFinished = false;

        if (instancedMat != null)
        {
            instancedMat.SetFloat("_Progress", 0f);
            instancedMat.SetFloat("_IsHiding", 0f);
            // Apply the direction selected in the inspector
            instancedMat.SetFloat("_Direction", (float)direction);
        }
    }

    private void Update()
    {
        if (instancedMat == null || isFinished) return;

        // Push the direction to the material every frame. 
        // This allows you to change the direction in the inspector during Play Mode for testing.
        instancedMat.SetFloat("_Direction", (float)direction);

        // Increase timer
        currentTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentTime / duration);

        // Push values to the shader
        instancedMat.SetFloat("_Progress", progress);
        instancedMat.SetFloat("_IsHiding", isHiding ? 1f : 0f);

        // Check if the current phase (reveal or hide) is complete
        if (currentTime >= duration)
        {
            if (!isHiding)
            {
                // Just finished Revealing. ALWAYS transition to Hiding next.
                isHiding = true;
                currentTime = 0f;
            }
            else
            {
                // Just finished Hiding.
                if (loop)
                {
                    // If looping, start revealing again.
                    isHiding = false;
                    currentTime = 0f;
                }
                else
                {
                    // If NOT looping, the Reveal->Hide cycle is complete. Stop updating.
                    isFinished = true;
                }
            }
        }
    }

    public void PlayWindEffect()
    {
        currentTime = 0f;
        isHiding = false;
        isFinished = false;

        if (instancedMat != null)
        {
            instancedMat.SetFloat("_Progress", 0f);
            instancedMat.SetFloat("_IsHiding", 0f);
            instancedMat.SetFloat("_Direction", (float)direction);
        }
    }

    private void OnDestroy()
    {
        // Clean up instantiated material to prevent memory leaks
        if (instancedMat != null)
        {
            Destroy(instancedMat);
        }
    }
}
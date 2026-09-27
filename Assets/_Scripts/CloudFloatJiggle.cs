using UnityEngine;

/// <summary>
/// Idle animation (float, sway, squash & stretch, wobble).
/// If a UIHorizontalWanderer is on the same object, this layers ON TOP of it
/// (runs in LateUpdate, after the wanderer has moved things).
/// Works standalone too — just falls back to the editor pose as the base.
/// </summary>
public class CloudFloatJiggle : MonoBehaviour
{
    public enum CloudPreset
    {
        Custom,
        SoftSleepy,
        BouncyCartoon
    }

    [Header("Preset")]
    [Tooltip("Select a preset to auto-fill values below. Switch back to Custom to tweak manually.")]
    [SerializeField] private CloudPreset preset = CloudPreset.Custom;

    [Header("Float (Vertical)")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed = 1.5f;

    [Header("Sway (Horizontal)")]
    [SerializeField] private float swayAmplitude = 0.1f;
    [SerializeField] private float swaySpeed = 1.0f;

    [Header("Jiggle (Squash & Stretch)")]
    [Tooltip("How much the cloud squishes. Keep small (0.02 - 0.1) for soft clouds.")]
    [SerializeField] private float jiggleAmount = 0.05f;
    [SerializeField] private float jiggleSpeed = 2.0f;

    [Header("Rotation Wobble")]
    [SerializeField] private float rotationAmplitude = 2f;
    [SerializeField] private float rotationSpeed = 0.8f;

    [Header("Misc")]
    [Tooltip("Randomizes phase so multiple clouds don't move in sync.")]
    [SerializeField] private bool randomPhase = true;
    [SerializeField] private float phaseOffset = 0f;

    [Tooltip("Keep in sync with the wanderer's 'Use Unscaled Time' setting.")]
    [SerializeField] private bool useUnscaledTime = false;

    private Vector3 _startPosition;
    private Vector3 _startScale;
    private float _phase;
    private UIHorizontalWanderer _wanderer;

    // Tracking variable to detect when the dropdown changes in the Inspector
    private CloudPreset _lastPreset;

    private void Awake()
    {
        _startPosition = transform.localPosition;
        _startScale = transform.localScale;
        _phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : phaseOffset;
        _wanderer = GetComponent<UIHorizontalWanderer>();
    }

    private void LateUpdate()
    {
        float t = (useUnscaledTime ? Time.unscaledTime : Time.time) + _phase;

        // Base transform: whatever the wanderer last wrote, or the editor pose
        // if this object isn't travelling.
        Vector3 basePos = _wanderer != null ? _wanderer.BaseLocalPosition : _startPosition;
        Vector3 baseScale = _wanderer != null ? _wanderer.BaseLocalScale : _startScale;

        // --- Position: float + sway, added on top of the base ---
        float xOffset = Mathf.Sin(t * swaySpeed) * swayAmplitude;
        float yOffset = Mathf.Sin(t * floatSpeed) * floatAmplitude;
        transform.localPosition = basePos + new Vector3(xOffset, yOffset, 0f);

        // --- Scale: jelly-like squash & stretch around the base scale ---
        // (Base scale includes the wanderer's direction flip, so flipping still works.)
        float jiggle = Mathf.Sin(t * jiggleSpeed) * jiggleAmount;
        transform.localScale = new Vector3(
            baseScale.x * (1f + jiggle),
            baseScale.y * (1f - jiggle),
            baseScale.z
        );

        // --- Rotation: gentle wobble (wanderer never touches rotation) ---
        float rotZ = Mathf.Sin(t * rotationSpeed) * rotationAmplitude;
        transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    private void OnDisable()
    {
        // Only reset when standalone. When driven by the wanderer, resetting
        // here would yank the cloud back to its editor position mid-flight.
        // (Everything is recomputed every frame, so re-enabling never "jumps".)
        if (_wanderer == null)
        {
            transform.localPosition = _startPosition;
            transform.localScale = _startScale;
            transform.localRotation = Quaternion.identity;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (preset != _lastPreset)
        {
            _lastPreset = preset;

            switch (preset)
            {
                case CloudPreset.SoftSleepy:
                    floatAmplitude = 0.15f; floatSpeed = 1.0f;
                    swayAmplitude = 0.05f; swaySpeed = 0.6f;
                    jiggleAmount = 0.03f; jiggleSpeed = 1.2f;
                    rotationAmplitude = 1f; rotationSpeed = 0.5f;
                    break;

                case CloudPreset.BouncyCartoon:
                    floatAmplitude = 0.3f; floatSpeed = 2.0f;
                    swayAmplitude = 0.15f; swaySpeed = 1.5f;
                    jiggleAmount = 0.08f; jiggleSpeed = 3.5f;
                    rotationAmplitude = 3f; rotationSpeed = 1.2f;
                    break;

                case CloudPreset.Custom:
                    // Does nothing on purpose — keeps the current values so you
                    // can tweak from the last preset as a starting point.
                    break;
            }
        }
    }
#endif
}
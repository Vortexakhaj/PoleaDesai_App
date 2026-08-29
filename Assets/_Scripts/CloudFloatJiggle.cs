using UnityEngine;

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

    private Vector3 _startPosition;
    private Vector3 _startScale;
    private float _phase;

    // Tracking variable to detect when the dropdown changes in the Inspector
    private CloudPreset _lastPreset;

    private void Awake()
    {
        _startPosition = transform.localPosition;
        _startScale = transform.localScale;
        _phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : phaseOffset;
    }

    private void Update()
    {
        float t = Time.time + _phase;

        // --- Position: float + sway ---
        float xOffset = Mathf.Sin(t * swaySpeed) * swayAmplitude;
        float yOffset = Mathf.Sin(t * floatSpeed) * floatAmplitude;
        transform.localPosition = _startPosition + new Vector3(xOffset, yOffset, 0f);

        // --- Scale: jelly-like squash & stretch ---
        // X and Y are opposite phase so total area feels preserved.
        float jiggle = Mathf.Sin(t * jiggleSpeed) * jiggleAmount;
        transform.localScale = new Vector3(
            _startScale.x * (1f + jiggle),
            _startScale.y * (1f - jiggle),
            _startScale.z
        );

        // --- Rotation: gentle wobble ---
        float rotZ = Mathf.Sin(t * rotationSpeed) * rotationAmplitude;
        transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    private void OnDisable()
    {
        // Prevent visual "jump" if the object is re-enabled later.
        transform.localPosition = _startPosition;
        transform.localScale = _startScale;
        transform.localRotation = Quaternion.identity;
    }

    // This runs in the Unity Editor whenever a value is changed in the Inspector
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Only apply presets if the dropdown was actually changed
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
                    floatAmplitude = 0.2f; floatSpeed = 1.5f;
                    swayAmplitude = 0.1f; swaySpeed = 1f;
                    jiggleAmount = 0.05f; jiggleSpeed = 2.0f;
                    rotationAmplitude = 2f; rotationSpeed = 0.8f;
                    break;
            }
        }
    }
#endif
}
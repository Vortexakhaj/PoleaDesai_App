using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ClockSimulation : MonoBehaviour
{
    public enum TimeMode
    {
        SystemTime,
        CustomTime
    }
   
    [Tooltip("Hour hand Transform. The sprite's pivot must be at the hand's base (the clock's centre).")]
    [SerializeField] private Transform hourHand;
    [Tooltip("Minute hand Transform. The sprite's pivot must be at the hand's base (the clock's centre).")]
    [SerializeField] private Transform minuteHand;
   
    [Tooltip("SystemTime mirrors your real clock. CustomTime simulates time at customTimeSpeed.")]
    [SerializeField] private TimeMode mode = TimeMode.SystemTime;

    [Tooltip("CustomTime: in-game minutes per real second (60 = 1 hour per real second). Negative = runs backwards.")]
    [SerializeField] private float customTimeSpeed = 10f;

    [Tooltip("CustomTime: start the simulated clock at the current system time instead of 12:00.")]
    [SerializeField] private bool startAtSystemTime = false;
    
    [Tooltip("The hand sprite's Z rotation when it points at 12:00: 0 = art points up, 90 = right, 180 = down, 270 = left.")]
    [SerializeField] private float twelveOClockAngle = 0f;

    // Anchor-based custom time: computed from real elapsed time each frame, never accumulated,
    // so frame drops, long hitches, and timeScale changes cannot make it drift or lose time.
    private double _anchorSimMinutes;
    private double _anchorRealSeconds;  // Time.realtimeSinceStartupAsDouble at the anchor moment
    private TimeMode _lastMode;         // detects mid-play mode changes
    private float _lastSpeed;           // detects mid-play speed changes
    private double _lastMinutes;        // time currently shown on the face

    private const double MinutesPer12Hours = 720.0;

    /// <summary>Current time on the clock face, in minutes past 12:00 (0-720). Used by the custom inspector.</summary>
    public double CurrentMinutes => _lastMinutes;

    private void Awake()
    {
        if (hourHand == null || minuteHand == null)
            Debug.LogError($"ClockSimulation on '{name}': assign both hand transforms.", this);
    }

    private void Start()
    {
        _lastMinutes = (mode == TimeMode.CustomTime && startAtSystemTime) ? GetSystemMinutes() : 0.0;
        _lastMode = mode;
        ReAnchor(_lastMinutes);
    }

    private void Update()
    {
        if (hourHand == null || minuteHand == null) return;

        double totalMinutes;

        if (mode == TimeMode.SystemTime)
        {
            // Reading the OS clock every frame is inherently frame-drop-proof:
            // a dropped frame only means the hands jump straight to the correct spot.
            totalMinutes = GetSystemMinutes();
        }
        else
        {
            // Re-anchor on mode/speed change so hands keep gliding from wherever they are,
            // instead of teleporting back to 12:00 or jumping.
            if (mode != _lastMode || !Mathf.Approximately(_lastSpeed, customTimeSpeed))
                ReAnchor(_lastMinutes);

            totalMinutes = Wrap12h(_anchorSimMinutes
                + (Time.realtimeSinceStartupAsDouble - _anchorRealSeconds) * customTimeSpeed);
        }

        _lastMode = mode;
        _lastMinutes = totalMinutes;

        UpdateClockHands(totalMinutes);
    }

    private void ReAnchor(double currentMinutes)
    {
        _anchorSimMinutes = currentMinutes;
        _anchorRealSeconds = Time.realtimeSinceStartupAsDouble;
        _lastSpeed = customTimeSpeed;
    }

    private static double GetSystemMinutes()
    {
        DateTime now = DateTime.Now;
        return now.Hour * 60.0 + now.Minute + now.Second / 60.0 + now.Millisecond / 60000.0;
    }

    private static double Wrap12h(double minutes)
    {
        minutes %= MinutesPer12Hours;
        return minutes < 0.0 ? minutes + MinutesPer12Hours : minutes;
    }

    private void UpdateClockHands(double totalMinutes)
    {
        // Degrees clockwise from 12:00.
        double minuteAngle = totalMinutes * 6.0;   // 360° / 60 minutes
        double hourAngle = totalMinutes * 0.5;   // 360° / 720 minutes (creeps between numbers)

        // Unity's positive Z rotation is counter-clockwise on screen in a standard 2D scene,
        // so SUBTRACT to make the hands turn clockwise like a real clock.
        minuteHand.localRotation = Quaternion.Euler(0f, 0f, twelveOClockAngle - (float)minuteAngle);
        hourHand.localRotation = Quaternion.Euler(0f, 0f, twelveOClockAngle - (float)hourAngle);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ClockSimulation))]
[CanEditMultipleObjects]
public class ClockSimulationEditor : Editor
{
    private SerializedProperty _script;
    private SerializedProperty _hourHand;
    private SerializedProperty _minuteHand;
    private SerializedProperty _mode;
    private SerializedProperty _customTimeSpeed;
    private SerializedProperty _startAtSystemTime;
    private SerializedProperty _twelveOClockAngle;

    private void OnEnable()
    {
        _script = serializedObject.FindProperty("m_Script");
        _hourHand = serializedObject.FindProperty("hourHand");
        _minuteHand = serializedObject.FindProperty("minuteHand");
        _mode = serializedObject.FindProperty("mode");
        _customTimeSpeed = serializedObject.FindProperty("customTimeSpeed");
        _startAtSystemTime = serializedObject.FindProperty("startAtSystemTime");
        _twelveOClockAngle = serializedObject.FindProperty("twelveOClockAngle");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Locked script header, like the default inspector.
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(_script, true);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Clock Hands", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_hourHand);
        EditorGUILayout.PropertyField(_minuteHand);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Time Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_mode);

        bool mixed = _mode.hasMultipleDifferentValues;
        bool isCustom = mixed ||
            (ClockSimulation.TimeMode)_mode.enumValueIndex == ClockSimulation.TimeMode.CustomTime;
        bool single = targets.Length == 1;

        if (isCustom)
        {
            // --- CustomTime-only variables ---
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_customTimeSpeed);
            EditorGUILayout.PropertyField(_startAtSystemTime);
            EditorGUI.indentLevel--;

            // Live readout of what the simulated clock is currently showing.
            if (single && !mixed && Application.isPlaying)
                EditorGUILayout.LabelField("Currently Showing", Format(((ClockSimulation)target).CurrentMinutes));
        }
        else
        {
            // --- SystemTime: no settings, just show what it's mirroring ---
            EditorGUILayout.HelpBox("Hands mirror your device clock (DateTime.Now).", MessageType.None);

            if (single && !mixed)
                EditorGUILayout.LabelField("Currently Showing", Format(((ClockSimulation)target).CurrentMinutes));
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Sprite Orientation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_twelveOClockAngle);

        // A live label needs continuous repaints to tick.
        if (single && (Application.isPlaying || !isCustom))
            Repaint();

        serializedObject.ApplyModifiedProperties();
    }

    private static string Format(double totalMinutes)
    {
        int h = (int)(totalMinutes / 60.0) % 12;
        if (h == 0) h = 12;
        int m = (int)(totalMinutes % 60.0);
        int s = (int)(totalMinutes * 60.0 % 60.0);
        return $"{h:00}:{m:00}:{s:00}";
    }
}
#endif
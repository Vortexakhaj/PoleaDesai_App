using UnityEngine;
using System;

public class ClockSimulation : MonoBehaviour
{
    public enum TimeMode
    {
        SystemTime,
        CustomTime
    }

    [Header("Clock Hands")]
    [Tooltip("Drag the Sprite Renderer for the Hour Hand here.")]
    [SerializeField] private Transform hourHand;
    [Tooltip("Drag the Sprite Renderer for the Minute Hand here.")]
    [SerializeField] private Transform minuteHand;

    [Header("Time Settings")]
    [Tooltip("SystemTime uses your real computer clock. CustomTime lets you simulate time passing faster.")]
    [SerializeField] private TimeMode mode = TimeMode.SystemTime;

    [Tooltip("If using CustomTime: How many in-game minutes pass per real-world second. (e.g., 60 = 1 hour per real second)")]
    [SerializeField] private float customTimeSpeed = 10f;

    [Header("Sprite Orientation")]
    [Tooltip("The angle of your sprites in the Inspector when they are pointing at 12:00. Usually 0, but 90 if they point right by default.")]
    [SerializeField] private float twelveOClockAngle = 0f;

    private float _simulatedMinutes = 0f;

    private void Update()
    {
        float totalMinutes;

        if (mode == TimeMode.SystemTime)
        {
            // Get real-world time
            DateTime now = DateTime.Now;
            // Convert to total minutes, including fractional minutes (seconds/milliseconds) for smooth motion
            totalMinutes = (now.Hour * 60f) + now.Minute + (now.Second / 60f) + (now.Millisecond / 60000f);
        }
        else
        {
            // Simulate time passing
            _simulatedMinutes += Time.deltaTime * customTimeSpeed;

            // Wrap around 12 hours (720 minutes) to loop the clock
            _simulatedMinutes = Mathf.Repeat(_simulatedMinutes, 720f);
            totalMinutes = _simulatedMinutes;
        }

        UpdateClockHands(totalMinutes);
    }

    private void UpdateClockHands(float totalMinutes)
    {
        // --- Minute Hand ---
        // 360 degrees / 60 minutes = 6 degrees per minute
        float minuteAngle = totalMinutes * 6f;

        // --- Hour Hand ---
        // 360 degrees / 12 hours (720 minutes) = 0.5 degrees per minute
        // This ensures the hour hand slowly moves between numbers as the minutes increase, exactly like a real clock.
        float hourAngle = totalMinutes * 0.5f;

        // Apply rotations
        minuteHand.localRotation = Quaternion.Euler(0f, 0f, twelveOClockAngle + minuteAngle);
        hourHand.localRotation = Quaternion.Euler(0f, 0f, twelveOClockAngle + hourAngle);
    }
}
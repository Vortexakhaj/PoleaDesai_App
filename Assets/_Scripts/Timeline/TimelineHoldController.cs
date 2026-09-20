using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;

public class TimelineHoldController : MonoBehaviour
{
    [Header("Timeline Settings")]
    [Tooltip("Drag your PlayableDirector here.")]
    public PlayableDirector timelineDirector;

    [Tooltip("If true, the timeline will pause the moment this object is enabled.")]
    public bool pauseTimelineOnEnable = true;

    [Header("Events")]
    [Tooltip("Fires when the timeline is resumed.")]
    public UnityEvent onTimelineResumed;

    private void OnEnable()
    {
        if (pauseTimelineOnEnable && timelineDirector != null)
        {
            // Ensure the timeline is actually playing before pausing it
            if (timelineDirector.state == PlayState.Playing)
            {
                timelineDirector.Pause();
            }
        }
    }

    /// <summary>
    /// Call this method from a UnityEvent to resume the timeline.
    /// </summary>
    public void ResumeTimeline()
    {
        if (timelineDirector != null)
        {
            if (timelineDirector.state == PlayState.Paused)
            {
                timelineDirector.Play();
                onTimelineResumed?.Invoke();
            }
        }
    }
}
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;

public class TimelineSequenceController : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector timelineDirector;
    public UITextTypewriter typewriter;
    public UI_TweenScale_Backward uiScaler;

    [Header("UI (Optional)")]
    public Button playPauseButton;
    public TextMeshProUGUI buttonText;

    // Static reference so your UI button can control whichever frame is currently active
    public static TimelineSequenceController ActiveController;

    private bool isHoldingForText = false;
    private bool isUserPaused = false;
    public bool IsUserPaused => isUserPaused;

    private void Awake()
    {
        if (ActiveController != null)
        {
            Destroy(gameObject);
            return;
        }
        ActiveController = this;
    }

    public void PauseTimeline()
    {
        if (timelineDirector != null && timelineDirector.state == PlayState.Playing)
        {
            timelineDirector.Pause();
            isHoldingForText = true;
            isUserPaused = false;

            if (typewriter != null) typewriter.isPaused = isUserPaused;
            if (uiScaler != null) uiScaler.isPaused = isUserPaused;
            if (buttonText != null) buttonText.text = "Pause";
        }
    }

    public void ResumeTimeline()
    {
        if (timelineDirector != null && timelineDirector.state == PlayState.Paused)
        {
            timelineDirector.Play();
            isHoldingForText = false;
            isUserPaused = false;

            if (typewriter != null) typewriter.isPaused = isUserPaused;
            if (uiScaler != null) uiScaler.isPaused = isUserPaused;
            if (buttonText != null) buttonText.text = "Play";
        }
    }


    /// <summary>
    /// Hook this to the Typewriter's onTypingEnd event.
    /// </summary>
    public void OnTypewriterFinished()
    {
        isHoldingForText = false;

        // If the user hasn't manually paused, resume the timeline
        if (!isUserPaused && timelineDirector != null)
        {
            timelineDirector.Play();
        }
    }

    /// <summary>
    /// Hook your UI Play/Pause Button to this method.
    /// </summary>
    public void TogglePlayPause()
    {
        if (isUserPaused)
        {
            // --- RESUME ---
            isUserPaused = false;
            if (typewriter != null) typewriter.isPaused = false;
            if (uiScaler != null) uiScaler.isPaused = false;

            // Only resume timeline if the text isn't still typing
            if (!isHoldingForText && timelineDirector != null)
                timelineDirector.Play();

            if (buttonText != null) buttonText.text = "Pause";
        }
        else
        {
            // --- PAUSE ---
            isUserPaused = true;
            if (timelineDirector != null) timelineDirector.Pause();
            if (typewriter != null) typewriter.isPaused = true;
            if (uiScaler != null) uiScaler.isPaused = true;

            if (buttonText != null) buttonText.text = "Play";
        }
    }    
    
}
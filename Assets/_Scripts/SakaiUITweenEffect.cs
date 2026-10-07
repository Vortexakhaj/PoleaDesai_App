using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Coffee.UIEffects;

/// Tweens UIEffect TransitionRate, ToneLevel & Rotation from TARGET → FINISH.
/// The tween is rebuilt from the Inspector values on EVERY play,
/// so duration/ease/loop changes always apply — even mid Play mode.
[RequireComponent(typeof(UIEffect))]
[AddComponentMenu("Sakai/UI Effect Tween (Event-Driven)")]
public class SakaiUITweenEffect : MonoBehaviour
{
    [Header("Refs (auto-grabbed if empty)")]
    public UIEffect uiEffect;

    [Header("TARGET (start) values")]
    public float targetTransitionRate = 0f;
    public float targetToneLevel = 0f;
    public float targetRotation = 0f;

    [Header("FINISH (end) values")]
    public float finishTransitionRate = 1f;
    public float finishToneLevel = 1f;
    public float finishRotation = 360f;

    [Header("Tween settings")]
    [Min(0.01f)] public float duration = 0.75f;
    public Ease ease = Ease.OutBack;
    [Min(0f)] public float delay = 0f;      // NOTE: total time = delay + duration
    public bool loop = false;
    public LoopType loopType = LoopType.Yoyo;
    public bool useUnscaledTime = false;
    public bool playOnEnable = false;

    [Header("Events fired by the tween")]
    public UnityEvent onReachedFinish;
    public UnityEvent onReachedTarget;

    [Header("Debug")]
    [Tooltip("Log the duration DOTween actually uses, each time it plays.")]
    public bool logDurationOnPlay = false;

    Tween driver;
    bool goingForward = true;
    bool finishFired, targetFired = true;

    // ───────────────────────────── setup ─────────────────────────────

    void Awake()
    {
        uiEffect = uiEffect ? uiEffect : GetComponent<UIEffect>();       
        Apply(0f);                                 // start snapped at TARGET
    }

    void OnEnable()
    {
        if (playOnEnable) PlayForward();
    }

    // Inspector changed while playing → rebuild now, so tweaks apply live.
    void OnValidate()
    {
        if (Application.isPlaying && driver != null && driver.IsPlaying())
        {
            if (goingForward) PlayForward();
            else PlayBackward();
        }
    }

    // ★ THE FIX: always builds with the CURRENT field values.
    Tween BuildDriver(float durationOverride = -1f)
    {
        if (driver != null && driver.IsActive()) driver.Kill();

        float dur = durationOverride > 0f ? durationOverride : Mathf.Max(0.01f, duration);

        driver = DOTween.To(() => 0f, t => Apply(t), 1f, dur)
            .SetEase(ease)
            .SetDelay(delay)
            .SetLoops(loop ? -1 : 1, loopType)
            .SetUpdate(useUnscaledTime)
            .SetAutoKill(false)
            .Pause();

        if (logDurationOnPlay)
            Debug.Log($"[SakaiTween] {name}: duration = {driver.Duration():0.###}s " +
                      $"(+delay {delay:0.###}s) | tween.timeScale = {driver.timeScale} | " +
                      $"Time.timeScale = {Time.timeScale}", this);

        return driver;
    }

    // t = 0 → TARGET, t = 1 → FINISH
    void Apply(float t)
    {
        if (uiEffect)
        {
            uiEffect.transitionRate = Mathf.LerpUnclamped(targetTransitionRate, finishTransitionRate, t);
            uiEffect.transitionRotation = Mathf.LerpUnclamped(targetRotation, finishRotation, t);
        }       
        if (t >= 1f && !finishFired) { finishFired = true; targetFired = false; onReachedFinish?.Invoke(); }
        else if (t <= 0f && !targetFired) { targetFired = true; finishFired = false; onReachedTarget?.Invoke(); }
    }

    // ─────────────── PUBLIC API — attach these to events ───────────────

    [ContextMenu("Play Forward")]
    public void PlayForward()
    {
        goingForward = true;
        finishFired = false;
        targetFired = true;                       // starting at target: don't re-fire it
        BuildDriver().Restart();
    }

    [ContextMenu("Play Backward")]
    public void PlayBackward()
    {
        goingForward = false;
        finishFired = true;
        targetFired = false;
        Tween d = BuildDriver();
        d.Complete();                             // snap to finish values
        d.PlayBackwards();
    }

    public void ResumeForward() { goingForward = true; if (driver != null && driver.IsActive()) driver.PlayForward(); }
    public void ResumeBackward() { goingForward = false; if (driver != null && driver.IsActive()) driver.PlayBackwards(); }

    public void Toggle() { if (goingForward) PlayBackward(); else PlayForward(); }

    public void Pause() => driver?.Pause();
    public void Resume() { if (driver != null && driver.IsActive()) driver.Play(); }

    public void StopAtTarget() { Tween d = BuildDriver(); d.Rewind(); d.Pause(); }
    public void StopAtFinish() { Tween d = BuildDriver(); d.Complete(); d.Pause(); }

    /// One-off play using a duration supplied by the event. Does NOT touch the Inspector field.
    public void PlayForwardWithDuration(float d)
    {
        goingForward = true;
        finishFired = false;
        targetFired = true;
        BuildDriver(d > 0f ? d : duration).Restart();
    }

    /// Change duration from code; restarts the current leg if it's playing.
    public void SetDuration(float newDuration)
    {
        duration = Mathf.Max(0.01f, newDuration);
        if (Application.isPlaying && driver != null && driver.IsPlaying())
        {
            if (goingForward) PlayForward();
            else PlayBackward();
        }
    }

    void OnDestroy() => driver?.Kill();
}
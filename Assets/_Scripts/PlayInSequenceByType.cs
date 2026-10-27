using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayInSequenceByType : MonoBehaviour
{
    [Flags]
    public enum TweenType
    {
        None = 0,
        Move = 1 << 0,
        Scale = 1 << 1,
        Alpha = 1 << 2,
        Rotation = 1 << 3
    }

    [Header("Targets")]
    public RectTransform[] inOut;
    public bool getIniPos = true;

    [Header("Tween (same as UI_Tween_Backward)")]
    public TweenType tweenType = TweenType.Move | TweenType.Scale;
    public AnimationCurve animCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public AnimationCurve animCurveBackward = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [Tooltip("Playback speed. Time per element = curve last key time / speed")]
    public float speed = 1f;
    [Tooltip("If checked Scale is uniform, else only on Y axis")]
    public bool isUniform = true;
    public bool isLoop = false;
    public bool playAtAwake = false;

    [Header("Sequence")]
    [Tooltip("True: elements play one after another (Append). False: all at once (Join).")]
    public bool appendTweens = true;

    [Header("From values (per element)")]
    public Vector2[] FromPos;
    [SerializeField] Vector2[] initialPos;

    [Header("Start state / visibility")]
    [Tooltip("Snap every element to the curve's start state the moment the sequence starts, so elements waiting for their turn (append mode) aren't sitting fully visible at their final state")]
    public bool primeAtStart = true;
    [Tooltip("Snap elements to their 'from' state as soon as this component enables, so they don't sit fully visible while waiting for StartAnim")]
    public bool primeOnEnable = false;
    [Tooltip("Actually deactivate the GameObjects while fully faded out (value <= threshold) and reactivate them once visible again")]
    public bool deactivateWhenHidden = false;
    public float hiddenAlphaThreshold = 0.01f;

    [Header("Events")]
    public float delayeventby;
    public UnityEvent animEvent;
    public UnityEvent animBackwardEvent;

    // runtime cache
    Vector3[] initScale;
    Quaternion[] initRot;
    float[] initAlpha;
    bool[] initActive;
    CanvasGroup[] canvasGroups;
    Coroutine routine;

    void Awake()
    {
        int n = inOut != null ? inOut.Length : 0;

        if (getIniPos)
        {
            initialPos = new Vector2[n];
            for (int i = 0; i < n; i++)
                if (inOut[i] != null) initialPos[i] = inOut[i].anchoredPosition;
        }

        initScale = new Vector3[n];
        initRot = new Quaternion[n];
        initAlpha = new float[n];
        initActive = new bool[n];
        canvasGroups = new CanvasGroup[n];

        for (int i = 0; i < n; i++)
        {
            if (inOut[i] == null) continue;
            initScale[i] = inOut[i].localScale;
            initRot[i] = inOut[i].localRotation;
            initActive[i] = inOut[i].gameObject.activeSelf;
        }

        // Alpha selected -> make sure every target has a CanvasGroup (auto-add if missing)
        if ((tweenType & TweenType.Alpha) != 0)
            for (int i = 0; i < n; i++)
                if (inOut[i] != null) GetGroup(i);

        if (FromPos != null && FromPos.Length < n)
            Debug.LogWarning("PlayInSequence: FromPos length < inOut length, Move will be skipped for missing entries.", this);
    }

    CanvasGroup GetGroup(int i)
    {
        if (canvasGroups[i] == null)
        {
            canvasGroups[i] = inOut[i].GetComponent<CanvasGroup>();
            if (canvasGroups[i] == null)
                canvasGroups[i] = inOut[i].gameObject.AddComponent<CanvasGroup>();
            initAlpha[i] = canvasGroups[i].alpha;
        }
        return canvasGroups[i];
    }

    void OnEnable()
    {
        if (playAtAwake) { StartAnim(); return; }
        if (primeOnEnable) Prime();
    }

    /// Snap all elements to the forward curve's start state (alpha 0 / From pos / from scale).
    /// Call this any time you want them "parked" in their pre-animation state.
    public void Prime()
    {
        int n = inOut != null ? inOut.Length : 0;
        for (int i = 0; i < n; i++)
            if (inOut[i] != null) Apply(i, animCurve.Evaluate(0f));
    }

    public void StartAnim() => StartRoutine(false);
    public void StartAnimBackward() => StartRoutine(true);
    public void Play() => StartAnim();
    public void PlayBackward() => StartAnimBackward();

    public void StopAnim()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
    }

    public void StopAndReset()
    {
        StopAnim();
        ResetTween();
    }

    void StartRoutine(bool backward)
    {
        StopAnim();
        routine = StartCoroutine(SequenceRoutine(backward));
    }

    IEnumerator SequenceRoutine(bool backward)
    {
        int n = inOut != null ? inOut.Length : 0;
        if (n == 0) yield break;

        AnimationCurve curve = backward ? animCurveBackward : animCurve;
        float duration = LastKeyTime(curve);

        if (duration <= 0f || speed <= 0f)
        {
            float endValue = curve.keys.Length > 0 ? curve.keys[curve.keys.Length - 1].value : 0f;
            for (int i = 0; i < n; i++) Apply(i, endValue);
            FireEvent(backward);
            yield break;
        }

        // NEW: snap EVERY element to the start state before anything animates,
        // so append-mode elements don't sit at their final state waiting for their turn.
        // Forward -> hidden/from state; Backward -> full state (correct for an out-animation).
        if (primeAtStart)
            for (int i = 0; i < n; i++) Apply(i, curve.Evaluate(0f));

        do
        {
            if (appendTweens)
            {
                for (int i = 0; i < n; i++)
                {
                    for (float t = 0f; t < duration; t += speed * Time.deltaTime)
                    {
                        Apply(i, curve.Evaluate(t));
                        yield return null;
                    }
                    Apply(i, curve.Evaluate(duration));
                }
            }
            else
            {
                for (float t = 0f; t < duration; t += speed * Time.deltaTime)
                {
                    for (int i = 0; i < n; i++) Apply(i, curve.Evaluate(t));
                    yield return null;
                }
                for (int i = 0; i < n; i++) Apply(i, curve.Evaluate(duration));
            }

            if (delayeventby > 0f) yield return new WaitForSeconds(delayeventby);
            FireEvent(backward);
        }
        while (isLoop);
    }

    void FireEvent(bool backward)
    {
        if (backward) animBackwardEvent?.Invoke();
        else animEvent?.Invoke();
    }

    void Apply(int i, float value)
    {
        RectTransform tr = inOut[i];
        if (tr == null) return;

        if ((tweenType & TweenType.Move) != 0 && FromPos != null && i < FromPos.Length && initialPos != null && i < initialPos.Length)
        {
            tr.anchoredPosition = Vector2.LerpUnclamped(FromPos[i], initialPos[i], value);
        }

        if ((tweenType & TweenType.Scale) != 0)
        {
            if (isUniform) tr.localScale = initScale[i] * value;
            else tr.localScale = new Vector3(initScale[i].x, initScale[i].y * value, initScale[i].z);
        }

        if ((tweenType & TweenType.Alpha) != 0)
            GetGroup(i).alpha = value;

        if ((tweenType & TweenType.Rotation) != 0)
            tr.localRotation = initRot[i] * Quaternion.Euler(0f, 0f, value * 360f);

        UpdateActiveState(i, value);
    }

    // NEW: true GameObject on/off around the fade
    void UpdateActiveState(int i, float value)
    {
        if (!deactivateWhenHidden) return;
        // only meaningful when something actually makes the element invisible
        if ((tweenType & (TweenType.Alpha | TweenType.Scale)) == 0) return;
        // never deactivate the object this coroutine lives on
        if (inOut[i].gameObject == gameObject) return;

        bool hidden = value <= hiddenAlphaThreshold;
        if (inOut[i].gameObject.activeSelf == hidden)
            inOut[i].gameObject.SetActive(!hidden);
    }

    static float LastKeyTime(AnimationCurve curve)
    {
        Keyframe[] keys = curve.keys;
        return keys.Length > 0 ? keys[keys.Length - 1].time : 0f;
    }

    public void ResetTween()
    {
        int n = inOut != null ? inOut.Length : 0;
        for (int i = 0; i < n; i++)
        {
            if (inOut[i] == null) continue;
            if (initialPos != null && i < initialPos.Length) inOut[i].anchoredPosition = initialPos[i];
            inOut[i].localScale = initScale[i];
            inOut[i].localRotation = initRot[i];
            if (canvasGroups[i] != null) canvasGroups[i].alpha = initAlpha[i];
            inOut[i].gameObject.SetActive(initActive[i]); // restore authored active state too
        }
    }
}
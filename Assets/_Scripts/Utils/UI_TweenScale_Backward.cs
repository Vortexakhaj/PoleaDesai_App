using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UI_TweenScale_Backward : MonoBehaviour
{
    [Flags]
    public enum TweenType
    {
        None = 0,
        Scale = 1 << 0,
        Alpha = 1 << 1,
        Rotation = 1 << 2
    }

    public TweenType tweenType = TweenType.Scale;

    [Header("Curves")]
    public AnimationCurve animCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public AnimationCurve animCurveBackward = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Space(10)]
    public float speed = 1f;
    public bool isLoop = false;
    public bool playAtAwake = false;
    [Tooltip("If Selected Scale Uniform else Only on Y axis")]
    public bool isUniform = true;

    [Header("Events")]
    public float delayeventby;
    public UnityEvent tweenEvent;
    public UnityEvent tweenBackwardEvent;

    Transform _transform;
    Image _image;
    Vector3 _initScale;
    Quaternion _initRot;
    float _initAlpha;
    Coroutine _routine;

    void Awake()
    {
        _transform = transform;
        _initScale = _transform.localScale;
        _initRot = _transform.localRotation;
        _image = GetComponent<Image>();
        if (_image != null) _initAlpha = _image.color.a;
    }

    void OnEnable()
    {
        if (playAtAwake) Play();
    }

    public void Play() => StartRoutine(false);
    public void PlayBackward() => StartRoutine(true);

    public void Stop()
    {
        StopRoutine();
        ResetTween();
    }

    void StartRoutine(bool backward)
    {
        StopRoutine();
        _routine = StartCoroutine(TweenRoutine(backward));
    }

    void StopRoutine()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    IEnumerator TweenRoutine(bool backward)
    {
        AnimationCurve main = backward ? animCurveBackward : animCurve;
        float duration = LastKeyTime(main);

        if (duration <= 0f) // empty/degenerate curve — apply end state and exit instead of crashing
        {
            Keyframe[] keys = main.keys;
            Apply(keys.Length > 0 ? keys[keys.Length - 1].value : 0f, 0f);
            yield break;
        }

        for (float t = 0f; ; t += speed * Time.deltaTime)
        {
            if (t > duration)
                t = isLoop ? t % duration : duration;

            Apply(main.Evaluate(t), t);

            if (t >= duration && !isLoop) break;
            yield return null;
        }

        yield return new WaitForSeconds(delayeventby);

        if (backward)
            tweenBackwardEvent?.Invoke();
        else
            tweenEvent?.Invoke();
    }

    void Apply(float value, float t)
    {
        if ((tweenType & TweenType.Scale) != 0)
        {
            if (isUniform)
                _transform.localScale = _initScale * value;
            else
                _transform.localScale = new Vector3(_initScale.x, _initScale.y * value, _initScale.z);
        }

        if ((tweenType & TweenType.Alpha) != 0 && _image != null)
        {
            Color c = _image.color;
            c.a = value;
            _image.color = c;
        }

        if ((tweenType & TweenType.Rotation) != 0)
            _transform.localRotation = _initRot * Quaternion.Euler(0f, 0f, value * 360f);
    }

    static float LastKeyTime(AnimationCurve curve)
    {
        Keyframe[] keys = curve.keys;
        return keys.Length > 0 ? keys[keys.Length - 1].time : 0f;
    }

    public void ResetTween()
    {
        StopRoutine();
        _transform.localScale = _initScale;
        _transform.localRotation = _initRot;
        if (_image != null)
        {
            Color c = _image.color;
            c.a = _initAlpha;
            _image.color = c;
        }
    }
}
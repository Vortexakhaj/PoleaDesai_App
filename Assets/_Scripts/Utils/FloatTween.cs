using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CanvasGroup))]
public class FloatTween : MonoBehaviour
{
    [SerializeField] bool RunOnAwake = true;
    [SerializeField] bool RunOnEnable = false;

    [SerializeField] float StartDelay = 0.1f;
    [SerializeField] float fromValue = 0f;
    [SerializeField] float toValue = 1f;
    [SerializeField] float Speed = 1f;
    Coroutine coroutine;
    [SerializeField] bool isRunning;
    [SerializeField] bool isRunningReverse;

    private CanvasGroup canvasGroup;

    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    public UnityEvent OnStart;
    public FloatEvent OnChange;

    public UnityEvent OnComplete;
    public UnityEvent OnCompleteReverse;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        OnChange.AddListener((value) => { canvasGroup.alpha = value; });

        if (RunOnAwake)
            Tween();
    }

    void OnEnable()
    {
        if (RunOnEnable)
            Tween();
    }

    void OnDisable()
    {

    }

    public void Tween()
    {
        if (!gameObject.activeInHierarchy)
            return;      
        coroutine = StartCoroutine(Tween(fromValue, toValue));

    }

    public void TweenReverse()
    {
        if (!gameObject.activeInHierarchy)
            return;        
        coroutine = StartCoroutine(Tween(toValue, fromValue, true));
    }

    IEnumerator Tween(float from, float to, bool isreverse = false)
    {
        isRunning = isRunningReverse = isreverse;
        if (!isreverse) OnStart?.Invoke();
        OnChange?.Invoke(from);
        yield return new WaitForSeconds(StartDelay);

        var value = from;
        while (!Mathf.Approximately(value, to))
        {
            value = Mathf.MoveTowards(value, to, Time.deltaTime * Speed);
            OnChange?.Invoke(value);
            yield return null;
        }
        if (!isreverse) OnComplete?.Invoke();
        else OnCompleteReverse?.Invoke();
        isRunning = isRunningReverse = false;
    }

}
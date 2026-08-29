using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class PlayInSequence : MonoBehaviour
{
    public bool getIniPos;
    public bool appendTweens;
    public bool isImageBounce;
    public Ease ease;
    [SerializeField] RectTransform[] inOut;
    [SerializeField] Vector2[] FromPos;
    [SerializeField] Vector2[] initialPos;
    [SerializeField] Vector2[] initialScale;
    [SerializeField] float duration = 1f;

    public UnityEvent animEvent;

    private void Awake()
    {
        if (getIniPos == true)
        {
            for (int i = 0; i < inOut.Length; i++)
            {
                int index = i;
                initialPos[index] = inOut[index].anchoredPosition;
            }
        }
        initialScale = new Vector2[inOut.Length];
        for (int i = 0; i < inOut.Length; i++)
        {
            int index = i;
            initialScale[index] = inOut[index].localScale;
        }
    }

    public void StartAnim()
    {
        DOTween.Kill(gameObject);
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < inOut.Length; i++)
        {
            int index = i;
            //inOut[index].DOKill(inOut[index].gameObject);
            if (appendTweens)
            {
                sequence.Append(inOut[index].GetComponent<RectTransform>().DOAnchorPos(initialPos[index], /*duration * (index + 1)*/duration).From(FromPos[index]).SetEase(ease)).SetId(gameObject);
                if (isImageBounce)
                    sequence.Join(inOut[index].DOScale(initialScale[index], duration).From(0).SetEase(ease)).SetId(gameObject);
            }
            else
            {
                sequence.Join(inOut[index].GetComponent<RectTransform>().DOAnchorPos(initialPos[index], duration).From(FromPos[index]).SetEase(ease)).SetId(gameObject);
                if (isImageBounce)
                    sequence.Join(inOut[index].DOScale(initialScale[index], duration).From(0).SetEase(ease)).SetId(gameObject);
            }
        }
        sequence.Play();
        sequence.OnComplete(() =>
        {
            animEvent?.Invoke();
        });
    }

}

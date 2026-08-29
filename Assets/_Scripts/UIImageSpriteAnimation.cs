using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
public class UIImageSpriteAnimation : MonoBehaviour
{
    [SerializeField] private List<Sprite> frames;
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool preserveAspect = true;

    private Image image;
    private float timer;
    private int currentFrame;
    private bool isPlaying;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (preserveAspect) image.preserveAspect = true;

        if (playOnAwake) Play();
        else Pause();
    }

    private void Update()
    {
        if (!isPlaying || frames == null || frames.Count == 0) return;

        timer += Time.deltaTime;
        float frameDuration = 1f / frameRate;

        if (timer >= frameDuration)
        {
            timer -= frameDuration;
            currentFrame = (currentFrame + 1) % frames.Count;

            if (!loop && currentFrame == 0)
            {
                Stop();
                return;
            }

            image.sprite = frames[currentFrame];
        }
    }

    public void Play()
    {
        if (frames == null || frames.Count == 0)
        {
            Debug.LogWarning("No frames assigned to UISpriteAnimation", this);
            return;
        }

        isPlaying = true;
        currentFrame = 0;
        timer = 0f;
        image.sprite = frames[0];
    }

    public void Pause() => isPlaying = false;
    public void Stop()
    {
        isPlaying = false;
        currentFrame = 0;
        image.sprite = frames[0];
    }

    public void SetAnimation(List<Sprite> newFrames, float newFrameRate)
    {
        frames = newFrames;
        frameRate = newFrameRate;
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(Image))]
public class UIImageSpriteAnimator : MonoBehaviour
{
    [System.Serializable]
    public class AnimationClip
    {
        public string name;
        public List<Sprite> frames;
        public float frameRate = 12f;
        public bool loop = true;
    }

    [System.Serializable]
    public class AnimationTransition
    {
        public string fromState;
        public string toState;
        public float transitionDuration = 0.1f;
        public bool hasExitTime = false;
    }

    [Header("Animations")]
    [SerializeField] private List<AnimationClip> animations = new List<AnimationClip>();
    [SerializeField] private string defaultState;

    [Header("Transitions")]
    [SerializeField] private List<AnimationTransition> transitions = new List<AnimationTransition>();

    [Header("Settings")]
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool preserveAspect = true;

    private Image image;
    public AnimationClip currentClip;
    private float clipTimer;
    private int currentFrame;
    private bool isPlaying;

    // Transition variables
    private AnimationClip nextClip;
    private float transitionTimer;
    private bool inTransition;
    private float transitionProgress;
    private AnimationTransition crossFadeTransition;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (preserveAspect) image.preserveAspect = true;

        if (playOnAwake && !string.IsNullOrEmpty(defaultState))
        {
            Play(defaultState);
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        if (inTransition)
        {
            UpdateTransition();
        }
        else
        {
            UpdateAnimation();
            CheckTransitions();
        }

        if (Input.GetKey(KeyCode.F))
        {
            CrossFade("Active", 0.2f);
        }
        if (Input.GetKey(KeyCode.A))
        {
            CrossFade("InActive", 0.2f);
        }

    }

    private void UpdateAnimation()
    {
        if (currentClip == null || currentClip.frames.Count == 0) return;

        clipTimer += Time.deltaTime;
        float frameDuration = 1f / currentClip.frameRate;

        while (clipTimer >= frameDuration)
        {
            clipTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % currentClip.frames.Count;

            if (!currentClip.loop && currentFrame == 0)
            {
                Stop();
                return;
            }

            image.sprite = currentClip.frames[currentFrame];
        }
    }

    private void CheckTransitions()
    {
        if (currentClip == null) return;

        foreach (var transition in transitions)
        {
            if (transition.fromState == currentClip.name)
            {
                if (transition.hasExitTime && currentFrame < currentClip.frames.Count - 1)
                    continue;

                StartTransition(transition);
                break;
            }
        }
    }    

    private void StartTransition(AnimationTransition transition)
    {
        nextClip = animations.Find(a => a.name == transition.toState);
        if (nextClip == null) return;
        Debug.Log("next Clip In Transition");
        inTransition = true;
        transitionTimer = 0f;
    }

    private void UpdateTransition()
    {
        if (nextClip == null)
        {
            inTransition = false;
            return;
        }

        transitionTimer += Time.deltaTime;
        var transition = transitions.Find(t => t.fromState == currentClip.name && t.toState == nextClip.name);
        if (transition == null)
        {
            transition = crossFadeTransition;
        }

        transitionProgress = Mathf.Clamp01(transitionTimer / transition.transitionDuration);

        // During transition, blend between the last frame of current clip and first frame of next clip
        if (transitionProgress >= 1f)
        {
            CompleteTransition();
        }
    }

    private void CompleteTransition()
    {
        currentClip = nextClip;
        currentFrame = 0;
        clipTimer = 0f;
        image.sprite = currentClip.frames[0];
        inTransition = false;
        nextClip = null;
        crossFadeTransition = null;
    }

    public void Play(string stateName)
    {
        var clip = animations.Find(a => a.name == stateName);
        if (clip == null)
        {
            Debug.LogWarning($"Animation state {stateName} not found");
            return;
        }

        currentClip = clip;
        currentFrame = 0;
        clipTimer = 0f;
        image.sprite = currentClip.frames[0];
        isPlaying = true;
        inTransition = false;
    }

    public void CrossFade(string stateName, float transitionDuration)
    {
        var clip = animations.Find(a => a.name == stateName);
        if (clip == null)
        {
            Debug.LogWarning($"Animation state {stateName} not found");
            return;
        }

        if (currentClip == null || currentClip.name == stateName)
        {
            Play(stateName);
            return;
        }

        // Create a temporary transition
        var tempTransition = new AnimationTransition
        {
            fromState = currentClip.name,
            toState = stateName,
            transitionDuration = transitionDuration,
            hasExitTime = false
        };
        crossFadeTransition = tempTransition;
       // Debug.Log("crossfade");
        StartTransition(tempTransition);
    }

    public void Pause() => isPlaying = false;
    public void Resume() => isPlaying = true;
    public void Stop()
    {
        isPlaying = false;
        currentFrame = 0;
        if (currentClip != null && currentClip.frames.Count > 0)
        {
            image.sprite = currentClip.frames[0];
        }
    }

    public void AddAnimation(AnimationClip newClip)
    {
        if (animations.Exists(a => a.name == newClip.name))
        {
            Debug.LogWarning($"Animation {newClip.name} already exists");
            return;
        }
        animations.Add(newClip);
    }

    public void AddTransition(string fromState, string toState, float duration = 0.1f, bool exitTime = false)
    {
        if (!animations.Exists(a => a.name == fromState)) return;
        if (!animations.Exists(a => a.name == toState)) return;

        transitions.Add(new AnimationTransition
        {
            fromState = fromState,
            toState = toState,
            transitionDuration = duration,
            hasExitTime = exitTime
        });
    }
}
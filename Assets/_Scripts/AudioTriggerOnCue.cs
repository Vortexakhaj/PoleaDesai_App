using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioTriggerOnCue : MonoBehaviour
{
    [SerializeField] AudioSource mainSource;

    [Tooltip("Time in seconds for the crossfade between the old and new clip.")]
    [SerializeField] float fadeDuration = 0.5f;

    AudioSource fadeOutSource;
    Coroutine fadeRoutine;
    float targetVolume = 1f;

    void Awake()
    {
        if (mainSource == null)
            mainSource = GetComponent<AudioSource>();

        // Remember the intended volume so we can fade up to it.
        targetVolume = mainSource.volume;

        // Secondary source used only to fade out the previously playing clip.
        fadeOutSource = gameObject.AddComponent<AudioSource>();
        CopySourceSettings(mainSource, fadeOutSource);
        fadeOutSource.playOnAwake = false;
        fadeOutSource.volume = 0f;
    }

    public void PlayAudioClip(AudioClip clip)
    {
        if (clip == null) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(CrossfadeTo(clip));
    }

    IEnumerator CrossfadeTo(AudioClip newClip)
    {
        // 1. Hand the currently playing clip over to the fade-out source.
        if (mainSource.isPlaying && mainSource.clip != null)
        {
            fadeOutSource.clip = mainSource.clip;
            fadeOutSource.time = mainSource.time;
            fadeOutSource.volume = mainSource.volume;
            fadeOutSource.Play();
        }
        else
        {
            fadeOutSource.Stop();
        }

        // 2. Swap in the new clip on the main source, starting silent.
        mainSource.Stop();
        mainSource.clip = newClip;
        mainSource.volume = 0f;
        mainSource.Play();

        // 3. Crossfade.
        float startOutVol = fadeOutSource.volume;
        float duration = Mathf.Max(fadeDuration, 0.0001f);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            mainSource.volume = Mathf.Lerp(0f, targetVolume, k);

            if (fadeOutSource.isPlaying)
                fadeOutSource.volume = Mathf.Lerp(startOutVol, 0f, k);

            yield return null;
        }

        // 4. Cleanup.
        mainSource.volume = targetVolume;
        fadeOutSource.Stop();
        fadeOutSource.volume = 0f;
        fadeRoutine = null;
    }

    void CopySourceSettings(AudioSource from, AudioSource to)
    {
        to.outputAudioMixerGroup = from.outputAudioMixerGroup;
        to.mute = from.mute;
        to.bypassEffects = from.bypassEffects;
        to.bypassListenerEffects = from.bypassListenerEffects;
        to.bypassReverbZones = from.bypassReverbZones;
        to.priority = from.priority;
        to.pitch = from.pitch;
        to.panStereo = from.panStereo;
        to.spatialBlend = from.spatialBlend;
        to.reverbZoneMix = from.reverbZoneMix;
        to.dopplerLevel = from.dopplerLevel;
        to.spread = from.spread;
        to.rolloffMode = from.rolloffMode;
        to.minDistance = from.minDistance;
        to.maxDistance = from.maxDistance;
        to.loop = from.loop;
    }
}
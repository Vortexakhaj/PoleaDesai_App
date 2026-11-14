using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioTriggerOnCue : MonoBehaviour
{
    [HideInInspector] public bool isPaused;
    [SerializeField] AudioSource mainSource;
    public AudioSource MainSource => mainSource;

    public void PlayAudioClipImmediate(AudioClip clip)
    {        
        if (clip == null)
        {
            mainSource.Stop();
            return;
        }
        mainSource.clip = clip;
        mainSource.Play();
    }   


}
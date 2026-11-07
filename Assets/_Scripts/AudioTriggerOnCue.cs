using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioTriggerOnCue : MonoBehaviour
{
    [SerializeField] AudioSource mainSource;      

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
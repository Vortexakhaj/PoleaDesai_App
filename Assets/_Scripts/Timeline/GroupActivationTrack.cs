using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.9f, 0.4f, 0.4f)]
[TrackClipType(typeof(GroupActivationClip))]
[System.Serializable]
public class GroupActivationTrack : TrackAsset
{
    public GameObject[] targetObjects;

    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var mixer = ScriptPlayable<GroupActivationMixerBehaviour>.Create(graph, inputCount);

        // Pass the array of objects from the Track to the Mixer Behaviour
        mixer.GetBehaviour().targetObjects = targetObjects;

        return mixer;
    }
}
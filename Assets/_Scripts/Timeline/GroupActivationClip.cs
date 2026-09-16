using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class GroupActivationClip : PlayableAsset, ITimelineClipAsset
{
    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        // The mixer handles all the logic, so we just create a basic Playable here
        return Playable.Create(graph);
    }
}
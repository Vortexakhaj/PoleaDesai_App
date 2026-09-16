using UnityEngine;
using UnityEngine.Playables;

public class GroupActivationMixerBehaviour : PlayableBehaviour
{
    public GameObject[] targetObjects;
    private bool[] initialStates;

    public override void OnGraphStart(Playable playable)
    {
        if (targetObjects == null || targetObjects.Length == 0) return;

        initialStates = new bool[targetObjects.Length];

        // Store the initial active state of every object so we can restore it when the timeline stops
        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null)
            {
                initialStates[i] = targetObjects[i].activeSelf;
            }
        }
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (targetObjects == null || targetObjects.Length == 0) return;

        int inputCount = playable.GetInputCount();
        bool anyClipActive = false;

        // Check if any clip on this track is currently active (weight > 0)
        for (int i = 0; i < inputCount; i++)
        {
            if (playable.GetInputWeight(i) > 0f)
            {
                anyClipActive = true;
                break;
            }
        }

        // Apply the state to all GameObjects in the array
        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null && targetObjects[i].activeSelf != anyClipActive)
            {
                targetObjects[i].SetActive(anyClipActive);
            }
        }
    }

    public override void OnGraphStop(Playable playable)
    {
        if (targetObjects == null || initialStates == null) return;

        // Restore the initial states of the objects so they don't stay hidden if the timeline ends
        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null && i < initialStates.Length)
            {
                targetObjects[i].SetActive(initialStates[i]);
            }
        }
    }
}
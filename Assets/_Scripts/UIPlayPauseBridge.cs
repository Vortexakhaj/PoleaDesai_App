using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIPlayPauseBridge : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        // Automatically hook the button click to the bridge method
        button.onClick.AddListener(BridgeClick);
    }

    // This public method calls the static method on the Timeline Controller
    public void BridgeClick()
    {
        TimelineSequenceController.GlobalTogglePlayPause();
    }
}
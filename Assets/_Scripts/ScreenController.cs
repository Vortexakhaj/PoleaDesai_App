using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ScreenController : MonoBehaviour
{
    [SerializeField] private bool showEditor;

    [Header("ScreensControl")]
    public int screenIndex;
    public ToggleGroup screenToggler;

    // Start is called before the first frame update
    void Start()
    {
        if (screenIndex > screenToggler.transform.childCount) return;

        screenToggler.transform.GetChild(screenIndex).GetComponent<Toggle>().isOn = true;
    }

    public void EnableScreen(int index, bool status)
    {
        if (screenToggler == null)
        {
            Debug.LogError("screenToggler is not assigned.");
            return;
        }

        if (index < 0 || index >= screenToggler.transform.childCount)
        {
            Debug.LogError($"Index {index} is out of bounds. screenToggler has {screenToggler.transform.childCount} children.");
            return;
        }

        var toggle = screenToggler.transform.GetChild(index).GetComponent<OnCLickTrigger>();
        if (toggle != null)
        {
            if (toggle.TriggerScreen != null) toggle.TriggerScreen.SetActive(status);
            if (toggle.TriggerScreenD2 != null) toggle.TriggerScreenD2.SetActive(status);
        }
    }  

}


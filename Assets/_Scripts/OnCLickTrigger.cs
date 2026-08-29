using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class OnCLickTrigger : MonoBehaviour
{
    public Toggle toggle;

    public bool setSiblingIndex;
    public int siblingIndex;
    public GameObject TriggerScreen;
    public GameObject TriggerScreenD2;

    public bool toggleNextPagetrue = true;
    public UnityEvent toggleTrueEvent;
    public bool toggleNextPagefalse = true;
    public UnityEvent toggleFalseEvent;

    protected virtual void Awake()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener((value) => { OnToggleClicked(value); });
        }
    }
    public virtual void OnToggleClicked(bool value)
    {
        //if (TriggerScreen == null)
        //{
        //    Debug.LogError("TriggerScreen is not assigned!", this);
        //    return;
        //}

        if (value)
        {
            ActivateScreen();
        }
        else
        {
            DeactivateScreen();
        }
    }

    private void ActivateScreen()
    {
        // Set active state first
        if (TriggerScreen) TriggerScreen.SetActive(true);

        // Perform other actions
        if (setSiblingIndex)
        {
            TriggerScreen.transform.SetSiblingIndex(siblingIndex);
        }

        if (TriggerScreenD2) TriggerScreenD2.SetActive(true);


        if (toggleNextPagetrue)
        {
            toggleTrueEvent?.Invoke();
        }
    }

    private void DeactivateScreen()
    {
        // Perform actions that should happen before deactivation
        if (toggleNextPagefalse)
        {
            toggleFalseEvent?.Invoke();
        }

        // Set active state last
        if (TriggerScreen) TriggerScreen.SetActive(false);

        if (TriggerScreenD2) TriggerScreenD2.SetActive(false);

    }

    public virtual void SetToggleNextPageTrue(bool value)
    {
        toggleNextPagetrue = value;
    }

    public virtual void SetToggleNextPageFalse(bool value)
    {
        toggleNextPagefalse = value;
    }
}

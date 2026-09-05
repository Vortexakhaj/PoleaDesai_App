using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;

public class BubbleManager : MonoBehaviour
{
    public MemoryBubble[] bubbles;
    public int resetIntervalDays = 10;

    [Header("Visitor Selection Limits")]
    [Tooltip("How many bubbles can a single visitor select at the same time before pressing DONE?")]
    public int maxConcurrentSelections = 3;

    // Tracks the exact order of bubbles selected by the current visitor
    private List<MemoryBubble> currentSelectionOrder = new List<MemoryBubble>();

    [SerializeField] private Button doneButton;
    [SerializeField] private int minSelectionsToEnableDone = 1;

    [SerializeField] private float doneDelay = 1.5f;
    public UnityEvent doneSelectionEvent;

    private GameObject _blocker;
    private string saveFilePath;

    [Serializable]
    public class BubbleData { public int selectionCount; }

    [Serializable]
    public class SaveData
    {
        public int thresholdClickCount;
        public string lastResetDate;
        public List<BubbleData> bubbleData = new List<BubbleData>();
    }

    void Awake()
    {
        saveFilePath = Path.Combine(Application.streamingAssetsPath, "museumBubbles.json");
        bubbles = GetComponentsInChildren<MemoryBubble>();
        doneButton.onClick.AddListener(OnDoneButtonPressed);
    }

    void Start()
    {
        LoadAndInitializeData();
        UpdateDoneButtonInteractable();
    }

    // ---------------------------------------------------------
    // CALLED BY BUBBLES WHEN THEY ARE CLICKED
    // ---------------------------------------------------------

    // Returns TRUE if the bubble is allowed to be selected
    public bool RequestSelection(MemoryBubble bubbleRequesting)
    {
        // If limit is 0, no selections allowed at all
        if (maxConcurrentSelections <= 0)
        {
            UpdateDoneButtonInteractable();
            return false;
        }

        if (currentSelectionOrder.Count < maxConcurrentSelections)
        {
            // We have room! Add to the end of the list.
            if (!currentSelectionOrder.Contains(bubbleRequesting))
                currentSelectionOrder.Add(bubbleRequesting);
            UpdateDoneButtonInteractable();
            return true;
        }
        else
        {
            // Limit reached! Remove the OLDEST selected bubble (index 0)
            MemoryBubble oldestBubble = currentSelectionOrder[0];
            oldestBubble.ForceDeselect(); // Tell the old bubble to turn off its visual selection

            currentSelectionOrder.RemoveAt(0); // Remove from list

            // Add the newly clicked bubble to the end of the list
            currentSelectionOrder.Add(bubbleRequesting);
            UpdateDoneButtonInteractable();
            return true;
        }
    }

    // Called by bubbles when a user manually deselects a bubble
    public void RegisterDeselection(MemoryBubble bubbleDeselecting)
    {
        if (currentSelectionOrder.Contains(bubbleDeselecting))
        {
            currentSelectionOrder.Remove(bubbleDeselecting);
        }
        UpdateDoneButtonInteractable();
    }


    public void StartLoading()
    {
        Canvas root = GetComponentInParent<Canvas>().rootCanvas;
        _blocker = UIBlocker.CreateBlocker(
            root,
            onBlockerClicked: null,                       // no click handling
            sortingOrderOffset: 5,
            tintColor: new Color(0, 0, 0, 0f));         // dim background       
    }

    public void StopLoading()
    {
        UIBlocker.DestroyBlocker(_blocker);
    }

    // ---------------------------------------------------------
    // CALL THIS FROM YOUR "DONE" UI BUTTON
    // ---------------------------------------------------------
    public void OnDoneButtonPressed()
    {
        Debug.Log("Done button pressed. Confirming selections...");

        // Loop through all bubbles and lock in their counts
        foreach (MemoryBubble bubble in bubbles)
        {
            bubble.ConfirmSelection();
        }

        // Clear the tracking list for the next visitor
        currentSelectionOrder.Clear();

        // Save the updated cumulative counts to the local machine
        SaveCurrentState();

        // Disable the done button since no bubbles are selected anymore
        UpdateDoneButtonInteractable();
        StartLoading();
        StartCoroutine(InvokeDoneButtonEventWithDelay(doneDelay));
    }

    IEnumerator InvokeDoneButtonEventWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopLoading();
        doneSelectionEvent?.Invoke();
    }

    private void UpdateDoneButtonInteractable()
    {
        if (doneButton != null)
        {
            // The button is only interactable when the current selections match the max limit
            doneButton.interactable = (currentSelectionOrder.Count >= minSelectionsToEnableDone);
        }
        else
        {
            Debug.LogWarning("Done Button is not assigned to the BubbleManager!");
        }
    }

    private void LoadAndInitializeData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

            DateTime lastDate = DateTime.Parse(loadedData.lastResetDate);
            TimeSpan difference = DateTime.Now - lastDate;

            if (difference.TotalDays >= resetIntervalDays)
            {
                Debug.Log("10 Days passed. Resetting bubbles.");
                ResetAllBubbles();
            }
            else
            {
                for (int i = 0; i < bubbles.Length; i++)
                {
                    bubbles[i].thresholdClickCount = loadedData.thresholdClickCount;
                    if (i < loadedData.bubbleData.Count)
                    {
                        bubbles[i].InitializeFromData(loadedData.bubbleData[i].selectionCount);
                    }
                }
            }
        }
        else
        {
            SaveCurrentState();
        }
    }

    public async void SaveCurrentState()
    {
        SaveData data = new SaveData();
        data.lastResetDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.thresholdClickCount = Mathf.Max(bubbles[0].thresholdClickCount, 1);
        foreach (MemoryBubble bubble in bubbles)
        {
            BubbleData bData = new BubbleData();
            bData.selectionCount = bubble.SelectionCount;
            data.bubbleData.Add(bData);
        }

        string json = JsonUtility.ToJson(data, true);

        if (!Directory.Exists(Application.streamingAssetsPath))
            Directory.CreateDirectory(Application.streamingAssetsPath);
        await File.WriteAllTextAsync(Path.Combine(Application.streamingAssetsPath, "museumBubbles.json"), json);
    }

    [ContextMenu("ResetRewrite")]
    private void ResetAllBubbles()
    {
        foreach (MemoryBubble bubble in bubbles)
        {
            bubble.ResetBubble();
        }
        SaveCurrentState();
    }

    void OnApplicationQuit() { SaveCurrentState(); }
    void OnApplicationPause(bool pauseStatus) { if (pauseStatus) SaveCurrentState(); }
}
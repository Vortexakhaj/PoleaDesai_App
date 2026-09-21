using System;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(TextMeshProUGUI))]
public class UITextTypewriterObjectEnabler : UITextTypewriter
{
    [System.Serializable]
    public class WordObjectLink
    {
        [Tooltip("The group of words that must appear in the typing text to trigger the object.")]
        public string triggerPhrase;
        [Tooltip("The GameObject to activate.")]
        public GameObject targetObject;
    }

    [Header("Word Object Enabler")]
    [Tooltip("Array of phrases and objects. All objects are disabled when typing starts, and enabled when the phrase is typed.")]
    public WordObjectLink[] objectLinks;

    public void DisableAllObjects()
    {
        OnTypingStart();
    }

    // Called automatically when a new text block starts typing
    protected override void OnTypingStart()
    {
        base.OnTypingStart();

        if (objectLinks == null) return;

        // Disable all objects when typing begins
        foreach (var link in objectLinks)
        {
            if (link.targetObject != null)
            {
                link.targetObject.SetActive(false);
            }
        }
    }

    // Called automatically as each chunk (character/word/line) is typed
    protected override void OnChunkTyped(string visibleText)
    {
        base.OnChunkTyped(visibleText);

        if (objectLinks == null || string.IsNullOrEmpty(visibleText)) return;

        // Check if any trigger phrases are now present in the visible text
        foreach (var link in objectLinks)
        {
            if (link.targetObject != null && !link.targetObject.activeSelf && !string.IsNullOrEmpty(link.triggerPhrase))
            {
                // Case-insensitive check to see if the phrase has been typed out
                if (visibleText.IndexOf(link.triggerPhrase, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    link.targetObject.SetActive(true);
                }
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UITextTypewriterObjectEnabler))]
public class UITextTypewriterObjectEnablerEditor : UITextTypewriterEditor
{
    public override void OnInspectorGUI()
    {
        // Draw the entire base class Inspector (Typing settings, Audio, etc.)
        base.OnInspectorGUI();

        // Draw a space and the header for the derived class
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Word Object Enabler", EditorStyles.boldLabel);

        // Draw the objectLinks array specifically for this derived class
        SerializedProperty objectLinksProp = serializedObject.FindProperty("objectLinks");
        EditorGUILayout.PropertyField(objectLinksProp, true);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
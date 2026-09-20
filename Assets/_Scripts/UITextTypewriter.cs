using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum TypingMode { Characters, Words, Lines, Paragraphs }

[RequireComponent(typeof(TextMeshProUGUI))]
public class UITextTypewriter : MonoBehaviour
{
    public UnityEvent onTypingComplete;

    [Header("Typing Settings")]
    public TextMeshProUGUI text;
    public bool playOnEnable = true;
    public bool autoRepeat = false;
    public float autoRepeatDelay = 2f;
    public float delayToStart = 0f;
    public float delayToEnd = 0f;

    [Tooltip("Characters = type letter by letter.\nWords = type word by word.\nLines = type line by line (respects visual wrapping).\nParagraphs = type paragraph by paragraph.")]
    public TypingMode typingMode = TypingMode.Characters;

    [Tooltip("Delay between characters.")]
    public float delayBetweenChars = 0.125f;

    [Tooltip("Delay between words.")]
    public float delayBetweenWords = 0.15f;

    [Tooltip("Delay between lines.")]
    public float delayBetweenLines = 0.3f;

    [Tooltip("Delay between paragraphs.")]
    public float delayBetweenParagraphs = 0.5f;

    public float delayAfterPunctuation = 0.5f;
    public string trailingChar;

    [Tooltip("Array of text blocks to type out. If empty, defaults to the TextMeshProUGUI's current text.")]
    [TextArea] public string[] stories;

    [Tooltip("Delay between each text block in the array.")]
    public float delayBetweenStories = 1.5f;

    [Tooltip("If true, rich text tags are applied instantly. (Note: This is now handled natively by TMP's color tag system).")]
    public bool skipRichTextTags = true;

    // --- PAUSE SYSTEM ---
    [HideInInspector] public bool isPaused = false;

    private bool lastCharPunctuation = false;
    private char charComma;
    private char charPeriod;
    private char charEmpty;

    private Coroutine typingCoroutine;
    private bool useArray = true;
    private string story; // Internal variable for the currently playing text

    [Header("Audio Settings")]
    [Tooltip("When true requires AudioSource on this object.")]
    public bool useAudio = true;
    [Range(0f, 2f)]
    public float volume = .3f;
    [Tooltip("GameObject with AudioSource component.")]
    public GameObject AudioTypping;
    private AudioSource TyppingFX;

    // Custom wait method that respects the pause flag (because standard WaitForSeconds cannot be paused)
    private IEnumerator WaitWithPause(float delay)
    {
        float timer = 0f;
        while (timer < delay)
        {
            if (!isPaused)
                timer += Time.deltaTime;
            yield return null;
        }
    }

    private float GetCurrentDelay()
    {
        switch (typingMode)
        {
            case TypingMode.Words: return delayBetweenWords;
            case TypingMode.Lines: return delayBetweenLines;
            case TypingMode.Paragraphs: return delayBetweenParagraphs;
            default: return delayBetweenChars;
        }
    }

    void Awake()
    {
        if (useAudio)
        {
            TyppingFX = GetComponent<AudioSource>();
            if (AudioTypping != null)
                TyppingFX.clip = AudioTypping.GetComponent<AudioSource>().clip;
        }

        text = GetComponent<TextMeshProUGUI>();

        if (stories == null || stories.Length == 0)
        {
            useArray = false;
            stories = new string[1];
            stories[0] = text.text;
        }

        charComma = Convert.ToChar(44);
        charPeriod = Convert.ToChar(46);
        charEmpty = Convert.ToChar(" ");
    }

    private void OnEnable()
    {
        isPaused = false; // Reset pause state when object is re-enabled
        if (playOnEnable)
        {
            StartTypewriter();
        }
    }

    private void OnDisable()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    public void StartTypewriter()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypewriterSequence(delayToStart, delayToEnd));
    }

    public void ChangeText(string textContent, float delay = 0, float delayToEnd = 0)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        story = textContent;
        useArray = false;

        typingCoroutine = StartCoroutine(TypewriterSequence(delay, delayToEnd));
    }

    public void ChangeText(string[] textArray, float delay = 0, float delayToEnd = 0)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        stories = textArray;
        useArray = true;

        typingCoroutine = StartCoroutine(TypewriterSequence(delay, delayToEnd));
    }

    IEnumerator TypewriterSequence(float delay, float delayEnd)
    {
        if (delay > 0f)
            yield return StartCoroutine(WaitWithPause(delay));

        do
        {
            if (useArray)
            {
                for (int i = 0; i < stories.Length; i++)
                {
                    story = stories[i];
                    yield return StartCoroutine(PlayText());

                    if (i < stories.Length - 1)
                    {
                        yield return StartCoroutine(WaitWithPause(delayBetweenStories));
                    }
                }
            }
            else
            {
                story = stories[0];
                yield return StartCoroutine(PlayText());
            }

            if (autoRepeat)
                yield return StartCoroutine(WaitWithPause(autoRepeatDelay));

        } while (autoRepeat);

        typingCoroutine = null;

        if (delayEnd > 0f)
            yield return StartCoroutine(WaitWithPause(delayEnd));

        onTypingComplete?.Invoke();
    }

    IEnumerator PlayText()
    {
        // Set the full text first so TextMeshPro calculates the correct visual layout and line breaks
        text.text = story;
        text.ForceMeshUpdate();

        int totalVisibleChars = text.textInfo.characterCount;

        // Cache character info so we don't rely on textInfo after we start modifying text.text
        TMP_CharacterInfo[] charInfos = new TMP_CharacterInfo[totalVisibleChars];
        if (totalVisibleChars > 0)
        {
            Array.Copy(text.textInfo.characterInfo, charInfos, totalVisibleChars);
        }

        // Cache visual line end indices based on TMP's word wrapping
        List<int> lineEndVisIndices = new List<int>();
        for (int i = 0; i < text.textInfo.lineCount; i++)
        {
            int lastVisIdx = text.textInfo.lineInfo[i].lastVisibleCharacterIndex + 1;
            if (lastVisIdx <= 0) lastVisIdx = 1;
            lineEndVisIndices.Add(lastVisIdx);
        }

        int currentVisIndex = 0;
        int currentLine = 0;

        while (currentVisIndex < totalVisibleChars)
        {
            // Cleanly freeze the typing loop entirely if paused using WaitWhile
            yield return new WaitWhile(() => isPaused);

            // Wait for punctuation delay from the PREVIOUS chunk
            if (lastCharPunctuation)
            {
                if (useAudio && TyppingFX != null) TyppingFX.Pause();
                yield return StartCoroutine(WaitWithPause(delayAfterPunctuation));
                lastCharPunctuation = false;
            }

            // Determine target visible index
            int targetVisIndex = currentVisIndex;

            switch (typingMode)
            {
                case TypingMode.Characters:
                    targetVisIndex = currentVisIndex + 1;
                    break;
                case TypingMode.Words:
                    targetVisIndex = GetEndOfWordIndex(currentVisIndex, charInfos);
                    break;
                case TypingMode.Lines:
                    if (currentLine < lineEndVisIndices.Count)
                    {
                        targetVisIndex = lineEndVisIndices[currentLine];
                        currentLine++;
                    }
                    else
                    {
                        targetVisIndex = totalVisibleChars;
                    }
                    // Prevent infinite loop if line is empty or we are already past it
                    if (targetVisIndex <= currentVisIndex) targetVisIndex = currentVisIndex + 1;
                    break;
                case TypingMode.Paragraphs:
                    targetVisIndex = GetEndOfParagraphIndex(currentVisIndex, charInfos);
                    break;
            }

            // Determine target string index from the visible character index
            int targetStringIndex = story.Length;
            if (targetVisIndex > 0 && targetVisIndex <= totalVisibleChars)
            {
                targetStringIndex = charInfos[targetVisIndex - 1].index + 1;
            }
            else if (targetVisIndex == 0)
            {
                targetStringIndex = 0;
            }

            // Update Text Component
            string visiblePart = story.Substring(0, targetStringIndex);
            string hiddenPart = story.Substring(targetStringIndex);

            // Hide the un-typed part by making it transparent. This preserves layout and rich text tags flawlessly!
            if (string.IsNullOrEmpty(trailingChar))
            {
                text.text = visiblePart + "<color=#00000000>" + hiddenPart + "</color>";
            }
            else
            {
                text.text = visiblePart + trailingChar + "<color=#00000000>" + hiddenPart + "</color>";
            }

            // Play audio for the chunk
            if (useAudio && TyppingFX != null && TyppingFX.clip != null)
                TyppingFX.PlayOneShot(TyppingFX.clip, volume);

            // Check punctuation to trigger delay for the NEXT chunk
            char lastVisibleChar = charInfos[targetVisIndex - 1].character;
            bool isPunctuation = false;

            if (typingMode == TypingMode.Characters)
            {
                if (lastVisibleChar == charEmpty || lastVisibleChar == charComma || lastVisibleChar == charPeriod)
                    isPunctuation = true;
            }
            else
            {
                if (lastVisibleChar == charComma || lastVisibleChar == charPeriod)
                    isPunctuation = true;
            }

            if (isPunctuation)
            {
                lastCharPunctuation = true;
            }

            currentVisIndex = targetVisIndex;

            yield return StartCoroutine(WaitWithPause(GetCurrentDelay()));
        }

        // Strip the trailing cursor and reveal the final clean string
        text.text = story;
        text.ForceMeshUpdate();
    }

    private int GetEndOfWordIndex(int startIndex, TMP_CharacterInfo[] charInfos)
    {
        int index = startIndex;
        int charCount = charInfos.Length;

        // Skip current word
        while (index < charCount && !char.IsWhiteSpace(charInfos[index].character))
        {
            index++;
        }
        // Skip whitespace to next word
        while (index < charCount && char.IsWhiteSpace(charInfos[index].character))
        {
            index++;
        }
        return index;
    }

    private int GetEndOfParagraphIndex(int startIndex, TMP_CharacterInfo[] charInfos)
    {
        int index = startIndex;
        int charCount = charInfos.Length;

        while (index < charCount)
        {
            char c = charInfos[index].character;
            if (c == '\n' || c == '\r')
            {
                index++;
                // Consume consecutive newlines
                while (index < charCount && (charInfos[index].character == '\n' || charInfos[index].character == '\r'))
                {
                    index++;
                }
                break;
            }
            index++;
        }
        return index;
    }
}

// =========================================================================================
// CUSTOM EDITOR SCRIPT
// =========================================================================================
#if UNITY_EDITOR
[CustomEditor(typeof(UITextTypewriter))]
public class UITextTypewriterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (!serializedObject.FindProperty("autoRepeat").boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onTypingComplete"));
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("text"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRepeat"));

        if (serializedObject.FindProperty("autoRepeat").boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRepeatDelay"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayToStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayToEnd"));

        EditorGUILayout.Space();

        SerializedProperty modeProp = serializedObject.FindProperty("typingMode");
        EditorGUILayout.PropertyField(modeProp);

        TypingMode currentMode = (TypingMode)modeProp.enumValueIndex;

        switch (currentMode)
        {
            case TypingMode.Characters:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenChars"));
                break;
            case TypingMode.Words:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenWords"));
                break;
            case TypingMode.Lines:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenLines"));
                break;
            case TypingMode.Paragraphs:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenParagraphs"));
                break;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayAfterPunctuation"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("trailingChar"));

        EditorGUILayout.Space();

        SerializedProperty storiesProp = serializedObject.FindProperty("stories");
        EditorGUILayout.PropertyField(storiesProp, true);

        if (storiesProp.arraySize > 1)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenStories"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("skipRichTextTags"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useAudio"));

        if (serializedObject.FindProperty("useAudio").boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioTypping"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Text;
using System;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum TypingMode { Characters, Words, Paragraphs }

[RequireComponent(typeof(TextMeshProUGUI))]
public class UITextTypewriter : MonoBehaviour
{
    [Header("Typing Settings")]
    public TextMeshProUGUI text;
    public bool playOnEnable = true;
    public bool autoRepeat = false;
    public float delayToStart = 0f;
    [Tooltip("If true, rich text tags (e.g. <b>, <color=red>, </color>) are added instantly instead of typed out.")]
    public bool skipRichTextTags = true;
    [Tooltip("Characters = type letter by letter.\nWords = type word by word.\nParagraphs = type paragraph by paragraph.")]
    public TypingMode typingMode = TypingMode.Characters;

    [Tooltip("Delay between characters.")]
    public float delayBetweenChars = 0.125f;

    [Tooltip("Delay between words.")]
    public float delayBetweenWords = 0.15f;

    [Tooltip("Delay between paragraphs.")]
    public float delayBetweenParagraphs = 0.5f;

    public float delayAfterPunctuation = 0.5f;
    public string trailingChar;
    [TextArea] public string story;    

    private bool lastCharPunctuation = false;
    private char charComma;
    private char charPeriod;
    private char charEmpty;

    private Coroutine typingCoroutine;

    [Header("Audio Settings")]
    [Tooltip("When true requires AudioSource on this object.")]
    public bool useAudio = true;
    [Range(0f, 2f)]
    public float volume = .3f;
    [Tooltip("GameObject with AudioSource component.")]
    public GameObject AudioTypping;
    private AudioSource TyppingFX;

    // Helper property to dynamically get the correct delay based on the current Inspector mode
    private float GetCurrentDelay()
    {
        switch (typingMode)
        {
            case TypingMode.Words: return delayBetweenWords;
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

        charComma = Convert.ToChar(44);
        charPeriod = Convert.ToChar(46);
        charEmpty = Convert.ToChar(" ");
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            ChangeText(text.text, delayToStart);
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

    public void ChangeText(string textContent, float delay = 0)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        story = textContent;
        text.text = "";

        typingCoroutine = StartCoroutine(TypewriterSequence(delay));
    }

    public void StartTypewriter()
    {
        ChangeText(story, delayToStart);
    }

    IEnumerator TypewriterSequence(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        do
        {
            text.text = "";
            yield return StartCoroutine(PlayText());

            if (autoRepeat)
                yield return new WaitForSeconds(2f);

        } while (autoRepeat);
    }

    IEnumerator PlayText()
    {
        int i = 0;
        while (i < story.Length)
        {
            // Extract the next chunk of text based on the typing mode
            string chunk = GetNextChunk(story, i, out int nextIndex);
            i = nextIndex;

            if (string.IsNullOrEmpty(chunk))
                continue;

            // Check if the chunk is purely a rich text tag
            bool isPureTag = skipRichTextTags && chunk.StartsWith("<") && chunk.EndsWith(">");

            if (isPureTag)
            {
                // Append the tag instantly without audio, delay, or trailing char flicker
                if (trailingChar.Length > 0 && text.text.Length >= trailingChar.Length)
                    text.text = text.text[..^trailingChar.Length];

                text.text += chunk;
                text.text += trailingChar;
                continue;
            }

            // --- Handle visible text chunks ---

            if (lastCharPunctuation)
            {
                if (useAudio && TyppingFX != null) TyppingFX.Pause();
                yield return new WaitForSeconds(delayAfterPunctuation);
                lastCharPunctuation = false;
            }

            // Check punctuation to trigger delay for the NEXT chunk
            char lastVisibleChar = GetLastVisibleChar(chunk);
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
                if (useAudio && TyppingFX != null) TyppingFX.Pause();
                lastCharPunctuation = true;
            }

            // Play audio once per chunk
            if (useAudio && TyppingFX != null && TyppingFX.clip != null)
                TyppingFX.PlayOneShot(TyppingFX.clip, volume);

            // Update text component
            if (trailingChar.Length > 0 && text.text.Length >= trailingChar.Length)
                text.text = text.text[..^trailingChar.Length];

            text.text += chunk;
            text.text += trailingChar;

            // Dynamically fetch the correct delay based on the current mode
            yield return new WaitForSeconds(GetCurrentDelay());
        }

        // Strip the trailing cursor once finished
        if (trailingChar.Length > 0 && text.text.Length >= trailingChar.Length)
            text.text = text.text[..^trailingChar.Length];

        typingCoroutine = null;
    }

    private string GetNextChunk(string source, int index, out int nextIndex)
    {
        if (typingMode == TypingMode.Characters)
        {
            if (skipRichTextTags && source[index] == '<')
            {
                int close = source.IndexOf('>', index);
                if (close != -1)
                {
                    nextIndex = close + 1;
                    return source.Substring(index, close - index + 1);
                }
            }
            nextIndex = index + 1;
            return source.Substring(index, 1);
        }

        int start = index;
        while (index < source.Length)
        {
            if (skipRichTextTags && source[index] == '<')
            {
                int close = source.IndexOf('>', index);
                if (close != -1)
                {
                    index = close + 1;
                    continue;
                }
            }

            if (typingMode == TypingMode.Words && char.IsWhiteSpace(source[index]))
            {
                index++;
                break;
            }

            if (typingMode == TypingMode.Paragraphs && (source[index] == '\n' || source[index] == '\r'))
            {
                index++;
                break;
            }

            index++;
        }

        nextIndex = index;
        return source.Substring(start, index - start);
    }

    private char GetLastVisibleChar(string chunk)
    {
        for (int j = chunk.Length - 1; j >= 0; j--)
        {
            if (skipRichTextTags && chunk[j] == '>')
            {
                int startTag = chunk.LastIndexOf('<', j);
                if (startTag != -1)
                {
                    j = startTag;
                    continue;
                }
            }
            return chunk[j];
        }
        return charEmpty;
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

        EditorGUILayout.PropertyField(serializedObject.FindProperty("text"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRepeat"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayToStart"));

        EditorGUILayout.Space();

        SerializedProperty modeProp = serializedObject.FindProperty("typingMode");
        EditorGUILayout.PropertyField(modeProp);

        TypingMode currentMode = (TypingMode)modeProp.enumValueIndex;

        // Conditionally draw the delay variables based on the enum selection
        switch (currentMode)
        {
            case TypingMode.Characters:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenChars"));
                break;
            case TypingMode.Words:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenWords"));
                break;
            case TypingMode.Paragraphs:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenParagraphs"));
                break;
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("delayAfterPunctuation"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("trailingChar"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("story"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("skipRichTextTags"));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useAudio"));

        if (serializedObject.FindProperty("useAudio").boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("volume"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AudioTypping"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
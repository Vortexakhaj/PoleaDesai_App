using UnityEditor;

[CustomEditor(typeof(OnCLickTrigger))]
[CanEditMultipleObjects]
public class OnCLickTriggerEditor : Editor // Inherit from ToggleEditor
{
    // Find the properties in your OnCLickTrigger script
    SerializedProperty toggleProp;
    SerializedProperty setSiblingIndexProp;
    SerializedProperty siblingIndexProp;
    SerializedProperty TriggerScreenProp;
    SerializedProperty TriggerScreenD2Prop;
    SerializedProperty toggleNextPagetrueProp;
    SerializedProperty toggleTrueEventProp;
    SerializedProperty toggleNextPagefalseProp;
    SerializedProperty toggleFalseEventProp;

    // This function is called when the editor is enabled
    private void OnEnable()
    {
        // Link the SerializedProperties to the actual fields in your script
        toggleProp = serializedObject.FindProperty("toggle");
        setSiblingIndexProp = serializedObject.FindProperty("setSiblingIndex");
        siblingIndexProp = serializedObject.FindProperty("siblingIndex");
        TriggerScreenProp = serializedObject.FindProperty("TriggerScreen");
        TriggerScreenD2Prop = serializedObject.FindProperty("TriggerScreenD2");
        toggleNextPagetrueProp = serializedObject.FindProperty("toggleNextPagetrue");
        toggleTrueEventProp = serializedObject.FindProperty("toggleTrueEvent");
        toggleNextPagefalseProp = serializedObject.FindProperty("toggleNextPagefalse");
        toggleFalseEventProp = serializedObject.FindProperty("toggleFalseEvent");
    }

    // This is the main function that draws the Inspector
    public override void OnInspectorGUI()
    {
        // IMPORTANT: Always call this first
        // It loads the actual values from your script into the SerializedProperties
        serializedObject.Update();

        // --- Draw your custom properties ---
        // We'll use EditorGUILayout.PropertyField to draw them for us
        EditorGUILayout.PropertyField(toggleProp);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(setSiblingIndexProp);
        if (setSiblingIndexProp.boolValue)
        {
            EditorGUI.indentLevel++; // Indent for clarity
            EditorGUILayout.PropertyField(siblingIndexProp);
            EditorGUI.indentLevel--; // Un-indent
        }
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(TriggerScreenProp);
        EditorGUILayout.PropertyField(TriggerScreenD2Prop);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(toggleNextPagetrueProp);
        if (toggleNextPagetrueProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(toggleTrueEventProp); // This will now update correctly
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(toggleNextPagefalseProp);
        if (toggleNextPagefalseProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(toggleFalseEventProp); // This will also update correctly
            EditorGUI.indentLevel--;
        }

        // IMPORTANT: Always call this last
        // It applies any changes made in the Inspector back to your script
        // AND it automatically handles the SetDirty() call for you!
        serializedObject.ApplyModifiedProperties();
    }
}
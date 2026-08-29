using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(ScreenController))]
public class ScreenControllerEditor : Editor
{
    private SerializedProperty _screenIndex;
    private SerializedProperty _screenToggler;
    private SerializedProperty _showEditor;

    ScreenController scController;    

    private void OnEnable()
    {
        _screenIndex = serializedObject.FindProperty("screenIndex");
        _screenToggler = serializedObject.FindProperty("screenToggler");
        _showEditor = serializedObject.FindProperty("showEditor");
        scController = (ScreenController)this.target;
        //TUIOController.InitializeInEditMode();
        //tuio = TUIOController.instance;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //if (Application.isPlaying)
        //{
        //    base.OnInspectorGUI();
        //    return;
        //}

        if (scController.screenToggler == null)
        {
            // Show a helpful message to the user
            EditorGUILayout.HelpBox("The 'Screen Toggler' reference is not set. Please assign it from the Scene Hierarchy.", MessageType.Info);

            // Draw the property field so the user can drag and drop the object.
            // This allows them to assign the reference directly in the inspector.
            if (_screenToggler != null)
            {
                if (!Application.isPlaying)
                    EditorGUILayout.PropertyField(_screenToggler, new GUIContent("Screen Toggler"));
            }
            else
            {
                // This is an internal error if the property name is wrong in OnEnable()
                EditorGUILayout.HelpBox("Internal Error: Could not find the 'screenToggler' property. Check the property name in OnEnable().", MessageType.Error);
            }

            // Apply any changes (like the user dropping an object) and stop drawing the rest of the GUI.
            // We don't want to proceed until this reference is set.
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUI.BeginChangeCheck();

        if (_screenIndex != null)
        {
            int maxIndex = Mathf.Max(0, scController.screenToggler.transform.childCount - 1);
            _screenIndex.intValue = EditorGUILayout.IntSlider("ScreenIndex", _screenIndex.intValue, 0, maxIndex);
        }
        if (_screenToggler != null)
        {
            EditorGUILayout.PropertyField(_screenToggler, new GUIContent("Screen Toggler"));
        }
        // Display the current value of the singleton variable
        //EditorGUILayout.LabelField("TuioController", EditorStyles.boldLabel);
        //EditorGUILayout.LabelField("Should Marker Detect", tuio.shouldMarkerDetect.ToString());

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            if (scController != null && scController.screenToggler != null)
            {
                for (int i = 0; i < scController.screenToggler.transform.childCount; i++)
                {
                    int index = i;
                    scController.EnableScreen(index, false);
                    Toggle toggle = scController.screenToggler.transform.GetChild(index).GetComponent<Toggle>();
                    if (toggle != null) toggle.isOn = false;
                }
                //tuio.shouldMarkerDetect = _screenIndex.intValue == 3;
                //EditorUtility.SetDirty(tuio);                   

                if (_screenIndex != null)
                {
                    Toggle selectedToggle = scController.screenToggler.transform.GetChild(_screenIndex.intValue).GetComponent<Toggle>();
                    if (selectedToggle != null)
                    {
                        selectedToggle.isOn = true;
                        OnCLickTrigger trigger = selectedToggle.GetComponent<OnCLickTrigger>();
                        if (trigger != null)
                        {
                            trigger.OnToggleClicked(true);
                            trigger.toggleTrueEvent?.Invoke();
                        }
                    }
                }
            }
            Repaint();
        }
        serializedObject.ApplyModifiedProperties();
    }


    private void ChangeScreen()
    {
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            for (int i = 0; i < scController.screenToggler.transform.childCount; i++)
            {
                int index = i;
                scController.EnableScreen(index, false);
                scController.screenToggler.transform.GetChild(scController.screenIndex).GetComponent<Toggle>().isOn = false;
            }
            scController.screenToggler.transform.GetChild(scController.screenIndex).GetComponent<Toggle>().isOn = true;
            scController.EnableScreen(scController.screenIndex, true);
        }
    }

}

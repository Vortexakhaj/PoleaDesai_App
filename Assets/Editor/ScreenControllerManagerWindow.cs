using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ScreenControllerManagerWindow : EditorWindow
{
    private ScreenController[] allControllers;
    private Vector2 scrollPosition;
    private bool refreshList = true;
    private bool includeInactive = false;
    private bool needsRepaint = false; // Flag to trigger a repaint

    [MenuItem("Tools/Screen Controller Manager")]
    public static void ShowWindow()
    {
        GetWindow<ScreenControllerManagerWindow>("Screen Controller Manager");
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        refreshList = true;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            refreshList = true;
        }
    }

    private void OnGUI()
    {
        // Reset the repaint flag at the start of each frame
        needsRepaint = false;

        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Screen Controller Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        if (GUILayout.Button("Refresh List"))
        {
            refreshList = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (allControllers == null)
        {
            refreshList = true;
        }

        if (refreshList)
        {
            FindAllControllers();
            refreshList = false;
        }

        if (allControllers != null && allControllers.Length > 0)
        {
            EditorGUILayout.LabelField($"Found {allControllers.Length} ScreenController(s):", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var rootObjects = allControllers
                .Where(c => c != null)
                .Select(c => c.transform.root)
                .Where(r => r != null)
                .Distinct()
                .OrderBy(r => r.GetSiblingIndex());

            foreach (var root in rootObjects)
            {
                if (root != null && root.transform != null)
                {
                    DrawHierarchyItem(root.transform, 0);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("No ScreenController instances found in the scene.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        // If the slider was changed, repaint the window to show the new children
        if (needsRepaint)
        {
            Repaint();
        }
    }

    private void FindAllControllers()
    {
        allControllers = FindObjectsOfType<ScreenController>(includeInactive);
    }

    /// <summary>
    /// NEW: Recursively draws the hierarchy until a ScreenController is found,
    /// then draws the controller and its active screen's children.
    /// </summary>
    private void DrawHierarchyItem(Transform parentTransform, int indentLevel)
    {
        if (parentTransform == null) return;

        EditorGUI.indentLevel = indentLevel;
        var controller = parentTransform.GetComponent<ScreenController>();

        // If this object has a controller, draw its UI and its active children.
        if (controller != null)
        {
            DrawScreenController(controller);

            // --- NEW LOGIC: Draw children of the active screen ---
            if (controller.screenToggler != null && controller.screenIndex >= 0 && controller.screenIndex < controller.screenToggler.transform.childCount)
            {
                Transform activeScreen = controller.screenToggler.transform.GetChild(controller.screenIndex);
                if (activeScreen != null)
                {

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    //GUILayout.Space(15); // Indent the header
                    EditorGUILayout.LabelField($"Active Screen: {activeScreen.name}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = activeScreen.gameObject;
                        EditorGUIUtility.PingObject(activeScreen.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                    //DrawActiveScreenChildren(activeScreen, 1);

                }
            }
            else
            {
                EditorGUILayout.HelpBox("Cannot display children: Screen Toggler is not set or Screen Index is out of bounds.", MessageType.Warning);
            }
        }

        bool hasChildWithController = parentTransform.Cast<Transform>().Any(child => child.GetComponentInChildren<ScreenController>(includeInactive) != null);
        if (hasChildWithController)
        {
            // If no controller on this object, recurse to find one in its children.
            // This maintains the ability to find controllers deep in the hierarchy.
            EditorGUILayout.LabelField(parentTransform.name, EditorStyles.boldLabel);
            foreach (Transform child in parentTransform)
            {
                var childController = child.GetComponentInChildren<ScreenController>();

                if (childController != null)
                {
                    DrawHierarchyItem(childController.transform, indentLevel + 1);
                }

            }
        }

        EditorGUI.indentLevel = 0;
    }

    /// <summary>
    /// NEW: Recursively draws all children of a given transform.
    /// </summary>
    private void DrawActiveScreenChildren(Transform parent, int indentLevel)
    {
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child == null) continue;

            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.LabelField(child.gameObject.name);

            // Recurse for grandchildren
            if (child.childCount > 0)
            {
                DrawActiveScreenChildren(child, indentLevel + 1);
            }
        }
    }

    /// <summary>
    /// MODIFIED: Draws the GUI for a single ScreenController and sets the repaint flag.
    /// </summary>
    private void DrawScreenController(ScreenController controller)
    {
        if (controller == null || controller.gameObject == null) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(controller.gameObject.name, EditorStyles.boldLabel);
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            Selection.activeGameObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Screen Toggler", controller.screenToggler, typeof(ToggleGroup), true);
        EditorGUI.EndDisabledGroup();

        if (controller.screenToggler != null)
        {
            int maxIndex = Mathf.Max(0, controller.screenToggler.transform.childCount - 1);
            int newIndex = EditorGUILayout.IntSlider("Screen Index", controller.screenIndex, 0, maxIndex);

            if (newIndex != controller.screenIndex)
            {
                Undo.RecordObject(controller, "Change Screen Index");
                controller.screenIndex = newIndex;

                // --- NEW: Set flag to repaint the window ---
                needsRepaint = true;

                if (!Application.isPlaying)
                {
                    for (int i = 0; i < controller.screenToggler.transform.childCount; i++)
                    {
                        Toggle toggle = controller.screenToggler.transform.GetChild(i).GetComponent<Toggle>();
                        if (toggle != null) toggle.isOn = false;
                        controller.EnableScreen(i, false);
                    }

                    Toggle selectedToggle = controller.screenToggler.transform.GetChild(newIndex).GetComponent<Toggle>();
                    if (selectedToggle != null)
                    {
                        //selectedToggle.isOn = true;
                        OnCLickTrigger trigger = selectedToggle.GetComponent<OnCLickTrigger>();
                        if (trigger != null)
                        {
                            trigger.OnToggleClicked(true);
                            trigger.toggleTrueEvent?.Invoke();
                        }
                    }
                    controller.EnableScreen(newIndex, true);
                }

                EditorUtility.SetDirty(controller);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Screen Toggler is not set for this controller.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }
}

#region Expanded All Screens
//using System.Linq;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UI;

//public class ScreenControllerManagerWindow : EditorWindow
//{
//    private ScreenController[] allControllers;
//    private Vector2 scrollPosition;
//    private bool refreshList = true;
//    private bool includeInactive = false;

//    [MenuItem("Tools/Screen Controller Manager")]
//    public static void ShowWindow()
//    {
//        GetWindow<ScreenControllerManagerWindow>("Screen Controller Manager");
//    }

//    private void OnEnable()
//    {
//        // Subscribe to the play mode state change event
//        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
//        refreshList = true;
//    }

//    private void OnDisable()
//    {
//        // Always unsubscribe to prevent memory leaks
//        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
//    }

//    private void OnPlayModeStateChanged(PlayModeStateChange state)
//    {
//        // Force a refresh when we are firmly back in Edit Mode.
//        if (state == PlayModeStateChange.EnteredEditMode)
//        {
//            refreshList = true;
//        }
//    }

//    private void OnGUI()
//    {
//        EditorGUILayout.BeginVertical();

//        EditorGUILayout.LabelField("Screen Controller Manager", EditorStyles.boldLabel);
//        EditorGUILayout.Space();

//        EditorGUILayout.BeginHorizontal();
//        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
//        if (GUILayout.Button("Refresh List"))
//        {
//            refreshList = true;
//        }
//        EditorGUILayout.EndHorizontal();

//        EditorGUILayout.Space();

//        // --- DEFENSIVE CHECK ---
//        // If our data is null, force a refresh.
//        if (allControllers == null)
//        {
//            refreshList = true;
//        }

//        if (refreshList)
//        {
//            FindAllControllers();
//            refreshList = false;
//        }

//        if (allControllers != null && allControllers.Length > 0)
//        {
//            EditorGUILayout.LabelField($"Found {allControllers.Length} ScreenController(s):", EditorStyles.miniBoldLabel);
//            EditorGUILayout.Space();

//            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

//            var rootObjects = allControllers
//                .Where(c => c != null)
//                .Select(c => c.transform.root)
//                .Where(r => r != null)
//                .Distinct()
//                .OrderBy(r => r.GetSiblingIndex());

//            foreach (var root in rootObjects)
//            {
//                if (root != null && root.transform != null)
//                {
//                    DrawHierarchyItem(root.transform, 0);
//                }
//            }

//            EditorGUILayout.EndScrollView();
//        }
//        else
//        {
//            EditorGUILayout.HelpBox("No ScreenController instances found in the scene.", MessageType.Info);
//        }

//        EditorGUILayout.EndVertical();
//    }

//    private void FindAllControllers()
//    {
//        allControllers = FindObjectsOfType<ScreenController>(includeInactive);
//    }

//    /// <summary>
//    /// Recursively draws the entire hierarchy, always expanded.
//    /// </summary>
//    /// <param name="parentTransform">The transform to draw.</param>
//    /// <param name="indentLevel">The current indentation level.</param>
//    private void DrawHierarchyItem(Transform parentTransform, int indentLevel)
//    {
//        // --- CRITICAL DEFENSIVE CHECK ---
//        if (parentTransform == null) return;

//        // Set indentation
//        EditorGUI.indentLevel = indentLevel;

//        var controller = parentTransform.GetComponent<ScreenController>();
//        bool hasChildWithController = parentTransform.Cast<Transform>().Any(child => child.GetComponentInChildren<ScreenController>(includeInactive) != null);

//        // If this object has a controller, draw its UI.
//        if (controller != null)
//        {
//            DrawScreenController(controller);
//        }
//        // If this object has no controller but has children that do, draw its name as a header.
//        else if (hasChildWithController)
//        {
//            EditorGUILayout.LabelField(parentTransform.name, EditorStyles.boldLabel);
//        }

//        // If there are children with controllers in this subtree, ALWAYS draw them.
//        if (hasChildWithController)
//        {
//            foreach (Transform child in parentTransform)
//            {
//                DrawHierarchyItem(child, indentLevel + 1);
//            }
//        }

//        // Reset indentation
//        EditorGUI.indentLevel = 0;
//    }

//    /// <summary>
//    /// Draws the GUI for a single ScreenController.
//    /// </summary>
//    /// <param name="controller">The ScreenController to draw.</param>
//    private void DrawScreenController(ScreenController controller)
//    {
//        // --- DEFENSIVE CHECK ---
//        if (controller == null || controller.gameObject == null) return;

//        EditorGUILayout.BeginVertical("box");

//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.LabelField(controller.gameObject.name, EditorStyles.boldLabel);
//        if (GUILayout.Button("Select", GUILayout.Width(60)))
//        {
//            Selection.activeGameObject = controller.gameObject;
//            EditorGUIUtility.PingObject(controller.gameObject);
//        }
//        EditorGUILayout.EndHorizontal();

//        EditorGUI.BeginDisabledGroup(true);
//        EditorGUILayout.ObjectField("Screen Toggler", controller.screenToggler, typeof(ToggleGroup), true);
//        EditorGUI.EndDisabledGroup();

//        if (controller.screenToggler != null)
//        {
//            int maxIndex = Mathf.Max(0, controller.screenToggler.transform.childCount - 1);
//            int newIndex = EditorGUILayout.IntSlider("Screen Index", controller.screenIndex, 0, maxIndex);

//            if (newIndex != controller.screenIndex)
//            {
//                Undo.RecordObject(controller, "Change Screen Index");
//                controller.screenIndex = newIndex;

//                if (!Application.isPlaying)
//                {
//                    for (int i = 0; i < controller.screenToggler.transform.childCount; i++)
//                    {
//                        Toggle toggle = controller.screenToggler.transform.GetChild(i).GetComponent<Toggle>();
//                        if (toggle != null) toggle.isOn = false;
//                        controller.EnableScreen(i, false);
//                    }

//                    Toggle selectedToggle = controller.screenToggler.transform.GetChild(newIndex).GetComponent<Toggle>();
//                    if (selectedToggle != null)
//                    {
//                        //selectedToggle.isOn = true;
//                        OnCLickTrigger trigger = selectedToggle.GetComponent<OnCLickTrigger>();
//                        if (trigger != null)
//                        {
//                            trigger.OnToggleClicked(true);
//                            trigger.toggleTrueEvent?.Invoke();
//                        }
//                    }
//                    controller.EnableScreen(newIndex, true);
//                }

//                EditorUtility.SetDirty(controller);
//            }
//        }
//        else
//        {
//            EditorGUILayout.HelpBox("Screen Toggler is not set for this controller.", MessageType.Warning);
//        }

//        EditorGUILayout.EndVertical();
//        EditorGUILayout.Space();
//    }
//}
#endregion

#region Foldout version
//using UnityEngine;
//using UnityEditor;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine.UI;

//public class ScreenControllerManagerWindow : EditorWindow
//{
//    private ScreenController[] allControllers;
//    private Dictionary<Transform, bool> foldoutStates = new Dictionary<Transform, bool>();
//    private Vector2 scrollPosition;
//    private bool refreshList = true;
//    private bool includeInactive = false;

//    [MenuItem("Tools/Screen Controller Manager")]
//    public static void ShowWindow()
//    {
//        GetWindow<ScreenControllerManagerWindow>("Screen Controller Manager");
//    }

//    private void OnEnable()
//    {
//        // Subscribe to the play mode state change event
//        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
//        refreshList = true;
//    }

//    private void OnDisable()
//    {
//        // Always unsubscribe to prevent memory leaks
//        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
//    }

//    /// <summary>
//    /// This callback is triggered when the play mode state changes.
//    /// We use it to force a refresh of our controller list when we are firmly back in Edit Mode.
//    /// </summary>
//    private void OnPlayModeStateChanged(PlayModeStateChange state)
//    {
//        if (state == PlayModeStateChange.EnteredEditMode)
//        {
//            refreshList = true;
//        }
//    }

//    private void OnGUI()
//    {
//        EditorGUILayout.BeginVertical();

//        // Header
//        EditorGUILayout.LabelField("Screen Controller Manager", EditorStyles.boldLabel);
//        EditorGUILayout.Space();

//        // Options
//        EditorGUILayout.BeginHorizontal();
//        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
//        if (GUILayout.Button("Refresh List"))
//        {
//            refreshList = true;
//        }
//        EditorGUILayout.EndHorizontal();

//        EditorGUILayout.Space();

//        // --- DEFENSIVE CHECK ---
//        // If our data is null (e.g., after a domain reload or play mode exit), force a refresh.
//        if (allControllers == null)
//        {
//            refreshList = true;
//        }

//        // Refresh the list if needed
//        if (refreshList)
//        {
//            FindAllControllers();
//            refreshList = false;
//        }

//        // Display controllers
//        if (allControllers != null && allControllers.Length > 0)
//        {
//            EditorGUILayout.LabelField($"Found {allControllers.Length} ScreenController(s):", EditorStyles.miniBoldLabel);
//            EditorGUILayout.Space();

//            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

//            // Get unique root objects. Use a null-conditional operator to prevent errors if a controller is null.
//            var rootObjects = allControllers
//                .Where(c => c != null) // Filter out any null controllers in the array
//                .Select(c => c.transform.root)
//                .Where(r => r != null) // Filter out any null root transforms
//                .Distinct()
//                .OrderBy(r => r.GetSiblingIndex());

//            foreach (var root in rootObjects)
//            {
//                // --- DEFENSIVE CHECK ---
//                // Ensure the root object and its transform are valid before drawing.
//                if (root != null && root.transform != null)
//                {
//                    DrawHierarchyItem(root.transform, 0);
//                }
//            }

//            EditorGUILayout.EndScrollView();
//        }
//        else
//        {
//            EditorGUILayout.HelpBox("No ScreenController instances found in the scene.", MessageType.Info);
//        }

//        EditorGUILayout.EndVertical();
//    }

//    private void FindAllControllers()
//    {
//        allControllers = GameObject.FindObjectsOfType<ScreenController>(includeInactive);

//        // Clean up the foldout states by removing keys for destroyed objects.
//        // This is more robust and handles cases where the dictionary might be modified.
//        var keysToRemove = foldoutStates.Keys.Where(k => k == null).ToList();
//        foreach (var key in keysToRemove)
//        {
//            foldoutStates.Remove(key);
//        }
//    }

//    /// <summary>
//    /// Recursively draws the hierarchy of objects containing ScreenControllers.
//    /// </summary>
//    /// <param name="parentTransform">The transform to draw.</param>
//    /// <param name="indentLevel">The current indentation level.</param>
//    private void DrawHierarchyItem(Transform parentTransform, int indentLevel)
//    {
//        // --- CRITICAL DEFENSIVE CHECK ---
//        // If the transform is null (e.g., destroyed during a play mode transition), exit immediately.
//        if (parentTransform == null)
//        {
//            return;
//        }

//        // Set indentation
//        EditorGUI.indentLevel = indentLevel;

//        // Check if the current transform has a ScreenController
//        var controller = parentTransform.GetComponent<ScreenController>();
//        if (controller != null)
//        {
//            DrawScreenController(controller);
//        }

//        // Check if any children have a ScreenController to determine if we need a foldout
//        bool hasChildWithController = parentTransform.Cast<Transform>().Any(child => child.GetComponentInChildren<ScreenController>(includeInactive) != null);

//        if (hasChildWithController)
//        {
//            // Get or initialize the foldout state
//            if (!foldoutStates.ContainsKey(parentTransform))
//            {
//                foldoutStates[parentTransform] = false; // Default to collapsed
//            }

//            // Draw the foldout
//            foldoutStates[parentTransform] = EditorGUILayout.Foldout(foldoutStates[parentTransform], parentTransform.name, true);

//            // If folded out, recursively draw children
//            if (foldoutStates[parentTransform])
//            {
//                foreach (Transform child in parentTransform)
//                {
//                    // The recursive call itself is protected by the check at the start of this method.
//                    DrawHierarchyItem(child, indentLevel + 1);
//                }
//            }
//        }
//        else if (controller == null)
//        {
//            // If this object has no controller and no children with controllers, we don't need to draw it.
//            return;
//        }

//        // Reset indentation
//        EditorGUI.indentLevel = 0;
//    }

//    /// <summary>
//    /// Draws the GUI for a single ScreenController.
//    /// </summary>
//    /// <param name="controller">The ScreenController to draw.</param>
//    private void DrawScreenController(ScreenController controller)
//    {
//        // --- DEFENSIVE CHECK ---
//        // Ensure the controller and its game object are valid.
//        if (controller == null || controller.gameObject == null)
//        {
//            return;
//        }

//        EditorGUILayout.BeginVertical("box");

//        // Controller name and GameObject
//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.LabelField(controller.gameObject.name, EditorStyles.boldLabel);
//        if (GUILayout.Button("Select", GUILayout.Width(60)))
//        {
//            Selection.activeGameObject = controller.gameObject;
//            EditorGUIUtility.PingObject(controller.gameObject);
//        }
//        EditorGUILayout.EndHorizontal();

//        // Screen Toggler reference
//        EditorGUI.BeginDisabledGroup(true);
//        EditorGUILayout.ObjectField("Screen Toggler", controller.screenToggler, typeof(ToggleGroup), true);
//        EditorGUI.EndDisabledGroup();

//        // Screen Index slider
//        if (controller.screenToggler != null)
//        {
//            int maxIndex = Mathf.Max(0, controller.screenToggler.transform.childCount - 1);
//            int newIndex = EditorGUILayout.IntSlider("Screen Index", controller.screenIndex, 0, maxIndex);

//            if (newIndex != controller.screenIndex)
//            {
//                Undo.RecordObject(controller, "Change Screen Index");
//                controller.screenIndex = newIndex;

//                if (!Application.isPlaying)
//                {
//                    for (int i = 0; i < controller.screenToggler.transform.childCount; i++)
//                    {
//                        Toggle toggle = controller.screenToggler.transform.GetChild(i).GetComponent<Toggle>();
//                        if (toggle != null) toggle.isOn = false;
//                        controller.EnableScreen(i, false);
//                    }

//                    Toggle selectedToggle = controller.screenToggler.transform.GetChild(newIndex).GetComponent<Toggle>();
//                    if (selectedToggle != null)
//                    {
//                        selectedToggle.isOn = true;
//                        OnCLickTrigger trigger = selectedToggle.GetComponent<OnCLickTrigger>();
//                        if (trigger != null)
//                        {
//                            trigger.OnToggleClicked(true);
//                            trigger.toggleTrueEvent?.Invoke();
//                        }
//                    }
//                    controller.EnableScreen(newIndex, true);
//                }

//                EditorUtility.SetDirty(controller);
//            }
//        }
//        else
//        {
//            EditorGUILayout.HelpBox("Screen Toggler is not set for this controller.", MessageType.Warning);
//        }

//        EditorGUILayout.EndVertical();
//        EditorGUILayout.Space();
//    }
//}
#endregion
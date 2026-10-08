using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


public class PrefabPlacerWindow : EditorWindow
{
    // --- Settings (generic for future use) ---
    [SerializeField] private GameObject prefabToPlace;
    [SerializeField] private Transform parentForPlacedObjects;
    [SerializeField] private LayerMask placementLayers = ~0; // default: everything
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [SerializeField] private bool placementMode = false;
    [SerializeField] private bool alignToSurfaceNormal = false;
    [SerializeField] private bool randomYaw = true;
    [SerializeField] private float yawMin = 0f;
    [SerializeField] private float yawMax = 360f;

    [SerializeField] private bool consumeClickEvents = true;
    [SerializeField] private bool requireLeftClick = true;

    private const string WindowTitle = "Prefab Placer";

    [MenuItem("Tools/Level Design/Prefab Placer")]
    public static void Open()
    {
        var w = GetWindow<PrefabPlacerWindow>();
        w.titleContent = new GUIContent(WindowTitle);
        w.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        placementMode = false;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Disable placement as we enter play mode
        if (state == PlayModeStateChange.ExitingEditMode ||
            state == PlayModeStateChange.EnteredPlayMode)
        {
            placementMode = false;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Edit Mode Only", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            DrawPlacementToggle();
        }

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Placement is disabled in Play Mode.", MessageType.Warning);
        }

        EditorGUILayout.Space(8);
        DrawSettings();
        EditorGUILayout.Space(8);
        DrawHelp();
    }

    private void DrawPlacementToggle()
    {
        var old = placementMode;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Placement Mode", EditorStyles.boldLabel);

            // Big toggle button
            GUI.backgroundColor = placementMode ? new Color(0.6f, 1f, 0.6f) : Color.white;
            placementMode = GUILayout.Toggle(placementMode, placementMode ? "ON (Click to Place)" : "OFF", "Button", GUILayout.Height(28));
            GUI.backgroundColor = Color.white;

            if (placementMode && prefabToPlace == null)
            {
                EditorGUILayout.HelpBox("Assign a Prefab to place before enabling placement.", MessageType.Error);
            }
        }

        // If user turned it on without a prefab, force it off
        if (!old && placementMode && prefabToPlace == null)
        {
            placementMode = false;
        }
    }

    private void DrawSettings()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Prefab Settings", EditorStyles.boldLabel);

            prefabToPlace = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Prefab To Place"),
                prefabToPlace,
                typeof(GameObject),
                false);

            parentForPlacedObjects = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Parent (Optional)"),
                parentForPlacedObjects,
                typeof(Transform),
                true);

            placementLayers = LayerMaskField("Placement Layers", placementLayers);

            positionOffset = EditorGUILayout.Vector3Field(
                new GUIContent("Position Offset"),
                positionOffset);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);

            alignToSurfaceNormal = EditorGUILayout.Toggle(
                new GUIContent("Align To Surface Normal"),
                alignToSurfaceNormal);

            randomYaw = EditorGUILayout.Toggle(
                new GUIContent("Random Yaw (Y axis)"),
                randomYaw);

            using (new EditorGUI.DisabledScope(!randomYaw))
            {
                yawMin = EditorGUILayout.FloatField(new GUIContent("Yaw Min"), yawMin);
                yawMax = EditorGUILayout.FloatField(new GUIContent("Yaw Max"), yawMax);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);

            consumeClickEvents = EditorGUILayout.Toggle(
                new GUIContent("Consume Click Events", "Prevents selecting objects while placing"),
                consumeClickEvents);

            requireLeftClick = EditorGUILayout.Toggle(
                new GUIContent("Require Left Click"),
                requireLeftClick);
        }
    }

    private void DrawHelp()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("How to Use", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1) Assign Prefab (coin).");
            EditorGUILayout.LabelField("2) Set Placement Layers (e.g., Environment).");
            EditorGUILayout.LabelField("3) Toggle Placement Mode ON.");
            EditorGUILayout.LabelField("4) Click the environment in Scene View to place repeatedly.");
            EditorGUILayout.LabelField("5) Toggle OFF when done.");
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Hard block: no runtime / play mode.
        if (Application.isPlaying) return;
        if (!placementMode) return;
        if (prefabToPlace == null) return;

        // Make sure SceneView keeps getting events
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Event e = Event.current;
        if (e == null) return;

        // Optional: show a simple hint
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 260, 60), EditorStyles.helpBox);
        GUILayout.Label("Prefab Placer: Placement ON", EditorStyles.boldLabel);
        GUILayout.Label("Click on allowed surfaces to place.");
        GUILayout.EndArea();
        Handles.EndGUI();

        // Only place on mouse down (click)
        if (e.type != EventType.MouseDown) return;

        if (requireLeftClick && e.button != 0) return; // left click only

        // Avoid placing while alt is held (camera orbit)
        if (e.alt) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayers, QueryTriggerInteraction.Ignore))
        {
            PlacePrefabAtHit(hit);
            if (consumeClickEvents)
                e.Use();
        }
        else
        {
            // no hit => do nothing
            if (consumeClickEvents)
                e.Use();
        }
    }

    private void PlacePrefabAtHit(RaycastHit hit)
    {
        // Position
        Vector3 pos = hit.point + positionOffset;

        // Rotation
        Quaternion rot = Quaternion.identity;

        if (alignToSurfaceNormal)
        {
            // Align prefab's up axis to the surface normal
            rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        else
        {
            // Keep prefab's original rotation if possible
            rot = prefabToPlace.transform.rotation;
        }

        if (randomYaw)
        {
            float yaw = Random.Range(yawMin, yawMax);
            rot = rot * Quaternion.Euler(0f, yaw, 0f);
        }

        // Instantiate as prefab instance (keeps connection)
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);

        if (instance == null)
        {
            // Fallback
            instance = Instantiate(prefabToPlace);
            instance.name = prefabToPlace.name;
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Place {prefabToPlace.name}");

        if (parentForPlacedObjects != null)
        {
            Undo.SetTransformParent(instance.transform, parentForPlacedObjects, "Parent placed object");
        }

        instance.transform.position = pos;
        instance.transform.rotation = rot;

        // Mark scene dirty for saving
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // Unity doesn't expose LayerMask field nicely by default; implement it.
    private static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        var layers = UnityEditorInternal.InternalEditorUtility.layers;
        int maskWithoutEmpty = 0;

        for (int i = 0; i < layers.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layers[i]);
            if (((1 << layer) & selected.value) != 0)
                maskWithoutEmpty |= (1 << i);
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

        int mask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) != 0)
                mask |= (1 << LayerMask.NameToLayer(layers[i]));
        }

        selected.value = mask;
        return selected;
    }
}
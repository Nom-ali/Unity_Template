using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

public class ComponentCopierWindow : EditorWindow
{
    private GameObject originalObject;
    private List<GameObject> targetObjects = new List<GameObject>();
    private Dictionary<Component, bool> componentsToCopy = new Dictionary<Component, bool>();
    private bool copyValues = false;

    [MenuItem("Window/Component Copier")]
    public static void ShowWindow()
    {
        GetWindow<ComponentCopierWindow>("Component Copier");
    }

    private void OnGUI()
    {
        GUILayout.Label("Component Copier", EditorStyles.boldLabel);

        originalObject = (GameObject)EditorGUILayout.ObjectField("Original GameObject", originalObject, typeof(GameObject), true);

        // Target GameObjects field
        GUILayout.Label("Target GameObjects (Hold Ctrl to add multiple):");
        for (int i = 0; i < targetObjects.Count; i++)
        {
            targetObjects[i] = (GameObject)EditorGUILayout.ObjectField($"Target GameObject {i + 1}", targetObjects[i], typeof(GameObject), true);
        }

        if (GUILayout.Button("Add Target GameObject"))
        {
            targetObjects.Add(null);
        }

        if (originalObject != null)
        {
            DrawComponentList();
        }

        copyValues = EditorGUILayout.Toggle("Copy Component Values", copyValues);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            SelectAllComponents(true);
        }

        if (GUILayout.Button("Deselect All"))
        {
            SelectAllComponents(false);
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Copy Components"))
        {
            CopyComponents();
        }
    }

    private void DrawComponentList()
    {
        // Make a copy of the dictionary to handle the GUI state.
        var tempComponentsToCopy = new Dictionary<Component, bool>(componentsToCopy);

        var components = originalObject.GetComponents<Component>();

        EditorGUILayout.BeginVertical();

        foreach (var component in components)
        {
            if (component == null || component is Transform) continue;

            // Use a unique identifier for each component
            string componentName = $"{component.GetType().Name}";

            // Initialize checkbox state based on the temp dictionary
            bool isSelected = tempComponentsToCopy.ContainsKey(component) && tempComponentsToCopy[component];

            // Display the checkbox and update the state
            bool newSelection = EditorGUILayout.Toggle(componentName, isSelected);

            // Update the temp dictionary
            if (tempComponentsToCopy.ContainsKey(component))
            {
                tempComponentsToCopy[component] = newSelection;
            }
            else
            {
                tempComponentsToCopy.Add(component, newSelection);
            }
        }

        // Update the main dictionary with the new states
        componentsToCopy = new Dictionary<Component, bool>(tempComponentsToCopy);

        EditorGUILayout.EndVertical();

        // Force repaint to update the GUI
        Repaint();
    }

    private void SelectAllComponents(bool select)
    {
        var components = originalObject.GetComponents<Component>();

        // Collect components to update
        var componentsToUpdate = new List<Component>(components);

        foreach (var component in componentsToUpdate)
        {
            if (component == null || component is Transform) continue;

            // Update the main dictionary safely
            if (componentsToCopy.ContainsKey(component))
            {
                componentsToCopy[component] = select;
            }
            else
            {
                componentsToCopy.Add(component, select);
            }
        }

        // Force repaint to reflect the changes
        Repaint();
    }

    private void CopyComponents()
    {
        if (targetObjects.Count == 0 || originalObject == null)
        {
            Debug.LogWarning("Please assign both the original and at least one target GameObject.");
            return;
        }

        foreach (var target in targetObjects)
        {
            if (target == null) continue;

            foreach (var kvp in componentsToCopy)
            {
                if (kvp.Value)
                {
                    var component = kvp.Key;
                    var componentType = component.GetType();

                    if (copyValues)
                    {
                        CopyComponentWithValues(component, target);
                    }
                    else
                    {
                        target.AddComponent(componentType);
                    }

                    Debug.Log($"Copied component: {componentType.Name} to {target.name}");
                }
            }
        }
    }

    private void CopyComponentWithValues(Component component, GameObject target)
    {
        var targetComponent = target.AddComponent(component.GetType());

        var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (!field.IsStatic)
            {
                field.SetValue(targetComponent, field.GetValue(component));
            }
        }
    }
}





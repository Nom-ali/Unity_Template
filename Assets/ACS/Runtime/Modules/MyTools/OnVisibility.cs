using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class OnVisibility : MonoBehaviour
{
    private Component[] components;
    private Dictionary<Component, bool> previousStates;

    private void Awake()
    {
        // Cache all components attached to this GameObject
        components = transform.parent.gameObject.GetComponents<Component>();
        previousStates = new Dictionary<Component, bool>();

        // Store the current enabled status of each component
        foreach (var component in components)
        {
            if (!(component is Transform))
            {
                previousStates[component] = component.GetType().GetProperty("enabled").GetValue(component, null) as bool? ?? false;
            }
        }
    }

    private void OnBecameInvisible()
    {
        // Disable all components and store their previous states
        foreach (var component in components)
        {
            if (!(component is Transform))
            {
                var property = component.GetType().GetProperty("enabled");
                if (property != null)
                {
                    previousStates[component] = (bool)property.GetValue(component);
                    property.SetValue(component, false);
                }
            }
        }
    }

    private void OnBecameVisible()
    {
        // Enable components based on their previous states
        foreach (var component in components)
        {
            if (!(component is Transform))
            {
                var property = component.GetType().GetProperty("enabled");
                if (property != null && previousStates.TryGetValue(component, out bool wasEnabled))
                {
                    property.SetValue(component, wasEnabled);
                }
            }
        }
    }
}





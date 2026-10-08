using UnityEngine;
using System.Collections.Generic;
using MyBox;

public class SingletonManager : MonoBehaviour
{
    // A dictionary to hold references to singleton instances
    private static Dictionary<System.Type, MonoBehaviour> Singletons = new();
   
    // Register a MonoBehaviour-derived class as a singleton
    public static void RegisterSingleton<T>(T instance) where T : MonoBehaviour
    {
        // Check if the instance is null
        if (instance == null)
        {
            Debug.LogError("SingletonManager: Attempted to register a null instance as a singleton.");
            return;
        }

        // If the singleton is already registered, log a warning and return
        if (Singletons.ContainsKey(typeof(T)))
        {
            Debug.LogWarning($"SingletonManager: {typeof(T)} is already registered as a singleton.");
            return;
        }

        // Register the singleton instance in the dictionary
        Singletons[typeof(T)] = instance;

        Debug.Log($"SingletonManager: {typeof(T)} successfully registered as a singleton, Count: {Singletons.Count}");
    }

    public static void RegisterSingleton<T>(T instance, bool setParent) where T : MonoBehaviour
    {
        // Check if the instance is null
        if (instance == null)
        {
            Debug.LogError("SingletonManager: Attempted to register a null instance as a singleton.");
            return;
        }

        // If the singleton is already registered, log a warning and return
        if (Singletons.ContainsKey(typeof(T)))
        {
            Debug.LogWarning($"SingletonManager: {typeof(T)} is already registered as a singleton.");
            return;
        }

        // Register the singleton instance in the dictionary
        Singletons[typeof(T)] = instance;

        // Optionally, you can ensure the singleton is a child of this manager for scene persistence
        if(setParent) ParentIfNeeded(instance.gameObject);

        Debug.Log($"SingletonManager: {typeof(T)} successfully registered as a singleton, Count: {Singletons.Count}");
    }

    // Unregister a MonoBehaviour-derived class from the Singletons dictionary
    public static void UnregisterSingleton<T>(bool destroyGameObject = false) where T : MonoBehaviour
    {
        // Check if the singleton exists
        if (Singletons.TryGetValue(typeof(T), out MonoBehaviour instance))
        {
            // Log the unregistration

            // Optionally, destroy the GameObject if specified
            if (destroyGameObject)
            {
                //GameObject singletonObject = Singletons[typeof(T)].gameObject;
                Destroy(instance.gameObject);
                Debug.Log($"SingletonManager: {typeof(T)}'s GameObject destroyed.");
            }

            // Remove the singleton from the dictionary
            Singletons.Remove(typeof(T));
            Debug.Log($"SingletonManager: {typeof(T)} successfully unregistered as a singleton, Count: {Singletons.Count}");
        }
        else
        {
            Debug.LogWarning($"SingletonManager: {typeof(T)} not registered as a singleton, cannot unregister.");
        }
    }



    // Retrieve the singleton instance by type
    public static T GetSingleton<T>() where T : MonoBehaviour
    {
        if (Singletons.TryGetValue(typeof(T), out MonoBehaviour singleton))
        {
            return singleton as T;
        }
        else
        {
            Debug.LogError($"SingletonManager: Singleton of type {typeof(T)} not found.");
            return null;
        }
    }

    // Optionally, clear all registered Singletons
    public static void ClearSingletons()
    {
        Singletons.Clear();
    }

    // SingletonManager instance for easier access
    private static SingletonManager instance;

    private void Awake()
    {
        // Ensure only one SingletonManager exists and persist it across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("SingletonManager: An instance of SingletonManager already exists. Destroying the new one.");
            Destroy(gameObject);
        }
    }

    public static void ParentIfNeeded(GameObject targetObject)
    {
        if (targetObject.HasComponent<RectTransform>())
        {
            // The object has a RectTransform, so look for a Canvas in the children
            Canvas canvas = instance.transform.GetComponentInChildren<Canvas>();

            if (canvas != null)
            {
                // If a Canvas is found, make this object a child of that Canvas
                targetObject.transform.SetParent(canvas.transform);
                Debug.Log($"SingletonManager: {targetObject.name} parented to Canvas.");
            }
            else
            {
                Debug.LogWarning($"SingletonManager: No Canvas found in children of {targetObject.name}.");
            }
        }
        else
        {
                targetObject.transform.SetParent(instance.transform);
                Debug.Log($"SingletonManager: {targetObject.name} parented to simple parent.");
        }
    }
}





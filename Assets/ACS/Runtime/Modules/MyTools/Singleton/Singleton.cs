using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                if (_instance == null)
                {
                    Debug.LogError($"No instance of {typeof(T)} found in the scene.");
                }
            }
            return _instance;
        }
    }

    [Header("Optional Singleton Settings")]
    [Tooltip("Should this singleton persist between scenes?")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject); // Ensures only one instance exists
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}





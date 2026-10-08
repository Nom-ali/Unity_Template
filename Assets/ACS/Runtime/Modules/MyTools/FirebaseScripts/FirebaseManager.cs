#if ACS_FIREBASE
using ACS.AdsManager;
using Firebase.Analytics;
using Firebase.Extensions;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [SerializeField] private FirebaseSDK SelectedFirebaseSDK = FirebaseSDK.RemoteConfig | FirebaseSDK.Analytics;

    [Space]
    [SerializeField] private CustomAnalytics customAnalytics;
#if ACS_FIREBASE_REMOTE_CONFIG
    [SerializeField] private FirebaseRemoteConfigManager remoteConfig;
#endif

    private bool isInitialized;
    internal bool IsInitialized => isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        yield return null;

#if UNITY_IOS && ACS_APPLE_ATT && !UNITY_EDITOR
        yield return WaitForIosTrackingAuthorization();
#elif UNITY_ANDROID && !UNITY_EDITOR
#if ACS_APPLOVIN
        Debug.Log("Android consent: using AppLovin MAX.");
        yield return AndroidConsentGate.WaitForAppLovinConsent();
#elif ACS_ADMOB_UMP
        Debug.Log("Android consent: AppLovin is unavailable; using Google UMP.");
        GDPR_Android.EnsureConsentFlow();
        yield return new WaitUntil(() => GDPR_Android.IsConsentFlowComplete);
#else
        Debug.Log("Android consent: no supported consent SDK is enabled; continuing without a consent form.");
#endif
#endif

        Init();
    }

#if UNITY_IOS && ACS_APPLE_ATT && !UNITY_EDITOR
    private IEnumerator WaitForIosTrackingAuthorization()
    {
        if (ATT_PopupHandler.Instance == null)
        {
            Debug.Log("iOS ATT: no scene handler found; creating one automatically.");
            new GameObject("ATT_PopupHandler").AddComponent<ATT_PopupHandler>();
        }

        const float timeoutSeconds = 30f;
        float timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;

        while (!ATT_PopupHandler.IsTrackingAuthorizationResolved && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }

        if (!ATT_PopupHandler.IsTrackingAuthorizationResolved)
        {
            Debug.LogWarning($"iOS ATT: authorization was not resolved within {timeoutSeconds} seconds. Continuing startup.");
        }
    }
#endif

    internal async void Init()
    {
        if (SelectedFirebaseSDK == FirebaseSDK.None)
        {
            Debug.LogError("Firebase: No SDK selected. Please select at least one SDK.");
            return;
        }

        await Setup();
        await Custom_Start();
    }

    private async Task Setup()
    {
        if (SelectedFirebaseSDK.HasFlag(FirebaseSDK.Analytics) && customAnalytics == null)
        {
            customAnalytics = gameObject.AddComponent<CustomAnalytics>();
        }

#if ACS_FIREBASE_REMOTE_CONFIG
        if (SelectedFirebaseSDK.HasFlag(FirebaseSDK.RemoteConfig) && remoteConfig == null)
        {
            remoteConfig = gameObject.AddComponent<FirebaseRemoteConfigManager>();
        }
#endif

        await Task.CompletedTask;
    }

    private async Task Custom_Start()
    {
        Firebase.DependencyStatus dependencyStatus = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task => task.Result);

        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            if (SelectedFirebaseSDK.HasFlag(FirebaseSDK.Analytics))
            {
                customAnalytics.Initialize(dependencyStatus);
            }

#if ACS_FIREBASE_REMOTE_CONFIG
            if (SelectedFirebaseSDK.HasFlag(FirebaseSDK.RemoteConfig) && remoteConfig != null)
            {
                await remoteConfig.InitializeFirebaseAsync(dependencyStatus);
            }
#endif

            isInitialized = true;
            Debug.Log("Firebase: is ready to Use!!!");
            return;
        }

        isInitialized = false;
        Debug.LogError($"Firebase: Could not resolve all Firebase dependencies: {dependencyStatus}");
    }

    public T GetRemoteConfigValue<T>(string key, T defaultValue)
    {
#if ACS_FIREBASE_REMOTE_CONFIG
        if (remoteConfig == null)
        {
            Debug.LogError("Firebase: RemoteConfigManager is not initialized:\nReturning default value: Key: {key}, VAlue: {defaultValue}");
            return defaultValue;
        }

        try
        {
            var value = remoteConfig.GetConfigValue<T>(key, defaultValue);
            return value;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Firebase: Error getting config value for key '{key}': {ex.Message}");
            return defaultValue;
        }
#else
        return defaultValue;
#endif
    }

    public void LogEvents(params string[] messages)
    {
        if (customAnalytics == null)
        {
            Debug.LogError("Firebase: CustomAnalytics is not initialized.");
            return;
        }

        if (messages == null || messages.Length == 0)
        {
            Debug.LogError("Firebase: No messages to log.");
            return;
        }

        customAnalytics.LogEvent(messages);
    }

    public void LogEvents(string messages, Parameter[] parameters)
    {
        if (customAnalytics == null)
        {
            Debug.LogError("Firebase: CustomAnalytics is not initialized.");
            return;
        }

        if (messages == null || messages.Length == 0 || parameters == null || parameters.Length <= 0)
        {
            Debug.LogError("Firebase: No messages to log.");
            return;
        }

        customAnalytics.LogEvent(messages, parameters);
    }
}

[System.Serializable, System.Flags]
public enum FirebaseSDK
{
    None = 0,
    Analytics = 1 << 0,
    RemoteConfig = 1 << 1
}
#endif

#if ACS_FIREBASE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
#if ACS_FIREBASE_REMOTE_CONFIG
using Firebase.RemoteConfig;
#endif
using UnityEngine;

public class FirebaseRemoteConfigManager : MonoBehaviour
{
    [SerializeField] private uint fetchIntervalInSeconds = 3600;
    [SerializeField] private bool fetchOnAwake = true;

    private bool isFirebaseInitialized = false;
    private DateTime lastFetchTime;

    internal async Task InitializeFirebaseAsync(DependencyStatus dependencyStatus)
    {
        try
        {
            if (dependencyStatus == DependencyStatus.Available)
            {
                isFirebaseInitialized = true;
#if ACS_FIREBASE_REMOTE_CONFIG
                await SetDefaultValuesAsync();

                if (fetchOnAwake)
                {
                    await FetchRemoteConfigAsync();
                }
#else
                await Task.Yield();
#endif
            }
            else
            {
                Debug.LogError($"Remote: Could not resolve Firebase dependencies: {dependencyStatus}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Remote: initialization failed: {ex.Message}");
        }
    }

#if ACS_FIREBASE_REMOTE_CONFIG
    private async Task SetDefaultValuesAsync()
    {
        var defaults = new Dictionary<string, object>
        {
#if UNITY_IOS
            { RemoteConfigKeys.ENABLE_ADS_IOS, true },
#elif UNITY_ANDROID
            { RemoteConfigKeys.ENABLE_ADS_ANDROID, true },
#endif
        };
        try
        {
            await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Remote: Failed to set default values: {ex.Message}");
        }
    }

    public async Task<bool> FetchRemoteConfigAsync()
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogWarning("Remote: Firebase is not initialized yet. Cannot fetch remote config.");
            return false;
        }

        TimeSpan timeSinceLastFetch = DateTime.UtcNow - lastFetchTime;
        if (timeSinceLastFetch.TotalSeconds < fetchIntervalInSeconds)
        {
            Debug.Log($"Remote: Skipping fetch due to cooldown. Next fetch available in {fetchIntervalInSeconds - timeSinceLastFetch.TotalSeconds} seconds");
            return true;
        }

        try
        {
            await FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.FromSeconds(3));
            lastFetchTime = DateTime.UtcNow;

            bool activated = await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();

            if (activated)
            {
                Debug.Log("Remote: config fetched and activated successfully");
            }
            else
            {
                Debug.Log("Remote: config was fetched but not activated (no changes found)");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Remote: config fetch or activation failed: {ex.Message}");
            return false;
        }
    }

    public void FetchRemoteConfig()
    {
        _ = FetchRemoteConfigAsync();
    }

    internal T GetConfigValue<T>(string key, T defaultValue)
    {
        if (!isFirebaseInitialized)
        {
            Debug.Log($"Remote: Firebase not initialized. Returning default value for key: {key}");
            return defaultValue;
        }

        try
        {
            ConfigValue value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            if (typeof(T) == typeof(bool))
            {
                T result = (T)(object)value.BooleanValue;
                Debug.Log($"Remote: Key: {key}, Value: {result}");
                return result;
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
            {
                T result = (T)(object)(int)value.LongValue;
                Debug.Log($"Remote: Key: {key}, Value: {result}");
                return result;
            }
            else if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                T result = (T)(object)(float)value.DoubleValue;
                Debug.Log($"Remote: Key: {key}, Value: {result}");
                return result;
            }
            else if (typeof(T) == typeof(string))
            {
                T result = (T)(object)value.StringValue;
                Debug.Log($"Remote: Key: {key}, Value: {result}");
                return result;
            }

            Debug.Log($"Remote: Key: {key} has unsupported type. Returning default value.");
            return defaultValue;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Remote: Error retrieving value for key: {key}, Returning default value. Exception: {ex.Message}");
            return defaultValue;
        }
    }   

    public async Task<bool> ForceFetchRemoteConfigAsync()
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogWarning("Remote: Firebase is not initialized yet. Cannot fetch remote config.");
            return false;
        }

        try
        {
            await FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.FromSeconds(3));
            lastFetchTime = DateTime.UtcNow;

            bool activated = await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
            Debug.Log(activated ?
                "Remote: config force fetched and activated" :
                "Remote: config force fetched but not activated (no changes found)");

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Remote: Force fetch of remote config failed: {ex.Message}");
            return false;
        }
    }
#endif
    public DateTime GetLastFetchTime()
    {
        return lastFetchTime;
    }
}
#else
                using UnityEngine;

public class FirebaseRemoteConfigManager : MonoBehaviour
{
}
#endif





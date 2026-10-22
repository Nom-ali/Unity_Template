using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
#if ACS_UGS_REMOTE_CONFIG
using Unity.Services.RemoteConfig;
#endif

namespace ACS.UnityServices
{
    public enum RemoteFetchStatus { Unavailable, Fresh, Cached, Default, Failed }
    public sealed class RemoteFetchResult
    {
        public RemoteFetchStatus Status { get; private set; }
        public UpdatePolicy UpdatePolicy { get; private set; }
        public RemoteFetchResult(RemoteFetchStatus status, UpdatePolicy policy = null) { Status = status; UpdatePolicy = policy; }
    }
    [DisallowMultipleComponent]
    public sealed class UnityRemoteConfigManager : MonoBehaviour
    {
        public bool IsInitialized { get; private set; }
        public RemoteFetchStatus LastFetchStatus { get; private set; }
        private Task<RemoteFetchResult> fetching;
#if ACS_UGS_REMOTE_CONFIG
        private struct Attributes { }
#endif
        internal void Initialize() { IsInitialized = true; }
        public Task<RemoteFetchResult> FetchAsync(string platformPrefix)
        {
            if (fetching == null || fetching.IsCompleted) fetching = FetchInternalAsync(platformPrefix);
            return fetching;
        }
        private async Task<RemoteFetchResult> FetchInternalAsync(string prefix)
        {
#if ACS_UGS_REMOTE_CONFIG
            if (!IsInitialized) return new RemoteFetchResult(RemoteFetchStatus.Unavailable);
            try
            {
                var config = await RemoteConfigService.Instance.FetchConfigsAsync(new Attributes(), new Attributes());
                LastFetchStatus = config.origin == ConfigOrigin.Remote ? RemoteFetchStatus.Fresh :
                    config.origin == ConfigOrigin.Cached ? RemoteFetchStatus.Cached : RemoteFetchStatus.Default;
                UpdatePolicy policy;
                UpdatePolicy.TryCreate(config.GetString(prefix + "minimum_version", ""),
                    config.GetString(prefix + "latest_version", ""), config.GetString(prefix + "update_severity", ""),
                    config.GetString(prefix + "store_url", ""), out policy);
                if (LastFetchStatus == RemoteFetchStatus.Fresh && policy == null)
                    Debug.LogWarning("Unity Remote Config: update policy is missing or invalid; allowing play.", this);
                return new RemoteFetchResult(LastFetchStatus, policy);
            }
            catch (Exception ex) { Debug.LogWarning("Unity Remote Config fetch failed: " + ex.Message, this); LastFetchStatus = RemoteFetchStatus.Failed; }
#endif
            await Task.CompletedTask;
#if ACS_UGS_REMOTE_CONFIG
            return new RemoteFetchResult(LastFetchStatus = RemoteFetchStatus.Failed);
#else
            return new RemoteFetchResult(LastFetchStatus = RemoteFetchStatus.Unavailable);
#endif
        }
        public T GetValue<T>(string key, T fallback)
        {
#if ACS_UGS_REMOTE_CONFIG
            if (!IsInitialized) return fallback;
            try
            {
                var c = RemoteConfigService.Instance.appConfig;
                if (!c.HasKey(key)) return fallback;
                object value;
                if (typeof(T) == typeof(string)) value = c.GetString(key);
                else if (typeof(T) == typeof(bool)) value = c.GetBool(key);
                else if (typeof(T) == typeof(int)) value = c.GetInt(key);
                else if (typeof(T) == typeof(long)) value = c.GetLong(key);
                else if (typeof(T) == typeof(float)) value = c.GetFloat(key);
                else if (typeof(T) == typeof(double)) value = Convert.ToDouble(c.GetJson(key), CultureInfo.InvariantCulture);
                else return fallback;
                return (T)value;
            }
            catch (Exception ex) { Debug.LogWarning("Unity Remote Config value '" + key + "': " + ex.Message, this); }
#endif
            return fallback;
        }
    }
}

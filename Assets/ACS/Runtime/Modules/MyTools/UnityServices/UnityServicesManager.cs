using System;
using System.Threading.Tasks;
using UnityEngine;
#if ACS_UGS_CORE
using Unity.Services.Core;
using Unity.Services.Core.Environments;
#endif
#if ACS_UGS_AUTH
using Unity.Services.Authentication;
#endif

namespace ACS.UnityServices
{
    public enum ServiceStatus { Disabled, PackageMissing, WaitingForDependency, Initializing, Ready, Failed }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityRemoteConfigManager), typeof(UnityAnalyticsManager))]
    [RequireComponent(typeof(UnityCloudSaveManager), typeof(UnityPlayerAccountsManager))]
    public sealed class UnityServicesManager : MonoBehaviour
    {
        public static UnityServicesManager Instance { get; private set; }
        [SerializeField] private bool enableAuthentication = true;
        [SerializeField] private bool enableRemoteConfig = true;
        [SerializeField] private bool enableAnalytics;
        [SerializeField] private bool enableCloudSave;
        [SerializeField] private bool enablePlayerAccounts;
        [SerializeField] private string environment = "production";
        [SerializeField] private bool initializeOnStart = true;
        public bool AuthenticationEnabled => enableAuthentication;
        public bool RemoteConfigEnabled => enableRemoteConfig;
        public bool AnalyticsEnabled => enableAnalytics;
        public bool CloudSaveEnabled => enableCloudSave;
        public bool PlayerAccountsEnabled => enablePlayerAccounts;
        public ServiceStatus AuthenticationStatus { get; private set; }
        public ServiceStatus RemoteConfigStatus { get; private set; }
        public ServiceStatus AnalyticsStatus { get; private set; }
        public ServiceStatus CloudSaveStatus { get; private set; }
        public ServiceStatus PlayerAccountsStatus { get; private set; }
        public UnityRemoteConfigManager RemoteConfig { get; private set; }
        public UnityAnalyticsManager Analytics { get; private set; }
        public UnityCloudSaveManager CloudSave { get; private set; }
        public UnityPlayerAccountsManager PlayerAccounts { get; private set; }
        public UnityProgressSync ProgressSync { get; private set; }
        public bool IsAccountTransitionInProgress => accountTransition != null;
        internal string DataOwnerKey => "ACS.CloudSave.LocalOwner.v1." + Application.cloudProjectId + "." + environment;
        public string CurrentPlayerId
        {
            get
            {
                if (PlayerAccounts && PlayerAccounts.IsInitialized) return PlayerAccounts.PlayerId;
#if ACS_UGS_AUTH && ACS_UGS_CORE
                if (AuthenticationStatus == ServiceStatus.Ready && AuthenticationService.Instance.IsSignedIn)
                    return AuthenticationService.Instance.PlayerId;
#endif
                return "";
            }
        }
        private TaskCompletionSource<bool> accountTransition;
        private Task initialization;
        private static Task coreInitialization;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { Instance = null; coreInitialization = null; }

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RemoteConfig = GetComponent<UnityRemoteConfigManager>();
            Analytics = GetComponent<UnityAnalyticsManager>();
            CloudSave = GetComponent<UnityCloudSaveManager>();
            // Existing scene instances may predate this required component.
            if (!CloudSave) CloudSave = gameObject.AddComponent<UnityCloudSaveManager>();
            CloudSave.AttachOwner(this);
            PlayerAccounts = GetComponent<UnityPlayerAccountsManager>();
            if (!PlayerAccounts) PlayerAccounts = gameObject.AddComponent<UnityPlayerAccountsManager>();
            PlayerAccounts.AttachOwner(this);
            ProgressSync = GetComponent<UnityProgressSync>();
            if (!ProgressSync) ProgressSync = gameObject.AddComponent<UnityProgressSync>();
            ProgressSync.Initialize(this);
            RefreshStatuses();
        }
        private async void Start() { if (initializeOnStart) await InitializeAsync(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void OnValidate()
        {
            if (enableRemoteConfig && !enableAuthentication)
                Debug.LogWarning("Unity Remote Config requires Authentication to be checked. Remote Config will be skipped.", this);
            if (enablePlayerAccounts && !enableAuthentication)
                Debug.LogWarning("Unity Player Accounts requires Authentication to be checked. Player Accounts will be skipped.", this);
            if (enableCloudSave && !enableAuthentication)
                Debug.LogWarning("Unity Cloud Save requires Authentication to be checked. Cloud Save will be skipped.", this);
        }
        private void RefreshStatuses()
        {
            AuthenticationStatus = !enableAuthentication ? ServiceStatus.Disabled : ServiceStatus.PackageMissing;
            RemoteConfigStatus = !enableRemoteConfig ? ServiceStatus.Disabled : ServiceStatus.PackageMissing;
            AnalyticsStatus = !enableAnalytics ? ServiceStatus.Disabled : ServiceStatus.PackageMissing;
            CloudSaveStatus = !enableCloudSave ? ServiceStatus.Disabled : ServiceStatus.PackageMissing;
            PlayerAccountsStatus = !enablePlayerAccounts ? ServiceStatus.Disabled : ServiceStatus.PackageMissing;
#if ACS_UGS_PLAYER_ACCOUNTS
            if (enablePlayerAccounts) PlayerAccountsStatus = ServiceStatus.WaitingForDependency;
#endif
#if ACS_UGS_AUTH
            if (enableAuthentication) AuthenticationStatus = ServiceStatus.WaitingForDependency;
#endif
#if ACS_UGS_REMOTE_CONFIG
            if (enableRemoteConfig) RemoteConfigStatus = ServiceStatus.WaitingForDependency;
#endif
#if ACS_UGS_ANALYTICS
            if (enableAnalytics) AnalyticsStatus = ServiceStatus.WaitingForDependency;
#endif
#if ACS_UGS_CLOUD_SAVE
            if (enableCloudSave) CloudSaveStatus = ServiceStatus.WaitingForDependency;
#endif
        }
        public Task InitializeAsync()
        {
            if (accountTransition != null) return accountTransition.Task;
            if (initialization == null || (initialization.IsCompleted &&
                (AuthenticationStatus == ServiceStatus.Failed || AnalyticsStatus == ServiceStatus.Failed)))
                initialization = InitializeServicesAsync();
            return initialization;
        }
        private async Task InitializeServicesAsync()
        {
#if ACS_UGS_CORE
            bool needsCore = AuthenticationStatus != ServiceStatus.Disabled && AuthenticationStatus != ServiceStatus.PackageMissing;
            needsCore |= AnalyticsStatus != ServiceStatus.Disabled && AnalyticsStatus != ServiceStatus.PackageMissing;
            if (!needsCore) return;
            try
            {
                if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
                {
                    if (coreInitialization == null || coreInitialization.IsFaulted || coreInitialization.IsCanceled)
                        coreInitialization = Unity.Services.Core.UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(environment));
                    await coreInitialization;
                }
            }
            catch (Exception ex)
            {
                if (AuthenticationStatus == ServiceStatus.WaitingForDependency) AuthenticationStatus = ServiceStatus.Failed;
                if (AnalyticsStatus == ServiceStatus.WaitingForDependency) AnalyticsStatus = ServiceStatus.Failed;
                if (RemoteConfigStatus == ServiceStatus.WaitingForDependency && enableAuthentication) RemoteConfigStatus = ServiceStatus.Failed;
                if (CloudSaveStatus == ServiceStatus.WaitingForDependency && enableAuthentication) CloudSaveStatus = ServiceStatus.Failed;
                if (PlayerAccountsStatus == ServiceStatus.WaitingForDependency && enableAuthentication) PlayerAccountsStatus = ServiceStatus.Failed;
                Debug.LogWarning("Unity Services initialization failed: " + ex.Message, this);
                return;
            }
#endif
            // Analytics stays dormant until its own consent is accepted; it does not await sign-in.
#if ACS_UGS_ANALYTICS && ACS_UGS_CORE
            if (enableAnalytics && AnalyticsStatus != ServiceStatus.Ready)
            {
                try { AnalyticsStatus = ServiceStatus.Initializing; Analytics.Initialize(); AnalyticsStatus = ServiceStatus.Ready; }
                catch (Exception ex) { AnalyticsStatus = ServiceStatus.Failed; Debug.LogWarning("Unity Analytics: " + ex.Message, this); }
            }
#endif
#if ACS_UGS_AUTH && ACS_UGS_CORE
            if (enableAuthentication)
            {
                AuthenticationStatus = ServiceStatus.Initializing;
                try
                {
                    if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    AuthenticationStatus = ServiceStatus.Ready;
                }
                catch (Exception ex) { AuthenticationStatus = ServiceStatus.Failed; Debug.LogWarning("Unity Authentication: " + ex.Message, this); }
            }
#endif
#if ACS_UGS_REMOTE_CONFIG
            if (enableRemoteConfig && enableAuthentication && AuthenticationStatus == ServiceStatus.Ready)
            { RemoteConfig.Initialize(); RemoteConfigStatus = ServiceStatus.Ready; }
#endif
#if ACS_UGS_CLOUD_SAVE && ACS_UGS_AUTH && ACS_UGS_CORE
            if (enableCloudSave && enableAuthentication && AuthenticationStatus == ServiceStatus.Ready)
            {
                CloudSaveStatus = ServiceStatus.Initializing;
                CloudSave.Initialize(this);
                CloudSaveStatus = ServiceStatus.Ready;
            }
#endif
#if ACS_UGS_PLAYER_ACCOUNTS && ACS_UGS_AUTH && ACS_UGS_CORE
            if (enablePlayerAccounts && enableAuthentication && AuthenticationStatus == ServiceStatus.Ready)
            {
                PlayerAccountsStatus = ServiceStatus.Initializing;
                PlayerAccounts.Initialize(this);
                PlayerAccountsStatus = ServiceStatus.Ready;
            }
#endif
            await Task.CompletedTask;
        }
        internal void BeginAccountTransition()
        {
            accountTransition = new TaskCompletionSource<bool>();
            AuthenticationStatus = ServiceStatus.Initializing;
            if (enableRemoteConfig && RemoteConfigStatus == ServiceStatus.Ready) RemoteConfigStatus = ServiceStatus.WaitingForDependency;
            if (enableCloudSave && CloudSaveStatus == ServiceStatus.Ready) CloudSaveStatus = ServiceStatus.WaitingForDependency;
        }
        internal void EndAccountTransition(bool signedIn)
        {
            AuthenticationStatus = signedIn ? ServiceStatus.Ready : ServiceStatus.Failed;
            if (enableRemoteConfig && RemoteConfig.IsInitialized) RemoteConfigStatus = signedIn ? ServiceStatus.Ready : ServiceStatus.WaitingForDependency;
            if (enableCloudSave && CloudSave.IsInitialized) CloudSaveStatus = signedIn ? ServiceStatus.Ready : ServiceStatus.WaitingForDependency;
            var pending = accountTransition; accountTransition = null; pending?.TrySetResult(true);
        }
    }
}

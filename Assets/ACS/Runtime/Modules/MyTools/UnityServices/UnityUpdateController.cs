using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace ACS.UnityServices
{
    [RequireComponent(typeof(UnityServicesManager), typeof(UnityServicePanels))]
    public sealed class UnityUpdateController : MonoBehaviour
    {
        [SerializeField] private bool checkUpdates = true;
        [SerializeField] private bool enforceInEditor;
        [SerializeField] private bool editorUseIos;
        [SerializeField, Min(1)] private float startupTimeoutSeconds = 10f;
        private UnityServicesManager services;
        private UnityServicePanels panels;
        private bool started, startupComplete, checking, required, optional, consentOpen, privacyRequested;
        private bool offlineAtStartup, retryUsed, wasPaused;
        private UnityServicePause pause;
        private static bool normalShown;
        public bool IsUpdateBlocking => required || optional || checking;
        public bool StartupComplete => startupComplete;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { normalShown = false; }

        public static UnityUpdateController Ensure(UnityUpdateController configuredPrefab = null)
        {
            if (UnityServicesManager.Instance) return UnityServicesManager.Instance.GetComponent<UnityUpdateController>();
            var existing = FindFirstObjectByType<UnityServicesManager>();
            if (existing) return existing.GetComponent<UnityUpdateController>();
            if (configuredPrefab) return Instantiate(configuredPrefab);
            var prefab = Resources.Load<GameObject>("UnityServices");
            if (!prefab) { Debug.LogWarning("UnityServices resource prefab missing; skipping update check."); return null; }
            return Instantiate(prefab).GetComponent<UnityUpdateController>();
        }
        private void Awake()
        {
            services = GetComponent<UnityServicesManager>(); panels = GetComponent<UnityServicePanels>();
        }
        private void Start() { if (UnityServicesManager.Instance == services) BeginStartup(); }
        private void BeginStartup()
        {
            if (started) return; started = true;
            services.Analytics.PrivacyRequested += RequestPrivacy;
            StartCoroutine(Startup());
        }
        public IEnumerator WaitForStartup()
        {
            BeginStartup();
            while (!startupComplete || IsUpdateBlocking) yield return null;
        }
        private bool ShouldCheck
        {
            get
            {
                if (!checkUpdates || !services.RemoteConfigEnabled || !services.AuthenticationEnabled) return false;
#if UNITY_EDITOR
                return enforceInEditor;
#elif UNITY_ANDROID || UNITY_IOS
                return true;
#else
                return false;
#endif
            }
        }
        private string Prefix
        {
            get
            {
#if UNITY_EDITOR
                return editorUseIos ? "ios_" : "android_";
#elif UNITY_IOS
                return "ios_";
#else
                return "android_";
#endif
            }
        }
        private IEnumerator Startup()
        {
            offlineAtStartup = Application.internetReachability == NetworkReachability.NotReachable;
            if (ShouldCheck && !offlineAtStartup) yield return CheckPolicy();
            else _ = services.InitializeAsync();
            startupComplete = true;
            // Allow other startup owners to create their existing consent handlers first.
            yield return null;
            while (true)
            {
                if (offlineAtStartup && !retryUsed && Application.internetReachability != NetworkReachability.NotReachable)
                {
                    retryUsed = true;
                    if (ShouldCheck) yield return CheckPolicy();
                }
                if (!IsUpdateBlocking && !consentOpen && services.AnalyticsStatus == ServiceStatus.Ready && ExistingConsentFinished() &&
                    (privacyRequested || services.Analytics.Consent == AnalyticsConsent.Unknown))
                {
                    privacyRequested = false; consentOpen = true;
                    panels.ShowConsent(ChooseConsent, services.Analytics.PrivacyUrl);
                }
                yield return null;
            }
        }
        private IEnumerator CheckPolicy()
        {
            if (checking) yield break;
            // Preserve a mandatory panel across timeouts, invalid responses, and store returns.
            checking = true;
            if (!required && !consentOpen) { panels.ShowChecking(); PauseGame(); }
            float until = Time.realtimeSinceStartup + Mathf.Max(1f, startupTimeoutSeconds);
            Task init = services.InitializeAsync();
            while (!init.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
            RemoteFetchResult result = null;
            if (init.IsCompleted && !init.IsFaulted && services.RemoteConfigStatus == ServiceStatus.Ready && Time.realtimeSinceStartup < until)
            {
                var fetch = services.RemoteConfig.FetchAsync(Prefix);
                while (!fetch.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
                if (fetch.IsCompleted && !fetch.IsFaulted && Time.realtimeSinceStartup < until) result = fetch.Result;
            }
            checking = false;
            if (Time.realtimeSinceStartup >= until)
                Debug.LogWarning(required ? "Unity update recheck timed out; keeping the required update panel." :
                    "Unity update check timed out; allowing play.", this);
            ApplyResult(result);
        }
        private void ApplyResult(RemoteFetchResult result)
        {
            if (result != null && result.Status == RemoteFetchStatus.Fresh && result.UpdatePolicy != null)
            {
                var policy = result.UpdatePolicy;
                var requirement = policy.Evaluate(Application.version);
                required = requirement == UpdateRequirement.Required;
                if (required || (requirement == UpdateRequirement.Optional && !normalShown))
                {
                    if (consentOpen) { consentOpen = false; privacyRequested = true; }
                    optional = !required;
                    if (optional) normalShown = true;
                    PauseGame();
                    panels.ShowUpdate(policy, required, () => Application.OpenURL(policy.StoreUrl), CloseOptional);
                    return;
                }
                optional = false;
            }
            if (!required && !optional && !consentOpen) { panels.Hide(); ResumeGame(); }
        }
        private void CloseOptional() { if (required) return; optional = false; panels.Hide(); ResumeGame(); }
        private void PauseGame()
        {
            if (pause == null) pause = UnityServicePause.Acquire();
        }
        private void ResumeGame()
        {
            pause?.Dispose(); pause = null;
        }
        private void ChooseConsent(bool accepted)
        {
            try { services.Analytics.SetAnalyticsConsent(accepted); }
            catch (Exception ex) { Debug.LogWarning("Unity Analytics consent could not be saved: " + ex.Message, this); }
            finally { consentOpen = false; panels.Hide(); }
        }
        private void RequestPrivacy() { privacyRequested = true; }
        private static bool ExistingConsentFinished()
        {
#if UNITY_IOS && ACS_ADMOB_UMP && ACS_APPLE_ATT && !UNITY_EDITOR
            var iosConsent = FindFirstObjectByType<GDPR_IOS>();
            if (iosConsent && !iosConsent.IsConsentFlowComplete) return false;
#endif
#if UNITY_IOS && ACS_APPLE_ATT && !UNITY_EDITOR
            if (FindFirstObjectByType<ATT_PopupHandler>() && !ATT_PopupHandler.IsTrackingAuthorizationResolved) return false;
#endif
#if UNITY_ANDROID && ACS_APPLOVIN && !UNITY_EDITOR
            if (AndroidConsentGate.IsConsentPending) return false;
#elif UNITY_ANDROID && ACS_ADMOB_UMP && !UNITY_EDITOR
            if (GDPR_Android.Instance && !GDPR_Android.IsConsentFlowComplete) return false;
#endif
            return true;
        }
        private void OnApplicationPause(bool paused)
        {
            if (paused) wasPaused = true;
            else if (wasPaused) { wasPaused = false; RecheckMandatory(); }
        }
        private void OnApplicationFocus(bool focused) { if (focused) RecheckMandatory(); }
        private void RecheckMandatory() { if (started && required && !checking) StartCoroutine(CheckPolicy()); }
        private void OnDestroy()
        {
            if (services && services.Analytics) services.Analytics.PrivacyRequested -= RequestPrivacy;
            ResumeGame();
        }
    }
}

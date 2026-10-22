using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using ACS.UnityServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ACS.Editor
{
    // Batch entry points operate only on a disposable validation project.
    [InitializeOnLoad]
    public static class UnityServicesValidation
    {
        private static int assertions;
        static UnityServicesValidation() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }
        private static void Check(bool value, string message)
        {
            if (!value) throw new Exception("Unity services validation: " + message);
            assertions++;
        }
        private static void Set(object value, string field, object next)
        { value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, next); }
        public static void ValidateSourceAndAssets()
        {
#if ACS_UGS_CORE
            Debug.Log("ACS_UGS_CORE_COMPILED");
#endif
#if ACS_UGS_AUTH
            Debug.Log("ACS_UGS_AUTH_COMPILED");
#endif
#if ACS_UGS_REMOTE_CONFIG
            Debug.Log("ACS_UGS_REMOTE_CONFIG_COMPILED");
#endif
#if ACS_UGS_PLAYER_ACCOUNTS
            Debug.Log("ACS_UGS_PLAYER_ACCOUNTS_COMPILED");
#endif
#if ACS_UGS_CLOUD_SAVE
            Debug.Log("ACS_UGS_CLOUD_SAVE_COMPILED");
#endif
#if ACS_UGS_ANALYTICS
            Debug.Log("ACS_UGS_ANALYTICS_COMPILED");
#endif
            foreach (var severity in new[] { "normal", "critical" })
            {
                UpdatePolicy p;
                Check(UpdatePolicy.TryCreate("1.2", "1.10", severity, "https://example.com/game", out p), "valid policy");
                Check(p.Evaluate("1.1") == UpdateRequirement.Required, "minimum forces " + severity);
                Check(p.Evaluate("1.2.0.0") == (severity == "critical" ? UpdateRequirement.Required : UpdateRequirement.Optional), "minimum boundary " + severity);
                Check(p.Evaluate("1.9") == (severity == "critical" ? UpdateRequirement.Required : UpdateRequirement.Optional), "numeric ordering " + severity);
                Check(p.Evaluate("1.10.0") == UpdateRequirement.None, "latest boundary");
                Check(p.Evaluate("2") == UpdateRequirement.None, "newer release");
            }
            foreach (var version in new[] { "", "1..2", "1.0-beta", "-1", "1.2.3.4.5", " 1", "2147483648" })
            { Version parsed; Check(!UpdatePolicy.TryVersion(version, out parsed), "reject malformed version " + version); }
            UpdatePolicy invalid;
            Check(!UpdatePolicy.TryCreate("2", "1", "normal", "https://example.com", out invalid), "reversed thresholds");
            Check(!UpdatePolicy.TryCreate("1", "2", "unknown", "https://example.com", out invalid), "unknown severity");
            Check(!UpdatePolicy.TryCreate("1", "2", "normal", "", out invalid), "missing store");
            Check(!UpdatePolicy.TryCreate("1", "2", "normal", "javascript:alert(1)", out invalid), "invalid store scheme");
            var prefab = Resources.Load<GameObject>("UnityServices");
            Check(prefab && prefab.GetComponent<UnityServicesManager>(), "manager resource prefab");
            Check(prefab.GetComponent<UnityRemoteConfigManager>(), "separate remote component");
            Check(prefab.GetComponent<UnityAnalyticsManager>(), "separate analytics component");
            Check(prefab.GetComponent<UnityCloudSaveManager>(), "separate cloud save component");
            Check(!prefab.GetComponent<UnityServicesManager>().CloudSaveEnabled, "cloud save is opt in");
            Check(prefab.GetComponent<UnityPlayerAccountsManager>(), "separate Player Accounts component");
            Check(prefab.GetComponent<UnityPlayerAccountsUI>(), "Player Accounts UI component");
            Check(!prefab.GetComponent<UnityServicesManager>().PlayerAccountsEnabled, "Player Accounts is opt in");
            Check(prefab.GetComponent<UnityUpdateController>(), "update component");
            var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ACS/Runtime/Prefabs/UIManager.prefab");
            var uiSerialized = new SerializedObject(ui.GetComponent<UIManager>());
            Check(uiSerialized.FindProperty("unityServicesPrefab").objectReferenceValue, "UIManager prefab startup reference");
            EditorSceneManager.OpenScene("Assets/ACS/Runtime/Modules/MyTools/AdsManager/Demo/Demo.unity", OpenSceneMode.Single);
            var sceneManagers = UnityEngine.Object.FindObjectsByType<UnityServicesManager>(FindObjectsSortMode.None);
            Check(sceneManagers.Length == 1, "demo has one services manager instance");
            Check(sceneManagers[0].GetComponent<UnityUpdateController>(), "demo update controller reference");
            Debug.Log("ACS_UGS_EDITOR_CHECKS_PASSED assertions=" + assertions);
        }
        public static void CapturePanels()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
            var host = new GameObject("Panel preview");
            var panels = host.AddComponent<UnityServicePanels>(); panels.Build();
            var cameraObject = new GameObject("Preview camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            var target = new RenderTexture(1080, 1920, 24); camera.targetTexture = target;
            var canvas = host.GetComponentInChildren<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1;
            UpdatePolicy policy; UpdatePolicy.TryCreate("1", "2", "normal", "https://example.com", out policy);
            panels.ShowUpdate(policy, true, () => { }, () => { });
            Capture(camera, target, "/tmp/acs-update-critical.png");
            panels.ShowUpdate(policy, false, () => { }, () => { });
            Capture(camera, target, "/tmp/acs-update-normal.png");
            panels.ShowConsent(value => { }, "https://example.com/privacy");
            Capture(camera, target, "/tmp/acs-analytics-consent.png");
            panels.ShowDialog(panels, "Your account", "You are playing as a guest. Link an account to access this player's cloud saves on your other devices.",
                "Link account", () => { }, "Sign in", () => { }, "Close", () => { });
            Capture(camera, target, "/tmp/acs-player-accounts-panel.png");
            Debug.Log("ACS_UGS_PANELS_CAPTURED");
        }
        private static void Capture(Camera camera, RenderTexture target, string path)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(texture);
        }
        public static void RunPlayMode()
        {
            ValidateSourceAndAssets();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Unity Services Validation"); go.AddComponent<UnityServicesValidationRunner>();
            SessionState.SetBool("ACS.UGS.RunPlayValidation", true);
            EditorApplication.isPlaying = true;
        }
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ACS.UGS.RunPlayValidation", false))
            {
                SessionState.SetBool("ACS.UGS.RunPlayValidation", false);
                UnityEngine.Object.FindFirstObjectByType<UnityServicesValidationRunner>().Run(Exercise());
            }
        }
        public static IEnumerator Exercise()
        {
            assertions = 0;
            var go = new GameObject("Disabled services"); go.SetActive(false);
            var manager = go.AddComponent<UnityServicesManager>();
            Set(manager, "enableAuthentication", false); Set(manager, "enableRemoteConfig", false);
            Set(manager, "enableAnalytics", false); Set(manager, "initializeOnStart", false);
            var controller = go.AddComponent<UnityUpdateController>(); controller.enabled = false;
            go.SetActive(true);
            Check(manager.AuthenticationStatus == ServiceStatus.Disabled, "unchecked Auth stays disabled");
            Check(manager.RemoteConfigStatus == ServiceStatus.Disabled, "unchecked Remote stays disabled");
            Check(manager.AnalyticsStatus == ServiceStatus.Disabled, "unchecked Analytics stays disabled");
            var init = manager.InitializeAsync();
            Check(ReferenceEquals(init, manager.InitializeAsync()), "initialization task reused");
            while (!init.IsCompleted) yield return null;
            Check(!manager.RemoteConfig.IsInitialized, "disabled remote not initialized");
            Check(manager.CloudSaveStatus == ServiceStatus.Disabled, "unchecked Cloud Save stays disabled");
            Check(!manager.CloudSave.IsInitialized && !manager.CloudSave.IsReady, "disabled Cloud Save not initialized");
            var blockedSave = manager.CloudSave.SaveAsync("test-key", 10);
            Check(blockedSave.IsFaulted && blockedSave.Exception.GetBaseException() is InvalidOperationException,
                "disabled cloud writes fail explicitly without contacting the server");
            var blockedLoad = manager.CloudSave.LoadAsync("test-key", 99);
            Check(blockedLoad.IsFaulted && blockedLoad.Exception.GetBaseException() is InvalidOperationException,
                "unavailable cloud load does not return fallback progress");
            var invalidLoad = manager.CloudSave.LoadAsync<int>(" ");
            Check(invalidLoad.IsFaulted && invalidLoad.Exception.GetBaseException() is ArgumentException, "empty cloud keys rejected");
            var nullSave = manager.CloudSave.SaveAsync((System.Collections.Generic.IDictionary<string, object>)null);
            Check(nullSave.IsFaulted && nullSave.Exception.GetBaseException() is ArgumentNullException, "null cloud payload rejected");
            var emptySave = manager.CloudSave.SaveAsync(new System.Collections.Generic.Dictionary<string, object>());
            Check(emptySave.IsFaulted && emptySave.Exception.GetBaseException() is ArgumentException, "empty cloud payload rejected");
            Check(!manager.Analytics.IsCollecting, "disabled analytics dormant");
            Check(manager.RemoteConfig.GetValue("missing", 123L) == 123L, "long fallback");
            Check(manager.RemoteConfig.GetValue("missing", 1.25d) == 1.25d, "double fallback");
            Check(!manager.Analytics.RecordEvent("disabled"), "events rejected without collection");
            var panels = go.GetComponent<UnityServicePanels>(); panels.Build();
            UpdatePolicy p; UpdatePolicy.TryCreate("1", "999", "normal", "https://example.com", out p);
            int closed = 0, updates = 0;
            panels.ShowUpdate(p, false, () => updates++, () => closed++);
            var secondary = (Button)typeof(UnityServicePanels).GetField("secondary", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panels);
            var primary = (Button)typeof(UnityServicePanels).GetField("primary", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panels);
            Check(panels.Visible && panels.NormalVisible, "normal panel shown");
            Check(!secondary.interactable, "close initially locked");
            secondary.onClick.Invoke(); Check(closed == 0, "early dismissal rejected");
            primary.onClick.Invoke(); Check(updates == 1, "update available immediately");
            float before = Time.timeScale; Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(5.1f);
            Check(secondary.interactable, "close unlocks at zero timescale");
            secondary.onClick.Invoke(); Check(closed == 1, "close enabled after five seconds");
            Time.timeScale = before;
            panels.ShowUpdate(p, true, () => updates++, () => closed++);
            Check(!secondary.gameObject.activeSelf && panels.Visible, "critical has no close");
            yield return new WaitForSecondsRealtime(.1f);
            Check(panels.Visible, "critical does not auto dismiss");
            var apply = typeof(UnityUpdateController).GetMethod("ApplyResult", BindingFlags.Instance | BindingFlags.NonPublic);
            UpdatePolicy critical; UpdatePolicy.TryCreate("1", "999", "critical", "https://example.com", out critical);
            apply.Invoke(controller, new object[] { new RemoteFetchResult(RemoteFetchStatus.Fresh, critical) });
            Check(controller.IsUpdateBlocking && Time.timeScale == 0, "fresh critical blocks gameplay");
            apply.Invoke(controller, new object[] { new RemoteFetchResult(RemoteFetchStatus.Failed) });
            Check(controller.IsUpdateBlocking && panels.Visible, "failed recheck preserves critical");
            apply.Invoke(controller, new object[] { new RemoteFetchResult(RemoteFetchStatus.Cached, p) });
            Check(controller.IsUpdateBlocking, "cached response cannot release critical");
            UpdatePolicy allowed; UpdatePolicy.TryCreate("0", "0", "normal", "https://example.com", out allowed);
            apply.Invoke(controller, new object[] { new RemoteFetchResult(RemoteFetchStatus.Fresh, allowed) });
            Check(!controller.IsUpdateBlocking && Time.timeScale == before, "fresh allowed releases critical");
            apply.Invoke(controller, new object[] { new RemoteFetchResult(RemoteFetchStatus.Fresh, p) });
            Check(panels.NormalVisible, "optional prompt first session appearance");
            typeof(UnityUpdateController).GetMethod("CloseOptional", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            apply.Invoke(controller, new object[] { new RemoteFetchResult(RemoteFetchStatus.Fresh, p) });
            Check(!panels.Visible, "normal prompt once per session");
            // A startup deadline expires even while initialization is still in progress.
            Set(controller, "startupTimeoutSeconds", 1f);
            var pendingInit = new TaskCompletionSource<bool>();
            Set(manager, "initialization", pendingInit.Task);
            var checkPolicy = typeof(UnityUpdateController).GetMethod("CheckPolicy", BindingFlags.Instance | BindingFlags.NonPublic);
            yield return (IEnumerator)checkPolicy.Invoke(controller, null);
            Check(!controller.IsUpdateBlocking && !panels.Visible && Time.timeScale == before, "initialization timeout releases startup");
            pendingInit.SetResult(true);
            yield return null;
            Check(!panels.Visible, "late initialization never opens update panel");
            var pendingFetch = new TaskCompletionSource<RemoteFetchResult>();
            Set(manager, "initialization", Task.CompletedTask);
            Set(manager, "<RemoteConfigStatus>k__BackingField", ServiceStatus.Ready);
            Set(manager.RemoteConfig, "fetching", pendingFetch.Task);
            yield return (IEnumerator)checkPolicy.Invoke(controller, null);
            Check(!controller.IsUpdateBlocking && !panels.Visible, "fetch timeout allows play");
            pendingFetch.SetResult(new RemoteFetchResult(RemoteFetchStatus.Fresh, critical));
            yield return null;
            Check(!panels.Visible && !controller.IsUpdateBlocking, "late mandatory fetch ignored after deadline");
            var backgroundCanvas = new GameObject("Background UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            var group = backgroundCanvas.GetComponent<CanvasGroup>();
            panels.ShowUpdate(p, true, () => updates++, () => closed++);
            Check(!group.interactable, "underlying canvas input blocked");
            panels.Hide(); Check(group.interactable, "underlying input restored");
            UnityEngine.Object.Destroy(backgroundCanvas);
            panels.ShowConsent(value => { }, "https://example.com/privacy");
            Check(panels.Visible && !panels.NormalVisible && secondary.interactable, "consent choices available without normal timer");
            panels.Hide();
            var analytics = manager.Analytics;
            int saved = PlayerPrefs.GetInt("ACS.UnityAnalytics.Consent.v1", -1);
            analytics.SetAnalyticsConsent(false);
            Check(analytics.Consent == AnalyticsConsent.Declined && !analytics.IsCollecting, "decline persists without collection");
            analytics.SetAnalyticsConsent(true);
            Check(analytics.Consent == AnalyticsConsent.Accepted && !analytics.IsCollecting, "uninitialized analytics never collects");
            if (saved < 0) PlayerPrefs.DeleteKey("ACS.UnityAnalytics.Consent.v1"); else PlayerPrefs.SetInt("ACS.UnityAnalytics.Consent.v1", saved);
            PlayerPrefs.Save();
            UnityEngine.Object.Destroy(go); yield return null;
            Check(UnityServicesManager.Instance == null, "singleton cleared on destruction");
            var dependency = new GameObject("Dependency validation"); dependency.SetActive(false);
            var dependencyManager = dependency.AddComponent<UnityServicesManager>();
            Set(dependencyManager, "enableAuthentication", false); Set(dependencyManager, "initializeOnStart", false);
            Set(dependencyManager, "enableCloudSave", true);
            dependency.SetActive(true);
            var dependencyTask = dependencyManager.InitializeAsync(); while (!dependencyTask.IsCompleted) yield return null;
            Check(dependencyManager.AuthenticationStatus == ServiceStatus.Disabled, "Auth remains explicitly disabled");
#if ACS_UGS_REMOTE_CONFIG
            Check(dependencyManager.RemoteConfigStatus == ServiceStatus.WaitingForDependency, "Remote waits for explicit Auth selection");
#else
            Check(dependencyManager.RemoteConfigStatus == ServiceStatus.PackageMissing, "missing Remote package status");
#endif
            Check(!dependencyManager.RemoteConfig.IsInitialized, "dependency cannot silently start Auth or Remote");
#if ACS_UGS_CLOUD_SAVE
            Check(dependencyManager.CloudSaveStatus == ServiceStatus.WaitingForDependency, "Cloud Save requires explicit Authentication selection");
#else
            Check(dependencyManager.CloudSaveStatus == ServiceStatus.PackageMissing, "missing Cloud Save SDK reported");
#endif
            Check(!dependencyManager.CloudSave.IsInitialized && !dependencyManager.CloudSave.IsReady, "Cloud Save cannot silently start Authentication");
            UnityEngine.Object.Destroy(dependency); yield return null;
            var progressChecks = UnityProgressSyncValidation.Exercise();
            while (progressChecks.MoveNext()) yield return progressChecks.Current;
            var accountChecks = UnityPlayerAccountsValidation.Exercise();
            while (accountChecks.MoveNext()) yield return accountChecks.Current;
            Debug.Log("ACS_UGS_PLAY_CHECKS_PASSED assertions=" + assertions);
        }
    }
}

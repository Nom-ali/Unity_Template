using ACS.UnityServices;
using UnityEditor;
using UnityEngine;

namespace ACS.Editor
{
    [CustomEditor(typeof(UnityServicesManager))]
    public sealed class UnityServicesManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var manager = (UnityServicesManager)target;
            if (manager.RemoteConfigEnabled && !manager.AuthenticationEnabled)
                EditorGUILayout.HelpBox("Remote Config is skipped until Authentication is also enabled.", MessageType.Warning);
            if (manager.PlayerAccountsEnabled && !manager.AuthenticationEnabled)
                EditorGUILayout.HelpBox("Player Accounts is skipped until Authentication is enabled.", MessageType.Warning);
#if !ACS_UGS_PLAYER_ACCOUNTS
            if (manager.PlayerAccountsEnabled) EditorGUILayout.HelpBox("Player Accounts SDK is missing. Install Authentication 3.8.1 or later through ACS Template Setup.", MessageType.Warning);
#endif
            if (manager.PlayerAccountsEnabled)
            {
#if ACS_UGS_PLAYER_ACCOUNTS
                var accountSettings = Resources.Load<ScriptableObject>("UnityPlayerAccountSettings");
                var clientId = accountSettings ? new SerializedObject(accountSettings).FindProperty("clientId") : null;
                if (clientId == null || string.IsNullOrWhiteSpace(clientId.stringValue))
                    EditorGUILayout.HelpBox("Configure the Unity Player Accounts provider and Client ID before browser sign-in.", MessageType.Warning);
#endif
                if (GUILayout.Button("Configure Unity Player Accounts"))
                    SettingsService.OpenProjectSettings("Project/Services/Unity Player Accounts");
            }
            if (manager.CloudSaveEnabled && !manager.AuthenticationEnabled)
                EditorGUILayout.HelpBox("Cloud Save is skipped until Authentication is also enabled.", MessageType.Warning);
#if !ACS_UGS_CLOUD_SAVE
            if (manager.CloudSaveEnabled) EditorGUILayout.HelpBox("Cloud Save package is missing. Install it through ACS Template Setup.", MessageType.Warning);
#endif
#if !ACS_UGS_AUTH
            if (manager.AuthenticationEnabled) EditorGUILayout.HelpBox("Authentication package is missing. This service will not start.", MessageType.Warning);
#endif
#if !ACS_UGS_REMOTE_CONFIG
            if (manager.RemoteConfigEnabled) EditorGUILayout.HelpBox("Remote Config package is missing. This service will not start.", MessageType.Warning);
#endif
#if !ACS_UGS_ANALYTICS
            if (manager.AnalyticsEnabled) EditorGUILayout.HelpBox("Analytics package is missing. Install Unity Analytics through ACS Template Setup.", MessageType.Warning);
#endif
            if (!Application.isPlaying) return;
            EditorGUILayout.LabelField("Authentication", manager.AuthenticationStatus.ToString());
            EditorGUILayout.LabelField("Remote Config", manager.RemoteConfigStatus.ToString());
            EditorGUILayout.LabelField("Player Accounts", manager.PlayerAccountsStatus.ToString());
            EditorGUILayout.LabelField("Player ID", manager.CurrentPlayerId);
            EditorGUILayout.Toggle("Account linked", manager.PlayerAccounts.IsLinked);
            EditorGUILayout.Toggle("Account operation busy", manager.PlayerAccounts.IsBusy);
            EditorGUILayout.Toggle("Cloud data restore required", manager.CloudSave.RequiresAccountRestore);
            EditorGUILayout.Toggle("Progress sync busy", manager.ProgressSync.IsBusy);
            EditorGUILayout.Toggle("Progress restored", manager.ProgressSync.IsRestored);
            if (!string.IsNullOrEmpty(manager.ProgressSync.LastError))
                EditorGUILayout.HelpBox(manager.ProgressSync.LastError, MessageType.Warning);
            if (!string.IsNullOrEmpty(manager.PlayerAccounts.LastDiagnostic))
                EditorGUILayout.HelpBox(manager.PlayerAccounts.LastDiagnostic, MessageType.Info);
            if (manager.PlayerAccountsStatus == ServiceStatus.Ready && GUILayout.Button("Account options"))
                manager.GetComponent<UnityPlayerAccountsUI>()?.Show();
            EditorGUILayout.LabelField("Cloud Save", manager.CloudSaveStatus.ToString());
            EditorGUILayout.LabelField("Analytics", manager.AnalyticsStatus.ToString());
            EditorGUILayout.LabelField("Analytics consent", manager.Analytics.Consent.ToString());
            EditorGUILayout.Toggle("Analytics collecting", manager.Analytics.IsCollecting);
            if (manager.AnalyticsStatus == ServiceStatus.Ready && GUILayout.Button("Analytics privacy")) manager.Analytics.OpenPrivacyChoice();
            Repaint();
        }
    }
}

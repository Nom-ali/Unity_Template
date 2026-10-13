#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY
using UnityEditor;
using UnityEngine;
#if ACS_ADMOB
using GoogleMobileAds.Editor;
#endif
#if ACS_APPLOVIN
using AppLovinMax.Scripts.IntegrationManager.Editor;
#endif

[CustomEditor(typeof(AdSettings))]
public class AdSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("SDK Settings Configuration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("These buttons copy trimmed configured values into the installed SDK settings. Empty source IDs never overwrite existing SDK IDs.", MessageType.Info);

#if ACS_ADMOB
        if (GUILayout.Button("Configure Google Mobile Ads Settings"))
            ConfigureGoogleMobileAdsSettings((AdSettings)target);
#endif
#if ACS_APPLOVIN
        if (GUILayout.Button("Configure AppLovin Integration Manager"))
            ConfigureAppLovinIntegrationManager((AdSettings)target);
#endif
    }

#if ACS_ADMOB
    private static void ConfigureGoogleMobileAdsSettings(AdSettings settings)
    {
        GoogleMobileAdsSettings googleSettings = GoogleMobileAdsSettings.LoadInstance();
        Undo.RecordObject(googleSettings, "Configure Google Mobile Ads Settings");

        bool copiedValue = false;
        string androidAppId = settings.GetAdmobAppId(Platform.Android)?.Trim();
        if (!string.IsNullOrWhiteSpace(androidAppId))
        {
            googleSettings.GoogleMobileAdsAndroidAppId = androidAppId;
            copiedValue = true;
        }

        string iosAppId = settings.GetAdmobAppId(Platform.IOS)?.Trim();
        if (!string.IsNullOrWhiteSpace(iosAppId))
        {
            googleSettings.GoogleMobileAdsIOSAppId = iosAppId;
            copiedValue = true;
        }

        string trackingDescription = settings.TrackingUsageDescription?.Trim();
        if (!string.IsNullOrWhiteSpace(trackingDescription))
        {
            googleSettings.UserTrackingUsageDescription = trackingDescription;
            copiedValue = true;
        }

        EditorUtility.SetDirty(googleSettings);
        AssetDatabase.SaveAssets();
        Debug.Log(copiedValue
            ? "Google Mobile Ads Settings configured from AdSettings."
            : "Google Mobile Ads Settings was not changed because no App ID or tracking description is configured in AdSettings.");
    }
#endif

#if ACS_APPLOVIN
    private static void ConfigureAppLovinIntegrationManager(AdSettings settings)
    {
        AppLovinSettings appLovinSettings = AppLovinSettings.Instance;
        AppLovinInternalSettings internalSettings = AppLovinInternalSettings.Instance;
        Undo.RecordObject(appLovinSettings, "Configure AppLovin Integration Manager");
        Undo.RecordObject(internalSettings, "Configure AppLovin Privacy Settings");

#if ACS_ADMOB
        string androidAppId = settings.GetAdmobAppId(Platform.Android)?.Trim();
        if (!string.IsNullOrWhiteSpace(androidAppId))
            appLovinSettings.AdMobAndroidAppId = androidAppId;

        string iosAppId = settings.GetAdmobAppId(Platform.IOS)?.Trim();
        if (!string.IsNullOrWhiteSpace(iosAppId))
            appLovinSettings.AdMobIosAppId = iosAppId;
#endif

        string sdkKey = settings.MaxSDKKey?.Trim();
        if (!string.IsNullOrWhiteSpace(sdkKey))
            appLovinSettings.SdkKey = sdkKey;

        appLovinSettings.QualityServiceEnabled = true;
        internalSettings.ConsentFlowEnabled = true;

        string privacyPolicyUrl = GetPrivacyPolicyUrlForActiveBuildTarget();
        if (!string.IsNullOrWhiteSpace(privacyPolicyUrl))
            internalSettings.ConsentFlowPrivacyPolicyUrl = privacyPolicyUrl;

        string trackingDescription = settings.TrackingUsageDescription?.Trim();
        if (!string.IsNullOrWhiteSpace(trackingDescription))
        {
            internalSettings.OverrideDefaultUserTrackingUsageDescriptions = true;
            internalSettings.UserTrackingUsageDescriptionEn = trackingDescription;
            internalSettings.UserTrackingUsageLocalizationEnabled = false;
        }

        appLovinSettings.SaveAsync();
        internalSettings.Save();
        AssetDatabase.SaveAssets();
        Debug.Log("AppLovin Integration Manager configured from AdSettings.");
    }

    private static string GetPrivacyPolicyUrlForActiveBuildTarget()
    {
        return EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS
            ? SharedVariables.IOSPrivacyPolicyLink?.Trim()
            : SharedVariables.AndroidPrivacyPolicyLink?.Trim();
    }
#endif
}
#endif

#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY
using ACS.AdsManager;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(AdsManager))]
public class AdsManagerEditor : Editor
{
    private SerializedProperty adSDK;
    private SerializedProperty platform;
    private SerializedProperty showDebugLogs;
    private SerializedProperty setFrames;
    private SerializedProperty frames;
    private SerializedProperty adSettings;
    private SerializedProperty remoteKeys;
    private SerializedProperty defaultEnableAds;
    private SerializedProperty defaultEnableTestAds;
    private SerializedProperty priorityMode;
    private SerializedProperty universalPriorityOrder;
    private SerializedProperty bannerPriorityOrder;
    private SerializedProperty bigBannerPriorityOrder;
    private SerializedProperty interstitialPriorityOrder;
    private SerializedProperty rewardedPriorityOrder;
    private SerializedProperty appOpenPriorityOrder;
    private SerializedProperty showLoadingPanel;
    private SerializedProperty neverSleepMode;
    private SerializedProperty showBannerOnLoad;
    private SerializedProperty showAppOpenAdOnLoad;
    private SerializedProperty showAppOpenInBackground;
    private SerializedProperty showAppOpenAfterInterAds;
    private SerializedProperty dontDestroyOnLoad;

    private void OnEnable()
    {
        adSDK = serializedObject.FindProperty("adSDK");
        platform = serializedObject.FindProperty("m_Platform");
        showDebugLogs = serializedObject.FindProperty("ShowDebugLogs");
        setFrames = serializedObject.FindProperty("SetFrames");
        frames = serializedObject.FindProperty("Frames");
        adSettings = serializedObject.FindProperty("adSettings");
        remoteKeys = serializedObject.FindProperty("RemoteKeys");
        defaultEnableAds = serializedObject.FindProperty("DEFAULT_ENABLE_ADS");
        defaultEnableTestAds = serializedObject.FindProperty("DEFAULT_ENABLE_TEST_ADS");
        priorityMode = serializedObject.FindProperty("priorityMode");
        universalPriorityOrder = serializedObject.FindProperty("AdPriorityOrder");
        bannerPriorityOrder = serializedObject.FindProperty("bannerPriorityOrder");
        bigBannerPriorityOrder = serializedObject.FindProperty("bigBannerPriorityOrder");
        interstitialPriorityOrder = serializedObject.FindProperty("interstitialPriorityOrder");
        rewardedPriorityOrder = serializedObject.FindProperty("rewardedPriorityOrder");
        appOpenPriorityOrder = serializedObject.FindProperty("appOpenPriorityOrder");
        showLoadingPanel = serializedObject.FindProperty("ShowLoadingPanel");
        neverSleepMode = serializedObject.FindProperty("NeverSleepMode");
        showBannerOnLoad = serializedObject.FindProperty("ShowBannerOnLoad");
        showAppOpenAdOnLoad = serializedObject.FindProperty("ShowAppOpenAdOnLoad");
        showAppOpenInBackground = serializedObject.FindProperty("ShowAppOpenInBackground");
        showAppOpenAfterInterAds = serializedObject.FindProperty("ShowAppOpenAfterInterAds");
        dontDestroyOnLoad = serializedObject.FindProperty("dontDestroyOnLoad");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSDKSection();
        DrawConfigurationSection();
        DrawPrioritySection();
        DrawBehaviourSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSDKSection()
    {
        EditorGUILayout.PropertyField(dontDestroyOnLoad, new GUIContent("Persist Between Scenes", "Keeps this AdsManager instance when a new scene loads."));
        EditorGUILayout.Space(2);
        DrawSectionHeader("SDK Selection");

        DrawSDKSelection();

        EditorGUILayout.PropertyField(platform, new GUIContent("Target Platform"));
        if (GetSelectedSDKs() == AdSDK.None)
        {
            EditorGUILayout.HelpBox(
                "Select at least one SDK before showing ads.",
                MessageType.Warning
            );
        }
    }

    private void DrawConfigurationSection()
    {
        DrawSectionHeader("Configuration");
        EditorGUILayout.PropertyField(adSettings, new GUIContent("Ad Settings"));
        EditorGUILayout.PropertyField(remoteKeys, new GUIContent("Use Remote Config Keys", "Uses Firebase Remote Config values for ad enablement after Firebase is ready."));

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Remote Config Defaults", LabelStyles.GetLabelStyle());
        EditorGUILayout.PropertyField(defaultEnableAds, new GUIContent("Enable Ads by Default"));
        EditorGUILayout.PropertyField(defaultEnableTestAds, new GUIContent("Enable Test Ads by Default"));

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Debug and Performance", LabelStyles.GetLabelStyle());
        EditorGUILayout.PropertyField(showDebugLogs, new GUIContent("Show Debug Logs"));
        EditorGUILayout.PropertyField(setFrames, new GUIContent("Set Frame Rate"));
        if (setFrames.boolValue)
        {
            EditorGUILayout.PropertyField(frames, new GUIContent("Target Frame Rate"));
        }
    }

    private void DrawSDKSelection()
    {
        int selectedMask = adSDK.enumValueFlag;
        Rect controlRect = EditorGUILayout.GetControlRect();
        controlRect = EditorGUI.PrefixLabel(controlRect, new GUIContent("Enabled SDKs"));

        if (!EditorGUI.DropdownButton(controlRect, new GUIContent(GetSDKSelectionLabel(selectedMask)), FocusType.Keyboard, EditorStyles.popup))
            return;

        GenericMenu menu = new();
        int allSDKsMask = (int)(AdSDK.Admob | AdSDK.Applovin | AdSDK.UnityAds);
        menu.AddItem(new GUIContent("None"), selectedMask == 0, () => PersistSDKSelection(0));
        menu.AddItem(new GUIContent("Everything"), selectedMask == allSDKsMask, () => PersistSDKSelection(allSDKsMask));
        menu.AddSeparator(string.Empty);
        AddSDKMenuItem(menu, "AdMob", AdSDK.Admob, selectedMask);
        AddSDKMenuItem(menu, "AppLovin", AdSDK.Applovin, selectedMask);
        AddSDKMenuItem(menu, "Unity Ads", AdSDK.UnityAds, selectedMask);
        menu.DropDown(controlRect);
    }

    private void AddSDKMenuItem(GenericMenu menu, string label, AdSDK sdk, int selectedMask)
    {
        bool isSelected = (selectedMask & (int)sdk) != 0;
        menu.AddItem(new GUIContent(label), isSelected, () =>
        {
            int updatedMask = isSelected
                ? selectedMask & ~(int)sdk
                : selectedMask | (int)sdk;
            PersistSDKSelection(updatedMask);
        });
    }

    private void PersistSDKSelection(int updatedMask)
    {
        serializedObject.Update();
        Undo.RecordObjects(targets, "Change Enabled Ad SDKs");
        adSDK.enumValueFlag = updatedMask;
        serializedObject.ApplyModifiedProperties();

        foreach (Object inspectedTarget in targets)
        {
            EditorUtility.SetDirty(inspectedTarget);

            if (inspectedTarget is Component component)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                if (component.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }
        }

        serializedObject.Update();
        Repaint();
    }

    private static string GetSDKSelectionLabel(int selectedMask)
    {
        int allSDKsMask = (int)(AdSDK.Admob | AdSDK.Applovin | AdSDK.UnityAds);
        if (selectedMask == 0)
            return "None";
        if (selectedMask == allSDKsMask)
            return "Everything";

        string label = string.Empty;
        if ((selectedMask & (int)AdSDK.Admob) != 0)
            label = "AdMob";
        if ((selectedMask & (int)AdSDK.Applovin) != 0)
            label += string.IsNullOrEmpty(label) ? "AppLovin" : ", AppLovin";
        if ((selectedMask & (int)AdSDK.UnityAds) != 0)
            label += string.IsNullOrEmpty(label) ? "Unity Ads" : ", Unity Ads";

        return label;
    }

    private void DrawPrioritySection()
    {
        DrawSectionHeader("Ad Priority");
        EditorGUILayout.PropertyField(priorityMode, new GUIContent("Priority Mode"));
        EditorGUILayout.HelpBox("Unity Ads is used only for Interstitial and Rewarded. Unity entries are ignored for Banner and Big Banner placements.", MessageType.Info);
        if (priorityMode.enumValueIndex == (int)AdPriorityMode.Universal)
        {
            EditorGUILayout.HelpBox("This one order is used for every ad format.", MessageType.Info);
            EditorGUILayout.PropertyField(universalPriorityOrder, new GUIContent("Universal Order"), true);
            return;
        }


        EditorGUILayout.HelpBox("Each ad format uses its own provider order.", MessageType.Info);
        DrawPriorityOrder("Banner", bannerPriorityOrder);
        DrawPriorityOrder("Big Banner", bigBannerPriorityOrder);
        DrawPriorityOrder("Interstitial", interstitialPriorityOrder);
        DrawPriorityOrder("Rewarded", rewardedPriorityOrder);

        if (IsSDKSelected(AdSDK.Admob))
        {
            DrawPriorityOrder("App Open", appOpenPriorityOrder);
        }
        else
        {
            EditorGUILayout.HelpBox("App Open controls are hidden because this manager supports App Open ads through AdMob only.", MessageType.None);
        }
    }

    private void DrawBehaviourSection()
    {
        DrawSectionHeader("Runtime Behaviour");
        EditorGUILayout.PropertyField(showLoadingPanel, new GUIContent("Show Loading Panel"));
        EditorGUILayout.PropertyField(neverSleepMode, new GUIContent("Never Sleep"));

        EditorGUILayout.PropertyField(showBannerOnLoad, new GUIContent("Show Banners on Load", "Shows normal banner placements as they become ready."));
        EditorGUILayout.HelpBox("When disabled, use ShowBanner or ShowBigBannerAds manually. Configure placement IDs and positions in the Ad Settings asset.", MessageType.None);

        if (IsSDKSelected(AdSDK.Admob) || IsSDKSelected(AdSDK.Applovin))
        {
            EditorGUILayout.PropertyField(showAppOpenAdOnLoad, new GUIContent("Show App Open on Load"));
            EditorGUILayout.PropertyField(showAppOpenInBackground, new GUIContent("Show App Open in Background"));
            EditorGUILayout.PropertyField(showAppOpenAfterInterAds, new GUIContent("Show App Open After Interstitials"));
        }
    }

    private void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, LabelStyles.GetHeaderStyle());
        EditorGUILayout.Space(2);
    }

    private static void DrawPriorityOrder(string title, SerializedProperty property)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(title + " Order"), true);
        EditorGUILayout.Space(2);
    }

    private AdSDK GetSelectedSDKs()
    {
        return (AdSDK)adSDK.enumValueFlag;
    }

    private bool IsSDKSelected(AdSDK sdk)
    {
        return (GetSelectedSDKs() & sdk) != 0;
    }

   
}
#endif

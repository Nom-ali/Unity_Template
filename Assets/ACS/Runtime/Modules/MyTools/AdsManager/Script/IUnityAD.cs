#if ACS_UNITY_ADS_LEGACY
using ACS.SaveManager;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Unity Ads is used for interstitial and rewarded video only.
/// </summary>
public class IUnityAD : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    private sealed class PlacementState
    {
        public string PlacementId;
        public bool Ready;
        public Action OnClose;
        public Action OnFailed;
        public Action OnRewarded;
        public int RetryAttempt;
    }

    private bool IsAd_Removed => SaveManager.Prefs.GetBool(SharedVariables.RemoveAds, false);
    private const int MaxRetryAttempts = 3;

    [SerializeField] private Platform m_Platform = Platform.Android;
    [SerializeField] private bool enableTestIds;

    private string unityGameId;
    private readonly Dictionary<string, PlacementState> interstitialStates = new();
    private readonly Dictionary<string, PlacementState> rewardedStates = new();
    private readonly Dictionary<string, string> interstitialKeyByPlacementId = new();
    private readonly Dictionary<string, string> rewardedKeyByPlacementId = new();
    private readonly List<string> interstitialPlacementOrder = new();
    private readonly List<string> rewardedPlacementOrder = new();
    private AdFormatToggles formatToggles;

    public IEnumerator InitializeUnity_ID(AdSettings adSettings, Platform platform, bool testing, AdFormatToggles enabledFormats)
    {
        m_Platform = platform;
        enableTestIds = testing;
        formatToggles = enabledFormats ?? new AdFormatToggles();
        unityGameId = platform == Platform.Android ? adSettings.UnityAndroidID : adSettings.UnityIosID;
        if (formatToggles.interstitial)
            CreatePlacements(adSettings.GetUnityInterstitialIds(platform), interstitialStates, interstitialKeyByPlacementId, interstitialPlacementOrder, "interstitial");
        if (formatToggles.rewarded)
            CreatePlacements(adSettings.GetUnityRewardedIds(platform), rewardedStates, rewardedKeyByPlacementId, rewardedPlacementOrder, "rewarded");
        Debug.Log("SDK: UNITY, Initializing video ads.");
        Advertisement.Initialize(unityGameId, enableTestIds, this);
        yield return null;
    }

    public bool TryShowUnityInterstitial(string placementKey, Action onClose, Action onFailed = null)
    {
        if (IsAd_Removed || !TryGetReadyPlacement(interstitialStates, interstitialPlacementOrder, placementKey, out PlacementState state))
            return false;
        state.OnClose = onClose;
        state.OnFailed = onFailed;
        Advertisement.Show(state.PlacementId, this);
        return true;
    }

    public void ShowUnityInter(Action onClose) => TryShowUnityInterstitial(null, onClose);

    public bool TryShowUnityRewarded(string placementKey, Action action)
    {
        if (!TryGetReadyPlacement(rewardedStates, rewardedPlacementOrder, placementKey, out PlacementState state))
            return false;
        state.OnRewarded = action;
        Advertisement.Show(state.PlacementId, this);
        return true;
    }

    public void ShowUnityRewarded(Action action) => TryShowUnityRewarded(null, action);

    public void OnInitializationComplete()
    {
        if (formatToggles.interstitial)
            LoadAll(interstitialStates, interstitialPlacementOrder);
        if (formatToggles.rewarded)
            LoadAll(rewardedStates, rewardedPlacementOrder);
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError("Unity Ads initialization failed: " + error + " - " + message);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (TryGetState(interstitialStates, interstitialKeyByPlacementId, placementId, out PlacementState interstitial))
        {
            interstitial.RetryAttempt = 0;
            interstitial.Ready = true;
        }
        else if (TryGetState(rewardedStates, rewardedKeyByPlacementId, placementId, out PlacementState rewarded))
        {
            rewarded.RetryAttempt = 0;
            rewarded.Ready = true;
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError("Unity Ads failed to load " + placementId + ": " + message);
        RetryFailedLoad(interstitialStates, interstitialKeyByPlacementId, placementId);
        RetryFailedLoad(rewardedStates, rewardedKeyByPlacementId, placementId);
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError("Unity Ads failed to show " + placementId + ": " + message);
        if (TryGetState(interstitialStates, interstitialKeyByPlacementId, placementId, out PlacementState interstitial))
        {
            interstitial.Ready = false;
            Action onFailed = interstitial.OnFailed;
            interstitial.OnClose = null;
            interstitial.OnFailed = null;
            Load(interstitial);
            onFailed?.Invoke();
        }
        else if (TryGetState(rewardedStates, rewardedKeyByPlacementId, placementId, out PlacementState rewarded))
        {
            rewarded.Ready = false;
            rewarded.OnRewarded = null;
            Load(rewarded);
        }
    }

    public void OnUnityAdsShowStart(string placementId) { }
    public void OnUnityAdsShowClick(string placementId) { }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (TryGetState(interstitialStates, interstitialKeyByPlacementId, placementId, out PlacementState interstitial))
        {
            interstitial.RetryAttempt = 0;
            interstitial.Ready = false;
            Action onClose = interstitial.OnClose;
            interstitial.OnClose = null;
            interstitial.OnFailed = null;
            Load(interstitial);
            onClose?.Invoke();
            return;
        }

        if (TryGetState(rewardedStates, rewardedKeyByPlacementId, placementId, out PlacementState rewarded))
        {
            rewarded.RetryAttempt = 0;
            rewarded.Ready = false;
            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
                rewarded.OnRewarded?.Invoke();

            rewarded.OnRewarded = null;
            Load(rewarded);
        }
    }

    public bool IsInterstitialReady() => IsInterstitialReady(null);
    public bool IsInterstitialReady(string placementKey) => TryGetReadyPlacement(interstitialStates, interstitialPlacementOrder, placementKey, out _);
    public bool IsRewardedAdReady() => IsRewardedAdReady(null);
    public bool IsRewardedAdReady(string placementKey) => TryGetReadyPlacement(rewardedStates, rewardedPlacementOrder, placementKey, out _);

    private static void CreatePlacements(IReadOnlyList<UnityLegacyPlacementIDs> placements, Dictionary<string, PlacementState> states,
        Dictionary<string, string> keysByPlacementId, List<string> placementOrder, string format)
    {
        if (placements == null)
            return;
        foreach (UnityLegacyPlacementIDs placement in placements)
        {
            string key = placement.placementKey?.Trim();
            string placementId = placement.placementId?.Trim();
            if (!placement.enabled || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(placementId))
                continue;
            if (states.ContainsKey(key) || keysByPlacementId.ContainsKey(placementId))
            {
                Debug.LogError("Unity Legacy Ads: Duplicate " + format + " placement key or placement ID: " + key);
                continue;
            }
            states.Add(key, new PlacementState { PlacementId = placementId });
            keysByPlacementId.Add(placementId, key);
            placementOrder.Add(key);
        }
    }

    private static bool TryGetState(Dictionary<string, PlacementState> states, Dictionary<string, string> keysByPlacementId, string placementId, out PlacementState state)
    {
        state = null;
        return keysByPlacementId.TryGetValue(placementId, out string key) && states.TryGetValue(key, out state);
    }

    private static bool TryGetReadyPlacement(Dictionary<string, PlacementState> states, List<string> placementOrder, string placementKey, out PlacementState readyState)
    {
        if (!string.IsNullOrWhiteSpace(placementKey) && states.TryGetValue(placementKey, out PlacementState requested) && requested.Ready)
        {
            readyState = requested;
            return true;
        }
        foreach (string key in placementOrder)
        {
            if (key == placementKey || !states.TryGetValue(key, out PlacementState state) || !state.Ready)
                continue;
            readyState = state;
            return true;
        }
        readyState = null;
        return false;
    }

    private void LoadAll(Dictionary<string, PlacementState> states, List<string> placementOrder)
    {
        foreach (string key in placementOrder)
            if (states.TryGetValue(key, out PlacementState state))
                Load(state);
    }

    private void RetryFailedLoad(Dictionary<string, PlacementState> states, Dictionary<string, string> keysByPlacementId, string placementId)
    {
        if (!TryGetState(states, keysByPlacementId, placementId, out PlacementState state))
            return;
        state.Ready = false;
        if (state.RetryAttempt++ < MaxRetryAttempts)
            Load(state);
    }

    private void Load(PlacementState state)
    {
        if (state != null && !string.IsNullOrWhiteSpace(state.PlacementId))
            Advertisement.Load(state.PlacementId, this);
    }
}
#endif

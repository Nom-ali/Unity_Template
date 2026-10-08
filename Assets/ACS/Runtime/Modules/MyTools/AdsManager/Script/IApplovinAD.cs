#if ACS_APPLOVIN
using ACS.SaveManager;
#if ACS_FIREBASE
using Firebase.Analytics;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IApplovinAD : MonoBehaviour
{
    #region Variables
    private sealed class PlacementState
    {
        public string AdUnitId;
        public bool Ready;
        public Action OnClose;
        public int RetryAttempt;
    }

    private bool IsAd_Removed => SaveManager.Prefs.GetBool(SharedVariables.RemoveAds, false);

    private readonly Dictionary<string, PlacementState> bannerStates = new();
    private readonly Dictionary<string, PlacementState> mrecStates = new();
    private readonly Dictionary<string, PlacementState> interstitialStates = new();
    private readonly Dictionary<string, string> bannerKeyByAdUnitId = new();
    private readonly Dictionary<string, string> mrecKeyByAdUnitId = new();
    private readonly Dictionary<string, string> interstitialKeyByAdUnitId = new();

    private Platform platform;
    private bool showBannerOnLoad;
    private string appOpenAdUnitId;
    private string rewardedAdUnitId;
    private IReadOnlyList<ApplovinBannerAdIDs> bannerAdIDs;
    private IReadOnlyList<ApplovinBannerAdIDs> bigBannerAdIDs;
    private IReadOnlyList<ApplovinInterstitialAdIDs> interstitialAdIDs;
    
    private Action rewardAction;
    private bool showAppOpenAdOnLoad;
    private bool firstTimeAppOpen = true;
    private bool adsLoaded;
    private int appOpenRetryAttempt;
    private int rewardedRetryAttempt;

    [SerializeField] private AdsStatus bannerStatus = AdsStatus.None;
    [SerializeField] private ApplovinAdIDs ids;

    public AdsStatus CurrentBannerStatus { get => bannerStatus; private set => bannerStatus = value; }
    #endregion


    #region Initialization
    public IEnumerator Initialize_AppLovin(AdSettings adSettings, Platform targetPlatform, bool showBannersOnLoad, bool showAppOpenOnLoad)
    {
        if (adSettings == null)
        {
            Debug.LogError("AppLovin: AdSettings is missing.");
            yield break;
        }

        platform = targetPlatform;
        showBannerOnLoad = showBannersOnLoad;
        showAppOpenAdOnLoad = showAppOpenOnLoad;
      
        ids = adSettings.GetApplovinAdIds(platform);
        appOpenAdUnitId = ids.AppOpenAdUnitId?.Trim();
        rewardedAdUnitId = ids.RewardedAdUnitId?.Trim();
        bannerAdIDs = ids.bannerIds;
        bigBannerAdIDs = ids.bigBannerIds;
        interstitialAdIDs = adSettings.GetApplovinInterstitialIds(platform);

        yield return InitializeApplovin(adSettings);
    }

    private IEnumerator InitializeApplovin(AdSettings adSettings)
    {
        if (MaxSdk.IsInitialized())
        {
            LoadEveryAd();
            yield break;
        }

        MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;

        MaxSdk.SetExtraParameter("pisw", "true");
        MaxSdk.SetHasUserConsent(true);

        MaxSdk.InitializeSdk();
        yield return null;
    }

    private void OnSdkInitialized(MaxSdkBase.SdkConfiguration configuration)
    {
        MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
        LoadEveryAd();
    }

    void LoadEveryAd()
    {
        if (adsLoaded)
            return;

        adsLoaded = true;
        if (!IsAd_Removed)
        {
            InitializeAppOpenAd();
            InitializeBannerAds(bannerAdIDs, false);
            InitializeBannerAds(bigBannerAdIDs, true);
            InitializeInterstitialAds(interstitialAdIDs);
        }

        InitializeRewardedAds();
    }

    #endregion


    #region AppOpen
    private void InitializeAppOpenAd()
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            return;
        }

        if (string.IsNullOrWhiteSpace(appOpenAdUnitId))
            return;

        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenLoaded;
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenLoadFailed;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenHidden;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenDisplayFailed;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += (message, info) => LogRevenuePaid(appOpenAdUnitId, "AppOpen", info);

        LoadAppOpenAd();
    }

    private void OnAppOpenLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        appOpenRetryAttempt = 0;

#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN
        if (showAppOpenAdOnLoad && firstTimeAppOpen)
        {
            firstTimeAppOpen = false;
            ACS.AdsManager.AdsManager.Instance?.ShowAppOpen();
        }
#endif
    }

    private void OnAppOpenLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        appOpenRetryAttempt++;
        Invoke(nameof(LoadAppOpenAd), (float)Math.Pow(2, Math.Min(6, appOpenRetryAttempt)));
    }

    private void OnAppOpenHidden(string adUnitId, MaxSdkBase.AdInfo adInfo) => LoadAppOpenAd();

    private void OnAppOpenDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo) => LoadAppOpenAd();
  
    private void LoadAppOpenAd()
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            return;
        }

        if (!IsAd_Removed && !string.IsNullOrWhiteSpace(appOpenAdUnitId))
            MaxSdk.LoadAppOpenAd(appOpenAdUnitId);
    }

    public bool TryShowAppOpenAd()
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            return false;
        }

        if (!IsAppOpenAdReady())
        {
            LoadAppOpenAd();
            return false;
        }

        MaxSdk.ShowAppOpenAd(appOpenAdUnitId);
        return true;
    }
    #endregion


    #region Banners / Big Banners
    private void InitializeBannerAds(IReadOnlyList<ApplovinBannerAdIDs> placements, bool isMrec)
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            return;
        }

        if (placements == null || placements.Count == 0)
        {
            return;
        }

        if (isMrec)
        {
            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMrecAdLoaded;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMrecAdFailed;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += (adUnitId, info) => LogRevenuePaid(adUnitId, "Big Banner", info);
        }
        else
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailed;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adUnitId, info) => LogRevenuePaid(adUnitId, "Banner", info);
        }

        foreach (ApplovinBannerAdIDs placement in placements)
        {
            string placementKey = placement.placementKey?.Trim();
            if (!placement.enabled || string.IsNullOrWhiteSpace(placementKey))
                continue;

            string adUnitId = placement.appLovinAdUnitId?.Trim();
            if (string.IsNullOrWhiteSpace(adUnitId))
                continue;

            Dictionary<string, PlacementState> states = isMrec ? mrecStates : bannerStates;
            Dictionary<string, string> keysByAdUnitId = isMrec ? mrecKeyByAdUnitId : bannerKeyByAdUnitId;
            if (states.ContainsKey(placementKey) || keysByAdUnitId.ContainsKey(adUnitId))
            {
                Debug.LogError("AppLovin: Duplicate " + (isMrec ? "big banner" : "banner") + " placement key or ad unit ID: " + placementKey);
                continue;
            }

            states.Add(placementKey, new PlacementState { AdUnitId = adUnitId });
            keysByAdUnitId.Add(adUnitId, placementKey);

            Debug.Log($"Applovin: Loading Banner, size: {(isMrec ? "Adaptive" : "Normal Banner")}, Ad ID {adUnitId}");
            if (isMrec)
                MaxSdk.CreateMRec(adUnitId, new MaxSdkBase.AdViewConfiguration(placement.appLovinPosition));
            else
            {
                MaxSdk.SetBannerExtraParameter(adUnitId, "adaptive_banner", placement.useAppLovinAdaptiveBanner ? "true" : "false");
                MaxSdk.CreateBanner(adUnitId, new MaxSdkBase.AdViewConfiguration(placement.appLovinPosition));
                MaxSdk.SetBannerBackgroundColor(adUnitId, Color.black);
            }
        }
    }

    public bool TryShowBanner(string placementKey) => TryShowPlacement(bannerStates, placementKey, false);
    public bool TryShowMrecBanner(string placementKey) => TryShowPlacement(mrecStates, placementKey, true);

    private bool TryShowPlacement(Dictionary<string, PlacementState> states, string placementKey, bool isMrec)
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            return false;
        }

        if (!states.TryGetValue(placementKey, out PlacementState state) || !state.Ready)
            return false;

        if (isMrec)
            MaxSdk.ShowMRec(state.AdUnitId);
        else
            MaxSdk.ShowBanner(state.AdUnitId);

        return true;
    }

    public void HideBanner(string placementKey) => HidePlacement(bannerStates, placementKey, false);
    public void HideMrecBanner(string placementKey) => HidePlacement(mrecStates, placementKey, true);
    public void HideAllBanners() => HideAllPlacements(bannerStates, false);
    public void HideAllMrecBanners() => HideAllPlacements(mrecStates, true);

    private static void HidePlacement(Dictionary<string, PlacementState> states, string placementKey, bool isMrec)
    {
        if (!states.TryGetValue(placementKey, out PlacementState state))
            return;

        if (isMrec)
            MaxSdk.HideMRec(state.AdUnitId);
        else
            MaxSdk.HideBanner(state.AdUnitId);
    }

    private static void HideAllPlacements(Dictionary<string, PlacementState> states, bool isMrec)
    {
        foreach (PlacementState state in states.Values)
        {
            if (isMrec)
                MaxSdk.HideMRec(state.AdUnitId);
            else
                MaxSdk.HideBanner(state.AdUnitId);
        }
    }

    // No-parameter methods hide every placement.
    public void DestroyBanner() => HideAllBanners();
    public void DestroyMRecBanner() => HideAllMrecBanners();
    public void HideApplovin_Banner() => HideAllBanners();
    public void HideApplovin_MRecBanner() => HideAllMrecBanners();

    private void OnBannerAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        SetPlacementReady(bannerKeyByAdUnitId, bannerStates, adUnitId, true);
    }
    private void OnBannerAdFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        SetPlacementReady(bannerKeyByAdUnitId, bannerStates, adUnitId, false);
        Debug.LogWarning("AppLovin: Banner failed to load: " + adUnitId + " (" + errorInfo.Code + ")");
    }
    private void OnMrecAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        SetPlacementReady(mrecKeyByAdUnitId, mrecStates, adUnitId, true);
    }
    private void OnMrecAdFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        SetPlacementReady(mrecKeyByAdUnitId, mrecStates, adUnitId, false);
        Debug.LogWarning("AppLovin: MREC failed to load: " + adUnitId + " (" + errorInfo.Code + ")");
    }
    private void SetPlacementReady(Dictionary<string, string> keysByAdUnitId, Dictionary<string, PlacementState> states, string adUnitId, bool ready)
    {
        if (!keysByAdUnitId.TryGetValue(adUnitId, out string placementKey) || !states.TryGetValue(placementKey, out PlacementState state))
            return;

        state.Ready = ready;
        if (states == bannerStates)
        {
            CurrentBannerStatus = ready ? AdsStatus.Ready : AdsStatus.NotReady;
#if ACS_ADMOB || ACS_APPLOVIN
            if (ready && showBannerOnLoad)
                ACS.AdsManager.AdsManager.Instance?.ShowBanner();
#endif
        }
    }
    #endregion


    #region Interstitial
    private void InitializeInterstitialAds(IReadOnlyList<ApplovinInterstitialAdIDs> placements)
    {
        if (IsAd_Removed || placements == null || placements.Count == 0)
            return;

        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaid;

        foreach (ApplovinInterstitialAdIDs placement in placements)
        {
            string placementKey = placement.placementKey?.Trim();
            string adUnitId = placement.appLovinAdUnitId?.Trim();
            if (!placement.enabled || string.IsNullOrWhiteSpace(placementKey) || string.IsNullOrWhiteSpace(adUnitId))
                continue;

            if (interstitialStates.ContainsKey(placementKey) || interstitialKeyByAdUnitId.ContainsKey(adUnitId))
            {
                Debug.LogError("AppLovin: Duplicate interstitial placement key or ad unit ID: " + placementKey);
                continue;
            }

            interstitialStates.Add(placementKey, new PlacementState { AdUnitId = adUnitId });
            interstitialKeyByAdUnitId.Add(adUnitId, placementKey);
            LoadInterstitial(placementKey);
        }
    }

    public bool TryShowApplovinInterstitial(string placementKey, Action onClose)
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            onClose?.Invoke();
            return false;
        }

        if (!TryGetReadyInterstitialState(placementKey, out PlacementState state))
            return false;

        Debug.Log("[Applovin] Showing interstitial ad for placement: " + (placementKey ?? "any"));
        state.OnClose = onClose;
        MaxSdk.ShowInterstitial(state.AdUnitId);
        return true;
    }

    public void ShowApplovin_Interstitial(Action onClose) => TryShowApplovinInterstitial(null, onClose);

    private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (TryGetInterstitialState(adUnitId, out PlacementState state))
        {
            state.Ready = true;
            state.RetryAttempt = 0;
        }
    }

    private void OnInterstitialFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        if (!TryGetInterstitialState(adUnitId, out PlacementState state))
            return;

        state.Ready = false;
        state.RetryAttempt++;
        if (state.RetryAttempt >= 3)
        {
            Debug.LogError("AppLovin: Interstitial failed to load for placement: " + interstitialKeyByAdUnitId[adUnitId]);
            return;
        }

        StartCoroutine(RetryInterstitialLoad(interstitialKeyByAdUnitId[adUnitId], state.RetryAttempt));
    }

    private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        CompleteInterstitial(adUnitId);
    }

    private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        CompleteInterstitial(adUnitId);
    }

    private void OnInterstitialRevenuePaid(string adUnitId, MaxSdkBase.AdInfo info)
    {
        if (interstitialKeyByAdUnitId.TryGetValue(adUnitId, out string placementKey))
            LogRevenuePaid(adUnitId, placementKey, info);
    }

    private void CompleteInterstitial(string adUnitId)
    {
        if (!TryGetInterstitialState(adUnitId, out PlacementState state))
            return;

        Action onClose = state.OnClose;
        state.OnClose = null;
        state.Ready = false;
        onClose?.Invoke();
        LoadInterstitial(interstitialKeyByAdUnitId[adUnitId]);
    }

    private IEnumerator RetryInterstitialLoad(string placementKey, int retryAttempt)
    {
        yield return new WaitForSeconds((float)Math.Pow(2, Math.Min(6, retryAttempt)));
        LoadInterstitial(placementKey);
    }

    private void LoadInterstitial(string placementKey)
    {
        if (IsAd_Removed || !interstitialStates.TryGetValue(placementKey, out PlacementState state) || string.IsNullOrWhiteSpace(state.AdUnitId))
            return;

        MaxSdk.LoadInterstitial(state.AdUnitId);
    }
    #endregion


    #region Rewarded
    private void InitializeRewardedAds()
    {
        if (rewardedRetryAttempt >= 3)
        {
            Debug.LogError($"Applovin: Failed to load, Retrying attempt {rewardedRetryAttempt}");
            return;
        }

        if (string.IsNullOrWhiteSpace(rewardedAdUnitId))
            return;

        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedFailed;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceived;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adUnitId, info) => LogRevenuePaid(adUnitId, "Rewarded", info);
        
        MaxSdk.LoadRewardedAd(rewardedAdUnitId);
    }

    public void ShowApplovin_RewardedAd(Action action)
    {
        if (!MaxSdk.IsRewardedAdReady(rewardedAdUnitId))
            return;

        rewardAction = action;
        MaxSdk.ShowRewardedAd(rewardedAdUnitId);
    }

    private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo) => rewardedRetryAttempt = 0;
    private void OnRewardedFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        rewardedRetryAttempt++;
        Invoke(nameof(LoadRewarded), (float)Math.Pow(2, Math.Min(6, rewardedRetryAttempt)));
    }
    private void OnRewardedDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo) => LoadRewarded();
    private void OnRewardedHidden(string adUnitId, MaxSdkBase.AdInfo adInfo) => LoadRewarded();
    private void OnRewardedReceived(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo) => rewardAction?.Invoke();
   
    private void LoadRewarded()
    {
        if (!string.IsNullOrWhiteSpace(rewardedAdUnitId))
            MaxSdk.LoadRewardedAd(rewardedAdUnitId);
    }
    #endregion


    #region Helpers
    public bool IsBannerReady() => HasReadyPlacement(bannerStates);
    public bool IsMedBannerReady() => HasReadyPlacement(mrecStates);
    public bool IsInterstitialReady() => IsInterstitialReady(null);
    public bool IsInterstitialReady(string placementKey) => TryGetReadyInterstitialState(placementKey, out _);
    public bool IsRewardedAdReady() => !string.IsNullOrWhiteSpace(rewardedAdUnitId) && MaxSdk.IsRewardedAdReady(rewardedAdUnitId);
    public bool IsAppOpenAdReady() => !string.IsNullOrWhiteSpace(appOpenAdUnitId) && MaxSdk.IsAppOpenAdReady(appOpenAdUnitId);

    private static bool HasReadyPlacement(Dictionary<string, PlacementState> states)
    {
        foreach (PlacementState state in states.Values)
            if (state.Ready)
                return true;

        return false;
    }

    private bool TryGetInterstitialState(string adUnitId, out PlacementState state)
    {
        state = null;
        return interstitialKeyByAdUnitId.TryGetValue(adUnitId, out string placementKey)
            && interstitialStates.TryGetValue(placementKey, out state);
    }

    private bool TryGetReadyInterstitialState(string placementKey, out PlacementState readyState)
    {
        if (!string.IsNullOrWhiteSpace(placementKey))
        {
            if (interstitialStates.TryGetValue(placementKey, out PlacementState requestedState)
                && requestedState.Ready
                && MaxSdk.IsInterstitialReady(requestedState.AdUnitId))
            {
                readyState = requestedState;
                return true;
            }

            readyState = null;
            return false;
        }

        foreach (PlacementState state in interstitialStates.Values)
        {
            if (state.Ready && MaxSdk.IsInterstitialReady(state.AdUnitId))
            {
                readyState = state;
                return true;
            }
        }

        readyState = null;
        return false;
    }

    private static void LogRevenuePaid(string adUnitId, string adPosition, MaxSdkBase.AdInfo adInfo)
    {
#if ACS_FIREBASE
        Parameter[] parameters = AdAnalyticsHelper.BuildAdParameters(adUnitId, adPosition, Application.platform.ToString(), adInfo);
        FirebaseManager.Instance?.LogEvents("ad_campaign_impression", parameters);
#endif
    }
    #endregion
}
#endif

#if ACS_ADMOB
using ACS.AdsManager;
using ACS.SaveManager;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAdmobAD : MonoBehaviour
{
    #region Variables
    private sealed class BannerState
    {
        public BannerView View;
        public bool Ready;
    }

    private sealed class InterstitialState
    {
        public string AdUnitId;
        public InterstitialAd Ad;
        public Action OnClose;
        public Action OnFailed;
        public int RetryAttempt;
    }

    private sealed class RewardedState
    {
        public string AdUnitId;
        public RewardedAd Ad;
        public Action RewardAction;
        public int RetryAttempt;
    }

    private sealed class AppOpenState
    {
        public string AdUnitId;
        public AppOpenAd Ad;
        public int RetryAttempt;
    }

    private bool IsAd_Removed => SaveManager.Prefs.GetBool(SharedVariables.RemoveAds, false, true);

    private readonly Dictionary<string, BannerState> bannerStates = new();
    private readonly Dictionary<string, BannerState> bigBannerStates = new();
    private readonly Dictionary<string, InterstitialState> interstitialStates = new();
    private readonly Dictionary<string, RewardedState> rewardedStates = new();
    private readonly Dictionary<string, AppOpenState> appOpenStates = new();
    private readonly List<string> rewardedPlacementOrder = new();
    private readonly List<string> appOpenPlacementOrder = new();

    private Platform platform;
    private bool enableTestIds;
    private bool showBannerOnLoad;
    private bool showAppOpenAdOnLoad;
    private bool canShowAppOpenAd = true;
    private AdFormatToggles formatToggles;
    private AdmobAdIDs liveIds;

    [HideInInspector] public bool AdmobInitialized;
    [SerializeField] private AdsStatus bannerStatus = AdsStatus.None;
    public AdsStatus CurrentBannerStatus { get => bannerStatus; private set => bannerStatus = value; }

    private AdmobAdIDs ids;
    private IReadOnlyList<AdmobBannerAdIDs> bannerIDs;
    private IReadOnlyList<AdmobBannerAdIDs> bigBannerIDs;
    private IReadOnlyList<AdmobInterstitialAdIDs> interstitialIDs;
    private IReadOnlyList<AdmobFullScreenAdIDs> rewardedIDs;
    private IReadOnlyList<AdmobFullScreenAdIDs> appOpenIDs;

    private const int FullScreenMaxRetryAttempts = 3;
    private const int InterMaxRetryAttempts = 3;

    private readonly AdmobAdIDs androidTestingIds = new()
    {
        InterID = "ca-app-pub-3940256099942544/1033173712",
        RewardedID = "ca-app-pub-3940256099942544/5224354917",
        AppOpenID = "ca-app-pub-3940256099942544/9257395921",
        interstitialIds = new AdmobInterstitialAdIDs[1]
        {
            new()
            {
                placementKey = "default",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/1033173712"
            }
        },
        bannerIds = new AdmobBannerAdIDs[2] 
        {
            new()
            {
                placementKey = "banner1",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/6300978111",
                admobPosition = AdPosition.Top
            },
            new()
            {
                placementKey = "banner2",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/6300978111",
                admobPosition = AdPosition.Bottom
            }
        },
        bigBannerIds = new AdmobBannerAdIDs[2] 
        {
            new()
            {
                placementKey = "banner1",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/6300978111",
                admobPosition = AdPosition.BottomLeft
            },
            new()
            {
                placementKey = "banner2",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/6300978111",
                admobPosition = AdPosition.BottomRight
            }
        }

        
    };

    private readonly AdmobAdIDs iosTestingIds = new()
    {
        InterID = "ca-app-pub-3940256099942544/4411468910",
        RewardedID = "ca-app-pub-3940256099942544/1712485313",
        AppOpenID = "ca-app-pub-3940256099942544/5575463023",
        interstitialIds = new AdmobInterstitialAdIDs[1]
        {
            new()
            {
                placementKey = "default",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/4411468910"
            }
        },
        bannerIds = new AdmobBannerAdIDs[2]
        {
            new()
            {
                placementKey = "banner1",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/2934735716",
                admobPosition = AdPosition.Top
            },
            new()
            {
                placementKey = "banner2",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/2934735716",
                admobPosition = AdPosition.Bottom
            }
        },
        bigBannerIds = new AdmobBannerAdIDs[2]
        {
            new()
            {
                placementKey = "banner1",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/2934735716",
                admobPosition = AdPosition.BottomLeft
            },
            new()
            {
                placementKey = "banner2",
                enabled = true,
                admobAdUnitId = "ca-app-pub-3940256099942544/2934735716",
                admobPosition = AdPosition.BottomRight
            }
        }
    };
    #endregion


    #region Initializations
    public IEnumerator Initialize_Admob(AdSettings adSettings, Platform targetPlatform, bool showBannersOnLoad, bool appOpenOnLoad, bool testing, AdFormatToggles enabledFormats)
    {
        if (adSettings == null)
        {
            Debug.LogError("AdMob: AdSettings is missing.");
            yield break;
        }

        platform = targetPlatform;
        enableTestIds = testing;
        showBannerOnLoad = showBannersOnLoad;
        showAppOpenAdOnLoad = appOpenOnLoad;
        formatToggles = enabledFormats ?? new AdFormatToggles();
        liveIds = adSettings.GetAdmobAdIds(platform);

        ids = enableTestIds ? (platform == Platform.Android ? androidTestingIds : iosTestingIds) : liveIds;
        bannerIDs = ids.bannerIds;
        bigBannerIDs = ids.bigBannerIds;
        interstitialIDs = enableTestIds
            ? ids.interstitialIds
            : adSettings.GetAdmobInterstitialIds(platform);
        rewardedIDs = enableTestIds
            ? CreateTestingFullScreenPlacements(adSettings.GetAdmobRewardedIds(platform), ids.RewardedID)
            : adSettings.GetAdmobRewardedIds(platform);
        appOpenIDs = enableTestIds
            ? CreateTestingFullScreenPlacements(adSettings.GetAdmobAppOpenIds(platform), ids.AppOpenID)
            : adSettings.GetAdmobAppOpenIds(platform);

        if (Application.platform == RuntimePlatform.IPhonePlayer)
            MobileAds.SetiOSAppPauseOnBackground(true);

        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize(initStatus =>
        {
            if (initStatus == null)
            {
                Debug.LogError("AdMob initialization failed.");
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            foreach (var adapter in initStatus.getAdapterStatusMap())
            {
                Debug.Log(
                    $"Admob Adapter: {adapter.Key} is <color=yellow>{adapter.Value.InitializationState}</color>"
                );
            }
#endif

            AdmobInitialized = true;

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                LoadEveryAd();
            });
        });

        yield return null;
    }

    internal void LoadEveryAd()
    {
        if (!IsAd_Removed)
        {
            if (formatToggles.banner)
                CreateBannerPlacements(bannerIDs, false);
            if (formatToggles.bigBanner)
                CreateBannerPlacements(bigBannerIDs, true);
            if (formatToggles.interstitial)
                CreateInterstitialPlacements(interstitialIDs);
            if (formatToggles.appOpen)
                CreateAppOpenPlacements(appOpenIDs);
        }

        if (formatToggles.rewarded)
            CreateRewardedPlacements(rewardedIDs);
    }
    #endregion


    #region Banners / Big Banners
    private void CreateBannerPlacements(IReadOnlyList<AdmobBannerAdIDs> placements, bool bigBanner)
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            return;
        }

        foreach (AdmobBannerAdIDs placement in placements)
        {
            string placementKey = placement.placementKey?.Trim();
            if (!placement.enabled || string.IsNullOrWhiteSpace(placementKey))
                continue;

            Dictionary<string, BannerState> states = bigBanner ? bigBannerStates : bannerStates;
            if (states.ContainsKey(placementKey))
            {
                Debug.LogError("AdMob: Duplicate " + (bigBanner ? "big banner" : "banner") + " placement key: " + placementKey);
                continue;
            }
                      
            if (string.IsNullOrWhiteSpace(placement.admobAdUnitId))
                continue;

            AdSize size = bigBanner ? AdSize.MediumRectangle : AdSize.Banner;
            BannerState state = new();
            state.View = new BannerView(placement.admobAdUnitId.Trim(), size, placement.admobPosition);
          
            state.View.OnBannerAdLoaded += () =>
            {
                state.Ready = true;
                if (!bigBanner)
                {
                    CurrentBannerStatus = AdsStatus.Ready;

                    if (showBannerOnLoad)
                        AdsManager.Instance?.ShowBanner();
                }
            };

            state.View.OnBannerAdLoadFailed += (error) =>
            {
                state.Ready = false;
                if (!bigBanner)
                    CurrentBannerStatus = AdsStatus.NotReady;
                Debug.LogWarning("AdMob: " + placementKey + " failed to load: " + error.GetMessage());
            };

            state.View.OnAdPaid += (adValue) => 
            {
                LogRevenuePaid(bigBanner ? "Big Banner" : "Banner", placement.admobAdUnitId, placementKey, state.View.GetResponseInfo(), adValue);
            };

            Debug.Log($"Admob: Loading Banner, size: {size}, Ad ID {placement.admobAdUnitId}");
            state.View.LoadAd(new AdRequest());
            state.View.Hide();
            states.Add(placementKey, state);
        }
    }

    public bool TryShowBanner(string placementKey) => TryShowPlacement(bannerStates, placementKey);
    public bool TryShowBigBanner(string placementKey) => TryShowPlacement(bigBannerStates, placementKey);

    private bool TryShowPlacement(Dictionary<string, BannerState> states, string placementKey)
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            DestroyBanner();
            DestroyMedBanner();
            return false;
        }

        if (!states.TryGetValue(placementKey, out BannerState state) || !state.Ready)
            return false;

        state.View.Show();
        return true;
    }

    public void HideBanner(string placementKey) => HidePlacement(bannerStates, placementKey);
    public void HideBigBanner(string placementKey) => HidePlacement(bigBannerStates, placementKey);
    public void HideAllBanners() => HideAllPlacements(bannerStates);
    public void HideAllBigBanners() => HideAllPlacements(bigBannerStates);

    private void HidePlacement(Dictionary<string, BannerState> states, string placementKey)
    {
        if (states.TryGetValue(placementKey, out BannerState state))
            state.View.Hide();
    }

    private void HideAllPlacements(Dictionary<string, BannerState> states)
    {
        foreach (BannerState state in states.Values)
            state.View.Hide();
    }

    // No-parameter methods hide all active placements.
    public void DestroyBanner() => HideAllBanners();
    public void DestroyMedBanner() => HideAllBigBanners();
    public void AdmobBannerHide() => HideAllBanners();
    public void AdmobMedBannerHide() => HideAllBigBanners();
    #endregion Banners


    #region Interstitial AdS
    private void CreateInterstitialPlacements(IReadOnlyList<AdmobInterstitialAdIDs> placements)
    {
        if (placements == null)
            return;

        foreach (AdmobInterstitialAdIDs placement in placements)
        {
            string placementKey = placement.placementKey?.Trim();
            string adUnitId = placement.admobAdUnitId?.Trim();
            if (!placement.enabled || string.IsNullOrWhiteSpace(placementKey) || string.IsNullOrWhiteSpace(adUnitId))
                continue;

            if (interstitialStates.ContainsKey(placementKey))
            {
                Debug.LogError("AdMob: Duplicate interstitial placement key: " + placementKey);
                continue;
            }

            InterstitialState state = new() { AdUnitId = adUnitId };
            interstitialStates.Add(placementKey, state);
            RequestInterstitial(placementKey, state);
        }
    }

    private void RequestInterstitial(string placementKey, InterstitialState state)
    {
        if (IsAd_Removed || state == null || string.IsNullOrWhiteSpace(state.AdUnitId) || state.RetryAttempt >= InterMaxRetryAttempts)
            return;

        state.Ad?.Destroy();
        state.Ad = null;
        InterstitialAd.Load(state.AdUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                state.RetryAttempt++;
                Debug.LogError("AdMob: Interstitial failed to load for placement: " + placementKey);
                RequestInterstitial(placementKey, state);
                return;
            }

            state.Ad = ad;
            state.RetryAttempt = 0;
            ad.OnAdFullScreenContentClosed += () =>
            {
                Action onClose = state.OnClose;
                state.OnClose = null;
                state.OnFailed = null;
                onClose?.Invoke();
                RequestInterstitial(placementKey, state);
            };
            ad.OnAdFullScreenContentFailed += failedError =>
            {
                Action onFailed = state.OnFailed;
                state.OnClose = null;
                state.OnFailed = null;
                onFailed?.Invoke();
                RequestInterstitial(placementKey, state);
            };
            ad.OnAdPaid += adValue => LogRevenuePaid("Interstitial", state.AdUnitId, placementKey, ad.GetResponseInfo(), adValue);
        });
    }

    public bool TryShowAdmobInterstitial(string placementKey, Action onClose, Action onFailed = null)
    {
        if (IsAd_Removed)
        {
            Debug.LogError("Remove ads is purchased");
            DestroyInterAds();
            onFailed?.Invoke();
            return false;
        }

        if (!TryGetReadyInterstitialState(placementKey, out InterstitialState state))
            return false;

        state.OnClose = onClose;
        state.OnFailed = onFailed;
        state.Ad.Show();
        return true;
    }

    public void ShowAdmobInterAds(Action onClose) => TryShowAdmobInterstitial(null, onClose);

    public void DestroyInterAds()
    {
        foreach (InterstitialState state in interstitialStates.Values)
        {
            state.Ad?.Destroy();
            state.Ad = null;
            state.OnClose = null;
            state.OnFailed = null;
        }
    }
    #endregion Interstitial


    #region Rewarded Ads
    private void CreateRewardedPlacements(IReadOnlyList<AdmobFullScreenAdIDs> placements)
    {
        if (placements == null)
            return;

        foreach (AdmobFullScreenAdIDs placement in placements)
        {
            string key = placement.placementKey?.Trim();
            string adUnitId = placement.admobAdUnitId?.Trim();
            if (!placement.enabled || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(adUnitId))
                continue;
            if (rewardedStates.ContainsKey(key))
            {
                Debug.LogError("AdMob: Duplicate rewarded placement key: " + key);
                continue;
            }

            RewardedState state = new() { AdUnitId = adUnitId };
            rewardedStates.Add(key, state);
            rewardedPlacementOrder.Add(key);
            RequestRewardedAd(key, state);
        }
    }

    private void RequestRewardedAd(string key, RewardedState state)
    {
        if (state == null || state.RetryAttempt >= FullScreenMaxRetryAttempts || string.IsNullOrWhiteSpace(state.AdUnitId))
            return;

        state.Ad?.Destroy();
        state.Ad = null;
        RewardedAd.Load(state.AdUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                state.RetryAttempt++;
                RequestRewardedAd(key, state);
                return;
            }

            state.Ad = ad;
            state.RetryAttempt = 0;
            ad.OnAdFullScreenContentClosed += () => RequestRewardedAd(key, state);
            ad.OnAdFullScreenContentFailed += _ => RequestRewardedAd(key, state);
            ad.OnAdPaid += adValue => LogRevenuePaid("Rewarded", state.AdUnitId, key, ad.GetResponseInfo(), adValue);
        });
    }

    public bool TryShowAdmobRewarded(string placementKey, Action action)
    {
        if (!TryGetReadyRewardedState(placementKey, out RewardedState state))
            return false;

        state.RewardAction = action;
        state.Ad.Show(_ => state.RewardAction?.Invoke());
        return true;
    }

    public void ShowAdmobRewardedAds(Action action) => TryShowAdmobRewarded(null, action);

    public void DestroyRewardedAds()
    {
        foreach (RewardedState state in rewardedStates.Values)
        {
            state.Ad?.Destroy();
            state.Ad = null;
            state.RewardAction = null;
        }
    }
    #endregion Rewarded


    #region AppOpen Ads
    private void CreateAppOpenPlacements(IReadOnlyList<AdmobFullScreenAdIDs> placements)
    {
        if (placements == null)
            return;

        foreach (AdmobFullScreenAdIDs placement in placements)
        {
            string key = placement.placementKey?.Trim();
            string adUnitId = placement.admobAdUnitId?.Trim();
            if (!placement.enabled || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(adUnitId))
                continue;
            if (appOpenStates.ContainsKey(key))
            {
                Debug.LogError("AdMob: Duplicate App Open placement key: " + key);
                continue;
            }

            AppOpenState state = new() { AdUnitId = adUnitId };
            appOpenStates.Add(key, state);
            appOpenPlacementOrder.Add(key);
            RequestAppOpenAd(key, state);
        }
    }

    private void RequestAppOpenAd(string key, AppOpenState state)
    {
        if (IsAd_Removed || state == null || state.RetryAttempt >= FullScreenMaxRetryAttempts || string.IsNullOrWhiteSpace(state.AdUnitId))
            return;

        state.Ad?.Destroy();
        state.Ad = null;
        AppOpenAd.Load(state.AdUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                state.RetryAttempt++;
                RequestAppOpenAd(key, state);
                return;
            }

            state.Ad = ad;
            state.RetryAttempt = 0;
            if (showAppOpenAdOnLoad && canShowAppOpenAd)
            {
                AdsManager.Instance.ShowAppOpen();
                canShowAppOpenAd = false;
            }

            ad.OnAdFullScreenContentClosed += () => RequestAppOpenAd(key, state);
            ad.OnAdFullScreenContentFailed += _ => RequestAppOpenAd(key, state);
            ad.OnAdPaid += adValue => LogRevenuePaid("App Open", state.AdUnitId, key, ad.GetResponseInfo(), adValue);
        });
    }

    public bool TryShowAppOpenAd(string placementKey)
    {
        if (IsAd_Removed)
        {
            DestroyAppOpenAd();
            return false;
        }

        if (!TryGetReadyAppOpenState(placementKey, out AppOpenState state))
            return false;

        canShowAppOpenAd = false;
        state.Ad.Show();
        return true;
    }

    public void ShowAppOpenAd() => TryShowAppOpenAd(null);

    public void DestroyAppOpenAd()
    {
        foreach (AppOpenState state in appOpenStates.Values)
        {
            state.Ad?.Destroy();
            state.Ad = null;
        }
    }
    #endregion AppOpen


    #region Helpers Method
    public bool IsBannerReady => HasReadyPlacement(bannerStates);
    public bool IsMedBannerReady => HasReadyPlacement(bigBannerStates);
    public bool IsInterstitialReady() => IsInterstitialReady(null);

    public bool IsInterstitialReady(string placementKey) => TryGetReadyInterstitialState(placementKey, out _);

    public bool IsRewardedAdReady() => IsRewardedAdReady(null);

    public bool IsRewardedAdReady(string placementKey)
    {
        if (TryGetReadyRewardedState(placementKey, out _))
            return true;

        RequestMissingRewardedPlacements();
        return false;
    }

    public bool IsAppOpenAdReady() => IsAppOpenAdReady(null);

    public bool IsAppOpenAdReady(string placementKey)
    {
        if (TryGetReadyAppOpenState(placementKey, out _))
            return true;

        RequestMissingAppOpenPlacements();
        return false;
    }

    private bool HasReadyPlacement(Dictionary<string, BannerState> states)
    {
        foreach (BannerState state in states.Values)
            if (state.Ready)
                return true;

        return false;
    }

    private bool TryGetReadyInterstitialState(string placementKey, out InterstitialState readyState)
    {
        if (!string.IsNullOrWhiteSpace(placementKey))
        {
            if (interstitialStates.TryGetValue(placementKey, out InterstitialState requestedState) && requestedState.Ad != null && requestedState.Ad.CanShowAd())
            {
                readyState = requestedState;
                return true;
            }
        }

        foreach (InterstitialState state in interstitialStates.Values)
        {
            if (state.Ad != null && state.Ad.CanShowAd())
            {
                readyState = state;
                return true;
            }
        }

        readyState = null;
        return false;
    }

    private bool TryGetReadyRewardedState(string placementKey, out RewardedState readyState)
    {
        return TryGetReadyFullScreenState(rewardedStates, rewardedPlacementOrder, placementKey, out readyState);
    }

    private bool TryGetReadyAppOpenState(string placementKey, out AppOpenState readyState)
    {
        if (!string.IsNullOrWhiteSpace(placementKey) && appOpenStates.TryGetValue(placementKey, out AppOpenState requested) && requested.Ad != null && requested.Ad.CanShowAd())
        {
            readyState = requested;
            return true;
        }

        foreach (string key in appOpenPlacementOrder)
        {
            if (key == placementKey || !appOpenStates.TryGetValue(key, out AppOpenState state) || state.Ad == null || !state.Ad.CanShowAd())
                continue;

            readyState = state;
            return true;
        }

        readyState = null;
        return false;
    }

    private static bool TryGetReadyFullScreenState(Dictionary<string, RewardedState> states, List<string> placementOrder, string placementKey, out RewardedState readyState)
    {
        if (!string.IsNullOrWhiteSpace(placementKey) && states.TryGetValue(placementKey, out RewardedState requested) && requested.Ad != null && requested.Ad.CanShowAd())
        {
            readyState = requested;
            return true;
        }

        foreach (string key in placementOrder)
        {
            if (key == placementKey || !states.TryGetValue(key, out RewardedState state) || state.Ad == null || !state.Ad.CanShowAd())
                continue;

            readyState = state;
            return true;
        }

        readyState = null;
        return false;
    }

    private void RequestMissingRewardedPlacements()
    {
        foreach (KeyValuePair<string, RewardedState> pair in rewardedStates)
            if (pair.Value.Ad == null)
                RequestRewardedAd(pair.Key, pair.Value);
    }

    private void RequestMissingAppOpenPlacements()
    {
        foreach (KeyValuePair<string, AppOpenState> pair in appOpenStates)
            if (pair.Value.Ad == null)
                RequestAppOpenAd(pair.Key, pair.Value);
    }

    private static IReadOnlyList<AdmobFullScreenAdIDs> CreateTestingFullScreenPlacements(IReadOnlyList<AdmobFullScreenAdIDs> configuredPlacements, string testAdUnitId)
    {
        List<AdmobFullScreenAdIDs> testingPlacements = new();
        if (configuredPlacements != null)
        {
            foreach (AdmobFullScreenAdIDs placement in configuredPlacements)
            {
                string key = placement.placementKey?.Trim();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                testingPlacements.Add(new AdmobFullScreenAdIDs
                {
                    placementKey = key,
                    enabled = placement.enabled,
                    admobAdUnitId = testAdUnitId,
                });
            }
        }

        if (testingPlacements.Count == 0 && !string.IsNullOrWhiteSpace(testAdUnitId))
            testingPlacements.Add(new AdmobFullScreenAdIDs { placementKey = "default", enabled = true, admobAdUnitId = testAdUnitId });

        return testingPlacements;
    }

    private void LogRevenuePaid(string adFormat, string adUnitId, string adPosition, ResponseInfo responseInfo, AdValue adValue)
    {
#if ACS_FIREBASE
        var parameters = AdAnalyticsHelper.BuildAdParameters(adFormat, adUnitId, adPosition, Application.platform.ToString(), responseInfo, adValue);
        FirebaseManager.Instance?.LogEvents("ad_campaign_impression", parameters);
#endif
    }
#endregion

}
#endif

#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
using System;
using System.Collections.Generic;
using UnityEngine;

#if ACS_ADMOB
using GoogleMobileAds.Api;
#endif

[CreateAssetMenu(fileName = "AdSettings", menuName = "Ads/AdSettings", order = 1)]
public class AdSettings : ScriptableObject
{

#if ACS_ADMOB
    [Header("********** AdMob AD IDs **********")]
    [SerializeField] private AdmobAdIDs androidAdmobAdIDs;
    [Space]
    [SerializeField] private AdmobAdIDs iosAdmobAdIDs;
#endif

#if ACS_APPLOVIN
    [Header("********** AppLovin AD IDs **********")]
    [SerializeField] private string MaxSdkKey = "A_TZOfK-bMvZru7ArLakUlx7uAdRpxDloxM6348R3N75f9QiIMbIintpUUe1Tp3DzfLnrnhv2ne6zduNcpBpNu";
    [SerializeField] private ApplovinAdIDs androidApplovinAdIDs;
    [Space]
    [SerializeField] private ApplovinAdIDs iosApplovinAdIDs;
#endif

#if ACS_UNITY_ADS_LEGACY
    [Header("********** Unity Video Settings **********")]
    [SerializeField] private string UnityID_Adroid;
    [SerializeField] private string UnityID_iOS;
    [Header("Unity Legacy Android Placements")]
    [Tooltip("Named interstitial placements. Leave empty to use the legacy default placement.")]
    [SerializeField] private UnityLegacyPlacementIDs[] androidUnityInterstitialIds;
    [Tooltip("Named rewarded placements. Leave empty to use the legacy default placement.")]
    [SerializeField] private UnityLegacyPlacementIDs[] androidUnityRewardedIds;
    [Header("Unity Legacy iOS Placements")]
    [Tooltip("Named interstitial placements. Leave empty to use the legacy default placement.")]
    [SerializeField] private UnityLegacyPlacementIDs[] iosUnityInterstitialIds;
    [Tooltip("Named rewarded placements. Leave empty to use the legacy default placement.")]
    [SerializeField] private UnityLegacyPlacementIDs[] iosUnityRewardedIds;
#endif

#if ACS_UNITY_LEVELPLAY
    [Header("********** LevelPlay AD IDs **********")]
    [SerializeField] private ACS.LevelPlayPlatformAdSettings levelPlayAdIdsAndroid = new();
    [Space]
    [SerializeField] private ACS.LevelPlayPlatformAdSettings levelPlayAdIdsIOS = new();
#endif

    [Space]
    [SerializeField, TextArea(2, 4)] private string UserTrackingUsageDescription = "Allow tracking to help us show ads that match your interests and keep the game free for everyone.";
    public string TrackingUsageDescription => UserTrackingUsageDescription;

#if ACS_ADMOB
    public AdmobAdIDs GetAdmobAdIds(Platform platform) =>
        platform == Platform.Android ? androidAdmobAdIDs : iosAdmobAdIDs;

    public IReadOnlyList<AdmobBannerAdIDs> GetAdmobBannerIds(Platform platform, bool bigBanner)
    {
        AdmobAdIDs ids = GetAdmobAdIds(platform);
        return bigBanner ? ids.bigBannerIds ?? Array.Empty<AdmobBannerAdIDs>() : ids.bannerIds ?? Array.Empty<AdmobBannerAdIDs>();
    }

    public IReadOnlyList<AdmobInterstitialAdIDs> GetAdmobInterstitialIds(Platform platform)
    {
        AdmobAdIDs ids = GetAdmobAdIds(platform);
        if (ids.interstitialIds != null && ids.interstitialIds.Length > 0)
            return ids.interstitialIds;

        return string.IsNullOrWhiteSpace(ids.InterID)
            ? Array.Empty<AdmobInterstitialAdIDs>()
            : new[] { new AdmobInterstitialAdIDs { placementKey = "default", enabled = true, admobAdUnitId = ids.InterID } };
    }

    public IReadOnlyList<AdmobFullScreenAdIDs> GetAdmobRewardedIds(Platform platform) =>
        GetAdmobFullScreenIds(GetAdmobAdIds(platform).rewardedIds, GetAdmobAdIds(platform).RewardedID);

    public IReadOnlyList<AdmobFullScreenAdIDs> GetAdmobAppOpenIds(Platform platform) =>
        GetAdmobFullScreenIds(GetAdmobAdIds(platform).appOpenIds, GetAdmobAdIds(platform).AppOpenID);

    private static IReadOnlyList<AdmobFullScreenAdIDs> GetAdmobFullScreenIds(AdmobFullScreenAdIDs[] placements, string legacyId)
    {
        if (placements != null && placements.Length > 0)
            return placements;

        return string.IsNullOrWhiteSpace(legacyId)
            ? Array.Empty<AdmobFullScreenAdIDs>()
            : new[] { new AdmobFullScreenAdIDs { placementKey = "default", enabled = true, admobAdUnitId = legacyId } };
    }

    public string GetAdmobAppId(Platform platform) => GetAdmobAdIds(platform).APPID;
#endif

#if ACS_APPLOVIN
    public string MaxSDKKey => MaxSdkKey;

    public ApplovinAdIDs GetApplovinAdIds(Platform platform) =>
        platform == Platform.Android ? androidApplovinAdIDs : iosApplovinAdIDs;

    public IReadOnlyList<ApplovinBannerAdIDs> GetApplovinBannerIds(Platform platform, bool bigBanner)
    {
        ApplovinAdIDs ids = GetApplovinAdIds(platform);
        return bigBanner ? ids.bigBannerIds ?? Array.Empty<ApplovinBannerAdIDs>() : ids.bannerIds ?? Array.Empty<ApplovinBannerAdIDs>();
    }

    public IReadOnlyList<ApplovinInterstitialAdIDs> GetApplovinInterstitialIds(Platform platform)
    {
        ApplovinAdIDs ids = GetApplovinAdIds(platform);
        if (ids.interstitialIds != null && ids.interstitialIds.Length > 0)
            return ids.interstitialIds;

        return string.IsNullOrWhiteSpace(ids.InterstitialAdUnitId)
            ? Array.Empty<ApplovinInterstitialAdIDs>()
            : new[] { new ApplovinInterstitialAdIDs { placementKey = "default", enabled = true, appLovinAdUnitId = ids.InterstitialAdUnitId } };
    }

    public IReadOnlyList<ApplovinFullScreenAdIDs> GetApplovinRewardedIds(Platform platform) =>
        GetApplovinFullScreenIds(GetApplovinAdIds(platform).rewardedIds, GetApplovinAdIds(platform).RewardedAdUnitId);

    public IReadOnlyList<ApplovinFullScreenAdIDs> GetApplovinAppOpenIds(Platform platform) =>
        GetApplovinFullScreenIds(GetApplovinAdIds(platform).appOpenIds, GetApplovinAdIds(platform).AppOpenAdUnitId);

    private static IReadOnlyList<ApplovinFullScreenAdIDs> GetApplovinFullScreenIds(ApplovinFullScreenAdIDs[] placements, string legacyId)
    {
        if (placements != null && placements.Length > 0)
            return placements;

        return string.IsNullOrWhiteSpace(legacyId)
            ? Array.Empty<ApplovinFullScreenAdIDs>()
            : new[] { new ApplovinFullScreenAdIDs { placementKey = "default", enabled = true, appLovinAdUnitId = legacyId } };
    }
#endif

#if ACS_UNITY_ADS_LEGACY
    public string UnityAndroidID => UnityID_Adroid;
    public string UnityIosID => UnityID_iOS;

    public IReadOnlyList<UnityLegacyPlacementIDs> GetUnityInterstitialIds(Platform platform) =>
        GetUnityPlacementIds(platform == Platform.Android ? androidUnityInterstitialIds : iosUnityInterstitialIds,
            platform == Platform.Android ? "Interstitial_Android" : "Interstitial_iOS");

    public IReadOnlyList<UnityLegacyPlacementIDs> GetUnityRewardedIds(Platform platform) =>
        GetUnityPlacementIds(platform == Platform.Android ? androidUnityRewardedIds : iosUnityRewardedIds,
            platform == Platform.Android ? "Rewarded_Android" : "Rewarded_iOS");

    private static IReadOnlyList<UnityLegacyPlacementIDs> GetUnityPlacementIds(UnityLegacyPlacementIDs[] placements, string legacyPlacementId)
    {
        if (placements != null && placements.Length > 0)
            return placements;

        return new[] { new UnityLegacyPlacementIDs { placementKey = "default", enabled = true, placementId = legacyPlacementId } };
    }
#endif

#if ACS_UNITY_LEVELPLAY
    public ACS.LevelPlayPlatformAdSettings GetLevelPlayAdIds(Platform platform) =>
        platform == Platform.Android ? levelPlayAdIdsAndroid : levelPlayAdIdsIOS;
#endif
}
#endif

#if ACS_UNITY_LEVELPLAY
namespace ACS
{
    [Serializable]
    public sealed class LevelPlayPlatformAdSettings
    {
        [Tooltip("LevelPlay application key for this platform.")]
        public string appKey;

        [Header("Standard Banners")]
        public List<LevelPlayManager.BannerUnit> banners = new();

        [Header("MREC / Big Banners")]
        public List<LevelPlayManager.MrecBannerUnit> mrecBanners = new();

        [Header("Interstitials")]
        public List<LevelPlayManager.FullScreenUnit> interstitials = new();

        [Header("Rewarded")]
        public List<LevelPlayManager.FullScreenUnit> rewardeds = new();
    }
}
#endif


#if ACS_ADMOB
    [Serializable]
    public struct AdmobAdIDs
    {
        public string APPID;
        public string InterID;
        public string RewardedID;
        public string AppOpenID;
        [Header("Named Banner Placements")]
        public AdmobBannerAdIDs[] bannerIds;
        public AdmobBannerAdIDs[] bigBannerIds;
        [Header("Named Fullscreen Placements")]
        public AdmobInterstitialAdIDs[] interstitialIds;
        [Tooltip("Leave empty to use RewardedID as the default placement.")]
        public AdmobFullScreenAdIDs[] rewardedIds;
        [Tooltip("Leave empty to use AppOpenID as the default placement.")]
        public AdmobFullScreenAdIDs[] appOpenIds;
    }

    [Serializable]
    public struct AdmobBannerAdIDs
    {
        public string placementKey;
        public bool enabled;
        public string admobAdUnitId;
        public AdPosition admobPosition;
    }

    [Serializable]
    public struct AdmobInterstitialAdIDs
    {
        public string placementKey;
        public bool enabled;
        public string admobAdUnitId;
    }

    [Serializable]
    public struct AdmobFullScreenAdIDs
    {
        [Tooltip("Shared key used by game code and provider priority fallback.")]
        public string placementKey;
        public bool enabled;
        public string admobAdUnitId;
    }
#endif

#if ACS_APPLOVIN
    [Serializable]
    public struct ApplovinAdIDs
    {
        public string InterstitialAdUnitId;
        public string RewardedAdUnitId;
        public string AppOpenAdUnitId;
        [Header("Named Banner Placements")]
        public ApplovinBannerAdIDs[] bannerIds;
        public ApplovinBannerAdIDs[] bigBannerIds;
        [Header("Named Fullscreen Placements")]
        public ApplovinInterstitialAdIDs[] interstitialIds;
        [Tooltip("Leave empty to use RewardedAdUnitId as the default placement.")]
        public ApplovinFullScreenAdIDs[] rewardedIds;
        [Tooltip("Leave empty to use AppOpenAdUnitId as the default placement.")]
        public ApplovinFullScreenAdIDs[] appOpenIds;
    }

    [Serializable]
    public struct ApplovinBannerAdIDs
    {
        public string placementKey;
        public bool enabled;
        public string appLovinAdUnitId;
        public MaxSdkBase.AdViewPosition appLovinPosition;
        [Tooltip("Only applies to normal AppLovin banners. Disabled uses fixed-size banners; enabled uses adaptive banners.")]
        public bool useAppLovinAdaptiveBanner;
    }

    [Serializable]
    public struct ApplovinInterstitialAdIDs
    {
        public string placementKey;
        public bool enabled;
        public string appLovinAdUnitId;
    }

    [Serializable]
    public struct ApplovinFullScreenAdIDs
    {
        [Tooltip("Shared key used by game code and provider priority fallback.")]
        public string placementKey;
        public bool enabled;
        public string appLovinAdUnitId;
    }
#endif

#if ACS_UNITY_ADS_LEGACY
    [Serializable]
    public struct UnityLegacyPlacementIDs
    {
        [Tooltip("Shared key used by game code and provider priority fallback.")]
        public string placementKey;
        public bool enabled;
        public string placementId;
    }
#endif

#if ACS_FIREBASE
using System.Collections.Generic;
using Firebase.Analytics;
using UnityEngine;
#if ACS_ADMOB
using GoogleMobileAds.Api;
#endif
#if ACS_UNITY_LEVELPLAY
using Unity.Services.LevelPlay;
#endif

public static class AdAnalyticsHelper
{
#if ACS_ADMOB
    public static Parameter[] BuildAdParameters(
        string adFormat,
        string adId,
        string adPosition,
        string adPlatform,
        ResponseInfo responseInfo = null,
        AdValue adValue = null,
        Dictionary<string, object> extraParams = null)
    {
        List<Parameter> parameters = new List<Parameter>
        {
            new Parameter("ad_format", SafeString(adFormat)),
            new Parameter("ad_id", SafeString(adId)),
            new Parameter("ad_position", SafeString(adPosition)),
            new Parameter("ad_platform", SafeString(adPlatform)),
            new Parameter("adapter", GetAdapter(responseInfo)),
            new Parameter("ad_source", GetAdSourceName(responseInfo)),
            new Parameter("ad_source_instance", GetAdSourceInstanceName(responseInfo)),
            new Parameter("response_id", GetResponseId(responseInfo))
        };

        if (adValue != null)
        {
            double revenueValue = adValue.Value / 1000000.0;

            parameters.Add(new Parameter("currency_code", SafeString(adValue.CurrencyCode)));
            parameters.Add(new Parameter("value_micros", adValue.Value));
            parameters.Add(new Parameter("value", revenueValue));
            parameters.Add(new Parameter("precision", adValue.Precision.ToString()));
        }

        if (extraParams != null)
        {
            foreach (var item in extraParams)
            {
                AddExtraParameter(parameters, item.Key, item.Value);
            }
        }

        return parameters.ToArray();
    }

    private static string GetAdapter(ResponseInfo responseInfo)
    {
        if (responseInfo == null)
            return "unknown";

        string adapter = responseInfo.GetMediationAdapterClassName();
        return SafeString(adapter);
    }

    private static string GetResponseId(ResponseInfo responseInfo)
    {
        if (responseInfo == null)
            return "unknown";

        return SafeString(responseInfo.GetResponseId());
    }

    private static string GetAdSourceName(ResponseInfo responseInfo)
    {
        if (responseInfo == null)
            return "unknown";

        string loadedAdapter = responseInfo.GetMediationAdapterClassName();

        if (string.IsNullOrEmpty(loadedAdapter))
            return "unknown";

        foreach (AdapterResponseInfo adapterInfo in responseInfo.GetAdapterResponses())
        {
            if (adapterInfo.AdapterClassName == loadedAdapter)
            {
                if (!string.IsNullOrEmpty(adapterInfo.AdSourceName))
                    return SafeString(adapterInfo.AdSourceName);

                if (!string.IsNullOrEmpty(adapterInfo.AdSourceInstanceName))
                    return SafeString(adapterInfo.AdSourceInstanceName);

                return SafeString(adapterInfo.AdapterClassName);
            }
        }

        return SafeString(loadedAdapter);
    }

    private static string GetAdSourceInstanceName(ResponseInfo responseInfo)
    {
        if (responseInfo == null)
            return "unknown";

        string loadedAdapter = responseInfo.GetMediationAdapterClassName();

        if (string.IsNullOrEmpty(loadedAdapter))
            return "unknown";

        foreach (AdapterResponseInfo adapterInfo in responseInfo.GetAdapterResponses())
        {
            if (adapterInfo.AdapterClassName == loadedAdapter)
            {
                if (!string.IsNullOrEmpty(adapterInfo.AdSourceInstanceName))
                    return SafeString(adapterInfo.AdSourceInstanceName);

                return "unknown";
            }
        }

        return "unknown";
    }
#endif

#if ACS_APPLOVIN
    public static Parameter[] BuildAdParameters(
        string adId,
        string adPosition,
        string adPlatform,
        MaxSdkBase.AdInfo adInfo,
        Dictionary<string, object> extraParams = null)
    {
        List<Parameter> parameters = new List<Parameter>
        {
            new Parameter("ad_format", SafeString(adInfo.AdFormat)),
            new Parameter("ad_id", SafeString(adId)),
            new Parameter("ad_position", SafeString(adPosition)),
            new Parameter("ad_platform", SafeString(adPlatform)),
            new Parameter("adapter", GetAppLovinNetworkName(adInfo)),
            new Parameter("ad_source", GetAppLovinNetworkName(adInfo)),
            new Parameter("ad_source_instance", GetAppLovinNetworkPlacement(adInfo)),
        };

        if (adInfo != null)
        {
            parameters.Add(new Parameter("value", adInfo.Revenue));
            parameters.Add(new Parameter("precision", SafeString(adInfo.RevenuePrecision)));
            parameters.Add(new Parameter("placement", SafeString(adInfo.Placement)));
            parameters.Add(new Parameter("creative_id", SafeString(adInfo.CreativeIdentifier)));
            parameters.Add(new Parameter("dsp_name", SafeString(adInfo.DspName)));
            parameters.Add(new Parameter("latency_millis", adInfo.LatencyMillis));
        }

        if (extraParams != null)
        {
            foreach (var item in extraParams)
            {
                AddExtraParameter(parameters, item.Key, item.Value);
            }
        }

        return parameters.ToArray();
    }

    private static string GetAppLovinNetworkName(MaxSdkBase.AdInfo adInfo)
    {
        return adInfo == null ? "unknown" : SafeString(adInfo.NetworkName);
    }

    private static string GetAppLovinNetworkPlacement(MaxSdkBase.AdInfo adInfo)
    {
        return adInfo == null ? "unknown" : SafeString(adInfo.NetworkPlacement);
    }
#endif

#if ACS_UNITY_LEVELPLAY
    public static Parameter[] BuildLevelPlayParameters(
        string fallbackFormat,
        string fallbackAdUnitId,
        string fallbackPlacement,
        string placementKey,
        LevelPlayAdInfo adInfo = null,
        LevelPlayAdError error = null,
        LevelPlayImpressionData impressionData = null,
        Dictionary<string, object> extraParams = null)
    {
        string adFormat = impressionData?.AdFormat ?? adInfo?.AdFormat ?? fallbackFormat;
        string adUnitId = impressionData?.MediationAdUnitId ?? adInfo?.AdUnitId ?? fallbackAdUnitId;
        string placement = impressionData?.Placement ?? adInfo?.PlacementName ?? fallbackPlacement;
        string network = impressionData?.AdNetwork ?? adInfo?.AdNetwork;
        string instance = impressionData?.InstanceName ?? adInfo?.InstanceName;
        double? revenue = impressionData?.Revenue ?? adInfo?.Revenue;
        string precision = impressionData?.Precision ?? adInfo?.Precision;

        List<Parameter> parameters = new List<Parameter>
        {
            new Parameter("ad_format", SafeString(adFormat)),
            new Parameter("ad_id", SafeString(adUnitId)),
            new Parameter("ad_position", SafeString(placement)),
            new Parameter("ad_platform", Application.platform.ToString()),
            new Parameter("adapter", SafeString(network)),
            new Parameter("ad_source", SafeString(network)),
            new Parameter("ad_source_instance", SafeString(instance)),
            new Parameter("placement_key", SafeString(placementKey))
        };

        if (revenue.HasValue)
            parameters.Add(new Parameter("value", revenue.Value));
        if (!string.IsNullOrWhiteSpace(precision))
            parameters.Add(new Parameter("precision", SafeString(precision)));
        if (error != null)
        {
            parameters.Add(new Parameter("error_code", error.ErrorCode));
            parameters.Add(new Parameter("error_message", SafeString(error.ErrorMessage)));
        }

        if (extraParams != null)
            foreach (KeyValuePair<string, object> item in extraParams)
                AddExtraParameter(parameters, item.Key, item.Value);

        return parameters.ToArray();
    }
#endif

    private static void AddExtraParameter(List<Parameter> parameters, string key, object value)
    {
        if (string.IsNullOrEmpty(key) || value == null)
            return;

        if (value is int intValue)
            parameters.Add(new Parameter(key, intValue));
        else if (value is long longValue)
            parameters.Add(new Parameter(key, longValue));
        else if (value is float floatValue)
            parameters.Add(new Parameter(key, floatValue));
        else if (value is double doubleValue)
            parameters.Add(new Parameter(key, doubleValue));
        else if (value is bool boolValue)
            parameters.Add(new Parameter(key, boolValue ? 1 : 0));
        else
            parameters.Add(new Parameter(key, SafeString(value.ToString())));
    }

    private static string SafeString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "unknown";

        return value.Length > 100 ? value.Substring(0, 100) : value;
    }
}
#endif

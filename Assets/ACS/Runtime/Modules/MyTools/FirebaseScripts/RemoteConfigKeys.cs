public class RemoteConfigKeys
{
    public static string ENABLE_ADS_ANDROID = "enable_ads_android_v" + GetVersion();
    public static string ENABLE_ADS_IOS = "enable_ads_ios_v" + GetVersion();

    static string GetVersion()
    {
        return UnityEngine.Application.version.Replace(".", "_");
    }
}




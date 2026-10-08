#if ACS_APPLE_ATT
using System.Collections;
using UnityEngine;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class ATT_PopupHandler : Singleton<ATT_PopupHandler>
{
    public static bool IsTrackingAuthorizationResolved { get; private set; }

    private bool requestStarted;

    private IEnumerator Start()
    {
        IsTrackingAuthorizationResolved = false;
        yield return null;
        SendTrackingRequest();
    }

    public void SendTrackingRequest()
    {
        if (requestStarted || IsTrackingAuthorizationResolved)
            return;

        requestStarted = true;
        StartCoroutine(RequestTrackingAuthorization());
    }

    private IEnumerator RequestTrackingAuthorization()
    {
#if UNITY_IOS
        var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            yield return null;
            ATTrackingStatusBinding.RequestAuthorizationTracking();
            Debug.Log("RequestAuthorizationTracking");
        }
#else
        Debug.Log("Unity iOS Support: App Tracking Transparency status not checked, because the platform is not iOS.");
#endif

        yield return WaitForTrackingAuthorizationResolution();
        IsTrackingAuthorizationResolved = true;
        yield return null;
        Destroy(gameObject);
    }

    private IEnumerator WaitForTrackingAuthorizationResolution()
    {
#if UNITY_IOS
        while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            yield return null;
        }
#endif
        yield break;
    }
}
#endif

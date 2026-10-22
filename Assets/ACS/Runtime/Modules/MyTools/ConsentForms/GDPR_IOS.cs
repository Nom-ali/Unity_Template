#if ACS_ADMOB_UMP && ACS_APPLE_ATT
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

#if ACS_APPLE_ATT
[RequireComponent(typeof(ATT_PopupHandler))]
#endif
public class GDPR_IOS : MonoBehaviour
{
    public static bool InitilizeAds = false;
    public bool IsConsentFlowComplete { get; private set; }
    ConsentForm _consentForm;

    void Start()
    {
        var debugSettings = new ConsentDebugSettings
        {
            DebugGeography = DebugGeography.EEA,
            TestDeviceHashedIds = new List<string>
            {
                "965E4A26737DF85475A353251709C315"
            }
        };

        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = debugSettings,
        };

        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    void OnConsentInfoUpdated(FormError error)
    {
        if (error != null)
        {
            Debug.LogError(error);
            IsConsentFlowComplete = true;
            ATT_PopupHandler.Instance.SendTrackingRequest();
            return;
        }

        if (ConsentInformation.IsConsentFormAvailable())
        {
            LoadConsentForm();
        }
        else
        {
            IsConsentFlowComplete = true;
            ATT_PopupHandler.Instance.SendTrackingRequest();
        }
    }

    void LoadConsentForm()
    {
        ConsentForm.Load(OnLoadConsentForm);
    }

    void OnLoadConsentForm(ConsentForm consentForm, FormError error)
    {
        if (error != null)
        {
            Debug.LogError(error);
            IsConsentFlowComplete = true;
            ATT_PopupHandler.Instance.SendTrackingRequest();
            return;
        }

        _consentForm = consentForm;

        if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
        {
            _consentForm.Show(OnShowForm);
        }
        else
        {
            IsConsentFlowComplete = true;
            ATT_PopupHandler.Instance.SendTrackingRequest();
        }
    }

    void OnShowForm(FormError error)
    {
        if (error != null)
        {
            Debug.LogError(error);
            IsConsentFlowComplete = true;
            return;
        }

        IsConsentFlowComplete = true;
        ATT_PopupHandler.Instance.SendTrackingRequest();
        LoadConsentForm();
    }
}
#else
using UnityEngine;

public class GDPR_IOS : MonoBehaviour
{
}
#endif





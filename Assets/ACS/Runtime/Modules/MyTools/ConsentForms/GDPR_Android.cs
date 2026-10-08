#if ACS_ADMOB_UMP
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public class GDPR_Android : MonoBehaviour
{
    public static GDPR_Android Instance { get; private set; }
    public static bool IsConsentFlowComplete { get; private set; }

    private ConsentForm consentForm;

    public static void EnsureConsentFlow()
    {
        if (IsConsentFlowComplete || Instance != null)
            return;

        new GameObject("GoogleConsent_Android").AddComponent<GDPR_Android>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        IsConsentFlowComplete = false;

        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ConsentDebugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.EEA,
                TestDeviceHashedIds = new List<string>
                {
                    "965E4A26737DF85475A353251709C315"
                }
            },
#endif
        };

        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    private void OnConsentInfoUpdated(FormError error)
    {
        if (error != null)
        {
            Debug.LogError($"Consent Info Update Error: {error.Message}");
            CompleteConsentFlow();
            return;
        }

        if (ConsentInformation.IsConsentFormAvailable())
        {
            ConsentForm.Load(OnConsentFormLoaded);
        }
        else
        {
            CompleteConsentFlow();
        }
    }

    private void OnConsentFormLoaded(ConsentForm loadedForm, FormError error)
    {
        if (error != null)
        {
            Debug.LogError($"Consent Form Load Error: {error.Message}");
            CompleteConsentFlow();
            return;
        }

        consentForm = loadedForm;
        if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
        {
            consentForm.Show(OnConsentFormShown);
        }
        else
        {
            CompleteConsentFlow();
        }
    }

    private void OnConsentFormShown(FormError error)
    {
        if (error != null)
        {
            Debug.LogError($"Consent Form Show Error: {error.Message}");
        }

        CompleteConsentFlow();
    }

    private static void CompleteConsentFlow()
    {
        IsConsentFlowComplete = true;
        Debug.Log("Google UMP consent flow completed.");
    }
}
#else
using UnityEngine;

public class GDPR_Android : MonoBehaviour
{
}
#endif

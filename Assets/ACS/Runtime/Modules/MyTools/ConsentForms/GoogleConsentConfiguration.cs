using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GoogleConsentConfiguration : MonoBehaviour
{
    [System.Serializable]
    public enum ConsentPlatform
    {
        None,
        Android,
        iOS,
    }
    [SerializeField] private ConsentPlatform consentPlatform = ConsentPlatform.None;

    private GDPR_Android androidConsent;
    private GDPR_IOS iOSConsent;
    private bool setupComplete = false;

    private void ConsentSetup()
    {
        switch (consentPlatform)
        {
            case ConsentPlatform.Android:   // Android specific consent setup
                if (androidConsent != null)
                {
                    Debug.Log("<color=yellow>Google Consent for Android is already set up.</color>");
                    return;
                }
                GameObject androidGameObject = new GameObject("GoogleConsent_Android");
                androidConsent = androidGameObject.AddComponent<GDPR_Android>();
                Debug.Log("<color=green>Setting up Google Consent for Android</color>");
                break;

            case ConsentPlatform.iOS:   // iOS specific consent setup
                if (iOSConsent != null)
                {
                    Debug.Log("<color=yellow>Google Consent for iOS is already set up.</color>");
                    return;
                }
                GameObject iOSGameObject = new GameObject("GoogleConsent_IOS");
                iOSConsent = iOSGameObject.AddComponent<GDPR_IOS>();
                Debug.Log("<color=green>Setting up Google Consent for iOS</color>");
                break;

            default:
                Debug.LogError("Consent Platform not set or unsupported.");
                break;
        }
        setupComplete = true;
    }

    private void Awake()
    {
        // Ensure setup occurs when GameObject is instantiated at runtime
        if (consentPlatform == ConsentPlatform.None && Application.isPlaying)
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                consentPlatform = ConsentPlatform.Android;
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                consentPlatform = ConsentPlatform.iOS;
            }
            else
            {
                Debug.LogError("Consent Platform not set or unsupported.");
                return;
            }
            ConsentSetup();
            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Setup consent for the selected platform
        if (consentPlatform != ConsentPlatform.None && !setupComplete)
        {
            ConsentSetup();
            Debug.Log($"<color=green>Consent Platform set to: {consentPlatform}</color>");

            // Schedule destruction for a safe time using EditorApplication.delayCall
            EditorApplication.delayCall += () => {
                if (this != null && gameObject != null)
                {
                    Debug.Log("Destroying GoogleConsentConfiguration GameObject");
                    DestroyImmediate(gameObject);
                }
            };
        }
    }
#endif
}




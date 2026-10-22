using System;
using System.Collections.Generic;
using UnityEngine;
#if ACS_UGS_ANALYTICS
using Unity.Services.Analytics;
#endif

namespace ACS.UnityServices
{
    public enum AnalyticsConsent { Unknown, Accepted, Declined }
    [DisallowMultipleComponent]
    public sealed class UnityAnalyticsManager : MonoBehaviour
    {
        private const string ConsentKey = "ACS.UnityAnalytics.Consent.v1";
        public bool IsInitialized { get; private set; }
        public bool IsCollecting { get; private set; }
        public AnalyticsConsent Consent
        {
            get
            {
                int value = PlayerPrefs.GetInt(ConsentKey, 0);
                return value == 1 ? AnalyticsConsent.Accepted : value == 2 ? AnalyticsConsent.Declined : AnalyticsConsent.Unknown;
            }
        }
        public event Action PrivacyRequested;
        public string PrivacyUrl
        {
            get
            {
#if ACS_UGS_ANALYTICS
                if (IsInitialized) return AnalyticsService.Instance.PrivacyUrl;
#endif
                return "";
            }
        }
        internal void Initialize()
        {
            IsInitialized = true;
            ApplyConsent();
        }
        public void SetAnalyticsConsent(bool accepted)
        {
            PlayerPrefs.SetInt(ConsentKey, (int)(accepted ? AnalyticsConsent.Accepted : AnalyticsConsent.Declined));
            PlayerPrefs.Save();
            ApplyConsent();
        }
        private void ApplyConsent()
        {
            if (!IsInitialized) return;
#if ACS_UGS_ANALYTICS
            try
            {
                if (Consent == AnalyticsConsent.Accepted) AnalyticsService.Instance.StartDataCollection();
                else AnalyticsService.Instance.StopDataCollection();
                IsCollecting = Consent == AnalyticsConsent.Accepted;
            }
            catch (Exception ex)
            {
                IsCollecting = false;
                Debug.LogWarning("Unity Analytics consent could not be applied: " + ex.Message, this);
            }
#endif
        }
        public void OpenPrivacyChoice() { if (IsInitialized) PrivacyRequested?.Invoke(); }
        public bool RecordEvent(string name, IDictionary<string, object> parameters = null)
        {
            if (!IsInitialized || !IsCollecting || string.IsNullOrWhiteSpace(name)) return false;
#if ACS_UGS_ANALYTICS
            try
            {
                var data = new CustomEvent(name);
                if (parameters != null) foreach (var pair in parameters) data.Add(pair.Key, pair.Value);
                AnalyticsService.Instance.RecordEvent(data);
                return true;
            }
            catch (Exception ex) { Debug.LogWarning("Unity Analytics event: " + ex.Message, this); }
#endif
            return false;
        }
    }
}

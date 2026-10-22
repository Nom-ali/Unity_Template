using System;
using System.Collections;
using UnityEngine;

public enum OfflinePopupMode { TimedReminder, UntilConnected }

[Serializable]
public class InternetConnectivity
{
    internal bool checkInternet = true;
    internal Action OnInternetLost;
    internal Action OnInternetRestored;
    internal OfflinePopupMode Mode = OfflinePopupMode.TimedReminder;
    internal float ReminderInterval = 30f;
    internal float PopupDuration = 3f;
    private bool popupShown;
    private double nextReminder, dismissAt;

    internal void Stop()
    {
        if (!popupShown) return;
        popupShown = false;
        OnInternetRestored?.Invoke();
    }

    internal void UpdateConnectivity(bool offline, double now)
    {
        if (!offline)
        {
            Stop();
            nextReminder = 0;
            return;
        }
        bool persistent = Mode == OfflinePopupMode.UntilConnected;
        if (popupShown && !persistent && now >= dismissAt) Stop();
        if (!popupShown && (persistent || now >= nextReminder))
        {
            popupShown = true;
            nextReminder = now + Mathf.Max(1f, ReminderInterval);
            dismissAt = now + Mathf.Min(Mathf.Max(0.1f, PopupDuration), Mathf.Max(1f, ReminderInterval));
            OnInternetLost?.Invoke();
        }
    }

    internal IEnumerator CheckInternet()
    {
        if (!checkInternet) yield break;
        nextReminder = 0;
        var poll = new WaitForSecondsRealtime(0.25f);
        while (checkInternet)
        {
            UpdateConnectivity(Application.internetReachability == NetworkReachability.NotReachable,
                Time.realtimeSinceStartupAsDouble);
            yield return poll;
        }
        Stop();
    }
}

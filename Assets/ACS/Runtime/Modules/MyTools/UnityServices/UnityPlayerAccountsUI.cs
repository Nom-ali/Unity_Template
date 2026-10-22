using System;
using System.Threading.Tasks;
using UnityEngine;

namespace ACS.UnityServices
{
    [DisallowMultipleComponent]
    public sealed class UnityPlayerAccountsUI : MonoBehaviour
    {
        private UnityServicesManager manager;
        private UnityServicePanels panels;
        private UnityUpdateController updates;
        private bool subscribed;
        private void Awake()
        {
            manager = GetComponent<UnityServicesManager>(); panels = GetComponent<UnityServicePanels>();
            updates = GetComponent<UnityUpdateController>();
        }
        private void OnEnable()
        {
            if (manager && manager.PlayerAccounts && !subscribed)
            { manager.PlayerAccounts.StateChanged += RefreshBusy; subscribed = true; }
        }
        private void Start() { OnEnable(); }
        private void OnDisable()
        {
            if (subscribed && manager && manager.PlayerAccounts) manager.PlayerAccounts.StateChanged -= RefreshBusy;
            subscribed = false;
        }
        public void Show()
        {
            if (!manager || !manager.PlayerAccountsEnabled || !panels ||
                (updates && (!updates.StartupComplete || updates.IsUpdateBlocking)) ||
                (panels.Visible && !ReferenceEquals(panels.DialogOwner, this))) return;
            if (manager.PlayerAccountsStatus != ServiceStatus.Ready)
            { Message("Account unavailable", "Account sign-in is unavailable. Please try again later."); return; }
            if (manager.PlayerAccounts.IsBusy || manager.ProgressSync.IsBusy) { ShowBusy(); return; }
            if (manager.PlayerAccounts.IsLinked)
                Dialog("Your account", "This player is linked to a Unity Player Account.",
                    manager.ProgressSync.IsEnabled ? "Cloud progress" : "Manage account", () => {
                        if (manager.ProgressSync.IsEnabled) ShowCloudProgress();
                        else Application.OpenURL("https://player-account.unity.com"); },
                    "Switch / sign out", ShowSwitchOptions, "Close", Close);
            else
                Dialog("Your account", "You are playing as a guest. Link an account to access this player's cloud saves on your other devices.",
                    "Link account", () => Run(manager.PlayerAccounts.LinkAsync),
                    "Sign in", ConfirmSignIn, "Close", Close);
        }
        private void ShowSwitchOptions()
        {
            Dialog("Account options", "You can sign in to another account or sign out and continue as a new guest.",
                "Sign in", ConfirmSignIn, "Sign out", ConfirmSignOut, "Back", Show);
        }
        private void ConfirmSignIn()
        {
            Dialog("Sign in", "Your current progress will be kept in a local backup. Signing in restores the selected account's cloud progress.",
                "Continue", () => Run(manager.PlayerAccounts.SignInAsync), "Cancel", Show, null, null);
        }
        private void ConfirmSignOut()
        {
            Dialog("Sign out", "Your linked cloud saves stay with your account. Continue as a new guest?",
                "Sign out", () => Run(manager.PlayerAccounts.SignOutAsync), "Cancel", Show, null, null);
        }
        private async void Run(Func<Task<PlayerAccountResult>> operation)
        {
            try
            {
                Dialog("Your account", "Preparing your progress…", null, null, null, null, null, null);
                var task = operation(); RefreshBusy();
                var result = await task;
                if (this && ReferenceEquals(panels.DialogOwner, this)) Message("Your account", result.Message);
            }
            catch (Exception)
            {
                if (this && ReferenceEquals(panels.DialogOwner, this)) Message("Account unavailable", "Please try again.");
            }
        }
        private void RefreshBusy()
        {
            if (!this || !manager || !panels || !ReferenceEquals(panels.DialogOwner, this)) return;
            if (manager.PlayerAccounts.IsBusy) ShowBusy();
        }
        private void ShowBusy()
        {
                Dialog("Your account", manager.PlayerAccounts.CanCancel ? "Complete sign-in in your browser, then return to the game." : "Finishing your account request…",
                    null, null, manager.PlayerAccounts.CanCancel ? "Cancel" : null,
                    manager.PlayerAccounts.CanCancel ? (Action)manager.PlayerAccounts.CancelSignIn : null, null, null);
        }
        private void ShowCloudProgress()
        {
            Dialog("Cloud progress", manager.ProgressSync.LastError ??
                (manager.ProgressSync.IsRestored ? "Progress is ready on this device. Sync before switching devices." : "Cloud progress has not been restored yet."),
                "Sync now", SyncNow, "Manage account", () => Application.OpenURL("https://player-account.unity.com"), "Back", Show);
        }
        private async void SyncNow()
        {
            using (UnityServicePause.Acquire())
            {
                Dialog("Cloud progress", "Synchronizing progress…", null, null, null, null, null, null);
                bool success = await manager.ProgressSync.SynchronizeAsync();
                if (this && ReferenceEquals(panels.DialogOwner, this))
                    Message("Cloud progress", success ? "Your progress is synchronized." : "Synchronization failed. Local progress is kept. Please try again when connected.");
            }
        }
        private void Message(string title, string body)
        { Dialog(title, body, "Account options", Show, "Close", Close, null, null); }
        private void Dialog(string title, string body, string a, Action first, string b, Action second, string c, Action third)
        {
            if (updates && updates.IsUpdateBlocking) return;
            if (panels.Visible && !ReferenceEquals(panels.DialogOwner, this)) return;
            panels.ShowDialog(this, title, body, a, first, b, second, c, third);
        }
        private void Close() { if (ReferenceEquals(panels.DialogOwner, this)) panels.Hide(); }
    }
}

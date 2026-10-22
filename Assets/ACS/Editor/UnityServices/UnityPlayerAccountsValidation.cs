using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ACS.UnityServices;
using UnityEngine;

namespace ACS.Editor
{
    public static class UnityPlayerAccountsValidation
    {
        private static int checks;
        private static void Check(bool passed, string message)
        { if (!passed) throw new Exception("Player Accounts: " + message); checks++; }
        private static void Set(object target, string name, object value)
        { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        private static void Call(object target, string name, params object[] arguments)
        { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments); }
        public static IEnumerator Exercise()
        {
            checks = 0;
            var task = CheckAsync();
            float deadline = Time.realtimeSinceStartup + 15;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            if (!task.IsCompleted) throw new Exception("Account checks timed out.");
            if (task.IsFaulted) throw task.Exception.GetBaseException();
            Debug.Log("ACS_UGS_PLAYER_ACCOUNTS_CHECKS_PASSED assertions=" + checks);
        }
        private static async Task CheckAsync()
        {
            var go = new GameObject("Player Accounts validation"); go.SetActive(false);
            var manager = go.AddComponent<UnityServicesManager>();
            Set(manager, "initializeOnStart", false); Set(manager, "enableAuthentication", false);
            Set(manager, "enableRemoteConfig", false); Set(manager, "enableAnalytics", false);
            Set(manager, "enableCloudSave", false); Set(manager, "enablePlayerAccounts", false);
            string environment = "account-validation-" + Guid.NewGuid().ToString("N");
            Set(manager, "environment", environment);
            go.SetActive(true);
            string ownerKey = "ACS.CloudSave.LocalOwner.v1." + Application.cloudProjectId + "." + environment;
            try
            {
                await manager.InitializeAsync();
                var accounts = manager.PlayerAccounts;
                Check(manager.PlayerAccountsStatus == ServiceStatus.Disabled && !accounts.IsInitialized, "disabled service remains dormant");
                Check((await accounts.LinkAsync()).Outcome == PlayerAccountOutcome.Unavailable, "disabled sign-in never launches browser");
                Set(manager, "enablePlayerAccounts", true);
                Check((await accounts.SignInAsync()).Outcome == PlayerAccountOutcome.Unavailable, "unchecked Authentication blocks account sign-in");
                Set(manager, "enableAuthentication", true);
                Set(manager, "<AuthenticationStatus>k__BackingField", ServiceStatus.Ready);
                Set(manager, "<PlayerAccountsStatus>k__BackingField", ServiceStatus.Ready);
                var backend = new FakeBackend();
                Call(accounts, "Initialize", manager, backend);
                manager.CloudSave.ConfirmAccountDataReady(backend.PlayerId);
                Check(!manager.CloudSave.RequiresAccountRestore, "known guest local data has matching owner");
                int changes = 0;
                bool eventSafe = false;
                accounts.AccountChanged += (previous, current) =>
                {
                    changes++;
                    eventSafe = !accounts.IsBusy && manager.AuthenticationStatus == ServiceStatus.Ready;
                };
                var linked = await accounts.LinkAsync();
                Check(linked.Succeeded && backend.LinkCalls == 1 && accounts.IsLinked, "link succeeds");
                Check(manager.CurrentPlayerId == "guest-original" && changes == 0, "link preserves guest player ID");
                Check(!manager.CloudSave.RequiresAccountRestore, "link preserves cloud write owner");
                int browsers = backend.BrowserCalls;
                Check((await accounts.LinkAsync()).Succeeded && backend.BrowserCalls == browsers, "already-linked player does not open another browser");
                backend.Linked = false; backend.Conflict = true;
                var collision = await accounts.LinkAsync();
                Check(collision.Outcome == PlayerAccountOutcome.AccountAlreadyLinked, "link collision is explicit");
                Check(backend.SignInCalls == 0 && manager.CurrentPlayerId == "guest-original", "collision never force-links or switches identity");
                Check(!manager.CloudSave.RequiresAccountRestore && !accounts.IsBusy, "collision releases operation without changing data owner");
                backend.Conflict = false; backend.PendingBrowser = new TaskCompletionSource<string>();
                var cancelled = accounts.SignInAsync();
                Check(accounts.IsBusy && accounts.CanCancel, "browser flow cancellable");
                var duplicate = await accounts.LinkAsync();
                Check(duplicate.Outcome == PlayerAccountOutcome.Busy, "duplicate flow rejected");
                accounts.CancelSignIn();
                Check((await cancelled).Outcome == PlayerAccountOutcome.Cancelled, "cancellation reported");
                backend.PendingBrowser.TrySetResult("late-fake-token"); await Task.Yield();
                Check(backend.SignInCalls == 0 && manager.CurrentPlayerId == "guest-original", "late browser completion cannot switch account");
                backend.PendingBrowser = null;
                backend.FailSignIn = true;
                Check((await accounts.SignInAsync()).Outcome == PlayerAccountOutcome.Failed, "failed authentication is reported");
                Check(manager.CurrentPlayerId == "guest-original" && manager.AuthenticationStatus == ServiceStatus.Ready, "failed switch preserves/reports original signed-in identity");
                backend.FailSignIn = false;
                // Simulate an in-flight cloud call: sign-in must not even open its browser until it drains.
                Call(manager.CloudSave, "BeginOperation");
                browsers = backend.BrowserCalls;
                var draining = accounts.SignInAsync();
                Check(accounts.IsBusy && backend.BrowserCalls == browsers, "account switch waits for cloud operations");
                accounts.CancelSignIn();
                Check((await draining).Outcome == PlayerAccountOutcome.Cancelled, "waiting for cloud drain can be cancelled safely");
                Call(manager.CloudSave, "EndOperation");
                backend.PendingCommit = new TaskCompletionSource<bool>();
                var switching = accounts.SignInAsync();
                Check(accounts.IsBusy && !accounts.CanCancel && manager.IsAccountTransitionInProgress, "identity commit cannot be cancelled halfway");
                Check(!manager.InitializeAsync().IsCompleted, "other initialization callers wait for identity commit");
                bool rejected = false;
                try { manager.CloudSave.ConfirmAccountDataReady("target-account"); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "cannot acknowledge data during identity change");
                accounts.CancelSignIn();
                backend.PendingCommit.SetResult(true);
                Check((await switching).Succeeded && manager.CurrentPlayerId == "target-account", "existing account sign-in switches player ID");
                Check(changes == 1 && eventSafe, "identity event fires after services are ready and operation gate opens");
                Check(manager.CloudSave.RequiresAccountRestore, "new account cannot inherit old local write ownership");
                Check(PlayerPrefs.GetString(ownerKey) == "guest-original", "ownership fence persists across app sessions");
                rejected = false;
                try { manager.CloudSave.ConfirmAccountDataReady("wrong-player"); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected && manager.CloudSave.RequiresAccountRestore, "wrong-player acknowledgment rejected");
                manager.CloudSave.ConfirmAccountDataReady("target-account");
                Check(!manager.CloudSave.RequiresAccountRestore, "correct explicit restoration acknowledgment releases ownership fence");
                backend.PendingCommit = null;
                Check((await accounts.SignOutAsync()).Succeeded && !accounts.IsLinked, "sign-out creates a guest without unlinking target account");
                Check(changes == 2 && manager.CurrentPlayerId == "new-guest", "sign-out publishes identity change");
                Check(manager.CloudSave.RequiresAccountRestore && PlayerPrefs.GetString(ownerKey) == "target-account", "sign-out cannot upload previous account progress as guest");
                manager.CloudSave.ConfirmAccountDataReady("new-guest");
                Check(!manager.CloudSave.RequiresAccountRestore, "guest data reset can be acknowledged");
                int signouts = backend.SignOutCalls;
                Check((await accounts.SignOutAsync()).Succeeded && backend.SignOutCalls == signouts, "signing out an existing guest is a no-op");
                // The account dialog cannot retain ownership when a mandatory update takes over.
                var panels = go.AddComponent<UnityServicePanels>(); panels.Build();
                object accountDialog = new object();
                panels.ShowDialog(accountDialog, "Account", "Account options", "Link", () => { }, "Close", () => { }, null, null);
                Check(ReferenceEquals(panels.DialogOwner, accountDialog), "account dialog has explicit ownership");
                UpdatePolicy policy; UpdatePolicy.TryCreate("1", "999", "critical", "https://example.com", out policy);
                panels.ShowUpdate(policy, true, () => { }, () => { });
                Check(panels.DialogOwner == null && panels.Visible, "mandatory update takes ownership away from account UI");
                panels.Hide();
            }
            finally
            {
                PlayerPrefs.DeleteKey(ownerKey); PlayerPrefs.Save();
                UnityEngine.Object.Destroy(go);
            }
        }
        private sealed class FakeBackend : IUnityPlayerAccountsBackend
        {
            public bool Signed = true, Linked, Conflict, FailSignIn;
            public string Id = "guest-original";
            public int BrowserCalls, LinkCalls, SignInCalls, SignOutCalls;
            public TaskCompletionSource<string> PendingBrowser;
            public TaskCompletionSource<bool> PendingCommit;
            public bool IsSignedIn => Signed;
            public string PlayerId => Id;
            public bool HasUnityIdentity => Linked;
            public async Task<string> GetTokenAsync(CancellationToken cancellation)
            {
                BrowserCalls++;
                if (PendingBrowser == null) return "fake-token-not-sent-to-Unity";
                using (cancellation.Register(() => PendingBrowser.TrySetCanceled())) return await PendingBrowser.Task;
            }
            public Task LinkAsync(string token)
            {
                LinkCalls++;
                if (Conflict) throw new PlayerAccountLinkConflictException();
                Linked = true; return Task.CompletedTask;
            }
            public async Task SignInAsync(string token)
            {
                SignInCalls++;
                if (FailSignIn) throw new InvalidOperationException("Simulated authentication failure.");
                Signed = false;
                if (PendingCommit != null) await PendingCommit.Task;
                Id = "target-account"; Signed = true; Linked = true;
            }
            public Task SignOutToGuestAsync()
            {
                SignOutCalls++; Id = "new-guest"; Signed = true; Linked = false;
                return Task.CompletedTask;
            }
        }
    }
}

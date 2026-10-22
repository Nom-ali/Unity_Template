using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ACS.UnityServices;
using UnityEngine;
using Prefs = ACS.SaveManager.SaveManager.Prefs;

namespace ACS.Editor
{
    public static class UnityProgressSyncValidation
    {
        private static int checks;
        private static void Check(bool passed, string message)
        { if (!passed) throw new Exception("Progress sync: " + message); checks++; }
        private static void Set(object target, string name, object value)
        { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
        private static object Call(object target, string name, params object[] args)
        { return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args); }
        public static IEnumerator Exercise()
        {
            checks = 0; var task = CheckAsync();
            float deadline = Time.realtimeSinceStartup + 20;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            if (!task.IsCompleted) throw new Exception("Progress checks timed out.");
            if (task.IsFaulted) throw task.Exception.GetBaseException();
            Debug.Log("ACS_UGS_PROGRESS_SYNC_CHECKS_PASSED assertions=" + checks);
        }
        private static async Task CheckAsync()
        {
            var go = new GameObject("Progress sync validation"); go.SetActive(false);
            var manager = go.AddComponent<UnityServicesManager>();
            Set(manager, "initializeOnStart", false); Set(manager, "enableAuthentication", false);
            Set(manager, "enableRemoteConfig", false); Set(manager, "enableAnalytics", false);
            string environment = "sync-validation-" + Guid.NewGuid().ToString("N"); Set(manager, "environment", environment);
            go.SetActive(true); await manager.InitializeAsync();
            var sync = manager.ProgressSync;
            // Keep the tests isolated from real game progress and the network.
            Set(sync, "progressKeys", new[] { "ACS.Test.Level", "ACS.Test.Coins" });
            Set(sync, "progressPrefixes", new[] { "ACS.Test.Skin_" });
            Set(sync, "known", new HashSet<string>(new[] { "ACS.Test.Level", "ACS.Test.Coins" }));
            var backend = new AccountBackend();
            var cloud = new Cloud();
            string scope = "ACS.CloudSave.LocalOwner.v1." + Application.cloudProjectId + "." + environment;
            try
            {
                var pauseType = typeof(UnityServicesManager).Assembly.GetType("ACS.UnityServices.UnityServicePause");
                var acquire = pauseType.GetMethod("Acquire", BindingFlags.Public | BindingFlags.Static);
                float originalTime = Time.timeScale;
                var accountPause = (IDisposable)acquire.Invoke(null, null);
                var criticalPause = (IDisposable)acquire.Invoke(null, null);
                accountPause.Dispose();
                Check(Time.timeScale == 0, "releasing account pause cannot release a concurrent critical pause");
                criticalPause.Dispose();
                Check(Time.timeScale == originalTime, "final pause restores the original time scale");
                bool restoredEventSafe = false;
                sync.ProgressRestored += () => restoredEventSafe = sync.IsRestored && !manager.CloudSave.RequiresAccountRestore;
                Check(!await sync.SynchronizeAsync() && cloud.Reads == 0, "unchecked services remain dormant");
                Set(manager, "enableAuthentication", true); Set(manager, "enableCloudSave", true); Set(manager, "enablePlayerAccounts", true);
                Set(manager, "<AuthenticationStatus>k__BackingField", ServiceStatus.Ready);
                Set(manager, "<PlayerAccountsStatus>k__BackingField", ServiceStatus.Ready);
                Call(manager.PlayerAccounts, "Initialize", manager, backend);
                Call(sync, "Initialize", manager, cloud);
                Prefs.SetInt("ACS.Test.Level", 7); Prefs.SetInt("ACS.Test.Coins", 50);
                Prefs.SetBool("ACS.Test.Skin_red", true); PlayerPrefs.SetInt("ACS.Test.DeviceSetting", 123);
                Check(await sync.SynchronizeAsync(), "first guest progress uploads after successful missing-key read");
                Check(cloud.Writes == 1 && cloud.Level == 7, "existing guest progress is preserved");
                Check(sync.IsRestored && !manager.CloudSave.RequiresAccountRestore, "restoration acknowledges correct player");
                Check(restoredEventSafe, "restore notification observes applied data and acknowledged ownership");
                Check(cloud.Remote.Value.entries.Count == 3, "snapshot contains only configured progress");
                Check(cloud.Remote.Value.entries.Exists(e => e.key == "ACS.Test.Skin_red"), "dynamic prefix writes are tracked");
                Prefs.SetInt("ACS.Test.Level", 8);
                Check(await sync.SynchronizeAsync() && cloud.Level == 8 && cloud.LastLock == "r1", "local edits use the previously read write lock");
                // Changes during upload must remain dirty and be uploaded by a subsequent sync.
                cloud.PendingSave = new TaskCompletionSource<bool>();
                Prefs.SetInt("ACS.Test.Level", 9); var uploading = sync.SynchronizeAsync();
                Check(ReferenceEquals(uploading, sync.SynchronizeAsync()), "concurrent synchronization requests join one task");
                Prefs.SetInt("ACS.Test.Level", 10); cloud.PendingSave.SetResult(true);
                Check(await uploading && cloud.Level == 9, "upload uses an immutable snapshot");
                cloud.PendingSave = null;
                Check(await sync.SynchronizeAsync() && cloud.Level == 10, "edits during upload are not lost");
                Prefs.DeleteKey("ACS.Test.Skin_red");
                Check(await sync.SynchronizeAsync() && !cloud.Remote.Value.entries.Exists(e => e.key == "ACS.Test.Skin_red"),
                    "deleted progress keys are removed from the cloud snapshot");
                Prefs.SetBool("ACS.Test.Skin_red", true); await sync.SynchronizeAsync();
                // Another device advances the cloud revision: cloud wins, and local progress is backed up.
                Prefs.SetInt("ACS.Test.Level", 11); cloud.SetRemote(30, "other-device");
                Check(await sync.SynchronizeAsync() && Prefs.GetInt("ACS.Test.Level") == 30, "newer cloud revision restores instead of overwriting");
                Check(PlayerPrefs.HasKey(scope + ".Progress.guest.BeforeCloudRestore"), "conflicting local progress has a recovery backup");
                Check(PlayerPrefs.GetInt("ACS.Test.DeviceSetting") == 123, "device settings survive restore");
                // Signing into an existing account must apply its cloud data and discard no guest backup.
                backend.OnSignIn = () => cloud.SetRemote(60, "target-r1");
                var signedIn = await manager.PlayerAccounts.SignInAsync();
                Check(signedIn.Succeeded && backend.PlayerId == "existing", "sign-in succeeds");
                Check(Prefs.GetInt("ACS.Test.Level") == 60 && sync.IsRestored, "sign-in automatically restores target cloud progress before completing");
                Check(!Prefs.HasKey("ACS.Test.Skin_red"), "absent target keys reset instead of inheriting guest data");
                Check(PlayerPrefs.HasKey(scope + ".Progress.guest"), "previous guest profile stays backed up");
                Check(!manager.CloudSave.RequiresAccountRestore, "uploads enabled only after target restore");
                // Failed load for a new identity does not upload guest/account data.
                cloud.FailReads = true; int writes = cloud.Writes;
                Check((await manager.PlayerAccounts.SignOutAsync()).Succeeded, "sign-out creates a new guest despite cloud failure");
                Check(Prefs.GetInt("ACS.Test.Level") == 0 && cloud.Writes == writes, "failed target load resets to its local defaults without uploading previous player");
                Check(manager.CloudSave.RequiresAccountRestore && !sync.IsRestored, "failed restore keeps the persistent upload fence");
                Prefs.SetInt("ACS.Test.Level", 3); cloud.FailReads = false; cloud.Remote = null;
                Check(await sync.SynchronizeAsync() && cloud.Level == 3, "reconnection retries and saves this guest's own progress");
                // Invalid snapshots must not erase local data or be treated as missing data.
                cloud.Remote = new CloudSaveValue<ProgressSnapshot> { Value = new ProgressSnapshot { schema = 99 }, WriteLock = "bad" };
                writes = cloud.Writes;
                Check(!await sync.SynchronizeAsync() && cloud.Writes == writes && Prefs.GetInt("ACS.Test.Level") == 3, "unsupported schema preserves local save and performs no upload");
                // A write conflict is never retried as a force write.
                cloud.SetRemote(3, "valid"); await sync.SynchronizeAsync();
                Prefs.SetInt("ACS.Test.Level", 4); cloud.FailWrites = true; writes = cloud.Writes;
                Check(!await sync.SynchronizeAsync() && cloud.Writes == writes + 1, "failed write is reported with no force-write fallback");
                cloud.FailWrites = false; cloud.SetRemote(80, "concurrent-change");
                Check(await sync.SynchronizeAsync() && Prefs.GetInt("ACS.Test.Level") == 80, "retry resolves concurrent update by restoring cloud");
                // Simulate process restart of the sync component, retaining durable offline progress + base revision.
                Prefs.SetInt("ACS.Test.Level", 81);
                Set(sync, "profile", null); Set(sync, "restored", false); Set(sync, "changes", 0);
                Check(await sync.SynchronizeAsync() && cloud.Level == 81, "offline dirty progress and revision survive component restart");
            }
            finally
            {
                foreach (string id in new[] { "guest", "existing", "new-guest" })
                { PlayerPrefs.DeleteKey(scope + ".Progress." + id); PlayerPrefs.DeleteKey(scope + ".Progress." + id + ".BeforeCloudRestore"); }
                PlayerPrefs.DeleteKey(scope); PlayerPrefs.DeleteKey(scope + ".Progress.Keys");
                foreach (string key in new[] { "ACS.Test.Level", "ACS.Test.Coins", "ACS.Test.Skin_red", "ACS.Test.DeviceSetting" }) PlayerPrefs.DeleteKey(key);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
        private sealed class AccountBackend : IUnityPlayerAccountsBackend
        {
            public Action OnSignIn;
            public bool IsSignedIn => true;
            public string PlayerId { get; private set; } = "guest";
            public bool HasUnityIdentity => PlayerId == "existing";
            public Task<string> GetTokenAsync(CancellationToken token) { return Task.FromResult("token"); }
            public Task LinkAsync(string token) { return Task.CompletedTask; }
            public Task SignInAsync(string token) { PlayerId = "existing"; OnSignIn?.Invoke(); return Task.CompletedTask; }
            public Task SignOutToGuestAsync() { PlayerId = "new-guest"; return Task.CompletedTask; }
        }
        private sealed class Cloud : IUnityProgressCloud
        {
            public bool IsReady => true;
            public CloudSaveValue<ProgressSnapshot> Remote;
            public bool FailReads, FailWrites;
            public int Reads, Writes;
            public string LastLock;
            public TaskCompletionSource<bool> PendingSave;
            public int Level => Remote == null ? 0 : int.Parse(Remote.Value.entries.Find(e => e.key == "ACS.Test.Level").value);
            public Task<CloudSaveValue<ProgressSnapshot>> LoadAsync()
            { Reads++; if (FailReads) throw new Exception("Simulated offline"); return Task.FromResult(Remote); }
            public async Task<string> SaveAsync(ProgressSnapshot snapshot, string writeLock)
            {
                Writes++; LastLock = writeLock;
                if (FailWrites) throw new Exception("Simulated write conflict");
                if (PendingSave != null) await PendingSave.Task;
                string revision = "r" + Writes;
                Remote = new CloudSaveValue<ProgressSnapshot> { Value = snapshot, WriteLock = revision }; return revision;
            }
            public void SetRemote(int level, string revision)
            {
                Remote = new CloudSaveValue<ProgressSnapshot> { WriteLock = revision, Value = new ProgressSnapshot {
                    entries = new List<ProgressEntry> { new ProgressEntry { key = "ACS.Test.Level", value = level.ToString() } } } };
            }
        }
    }
}

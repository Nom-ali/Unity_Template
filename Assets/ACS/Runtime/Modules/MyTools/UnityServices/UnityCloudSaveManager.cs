using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if ACS_UGS_CLOUD_SAVE && ACS_UGS_AUTH
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
#endif

namespace ACS.UnityServices
{
    public sealed class CloudSaveValue<T>
    {
        public T Value;
        public string WriteLock;
    }
    /// <summary>Explicit player-data access. Does not replace or automatically upload local saves.</summary>
    [DisallowMultipleComponent]
    public sealed class UnityCloudSaveManager : MonoBehaviour
    {
        private UnityServicesManager owner;
        public bool IsInitialized { get; private set; }
        private int activeOperations;
        private TaskCompletionSource<bool> idle;
        public bool RequiresAccountRestore => owner && PlayerPrefs.HasKey(owner.DataOwnerKey) &&
            PlayerPrefs.GetString(owner.DataOwnerKey) != owner.CurrentPlayerId;
        internal void AttachOwner(UnityServicesManager manager) { owner = manager; }
        internal Task WaitForIdleAsync() { return activeOperations == 0 ? Task.CompletedTask : idle.Task; }
        internal void PreserveDataOwner(string previousPlayer)
        {
            if (!owner || PlayerPrefs.HasKey(owner.DataOwnerKey)) return;
            PlayerPrefs.SetString(owner.DataOwnerKey, string.IsNullOrEmpty(previousPlayer) ? "!unrestored" : previousPlayer);
            PlayerPrefs.Save();
        }
        /// <summary>Call only after applying/resetting this player's local data; reading one key is not enough.</summary>
        public void ConfirmAccountDataReady(string expectedPlayerId)
        {
            if (!owner || owner.AuthenticationStatus != ServiceStatus.Ready || owner.IsAccountTransitionInProgress ||
                (owner.PlayerAccounts && owner.PlayerAccounts.IsBusy) || string.IsNullOrEmpty(expectedPlayerId) ||
                expectedPlayerId != owner.CurrentPlayerId)
                throw new InvalidOperationException("Cannot acknowledge local data for a different or changing player.");
            PlayerPrefs.SetString(owner.DataOwnerKey, expectedPlayerId);
            PlayerPrefs.Save();
        }
        private void BeginOperation()
        {
            if (activeOperations++ == 0) idle = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        private void EndOperation()
        {
            if (--activeOperations == 0) { var pending = idle; idle = null; pending.TrySetResult(true); }
        }
        public bool IsReady
        {
            get
            {
#if ACS_UGS_CLOUD_SAVE && ACS_UGS_AUTH && ACS_UGS_CORE
                return IsInitialized && owner && owner.CloudSaveEnabled && owner.AuthenticationEnabled &&
                    owner.CloudSaveStatus == ServiceStatus.Ready && owner.AuthenticationStatus == ServiceStatus.Ready &&
                    AuthenticationService.Instance.IsSignedIn && !owner.IsAccountTransitionInProgress &&
                    (!owner.PlayerAccounts || !owner.PlayerAccounts.IsBusy);
#else
                return false;
#endif
            }
        }
        internal void Initialize(UnityServicesManager manager)
        {
            owner = manager; IsInitialized = true;
            if (!string.IsNullOrEmpty(owner.CurrentPlayerId)) PreserveDataOwner(owner.CurrentPlayerId);
        }
        private void RequireReady()
        {
            if (!IsReady) throw new InvalidOperationException(
                "Cloud Save is unavailable. Install its SDK, enable Cloud Save and Authentication, and await UnityServicesManager.InitializeAsync().");
        }
        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A Cloud Save key is required.", nameof(key));
        }
        public Task SaveAsync<T>(string key, T value)
        {
            ValidateKey(key);
            return SaveAsync(new Dictionary<string, object> { { key, value } });
        }
        /// <summary>Writes only the supplied keys in the current player's default access class.</summary>
        public async Task SaveAsync(IDictionary<string, object> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Count == 0) throw new ArgumentException("Supply at least one Cloud Save value.", nameof(values));
            var snapshot = new Dictionary<string, object>(values);
            foreach (string key in snapshot.Keys) ValidateKey(key);
            RequireReady();
            if (RequiresAccountRestore) throw new InvalidOperationException(
                "Cloud writes are blocked after an account change. Restore/reset this player's local data and call ConfirmAccountDataReady(playerId).");
            BeginOperation();
            try
            {
#if ACS_UGS_CLOUD_SAVE && ACS_UGS_AUTH && ACS_UGS_CORE
            await CloudSaveService.Instance.Data.Player.SaveAsync(snapshot);
#else
            await Task.CompletedTask;
#endif
            }
            finally { EndOperation(); }
        }
        /// <summary>Returns fallback only for a missing key. Network, authentication, and conversion errors propagate.</summary>
        public async Task<T> LoadAsync<T>(string key, T fallback = default(T))
        {
            ValidateKey(key);
            RequireReady();
            string player = owner.CurrentPlayerId;
            BeginOperation();
            try
            {
#if ACS_UGS_CLOUD_SAVE && ACS_UGS_AUTH && ACS_UGS_CORE
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
            if (owner.CurrentPlayerId != player) throw new InvalidOperationException("Player changed during Cloud Save load.");
            Unity.Services.CloudSave.Models.Item item;
            if (data.TryGetValue(key, out item)) return item.Value.GetAs<T>();
#else
            await Task.CompletedTask;
#endif
            return fallback;
            }
            finally { EndOperation(); }
        }
        public async Task<CloudSaveValue<T>> LoadVersionedAsync<T>(string key)
        {
            ValidateKey(key); RequireReady();
            string player = owner.CurrentPlayerId;
            BeginOperation();
            try
            {
#if ACS_UGS_CLOUD_SAVE && ACS_UGS_AUTH && ACS_UGS_CORE
                var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
                if (owner.CurrentPlayerId != player) throw new InvalidOperationException("Player changed during cloud load.");
                Unity.Services.CloudSave.Models.Item item;
                if (data.TryGetValue(key, out item))
                    return new CloudSaveValue<T> { Value = item.Value.GetAs<T>(), WriteLock = item.WriteLock };
#else
                await Task.CompletedTask;
#endif
                return null;
            }
            finally { EndOperation(); }
        }
        public async Task<string> SaveVersionedAsync<T>(string key, T value, string writeLock)
        {
            ValidateKey(key); RequireReady();
            if (RequiresAccountRestore) throw new InvalidOperationException("Restore this player's data before uploading.");
            BeginOperation();
            try
            {
#if ACS_UGS_CLOUD_SAVE && ACS_UGS_AUTH && ACS_UGS_CORE
                var locks = await CloudSaveService.Instance.Data.Player.SaveAsync(
                    new Dictionary<string, Unity.Services.CloudSave.Models.SaveItem> {
                        { key, new Unity.Services.CloudSave.Models.SaveItem(value, writeLock) } });
                return locks[key];
#else
                await Task.CompletedTask;
                return null;
#endif
            }
            finally { EndOperation(); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LocalPrefs = ACS.SaveManager.SaveManager.Prefs;

namespace ACS.UnityServices
{
    [Serializable]
    public sealed class ProgressEntry { public string key; public string value; }
    [Serializable]
    public sealed class ProgressSnapshot
    {
        public int schema = 1;
        public List<ProgressEntry> entries = new List<ProgressEntry>();
    }
    [Serializable]
    internal sealed class LocalProgressProfile
    {
        public string player;
        public string writeLock;
        public bool dirty;
        public ProgressSnapshot snapshot = new ProgressSnapshot();
    }
    public interface IUnityProgressCloud
    {
        bool IsReady { get; }
        Task<CloudSaveValue<ProgressSnapshot>> LoadAsync();
        Task<string> SaveAsync(ProgressSnapshot snapshot, string writeLock);
    }
    internal sealed class UnityProgressCloud : IUnityProgressCloud
    {
        private readonly UnityCloudSaveManager cloud;
        public const string Key = "acs_progress_v1";
        public UnityProgressCloud(UnityCloudSaveManager cloud) { this.cloud = cloud; }
        public bool IsReady => cloud.IsReady;
        public Task<CloudSaveValue<ProgressSnapshot>> LoadAsync() { return cloud.LoadVersionedAsync<ProgressSnapshot>(Key); }
        public Task<string> SaveAsync(ProgressSnapshot snapshot, string writeLock)
        { return cloud.SaveVersionedAsync(Key, snapshot, writeLock); }
    }

    /// <summary>Account-owned progress only. Device preferences, consent and purchase flags are excluded.</summary>
    [DisallowMultipleComponent]
    public sealed class UnityProgressSync : MonoBehaviour
    {
        [SerializeField] private bool synchronizeProgress = true;
        [SerializeField, Min(2)] private float saveDelaySeconds = 3;
        [SerializeField] private string[] progressKeys = {
            "Coins", "CurrentLevelNo", "UnlockedLevels", "GameCompleted", "RandomLevel", "LastPlayedLevel",
            "FreeMode", "Character", "ModeIndex", "Tutorial_1", "Tutorial_2", "HammerAmount", "Player_Female",
            "Default", "CharacterGender", "CharacterName", "IconIndex"
        };
        [SerializeField] private string[] progressPrefixes = {
            "Level_", "Skin_", "SkinMale_", "SkinFemale_", "SelectedSkin_", "SelectedPlayer_", "Unlocked_"
        };
        private UnityServicesManager owner;
        private IUnityProgressCloud cloud;
        private Task<bool> operation;
        private readonly HashSet<string> known = new HashSet<string>(StringComparer.Ordinal);
        private LocalProgressProfile profile;
        private bool applying, restored;
        private bool refreshRequested = true;
        private float nextAttempt;
        private int changes;
        public bool IsEnabled => synchronizeProgress && owner && owner.CloudSaveEnabled && owner.AuthenticationEnabled;
        public bool IsBusy => operation != null && !operation.IsCompleted;
        public bool IsRestored => restored && profile != null && profile.player == owner.CurrentPlayerId;
        public string LastError { get; private set; }
        public event Action ProgressRestored;
        private string Scope => owner.DataOwnerKey + ".Progress.";
        private string ProfileKey(string player) { return Scope + player; }
        private void Awake()
        {
            owner = GetComponent<UnityServicesManager>();
            LocalPrefs.Changed += Changed; LocalPrefs.Accessed += Accessed;
        }
        private void OnDestroy() { LocalPrefs.Changed -= Changed; LocalPrefs.Accessed -= Accessed; }
        internal void Initialize(UnityServicesManager manager, IUnityProgressCloud implementation = null)
        {
            owner = manager;
            if (implementation != null) cloud = implementation;
            if (cloud == null) cloud = new UnityProgressCloud(owner.CloudSave);
            foreach (string key in progressKeys) known.Add(key);
            foreach (string key in LocalPrefs.AccessedKeys) if (Allowed(key)) known.Add(key);
            var index = PlayerPrefs.GetString(Scope + "Keys", "");
            if (!string.IsNullOrEmpty(index))
            {
                try { foreach (var item in JsonUtility.FromJson<ProgressSnapshot>(index).entries) if (Allowed(item.key)) known.Add(item.key); }
                catch (Exception) { Debug.LogWarning("Progress key index could not be read; configured keys remain available.", this); }
            }
            if (IsEnabled) SaveIndex();
        }
        private bool Allowed(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            foreach (string exact in progressKeys) if (key == exact) return true;
            foreach (string prefix in progressPrefixes)
                if (!string.IsNullOrEmpty(prefix) && key.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }
        private void Accessed(string key)
        {
            if (!IsEnabled || applying || !Allowed(key) || !known.Add(key)) return;
            SaveIndex();
        }
        private void SaveIndex()
        {
            var index = new ProgressSnapshot();
            foreach (string key in known) index.entries.Add(new ProgressEntry { key = key });
            PlayerPrefs.SetString(Scope + "Keys", JsonUtility.ToJson(index));
        }
        private void Changed(string key)
        {
            if (!IsEnabled || applying || !Allowed(key)) return;
            if (known.Add(key)) SaveIndex();
            changes++;
            if (profile != null) { profile.dirty = true; BackupCurrentPlayer(); }
            nextAttempt = Time.realtimeSinceStartup + saveDelaySeconds;
        }
        private ProgressSnapshot Capture()
        {
            var result = new ProgressSnapshot();
            foreach (string key in known)
                if (Allowed(key) && PlayerPrefs.HasKey(key))
                    result.entries.Add(new ProgressEntry { key = key, value = PlayerPrefs.GetString(key) });
            return result;
        }
        private void Validate(ProgressSnapshot snapshot)
        {
            if (snapshot == null || snapshot.schema != 1 || snapshot.entries == null || snapshot.entries.Count > 4096)
                throw new InvalidOperationException("Cloud progress has an unsupported or invalid format.");
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in snapshot.entries)
                if (item == null || !Allowed(item.key) || item.value == null || !unique.Add(item.key))
                    throw new InvalidOperationException("Cloud progress contains invalid or unconfigured keys.");
        }
        private LocalProgressProfile ReadProfile(string player)
        {
            string json = PlayerPrefs.GetString(ProfileKey(player), "");
            if (string.IsNullOrEmpty(json)) return null;
            var cached = JsonUtility.FromJson<LocalProgressProfile>(json);
            if (cached == null || cached.player != player) throw new InvalidOperationException("Invalid local progress owner.");
            Validate(cached.snapshot); return cached;
        }
        internal void BackupCurrentPlayer()
        {
            if (!IsEnabled || profile == null) return;
            profile.snapshot = Capture();
            PlayerPrefs.SetString(ProfileKey(profile.player), JsonUtility.ToJson(profile));
            PlayerPrefs.Save();
        }
        internal void PrepareAccountChange()
        {
            if (!IsEnabled) return;
            string player = owner.CurrentPlayerId;
            if (string.IsNullOrEmpty(player)) return;
            if (profile == null)
            {
                string dataOwner = PlayerPrefs.GetString(owner.DataOwnerKey, player);
                if (dataOwner != player) return;
                profile = ReadProfile(player) ?? new LocalProgressProfile { player = player, dirty = true };
            }
            BackupCurrentPlayer();
        }
        private void Apply(ProgressSnapshot snapshot)
        {
            Validate(snapshot);
            applying = true;
            try
            {
                foreach (string key in known) if (Allowed(key)) PlayerPrefs.DeleteKey(key);
                foreach (var item in snapshot.entries)
                { known.Add(item.key); PlayerPrefs.SetString(item.key, item.value); }
                // A transient level-selection override must not survive an account switch.
                PlayerPrefs.DeleteKey(SharedVariables.RepeatingLevels);
                SaveIndex(); PlayerPrefs.Save();
                var ui = UIManager.Instance;
                ACS.SaveManager.SaveManager.Currency.Value = LocalPrefs.GetInt(SharedVariables.Coins, ui ? ui.DefaultCoins : 10);
                if (ui) ui.UpdateLevelText();
            }
            finally { applying = false; }
        }
        private void NotifyRestored()
        {
            ACS.SaveManager.SaveManager.NotifyProgressRestored();
            var handlers = ProgressRestored;
            if (handlers != null) foreach (Action handler in handlers.GetInvocationList())
                try { handler(); } catch (Exception ex) { Debug.LogException(ex); }
        }
        public Task<bool> SynchronizeAsync()
        {
            if (IsBusy) return operation;
            if (!IsEnabled || cloud == null || !cloud.IsReady || string.IsNullOrEmpty(owner.CurrentPlayerId))
                return Task.FromResult(false);
            operation = SynchronizeCoreAsync(); return operation;
        }
        private async Task<bool> SynchronizeCoreAsync()
        {
            string player = owner.CurrentPlayerId;
            LastError = null;
            bool notify = !IsRestored;
            try
            {
                if (profile == null || profile.player != player)
                {
                    BackupCurrentPlayer();
                    bool sameOwner = PlayerPrefs.GetString(owner.DataOwnerKey, player) == player;
                    profile = ReadProfile(player) ?? new LocalProgressProfile { player = player, dirty = sameOwner };
                    if (sameOwner) { profile.snapshot = Capture(); profile.dirty |= changes > 0; }
                    else Apply(profile.snapshot);
                    restored = false;
                }
                int atRead = changes;
                var remote = await cloud.LoadAsync();
                if (!this || owner.CurrentPlayerId != player) throw new InvalidOperationException("Account changed while loading progress.");
                if (remote != null)
                {
                    Validate(remote.Value);
                    if (string.IsNullOrEmpty(remote.WriteLock)) throw new InvalidOperationException("Cloud progress has no write lock.");
                    bool localPending = profile.dirty || changes != atRead;
                    bool safeOfflineUpload = localPending && profile.writeLock != null && profile.writeLock == remote.WriteLock;
                    if (!safeOfflineUpload)
                    {
                        if (localPending)
                        {
                            BackupCurrentPlayer();
                            PlayerPrefs.SetString(ProfileKey(player) + ".BeforeCloudRestore", JsonUtility.ToJson(profile));
                        }
                        Apply(remote.Value);
                        notify = true;
                        profile.dirty = false;
                    }
                    profile.writeLock = remote.WriteLock;
                }
                else
                {
                    // Missing data is valid only after a successful read. Never use a failed read as an empty save.
                    profile.writeLock = null; profile.dirty = true;
                }
                owner.CloudSave.ConfirmAccountDataReady(player);
                restored = true;
                BackupCurrentPlayer();
                if (notify) NotifyRestored();
                if (profile.dirty)
                {
                    int revision = changes;
                    var snapshot = Capture();
                    profile.writeLock = await cloud.SaveAsync(snapshot, profile.writeLock);
                    if (owner.CurrentPlayerId != player) throw new InvalidOperationException("Account changed while saving progress.");
                    profile.dirty = changes != revision;
                    BackupCurrentPlayer();
                }
                return true;
            }
            catch (Exception ex)
            {
                LastError = "Cloud progress could not be synchronized. Local progress is kept; synchronization will retry.";
                // A write conflict is retried by reading cloud data; never force overwrite a stale revision.
                Debug.LogWarning(LastError + " " + ex.Message, this);
                return false;
            }
            finally { nextAttempt = Time.realtimeSinceStartup + 30; }
        }
        private void Update()
        {
            if (!IsEnabled || cloud == null || !cloud.IsReady || IsBusy || Time.realtimeSinceStartup < nextAttempt) return;
            var updates = GetComponent<UnityUpdateController>();
            if (updates && (!updates.StartupComplete || updates.IsUpdateBlocking)) return;
            if (refreshRequested || !IsRestored || (profile != null && profile.dirty))
            { refreshRequested = false; _ = SynchronizeAsync(); }
        }
        private void OnApplicationFocus(bool focused)
        { if (focused && IsEnabled) { nextAttempt = 0; refreshRequested = true; } }
        private void OnApplicationPause(bool paused)
        { if (paused) { BackupCurrentPlayer(); if (!IsBusy) _ = SynchronizeAsync(); }
            else { nextAttempt = 0; refreshRequested = true; } }
    }

    // Update checks and account changes can overlap. Resume only when both release their pause.
    internal sealed class UnityServicePause : IDisposable
    {
        private static int count;
        private static float previous;
        private bool disposed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { count = 0; }
        public static UnityServicePause Acquire()
        {
            if (count++ == 0) previous = Time.timeScale;
            Time.timeScale = 0;
            return new UnityServicePause();
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (--count == 0) Time.timeScale = previous;
        }
    }
}

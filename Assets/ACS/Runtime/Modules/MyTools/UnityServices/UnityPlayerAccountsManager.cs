using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ACS.UnityServices
{
    public enum PlayerAccountOutcome { Success, Unavailable, Busy, Cancelled, AccountAlreadyLinked, Failed }
    public sealed class PlayerAccountResult
    {
        public PlayerAccountOutcome Outcome { get; private set; }
        public string Message { get; internal set; }
        public bool Succeeded => Outcome == PlayerAccountOutcome.Success;
        public PlayerAccountResult(PlayerAccountOutcome outcome, string message) { Outcome = outcome; Message = message; }
    }
    public sealed class PlayerAccountLinkConflictException : Exception
    { public PlayerAccountLinkConflictException() : base("This account is already linked to another player.") { } }

    /// <summary>SDK boundary; replaceable in deterministic tests without browser or cloud traffic.</summary>
    public interface IUnityPlayerAccountsBackend
    {
        bool IsSignedIn { get; }
        string PlayerId { get; }
        bool HasUnityIdentity { get; }
        Task<string> GetTokenAsync(CancellationToken cancellation);
        Task LinkAsync(string accessToken);
        Task SignInAsync(string accessToken);
        Task SignOutToGuestAsync();
    }

    [DisallowMultipleComponent]
    public sealed class UnityPlayerAccountsManager : MonoBehaviour
    {
        [SerializeField, Min(10)] private float browserTimeoutSeconds = 120;
        private UnityServicesManager owner;
        private IUnityPlayerAccountsBackend backend;
        private CancellationTokenSource cancellation;
        private bool committing;
        private bool preparing;
        public bool IsInitialized => backend != null;
        public bool IsBusy { get; private set; }
        public bool CanCancel => IsBusy && !committing;
        public bool IsLinked => backend != null && backend.IsSignedIn && backend.HasUnityIdentity;
        public string PlayerId => backend != null && backend.IsSignedIn ? backend.PlayerId : "";
        public PlayerAccountResult LastResult { get; private set; }
        public string LastDiagnostic { get; private set; }
        public event Action StateChanged;
        public event Action<string, string> AccountChanged;
        internal void AttachOwner(UnityServicesManager manager) { owner = manager; }
        internal void Initialize(UnityServicesManager manager, IUnityPlayerAccountsBackend implementation = null)
        {
            owner = manager;
            if (implementation != null) backend = implementation;
#if ACS_UGS_PLAYER_ACCOUNTS && ACS_UGS_AUTH && ACS_UGS_CORE
            if (backend == null) backend = new UnityPlayerAccountsBackend();
#endif
        }
        public Task<PlayerAccountResult> LinkAsync() { return RunAsync(0); }
        /// <summary>Explicitly switches accounts; caller must restore/reset local data before acknowledging cloud writes.</summary>
        public Task<PlayerAccountResult> SignInAsync() { return RunAsync(1); }
        /// <summary>Signs out of both SDK sessions and creates a new guest. Does not unlink/delete the old account.</summary>
        public Task<PlayerAccountResult> SignOutAsync() { return RunAsync(2); }
        public void CancelSignIn() { if (CanCancel) cancellation?.Cancel(); }
        private async Task<PlayerAccountResult> RunAsync(int operation)
        {
            if (IsBusy || preparing || (owner && owner.ProgressSync && owner.ProgressSync.IsBusy))
                return new PlayerAccountResult(PlayerAccountOutcome.Busy, "An account or save operation is already in progress. Please try again shortly.");
            if (!owner || !owner.PlayerAccountsEnabled || !owner.AuthenticationEnabled)
                return new PlayerAccountResult(PlayerAccountOutcome.Unavailable, "Account sign-in is unavailable.");
            preparing = true;
            try
            {
                // Flush pending progress before locking cloud access for browser authentication.
                if (owner.ProgressSync.IsEnabled) await owner.ProgressSync.SynchronizeAsync();
            }
            finally { preparing = false; }
            var gameplayPause = UnityServicePause.Acquire();
            IsBusy = true; committing = false; LastDiagnostic = "";
            cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(Mathf.Max(10, browserTimeoutSeconds)));
            string oldPlayer = "", newPlayer = "";
            bool transition = false;
            NotifyState();
            try
            {
                await WaitAsync(owner.InitializeAsync(), cancellation.Token);
                if (!IsInitialized || owner.PlayerAccountsStatus != ServiceStatus.Ready || !backend.IsSignedIn)
                    return SetResult(PlayerAccountOutcome.Unavailable, "Account sign-in is unavailable. Please try again.");
                oldPlayer = backend.PlayerId;
                owner.ProgressSync.PrepareAccountChange();
                if (operation == 0 && backend.HasUnityIdentity)
                    return SetResult(PlayerAccountOutcome.Success, "Your current progress is already linked to an account.");
                if (operation == 2 && !backend.HasUnityIdentity)
                    return SetResult(PlayerAccountOutcome.Success, "You are already playing as a guest.");
                await WaitAsync(owner.CloudSave.WaitForIdleAsync(), cancellation.Token);
                string token = operation == 2 ? null : await backend.GetTokenAsync(cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (operation != 2 && string.IsNullOrEmpty(token)) throw new InvalidOperationException("Player Accounts returned no token.");
                // SDK authentication requests cannot be safely abandoned halfway through a player-ID change.
                committing = true; cancellation.CancelAfter(Timeout.Infinite); NotifyState();
                owner.BeginAccountTransition(); transition = true;
                if (operation != 0) owner.CloudSave.PreserveDataOwner(oldPlayer);
                if (operation == 0) await backend.LinkAsync(token);
                else if (operation == 1) await backend.SignInAsync(token);
                else await backend.SignOutToGuestAsync();
                newPlayer = backend.IsSignedIn ? backend.PlayerId : "";
                if (string.IsNullOrEmpty(newPlayer)) throw new InvalidOperationException("Authentication did not return a player.");
                if (newPlayer != oldPlayer) owner.CloudSave.PreserveDataOwner(oldPlayer);
                return SetResult(PlayerAccountOutcome.Success, operation == 0 ? "Account linked. Your current progress stays with this player." :
                    operation == 1 ? "Signed in. Your account's saved progress can now be restored." : "Signed out. You are now playing as a new guest.");
            }
            catch (PlayerAccountLinkConflictException)
            { return SetResult(PlayerAccountOutcome.AccountAlreadyLinked, "That account already has a player profile. Choose Sign in to access it, or keep playing with your current profile."); }
            catch (OperationCanceledException) when (!committing)
            { return SetResult(PlayerAccountOutcome.Cancelled, "Account sign-in was cancelled or timed out. Your current account has not been changed."); }
            catch (Exception ex)
            {
                LastDiagnostic = ex.Message;
                return SetResult(PlayerAccountOutcome.Failed, "The account operation could not be completed. Please try again.");
            }
            finally
            {
                try
                {
                    if (transition)
                    {
                        newPlayer = backend.IsSignedIn ? backend.PlayerId : "";
                        owner.EndAccountTransition(backend.IsSignedIn);
                        if (newPlayer != oldPlayer) owner.CloudSave.PreserveDataOwner(oldPlayer);
                    }
                    committing = false; IsBusy = false;
                    cancellation.Dispose(); cancellation = null;
                    if (transition && !string.IsNullOrEmpty(newPlayer) && LastResult != null &&
                        (LastResult.Succeeded || newPlayer != oldPlayer) && owner.ProgressSync.IsEnabled)
                    {
                        bool synced = await owner.ProgressSync.SynchronizeAsync();
                        LastResult.Message = synced ? "Your account is ready. Progress is synchronized." :
                            "Your account is connected. Cloud sync is pending; local progress is kept. Retry cloud sync when online.";
                    }
                    NotifyState();
                    if (transition && oldPlayer != newPlayer) NotifyAccountChanged(oldPlayer, newPlayer);
                }
                finally { gameplayPause.Dispose(); }
            }
        }
        private PlayerAccountResult SetResult(PlayerAccountOutcome outcome, string message)
        { return LastResult = new PlayerAccountResult(outcome, message); }
        private static async Task WaitAsync(Task task, CancellationToken token)
        {
            var cancelled = new TaskCompletionSource<bool>();
            using (token.Register(() => cancelled.TrySetResult(true)))
            {
                if (await Task.WhenAny(task, cancelled.Task) != task) throw new OperationCanceledException(token);
                await task;
                token.ThrowIfCancellationRequested();
            }
        }
        private void NotifyState()
        {
            var handlers = StateChanged;
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
                try { handler(); } catch (Exception ex) { Debug.LogException(ex); }
        }
        private void NotifyAccountChanged(string previous, string current)
        {
            var handlers = AccountChanged;
            if (handlers == null) return;
            foreach (Action<string, string> handler in handlers.GetInvocationList())
                try { handler(previous, current); } catch (Exception ex) { Debug.LogException(ex); }
        }
        private void OnDestroy() { if (CanCancel) cancellation?.Cancel(); }
    }
}

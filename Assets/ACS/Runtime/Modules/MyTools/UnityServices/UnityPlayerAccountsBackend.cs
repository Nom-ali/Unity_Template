#if ACS_UGS_PLAYER_ACCOUNTS && ACS_UGS_AUTH && ACS_UGS_CORE
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;

namespace ACS.UnityServices
{
    internal sealed class UnityPlayerAccountsBackend : IUnityPlayerAccountsBackend
    {
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : "";
        private string recoveredPlayerId;
        private bool recoveredUnityIdentity;
        public bool HasUnityIdentity => IsSignedIn &&
            (!string.IsNullOrEmpty(AuthenticationService.Instance.PlayerInfo?.GetUnityId()) ||
                (PlayerId == recoveredPlayerId && recoveredUnityIdentity));
        public async Task<string> GetTokenAsync(CancellationToken cancellation)
        {
            var accounts = PlayerAccountService.Instance;
            // Every user-triggered operation gets a fresh browser flow; no stale browser token is reused.
            accounts.SignOut();
            var completed = new TaskCompletionSource<string>();
            Action success = () => completed.TrySetResult(accounts.AccessToken);
            Action signedOut = () => completed.TrySetCanceled();
            Action<RequestFailedException> failure = error => completed.TrySetException(error);
            accounts.SignedIn += success; accounts.SignedOut += signedOut; accounts.SignInFailed += failure;
            using (cancellation.Register(() => completed.TrySetCanceled()))
            {
                try
                {
                    ObserveBrowserStart(accounts.StartSignInAsync(), completed);
                    return await completed.Task;
                }
                finally
                {
                    accounts.SignedIn -= success; accounts.SignedOut -= signedOut; accounts.SignInFailed -= failure;
                    if (!completed.Task.IsCompletedSuccessfully) accounts.SignOut();
                }
            }
        }
        private static async void ObserveBrowserStart(Task launch, TaskCompletionSource<string> completion)
        {
            try { await launch; }
            catch (Exception ex) { completion.TrySetException(ex); }
        }
        public async Task LinkAsync(string token)
        {
            try { await AuthenticationService.Instance.LinkWithUnityAsync(token); }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            { throw new PlayerAccountLinkConflictException(); }
        }
        public async Task SignInAsync(string token)
        {
            var auth = AuthenticationService.Instance;
            // Retain the old credentials only in this operation's memory, never in custom storage/logs.
            // Some SDK error paths clear the cached session; do not create a new guest on switch failure.
            string previousAccess = auth.AccessToken;
            string previousSession = auth.SessionToken;
            string previousPlayer = auth.PlayerId;
            bool wasLinked = HasUnityIdentity;
            auth.SignOut(false);
            try
            {
                await auth.SignInWithUnityAsync(token);
                recoveredPlayerId = null; recoveredUnityIdentity = false;
            }
            catch
            {
                if (!auth.IsSignedIn)
                {
                    auth.ProcessAuthenticationTokens(previousAccess, previousSession);
                    recoveredPlayerId = previousPlayer; recoveredUnityIdentity = wasLinked;
                }
                throw;
            }
        }
        public async Task SignOutToGuestAsync()
        {
            AuthenticationService.Instance.SignOut(true);
            PlayerAccountService.Instance.SignOut();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
#endif

# Unity Player Accounts

The template supports an optional Unity Player Accounts login on Android, iOS, Windows, and macOS. The Authentication 3.8.1 package already installed in the project includes the Player Accounts SDK; no separate UPA package is required.

## Configure the game

1. Use the same Unity cloud project and production environment for all builds of this game.
2. In the Unity dashboard, open Authentication > Identity Providers > Add Identity Provider > Unity Player Accounts.
3. Register Android, iOS, and PC. PC is also required for Editor Play Mode login.
4. In the Editor, open Project Settings > Services > Authentication and refresh the provider configuration. Then open Services > Unity Player Accounts and configure the correct Client ID. The manager Inspector includes a **Configure Unity Player Accounts** shortcut to the SDK settings.
5. Match the SDK's registered redirects: desktop `http://localhost/callback`, mobile `unitydl://com.unityplayeraccounts.{unity-project-id}` unless deliberately using an SDK-supported custom redirect. Keep the project ID, mobile application identifiers, and dashboard platform configuration consistent.
6. On the `UnityServices` resource prefab, check **Authentication** and **Player Accounts**. Check **Cloud Save** if this game uses it. Player Accounts defaults off and never automatically opens a browser at startup.
7. Enter the Settings panel and select **Account**. Existing guest play and Remote Config continue using anonymous/cached authentication.

Official SDK setup and redirects: https://docs.unity.com/en-us/authentication/unity-player-accounts

## Account actions

- **Link account** opens the system browser, then uses `LinkWithUnityAsync` while keeping the current Unity player ID. Existing cloud saves remain under that same identity. When Cloud Save and progress synchronization are enabled, pending guest progress is synchronized before linking and retried afterward.
- **Sign in** asks for confirmation before opening the browser and switching to the target account with `SignInWithUnityAsync`. This works for a returning account, and the SDK can create a new account if none exists.
- A link collision returns **AccountAlreadyLinked**. The template never force-links, merges, unlinks, or automatically switches accounts. The player can explicitly choose Sign in instead.
- **Sign out** asks for confirmation, signs out of Unity Authentication and the Player Accounts SDK, clears the current session credentials through the SDK, and creates a new anonymous guest. It does not delete/unlink the previous account or its cloud data.
- A linked player's account screen includes **Cloud progress > Sync now** when synchronization is enabled. **Manage account** opens the Unity Player Accounts account portal.
- After the best-effort progress flush, Cancel and a configurable 120-second deadline apply while preparing/opening/waiting for browser sign-in. Once the SDK identity commit starts, cancellation is disabled until it finishes, avoiding an abandoned half-completed switch. Browser closing/back behavior is platform-dependent; cancellation/timeout returns control without applying a late browser result to Authentication.
- Failed sign-in attempts try to restore the prior Authentication identity using the SDK's original credentials retained only in memory for the operation. No access/session tokens are written to template storage or logs. If recovery fails, Authentication reports Failed and cloud calls remain unavailable until recovery/retry.

The account dialog shares the service overlay with explicit ownership. Update/Analytics dialogs can take ownership; a late account result cannot dismiss or replace a required update panel. The Settings button is visible only when the Player Accounts feature is enabled. Missing packages or an unchecked Authentication dependency are reported in the manager Inspector.

## API

Classes live in `ACS.UnityServices`.

```csharp
var services = UnityServicesManager.Instance;
await services.InitializeAsync();

PlayerAccountResult result = await services.PlayerAccounts.LinkAsync();
// Or: SignInAsync(), SignOutAsync(). These are explicit player-triggered operations.
if (!result.Succeeded)
    UnityEngine.Debug.Log(result.Message);
```

`PlayerAccountOutcome` distinguishes Success, Unavailable, Busy, Cancelled, AccountAlreadyLinked, and Failed. `IsLinked`, `PlayerId`, `IsBusy`, `CanCancel`, `LastResult`, and `LastDiagnostic` expose account state; `StateChanged` refreshes UI. `CancelSignIn()` cancels the pre-commit phase. Repeated operations return Busy instead of starting concurrent browser/authentication flows.

`AccountChanged(previousPlayerId, currentPlayerId)` fires only if the Unity player ID changes, after the account operation is no longer busy and service statuses have been synchronized. A successful link preserves the player ID and therefore does not fire this event. A failed recovery can publish an empty current ID; do not attempt cloud restoration until Authentication is ready.

## Cloud data ownership on account changes

`UnityProgressSync` now connects account operations to `SaveManager.Prefs`. Enable **Authentication**, **Cloud Save**, and **Player Accounts** on the UnityServices prefab. The separate Progress Sync component defaults to enabled but remains dormant when Cloud Save or Authentication is unchecked or unavailable.

- Startup fetches `acs_progress_v1` for the authenticated player. UIManager waits up to ten seconds after the update flow; failures preserve local data and allow play. A late successful response can still apply cloud progress and emit `ProgressRestored`.
- Before an account operation, pending progress gets a best-effort synchronization attempt. Guest linking preserves the same player ID. For sign-in to an existing account, that account's cloud snapshot is restored before the sign-in task completes and uploads are acknowledged.
- A successful read with no snapshot starts a new profile: the existing guest keeps its local progress; a different/new guest receives its own cached progress or defaults. A failed read is never interpreted as an empty cloud save.
- Configured `SaveManager.Prefs` changes schedule a save after three unscaled seconds without further changes. Returning to the app requests synchronization. Failed requests retry after thirty seconds while services are available; local changes can schedule an earlier attempt. Pause/background triggers a best-effort flush; mobile termination does not guarantee an upload, so use **Sync now** before changing devices.
- Profiles are backed up locally per Unity project, environment, and player ID. The current local owner marker survives restarts. After changing identities, failed cloud restoration leaves writes blocked; the target's own local cache/defaults can still be used offline.
- Existing cloud records use Unity write locks. Offline local edits upload when their saved base revision still matches the cloud. If another device changed the cloud, cloud progress wins and pending local progress is kept in a `.BeforeCloudRestore` recovery backup. There is no automatic currency/inventory merge. First creation uses the SDK's unlocked create semantics; simultaneous first writes are not a server-side transaction.
- Account browser/commit/restore operations pause gameplay. Service pause ownership is shared with mandatory updates, so finishing account work cannot release a critical-update pause.

### Synchronized data

Default exact keys cover coins, level progression, tutorial completion, hammer count, selected character/mode, and character profile (name/gender/icon). Configured prefixes cover level data, skins, selections, and unlocks. `UnityProgressSync` exposes these lists in the Inspector. Only these keys are included; device audio/vibration settings, Analytics/advertising consent, tokens, purchase receipts, Remove Ads, and bundle flags are excluded. Purchase restoration remains with IAP.

The bridge preserves the raw strings written by `SaveManager.Prefs`; all platform builds must use the same save schema/serialization configuration. Direct Unity `PlayerPrefs.SetInt/SetFloat` calls, JSON save files, external plugins, and custom runtime caches are not automatically serialized by this bridge. Add game-specific progress keys to the lists and write them through `SaveManager.Prefs`. Unity cannot enumerate all legacy PlayerPrefs keys: list existing dynamic keys explicitly during migration if they have never been accessed by this bridge. Prefix keys are tracked as accessed/written and retained in the project's key index.

Currency observers are refreshed after application. Subscribe custom runtime models/UI to `ProgressRestored`; rebuild from `SaveManager.Prefs` when it fires. Avoid retaining previous-player inventory in gameplay objects. The template's event is delivered after local data is applied and ownership is confirmed:

```csharp
var services = UnityServicesManager.Instance;
services.ProgressSync.ProgressRestored += ReloadGameModelsFromLocalPrefs;
bool synchronized = await services.ProgressSync.SynchronizeAsync();
// Unsubscribe the same handler when its owner is destroyed.
```

`ProgressSync.IsBusy`, `IsRestored`, and `LastError` expose status. `CloudSave.RequiresAccountRestore` remains the durable upload fence. The bridge acknowledges it after valid restoration. Games replacing this bridge must call `ConfirmAccountDataReady(expectedPlayerId)` only after restoring/resetting their full local model. Do not use the low-level API to overwrite `acs_progress_v1`, or remove ownership/backup markers independently.

Use the account manager for identity changes; direct SDK profile switching bypasses its operation drain and automatic restoration. Privacy/account data deletion remains game-specific; the account portal is available through Manage account.

Write-lock reference: https://docs.unity.com/en-us/cloud-save/concepts/write-locks

## Testing

The Editor suite uses an injected `IUnityPlayerAccountsBackend` to verify link preservation, collision behavior, cancellation, duplicate rejection, failed switches, operation draining, identity-change notifications, and persisted cloud write ownership without browser access or production cloud writes. The actual SDK adapter is separately compiled with its package guards.

For real integration, configure the provider/Client ID, then verify linking a guest on one device, signing into that account on another platform, restoring its cloud save, sign-out, browser cancellation, failed network requests, and conflicting existing accounts. Native browser redirects and live cloud/account round trips require device/manual validation.

### Verified for this template change

Unity 6000.0.74f1 compiled the actual Player Accounts SDK adapter in a disposable copy of the current project. The existing 45 Play Mode checks and 35 new simulated account-flow checks passed (80 total), along with 35 Editor policy/prefab/scene checks. Eight source compilation variants passed, including absent SDKs, missing Authentication, Player Accounts alone with Authentication, and Player Accounts with Remote Config/Cloud Save. The guest account dialog was rendered at 1080 x 1920 and inspected for text/button layout. Real provider login, native redirects, live identity recovery, and cross-device cloud round trips were not performed.

### Automatic progress synchronization validation

Unity 6000.0.74f1 compiled the final implementation in a disposable project copy. The final run passed 35 Editor checks and 110 Play Mode assertions: 45 existing service/update checks, 35 account-flow checks, and 30 new progress-sync checks. The new suite exercises guest creation, dynamic-key tracking/deletion, automatic existing-account restore, per-player backups, ownership fencing after failed loads, reconnection, local changes during upload, task reuse, cloud revision conflicts, invalid schema, persisted offline revisions, restore notification ordering, and overlapping account/critical-update pauses.

Eight source compile variants passed, including absent packages, missing Authentication, and combined Player Accounts/Remote Config/Cloud Save. The matrix uses small game-facing stubs while the full Unity run compiles the actual SaveManager/UI code and installed SDK adapters. No production cloud writes were made. Live browser sign-in, native redirects, actual network failures, cross-device transfers, and Android/iOS/PC builds remain device/integration checks.

Final local validation log: `/tmp/acs-progress-sync-verified.log`.

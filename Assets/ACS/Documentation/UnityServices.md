# Unity Services and update checks

Classes use the `ACS.UnityServices` namespace.

`UnityServicesManager` independently enables Authentication, Remote Config, Analytics, Cloud Save, and Unity Player Accounts. A service needs both its installed SDK and its Inspector checkbox. Firebase retains its existing manager and selection. Enabling Unity never disables Firebase. LevelPlay and IAP keep their existing owners.

## Setup

The `UnityServices` resource prefab is included in the demo and referenced by the UIManager prefab. It survives scene changes and rejects duplicates. Customize this prefab for projects using the template. Authentication and Remote Config default on; Analytics defaults off. Choose the Unity environment before startup (default `production`). Environment changes require restarting the app.

Remote Config 4.2.5 and Authentication 3.8.1 are in the manifest. Install Analytics using **ACS > Template Setup** (Unity Services > Unity Analytics), then check Enable Analytics on the manager. The ACS symbol manager detects installed assemblies; **ACS > Sync Symbols** can refresh detection. Missing SDKs stay compilable, with PackageMissing status and an Inspector warning. An installed but unchecked service does not get activated by this manager. Unity Services Core may initialize dormant installed SDK internals as part of its own initialization; Analytics collection is separately controlled.

Remote Config requires Authentication checked. If unchecked, it remains WaitingForDependency and no implicit sign-in occurs. Authentication is anonymous. Core initialization and in-progress service operations are shared. Authentication failure does not prevent Analytics consent handling. Overall startup update checks have a ten-second deadline, including Core initialization and authentication.

For game-specific settings use `UnityServicesManager.Instance.RemoteConfig.GetValue<T>(key, fallback)`; bool, int, long, float, double, and string are supported. Getters expose the SDK's current values, including its cache. The update controller exclusively uses the fresh snapshot returned by `FetchAsync`, not these getters. Firebase callers continue using `FirebaseManager.GetRemoteConfigValue<T>`.

## Remote Config dashboard

Link the Unity project to the correct cloud project and enable/configure Remote Config in the selected environment. Create these **string** keys for both platforms:

| Android key | iOS key | Initial example |
|---|---|---|
| android_minimum_version | ios_minimum_version | 1.0 |
| android_latest_version | ios_latest_version | 1.0 |
| android_update_severity | ios_update_severity | normal |
| android_store_url | ios_store_url | Your platform's HTTPS store page |

Use the real Google Play listing URL for Android and the real App Store listing URL for iOS. There is no placeholder URL fallback: an incomplete policy is ignored. Publish the configuration. Verify the target release is available in the platform's store and intended territories before raising enforced version thresholds. Keep Android and iOS rollout values separate.

Versions contain one to four numeric components. `1.0` equals `1.0.0.0`; `1.10` exceeds `1.9`. Below minimum always requires updating. Critical severity requires updating below latest even when minimum is met. Normal severity shows an optional prompt below latest. Latest must be at least minimum. Unknown severities, malformed versions, or missing/invalid HTTPS store URLs cause the policy to be ignored with a diagnostic.

## Startup panels

The UIManager starts its existing internet monitor, then waits for the update gate before entering the main menu. The persistent service canvas builds a dedicated full-screen dialog, with its own listeners and input blocker. Underlying canvases are made non-interactable while the dialog is visible; their prior states are restored when it closes. Update dialogs pause scaled gameplay and restore the prior time scale on release. Game-specific systems that process gameplay without scaled time must honor `UnityUpdateController.IsUpdateBlocking` as well.

Critical panels have only Update. No Cancel/Close, Escape dismissal, automatic timeout, or store-return bypass exists. A fresh permitted policy can release the block; failed, cached, and invalid rechecks cannot release an already active critical panel. The updated installation is re-evaluated on its next launch.

Normal panels offer Update immediately and Close after five seconds of real time. They do not auto-dismiss. Once displayed, they do not appear again that app session, including scene changes and store returns. Closing and relaunching the app starts a new session. Editor enforcement is off by default: enable Enforce In Editor on the prefab/controller and select the platform to test it.

Offline startup and failed/timed-out checks allow play. The existing Check Internet toggle and popup modes remain authoritative for connection notices. The controller retries once when connectivity returns after offline startup. Late startup results do not mutate UI after that check's deadline. An already known mandatory requirement is retained on later failure.

## Analytics privacy

The Analytics checkbox enables the service, not permission to collect. The first-use Accept/Decline dialog appears after update handling and active existing consent flows. Decisions are saved under `ACS.UnityAnalytics.Consent.v1`. Consent is separate from ad consent/ATT. The Analytics Privacy button is added at the bottom of the existing Settings panel when Analytics is ready; it reopens the decision and supports revocation. Privacy policy opens `AnalyticsService.Instance.PrivacyUrl`.

Accepted consent starts collection on Unity 6.0 using StartDataCollection; declined or unknown consent stops/leaves collection dormant. `RecordEvent(name, parameters)` rejects events unless collection is active. Disabled services, missing packages, and Analytics errors never block gameplay. Unity 6.2+ migration requires reviewing the SDK's EndUserConsent integration before upgrading this project's Editor.

## Validation

Run validation only in a disposable project copy: the Play Mode batch entry point intentionally replaces the test scene and exits the Editor.

- `-batchmode -nographics -quit -executeMethod ACS.Editor.UnityServicesValidation.ValidateSourceAndAssets`: deterministic version/policy and prefab-reference checks.
- `-batchmode -nographics -executeMethod ACS.Editor.UnityServicesValidation.RunPlayMode`: real Play Mode checks for disabled services, task reuse, update panel timing/input state, mandatory-policy persistence, normal prompt session state, and saved consent state.

Cloud policy delivery, native store navigation, and device-specific consent dialogs still require configured cloud environments and Android/iOS device checks.

## Firebase source review (unchanged)

- `FirebaseRemoteConfigManager.GetConfigValue<T>` boxes long requests as int and double requests as float. Those casts throw and return fallbacks; large long values also truncate before casting.
- Both Firebase fetch methods advance lastFetchTime before activation completes. An activation failure can cause the next regular request to skip fetching and return true during the local cooldown.
- No Firebase behavior was changed for the Unity service integration.

### Verified in this implementation

Unity 6000.0.74f1 validation ran in a disposable copy of the current working tree, preserving the already-open main Editor session. All four UGS compile paths (Core, Authentication, Remote Config, Analytics) were explicitly enabled for the final full-SDK run.

- 30 Editor policy/asset/scene checks passed.
- 36 Play Mode behavior checks passed, including real-time dismissal at timeScale zero, startup and fetch deadlines, ignored late results, critical-block preservation, background canvas input restoration, and persisted consent values.
- Nine source compilation combinations passed: eight combinations of optional Auth/Remote/Analytics symbols with Core present, plus all SDKs absent. These are source compilation checks, not nine device runs.
- Critical, normal, and Analytics consent dialogs were rendered through Unity at 1080 x 1920 and visually inspected.
- Firebase source was reviewed without edits. Cloud delivery, actual Analytics event transmission, native store navigation, and Android/iOS device behavior were not validated.

## Cloud Save

Cloud Save SDK 3.4.2 (`com.unity.services.cloudsave`) is installed and listed in ACS > Template Setup > Unity Services. Enable **Cloud Save** and **Authentication** on the UnityServices prefab to use it. Cloud Save defaults off. It requires the SDK package and both checkboxes; otherwise its status reports Disabled, PackageMissing, or WaitingForDependency. The manager shares its existing Core initialization, selected environment, and anonymous sign-in. No second login flow is added.

The separate `UnityCloudSaveManager` component exposes `IsInitialized`, `IsReady`, single-value/batch `SaveAsync`, and typed `LoadAsync`. Await initialization and check readiness before using it:

```csharp
using ACS.UnityServices;
using System.Collections.Generic;

var manager = UnityServicesManager.Instance;
await manager.InitializeAsync();
if (!manager.CloudSave.IsReady) return; // Show/retry unavailable cloud access as appropriate.

try
{
    await manager.CloudSave.SaveAsync("current_level", 12);
    int level = await manager.CloudSave.LoadAsync("current_level", 1);
    await manager.CloudSave.SaveAsync(new Dictionary<string, object>
    {
        { "current_level", level },
        { "coins", 250 }
    });
}
catch (System.Exception exception)
{
    // Keep the local progress intact; report or retry the failed operation.
    UnityEngine.Debug.LogWarning(exception.Message);
}
```

These operations use the signed-in player's default access class in the selected environment. Writes update only supplied keys, using last-write-wins SDK semantics; the low-level API does not resolve conflicts itself. The separate progress bridge described below uses versioned reads/writes. `LoadAsync` returns its fallback only when a successful request has no such key. Authentication, unavailable-service, network, SDK-validation, and conversion errors propagate to the caller. Empty keys and null/empty save payloads are rejected before contacting Unity. Stored values must be serializable by the SDK.

The optional progress bridge synchronizes configured SaveManager progress only. Firebase, ads, consent, device settings, and purchase entitlements remain under their existing systems. After explicitly saving, inspect the player's data in the same project's Unity Dashboard Cloud Save service and environment. No private server or Cloud Code is needed for these client player-data APIs. Anonymous accounts are suitable for testing; linking to a recoverable identity is necessary to reliably recover the same saves after reinstall or on a different device.

SDK reference: https://docs.unity.com/en-us/cloud-save/tutorials/unity-sdk

### Cloud Save extension validation

Unity 6000.0.74f1 compilation and 32 Editor checks passed in a temporary copy of the current project. The expanded Play Mode suite passed 45 checks, including disabled Cloud Save, unavailable load/write errors, argument validation, and the explicit Authentication dependency. Five source compilation variants passed: no SDKs, Cloud Save without Authentication, Authentication without Cloud Save, Authentication with Cloud Save, and Remote Config alongside Cloud Save. These checks do not write player data to the cloud. Live cloud round trips and Android/iOS device checks remain to be performed against the linked project.

## Unity Player Accounts integration

See [Unity Player Accounts](UnityPlayerAccounts.md) for provider/Client ID setup, account actions, SDK API, and automatic SaveManager progress synchronization. Enable Authentication and Player Accounts on the resource prefab; the SDK is included in the existing Authentication package. Settings > Account provides linking, confirmed sign-in, sign-out to a guest, and the Unity account portal. The feature defaults off.

After an account change, `CloudSave.RequiresAccountRestore` blocks uploads until the game restores/resets the appropriate local progress and calls `CloudSave.ConfirmAccountDataReady(expectedPlayerId)`. This ownership fence persists across restarts. Existing cloud calls drain before identity commits and new calls are blocked during account operations. The separate UnityProgressSync bridge now performs that restoration automatically for its configured progress keys; custom save files/models require integration. It never merges currencies or inventories.

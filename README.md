# UnityTemplate

UnityTemplate is a Unity 6 mobile-game foundation built around the **ACS** (Application Core System) framework. It provides reusable project setup tooling, UI helpers, monetization adapters, consent flows, analytics hooks, persistence utilities, and common gameplay-support components.

## Requirements

- Unity `6000.0.73f1`
- Universal Render Pipeline `17.0.4`
- Input System `1.19.0`
- Unity IAP `5.3.1`

## Quick start

1. Open the project with the required Unity version.
2. Open the enabled `Demo.unity` scene to explore the ads demonstration.
3. Use **ACS > Template Setup** to configure project information, packages, presets, and in-app purchases.
4. Allow Unity to finish importing and compiling after installing or removing an optional SDK.

## Template Setup

The **ACS > Template Setup** window is the project-facing configuration tool. It contains four areas:

- **Project Setup** – project-level configuration.
- **Packages** – Unity packages and supported third-party integrations.
- **Presets** – Texture Importer, `Image`, and `TextMeshProUGUI` presets.
- **In-App Purchases** – ACS product registry editing, generated product IDs, and Unity IAP catalog synchronization.

The package page supports Unity LevelPlay, AppLovin MAX, Firebase Analytics, Firebase Remote Config, Firebase Crashlytics, and other configured Unity packages. Integrations remain optional: ACS only enables their compile symbols after their corresponding SDK is present.

## ACS modules

### Core and UI

- Input, click/raycast, timer, scene-transition, singleton, object-pool, and utility helpers.
- UI manager, safe-area support, responsive bottom bar, loading panels, settings controls, popups, boosters, and level-selection helpers.
- Panel transitions for fade, scale, directional, fill, and CanvasGroup animation.
- Button, coin, logo, NPC, and UI transfer animations.

### Monetization and consent

- `AdsManager` provides one API for banner, MREC, interstitial, rewarded, and app-open ads.
- Optional provider adapters are included for AdMob, AppLovin MAX, and Unity Ads legacy.
- Consent support includes Google UMP configuration, Android/iOS GDPR helpers, an Android consent gate, and ATT handling.
- The project includes a dedicated ads demo scene and prefabs for the ads and consent managers.

### In-app purchases

- Unity IAP manager and editor support.
- Product-ID and product-catalog registries.
- Reward registry and in-app purchase popup manager.
- Compatibility support for the older IAP manager while projects migrate to the current registry flow.

### Firebase and analytics

- Firebase initialization, analytics events, remote configuration, remote-config keys, and configurable popup delivery.
- Ad analytics bridge for supported ad providers.
- Firebase SDK packages are optional and must be installed through Template Setup before the related ACS features are enabled.

### Gameplay-support utilities

- Save manager with PlayerPrefs and file helpers.
- Shared variables, events, triggers, checkpoints/lives, audio, character selection, screenshot capture, connectivity checks, colours, and development logging.

## Project layout

```text
Assets/
  ACS/
    Editor/                 ACS define synchronization and Template Setup
    Runtime/
      Core/                 Input, timers, scene transitions, extensions
      Modules/MyTools/      Reusable ACS gameplay and service modules
      Prefabs/              Manager, UI, consent, and Firebase prefabs
  Settings/                 URP and template preset assets
  Resources/                Template runtime resources
```

MyBox is bundled as a third-party utility package and is intentionally kept separate from ACS-owned modules.

## Optional SDK status

The source contains adapters for ads and Firebase, but adapter code alone does not mean an SDK is installed. `ACSDefineManager` owns the feature symbols and keeps them aligned with the detected packages/assets. Before shipping, install the required provider SDKs, configure their IDs in the relevant ACS settings, and validate on the target device.

## Validation status

The project includes Unity Test Framework, but no ACS EditMode or PlayMode test suite is currently included. The build scene is an ACS ads demonstration, so production games should add their own boot and gameplay scenes to Build Settings.

## Repository notes

- Keep provider SDKs and their ACS symbols synchronized through the Template Setup workflow.
- Do not commit credentials, signing keys, or production SDK identifiers.
- The `Assets/Backup` folder contains source snapshots created before ACS script changes.
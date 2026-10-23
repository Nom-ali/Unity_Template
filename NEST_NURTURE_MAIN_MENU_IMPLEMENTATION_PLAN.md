# Nest & Nurture Main Menu — Implementation Plan

## Design source

- Superdesign project: `6f4afef3-9708-4839-8b0d-9f3e921adeb6`
- Draft: `22159887-c169-442b-be29-90b226940414`, version 1
- Target composition: 1600 × 900 landscape menu with a warm illustrated nursery, a token counter, one Play action, and six secondary destinations.

The Superdesign HTML is visual reference only. Rebuild its hierarchy with Unity uGUI, sprite assets, TMP assets, and existing ACS behaviours; do not embed browser HTML, remote fonts, or Iconify assets in the game.

## Existing integration points

- `UIManager.cs` starts the UI flow by showing `PanelType.MainMenu` and keeps the manager alive between scenes.
- `UIManagerBase.cs` owns panel transitions, URL routing, scene navigation, button sounds, and the `PanelType` registry.
- `UIManager.prefab` already contains the serialized `MainMenu` panel, `LevelSelection`, `Gameplay`, `LevelComplete`, `LevelFail`, loading/popup panels, a coin bar, and an independent settings panel.
- `ButtonListeners.cs` and `AddListeners.cs` are the existing serialized button-binding path. New menu buttons must use this path rather than persistent Unity button events or custom duplicate listeners.
- `SettingScript.cs` owns music, sound, and vibration controls after the settings panel becomes visible.
- `SafeAreaController.cs` already supports edge insets and banner reservations; attach/configure it on the main menu content root instead of hand-coding notch offsets.
- `IAPButton.cs`, `IAPManager.cs`, and `IAPProductRegistry.asset` are the purchase chain. Current authored products are `RemoveAds`, `Bundle`, `Coin500`, `Coin1000`, `Coin2500`, and `Coin5000`.

## Decisions required before implementation

1. Confirm whether this replaces the template’s current `MainMenu` panel or is a game-specific menu prefab used by a new game scene. This plan assumes replacement only after a game scene and production art are available.
2. Confirm the six destinations. The draft names are Boutique, Settings, Milestones, More Stories, Support, and Privacy. Existing ACS routes cover Settings, More Games, Rate Us, and Privacy Policy; Milestones and Support need explicit game-level destinations.
3. Confirm whether the top-right token counter represents the existing coin currency. If it does, reuse the serialized coin bar/text binding. If it is a separate token economy, add a dedicated persisted currency owner instead of relabelling or reusing coins.
4. Confirm the product/offer mapping for Boutique. Use existing registry products unless new products are deliberately authored through the ACS IAP registry; do not add hard-coded product IDs or fixed store prices in UI code.
5. Provide or approve production art, typography licences, localized copy, and platform URLs. The design’s CSS scenery, title, placeholder balance, and labels are not shipping assets or final product copy.

## Implementation sequence

### 1. Establish the asset package

- Create a game-owned menu art folder separate from ACS runtime assets.
- Add the illustrated nursery background as layered sprites: wall/floor, window/daylight, shelf/books/plant, chair/blanket/cat, blossom/heart particles, title, token, and menu icons.
- Import opaque art as sprites with appropriate compression; keep UI slices at full alpha quality. Use sprite atlases for the menu icon/button families.
- Create TMP font assets from licensed, shipped font files that reproduce the rounded display and body styles. Configure fallback assets for localized glyph coverage.
- Define reusable ScriptableObject or prefab styling for cream tiles, cocoa outlines, colored label strips, and press-state shadows so six menu buttons remain visually consistent.

### 2. Build the uGUI hierarchy

- Create a dedicated landscape Canvas with `CanvasScaler` set to Scale With Screen Size and a 1600 × 900 reference resolution.
- Add a safe-area content root using `SafeAreaController`; preserve room for optional banners through its existing reservation settings rather than overlapping the lower menu row.
- Build ordered layers: static room background, decorative depth layers, main content/title, top-right currency, primary Play button, then the six-button lower navigation row.
- Recreate the designed composition with anchors, layout groups, and sprite slices. Keep decorative sprites non-raycastable; only controls should receive input.
- Use the existing animation components (`DoScale`, `DoFadeCanvas`, or their established equivalents) for menu entrance and button feedback where they match the required behaviour. Avoid adding a second animation framework.

### 3. Replace or mount the main-menu panel safely

- Preserve `PanelType.MainMenu` as the startup route used by `UIManager.cs`.
- If replacing the current serialized main-menu object in `UIManager.prefab`, duplicate the existing panel first, then swap only the `PanelsList` `MainMenu` reference after the new panel is complete.
- Attach `ButtonListeners` to the new panel and configure its `AddListeners` entries in the Inspector. Do not change `UIManagerBase` panel ownership or the existing panel lifecycle.
- Keep the menu inactive in the prefab if the current panel manager expects to reveal it; verify its assigned `AnimationBase` does not conflict with `ShowPanel`.

### 4. Bind actions through existing ACS owners

| Design control | Planned binding | Implementation note |
| --- | --- | --- |
| Play | `LoadSceneBy.Gameplay` | Reuse the existing loading transition and selected production gameplay scene. |
| Boutique | `ShowPanel` for `PanelType.Shop` | First add and serialize an actual Shop panel; until then leave this action deliberately unavailable rather than routing to an unrelated panel. |
| Settings | `ShowPanelInd` for `PanelType.Setting` | Reuse the existing `SettingScript` controls. |
| Milestones | Game-specific panel or scene | Add a dedicated panel type only after its data/progression owner is defined. |
| More Stories | `URL` using `URLs.MoreGames` | Reuse the configured platform link and existing URL routing. |
| Support | Product-owned support route | Add a configured support URL/action; do not overload Privacy Policy or More Games. |
| Privacy | `URL` using `URLs.PrivacyPolicy` | Reuse the configured platform link. |
| Token plus | Boutique/Shop only after Shop exists | Keep it display-only until its destination is serialized and tested. |

### 5. Integrate currency and purchases

- Bind the displayed count to the existing coin UI only if product confirms tokens and coins are the same economy. Otherwise introduce a separate, named UI binding and persistence key owned by the game, not by the visual panel.
- Populate Boutique offers with `IAPButton` components configured against generated `IAPProductType` values from the existing registry.
- Enable localized price labels through `IAPButton`; never display the design’s placeholder prices as production values.
- For `RemoveAds`, use the existing permanent-ownership path and `InAppPopupManager` semantics so the offer hides or disables after a confirmed entitlement/restore.
- Retain `IAPProductRegistry.asset` as the product-authoring source of truth. Regenerate IDs/catalog through the ACS IAP workflow when products change.

### 6. Add responsive and accessibility polish

- Test the landscape layout at the 1600 × 900 reference size plus the supported phone/tablet aspect ratios, including safe-area devices and a visible banner reservation.
- Enforce 44px-equivalent hit targets, TMP overflow handling, and clear selected/disabled states.
- Provide audio-button feedback through the existing `UIManagerBase.OnButtonClicked` path; avoid duplicate sound listeners.
- Localize title, tag line, tile labels, currency formatting, and any legal/support labels before release.

## Validation and handoff

1. Open the target production scene in Unity and verify the `UIManager` has exactly one valid MainMenu reference.
2. Enter Play Mode: startup reaches the menu, Play transitions through the current loading flow, and opening/closing Settings preserves the menu state.
3. Exercise all URL actions on Android and iOS test builds with configured links; verify unsupported/missing Support and Milestones routes fail visibly during development rather than silently doing nothing.
4. Test the currency value after initialization, scene reload, and purchase/reward updates. Confirm the plus route does not open a missing panel.
5. Run store-sandbox purchases and restore: localized prices load, consumables grant once, and Remove Ads remains owned after restart/restore.
6. Validate safe areas, landscape orientation, lower-row banner spacing, accessibility focus, and no UI overlap on target devices.
7. Capture Unity Console and device evidence separately. Static source checks do not prove prefab wiring, Play Mode interactions, platform URLs, or native store behaviour.

## Expected touched areas when implementation is approved

- New game-owned main-menu prefab and art/font assets.
- The serialized main-menu entry in `UIManager.prefab` only if this is the selected integration point.
- Existing settings and panel-manager scripts remain unchanged unless a new Milestones/Support behaviour is explicitly approved.
- The IAP registry changes only when the approved product catalog changes.

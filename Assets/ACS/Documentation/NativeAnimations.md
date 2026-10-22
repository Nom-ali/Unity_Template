# Native ACS animations

ACS animations use Unity directly. No replacement animation package is required.
Existing panel, button, logo, coin, value-transfer, text-bar and settings components keep their script identities, serialized fields and public entry points. Existing scene/prefab files are not rewritten by this migration.

## Parent manager

Add **AnimationManager** (`ACS.Animations`) to a parent. Eligible panel, ButtonAnimations and Logo components on the manager itself and beneath it appear in Managed Animations, including inactive objects. **Ignore Parent** defaults off. Nested managers own their own descendants. Coins and value-transfer effects are deliberately excluded and keep their existing explicit triggers.

Automatic playback and parent deactivation default off. Call `manager.Show()`, `manager.Hide()` or `manager.Play()`; callback overloads accept an `Action`. `Stop()` cancels the group without a success callback. A new Show/Hide replaces and cancels the old group. Pause/Resume suspend an active group. UnityEvents are exposed for Show/Hide start/completion.

Each direction has independent settings:

- **All At Once:** invoke every eligible child in the same frame.
- **Separate Wait For Completion:** invoke the next child after the preceding finite animation finishes.
- **Separate Shared Delay:** invoke children at fixed offsets without waiting or interrupting earlier children. At 0.2 seconds: 0.0, 0.2, 0.4, 0.6. Zero invokes all immediately. Frame-based starts occur on the first update at or after each offset.

All panel scheduling lives in AnimationManager. Each direction has a Start Delay, and Per-Animation Delays provides separate Show/Hide offsets for each managed effect. In parallel/shared mode, a per-animation delay postpones only that effect. In completion mode, it waits after the preceding effect finishes. Standalone Do scripts have no internal delays or child lists. The group completes after all finite animations finish. Infinite decorative loops count as started and continue until Stop, Hide or replacement. A looping DoFill Show does not prevent its finite Hide from completing.

Use Hierarchy Order follows the hierarchy. Turn it off to drag the custom order; refresh retains existing order and appends new discoveries. Reverse Hide Order reverses the resolved order. Children refresh automatically after Editor hierarchy/eligibility edits and again at playback. Ignored children keep independent playback. A direct Show/Hide call on an owned Do script routes to its manager once, with its callback attached to group completion. Ignore Parent bypasses the manager and permits manual calls or Show On Enable. Owned child auto-start is suppressed without changing the saved toggle.

Duration/ease/custom-curve overrides are optional. Scheduling time and overriding child time scale are separate controls. Unchecked Override Child Time Scale preserves each child's authored mode.

**Reduced Motion** defaults off: finite effects apply their final state and complete, and decorative loops remain stopped. Rewards/currency effects are unaffected because the manager does not own them.

## Preparation and interrupted toggles

Every manager Show request first captures/resolves all eligible targets, then applies every starting pose synchronously, before scheduling begins or the group's start event fires. The manager's own animation and inactive children participate. Ignored effects and nested-manager contents are untouched. Preparation neither creates running effect handles nor invokes child On Start/On Complete events. Pending objects hold their prepared values until their scheduled turn; dispatch does not reset or re-resolve their poses.

Fresh Show uses zero scale, starting alpha/fill and the chosen directional offset. DoScale and scaling-enabled DoDirectional expose **Use Custom Starting Scale**, off by default. Enable it to reveal the Starting Scale vector. This changes the fresh Show start only; the authored Show target and zero-scale Hide endpoint stay unchanged. Fresh Hide prepares the fully shown state.

When Show/Hide replaces a playing or paused group, capture current values before cancelling old handles, including any values that decorative Stop cleanup would normally restore. The replacement uses those captured values as its starting state and retains the new direction's configured delays, ordering and duration. Objects hold current values while queued. Successful completion callbacks from cancelled groups are discarded. A fresh Show after group completion prepares its configured starts again. A standalone Do effect follows the same current-value replacement rule.

Preview Show/Hide replacements on the same manager retain the original session snapshot and continue from current values. Stop Preview restores the original snapshot even after many toggles. Switching managers restores the old manager before taking a new snapshot. A new preview session recaptures edited authored targets. Decorative loops are prepared without running and start only on their scheduled turn.

Custom managed components may implement `IManagedAnimationPreparation` alongside `IManagedAnimation`: `CapturePreparation(show, options, preserveCurrentState)` resolves values without mutating visuals, `ApplyPreparation()` applies captured starts without events/playback, and `ClearPreparation()` invalidates pending preparation. The manager completes the capture pass for all participants before its apply pass. Existing custom implementations without the optional interface retain their playback compatibility but cannot be prepared generically.

## Examples

### Popup with parallel fade and scale

Place DoScale and DoFadeCanvas on the popup beneath a manager. Select All At Once for Show and Hide. Leave their own durations/eases, or enable manager overrides. For code-only composition:

```csharp
using ACS.Animations;

AnimationSequence popup = AnimationTimeline.Sequence().SetOwner(this);
popup.Append(transform.AnimateScale(Vector3.one, .25f).SetEase(Ease.OutBack));
popup.Join(canvasGroup.AnimateFade(1, .2f));
popup.OnComplete(OnPopupShown);
```

Helpers are scheduled automatically; fluent settings are read before the first update. Set initial scale/alpha explicitly when needed.

### Completion-based sequence

Configure Show as Separate Wait For Completion, arrange Children, then:

```csharp
manager.Show(() => continueButton.interactable = true);
```

This waits for each finite child and the manager-configured delays. Decorative loops do not block later children.

### Shared-delay menu cascade

Configure Show as Separate Shared Delay, Shared Start Interval = 0.2. Four menu items start at 0, 0.2, 0.4 and 0.6 seconds. A 0.5-second first item overlaps the second and third. Set Hide independently, optionally with Reverse Hide Order.

## Everyday extensions

`AnimateMove`, `AnimateLocalMove`, `AnimateLocalMoveY`, `AnimateAnchoredPosition`/`AnimateAnchorPos`, `AnimateAnchorPosY`, `AnimateRotate`, `AnimateLocalRotate`/`AnimateLocalRotation`, `AnimateScale`, Graphic/CanvasGroup `AnimateFade`, Graphic `AnimateColor`, Image `AnimateFillAmount`, Slider `AnimateValue`, scale `AnimatePunch`, local-position `AnimateShake`.

```csharp
AnimationHandle motion = transform.AnimateLocalMove(Vector3.up * 20, .3f)
    .SetRelative()
    .SetEase(Ease.OutQuad)
    .SetDelay(.1f)
    .SetUpdate(true) // unscaled
    .SetOwner(this)
    .OnComplete(OnArrived);

motion.Pause();
motion.Resume();
// yield return motion.WaitForCompletion();
// motion.Cancel(); // no successful completion
```

Use `SetEase(AnimationCurve)` for a custom curve. `SetLoops(count, LoopType.Restart/Yoyo/Incremental)` uses -1 for infinite loops. Relative endpoints apply to numeric/vector/color property helpers; punch/shake are damped offsets that return to their captured starting state. Cancellation leaves the current pose; the calling component owns any restoration. A cancelled old handle cannot cancel its replacement. Assign SetOwner to cancel when the owner is disabled/destroyed. Target destruction or GameObject deactivation also cancels. CancelAnimations cancels handles targeting or owned by the supplied object.

Avoid overlapping independent writers to the same property. Explicitly cancel your previous handle before replacement; different properties and different targets can run concurrently. A sequence owns finite child tracks; apply looping to the sequence itself. Its clock controls nested tracks. Sequence setup and handle creation allocate; the warmed runner update reuses storage and allocates no collections per frame.

## Compatibility and preview

Existing enum numbers retain saved ease and loop selections. DoScale, DoDirectional, DoFade, DoFadeCanvas and DoFill share one playback/cancellation/event lifecycle. Their own scripts only cache targets, prepare a pose and create the visual effect. Runtime targets are cached once; group discovery runs on refresh/playback, not every frame.

The Do Inspector groups Control, effect settings, Show and Hide. UIManagerBase also prefers the manager when a panel has no root Do script. Each direction has independent On Start and On Complete event toggles. Existing event field names and persistent listeners are preserved. Loop controls appear only for DoFill. MyBox drawers remain available for effect controls. Timing, ordering and per-animation delays live in the manager Inspector.

Legacy ChildList, Callings, InitialDelay and their scheduling coroutines have been removed. Eight existing authored groups in five prefabs were moved to managers, retaining parent-first Show order and reversed Hide order. The migrated manager deactivates its root after Hide where the old parent did so. New managers keep automatic playback and parent deactivation off. Obsolete serialized scheduling data was removed from six prefab assets; existing component IDs, script GUIDs, events and visual settings were retained. Completion-based scheduling deliberately replaces the legacy duration-only waits.

A managed effect on the manager's own GameObject is included unless Ignore Parent is checked. It cannot deactivate the manager early: descendant deactivation waits for all finite effects, and root deactivation is controlled exclusively by Deactivate Parent After Hide. Inactive ancestor containers are activated before a managed Show. Nested managers remain independent ownership boundaries.

DoDirectional preserves screen-based offsets by default; Use Canvas Space Offsets opts into canvas-aware offsets. Reward iteration amounts and timing in GiveCoins are unchanged.

Preview Show/Hide runs in a scene instance or Prefab Mode. Stop Preview restores transforms, active states, UI values and serialized animation settings. Selection changes, script reload, scene saving and entering Play Mode also restore the snapshot. Preview suppresses panel/manager gameplay UnityEvents. The Inspector warns about invalid targets, duplicate/cyclic references, conflicting property writers and infinite loops; it displays shared-delay offsets and runtime status.

Run **ACS > Animations > Run Edit Mode Checks** outside Play Mode in an otherwise idle editor. The check creates temporary test objects and cancels animation handles. The isolated command-line Play Mode entry point opens an empty scene and is intended only for a disposable validation copy, not an unsaved working scene.

Validation used Unity 6000.0.74f1 on macOS in an isolated project copy (194 Edit Mode checks, 168 Play Mode checks, plus real-frame preparation/replacement and overlap checks; 0 managed bytes across 1,000 warmed runner updates): deterministic endpoints, easing, loops, delays, scaled/unscaled time, cancellation/replacement, callback reentrancy, destruction, manager scheduling/ownership, consumer cleanup, preview restoration and warmed runner allocations. Real-frame Play Mode separately checks uninterrupted shared-delay overlap and final completion. An installed DOTween DLL was used only in the disposable validation project for 3,636 numerical easing comparisons (maximum error 1.192093e-7); it is not included here.

Windows/Unity 6000.0.73f1, device/native screenshot integration, reward persistence and full visual equivalence against DOTween are unverified. Coin cancellation/pooling was tested without granting currency. Screenshot animation migration was compiled with a minimal NativeGallery API stub, including the Android callback branch, because the optional plugin is absent. Native screenshot/gallery behavior remains unverified.

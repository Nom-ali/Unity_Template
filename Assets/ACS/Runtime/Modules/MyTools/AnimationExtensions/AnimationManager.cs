using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ACS.Animations
{
    public enum ChildPlaybackMode { AllAtOnce, SeparateWaitForCompletion, SeparateSharedDelay }
    public enum ManagerPlaybackState { Idle, Playing, Paused, Stopping }

    [Serializable]
    public sealed class AnimationGroupSettings
    {
        public ChildPlaybackMode mode;
        [Min(0)] public float startDelay;
        [Min(0)] public float sharedDelay = .2f;
        public bool unscaled = true;
        public bool overrideDuration;
        [Min(0)] public float duration = .3f;
        public bool overrideEase;
        public Ease ease = Ease.OutQuad;
        public bool useCustomCurve;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool overrideChildTimeScale;
    }

    [Serializable]
    public sealed class AnimationChildDelay
    {
        public MonoBehaviour animation;
        [Min(0)] public float showDelay;
        [Min(0)] public float hideDelay;
    }

    /// <summary>Owns scheduling for eligible animations on this object and beneath it.</summary>
    [DefaultExecutionOrder(-150), DisallowMultipleComponent]
    public sealed class AnimationManager : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> children = new();
        [SerializeField] private List<AnimationChildDelay> childDelays = new();
        [SerializeField] private bool useHierarchyOrder = true;
        [SerializeField] private bool playOnEnable;
        [SerializeField] private bool reverseHideOrder = true;
        [SerializeField] private bool deactivateParentAfterHide;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private AnimationGroupSettings showSettings = new();
        [SerializeField] private AnimationGroupSettings hideSettings = new();
        [SerializeField] private UnityEvent onShowStarted = new();
        [SerializeField] private UnityEvent onShowComplete = new();
        [SerializeField] private UnityEvent onHideStarted = new();
        [SerializeField] private UnityEvent onHideComplete = new();

        private sealed class Entry
        {
            public MonoBehaviour Component;
            public IManagedAnimation Animation;
            public float Due;
            public bool Started, Done;
            public bool PreparationValid = true;
        }

        private readonly List<Entry> playing = new();
        private readonly List<Entry> preparing = new();
        private readonly List<MonoBehaviour> discovered = new();
        private readonly List<MonoBehaviour> componentBuffer = new();
        private AnimationHandle clockHandle;
        private AnimationGroupSettings settings;
        private AnimationPlaybackOptions options;
        private float clock;
        private int sequentialIndex, epoch;
        private bool showing, activating;
        private Action completion;

        public static event Action ConfigurationChanged;
        public static void NotifyConfigurationChanged() => ConfigurationChanged?.Invoke();
        public static bool IsPreviewing { get; set; }
        public ManagerPlaybackState State { get; private set; }
        public IReadOnlyList<MonoBehaviour> Children => children;
        public MonoBehaviour CurrentChild => settings != null && settings.mode == ChildPlaybackMode.SeparateWaitForCompletion
            && State != ManagerPlaybackState.Idle && sequentialIndex < playing.Count ? playing[sequentialIndex].Component : null;
        public float GetStartDelay(bool show) => Mathf.Max(0, (show ? showSettings : hideSettings).startDelay);

        private void Awake() => RefreshChildren();
        private void OnEnable()
        {
            RefreshChildren();
            if (playOnEnable && !IsPreviewing && !activating) Show();
        }
        private void OnDisable() { Stop(); ReleaseOwnership(); }
        private void OnDestroy() { Stop(); ReleaseOwnership(); }
        private void OnValidate()
        {
            showSettings ??= new();
            hideSettings ??= new();
            NotifyConfigurationChanged();
        }

        public void RefreshChildren()
        {
            componentBuffer.Clear();
            discovered.Clear();
            GetComponentsInChildren(true, componentBuffer);
            for (int i = 0; i < componentBuffer.Count; i++)
                if (Eligible(componentBuffer[i])) discovered.Add(componentBuffer[i]);

            for (int i = children.Count - 1; i >= 0; i--)
            {
                var component = children[i];
                if (component != null && discovered.Contains(component))
                {
                    if (children.IndexOf(component) != i) children.RemoveAt(i);
                    continue;
                }
                if (component is IManagedAnimation old && old.Controller == this)
                {
                    if (!old.IgnoreParent) old.StopManaged();
                    old.Controller = null;
                }
                children.RemoveAt(i);
            }
            if (useHierarchyOrder) { children.Clear(); children.AddRange(discovered); }
            else
                for (int i = 0; i < discovered.Count; i++)
                    if (!children.Contains(discovered[i])) children.Add(discovered[i]);

            for (int i = 0; i < children.Count; i++)
            {
                var animation = (IManagedAnimation)children[i];
                if (enabled)
                {
                    if (animation.Controller != this && animation.IsAnimating) animation.StopManaged();
                    animation.Controller = this;
                }
                else if (animation.Controller == this) animation.Controller = null;
            }
            SynchronizeDelays();
        }

        private void SynchronizeDelays()
        {
            for (int i = childDelays.Count - 1; i >= 0; i--)
                if (childDelays[i] == null || !children.Contains(childDelays[i].animation)) childDelays.RemoveAt(i);
            for (int i = 0; i < children.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < childDelays.Count; j++)
                    if (childDelays[j].animation == children[i]) { found = true; break; }
                if (!found) childDelays.Add(new AnimationChildDelay { animation = children[i] });
            }
        }

        public float GetChildDelay(MonoBehaviour animation, bool show)
        {
            for (int i = 0; i < childDelays.Count; i++)
                if (childDelays[i].animation == animation)
                    return Mathf.Max(0, show ? childDelays[i].showDelay : childDelays[i].hideDelay);
            return 0;
        }

        private bool Eligible(MonoBehaviour component)
            => component != null && component is IManagedAnimation animation && !animation.IgnoreParent
                && component.GetComponentInParent<AnimationManager>(true) == this;

        private void ReleaseOwnership()
        {
            for (int i = 0; i < children.Count; i++)
                if (children[i] is IManagedAnimation animation && animation.Controller == this) animation.Controller = null;
        }

        public void Show() => Show(null);
        public void Hide() => Hide(null);
        public void Play() => Show();
        public void Show(Action complete) => Begin(true, complete);
        public void Hide(Action complete) => Begin(false, complete);

        private void Begin(bool show, Action complete)
        {
            bool preserveCurrentState = State == ManagerPlaybackState.Playing || State == ManagerPlaybackState.Paused;
            int previousRun = epoch;
            RefreshChildren();
            if (epoch != previousRun) return;
            var nextSettings = show ? showSettings : hideSettings;
            var nextOptions = new AnimationPlaybackOptions
            {
                overrideDuration = nextSettings.overrideDuration, duration = nextSettings.duration,
                overrideEase = nextSettings.overrideEase, ease = nextSettings.ease,
                overrideTimeScale = nextSettings.overrideChildTimeScale, unscaled = nextSettings.unscaled,
                reducedMotion = reducedMotion, useCustomCurve = nextSettings.overrideEase && nextSettings.useCustomCurve,
                curve = nextSettings.curve
            };

            // Resolve every target/offset before cancellation or parent preparation can change
            // the values used by a descendant. Capture never starts playback or sends events.
            preparing.Clear();
            for (int i = 0; i < children.Count; i++)
            {
                if (!Eligible(children[i])) continue;
                var entry = new Entry { Component = children[i], Animation = (IManagedAnimation)children[i] };
                preparing.Add(entry);
                if (entry.Animation is IManagedAnimationPreparation preparation)
                {
                    try { entry.PreparationValid = preparation.CapturePreparation(show, nextOptions, preserveCurrentState); }
                    catch (Exception error) { entry.PreparationValid = false; Debug.LogException(error, entry.Component); }
                }
                if (epoch != previousRun) return;
            }

            StopPlayback(false); // Preserve captured poses even when a decorator's Stop restores its transform.
            int run = previousRun + 1;
            if (epoch != run) return;
            playing.AddRange(preparing);
            preparing.Clear();
            showing = show;
            settings = nextSettings;
            options = nextOptions;
            completion = complete;
            clock = 0;
            sequentialIndex = 0;
            if (!show && reverseHideOrder) playing.Reverse();

            // Prepare inactive effects before activation invokes Awake/OnEnable. Their cached
            // authored targets must not be recaptured from the newly prepared pose.
            for (int i = 0; i < playing.Count; i++)
            {
                var item = playing[i];
                if (item.PreparationValid && item.Animation is IManagedAnimationPreparation preparation)
                {
                    try { preparation.ApplyPreparation(); }
                    catch (Exception error) { item.PreparationValid = false; Debug.LogException(error, item.Component); }
                }
                float interval = settings.mode == ChildPlaybackMode.SeparateSharedDelay ? i * Mathf.Max(0, settings.sharedDelay) : 0;
                item.Due = Mathf.Max(0, settings.startDelay) + interval + GetChildDelay(item.Component, show);
                if (epoch != run) return;
            }
            if (!gameObject.activeSelf)
            {
                activating = true;
                try { gameObject.SetActive(true); }
                finally { activating = false; }
            }
            if (epoch != run || !isActiveAndEnabled) return;

            State = ManagerPlaybackState.Playing;
            if (CanInvokeEvents) (show ? onShowStarted : onHideStarted)?.Invoke();
            if (run != epoch) return;
            clockHandle = new AnimationHandle(this, float.MaxValue, null, _ => Step()).SetEase(Ease.Linear).SetOwner(this);
            DispatchDue();
        }

        private static bool CanInvokeEvents => !IsPreviewing || Application.isPlaying;

        private void Step()
        {
            if (State != ManagerPlaybackState.Playing) return;
            clock += settings.unscaled ? AnimationRunner.LastUnscaledDelta : AnimationRunner.LastScaledDelta;
            for (int i = 0; i < playing.Count; i++)
            {
                var item = playing[i];
                if (item.Started && !item.Done && (item.Component == null || item.Animation.IgnoreParent
                    || item.Animation.Controller != this || !item.Animation.IsAnimating)) item.Done = true;
            }
            DispatchDue();
        }

        private void DispatchDue()
        {
            int run = epoch;
            if (settings.mode == ChildPlaybackMode.SeparateWaitForCompletion)
            {
                while (sequentialIndex < playing.Count)
                {
                    var item = playing[sequentialIndex];
                    if (!item.Started && clock + .000001f >= item.Due) StartEntry(item, run);
                    if (run != epoch || !item.Done) return;
                    ++sequentialIndex;
                    if (sequentialIndex < playing.Count)
                        playing[sequentialIndex].Due = clock + GetChildDelay(playing[sequentialIndex].Component, showing);
                }
            }
            else
            {
                for (int i = 0; i < playing.Count; i++)
                {
                    var item = playing[i];
                    if (!item.Started && clock + .000001f >= item.Due) StartEntry(item, run);
                    if (run != epoch) return;
                }
            }
            if (clock < Mathf.Max(0, settings.startDelay)) return;
            for (int i = 0; i < playing.Count; i++) if (!playing[i].Started || !playing[i].Done) return;
            Finish(run);
        }

        private void StartEntry(Entry item, int run)
        {
            item.Started = true;
            if (!item.PreparationValid) { item.Done = true; return; }
            if (!Eligible(item.Component)) { item.Done = true; return; }
            if (showing) ActivateForShow(item.Component.transform, run);
            if (run != epoch) return;
            if (!item.Component.isActiveAndEnabled || !Eligible(item.Component)) { item.Done = true; return; }
            bool infinite = showing && item.Animation.IsLooping && !reducedMotion
                && !(item.Component is DoFill && options.overrideDuration && options.duration <= 0);
            try { item.Animation.PlayManaged(showing, options, () => { if (run == epoch) item.Done = true; }); }
            catch (Exception error) { item.Done = true; Debug.LogException(error, item.Component); }
            if (infinite) item.Done = true;
        }

        private void ActivateForShow(Transform target, int run)
        {
            if (target == transform || target == null || run != epoch) return;
            ActivateForShow(target.parent, run);
            if (run == epoch && !target.gameObject.activeSelf) target.gameObject.SetActive(true);
        }

        private void Finish(int run)
        {
            State = ManagerPlaybackState.Idle;
            clockHandle?.Cancel();
            clockHandle = null;
            // Do not let a fast parent Hide disable slower descendant effects. The manager's own
            // GameObject is controlled only by Deactivate Parent After Hide.
            if (!showing)
                for (int i = playing.Count - 1; i >= 0; i--)
                {
                    var item = playing[i];
                    if (item.Component is AnimationBase panel && panel.transform != transform
                        && !panel.IgnoreParent && panel.Controller == this) panel.FinishManagedHide();
                    if (run != epoch) return;
                }
            var callback = completion;
            completion = null;
            if (CanInvokeEvents) (showing ? onShowComplete : onHideComplete)?.Invoke();
            if (run != epoch) return;
            callback?.Invoke();
            if (run == epoch && !showing && deactivateParentAfterHide) gameObject.SetActive(false);
        }

        public void Stop() => StopPlayback(true);

        private void StopPlayback(bool clearPreparation)
        {
            int run = ++epoch;
            State = ManagerPlaybackState.Stopping;
            clockHandle?.Cancel();
            clockHandle = null;
            for (int i = 0; i < playing.Count; i++)
            {
                var item = playing[i];
                if (item.Started && item.Component != null && !item.Animation.IgnoreParent && item.Animation.Controller == this)
                    item.Animation.StopManaged();
                if (run != epoch) return;
                if (clearPreparation && item.Animation is IManagedAnimationPreparation preparation) preparation.ClearPreparation();
            }
            playing.Clear();
            completion = null;
            sequentialIndex = 0;
            State = ManagerPlaybackState.Idle;
        }

        public void Pause()
        {
            if (State != ManagerPlaybackState.Playing) return;
            State = ManagerPlaybackState.Paused;
            PauseChildren(true);
        }
        public void Resume()
        {
            if (State != ManagerPlaybackState.Paused) return;
            State = ManagerPlaybackState.Playing;
            PauseChildren(false);
        }
        private void PauseChildren(bool pause)
        {
            for (int i = 0; i < playing.Count; i++)
                if (playing[i].Started && playing[i].Component != null && !playing[i].Animation.IgnoreParent)
                    AnimationRunner.PauseOwner(playing[i].Component, pause);
        }
    }
}

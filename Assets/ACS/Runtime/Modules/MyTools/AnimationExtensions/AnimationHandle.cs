using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ACS.Animations
{
    // Numeric values deliberately match existing serialized ACS ease/loop selections.
    public enum Ease
    {
        Unset = 0, Linear = 1, InSine = 2, OutSine = 3, InOutSine = 4,
        InQuad = 5, OutQuad = 6, InOutQuad = 7, InCubic = 8, OutCubic = 9, InOutCubic = 10,
        InQuart = 11, OutQuart = 12, InOutQuart = 13, InQuint = 14, OutQuint = 15, InOutQuint = 16,
        InExpo = 17, OutExpo = 18, InOutExpo = 19, InCirc = 20, OutCirc = 21, InOutCirc = 22,
        InElastic = 23, OutElastic = 24, InOutElastic = 25, InBack = 26, OutBack = 27, InOutBack = 28,
        InBounce = 29, OutBounce = 30, InOutBounce = 31, Flash = 32, InFlash = 33, OutFlash = 34, InOutFlash = 35
    }
    public enum LoopType { Restart = 0, Yoyo = 1, Incremental = 2 }
    public enum AnimationPlaybackState { Pending, Playing, Paused, Completed, Cancelled }

    public class AnimationHandle
    {
        internal UnityEngine.Object Target;
        internal MonoBehaviour Owner;
        internal bool Nested;
        internal bool Relative;
        internal float duration, delay, elapsed;
        internal int loops = 1;
        internal LoopType loopType;
        internal Ease ease = Ease.OutQuad;
        internal bool unscaled, initialized;
        private Action complete, cancelled;
        private AnimationCurve curve;
        protected Action<float> apply;
        private Action initialize;
        public AnimationPlaybackState State { get; private set; } = AnimationPlaybackState.Pending;
        internal bool active => IsActive;
        public bool IsActive => State != AnimationPlaybackState.Completed && State != AnimationPlaybackState.Cancelled;
        public bool IsComplete => State == AnimationPlaybackState.Completed;
        public bool IsInfinite => loops < 0;
        public float TotalDuration => loops < 0 ? float.PositiveInfinity : delay + duration * Mathf.Max(1, loops);
        internal AnimationHandle(UnityEngine.Object target, float seconds, Action initialize, Action<float> apply)
        { Target = target; duration = Mathf.Max(0, seconds); this.initialize = initialize; this.apply = apply; AnimationRunner.Register(this); }
        public AnimationHandle SetRelative(bool relative = true) { Relative = relative; return this; }
        public AnimationHandle SetEase(Ease value) { ease = value; curve = null; return this; }
        public AnimationHandle SetEase(AnimationCurve value) { curve = value; return this; }
        public AnimationHandle SetDelay(float seconds) { delay = Mathf.Max(0, seconds); return this; }
        public AnimationHandle SetUpdate(bool ignoreTimeScale) { unscaled = ignoreTimeScale; return this; }
        public AnimationHandle SetOwner(MonoBehaviour owner) { Owner = owner; return this; }
        public AnimationHandle SetLoops(int count, LoopType mode = LoopType.Restart) { loops = count == 0 ? 1 : count; loopType = mode; return this; }
        public AnimationHandle OnComplete(Action callback) { if (IsComplete) callback?.Invoke(); else if (IsActive) complete += callback; return this; }
        public AnimationHandle OnCancelled(Action callback) { if (State == AnimationPlaybackState.Cancelled) callback?.Invoke(); else if (IsActive) cancelled += callback; return this; }
        public virtual void Pause() { if (IsActive) State = AnimationPlaybackState.Paused; }
        public virtual void Resume() { if (State == AnimationPlaybackState.Paused) State = AnimationPlaybackState.Playing; }
        public void Cancel()
        {
            if (!IsActive) return;
            State = AnimationPlaybackState.Cancelled;
            var callback = cancelled; complete = cancelled = null;
            OnCancel(); callback?.Invoke();
        }
        public void Kill() => Cancel();
        protected virtual void OnCancel() { }
        public IEnumerator WaitForCompletion() { while (IsActive) yield return null; }
        internal void Tick(float scaledDelta, float unscaledDelta)
        {
            if (!IsActive || State == AnimationPlaybackState.Paused) return;
            if (!ValidateTarget()) return;
            Advance(unscaled ? unscaledDelta : scaledDelta);
        }
        internal bool ValidateTarget()
        {
            if ((Target != null && Target is Component component && !component.gameObject.activeInHierarchy) ||
                (!ReferenceEquals(Target, null) && Target == null) ||
                (!ReferenceEquals(Owner, null) && (Owner == null || !Owner.isActiveAndEnabled))) { Cancel(); return false; }
            return true;
        }
        internal void SeekTimeline(float time)
        {
            if (State == AnimationPlaybackState.Cancelled || State == AnimationPlaybackState.Paused || !ValidateTarget()) return;
            State = AnimationPlaybackState.Playing; elapsed = time; Advance(0);
        }
        internal void Advance(float delta)
        {
            if (!IsActive || State == AnimationPlaybackState.Paused) return;
            elapsed += Mathf.Max(0, delta);
            if (elapsed < delay) return;
            if (!initialized) { initialized = true; initialize?.Invoke(); initialize = null; }
            State = AnimationPlaybackState.Playing;
            float playTime = Mathf.Max(0, elapsed - delay);
            bool finished = duration <= 0 || (loops > 0 && playTime >= duration * loops);
            int cycle = duration <= 0 ? 0 : finished ? Mathf.Max(0, loops - 1) : Mathf.FloorToInt(playTime / duration);
            float t = finished ? 1 : duration <= 0 ? 1 : (playTime - cycle * duration) / duration;
            if (loopType == LoopType.Yoyo && (cycle & 1) != 0) t = 1 - t;
            float value = curve == null ? AnimationEasing.Evaluate(ease, t) : curve.Evaluate(t);
            if (loopType == LoopType.Incremental) value += cycle;
            Apply(value, t, cycle);
            if (finished && IsActive)
            {
                State = AnimationPlaybackState.Completed;
                var callback = complete; complete = cancelled = null;
                callback?.Invoke();
            }
        }
        protected virtual void Apply(float value, float normalized, int cycle) => apply?.Invoke(value);
        internal virtual void Rewind()
        { elapsed = 0; if (State != AnimationPlaybackState.Cancelled) State = AnimationPlaybackState.Pending; }
    }

    // Composition is used by the built-in effects; game-facing helpers use Animate… names.
    public sealed class AnimationSequence : AnimationHandle
    {
        private struct Track { public float Start; public AnimationHandle Handle; }
        private readonly List<Track> tracks = new();
        private float lastStart;
        private int lastCycle;
        internal AnimationSequence() : base(null, 0, null, null) { ease = Ease.Linear; }
        public new AnimationSequence SetUpdate(bool ignoreTimeScale) { base.SetUpdate(ignoreTimeScale); return this; }
        public new AnimationSequence SetOwner(MonoBehaviour owner) { base.SetOwner(owner); return this; }
        public AnimationSequence Append(AnimationHandle child) { lastStart = duration; Add(child, lastStart); return this; }
        internal AnimationSequence Insert(float start, AnimationHandle child) { lastStart=start; Add(child,start);return this; }
        public AnimationSequence Join(AnimationHandle child) { Add(child, lastStart); return this; }
        public AnimationSequence AppendInterval(float seconds) { lastStart = duration; duration += Mathf.Max(0, seconds); return this; }
        private void Add(AnimationHandle child, float start)
        {
            if (child.IsInfinite) child.SetLoops(1,child.loopType);
            child.Nested = true;
            tracks.Add(new Track { Start = start, Handle = child });
            duration = Mathf.Max(duration, start + child.TotalDuration);
        }
        protected override void Apply(float value, float normalized, int cycle)
        {
            if (lastCycle != cycle)
            { for (int i = 0; i < tracks.Count; i++) tracks[i].Handle.Rewind(); lastCycle = cycle; }
            float time = Mathf.Clamp01(value) * duration;
            for (int i = 0; i < tracks.Count; i++)
            {
                if (!IsActive) return;
                var track = tracks[i];
                if (time < track.Start) continue;
                track.Handle.SeekTimeline(time-track.Start);
            }
        }
        public override void Pause() { base.Pause();for(int i=0;i<tracks.Count;i++)tracks[i].Handle.Pause(); }
        public override void Resume() { base.Resume();for(int i=0;i<tracks.Count;i++)tracks[i].Handle.Resume(); }
        internal override void Rewind() { base.Rewind();lastCycle=0;for(int i=0;i<tracks.Count;i++)tracks[i].Handle.Rewind(); }
        protected override void OnCancel() { for (int i = 0; i < tracks.Count; i++) tracks[i].Handle.Cancel(); }
    }
    public static class AnimationTimeline { public static AnimationSequence Sequence() => new AnimationSequence(); }
}

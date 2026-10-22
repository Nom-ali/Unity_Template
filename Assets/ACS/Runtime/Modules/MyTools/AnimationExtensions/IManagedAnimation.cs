using System;
namespace ACS.Animations
{
    [Serializable]
    public struct AnimationPlaybackOptions
    {
        public bool overrideDuration;
        public float duration;
        public bool overrideEase;
        public Ease ease;
        public bool useCustomCurve;
        public UnityEngine.AnimationCurve curve;
        public bool overrideTimeScale;
        public bool unscaled;
        public bool reducedMotion;
    }
    /// <summary>Capture all effects before applying any pose; neither phase starts playback.</summary>
    public interface IManagedAnimationPreparation
    {
        bool CapturePreparation(bool show, AnimationPlaybackOptions options, bool preserveCurrentState);
        void ApplyPreparation();
        void ClearPreparation();
    }
    public interface IManagedAnimation
    {
        bool IgnoreParent { get; }
        bool IsLooping { get; }
        bool IsAnimating { get; }
        AnimationManager Controller { get; set; }
        void PlayManaged(bool show, AnimationPlaybackOptions options, Action complete);
        void StopManaged();
    }
}

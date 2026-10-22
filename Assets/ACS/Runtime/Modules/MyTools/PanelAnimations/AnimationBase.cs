using System;
using ACS.Animations;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Shared lifecycle for a single panel effect. AnimationManager owns group scheduling.</summary>
public abstract class AnimationBase : MonoBehaviour, IManagedAnimation, IManagedAnimationPreparation
{
    [SerializeField] private bool ignoreParent;
    [SerializeField] internal bool PlayOnEnable;
    [SerializeField] internal bool DisableOnHide = true;
    [SerializeField, Min(0)] internal float ShowDuration = .3f;
    [SerializeField, Min(0)] internal float HideDuration = .3f;
    [SerializeField] internal Ease ShowEase = Ease.Unset;
    [SerializeField] internal Ease HideEase = Ease.Unset;
    [SerializeField] internal DefineEvents SelectShowEvents = new();
    [SerializeField] internal DefineEvents SelectHideEvents = new();
    public bool isTimeIndependent;

    private RectTransform cachedRectTransform;
    private AnimationHandle activeAnimation;
    private bool initialized;
    private bool activating;
    private int playbackVersion;
    private bool prepared, preparedShow;

    protected RectTransform rectTransform => cachedRectTransform;
    public bool IgnoreParent => ignoreParent;
    public AnimationManager Controller { get; set; }
    public virtual bool IsLooping => false;
    public bool IsAnimating => activeAnimation != null && activeAnimation.IsActive;

    public virtual void Awake() => Initialize();

    // A new preview session recaptures authored targets; runtime Awake must remain idempotent
    // when an inactive object was already prepared before activation.
    public void InvalidatePreviewTargets()
    {
        if (Application.isPlaying) return;
        initialized = false; prepared = false;
    }

    public void RefreshPreviewTargets()
    {
        if (Application.isPlaying) return;
        initialized = false;
        prepared = false;
        Initialize();
    }

    private bool Initialize()
    {
        if (initialized) return true;
        if (cachedRectTransform == null) cachedRectTransform = GetComponent<RectTransform>();
        if (cachedRectTransform == null || !CacheTarget())
        {
            Debug.LogError(GetType().Name + " requires a RectTransform and its effect target.", this);
            return false;
        }
        initialized = true;
        return true;
    }

    protected abstract bool CacheTarget();
    protected abstract void CaptureAnimation(bool show, bool preserveCurrentState);
    protected abstract void ApplyPreparedAnimation();

    public bool CapturePreparation(bool show, AnimationPlaybackOptions options, bool preserveCurrentState)
    {
        prepared = false;
        if (!Initialize()) return false;
        CaptureAnimation(show, preserveCurrentState);
        preparedShow = show;
        prepared = true;
        return true;
    }

    public void ApplyPreparation()
    {
        if (prepared) ApplyPreparedAnimation();
    }

    public void ClearPreparation() => prepared = false;
    protected abstract AnimationHandle CreateAnimation(bool show, float duration, Ease ease, bool reducedMotion);
    protected virtual void RestoreHiddenState() { }
    protected virtual void ApplyCustomCurve(AnimationHandle animation, AnimationCurve curve) => animation.SetEase(curve);

    private void OnEnable()
    {
        ResolveManager();
        if (PlayOnEnable && !activating && Controller == null && !AnimationManager.IsPreviewing) Show();
    }

    private void OnDisable() => StopManaged();
    private void OnDestroy() => StopManaged();

    /// <summary>Includes a manager on this GameObject. Ignore Parent always leaves the effect independent.</summary>
    public AnimationManager ResolveManager()
    {
        var manager = ignoreParent ? null : GetComponentInParent<AnimationManager>(true);
        Controller = manager != null && manager.enabled ? manager : null;
        return Controller;
    }

    public void Show() => Show(null);
    public void Hide() => Hide(null);
    public void Show(Action complete) => RequestPlayback(true, complete);
    public void Hide(Action complete) => RequestPlayback(false, complete);

    private void RequestPlayback(bool show, Action complete)
    {
        var manager = ResolveManager();
        if (manager != null)
        {
            if (show) manager.Show(complete); else manager.Hide(complete);
            return;
        }
        PlayEffect(show, default, false, complete);
    }

    public void PlayManaged(bool show, AnimationPlaybackOptions options, Action complete)
        => PlayEffect(show, options, true, complete);

    private void PlayEffect(bool show, AnimationPlaybackOptions options, bool managed, Action complete)
    {
        bool usePreparedState = managed && prepared && preparedShow == show;
        if (!usePreparedState && !CapturePreparation(show, options, IsAnimating)) return;
        StopManaged();
        int version = playbackVersion;
        if (!usePreparedState) ApplyPreparation();
        if (show && !gameObject.activeSelf)
        {
            activating = true;
            try { gameObject.SetActive(true); }
            finally { activating = false; }
        }
        if (version != playbackVersion || !isActiveAndEnabled || !Initialize()) return;

        prepared = false;
        var events = show ? SelectShowEvents : SelectHideEvents;
        InvokeEvent(events, EventTypes.OnStart);
        if (version != playbackVersion) return;

        float duration = options.reducedMotion ? 0 : options.overrideDuration ? options.duration : show ? ShowDuration : HideDuration;
        Ease ease = options.overrideEase ? options.ease : show ? ShowEase : HideEase;
        var handle = CreateAnimation(show, Mathf.Max(0, duration), ease, options.reducedMotion);
        if (handle == null) { complete?.Invoke(); return; }
        activeAnimation = handle.SetOwner(this).SetUpdate(options.overrideTimeScale ? options.unscaled : isTimeIndependent);
        if (options.useCustomCurve && options.curve != null) ApplyCustomCurve(activeAnimation, options.curve);
        activeAnimation.OnComplete(() =>
        {
            if (version != playbackVersion) return;
            InvokeEvent(events, EventTypes.OnComplete);
            if (version != playbackVersion) return;
            // Managed effects never deactivate an ancestor while the rest of the group is still running.
            if (!show && !managed && DisableOnHide)
            {
                RestoreHiddenState();
                gameObject.SetActive(false);
                // Unity does not send OnDisable to these components during an Edit Mode preview.
                int expectedVersion = Application.isPlaying ? version + 1 : version;
                if (playbackVersion != expectedVersion) return; // OnDisable may start a replacement.
            }
            complete?.Invoke();
        });
    }

    private static void InvokeEvent(DefineEvents events, EventTypes phase)
    {
        if (events == null || (events.AddEvents & phase) == 0 || (AnimationManager.IsPreviewing && !Application.isPlaying)) return;
        if (phase == EventTypes.OnStart) events.m_OnStart?.Invoke();
        else events.m_OnComplete?.Invoke();
    }

    public void StopManaged()
    {
        ++playbackVersion;
        activeAnimation?.Cancel();
        activeAnimation = null;
    }

    internal void FinishManagedHide()
    {
        if (!DisableOnHide || ignoreParent) return;
        RestoreHiddenState();
        gameObject.SetActive(false);
    }

    protected virtual void OnValidate()
    {
        ShowDuration = Mathf.Max(0, ShowDuration);
        HideDuration = Mathf.Max(0, HideDuration);
        SelectShowEvents ??= new();
        SelectHideEvents ??= new();
        AnimationManager.NotifyConfigurationChanged();
    }
}

[Serializable]
public class DefineEvents
{
    [SerializeField] internal EventTypes AddEvents = EventTypes.None;
    public UnityEvent m_OnStart = new();
    public UnityEvent m_OnComplete = new();
}

[Serializable, Flags]
public enum EventTypes { None = 0, OnStart = 1, OnComplete = 2 }

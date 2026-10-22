using ACS.Animations;
using UnityEngine;

public class ButtonAnimations : MonoBehaviour, IManagedAnimation, IManagedAnimationPreparation
{


    [SerializeField] private AnimationStyle animationStyle = AnimationStyle.Jelly;

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Breathing)]
    public Breath breathAnimation = new();

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Pulse)]
    public Pulse pulseAnimation = new();

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Wiggle)]
    public Wiggle wiggleAnimation = new();

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Bounce)]
    public Bounce bounceAnimation = new();

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Heartbeat)]
    public Heartbeat heartbeatAnimation = new();

    [SerializeField] private float duration = 0.8f;

    [SerializeField] private bool delay = true;
    [SerializeField] private float delayDuration = 0.8f;
    
    [SerializeField] private bool ignoreTimeScale = true;

    [Header("Parent Control")]
    [SerializeField] private bool ignoreParent;
    public bool IgnoreParent => ignoreParent;
    public bool IsLooping => animationStyle != AnimationStyle.None;
    public bool IsAnimating => currentTween != null && currentTween.IsActive;
    public AnimationManager Controller { get; set; }
    private bool prepared, playingPrepared, targetCached;
    private Vector3 preparedScale;
    private Quaternion preparedRotation;
    private Vector2 preparedPosition;

    public bool CapturePreparation(bool show, AnimationPlaybackOptions options, bool preserveCurrentState)
    {
        CacheTarget();
        prepared = target != null;
        if (!prepared) return false;
        preparedScale = preserveCurrentState ? target.localScale : Vector3.one;
        preparedRotation = preserveCurrentState ? target.localRotation : Quaternion.identity;
        preparedPosition = preserveCurrentState ? target.anchoredPosition : initialAnchoredPos;
        if (!preserveCurrentState && show && !options.reducedMotion)
        {
            switch (animationStyle)
            {
                case AnimationStyle.Breathing: preparedScale = Vector3.one * breathAnimation.startScale; break;
                case AnimationStyle.Pulse: preparedScale = Vector3.one * pulseAnimation.startScale; break;
                case AnimationStyle.Heartbeat: preparedScale = Vector3.one * heartbeatAnimation.startScale; break;
                case AnimationStyle.Wiggle: preparedRotation = Quaternion.Euler(0, 0, wiggleAnimation.startRotation); break;
                case AnimationStyle.Jelly: preparedScale = new Vector3(1, .95f, 1); break;
            }
        }
        return true;
    }

    public void ApplyPreparation()
    {
        if (!prepared || target == null) return;
        target.localScale = preparedScale;
        target.localRotation = preparedRotation;
        target.anchoredPosition = preparedPosition;
    }

    public void ClearPreparation() => prepared = false;

    public void InvalidatePreviewTargets()
    {
        if (Application.isPlaying) return;
        targetCached = false; prepared = false;
    }

    public void RefreshPreviewTargets()
    {
        if (Application.isPlaying) return;
        targetCached = false;
        CacheTarget();
    }

    public void PlayManaged(bool show, AnimationPlaybackOptions options, System.Action complete)
    {
        if (!prepared)
        {
            if (!CapturePreparation(show, options, IsAnimating)) { complete?.Invoke(); return; }
            KillCurrentTween();
            ApplyPreparation();
        }
        else KillCurrentTween();
        prepared = false;
        if (!show || options.reducedMotion) { complete?.Invoke(); return; }
        float savedDuration = duration;
        bool savedTimeScale = ignoreTimeScale;
        if (options.overrideDuration) duration = Mathf.Max(.001f, options.duration);
        if (options.overrideTimeScale) ignoreTimeScale = options.unscaled;
        playingPrepared = true;
        try
        {
            StartAnimation();
            if (options.useCustomCurve && options.curve != null) currentTween?.SetEase(options.curve);
            else if (options.overrideEase) currentTween?.SetEase(options.ease);
        }
        finally { playingPrepared = false; duration = savedDuration; ignoreTimeScale = savedTimeScale; }
        complete?.Invoke(); // decorative loops count as started
    }
    public void StopManaged() => StopAnimation();
    private RectTransform target;
    private AnimationHandle currentTween;
    private Vector2 initialAnchoredPos;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    private void Awake() => CacheTarget();

    private void CacheTarget()
    {
        if (target == null) target = GetComponent<RectTransform>();
        if (target == null || targetCached) return;
        initialAnchoredPos = target.anchoredPosition;
        targetCached = true;
    }

    private void OnEnable()
    {
        if (Controller != null || AnimationManager.IsPreviewing) return;
        if(delay)
            currentTween = new AnimationHandle(this, 0, null, null).SetDelay(delayDuration).SetUpdate(ignoreTimeScale).SetOwner(this).OnComplete(StartAnimation);
        else
            StartAnimation();
    }

    private void OnDisable()
    {
        KillCurrentTween();
        ResetTransform();
    }

    void KillCurrentTween()
    {
        CancelInvoke(nameof(StartAnimation));
        currentTween?.Kill();
        currentTween = null;
    }

    void ResetTransform()
    {
        if (playingPrepared) return;
        if (target == null) target = GetComponent<RectTransform>();
        if (target == null) return;

        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
        target.anchoredPosition = initialAnchoredPos;
    }

    public void PlayBreathing()
    {
        KillCurrentTween();
        ResetTransform();

        if (!playingPrepared) target.localScale = Vector3.one * breathAnimation.startScale;

        currentTween = target
            .AnimateScale(breathAnimation.breatheScale, duration).SetOwner(this)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(ignoreTimeScale);
    }

    public void PlayPulse()
    {
        KillCurrentTween();
        ResetTransform();

        if (!playingPrepared) target.localScale = Vector3.one * pulseAnimation.startScale;

        currentTween = target
            .AnimateScale(pulseAnimation.pulseScale, duration * 0.5f).SetOwner(this)
            .SetEase(Ease.OutQuad)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(ignoreTimeScale);
    }

    public void PlayWiggle()
    {
        KillCurrentTween();
        ResetTransform();

        if (!playingPrepared) target.localRotation = Quaternion.Euler(0f, 0f, wiggleAnimation.startRotation);

        currentTween = target
            .AnimateRotate(new Vector3(0f, 0f, wiggleAnimation.wiggleAngle), duration * 0.25f).SetOwner(this)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetRelative()
            .SetUpdate(ignoreTimeScale);
    }

    public void PlayBounce()
    {
        KillCurrentTween();
        ResetTransform();

        AnimationSequence sequence = AnimationTimeline.Sequence().SetUpdate(ignoreTimeScale).SetOwner(this);

        sequence.Append(target.AnimateScale(bounceAnimation.bounceScale, duration * 0.25f).SetOwner(this).SetEase(Ease.OutQuad));
        sequence.Join(target.AnimateAnchorPosY(initialAnchoredPos.y + bounceAnimation.moveY, duration * 0.25f).SetOwner(this).SetEase(Ease.OutQuad));

        sequence.Append(target.AnimateScale(bounceAnimation.startScale, duration * 0.35f).SetOwner(this).SetEase(Ease.InQuad));
        sequence.Join(target.AnimateAnchorPosY(initialAnchoredPos.y, duration * 0.35f).SetOwner(this).SetEase(Ease.InQuad));

        sequence.AppendInterval(duration * 0.4f);
        sequence.SetLoops(-1);

        currentTween = sequence;
    }

    public void PlayHeartbeat()
    {
        KillCurrentTween();
        ResetTransform();

        if (!playingPrepared) target.localScale = Vector3.one * heartbeatAnimation.startScale;

        AnimationSequence sequence = AnimationTimeline.Sequence().SetUpdate(ignoreTimeScale).SetOwner(this);

        sequence.Append(target.AnimateScale(heartbeatAnimation.firstBeatScale, duration * 0.18f).SetOwner(this).SetEase(Ease.OutQuad));
        sequence.Append(target.AnimateScale(heartbeatAnimation.startScale, duration * 0.14f).SetOwner(this).SetEase(Ease.InQuad));

        sequence.Append(target.AnimateScale(heartbeatAnimation.secondBeatScale, duration * 0.16f).SetOwner(this).SetEase(Ease.OutQuad));
        sequence.Append(target.AnimateScale(heartbeatAnimation.startScale, duration * 0.14f).SetOwner(this).SetEase(Ease.InQuad));

        sequence.AppendInterval(heartbeatAnimation.pauseAfterBeat);
        sequence.SetLoops(-1);

        currentTween = sequence;
    }

    void PlayJelly()
    {
        KillCurrentTween();
        ResetTransform();

        Vector3 squashed = new Vector3(1f, 0.95f, 1f);
        Vector3 stretched = new Vector3(0.95f, 1f, 1f);

        if (!playingPrepared) target.localScale = squashed;

        currentTween = target
            .AnimateScale(stretched, duration).SetOwner(this)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(ignoreTimeScale);
    }

    [MyBox.ButtonMethod]
    public void StopAnimation()
    {
        KillCurrentTween();
        ResetTransform();
    }

    [MyBox.ButtonMethod]
    void StartAnimation()
    {
        KillCurrentTween();
        if (target == null)
            target = GetComponent<RectTransform>();

        if (target == null) return;
        if (!playingPrepared) initialAnchoredPos = target.anchoredPosition;

        switch (animationStyle)
        {
            case AnimationStyle.Breathing:
                PlayBreathing();
                break;

            case AnimationStyle.Pulse:
                PlayPulse();
                break;

            case AnimationStyle.Wiggle:
                PlayWiggle();
                break;

            case AnimationStyle.Bounce:
                PlayBounce();
                break;

            case AnimationStyle.Heartbeat:
                PlayHeartbeat();
                break;
            case AnimationStyle.Jelly:
                PlayJelly();
                break;
        }
    }
    private void OnValidate() => AnimationManager.NotifyConfigurationChanged();

}

[System.Serializable]
public enum AnimationStyle
{
    None,
    Breathing,
    Pulse,
    Wiggle,
    Bounce,
    Heartbeat,
    Jelly,

}

[System.Serializable]
public class Breath
{
    public float startScale = 1f;
    public float breatheScale = 1.08f;
}

[System.Serializable]
public class Pulse
{
    public float startScale = 1f;
    public float pulseScale = 1.12f;
}

[System.Serializable]
public class Wiggle
{
    public float startRotation = 0f;
    public float wiggleAngle = 8f;
}

[System.Serializable]
public class Bounce
{
    public float startScale = 1f;
    public float bounceScale = 1.1f;
    public float moveY = 12f;
}

[System.Serializable]
public class Heartbeat
{
    public float startScale = 1f;
    public float firstBeatScale = 1.12f;
    public float secondBeatScale = 1.06f;
    public float pauseAfterBeat = 0.35f;
}

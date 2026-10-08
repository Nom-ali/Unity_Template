#if ACS_DOTWEEN
using DG.Tweening;
using UnityEngine;

public class ButtonAnimations : MonoBehaviour
{


    [SerializeField] private AnimationStyle animationStyle = AnimationStyle.Jelly;

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Breathing)]
    public Breath breathAnimation;

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Pulse)]
    public Pulse pulseAnimation;

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Wiggle)]
    public Wiggle wiggleAnimation;

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Bounce)]
    public Bounce bounceAnimation;

    [MyBox.ConditionalField(nameof(animationStyle), false, AnimationStyle.Heartbeat)]
    public Heartbeat heartbeatAnimation;

    [SerializeField] private float duration = 0.8f;

    [SerializeField] private bool delay = true;
    [SerializeField] private float delayDuration = 0.8f;
    
    [SerializeField] private bool ignoreTimeScale = true;

    private RectTransform target;
    private Tween currentTween;
    private Vector2 initialAnchoredPos;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        if (target != null)
            initialAnchoredPos = target.anchoredPosition;
    }

    private void OnEnable()
    {
        if(delay)
            Invoke(nameof(StartAnimation), delayDuration);
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
        currentTween?.Kill();
        currentTween = null;
    }

    void ResetTransform()
    {
        if (target == null)
            return;

        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
        target.anchoredPosition = initialAnchoredPos;
    }

    public void PlayBreathing()
    {
        KillCurrentTween();
        ResetTransform();

        target.localScale = Vector3.one * breathAnimation.startScale;

        currentTween = target
            .DOScale(breathAnimation.breatheScale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(ignoreTimeScale);
    }

    public void PlayPulse()
    {
        KillCurrentTween();
        ResetTransform();

        target.localScale = Vector3.one * pulseAnimation.startScale;

        currentTween = target
            .DOScale(pulseAnimation.pulseScale, duration * 0.5f)
            .SetEase(Ease.OutQuad)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(ignoreTimeScale);
    }

    public void PlayWiggle()
    {
        KillCurrentTween();
        ResetTransform();

        target.localRotation = Quaternion.Euler(0f, 0f, wiggleAnimation.startRotation);

        currentTween = target
            .DORotate(new Vector3(0f, 0f, wiggleAnimation.wiggleAngle), duration * 0.25f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetRelative()
            .SetUpdate(ignoreTimeScale);
    }

    public void PlayBounce()
    {
        KillCurrentTween();
        ResetTransform();

        Sequence sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale);

        sequence.Append(target.DOScale(bounceAnimation.bounceScale, duration * 0.25f).SetEase(Ease.OutQuad));
        sequence.Join(target.DOAnchorPosY(initialAnchoredPos.y + bounceAnimation.moveY, duration * 0.25f).SetEase(Ease.OutQuad));

        sequence.Append(target.DOScale(bounceAnimation.startScale, duration * 0.35f).SetEase(Ease.InQuad));
        sequence.Join(target.DOAnchorPosY(initialAnchoredPos.y, duration * 0.35f).SetEase(Ease.InQuad));

        sequence.AppendInterval(duration * 0.4f);
        sequence.SetLoops(-1);

        currentTween = sequence;
    }

    public void PlayHeartbeat()
    {
        KillCurrentTween();
        ResetTransform();

        target.localScale = Vector3.one * heartbeatAnimation.startScale;

        Sequence sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale);

        sequence.Append(target.DOScale(heartbeatAnimation.firstBeatScale, duration * 0.18f).SetEase(Ease.OutQuad));
        sequence.Append(target.DOScale(heartbeatAnimation.startScale, duration * 0.14f).SetEase(Ease.InQuad));

        sequence.Append(target.DOScale(heartbeatAnimation.secondBeatScale, duration * 0.16f).SetEase(Ease.OutQuad));
        sequence.Append(target.DOScale(heartbeatAnimation.startScale, duration * 0.14f).SetEase(Ease.InQuad));

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

        target.localScale = squashed;

        currentTween = target
            .DOScale(stretched, duration)
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
        if (target == null)
            target = GetComponent<RectTransform>();

        initialAnchoredPos = target.anchoredPosition;

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
#endif
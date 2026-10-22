using ACS.Animations;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class DoFadeCanvas : AnimationBase
{
    [SerializeField, HideInInspector] private CanvasGroup FadeObject;
    [SerializeField, HideInInspector] private float orignalAlpha;

    protected override bool CacheTarget()
    {
        if (FadeObject == null) FadeObject = GetComponent<CanvasGroup>();
        if (FadeObject == null) return false;
        orignalAlpha = FadeObject.alpha > 0 ? FadeObject.alpha : 1;
        return true;
    }

    private float preparedAlpha;

    protected override void CaptureAnimation(bool show, bool preserveCurrentState)
        => preparedAlpha = preserveCurrentState ? FadeObject.alpha : show ? 0 : orignalAlpha;

    protected override void ApplyPreparedAnimation() => FadeObject.alpha = preparedAlpha;

    protected override AnimationHandle CreateAnimation(bool show, float duration, Ease ease, bool reducedMotion)
        => FadeObject.AnimateFade(show ? orignalAlpha : 0, duration).SetEase(ease);
}

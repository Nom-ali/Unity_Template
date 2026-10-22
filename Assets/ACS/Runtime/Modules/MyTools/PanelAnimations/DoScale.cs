using ACS.Animations;
using UnityEngine;

/// <summary>Shows at the authored scale and hides to zero.</summary>
public class DoScale : AnimationBase
{
    [SerializeField, HideInInspector] private Vector3 TargetScale;
    [SerializeField] private bool useCustomStartingScale;
    [SerializeField] private Vector3 startingScale = Vector3.zero;
    private Vector3 preparedScale;

    protected override bool CacheTarget()
    {
        TargetScale = rectTransform.localScale;
        return true;
    }

    protected override void CaptureAnimation(bool show, bool preserveCurrentState)
        => preparedScale = preserveCurrentState ? rectTransform.localScale
            : show ? (useCustomStartingScale ? startingScale : Vector3.zero) : TargetScale;

    protected override void ApplyPreparedAnimation() => rectTransform.localScale = preparedScale;

    protected override AnimationHandle CreateAnimation(bool show, float duration, Ease ease, bool reducedMotion)
        => rectTransform.AnimateScale(show ? TargetScale : Vector3.zero, duration).SetEase(ease);

    protected override void OnValidate()
    {
        base.OnValidate();
        if (ShowEase == Ease.Unset) ShowEase = Ease.OutBack;
        if (HideEase == Ease.Unset) HideEase = Ease.InBack;
    }
}

using ACS.Animations;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DoFade : AnimationBase
{
    [SerializeField, HideInInspector] private Image _FadeObject;
    [SerializeField, HideInInspector] private float orignalAlpha;

    protected override bool CacheTarget()
    {
        if (_FadeObject == null) _FadeObject = GetComponent<Image>();
        if (_FadeObject == null) return false;
        orignalAlpha = orignalAlpha > 0 ? orignalAlpha : _FadeObject.color.a;
        return true;
    }

    private float preparedAlpha;

    protected override void CaptureAnimation(bool show, bool preserveCurrentState)
        => preparedAlpha = preserveCurrentState ? _FadeObject.color.a : show ? 0 : orignalAlpha;

    protected override void ApplyPreparedAnimation()
    {
        var color = _FadeObject.color;
        color.a = preparedAlpha;
        _FadeObject.color = color;
    }

    protected override AnimationHandle CreateAnimation(bool show, float duration, Ease ease, bool reducedMotion)
        => _FadeObject.AnimateFade(show ? orignalAlpha : 0, duration).SetEase(ease);
}

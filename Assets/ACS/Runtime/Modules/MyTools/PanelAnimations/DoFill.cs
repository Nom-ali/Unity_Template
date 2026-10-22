using ACS.Animations;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DoFill : AnimationBase
{
    [SerializeField] internal int Loops = 1;
    [SerializeField] internal LoopType LoopType = LoopType.Restart;
    private Image image;
    public override bool IsLooping => Loops < 0 && ShowDuration > 0;

    protected override bool CacheTarget()
    {
        image = GetComponent<Image>();
        return image != null;
    }

    private float preparedFill;

    protected override void CaptureAnimation(bool show, bool preserveCurrentState)
        => preparedFill = preserveCurrentState ? image.fillAmount : show ? 0 : 1;

    protected override void ApplyPreparedAnimation() => image.fillAmount = preparedFill;

    protected override AnimationHandle CreateAnimation(bool show, float duration, Ease ease, bool reducedMotion)
        => image.AnimateFillAmount(show ? 1 : 0, duration).SetEase(ease)
            .SetLoops(show && !reducedMotion && duration > 0 ? Loops : 1, LoopType);
}

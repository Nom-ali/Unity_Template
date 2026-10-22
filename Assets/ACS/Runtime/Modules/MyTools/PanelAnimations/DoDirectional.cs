using ACS.Animations;
using MyBox;
using UnityEngine;

/// <summary>Slides between the authored position and a directional offset, optionally scaling.</summary>
public class DoDirectional : AnimationBase
{
    public enum Direction { Left, Right, Up, Down, TopLeft, TopRight, BottomLeft, BottomRight }
    public enum ScaleOption { None, ScaleIn, ScaleOut }

    [SerializeField] private bool RandomDirection;
    [ConditionalField(nameof(RandomDirection), true)]
    [SerializeField] private Direction OnShowDirection;
    [ConditionalField(nameof(RandomDirection), true)]
    [SerializeField] private Direction OnHideDirection;
    [SerializeField, Min(0)] private float multiplier = 1;
    [SerializeField] private bool Scaling;
    [Tooltip("Use canvas-local distances instead of legacy screen-pixel offsets.")]
    [SerializeField] private bool useCanvasSpaceOffsets;

    [SerializeField] private bool useCustomStartingScale;
    [SerializeField] private Vector3 startingScale = Vector3.zero;

    private Vector3 targetScale;
    private Vector3 preparedScale;
    private Vector2 preparedPosition;
    private Vector2 originalPosition;
    private Vector2 destination;
    private AnimationHandle moveAnimation, scaleAnimation;

    protected override bool CacheTarget()
    {
        targetScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
        return true;
    }

    protected override void CaptureAnimation(bool show, bool preserveCurrentState)
    {
        var direction = RandomDirection ? (Direction)Random.Range(0, 8) : show ? OnShowDirection : OnHideDirection;
        var offsetPosition = originalPosition + DirectionVector(direction) * ViewSize() * multiplier;
        destination = show ? originalPosition : offsetPosition;
        preparedPosition = preserveCurrentState ? rectTransform.anchoredPosition : show ? offsetPosition : originalPosition;
        preparedScale = preserveCurrentState ? rectTransform.localScale
            : show ? (useCustomStartingScale ? startingScale : Vector3.zero) : targetScale;
    }

    protected override void ApplyPreparedAnimation()
    {
        rectTransform.anchoredPosition = preparedPosition;
        if (Scaling) rectTransform.localScale = preparedScale;
    }

    protected override AnimationHandle CreateAnimation(bool show, float duration, Ease ease, bool reducedMotion)
    {
        moveAnimation = rectTransform.AnimateAnchorPos(destination, duration).SetOwner(this).SetEase(ease);
        scaleAnimation = null;
        if (!Scaling) return moveAnimation;
        var sequence = AnimationTimeline.Sequence();
        sequence.Append(moveAnimation);
        scaleAnimation = rectTransform.AnimateScale(show ? targetScale : Vector3.zero, duration).SetOwner(this).SetEase(ease);
        sequence.Join(scaleAnimation);
        return sequence;
    }

    protected override void ApplyCustomCurve(AnimationHandle animation, AnimationCurve curve)
    {
        moveAnimation?.SetEase(curve);
        scaleAnimation?.SetEase(curve);
    }

    protected override void RestoreHiddenState() => rectTransform.anchoredPosition = originalPosition;

    private Vector2 ViewSize()
    {
        var size = new Vector2(Screen.width, Screen.height);
        if (!useCanvasSpaceOffsets || !(rectTransform.parent is RectTransform parent)) return size;
        var canvas = GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Vector2.zero, camera, out var bottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, size, camera, out var topRight);
        return new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
    }

    private static Vector2 DirectionVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Left: return Vector2.left;
            case Direction.Right: return Vector2.right;
            case Direction.Up: return Vector2.up;
            case Direction.Down: return Vector2.down;
            case Direction.TopLeft: return new Vector2(-1, 1);
            case Direction.TopRight: return Vector2.one;
            case Direction.BottomLeft: return -Vector2.one;
            default: return new Vector2(1, -1);
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        multiplier = Mathf.Max(0, multiplier);
        if (ShowEase == Ease.Unset) ShowEase = Ease.OutBack;
        if (HideEase == Ease.Unset) HideEase = Ease.InBack;
    }
}

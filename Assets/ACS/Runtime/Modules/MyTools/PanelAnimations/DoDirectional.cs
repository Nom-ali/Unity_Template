#if ACS_DOTWEEN
using UnityEngine;
using DG.Tweening;
using MyBox;

public class DoDirectional : AnimationBase
{
    public enum Direction
    {
        Left,
        Right,
        Up,
        Down,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum ScaleOption
    {
        None,
        ScaleIn,
        ScaleOut
    }

    [Header("********** Directional **********")]
    [SerializeField] private bool RandomDirection = false;
    [ConditionalField(nameof(RandomDirection), true)]
    [SerializeField] private Direction OnShowDirection;
    [ConditionalField(nameof(RandomDirection), true)]
    [SerializeField] private Direction OnHideDirection;
    [SerializeField] private float multiplier = 1.0f;
    [SerializeField] private bool Scaling = false;
    private Vector3 TargetScale;
    private Vector2 originalPosition;

    public override void Awake()
    {
          currentObject = gameObject;
        TargetScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
    }


    internal override void OnEnable()
    {
        if (ShouldPlayOnEnable)
            Show();
    }

    public override void Hide()
    {
        OnHide(null);
    }

    public override void Hide(System.Action action)
    {
        OnHide(action);
    }

    public override void Show()
    {
        OnShow(null);
    }

    public override void Show(System.Action action)
    {
        OnShow(action);
    }

    internal override void OnShow(System.Action action)
    {
        ActivateGameObjectForShow();

        KillActiveTween();

        Vector2 startPosition = GetStartPosition(!RandomDirection ? OnShowDirection : (Direction)Random.Range(0, 8));
        originalPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = startPosition;
      

        if (Scaling) rectTransform.localScale = Vector3.zero;

        if (SelectShowEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectShowEvents.m_OnStart?.Invoke();

        if (Callings.ShowCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Show))
            ShowCalls();

        tweening = CreateDirectionalTween(originalPosition, TargetScale, ShowDuration, ShowEase, Delays.Show)
            .OnComplete(() =>
            {
                if (SelectShowEvents.AddEvents.HasFlag(EventTypes.OnComplete))
                    SelectShowEvents.m_OnComplete?.Invoke();

                if (Callings.ShowCall.CallingTime.Equals(CallTime.OnComplete) && Callings.CallThis.HasFlag(Delays.Show))
                {
                    ShowCalls();
                }
                action?.Invoke();
            });
    }

    internal override void OnHide(System.Action action)
    {
        KillActiveTween();

        Vector2 targetPosition = GetStartPosition(!RandomDirection ? OnHideDirection : (Direction)Random.Range(0, 8));
        if (Scaling) rectTransform.localScale = TargetScale;

        if (SelectHideEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectHideEvents.m_OnStart?.Invoke();

        if (Callings.HideCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Hide))
            HideCalls();

        tweening = CreateDirectionalTween(targetPosition, Vector3.zero, HideDuration, HideEase, Delays.Hide)
           .OnComplete(() =>
           {
               if (SelectHideEvents.AddEvents.HasFlag(EventTypes.OnComplete))
                   SelectHideEvents.m_OnComplete?.Invoke();

               if (Callings.HideCall.CallingTime.Equals(CallTime.OnComplete) && Callings.CallThis.HasFlag(Delays.Hide))
               {
                   HideCalls(() =>
                   {
                       if (DisableOnHide) 
                       {
                           rectTransform.anchoredPosition = originalPosition;
                           gameObject.SetActive(false);
                       }
                   });
               }
               else if (DisableOnHide)
               {
                   rectTransform.anchoredPosition = originalPosition;
                   gameObject.SetActive(false);
               }

               action?.Invoke();
           });
    }

    Vector2 GetStartPosition(Direction moveDirection)
    {
     
        Vector2 startPos = rectTransform.anchoredPosition;
        switch (moveDirection)
        {
            case Direction.Left:
                startPos.x -= Screen.width * multiplier;
                break;
            case Direction.Right:
                startPos.x += Screen.width * multiplier;
                break;
            case Direction.Up:
                startPos.y += Screen.height * multiplier;
                break;
            case Direction.Down:
                startPos.y -= Screen.height * multiplier;
                break;
            case Direction.TopLeft:
                startPos.x -= Screen.width * multiplier;
                startPos.y += Screen.height * multiplier;
                break;
            case Direction.TopRight:
                startPos.x += Screen.width * multiplier;
                startPos.y += Screen.height * multiplier;
                break;
            case Direction.BottomLeft:
                startPos.x -= Screen.width * multiplier;
                startPos.y -= Screen.height * multiplier;
                break;
            case Direction.BottomRight:
                startPos.x += Screen.width * multiplier;
                startPos.y -= Screen.height * multiplier;
                break;
        }
        return startPos;
    }

    private Tween CreateDirectionalTween(Vector2 targetPosition, Vector3 targetScale, float duration, Ease ease, Delays animationPhase)
    {
        Sequence sequence = DOTween.Sequence().SetUpdate(isTimeIndependent);
        float delay = GetDelayFor(animationPhase);

        if (delay > 0f)
            sequence.AppendInterval(delay);

        if (Scaling)
            sequence.Join(rectTransform.DOScale(targetScale, duration).SetEase(ease));

        sequence.Join(rectTransform.DOAnchorPos(targetPosition, duration).SetEase(ease));
        return sequence;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if(ShowEase == Ease.Unset)
            ShowEase = Ease.OutBack;
        if (HideEase == Ease.Unset)
            HideEase = Ease.InBack;
    }
#endif
}
#endif


#if ACS_DOTWEEN
using DG.Tweening;
using MyBox;
using System;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class DoFadeCanvas : AnimationBase
{
    [ReadOnly, SerializeField] private CanvasGroup FadeObject;
    [ReadOnly, SerializeField] private float orignalAlpha = 0;
    
    private CanvasGroup fadeobject
    {
        get
        {
            if (FadeObject == null)
                FadeObject = GetComponent<CanvasGroup>();
            return FadeObject;
        }
    }

    public override void Awake()
    {
        currentObject = gameObject;
        // Panels are commonly authored hidden at alpha 0. In that case, fade back to a visible alpha.
        orignalAlpha = fadeobject.alpha > 0f ? fadeobject.alpha : 1f;
        
        SetValidChildrenActive(false);
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

    public override void Hide(Action action)
    {
       OnHide(action);
    }

    public override void Show()
    {
        OnShow(null);
    }

    public override void Show(Action action)
    {
       OnShow(action);  
    }

    internal override void OnShow(Action action)
    {
        ActivateGameObjectForShow();
        KillActiveTween();

        fadeobject.alpha = 0;

        if (SelectShowEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectShowEvents.m_OnStart?.Invoke();

        if (Callings.ShowCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Show))
            ShowCalls();

        tweening = fadeobject.DOFade(orignalAlpha, ShowDuration).SetEase(ShowEase).SetUpdate(isTimeIndependent)
            .SetDelay(GetDelayFor(Delays.Show))
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

    internal override void OnHide(Action action)
    {
        KillActiveTween();
        fadeobject.alpha = orignalAlpha;

        if (SelectHideEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectHideEvents.m_OnStart?.Invoke();

        if (Callings.HideCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Hide))
            HideCalls();

        tweening = fadeobject.DOFade(0, HideDuration).SetEase(HideEase).SetUpdate(isTimeIndependent)
            .SetDelay(GetDelayFor(Delays.Hide))
            .OnComplete(() =>
            {
                if (SelectHideEvents.AddEvents.HasFlag(EventTypes.OnComplete))
                    SelectHideEvents.m_OnComplete?.Invoke();

                if (Callings.HideCall.CallingTime.Equals(CallTime.OnComplete) && Callings.CallThis.HasFlag(Delays.Hide))
                {
                    HideCalls(() =>
                    {
                        if (DisableOnHide)
                            gameObject.SetActive(false);
                    });
                }
                else if (DisableOnHide)
                    gameObject.SetActive(false);

                action?.Invoke();
            });
    }
}
#endif

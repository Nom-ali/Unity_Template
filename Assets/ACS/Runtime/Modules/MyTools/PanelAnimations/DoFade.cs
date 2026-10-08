#if ACS_DOTWEEN
using DG.Tweening;
using MyBox;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DoFade : AnimationBase
{
    [ReadOnly, SerializeField] private Image _FadeObject;
    [ReadOnly, SerializeField] private float orignalAlpha;

    private Image FadeObject 
    {
        get
        {
            if(!_FadeObject) 
                _FadeObject = GetComponent<Image>();
            
            return _FadeObject;
        }
    }


    public override void Awake()
    {
        currentObject = gameObject;
      
        if (FadeObject == null)
        {
            Debug.LogError("DoFade requires an Image component.", gameObject);
            enabled = false;
            return;
        }

        if (orignalAlpha > 0f)
            FadeObject.SetAlpha(orignalAlpha);

        orignalAlpha = FadeObject.color.a;

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

        FadeObject.SetAlpha(0f);

        if (SelectShowEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectShowEvents.m_OnStart?.Invoke();

        if (Callings.ShowCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Show))
            ShowCalls();

        //Animating
        tweening = FadeObject.DOFade(orignalAlpha, ShowDuration).SetEase(ShowEase).SetUpdate(isTimeIndependent)
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
        FadeObject.SetAlpha(orignalAlpha);

        if (SelectHideEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectHideEvents.m_OnStart?.Invoke();

        if (Callings.HideCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Hide))
            HideCalls();

        //Animating
        tweening = FadeObject.DOFade(0, HideDuration).SetEase(HideEase).SetUpdate(isTimeIndependent)
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

#if ACS_DOTWEEN
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DoFill : AnimationBase
{
    private Image image = null;


    public override void Awake()
    {
        currentObject = gameObject;
        if (image == null)
            image = GetComponent<Image>();
    }

    internal override void OnEnable()
    {

        if (ShouldPlayOnEnable)
            Show();
    }

    public override void Show()
    {
        OnShow(null);
    }

    public override void Hide()
    {
        OnHide(null);
    }

    public override void Show(Action action)
    {
        OnShow(action);
    }

    public override void Hide(Action action)
    {
        OnHide(action);
    }

    internal override void OnShow(Action action)
    {
        //Debug.Log("Show Called", gameObject);

        ActivateGameObjectForShow();
        KillActiveTween();

        image.fillAmount = 0;

        if (SelectShowEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectShowEvents.m_OnStart?.Invoke();

        if (Callings.ShowCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Show))
            ShowCalls();
        tweening = image.DOFillAmount(1f, ShowDuration).SetEase(ShowEase).SetUpdate(isTimeIndependent).SetLoops(Loops, LoopType)
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
        //Debug.Log("Hide Called", gameObject);
        KillActiveTween();
        image.fillAmount = 1;

        if (SelectHideEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectHideEvents.m_OnStart?.Invoke();

        if (Callings.HideCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Hide))
            HideCalls();

        tweening = image.DOFillAmount(0f, HideDuration).SetEase(HideEase).SetUpdate(isTimeIndependent)
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


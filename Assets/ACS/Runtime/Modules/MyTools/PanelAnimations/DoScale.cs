#if ACS_DOTWEEN
using DG.Tweening;
using System;
using UnityEngine;

public class DoScale : AnimationBase
{
    [MyBox.ReadOnly, SerializeField] private Vector3 TargetScale;

    public override void Awake()
    {
        currentObject = gameObject;
        TargetScale = rectTransform.localScale;
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

        rectTransform.localScale = Vector3.zero;

        if (SelectShowEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectShowEvents.m_OnStart?.Invoke();
        
        if (Callings.ShowCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Show))
            ShowCalls();

        tweening = rectTransform.DOScale(TargetScale, ShowDuration).SetEase(ShowEase).SetUpdate(isTimeIndependent)
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
        rectTransform.localScale = TargetScale;

        if (SelectHideEvents.AddEvents.HasFlag(EventTypes.OnStart))
            SelectHideEvents.m_OnStart?.Invoke();

        if (Callings.HideCall.CallingTime.Equals(CallTime.OnStart) && Callings.CallThis.HasFlag(Delays.Hide))
            HideCalls();

        tweening = rectTransform.DOScale(Vector3.zero, HideDuration).SetEase(HideEase).SetUpdate(isTimeIndependent)
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

#if UNITY_EDITOR
    private void OnValidate()
    {
            if(ShowEase == Ease.Unset)
                ShowEase = Ease.OutBack;
            if(HideEase == Ease.Unset)
                HideEase = Ease.InBack;
    }
#endif
}
#endif


#if ACS_DOTWEEN
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System.Linq;
using System;


public abstract class AnimationBase : MonoBehaviour
{
    [Header("********** Children ********** ")]
    [ReadOnly ,SerializeField] internal List<AnimationBase> ChildList = new();
  
    private RectTransform cachedRectTransform;
    private Coroutine childAnimationRoutine;
    private bool isActivatingForShow;

    internal GameObject currentObject = null;

    internal RectTransform rectTransform
    {
        get
        {
            if (cachedRectTransform == null)
                cachedRectTransform = GetComponent<RectTransform>();

            return cachedRectTransform;
        }
    }

    [Header("********** Setting ********** ")]
    [SerializeField] internal bool PlayOnEnable = false;
    [SerializeField] internal bool DisableOnHide = true;
    [SerializeField] internal InitialDelays InitialDelay = new();
    [SerializeField] internal Calls Callings = new();

    [Header("********** Duration ********** ")]
    [SerializeField] internal float ShowDuration = 0.3f;
    [SerializeField] internal float HideDuration = 0.3f;

    [Header("********** Loop ********** ")]
    [SerializeField] internal int Loops = 1;
    [ConditionalField(nameof(Loops), true, 0, 1)]
    [SerializeField] internal LoopType LoopType = LoopType.Restart;

    [Header("**********  Ease ********** ")]
    [SerializeField] internal Ease ShowEase = Ease.Unset;
    [SerializeField] internal Ease HideEase = Ease.Unset;

    [Header("********** Events ********** ")]
    [SerializeField] internal DefineEvents SelectShowEvents = new();
    [SerializeField] internal DefineEvents SelectHideEvents = new();

    public bool isTimeIndependent = false;

    internal Tween tweening = null;

    internal bool ShouldPlayOnEnable => PlayOnEnable && !isActivatingForShow;

    public virtual void Awake()
    {
        
    }

    public abstract void Show();
    public abstract void Hide();

    public abstract void Show(Action action);
    public abstract void Hide(Action action);
          
    internal abstract void OnShow(Action action);
    internal abstract void OnHide(Action action);

    internal virtual void OnEnable()
    {

    }
    internal virtual void OnDisable()
    {
        KillActiveTween();
        StopChildAnimationRoutine();
    }
   
    internal void ActivateGameObjectForShow()
    {
        // A disabled GameObject does not run Awake until it is activated. Show can
        // therefore be called before derived animation components assign this field.
        if (currentObject == null)
            currentObject = gameObject;

        if (currentObject.activeSelf)
            return;

        isActivatingForShow = true;
        try
        {
            currentObject.SetActive(true);
        }
        finally
        {
            isActivatingForShow = false;
        }
    }

    internal void KillActiveTween()
    {
        if (tweening != null && tweening.active)
            tweening.Kill();

        tweening = null;
    }

    internal void ShowCalls(Action onComplete = null)
    {
        StartChildAnimation(Delays.Show, onComplete);
    }
    
    internal void HideCalls(Action onComplete = null)
    {
        StartChildAnimation(Delays.Hide, onComplete);
    }

    private void StartChildAnimation(Delays animationPhase, Action onComplete)
    {
        StopChildAnimationRoutine();

        if (!Callings.CallThis.HasFlag(animationPhase))
        {
            onComplete?.Invoke();
            return;
        }

        RunBy runBy = animationPhase == Delays.Show
            ? Callings.ShowCall.Call
            : Callings.HideCall.Call;

        List<AnimationBase> children = GetValidChildren();
        if (runBy == RunBy.None || children.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        childAnimationRoutine = StartCoroutine(RunChildren(children, animationPhase, runBy, onComplete));
    }

    private IEnumerator RunChildren(List<AnimationBase> children, Delays animationPhase, RunBy runBy, Action onComplete)
    {
        if (runBy == RunBy.RunAllAtOnce)
            yield return RunChildrenAtOnce(children, animationPhase);
        else
            yield return RunChildrenOneByOne(children, animationPhase);

        childAnimationRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator RunChildrenAtOnce(List<AnimationBase> children, Delays animationPhase)
    {
        for (int i = 0; i < children.Count; i++)
        {
            AnimationBase child = children[i];

            if (animationPhase == Delays.Show)
                child.Show();
            else
                child.Hide();

            yield return null;
        }
    }

    private IEnumerator RunChildrenOneByOne(List<AnimationBase> children, Delays animationPhase)
    {
        int startIndex = animationPhase == Delays.Show ? 0 : children.Count - 1;
        int endIndex = animationPhase == Delays.Show ? children.Count : -1;
        int step = animationPhase == Delays.Show ? 1 : -1;

        for (int i = startIndex; i != endIndex; i += step)
        {
            AnimationBase child = children[i];

            if (animationPhase == Delays.Show)
            {
                child.Show();
                yield return new WaitForSecondsRealtime(child.ShowDuration);
            }
            else
            {
                child.Hide();
                yield return new WaitForSecondsRealtime(child.HideDuration);
            }
        }
    }

    private List<AnimationBase> GetValidChildren()
    {
        List<AnimationBase> validChildren = new();
        HashSet<AnimationBase> seenChildren = new();

        if (ChildList == null)
            return validChildren;

        foreach (AnimationBase child in ChildList)
        {
            if (child != null && child != this && seenChildren.Add(child))
                validChildren.Add(child);
        }

        return validChildren;
    }

    /// <summary>
    /// Changes the active state of valid configured child animations only.
    /// Missing Inspector references and duplicate entries are ignored.
    /// </summary>
    protected void SetValidChildrenActive(bool active)
    {
        foreach (AnimationBase child in GetValidChildren())
        {
            if (child.gameObject.activeSelf != active)
                child.gameObject.SetActive(active);
        }
    }

    private void StopChildAnimationRoutine()
    {
        if (childAnimationRoutine == null)
            return;

        StopCoroutine(childAnimationRoutine);
        childAnimationRoutine = null;
    }

    internal float GetDelayFor(Delays animationPhase)
    {
        if (!InitialDelay.InitialDelay || !InitialDelay.AddIn.HasFlag(animationPhase))
            return 0f;

        if (InitialDelay.DelayValue == DelayFrom.StaticValue)
            return Mathf.Max(0f, InitialDelay.StaticValue);

        if (InitialDelay.DelayValue == DelayFrom.FromChild)
        {
            return GetValidChildren().Sum(item =>
                animationPhase == Delays.Show ? item.ShowDuration : item.HideDuration);
        }

        return 0f;
    }

    // Kept for existing callers outside this module. New panel animations should use GetDelayFor.
    internal float GetDelays()
    {
        if (!InitialDelay.InitialDelay)
            return 0f;

        if (InitialDelay.DelayValue == DelayFrom.StaticValue)
            return InitialDelay.StaticValue;

        if (InitialDelay.DelayValue == DelayFrom.FromChild)
            return GetValidChildren().Sum(item => item.HideDuration);

        return 0f;
    }


#if UNITY_EDITOR
    [ButtonMethod(ButtonMethodDrawOrder.BeforeInspector)]
    public void GetChildComponent()
    {
        AnimationBase[] chilList = GetComponentsInChildren<AnimationBase>(true);
        ChildList = chilList.Where(child => child.gameObject != gameObject).ToList();
        Debug.Log($"{ChildList.Count} children found.");

        if(ChildList.Count > 0)
        {
            InitialDelay.InitialDelay = true;
            InitialDelay.DelayValue = DelayFrom.FromChild;
            InitialDelay.AddIn = Delays.Hide;

            Callings.CallThis |= (Delays.Hide | Delays.Show);
            Callings.ShowCall.CallingTime = CallTime.OnComplete;
            Callings.ShowCall.Call = RunBy.RunOneByOne;
            
            Callings.HideCall.CallingTime = CallTime.OnStart;
            Callings.HideCall.Call = RunBy.RunOneByOne;

        }
    }
#endif

    private void OnValidate()
    {
        ChildList ??= new();
        InitialDelay ??= new();
        Callings ??= new();
        SelectShowEvents ??= new();
        SelectHideEvents ??= new();
    }

}

[Serializable]
public class DefineEvents
{
    [SerializeField] internal EventTypes AddEvents = EventTypes.None;
   
    [ConditionalField(nameof(AddEvents), false, EventTypes.OnStart)]
    public UnityEvent m_OnStart = null;

    [ConditionalField(nameof(AddEvents), false, EventTypes.OnComplete)]
    public UnityEvent m_OnComplete = null;
}

[Serializable, Flags]
public enum EventTypes
{
    None = 0,
  
    OnStart = 1 << 0,
    OnComplete = 1 << 1
}

[Serializable]
public enum DelayFrom
{
    None,
    StaticValue,
    FromChild
}
   
[Serializable, Flags]
public enum Delays
{
    None = 0,
    Show = 1 << 0,
    Hide = 1 << 1
}

[Serializable]
public enum RunBy
{
    None, 
    RunAllAtOnce,
    RunOneByOne
}

[Serializable]
public class InitialDelays
{
    [SerializeField] internal bool InitialDelay = false;
    [ConditionalField(nameof(InitialDelay), false)]
    public DelayFrom DelayValue = DelayFrom.None;
    [ConditionalField(nameof(DelayValue), true, DelayFrom.None)]
    public Delays AddIn = Delays.None;
    [ConditionalField(nameof(DelayValue), false, DelayFrom.StaticValue)]
    public float StaticValue = 0.3f;
}

[Serializable]
public class Calls
{ 
    [SerializeField] internal Delays CallThis = Delays.None;
    [ConditionalField(nameof(CallThis), false, Delays.Show)]
    [SerializeField] internal OnCall ShowCall;
    [ConditionalField(nameof(CallThis), false, Delays.Hide)]
    [SerializeField] internal OnCall HideCall;
}

[Serializable]
public struct OnCall
{
    [SerializeField] internal CallTime CallingTime;
    [SerializeField] internal RunBy Call;
}

[Serializable]
public enum CallTime
{
    None, 
    OnStart,
    OnComplete
}


#endif


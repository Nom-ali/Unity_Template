#if !ACS_DOTWEEN
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationBase : MonoBehaviour
{
    [SerializeField] internal List<AnimationBase> ChildList = new();
    [SerializeField] internal float ShowDuration = 0.3f;
    [SerializeField] internal float HideDuration = 0.3f;
    public bool isTimeIndependent = false;

    public virtual void Awake() { }
    internal virtual void OnEnable() { }
    public virtual void Show() { }
    public virtual void Hide() { }
    public virtual void Show(Action action) { action?.Invoke(); }
    public virtual void Hide(Action action) { action?.Invoke(); }
    internal virtual void OnShow(Action action) { action?.Invoke(); }
    internal virtual void OnHide(Action action) { action?.Invoke(); }
    internal float GetDelays() => 0f;
}

public class DoFade : AnimationBase { }

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
}

public class DoScale : AnimationBase { }

public class DoFadeCanvas : AnimationBase { }

public class DoFill : AnimationBase { }

public class Logo : MonoBehaviour { }
#endif

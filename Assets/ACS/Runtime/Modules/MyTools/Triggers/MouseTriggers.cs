using MyBox;
using System;
using UnityEngine;
using UnityEngine.Events;

public class MouseTriggers : MonoBehaviour
{
    [Serializable, Flags]
    public enum MouseTriggersEvents
    {
        None = 0,
        OnMouseDown = 1 << 0,
        OnMouseUp = 1 << 1,
        OnMouseEnter = 1 << 2,
        OnMouseExit = 1 << 3,
        OnMouseOver = 1 << 4
    }

    [SerializeField] private MouseTriggersEvents triggers  = MouseTriggersEvents.None;

    [ConditionalField(nameof(triggers),false,  MouseTriggersEvents.OnMouseDown)]
    [Space, SerializeField] internal UnityEvent m_OnMouseDown = null;
    
    [ConditionalField(nameof(triggers),false,  MouseTriggersEvents.OnMouseUp)]
    [Space, SerializeField] internal UnityEvent m_OnMouseUp = null;
       
    [ConditionalField(nameof(triggers),false,  MouseTriggersEvents.OnMouseEnter)]
    [Space, SerializeField] internal UnityEvent m_OnMouseEnter = null;
       
    [ConditionalField(nameof(triggers),false,  MouseTriggersEvents.OnMouseExit)]
    [Space, SerializeField] internal UnityEvent m_OnMouseExit = null;
       
    [ConditionalField(nameof(triggers),false,  MouseTriggersEvents.OnMouseOver)]
    [Space, SerializeField] internal UnityEvent m_OnMouseOver = null;

    private void OnMouseDown()
    {
        if(triggers.HasFlag(MouseTriggersEvents.OnMouseDown))
        {
            m_OnMouseDown?.Invoke();
            Debug.Log("Mouse Down");
        }
    }

    private void OnMouseUp()
    {
        if (triggers.HasFlag(MouseTriggersEvents.OnMouseUp))
        {
            m_OnMouseUp?.Invoke();
            Debug.Log("Mouse Up");
        }
    }

    private void OnMouseEnter()
    {
        if (triggers.HasFlag(MouseTriggersEvents.OnMouseEnter))
        {
            m_OnMouseEnter?.Invoke();
            Debug.Log("Mouse Enter");
        }

    }

    private void OnMouseExit()
    {
        if (triggers.HasFlag(MouseTriggersEvents.OnMouseExit))
        {
            m_OnMouseExit?.Invoke();
            Debug.Log("Mouse Exit");
        }

    }

    private void OnMouseOver()
    {
        if (triggers.HasFlag(MouseTriggersEvents.OnMouseOver))
        {
            m_OnMouseOver?.Invoke();
            Debug.Log("Mouse Over");
        }
        
    }
}





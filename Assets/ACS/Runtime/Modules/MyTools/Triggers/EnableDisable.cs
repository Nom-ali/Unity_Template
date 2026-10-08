using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ACS
{
    public class EnableDisable : MonoBehaviour
    {

        [SerializeField] SharedEnums.DefaultEvents defaultEvents = SharedEnums.DefaultEvents.None;

        [MyBox.ConditionalField(nameof(defaultEvents), false, SharedEnums.DefaultEvents.OnEnable)]
        [SerializeField] private UnityEvent m_OnEnable;

        [MyBox.ConditionalField(nameof(defaultEvents), false, SharedEnums.DefaultEvents.OnDisable)]
        [SerializeField] private UnityEvent m_OnDisable;

        [MyBox.ConditionalField(nameof(defaultEvents), false, SharedEnums.DefaultEvents.OnDestroy)]
        [SerializeField] private UnityEvent m_OnDestroy;

        [SerializeField] private float  Delay = 0f;

        private void OnEnable()
        {
            StartCoroutine(CallWithDelay(m_OnEnable));
        }

        private void OnDisable()
        {
            m_OnDisable?.Invoke();
        }

        private void OnDestroy()
        {
            m_OnDestroy?.Invoke();
        }

        IEnumerator CallWithDelay(UnityEvent unityEvent)
        {
            yield return new WaitForSeconds(Delay);
            unityEvent?.Invoke();
        }
    }
}





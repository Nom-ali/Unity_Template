using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Triggers : MonoBehaviour
{
    [SerializeField] private string compareTag = "";
    [SerializeField] private bool DisableOnTrigger = false;
    [SerializeField] private RigidbodyConstraints OnEnterRigidbodyConstraints = RigidbodyConstraints.None;
    [SerializeField] private UnityEvent<Collider2D> onTriggerEnter;
    [SerializeField] private UnityEvent<Collider2D> onTriggerExit;
    [MyBox.ReadOnly] public GameObject TriggeredObject;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(compareTag))
        {
            TriggeredObject = other.gameObject;
            onTriggerEnter?.Invoke(other);
            
            if (!OnEnterRigidbodyConstraints.Equals(RigidbodyConstraints.None) && other.TryGetComponent(out Rigidbody rb))
                rb.constraints = OnEnterRigidbodyConstraints;
            gameObject.SetActive(!DisableOnTrigger);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(compareTag))
        {
            TriggeredObject = null;
            onTriggerExit?.Invoke(other);
        }
    }
}





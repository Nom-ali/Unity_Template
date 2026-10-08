using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckPoint : MonoBehaviour
{
    [SerializeField] private string compareTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(compareTag))
        {
            CheckPointSystem.SetCheckPoint?.Invoke(transform);
        }
    }
}





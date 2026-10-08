using UnityEngine;
using System;

public static class MonoBehaviourExtensions
{
    public static void InvokeAfterDelay(this MonoBehaviour monoBehaviour, float delay, Action action)
    {
        monoBehaviour.StartCoroutine(InvokeCoroutine(delay, action));
    }

    private static System.Collections.IEnumerator InvokeCoroutine(float delay, Action action)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified time
        action?.Invoke(); // Execute the provided action
    }
}





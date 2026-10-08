
using System;
using UnityEngine;

public class CheckPointSystem : MonoBehaviour
{
    public static Action<Transform> SetCheckPoint; 
    public static Func<Transform> GetCheckPoint; 

    [SerializeField] private Transform CurrentCheckPoint;

    private void OnEnable()
    {
        SetCheckPoint += setCheckPOint;
        GetCheckPoint += getCheckPoint;
    }

    private void OnDisable()
    {
        SetCheckPoint -= setCheckPOint;
        GetCheckPoint -= getCheckPoint;
    }

    void setCheckPOint(Transform checkpoint)
    {
        CurrentCheckPoint = checkpoint;
    }

    Transform getCheckPoint()
    {
        return CurrentCheckPoint;
    }
}





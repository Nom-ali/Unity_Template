using ACS;
using UnityEngine;

public class ButtonListeners : MonoBehaviour, IInitialization
{
    [SerializeField] private AddListeners listeners;
          
    public void Init(UIManagerBase uIManager)
    {
        listeners.Init(uIManager);
    }

    private void OnDisable()
    {
        listeners.OnDisableRemoveAll();
    }
}





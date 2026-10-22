using ACS;
using UnityEngine;

public class ButtonListeners : MonoBehaviour, IInitialization
{
    [SerializeField] private AddListeners listeners;
    private UIManagerBase manager;

    public void Init(UIManagerBase uIManager)
    {
        manager = uIManager;
        if (isActiveAndEnabled) listeners?.Init(manager);
    }
    private void OnEnable()
    {
        if (!manager) manager = GetComponentInParent<UIManagerBase>();
        if (manager) listeners?.Init(manager);
    }
    private void OnDisable() => listeners?.OnDisableRemoveAll();
}

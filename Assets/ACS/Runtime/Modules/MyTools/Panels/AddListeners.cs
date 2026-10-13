using ACS;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AddListeners
{
    [SerializeField] private ButtonAction[] AddButtons;

    public Button GetButton(int ButtonIndex)
    {
        return AddButtons[ButtonIndex].button;
    }

    public virtual void Init(UIManagerBase manager)
    {
        foreach (var button in AddButtons)
        {
            ButtonAction btn = button;
            UIManagerBase managerBase = manager;
            btn.button.onClick.RemoveAllListeners();
            btn.button.onClick.AddListener(() =>
            {
                Debug.Log("btn: " + btn.button.name + " pressed");
                if (btn.AdsOnClick)
                {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
                    ACS.AdsManager.AdsManager.Instance.ShowInterAds();
#endif
                }
                Debug.Log($"Btn {btn.button.name} pressed", btn.button);
                managerBase.OnButtonClicked(btn);
            });
            btn.button.interactable = button.Interactable;
        }
    }

    internal void OnDisableRemoveAll()
    {
        foreach (var button in AddButtons)
        {
            if(button.button.onClick.GetPersistentEventCount() > 0)
                button.button.onClick.RemoveAllListeners();
        }
    }
}





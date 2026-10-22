using ACS;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class AddListeners
{
    [SerializeField] private ButtonAction[] AddButtons;
    [NonSerialized] private List<Binding> bindings;
    private struct Binding { public Button button; public UnityAction callback; }

    public Button GetButton(int ButtonIndex) => AddButtons[ButtonIndex].button;

    public virtual void Init(UIManagerBase manager)
    {
        OnDisableRemoveAll();
        if (!manager || AddButtons == null) return;
        bindings ??= new List<Binding>(AddButtons.Length);
        foreach (var entry in AddButtons)
        {
            if (entry == null || !entry.button) continue;
            ButtonAction action = entry;
            UnityAction callback = () =>
            {
                if (!manager) return;
                if (action.AdsOnClick)
                {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
                    ACS.AdsManager.AdsManager.Instance?.ShowInterAds();
#endif
                }
                manager.OnButtonClicked(action);
            };
            entry.button.onClick.AddListener(callback);
            bindings.Add(new Binding { button = entry.button, callback = callback });
            entry.button.interactable = entry.Interactable;
        }
    }

    internal void OnDisableRemoveAll()
    {
        if (bindings == null) return;
        foreach (var binding in bindings)
            if (binding.button) binding.button.onClick.RemoveListener(binding.callback);
        bindings.Clear();
    }
}

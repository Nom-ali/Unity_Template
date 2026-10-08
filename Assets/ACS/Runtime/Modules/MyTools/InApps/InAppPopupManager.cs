#if ACS_IAP

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows configured in-app panels or buttons only while their associated
/// product is not permanently owned.
/// </summary>
[DisallowMultipleComponent]
public sealed class InAppPopupManager : MonoBehaviour
{
    [Serializable]
    public sealed class PopupBinding
    {
        [Tooltip("Product used for both ownership checks and Show/Hide calls.")]
        public IAPProductType productType;

        [Tooltip("Panel or button GameObject controlled by this entry.")]
        public GameObject target;
    }

    [SerializeField]
    private PopupBinding[] popupBindings = new PopupBinding[0];
      
    private readonly Dictionary<IAPProductType, PopupBinding> bindingsByProductType =
        new Dictionary<IAPProductType, PopupBinding>();

    public static InAppPopupManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[In-App Popups] Only one InAppPopupManager can be active at a time.", gameObject);
            enabled = false;
            return;
        }

        Instance = this;
        BuildBindingLookup();
        HideAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Shows the target identified by <paramref name="productType"/> only
    /// when its product is not owned. The callback runs wether it return true or false
    /// </summary>
    public PopupBinding Show(IAPProductType productType, Action onShown = null)
    {
        if (!TryGetBinding(productType, out PopupBinding binding))
        {
            onShown?.Invoke();
            return null;
        }

        if (IsProductOwned(binding.productType))
        {
            binding.target.SetActive(false);
            onShown?.Invoke();
            return null;
        }

        if (binding.target.TryGetComponent(out AnimationBase animation))
            animation.Show(onShown);
        else
        {
            binding.target.SetActive(true);
            onShown?.Invoke();
        }

        if (binding.target.TryGetComponent(out ButtonListeners listeners))
            listeners.Init(UIManager.Instance);

        return binding;
    }

    /// <summary>
    /// Static convenience method for callers that do not hold a manager reference.
    /// </summary>
    public PopupBinding ShowPopup(IAPProductType productType, Action onShown = null)
    {
        return Show(productType, onShown);
    }

    /// <summary>
    /// Hides a configured target regardless of ownership state.
    /// </summary>
    public bool Hide(IAPProductType productType)
    {
        if (!TryGetBinding(productType, out PopupBinding binding))
        {
            return false;
        }

        binding.target.SetActive(false);
        return true;
    }

    /// <summary>
    /// Re-applies ownership checks to all configured targets. Call this after
    /// a purchase or restore if a target is already visible.
    /// </summary>
    public void HideAll()
    {
        foreach (PopupBinding binding in bindingsByProductType.Values)
        {            
            binding.target.SetActive(false);   
        }
    }

    private void BuildBindingLookup()
    {
        bindingsByProductType.Clear();

        foreach (PopupBinding binding in popupBindings)
        {
            if (binding == null || binding.target == null)
            {
                Debug.LogWarning("[In-App Popups] Every entry needs a product type and a target GameObject.", gameObject);
                continue;
            }

            if (binding.productType == IAPProductType.None)
            {
                Debug.LogWarning("[In-App Popups] Product type None cannot control a target.", binding.target);
                continue;
            }

            if (string.IsNullOrEmpty(IAPProductIDs.GetProductId(binding.productType)))
            {
                Debug.LogError($"[In-App Popups] Product type '{binding.productType}' has no mapped product ID.", binding.target);
                continue;
            }

            if (bindingsByProductType.ContainsKey(binding.productType))
            {
                Debug.LogError($"[In-App Popups] Duplicate product type '{binding.productType}'.", binding.target);
                continue;
            }

            bindingsByProductType.Add(binding.productType, binding);
        }
    }

    private bool TryGetBinding(IAPProductType productType, out PopupBinding binding)
    {
        binding = null;

        if (productType == IAPProductType.None)
        {
            Debug.LogWarning("[In-App Popups] A product type is required.", gameObject);
            return false;
        }

        if (!bindingsByProductType.TryGetValue(productType, out binding))
        {
            Debug.LogWarning($"[In-App Popups] No target is configured for product type '{productType}'.", gameObject);
            return false;
        }

        return true;
    }

    private bool IsProductOwned(IAPProductType productType)
    {
        string productId = IAPProductIDs.GetProductId(productType);
        return IAPRewardRegistry.TryIsOwned(productId, out bool owned) && owned;
    }
}

#endif

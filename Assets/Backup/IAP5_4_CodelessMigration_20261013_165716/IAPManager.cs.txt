#if ACS_IAP

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public sealed class IAPManager : MonoBehaviour
{
    [Serializable]
    public struct DisableObjects
    {
        public GameObject[] objeccts;
    }

    [SerializeField] private IAPButtonType buttonType = IAPButtonType.None;

    [SerializeField] private GameObject LoadingPanel;

    [SerializeField] private bool ShowPriceFromStore = false;

    [SerializeField] private TMP_Text priceText;

    [SerializeField] private bool DisableOnPurchase = false;
      
    [SerializeField] private IAPProductType DisableProductType = IAPProductType.None;

    [SerializeField] internal bool self = true;

    [SerializeField] internal DisableObjects objects;

    // ---------------------------------------------------------
    // Cached references
    // ---------------------------------------------------------

    private CodelessIAPButton codelessIAPButton;
    private AnimationBase animationBase;
    private Button hookedButton;

    private bool eventsSubscribed;

    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        TryGetComponent(out animationBase);

        if (!TryGetComponent(out codelessIAPButton))
        {
            Debug.LogError("[IAP] CodelessIAPButton component not found.", gameObject);

            enabled = false;
            return;
        }

        CacheButton();

        if (ShowPriceFromStore && priceText == null)
        {
            priceText = GetComponentInChildren<TMP_Text>(true);
        }

        if (codelessIAPButton.buttonType ==
            CodelessButtonType.Purchase)
        {
            ShowProductPrice();
        }
    }

    private void OnEnable()
    {
        if (codelessIAPButton == null)
            return;

        // Purchased non-consumable can disable this object here.
        if (DisablePurchasedObjects())
            return;

        SubscribeEvents();
        HookButton();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        UnhookButton();
    }

    // =========================================================
    // Button
    // =========================================================

    private void CacheButton()
    {
        if (codelessIAPButton.button != null)
            return;

        if (TryGetComponent(out Button uiButton))
        {
            codelessIAPButton.button = uiButton;
        }
    }

    private void HookButton()
    {
        CacheButton();

        hookedButton = codelessIAPButton.button;

        if (hookedButton == null)
        {
            Debug.LogWarning("[IAP] UI Button reference is missing.", gameObject);
            return;
        }

        // Remove only our own listener.
        hookedButton.onClick.RemoveListener(ShowPanel);
        hookedButton.onClick.AddListener(ShowPanel);
    }

    private void UnhookButton()
    {
        if (hookedButton == null)
            return;

        hookedButton.onClick.RemoveListener(ShowPanel);
        hookedButton = null;
    }

    // =========================================================
    // Unity IAP events
    // =========================================================

    private void SubscribeEvents()
    {
        if (eventsSubscribed || codelessIAPButton == null)
            return;

        if (codelessIAPButton.buttonType ==
            CodelessButtonType.Purchase)
        {
            codelessIAPButton.onProductFetched
                .AddListener(OnProductDataFetched);

            codelessIAPButton.onProductFetchFailed
                .AddListener(OnProductDataFetchedFailed);

            codelessIAPButton.onPurchaseFetched
                .AddListener(OnPurchaseFetched);

            codelessIAPButton.onOrderPending
                .AddListener(OnOrderPending);

            codelessIAPButton.onOrderConfirmed
                .AddListener(OnOrderConfirmed);

            codelessIAPButton.onPurchaseFailed
                .AddListener(OnPurchaseFailed);

            codelessIAPButton.onOrderDeferred
                .AddListener(OnOrderDeferred);
        }
        else if (codelessIAPButton.buttonType ==
                 CodelessButtonType.Restore)
        {
            codelessIAPButton.onTransactionsRestored
                .AddListener(OnTransactionRestored);
        }

        eventsSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!eventsSubscribed || codelessIAPButton == null)
            return;

        if (codelessIAPButton.buttonType ==
            CodelessButtonType.Purchase)
        {
            codelessIAPButton.onProductFetched
                .RemoveListener(OnProductDataFetched);

            codelessIAPButton.onProductFetchFailed
                .RemoveListener(OnProductDataFetchedFailed);

            codelessIAPButton.onPurchaseFetched
                .RemoveListener(OnPurchaseFetched);

            codelessIAPButton.onOrderPending
                .RemoveListener(OnOrderPending);

            codelessIAPButton.onOrderConfirmed
                .RemoveListener(OnOrderConfirmed);

            codelessIAPButton.onPurchaseFailed
                .RemoveListener(OnPurchaseFailed);

            codelessIAPButton.onOrderDeferred
                .RemoveListener(OnOrderDeferred);
        }
        else if (codelessIAPButton.buttonType ==
                 CodelessButtonType.Restore)
        {
            codelessIAPButton.onTransactionsRestored
                .RemoveListener(OnTransactionRestored);
        }

        eventsSubscribed = false;
    }

    // =========================================================
    // Loading panel
    // =========================================================

    private void ShowPanel()
    {
        if (animationBase != null)
        {
            animationBase.Show();
            return;
        }

        if (LoadingPanel != null)
        {
            LoadingPanel.SetActive(true);
        }
    }

    private void HidePanel()
    {
        if (animationBase != null)
        {
            animationBase.Hide();
            return;
        }

        if (LoadingPanel != null)
        {
            LoadingPanel.SetActive(false);
        }
    }

    // =========================================================
    // Store price
    // =========================================================

    private void ShowProductPrice(string message = "Buy")
    {
        if (!ShowPriceFromStore || priceText == null)
            return;

        priceText.SetText(message);
    }

    private void OnProductDataFetched(Product product)
    {
        if (product == null)
            return;

        ShowProductPrice(product.metadata.localizedPriceString);

        if (codelessIAPButton.button != null)
        {
            codelessIAPButton.button.interactable = true;
        }
        DevLogs.Log($"[IAP] Product fetched: {product.definition.id}");
    }

    private void OnProductDataFetchedFailed(
        ProductDefinition definition,
        string message)
    {
        ShowProductPrice();

        if (codelessIAPButton.button != null)
        {
            codelessIAPButton.button.interactable = false;
        }

        Debug.LogWarning($"[IAP] Product fetch failed: {message}",gameObject);
    }

    // =========================================================
    // Purchase callbacks
    // =========================================================

    /// <summary>
    /// Previously owned/fetched products.
    ///
    /// Non-consumables can safely restore entitlement here.
    /// Consumables MUST NOT be replayed.
    /// </summary>
    private void OnPurchaseFetched(Order order)
    {
        if (order == null)
        {
            Debug.LogWarning("[IAP] OnPurchaseFetched received null Order.");
            return;
        }

        foreach (var item in order.CartOrdered.Items())
        {
            Product product = item.Product;

            if (product == null ||
                product.definition == null)
            {
                continue;
            }

            string productId =
                product.definition.id;

            // Never restore/replay consumables.
            if (IAPRewardRegistry.IsConsumable(productId))
                continue;

            IAPRewardRegistry.Grant(productId);
        }
    }

    /// <summary>
    /// New purchase reward point.
    /// </summary>
    private void OnOrderPending(
        PendingOrder pendingOrder)
    {
        if (pendingOrder == null)
        {
            Debug.LogWarning(
                "[IAP] OnOrderPending received null order."
            );

            HidePanel();
            return;
        }

        foreach (var item in pendingOrder.CartOrdered.Items())
        {
            Product product = item.Product;

            if (product == null ||
                product.definition == null)
            {
                continue;
            }

            IAPRewardRegistry.Grant(product.definition.id);
        }

        HidePanel();
    }

    private void OnOrderConfirmed(
        ConfirmedOrder confirmedOrder)
    {
        HidePanel();

        if (confirmedOrder == null)
        {
            DevLogs.Log("[IAP] OnOrderConfirmed received null order.");
            return;
        }

        DevLogs.Log("[IAP] Order confirmed.");

        DisablePurchasedObjects();
    }

    private void OnOrderDeferred(
        DeferredOrder deferredOrder)
    {
        HidePanel();

        if (deferredOrder == null)
        {
            DevLogs.Log("[IAP] OnOrderDeferred received null order.");

            return;
        }

        DevLogs.Log("[IAP] Purchase deferred / awaiting approval.");
    }

    private void OnPurchaseFailed(
        FailedOrder failedOrder)
    {
        HidePanel();

        if (failedOrder == null)
        {
            Debug.LogWarning("[IAP] OnPurchaseFailed received null order.");

            return;
        }

        Debug.LogWarning("[IAP] Purchase failed.", gameObject);
    }

    private void OnTransactionRestored(
        bool success,
        string error)
    {
        HidePanel();

        if (success)
        {
            DevLogs.Log("[IAP] Transactions restored.");

            DisablePurchasedObjects();
            return;
        }

        Debug.LogWarning($"[IAP] Restore failed: {error}", gameObject);
    }

    // =========================================================
    // Purchased button disabling
    // =========================================================

    private bool DisablePurchasedObjects()
    {
        if (!DisableOnPurchase)
            return false;

        string productKey =
            DisableProductType == IAPProductType.None
                ? gameObject.name
                : IAPProductIDs.GetProductId(DisableProductType);

        if (string.IsNullOrEmpty(productKey))
        {
            Debug.LogWarning($"[IAP] No product ID is mapped for '{DisableProductType}'.", gameObject);

            return false;
        }

        if (!IAPRewardRegistry.TryIsOwned(productKey, out bool purchased))
        {
            DevLogs.Log($"[IAP] No ownership check registered for '{productKey}'.");

            return false;
        }

        if (!purchased)
            return false;

        if (self)
        {
            if (gameObject.TryGetComponent(out AnimationBase animation))
                animation.Hide();
            else 
                gameObject.SetActive(false);

            return true;
        }

        GameObject[] targets =
            objects.objeccts;

        if (targets == null)
            return false;

        for (int i = 0; i < targets.Length; i++)
        {
            GameObject target =
                targets[i];

            if (target != null &&
                target.activeSelf)
            {
                if (target.TryGetComponent(out AnimationBase animation))
                    animation.Hide();
                else
                    target.SetActive(false);

            }
        }

        return false;
    }

    // =========================================================
    // Logging
    // =========================================================

  

    // =========================================================
    // Editor auto setup
    // =========================================================

#if UNITY_EDITOR

    private void OnValidate()
    {
        if (buttonType == IAPButtonType.None)
            return;

        if (!TryGetComponent(out Image image))
        {
            image =
                gameObject.AddComponent<Image>();
        }

        if (!TryGetComponent(out Button button))
        {
            button =
                gameObject.AddComponent<Button>();
        }

        if (!TryGetComponent(
                out CodelessIAPButton iapButton))
        {
            iapButton =
                gameObject.AddComponent<CodelessIAPButton>();
        }

        if (ShowPriceFromStore)
        {
            priceText = GetComponentInChildren<TMP_Text>(true);
        }

        iapButton.buttonType =
            buttonType == IAPButtonType.Purchase
                ? CodelessButtonType.Purchase
                : CodelessButtonType.Restore;

        iapButton.button = button;
    }

#endif
}

#endif

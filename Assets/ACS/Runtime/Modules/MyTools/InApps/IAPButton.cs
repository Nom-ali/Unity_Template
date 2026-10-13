#if ACS_IAP

using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public enum IAPButtonAction
{
    Purchase,
    Restore
}

/// <summary>
/// UI adapter for a single IAP 5 purchase or restore action.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class IAPButton : MonoBehaviour
{
    [SerializeField] private IAPButtonAction action = IAPButtonAction.Purchase;
    [SerializeField] private IAPProductType productType = IAPProductType.None;
    [SerializeField] private bool showLocalizedPrice;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private bool disableWhenOwned;
    [SerializeField] private GameObject[] disableTargets = new GameObject[0];

    private Button button;
    private IAPManager manager;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (showLocalizedPrice && priceText == null)
        {
            priceText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        BindManager();
        Refresh();
    }

    private void Start()
    {
        BindManager();
        Refresh();
    }

    private void OnDisable()
    {
        UnbindManager();
    }

    public void Trigger()
    {
        BindManager();

        if (manager == null)
        {
            Debug.LogWarning("[IAP] No IAPManager is available.", gameObject);
            return;
        }

        ShowLoading(true);

        bool started = action == IAPButtonAction.Restore
            ? manager.Restore()
            : manager.Purchase(productType);

        if (!started)
        {
            ShowLoading(false);
            Refresh();
        }
    }

    private void BindManager()
    {
        if (manager != null || IAPManager.Instance == null)
            return;

        manager = IAPManager.Instance;
        manager.StoreReadyChanged += OnStoreReadyChanged;
        manager.ProductFetched += OnProductFetched;
        manager.ProductFetchFailed += OnProductFetchFailed;
        manager.PurchaseCompleted += OnPurchaseCompleted;
        manager.PurchaseFailed += OnPurchaseFailed;
        manager.PurchaseDeferred += OnPurchaseDeferred;
        manager.RestoreCompleted += OnRestoreCompleted;

        button.onClick.RemoveListener(Trigger);
        button.onClick.AddListener(Trigger);
    }

    private void UnbindManager()
    {
        if (manager == null)
            return;

        manager.StoreReadyChanged -= OnStoreReadyChanged;
        manager.ProductFetched -= OnProductFetched;
        manager.ProductFetchFailed -= OnProductFetchFailed;
        manager.PurchaseCompleted -= OnPurchaseCompleted;
        manager.PurchaseFailed -= OnPurchaseFailed;
        manager.PurchaseDeferred -= OnPurchaseDeferred;
        manager.RestoreCompleted -= OnRestoreCompleted;
        button.onClick.RemoveListener(Trigger);
        manager = null;
    }

    private void Refresh()
    {
        if (button == null)
            return;

        if (action == IAPButtonAction.Restore)
        {
            button.interactable = manager != null && manager.IsReady;
            return;
        }

        bool owned = IsOwned();
        ApplyOwnedState(owned);

        if (showLocalizedPrice && manager != null &&
            manager.TryGetLocalizedPrice(productType, out string localizedPrice) && priceText != null)
        {
            priceText.SetText(localizedPrice);
        }

        button.interactable = !owned && manager != null && manager.IsReady;
    }

    private bool IsOwned()
    {
        string productId = IAPProductIDs.GetProductId(productType);
        return IAPRewardRegistry.TryIsOwned(productId, out bool owned) && owned;
    }

    private void ApplyOwnedState(bool owned)
    {
        if (!disableWhenOwned || !owned)
            return;

        for (int i = 0; i < disableTargets.Length; i++)
        {
            if (disableTargets[i] != null)
            {
                disableTargets[i].SetActive(false);
            }
        }
    }

    private void OnStoreReadyChanged(bool isReady) => Refresh();

    private void OnProductFetched(IAPProductType fetchedProductType, Product product)
    {
        if (action != IAPButtonAction.Purchase || fetchedProductType != productType)
            return;

        if (showLocalizedPrice && priceText != null)
        {
            priceText.SetText(product.metadata.localizedPriceString);
        }

        Refresh();
    }

    private void OnProductFetchFailed(IAPProductType failedProductType, string reason)
    {
        if (action == IAPButtonAction.Purchase && failedProductType == productType)
        {
            button.interactable = false;
            Debug.LogWarning($"[IAP] Product fetch failed for '{productType}': {reason}", gameObject);
        }
    }

    private void OnPurchaseCompleted(IAPProductType completedProductType)
    {
        if (action == IAPButtonAction.Purchase && completedProductType == productType)
        {
            ShowLoading(false);
            Refresh();
        }
    }

    private void OnPurchaseFailed(IAPProductType failedProductType, string reason)
    {
        if (action == IAPButtonAction.Purchase && failedProductType == productType)
        {
            ShowLoading(false);
            Refresh();
        }
    }

    private void OnPurchaseDeferred(IAPProductType deferredProductType)
    {
        if (action == IAPButtonAction.Purchase && deferredProductType == productType)
        {
            ShowLoading(false);
            Refresh();
        }
    }

    private void OnRestoreCompleted(bool success, string error)
    {
        if (action == IAPButtonAction.Restore)
        {
            ShowLoading(false);
            Refresh();
        }
    }

    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
    }
}

#endif

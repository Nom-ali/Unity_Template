#if ACS_IAP

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Owns the Unity IAP 5 store connection, products, purchases, and restores.
/// Product IDs and types are generated from the ACS IAP product registry.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class IAPManager : MonoBehaviour
{
    private const string GrantedTransactionPrefix = "ACS_IAP_GRANTED_";

    public static IAPManager Instance { get; private set; }

    public bool IsReady { get; private set; }

    public event Action<bool> StoreReadyChanged;
    public event Action<IAPProductType, Product> ProductFetched;
    public event Action<IAPProductType, string> ProductFetchFailed;
    public event Action<IAPProductType> PurchaseCompleted;
    public event Action<IAPProductType, string> PurchaseFailed;
    public event Action<IAPProductType> PurchaseDeferred;
    public event Action<bool, string> RestoreCompleted;

    private readonly List<ProductDefinition> productDefinitions = new List<ProductDefinition>();
    private readonly Dictionary<string, IAPProductType> productTypesById =
        new Dictionary<string, IAPProductType>(StringComparer.Ordinal);

    private StoreController store;
    private bool isConnecting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        if (Instance != null)
            return;

        GameObject managerObject = new GameObject("IAP Manager");
        managerObject.AddComponent<IAPManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildProductDefinitions();
        SubscribeStoreEvents();
        ConnectStore();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnsubscribeStoreEvents();
            Instance = null;
        }
    }

    public bool Purchase(IAPProductType productType)
    {
        if (!IsReady || store == null)
        {
            Debug.LogWarning("[IAP] Store is not ready for purchases.");
            return false;
        }

        if (!TryGetProduct(productType, out Product product))
        {
            Debug.LogWarning($"[IAP] Product '{productType}' is not available from the store.");
            return false;
        }

        store.PurchaseProduct(product);
        return true;
    }

    public bool Restore()
    {
        if (!IsReady || store == null)
        {
            RestoreCompleted?.Invoke(false, "Store is not ready.");
            return false;
        }

        store.RestoreTransactions((success, error) =>
        {
            RestoreCompleted?.Invoke(success, error ?? string.Empty);
        });

        return true;
    }

    public bool TryGetLocalizedPrice(IAPProductType productType, out string localizedPrice)
    {
        localizedPrice = null;

        if (!TryGetProduct(productType, out Product product))
            return false;

        localizedPrice = product.metadata.localizedPriceString;
        return !string.IsNullOrEmpty(localizedPrice);
    }

    private async void ConnectStore()
    {
        if (store == null || isConnecting)
            return;

        isConnecting = true;

        try
        {
            await store.Connect();
        }
        catch (Exception exception)
        {
            isConnecting = false;
            Debug.LogError($"[IAP] Store connection threw an exception: {exception.Message}");
        }
    }

    private void BuildProductDefinitions()
    {
        productDefinitions.Clear();
        productTypesById.Clear();

        foreach (IAPProductType productType in Enum.GetValues(typeof(IAPProductType)))
        {
            if (productType == IAPProductType.None)
                continue;

            string productId = IAPProductIDs.GetProductId(productType);
            if (string.IsNullOrWhiteSpace(productId))
            {
                Debug.LogError($"[IAP] Product type '{productType}' has no generated product ID.");
                continue;
            }

            if (productTypesById.ContainsKey(productId))
            {
                Debug.LogError($"[IAP] Duplicate generated product ID '{productId}'.");
                continue;
            }

            productTypesById.Add(productId, productType);
            productDefinitions.Add(new ProductDefinition(
                productId,
                IAPProductIDs.GetProductType(productType)));
        }
    }

    private void SubscribeStoreEvents()
    {
        store = UnityIAPServices.StoreController();

        store.OnStoreConnected += OnStoreConnected;
        store.OnStoreDisconnected += OnStoreDisconnected;
        store.OnProductsFetched += OnProductsFetched;
        store.OnProductsFetchFailed += OnProductsFetchFailed;
        store.OnPurchasesFetched += OnPurchasesFetched;
        store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        store.OnPurchasePending += OnPurchasePending;
        store.OnPurchaseConfirmed += OnPurchaseConfirmed;
        store.OnPurchaseFailed += OnPurchaseFailed;
        store.OnPurchaseDeferred += OnPurchaseDeferred;
        store.OnAuthAccountChanged += OnAuthAccountChanged;
    }

    private void UnsubscribeStoreEvents()
    {
        if (store == null)
            return;

        store.OnStoreConnected -= OnStoreConnected;
        store.OnStoreDisconnected -= OnStoreDisconnected;
        store.OnProductsFetched -= OnProductsFetched;
        store.OnProductsFetchFailed -= OnProductsFetchFailed;
        store.OnPurchasesFetched -= OnPurchasesFetched;
        store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
        store.OnPurchasePending -= OnPurchasePending;
        store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
        store.OnPurchaseFailed -= OnPurchaseFailed;
        store.OnPurchaseDeferred -= OnPurchaseDeferred;
        store.OnAuthAccountChanged -= OnAuthAccountChanged;
    }

    private void OnStoreConnected()
    {
        isConnecting = false;

        if (productDefinitions.Count == 0)
        {
            Debug.LogError("[IAP] The generated product registry is empty.");
            return;
        }

        store.FetchProducts(productDefinitions);
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        isConnecting = false;
        SetStoreReady(false);
        Debug.LogWarning($"[IAP] Store disconnected: {failure.Message}");
    }

    private void OnProductsFetched(List<Product> products)
    {
        SetStoreReady(true);

        foreach (Product product in products)
        {
            if (TryGetProductType(product.definition.id, out IAPProductType productType))
            {
                ProductFetched?.Invoke(productType, product);
            }
        }

        store.FetchPurchases();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        SetStoreReady(false);

        foreach (ProductDefinition definition in failure.FailedFetchProducts)
        {
            if (TryGetProductType(definition.id, out IAPProductType productType))
            {
                ProductFetchFailed?.Invoke(productType, failure.FailureReason.ToString());
            }
        }

        Debug.LogError($"[IAP] Product fetch failed: {failure.FailureReason}");
    }

    private void OnPurchasesFetched(Orders orders)
    {
        foreach (ConfirmedOrder confirmedOrder in orders.ConfirmedOrders)
        {
            RestorePermanentEntitlements(confirmedOrder);
        }
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning($"[IAP] Existing-purchase fetch failed: {failure.Message}");
    }

    private void OnPurchasePending(PendingOrder pendingOrder)
    {
        string transactionId = pendingOrder.Info.TransactionID;
        if (!string.IsNullOrEmpty(transactionId) && PlayerPrefs.HasKey(GrantedTransactionPrefix + transactionId))
        {
            store.ConfirmPurchase(pendingOrder);
            return;
        }

        if (!GrantOrder(pendingOrder))
        {
            Debug.LogError("[IAP] A pending order has no reward route and was not confirmed.");
            return;
        }

        if (!string.IsNullOrEmpty(transactionId))
        {
            PlayerPrefs.SetInt(GrantedTransactionPrefix + transactionId, 1);
            PlayerPrefs.Save();
        }

        store.ConfirmPurchase(pendingOrder);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failedOrder)
        {
            OnPurchaseFailed(failedOrder);
            return;
        }

        if (!(order is ConfirmedOrder confirmedOrder))
            return;

        foreach (var item in confirmedOrder.CartOrdered.Items())
        {
            if (TryGetProductType(item.Product.definition.id, out IAPProductType productType))
            {
                PurchaseCompleted?.Invoke(productType);
            }
        }
    }

    private void OnPurchaseFailed(FailedOrder failedOrder)
    {
        if (failedOrder.CartOrdered == null)
        {
            Debug.LogWarning($"[IAP] Purchase failed: {failedOrder.FailureReason} - {failedOrder.Details}");
            return;
        }

        foreach (var item in failedOrder.CartOrdered.Items())
        {
            if (TryGetProductType(item.Product.definition.id, out IAPProductType productType))
            {
                PurchaseFailed?.Invoke(productType, failedOrder.FailureReason.ToString());
            }
        }

        Debug.LogWarning($"[IAP] Purchase failed: {failedOrder.FailureReason} - {failedOrder.Details}");
    }

    private void OnPurchaseDeferred(DeferredOrder deferredOrder)
    {
        foreach (var item in deferredOrder.CartOrdered.Items())
        {
            if (TryGetProductType(item.Product.definition.id, out IAPProductType productType))
            {
                PurchaseDeferred?.Invoke(productType);
            }
        }
    }

    private void OnAuthAccountChanged()
    {
        SetStoreReady(false);
        store.FetchProducts(productDefinitions);
        store.FetchPurchases();
    }

    private bool GrantOrder(Order order)
    {
        bool grantedEveryItem = true;

        foreach (var item in order.CartOrdered.Items())
        {
            grantedEveryItem &= IAPRewardRegistry.Grant(item.Product.definition.id);
        }

        return grantedEveryItem;
    }

    private void RestorePermanentEntitlements(Order order)
    {
        foreach (var item in order.CartOrdered.Items())
        {
            string productId = item.Product.definition.id;
            if (!IAPRewardRegistry.IsConsumable(productId))
            {
                IAPRewardRegistry.Grant(productId);
            }
        }
    }

    private bool TryGetProduct(IAPProductType productType, out Product product)
    {
        product = null;

        if (store == null || !TryGetProductId(productType, out string productId))
            return false;

        product = store.GetProductById(productId);
        return product != null;
    }

    private bool TryGetProductId(IAPProductType productType, out string productId)
    {
        productId = IAPProductIDs.GetProductId(productType);
        return !string.IsNullOrEmpty(productId);
    }

    private bool TryGetProductType(string productId, out IAPProductType productType)
    {
        return productTypesById.TryGetValue(productId, out productType);
    }

    private void SetStoreReady(bool value)
    {
        if (IsReady == value)
            return;

        IsReady = value;
        StoreReadyChanged?.Invoke(value);
    }
}

#endif

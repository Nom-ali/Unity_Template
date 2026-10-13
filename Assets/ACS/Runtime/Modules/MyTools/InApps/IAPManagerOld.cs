#if ACS_IAP
using System.Collections.Generic;
using UnityEngine.Purchasing;
using System.Collections;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

namespace ACS_IAP_LEGACY
{
    public class IAPManagerOld : MonoBehaviour
    {
        [Serializable]
        public struct DisableObjects
        {
            public GameObject[] objeccts;
        }

        [SerializeField] private IAPButtonType buttonType = IAPButtonType.None;
        [SerializeField] private GameObject LoadingPanel;
        [SerializeField] private bool ShowPriceFromStore = false;
        [SerializeField] private bool DisableOnPurchase = false;
        [SerializeField] internal bool self = true;
        [SerializeField] internal DisableObjects objects;

        private Image image;
        private Button button;
        private CodelessIAPButton codelessIAPButton;
        private Dictionary<string, Action> productRewards;

        private AnimationBase _animationBase;
        private AnimationBase animationBase
        {
            get
            {
                if (_animationBase == null)
                    _animationBase = GetComponent<AnimationBase>();
                return _animationBase;
            }
        }


        void HidePanel()
        {
            if (animationBase)
                animationBase.Hide();
            else
                LoadingPanel?.SetActive(false);
        }

        void Showpanel()
        {
            if (animationBase)
                animationBase.Show();
            else
                LoadingPanel?.SetActive(true);

        }

        private Dictionary<string, Func<bool>> IAPProductPurchased = new()
    {
        { "removeads", () => ACS.SaveManager.SaveManager.Prefs.GetBool(SharedVariables.RemoveAds, false, true) == true},
    };

        private void OnEnable()
        {
            DisableObject();
        }

        private void Awake()
        {

            if (TryGetComponent(out codelessIAPButton))
            {
                if (codelessIAPButton.buttonType == CodelessButtonType.Purchase)
                {
                    ShowProductPrice();

                    codelessIAPButton.onProductFetched.RemoveAllListeners();
                    codelessIAPButton.onProductFetched.AddListener(OnProductDataFetched);

                    codelessIAPButton.onProductFetchFailed.RemoveAllListeners();
                    codelessIAPButton.onProductFetchFailed.AddListener(OnProductDataFetchedFailed);

                    codelessIAPButton.onPurchaseFetched.RemoveAllListeners();
                    codelessIAPButton.onPurchaseFetched.AddListener(OnPurchaseFetched);

                    codelessIAPButton.onOrderPending.RemoveAllListeners();
                    codelessIAPButton.onOrderPending.AddListener(OnOrderPending);

                    codelessIAPButton.onOrderConfirmed.RemoveAllListeners();
                    codelessIAPButton.onOrderConfirmed.AddListener(OnOrderConfirmed);

                    codelessIAPButton.onPurchaseFailed.RemoveAllListeners();
                    codelessIAPButton.onPurchaseFailed.AddListener(OnPurchaseFailed);

                    codelessIAPButton.onOrderDeferred.RemoveAllListeners();
                    codelessIAPButton.onOrderDeferred.AddListener(OnOrderDeferred);
                }
                else if (codelessIAPButton.buttonType == CodelessButtonType.Restore)
                {
                    codelessIAPButton.onTransactionsRestored.RemoveAllListeners();
                    codelessIAPButton.onTransactionsRestored.AddListener(OnTransactionRestored);
                }
            }
            else
            {
                Debug.LogError("No Codeless IAP m_Button found");
            }
        }

        private IEnumerator Start()
        {
            InitializeProductRewards();

            yield return new WaitUntil(() => codelessIAPButton != null && codelessIAPButton.button != null);

            if (codelessIAPButton.button)
                codelessIAPButton.button.onClick.AddListener(() =>
                {
                    Showpanel();
                });

            yield return null;
        }


        private void DisableObject()
        {
            string lowName = gameObject.name.ToLower();
            if (DisableOnPurchase && IAPProductPurchased.TryGetValue(lowName, out var action))
            {
                bool check = (bool)action?.Invoke();
                Debug.Log($"[IAP] Disabling object based on purchase Product '{lowName}' purchased: {check}", gameObject);
                if (DisableOnPurchase && check)
                {
                    if (self)
                        gameObject.SetActive(false);
                    else
                    {
                        foreach (GameObject obj in objects.objeccts)
                        {
                            if (obj != null && obj.activeSelf)
                            {
                                obj.SetActive(false);
                            }
                        }
                    }
                }

            }
        }

        void ShowProductPrice(string message = "Buy")
        {
            if (ShowPriceFromStore)
            {
                var text = GetComponentInChildren<TextMeshProUGUI>();
                if (text)
                    text.text = message;
            }
        }

        void OnProductDataFetched(Product product)
        {
            if (product == null) return;

            Debug.Log("Product data fetched: " + product);
            ShowProductPrice(product.metadata.localizedPriceString);
        }

        void OnProductDataFetchedFailed(ProductDefinition definition, string message)
        {
            ShowProductPrice();

            codelessIAPButton.button.interactable = false;
        }

        void OnPurchaseFetched(Order order)
        {
            if (order == null)
            {
                Debug.LogWarning("[IAP] OnPurchaseFetched called with null Order.");
                return;
            }

            foreach (var item in order.CartOrdered.Items())
            {
                GrantProduct(item.Product);
            }
        }

        private void OnOrderPending(PendingOrder pendingOrder)
        {
            if (pendingOrder == null)
            {
                Debug.LogWarning("[IAP] OnOrderPending called with null PendingOrder.");
                return;
            }

            foreach (var item in pendingOrder.CartOrdered.Items())
            {
                Debug.Log($"[IAP] cart item: {item.Product}");
                GrantProduct(item.Product);
            }

            HidePanel();
        }

        private void OnOrderConfirmed(ConfirmedOrder confirmedOrder)
        {
            if (confirmedOrder == null)
            {
                Debug.LogWarning("[IAP] OnOrderConfirmed called with null ConfirmedOrder.");
                return;
            }

            Debug.Log("[IAP] Order confirmed.");
            DisableObject();
            // Don’t grant here (granting should be done in OnOrderPending).
            // Use for: analytics, UI success popup, hide spinner, etc.
        }

        private void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            if (deferredOrder == null)
            {
                Debug.LogWarning("[IAP] OnOrderDeferred called with null DeferredOrder.");
                return;
            }

            Debug.Log("[IAP] Order deferred (e.g., Ask to Buy). Show waiting UI.");
        }


        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            if (failedOrder == null)
            {
                Debug.LogWarning("[IAP] OnPurchaseFailed called with null FailedOrder.");
                return;
            }

            // FailedOrder usually contains details; depending on exact IAP build,
            // you may have accessors for reason/message.
            Debug.LogWarning("[IAP] Purchase failed (FailedOrder).");
            HidePanel();
            // Example: show "Cancelled" / "Failed" UI and re-enable button
        }

        void OnTransactionRestored(bool success, string error)
        {
            Debug.Log("Transactions restored: " + success);
            HidePanel();
        }

        /// <summary>
        /// Grants the purchased or restored product entitlements.
        /// </summary>
        /// <param name="product">The product to grant</param>
        private void GrantProduct(Product product)
        {
            string id = product.definition.id.ToLower();

            if (PlayerPrefs.GetInt($"IAP_GRANTED_{id}", 0) == 1)
                return;

            if (productRewards.TryGetValue(id, out var rewardAction))
            {
                rewardAction.Invoke();
                PlayerPrefs.SetInt($"IAP_GRANTED_{id}", 1);
                Debug.Log($"[IAP] Granted product: {id}");
            }
            else
            {
                Debug.LogWarning($"[IAP] Unhandled product ID: {id}");
            }
        }

        private void InitializeProductRewards()
        {
            productRewards = new Dictionary<string, Action>
        {
            {
                IAPProductIDs.RemoveAds, () =>
                {
                    ACS.SaveManager.SaveManager.Prefs.SetBool(SharedVariables.RemoveAds, true, true);
                    #if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY || ACS_UNITY_LEVELPLAY
                        ACS.AdsManager.AdsManager.Instance.ApplyRemoveAdsPurchase();
                    #endif
                }
            }
        };
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (buttonType != IAPButtonType.None)
            {
                TryGetComponent(out image);
                if (!image)
                    image = gameObject.AddComponent<Image>();

                TryGetComponent(out button);
                if (!button)
                    button = gameObject.AddComponent<Button>();

                TryGetComponent(out codelessIAPButton);
                if (!codelessIAPButton)
                    codelessIAPButton = gameObject.AddComponent<CodelessIAPButton>();

                if (codelessIAPButton)
                {
                    codelessIAPButton.buttonType = buttonType == IAPButtonType.Purchase ? CodelessButtonType.Purchase : CodelessButtonType.Restore;
                    codelessIAPButton.button = button;
                }
            }
        }
#endif

    }
}
#endif

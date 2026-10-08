using ACS.SaveManager;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns booster inventory, purchases, and UI purchase flow.
/// Gameplay code assigns each booster's effect at runtime through RegisterBooster.
/// </summary>
[DisallowMultipleComponent]
public class BoosterManager : MonoBehaviour
{
    [Serializable]
    public class BoosterData
    {
        [Header("Identity")]
        [Tooltip("Unique stable key used for saved count, saved price, buttons, and runtime listeners.")]
        public string boosterId;

        public Sprite icon;

        [Header("Free Inventory")]
        [Min(0)]
        [Tooltip("Granted only the first time this booster ID is seen on this device.")]
        public int defaultCount = 2;

        [Header("Coin Purchase")]
        [Min(0)] public int startingCoinPrice = 100;
        [Min(0)] public int coinPriceIncrease = 50;

        [Min(1)]
        [Tooltip("Minimum number of boosters granted by one coin purchase.")]
        public int minCoinReward = 2;

        [Min(1)]
        [Tooltip("Maximum number of boosters granted by one coin purchase.")]
        public int maxCoinReward = 3;

        [NonSerialized] public int currentCount;
        [NonSerialized] public int currentCoinPrice;
    }

    [Header("Booster Catalog")]
    [Tooltip("Add every booster here. Each entry owns its own saved count and coin price.")]
    [SerializeField] private List<BoosterData> boosters = new List<BoosterData>();

    [Header("Purchase Panel")]
    [SerializeField] private AnimationBase boosterPurchasePanel;
    [SerializeField] private Image selectedBoosterIcon;
    [SerializeField] private TMP_Text selectedBoosterCountText;
    [SerializeField] private TMP_Text coinPriceText;
    [SerializeField] private TMP_Text coinRewardText;
    [SerializeField] private Button buyWithCoinsButton;
    [SerializeField] private Button watchRewardedAdButton;
    [SerializeField] private Button closePurchasePanelButton;

    private readonly Dictionary<string, Action> boosterListeners =
        new Dictionary<string, Action>(StringComparer.Ordinal);

    private BoosterData selectedBooster;
    private bool rewardedAdInProgress;

    /// <summary>
    /// Set at runtime by the project's currency system.
    /// Return true only after the specified amount was successfully spent.
    /// </summary>
    public Func<int, bool> TrySpendCoins { get; private set; }

    /// <summary>
    /// Set at runtime by the project's rewarded-ad system.
    /// Invoke the supplied callback with true only when the reward is earned.
    /// </summary>
    public Action<Action<bool>> ShowRewardedAd { get; private set; }

    /// <summary>
    /// boosterId, new remaining count.
    /// BoosterButton uses this to show the count or a plus sign.
    /// </summary>
    public event Action<string, int> OnBoosterCountChanged;

    /// <summary>
    /// boosterId, new coin price.
    /// </summary>
    public event Action<string, int> OnBoosterPriceChanged;

    public BoosterData SelectedBooster => selectedBooster;
    public IReadOnlyList<BoosterData> Boosters => boosters;

    private void Awake()
    {
        // Use the project's standard currency by default. A project can still
        // explicitly replace this provider through ConfigurePurchaseProviders.
        TrySpendCoins = TrySpendCoinsFromSaveManager;

        ValidateCatalog();
        LoadBoosters();
        BindPurchasePanelButtons();

        if (boosterPurchasePanel != null && boosterPurchasePanel.gameObject.activeSelf)
        {
            boosterPurchasePanel.Hide();
        }
    }

    private void OnDestroy()
    {
        UnbindPurchasePanelButtons();
    }

    private void OnValidate()
    {
        ValidateCatalog();
    }

    /// <summary>
    /// Supplies the project-specific payment and rewarded-ad implementations.
    /// Passing null intentionally disables that option.
    /// </summary>
    public void ConfigurePurchaseProviders(
        Func<int, bool> trySpendCoins,
        Action<Action<bool>> showRewardedAd)
    {
        TrySpendCoins = trySpendCoins;
        ShowRewardedAd = showRewardedAd;
    }

    private static bool TrySpendCoinsFromSaveManager(int price)
    {
        if (price < 0 || SaveManager.Currency.Value < price)
        {
            return false;
        }

        SaveManager.Currency.Value -= price;
        return true;
    }

    #region Runtime Booster Effects

    /// <summary>
    /// Registers the gameplay effect for one booster ID.
    /// Register again with the same ID to replace the previous effect.
    /// </summary>
    public void RegisterBooster(string boosterId, Action functionality)
    {
        if (!TryGetBooster(boosterId, out _))
        {
            Debug.LogWarning($"[Booster] Cannot register unknown booster '{boosterId}'.", this);
            return;
        }

        boosterListeners[boosterId] = functionality;
    }

    public void UnregisterBooster(string boosterId)
    {
        boosterListeners.Remove(boosterId);
    }

    #endregion

    #region Use Flow

    /// <summary>
    /// Called by one BoosterButton. Uses an available booster or opens its purchase panel.
    /// </summary>
    public void UseBooster(string boosterId)
    {
        if (!TryGetBooster(boosterId, out BoosterData booster))
        {
            Debug.LogWarning($"[Booster] Unknown booster '{boosterId}'.", this);
            return;
        }

        if (booster.currentCount <= 0)
        {
            OpenPurchasePanel(booster);
            return;
        }

        if (!boosterListeners.TryGetValue(booster.boosterId, out Action functionality) ||
            functionality == null)
        {
            Debug.LogWarning(
                $"[Booster] '{booster.boosterId}' has inventory but no runtime listener.",
                this
            );
            return;
        }

        try
        {
            functionality.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return;
        }

        ChangeBoosterCount(booster, -1);
    }

    public void OpenPurchasePanel(string boosterId)
    {
        if (TryGetBooster(boosterId, out BoosterData booster))
        {
            OpenPurchasePanel(booster);
        }
    }

    private void OpenPurchasePanel(BoosterData booster)
    {
        selectedBooster = booster;
        RefreshPurchasePanel();

        if (boosterPurchasePanel != null)
        {
            boosterPurchasePanel.Show();
        }
    }

    public void ClosePurchasePanel()
    {
        selectedBooster = null;

        if (boosterPurchasePanel != null)
        {
            boosterPurchasePanel.Hide();
        }
    }

    #endregion

    #region Purchase Flow

    public void BuySelectedBoosterWithCoins()
    {
        if (selectedBooster == null)
        {
            return;
        }

        if (TrySpendCoins == null)
        {
            Debug.LogWarning("[Booster] No coin purchase provider was configured.", this);
            return;
        }

        int price = selectedBooster.currentCoinPrice;
        if (!TrySpendCoins.Invoke(price))
        {
            Debug.Log("[Booster] Not enough coins.", this);
            return;
        }

        int minimumReward = Mathf.Min(selectedBooster.minCoinReward, selectedBooster.maxCoinReward);
        int maximumReward = Mathf.Max(selectedBooster.minCoinReward, selectedBooster.maxCoinReward);
        int rewardAmount = UnityEngine.Random.Range(minimumReward, maximumReward + 1);

        ChangeBoosterCount(selectedBooster, rewardAmount);

        selectedBooster.currentCoinPrice += selectedBooster.coinPriceIncrease;
        SaveBooster(selectedBooster);
        OnBoosterPriceChanged?.Invoke(
            selectedBooster.boosterId,
            selectedBooster.currentCoinPrice
        );

        RefreshPurchasePanel();
    }

    public void WatchAdForSelectedBooster()
    {
        
        if (selectedBooster == null || rewardedAdInProgress)
        {
            return;
        }

        if (ShowRewardedAd == null)
        {
            Debug.LogWarning("[Booster] No rewarded-ad provider was configured.", this);
            return;
        }

        BoosterData boosterBeingRewarded = selectedBooster;
        rewardedAdInProgress = true;

        try
        {
            ShowRewardedAd.Invoke(rewardEarned =>
            {
                rewardedAdInProgress = false;

                if (!rewardEarned)
                {
                    return;
                }

                // Rewarded ads always give exactly one booster.
                ChangeBoosterCount(boosterBeingRewarded, 1);

                if (selectedBooster == boosterBeingRewarded)
                {
                    RefreshPurchasePanel();
                }
            });
        }
        catch (Exception exception)
        {
            rewardedAdInProgress = false;
            Debug.LogException(exception, this);
        }
    }

    #endregion

    #region Inventory API

    public bool TryGetBooster(string boosterId, out BoosterData booster)
    {
        booster = GetBooster(boosterId);
        return booster != null;
    }

    public int GetBoosterCount(string boosterId)
    {
        return TryGetBooster(boosterId, out BoosterData booster)
            ? booster.currentCount
            : 0;
    }

    public Sprite GetBoosterIcon(string boosterId)
    {
        return TryGetBooster(boosterId, out BoosterData booster)
            ? booster.icon
            : null;
    }

    public void AddBooster(string boosterId, int amount)
    {
        if (amount <= 0 || !TryGetBooster(boosterId, out BoosterData booster))
        {
            return;
        }

        ChangeBoosterCount(booster, amount);
    }

    private void ChangeBoosterCount(BoosterData booster, int amount)
    {
        booster.currentCount = Mathf.Max(0, booster.currentCount + amount);
        SaveBooster(booster);
        OnBoosterCountChanged?.Invoke(booster.boosterId, booster.currentCount);
    }

    #endregion

    #region Panel UI

    private void RefreshPurchasePanel()
    {
        if (selectedBooster == null)
        {
            return;
        }

        if (selectedBoosterIcon != null)
        {
            selectedBoosterIcon.sprite = selectedBooster.icon;
        }

        if (selectedBoosterCountText != null)
        {
            selectedBoosterCountText.text = selectedBooster.currentCount.ToString();
        }

        if (coinPriceText != null)
        {
            coinPriceText.text = selectedBooster.currentCoinPrice.ToString();
        }

        if (coinRewardText != null)
        {
            int minimumReward = Mathf.Min(selectedBooster.minCoinReward, selectedBooster.maxCoinReward);
            int maximumReward = Mathf.Max(selectedBooster.minCoinReward, selectedBooster.maxCoinReward);

            coinRewardText.text = minimumReward == maximumReward
                ? $"+{minimumReward}"
                : $"+{minimumReward}-{maximumReward}";
        }

        if (buyWithCoinsButton != null)
        {
            buyWithCoinsButton.interactable = TrySpendCoins != null;
        }

        if (watchRewardedAdButton != null)
        {
            watchRewardedAdButton.interactable = !rewardedAdInProgress && ShowRewardedAd != null;
        }
    }

    private void BindPurchasePanelButtons()
    {
        BindButton(buyWithCoinsButton, BuySelectedBoosterWithCoins);
        BindButton(watchRewardedAdButton, WatchAdForSelectedBooster);
        BindButton(closePurchasePanelButton, ClosePurchasePanel);
    }

    private void UnbindPurchasePanelButtons()
    {
        UnbindButton(buyWithCoinsButton, BuySelectedBoosterWithCoins);
        UnbindButton(watchRewardedAdButton, WatchAdForSelectedBooster);
        UnbindButton(closePurchasePanelButton, ClosePurchasePanel);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }

    #endregion

    #region Saving and Validation

    private BoosterData GetBooster(string boosterId)
    {
        if (string.IsNullOrWhiteSpace(boosterId))
        {
            return null;
        }

        return boosters.Find(booster =>
            booster != null &&
            string.Equals(booster.boosterId, boosterId, StringComparison.Ordinal)
        );
    }

    private void LoadBoosters()
    {
        foreach (BoosterData booster in boosters)
        {
            if (!IsUsable(booster))
            {
                continue;
            }

            booster.currentCount = PlayerPrefs.GetInt(
                GetCountKey(booster.boosterId),
                Mathf.Max(0, booster.defaultCount)
            );

            booster.currentCoinPrice = PlayerPrefs.GetInt(
                GetPriceKey(booster.boosterId),
                Mathf.Max(0, booster.startingCoinPrice)
            );

            OnBoosterCountChanged?.Invoke(booster.boosterId, booster.currentCount);
            OnBoosterPriceChanged?.Invoke(booster.boosterId, booster.currentCoinPrice);
        }
    }

    private void SaveBooster(BoosterData booster)
    {
        PlayerPrefs.SetInt(GetCountKey(booster.boosterId), booster.currentCount);
        PlayerPrefs.SetInt(GetPriceKey(booster.boosterId), booster.currentCoinPrice);
        PlayerPrefs.Save();
    }

    private static bool IsUsable(BoosterData booster)
    {
        return booster != null && !string.IsNullOrWhiteSpace(booster.boosterId);
    }

    private void ValidateCatalog()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (BoosterData booster in boosters)
        {
            if (booster == null)
            {
                continue;
            }

            booster.defaultCount = Mathf.Max(0, booster.defaultCount);
            booster.startingCoinPrice = Mathf.Max(0, booster.startingCoinPrice);
            booster.coinPriceIncrease = Mathf.Max(0, booster.coinPriceIncrease);
            booster.minCoinReward = Mathf.Max(1, booster.minCoinReward);
            booster.maxCoinReward = Mathf.Max(1, booster.maxCoinReward);

            if (string.IsNullOrWhiteSpace(booster.boosterId) || !ids.Add(booster.boosterId))
            {
                Debug.LogWarning(
                    "[Booster] Every catalog entry needs a unique non-empty booster ID.",
                    this
                );
            }
        }
    }

    private static string GetCountKey(string boosterId)
    {
        return $"BOOSTER_COUNT_{boosterId}";
    }

    private static string GetPriceKey(string boosterId)
    {
        return $"BOOSTER_PRICE_{boosterId}";
    }

    #endregion
}

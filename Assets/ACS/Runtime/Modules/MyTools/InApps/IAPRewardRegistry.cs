#if ACS_IAP

using ACS.SaveManager;
using ACS_IAP_LEGACY;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class IAPRewardRegistry
{
    // =========================================================
    // Reward routing
    //
    // Created once for entire application.
    // =========================================================

    private static readonly Dictionary<string, Action> Rewards = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
    {
        {
            IAPProductIDs.RemoveAds,
            GrantRemoveAds
        },

        {
            IAPProductIDs.Bundle,
            GrantBundle
        },

        {
            IAPProductIDs.Coin500,
            GrantCoins500
        },

        {
            IAPProductIDs.Coin1000,
            GrantCoins1000
        },

        {
            IAPProductIDs.Coin2500,
            GrantCoins2500
        },

        {
            IAPProductIDs.Coin5000,
            GrantCoins5000
        },
    };

    // =========================================================
    // Consumables
    //
    // These products can be purchased multiple times.
    // Never restore/replay them from purchase history.
    // =========================================================

    private static readonly HashSet<string> ConsumableProducts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            IAPProductIDs.Coin500,
            IAPProductIDs.Coin1000,
            IAPProductIDs.Coin2500,
            IAPProductIDs.Coin5000,
        };

    // =========================================================
    // Ownership checks
    //
    // Only permanent entitlements belong here.
    // =========================================================

    private static readonly Dictionary<string, Func<bool>> OwnershipChecks = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase)
    {
        // Remove Ads
        {
            IAPProductIDs.RemoveAds,
            HasRemoveAds
        },

        {
            "removeads",
            HasRemoveAds
        },

        // Unlock All
        {
            IAPProductIDs.Bundle,
            HasUnlockAll
        },

        {
            "unlockall",
            HasUnlockAll
        }
    };

    // =========================================================
    // Public API
    // =========================================================

    public static bool Grant(string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return false;

        if (!Rewards.TryGetValue(productId, out Action reward))
        {
            DevLogs.Log($"[IAP] No reward registered for product: {productId}", Color.red);

            return false;
        }

        reward.Invoke();

        DevLogs.Log($"[IAP] Reward processed: {productId}", Color.yellow);

        return true;
    }

    public static bool IsConsumable(
        string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return false;

        return ConsumableProducts.Contains(productId);
    }

    /// <summary>
    /// Returns false when no ownership checker is registered.
    /// If true is returned, 'owned' contains the entitlement state.
    /// </summary>
    public static bool TryIsOwned(string productKey, out bool owned)
    {
        owned = false;

        if (string.IsNullOrEmpty(productKey))
            return false;

        if (!OwnershipChecks.TryGetValue(productKey, out Func<bool> ownershipCheck))
        {
            return false;
        }

        owned = ownershipCheck != null && ownershipCheck.Invoke();
        return true;
    }

    public static bool IsOwned(string productKey)
    {
        return TryIsOwned(productKey, out bool owned) && owned;
    }

    // =========================================================
    // Remove Ads
    // NON-CONSUMABLE
    // =========================================================

    private static bool HasRemoveAds()
    {
        return ACS.SaveManager.SaveManager.Prefs.GetBool(SharedVariables.RemoveAds, false, true);
    }

    private static void GrantRemoveAds()
    {
        // Idempotent:
        // getting this callback multiple times is safe.
#if ACS_JSON
        if (!HasRemoveAds())
        {
            ACS.SaveManager.SaveManager.Prefs.SetBool(SharedVariables.RemoveAds, true, true);
        }
#else
        DevLogs.LogError("[IAP] PlayerPrefs is not configured.");
#endif

#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY
        var adsManager =
            ACS.AdsManager.AdsManager.Instance;

        if (adsManager != null)
        {
            adsManager.DestroyBanner();
            adsManager.DestroyBigBanner();
        }
#endif

        DevLog("[IAP] Remove Ads granted.");
    }

    // =========================================================
    // Unlock All
    // NON-CONSUMABLE EXAMPLE
    // =========================================================

    private const string UnlockAllPref =
        "IAP_UNLOCK_ALL";

    private static bool HasUnlockAll()
    {
        return SaveManager.Prefs.GetBool(SharedVariables.Bundle, false, true);
    }

    private static void GrantBundle()
    {
        // Permanent item.
        // Don't grant repeatedly.
        if (HasUnlockAll())
            return;
#if ACS_JSON
        SaveManager.Prefs.SetBool(SharedVariables.Bundle, true , true);
        SaveManager.Currency.Value += 1000;
        SaveManager.Prefs.SetBool(SharedVariables.RemoveAds, true, true);
        DisableAds();
#else
        DevLogs.LogError("[IAP] SaveManager is not configured.");
#endif
       
        SaveManager.Prefs.Save();
        DevLog("[IAP] Unlock All granted.");
    }

    // =========================================================
    // Coins 1000
    // CONSUMABLE EXAMPLE
    // =========================================================

   

    private static void GrantCoins500() => GrantCoins(500);
    private static void GrantCoins1000() => GrantCoins(1000);
    private static void GrantCoins2500() => GrantCoins(2500);
    private static void GrantCoins5000() => GrantCoins(5000);
    private static void GrantCoins(int coins)
    {
       

        SaveManager.Currency.Value += coins;

        DevLog("[IAP] 1000 Coins granted.");
    }

    static void DisableAds()
    {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY
        var adsManager =
            ACS.AdsManager.AdsManager.Instance;

        if (adsManager != null)
        {
            adsManager.DestroyBanner();
            adsManager.DestroyBigBanner();
        }
#endif
    }


    // =========================================================
    // Logging
    // =========================================================

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void DevLog(
        string message)
    {
        Debug.Log(message);
    }
}


#endif
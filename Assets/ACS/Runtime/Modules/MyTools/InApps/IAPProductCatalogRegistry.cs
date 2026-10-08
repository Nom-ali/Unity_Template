#if ACS_IAP

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(
    fileName = "IAPProductCatalogRegistry",
    menuName = "ACS/IAP/Product Registry"
)]
public sealed class IAPProductCatalogRegistry : ScriptableObject
{
    [Tooltip("The single source of truth for generated IAPProductType values and catalog products.")]
    public List<IAPProductDefinition> products = new List<IAPProductDefinition>();
}

[Serializable]
public sealed class IAPProductDefinition
{
    [Tooltip("C# enum and constant name, for example: RemoveAds.")]
    public string enumName;

    [Tooltip("Store product ID, for example: com.company.game.remove.ads.")]
    public string productId;

    [Tooltip("Product type used when this ID is added to Unity's IAP catalog.")]
    public ProductType catalogProductType = ProductType.NonConsumable;
}

#endif

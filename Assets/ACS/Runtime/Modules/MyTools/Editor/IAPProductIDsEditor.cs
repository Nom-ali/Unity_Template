#if UNITY_EDITOR && ACS_IAP

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Edits an IAPProductCatalogRegistry asset and generates the runtime IAP ID/type map.
/// </summary>
public sealed class IAPProductIDsEditor : EditorWindow
{
    private const string ProductIdsSourcePath =
        "Assets/ACS/Runtime/Modules/MyTools/InApps/IAPProductIDs.cs";

    private const string DefaultRegistryPath =
        "Assets/ACS/Runtime/Modules/MyTools/InApps/IAPProductRegistry.asset";

    private static readonly Regex ValidIdentifier =
        new Regex("^[A-Za-z_][A-Za-z0-9_]*$");

    private static readonly HashSet<string> CSharpKeywords =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

    [SerializeField]
    private IAPProductCatalogRegistry registry;

    private SerializedObject registrySerializedObject;
    private SerializedProperty productsProperty;
    private Vector2 scrollPosition;
    private string statusMessage;
    private MessageType statusType = MessageType.None;

    [MenuItem("ACS/IAP/Product Registry")]
    private static void Open()
    {
        IAPProductIDsEditor window = GetWindow<IAPProductIDsEditor>("IAP Products");
        window.minSize = new Vector2(600f, 400f);
        window.Show();
    }

    private void OnEnable()
    {
        if (registry == null)
        {
            FindRegistry();
        }

        CacheRegistryProperties();
    }

    private void OnGUI()
    {
        DrawProductRegistryGUI(false);
    }

    /// <summary>Draws the existing IAP registry workflow inside Template Setup.</summary>
    internal void DrawEmbeddedGUI()
    {
        DrawProductRegistryGUI(true);
    }

    private void DrawProductRegistryGUI(bool embedded)
    {
        if (!embedded)
        {
            EditorGUILayout.LabelField("IAP Product Registry", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Add products here, then generate the runtime enum, IDs, and product types from the same list.",
                EditorStyles.wordWrappedLabel
            );
        }

        EditorGUILayout.Space(8f);
        DrawRegistrySelector();

        if (registry == null)
        {
            DrawEmptyRegistryState();
            return;
        }

        DrawActions();

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        DrawProductsEditor(embedded);
    }

    private void DrawRegistrySelector()
    {
        EditorGUI.BeginChangeCheck();
        registry = (IAPProductCatalogRegistry)EditorGUILayout.ObjectField(
            "Product Registry",
            registry,
            typeof(IAPProductCatalogRegistry),
            false
        );

        if (EditorGUI.EndChangeCheck())
        {
            CacheRegistryProperties();
            statusMessage = null;
        }
    }

    private void DrawEmptyRegistryState()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "Create a registry asset once for this project. It will be seeded from the existing IAPProductType mappings.",
            MessageType.Info
        );

        if (GUILayout.Button("Create Product Registry Asset", GUILayout.Height(26f)))
        {
            CreateDefaultRegistryAsset();
        }
    }

    private void DrawProductsEditor(bool embedded)
    {
        if (registrySerializedObject == null || productsProperty == null)
        {
            CacheRegistryProperties();
        }

        registrySerializedObject.Update();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Products", EditorStyles.boldLabel);

        if (embedded)
        {
            EditorGUILayout.PropertyField(productsProperty, new GUIContent("Product Entries"), true);
        }
        else
        {
            float reservedTopHeight = string.IsNullOrEmpty(statusMessage) ? 130f : 180f;
            float productsViewHeight = Mathf.Max(180f, position.height - reservedTopHeight);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(productsViewHeight), GUILayout.ExpandHeight(true));
            EditorGUILayout.PropertyField(productsProperty, new GUIContent("Product Entries"), true);
            EditorGUILayout.EndScrollView();
        }

        if (registrySerializedObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(registry);
        }
    }

    private void DrawActions()
    {
        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate IAPProductIDs.cs", GUILayout.Height(26f)))
            {
                GenerateProductIdsSource();
            }

        }
    }

    private void CreateDefaultRegistryAsset()
    {
        var newRegistry = CreateInstance<IAPProductCatalogRegistry>();

        foreach (IAPProductDefinition existingProduct in GetExistingProductDefinitions())
        {
            newRegistry.products.Add(new IAPProductDefinition
            {
                enumName = existingProduct.enumName,
                productId = existingProduct.productId,
                catalogProductType = IAPRewardRegistry.IsConsumable(existingProduct.productId)
                    ? ProductType.Consumable
                    : ProductType.NonConsumable
            });
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(DefaultRegistryPath);
        AssetDatabase.CreateAsset(newRegistry, assetPath);
        AssetDatabase.SaveAssets();

        registry = newRegistry;
        CacheRegistryProperties();
        Selection.activeObject = registry;
        statusMessage = "Product registry asset created. Add or edit entries, then generate the runtime source.";
        statusType = MessageType.Info;
    }

    private bool GenerateProductIdsSource()
    {
        if (!TryGetValidatedProducts(out List<IAPProductDefinition> products, out string error))
        {
            statusMessage = error;
            statusType = MessageType.Error;
            return false;
        }

        CreateProductIdsSourceBackup();
        File.WriteAllText(ProductIdsSourcePath, BuildProductIdsSource(products));
        AssetDatabase.ImportAsset(ProductIdsSourcePath, ImportAssetOptions.ForceUpdate);

        statusMessage = $"Generated IAPProductIDs.cs with {products.Count} product(s).";
        statusType = MessageType.Info;
        return true;
    }

    private static List<IAPProductDefinition> GetExistingProductDefinitions()
    {
        var existingProducts = new List<IAPProductDefinition>();
        if (!File.Exists(ProductIdsSourcePath))
        {
            return existingProducts;
        }

        string source = File.ReadAllText(ProductIdsSourcePath);
        MatchCollection matches = Regex.Matches(
            source,
            @"public\s+const\s+string\s+(?<enumName>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*""(?<productId>(?:\\.|[^""])*)""\s*;",
            RegexOptions.Multiline
        );

        foreach (Match match in matches)
        {
            string enumName = match.Groups["enumName"].Value;
            string productId = Regex.Unescape(match.Groups["productId"].Value);
            existingProducts.Add(new IAPProductDefinition
            {
                enumName = enumName,
                productId = productId
            });
        }

        return existingProducts;
    }

    private bool TryGetValidatedProducts(
        out List<IAPProductDefinition> products,
        out string error)
    {
        products = new List<IAPProductDefinition>();
        error = null;

        if (registry == null || registry.products == null || registry.products.Count == 0)
        {
            error = "Add at least one product entry to the registry.";
            return false;
        }

        var enumNames = new HashSet<string>(StringComparer.Ordinal);
        var productIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < registry.products.Count; i++)
        {
            IAPProductDefinition product = registry.products[i];
            int entryNumber = i + 1;

            if (product == null || string.IsNullOrWhiteSpace(product.enumName) || string.IsNullOrWhiteSpace(product.productId))
            {
                error = $"Product entry {entryNumber} needs both an enum name and a product ID.";
                return false;
            }

            if (!ValidIdentifier.IsMatch(product.enumName) || CSharpKeywords.Contains(product.enumName))
            {
                error = $"'{product.enumName}' is not a valid C# enum name.";
                return false;
            }

            if (string.Equals(product.enumName, "None", StringComparison.Ordinal))
            {
                error = "'None' is reserved for the no-product enum value.";
                return false;
            }

            if (!enumNames.Add(product.enumName))
            {
                error = $"Duplicate enum name '{product.enumName}'.";
                return false;
            }

            if (!productIds.Add(product.productId))
            {
                error = $"Duplicate product ID '{product.productId}'.";
                return false;
            }

            products.Add(product);
        }

        return true;
    }

    private static string BuildProductIdsSource(List<IAPProductDefinition> products)
    {
        var source = new StringBuilder();

        source.AppendLine("#if ACS_IAP");
        source.AppendLine();
        source.AppendLine("using UnityEngine.Purchasing;");
        source.AppendLine();
        source.AppendLine("// Generated by ACS > IAP > Product Registry. Edit the IAPProductCatalogRegistry asset, not this file.");
        source.AppendLine("public enum IAPProductType");
        source.AppendLine("{");
        source.AppendLine("    None,");

        foreach (IAPProductDefinition product in products)
        {
            source.AppendLine($"    {product.enumName},");
        }

        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine("public enum IAPButtonType");
        source.AppendLine("{");
        source.AppendLine("    None,");
        source.AppendLine("    Purchase,");
        source.AppendLine("    Restore");
        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine("public static class IAPProductIDs");
        source.AppendLine("{");

        foreach (IAPProductDefinition product in products)
        {
            source.AppendLine($"    public const string {product.enumName} = \"{EscapeCSharpString(product.productId)}\";");
        }

        source.AppendLine();
        source.AppendLine("    public static string GetProductId(IAPProductType productType)");
        source.AppendLine("    {");
        source.AppendLine("        switch (productType)");
        source.AppendLine("        {");

        foreach (IAPProductDefinition product in products)
        {
            source.AppendLine($"            case IAPProductType.{product.enumName}:");
            source.AppendLine($"                return {product.enumName};");
            source.AppendLine();
        }

        source.AppendLine("            default:");
        source.AppendLine("                return null;");
        source.AppendLine("        }");
        source.AppendLine("    }");
        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine("public static ProductType GetProductType(IAPProductType productType)");
        source.AppendLine("{");
        source.AppendLine("    switch (productType)");
        source.AppendLine("    {");

        foreach (IAPProductDefinition product in products)
        {
            source.AppendLine($"        case IAPProductType.{product.enumName}:");
            source.AppendLine($"            return ProductType.{product.catalogProductType};");
        }

        source.AppendLine();
        source.AppendLine("        default:");
        source.AppendLine("            return ProductType.Unknown;");
        source.AppendLine("    }");
        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine("public static bool TryGetProductType(string productId, out IAPProductType productType)");
        source.AppendLine("{");
        source.AppendLine("    productType = IAPProductType.None;");
        source.AppendLine();
        source.AppendLine("    if (string.IsNullOrEmpty(productId))");
        source.AppendLine("        return false;");
        source.AppendLine();
        source.AppendLine("    foreach (IAPProductType candidate in System.Enum.GetValues(typeof(IAPProductType)))");
        source.AppendLine("    {");
        source.AppendLine("        if (candidate != IAPProductType.None && GetProductId(candidate) == productId)");
        source.AppendLine("        {");
        source.AppendLine("            productType = candidate;");
        source.AppendLine("            return true;");
        source.AppendLine("        }");
        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    return false;");
        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine("#endif");

        return source.ToString();
    }

    private static void CreateProductIdsSourceBackup()
    {
        if (!File.Exists(ProductIdsSourcePath))
        {
            return;
        }

        string backupDirectory =
            "Assets/Backup/IAPProductIDs_Generated_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmssfff");

        Directory.CreateDirectory(backupDirectory);
        File.Copy(
            ProductIdsSourcePath,
            Path.Combine(backupDirectory, "IAPProductIDs.cs.txt"),
            false
        );
    }

    private void FindRegistry()
    {
        string[] registryGuids = AssetDatabase.FindAssets("t:IAPProductCatalogRegistry");
        if (registryGuids.Length == 0)
        {
            return;
        }

        string registryPath = AssetDatabase.GUIDToAssetPath(registryGuids[0]);
        registry = AssetDatabase.LoadAssetAtPath<IAPProductCatalogRegistry>(registryPath);
    }

    private void CacheRegistryProperties()
    {
        if (registry == null)
        {
            registrySerializedObject = null;
            productsProperty = null;
            return;
        }

        registrySerializedObject = new SerializedObject(registry);
        productsProperty = registrySerializedObject.FindProperty("products");
    }

    private static string EscapeCSharpString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}

#endif

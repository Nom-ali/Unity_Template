#if UNITY_EDITOR && ACS_IAP

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IAPManager))]
public sealed class IAPManager1Editor : Editor
{
    // IAP
    private SerializedProperty buttonType;

    // Loading
    private SerializedProperty loadingPanel;

    // Price
    private SerializedProperty showPriceFromStore;
    private SerializedProperty priceText;

    // Disable on purchase
    private SerializedProperty disableOnPurchase;
    private SerializedProperty disableProductType;
    private SerializedProperty self;
    private SerializedProperty disableObjects;

    private void OnEnable()
    {
        // -----------------------------------------------------
        // IAP
        // -----------------------------------------------------

        buttonType =
            serializedObject.FindProperty("buttonType");

        // -----------------------------------------------------
        // Loading
        // -----------------------------------------------------

        loadingPanel =
            serializedObject.FindProperty("LoadingPanel");

        // -----------------------------------------------------
        // Store Price
        // -----------------------------------------------------

        showPriceFromStore =
            serializedObject.FindProperty("ShowPriceFromStore");

        priceText =
            serializedObject.FindProperty("priceText");

        // -----------------------------------------------------
        // Purchased State
        // -----------------------------------------------------

        disableOnPurchase =
            serializedObject.FindProperty("DisableOnPurchase");

        disableProductType =
            serializedObject.FindProperty("DisableProductType");

        self =
            serializedObject.FindProperty("self");

        // Access:
        // objects.objeccts
        SerializedProperty objects =
            serializedObject.FindProperty("objects");

        disableObjects =
            objects?.FindPropertyRelative("objeccts");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawButtonType();

        IAPButtonType selectedType =
            (IAPButtonType)buttonType.enumValueIndex;

        // -----------------------------------------------------
        // NONE
        // -----------------------------------------------------

        if (selectedType == IAPButtonType.None)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space(8);

        // -----------------------------------------------------
        // RESTORE
        // -----------------------------------------------------

        if (selectedType == IAPButtonType.Restore)
        {
            DrawLoadingSettings();

            serializedObject.ApplyModifiedProperties();
            return;
        }

        // -----------------------------------------------------
        // PURCHASE
        // -----------------------------------------------------

        if (selectedType == IAPButtonType.Purchase)
        {
            DrawLoadingSettings();

            EditorGUILayout.Space(8);

            DrawStorePriceSettings();

            EditorGUILayout.Space(8);

            DrawDisableSettings();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // =========================================================
    // Button Type
    // =========================================================

    private void DrawButtonType()
    {
        EditorGUILayout.LabelField("[IAP Manager]", LabelStyles.GetHeaderStyle());
        EditorGUILayout.PropertyField(
            buttonType,
            new GUIContent(
                "Button Type",
                "Select whether this is a purchase or restore button."
            )
        );
    }

    // =========================================================
    // Loading
    // =========================================================

    private void DrawLoadingSettings()
    {
        EditorGUILayout.LabelField(
            "Loading", LabelStyles.GetLabelStyle());

        EditorGUILayout.PropertyField(
            loadingPanel,
            new GUIContent(
                "Loading Panel",
                "Shown while purchase/restore operation is processing."
            )
        );
    }

    // =========================================================
    // Store Price
    // =========================================================

    private void DrawStorePriceSettings()
    {
        EditorGUILayout.LabelField(
            "Store Price",
            LabelStyles.GetLabelStyle()
        );

        EditorGUILayout.PropertyField(
            showPriceFromStore,
            new GUIContent(
                "Show Price From Store",
                "Display the localized product price received from the store."
            )
        );

        if (!showPriceFromStore.boolValue)
            return;

        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(
            priceText,
            new GUIContent(
                "Price Text",
                "TMP text used to display the localized store price."
            )
        );

        EditorGUI.indentLevel--;
    }

    // =========================================================
    // Disable Purchased Product
    // =========================================================

    private void DrawDisableSettings()
    {
        EditorGUILayout.LabelField(
            "Purchased State",
            LabelStyles.GetLabelStyle()
        );

        EditorGUILayout.PropertyField(
            disableOnPurchase,
            new GUIContent(
                "Disable On Purchase",
                "Disable this object or assigned objects when the product is already owned."
            )
        );

        if (!disableOnPurchase.boolValue)
            return;

        EditorGUI.indentLevel++;

        // -----------------------------------------------------
        // Self
        // -----------------------------------------------------

        EditorGUILayout.PropertyField(
            self,
            new GUIContent(
                "Disable Self",
                "Disable this GameObject after purchase."
            )
        );

        if (self.boolValue)
        {
            // -------------------------------------------------
            // Product Type
            // -------------------------------------------------

            EditorGUILayout.PropertyField(
                disableProductType,
                new GUIContent(
                    "Product Type",
                    "Maps to the configured store product ID. None uses the GameObject name."
                )
            );

            if (disableProductType.enumValueIndex ==
                (int)IAPProductType.None)
            {
                EditorGUILayout.HelpBox(
                    "Product Type is None. The GameObject name will be used as the ownership key.",
                    MessageType.Info
                );
            }

            EditorGUILayout.Space(3);
        }

        // -----------------------------------------------------
        // External objects
        // -----------------------------------------------------

        if (!self.boolValue)
        {
            EditorGUI.indentLevel++;

            if (disableObjects != null)
            {
                EditorGUILayout.PropertyField(
                    disableObjects,
                    new GUIContent(
                        "Objects To Disable"
                    ),
                    true
                );
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.indentLevel--;
    }
}

#endif

#if UNITY_EDITOR && ACS_IAP

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IAPManager))]
public sealed class IAPManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "IAPManager is created before the first scene and uses the generated IAP product registry. " +
            "Add IAPButton to purchase or restore UI controls.",
            MessageType.Info);

        if (GUILayout.Button("Open IAP Product Registry"))
        {
            EditorApplication.ExecuteMenuItem("ACS/IAP/Product Registry");
        }
    }
}

#endif

using UnityEditor;

namespace ACS.Editor
{
    internal sealed class ACSDefineSyncPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            EditorApplication.delayCall += ACSDefineManager.SyncDefines;
        }
    }
}

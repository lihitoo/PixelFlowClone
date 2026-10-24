using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace PixelFlow.Editor
{
    public static partial class PixelFlowFoundationAuthoring
    {
        [MenuItem("Pixel Flow/Foundation/Register Authored Shop")]
        public static void AuthorNativeShop() => RegisterPanelAddress(HomeShopAuthoring.ShopPath, PixelFlowPanelAddress.Shop);

        internal static void RegisterPanelAddress(string assetPath, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) throw new System.InvalidOperationException("Addressables settings are missing.");
            foreach (var group in settings.groups)
                if (group != null)
                    foreach (var old in new System.Collections.Generic.List<AddressableAssetEntry>(group.entries))
                        if (old.address == address && old.guid != AssetDatabase.AssetPathToGUID(assetPath)) settings.RemoveAssetEntry(old.guid);
            var destination = settings.FindGroup("PixelFlow UI") ?? settings.CreateGroup("PixelFlow UI", false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), destination);
            entry.address = address;
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        }
    }
}

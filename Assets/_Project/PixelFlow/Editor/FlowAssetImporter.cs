using UnityEditor;
using UnityEngine;

namespace PixelFlow.Editor
{
    // Restricted to selected owned PixelFlow derivatives; original export stays outside Assets.
    public sealed class FlowAssetImporter : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/PixelFlow/Resources/PixelFlow/";
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root + "UI/", System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter; importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.maxTextureSize = assetPath.Contains("/Home/") || assetPath.Contains("/Shop/") ? 2048 : 512;
        }
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root + "Audio/", System.StringComparison.Ordinal)) return;
            var importer = (AudioImporter)assetImporter; var settings = importer.defaultSampleSettings;
            settings.loadType = assetPath.EndsWith(".ogg", System.StringComparison.Ordinal) ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
        }
    }
}

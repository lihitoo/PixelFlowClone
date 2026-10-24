using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelFlow.Editor
{
    // Narrow, reviewable commands for the connected Editor; never starts a player build.
    public static class HomeReviewCommands
    {
        [Serializable] private sealed class Intake { public Entry[] receipts; }
        [Serializable] private sealed class Entry { public string role, output; public float[] sourceBorder; }
        [Serializable] private sealed class Binding { public string role, path, guid; public Vector4 border; }
        [Serializable] private sealed class Bindings { public Binding[] assets; }
        private const string Evidence = "Docs/ExactClone/Home/";
        private const string ScopeKey = "PixelFlow.HomeReview.PreviousScope";
        private const string OwnedKey = "PixelFlow.HomeReview.Owned";

        [MenuItem("Pixel Flow/Home Review/Prepare Test Evidence")]
        public static void PrepareTestEvidence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            Environment.SetEnvironmentVariable("PIXEL_FLOW_REVIEW_RUN", "home-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }

        [MenuItem("Pixel Flow/Home Review/Game View Portrait 1080x1920")]
        public static void Portrait()
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var activeGroup = sizesType.GetProperty("currentGroupType", flags).GetValue(sizes);
            var group = sizesType.GetMethod("GetGroup", flags).Invoke(sizes, new[] { activeGroup });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
            var kind = assembly.GetType("UnityEditor.GameViewSizeType", true);
            var size = Activator.CreateInstance(sizeType, flags, null, new[] { Enum.Parse(kind, "FixedResolution"), (object)1080, 1920, "PixelFlow Home Review" }, null);
            group.GetType().GetMethod("AddCustomSize", flags).Invoke(group, new[] { size });
            int index = (int)group.GetType().GetMethod("GetTotalCount", flags).Invoke(group, null) - 1;
            var gameViewType = assembly.GetType("UnityEditor.GameView", true);
            var window = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetProperty("selectedSizeIndex", flags).SetValue(window, index);
            window.Focus(); window.Repaint();
        }

        [MenuItem("Pixel Flow/Home Review/Prepare Original Assets")]
        public static void PrepareAssets() => PrepareAssetsAt(Evidence + "home-asset-intake.json", Evidence + "home-import-bindings.json");
        [MenuItem("Pixel Flow/Shop Review/Prepare Original Assets")]
        public static void PrepareShopAssets() => PrepareAssetsAt("Docs/ExactClone/Shop/shop-asset-intake.json", "Docs/ExactClone/Shop/shop-import-bindings.json");
        private static void PrepareAssetsAt(string sourcePath, string outputPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before asset preparation.");
            var intake = JsonUtility.FromJson<Intake>(File.ReadAllText(sourcePath));
            var result = new Bindings { assets = new Binding[intake.receipts.Length] };
            for (int i = 0; i < intake.receipts.Length; i++)
            {
                var entry = intake.receipts[i];
                var importer = AssetImporter.GetAtPath(entry.output) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing importer: " + entry.output);
                var b = entry.sourceBorder;
                var border = new Vector4(b[0], b[1], b[2], b[3]);
                if (importer.spriteBorder != border || importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spriteBorder = border; importer.SaveAndReimport();
                }
                result.assets[i] = new Binding { role = entry.role, path = entry.output, guid = AssetDatabase.AssetPathToGUID(entry.output), border = border };
            }
            File.WriteAllText(outputPath, JsonUtility.ToJson(result, true));
            Debug.Log("Prepared original UI sprites: " + result.assets.Length);
        }

        [MenuItem("Pixel Flow/Home Review/Begin Isolated Review")]
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Already playing.");
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/_Project/Scenes/BootstrapScene.unity" || scene.isDirty)
                throw new InvalidOperationException("Expected the saved template BootstrapScene (Init).");
            SessionState.SetString(ScopeKey, Environment.GetEnvironmentVariable("PIXEL_FLOW_EDITOR_SAVE_SCOPE") ?? "");
            SessionState.SetBool(OwnedKey, true);
            Environment.SetEnvironmentVariable("PIXEL_FLOW_EDITOR_SAVE_SCOPE", "HomeReview_" + Guid.NewGuid().ToString("N"));
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Pixel Flow/Home Review/End Isolated Review")]
        public static void End()
        {
            if (!SessionState.GetBool(OwnedKey, false)) throw new InvalidOperationException("No owned Home review session.");
            EditorApplication.isPlaying = false;
        }

        [MenuItem("Pixel Flow/Home Review/Fixture Home Level 36")]
        public static void Fixture36() => Fixture(35);
        [MenuItem("Pixel Flow/Home Review/Fixture Home Level 101")]
        public static void Fixture101() => Fixture(100);
        private static void Fixture(int level)
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool(OwnedKey, false)) throw new InvalidOperationException("Begin an isolated review first.");
            var app = UnityEngine.Object.FindFirstObjectByType<PixelFlowApp>();
            if (!app.SaveDirectory.StartsWith("PixelFlowCloneSmoke_HomeReview_", StringComparison.Ordinal)) throw new InvalidOperationException("Expected isolated save.");
            app.Progress.Data.UnlockedLevel = level; app.Progress.Data.Coins = 2000;
            app.Progress.Save();
            typeof(PixelFlowApp).GetMethod("Home", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(app, null);
            typeof(PixelFlowApp).GetMethod("SelectLevel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(app, new object[] { level });
            Debug.Log("Review fixture only: seeded level " + (level + 1) + "; this is not earned progression.");
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged -= RestoreScope;
            EditorApplication.playModeStateChanged += RestoreScope;
        }
        private static void RestoreScope(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(OwnedKey, false)) return;
            string previous = SessionState.GetString(ScopeKey, "");
            Environment.SetEnvironmentVariable("PIXEL_FLOW_EDITOR_SAVE_SCOPE", string.IsNullOrEmpty(previous) ? null : previous);
            SessionState.SetBool(OwnedKey, false);
        }
    }
}

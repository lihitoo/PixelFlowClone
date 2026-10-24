using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace PixelFlow.Editor
{
    public static class PixelFlowBuild
    {
        private const string Root = "Assets/_Project/PixelFlow";
        public const string ScenePath = Root + "/Scenes/PixelFlow.unity";

        [MenuItem("Pixel Flow/Create or Open Game Scene")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            EditorSceneManager.OpenScene(PixelFlowFoundationAuthoring.InitScenePath);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PixelFlowFoundationAuthoring.InitScenePath);
        }

        // Historical extraction/authoring recipe; not a runtime entry or update command.
        private static void PrepareLegacyPresentation()
        {
            Directory.CreateDirectory(Root + "/Generated"); Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(Root + "/Art", "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.maxTextureSize = 1024; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                if (!path.EndsWith("Pig.png", StringComparison.OrdinalIgnoreCase))
                {
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    if (path.Contains("Button")) importer.spriteBorder = new Vector4(22, 22, 22, 22);
                }
                importer.SaveAndReimport();
            }
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(Root + "/Art/Pig.obj");
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None; modelImporter.globalScale = 1; modelImporter.SaveAndReimport();
            string fontPath = Root + "/Generated/PixelFlowFont.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Art/RobotoBold.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                font.name = "PixelFlow Roboto Bold";
                string characters = new string(Enumerable.Range(32, 95).Select(x => (char)x).ToArray());
                if (!font.TryAddCharacters(characters, out string missing)) throw new InvalidOperationException("Font glyphs missing: " + missing);
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(font, fontPath);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
                AssetDatabase.AddObjectToAsset(font.material, font);
            }
            string materialPath = Root + "/Generated/PixelFlowLit.mat";
            var lit = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (lit == null)
            {
                lit = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "PixelFlow Lit" };
                lit.SetFloat("_Smoothness", .25f); AssetDatabase.CreateAsset(lit, materialPath);
            }
            string presentationPath = Root + "/Generated/PixelFlowPresentation.asset";
            var presentation = AssetDatabase.LoadAssetAtPath<PixelFlowPresentation>(presentationPath);
            if (presentation == null)
            {
                presentation = ScriptableObject.CreateInstance<PixelFlowPresentation>();
                AssetDatabase.CreateAsset(presentation, presentationPath);
            }
            presentation.PigMesh = AssetDatabase.LoadAllAssetsAtPath(Root + "/Art/Pig.obj").OfType<Mesh>().First();
            presentation.PigTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Pig.png");
            presentation.Font = font; presentation.LitMaterial = lit;
            presentation.Background = new Color(.19215687f, .3019608f, .4745098f);
            presentation.Logo = LoadSprite("Logo"); presentation.HeroPig = LoadSprite("HeroPig");
            presentation.PlayButton = LoadSprite("PlayButton"); presentation.BlueButton = LoadSprite("BlueButton");
            presentation.Tap = LoadClip("Tap"); presentation.Pop = LoadClip("Pop"); presentation.Launch = LoadClip("Launch"); presentation.Win = LoadClip("Win");
            EditorUtility.SetDirty(presentation); AssetDatabase.SaveAssets();

            const string pipelinePath = Root + "/Generated/PixelFlowPipeline.asset";
            if (!File.Exists(pipelinePath)) AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset", pipelinePath);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null) throw new InvalidOperationException("Template mobile pipeline missing.");
            pipeline.renderScale = 1; pipeline.msaaSampleCount = 4; EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.companyName = "Dreamy"; PlayerSettings.productName = "Pixel Flow Clone";
            PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.dreamy.pixelflowclone");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.dreamy.pixelflowclone");

            Scene scene;
            if (File.Exists(ScenePath)) scene = EditorSceneManager.OpenScene(ScenePath);
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 8.4f;
                camera.transform.position = new Vector3(0, 16, -10); camera.transform.LookAt(Vector3.zero);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = presentation.Background;
                camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                var light = new GameObject("Soft Key Light", typeof(Light)).GetComponent<Light>(); light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(48, -28, 0); light.intensity = 1.15f; light.shadows = LightShadows.Soft; light.shadowStrength = .35f;
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.72f, .78f, .84f);
                var app = new GameObject("Pixel Flow", typeof(PixelFlowApp)).GetComponent<PixelFlowApp>();
                var serialized = new SerializedObject(app); serialized.FindProperty("presentation").objectReferenceValue = presentation;
                serialized.FindProperty("worldCamera").objectReferenceValue = camera; serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            // Refresh persistent identities after imports, then wire both new and existing
            // owned scenes. A build must not ship a transient/null ScriptableObject reference.
            presentation = AssetDatabase.LoadAssetAtPath<PixelFlowPresentation>(presentationPath);
            if (presentation == null || !AssetDatabase.Contains(presentation))
                throw new InvalidOperationException("Presentation is not a persistent project asset.");
            var sceneApp = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PixelFlowApp>(true)).Single();
            var sceneCamera = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Camera>(true)).Single();
            sceneCamera.backgroundColor = presentation.Background;
            var bindings = new SerializedObject(sceneApp);
            bindings.FindProperty("presentation").objectReferenceValue = presentation;
            bindings.FindProperty("worldCamera").objectReferenceValue = sceneCamera;
            bindings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sceneApp); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath);
            scene = EditorSceneManager.OpenScene(ScenePath);
            sceneApp = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PixelFlowApp>(true)).Single();
            bindings = new SerializedObject(sceneApp);
            if (bindings.FindProperty("presentation").objectReferenceValue == null || bindings.FindProperty("worldCamera").objectReferenceValue == null)
                throw new InvalidOperationException("Clone scene failed the save/reopen reference check.");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                        throw new InvalidOperationException("Missing script in clone scene: " + transform.name);
            PixelFlowFoundationAuthoring.UseTemplateScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[PixelFlow] Scene and presentation prepared: " + ScenePath);
        }

        private static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/" + name + ".png");
        private static AudioClip LoadClip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/" + name + ".wav");

        [MenuItem("Pixel Flow/Build Windows Playable")]
        public static void Windows()
        {
            Prepare(); Directory.CreateDirectory("Builds/Windows");
            EditorUserBuildSettings.development = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PixelFlowFoundationAuthoring.InitScenePath, PixelFlowFoundationAuthoring.MainScenePath }, locationPathName = "Builds/Windows/PixelFlowClone.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None,
                extraScriptingDefines = new[] { "PIXEL_FLOW_VERIFICATION" }
            });
            WriteReport(report, "Docs/Evidence/windows-build.json");
        }

        [MenuItem("Pixel Flow/Build Android Development")]
        public static void Android()
        {
            Prepare(); Directory.CreateDirectory("Builds/Android");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { PixelFlowFoundationAuthoring.InitScenePath, PixelFlowFoundationAuthoring.MainScenePath }, locationPathName = "Builds/Android/PixelFlowClone-dev.apk",
                target = BuildTarget.Android, options = BuildOptions.Development
            });
            WriteReport(report, "Docs/Evidence/android-build.json");
        }

        private static void WriteReport(BuildReport report, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var summary = report.summary;
            string json = "{\n  \"result\": \"" + summary.result + "\",\n  \"target\": \"" + summary.platform + "\",\n  \"bytes\": " + summary.totalSize +
                ",\n  \"errors\": " + summary.totalErrors + ",\n  \"warnings\": " + summary.totalWarnings + ",\n  \"unity\": \"" + Application.unityVersion + "\",\n  \"development\": " + ((summary.options & BuildOptions.Development) != 0).ToString().ToLowerInvariant() + "\n}\n";
            File.WriteAllText(path, json);
            if (summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed: " + summary.result);
        }
    }
}

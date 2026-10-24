using System;
using System.Linq;
using Dreamy.Feedback;
using Dreamy.Audio;
using Dreamy.Template;
using Dreamy.Template.Home;
using Dreamy.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelFlow.Editor
{
    public static partial class PixelFlowFoundationAuthoring
    {
        public const string InitScenePath = "Assets/_Project/Scenes/BootstrapScene.unity";
        public const string MainScenePath = "Assets/_Project/Scenes/MainScene.unity";

        [MenuItem("Pixel Flow/Foundation/Use Template Init And Main Scenes")]
        public static void UseTemplateScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before scene authoring.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene edits before migrating startup.");

            // Retain the template scene assets/GUIDs and its authored UI layers.
            var main = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var manager = main.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PanelManager>(true)).Single();
            var app = main.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PixelFlowApp>(true)).SingleOrDefault();
            if (app == null)
            {
                var source = EditorSceneManager.OpenScene(Root + "/Scenes/PixelFlow.unity", OpenSceneMode.Additive);
                var sourceApp = source.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PixelFlowApp>(true)).Single();
                // Replace only the template demonstration camera/light with the
                // verified game's camera/light; retain all template UI objects.
                foreach (var root in main.GetRootGameObjects())
                    if (root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null)
                        UnityEngine.Object.DestroyImmediate(root);
                foreach (var root in source.GetRootGameObjects())
                    if (root == sourceApp.gameObject || root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null)
                    {
                        var copy = UnityEngine.Object.Instantiate(root); copy.name = root.name;
                        SceneManager.MoveGameObjectToScene(copy, main);
                    }
                app = main.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PixelFlowApp>(true)).Single();
                // Cross-root camera references must point into MainScene.
                var appSo = new SerializedObject(app);
                appSo.FindProperty("worldCamera").objectReferenceValue = main.GetRootGameObjects().Select(x => x.GetComponent<Camera>()).Single(x => x != null);
                appSo.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.CloseScene(source, true);
            }
            SceneManager.SetActiveScene(main);
            foreach (var bootstrap in main.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<HomeBootstrap>(true)))
                bootstrap.enabled = false; // Native Pixel Flow Home replaces the sample Home.
            foreach (var events in main.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<EventSystem>(true)))
            {
                var legacy = events.GetComponent<StandaloneInputModule>();
                if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);
                var input = events.GetComponent<InputSystemUIInputModule>() ?? events.gameObject.AddComponent<InputSystemUIInputModule>();
                input.AssignDefaultActions();
            }
            var canvas = manager.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = manager.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            var application = new SerializedObject(app); application.FindProperty("uiCanvas").objectReferenceValue = canvas;
            application.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(main); EditorSceneManager.SaveScene(main);

            var init = EditorSceneManager.OpenScene(InitScenePath, OpenSceneMode.Single);
            var installer = UnityEngine.Object.FindFirstObjectByType<GameInstaller>();
            if (installer == null) throw new InvalidOperationException("Template bootstrap installer is missing.");
            // This consumer initializes Audio at the composition root after
            // configuration, so the sample Awake bootstrap must not compete.
            foreach (var audio in init.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<AudioBootstrap>(true)))
                UnityEngine.Object.DestroyImmediate(audio);
            var profile = installer.GetComponent<PixelFlowInstallerProfile>() ?? installer.gameObject.AddComponent<PixelFlowInstallerProfile>();
            var profileSo = new SerializedObject(profile);
            profileSo.FindProperty("audioCues").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PixelFlowAudioCues>(AudioRoot + "/PixelFlowAudioCues.asset");
            profileSo.ApplyModifiedPropertiesWithoutUndo();
            var rootSo = new SerializedObject(installer); rootSo.FindProperty("gameCompositionProfile").objectReferenceValue = profile;
            rootSo.FindProperty("feedbackHostPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/_Feature/Dreamy Feedback/0.4.0/Basic Feedback/Generated/Rigs/GameFeedbackRig.prefab").GetComponent<FeedbackHost>();
            rootSo.ApplyModifiedPropertiesWithoutUndo();
            var gameInit = UnityEngine.Object.FindFirstObjectByType<GameInit>();
            var initSo = new SerializedObject(gameInit); initSo.FindProperty("playTemplateMusic").boolValue = false; initSo.ApplyModifiedPropertiesWithoutUndo();
            foreach (var legacy in UnityEngine.Object.FindObjectsByType<StandaloneInputModule>(FindObjectsSortMode.None))
            {
                var owner = legacy.gameObject; UnityEngine.Object.DestroyImmediate(legacy);
                (owner.GetComponent<InputSystemUIInputModule>() ?? owner.AddComponent<InputSystemUIInputModule>()).AssignDefaultActions();
            }
            EditorSceneManager.MarkSceneDirty(init); EditorSceneManager.SaveScene(init);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(InitScenePath, true), new EditorBuildSettingsScene(MainScenePath, true) };
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(InitScenePath);
            Selection.activeGameObject = installer.gameObject;
            Debug.Log("Template startup configured: BootstrapScene (Init) -> GameInstaller.Ready -> GameInit -> SceneLoader -> MainScene -> PixelFlow Home. No installer in MainScene.");
        }
    }
}

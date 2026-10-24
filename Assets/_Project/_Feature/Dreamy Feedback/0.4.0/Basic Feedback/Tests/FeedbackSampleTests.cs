using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Dreamy.Feedback.Samples.Tests
{
    public sealed class FeedbackSampleTests
    {
        [UnityTest] public IEnumerator CommonPresetsPlayWithStarterRigAndSupportStopComplete()
        {
#if UNITY_EDITOR
            var guid = System.Array.Find(AssetDatabase.FindAssets("GameFeedbackRig t:Prefab"),
                id => AssetDatabase.GUIDToAssetPath(id).Contains("Basic Feedback"));
            Assert.That(guid, Is.Not.Null);
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var cameraObject = new GameObject("Preset test camera", typeof(Camera));
            var anchor = new GameObject("Preset test HUD", typeof(RectTransform));
            try
            {
                var host = instance.GetComponent<FeedbackHost>();
                host.Initialize(cameraObject.GetComponent<Camera>());
                Assert.That(host.Vfx, Is.Not.Null);
                Assert.That(host.FloatingText, Is.Not.Null);
                string folder = Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace("\\", "/") + "/Definitions";
                var definitions = System.Array.FindAll(AssetDatabase.FindAssets("t:FeedbackDefinition", new[] { folder }), id => !AssetDatabase.GUIDToAssetPath(id).Contains("/Groups/"));
                Assert.That(definitions.Length, Is.EqualTo(13));
                foreach (var definitionGuid in definitions)
                {
                    var definition = AssetDatabase.LoadAssetAtPath<FeedbackDefinition>(AssetDatabase.GUIDToAssetPath(definitionGuid));
                    Assert.That(FeedbackGraphValidation.Validate(definition), Is.Null, definition.name);
                    var request = FeedbackRequestBuilder.For(definition).From(anchor).To(anchor.transform)
                        .At(Vector3.zero).IconsFrom(anchor.transform).WithAmount(3).Build();
                    var handle = host.Feedback.Play(request);
                    float deadline = Time.realtimeSinceStartup + 4;
                    while (handle.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.That(handle.Status, Is.EqualTo(FeedbackStatus.Completed), definition.name);
                    handle = host.Feedback.Play(request); handle.Complete();
                    Assert.That(handle.Status, Is.EqualTo(FeedbackStatus.Completed), definition.name);
                    handle = host.Feedback.Play(request); handle.Stop();
                    Assert.That(handle.Status, Is.EqualTo(FeedbackStatus.Stopped), definition.name);
                }
            }
            finally { Object.Destroy(instance); Object.Destroy(cameraObject); Object.Destroy(anchor); }
#else
            Assert.Ignore("Preset assets are loaded through AssetDatabase."); yield return null;
#endif
        }
        [UnityTest] public IEnumerator BasicScenePlaysAllChannelsAndClears()
        {
#if UNITY_EDITOR
            var guid = System.Array.Find(AssetDatabase.FindAssets("FeedbackDemo t:Scene"), id => AssetDatabase.GUIDToAssetPath(id).Contains("Basic Feedback"));
            Assert.That(guid, Is.Not.Null);
            EditorSceneManager.LoadSceneInPlayMode(AssetDatabase.GUIDToAssetPath(guid), new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var controls = Object.FindFirstObjectByType<FeedbackDemoControls>(); Assert.That(controls, Is.Not.Null);
            var host = Object.FindFirstObjectByType<FeedbackHost>(); Assert.That(host.IsInitialized, Is.True);
            foreach (var gameObject in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var child in gameObject.GetComponentsInChildren<Transform>(true)) Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero, child.name);
            Assert.That(Resources.Load<TMP_Settings>("TMP Settings"), Is.Not.Null, "Import TMP Essential Resources once before running the samples.");
            Canvas.ForceUpdateCanvases();
            foreach (var button in controls.GetComponentsInChildren<UnityEngine.UI.Button>()) Assert.That(((RectTransform)button.transform).rect.width, Is.GreaterThan(150), button.name);
            for (int i = 0; i < 4; i++) { controls.PlayReward(i); yield return null; controls.CompleteAll(); }
            for (int i = 0; i < controls.ChannelCount; i++) controls.PlayChannel(i);
            controls.PlayAll(); yield return null;
            controls.StopAll(); yield return null;
            var root = host.GetComponent<FeedbackRoot>();
            Assert.That(root.WorldVfxRoot.childCount, Is.Zero); Assert.That(root.FloatingTextRoot.childCount, Is.Zero); Assert.That(root.IconFlyRoot.GetComponentsInChildren<IconFlyInstance>(true), Is.Empty); Assert.That(root.UiVfxRoot.childCount, Is.Zero);
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) { Assert.That(text.font, Is.Not.Null, text.name);  }
            Assert.That(AssetDatabase.LoadAssetAtPath<Material>(Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guid))) + "/RewardParticles.mat").shader.name, Is.EqualTo("Dreamy/Feedback/SampleParticles"));
            Assert.That(host.transform.Find("PuzzleBoard"), Is.Null);
            yield return Capture("starter-portrait", 1080, 1920);
            yield return Capture("starter-landscape", 1920, 1080);
#else
            Assert.Ignore("Scene import check uses Editor scene loading."); yield return null;
#endif
        }
        [UnityTest] public IEnumerator CameraShakeMovesWorldProjectionAndRestoresRig()
        {
#if UNITY_EDITOR
            var path=System.Array.Find(AssetDatabase.FindAssets("FeedbackDemo t:Scene"),id=>AssetDatabase.GUIDToAssetPath(id).Contains("Basic Feedback"));
            EditorSceneManager.LoadSceneInPlayMode(AssetDatabase.GUIDToAssetPath(path),new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var host=Object.FindFirstObjectByType<FeedbackHost>();var camera=host.GetComponentInChildren<Camera>();
            var worldPoint=host.transform.Find("WorldTarget").position;
            var before=camera.WorldToViewportPoint(worldPoint);var rig=host.GetComponent<FeedbackRoot>().CameraRoot;var basis=rig.localPosition;
            var h=host.CameraShake.Shake("small");yield return null;yield return null;
            Assert.That(Vector3.Distance(camera.WorldToViewportPoint(worldPoint),before),Is.GreaterThan(.0001f));
            h.Complete();Assert.That(rig.localPosition,Is.EqualTo(basis));Assert.That(h.Status,Is.EqualTo(FeedbackStatus.Completed));
#else
            Assert.Ignore("Scene fixture requires Editor.");yield return null;
#endif
        }
        [UnityTest] public IEnumerator GameRigWorksWithExistingHudAndPlayerOwner()
        {
#if UNITY_EDITOR
            var guid=System.Array.Find(AssetDatabase.FindAssets("GameFeedbackRig t:Prefab"),id=>AssetDatabase.GUIDToAssetPath(id).Contains("Basic Feedback"));
            Assert.That(guid,Is.Not.Null);
            var rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
            var owner=new GameObject("RewardPanel",typeof(RectTransform));
            var source=new GameObject("RewardSource",typeof(RectTransform)); source.transform.SetParent(owner.transform,false);
            var target=new GameObject("ExistingHud",typeof(RectTransform)); target.transform.SetParent(owner.transform,false);
            try
            {
                Assert.That(rig.GetComponentsInChildren<Camera>(true),Is.Empty);
                Assert.That(rig.GetComponentsInChildren<SpriteRenderer>(true),Is.Empty);
                Assert.That(rig.GetComponent<FeedbackSampleDefinitions>(),Is.Null);
                Assert.That(rig.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true),Is.Empty);
                var host=rig.GetComponent<FeedbackHost>(); Assert.That(host.IsInitialized,Is.True);
                var player=owner.AddComponent<FeedbackPlayer>();
                var folder=Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guid));
                var data=new SerializedObject(player);
                data.FindProperty("host").objectReferenceValue=host;
                data.FindProperty("asset").objectReferenceValue=AssetDatabase.LoadAssetAtPath<FeedbackDefinition>(Path.GetDirectoryName(folder)+"/Definitions/Rewards/StarReward.asset");
                data.FindProperty("iconSource").objectReferenceValue=source.transform;
                data.FindProperty("useRuntimeCount").boolValue=true; data.FindProperty("iconCount").intValue=4;
                data.FindProperty("target").objectReferenceValue=target.transform; data.ApplyModifiedPropertiesWithoutUndo();
                player.Play(); yield return null;
                Assert.That(player.LastHandle.IsRunning,Is.True);
                Assert.That(host.GetComponent<FeedbackRoot>().IconFlyRoot.GetComponentsInChildren<IconFlyInstance>(),Has.Length.EqualTo(4));
                owner.SetActive(false); yield return null; yield return null;
                Assert.That(player.LastHandle.Status,Is.EqualTo(FeedbackStatus.Stopped));
            }
            finally { Object.Destroy(rig); Object.Destroy(owner); }
#else
            Assert.Ignore("Prefab fixture requires Editor.");yield return null;
#endif
        }
        public static IEnumerator Capture(string name, int width, int height)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var camera = Object.FindFirstObjectByType<Camera>();
            var target = new RenderTexture(width, height, 24); target.Create();
            var originalTarget = camera.targetTexture; camera.targetTexture = target;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length]; var cameras = new Camera[canvases.Length]; var distances = new float[canvases.Length];
            var scalers = Object.FindObjectsByType<UnityEngine.UI.CanvasScaler>(FindObjectsSortMode.None);
            var scaleModes = new UnityEngine.UI.CanvasScaler.ScaleMode[scalers.Length]; var scales = new float[scalers.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera; distances[i] = canvases[i].planeDistance;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 1;
            }
            for (int i = 0; i < scalers.Length; i++)
            {
                scaleModes[i] = scalers[i].uiScaleMode; scales[i] = scalers[i].scaleFactor;
                scalers[i].uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
                scalers[i].scaleFactor = Mathf.Sqrt((float)width / 1080 * height / 1920);
            }
            try
            {
                for (int i = 0; i < 3; i++) yield return null;
                Canvas.ForceUpdateCanvases();
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                else camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply(); RenderTexture.active = previous;
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../.omo/evidence/feedback-modular")); Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG()); Object.Destroy(texture);
            }
            finally
            {
                camera.targetTexture = originalTarget;
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i]; }
                for (int i = 0; i < scalers.Length; i++) { scalers[i].uiScaleMode = scaleModes[i]; scalers[i].scaleFactor = scales[i]; }
                target.Release(); Object.Destroy(target);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Dreamy.Feedback.Samples.Tests
{
    public sealed class FeedbackDemoInteractionTests
    {
        [UnityTest] public IEnumerator EveryChannelAndRewardButtonDispatchesThroughRealRaycast()
        {
#if UNITY_EDITOR
            var guid = AssetDatabase.FindAssets("FeedbackDemo t:Scene").First(g => AssetDatabase.GUIDToAssetPath(g).Contains("Basic Feedback"));
            EditorSceneManager.LoadSceneInPlayMode(AssetDatabase.GUIDToAssetPath(guid), new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var controls = Object.FindFirstObjectByType<FeedbackDemoControls>();
            var data = new SerializedObject(controls);
            var channels = data.FindProperty("channelButtons");
            Assert.That(channels.arraySize, Is.EqualTo(14));
            for (int i = 0; i < channels.arraySize; i++)
            {
                var button = (Button)channels.GetArrayElementAtIndex(i).objectReferenceValue;
                int before = controls.PlayCount;
                yield return Click(button);
                Assert.That(controls.PlayCount, Is.EqualTo(before + 1), button.name);
                Assert.That(controls.LastHandle.IsValid, Is.True, controls.Status);
                Assert.That(controls.Status, Does.Not.Contain("Unavailable"), button.name);
                if (i == 0)
                {
                    var host = Object.FindFirstObjectByType<FeedbackHost>();
                    Assert.That(host.GetComponent<FeedbackRoot>().WorldVfxRoot.GetComponentsInChildren<ParticleSystem>().Any(p => p.isPlaying), Is.True);
                }
                if (i == 2)
                {
                    var host = Object.FindFirstObjectByType<FeedbackHost>();
                    Assert.That(host.GetComponent<FeedbackRoot>().FloatingTextRoot.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "+100"), Is.True);
                }
                if (i == 3 || i >= 7 && i <= 10)
                {
                    var host = Object.FindFirstObjectByType<FeedbackHost>();
                    Assert.That(host.GetComponent<FeedbackRoot>().IconFlyRoot.GetComponentsInChildren<Image>().Length, Is.GreaterThan(0));
                }
                controls.CompleteAll();
                Assert.That(controls.LastHandle.Status, Is.EqualTo(FeedbackStatus.Completed), button.name);
            }
            var uiPreview = (Button)data.FindProperty("uiPreviewButton").objectReferenceValue;
            Assert.That(uiPreview, Is.Not.Null);
            var previewCount = controls.PlayCount;
            yield return Click(uiPreview);
            Assert.That(controls.PlayCount, Is.EqualTo(previewCount + 2), controls.Status);
            Assert.That(controls.LastHandle.IsValid, Is.True, controls.Status);
            var previewRoot = Object.FindFirstObjectByType<FeedbackHost>().GetComponent<FeedbackRoot>();
            Assert.That(previewRoot.UiVfxRoot.GetComponentInChildren<UiBurstVfxInstance>(), Is.Not.Null);
            Assert.That(previewRoot.FloatingTextRoot.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "+321"), Is.True);
            controls.CompleteAll();
            var rewards = data.FindProperty("rewardButtons");
            Assert.That(rewards.arraySize, Is.EqualTo(4));
            for (int i = 0; i < rewards.arraySize; i++)
            {
                yield return Click((Button)rewards.GetArrayElementAtIndex(i).objectReferenceValue);
                Assert.That(controls.LastHandle.IsRunning, Is.True);
                controls.CompleteAll();
            }
            yield return Click((Button)data.FindProperty("playAllButton").objectReferenceValue);
            yield return Click((Button)data.FindProperty("stopButton").objectReferenceValue);
            Assert.That(controls.Status, Does.Contain("Stopped"));
            yield return Click((Button)channels.GetArrayElementAtIndex(3).objectReferenceValue);
            yield return Click((Button)data.FindProperty("completeButton").objectReferenceValue);
            Assert.That(controls.LastHandle.Status, Is.EqualTo(FeedbackStatus.Completed));
            controls.gameObject.SetActive(false); yield return null;
            controls.gameObject.SetActive(true); yield return null;
            int count = controls.PlayCount;
            yield return Click((Button)channels.GetArrayElementAtIndex(3).objectReferenceValue);
            Assert.That(controls.PlayCount, Is.EqualTo(count + 1), "Reopen must not multiply listeners");
            controls.StopAll();
#else
            Assert.Ignore("Fixture loads the imported scene through Editor."); yield return null;
#endif
        }

        private static IEnumerator Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var scroll in button.GetComponentsInParent<ScrollRect>())
            {
                var viewport = scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform;
                Vector2 center = viewport.InverseTransformPoint(((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
                if (!viewport.rect.Contains(center))
                {
                    Vector2 delta = viewport.rect.center - center;
                    if (!scroll.horizontal) delta.x = 0; if (!scroll.vertical) delta.y = 0;
                    scroll.StopMovement();
                    scroll.content.anchoredPosition += (Vector2)scroll.content.parent.InverseTransformVector(viewport.TransformVector(delta));
                }
            }
            yield return null; Canvas.ForceUpdateCanvases();
            var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            var rect = (RectTransform)button.transform;
            var position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), button.name);
            var actual = hits[0].gameObject.GetComponentInParent<Button>();
            Assert.That(actual, Is.SameAs(button), "Top raycast must reach " + button.name);
            ExecuteEvents.Execute(actual.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(actual.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(actual.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}

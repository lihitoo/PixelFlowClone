using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelFlow.Editor
{
    [InitializeOnLoad]
    internal static class PixelFlowEditorStartup
    {
        private const string OpenedKey = "PixelFlow.OpenedCloneSceneThisSession";
        static PixelFlowEditorStartup()
        {
            if (!Application.isBatchMode && !SessionState.GetBool(OpenedKey, false)) EditorApplication.update += OpenWhenIdle;
        }
        private static void OpenWhenIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= OpenWhenIdle;
            SessionState.SetBool(OpenedKey, true);
            var active = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !string.IsNullOrEmpty(active.path) || active.isDirty || !File.Exists(PixelFlowFoundationAuthoring.InitScenePath)) return;
            EditorSceneManager.OpenScene(PixelFlowFoundationAuthoring.InitScenePath);
            Debug.Log("[PixelFlow] Opened the clone scene in the interactive Editor. No player build requested.");
        }
    }
}

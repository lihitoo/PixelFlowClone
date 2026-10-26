using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace PixelFlow.EditorReview
{
    // Test Runner callbacks are re-registered after EnterPlayMode domain reloads.
    // The MCP job wrapper alone cannot retain the completed run across that reload.
    internal sealed class ReviewResultRecorder : ICallbacks
    {
        internal const string OutputKey = "PixelFlow.Review.ResultPath";

        [InitializeOnLoadMethod]
        private static void Register() => TestRunnerApi.RegisterTestCallback(new ReviewResultRecorder());

        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            string simulator = SessionState.GetString("PixelFlow.Review.SimulatorType", "");
            SessionState.EraseString("PixelFlow.Review.SimulatorType");
            if (!string.IsNullOrEmpty(simulator) && System.Type.GetType(simulator) is System.Type simulatorType)
                EditorApplication.delayCall += () => EditorWindow.GetWindow(simulatorType);
            string path = SessionState.GetString(OutputKey, "");
            if (string.IsNullOrEmpty(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            TestRunnerApi.SaveResultToFile(result, path);
            SessionState.EraseString(OutputKey);
        }
    }
}

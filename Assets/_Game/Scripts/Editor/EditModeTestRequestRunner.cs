using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    public static class EditModeTestRequestRunner
    {
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopEditModeTests.request"));

        private static TestRunnerApi runner;
        private static TestCallbacks callbacks;

        static EditModeTestRequestRunner()
        {
            EditorApplication.delayCall += TryRun;
        }

        private static void TryRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath))
            {
                return;
            }

            File.Delete(RequestPath);
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            callbacks = new TestCallbacks();
            runner.RegisterCallbacks(callbacks);
            runner.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode
            }));
        }

        private sealed class TestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
                string message = $"Total {total}, Passed {result.PassCount}, Failed {result.FailCount}, Skipped {result.SkipCount}.";
                if (result.FailCount == 0)
                {
                    Debug.Log($"SWINGPOP_EDITMODE_TESTS_PASS: {message}");
                }
                else
                {
                    Debug.LogError($"SWINGPOP_EDITMODE_TESTS_FAIL: {message}");
                }

                if (runner != null)
                {
                    runner.UnregisterCallbacks(callbacks);
                    Object.DestroyImmediate(runner);
                    runner = null;
                    callbacks = null;
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}

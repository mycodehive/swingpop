using System.Collections.Generic;
using SwingPop.Gameplay.Ball;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SwingPop.Editor
{
    public static class M1ValidationTools
    {
        private static TestRunnerApi testRunnerApi;
        private static EditModeTestCallbacks testCallbacks;

        [MenuItem("SwingPop/M1/Run EditMode Tests _F7")]
        public static void RunEditModeTests()
        {
            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testCallbacks = new EditModeTestCallbacks();
            testRunnerApi.RegisterCallbacks(testCallbacks);
            testRunnerApi.Execute(new ExecutionSettings(new Filter
            {
                assemblyNames = new[] { "SwingPop.Tests.EditMode" },
                testMode = TestMode.EditMode
            }));
        }

        [MenuItem("SwingPop/M1/Run Play Physics Validation _F8")]
        public static void RunPlayPhysicsValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M1 physics validation.");
                return;
            }

            if (Object.FindFirstObjectByType<M1PlayModeValidationDriver>() != null)
            {
                Debug.LogWarning("M1 physics validation is already running.");
                return;
            }

            GolfBallController ball = Object.FindFirstObjectByType<GolfBallController>();
            if (ball == null)
            {
                Debug.LogError("SWINGPOP_M1_PLAYMODE_VALIDATION_FAIL: GolfBallController was not found.");
                return;
            }

            GameObject driverObject = new("M1 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M1PlayModeValidationDriver>().Begin(ball);
        }

        private sealed class EditModeTestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log("SwingPop M1 EditMode tests started.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Passed)
                {
                    Debug.Log($"SWINGPOP_M1_EDITMODE_TESTS_PASS: {result.PassCount} passed, {result.FailCount} failed.");
                }
                else
                {
                    Debug.LogError($"SWINGPOP_M1_EDITMODE_TESTS_FAIL: {result.PassCount} passed, {result.FailCount} failed. {result.Message}");
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogError($"M1 test failed: {result.FullName}\n{result.Message}\n{result.StackTrace}");
                }
            }
        }
    }

    internal sealed class M1PlayModeValidationDriver : MonoBehaviour
    {
        private const float TimeoutSeconds = 30f;

        private readonly List<BallState> observedStates = new();
        private GolfBallController ball;
        private float startTime;
        private bool finished;

        public void Begin(GolfBallController targetBall)
        {
            ball = targetBall;
            observedStates.Add(ball.State);
            ball.StateChanged += OnStateChanged;
            startTime = Time.realtimeSinceStartup;

            if (!ball.Launch())
            {
                FinishWithFailure("Launch command was rejected from Ready state.");
            }
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            if (ball.State == BallState.Stopped)
            {
                ValidateSequenceAndReset();
                return;
            }

            if (Time.realtimeSinceStartup - startTime >= TimeoutSeconds)
            {
                FinishWithFailure($"Timed out in {ball.State} at speed {ball.Speed:F3} m/s.");
            }
        }

        private void OnDestroy()
        {
            if (ball != null)
            {
                ball.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(BallState previousState, BallState nextState)
        {
            observedStates.Add(nextState);
        }

        private void ValidateSequenceAndReset()
        {
            BallState[] expectedStates =
            {
                BallState.Ready,
                BallState.Airborne,
                BallState.Bouncing,
                BallState.Rolling,
                BallState.Stopped
            };

            bool sequenceMatches = observedStates.Count == expectedStates.Length;
            for (int index = 0; sequenceMatches && index < expectedStates.Length; index++)
            {
                sequenceMatches = observedStates[index] == expectedStates[index];
            }

            if (!sequenceMatches)
            {
                FinishWithFailure($"Unexpected state sequence: {string.Join(" -> ", observedStates)}");
                return;
            }

            Vector3 stoppedPosition = ball.transform.position;
            float elapsedSeconds = Time.realtimeSinceStartup - startTime;
            ball.ResetBall();
            if (ball.State != BallState.Ready || Vector3.Distance(ball.transform.position, ball.ResetPosition) > 0.001f)
            {
                FinishWithFailure("Reset did not restore the Ready state and original position.");
                return;
            }

            finished = true;
            Debug.Log(
                $"SWINGPOP_M1_PLAYMODE_VALIDATION_PASS: Ready -> Airborne -> Bouncing -> Rolling -> Stopped -> Reset/Ready " +
                $"in {elapsedSeconds:F2}s. Stop position {stoppedPosition:F2}.");
            StopValidationPlayMode();
        }

        private void FinishWithFailure(string reason)
        {
            finished = true;
            Debug.LogError($"SWINGPOP_M1_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopValidationPlayMode();
        }

        private void StopValidationPlayMode()
        {
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}

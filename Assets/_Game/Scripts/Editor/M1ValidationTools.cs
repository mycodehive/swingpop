using System.Collections.Generic;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Shot;
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

            if (Object.FindAnyObjectByType<M1PlayModeValidationDriver>() != null)
            {
                Debug.LogWarning("M1 physics validation is already running.");
                return;
            }

            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
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

        [MenuItem("SwingPop/M2/Run Shot Integration Validation _F9")]
        public static void RunM2ShotIntegrationValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M2 shot integration validation.");
                return;
            }

            if (Object.FindAnyObjectByType<M2PlayModeValidationDriver>() != null)
            {
                Debug.LogWarning("M2 shot integration validation is already running.");
                return;
            }

            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            if (ball == null || shotFlow == null || ball.Tuning == null)
            {
                Debug.LogError("SWINGPOP_M2_PLAYMODE_VALIDATION_FAIL: M2 scene dependencies were not found.");
                return;
            }

            GameObject driverObject = new("M2 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M2PlayModeValidationDriver>().Begin(ball, shotFlow);
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
                    Debug.Log($"SWINGPOP_EDITMODE_TESTS_PASS: {result.PassCount} passed, {result.FailCount} failed.");
                }
                else
                {
                    Debug.LogError($"SWINGPOP_EDITMODE_TESTS_FAIL: {result.PassCount} passed, {result.FailCount} failed. {result.Message}");
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

    internal sealed class M2PlayModeValidationDriver : MonoBehaviour
    {
        private const float ShotTimeoutSeconds = 30f;
        private const float FlowPowerSelectionSeconds = 0.75f;
        private const float FlowImpactSelectionSeconds = 0.455f;

        private readonly List<BallState> observedStates = new();
        private readonly List<ShotResult> results = new();
        private GolfBallController ball;
        private ShotFlowController shotFlow;
        private ShotTuningData shotTuning;
        private ValidationPhase phase;
        private float phaseStartTime;
        private int scenarioIndex;
        private ShotCommand currentCommand;
        private bool finished;

        private enum ValidationPhase
        {
            WaitingForInitialState,
            SelectingFlowPower,
            SelectingFlowImpact,
            WaitingForBall,
            WaitingToStartNext
        }

        private readonly struct ShotResult
        {
            public ShotResult(ShotCommand command, Vector3 displacement)
            {
                Command = command;
                Displacement = displacement;
            }

            public ShotCommand Command { get; }
            public Vector3 Displacement { get; }
            public float PlanarDistance => new Vector2(Displacement.x, Displacement.z).magnitude;
        }

        public void Begin(GolfBallController targetBall, ShotFlowController targetShotFlow)
        {
            ball = targetBall;
            shotFlow = targetShotFlow;
            shotTuning = GetShotTuning();
            if (shotTuning == null)
            {
                FinishWithFailure("ShotTuningData was not assigned to ShotFlowController.");
                return;
            }

            ball.StateChanged += OnBallStateChanged;
            phase = ValidationPhase.WaitingForInitialState;
            phaseStartTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            float elapsed = Time.realtimeSinceStartup - phaseStartTime;
            switch (phase)
            {
                case ValidationPhase.WaitingForInitialState:
                    if (shotFlow.State == ShotFlowState.Aiming && ball.State == BallState.Ready)
                    {
                        observedStates.Add(ball.State);
                        shotFlow.ConfirmCurrentStep();
                        if (shotFlow.State != ShotFlowState.PowerSelecting)
                        {
                            FinishWithFailure("Aiming did not transition to PowerSelecting.");
                            return;
                        }

                        phase = ValidationPhase.SelectingFlowPower;
                        phaseStartTime = Time.realtimeSinceStartup;
                    }
                    else if (elapsed >= 2f)
                    {
                        FinishWithFailure(
                            $"Timed out waiting for initial Aiming/Ready state. Flow={shotFlow.State}, Ball={ball.State}.");
                    }

                    break;
                case ValidationPhase.SelectingFlowPower when elapsed >= FlowPowerSelectionSeconds:
                    shotFlow.ConfirmCurrentStep();
                    if (shotFlow.State != ShotFlowState.ImpactSelecting)
                    {
                        FinishWithFailure("PowerSelecting did not transition to ImpactSelecting.");
                        return;
                    }

                    phase = ValidationPhase.SelectingFlowImpact;
                    phaseStartTime = Time.realtimeSinceStartup;
                    break;
                case ValidationPhase.SelectingFlowImpact when elapsed >= FlowImpactSelectionSeconds:
                    shotFlow.ConfirmCurrentStep();
                    if (shotFlow.State != ShotFlowState.ShotCommitted || !shotFlow.HasLastShotCommand)
                    {
                        FinishWithFailure("ImpactSelecting did not create a ShotCommand and commit the shot.");
                        return;
                    }

                    currentCommand = shotFlow.LastShotCommand;
                    phase = ValidationPhase.WaitingForBall;
                    phaseStartTime = Time.realtimeSinceStartup;
                    break;
                case ValidationPhase.WaitingForBall:
                    if (ball.State == BallState.Stopped)
                    {
                        RecordCompletedShot();
                    }
                    else if (elapsed >= ShotTimeoutSeconds)
                    {
                        FinishWithFailure($"Scenario {scenarioIndex} timed out in {ball.State} at {ball.Speed:F3} m/s.");
                    }

                    break;
                case ValidationPhase.WaitingToStartNext when elapsed >= 0.2f:
                    StartDirectScenario();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (ball != null)
            {
                ball.StateChanged -= OnBallStateChanged;
            }
        }

        private void OnBallStateChanged(BallState previousState, BallState nextState)
        {
            observedStates.Add(nextState);
        }

        private void RecordCompletedShot()
        {
            if (!HasExpectedBallSequence())
            {
                FinishWithFailure($"Scenario {scenarioIndex} state sequence was {string.Join(" -> ", observedStates)}.");
                return;
            }

            results.Add(new ShotResult(currentCommand, ball.transform.position - ball.ResetPosition));
            shotFlow.ResetShot();
            if (shotFlow.State != ShotFlowState.Aiming || ball.State != BallState.Ready)
            {
                FinishWithFailure("Reset did not restore ShotFlow Aiming and Ball Ready.");
                return;
            }

            scenarioIndex++;
            if (scenarioIndex >= 4)
            {
                ValidateResults();
                return;
            }

            phase = ValidationPhase.WaitingToStartNext;
            phaseStartTime = Time.realtimeSinceStartup;
        }

        private void StartDirectScenario()
        {
            observedStates.Clear();
            observedStates.Add(ball.State);

            currentCommand = scenarioIndex switch
            {
                1 => CreateCommand(-20f, 0.35f, 0f),
                2 => CreateCommand(-20f, 0.9f, 0f),
                _ => CreateCommand(20f, 0.9f, 0.9f)
            };

            if (!ball.Launch(currentCommand))
            {
                FinishWithFailure($"Scenario {scenarioIndex} launch was rejected.");
                return;
            }

            phase = ValidationPhase.WaitingForBall;
            phaseStartTime = Time.realtimeSinceStartup;
        }

        private ShotCommand CreateCommand(float aimAngle, float power, float impactOffset)
        {
            BallTuningData ballTuning = ball.Tuning;
            return ShotCalculator.CreateCommand(
                Vector3.forward,
                aimAngle,
                power,
                impactOffset,
                shotTuning.PerfectMaximumOffset,
                shotTuning.GreatMaximumOffset,
                shotTuning.GoodMaximumOffset,
                shotTuning.PerfectPowerMultiplier,
                shotTuning.GreatPowerMultiplier,
                shotTuning.GoodPowerMultiplier,
                shotTuning.MissPowerMultiplier,
                shotTuning.GreatDispersionDegrees,
                shotTuning.GoodDispersionDegrees,
                shotTuning.MissDispersionDegrees,
                ballTuning.LaunchSpeed,
                ballTuning.LaunchAngleDegrees);
        }

        private bool HasExpectedBallSequence()
        {
            BallState[] expected =
            {
                BallState.Ready,
                BallState.Airborne,
                BallState.Bouncing,
                BallState.Rolling,
                BallState.Stopped
            };
            if (observedStates.Count != expected.Length)
            {
                return false;
            }

            for (int index = 0; index < expected.Length; index++)
            {
                if (observedStates[index] != expected[index])
                {
                    return false;
                }
            }

            return true;
        }

        private void ValidateResults()
        {
            ShotResult flowResult = results[0];
            ShotResult lowPower = results[1];
            ShotResult highPower = results[2];
            ShotResult miss = results[3];

            if (flowResult.Command.ImpactGrade != ImpactGrade.Perfect)
            {
                FinishWithFailure($"Timed flow shot expected Perfect but produced {flowResult.Command.ImpactGrade}.");
                return;
            }

            if (highPower.PlanarDistance <= lowPower.PlanarDistance * 1.5f)
            {
                FinishWithFailure(
                    $"High power distance {highPower.PlanarDistance:F2} was not clearly above low power {lowPower.PlanarDistance:F2}.");
                return;
            }

            if (lowPower.Displacement.x >= 0f || highPower.Displacement.x >= 0f || miss.Displacement.x <= 0f)
            {
                FinishWithFailure("Opposing aim angles did not produce opposing lateral stop positions.");
                return;
            }

            if (miss.Command.ImpactGrade != ImpactGrade.Miss
                || miss.Command.EffectivePower01 >= highPower.Command.EffectivePower01
                || Mathf.Approximately(miss.Command.DispersionDegrees, 0f))
            {
                FinishWithFailure("MISS did not apply deterministic dispersion and power loss.");
                return;
            }

            finished = true;
            Debug.Log(
                $"SWINGPOP_M2_PLAYMODE_VALIDATION_PASS: Flow {flowResult.Command}; " +
                $"Low {lowPower.PlanarDistance:F2}m, High {highPower.PlanarDistance:F2}m, " +
                $"Miss {miss.PlanarDistance:F2}m at X {miss.Displacement.x:F2}. Reset/Aiming restored.");
            StopValidationPlayMode();
        }

        private ShotTuningData GetShotTuning()
        {
            SerializedObject serializedFlow = new(shotFlow);
            return serializedFlow.FindProperty("shotTuning").objectReferenceValue as ShotTuningData;
        }

        private void FinishWithFailure(string reason)
        {
            finished = true;
            Debug.LogError($"SWINGPOP_M2_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopValidationPlayMode();
        }

        private void StopValidationPlayMode()
        {
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}

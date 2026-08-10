using System.Collections.Generic;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Shot;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    public static class M3ValidationTools
    {
        [MenuItem("SwingPop/M3/Run Spin Comparison Validation _F6")]
        public static void RunSpinComparisonValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M3 spin comparison validation.");
                return;
            }

            if (Object.FindAnyObjectByType<M3PlayModeValidationDriver>() != null)
            {
                Debug.LogWarning("M3 spin comparison validation is already running.");
                return;
            }

            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            if (ball == null || shotFlow == null || ball.Tuning == null)
            {
                Debug.LogError("SWINGPOP_M3_PLAYMODE_VALIDATION_FAIL: M3 scene dependencies were not found.");
                return;
            }

            GameObject driverObject = new("M3 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M3PlayModeValidationDriver>().Begin(ball, shotFlow);
        }
    }

    internal sealed class M3PlayModeValidationDriver : MonoBehaviour
    {
        private const float ShotTimeoutSeconds = 35f;
        private const float ShotPower = 0.82f;

        private static readonly SpinPreset[] Presets =
        {
            SpinPreset.NoSpin,
            SpinPreset.TopSpin,
            SpinPreset.BackSpin,
            SpinPreset.LeftSideSpin,
            SpinPreset.RightSideSpin
        };

        private readonly List<BallState> observedStates = new();
        private readonly List<ShotResult> results = new();
        private GolfBallController ball;
        private ShotFlowController shotFlow;
        private ShotTuningData shotTuning;
        private int scenarioIndex;
        private float shotStartTime;
        private float nextShotStartTime;
        private bool waitingToStart;
        private bool finished;
        private Vector3 currentLandingDisplacement;
        private float currentMaximumForwardZ;

        private readonly struct ShotResult
        {
            public ShotResult(
                SpinPreset preset,
                Vector3 displacement,
                Vector3 landingDisplacement,
                float maximumForwardZ,
                float elapsedSeconds)
            {
                Preset = preset;
                Displacement = displacement;
                LandingDisplacement = landingDisplacement;
                MaximumForwardZ = maximumForwardZ;
                ElapsedSeconds = elapsedSeconds;
            }

            public SpinPreset Preset { get; }
            public Vector3 Displacement { get; }
            public Vector3 LandingDisplacement { get; }
            public float MaximumForwardZ { get; }
            public float ElapsedSeconds { get; }
            public float PlanarDistance => new Vector2(Displacement.x, Displacement.z).magnitude;
        }

        public void Begin(GolfBallController targetBall, ShotFlowController targetShotFlow)
        {
            ball = targetBall;
            shotFlow = targetShotFlow;
            SerializedObject serializedFlow = new(shotFlow);
            shotTuning = serializedFlow.FindProperty("shotTuning").objectReferenceValue as ShotTuningData;
            if (shotTuning == null || ball.State != BallState.Ready)
            {
                FinishWithFailure("Validation requires ShotTuningData and a Ready ball.");
                return;
            }

            ball.StateChanged += OnBallStateChanged;
            StartScenario();
        }

        private void Update()
        {
            if (finished)
            {
                return;
            }

            if (waitingToStart)
            {
                if (Time.realtimeSinceStartup >= nextShotStartTime)
                {
                    waitingToStart = false;
                    StartScenario();
                }

                return;
            }

            if (ball.State == BallState.Stopped)
            {
                RecordScenario();
                return;
            }

            currentMaximumForwardZ = Mathf.Max(
                currentMaximumForwardZ,
                (ball.PhysicsPosition - ball.ResetPosition).z);

            if (Time.realtimeSinceStartup - shotStartTime >= ShotTimeoutSeconds)
            {
                FinishWithFailure(
                    $"{Presets[scenarioIndex]} timed out in {ball.State} at {ball.Speed:F3} m/s.");
            }
        }

        private void OnDestroy()
        {
            if (ball != null)
            {
                ball.StateChanged -= OnBallStateChanged;
            }
        }

        private void StartScenario()
        {
            observedStates.Clear();
            observedStates.Add(ball.State);
            currentLandingDisplacement = Vector3.zero;
            currentMaximumForwardZ = float.NegativeInfinity;
            ShotCommand command = CreateCommand(ShotSpin.FromPreset(Presets[scenarioIndex]));
            if (!ball.Launch(command))
            {
                FinishWithFailure($"{Presets[scenarioIndex]} launch was rejected.");
                return;
            }

            shotStartTime = Time.realtimeSinceStartup;
        }

        private void RecordScenario()
        {
            if (!HasExpectedStateSequence())
            {
                FinishWithFailure(
                    $"{Presets[scenarioIndex]} state sequence was {string.Join(" -> ", observedStates)}.");
                return;
            }

            results.Add(new ShotResult(
                Presets[scenarioIndex],
                ball.PhysicsPosition - ball.ResetPosition,
                currentLandingDisplacement,
                currentMaximumForwardZ,
                Time.realtimeSinceStartup - shotStartTime));
            shotFlow.ResetShot();
            if (ball.State != BallState.Ready || shotFlow.State != ShotFlowState.Aiming)
            {
                FinishWithFailure("Reset did not restore Ball Ready and ShotFlow Aiming.");
                return;
            }

            scenarioIndex++;
            if (scenarioIndex >= Presets.Length)
            {
                ValidateComparison();
                return;
            }

            waitingToStart = true;
            nextShotStartTime = Time.realtimeSinceStartup + 0.2f;
        }

        private ShotCommand CreateCommand(ShotSpin spin)
        {
            BallTuningData ballTuning = ball.Tuning;
            return ShotCalculator.CreateCommand(
                Vector3.forward,
                0f,
                ShotPower,
                0f,
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
                ballTuning.LaunchAngleDegrees,
                spin);
        }

        private void OnBallStateChanged(BallState previousState, BallState nextState)
        {
            observedStates.Add(nextState);
            if (nextState == BallState.Bouncing)
            {
                currentLandingDisplacement = ball.PhysicsPosition - ball.ResetPosition;
            }
        }

        private bool HasExpectedStateSequence()
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

        private void ValidateComparison()
        {
            ShotResult noSpin = results[0];
            ShotResult topSpin = results[1];
            ShotResult backSpin = results[2];
            ShotResult leftSpin = results[3];
            ShotResult rightSpin = results[4];

            if (topSpin.PlanarDistance <= noSpin.PlanarDistance + 2f)
            {
                FinishWithFailure(
                    $"TopSpin {topSpin.PlanarDistance:F2}m did not exceed NoSpin {noSpin.PlanarDistance:F2}m clearly.");
                return;
            }

            if (backSpin.PlanarDistance >= noSpin.PlanarDistance - 2f)
            {
                FinishWithFailure(
                    $"BackSpin {backSpin.PlanarDistance:F2}m did not finish clearly shorter than NoSpin {noSpin.PlanarDistance:F2}m.");
                return;
            }

            float backSpinRollbackDistance = backSpin.MaximumForwardZ - backSpin.Displacement.z;
            if (backSpinRollbackDistance < 0.15f)
            {
                FinishWithFailure(
                    $"BackSpin rollback was not visible enough: {backSpinRollbackDistance:F2}m.");
                return;
            }

            if (leftSpin.Displacement.x >= -2f || rightSpin.Displacement.x <= 2f
                || leftSpin.Displacement.x >= rightSpin.Displacement.x)
            {
                FinishWithFailure(
                    $"Side spin did not separate correctly: Left X {leftSpin.Displacement.x:F2}, Right X {rightSpin.Displacement.x:F2}.");
                return;
            }

            finished = true;
            Debug.Log(
                "SWINGPOP_M3_PLAYMODE_VALIDATION_PASS: " +
                $"NoSpin {noSpin.PlanarDistance:F2}m (land Z {noSpin.LandingDisplacement.z:F2})/{noSpin.ElapsedSeconds:F2}s; " +
                $"Top {topSpin.PlanarDistance:F2}m (land Z {topSpin.LandingDisplacement.z:F2})/{topSpin.ElapsedSeconds:F2}s; " +
                $"Back {backSpin.PlanarDistance:F2}m (land Z {backSpin.LandingDisplacement.z:F2}, rollback {backSpinRollbackDistance:F2})/{backSpin.ElapsedSeconds:F2}s; " +
                $"Left X {leftSpin.Displacement.x:F2}m; Right X {rightSpin.Displacement.x:F2}m. " +
                "All shots completed Ready -> Airborne -> Bouncing -> Rolling -> Stopped -> Reset/Aiming.");
            StopValidationPlayMode();
        }

        private void FinishWithFailure(string reason)
        {
            finished = true;
            Debug.LogError($"SWINGPOP_M3_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopValidationPlayMode();
        }

        private void StopValidationPlayMode()
        {
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    public static class M5ValidationTools
    {
        [MenuItem("SwingPop/M5/Run Hole Flow Validation _F12")]
        public static void RunHoleFlowValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M5 validation.");
                return;
            }

            if (Object.FindAnyObjectByType<M5PlayModeValidationDriver>() != null)
            {
                Debug.LogWarning("M5 validation is already running.");
                return;
            }

            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            if (ball == null || shotFlow == null || holeFlow == null || holeFlow.Hole == null)
            {
                Debug.LogError("SWINGPOP_M5_PLAYMODE_VALIDATION_FAIL: M5 scene dependencies were not found.");
                return;
            }

            GameObject driverObject = new("M5 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M5PlayModeValidationDriver>().Begin(ball, shotFlow, holeFlow);
        }
    }

    internal sealed class M5PlayModeValidationDriver : MonoBehaviour
    {
        private const float TimeoutSeconds = 35f;

        private readonly Dictionary<TerrainSurfaceType, float> lieDistances = new();
        private GolfBallController ball;
        private ShotFlowController shotFlow;
        private HoleFlowController holeFlow;
        private ShotTuningData shotTuning;
        private ClubData driver;
        private ClubData putter;
        private Vector3 observedStopPosition;
        private bool observedStopped;
        private bool finished;
        private ScoreResult puttingResult;
        private Vector3 continuousStop;

        public void Begin(
            GolfBallController targetBall,
            ShotFlowController targetShotFlow,
            HoleFlowController targetHoleFlow)
        {
            ball = targetBall;
            shotFlow = targetShotFlow;
            holeFlow = targetHoleFlow;

            SerializedObject serializedFlow = new(shotFlow);
            shotTuning = serializedFlow.FindProperty("shotTuning").objectReferenceValue as ShotTuningData;
            SerializedObject serializedHole = new(holeFlow);
            driver = serializedHole.FindProperty("normalClub").objectReferenceValue as ClubData;
            putter = serializedHole.FindProperty("putter").objectReferenceValue as ClubData;
            if (shotTuning == null || driver == null || putter == null)
            {
                Fail("Shot tuning or club data was not assigned.");
                return;
            }

            WindController wind = Object.FindAnyObjectByType<WindController>();
            wind?.SetPreset(WindPreset.Calm);
            ball.StateChanged += OnBallStateChanged;
            StartCoroutine(RunValidation());
        }

        private void OnDestroy()
        {
            if (ball != null)
            {
                ball.StateChanged -= OnBallStateChanged;
            }
        }

        private IEnumerator RunValidation()
        {
            holeFlow.DebugResetHole();
            yield return WaitFor(() => ball.State == BallState.Ready && shotFlow.State == ShotFlowState.Aiming, "initial Hole Start");
            if (finished) yield break;

            Vector3 teePosition = ball.PhysicsPosition;
            BeginStopObservation();
            if (!shotFlow.TryCommitShot(0.48f, 0f))
            {
                Fail("Continuous shot commit was rejected.");
                yield break;
            }

            yield return WaitFor(
                () => observedStopped && ball.State == BallState.Ready && shotFlow.State == ShotFlowState.Aiming,
                "continuous next-shot setup");
            if (finished) yield break;

            continuousStop = observedStopPosition;
            if (Vector3.Distance(continuousStop, teePosition) < 2f
                || Vector3.Distance(ball.PhysicsPosition, continuousStop) > 0.02f
                || holeFlow.StrokeCount != 1)
            {
                Fail($"Continuous flow failed. Tee {teePosition:F2}, stop {continuousStop:F2}, current {ball.PhysicsPosition:F2}, stroke {holeFlow.StrokeCount}.");
                yield break;
            }

            foreach (TerrainSurfaceType lie in new[]
                     {
                         TerrainSurfaceType.Fairway,
                         TerrainSurfaceType.Rough,
                         TerrainSurfaceType.Bunker
                     })
            {
                yield return RunLieScenario(lie);
                if (finished) yield break;
            }

            if (!(lieDistances[TerrainSurfaceType.Fairway] > lieDistances[TerrainSurfaceType.Rough]
                  && lieDistances[TerrainSurfaceType.Rough] > lieDistances[TerrainSurfaceType.Bunker]))
            {
                Fail(
                    $"Lie distance order failed: Fairway {lieDistances[TerrainSurfaceType.Fairway]:F2}, " +
                    $"Rough {lieDistances[TerrainSurfaceType.Rough]:F2}, Bunker {lieDistances[TerrainSurfaceType.Bunker]:F2}.");
                yield break;
            }

            yield return RunPuttingScenario();
            if (finished) yield break;

            yield return RunHazardScenario();
            if (finished) yield break;

            Complete();
        }

        private IEnumerator RunLieScenario(TerrainSurfaceType lie)
        {
            TerrainSurface surface = FindSurface(lie);
            if (surface == null)
            {
                Fail($"{lie} surface was not found.");
                yield break;
            }

            Bounds bounds = surface.GetComponent<Collider>().bounds;
            Vector3 start = new(bounds.center.x, bounds.max.y + 0.15f, bounds.min.z + 5f);
            ball.PrepareNextShot(start, surface.Data);
            ShotCommand command = CreateCommand(Vector3.forward, 0.62f, surface.Data, driver);
            BeginStopObservation();
            if (!ball.Launch(command))
            {
                Fail($"{lie} comparison launch was rejected.");
                yield break;
            }

            yield return WaitFor(() => observedStopped, $"{lie} comparison stop");
            if (finished) yield break;
            lieDistances[lie] = Vector3.ProjectOnPlane(observedStopPosition - start, Vector3.up).magnitude;
        }

        private IEnumerator RunPuttingScenario()
        {
            holeFlow.DebugResetHole();
            yield return null;
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green);
            if (green == null)
            {
                Fail("Green surface was not found.");
                yield break;
            }

            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 puttStart = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(puttStart, green.Data);
            shotFlow.PrepareNextShot(cup - puttStart, putter);
            if (shotFlow.CurrentClub == null || !shotFlow.CurrentClub.IsPutter)
            {
                Fail("Green did not select the Putter.");
                yield break;
            }

            if (!shotFlow.TryCommitShot(0.45f, 0f) || !shotFlow.LastShotCommand.IsPutter)
            {
                Fail("Putter shot commit was rejected or used the wrong club.");
                yield break;
            }

            yield return WaitFor(
                () => holeFlow.State == HoleFlowState.HoleComplete && ball.State == BallState.Holed,
                "Putt Hole In");
            if (finished) yield break;

            puttingResult = holeFlow.Result;
            if (holeFlow.StrokeCount != 1 || puttingResult.Label != "Albatross")
            {
                Fail($"Hole result mismatch: stroke {holeFlow.StrokeCount}, result {puttingResult}.");
            }
        }

        private IEnumerator RunHazardScenario()
        {
            holeFlow.DebugResetHole();
            yield return WaitFor(() => ball.State == BallState.Ready, "hazard reset");
            if (finished) yield break;

            BeginStopObservation();
            if (!shotFlow.TryCommitShot(0.42f, 0f))
            {
                Fail("Hazard setup shot was rejected.");
                yield break;
            }

            yield return WaitFor(
                () => observedStopped && ball.State == BallState.Ready && shotFlow.State == ShotFlowState.Aiming,
                "hazard setup valid stop");
            if (finished) yield break;
            Vector3 recovery = ball.PhysicsPosition;

            foreach (TerrainSurfaceType hazard in new[]
                     {
                         TerrainSurfaceType.Water,
                         TerrainSurfaceType.OutOfBounds
                     })
            {
                TerrainSurface zone = FindSurface(hazard);
                TerrainSurface fairway = FindSurface(TerrainSurfaceType.Fairway);
                if (zone == null || fairway == null)
                {
                    Fail($"{hazard} or Fairway surface was not found.");
                    yield break;
                }

                Bounds bounds = zone.GetComponent<Collider>().bounds;
                Vector3 start = new(bounds.center.x, 0.2f, bounds.min.z - 1.5f);
                ball.PrepareNextShot(start, fairway.Data);
                shotFlow.PrepareNextShot(Vector3.forward, driver);
                if (!shotFlow.TryCommitShot(0.65f, 0f))
                {
                    Fail($"{hazard} shot was rejected.");
                    yield break;
                }

                yield return WaitFor(
                    () => ball.State == BallState.Ready
                          && ball.HasLastHazard
                          && ball.LastHazard == hazard,
                    $"{hazard} recovery");
                if (finished) yield break;

                if (Vector3.Distance(ball.PhysicsPosition, recovery) > 0.03f)
                {
                    Fail($"{hazard} recovered to {ball.PhysicsPosition:F2} instead of {recovery:F2}.");
                    yield break;
                }
            }

            if (holeFlow.StrokeCount != 5 || holeFlow.PenaltyCount != 2)
            {
                Fail($"Hazard count mismatch: strokes {holeFlow.StrokeCount}, penalties {holeFlow.PenaltyCount}.");
            }
        }

        private IEnumerator WaitFor(System.Func<bool> condition, string label)
        {
            float started = Time.time;
            while (!condition())
            {
                if (Time.time - started >= TimeoutSeconds)
                {
                    Fail($"{label} timed out in Ball={ball.State}, Shot={shotFlow.State}, Hole={holeFlow.State}.");
                    yield break;
                }
                yield return null;
            }
        }

        private ShotCommand CreateCommand(
            Vector3 direction,
            float power,
            TerrainSurfaceData surface,
            ClubData club)
        {
            ShotCommand command = ShotCalculator.CreateCommand(
                direction,
                0f,
                power,
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
                ball.Tuning.LaunchSpeed,
                ball.Tuning.LaunchAngleDegrees,
                ShotSpin.None);
            command = ShotCalculator.ApplySurfacePowerModifier(command, surface.PowerModifier);
            return ShotCalculator.ApplyClub(command, club);
        }

        private TerrainSurface FindSurface(TerrainSurfaceType type)
        {
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>())
            {
                if (surface.SurfaceType == type)
                {
                    return surface;
                }
            }
            return null;
        }

        private void BeginStopObservation()
        {
            observedStopped = false;
            observedStopPosition = Vector3.zero;
        }

        private void OnBallStateChanged(BallState previousState, BallState nextState)
        {
            if (nextState == BallState.Stopped)
            {
                observedStopped = true;
                observedStopPosition = ball.PhysicsPosition;
            }
        }

        private void Complete()
        {
            finished = true;
            Debug.Log(
                "SWINGPOP_M5_PLAYMODE_VALIDATION_PASS: " +
                $"continuous shot stopped at {continuousStop:F2} and resumed Aiming without reset; " +
                $"lie distance Fairway {lieDistances[TerrainSurfaceType.Fairway]:F2}m > " +
                $"Rough {lieDistances[TerrainSurfaceType.Rough]:F2}m > " +
                $"Bunker {lieDistances[TerrainSurfaceType.Bunker]:F2}m; " +
                $"Green selected Putter and completed Hole In with {puttingResult}; " +
                "Water/OOB each added +1 penalty and recovered to Last Valid Position; next shot remained available.");
            StopPlayMode();
        }

        private void Fail(string reason)
        {
            if (finished)
            {
                return;
            }

            finished = true;
            Debug.LogError($"SWINGPOP_M5_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopPlayMode();
        }

        private static void StopPlayMode()
        {
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}

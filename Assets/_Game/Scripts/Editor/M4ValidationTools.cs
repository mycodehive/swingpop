using System.Collections.Generic;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    public static class M4ValidationTools
    {
        [MenuItem("SwingPop/M4/Run Wind Hazard Validation _F10")]
        public static void RunWindHazardValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M4 validation.");
                return;
            }

            if (Object.FindAnyObjectByType<M4PlayModeValidationDriver>() != null)
            {
                Debug.LogWarning("M4 validation is already running.");
                return;
            }

            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            WindController wind = Object.FindAnyObjectByType<WindController>();
            if (ball == null || shotFlow == null || wind == null || ball.Tuning == null)
            {
                Debug.LogError("SWINGPOP_M4_PLAYMODE_VALIDATION_FAIL: M4 scene dependencies were not found.");
                return;
            }

            GameObject driverObject = new("M4 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M4PlayModeValidationDriver>().Begin(ball, shotFlow, wind);
        }
    }

    internal sealed class M4PlayModeValidationDriver : MonoBehaviour
    {
        private const float ShotPower = 0.82f;
        private const float TimeoutSeconds = 35f;

        private static readonly WindPreset[] Presets =
        {
            WindPreset.Calm,
            WindPreset.Tailwind,
            WindPreset.Headwind,
            WindPreset.LeftCrosswind,
            WindPreset.RightCrosswind
        };

        private readonly List<WindResult> results = new();
        private readonly List<TerrainResult> terrainResults = new();
        private GolfBallController ball;
        private ShotFlowController shotFlow;
        private WindController wind;
        private ShotTuningData shotTuning;
        private int presetIndex;
        private float scenarioStartTime;
        private Vector3 landingDisplacement;
        private bool waitingForNext;
        private float nextStartTime;
        private ValidationPhase phase;
        private Vector3 terrainStartPosition;
        private float terrainFirstBounceSpeed;

        private enum ValidationPhase
        {
            Wind,
            TerrainFairway,
            TerrainRough,
            TerrainBunker,
            TerrainGreen,
            Water,
            OutOfBounds,
            Complete
        }

        private readonly struct WindResult
        {
            public WindResult(WindPreset preset, Vector3 landing, Vector3 stopped)
            {
                Preset = preset;
                Landing = landing;
                Stopped = stopped;
            }

            public WindPreset Preset { get; }
            public Vector3 Landing { get; }
            public Vector3 Stopped { get; }
        }

        private readonly struct TerrainResult
        {
            public TerrainResult(TerrainSurfaceType surface, float rolloutDistance, float firstBounceSpeed)
            {
                Surface = surface;
                RolloutDistance = rolloutDistance;
                FirstBounceSpeed = firstBounceSpeed;
            }

            public TerrainSurfaceType Surface { get; }
            public float RolloutDistance { get; }
            public float FirstBounceSpeed { get; }
        }

        public void Begin(GolfBallController targetBall, ShotFlowController targetFlow, WindController targetWind)
        {
            ball = targetBall;
            shotFlow = targetFlow;
            wind = targetWind;
            SerializedObject serializedFlow = new(shotFlow);
            shotTuning = serializedFlow.FindProperty("shotTuning").objectReferenceValue as ShotTuningData;
            if (shotTuning == null || ball.State != BallState.Ready)
            {
                Fail("Validation requires ShotTuningData and a Ready ball.");
                return;
            }

            ball.StateChanged += OnBallStateChanged;
            StartWindScenario();
        }

        private void Update()
        {
            if (phase == ValidationPhase.Complete)
            {
                return;
            }

            if (waitingForNext)
            {
                if (Time.time >= nextStartTime)
                {
                    waitingForNext = false;
                    StartCurrentPhase();
                }
                return;
            }

            if (Time.time - scenarioStartTime >= TimeoutSeconds)
            {
                Fail($"{phase} timed out in ball state {ball.State}.");
                return;
            }

            if (phase is ValidationPhase.TerrainFairway
                or ValidationPhase.TerrainRough
                or ValidationPhase.TerrainBunker
                or ValidationPhase.TerrainGreen
                && ball.State == BallState.Bouncing)
            {
                terrainFirstBounceSpeed = Mathf.Max(terrainFirstBounceSpeed, ball.Velocity.y);
            }

            if (ball.State != BallState.Stopped)
            {
                return;
            }

            switch (phase)
            {
                case ValidationPhase.Wind:
                    RecordWindScenario();
                    break;
                case ValidationPhase.TerrainFairway:
                case ValidationPhase.TerrainRough:
                case ValidationPhase.TerrainBunker:
                case ValidationPhase.TerrainGreen:
                    RecordTerrainScenario();
                    break;
                case ValidationPhase.Water:
                    ValidateHazard(TerrainSurfaceType.Water, ValidationPhase.OutOfBounds);
                    break;
                case ValidationPhase.OutOfBounds:
                    ValidateHazard(TerrainSurfaceType.OutOfBounds, ValidationPhase.Complete);
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

        private void StartCurrentPhase()
        {
            if (phase == ValidationPhase.Wind)
            {
                StartWindScenario();
            }
            else if (phase is ValidationPhase.TerrainFairway
                     or ValidationPhase.TerrainRough
                     or ValidationPhase.TerrainBunker
                     or ValidationPhase.TerrainGreen)
            {
                StartTerrainScenario(GetTerrainTypeForPhase());
            }
            else if (phase is ValidationPhase.Water or ValidationPhase.OutOfBounds)
            {
                StartHazardScenario(phase == ValidationPhase.Water
                    ? TerrainSurfaceType.Water
                    : TerrainSurfaceType.OutOfBounds);
            }
            else
            {
                Complete();
            }
        }

        private void StartWindScenario()
        {
            landingDisplacement = Vector3.zero;
            wind.SetPreset(Presets[presetIndex]);
            if (!ball.Launch(CreateCommand(Vector3.forward)))
            {
                Fail($"{Presets[presetIndex]} launch was rejected.");
                return;
            }
            scenarioStartTime = Time.time;
        }

        private void RecordWindScenario()
        {
            results.Add(new WindResult(
                Presets[presetIndex],
                landingDisplacement,
                ball.PhysicsPosition - ball.ResetPosition));
            shotFlow.ResetShot();
            presetIndex++;
            if (presetIndex < Presets.Length)
            {
                ScheduleNext(ValidationPhase.Wind);
                return;
            }

            if (!ValidateWindComparison())
            {
                return;
            }

            wind.SetPreset(WindPreset.Calm);
            ScheduleNext(ValidationPhase.TerrainFairway);
        }

        private bool ValidateWindComparison()
        {
            WindResult calm = results[0];
            WindResult tail = results[1];
            WindResult head = results[2];
            WindResult left = results[3];
            WindResult right = results[4];
            if (tail.Landing.z <= calm.Landing.z || head.Landing.z >= calm.Landing.z)
            {
                Fail($"Wind carry order failed: Head {head.Landing.z:F2}, Calm {calm.Landing.z:F2}, Tail {tail.Landing.z:F2}.");
                return false;
            }

            if (left.Landing.x >= -0.5f || right.Landing.x <= 0.5f)
            {
                Fail($"Crosswind direction failed: Left X {left.Landing.x:F2}, Right X {right.Landing.x:F2}.");
                return false;
            }

            return true;
        }

        private void StartTerrainScenario(TerrainSurfaceType surfaceType)
        {
            TerrainSurface target = FindSurface(surfaceType);
            if (target == null)
            {
                Fail($"{surfaceType} TerrainSurface was not found.");
                return;
            }

            Bounds bounds = target.GetComponent<Collider>().bounds;
            terrainStartPosition = new Vector3(bounds.center.x, 0.2f, bounds.min.z + 6f);
            terrainFirstBounceSpeed = 0f;
            Rigidbody body = ball.GetComponent<Rigidbody>();
            body.position = terrainStartPosition;
            ball.transform.position = terrainStartPosition;
            if (!ball.Launch(CreateCommand(Vector3.forward, 0.65f)))
            {
                Fail($"{surfaceType} rollout launch was rejected.");
                return;
            }

            scenarioStartTime = Time.time;
        }

        private void RecordTerrainScenario()
        {
            TerrainSurfaceType surfaceType = GetTerrainTypeForPhase();
            Vector3 displacement = Vector3.ProjectOnPlane(ball.PhysicsPosition - terrainStartPosition, Vector3.up);
            terrainResults.Add(new TerrainResult(surfaceType, displacement.magnitude, terrainFirstBounceSpeed));
            shotFlow.ResetShot();

            ValidationPhase next = phase switch
            {
                ValidationPhase.TerrainFairway => ValidationPhase.TerrainRough,
                ValidationPhase.TerrainRough => ValidationPhase.TerrainBunker,
                ValidationPhase.TerrainBunker => ValidationPhase.TerrainGreen,
                _ => ValidationPhase.Water
            };

            if (next == ValidationPhase.Water && !ValidateTerrainComparison())
            {
                return;
            }

            ScheduleNext(next);
        }

        private bool ValidateTerrainComparison()
        {
            TerrainResult fairway = terrainResults[0];
            TerrainResult rough = terrainResults[1];
            TerrainResult bunker = terrainResults[2];
            TerrainResult green = terrainResults[3];
            if (fairway.RolloutDistance <= rough.RolloutDistance
                || rough.RolloutDistance <= bunker.RolloutDistance)
            {
                Fail($"Terrain rollout order failed: Fairway {fairway.RolloutDistance:F2}, Rough {rough.RolloutDistance:F2}, Bunker {bunker.RolloutDistance:F2}.");
                return false;
            }

            if (green.FirstBounceSpeed >= fairway.FirstBounceSpeed)
            {
                Fail($"Green bounce {green.FirstBounceSpeed:F2} was not lower than Fairway {fairway.FirstBounceSpeed:F2}.");
                return false;
            }

            return true;
        }

        private TerrainSurfaceType GetTerrainTypeForPhase()
        {
            return phase switch
            {
                ValidationPhase.TerrainRough => TerrainSurfaceType.Rough,
                ValidationPhase.TerrainBunker => TerrainSurfaceType.Bunker,
                ValidationPhase.TerrainGreen => TerrainSurfaceType.Green,
                _ => TerrainSurfaceType.Fairway
            };
        }

        private void StartHazardScenario(TerrainSurfaceType hazard)
        {
            TerrainSurface target = FindSurface(hazard);
            if (target == null)
            {
                Fail($"{hazard} TerrainSurface was not found.");
                return;
            }

            Rigidbody body = ball.GetComponent<Rigidbody>();
            Bounds bounds = target.GetComponent<Collider>().bounds;
            Vector3 start = new(bounds.center.x, 0.2f, bounds.min.z - 2f);
            body.position = start;
            ball.transform.position = start;
            if (!ball.Launch(CreateCommand(Vector3.forward)))
            {
                Fail($"{hazard} validation launch was rejected.");
                return;
            }
            scenarioStartTime = Time.time;
        }

        private void ValidateHazard(TerrainSurfaceType expected, ValidationPhase next)
        {
            if (!ball.HasLastHazard || ball.LastHazard != expected)
            {
                Fail($"Expected {expected}, got {(ball.HasLastHazard ? ball.LastHazard.ToString() : "None")}.");
                return;
            }

            shotFlow.ResetShot();
            if (ball.State != BallState.Ready || shotFlow.State != ShotFlowState.Aiming)
            {
                Fail($"{expected} recovery did not restore Ball Ready / ShotFlow Aiming.");
                return;
            }

            if (next == ValidationPhase.Complete)
            {
                Complete();
            }
            else
            {
                ScheduleNext(next);
            }
        }

        private TerrainSurface FindSurface(TerrainSurfaceType type)
        {
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsSortMode.None))
            {
                if (surface.SurfaceType == type)
                {
                    return surface;
                }
            }
            return null;
        }

        private ShotCommand CreateCommand(Vector3 direction, float power = ShotPower)
        {
            BallTuningData ballTuning = ball.Tuning;
            return ShotCalculator.CreateCommand(
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
                ballTuning.LaunchSpeed,
                ballTuning.LaunchAngleDegrees,
                ShotSpin.None);
        }

        private void OnBallStateChanged(BallState previousState, BallState nextState)
        {
            if (phase == ValidationPhase.Wind && nextState == BallState.Bouncing)
            {
                landingDisplacement = ball.PhysicsPosition - ball.ResetPosition;
            }
            else if (phase is ValidationPhase.TerrainFairway
                     or ValidationPhase.TerrainRough
                     or ValidationPhase.TerrainBunker
                     or ValidationPhase.TerrainGreen
                     && nextState == BallState.Bouncing)
            {
                terrainFirstBounceSpeed = Mathf.Max(0f, ball.Velocity.y);
            }
        }

        private void ScheduleNext(ValidationPhase next)
        {
            phase = next;
            waitingForNext = true;
            nextStartTime = Time.time + 0.25f;
        }

        private void Complete()
        {
            phase = ValidationPhase.Complete;
            WindResult calm = results[0];
            WindResult tail = results[1];
            WindResult head = results[2];
            WindResult left = results[3];
            WindResult right = results[4];
            TerrainResult fairway = terrainResults[0];
            TerrainResult rough = terrainResults[1];
            TerrainResult bunker = terrainResults[2];
            TerrainResult green = terrainResults[3];
            Debug.Log(
                "SWINGPOP_M4_PLAYMODE_VALIDATION_PASS: " +
                $"landing Head {head.Landing.z:F2}m < Calm {calm.Landing.z:F2}m < Tail {tail.Landing.z:F2}m; " +
                $"crosswind Left X {left.Landing.x:F2}m / Right X {right.Landing.x:F2}m; " +
                $"rollout Fairway {fairway.RolloutDistance:F2}m > Rough {rough.RolloutDistance:F2}m > Bunker {bunker.RolloutDistance:F2}m; " +
                $"bounce Green {green.FirstBounceSpeed:F2}m/s < Fairway {fairway.FirstBounceSpeed:F2}m/s; " +
                "Water and OutOfBounds stopped safely and Reset restored Ready/Aiming.");
            StopPlayMode();
        }

        private void Fail(string reason)
        {
            phase = ValidationPhase.Complete;
            Debug.LogError($"SWINGPOP_M4_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopPlayMode();
        }

        private static void StopPlayMode()
        {
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    public static class M6ValidationTools
    {
        [MenuItem("SwingPop/M6/Preview 12m Putt Camera _F3")]
        public static void PreviewLongPuttCamera()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before previewing the Putt camera.");
                return;
            }

            CameraDirector director = Object.FindAnyObjectByType<CameraDirector>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green);
            if (director == null || ball == null || shotFlow == null || holeFlow == null || green == null)
            {
                Debug.LogError("M6 Putt preview requires CameraDirector, Ball, ShotFlow, HoleFlow, and Green.");
                return;
            }

            SerializedObject serializedHole = new(holeFlow);
            ClubData putter = serializedHole.FindProperty("putter").objectReferenceValue as ClubData;
            if (putter == null)
            {
                Debug.LogError("M6 Putt preview could not find the assigned Putter.");
                return;
            }

            if (holeFlow.State != HoleFlowState.Playing)
            {
                holeFlow.DebugResetHole();
            }

            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 start = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 12f);
            ball.PrepareNextShot(start, green.Data);
            shotFlow.PrepareNextShot(cup - start, putter);
            director.RequestDebugMode(CameraMode.Putt);

            ShotDebugOverlay overlay = Object.FindAnyObjectByType<ShotDebugOverlay>();
            if (overlay != null)
            {
                SerializedObject serializedOverlay = new(overlay);
                serializedOverlay.FindProperty("showOverlay").boolValue = false;
                serializedOverlay.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("SWINGPOP_M6_PUTT_PREVIEW: 12m Green Putt prepared; Ball and Cup should both be visible. Exit Play Mode to restore the Debug overlay.");
        }

        [MenuItem("SwingPop/M6/Run Camera Flow Validation _F4")]
        public static void RunCameraFlowValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M6 camera validation.");
                return;
            }

            if (Object.FindAnyObjectByType<M6PlayModeValidationDriver>() != null)
            {
                Debug.LogWarning("M6 camera validation is already running.");
                return;
            }

            CameraDirector director = Object.FindAnyObjectByType<CameraDirector>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            if (director == null || ball == null || shotFlow == null || holeFlow == null)
            {
                Debug.LogError("SWINGPOP_M6_PLAYMODE_VALIDATION_FAIL: M6 scene dependencies were not found.");
                return;
            }

            GameObject driverObject = new("M6 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M6PlayModeValidationDriver>().Begin(director, ball, shotFlow, holeFlow);
        }

        private static TerrainSurface FindSurface(TerrainSurfaceType type)
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
    }

    internal sealed class M6PlayModeValidationDriver : MonoBehaviour
    {
        private const float TimeoutSeconds = 40f;

        private readonly List<CameraMode> observedModes = new();
        private CameraDirector director;
        private GolfBallController ball;
        private ShotFlowController shotFlow;
        private HoleFlowController holeFlow;
        private ClubData putter;
        private bool finished;
        private float maximumObservedFollowDistance;
        private float minimumObservedFov = float.MaxValue;
        private float maximumObservedFov;

        public void Begin(
            CameraDirector targetDirector,
            GolfBallController targetBall,
            ShotFlowController targetShotFlow,
            HoleFlowController targetHoleFlow)
        {
            director = targetDirector;
            ball = targetBall;
            shotFlow = targetShotFlow;
            holeFlow = targetHoleFlow;
            SerializedObject serializedHole = new(holeFlow);
            putter = serializedHole.FindProperty("putter").objectReferenceValue as ClubData;
            if (putter == null)
            {
                Fail("Putter data was not assigned.");
                return;
            }

            director.ModeChanged += OnModeChanged;
            StartCoroutine(RunValidation());
        }

        private void Update()
        {
            if (director == null)
            {
                return;
            }

            if (director.CurrentMode == CameraMode.BallFollow)
            {
                maximumObservedFollowDistance = Mathf.Max(maximumObservedFollowDistance, director.CurrentFollowDistance);
            }
            minimumObservedFov = Mathf.Min(minimumObservedFov, director.CurrentFieldOfView);
            maximumObservedFov = Mathf.Max(maximumObservedFov, director.CurrentFieldOfView);
        }

        private void OnDestroy()
        {
            if (director != null)
            {
                director.ModeChanged -= OnModeChanged;
            }
        }

        private IEnumerator RunValidation()
        {
            holeFlow.SetAutomaticFlowSuspended(false);
            holeFlow.DebugResetHole();
            yield return null;
            ObserveCurrentMode();
            if (director.CurrentMode != CameraMode.HoleIntro)
            {
                Fail($"Expected HoleIntro after reset, got {director.CurrentMode}.");
                yield break;
            }

            director.SkipIntro();
            yield return WaitFor(() => director.CurrentMode == CameraMode.Address, "Address after HoleIntro");
            if (finished) yield break;
            yield return WaitFor(() => director.CurrentMode == CameraMode.Aim, "Aim after Address hold");
            if (finished) yield break;

            shotFlow.ConfirmCurrentStep();
            yield return WaitFor(() => director.CurrentMode == CameraMode.Swing, "Swing during gauge selection");
            if (finished) yield break;
            shotFlow.CancelToAiming();
            yield return WaitFor(() => director.CurrentMode == CameraMode.Aim, "Aim after cancel");
            if (finished) yield break;

            if (!shotFlow.TryCommitShot(0.5f, 0f))
            {
                Fail("Normal shot commit was rejected.");
                yield break;
            }

            yield return WaitFor(() => director.CurrentMode == CameraMode.Impact, "Impact camera");
            if (finished) yield break;
            yield return WaitFor(() => observedModes.Contains(CameraMode.BallFollow), "BallFollow camera");
            if (finished) yield break;
            yield return WaitFor(() => observedModes.Contains(CameraMode.Landing), "Landing camera");
            if (finished) yield break;
            yield return WaitFor(() => observedModes.Contains(CameraMode.NextShot), "NextShot camera");
            if (finished) yield break;
            yield return WaitFor(
                () => ball.State == BallState.Ready && director.CurrentMode is CameraMode.Address or CameraMode.Aim,
                "next-shot Address/Aim recovery");
            if (finished) yield break;

            if (!ValidateOrderedSequence(new[]
                {
                    CameraMode.HoleIntro,
                    CameraMode.Address,
                    CameraMode.Aim,
                    CameraMode.Swing,
                    CameraMode.Impact,
                    CameraMode.BallFollow,
                    CameraMode.Landing,
                    CameraMode.NextShot,
                    CameraMode.Address
                }))
            {
                yield break;
            }

            holeFlow.SetAutomaticFlowSuspended(true);
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green);
            if (green == null)
            {
                Fail("Green surface was not found.");
                yield break;
            }

            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 longPuttStart = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 12f);
            ball.PrepareNextShot(longPuttStart, green.Data);
            shotFlow.PrepareNextShot(cup - longPuttStart, putter);
            yield return WaitFor(
                () => director.CurrentMode == CameraMode.Putt && !director.IsTransitioning,
                "long Putt framing camera");
            if (finished) yield break;
            yield return null;

            UnityEngine.Camera activeCamera = UnityEngine.Camera.main;
            if (activeCamera == null
                || !IsInsideSafeViewport(activeCamera.WorldToViewportPoint(ball.PhysicsPosition + Vector3.up * 0.15f))
                || !IsInsideSafeViewport(activeCamera.WorldToViewportPoint(cup + Vector3.up * 0.15f)))
            {
                Fail("12m Putt framing did not keep both Ball and Cup inside the camera viewport.");
                yield break;
            }

            Vector3 puttStart = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(puttStart, green.Data);
            shotFlow.PrepareNextShot(cup - puttStart, putter);
            yield return WaitFor(() => director.CurrentMode == CameraMode.Putt, "Putt address camera");
            if (finished) yield break;

            if (!shotFlow.TryCommitShot(0.45f, 0f))
            {
                Fail("Putt commit was rejected.");
                yield break;
            }

            yield return WaitFor(() => holeFlow.State == HoleFlowState.HoleComplete, "Putt Hole In");
            if (finished) yield break;
            yield return WaitFor(() => director.CurrentMode == CameraMode.HoleComplete, "HoleComplete camera");
            if (finished) yield break;
            yield return WaitFor(() => director.CurrentMode == CameraMode.Result, "Result camera");
            if (finished) yield break;

            if (!observedModes.Contains(CameraMode.Putt)
                || !observedModes.Contains(CameraMode.HoleComplete)
                || !observedModes.Contains(CameraMode.Result))
            {
                Fail("Putt, HoleComplete, or Result mode was not observed.");
                yield break;
            }

            if (!float.IsFinite(minimumObservedFov)
                || minimumObservedFov < 20f
                || maximumObservedFov > 100f
                || maximumObservedFollowDistance <= 1f)
            {
                Fail($"Camera telemetry invalid: FOV {minimumObservedFov:F1}-{maximumObservedFov:F1}, distance {maximumObservedFollowDistance:F1}.");
                yield break;
            }

            Complete();
        }

        private bool ValidateOrderedSequence(IReadOnlyList<CameraMode> required)
        {
            int cursor = 0;
            foreach (CameraMode observed in observedModes)
            {
                if (observed == required[cursor])
                {
                    cursor++;
                    if (cursor == required.Count)
                    {
                        return true;
                    }
                }
            }

            Fail($"Camera sequence order failed. Observed: {string.Join(" -> ", observedModes)}");
            return false;
        }

        private void ObserveCurrentMode()
        {
            if (observedModes.Count == 0 || observedModes[^1] != director.CurrentMode)
            {
                observedModes.Add(director.CurrentMode);
            }
        }

        private void OnModeChanged(CameraMode previous, CameraMode next)
        {
            if (observedModes.Count == 0)
            {
                observedModes.Add(previous);
            }
            if (observedModes[^1] != next)
            {
                observedModes.Add(next);
            }
        }

        private IEnumerator WaitFor(System.Func<bool> condition, string label)
        {
            float started = Time.time;
            while (!condition())
            {
                if (Time.time - started >= TimeoutSeconds)
                {
                    Fail($"{label} timed out in Camera={director.CurrentMode}, Ball={ball.State}, Shot={shotFlow.State}, Hole={holeFlow.State}.");
                    yield break;
                }
                yield return null;
            }
        }

        private static TerrainSurface FindSurface(TerrainSurfaceType type)
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

        private static bool IsInsideSafeViewport(Vector3 viewportPoint)
        {
            return viewportPoint.z > 0f
                   && viewportPoint.x is >= 0.04f and <= 0.96f
                   && viewportPoint.y is >= 0.04f and <= 0.96f;
        }

        private void Complete()
        {
            finished = true;
            Debug.Log(
                "SWINGPOP_M6_PLAYMODE_VALIDATION_PASS: " +
                $"camera sequence {string.Join(" -> ", observedModes)}; " +
                $"FOV {minimumObservedFov:F1}-{maximumObservedFov:F1}; max follow distance {maximumObservedFollowDistance:F1}m; " +
                "normal shot returned to next Address/Aim; putt used Putt camera and reached HoleComplete -> Result.");
            StopPlayMode();
        }

        private void Fail(string reason)
        {
            if (finished)
            {
                return;
            }
            finished = true;
            Debug.LogError($"SWINGPOP_M6_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopPlayMode();
        }

        private static void StopPlayMode()
        {
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}

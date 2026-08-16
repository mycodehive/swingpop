using System.Collections;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
using SwingPop.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SwingPop.Editor
{
    public static class M8ValidationTools
    {
        [MenuItem("SwingPop/M8/Run HUD Flow Validation")]
        public static void RunHudFlowValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M8 HUD validation.");
                return;
            }

            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            WindController wind = Object.FindAnyObjectByType<WindController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            CameraDirector cameraDirector = Object.FindAnyObjectByType<CameraDirector>();
            CanvasScaler scaler = hud != null ? hud.GetComponent<CanvasScaler>() : null;
            if (hud == null || hud.View == null || shotFlow == null || ball == null || wind == null
                || holeFlow == null || cameraDirector == null || scaler == null)
            {
                Debug.LogError("SWINGPOP_M8_PLAYMODE_VALIDATION_FAIL: M8 scene dependencies were not found.");
                return;
            }

            GameObject driverObject = new("M8 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M8PlayModeValidationDriver>().Begin(
                hud,
                shotFlow,
                ball,
                wind,
                holeFlow,
                cameraDirector,
                scaler);
        }
    }

    internal sealed class M8PlayModeValidationDriver : MonoBehaviour
    {
        private const float TimeoutSeconds = 45f;

        private GameplayHudPresenter hud;
        private GameplayHudView view;
        private ShotFlowController shotFlow;
        private GolfBallController ball;
        private WindController wind;
        private HoleFlowController holeFlow;
        private CameraDirector cameraDirector;
        private CanvasScaler scaler;
        private ShotInputController inputController;
        private ClubData driver;
        private ClubData putter;
        private bool finished;

        public void Begin(
            GameplayHudPresenter targetHud,
            ShotFlowController targetShotFlow,
            GolfBallController targetBall,
            WindController targetWind,
            HoleFlowController targetHoleFlow,
            CameraDirector targetCameraDirector,
            CanvasScaler targetScaler)
        {
            hud = targetHud;
            view = hud.View;
            shotFlow = targetShotFlow;
            ball = targetBall;
            wind = targetWind;
            holeFlow = targetHoleFlow;
            cameraDirector = targetCameraDirector;
            scaler = targetScaler;
            inputController = Object.FindAnyObjectByType<ShotInputController>();
            if (inputController != null)
            {
                inputController.enabled = false;
            }

            SerializedObject serializedHole = new(holeFlow);
            driver = serializedHole.FindProperty("normalClub").objectReferenceValue as ClubData;
            putter = serializedHole.FindProperty("putter").objectReferenceValue as ClubData;
            if (driver == null || putter == null)
            {
                Fail("Driver or Putter data is not assigned.");
                return;
            }

            StartCoroutine(RunValidation());
        }

        private void OnDestroy()
        {
            if (inputController != null)
            {
                inputController.enabled = true;
            }
        }

        private IEnumerator RunValidation()
        {
            holeFlow.SetAutomaticFlowSuspended(false);
            holeFlow.DebugResetHole();
            cameraDirector.SkipIntro();
            yield return null;
            yield return null;

            if (!view.IsAimVisible || !view.IsActionVisible || view.ActionLabel != "START SHOT"
                || !view.DistanceLabel.EndsWith("m") || string.IsNullOrEmpty(view.HeightLabel)
                || string.IsNullOrEmpty(view.WindLabel) || string.IsNullOrEmpty(view.ClubLabel))
            {
                Fail("Initial Aiming HUD is incomplete or unreadable.");
                yield break;
            }
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || scaler.referenceResolution != new Vector2(1920f, 1080f)
                || Mathf.Abs(scaler.matchWidthOrHeight - 0.5f) > 0.001f)
            {
                Fail("Canvas scaling is not configured for the 1920x1080 reference and balanced matching.");
                yield break;
            }

            foreach (SpinPreset preset in new[]
                     {
                         SpinPreset.NoSpin,
                         SpinPreset.TopSpin,
                         SpinPreset.BackSpin,
                         SpinPreset.LeftSideSpin,
                         SpinPreset.RightSideSpin
                     })
            {
                shotFlow.SetSpinPreset(preset);
                yield return null;
                if (view.SpinLabel != HudPresentationMapper.FormatSpin(preset, true))
                {
                    Fail($"Spin HUD mismatch for {preset}: '{view.SpinLabel}'.");
                    yield break;
                }
            }
            shotFlow.SetSpinPreset(SpinPreset.NoSpin);

            foreach (WindPreset preset in new[]
                     {
                         WindPreset.Calm,
                         WindPreset.Tailwind,
                         WindPreset.Headwind,
                         WindPreset.LeftCrosswind,
                         WindPreset.RightCrosswind
                     })
            {
                wind.SetPreset(preset);
                yield return null;
                if (!view.WindLabel.EndsWith("m/s"))
                {
                    Fail($"Wind HUD did not show m/s for {preset}.");
                    yield break;
                }
            }
            wind.SetPreset(WindPreset.Calm);

            Button actionButton = view.ActionButton;
            actionButton.onClick.Invoke();
            yield return null;
            if (shotFlow.State != ShotFlowState.PowerSelecting || !view.GaugeView.IsPowerVisible
                || view.ActionLabel != "SET POWER")
            {
                Fail("Primary Action click did not enter the visible Power state.");
                yield break;
            }

            yield return new WaitForSeconds(0.28f);
            actionButton.onClick.Invoke();
            yield return null;
            if (shotFlow.State != ShotFlowState.ImpactSelecting || !view.GaugeView.IsImpactVisible
                || view.ActionLabel != "IMPACT")
            {
                Fail("Primary Action click did not enter the visible Impact state.");
                yield break;
            }
            if (shotFlow.Tuning == null
                || Mathf.Abs(view.GaugeView.PerfectZoneFraction - shotFlow.Tuning.PerfectMaximumOffset) > 0.001f)
            {
                Fail("Impact Perfect zone does not match ShotTuningData.");
                yield break;
            }

            shotFlow.ForcePerfectImpactAndCommit();
            yield return null;
            if (view.ImpactMessage != "PERFECT" || view.GaugeView.IsPowerVisible
                || view.GaugeView.IsImpactVisible || view.IsActionVisible)
            {
                Fail("Committed shot HUD did not hide interaction gauges or show PERFECT feedback.");
                yield break;
            }

            yield return WaitFor(() => ball.State != BallState.Ready, "Ball launch after UI commit");
            if (finished) yield break;
            if (view.GaugeView.IsPowerVisible || view.GaugeView.IsImpactVisible || view.IsActionVisible)
            {
                Fail("Ball flight kept timing interaction active.");
                yield break;
            }

            yield return WaitFor(
                () => ball.State == BallState.Ready && shotFlow.State == ShotFlowState.Aiming,
                "continuous next-shot HUD");
            if (finished) yield break;
            yield return null;
            Vector3 recoveryPosition = ball.PhysicsPosition;
            if (!view.IsActionVisible || view.ActionLabel != "START SHOT"
                || view.LieLabel != HudPresentationMapper.FormatLie(ball.CurrentLie))
            {
                Fail("Next-shot HUD did not refresh Action or Lie.");
                yield break;
            }

            TerrainSurface water = FindSurface(TerrainSurfaceType.Water);
            TerrainSurface fairway = FindSurface(TerrainSurfaceType.Fairway);
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green);
            if (water == null || fairway == null || green == null)
            {
                Fail("Water, Fairway, or Green surface is missing.");
                yield break;
            }

            Bounds waterBounds = water.GetComponent<Collider>().bounds;
            Vector3 hazardStart = new(waterBounds.center.x, 0.2f, waterBounds.min.z - 1.5f);
            ball.PrepareNextShot(hazardStart, fairway.Data);
            shotFlow.PrepareNextShot(Vector3.forward, driver);
            if (!shotFlow.TryCommitShot(0.65f, 0f))
            {
                Fail("Water hazard test shot was rejected.");
                yield break;
            }
            yield return WaitFor(
                () => ball.State == BallState.Ready && ball.HasLastHazard,
                "Water hazard recovery HUD");
            if (finished) yield break;
            yield return null;
            if (!view.HazardMessage.Contains("WATER HAZARD")
                || !view.HazardMessage.Contains("+1 PENALTY")
                || Vector3.Distance(ball.PhysicsPosition, recoveryPosition) > 0.04f)
            {
                Fail("Hazard popup or recovery position is incorrect.");
                yield break;
            }

            holeFlow.SetAutomaticFlowSuspended(true);
            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 puttStart = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(puttStart, green.Data);
            shotFlow.PrepareNextShot(cup - puttStart, putter);
            yield return null;
            if (!view.ClubLabel.Contains("PUTTER") || view.LieLabel != "GREEN"
                || view.SpinLabel != "SPIN DISABLED")
            {
                Fail("Green HUD did not show Putter, Green lie, and disabled Spin.");
                yield break;
            }
            if (!shotFlow.TryCommitShot(0.45f, 0f))
            {
                Fail("Putter result shot was rejected.");
                yield break;
            }

            yield return WaitFor(() => holeFlow.State == HoleFlowState.HoleComplete, "HoleComplete Result HUD");
            if (finished) yield break;
            yield return new WaitForSeconds(0.5f);
            if (!view.ResultView.IsVisible || string.IsNullOrEmpty(view.ResultView.ResultLabel)
                || view.GaugeView.IsPowerVisible || view.GaugeView.IsImpactVisible || view.IsActionVisible)
            {
                Fail("HoleComplete did not show Result or hide gameplay interaction.");
                yield break;
            }

            Complete();
        }

        private IEnumerator WaitFor(System.Func<bool> condition, string label)
        {
            float started = Time.time;
            while (!condition())
            {
                if (Time.time - started >= TimeoutSeconds)
                {
                    Fail($"{label} timed out in Shot={shotFlow.State}, Ball={ball.State}, Hole={holeFlow.State}.");
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

        private void Complete()
        {
            finished = true;
            Debug.Log(
                "SWINGPOP_M8_PLAYMODE_VALIDATION_PASS: "
                + "Aiming HUD, Primary Action mouse command, Power/Impact gauges, tuning-backed Perfect zone, "
                + "five Spin presets, five Wind presets, Ball Flight hiding, Next Shot Lie, Water penalty popup, "
                + "Green Putter/Spin disabled, Result panel, and 1920x1080 CanvasScaler configuration passed.");
            StopPlayMode();
        }

        private void Fail(string reason)
        {
            if (finished)
            {
                return;
            }
            finished = true;
            Debug.LogError($"SWINGPOP_M8_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopPlayMode();
        }

        private static void StopPlayMode()
        {
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}

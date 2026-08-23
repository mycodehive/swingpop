using System.Collections;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using SwingPop.UI;
using SwingPop.VfxSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    public static class M9ValidationTools
    {
        private const string FoundationScenePath = "Assets/_Game/Scenes/Foundation.unity";

        [MenuItem("SwingPop/M9/Run VFX Audio Shot Feel Validation")]
        public static void RunShotFeelValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M9 shot-feel validation.");
                return;
            }

            ShotPresentationController presentation = Object.FindAnyObjectByType<ShotPresentationController>();
            GameplayAudioController audio = Object.FindAnyObjectByType<GameplayAudioController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            CameraDirector cameraDirector = Object.FindAnyObjectByType<CameraDirector>();
            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            ImpactVfxController impact = Object.FindAnyObjectByType<ImpactVfxController>();
            BallTrailController trail = Object.FindAnyObjectByType<BallTrailController>();
            LandingVfxController landing = Object.FindAnyObjectByType<LandingVfxController>();
            HoleInVfxController holeIn = Object.FindAnyObjectByType<HoleInVfxController>();
            if (presentation == null || audio == null || shotFlow == null || ball == null || holeFlow == null
                || cameraDirector == null || hud == null || impact == null || trail == null || landing == null
                || holeIn == null)
            {
                Debug.LogError("SWINGPOP_M9_PLAYMODE_VALIDATION_FAIL: M9 scene dependencies were not found.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

            GameObject driverObject = new("M9 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M9PlayModeValidationDriver>().Begin(
                presentation, audio, shotFlow, ball, holeFlow, cameraDirector, hud, impact, trail, landing, holeIn);
        }

        /// <summary>Batch entry point: loads the wired scene, enters Play Mode, then exits with validation status.</summary>
        public static void RunBatchValidation()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogWarning("RunBatchValidation is intended for Unity batch mode.");
                return;
            }

            EditorSceneManager.OpenScene(FoundationScenePath, OpenSceneMode.Single);
            EditorApplication.playModeStateChanged -= OnBatchPlayModeChanged;
            EditorApplication.playModeStateChanged += OnBatchPlayModeChanged;
            EditorApplication.isPlaying = true;
        }

        private static void OnBatchPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnBatchPlayModeChanged;
            EditorApplication.delayCall += RunShotFeelValidation;
        }

        [MenuItem("SwingPop/M9/Preview Normal Impact")]
        private static void PreviewNormalImpact() => PreviewImpact(ShotPresentationLevel.Normal);

        [MenuItem("SwingPop/M9/Preview Perfect Impact")]
        private static void PreviewPerfectImpact() => PreviewImpact(ShotPresentationLevel.Perfect);

        [MenuItem("SwingPop/M9/Preview Fairway Landing")]
        private static void PreviewFairwayLanding() => PreviewLanding(LandingEffectType.Grass);

        [MenuItem("SwingPop/M9/Preview Bunker Landing")]
        private static void PreviewBunkerLanding() => PreviewLanding(LandingEffectType.Sand);

        [MenuItem("SwingPop/M9/Preview Water Splash")]
        private static void PreviewWaterSplash() => PreviewLanding(LandingEffectType.Water);

        [MenuItem("SwingPop/M9/Preview Hole In")]
        private static void PreviewHoleIn()
        {
            HoleInVfxController controller = Object.FindAnyObjectByType<HoleInVfxController>();
            HoleFlowController flow = Object.FindAnyObjectByType<HoleFlowController>();
            if (controller == null || flow == null || !EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode with Foundation open before previewing the M9 Hole-In VFX.");
                return;
            }
            controller.Play(flow.Hole.CupPosition, CelebrationPresentationLevel.Strong);
        }

        [MenuItem("SwingPop/M9/Preview Audio Sequence")]
        private static void PreviewAudioSequence()
        {
            GameplayAudioController controller = Object.FindAnyObjectByType<GameplayAudioController>();
            if (controller == null || !EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode with Foundation open before previewing M9 audio.");
                return;
            }
            controller.Preview(GameplayAudioCue.Swing);
            controller.Preview(GameplayAudioCue.NormalImpact);
            controller.Preview(GameplayAudioCue.PerfectImpact);
            controller.Preview(GameplayAudioCue.BunkerLanding);
            controller.Preview(GameplayAudioCue.WaterHazard);
            controller.Preview(GameplayAudioCue.HoleIn);
            controller.Preview(GameplayAudioCue.Result);
        }

        private static void PreviewImpact(ShotPresentationLevel level)
        {
            ImpactVfxController controller = Object.FindAnyObjectByType<ImpactVfxController>();
            if (controller == null || !EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode with Foundation open before previewing M9 impact VFX.");
                return;
            }
            controller.Preview(level);
        }

        private static void PreviewLanding(LandingEffectType effect)
        {
            LandingVfxController controller = Object.FindAnyObjectByType<LandingVfxController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            if (controller == null || ball == null || !EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode with Foundation open before previewing M9 landing VFX.");
                return;
            }
            controller.Play(ball.PhysicsPosition, effect, 1f);
        }
    }

    internal sealed class M9PlayModeValidationDriver : MonoBehaviour
    {
        private const float TimeoutSeconds = 50f;

        private ShotPresentationController presentation;
        private GameplayAudioController audio;
        private ShotFlowController shotFlow;
        private GolfBallController ball;
        private HoleFlowController holeFlow;
        private CameraDirector cameraDirector;
        private GameplayHudPresenter hud;
        private ImpactVfxController impact;
        private BallTrailController trail;
        private LandingVfxController landing;
        private HoleInVfxController holeIn;
        private ShotInputController inputController;
        private ClubData driver;
        private ClubData putter;
        private bool finished;
        private int initialReusableCount;
        private int initialParticleCount;
        private int initialTrailCount;

        public void Begin(
            ShotPresentationController targetPresentation,
            GameplayAudioController targetAudio,
            ShotFlowController targetShotFlow,
            GolfBallController targetBall,
            HoleFlowController targetHoleFlow,
            CameraDirector targetCameraDirector,
            GameplayHudPresenter targetHud,
            ImpactVfxController targetImpact,
            BallTrailController targetTrail,
            LandingVfxController targetLanding,
            HoleInVfxController targetHoleIn)
        {
            presentation = targetPresentation;
            audio = targetAudio;
            shotFlow = targetShotFlow;
            ball = targetBall;
            holeFlow = targetHoleFlow;
            cameraDirector = targetCameraDirector;
            hud = targetHud;
            impact = targetImpact;
            trail = targetTrail;
            landing = targetLanding;
            holeIn = targetHoleIn;
            inputController = Object.FindAnyObjectByType<ShotInputController>();
            if (inputController != null)
            {
                inputController.enabled = false;
            }

            SerializedObject serializedHole = new(holeFlow);
            driver = serializedHole.FindProperty("normalClub")?.objectReferenceValue as ClubData;
            putter = serializedHole.FindProperty("putter")?.objectReferenceValue as ClubData;
            if (driver == null || putter == null)
            {
                Fail("Driver or Putter data is not assigned.");
                return;
            }
            initialReusableCount = presentation.ReusableEffectObjectCount;
            initialParticleCount = Object.FindObjectsByType<ParticleSystem>().Length;
            initialTrailCount = Object.FindObjectsByType<TrailRenderer>().Length;
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

            int impactBeforeNormal = presentation.ImpactPresentationCount;
            int swingBeforeNormal = audio.GetCueCount(GameplayAudioCue.Swing);
            if (!shotFlow.TryCommitShot(0.55f, 0.5f))
            {
                Fail("Normal shot was rejected.");
                yield break;
            }
            yield return WaitFor(() => ball.State != BallState.Ready, "Normal ball launch");
            if (finished) yield break;
            yield return null;
            float normalTrailWidth = trail.CurrentWidth;
            float normalTrailLifetime = trail.CurrentLifetime;
            if (presentation.ImpactPresentationCount != impactBeforeNormal + 1
                || presentation.LastImpactLevel != ShotPresentationLevel.Normal
                || impact.LastLevel != ShotPresentationLevel.Normal
                || audio.GetCueCount(GameplayAudioCue.NormalImpact) < 1
                || audio.GetCueCount(GameplayAudioCue.PerfectImpact) != 0
                || audio.GetCueCount(GameplayAudioCue.Swing) <= swingBeforeNormal
                || hud.View.ImpactMessage == "PERFECT")
            {
                Fail("Normal impact VFX/audio/HUD did not use the Normal profile exactly once.");
                yield break;
            }
            yield return WaitFor(() => ball.State == BallState.Ready && shotFlow.State == ShotFlowState.Aiming, "Normal shot finish");
            if (finished) yield break;
            if (presentation.SurfacePresentationCount < 1)
            {
                Fail("Normal shot did not produce a landing presentation.");
                yield break;
            }

            int impactBeforePerfect = presentation.ImpactPresentationCount;
            int perfectAudioBefore = audio.GetCueCount(GameplayAudioCue.PerfectImpact);
            if (!shotFlow.TryCommitShot(0.68f, 0f))
            {
                Fail("Perfect shot was rejected.");
                yield break;
            }
            yield return WaitFor(() => ball.State != BallState.Ready, "Perfect ball launch");
            if (finished) yield break;
            yield return null;
            if (presentation.ImpactPresentationCount != impactBeforePerfect + 1
                || presentation.LastImpactLevel != ShotPresentationLevel.Perfect
                || impact.LastLevel != ShotPresentationLevel.Perfect
                || audio.GetCueCount(GameplayAudioCue.PerfectImpact) != perfectAudioBefore + 1
                || trail.CurrentWidth <= normalTrailWidth
                || trail.CurrentLifetime <= normalTrailLifetime
                || hud.View.ImpactMessage != "PERFECT"
                || cameraDirector.CurrentMode != CameraMode.Impact)
            {
                Fail("Perfect impact did not strengthen synchronized VFX, trail, audio, HUD, and camera feedback.");
                yield break;
            }
            yield return new WaitForSeconds(0.15f);
            if (presentation.ImpactPresentationCount != impactBeforePerfect + 1)
            {
                Fail("One Perfect shot triggered duplicate impact presentations.");
                yield break;
            }
            yield return WaitFor(() => ball.State == BallState.Ready && shotFlow.State == ShotFlowState.Aiming, "Perfect shot finish");
            if (finished) yield break;

            int landingBeforePreviews = landing.PlayCount;
            landing.Play(ball.PhysicsPosition, LandingEffectType.Grass, 1f);
            landing.Play(ball.PhysicsPosition, LandingEffectType.Rough, 1f);
            landing.Play(ball.PhysicsPosition, LandingEffectType.Sand, 1f);
            if (landing.PlayCount != landingBeforePreviews + 3 || landing.LastEffect != LandingEffectType.Sand)
            {
                Fail("Grass, Rough, and Sand reusable landing previews did not play.");
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
            int waterAudioBefore = audio.GetCueCount(GameplayAudioCue.WaterHazard);
            Bounds waterBounds = water.GetComponent<Collider>().bounds;
            Vector3 hazardStart = new(waterBounds.center.x, 0.2f, waterBounds.min.z - 1.5f);
            ball.PrepareNextShot(hazardStart, fairway.Data);
            shotFlow.PrepareNextShot(Vector3.forward, driver);
            if (!shotFlow.TryCommitShot(0.65f, 0f))
            {
                Fail("Water hazard shot was rejected.");
                yield break;
            }
            yield return WaitFor(() => ball.State == BallState.Ready && ball.HasLastHazard, "Water hazard recovery");
            if (finished) yield break;
            if (landing.LastEffect != LandingEffectType.Water
                || audio.GetCueCount(GameplayAudioCue.WaterHazard) != waterAudioBefore + 1)
            {
                Fail("Water hazard did not trigger splash and hazard audio exactly once.");
                yield break;
            }

            holeFlow.SetAutomaticFlowSuspended(true);
            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 puttStart = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(puttStart, green.Data);
            shotFlow.PrepareNextShot(cup - puttStart, putter);
            int holeBefore = holeIn.PlayCount;
            int holeAudioBefore = audio.GetCueCount(GameplayAudioCue.HoleIn);
            if (!shotFlow.TryCommitShot(0.45f, 0f))
            {
                Fail("Hole-in putt was rejected.");
                yield break;
            }
            yield return WaitFor(() => holeFlow.State == HoleFlowState.HoleComplete, "Hole-in presentation");
            if (finished) yield break;
            yield return null;
            if (holeIn.PlayCount != holeBefore + 1
                || audio.GetCueCount(GameplayAudioCue.HoleIn) != holeAudioBefore + 1
                || audio.GetCueCount(GameplayAudioCue.Result) < 1
                || !hud.View.ResultView.IsVisible
                || cameraDirector.CurrentMode is not (CameraMode.HoleComplete or CameraMode.Result))
            {
                Fail("Hole-in did not synchronize VFX, audio, result HUD, and result camera.");
                yield break;
            }

            if (initialReusableCount != presentation.ReusableEffectObjectCount
                || initialParticleCount != Object.FindObjectsByType<ParticleSystem>().Length
                || initialTrailCount != Object.FindObjectsByType<TrailRenderer>().Length)
            {
                Fail("Repeated shots changed the reusable ParticleSystem or TrailRenderer object count.");
                yield break;
            }
            if (audio.GeneratedFallbackCount <= 0)
            {
                Fail("Placeholder audio fallback library was not available.");
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
                "SWINGPOP_M9_PLAYMODE_VALIDATION_PASS: Normal/Perfect impact distinction, single impact gate, "
                + "stronger Perfect trail, swing/impact/terrain/hazard/hole/result audio, grass/rough/sand/water VFX, "
                + "HUD/camera synchronization, and stable reusable effect object counts passed.");
            StopPlayMode(true);
        }

        private void Fail(string reason)
        {
            if (finished)
            {
                return;
            }
            finished = true;
            Debug.LogError($"SWINGPOP_M9_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopPlayMode(false);
        }

        private static void StopPlayMode(bool success)
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPlaying = false;
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(success ? 0 : 1);
                }
            };
        }
    }
}

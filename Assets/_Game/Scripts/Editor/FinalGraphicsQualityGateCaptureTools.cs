using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using SwingPop.UI;
using SwingPop.VfxSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    /// <summary>
    /// Creates the final graphics gate still-image set from the real Hole01 runtime graph.
    /// The tool drives existing gameplay/presentation entry points only; it does not alter
    /// shot physics, scoring, camera state ownership, or scene assets.
    /// </summary>
    [InitializeOnLoad]
    public static class FinalGraphicsQualityGateCaptureTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PendingKey = "SwingPop.FinalGraphicsGate.CapturePending";
        private const string PhaseKey = "SwingPop.FinalGraphicsGate.CapturePhase";
        private const string PhaseStartedKey = "SwingPop.FinalGraphicsGate.PhaseStarted";
        private const string ImpactMarkerKey = "SwingPop.FinalGraphicsGate.ImpactMarker";
        private const string ImpactSeenAtKey = "SwingPop.FinalGraphicsGate.ImpactSeenAt";
        private const string LandingMarkerKey = "SwingPop.FinalGraphicsGate.LandingMarker";
        private const string LandingSeenAtKey = "SwingPop.FinalGraphicsGate.LandingSeenAt";
        private const string InitialObjectsKey = "SwingPop.FinalGraphicsGate.InitialObjects";
        private const string InitialParticlesKey = "SwingPop.FinalGraphicsGate.InitialParticles";
        private const string InitialCamerasKey = "SwingPop.FinalGraphicsGate.InitialCameras";
        private const string InitialCanvasesKey = "SwingPop.FinalGraphicsGate.InitialCanvases";
        private const string InitialEventSystemsKey = "SwingPop.FinalGraphicsGate.InitialEventSystems";
        private const string InitialAudioSourcesKey = "SwingPop.FinalGraphicsGate.InitialAudioSources";

        private static readonly string OutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/final-graphics-quality-gate"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Library/FinalGraphicsQualityGate/Capture.result"));

        private static readonly string[] MasterCaptures =
        {
            "A1-Hole-Intro.png",
            "A2-Clean-Address.png",
            "A3-Aim.png",
            "B1-Power.png",
            "B2-Impact.png",
            "B3-Perfect-Impact.png",
            "C1-Normal-Flight.png",
            "C2-Perfect-Flight.png",
            "C3-Landing.png",
            "D1-Fairway-Next-Shot.png",
            "D2-Rough.png",
            "D3-Bunker.png",
            "D4-Water.png",
            "E1-Green-Putter-Address.png",
            "E2-Putt-Rolling.png",
            "E3-Cup-Approach.png",
            "F1-Hole-In.png",
            "F2-Character-Reaction.png",
            "F3-Result.png"
        };

        private static readonly string[] ResolutionCaptures =
        {
            "R1-Address-1600x900.png",
            "R2-Address-1280x720.png",
            "R3-Power-1600x900.png",
            "R4-Power-1280x720.png",
            "R5-Putt-1600x900.png",
            "R6-Putt-1280x720.png",
            "R7-Result-1600x900.png",
            "R8-Result-1280x720.png"
        };

        static FinalGraphicsQualityGateCaptureTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("SwingPop/Quality Gate/Capture Final Graphics Master Set")]
        public static void CaptureMasterSet()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before starting the final graphics capture set.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save or discard active scene changes before capture.");

            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            foreach (string path in Directory.GetFiles(OutputDirectory, "*.png", SearchOption.TopDirectoryOnly))
                File.Delete(path);
            if (File.Exists(ResultPath)) File.Delete(ResultPath);

            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetInt(ImpactMarkerKey, 0);
            SessionState.SetInt(LandingMarkerKey, 0);
            SessionState.SetString(ImpactSeenAtKey, string.Empty);
            SessionState.SetString(LandingSeenAtKey, string.Empty);
            SetPhaseStarted();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(PendingKey, false)) return;

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                ShotInputController input = Object.FindAnyObjectByType<ShotInputController>();
                if (input != null) input.enabled = false;

                ShotDebugOverlay overlay = Object.FindAnyObjectByType<ShotDebugOverlay>();
                overlay?.SetOverlayVisible(false);
                BallTrajectoryDebug trajectory = Object.FindAnyObjectByType<BallTrajectoryDebug>();
                trajectory?.SetTrajectoryVisible(false);

                StoreInitialRuntimeCounts();
                SetPhaseStarted();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(PendingKey, false);
                if (!File.Exists(ResultPath))
                    File.WriteAllText(ResultPath, "FAIL\nCapture ended without a result.");
                Debug.Log($"FINAL GRAPHICS QUALITY GATE CAPTURE COMPLETE | {OutputDirectory}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(File.ReadAllText(ResultPath).StartsWith("PASS", StringComparison.Ordinal) ? 0 : 1);
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying) return;

            int phase = SessionState.GetInt(PhaseKey, 0);
            double elapsed = PhaseElapsed();
            try
            {
                switch (phase)
                {
                    case 0 when elapsed >= 1.15d:
                        Capture("A1-Hole-Intro.png", 1920, 1080);
                        AdvancePhase();
                        break;
                    case 1 when elapsed >= 2.15d:
                        Require<CameraDirector>().RequestDebugMode(CameraMode.Address);
                        AdvancePhase();
                        break;
                    case 2 when elapsed >= 0.75d:
                        Capture("A2-Clean-Address.png", 1920, 1080);
                        Capture("R1-Address-1600x900.png", 1600, 900);
                        Capture("R2-Address-1280x720.png", 1280, 720);
                        Require<ShotFlowController>().SetAimInput(0.7f);
                        AdvancePhase();
                        break;
                    case 3 when elapsed >= 0.42d:
                        Require<ShotFlowController>().SetAimInput(0f);
                        Require<CameraDirector>().RequestDebugMode(CameraMode.Aim);
                        AdvancePhase();
                        break;
                    case 4 when elapsed >= 0.42d:
                        Capture("A3-Aim.png", 1920, 1080);
                        Require<ShotFlowController>().ConfirmCurrentStep();
                        AdvancePhase();
                        break;
                    case 5 when elapsed >= 0.42d:
                        Capture("B1-Power.png", 1920, 1080);
                        Capture("R3-Power-1600x900.png", 1600, 900);
                        Capture("R4-Power-1280x720.png", 1280, 720);
                        Require<ShotFlowController>().ConfirmCurrentStep();
                        AdvancePhase();
                        break;
                    case 6 when elapsed >= 0.42d:
                        Capture("B2-Impact.png", 1920, 1080);
                        BeginDriverShot(0.5f);
                        AdvancePhase();
                        break;
                    case 7 when ImpactOccurredAfterDelay():
                        AdvancePhase();
                        break;
                    case 8 when Require<GolfBallController>().State == BallState.Airborne && elapsed >= 0.38d:
                        Capture("C1-Normal-Flight.png", 1920, 1080);
                        ResetDriverView();
                        AdvancePhase();
                        break;
                    case 9 when elapsed >= 0.62d:
                        BeginDriverShot(0f);
                        AdvancePhase();
                        break;
                    case 10 when ImpactOccurredAfterDelay():
                        Capture("B3-Perfect-Impact.png", 1920, 1080);
                        AdvancePhase();
                        break;
                    case 11 when Require<GolfBallController>().State == BallState.Airborne && elapsed >= 0.38d:
                        Capture("C2-Perfect-Flight.png", 1920, 1080);
                        MarkLandingCount();
                        AdvancePhase();
                        break;
                    case 12 when LandingOccurredAfterDelay():
                        Capture("C3-Landing.png", 1920, 1080);
                        PrepareLie(TerrainSurfaceType.Fairway, CameraMode.NextShot);
                        AdvancePhase();
                        break;
                    case 13 when elapsed >= 0.72d:
                        Capture("D1-Fairway-Next-Shot.png", 1920, 1080);
                        PrepareLie(TerrainSurfaceType.Rough, CameraMode.Address);
                        AdvancePhase();
                        break;
                    case 14 when elapsed >= 0.78d:
                        Capture("D2-Rough.png", 1920, 1080);
                        PrepareLie(TerrainSurfaceType.Bunker, CameraMode.Address);
                        AdvancePhase();
                        break;
                    case 15 when elapsed >= 0.78d:
                        Capture("D3-Bunker.png", 1920, 1080);
                        PrepareWaterPresentation();
                        AdvancePhase();
                        break;
                    case 16 when elapsed >= 0.08d:
                        Capture("D4-Water.png", 1920, 1080);
                        PreparePuttAddress();
                        AdvancePhase();
                        break;
                    case 17 when elapsed >= 0.92d:
                        Capture("E1-Green-Putter-Address.png", 1920, 1080);
                        Capture("R5-Putt-1600x900.png", 1600, 900);
                        Capture("R6-Putt-1280x720.png", 1280, 720);
                        if (!Require<ShotFlowController>().TryCommitShot(0.45f, 0f))
                            throw new InvalidOperationException("Putter capture shot was rejected.");
                        AdvancePhase();
                        break;
                    case 18 when Require<GolfBallController>().State == BallState.Rolling && elapsed >= 0.12d:
                        Require<GameplayHudView>().HideTransientFeedback();
                        Capture("E2-Putt-Rolling.png", 1920, 1080);
                        AdvancePhase();
                        break;
                    case 19 when Require<PuttResultCinematicController>().Phase == PuttResultCinematicPhase.CupApproach
                                 && elapsed >= 0.04d:
                        Capture("E3-Cup-Approach.png", 1920, 1080);
                        AdvancePhase();
                        break;
                    case 20 when Require<HoleFlowController>().State == HoleFlowState.HoleComplete && elapsed >= 0.14d:
                        Capture("F1-Hole-In.png", 1920, 1080);
                        AdvancePhase();
                        break;
                    case 21 when Require<PuttResultCinematicController>().Phase == PuttResultCinematicPhase.CharacterReaction
                                 && elapsed >= 0.6d:
                        Capture("F2-Character-Reaction.png", 1920, 1080);
                        AdvancePhase();
                        break;
                    case 22 when Require<PuttResultCinematicController>().Phase == PuttResultCinematicPhase.ResultHold
                                 && elapsed >= 0.42d:
                        Capture("F3-Result.png", 1920, 1080);
                        Capture("R7-Result-1600x900.png", 1600, 900);
                        Capture("R8-Result-1280x720.png", 1280, 720);
                        WriteSuccessResult();
                        EditorApplication.isPlaying = false;
                        break;
                    default:
                        if (elapsed > 18d)
                            throw new TimeoutException($"Final graphics capture phase {phase} timed out.");
                        break;
                }
            }
            catch (Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
                EditorApplication.isPlaying = false;
            }
        }

        private static void BeginDriverShot(float impactOffset)
        {
            ShotFlowController shotFlow = Require<ShotFlowController>();
            if (shotFlow.State is ShotFlowState.PowerSelecting or ShotFlowState.ImpactSelecting)
                shotFlow.CancelToAiming();
            MarkImpactCount();
            if (!shotFlow.TryCommitShot(0.68f, impactOffset))
                throw new InvalidOperationException($"Driver capture shot was rejected at impact offset {impactOffset}.");
        }

        private static void ResetDriverView()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            CameraDirector camera = Require<CameraDirector>();
            camera.enabled = true;
            camera.SkipIntro();
        }

        private static void PrepareLie(TerrainSurfaceType type, CameraMode mode)
        {
            TerrainSurface surface = FindSurface(type)
                ?? throw new InvalidOperationException($"{type} surface was not found.");
            Collider collider = surface.GetComponent<Collider>();
            if (collider == null) throw new InvalidOperationException($"{type} surface has no gameplay collider.");

            HoleFlowController holeFlow = Require<HoleFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            holeFlow.SetAutomaticFlowSuspended(true);

            Vector3 position = collider.bounds.center;
            position.y = collider.bounds.max.y + 0.15f;
            ball.SetResetPose(position, Quaternion.identity, surface.Data, true);
            shotFlow.PrepareNextShot(holeFlow.Hole.CupPosition - position, ReadClub(holeFlow, "normalClub"));
            CameraDirector camera = Require<CameraDirector>();
            camera.enabled = true;
            camera.RequestDebugMode(mode);
        }

        private static void PrepareWaterPresentation()
        {
            Bounds bounds = FindPresentationSurfaceBounds(TerrainSurfaceType.Water);
            Vector3 target = new(bounds.center.x, bounds.max.y + 0.08f, bounds.center.z);
            TerrainSurface water = FindSurface(TerrainSurfaceType.Water)
                ?? throw new InvalidOperationException("Water surface was not found.");
            HoleFlowController holeFlow = Require<HoleFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            ball.SetResetPose(target + Vector3.up * 0.07f, Quaternion.identity, water.Data, true);
            shotFlow.PrepareNextShot(holeFlow.Hole.CupPosition - target, ReadClub(holeFlow, "normalClub"));

            CameraDirector director = Require<CameraDirector>();
            director.enabled = false;
            Camera camera = Require<Camera>();
            Vector3 position = target + new Vector3(-6f, 3.8f, -6f);
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
            camera.fieldOfView = 32f;

            Require<LandingVfxController>().Play(target, LandingEffectType.Water, 1f);
            GameplayHudView view = Require<GameplayHudView>();
            view.SetAimVisible(false);
            view.SetPrimaryAction(new HudActionPresentation(string.Empty, false, false), ShotFlowState.ShotCommitted);
            view.ShowHazard("WATER HAZARD\n+1 PENALTY", view.ResolveTone(HudSkinTone.Coral), 2.6f, 0.18f);
        }

        private static void PreparePuttAddress()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green)
                ?? throw new InvalidOperationException("Green surface was not found.");

            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 start = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(start, green.Data);
            shotFlow.PrepareNextShot(cup - start, ReadClub(holeFlow, "putter"));
            CameraDirector camera = Require<CameraDirector>();
            camera.enabled = true;
            Require<PuttResultCinematicController>().PreviewPuttReady();
        }

        private static ClubData ReadClub(HoleFlowController holeFlow, string propertyName)
        {
            ClubData club = new SerializedObject(holeFlow).FindProperty(propertyName)?.objectReferenceValue as ClubData;
            return club != null ? club : throw new InvalidOperationException($"HoleFlow {propertyName} is not assigned.");
        }

        private static TerrainSurface FindSurface(TerrainSurfaceType type)
        {
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include))
                if (surface.SurfaceType == type) return surface;
            return null;
        }

        private static Bounds FindPresentationSurfaceBounds(TerrainSurfaceType type)
        {
            string visualName = type == TerrainSurfaceType.Water ? "Water Deep Body" : "Bunker Layered Sand";
            GameObject visual = GameObject.Find(visualName);
            Renderer renderer = visual != null ? visual.GetComponentInChildren<Renderer>() : null;
            if (renderer != null) return renderer.bounds;
            TerrainSurface surface = FindSurface(type)
                ?? throw new InvalidOperationException($"{type} surface was not found.");
            return surface.GetComponent<Collider>().bounds;
        }

        private static bool ImpactOccurredAfterDelay()
        {
            if (Require<ImpactVfxController>().PlayCount <= SessionState.GetInt(ImpactMarkerKey, -1)) return false;
            string raw = SessionState.GetString(ImpactSeenAtKey, string.Empty);
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double seenAt))
            {
                SessionState.SetString(ImpactSeenAtKey, EditorApplication.timeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
                return false;
            }
            return EditorApplication.timeSinceStartup - seenAt >= 0.04d;
        }

        private static void MarkImpactCount()
        {
            SessionState.SetInt(ImpactMarkerKey, Require<ImpactVfxController>().PlayCount);
            SessionState.SetString(ImpactSeenAtKey, string.Empty);
        }

        private static bool LandingOccurredAfterDelay()
        {
            GolfBallController ball = Require<GolfBallController>();
            if (Require<LandingVfxController>().PlayCount <= SessionState.GetInt(LandingMarkerKey, -1)
                || ball.State is not (BallState.Bouncing or BallState.Rolling)) return false;
            string raw = SessionState.GetString(LandingSeenAtKey, string.Empty);
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double seenAt))
            {
                SessionState.SetString(LandingSeenAtKey, EditorApplication.timeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
                return false;
            }
            return EditorApplication.timeSinceStartup - seenAt >= 0.04d;
        }

        private static void MarkLandingCount()
        {
            SessionState.SetInt(LandingMarkerKey, Require<LandingVfxController>().PlayCount);
            SessionState.SetString(LandingSeenAtKey, string.Empty);
        }

        private static void Capture(string filename, int width, int height)
        {
            string path = Path.Combine(OutputDirectory, filename);
            Camera camera = Require<Camera>();
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Final Graphics Quality Gate Capture"
            };
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            float previousTimeScale = Time.timeScale;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            RenderMode[] modes = new RenderMode[canvases.Length];
            Camera[] canvasCameras = new Camera[canvases.Length];
            float[] distances = new float[canvases.Length];
            try
            {
                Time.timeScale = 0f;
                camera.targetTexture = target;
                for (int index = 0; index < canvases.Length; index++)
                {
                    modes[index] = canvases[index].renderMode;
                    canvasCameras[index] = canvases[index].worldCamera;
                    distances[index] = canvases[index].planeDistance;
                    if (canvases[index].renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        canvases[index].renderMode = RenderMode.ScreenSpaceCamera;
                        canvases[index].worldCamera = camera;
                        canvases[index].planeDistance = 0.5f;
                    }
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log($"Final graphics gate captured {filename} ({width}x{height}).");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                for (int index = 0; index < canvases.Length; index++)
                {
                    canvases[index].renderMode = modes[index];
                    canvases[index].worldCamera = canvasCameras[index];
                    canvases[index].planeDistance = distances[index];
                }
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }

        private static void StoreInitialRuntimeCounts()
        {
            SessionState.SetInt(InitialObjectsKey, CountSceneGameObjects());
            SessionState.SetInt(InitialParticlesKey, Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length);
            SessionState.SetInt(InitialCamerasKey, Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length);
            SessionState.SetInt(InitialCanvasesKey, Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length);
            SessionState.SetInt(InitialEventSystemsKey, Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length);
            SessionState.SetInt(InitialAudioSourcesKey, Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length);
        }

        private static void WriteSuccessResult()
        {
            foreach (string file in MasterCaptures)
                if (!File.Exists(Path.Combine(OutputDirectory, file)))
                    throw new FileNotFoundException("Expected master capture is missing.", file);
            foreach (string file in ResolutionCaptures)
                if (!File.Exists(Path.Combine(OutputDirectory, file)))
                    throw new FileNotFoundException("Expected resolution capture is missing.", file);

            int objects = CountSceneGameObjects();
            int particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length;
            int cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length;
            int canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length;
            int eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length;
            int audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include).Length;
            RequireStableCount("GameObjects", InitialObjectsKey, objects);
            RequireStableCount("ParticleSystems", InitialParticlesKey, particles);
            RequireStableCount("Cameras", InitialCamerasKey, cameras);
            RequireStableCount("Canvases", InitialCanvasesKey, canvases);
            RequireStableCount("EventSystems", InitialEventSystemsKey, eventSystems);
            RequireStableCount("AudioSources", InitialAudioSourcesKey, audioSources);

            PuttResultCinematicController cinematic = Require<PuttResultCinematicController>();
            string report =
                "PASS\n" +
                $"MasterCaptures={MasterCaptures.Length}\n" +
                $"ResolutionCaptures={ResolutionCaptures.Length}\n" +
                "MasterResolution=1920x1080\n" +
                "AlternateResolutions=1600x900,1280x720\n" +
                $"GameObjects={objects}\n" +
                $"ParticleSystems={particles}\n" +
                $"Cameras={cameras}\n" +
                $"Canvases={canvases}\n" +
                $"EventSystems={eventSystems}\n" +
                $"AudioSources={audioSources}\n" +
                $"CinematicStarts={cinematic.StartCount}\n" +
                $"CharacterReactions={cinematic.CharacterReactionCount}\n" +
                $"ResultReveals={cinematic.ResultRevealCount}\n" +
                "RuntimeObjectCountsStable=True\n";
            File.WriteAllText(ResultPath, report);
        }

        private static void RequireStableCount(string label, string key, int current)
        {
            int initial = SessionState.GetInt(key, -1);
            if (initial != current)
                throw new InvalidOperationException($"Runtime {label} count changed: {initial} -> {current}.");
        }

        private static int CountSceneGameObjects()
        {
            int count = 0;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                count += root.GetComponentsInChildren<Transform>(true).Length;
            return count;
        }

        private static T Require<T>() where T : Object
        {
            T value = Object.FindAnyObjectByType<T>();
            if (value == null) throw new InvalidOperationException($"Final graphics capture dependency is missing: {typeof(T).Name}");
            return value;
        }

        private static void AdvancePhase()
        {
            SessionState.SetInt(PhaseKey, SessionState.GetInt(PhaseKey, 0) + 1);
            SetPhaseStarted();
        }

        private static void SetPhaseStarted()
        {
            SessionState.SetString(
                PhaseStartedKey,
                EditorApplication.timeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
        }

        private static double PhaseElapsed()
        {
            return double.TryParse(
                SessionState.GetString(PhaseStartedKey, string.Empty),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double started)
                ? EditorApplication.timeSinceStartup - started
                : 0d;
        }
    }
}

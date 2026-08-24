using System.IO;
using System.Collections.Generic;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using SwingPop.VfxSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    public static class VfxHeroCaptureTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PendingKey = "SwingPop.VfxHero.CapturePending";
        private const string PhaseKey = "SwingPop.VfxHero.CapturePhase";
        private const string PhaseStartedKey = "SwingPop.VfxHero.CapturePhaseStarted";
        private const string ImpactMarkerKey = "SwingPop.VfxHero.ImpactMarker";
        private const string ImpactSeenAtKey = "SwingPop.VfxHero.ImpactSeenAt";
        private const string PeakParticlesKey = "SwingPop.VfxHero.PeakParticles";
        private static readonly string OutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/vfx-hero-pass"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Library/VfxHeroValidation/Capture.result"));

        static VfxHeroCaptureTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("SwingPop/VFX/Capture Hero VFX Review Set")]
        public static void CaptureHeroVfxReviewSet()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before starting the VFX Hero capture set.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Save or discard active scene changes before capture.");

            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            if (File.Exists(ResultPath)) File.Delete(ResultPath);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetInt(ImpactMarkerKey, 0);
            SessionState.SetInt(PeakParticlesKey, 0);
            SessionState.SetString(PhaseStartedKey, string.Empty);
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
                SetPhaseStarted();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(PendingKey, false);
                if (!File.Exists(ResultPath)) File.WriteAllText(ResultPath, "FAIL\nCapture ended without a result.");
                Debug.Log($"VFX HERO CAPTURE SET COMPLETE | {OutputDirectory}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(File.ReadAllText(ResultPath).StartsWith("PASS") ? 0 : 1);
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
                    case 0 when elapsed >= 4.2d:
                        BeginDriverShot(0.5f);
                        AdvancePhase();
                        break;
                    case 1 when ImpactOccurredAfterDelay():
                        Capture("N-Normal-Impact.png");
                        ResetDriverView();
                        AdvancePhase();
                        break;
                    case 2 when elapsed >= 0.65d:
                        BeginDriverShot(0.25f);
                        AdvancePhase();
                        break;
                    case 3 when ImpactOccurredAfterDelay():
                        Capture("G-Great-Impact.png");
                        ResetDriverView();
                        AdvancePhase();
                        break;
                    case 4 when elapsed >= 0.65d:
                        BeginDriverShot(0f);
                        AdvancePhase();
                        break;
                    case 5 when ImpactOccurredAfterDelay():
                        Capture("P-Perfect-Impact.png");
                        AdvancePhase();
                        break;
                    case 6 when elapsed >= 0.34d:
                        Capture("D-Perfect-Flight.png");
                        PrepareSurfaceView(TerrainSurfaceType.Bunker);
                        AdvancePhase();
                        break;
                    case 7 when elapsed >= 0.55d:
                        PlayLanding(TerrainSurfaceType.Bunker);
                        AdvancePhase();
                        break;
                    case 8 when elapsed >= 0.06d:
                        Capture("B-Bunker-Landing.png");
                        PrepareSurfaceView(TerrainSurfaceType.Water);
                        AdvancePhase();
                        break;
                    case 9 when elapsed >= 0.55d:
                        PlayLanding(TerrainSurfaceType.Water);
                        AdvancePhase();
                        break;
                    case 10 when elapsed >= 0.07d:
                        Capture("W-Water-Splash.png");
                        PreparePutterShot();
                        AdvancePhase();
                        break;
                    case 11 when elapsed >= 0.75d:
                        CommitPutterShot();
                        AdvancePhase();
                        break;
                    case 12 when ImpactOccurredAfterDelay():
                        Capture("F-Putter-Impact.png");
                        ShowHoleInResult();
                        AdvancePhase();
                        break;
                    case 13 when elapsed >= 0.32d:
                        Capture("H-Hole-In-Result.png");
                        WriteSuccessResult();
                        EditorApplication.isPlaying = false;
                        break;
                }
            }
            catch (System.Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
                EditorApplication.isPlaying = false;
            }
        }

        private static void BeginDriverShot(float impactOffset)
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            CameraDirector camera = Require<CameraDirector>();
            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            camera.enabled = true;
            camera.SkipIntro();
            MarkImpactCount();
            if (!shotFlow.TryCommitShot(0.68f, impactOffset))
                throw new System.InvalidOperationException($"Driver capture shot was rejected at impact offset {impactOffset}.");
        }

        private static void ResetDriverView()
        {
            Require<HoleFlowController>().DebugResetHole();
            Require<CameraDirector>().SkipIntro();
        }

        private static void PrepareSurfaceView(TerrainSurfaceType type)
        {
            Bounds bounds = FindPresentationSurfaceBounds(type);
            Vector3 target = new(bounds.center.x, bounds.max.y + 0.08f, bounds.center.z);
            Vector3 side = type == TerrainSurfaceType.Water ? new Vector3(-6f, 3.8f, -6f) : new Vector3(6f, 3.8f, -6f);
            CameraDirector director = Require<CameraDirector>();
            director.enabled = false;
            Camera camera = Require<Camera>();
            camera.transform.SetPositionAndRotation(target + side, Quaternion.LookRotation(target - (target + side), Vector3.up));
            camera.fieldOfView = 32f;
            Require<HoleFlowController>().SetAutomaticFlowSuspended(true);
        }

        private static void PlayLanding(TerrainSurfaceType surfaceType)
        {
            Bounds bounds = FindPresentationSurfaceBounds(surfaceType);
            LandingEffectType type = ShotPresentationResolver.ResolveLanding(surfaceType);
            Require<LandingVfxController>().Play(
                new Vector3(bounds.center.x, bounds.max.y + 0.03f, bounds.center.z), type, 1f);
        }

        private static void PreparePutterShot()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green)
                ?? throw new System.InvalidOperationException("Green surface was not found.");
            SerializedObject serializedHole = new(holeFlow);
            ClubData putter = serializedHole.FindProperty("putter")?.objectReferenceValue as ClubData;
            if (putter == null) throw new System.InvalidOperationException("Putter data is not assigned.");

            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 start = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(start, green.Data);
            shotFlow.PrepareNextShot(cup - start, putter);
            CameraDirector director = Require<CameraDirector>();
            director.enabled = true;
            director.RequestDebugMode(CameraMode.Address);
        }

        private static void CommitPutterShot()
        {
            MarkImpactCount();
            if (!Require<ShotFlowController>().TryCommitShot(0.42f, 0f))
                throw new System.InvalidOperationException("Putter capture shot was rejected.");
        }

        private static void ShowHoleInResult()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            holeFlow.DebugResetHole();
            Require<CameraDirector>().enabled = true;
            if (!holeFlow.TryCompleteHole(Require<GolfBallController>()))
                throw new System.InvalidOperationException("Hole-In capture could not complete the hole.");
        }

        private static bool ImpactOccurred()
        {
            return Require<ImpactVfxController>().PlayCount > SessionState.GetInt(ImpactMarkerKey, -1);
        }

        private static bool ImpactOccurredAfterDelay()
        {
            if (!ImpactOccurred()) return false;
            string raw = SessionState.GetString(ImpactSeenAtKey, string.Empty);
            if (!double.TryParse(raw, out double seenAt))
            {
                SessionState.SetString(ImpactSeenAtKey, EditorApplication.timeSinceStartup.ToString("R"));
                return false;
            }
            return EditorApplication.timeSinceStartup - seenAt >= 0.04d;
        }

        private static void MarkImpactCount()
        {
            SessionState.SetInt(ImpactMarkerKey, Require<ImpactVfxController>().PlayCount);
            SessionState.SetString(ImpactSeenAtKey, string.Empty);
        }

        private static Bounds FindPresentationSurfaceBounds(TerrainSurfaceType type)
        {
            string name = type == TerrainSurfaceType.Water ? "Water Deep Body" : "Bunker Layered Sand";
            GameObject visual = GameObject.Find(name);
            Renderer renderer = visual != null ? visual.GetComponentInChildren<Renderer>() : null;
            if (renderer != null) return renderer.bounds;
            TerrainSurface surface = FindSurface(type)
                ?? throw new System.InvalidOperationException($"{type} surface was not found.");
            return surface.GetComponent<Collider>().bounds;
        }

        private static TerrainSurface FindSurface(TerrainSurfaceType type)
        {
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include))
                if (surface.SurfaceType == type) return surface;
            return null;
        }

        private static T Require<T>() where T : Object
        {
            T value = Object.FindAnyObjectByType<T>();
            if (value == null) throw new System.InvalidOperationException($"VFX capture dependency is missing: {typeof(T).Name}");
            return value;
        }

        private static void AdvancePhase()
        {
            SessionState.SetInt(PhaseKey, SessionState.GetInt(PhaseKey, 0) + 1);
            SetPhaseStarted();
        }

        private static void SetPhaseStarted()
        {
            SessionState.SetString(PhaseStartedKey, EditorApplication.timeSinceStartup.ToString("R"));
        }

        private static double PhaseElapsed()
        {
            return double.TryParse(SessionState.GetString(PhaseStartedKey, string.Empty), out double started)
                ? EditorApplication.timeSinceStartup - started
                : 0d;
        }

        private static void Capture(string filename)
        {
            SamplePerformanceTelemetry();
            string path = Path.Combine(OutputDirectory, filename);
            Camera camera = Require<Camera>();
            const int width = 1920;
            const int height = 1080;
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32) { name = "VFX Hero Review Capture" };
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            RenderMode[] modes = new RenderMode[canvases.Length];
            Camera[] cameras = new Camera[canvases.Length];
            float[] distances = new float[canvases.Length];
            try
            {
                Time.timeScale = 0f;
                for (int index = 0; index < canvases.Length; index++)
                {
                    modes[index] = canvases[index].renderMode;
                    cameras[index] = canvases[index].worldCamera;
                    distances[index] = canvases[index].planeDistance;
                    if (canvases[index].renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        canvases[index].renderMode = RenderMode.ScreenSpaceCamera;
                        canvases[index].worldCamera = camera;
                        canvases[index].planeDistance = 0.5f;
                    }
                }
                Canvas.ForceUpdateCanvases();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log($"VFX Hero review captured {filename}.");
            }
            finally
            {
                Time.timeScale = 1f;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                for (int index = 0; index < canvases.Length; index++)
                {
                    canvases[index].renderMode = modes[index];
                    canvases[index].worldCamera = cameras[index];
                    canvases[index].planeDistance = distances[index];
                }
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }

        private static void SamplePerformanceTelemetry()
        {
            int activeParticles = 0;
            foreach (ParticleSystem effect in Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include))
                activeParticles += effect.particleCount;

            SessionState.SetInt(
                PeakParticlesKey,
                Mathf.Max(SessionState.GetInt(PeakParticlesKey, 0), activeParticles));
        }

        private static void WriteSuccessResult()
        {
            ShotPresentationController presentation = Require<ShotPresentationController>();
            ParticleSystem[] particles = presentation.GetComponentsInChildren<ParticleSystem>(true);
            TrailRenderer[] trails = presentation.GetComponentsInChildren<TrailRenderer>(true);
            Renderer[] renderers = presentation.GetComponentsInChildren<Renderer>(true);
            HashSet<Material> materials = new();
            int transparentRenderers = 0;
            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                    transparentRenderers++;
                foreach (Material material in renderer.sharedMaterials)
                    if (material != null) materials.Add(material);
            }

            string report =
                "PASS\n" +
                "N-Normal-Impact.png\nG-Great-Impact.png\nP-Perfect-Impact.png\nD-Perfect-Flight.png\n" +
                "B-Bunker-Landing.png\nW-Water-Splash.png\nF-Putter-Impact.png\nH-Hole-In-Result.png\n" +
                $"ParticleSystems={particles.Length}\n" +
                $"SampledPeakActiveParticles={SessionState.GetInt(PeakParticlesKey, 0)}\n" +
                $"VfxRenderers={renderers.Length}\n" +
                $"VfxMaterials={materials.Count}\n" +
                $"TransparentVfxRenderers={transparentRenderers}\n" +
                $"EffectObjects={particles.Length + trails.Length}\n" +
                $"TrailCount={trails.Length}\n";
            File.WriteAllText(ResultPath, report);
        }
    }
}

using System.IO;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    public static class HudSkinCaptureTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PendingKey = "SwingPop.HudSkin.CapturePending";
        private const string PhaseKey = "SwingPop.HudSkin.CapturePhase";
        private const string PhaseStartedKey = "SwingPop.HudSkin.CapturePhaseStarted";
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopHudSkinCapture.result"));
        private static readonly string OutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/hud-skin-pass"));

        static HudSkinCaptureTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("SwingPop/UI/Capture HUD Skin Review Set")]
        public static void CaptureHudSkinReviewSet()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before starting the HUD capture set.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Save or discard active scene changes before capture.");

            Directory.CreateDirectory(OutputDirectory);
            if (File.Exists(ResultPath)) File.Delete(ResultPath);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetString(PhaseStartedKey, string.Empty);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                SetPhaseStarted();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(PendingKey, false);
                if (!File.Exists(ResultPath)) File.WriteAllText(ResultPath, "FAIL\nCapture ended without a result.");
                Debug.Log($"HUD SKIN CAPTURE SET COMPLETE | {OutputDirectory}");
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
                        Capture("A-Address.png", 1920, 1080);
                        Require<GameplayHudView>().ActionButton.onClick.Invoke();
                        AdvancePhase();
                        break;
                    case 1 when elapsed >= 0.35d:
                        Capture("P-Power.png", 1920, 1080);
                        Require<GameplayHudView>().ActionButton.onClick.Invoke();
                        AdvancePhase();
                        break;
                    case 2 when elapsed >= 0.35d:
                        Capture("I-Impact.png", 1920, 1080);
                        Require<ShotFlowController>().ForcePerfectImpactAndCommit();
                        AdvancePhase();
                        break;
                    case 3 when IsFlightReady(elapsed):
                        Capture("D-Perfect-Flight.png", 1920, 1080);
                        PreparePutterAddress();
                        AdvancePhase();
                        break;
                    case 4 when elapsed >= 1d:
                        Capture("F-Putter.png", 1920, 1080);
                        ShowWaterHazardPresentation();
                        AdvancePhase();
                        break;
                    case 5 when elapsed >= 0.3d:
                        Capture("W-Water-Hazard.png", 1920, 1080);
                        ShowResult();
                        AdvancePhase();
                        break;
                    case 6 when elapsed >= 1.25d:
                        Capture("H-Result.png", 1920, 1080);
                        PrepareResolutionAddress();
                        AdvancePhase();
                        break;
                    case 7 when elapsed >= 0.6d:
                        Capture("R-1600x900.png", 1600, 900);
                        AdvancePhase();
                        break;
                    case 8 when elapsed >= 0.25d:
                        Capture("R-1280x720.png", 1280, 720);
                        File.WriteAllText(ResultPath,
                            "PASS\nA-Address.png\nP-Power.png\nI-Impact.png\nD-Perfect-Flight.png\nF-Putter.png\nW-Water-Hazard.png\nH-Result.png\nR-1600x900.png\nR-1280x720.png");
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

        private static bool IsFlightReady(double elapsed)
        {
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            return (ball != null && ball.State != BallState.Ready && elapsed >= 0.45d) || elapsed >= 2.5d;
        }

        private static void PreparePutterAddress()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            CameraDirector cameraDirector = Require<CameraDirector>();
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green);
            if (green == null) throw new System.InvalidOperationException("Green surface was not found.");
            SerializedObject serializedHole = new(holeFlow);
            ClubData putter = serializedHole.FindProperty("putter")?.objectReferenceValue as ClubData;
            if (putter == null) throw new System.InvalidOperationException("Putter data is not assigned.");

            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 start = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(start, green.Data);
            shotFlow.PrepareNextShot(cup - start, putter);
            cameraDirector.RequestDebugMode(CameraMode.Address);
        }

        private static void ShowWaterHazardPresentation()
        {
            GameplayHudView view = Require<GameplayHudView>();
            view.ShowHazard("WATER HAZARD\n+1 PENALTY", view.ResolveTone(HudSkinTone.Coral), 2.6f, 0.18f);
        }

        private static void ShowResult()
        {
            Require<GameplayHudView>().HideTransientFeedback();
            HoleFlowController holeFlow = Require<HoleFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            Rigidbody body = ball.GetComponent<Rigidbody>();
            if (body != null && body.isKinematic)
            {
                body.isKinematic = false;
                body.useGravity = false;
            }
            if (!holeFlow.TryCompleteHole(ball))
                throw new System.InvalidOperationException("Result capture could not complete the hole.");
        }

        private static void PrepareResolutionAddress()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            holeFlow.SetAutomaticFlowSuspended(false);
            holeFlow.DebugResetHole();
            Require<CameraDirector>().SkipIntro();
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
            if (value == null) throw new System.InvalidOperationException($"HUD capture dependency is missing: {typeof(T).Name}");
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

        private static void Capture(string filename, int width, int height)
        {
            string path = Path.Combine(OutputDirectory, filename);
            Camera camera = Require<Camera>();
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32) { name = "HUD Skin Review Capture" };
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
                Debug.Log($"HUD skin review captured {filename} ({width}x{height}).");
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
    }
}

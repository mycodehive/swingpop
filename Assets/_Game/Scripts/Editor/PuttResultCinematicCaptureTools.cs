using System.Collections.Generic;
using System.IO;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    public static class PuttResultCinematicCaptureTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PendingKey = "SwingPop.PuttResult.CapturePending";
        private const string PhaseKey = "SwingPop.PuttResult.CapturePhase";
        private const string PhaseStartedKey = "SwingPop.PuttResult.CapturePhaseStarted";
        private static readonly string OutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/putt-result-cinematic-pass"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Library/PuttResultCinematicValidation/Capture.result"));

        static PuttResultCinematicCaptureTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("SwingPop/Presentation/Capture Putt Result Review Set")]
        public static void CaptureReviewSet()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before starting the putt/result capture set.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Save or discard active scene changes before capture.");

            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            if (File.Exists(ResultPath)) File.Delete(ResultPath);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(PhaseKey, 0);
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
                SetPhaseStarted();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(PendingKey, false);
                if (!File.Exists(ResultPath)) File.WriteAllText(ResultPath, "FAIL\nCapture ended without a result.");
                Debug.Log($"PUTT / RESULT REVIEW CAPTURE COMPLETE | {OutputDirectory}");
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
                        PreparePuttAddress();
                        AdvancePhase();
                        break;
                    case 1 when elapsed >= 0.9d:
                        Capture("F1-Putt-Address.png");
                        if (!Require<ShotFlowController>().TryCommitShot(0.45f, 0f))
                            throw new System.InvalidOperationException("Putter capture shot was rejected.");
                        AdvancePhase();
                        break;
                    case 2 when Require<GolfBallController>().State == BallState.Rolling && elapsed >= 0.12d:
                        Object.FindAnyObjectByType<SwingPop.UI.GameplayHudView>()?.HideTransientFeedback();
                        Capture("F2-Putt-Rolling.png");
                        AdvancePhase();
                        break;
                    case 3 when Require<PuttResultCinematicController>().Phase == PuttResultCinematicPhase.CupApproach
                                && elapsed >= 0.04d:
                        Capture("F3-Cup-Approach.png");
                        AdvancePhase();
                        break;
                    case 4 when Require<HoleFlowController>().State == HoleFlowState.HoleComplete && elapsed >= 0.06d:
                        Capture("H1-Hole-In-Moment.png");
                        AdvancePhase();
                        break;
                    case 5 when Require<PuttResultCinematicController>().Phase == PuttResultCinematicPhase.CharacterReaction
                                && elapsed >= 0.6d:
                        Capture("H2-Character-Celebration.png");
                        AdvancePhase();
                        break;
                    case 6 when Require<PuttResultCinematicController>().Phase == PuttResultCinematicPhase.ResultHold
                                && elapsed >= 0.42d:
                        Capture("H3-Result.png");
                        WriteSuccessResult();
                        EditorApplication.isPlaying = false;
                        break;
                    default:
                        if (elapsed > 12d)
                            throw new System.TimeoutException($"Capture phase {phase} timed out.");
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

        private static void PreparePuttAddress()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            PrepareBallAtDistance(3f);
            CameraDirector camera = Require<CameraDirector>();
            camera.enabled = true;
            Require<PuttResultCinematicController>().PreviewPuttReady();
        }

        private static void PrepareBallAtDistance(float distance)
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green)
                ?? throw new System.InvalidOperationException("Green surface was not found.");
            ClubData putter = new SerializedObject(holeFlow).FindProperty("putter")?.objectReferenceValue as ClubData;
            if (putter == null) throw new System.InvalidOperationException("Putter data is not assigned.");

            Vector3 cup = holeFlow.Hole.CupPosition;
            float y = green.GetComponent<Collider>().bounds.max.y + 0.15f;
            Vector3 start = new(cup.x, y, cup.z - distance);
            ball.PrepareNextShot(start, green.Data);
            shotFlow.PrepareNextShot(cup - start, putter);
        }

        private static TerrainSurface FindSurface(TerrainSurfaceType type)
        {
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include))
                if (surface.SurfaceType == type) return surface;
            return null;
        }

        private static void Capture(string filename)
        {
            string path = Path.Combine(OutputDirectory, filename);
            Camera camera = Require<Camera>();
            const int width = 1920;
            const int height = 1080;
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32) { name = "Putt Result Review Capture" };
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            RenderMode[] modes = new RenderMode[canvases.Length];
            Camera[] canvasCameras = new Camera[canvases.Length];
            float[] distances = new float[canvases.Length];
            try
            {
                Time.timeScale = 0f;
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
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log($"Putt/result review captured {filename}.");
            }
            finally
            {
                Time.timeScale = 1f;
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

        private static void WriteSuccessResult()
        {
            PuttResultCinematicController controller = Require<PuttResultCinematicController>();
            HashSet<string> required = new()
            {
                "F1-Putt-Address.png", "F2-Putt-Rolling.png", "F3-Cup-Approach.png",
                "H1-Hole-In-Moment.png", "H2-Character-Celebration.png", "H3-Result.png"
            };
            foreach (string file in required)
                if (!File.Exists(Path.Combine(OutputDirectory, file)))
                    throw new FileNotFoundException("Expected review capture is missing.", file);

            string report =
                "PASS\n" + string.Join("\n", required) + "\n" +
                $"Resolution=1920x1080\n" +
                $"CinematicStarts={controller.StartCount}\n" +
                $"CharacterReactions={controller.CharacterReactionCount}\n" +
                $"ResultReveals={controller.ResultRevealCount}\n" +
                $"Cameras={Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length}\n" +
                $"Canvases={Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length}\n";
            File.WriteAllText(ResultPath, report);
        }

        private static T Require<T>() where T : Object
        {
            T value = Object.FindAnyObjectByType<T>();
            if (value == null) throw new System.InvalidOperationException($"Capture dependency is missing: {typeof(T).Name}");
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
    }
}

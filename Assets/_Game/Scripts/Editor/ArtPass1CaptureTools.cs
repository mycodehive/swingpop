using System.IO;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    public static class ArtPass1CaptureTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PendingKey = "SwingPop.ArtPass1.CapturePending";
        private const string PhaseKey = "SwingPop.ArtPass1.CapturePhase";
        private const string PhaseStartedKey = "SwingPop.ArtPass1.CapturePhaseStarted";
        private const string IdentityCaptureKey = "SwingPop.CharacterIdentity.Capture";
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopArtPass1Capture.request"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopArtPass1Capture.result"));
        private static readonly string ArtPass1OutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/art-pass-1"));
        private static readonly string IdentityRequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopCharacterIdentityCapture.request"));
        private static readonly string IdentityResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopCharacterIdentityCapture.result"));
        private static readonly string IdentityOutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/character-identity-pass"));
        private static string OutputDirectory => SessionState.GetBool(IdentityCaptureKey, false)
            ? IdentityOutputDirectory
            : ArtPass1OutputDirectory;
        private static string ActiveResultPath => SessionState.GetBool(IdentityCaptureKey, false)
            ? IdentityResultPath
            : ResultPath;

        static ArtPass1CaptureTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [DidReloadScripts]
        private static void ConsumeRequestAfterReload()
        {
            EditorApplication.delayCall += TryBeginRequestedCapture;
        }

        [MenuItem("SwingPop/Art Pass 1/Capture Review Set 1920x1080")]
        public static void CaptureReviewSet()
        {
            CaptureReviewSet(false);
        }

        [MenuItem("SwingPop/Character/Capture Identity Review Set 1920x1080")]
        public static void CaptureCharacterIdentityReviewSet()
        {
            CaptureReviewSet(true);
        }

        private static void CaptureReviewSet(bool identityCapture)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new System.InvalidOperationException("Stop Play Mode before starting the Art Pass 1 capture set.");
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                throw new System.InvalidOperationException("Save or discard active scene changes before capture.");
            }

            SessionState.SetBool(IdentityCaptureKey, identityCapture);
            Directory.CreateDirectory(OutputDirectory);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetString(PhaseStartedKey, string.Empty);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void TryBeginRequestedCapture()
        {
            bool identityCapture = File.Exists(IdentityRequestPath);
            if (!identityCapture && !File.Exists(RequestPath)) return;
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryBeginRequestedCapture;
                return;
            }
            File.Delete(identityCapture ? IdentityRequestPath : RequestPath);
            try
            {
                CaptureReviewSet(identityCapture);
            }
            catch (System.Exception exception)
            {
                File.WriteAllText(identityCapture ? IdentityResultPath : ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
            }
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
                if (!File.Exists(ActiveResultPath))
                {
                    File.WriteAllText(ActiveResultPath, "PASS\nA-Address.png\nD-Perfect-Flight.png\nF-Putter-Address.png\nH-Result.png");
                }
                Debug.Log($"CHARACTER REVIEW CAPTURE SET COMPLETE | {OutputDirectory}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(File.Exists(ActiveResultPath)
                        && File.ReadAllText(ActiveResultPath).StartsWith("PASS") ? 0 : 1);
                }
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    TryBeginRequestedCapture();
                }
                return;
            }
            if (!EditorApplication.isPlaying) return;
            double elapsed = PhaseElapsed();
            int phase = SessionState.GetInt(PhaseKey, 0);
            try
            {
                switch (phase)
                {
                    case 0 when elapsed >= 4.2d:
                        Capture("A-Address.png");
                        BeginPerfectShot();
                        AdvancePhase();
                        break;
                    case 1 when IsPerfectFlightReady(elapsed):
                        Capture("D-Perfect-Flight.png");
                        PreparePutterAddress();
                        AdvancePhase();
                        break;
                    case 2 when elapsed >= 1.2d:
                        Capture("F-Putter-Address.png");
                        ShowResult();
                        AdvancePhase();
                        break;
                    case 3 when elapsed >= 1.35d:
                        Capture("H-Result.png");
                        File.WriteAllText(ActiveResultPath, "PASS\nA-Address.png\nD-Perfect-Flight.png\nF-Putter-Address.png\nH-Result.png");
                        EditorApplication.isPlaying = false;
                        break;
                }
            }
            catch (System.Exception exception)
            {
                File.WriteAllText(ActiveResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
                EditorApplication.isPlaying = false;
            }
        }

        private static void BeginPerfectShot()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            CameraDirector cameraDirector = Require<CameraDirector>();
            ShotInputController input = Object.FindAnyObjectByType<ShotInputController>();
            if (input != null) input.enabled = false;
            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            cameraDirector.SkipIntro();
            if (!shotFlow.TryCommitShot(0.68f, 0f))
            {
                throw new System.InvalidOperationException("Perfect capture shot commit was rejected.");
            }
        }

        private static bool IsPerfectFlightReady(double elapsed)
        {
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            return ball != null && ball.State != BallState.Ready && elapsed >= 0.45d || elapsed >= 2.5d;
        }

        private static void PreparePutterAddress()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            CameraDirector cameraDirector = Require<CameraDirector>();
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green);
            if (green == null) throw new System.InvalidOperationException("Green surface was not found for Putter capture.");
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

        private static void ShowResult()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            GolfBallController ball = Require<GolfBallController>();
            Rigidbody body = ball.GetComponent<Rigidbody>();
            if (body != null && body.isKinematic)
            {
                body.isKinematic = false;
                body.useGravity = false;
            }
            if (!holeFlow.TryCompleteHole(ball))
            {
                throw new System.InvalidOperationException("Result capture could not complete the hole.");
            }
        }

        private static TerrainSurface FindSurface(TerrainSurfaceType type)
        {
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>(FindObjectsInactive.Include))
            {
                if (surface.SurfaceType == type) return surface;
            }
            return null;
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

        private static void Capture(string filename)
        {
            string path = Path.Combine(OutputDirectory, filename);
            Camera camera = Require<Camera>();
            const int width = 1920;
            const int height = 1080;
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32) { name = "ART PASS 1 Review Capture" };
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            RenderMode[] modes = new RenderMode[canvases.Length];
            Camera[] cameras = new Camera[canvases.Length];
            float[] distances = new float[canvases.Length];

            try
            {
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
                Debug.Log($"Character review captured {filename}");
            }
            finally
            {
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

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
    public static class CourseEnvironmentCaptureTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PendingKey = "SwingPop.CourseEnvironment.CapturePending";
        private const string PhaseKey = "SwingPop.CourseEnvironment.CapturePhase";
        private const string PhaseStartedKey = "SwingPop.CourseEnvironment.CapturePhaseStarted";
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopCourseEnvironmentCapture.request"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopCourseEnvironmentCapture.result"));
        private static readonly string OutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/course-environment-pass"));

        static CourseEnvironmentCaptureTools()
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

        [MenuItem("SwingPop/Environment/Capture Course Review Set 1920x1080")]
        public static void CaptureCourseReviewSet()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new System.InvalidOperationException("Stop Play Mode before starting the course environment capture set.");
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                throw new System.InvalidOperationException("Save or discard active scene changes before capture.");
            }
            Directory.CreateDirectory(OutputDirectory);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetString(PhaseStartedKey, string.Empty);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void TryBeginRequestedCapture()
        {
            if (!File.Exists(RequestPath)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryBeginRequestedCapture;
                return;
            }
            File.Delete(RequestPath);
            try
            {
                CaptureCourseReviewSet();
            }
            catch (System.Exception exception)
            {
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
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
                if (!File.Exists(ResultPath))
                {
                    File.WriteAllText(ResultPath,
                        "PASS\nA-Address.png\nD-Perfect-Flight.png\nF-Putter-Address.png\nH-Result.png\nW-Water.png\nB-Bunker.png");
                }
                Debug.Log($"COURSE ENVIRONMENT CAPTURE SET COMPLETE | {OutputDirectory}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(File.ReadAllText(ResultPath).StartsWith("PASS") ? 0 : 1);
                }
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) TryBeginRequestedCapture();
                return;
            }
            if (!EditorApplication.isPlaying) return;

            int phase = SessionState.GetInt(PhaseKey, 0);
            double elapsed = PhaseElapsed();
            try
            {
                switch (phase)
                {
                    case 0 when elapsed >= 4.2d:
                        Capture("A-Address.png", true);
                        PrepareEnvironmentView(new Vector3(-22f, 6.2f, 25f), new Vector3(-11.5f, 0.18f, 34f), 42f);
                        AdvancePhase();
                        break;
                    case 1 when elapsed >= 0.45d:
                        Capture("W-Water.png", false);
                        PrepareEnvironmentView(new Vector3(17f, 6.2f, 46f), new Vector3(7.5f, 0.2f, 54f), 42f);
                        AdvancePhase();
                        break;
                    case 2 when elapsed >= 0.45d:
                        Capture("B-Bunker.png", false);
                        BeginPerfectShot();
                        AdvancePhase();
                        break;
                    case 3 when IsPerfectFlightReady(elapsed):
                        Capture("D-Perfect-Flight.png", true);
                        PreparePutterAddress();
                        AdvancePhase();
                        break;
                    case 4 when elapsed >= 1.2d:
                        Capture("F-Putter-Address.png", true);
                        ShowResult();
                        AdvancePhase();
                        break;
                    case 5 when elapsed >= 1.35d:
                        Capture("H-Result.png", true);
                        File.WriteAllText(ResultPath,
                            "PASS\nA-Address.png\nD-Perfect-Flight.png\nF-Putter-Address.png\nH-Result.png\nW-Water.png\nB-Bunker.png");
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

        private static void PrepareEnvironmentView(Vector3 cameraPosition, Vector3 target, float fieldOfView)
        {
            ShotInputController input = Object.FindAnyObjectByType<ShotInputController>();
            if (input != null) input.enabled = false;
            HoleFlowController holeFlow = Require<HoleFlowController>();
            holeFlow.SetAutomaticFlowSuspended(true);
            CameraDirector director = Require<CameraDirector>();
            director.enabled = false;
            Camera camera = Require<Camera>();
            camera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(target - cameraPosition, Vector3.up));
            camera.fieldOfView = fieldOfView;
        }

        private static void BeginPerfectShot()
        {
            HoleFlowController holeFlow = Require<HoleFlowController>();
            ShotFlowController shotFlow = Require<ShotFlowController>();
            CameraDirector cameraDirector = Require<CameraDirector>();
            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            cameraDirector.enabled = true;
            cameraDirector.SkipIntro();
            if (!shotFlow.TryCommitShot(0.68f, 0f))
            {
                throw new System.InvalidOperationException("Perfect capture shot commit was rejected.");
            }
        }

        private static bool IsPerfectFlightReady(double elapsed)
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

        private static void Capture(string filename, bool includeHud)
        {
            string path = Path.Combine(OutputDirectory, filename);
            Camera camera = Require<Camera>();
            const int width = 1920;
            const int height = 1080;
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32) { name = "Course Environment Review Capture" };
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            RenderMode[] modes = new RenderMode[canvases.Length];
            Camera[] cameras = new Camera[canvases.Length];
            float[] distances = new float[canvases.Length];
            bool[] enabled = new bool[canvases.Length];

            try
            {
                for (int index = 0; index < canvases.Length; index++)
                {
                    modes[index] = canvases[index].renderMode;
                    cameras[index] = canvases[index].worldCamera;
                    distances[index] = canvases[index].planeDistance;
                    enabled[index] = canvases[index].enabled;
                    if (!includeHud)
                    {
                        canvases[index].enabled = false;
                    }
                    else if (canvases[index].renderMode == RenderMode.ScreenSpaceOverlay)
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
                Debug.Log($"Course environment review captured {filename}");
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
                    canvases[index].enabled = enabled[index];
                }
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}

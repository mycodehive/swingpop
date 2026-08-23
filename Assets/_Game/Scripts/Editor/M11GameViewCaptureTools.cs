using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwingPop.Editor
{
    [InitializeOnLoad]
    public static class M11GameViewCaptureTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string CapturePendingKey = "SwingPop.M11.CapturePending";
        private const string CaptureOutputKey = "SwingPop.M11.CaptureOutput";
        private const string CaptureStartedKey = "SwingPop.M11.CaptureStarted";
        private const string CaptureCompleteKey = "SwingPop.M11.CaptureComplete";
        private const string CaptureWidthKey = "SwingPop.M11.CaptureWidth";
        private const string CaptureHeightKey = "SwingPop.M11.CaptureHeight";
        private static readonly string RequestPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Temp/SwingPopM11Capture.request"));

        static M11GameViewCaptureTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= TryCapture;
            EditorApplication.update += TryCapture;
            EditorApplication.delayCall += TryRunCaptureRequest;
        }

        [MenuItem("SwingPop/M11/Capture Current Address 1920x1080")]
        public static void CaptureCurrentAddress()
        {
            BeginCapture(Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/M11CurrentAddress.png")), 1920, 1080);
        }

        public static void CaptureBaselineForAutomation()
        {
            BeginCapture(Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/review-captures/M11BaselineAddress.png")), 1920, 1080);
        }

        public static void CapturePolishedForAutomation()
        {
            BeginCapture(Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/review-captures/M11PolishedAddress.png")), 1920, 1080);
        }

        public static void CapturePolished1600ForAutomation()
        {
            BeginCapture(Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/review-captures/M11PolishedAddress_1600x900.png")), 1600, 900);
        }

        public static void CapturePolished1280ForAutomation()
        {
            BeginCapture(Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/review-captures/M11PolishedAddress_1280x720.png")), 1280, 720);
        }

        private static void BeginCapture(string outputPath, int width, int height)
        {
            SessionState.SetBool(CapturePendingKey, true);
            SessionState.SetBool(CaptureCompleteKey, false);
            SessionState.SetString(CaptureOutputKey, outputPath);
            SessionState.SetString(CaptureStartedKey, string.Empty);
            SessionState.SetInt(CaptureWidthKey, width);
            SessionState.SetInt(CaptureHeightKey, height);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void TryRunCaptureRequest()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
                EditorApplication.delayCall += TryRunCaptureRequest;
                return;
            }

            string request = File.ReadAllText(RequestPath).Trim().ToUpperInvariant();
            File.Delete(RequestPath);
            string filename = request == "POLISHED" ? "M11PolishedAddress.png" : "M11BaselineAddress.png";
            BeginCapture(Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/review-captures", filename)), 1920, 1080);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(CapturePendingKey, false))
            {
                return;
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetString(CaptureStartedKey, EditorApplication.timeSinceStartup.ToString("R"));
            }
            else if (change == PlayModeStateChange.EnteredEditMode
                     && SessionState.GetBool(CaptureCompleteKey, false))
            {
                string outputPath = SessionState.GetString(CaptureOutputKey, string.Empty);
                SessionState.SetBool(CapturePendingKey, false);
                SessionState.SetBool(CaptureCompleteKey, false);
                Debug.Log($"M11 Game View capture completed: {outputPath}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }

        private static void TryCapture()
        {
            if (!SessionState.GetBool(CapturePendingKey, false) || !EditorApplication.isPlaying)
            {
                return;
            }

            string startedText = SessionState.GetString(CaptureStartedKey, string.Empty);
            if (!double.TryParse(startedText, out double started)
                || EditorApplication.timeSinceStartup - started < 4.2d)
            {
                return;
            }

            string outputPath = SessionState.GetString(CaptureOutputKey, string.Empty);
            int width = SessionState.GetInt(CaptureWidthKey, 1920);
            int height = SessionState.GetInt(CaptureHeightKey, 1080);
            CaptureGameView(outputPath, width, height);
            SessionState.SetBool(CaptureCompleteKey, true);
            Time.timeScale = 1f;
            EditorApplication.isPlaying = false;
        }

        private static void CaptureGameView(string outputPath, int width, int height)
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                throw new System.InvalidOperationException("M11 capture could not find the active Camera.");
            }

            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "M11 Address Capture"
            };
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            RenderMode[] renderModes = new RenderMode[canvases.Length];
            Camera[] worldCameras = new Camera[canvases.Length];
            float[] planeDistances = new float[canvases.Length];

            try
            {
                Time.timeScale = 0f;
                for (int index = 0; index < canvases.Length; index++)
                {
                    renderModes[index] = canvases[index].renderMode;
                    worldCameras[index] = canvases[index].worldCamera;
                    planeDistances[index] = canvases[index].planeDistance;
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
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                for (int index = 0; index < canvases.Length; index++)
                {
                    canvases[index].renderMode = renderModes[index];
                    canvases[index].worldCamera = worldCameras[index];
                    canvases[index].planeDistance = planeDistances[index];
                }
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}

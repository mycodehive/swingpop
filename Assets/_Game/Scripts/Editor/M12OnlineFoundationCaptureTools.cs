using System;
using System.Globalization;
using System.IO;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SwingPop.Editor
{
    /// <summary>
    /// Drives the real Hole01 gameplay graph through a deterministic local two-player
    /// review sequence and captures the six M12 acceptance states.
    /// </summary>
    [InitializeOnLoad]
    public static class M12OnlineFoundationCaptureTools
    {
        private const string ScenePath = "Assets/_Game/Scenes/Hole01_SkyIsland.unity";
        private const string PendingKey = "SwingPop.M12.CapturePending";
        private const string PhaseKey = "SwingPop.M12.CapturePhase";
        private const string PhaseStartedKey = "SwingPop.M12.CapturePhaseStarted";
        private const string InitialObjectsKey = "SwingPop.M12.CaptureInitialObjects";
        private const string InitialCamerasKey = "SwingPop.M12.CaptureInitialCameras";
        private const string InitialCanvasesKey = "SwingPop.M12.CaptureInitialCanvases";

        private static readonly string OutputDirectory = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../docs/review-captures/m12-online-foundation"));
        private static readonly string ResultPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../Library/M12/Capture.result"));
        private static readonly string[] CaptureFiles =
        {
            "A-Player-A-Turn.png",
            "B-Player-A-Shot.png",
            "C-Player-B-Turn.png",
            "D-Player-B-Shot.png",
            "E-Player-A-Restored-Position.png",
            "F-Multiplayer-Hole-Result.png"
        };

        static M12OnlineFoundationCaptureTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem("SwingPop/Online/Capture M12 Review Set")]
        public static void CaptureReviewSet()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before starting the M12 capture set.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save or discard active scene changes before capture.");

            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            foreach (string file in CaptureFiles)
            {
                string path = Path.Combine(OutputDirectory, file);
                if (File.Exists(path)) File.Delete(path);
            }
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
                Object.FindAnyObjectByType<ShotDebugOverlay>()?.SetOverlayVisible(false);
                Object.FindAnyObjectByType<BallTrajectoryDebug>()?.SetTrajectoryVisible(false);

                MatchSessionController session = Require<MatchSessionController>();
                session.StartDevelopmentMatch(MultiplayerDevelopmentMode.LocalTwoPlayer, 0);
                SessionState.SetInt(InitialObjectsKey, CountSceneGameObjects());
                SessionState.SetInt(InitialCamerasKey, Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length);
                SessionState.SetInt(InitialCanvasesKey, Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length);
                SetPhaseStarted();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(PendingKey, false);
                if (!File.Exists(ResultPath)) File.WriteAllText(ResultPath, "FAIL\nCapture ended without a result.");
                Debug.Log($"M12 REVIEW CAPTURE COMPLETE | {OutputDirectory}");
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
                MatchSessionController session = Require<MatchSessionController>();
                GolfBallController ball = Require<GolfBallController>();
                ShotFlowController shotFlow = Require<ShotFlowController>();
                HoleFlowController holeFlow = Require<HoleFlowController>();

                switch (phase)
                {
                    case 0 when IsPreparing(session, "player-a")
                                && Require<MultiplayerTurnPresenter>().IsVisible
                                && Require<MultiplayerTurnPresenter>().TurnLabel == "YOUR TURN"
                                && elapsed >= 1d:
                        Capture(CaptureFiles[0]);
                        if (!shotFlow.TryCommitShot(0.68f, 0f)) throw new InvalidOperationException("Player A shot submission failed.");
                        AdvancePhase();
                        break;
                    case 1 when ball.State == BallState.Airborne && elapsed >= 0.2d:
                        Capture(CaptureFiles[1]);
                        CompleteActiveShot(session, ball, holeFlow, false, false);
                        AdvancePhase();
                        break;
                    case 2 when IsPreparing(session, "player-b") && elapsed >= 0.65d:
                        Capture(CaptureFiles[2]);
                        if (!session.SubmitSimulatedRemoteShotNow()) throw new InvalidOperationException("Player B simulated submission failed.");
                        AdvancePhase();
                        break;
                    case 3 when ball.State == BallState.Airborne && elapsed >= 0.2d:
                        Capture(CaptureFiles[3]);
                        CompleteActiveShot(session, ball, holeFlow, false, false);
                        AdvancePhase();
                        break;
                    case 4 when IsPreparing(session, "player-a") && elapsed >= 0.7d:
                        Capture(CaptureFiles[4]);
                        if (!shotFlow.TryCommitShot(0.45f, 0f)) throw new InvalidOperationException("Player A hole-out submission failed.");
                        AdvancePhase();
                        break;
                    case 5 when ball.State == BallState.Airborne && elapsed >= 0.16d:
                        CompleteActiveShot(session, ball, holeFlow, true, false);
                        AdvancePhase();
                        break;
                    case 6 when IsPreparing(session, "player-b") && elapsed >= 0.35d:
                        if (!session.SubmitSimulatedRemoteShotNow()) throw new InvalidOperationException("Player B final submission failed.");
                        AdvancePhase();
                        break;
                    case 7 when ball.State == BallState.Airborne && elapsed >= 0.16d:
                        if (!holeFlow.TryCompleteHole(ball)) throw new InvalidOperationException("Existing HoleFlow could not complete Player B.");
                        AdvancePhase();
                        break;
                    case 8 when session.CurrentSnapshot != null
                                && session.CurrentSnapshot.Phase == MatchPhase.HoleComplete
                                && elapsed >= 4.5d:
                        Capture(CaptureFiles[5]);
                        WriteResult(session);
                        AdvancePhase();
                        EditorApplication.isPlaying = false;
                        break;
                    default:
                        if (elapsed > 15d) throw new TimeoutException($"M12 capture phase {phase} timed out.");
                        break;
                }
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
                File.WriteAllText(ResultPath, "FAIL\n" + exception);
                Debug.LogException(exception);
                EditorApplication.isPlaying = false;
            }
        }

        private static bool IsPreparing(MatchSessionController session, string playerId)
        {
            MatchSnapshot snapshot = session.CurrentSnapshot;
            return snapshot != null
                   && snapshot.Phase == MatchPhase.Playing
                   && snapshot.TurnState == TurnState.PreparingShot
                   && snapshot.CurrentTurnPlayer == new MatchPlayerId(playerId);
        }

        private static void CompleteActiveShot(
            MatchSessionController session,
            GolfBallController ball,
            HoleFlowController holeFlow,
            bool holed,
            bool holeComplete)
        {
            MatchSnapshot snapshot = session.CurrentSnapshot
                ?? throw new InvalidOperationException("Match snapshot is missing.");
            if (!snapshot.TryGetPlayer(snapshot.CurrentTurnPlayer, out PlayerSnapshot player))
                throw new InvalidOperationException("Active player snapshot is missing.");

            Vector3 position = holed ? holeFlow.Hole.CupPosition : FindFairwayPosition(holeFlow, player.DisplayOrder);
            NetworkVector3 networkPosition = NetworkVector3.FromUnity(position);
            NetworkShotResult result = new(
                snapshot.MatchId,
                snapshot.CurrentTurnPlayer,
                snapshot.TurnIndex,
                snapshot.ShotSequence,
                networkPosition,
                networkPosition,
                holed ? TerrainSurfaceType.Green : TerrainSurfaceType.Fairway,
                player.StrokeCount + 1,
                player.PenaltyCount,
                holed,
                holeComplete,
                holed ? -2 : 0,
                holed ? "EAGLE" : string.Empty);
            if (!session.ApplyAuthoritativeShotResult(result))
                throw new InvalidOperationException("Authoritative result submission failed.");
        }

        private static Vector3 FindFairwayPosition(HoleFlowController holeFlow, int playerOrder)
        {
            Vector3 tee = holeFlow.Hole.TeePosition;
            Vector3 cup = holeFlow.Hole.CupPosition;
            float fraction = playerOrder == 0 ? 0.3f : 0.22f;
            Vector3 desired = Vector3.Lerp(tee, cup, fraction);
            Ray ray = new(desired + Vector3.up * 80f, Vector3.down);
            RaycastHit[] hits = Physics.RaycastAll(ray, 160f, ~0, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                TerrainSurface surface = hit.collider.GetComponent<TerrainSurface>()
                                         ?? hit.collider.GetComponentInParent<TerrainSurface>();
                if (surface != null && surface.SurfaceType == TerrainSurfaceType.Fairway)
                    return hit.point + Vector3.up * 0.08f;
            }
            return new Vector3(desired.x, tee.y, desired.z);
        }

        private static void Capture(string filename)
        {
            const int width = 1920;
            const int height = 1080;
            string path = Path.Combine(OutputDirectory, filename);
            Camera camera = Require<Camera>();
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32) { name = "M12 Review Capture" };
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

        private static void WriteResult(MatchSessionController session)
        {
            foreach (string file in CaptureFiles)
                if (!File.Exists(Path.Combine(OutputDirectory, file)))
                    throw new FileNotFoundException("Expected M12 capture is missing.", file);
            int objects = CountSceneGameObjects();
            int cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length;
            int canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length;
            if (objects != SessionState.GetInt(InitialObjectsKey, -1)
                || cameras != SessionState.GetInt(InitialCamerasKey, -1)
                || canvases != SessionState.GetInt(InitialCanvasesKey, -1))
                throw new InvalidOperationException("Runtime object counts changed during M12 capture.");

            LocalLoopbackTransport transport = session.Transport;
            string report =
                "PASS\n" +
                $"Captures={CaptureFiles.Length}\n" +
                "Resolution=1920x1080\n" +
                $"GameObjects={objects}\n" +
                $"Cameras={cameras}\n" +
                $"Canvases={canvases}\n" +
                $"TransportMessages={transport.DispatchedMessageCount}\n" +
                $"TransportBytes={transport.DispatchedPayloadBytes}\n" +
                $"MaxPayloadBytes={transport.MaximumPayloadBytes}\n" +
                $"SnapshotVersion={session.CurrentSnapshot.Version}\n" +
                $"MatchPhase={session.CurrentSnapshot.Phase}\n";
            File.WriteAllText(ResultPath, report);
        }

        private static int CountSceneGameObjects()
        {
            int count = 0;
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<Transform>(true).Length;
            return count;
        }

        private static T Require<T>() where T : Object
        {
            T value = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            return value != null ? value : throw new InvalidOperationException($"Required {typeof(T).Name} is missing.");
        }

        private static void AdvancePhase()
        {
            SessionState.SetInt(PhaseKey, SessionState.GetInt(PhaseKey, 0) + 1);
            SetPhaseStarted();
        }

        private static void SetPhaseStarted()
        {
            SessionState.SetString(PhaseStartedKey,
                EditorApplication.timeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
        }

        private static double PhaseElapsed()
        {
            string raw = SessionState.GetString(PhaseStartedKey, "0");
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double started)
                ? EditorApplication.timeSinceStartup - started
                : 0d;
        }
    }
}

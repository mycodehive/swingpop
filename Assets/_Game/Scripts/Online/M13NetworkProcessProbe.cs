using System;
using System.IO;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Debugging;
using UnityEngine;

namespace SwingPop.Online
{
    /// <summary>Development-build-only two-process acceptance probe activated exclusively by command line.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class M13NetworkProcessProbe : MonoBehaviour
    {
        private MatchSessionController session;
        private UnityTransportMatchTransport transport;
        private ShotFlowController shotFlow;
        private HoleFlowController holeFlow;
        private GolfBallController ball;
        private string logPath;
        private string captureDirectory;
        private bool automated;
        private bool localShotSubmitted;
        private bool forceHolePending;
        private float forceHoleAt;
        private float quitAt = -1f;
        private bool completed;
        private bool hostWaitingCaptured;
        private bool connectedCaptured;
        private bool hostTurnCaptured;
        private bool clientTurnCaptured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!HasArg(args, "-swingpopAutomatedNetworkTest")) return;
            new GameObject("M13 Network Process Probe").AddComponent<M13NetworkProcessProbe>();
        }

        private void Start()
        {
            automated = true;
            string[] args = Environment.GetCommandLineArgs();
            logPath = ReadArg(args, "-swingpopProbeLog=");
            captureDirectory = ReadArg(args, "-swingpopCaptureDirectory=");
            session = FindAnyObjectByType<MatchSessionController>();
            shotFlow = FindAnyObjectByType<ShotFlowController>();
            holeFlow = FindAnyObjectByType<HoleFlowController>();
            ball = FindAnyObjectByType<GolfBallController>();
            transport = session != null ? session.NetworkTransport : null;
            MultiplayerDebugOverlay overlay = FindAnyObjectByType<MultiplayerDebugOverlay>();
            overlay?.SetVisible(true);
            if (session == null || shotFlow == null || holeFlow == null || ball == null || transport == null)
            {
                Write("BOOTSTRAP FAILED: runtime dependencies missing");
                Application.Quit(2);
                return;
            }

            session.SnapshotChanged += OnSnapshot;
            transport.PlayerAssigned += player => Write($"PLAYER ASSIGNED {player}");
            transport.Disconnected += OnDisconnected;
            transport.ShotApprovedReceived += OnShotApproved;
            Write($"BOOT role={session.ActiveMode} endpoint={transport.Address}:{transport.Port} protocol={OnlineProtocol.CurrentVersion}");
        }

        private void Update()
        {
            if (!automated || session == null || transport == null) return;
            if (session.ActiveMode == MultiplayerDevelopmentMode.NetworkHost
                && transport.ConnectionState == NetworkConnectionState.Listening && !hostWaitingCaptured)
            {
                hostWaitingCaptured = true;
                Capture("A-Host-Waiting.png");
                Write("HOST WAITING");
            }
            if (transport.IsReady && !connectedCaptured)
            {
                connectedCaptured = true;
                Capture(session.ActiveMode == MultiplayerDevelopmentMode.NetworkClient
                    ? "B-Client-Connected.png" : "C-Host-Turn.png");
                Write("CONNECTED IN_MATCH");
            }

            MatchSnapshot snapshot = session.CurrentSnapshot;
            if (snapshot != null && snapshot.Phase == MatchPhase.Playing
                && snapshot.TurnState == TurnState.PreparingShot && session.CanSubmitShot && !localShotSubmitted)
            {
                if (session.ActiveMode == MultiplayerDevelopmentMode.NetworkHost && !hostTurnCaptured)
                {
                    hostTurnCaptured = true;
                    Capture("C-Host-Turn.png");
                }
                if (session.ActiveMode == MultiplayerDevelopmentMode.NetworkClient && !clientTurnCaptured)
                {
                    clientTurnCaptured = true;
                    Capture("D-Client-Turn.png");
                }
                localShotSubmitted = shotFlow.TryCommitShot(0.48f, 0f);
                Write($"LOCAL SHOT SUBMIT accepted={localShotSubmitted} player={session.LocalPlayerId} turn={snapshot.TurnIndex}");
            }

            if (forceHolePending && Time.realtimeSinceStartup >= forceHoleAt)
            {
                forceHolePending = false;
                bool forced = holeFlow.TryCompleteHole(ball);
                Write($"HOST GAMEPLAY FORCE-HOLE accepted={forced}");
            }

            if (completed && quitAt > 0f && Time.realtimeSinceStartup >= quitAt)
            {
                Write($"PROCESS COMPLETE tx={transport.SentBytes} rx={transport.ReceivedBytes} rtt={transport.RoundTripTimeMilliseconds:F0}");
                Application.Quit(0);
            }
        }

        private void OnDestroy()
        {
            if (session != null) session.SnapshotChanged -= OnSnapshot;
            if (transport != null)
            {
                transport.Disconnected -= OnDisconnected;
                transport.ShotApprovedReceived -= OnShotApproved;
            }
        }

        private void OnShotApproved(ApprovedShot approved)
        {
            Write($"SHOT APPROVED player={approved.PlayerId} seq={approved.ShotSequence}");
            if (session.ActiveMode == MultiplayerDevelopmentMode.NetworkHost)
            {
                forceHolePending = true;
                forceHoleAt = Time.realtimeSinceStartup + 0.35f;
            }
        }

        private void OnSnapshot(MatchSnapshot snapshot)
        {
            string hash = MatchSnapshotHash.Compute(snapshot);
            Write($"SNAPSHOT version={snapshot.Version} turn={snapshot.TurnIndex} current={snapshot.CurrentTurnPlayer} " +
                  $"phase={snapshot.Phase} state={snapshot.TurnState} hash={hash}");
            if (snapshot.TurnIndex >= 1)
                localShotSubmitted = false;
            if (snapshot.TurnIndex >= 1 && snapshot.TurnState == TurnState.PreparingShot)
                Capture(session.ActiveMode == MultiplayerDevelopmentMode.NetworkHost
                    ? "E-Host-Same-Snapshot.png" : "E-Client-Same-Snapshot.png");
            if (snapshot.Phase is MatchPhase.HoleComplete or MatchPhase.MatchComplete)
            {
                Capture(session.ActiveMode == MultiplayerDevelopmentMode.NetworkHost
                    ? "F-Match-Complete.png" : "F-Client-Match-Complete.png");
                completed = true;
                quitAt = Time.realtimeSinceStartup + (session.ActiveMode == MultiplayerDevelopmentMode.NetworkClient ? 1.5f : 4f);
            }
        }

        private void OnDisconnected(string reason)
        {
            Write($"DISCONNECTED reason={reason}");
            if (session.ActiveMode == MultiplayerDevelopmentMode.NetworkHost && completed)
                quitAt = Mathf.Min(quitAt, Time.realtimeSinceStartup + 0.5f);
        }

        private void Capture(string fileName)
        {
            if (string.IsNullOrWhiteSpace(captureDirectory)) return;
            Directory.CreateDirectory(captureDirectory);
            ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, fileName));
        }

        private void Write(string message)
        {
            string line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
            Debug.Log($"[M13][ProcessProbe] {message}", this);
            if (string.IsNullOrWhiteSpace(logPath)) return;
            string directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.AppendAllText(logPath, line);
        }

        private static bool HasArg(string[] args, string value)
        {
            foreach (string arg in args)
                if (string.Equals(arg, value, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string ReadArg(string[] args, string prefix)
        {
            foreach (string arg in args)
                if (arg != null && arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(prefix.Length).Trim('"');
            return string.Empty;
        }
    }
}

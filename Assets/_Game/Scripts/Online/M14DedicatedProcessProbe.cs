using System;
using System.IO;
using SwingPop.Debugging;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Online
{
    /// <summary>Development-only three-process acceptance probe enabled by an explicit command line flag.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class M14DedicatedProcessProbe : MonoBehaviour
    {
        private MatchSessionController session;
        private UnityTransportMatchTransport clientTransport;
        private DedicatedServerMatchTransport serverTransport;
        private DedicatedServerBootstrap serverBootstrap;
        private ShotFlowController shotFlow;
        private HoleFlowController holeFlow;
        private GolfBallController ball;
        private string logPath;
        private string captureDirectory;
        private int lastSubmittedTurn = -1;
        private bool forceHolePending;
        private float forceHoleAt;
        private float quitAt = -1f;
        private float failureAt;
        private bool listeningLogged;
        private bool completeLogged;
        private bool disconnectCaptured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!HasArg(args, "-swingpopAutomatedDedicatedTest")) return;
            new GameObject("M14 Dedicated Process Probe").AddComponent<M14DedicatedProcessProbe>();
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            logPath = ReadArg(args, "-swingpopProbeLog=");
            captureDirectory = ReadArg(args, "-swingpopCaptureDirectory=");
            session = FindAnyObjectByType<MatchSessionController>();
            shotFlow = FindAnyObjectByType<ShotFlowController>();
            holeFlow = FindAnyObjectByType<HoleFlowController>();
            ball = FindAnyObjectByType<GolfBallController>();
            serverBootstrap = FindAnyObjectByType<DedicatedServerBootstrap>();
            clientTransport = session != null ? session.NetworkTransport : null;
            serverTransport = session != null ? session.DedicatedServerTransport : null;
            failureAt = Time.realtimeSinceStartup + 120f;
            if (session == null || shotFlow == null || holeFlow == null || ball == null
                || clientTransport == null || serverTransport == null)
            {
                Write("BOOTSTRAP FAILED: runtime dependencies missing");
                Application.Quit(2);
                return;
            }

            session.SnapshotChanged += OnSnapshot;
            clientTransport.PlayerAssigned += OnPlayerAssigned;
            clientTransport.Disconnected += OnClientDisconnected;
            clientTransport.ShotApprovedReceived += OnShotApproved;
            serverTransport.PlayerConnected += player => Write($"PLAYER CONNECTED {player}");
            serverTransport.PlayerDisconnected += OnServerPlayerDisconnected;
            serverTransport.ShotApprovedReceived += OnShotApproved;
            MultiplayerDebugOverlay overlay = FindAnyObjectByType<MultiplayerDebugOverlay>();
            overlay?.SetVisible(true);
            Write($"BOOT mode={session.ActiveMode} endpoint={Endpoint()} protocol={OnlineProtocol.CurrentVersion}");
            if (session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer && serverBootstrap != null)
            {
                ServerPresentationStats stats = serverBootstrap.PresentationStats;
                Write($"HEADLESS nographics={serverBootstrap.IsNoGraphics} cameras={stats.ActiveCameras} " +
                      $"canvases={stats.ActiveCanvases} audio={stats.ActiveAudioSources} colliders={stats.GameplayColliders} " +
                      $"renderersDisabled={stats.DisabledRenderers}");
            }
        }

        private void Update()
        {
            if (session == null) return;
            if (Time.realtimeSinceStartup >= failureAt)
            {
                Write("FAILED timeout waiting for three-process acceptance");
                Application.Quit(2);
                return;
            }

            if (session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer
                && serverTransport.ConnectionState == NetworkConnectionState.Listening && !listeningLogged)
            {
                listeningLogged = true;
                Write("SERVER LISTENING");
            }

            MatchSnapshot snapshot = session.CurrentSnapshot;
            if (session.ActiveMode == MultiplayerDevelopmentMode.NetworkClient && snapshot != null
                && snapshot.Phase == MatchPhase.Playing
                && snapshot.TurnState == TurnState.PreparingShot
                && snapshot.CurrentTurnPlayer == session.LocalPlayerId
                && session.CanSubmitShot && snapshot.TurnIndex != lastSubmittedTurn)
            {
                lastSubmittedTurn = snapshot.TurnIndex;
                bool accepted = shotFlow.TryCommitShot(0.48f, 0f);
                Write($"LOCAL NATURAL SHOT SUBMIT accepted={accepted} player={session.LocalPlayerId} turn={snapshot.TurnIndex}");
            }

            if (forceHolePending && Time.realtimeSinceStartup >= forceHoleAt)
            {
                forceHolePending = false;
                bool forced = holeFlow.TryCompleteHole(ball);
                Write($"SERVER FORCE-HOLE AFTER NATURAL A/B accepted={forced}");
            }

            if (quitAt > 0f && Time.realtimeSinceStartup >= quitAt)
            {
                WriteCompletion();
                Application.Quit(0);
            }
        }

        private void OnDestroy()
        {
            if (session != null) session.SnapshotChanged -= OnSnapshot;
            if (clientTransport != null)
            {
                clientTransport.PlayerAssigned -= OnPlayerAssigned;
                clientTransport.Disconnected -= OnClientDisconnected;
                clientTransport.ShotApprovedReceived -= OnShotApproved;
            }
            if (serverTransport != null)
            {
                serverTransport.PlayerDisconnected -= OnServerPlayerDisconnected;
                serverTransport.ShotApprovedReceived -= OnShotApproved;
            }
        }

        private void OnPlayerAssigned(MatchPlayerId player)
        {
            Write($"PLAYER ASSIGNED {player}");
            Capture(player.Value == "player-a" ? "A-Client-A-Assigned.png" : "B-Client-B-Assigned.png");
        }

        private void OnShotApproved(ApprovedShot approved)
        {
            Write($"SHOT APPROVED player={approved.PlayerId} turn={approved.TurnIndex} seq={approved.ShotSequence}");
            if (session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer && approved.TurnIndex >= 2)
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

            if (snapshot.Phase == MatchPhase.Playing && snapshot.TurnState == TurnState.PreparingShot)
            {
                if (snapshot.TurnIndex == 0 && session.LocalPlayerId.Value == "player-a") Capture("C-A-Turn.png");
                if (snapshot.TurnIndex == 1 && session.LocalPlayerId.Value == "player-b") Capture("D-B-Turn.png");
                if (session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer
                    && snapshot.TurnIndex is 1 or 2)
                    Write($"NATURAL SHOT RESOLVED count={snapshot.TurnIndex}");
            }

            if (snapshot.Phase == MatchPhase.HoleComplete && !completeLogged)
            {
                completeLogged = true;
                if (session.ActiveMode == MultiplayerDevelopmentMode.NetworkClient)
                {
                    if (session.LocalPlayerId.Value == "player-a")
                        Capture("E-Same-Match-Complete.png");
                    else
                        Capture("E-Client-B-Match-Complete.png");
                    if (session.LocalPlayerId.Value == "player-b") quitAt = Time.realtimeSinceStartup + 1.5f;
                }
                Write("MATCH COMPLETE");
            }

            if (snapshot.Phase == MatchPhase.Aborted && session.ActiveMode == MultiplayerDevelopmentMode.NetworkClient
                && session.LocalPlayerId.Value == "player-a" && !disconnectCaptured)
            {
                disconnectCaptured = true;
                Capture("F-Disconnect-State.png");
                Write("REMOTE DISCONNECT STATE RECEIVED");
                quitAt = Time.realtimeSinceStartup + 1.5f;
            }
        }

        private void OnServerPlayerDisconnected(MatchPlayerId player)
        {
            Write($"SERVER DETECTED DISCONNECT player={player}");
            if (completeLogged && quitAt < 0f) quitAt = Time.realtimeSinceStartup + 5f;
        }

        private void OnClientDisconnected(string reason)
        {
            Write($"CLIENT DISCONNECTED reason={reason}");
            if (quitAt < 0f) quitAt = Time.realtimeSinceStartup + 0.5f;
        }

        private void WriteCompletion()
        {
            if (session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer)
            {
                Write($"PROCESS COMPLETE tx={serverTransport.SentBytes} rx={serverTransport.ReceivedBytes} " +
                      $"messages={serverTransport.MessageCount} hash={serverTransport.LocalSnapshotHash} " +
                      $"desync={serverTransport.DesyncCount}");
            }
            else
            {
                Write($"PROCESS COMPLETE player={session.LocalPlayerId} tx={clientTransport.SentBytes} " +
                      $"rx={clientTransport.ReceivedBytes} messages={clientTransport.MessageCount} " +
                      $"hash={clientTransport.LocalSnapshotHash} desync={clientTransport.DesyncCount}");
            }
        }

        private string Endpoint()
        {
            return session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer
                ? $"{serverTransport.Address}:{serverTransport.Port}"
                : $"{clientTransport.Address}:{clientTransport.Port}";
        }

        private void Capture(string fileName)
        {
            if (string.IsNullOrWhiteSpace(captureDirectory)
                || session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer) return;
            Directory.CreateDirectory(captureDirectory);
            ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, fileName));
        }

        private void Write(string message)
        {
            string line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
            Debug.Log($"[M14][ProcessProbe] {message}", this);
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

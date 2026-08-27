using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using SwingPop.Debugging;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Online
{
    /// <summary>Development-only acceptance probe for replacing an actual killed client process.</summary>
    [DefaultExecutionOrder(510)]
    public sealed class M15ReconnectProcessProbe : MonoBehaviour
    {
        private MatchSessionController session;
        private UnityTransportMatchTransport client;
        private DedicatedServerMatchTransport server;
        private ShotFlowController shotFlow;
        private string role = string.Empty;
        private string logPath = string.Empty;
        private string captureDirectory = string.Empty;
        private float deadline;
        private bool submitted;
        private bool sawGrace;
        private bool capturedInitial;
        private bool capturedRestored;
        private readonly Queue<string> pendingCaptures = new();
        private readonly HashSet<string> queuedCaptures = new(StringComparer.Ordinal);
        private float nextCaptureAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!HasArg(args, "-swingpopAutomatedReconnectTest")) return;
            new GameObject("M15 Reconnect Process Probe").AddComponent<M15ReconnectProcessProbe>();
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            role = ReadArg(args, "-swingpopM15Role=");
            logPath = ReadArg(args, "-swingpopProbeLog=");
            captureDirectory = ReadArg(args, "-swingpopCaptureDirectory=");
            float duration = ReadFloatArg(args, "-swingpopProbeDuration=", 120f);
            deadline = Time.realtimeSinceStartup + Mathf.Clamp(duration, 10f, 300f);
            session = FindAnyObjectByType<MatchSessionController>();
            shotFlow = FindAnyObjectByType<ShotFlowController>();
            client = session != null ? session.NetworkTransport : null;
            server = session != null ? session.DedicatedServerTransport : null;
            if (session == null || shotFlow == null || client == null || server == null)
            {
                Write("BOOTSTRAP FAILED dependencies missing");
                Application.Quit(2);
                return;
            }

            session.SnapshotChanged += OnSnapshot;
            client.ReconnectAccepted += OnReconnectAccepted;
            client.ReconnectRejected += value => Write($"RECONNECT REJECTED reason={value.Reason}");
            client.LifecycleChanged += OnLifecycle;
            server.LifecycleChanged += OnLifecycle;
            server.PlayerDisconnected += player => Write($"PLAYER DISCONNECTED player={player}");
            FindAnyObjectByType<MultiplayerDebugOverlay>()?.SetVisible(true);
            Write($"BOOT role={role} mode={session.ActiveMode} protocol={OnlineProtocol.CurrentVersion}");
        }

        private void OnDestroy()
        {
            if (session != null) session.SnapshotChanged -= OnSnapshot;
            if (client != null)
            {
                client.ReconnectAccepted -= OnReconnectAccepted;
                client.LifecycleChanged -= OnLifecycle;
            }
            if (server != null) server.LifecycleChanged -= OnLifecycle;
        }

        private void Update()
        {
            FlushCaptureQueue();
            if (Time.realtimeSinceStartup >= deadline)
            {
                Write("PROBE COMPLETE duration reached");
                Application.Quit(0);
                return;
            }
            if (session == null || submitted || !session.CanSubmitShot || session.CurrentSnapshot == null) return;
            MatchSnapshot snapshot = session.CurrentSnapshot;
            bool initialA = role.Equals("A", StringComparison.OrdinalIgnoreCase)
                            && session.LocalPlayerId.Value == "player-a" && snapshot.TurnIndex == 0;
            bool resumedB = role.Equals("B", StringComparison.OrdinalIgnoreCase)
                            && session.LocalPlayerId.Value == "player-b" && sawGrace
                            && snapshot.CurrentTurnPlayer == session.LocalPlayerId;
            if (!initialA && !resumedB) return;
            submitted = true;
            bool accepted = shotFlow.TryCommitShot(initialA ? 0.48f : 0.44f, 0f);
            Write($"NATURAL SHOT SUBMIT accepted={accepted} player={session.LocalPlayerId} turn={snapshot.TurnIndex}");
        }

        private void OnSnapshot(MatchSnapshot snapshot)
        {
            if (snapshot == null) return;
            string hash = MatchSnapshotHash.Compute(snapshot);
            Write($"SNAPSHOT version={snapshot.Version} turn={snapshot.TurnIndex} current={snapshot.CurrentTurnPlayer} " +
                  $"phase={snapshot.Phase} state={snapshot.TurnState} hash={hash}");
            if (!capturedInitial && role.Equals("A", StringComparison.OrdinalIgnoreCase)
                                 && snapshot.Phase == MatchPhase.Playing)
            {
                capturedInitial = true;
                Capture("A-Match-Playing.png");
            }
            for (int index = 0; index < snapshot.PlayerCount; index++)
            {
                PlayerSnapshot player = snapshot.GetPlayer(index);
                if (player.ConnectionState == PlayerConnectionState.ReconnectGrace)
                {
                    sawGrace = true;
                    Capture("B-Player-Disconnected.png");
                    Capture("C-Waiting-For-Reconnect.png");
                }
                if (player.ConnectionState == PlayerConnectionState.Expired)
                    Capture("G-Grace-Expired-Aborted.png");
            }
            if (role.Equals("A-Reconnect", StringComparison.OrdinalIgnoreCase) && client.ReconnectState == ReconnectClientState.Reconnected
                && !capturedRestored)
            {
                capturedRestored = true;
                Capture("E-State-Restored.png");
                Write($"STATE RESTORED player={session.LocalPlayerId} version={snapshot.Version} hash={hash}");
            }
        }

        private void OnReconnectAccepted(ReconnectAcceptedMessage accepted)
        {
            Write($"RECONNECT ACCEPTED player={accepted.PlayerId} generation={accepted.RotatedTicket.SessionGeneration} " +
                  $"snapshot={accepted.SnapshotVersion}");
            Capture("D-Reconnect-Accepted.png");
        }

        private void OnLifecycle(MatchLifecycleChangedMessage message)
        {
            Write($"LIFECYCLE state={message.LifecycleState} player={message.AffectedPlayer} " +
                  $"connection={message.PlayerConnectionState} deadline={message.GraceDeadlineUnixMilliseconds}");
            if (message.LifecycleState == DedicatedMatchLifecycleState.ReconnectGrace) sawGrace = true;
            if (message.LifecycleState == DedicatedMatchLifecycleState.Playing && sawGrace)
                Capture("F-Match-Resumed.png");
            if (message.LifecycleState is DedicatedMatchLifecycleState.Aborted or DedicatedMatchLifecycleState.Ended)
                Capture("G-Grace-Expired-Aborted.png");
        }

        private void Capture(string fileName)
        {
            if (string.IsNullOrWhiteSpace(captureDirectory)
                || session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer) return;
            if (queuedCaptures.Add(fileName)) pendingCaptures.Enqueue(fileName);
        }

        private void FlushCaptureQueue()
        {
            if (pendingCaptures.Count == 0 || Time.realtimeSinceStartup < nextCaptureAt) return;
            string fileName = pendingCaptures.Dequeue();
            Directory.CreateDirectory(captureDirectory);
            ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, fileName));
            nextCaptureAt = Time.realtimeSinceStartup + 1.5f;
        }

        private void Write(string message)
        {
            string line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
            Debug.Log($"[M15][ProcessProbe] {message}", this);
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

        private static float ReadFloatArg(string[] args, string prefix, float fallback)
        {
            string raw = ReadArg(args, prefix);
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value : fallback;
        }
    }
}

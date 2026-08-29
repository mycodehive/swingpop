using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SwingPop.Debugging;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Online
{
    /// <summary>Development-only real-process evidence probe. Authentication credentials are never logged.</summary>
    [DefaultExecutionOrder(520)]
    public sealed class M16AuthenticationProcessProbe : MonoBehaviour
    {
        private readonly Queue<string> captures = new();
        private readonly HashSet<string> queued = new(StringComparer.Ordinal);
        private MatchSessionController session;
        private UnityTransportMatchTransport client;
        private DedicatedServerMatchTransport server;
        private ShotFlowController shotFlow;
        private string role = string.Empty;
        private string logPath = string.Empty;
        private string captureDirectory = string.Empty;
        private float deadline;
        private float nextCaptureAt;
        private bool shotSubmitted;
        private bool sawGrace;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!HasArg(Environment.GetCommandLineArgs(), "-swingpopAutomatedAuthTest")) return;
            new GameObject("M16 Authentication Process Probe").AddComponent<M16AuthenticationProcessProbe>();
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            role = ReadArg(args, "-swingpopM16Role=");
            logPath = ReadArg(args, "-swingpopProbeLog=");
            captureDirectory = ReadArg(args, "-swingpopCaptureDirectory=");
            deadline = Time.realtimeSinceStartup + Mathf.Clamp(ReadFloatArg(args, "-swingpopProbeDuration=", 120f), 10f, 300f);
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
            client.AuthenticationAccepted += OnAuthenticationAccepted;
            client.AuthenticationRejected += OnAuthenticationRejected;
            client.PlayerAssigned += player => Write("MATCH PLAYER ASSIGNED player=" + player);
            client.ReconnectAccepted += OnReconnectAccepted;
            client.ReconnectRejected += value => Write("RECONNECT REJECTED reason=" + value.Reason);
            client.LifecycleChanged += OnLifecycle;
            server.LifecycleChanged += OnLifecycle;
            FindAnyObjectByType<MultiplayerDebugOverlay>()?.SetVisible(true);
            Write($"BOOT role={role} mode={session.ActiveMode} protocol={OnlineProtocol.CurrentVersion}");
        }

        private void OnDestroy()
        {
            if (session != null) session.SnapshotChanged -= OnSnapshot;
            if (client != null)
            {
                client.AuthenticationAccepted -= OnAuthenticationAccepted;
                client.AuthenticationRejected -= OnAuthenticationRejected;
                client.ReconnectAccepted -= OnReconnectAccepted;
                client.LifecycleChanged -= OnLifecycle;
            }
            if (server != null) server.LifecycleChanged -= OnLifecycle;
        }

        private void Update()
        {
            FlushCapture();
            if (Time.realtimeSinceStartup >= deadline)
            {
                Write($"PROCESS COMPLETE auth={client.AuthenticationState} player={session.LocalPlayerId} " +
                      $"tx={client.SentBytes} rx={client.ReceivedBytes} messages={client.MessageCount} desync={client.DesyncCount}");
                Application.Quit(0);
                return;
            }
            if (shotSubmitted || !session.CanSubmitShot || session.CurrentSnapshot == null) return;
            MatchSnapshot snapshot = session.CurrentSnapshot;
            bool aTurn = role.Equals("A", StringComparison.OrdinalIgnoreCase)
                         && session.LocalPlayerId.Value == "player-a" && snapshot.TurnIndex == 0;
            bool bTurn = role.Equals("B", StringComparison.OrdinalIgnoreCase)
                         && session.LocalPlayerId.Value == "player-b" && snapshot.TurnIndex >= 1;
            if (!aTurn && !bTurn) return;
            shotSubmitted = true;
            bool accepted = shotFlow.TryCommitShot(aTurn ? 0.48f : 0.44f, 0f);
            Write($"NATURAL SHOT SUBMIT accepted={accepted} player={session.LocalPlayerId} turn={snapshot.TurnIndex}");
        }

        private void OnAuthenticationAccepted(AuthAcceptedMessage accepted)
        {
            Write($"AUTH ACCEPTED account={DevelopmentAuthenticationProvider.Fingerprint(accepted.PlayerAccountId.Value)} " +
                  $"session={DevelopmentAuthenticationProvider.Fingerprint(accepted.AuthSessionId.Value)} " +
                  $"expiry={accepted.SessionExpiryUnixMilliseconds}");
            if (role.Equals("A", StringComparison.OrdinalIgnoreCase)) Capture("A-Client-A-Authenticated.png");
            if (role.Equals("B", StringComparison.OrdinalIgnoreCase)) Capture("B-Client-B-Authenticated.png");
        }

        private void OnAuthenticationRejected(AuthRejectedMessage rejected)
        {
            Write("AUTH REJECTED reason=" + rejected.Reason);
            Capture("D-Auth-Failure.png");
        }

        private void OnReconnectAccepted(ReconnectAcceptedMessage accepted)
        {
            Write($"RECONNECT ACCEPTED player={accepted.PlayerId} generation={accepted.RotatedTicket.SessionGeneration}");
            Capture("F-Reconnect-Same-Player.png");
        }

        private void OnLifecycle(MatchLifecycleChangedMessage message)
        {
            Write($"LIFECYCLE state={message.LifecycleState} player={message.AffectedPlayer} connection={message.PlayerConnectionState}");
            if (message.LifecycleState == DedicatedMatchLifecycleState.ReconnectGrace)
            {
                sawGrace = true;
                Capture("E-Disconnect-Reauth.png");
            }
        }

        private void OnSnapshot(MatchSnapshot snapshot)
        {
            if (snapshot == null) return;
            Write($"SNAPSHOT version={snapshot.Version} turn={snapshot.TurnIndex} current={snapshot.CurrentTurnPlayer} " +
                  $"phase={snapshot.Phase} state={snapshot.TurnState} hash={MatchSnapshotHash.Compute(snapshot)}");
            if (role.Equals("A", StringComparison.OrdinalIgnoreCase) && snapshot.Phase == MatchPhase.Playing)
                Capture("C-Match-Started.png");
            if (snapshot.Phase == MatchPhase.Playing && snapshot.TurnIndex >= 1)
                Capture("G-Match-Gameplay.png");
            if (sawGrace && role.Equals("A-Reconnect", StringComparison.OrdinalIgnoreCase))
                Capture("F-Reconnect-Same-Player.png");
        }

        private void Capture(string fileName)
        {
            if (string.IsNullOrWhiteSpace(captureDirectory)
                || session.ActiveMode == MultiplayerDevelopmentMode.DedicatedServer || !queued.Add(fileName)) return;
            captures.Enqueue(fileName);
        }

        private void FlushCapture()
        {
            if (captures.Count == 0 || Time.realtimeSinceStartup < nextCaptureAt) return;
            Directory.CreateDirectory(captureDirectory);
            string path = Path.Combine(captureDirectory, captures.Dequeue());
            Camera captureCamera = Camera.main;
            if (captureCamera == null) captureCamera = FindAnyObjectByType<Camera>();
            if (captureCamera == null)
            {
                Write("CAPTURE FAILED camera missing");
                return;
            }
            RenderTexture target = RenderTexture.GetTemporary(960, 540, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = captureCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new(960, 540, TextureFormat.RGB24, false);
            try
            {
                captureCamera.targetTexture = target;
                captureCamera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 960f, 540f), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Write("CAPTURED " + Path.GetFileName(path));
            }
            finally
            {
                captureCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                Destroy(image);
            }
            nextCaptureAt = Time.realtimeSinceStartup + 1.5f;
        }

        private void Write(string message)
        {
            Debug.Log("[M16][ProcessProbe] " + message, this);
            if (string.IsNullOrWhiteSpace(logPath)) return;
            string directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.AppendAllText(logPath, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
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
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
        }
    }
}

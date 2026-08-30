using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using SwingPop.Debugging;
using SwingPop.Gameplay.Shot;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Online
{
    /// <summary>Development-only four-process acceptance driver. It never bypasses Lobby or Match admission.</summary>
    public sealed class M17LobbyProcessProbe : MonoBehaviour
    {
        private string role = string.Empty;
        private string logPath = string.Empty;
        private string captureDirectory = string.Empty;
        private float deadline;
        private float nextActionAt;
        private float completionAt;
        private int lobbyStep;
        private bool shotSubmitted;
        private bool snapshotSubscribed;
        private MatchSessionController matchSession;
        private ShotFlowController shotFlow;
        private LobbyDevelopmentController lobby;
        private MatchSnapshot latestSnapshot;
        private readonly HashSet<string> captured = new(StringComparer.OrdinalIgnoreCase);
        private bool m18RelayReconnect;
        private bool reconnectRequested;
        private bool reconnectAccepted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!HasArg(Environment.GetCommandLineArgs(), "-swingpopAutomatedLobbyTest")) return;
            if (FindAnyObjectByType<M17LobbyProcessProbe>() != null) return;
            GameObject root = new("M17 Lobby Process Probe");
            DontDestroyOnLoad(root);
            root.AddComponent<M17LobbyProcessProbe>();
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            role = ReadArg(args, "-swingpopM17Role=");
            logPath = ReadArg(args, "-swingpopProbeLog=");
            captureDirectory = ReadArg(args, "-swingpopCaptureDirectory=");
            m18RelayReconnect = HasArg(args, "-swingpopM18RelayReconnect");
            deadline = Time.realtimeSinceStartup + Mathf.Clamp(
                ReadFloatArg(args, "-swingpopProbeDuration=", 150f), 20f, 300f);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Write($"BOOT role={role} scene={SceneManager.GetActiveScene().name} lobbyProtocol={LobbyProtocol.CurrentVersion} " +
                  $"gameplayProtocol={OnlineProtocol.CurrentVersion}");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (matchSession != null && snapshotSubscribed)
            {
                matchSession.SnapshotChanged -= OnSnapshot;
                if (matchSession.NetworkTransport != null)
                    matchSession.NetworkTransport.ReconnectAccepted -= OnReconnectAccepted;
            }
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                bool success = role.Equals("Service", StringComparison.OrdinalIgnoreCase)
                               || latestSnapshot != null && latestSnapshot.TurnIndex >= 2;
                Write($"PROCESS COMPLETE success={success} scene={SceneManager.GetActiveScene().name}");
                Application.Quit(success ? 0 : 2);
                return;
            }

            if (SceneManager.GetActiveScene().name == "Lobby_Development") TickLobby();
            else TickMatch();

            if (completionAt > 0f && Time.realtimeSinceStartup >= completionAt)
            {
                Write($"PROCESS COMPLETE success=True player={matchSession?.LocalPlayerId} " +
                      $"turn={latestSnapshot?.TurnIndex ?? -1} hash={MatchSnapshotHash.Compute(latestSnapshot)}");
                Application.Quit(0);
            }
        }

        private void TickLobby()
        {
            lobby ??= FindAnyObjectByType<LobbyDevelopmentController>();
            if (lobby == null || role.Equals("Service", StringComparison.OrdinalIgnoreCase)) return;
            if (!lobby.IsAuthenticated || lobby.RequestPending || Time.realtimeSinceStartup < nextActionAt) return;

            if (role.Equals("A", StringComparison.OrdinalIgnoreCase)) TickLobbyA();
            else if (role.Equals("B", StringComparison.OrdinalIgnoreCase)) TickLobbyB();
        }

        private void TickLobbyA()
        {
            if (lobbyStep == 0)
            {
                Capture("A-Lobby-Empty.png");
                lobby.CreateRoom("SwingPop Room 01");
                Write("CREATE REQUEST");
                lobbyStep = 1;
                nextActionAt = Time.realtimeSinceStartup + 0.5f;
                return;
            }
            LobbyMatchSnapshot room = lobby.CurrentMatch;
            if (lobbyStep == 1 && room != null && room.CurrentPlayers == 1)
            {
                Capture("B-Room-Created.png");
                Write($"ROOM CREATED id={room.LobbyMatchId} revision={room.Revision}");
                lobbyStep = 2;
            }
            if (lobbyStep == 2 && room != null && room.CurrentPlayers == 2)
            {
                Capture("D-Player-B-Joined.png");
                lobby.ToggleReady();
                Write("READY A REQUEST");
                lobbyStep = 3;
                nextActionAt = Time.realtimeSinceStartup + 0.5f;
                return;
            }
            if (lobbyStep == 3 && room != null && AllReady(room))
            {
                Capture("E-Both-Ready.png");
                lobby.StartRoomMatch();
                Write($"START REQUEST room={room.LobbyMatchId} revision={room.Revision}");
                Capture("F-Match-Starting.png");
                lobbyStep = 4;
            }
        }

        private void TickLobbyB()
        {
            if (lobbyStep == 0)
            {
                lobby.RefreshRooms();
                lobbyStep = 1;
                nextActionAt = Time.realtimeSinceStartup + 0.5f;
                return;
            }
            if (!lobby.IsInRoom && lobby.MatchList.Length > 0)
            {
                Capture("C-Room-List.png");
                LobbyMatchSnapshot room = lobby.MatchList[0];
                lobby.JoinRoom(room.LobbyMatchId);
                Write($"JOIN REQUEST room={room.LobbyMatchId}");
                lobbyStep = 2;
                nextActionAt = Time.realtimeSinceStartup + 0.5f;
                return;
            }
            if (!lobby.IsInRoom && Time.realtimeSinceStartup >= nextActionAt)
            {
                lobby.RefreshRooms();
                nextActionAt = Time.realtimeSinceStartup + 0.75f;
                return;
            }
            if (lobbyStep == 2 && lobby.IsInRoom && lobby.CurrentMatch.CurrentPlayers == 2)
            {
                lobby.ToggleReady();
                Write("READY B REQUEST");
                lobbyStep = 3;
            }
        }

        private void TickMatch()
        {
            if (matchSession == null)
            {
                matchSession = FindAnyObjectByType<MatchSessionController>();
                shotFlow = FindAnyObjectByType<ShotFlowController>();
                if (matchSession != null && !snapshotSubscribed)
                {
                    matchSession.SnapshotChanged += OnSnapshot;
                    snapshotSubscribed = true;
                    if (matchSession.NetworkTransport != null)
                        matchSession.NetworkTransport.ReconnectAccepted += OnReconnectAccepted;
                    FindAnyObjectByType<MultiplayerDebugOverlay>()?.SetVisible(true);
                    Write("MATCH SCENE LOADED connectivity=" +
                          matchSession.NetworkTransport?.ConnectivityLabel + " state=" +
                          matchSession.NetworkTransport?.ConnectivityState);
                }
            }
            if (m18RelayReconnect && role.Equals("A", StringComparison.OrdinalIgnoreCase)
                && latestSnapshot != null && latestSnapshot.TurnIndex >= 1 && !reconnectRequested)
            {
                reconnectRequested = true;
                bool requested = matchSession.NetworkTransport != null
                                 && matchSession.NetworkTransport.SimulateUnexpectedDisconnectForTesting();
                Write("RELAY RECONNECT REQUESTED accepted=" + requested);
                Capture("G-Reconnect.png");
                return;
            }
            if (matchSession == null || shotFlow == null || latestSnapshot == null || shotSubmitted
                || !matchSession.CanSubmitShot) return;
            bool aTurn = role.Equals("A", StringComparison.OrdinalIgnoreCase)
                         && matchSession.LocalPlayerId.Value == "player-a" && latestSnapshot.TurnIndex == 0;
            bool bTurn = role.Equals("B", StringComparison.OrdinalIgnoreCase)
                         && matchSession.LocalPlayerId.Value == "player-b" && latestSnapshot.TurnIndex >= 1;
            if (!aTurn && !bTurn) return;
            shotSubmitted = true;
            bool accepted = shotFlow.TryCommitShot(aTurn ? 0.48f : 0.44f, 0f);
            Write($"NATURAL SHOT SUBMIT accepted={accepted} player={matchSession.LocalPlayerId} turn={latestSnapshot.TurnIndex}");
        }

        private void OnSnapshot(MatchSnapshot snapshot)
        {
            if (snapshot == null) return;
            latestSnapshot = snapshot;
            Write($"SNAPSHOT game={snapshot.MatchId} version={snapshot.Version} turn={snapshot.TurnIndex} " +
                  $"player={snapshot.CurrentTurnPlayer} phase={snapshot.Phase} hash={MatchSnapshotHash.Compute(snapshot)}");
            if (role.Equals("A", StringComparison.OrdinalIgnoreCase) && snapshot.TurnIndex == 0)
                Capture("G-Connected-to-Hole01.png");
            if (role.Equals("A", StringComparison.OrdinalIgnoreCase) && snapshot.TurnIndex >= 1)
                Capture("H-Match-Gameplay.png");
            if (snapshot.TurnIndex >= 2 && completionAt <= 0f
                && (!m18RelayReconnect || !role.Equals("A", StringComparison.OrdinalIgnoreCase) || reconnectAccepted))
                completionAt = Time.realtimeSinceStartup + 3f;
        }

        private void OnReconnectAccepted(ReconnectAcceptedMessage message)
        {
            reconnectAccepted = true;
            Write($"RELAY RECONNECT ACCEPTED player={message.PlayerId} generation={message.RotatedTicket.SessionGeneration} " +
                  $"connectivity={matchSession?.NetworkTransport?.ConnectivityState}");
            Capture("G-Reconnect.png");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Write($"SCENE LOADED name={scene.name} mode={mode}");
            lobby = null;
            matchSession = null;
            shotFlow = null;
        }

        private void Capture(string fileName)
        {
            if (string.IsNullOrWhiteSpace(captureDirectory) || !captured.Add(fileName)) return;
            Directory.CreateDirectory(captureDirectory);
            string path = Path.Combine(captureDirectory, fileName);
            FindAnyObjectByType<LobbyDevelopmentView>()?.RefreshCaptureTelemetry();
            Texture2D image = CaptureCamera();
            if (image == null) { Write("CAPTURE FAILED " + Path.GetFileName(path)); return; }
            try
            {
                File.WriteAllBytes(path, image.EncodeToPNG());
                Write("CAPTURED " + Path.GetFileName(path));
            }
            finally
            {
                Destroy(image);
            }
        }

        private static Texture2D CaptureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) camera = FindAnyObjectByType<Camera>();
            if (camera == null) return null;
            RenderTexture target = RenderTexture.GetTemporary(960, 540, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new(960, 540, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 960f, 540f), 0, 0);
                image.Apply(false, false);
                return image;
            }
            catch (Exception)
            {
                Destroy(image);
                return null;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private void Write(string message)
        {
            Debug.Log("[M17][ProcessProbe] " + message, this);
            if (string.IsNullOrWhiteSpace(logPath)) return;
            string directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.AppendAllText(logPath, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
        }

        private static bool AllReady(LobbyMatchSnapshot room)
        {
            if (room == null || room.Members.Length != LobbyProtocol.MatchPlayerCapacity) return false;
            foreach (LobbyMatchMember member in room.Members)
                if (member.ReadyState != LobbyReadyState.Ready) return false;
            return true;
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
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed : fallback;
        }
    }
}

using System;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Online
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class MatchSessionController : MonoBehaviour, IShotCommitGate
    {
        private static readonly MatchPlayerId PlayerA = new("player-a");
        private static readonly MatchPlayerId PlayerB = new("player-b");

        [Header("Development")]
        [SerializeField] private MultiplayerDevelopmentSettings settings;

        [Header("Foundation")]
        [SerializeField] private LocalMatchAuthority authority;
        [SerializeField] private LocalLoopbackTransport transport;
        [SerializeField] private UnityTransportMatchTransport networkTransport;

        [Header("Existing Gameplay")]
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private GolfBallController ball;
        [SerializeField] private HoleFlowController holeFlow;
        [SerializeField] private TerrainSurfaceData[] surfaces;

        private readonly MatchSnapshotStore snapshotStore = new();
        private MultiplayerDevelopmentMode activeMode;
        private MatchPlayerId localPlayerId;
        private ApprovedShot activeApprovedShot;
        private bool hasActiveApprovedShot;
        private bool localSubmissionPending;
        private bool matchStarted;
        private float remoteTurnElapsed;
        private bool remoteSubmissionPending;

        public event Action<ShotCommand> ShotApproved;
        public event Action ShotRejected;
        public event Action<MatchSnapshot> SnapshotChanged;
        public event Action<ShotRejection> SubmissionRejected;

        public bool RequiresApproval => activeMode != MultiplayerDevelopmentMode.OfflineSingle;
        public bool CanSubmitShot => RequiresApproval && IsActiveTransportReady && !localSubmissionPending
                                     && snapshotStore.Current != null
                                     && snapshotStore.Current.Phase == MatchPhase.Playing
                                     && snapshotStore.Current.TurnState == TurnState.PreparingShot
                                     && snapshotStore.Current.CurrentTurnPlayer == localPlayerId;
        public bool CanResetShot => activeMode is not MultiplayerDevelopmentMode.NetworkHost
            and not MultiplayerDevelopmentMode.NetworkClient;
        public MultiplayerDevelopmentMode ActiveMode => activeMode;
        public MatchPlayerId LocalPlayerId => localPlayerId;
        public MatchSnapshot CurrentSnapshot => snapshotStore.Current;
        public LocalLoopbackTransport Transport => transport;
        public UnityTransportMatchTransport NetworkTransport => networkTransport;
        public LocalMatchAuthority Authority => authority;
        public bool IsConfigured => settings != null && authority != null && transport != null && networkTransport != null
                                    && shotFlow != null && ball != null && holeFlow != null
                                    && surfaces != null && surfaces.Length > 0;

        private void OnEnable()
        {
            if (transport != null)
            {
                transport.ShotApprovedReceived += OnShotApprovedReceived;
                transport.ShotRejectedReceived += OnShotRejectedReceived;
                transport.SnapshotReceived += OnSnapshotReceived;
            }
            if (networkTransport != null)
            {
                networkTransport.ShotApprovedReceived += OnShotApprovedReceived;
                networkTransport.ShotRejectedReceived += OnShotRejectedReceived;
                networkTransport.SnapshotReceived += OnSnapshotReceived;
                networkTransport.PlayerAssigned += OnPlayerAssigned;
                networkTransport.RemotePlayerReady += OnRemotePlayerReady;
                networkTransport.Disconnected += OnNetworkDisconnected;
            }
            if (holeFlow != null)
            {
                holeFlow.ShotResolved += OnGameplayShotResolved;
                holeFlow.HoleCompleted += OnGameplayHoleCompleted;
            }
        }

        private void Start()
        {
            if (matchStarted) return;
            MultiplayerDevelopmentMode mode = settings != null
                ? settings.Mode
                : MultiplayerDevelopmentMode.OfflineSingle;
            NetworkLaunchOptions launch = NetworkLaunchOptions.Parse(
                Environment.GetCommandLineArgs(),
                settings != null ? settings.HostAddress : "127.0.0.1",
                settings != null ? settings.Port : (ushort)7777);
            if (launch.HasNetworkOverride)
            {
                StartNetworkMatch(launch.Mode, launch.Address, launch.Port);
                return;
            }
            int latency = settings != null ? settings.SimulatedLatencyMs : 0;
            StartDevelopmentMatch(mode, latency);
        }

        private void Update()
        {
            if (activeMode != MultiplayerDevelopmentMode.LocalTwoPlayer || snapshotStore.Current == null)
                return;

            MatchSnapshot snapshot = snapshotStore.Current;
            bool remoteTurn = snapshot.Phase == MatchPhase.Playing
                              && snapshot.TurnState == TurnState.PreparingShot
                              && snapshot.CurrentTurnPlayer != localPlayerId;
            if (!remoteTurn)
            {
                remoteTurnElapsed = 0f;
                remoteSubmissionPending = false;
                return;
            }

            remoteTurnElapsed += Time.unscaledDeltaTime;
            float delay = settings != null ? settings.SimulatedRemoteShotDelay : 1.2f;
            if (!remoteSubmissionPending && remoteTurnElapsed >= delay)
                SubmitSimulatedRemoteShotNow();
        }

        private void OnDisable()
        {
            shotFlow?.ConfigureCommitGate(null);
            holeFlow?.SetAutomaticFlowSuspended(false);
            if (transport != null)
            {
                transport.ShotApprovedReceived -= OnShotApprovedReceived;
                transport.ShotRejectedReceived -= OnShotRejectedReceived;
                transport.SnapshotReceived -= OnSnapshotReceived;
                transport.CancelPending();
            }
            if (networkTransport != null)
            {
                networkTransport.ShotApprovedReceived -= OnShotApprovedReceived;
                networkTransport.ShotRejectedReceived -= OnShotRejectedReceived;
                networkTransport.SnapshotReceived -= OnSnapshotReceived;
                networkTransport.PlayerAssigned -= OnPlayerAssigned;
                networkTransport.RemotePlayerReady -= OnRemotePlayerReady;
                networkTransport.Disconnected -= OnNetworkDisconnected;
                networkTransport.CancelPending();
            }
            if (holeFlow != null)
            {
                holeFlow.ShotResolved -= OnGameplayShotResolved;
                holeFlow.HoleCompleted -= OnGameplayHoleCompleted;
            }
        }

        public void StartDevelopmentMatch(MultiplayerDevelopmentMode mode, int latencyMs)
        {
            if (!IsConfigured)
            {
                Debug.LogError("[M12][Match] MatchSessionController dependencies are incomplete.", this);
                return;
            }

            matchStarted = true;
            activeMode = mode;
            localPlayerId = PlayerA;
            hasActiveApprovedShot = false;
            localSubmissionPending = false;
            remoteSubmissionPending = false;
            remoteTurnElapsed = 0f;
            snapshotStore.Reset();
            networkTransport.CancelPending();
            transport.CancelPending();
            transport.Configure(authority, latencyMs, settings != null && settings.VerboseLogging);
            shotFlow.ConfigureCommitGate(this);
            holeFlow.SetAutomaticFlowSuspended(mode == MultiplayerDevelopmentMode.LocalTwoPlayer);

            if (mode is MultiplayerDevelopmentMode.NetworkHost or MultiplayerDevelopmentMode.NetworkClient)
            {
                string networkAddress = settings != null ? settings.HostAddress : "127.0.0.1";
                ushort networkPort = settings != null ? settings.Port : (ushort)7777;
                StartNetworkMatch(mode, networkAddress, networkPort);
                return;
            }

            Vector3 tee = holeFlow.Hole.TeePosition;
            PlayerSnapshot[] players = mode == MultiplayerDevelopmentMode.LocalTwoPlayer
                ? new[] { CreateInitialPlayer(PlayerA, "PLAYER A", 0, true, tee), CreateInitialPlayer(PlayerB, "PLAYER B", 1, false, tee) }
                : new[] { CreateInitialPlayer(PlayerA, "PLAYER", 0, true, tee) };
            MatchSnapshot initial = authority.StartMatch(new MatchId("local-hole01-match"), "hole-01", players);
            transport.PublishSnapshot(initial);
        }

        public void StartNetworkMatch(MultiplayerDevelopmentMode mode, string address, ushort port)
        {
            if (mode is not MultiplayerDevelopmentMode.NetworkHost and not MultiplayerDevelopmentMode.NetworkClient)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Network mode is required.");
            if (!IsConfigured)
            {
                Debug.LogError("[M13][Match] MatchSessionController dependencies are incomplete.", this);
                return;
            }

            matchStarted = true;
            activeMode = mode;
            localPlayerId = mode == MultiplayerDevelopmentMode.NetworkHost ? PlayerA : default;
            hasActiveApprovedShot = false;
            localSubmissionPending = false;
            remoteSubmissionPending = false;
            remoteTurnElapsed = 0f;
            snapshotStore.Reset();
            transport.CancelPending();
            networkTransport.CancelPending();
            networkTransport.Configure(mode == MultiplayerDevelopmentMode.NetworkHost ? authority : null,
                settings != null && settings.VerboseLogging);
            shotFlow.ConfigureCommitGate(this);
            holeFlow.SetAutomaticFlowSuspended(true);
            float timeout = settings != null ? settings.ConnectionTimeoutSeconds : 8f;
            bool started = mode == MultiplayerDevelopmentMode.NetworkHost
                ? networkTransport.StartHost(address, port, timeout)
                : networkTransport.StartClient(address, port, timeout);
            if (!started)
                Debug.LogError($"[M13][Match] Could not start {mode} at {address}:{port}.", this);
        }

        public bool TrySubmitShot(ShotCommand command)
        {
            if (!CanSubmitShot) return false;
            MatchSnapshot snapshot = snapshotStore.Current;
            localSubmissionPending = true;
            ShotSubmission submission = new(snapshot.MatchId, localPlayerId, snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, command);
            if (ActiveTransport.SubmitShot(submission)) return true;
            localSubmissionPending = false;
            return false;
        }

        public bool SubmitSimulatedRemoteShotNow()
        {
            MatchSnapshot snapshot = snapshotStore.Current;
            if (activeMode != MultiplayerDevelopmentMode.LocalTwoPlayer || snapshot == null
                || snapshot.Phase != MatchPhase.Playing || snapshot.TurnState != TurnState.PreparingShot
                || snapshot.CurrentTurnPlayer == localPlayerId || remoteSubmissionPending)
                return false;

            float power = settings != null ? settings.SimulatedRemotePower : 0.62f;
            if (!shotFlow.TryCreateShotCommand(power, 0f, out ShotCommand command)) return false;

            remoteSubmissionPending = true;
            ShotSubmission submission = new(snapshot.MatchId, snapshot.CurrentTurnPlayer, snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, command);
            if (transport.SubmitShot(submission)) return true;
            remoteSubmissionPending = false;
            return false;
        }

        public bool ApplyAuthoritativeShotResult(NetworkShotResult result)
        {
            return ActiveTransport != null && ActiveTransport.SubmitShotResult(result);
        }

        private void OnShotApprovedReceived(ApprovedShot approved)
        {
            MatchSnapshot snapshot = snapshotStore.Current;
            if (snapshot == null || approved.MatchId != snapshot.MatchId) return;

            activeApprovedShot = approved;
            hasActiveApprovedShot = true;
            if (approved.PlayerId == localPlayerId)
            {
                localSubmissionPending = false;
                ShotApproved?.Invoke(approved.Command);
            }
            else
            {
                remoteSubmissionPending = false;
                if (!shotFlow.TryExecuteApprovedShot(approved.Command))
                {
                    Debug.LogError("[M12][Shot] Approved remote shot could not enter the existing ShotFlow.", this);
                }
            }
        }

        private void OnShotRejectedReceived(ShotRejection rejection)
        {
            if (rejection.PlayerId == localPlayerId)
            {
                localSubmissionPending = false;
                ShotRejected?.Invoke();
            }
            else
            {
                remoteSubmissionPending = false;
            }
            SubmissionRejected?.Invoke(rejection);
        }

        private void OnGameplayShotResolved(HoleShotResolution resolution)
        {
            if (!RequiresApproval || !hasActiveApprovedShot) return;
            hasActiveApprovedShot = false;
            NetworkShotResult result = new(
                activeApprovedShot.MatchId,
                activeApprovedShot.PlayerId,
                activeApprovedShot.TurnIndex,
                activeApprovedShot.ShotSequence,
                NetworkVector3.FromUnity(resolution.BallPosition),
                NetworkVector3.FromUnity(resolution.LastValidPosition),
                resolution.Lie,
                resolution.StrokeCount,
                resolution.PenaltyCount,
                false,
                false);
            ActiveTransport.SubmitShotResult(result);
        }

        private void OnGameplayHoleCompleted(ScoreResult score)
        {
            if (!RequiresApproval || !hasActiveApprovedShot) return;
            hasActiveApprovedShot = false;
            NetworkShotResult result = new(
                activeApprovedShot.MatchId,
                activeApprovedShot.PlayerId,
                activeApprovedShot.TurnIndex,
                activeApprovedShot.ShotSequence,
                NetworkVector3.FromUnity(ball.PhysicsPosition),
                NetworkVector3.FromUnity(holeFlow.LastValidPosition),
                TerrainSurfaceType.Green,
                holeFlow.StrokeCount,
                holeFlow.PenaltyCount,
                true,
                true,
                score.RelativeToPar,
                score.Label);
            ActiveTransport.SubmitShotResult(result);
        }

        private void OnSnapshotReceived(MatchSnapshot snapshot)
        {
            if (!snapshotStore.TryApply(snapshot)) return;
            SnapshotChanged?.Invoke(snapshot);
            if (RequiresApproval
                && snapshot.Phase == MatchPhase.Playing
                && snapshot.TurnState == TurnState.PreparingShot)
            {
                RestoreCurrentPlayer(snapshot);
            }
        }

        private IMatchTransport ActiveTransport => activeMode is MultiplayerDevelopmentMode.NetworkHost
            or MultiplayerDevelopmentMode.NetworkClient ? networkTransport : transport;
        private bool IsActiveTransportReady => activeMode is MultiplayerDevelopmentMode.NetworkHost
            or MultiplayerDevelopmentMode.NetworkClient ? networkTransport != null && networkTransport.IsReady : true;

        private void OnPlayerAssigned(MatchPlayerId playerId)
        {
            localPlayerId = playerId;
        }

        private void OnRemotePlayerReady()
        {
            if (activeMode != MultiplayerDevelopmentMode.NetworkHost || authority == null || holeFlow == null) return;
            Vector3 tee = holeFlow.Hole.TeePosition;
            PlayerSnapshot[] players =
            {
                CreateInitialPlayer(PlayerA, "PLAYER A", 0, true, tee),
                CreateInitialPlayer(PlayerB, "PLAYER B", 1, false, tee)
            };
            MatchId matchId = new($"host-hole01-{DateTime.UtcNow:yyyyMMddHHmmss}");
            MatchSnapshot initial = authority.StartMatch(matchId, "hole-01", players);
            networkTransport.BeginHostedMatch(initial);
        }

        private void OnNetworkDisconnected(string reason)
        {
            bool rejectedPendingLocalShot = localSubmissionPending;
            localSubmissionPending = false;
            remoteSubmissionPending = false;
            if (rejectedPendingLocalShot) ShotRejected?.Invoke();
            Debug.LogWarning($"[M13][Match] Network disconnected: {reason}", this);
        }

        private void RestoreCurrentPlayer(MatchSnapshot snapshot)
        {
            if (!snapshot.TryGetPlayer(snapshot.CurrentTurnPlayer, out PlayerSnapshot player)) return;
            TerrainSurfaceData surface = FindSurface(player.Lie);
            if (surface == null)
            {
                Debug.LogError($"[M12][Snapshot] No TerrainSurfaceData for {player.Lie}.", this);
                return;
            }

            holeFlow.RestoreMultiplayerPlayer(
                player.BallPosition.ToUnity(),
                player.LastValidPosition.ToUnity(),
                surface,
                player.StrokeCount,
                player.PenaltyCount);
        }

        private TerrainSurfaceData FindSurface(TerrainSurfaceType type)
        {
            if (surfaces == null) return null;
            foreach (TerrainSurfaceData surface in surfaces)
                if (surface != null && surface.SurfaceType == type) return surface;
            return null;
        }

        private static PlayerSnapshot CreateInitialPlayer(
            MatchPlayerId id, string displayName, int slot, bool local, Vector3 tee)
        {
            NetworkVector3 position = NetworkVector3.FromUnity(tee);
            return new PlayerSnapshot(id, displayName, slot, slot, local,
                PlayerConnectionState.Connected, 0, 0, position, position, TerrainSurfaceType.Tee, false);
        }
    }
}

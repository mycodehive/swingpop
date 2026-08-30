using System;
using System.Threading.Tasks;
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
        [SerializeField] private DedicatedServerMatchTransport dedicatedServerTransport;
        [SerializeField] private ReconnectController reconnectController;
        [SerializeField] private AuthenticationController authenticationController;

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
        private bool matchSuspended;
        private MatchLifecycleChangedMessage latestLifecycle;

        public event Action<ShotCommand> ShotApproved;
        public event Action ShotRejected;
        public event Action<MatchSnapshot> SnapshotChanged;
        public event Action<ShotRejection> SubmissionRejected;

        public bool RequiresApproval => activeMode != MultiplayerDevelopmentMode.OfflineSingle;
        public bool CanSubmitShot => RequiresApproval && IsActiveTransportReady && !localSubmissionPending
                                     && !matchSuspended
                                     && snapshotStore.Current != null
                                     && snapshotStore.Current.Phase == MatchPhase.Playing
                                     && snapshotStore.Current.TurnState == TurnState.PreparingShot
                                     && snapshotStore.Current.CurrentTurnPlayer == localPlayerId;
        public bool CanResetShot => activeMode is not MultiplayerDevelopmentMode.NetworkHost
            and not MultiplayerDevelopmentMode.NetworkClient
            and not MultiplayerDevelopmentMode.DedicatedServer;
        public MultiplayerDevelopmentMode ActiveMode => activeMode;
        public MatchPlayerId LocalPlayerId => localPlayerId;
        public MatchSnapshot CurrentSnapshot => snapshotStore.Current;
        public LocalLoopbackTransport Transport => transport;
        public UnityTransportMatchTransport NetworkTransport => networkTransport;
        public DedicatedServerMatchTransport DedicatedServerTransport => dedicatedServerTransport;
        public LocalMatchAuthority Authority => authority;
        public ReconnectController ReconnectController => reconnectController;
        public AuthenticationController AuthenticationController => authenticationController;
        public bool IsMatchSuspended => matchSuspended;
        public MatchLifecycleChangedMessage LatestLifecycle => latestLifecycle;
        public bool IsConfigured => settings != null && authority != null && transport != null && networkTransport != null
                                    && dedicatedServerTransport != null && reconnectController != null
                                    && authenticationController != null
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
                networkTransport.LifecycleChanged += OnLifecycleChanged;
            }
            if (dedicatedServerTransport != null)
            {
                dedicatedServerTransport.ShotApprovedReceived += OnShotApprovedReceived;
                dedicatedServerTransport.ShotRejectedReceived += OnShotRejectedReceived;
                dedicatedServerTransport.SnapshotReceived += OnSnapshotReceived;
                dedicatedServerTransport.AllPlayersReady += OnAllDedicatedPlayersReady;
                dedicatedServerTransport.PlayerDisconnected += OnDedicatedPlayerDisconnected;
                dedicatedServerTransport.LifecycleChanged += OnLifecycleChanged;
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
            if (MatchAdmissionHandoff.TryPeek(out MatchAdmissionGrant pendingAdmission))
                launch = new NetworkLaunchOptions(MultiplayerDevelopmentMode.NetworkClient,
                    pendingAdmission.ServerAddress, pendingAdmission.ServerPort);
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
                networkTransport.LifecycleChanged -= OnLifecycleChanged;
                networkTransport.CancelPending();
            }
            if (dedicatedServerTransport != null)
            {
                dedicatedServerTransport.ShotApprovedReceived -= OnShotApprovedReceived;
                dedicatedServerTransport.ShotRejectedReceived -= OnShotRejectedReceived;
                dedicatedServerTransport.SnapshotReceived -= OnSnapshotReceived;
                dedicatedServerTransport.AllPlayersReady -= OnAllDedicatedPlayersReady;
                dedicatedServerTransport.PlayerDisconnected -= OnDedicatedPlayerDisconnected;
                dedicatedServerTransport.LifecycleChanged -= OnLifecycleChanged;
                dedicatedServerTransport.CancelPending();
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
            matchSuspended = false;
            latestLifecycle = default;
            networkTransport.CancelPending();
            dedicatedServerTransport.CancelPending();
            transport.CancelPending();
            transport.Configure(authority, latencyMs, settings != null && settings.VerboseLogging);
            shotFlow.ConfigureCommitGate(this);
            holeFlow.SetAutomaticFlowSuspended(mode == MultiplayerDevelopmentMode.LocalTwoPlayer);

            if (mode is MultiplayerDevelopmentMode.NetworkHost or MultiplayerDevelopmentMode.NetworkClient
                or MultiplayerDevelopmentMode.DedicatedServer)
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

        public async void StartNetworkMatch(MultiplayerDevelopmentMode mode, string address, ushort port)
        {
            if (mode is not MultiplayerDevelopmentMode.NetworkHost and not MultiplayerDevelopmentMode.NetworkClient
                and not MultiplayerDevelopmentMode.DedicatedServer)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Network mode is required.");
            if (!IsConfigured)
            {
                Debug.LogError("[M13][Match] MatchSessionController dependencies are incomplete.", this);
                return;
            }

            matchStarted = true;
            activeMode = mode;
            // Standalone network peers must continue pumping UTP while minimized or unfocused.
            // Otherwise one client losing focus can make the server expire an otherwise healthy peer.
            Application.runInBackground = true;
            localPlayerId = mode == MultiplayerDevelopmentMode.NetworkHost ? PlayerA : default;
            hasActiveApprovedShot = false;
            localSubmissionPending = false;
            remoteSubmissionPending = false;
            remoteTurnElapsed = 0f;
            snapshotStore.Reset();
            matchSuspended = false;
            latestLifecycle = default;
            transport.CancelPending();
            networkTransport.CancelPending();
            dedicatedServerTransport.CancelPending();
            bool verbose = settings != null && settings.VerboseLogging;
            foreach (string argument in Environment.GetCommandLineArgs())
                if (string.Equals(argument, "-swingpopVerboseNetwork", StringComparison.OrdinalIgnoreCase)) verbose = true;
            networkTransport.Configure(mode == MultiplayerDevelopmentMode.NetworkHost ? authority : null, verbose);
            dedicatedServerTransport.Configure(
                mode == MultiplayerDevelopmentMode.DedicatedServer ? authority : null,
                settings != null ? settings.DedicatedServerMaxPlayers : OnlineProtocol.DedicatedServerPlayerCapacity,
                verbose);
            dedicatedServerTransport.ConfigureReconnectPolicy(
                ReadReconnectGraceOverride(Environment.GetCommandLineArgs(),
                    settings != null ? settings.ReconnectGraceSeconds : 30f));
            bool authenticationEnabled = mode == MultiplayerDevelopmentMode.DedicatedServer
                                         && settings != null && settings.DevelopmentAuthenticationEnabled;
            SwingPop.Online.AuthenticationController.TryLoadServerSigningKey(
                Environment.GetCommandLineArgs(), out byte[] authKey);
            dedicatedServerTransport.ConfigureAuthentication(authenticationEnabled, authKey,
                settings != null ? settings.DevelopmentAuthenticationIssuer : "swingpop-development",
                settings != null ? settings.AuthenticationSessionLifetimeSeconds : 1800f,
                settings != null ? settings.AuthenticationTimeoutSeconds : 8f);
            IMatchAdmissionRegistry admissionRegistry = null;
            ConnectivityCredentialRegistry connectivityRegistry = null;
            MatchReservationFileDocument reservationDocument = null;
            string loadedReservationPath = string.Empty;
            if (mode == MultiplayerDevelopmentMode.DedicatedServer)
            {
                string reservationPath = MatchReservationFile.ReadArgument(Environment.GetCommandLineArgs(),
                    MatchReservationFile.ReservationFileArgument);
                if (!string.IsNullOrWhiteSpace(reservationPath))
                {
                    if (!MatchReservationFile.TryLoad(reservationPath, out reservationDocument,
                            out DevelopmentMatchAdmissionRegistry loadedRegistry,
                            out connectivityRegistry))
                    {
                        Debug.LogError("[M17][Admission] Match reservation file is invalid.", this);
                        return;
                    }
                    admissionRegistry = loadedRegistry;
                    loadedReservationPath = reservationPath;
                }
            }
            dedicatedServerTransport.ConfigureMatchAdmission(admissionRegistry);
            dedicatedServerTransport.ConfigureConnectivity(connectivityRegistry);
            if (reservationDocument != null
                && reservationDocument.ConnectivityMode == MatchConnectivityMode.ProductionRelay)
            {
                if (!ProductionRelayServerPayload.TryDeserialize(reservationDocument.ServerProviderPayload,
                        out ProductionRelayServerPayload serverPayload)
                    || !dedicatedServerTransport.SetProductionRelayServerPayload(serverPayload))
                {
                    Debug.LogError("[M19][Relay] Dedicated server Relay payload is invalid.", this);
                    return;
                }
                if (!MatchReservationFile.TryDeleteSensitiveReservation(loadedReservationPath, reservationDocument))
                    Debug.LogWarning("[M19][Relay] Sensitive temporary reservation could not be deleted.", this);
            }
            float liveness = settings != null ? settings.ConnectionLivenessTimeoutSeconds : 30f;
            networkTransport.ConfigureConnectionLiveness(liveness);
            dedicatedServerTransport.ConfigureConnectionLiveness(liveness);
            shotFlow.ConfigureCommitGate(this);
            holeFlow.SetAutomaticFlowSuspended(true);
            float timeout = settings != null ? settings.ConnectionTimeoutSeconds : 8f;
            MatchAdmissionGrant pendingGrant = default;
            bool hasPendingAdmission = mode == MultiplayerDevelopmentMode.NetworkClient
                                       && MatchAdmissionHandoff.TryPeek(out pendingGrant);
            if (hasPendingAdmission && (!networkTransport.SetConnectivityDescriptor(pendingGrant.Connectivity)
                                        || !networkTransport.SetMatchJoinTicket(pendingGrant.JoinTicket)))
            {
                Debug.LogError("[M18][Connectivity] Lobby admission descriptor is invalid or expired.", this);
                return;
            }
            if (hasPendingAdmission
                && pendingGrant.Connectivity.Mode == MatchConnectivityMode.ProductionRelay)
            {
                string[] commandLine = Environment.GetCommandLineArgs();
                string environment = MatchReservationFile.ReadArgument(commandLine,
                    LobbyDevelopmentController.UnityEnvironmentArgument);
                if (string.IsNullOrWhiteSpace(environment)) environment = "production";
                string connectionType = MatchReservationFile.ReadArgument(commandLine,
                    LobbyDevelopmentController.RelayConnectionTypeArgument);
                if (string.IsNullOrWhiteSpace(connectionType)) connectionType = "dtls";
                (ProductionRelayServerPayload Payload, ConnectivityProviderFailure Failure) joined =
                    await UnityRelayClientConnector.JoinAsync(pendingGrant.Connectivity, environment,
                        connectionType, 30f);
                if (joined.Failure.IsFailure
                    || !networkTransport.SetProductionRelayServerPayload(joined.Payload))
                {
                    Debug.LogError($"[M19][Relay] Production Relay join failed safely: {joined.Failure}. " +
                                   "No Direct fallback was attempted.", this);
                    return;
                }
                address = joined.Payload.Host;
                port = joined.Payload.Port;
            }
            bool started = mode switch
            {
                MultiplayerDevelopmentMode.NetworkHost => networkTransport.StartHost(address, port, timeout),
                MultiplayerDevelopmentMode.NetworkClient => networkTransport.StartClient(address, port, timeout),
                MultiplayerDevelopmentMode.DedicatedServer => dedicatedServerTransport.StartDedicatedServer(address, port, timeout),
                _ => false
            };
            if (started && hasPendingAdmission) MatchAdmissionHandoff.TryConsume(out _);
            if (started && mode == MultiplayerDevelopmentMode.DedicatedServer && reservationDocument != null)
            {
                if (reservationDocument.ConnectivityMode == MatchConnectivityMode.ProductionRelay)
                    _ = WriteProductionRelayReadyMarkerAsync(reservationDocument.GameMatchId);
                else MatchReservationFile.TryWriteReadyMarker(Environment.GetCommandLineArgs(),
                    reservationDocument.GameMatchId, $"{address}:{port}");
            }
            if (!started)
                Debug.LogError($"[M13][Match] Could not start {mode} at {address}:{port}.", this);
        }

        private async Task WriteProductionRelayReadyMarkerAsync(MatchId gameMatchId)
        {
            float timeout = 30f;
            float elapsed = 0f;
            while (elapsed < timeout && dedicatedServerTransport != null
                   && dedicatedServerTransport.IsCreated
                   && !dedicatedServerTransport.IsProductionRelayEstablished)
            {
                await Task.Delay(50);
                elapsed += 0.05f;
            }
            if (dedicatedServerTransport == null || !dedicatedServerTransport.IsProductionRelayEstablished)
            {
                Debug.LogError("[M19][Relay] Dedicated server did not bind to Unity Relay before timeout.", this);
                return;
            }
            MatchReservationFile.TryWriteReadyMarker(Environment.GetCommandLineArgs(), gameMatchId,
                $"relay:{ConnectivityProtocol.UnityRelayProvider}");
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
            if (activeMode == MultiplayerDevelopmentMode.DedicatedServer)
            {
                if (!shotFlow.TryExecuteAuthoritativeShot(approved.Command))
                    Debug.LogError("[M14][Shot] Dedicated authority could not execute the approved shot.", this);
                return;
            }
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

        private IMatchTransport ActiveTransport => activeMode switch
        {
            MultiplayerDevelopmentMode.NetworkHost or MultiplayerDevelopmentMode.NetworkClient => networkTransport,
            MultiplayerDevelopmentMode.DedicatedServer => dedicatedServerTransport,
            _ => transport
        };
        private bool IsActiveTransportReady => activeMode is MultiplayerDevelopmentMode.NetworkHost
            or MultiplayerDevelopmentMode.NetworkClient ? networkTransport != null && networkTransport.IsReady
            : activeMode == MultiplayerDevelopmentMode.DedicatedServer
                ? dedicatedServerTransport != null && dedicatedServerTransport.IsReady
                : true;

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

        private void OnAllDedicatedPlayersReady()
        {
            if (activeMode != MultiplayerDevelopmentMode.DedicatedServer || authority == null || holeFlow == null) return;
            Vector3 tee = holeFlow.Hole.TeePosition;
            PlayerSnapshot[] players =
            {
                CreateInitialPlayer(PlayerA, "PLAYER A", 0, false, tee),
                CreateInitialPlayer(PlayerB, "PLAYER B", 1, false, tee)
            };
            MatchId matchId = dedicatedServerTransport.ReservedGameMatchId.IsValid
                ? dedicatedServerTransport.ReservedGameMatchId
                : new MatchId($"server-hole01-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
            MatchSnapshot initial = authority.StartMatch(matchId, "hole-01", players);
            dedicatedServerTransport.BeginDedicatedMatch(initial);
        }

        private void OnDedicatedPlayerDisconnected(MatchPlayerId playerId)
        {
            localSubmissionPending = false;
            remoteSubmissionPending = false;
            Debug.LogWarning($"[M15][Connection] {playerId} disconnected; match suspended for reconnect grace.", this);
        }

        private void OnNetworkDisconnected(string reason)
        {
            bool rejectedPendingLocalShot = localSubmissionPending;
            localSubmissionPending = false;
            remoteSubmissionPending = false;
            if (rejectedPendingLocalShot) ShotRejected?.Invoke();
            Debug.LogWarning($"[M13][Match] Network disconnected: {reason}", this);
        }

        private void OnLifecycleChanged(MatchLifecycleChangedMessage message)
        {
            latestLifecycle = message;
            matchSuspended = message.LifecycleState == DedicatedMatchLifecycleState.ReconnectGrace;
            if (message.LifecycleState is DedicatedMatchLifecycleState.Aborted or DedicatedMatchLifecycleState.Ended)
            {
                localSubmissionPending = false;
                remoteSubmissionPending = false;
            }
            SnapshotChanged?.Invoke(snapshotStore.Current);
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

        private static float ReadReconnectGraceOverride(string[] arguments, float fallback)
        {
            const string prefix = "-swingpopReconnectGrace=";
            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    if (argument == null || !argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    if (float.TryParse(argument.Substring(prefix.Length),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float parsed))
                        return Mathf.Clamp(parsed, 3f, 120f);
                }
            }
            return Mathf.Clamp(fallback, 3f, 120f);
        }
    }
}

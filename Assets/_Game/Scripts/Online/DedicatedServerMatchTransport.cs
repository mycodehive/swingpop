using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace SwingPop.Online
{
    /// <summary>
    /// Server-only Unity Transport adapter for two remote players. It owns no local player and
    /// accepts gameplay results only from the dedicated process' existing Ball/HoleFlow graph.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DedicatedServerMatchTransport : MonoBehaviour, IMatchTransport
    {
        private sealed class ClientPeer
        {
            public NetworkConnection Connection;
            public readonly NetworkSequenceGuard InboundSequence = new();
            public MatchPlayerId PlayerId;
            public bool HandshakeComplete;
            public float RateWindowElapsed;
            public int SubmissionsInWindow;
            public int ReconnectRequestsInWindow;
            public int AuthenticationRequestsInWindow;
            public long SentBytes;
            public long ReceivedBytes;
            public long SnapshotVersion = -1;
            public string SnapshotHash = string.Empty;
            public long AcceptedAtMilliseconds;
            public bool Authenticated;
            public PlayerAccountId AccountId;
            public AuthSessionId AuthSessionId;
        }

        private sealed class RejectedPeer
        {
            public NetworkConnection Connection;
            public bool DisconnectIssued;
        }

        [SerializeField] private LocalMatchAuthority authority;
        [SerializeField, Range(2, 2)] private int maxPlayers = OnlineProtocol.DedicatedServerPlayerCapacity;
        [SerializeField] private bool verboseLogging;

        private readonly JsonMatchMessageSerializer serializer = new();
        private readonly ConnectionPlayerRegistry playerRegistry = new();
        private readonly DedicatedPlayerSlotAllocator slotAllocator = new();
        private readonly DedicatedMatchLifecycle lifecycle = new();
        private readonly List<ClientPeer> peers = new(OnlineProtocol.DedicatedServerPlayerCapacity);
        private readonly List<RejectedPeer> rejectedPeers = new();
        private readonly NetworkDesyncTelemetry desyncTelemetry = new();
        private readonly ReconnectSessionRegistry reconnectSessions = new();
        private readonly MatchPlayerOwnershipRegistry matchOwnership = new();
        private readonly IServerClock serverClock = new SystemServerClock();
        private AuthenticatedConnectionRegistry authenticationRegistry;
        private NetworkDriver driver;
        private NetworkPipeline reliablePipeline;
        private NetworkConnectionStateMachine connectionState = new();
        private string address = "127.0.0.1";
        private ushort port = 7777;
        private float timeoutSeconds = 8f;
        private float livenessTimeoutSeconds = 30f;
        private float pingElapsed;
        private long outboundSequence;
        private bool matchStarted;
        private float reconnectGraceSeconds = 30f;
        private float endedCleanupElapsed;
        private float reconnectRateWindowElapsed;
        private int reconnectRequestsInWindow;
        private bool authenticationRequired;
        private float authenticationTimeoutSeconds = 8f;
        private float authenticationRateWindowElapsed;
        private int authenticationRequestsInWindow;

        public event Action<ApprovedShot> ShotApprovedReceived;
        public event Action<ShotRejection> ShotRejectedReceived;
        public event Action<MatchSnapshot> SnapshotReceived;
        public event Action<MatchPlayerId> PlayerConnected;
        public event Action<MatchPlayerId> PlayerDisconnected;
        public event Action AllPlayersReady;
        public event Action<string> ServerError;
        public event Action<MatchLifecycleChangedMessage> LifecycleChanged;

        public int PendingMessageCount => 0;
        public int MessageCount { get; private set; }
        public int LastShotSubmissionBytes { get; private set; }
        public int LastSnapshotBytes { get; private set; }
        public int ConnectedPlayerCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < peers.Count; index++)
                    if (peers[index].HandshakeComplete) count++;
                return count;
            }
        }
        public int ReservedPlayerCount => slotAllocator.Count;
        public int MaxPlayers => Mathf.Clamp(maxPlayers, 2, 2);
        public NetworkConnectionState ConnectionState => connectionState.State;
        public DedicatedMatchLifecycleState LifecycleState => lifecycle.State;
        public string Address => address;
        public ushort Port => port;
        public long SentBytes { get; private set; }
        public long ReceivedBytes { get; private set; }
        public long OutboundSequence => outboundSequence;
        public int RejectedMessageCount { get; private set; }
        public ShotRejectReason LastRejectionReason { get; private set; }
        public int DesyncCount { get; private set; }
        public string LocalSnapshotHash { get; private set; } = string.Empty;
        public bool IsCreated => driver.IsCreated;
        public bool IsReady => matchStarted && connectionState.State == NetworkConnectionState.InMatch;
        public bool IsMatchSuspended => lifecycle.State == DedicatedMatchLifecycleState.ReconnectGrace;
        public float ReconnectGraceSeconds => reconnectGraceSeconds;
        public bool AuthenticationRequired => authenticationRequired;
        public int AuthenticatedConnectionCount => authenticationRegistry?.ActiveConnectionCount ?? 0;
        public int AuthenticationSessionCount => authenticationRegistry?.SessionCount ?? 0;

        private void Update() => Tick(Time.unscaledDeltaTime);
        private void OnDisable() => CancelPending();

        public void Configure(LocalMatchAuthority matchAuthority, int playerCapacity, bool verbose)
        {
            authority = matchAuthority;
            maxPlayers = Mathf.Clamp(playerCapacity, 2, 2);
            verboseLogging = verbose;
        }

        public void ConfigureReconnectPolicy(float graceSeconds)
        {
            reconnectGraceSeconds = Mathf.Clamp(graceSeconds, 3f, 120f);
        }

        public void ConfigureAuthentication(bool required, byte[] signingKey, string issuer,
            float sessionLifetimeSeconds, float authTimeoutSeconds)
        {
            authenticationRequired = required;
            authenticationTimeoutSeconds = Mathf.Clamp(authTimeoutSeconds, 5f, 10f);
            if (!required)
            {
                authenticationRegistry = null;
                return;
            }
            if (signingKey == null || signingKey.Length < 32)
            {
                authenticationRegistry = null;
                return;
            }
            DevelopmentAuthenticationProvider provider = new(signingKey, issuer);
            authenticationRegistry = new AuthenticatedConnectionRegistry(provider,
                Mathf.RoundToInt(Mathf.Clamp(sessionLifetimeSeconds, 60f, 7200f) * 1000f));
        }

        public void ConfigureConnectionLiveness(float seconds)
        {
            livenessTimeoutSeconds = Mathf.Clamp(seconds, 15f, 120f);
        }

        public bool StartDedicatedServer(string bindAddress, ushort networkPort, float connectionTimeoutSeconds)
        {
            ShutdownInternal(false);
            if (authenticationRequired && authenticationRegistry == null)
            {
                Fail("Development authentication is enabled but no runtime signing key was supplied.");
                return false;
            }
            address = string.IsNullOrWhiteSpace(bindAddress) ? "127.0.0.1" : bindAddress.Trim();
            port = networkPort;
            timeoutSeconds = Mathf.Clamp(connectionTimeoutSeconds, 5f, 10f);
            connectionState.TryTransition(NetworkConnectionState.Starting);
            CreateDriver();

            NetworkEndpoint endpoint = address == "127.0.0.1" || address.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                ? NetworkEndpoint.LoopbackIpv4.WithPort(port)
                : NetworkEndpoint.AnyIpv4.WithPort(port);
            int bindResult = driver.Bind(endpoint);
            if (bindResult != 0 || driver.Listen() != 0)
            {
                Fail($"bind/listen failed ({bindResult}) on {address}:{port}");
                return false;
            }

            connectionState.TryTransition(NetworkConnectionState.Listening);
            Log("Connection", $"Listening {address}:{port}; waiting for {MaxPlayers} players");
            return true;
        }

        public void BeginDedicatedMatch(MatchSnapshot initialSnapshot)
        {
            if (initialSnapshot == null || ConnectedPlayerCount != MaxPlayers || matchStarted) return;
            lifecycle.TryTransition(DedicatedMatchLifecycleState.Starting);
            matchStarted = true;
            connectionState.TryTransition(NetworkConnectionState.InMatch);
            for (int index = 0; index < peers.Count; index++)
            {
                ClientPeer peer = peers[index];
                if (!peer.HandshakeComplete || !peer.PlayerId.IsValid) continue;
                ReconnectTicket ticket = reconnectSessions.Register(initialSnapshot.MatchId, peer.PlayerId,
                    peer.AccountId, serverClock.UtcNowMilliseconds);
                SendTo(peer, NetworkMessageType.ReconnectTicketIssued, initialSnapshot.MatchId,
                    serializer.Serialize(new ReconnectTicketIssuedMessage(ticket)));
            }
            Broadcast(NetworkMessageType.MatchStarted, initialSnapshot.MatchId, serializer.Serialize(initialSnapshot));
            PublishSnapshot(initialSnapshot);
            lifecycle.TryTransition(DedicatedMatchLifecycleState.Playing);
            Log("Authority", $"Match started id={initialSnapshot.MatchId} players={ConnectedPlayerCount}");
        }

        public void ConfigureLatency(int milliseconds)
        {
            // A real server observes network conditions; it does not inject LocalLoopback latency.
        }

        public bool SubmitShot(ShotSubmission submission)
        {
            // A dedicated server is not a player and cannot originate ShotSubmission.
            return false;
        }

        public bool SubmitShotResult(NetworkShotResult result)
        {
            if (!IsReady || authority == null || !authority.ResolveShot(result)) return false;
            if (desyncTelemetry.RecordAuthoritative(result)) ReportDesync();
            MatchSnapshot snapshot = authority.CurrentSnapshot;
            PublishSnapshot(snapshot);
            Broadcast(NetworkMessageType.TurnChanged, result.MatchId,
                serializer.Serialize(new TurnChangedMessage(snapshot)));
            Log("Shot", $"Resolved player={result.PlayerId} seq={result.ShotSequence} lie={result.FinalLie}");
            return true;
        }

        public void PublishSnapshot(MatchSnapshot snapshot)
        {
            if (snapshot == null) return;
            string payload = serializer.Serialize(snapshot);
            LastSnapshotBytes = Encoding.UTF8.GetByteCount(payload);
            LocalSnapshotHash = MatchSnapshotHash.Compute(snapshot);
            SnapshotReceived?.Invoke(snapshot);
            if (driver.IsCreated && peers.Count > 0)
                Broadcast(NetworkMessageType.Snapshot, snapshot.MatchId, payload);
            if (snapshot.Phase == MatchPhase.HoleComplete)
                lifecycle.TryTransition(DedicatedMatchLifecycleState.HoleComplete);
            else if (snapshot.Phase == MatchPhase.Aborted)
                lifecycle.TryTransition(DedicatedMatchLifecycleState.Aborted);
            Log("Snapshot", $"v={snapshot.Version} turn={snapshot.TurnIndex} seq={snapshot.ShotSequence} hash={LocalSnapshotHash}");
        }

        public void Tick(float deltaTime)
        {
            if (!driver.IsCreated) return;
            driver.ScheduleUpdate().Complete();
            float safeDelta = Mathf.Max(0f, deltaTime);
            pingElapsed += safeDelta;
            reconnectRateWindowElapsed += safeDelta;
            authenticationRateWindowElapsed += safeDelta;
            if (reconnectRateWindowElapsed >= 1f)
            {
                reconnectRateWindowElapsed = 0f;
                reconnectRequestsInWindow = 0;
            }
            if (authenticationRateWindowElapsed >= 1f)
            {
                authenticationRateWindowElapsed = 0f;
                authenticationRequestsInWindow = 0;
            }
            PollRejectedConnections();
            AcceptConnections();
            for (int index = peers.Count - 1; index >= 0; index--)
            {
                ClientPeer peer = peers[index];
                peer.RateWindowElapsed += safeDelta;
                if (peer.RateWindowElapsed >= 1f)
                {
                    peer.RateWindowElapsed = 0f;
                    peer.SubmissionsInWindow = 0;
                    peer.ReconnectRequestsInWindow = 0;
                    peer.AuthenticationRequestsInWindow = 0;
                }
                PollPeer(peer);
            }

            CheckReconnectDeadline();
            if (lifecycle.State == DedicatedMatchLifecycleState.Aborted)
            {
                endedCleanupElapsed += safeDelta;
                if (endedCleanupElapsed >= 2f && lifecycle.TryTransition(DedicatedMatchLifecycleState.Ended))
                    BroadcastLifecycle(default, PlayerConnectionState.Expired, 0L, "Match ended after reconnect grace expiry");
            }

            if (peers.Count > 0 && pingElapsed >= 1f)
            {
                pingElapsed = 0f;
                Broadcast(NetworkMessageType.Ping, default,
                    serializer.Serialize(new PingMessage(NowMilliseconds())));
            }
        }

        public void CancelPending() => ShutdownInternal(true);

        public bool TryGetClientTelemetry(MatchPlayerId playerId, out long sent, out long received,
            out long snapshotVersion, out string snapshotHash)
        {
            foreach (ClientPeer peer in peers)
            {
                if (peer.PlayerId != playerId) continue;
                sent = peer.SentBytes;
                received = peer.ReceivedBytes;
                snapshotVersion = peer.SnapshotVersion;
                snapshotHash = peer.SnapshotHash;
                return true;
            }
            sent = 0;
            received = 0;
            snapshotVersion = -1;
            snapshotHash = string.Empty;
            return false;
        }

        public bool RevokeAuthenticationSession(AuthSessionId sessionId)
        {
            return authenticationRegistry != null && authenticationRegistry.Revoke(sessionId);
        }

        public bool TryGetMatchOwner(MatchPlayerId playerId, out PlayerAccountId accountId) =>
            matchOwnership.TryGetOwner(playerId, out accountId);

        private void CreateDriver()
        {
            NetworkSettings networkSettings = new();
            networkSettings.WithNetworkConfigParameters(
                connectTimeoutMS: 1000,
                maxConnectAttempts: Mathf.CeilToInt(timeoutSeconds),
                disconnectTimeoutMS: Mathf.RoundToInt(livenessTimeoutSeconds * 1000f),
                heartbeatTimeoutMS: 500,
                receiveQueueCapacity: 256,
                sendQueueCapacity: 256);
            networkSettings.WithFragmentationStageParameters(OnlineProtocol.MaximumPayloadBytes);
            // M15 uses UTP's WebSocket interface (TCP-backed on standalone players). A hard-killed
            // localhost UDP peer can poison the shared Windows loopback socket and starve the
            // remaining peer; the stream interface gives each peer an independent OS connection.
            driver = NetworkDriver.Create(new WebSocketNetworkInterface(), networkSettings);
            reliablePipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        }

        private void AcceptConnections()
        {
            NetworkConnection accepted;
            while ((accepted = driver.Accept()) != default)
            {
                if (peers.Count >= MaxPlayers + 2)
                {
                    SendToConnection(accepted, NetworkMessageType.ConnectionRejected, default,
                        serializer.Serialize(new ConnectionRejectedMessage(ShotRejectReason.MatchFull, "MatchFull")), null);
                    // Keep the socket until the next normal driver update flushes the rejection.
                    // Updating the whole driver here would reset unread events for players A/B.
                    rejectedPeers.Add(new RejectedPeer { Connection = accepted });
                    LastRejectionReason = ShotRejectReason.MatchFull;
                    RejectedMessageCount++;
                    Log("Connection", "Rejected excessive pending client: MatchFull");
                    continue;
                }

                peers.Add(new ClientPeer
                {
                    Connection = accepted,
                    AcceptedAtMilliseconds = serverClock.UtcNowMilliseconds
                });
                connectionState.TryTransition(NetworkConnectionState.Handshaking);
                Log("Connection", $"Accepted socket; handshakes={peers.Count}/{MaxPlayers}");
            }
        }

        private void PollRejectedConnections()
        {
            for (int index = rejectedPeers.Count - 1; index >= 0; index--)
            {
                RejectedPeer rejected = rejectedPeers[index];
                bool disconnected = false;
                NetworkEvent.Type eventType;
                while ((eventType = rejected.Connection.PopEvent(driver, out _)) != NetworkEvent.Type.Empty)
                    if (eventType == NetworkEvent.Type.Disconnect) disconnected = true;

                if (disconnected || !rejected.Connection.IsCreated)
                {
                    rejectedPeers.RemoveAt(index);
                    continue;
                }

                if (!rejected.DisconnectIssued)
                {
                    rejected.Connection.Disconnect(driver);
                    rejected.DisconnectIssued = true;
                }
            }
        }

        private void PollPeer(ClientPeer peer)
        {
            if (!peer.HandshakeComplete
                && serverClock.UtcNowMilliseconds - peer.AcceptedAtMilliseconds
                > (authenticationRequired ? authenticationTimeoutSeconds : timeoutSeconds) * 1000f)
            {
                RejectAndClose(peer, NetworkMessageType.ConnectionRejected,
                    serializer.Serialize(new ConnectionRejectedMessage(ShotRejectReason.ConnectionNotReady,
                        "Handshake timeout")));
                return;
            }
            NetworkEvent.Type eventType;
            while ((eventType = peer.Connection.PopEvent(driver, out DataStreamReader reader)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Data:
                        Receive(peer, reader);
                        break;
                    case NetworkEvent.Type.Disconnect:
                        byte reasonCode = reader.Length > 0 ? reader.ReadByte() : (byte)0;
                        Log("Connection", $"Disconnect event player={peer.PlayerId} reasonCode={reasonCode} handshake={peer.HandshakeComplete} " +
                                          $"sent={peer.SentBytes} received={peer.ReceivedBytes}");
                        RemovePeer(peer, "Remote client disconnected");
                        return;
                }
            }
        }

        private void Receive(ClientPeer peer, DataStreamReader reader)
        {
            if (!NetworkMessageRules.IsPayloadSizeValid(reader.Length))
            {
                RejectMalformed(ShotRejectReason.PayloadTooLarge);
                return;
            }

            byte[] bytes = new byte[reader.Length];
            reader.ReadBytes(bytes);
            peer.ReceivedBytes += bytes.Length;
            ReceivedBytes += bytes.Length;
            MessageCount++;
            NetworkMessageEnvelope envelope;
            try
            {
                envelope = serializer.Deserialize<NetworkMessageEnvelope>(Encoding.UTF8.GetString(bytes));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[M14][Server] Malformed envelope: {exception.Message}", this);
                RejectMalformed(ShotRejectReason.InvalidCommand);
                return;
            }

            ShotRejectReason rule = NetworkMessageRules.ValidateEnvelope(envelope, peer.InboundSequence);
            if (rule != ShotRejectReason.None)
            {
                RejectMalformed(rule);
                return;
            }
            if (!NetworkMessageRules.IsAllowedFromClient(envelope.MessageType))
            {
                RejectMalformed(ShotRejectReason.MessageDirectionNotAllowed);
                return;
            }
            Dispatch(peer, envelope);
        }

        private void Dispatch(ClientPeer peer, NetworkMessageEnvelope envelope)
        {
            if (authenticationRequired && !peer.Authenticated
                && !AuthenticationMessagePolicy.IsAllowedBeforeAuthentication(envelope.MessageType))
            {
                RejectAuthenticationAndClose(peer, AuthenticationRejectReason.AuthenticationRequired);
                return;
            }
            switch (envelope.MessageType)
            {
                case NetworkMessageType.AuthRequest:
                    HandleAuthenticationRequest(peer, serializer.Deserialize<AuthRequestMessage>(envelope.Payload));
                    break;
                case NetworkMessageType.ClientHello:
                    HandleClientHello(peer, serializer.Deserialize<ClientHelloMessage>(envelope.Payload));
                    break;
                case NetworkMessageType.ReconnectRequest:
                {
                    ReconnectRequestMessage request = serializer.Deserialize<ReconnectRequestMessage>(envelope.Payload);
                    if (envelope.MatchId != request.MatchId)
                        RejectReconnectAndClose(peer, ReconnectRejectReason.UnknownMatch);
                    else
                        HandleReconnectRequest(peer, request);
                    break;
                }
                case NetworkMessageType.ShotSubmission:
                    HandleShotSubmission(peer, serializer.Deserialize<ShotSubmission>(envelope.Payload));
                    break;
                case NetworkMessageType.PredictedShotResult:
                    if (desyncTelemetry.RecordPredicted(serializer.Deserialize<NetworkShotResult>(envelope.Payload)))
                        ReportDesync();
                    break;
                case NetworkMessageType.SnapshotHash:
                {
                    SnapshotHashMessage hash = serializer.Deserialize<SnapshotHashMessage>(envelope.Payload);
                    peer.SnapshotVersion = hash.SnapshotVersion;
                    peer.SnapshotHash = hash.Hash;
                    if (authority != null && authority.CurrentSnapshot.Version == hash.SnapshotVersion
                        && !string.Equals(LocalSnapshotHash, hash.Hash, StringComparison.Ordinal))
                        DesyncCount++;
                    break;
                }
                case NetworkMessageType.Ping:
                    SendTo(peer, NetworkMessageType.Pong, envelope.MatchId, envelope.Payload);
                    break;
                case NetworkMessageType.Pong:
                    break;
                case NetworkMessageType.DisconnectNotice:
                    RemovePeer(peer, serializer.Deserialize<DisconnectNoticeMessage>(envelope.Payload).Reason);
                    break;
            }
        }

        private void HandleClientHello(ClientPeer peer, ClientHelloMessage hello)
        {
            if (peer.HandshakeComplete || hello.RequestedRole != ClientRequestedRole.Player
                || authenticationRequired && !peer.Authenticated)
            {
                RejectMalformed(ShotRejectReason.InvalidCommand);
                return;
            }
            if (matchStarted)
            {
                RejectAndClose(peer, NetworkMessageType.ConnectionRejected,
                    serializer.Serialize(new ConnectionRejectedMessage(ShotRejectReason.MatchFull, "MatchFull")));
                LastRejectionReason = ShotRejectReason.MatchFull;
                RejectedMessageCount++;
                return;
            }
            if (!slotAllocator.TryAssign(out MatchPlayerId playerId)
                || !playerRegistry.TryBind(peer.Connection.GetHashCode(), playerId))
            {
                SendTo(peer, NetworkMessageType.ConnectionRejected, default,
                    serializer.Serialize(new ConnectionRejectedMessage(ShotRejectReason.MatchFull, "MatchFull")));
                LastRejectionReason = ShotRejectReason.MatchFull;
                RejectedMessageCount++;
                return;
            }

            peer.PlayerId = playerId;
            if (authenticationRequired && !matchOwnership.TryBind(playerId, peer.AccountId))
            {
                playerRegistry.Remove(peer.Connection.GetHashCode());
                slotAllocator.Release(playerId);
                RejectAuthenticationAndClose(peer, AuthenticationRejectReason.SessionConflict);
                return;
            }
            peer.HandshakeComplete = true;
            SendTo(peer, NetworkMessageType.PlayerAssigned, default,
                serializer.Serialize(new PlayerAssignedMessage(playerId)));
            connectionState.TryTransition(NetworkConnectionState.Connected);
            PlayerConnected?.Invoke(playerId);
            Log("Connection", $"{playerId} connected account={AccountFingerprint(peer.AccountId)} build={hello.ClientBuild}");
            if (ConnectedPlayerCount == MaxPlayers) AllPlayersReady?.Invoke();
        }

        private void HandleAuthenticationRequest(ClientPeer peer, AuthRequestMessage request)
        {
            peer.AuthenticationRequestsInWindow++;
            authenticationRequestsInWindow++;
            if (!authenticationRequired || authenticationRegistry == null)
            {
                RejectAuthenticationAndClose(peer, AuthenticationRejectReason.DevelopmentProviderDisabled);
                return;
            }
            if (request.ProtocolVersion != OnlineProtocol.CurrentVersion)
            {
                RejectAuthenticationAndClose(peer, AuthenticationRejectReason.UnsupportedVersion);
                return;
            }
            if (peer.Authenticated)
            {
                RejectAuthenticationAndClose(peer, AuthenticationRejectReason.AlreadyAuthenticated);
                return;
            }
            if (peer.AuthenticationRequestsInWindow > 3 || authenticationRequestsInWindow > 12)
            {
                RejectAuthenticationAndClose(peer, AuthenticationRejectReason.RateLimited);
                return;
            }

            AuthenticationBindingResult result = authenticationRegistry.Authenticate(
                peer.Connection.GetHashCode(), request.Credential, serverClock.UtcNowMilliseconds);
            if (!result.Accepted)
            {
                RejectAuthenticationAndClose(peer, result.Reason);
                return;
            }
            peer.Authenticated = true;
            peer.AccountId = result.Session.AccountId;
            peer.AuthSessionId = result.Session.SessionId;
            AuthAcceptedMessage accepted = new(result.Session.AccountId, result.Session.SessionId,
                result.Session.ExpiresAtMilliseconds);
            SendTo(peer, NetworkMessageType.AuthAccepted, default, serializer.Serialize(accepted));
            Log("Auth", $"Accepted account={AccountFingerprint(peer.AccountId)} session={SessionFingerprint(peer.AuthSessionId)}");
        }

        private void HandleShotSubmission(ClientPeer peer, ShotSubmission submission)
        {
            LastShotSubmissionBytes = Encoding.UTF8.GetByteCount(serializer.Serialize(submission));
            if (!peer.HandshakeComplete
                || authenticationRequired && (authenticationRegistry == null
                    || !authenticationRegistry.TryGetConnection(peer.Connection.GetHashCode(), out AuthenticatedPlayerSession authenticated)
                    || authenticated.AccountId != peer.AccountId
                    || !matchOwnership.IsOwner(submission.PlayerId, peer.AccountId))
                || !playerRegistry.IsBoundPlayer(peer.Connection.GetHashCode(), submission.PlayerId))
            {
                RejectSubmission(peer, submission, ShotRejectReason.PlayerSpoofing);
                return;
            }
            if (IsMatchSuspended)
            {
                RejectSubmission(peer, submission, ShotRejectReason.MatchSuspended);
                return;
            }
            peer.SubmissionsInWindow++;
            if (peer.SubmissionsInWindow > 8)
            {
                RejectSubmission(peer, submission, ShotRejectReason.RateLimited);
                return;
            }
            if (!IsReady || authority == null)
            {
                RejectSubmission(peer, submission, ShotRejectReason.ConnectionNotReady);
                return;
            }

            ShotSubmissionDecision decision = authority.SubmitShot(submission);
            if (!decision.Accepted)
            {
                RejectSubmission(peer, submission, decision.Rejection.Reason);
                return;
            }
            if (!authority.BeginShotPlayback(decision.Approved))
            {
                RejectSubmission(peer, submission, ShotRejectReason.InvalidTurn);
                return;
            }

            string payload = serializer.Serialize(decision.Approved);
            Broadcast(NetworkMessageType.ShotApproved, submission.MatchId, payload);
            ShotApprovedReceived?.Invoke(decision.Approved);
            PublishSnapshot(authority.CurrentSnapshot);
            Log("Shot", $"Approved player={submission.PlayerId} seq={decision.Approved.ShotSequence}");
        }

        private void RejectSubmission(ClientPeer peer, ShotSubmission submission, ShotRejectReason reason)
        {
            LastRejectionReason = reason;
            RejectedMessageCount++;
            ShotRejection rejection = new(submission, reason);
            SendTo(peer, NetworkMessageType.ShotRejected, submission.MatchId, serializer.Serialize(rejection));
            ShotRejectedReceived?.Invoke(rejection);
            Log("Authority", $"Rejected player={submission.PlayerId} reason={reason}");
        }

        private void RejectMalformed(ShotRejectReason reason)
        {
            LastRejectionReason = reason;
            RejectedMessageCount++;
            Log("Authority", $"Rejected message reason={reason}");
        }

        private void RemovePeer(ClientPeer peer, string reason)
        {
            if (!peers.Contains(peer)) return;
            MatchPlayerId playerId = peer.PlayerId;
            authenticationRegistry?.RemoveConnection(peer.Connection.GetHashCode());
            peers.Remove(peer);
            if (playerId.IsValid)
            {
                playerRegistry.Remove(peer.Connection.GetHashCode());
                PlayerDisconnected?.Invoke(playerId);
                Log("Connection", $"{playerId} disconnected reason={reason}");
                if (matchStarted && authority != null)
                {
                    long now = serverClock.UtcNowMilliseconds;
                    if (reconnectSessions.TryEnterGrace(playerId, now,
                            Mathf.RoundToInt(reconnectGraceSeconds * 1000f), out long deadline)
                        && authority.EnterReconnectGrace(playerId))
                    {
                        lifecycle.TryTransition(DedicatedMatchLifecycleState.ReconnectGrace);
                        PublishSnapshot(authority.CurrentSnapshot);
                        BroadcastLifecycle(playerId, PlayerConnectionState.ReconnectGrace, deadline,
                            "Player disconnected; match suspended");
                    }
                }
                else
                {
                    slotAllocator.Release(playerId);
                }
            }
        }

        private void HandleReconnectRequest(ClientPeer peer, ReconnectRequestMessage request)
        {
            if (authenticationRequired && (!peer.Authenticated || !peer.AccountId.IsValid))
            {
                RejectReconnectAndClose(peer, ReconnectRejectReason.AuthenticationRequired);
                return;
            }
            peer.ReconnectRequestsInWindow++;
            reconnectRequestsInWindow++;
            if (peer.ReconnectRequestsInWindow > 3 || reconnectRequestsInWindow > 8)
            {
                RejectReconnectAndClose(peer, ReconnectRejectReason.RateLimited);
                return;
            }
            bool duplicateBinding = playerRegistry.ContainsPlayer(request.PlayerId);
            ReconnectValidationResult validation = reconnectSessions.ValidateAndRotate(request,
                serverClock.UtcNowMilliseconds, duplicateBinding, peer.AccountId);
            if (!validation.Accepted)
            {
                RejectReconnectAndClose(peer, validation.Reason);
                return;
            }
            if (!playerRegistry.TryBind(peer.Connection.GetHashCode(), request.PlayerId)
                || authenticationRequired && !matchOwnership.IsOwner(request.PlayerId, peer.AccountId)
                || authority == null || !authority.ReconnectPlayer(request.PlayerId))
            {
                RejectReconnectAndClose(peer, ReconnectRejectReason.PlayerAlreadyConnected);
                return;
            }

            peer.PlayerId = request.PlayerId;
            peer.HandshakeComplete = true;
            MatchSnapshot snapshot = authority.CurrentSnapshot;
            ReconnectAcceptedMessage accepted = new(request.PlayerId, snapshot.MatchId,
                validation.RotatedTicket, snapshot.Version, snapshot.CurrentTurnPlayer);
            SendTo(peer, NetworkMessageType.ReconnectAccepted, snapshot.MatchId, serializer.Serialize(accepted));
            if (!reconnectSessions.HasPlayerInGrace)
                lifecycle.TryTransition(DedicatedMatchLifecycleState.Playing);
            PublishSnapshot(snapshot);
            BroadcastLifecycle(request.PlayerId, PlayerConnectionState.Connected, 0L,
                reconnectSessions.HasPlayerInGrace ? "Player reconnected; waiting for other player" : "Player reconnected; match resumed");
            PlayerConnected?.Invoke(request.PlayerId);
            Log("Reconnect", $"Accepted player={request.PlayerId} generation={validation.RotatedTicket.SessionGeneration} " +
                             $"account={AccountFingerprint(peer.AccountId)} fingerprint={ReconnectSessionRegistry.Fingerprint(validation.RotatedTicket.Secret)}");
        }

        private void RejectAuthenticationAndClose(ClientPeer peer, AuthenticationRejectReason reason)
        {
            RejectedMessageCount++;
            RejectAndClose(peer, NetworkMessageType.AuthRejected,
                serializer.Serialize(new AuthRejectedMessage(reason, reason.ToString())));
            Log("Auth", $"Rejected reason={reason}");
        }

        private void RejectReconnectAndClose(ClientPeer peer, ReconnectRejectReason reason)
        {
            RejectedMessageCount++;
            RejectAndClose(peer, NetworkMessageType.ReconnectRejected,
                serializer.Serialize(new ReconnectRejectedMessage(reason, reason.ToString())));
            Log("Reconnect", $"Rejected reason={reason}");
        }

        private void RejectAndClose(ClientPeer peer, NetworkMessageType type, string payload)
        {
            if (peer == null || !peers.Contains(peer)) return;
            SendTo(peer, type, default, payload);
            authenticationRegistry?.RemoveConnection(peer.Connection.GetHashCode());
            peers.Remove(peer);
            rejectedPeers.Add(new RejectedPeer { Connection = peer.Connection });
        }

        private void CheckReconnectDeadline()
        {
            if (!matchStarted || lifecycle.State != DedicatedMatchLifecycleState.ReconnectGrace) return;
            if (!reconnectSessions.TryExpire(serverClock.UtcNowMilliseconds, out MatchPlayerId expiredPlayer)) return;
            if (authority != null && authority.ExpireReconnectGrace(expiredPlayer)) PublishSnapshot(authority.CurrentSnapshot);
            lifecycle.TryTransition(DedicatedMatchLifecycleState.Aborted);
            reconnectSessions.MarkMatchEnded();
            endedCleanupElapsed = 0f;
            reconnectRateWindowElapsed = 0f;
            reconnectRequestsInWindow = 0;
            BroadcastLifecycle(expiredPlayer, PlayerConnectionState.Expired, 0L,
                "Reconnect grace expired; match aborted");
            Log("Reconnect", $"Grace expired player={expiredPlayer}; match aborted");
        }

        private void BroadcastLifecycle(MatchPlayerId playerId, PlayerConnectionState state, long deadline, string reason)
        {
            MatchLifecycleChangedMessage message = new(lifecycle.State, playerId, state, deadline, reason);
            MatchId id = authority != null && authority.CurrentSnapshot != null ? authority.CurrentSnapshot.MatchId : default;
            Broadcast(NetworkMessageType.MatchLifecycleChanged, id, serializer.Serialize(message));
            LifecycleChanged?.Invoke(message);
        }

        private void Broadcast(NetworkMessageType messageType, MatchId matchId, string payload)
        {
            for (int index = 0; index < peers.Count; index++)
                if (peers[index].HandshakeComplete) SendTo(peers[index], messageType, matchId, payload);
        }

        private bool SendTo(ClientPeer peer, NetworkMessageType messageType, MatchId matchId, string payload)
        {
            return peer != null && SendToConnection(peer.Connection, messageType, matchId, payload, peer);
        }

        private bool SendToConnection(NetworkConnection target, NetworkMessageType messageType, MatchId matchId,
            string payload, ClientPeer peer)
        {
            if (!driver.IsCreated || !target.IsCreated || !NetworkMessageRules.IsAllowedFromServer(messageType)) return false;
            NetworkMessageEnvelope envelope = new(messageType, matchId, ++outboundSequence, payload);
            byte[] bytes = Encoding.UTF8.GetBytes(serializer.Serialize(envelope));
            if (!NetworkMessageRules.IsPayloadSizeValid(bytes.Length))
            {
                LastRejectionReason = ShotRejectReason.PayloadTooLarge;
                RejectedMessageCount++;
                return false;
            }

            NativeArray<byte> nativeBytes = new(bytes, Allocator.Temp);
            int beginResult = driver.BeginSend(reliablePipeline, target, out DataStreamWriter writer, bytes.Length);
            if (beginResult != 0)
            {
                nativeBytes.Dispose();
                Debug.LogWarning($"[M15][Transport] BeginSend failed type={messageType} code={beginResult}", this);
                return false;
            }
            bool wrote = writer.WriteBytes(nativeBytes);
            nativeBytes.Dispose();
            if (!wrote)
            {
                driver.AbortSend(writer);
                return false;
            }
            int endResult = driver.EndSend(writer);
            if (endResult < 0)
            {
                Debug.LogWarning($"[M15][Transport] EndSend failed type={messageType} code={endResult}", this);
                return false;
            }
            SentBytes += bytes.Length;
            if (peer != null) peer.SentBytes += bytes.Length;
            MessageCount++;
            return true;
        }

        private void ReportDesync()
        {
            NetworkDesyncReport report = desyncTelemetry.LastReport;
            if (!report.IsMismatch) return;
            DesyncCount++;
            Debug.LogWarning($"[M14][Snapshot] Predicted mismatch position={report.PositionError:F3}m " +
                             $"lie={report.LieMatches} stroke={report.StrokeMatches} penalty={report.PenaltyMatches}", this);
        }

        private void Fail(string reason)
        {
            connectionState.TryTransition(NetworkConnectionState.Failed);
            Debug.LogError($"[M14][Server] {reason}", this);
            ServerError?.Invoke(reason);
        }

        private void ShutdownInternal(bool notifyRemote)
        {
            if (driver.IsCreated)
            {
                if (notifyRemote)
                    Broadcast(NetworkMessageType.DisconnectNotice, default,
                        serializer.Serialize(new DisconnectNoticeMessage("Dedicated server shutdown")));
                for (int index = 0; index < peers.Count; index++)
                    if (peers[index].Connection.IsCreated) peers[index].Connection.Disconnect(driver);
                for (int index = 0; index < rejectedPeers.Count; index++)
                    if (rejectedPeers[index].Connection.IsCreated) rejectedPeers[index].Connection.Disconnect(driver);
                driver.ScheduleUpdate().Complete();
                driver.Dispose();
            }
            peers.Clear();
            rejectedPeers.Clear();
            playerRegistry.Clear();
            slotAllocator.Reset();
            lifecycle.Reset();
            connectionState = new NetworkConnectionStateMachine();
            outboundSequence = 0;
            pingElapsed = 0f;
            matchStarted = false;
            reconnectSessions.Reset();
            authenticationRegistry?.Reset();
            matchOwnership.Reset();
            endedCleanupElapsed = 0f;
            authenticationRateWindowElapsed = 0f;
            authenticationRequestsInWindow = 0;
        }

        private static long NowMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private static string AccountFingerprint(PlayerAccountId accountId) => accountId.IsValid
            ? DevelopmentAuthenticationProvider.Fingerprint(accountId.Value) : "none";

        private static string SessionFingerprint(AuthSessionId sessionId) => sessionId.IsValid
            ? DevelopmentAuthenticationProvider.Fingerprint(sessionId.Value) : "none";

        private void Log(string category, string message)
        {
            if (verboseLogging) Debug.Log($"[M14][{category}] {message}", this);
        }
    }
}

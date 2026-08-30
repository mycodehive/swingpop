using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace SwingPop.Online
{
    public enum LobbyNetworkRole
    {
        None,
        Service,
        Client
    }

    [DisallowMultipleComponent]
    public sealed class LobbyNetworkTransport : MonoBehaviour
    {
        private sealed class ServicePeer
        {
            public NetworkConnection Connection;
            public readonly NetworkSequenceGuard Sequence = new();
            public long OutboundSequence;
            public long AcceptedAt;
            public LobbyPlayerSession Session;
            public bool Authenticated;
            public float RateElapsed;
            public int Operations;
        }

        private readonly JsonMatchMessageSerializer serializer = new();
        private readonly List<ServicePeer> peers = new();
        private readonly NetworkSequenceGuard clientInboundSequence = new();
        private readonly LobbySnapshotStore snapshotStore = new();
        private NetworkDriver driver;
        private NetworkPipeline pipeline;
        private NetworkConnection clientConnection;
        private LobbyNetworkRole role;
        private ILobbyService service;
        private AuthenticatedConnectionRegistry authRegistry;
        private string address = "127.0.0.1";
        private ushort port = 18817;
        private int maximumConnections = 16;
        private float handshakeTimeout = 8f;
        private string credential = string.Empty;
        private long clientOutboundSequence;
        private float clientElapsed;
        private bool verbose;

        public event Action<AuthAcceptedMessage> AuthenticationAccepted;
        public event Action<AuthRejectedMessage> AuthenticationRejected;
        public event Action<LobbyMatchListMessage> MatchListReceived;
        public event Action<LobbyMatchUpdatedMessage> MatchUpdated;
        public event Action<LobbyAdmissionGrantedMessage> AdmissionGranted;
        public event Action<LobbyOperationRejectedMessage> OperationRejected;
        public event Action<string> Disconnected;

        public LobbyNetworkRole Role => role;
        public NetworkConnectionState ConnectionState { get; private set; } = NetworkConnectionState.Offline;
        public AuthenticationClientState AuthenticationState { get; private set; }
        public LobbyPlayerSession ClientSession { get; private set; }
        public int ConnectedPeerCount => role == LobbyNetworkRole.Service
            ? peers.FindAll(value => value.Authenticated).Count : clientConnection.IsCreated ? 1 : 0;
        public LobbyMatchSnapshot LatestMatch { get; private set; }
        public LobbyMatchSnapshot[] LatestList { get; private set; } = Array.Empty<LobbyMatchSnapshot>();
        public LobbyRejectReason LastRejection { get; private set; }
        public long SentBytes { get; private set; }
        public long ReceivedBytes { get; private set; }

        private void Update() => Tick(Time.unscaledDeltaTime);
        private void OnDisable() => Shutdown();

        public bool StartService(string bindAddress, ushort bindPort, int maxConnections,
            float timeoutSeconds, IAuthenticationService authenticationService,
            long authSessionLifetimeMilliseconds, ILobbyService lobbyService, bool verboseLogging)
        {
            Shutdown();
            if (authenticationService == null || lobbyService == null) return false;
            role = LobbyNetworkRole.Service;
            address = string.IsNullOrWhiteSpace(bindAddress) ? "127.0.0.1" : bindAddress.Trim();
            port = bindPort;
            maximumConnections = Mathf.Clamp(maxConnections, 2, 64);
            handshakeTimeout = Mathf.Clamp(timeoutSeconds, 5f, 20f);
            service = lobbyService;
            authRegistry = new AuthenticatedConnectionRegistry(authenticationService,
                Math.Max(60_000L, authSessionLifetimeMilliseconds));
            verbose = verboseLogging;
            CreateDriver();
            NetworkEndpoint endpoint = address == "127.0.0.1" || address.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                ? NetworkEndpoint.LoopbackIpv4.WithPort(port) : NetworkEndpoint.AnyIpv4.WithPort(port);
            int bind = driver.Bind(endpoint);
            if (bind != 0 || driver.Listen() != 0)
            {
                Shutdown();
                return false;
            }
            ConnectionState = NetworkConnectionState.Listening;
            Log($"[M17][Lobby] Service listening {address}:{port}");
            return true;
        }

        public bool StartClient(string hostAddress, ushort hostPort, float timeoutSeconds,
            string developmentCredential, bool verboseLogging)
        {
            Shutdown();
            if (string.IsNullOrWhiteSpace(developmentCredential)) return false;
            role = LobbyNetworkRole.Client;
            address = string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress.Trim();
            port = hostPort;
            handshakeTimeout = Mathf.Clamp(timeoutSeconds, 5f, 20f);
            credential = developmentCredential.Trim();
            verbose = verboseLogging;
            AuthenticationState = AuthenticationClientState.CredentialReady;
            CreateDriver();
            if (!NetworkEndpoint.TryParse(address, port, out NetworkEndpoint endpoint))
            {
                Shutdown();
                return false;
            }
            clientConnection = driver.Connect(endpoint);
            ConnectionState = NetworkConnectionState.Connecting;
            return clientConnection.IsCreated;
        }

        public void Tick(float deltaTime)
        {
            if (!driver.IsCreated) return;
            driver.ScheduleUpdate().Complete();
            if (role == LobbyNetworkRole.Service) TickService(Mathf.Max(0f, deltaTime));
            else if (role == LobbyNetworkRole.Client) TickClient(Mathf.Max(0f, deltaTime));
        }

        public bool CreateMatch(CreateMatchRequest request) => SendClient(LobbyWireMessageType.CreateMatch, request);
        public bool ListMatches(ListMatchesRequest request) => SendClient(LobbyWireMessageType.ListMatches, request);
        public bool JoinMatch(LobbyMatchRequest request) => SendClient(LobbyWireMessageType.JoinMatch, request);
        public bool LeaveMatch(LobbyMatchRequest request) => SendClient(LobbyWireMessageType.LeaveMatch, request);
        public bool SetReady(SetReadyRequest request) => SendClient(LobbyWireMessageType.SetReady, request);
        public bool StartMatch(LobbyMatchRequest request) => SendClient(LobbyWireMessageType.StartMatch, request);
        public bool GetMatch(LobbyMatchRequest request) => SendClient(LobbyWireMessageType.GetMatch, request);
        public bool CloseMatch(LobbyMatchRequest request) => SendClient(LobbyWireMessageType.CloseMatch, request);

        private void TickService(float deltaTime)
        {
            NetworkConnection accepted;
            while ((accepted = driver.Accept()) != default)
            {
                if (peers.Count >= maximumConnections)
                {
                    accepted.Disconnect(driver);
                    continue;
                }
                peers.Add(new ServicePeer { Connection = accepted, AcceptedAt = Now() });
            }
            for (int index = peers.Count - 1; index >= 0; index--)
            {
                ServicePeer peer = peers[index];
                peer.RateElapsed += deltaTime;
                if (peer.RateElapsed >= 1f) { peer.RateElapsed = 0f; peer.Operations = 0; }
                if (!peer.Authenticated && Now() - peer.AcceptedAt > handshakeTimeout * 1000f)
                {
                    RemovePeer(peer, "Authentication timeout");
                    continue;
                }
                NetworkEvent.Type type;
                while ((type = peer.Connection.PopEvent(driver, out DataStreamReader reader)) != NetworkEvent.Type.Empty)
                {
                    if (type == NetworkEvent.Type.Data) ReceiveService(peer, reader);
                    else if (type == NetworkEvent.Type.Disconnect) { RemovePeer(peer, "Disconnected"); break; }
                }
            }
            service?.CleanupClosedMatches(Now(), 30_000L);
        }

        private void TickClient(float deltaTime)
        {
            clientElapsed += deltaTime;
            if (!clientConnection.IsCreated) return;
            NetworkEvent.Type type;
            while ((type = clientConnection.PopEvent(driver, out DataStreamReader reader)) != NetworkEvent.Type.Empty)
            {
                if (type == NetworkEvent.Type.Connect)
                {
                    ConnectionState = NetworkConnectionState.Handshaking;
                    AuthenticationState = AuthenticationClientState.Authenticating;
                    SendClient(LobbyWireMessageType.AuthRequest,
                        new AuthRequestMessage(credential, Guid.NewGuid().ToString("N")), true);
                }
                else if (type == NetworkEvent.Type.Data) ReceiveClient(reader);
                else if (type == NetworkEvent.Type.Disconnect)
                {
                    ConnectionState = NetworkConnectionState.Disconnected;
                    AuthenticationState = AuthenticationClientState.Disconnected;
                    Disconnected?.Invoke("Lobby service disconnected");
                }
            }
            if (ConnectionState is NetworkConnectionState.Connecting or NetworkConnectionState.Handshaking
                && clientElapsed >= handshakeTimeout)
            {
                ConnectionState = NetworkConnectionState.Failed;
                Disconnected?.Invoke("Lobby handshake timeout");
            }
        }

        private void ReceiveService(ServicePeer peer, DataStreamReader reader)
        {
            if (!TryReadEnvelope(reader, out LobbyNetworkEnvelope envelope)
                || envelope.ProtocolVersion != LobbyProtocol.CurrentVersion
                || !peer.Sequence.TryAccept(envelope.Sequence)
                || !LobbyNetworkRules.IsAllowedFromClient(envelope.MessageType)) return;
            if (!peer.Authenticated && envelope.MessageType != LobbyWireMessageType.AuthRequest) return;
            if (envelope.MessageType == LobbyWireMessageType.AuthRequest)
            {
                AuthenticationBindingResult result = authRegistry.Authenticate(peer.Connection.GetHashCode(),
                    serializer.Deserialize<AuthRequestMessage>(envelope.Payload).Credential, Now());
                if (!result.Accepted)
                {
                    SendService(peer, LobbyWireMessageType.AuthRejected,
                        new AuthRejectedMessage(result.Reason, result.Reason.ToString()));
                    return;
                }
                peer.Authenticated = true;
                peer.Session = new LobbyPlayerSession(result.Session.AccountId, result.Session.SessionId,
                    result.Session.ExpiresAtMilliseconds);
                SendService(peer, LobbyWireMessageType.AuthAccepted,
                    new AuthAcceptedMessage(result.Session.AccountId, result.Session.SessionId,
                        result.Session.ExpiresAtMilliseconds));
                return;
            }
            if (!authRegistry.TryGetConnection(peer.Connection.GetHashCode(), Now(),
                    out AuthenticatedPlayerSession activeSession))
            {
                Reject(peer, string.Empty, LobbyRejectReason.SessionRevoked);
                return;
            }
            peer.Session = new LobbyPlayerSession(activeSession.AccountId, activeSession.SessionId,
                activeSession.ExpiresAtMilliseconds);
            peer.Operations++;
            if (peer.Operations > 12)
            {
                Reject(peer, string.Empty, LobbyRejectReason.RateLimited);
                return;
            }
            DispatchService(peer, envelope);
        }

        private void DispatchService(ServicePeer peer, LobbyNetworkEnvelope envelope)
        {
            long now = Now();
            switch (envelope.MessageType)
            {
                case LobbyWireMessageType.CreateMatch:
                {
                    CreateMatchRequest request = serializer.Deserialize<CreateMatchRequest>(envelope.Payload);
                    LobbyOperationResult<LobbyMatchSnapshot> result = service.CreateMatch(peer.Session, request, now);
                    ReplyAndBroadcast(peer, request.RequestId, LobbyEventType.MatchCreated, result);
                    break;
                }
                case LobbyWireMessageType.ListMatches:
                {
                    ListMatchesRequest request = serializer.Deserialize<ListMatchesRequest>(envelope.Payload);
                    LobbyOperationResult<LobbyMatchSnapshot[]> result = service.ListMatches(peer.Session, request, now);
                    if (result.Accepted) SendService(peer, LobbyWireMessageType.MatchList,
                        new LobbyMatchListMessage(request.RequestId, Sanitize(result.Value)));
                    else Reject(peer, request.RequestId, result.Reason);
                    break;
                }
                case LobbyWireMessageType.JoinMatch:
                case LobbyWireMessageType.LeaveMatch:
                case LobbyWireMessageType.GetMatch:
                case LobbyWireMessageType.CloseMatch:
                {
                    LobbyMatchRequest request = serializer.Deserialize<LobbyMatchRequest>(envelope.Payload);
                    LobbyOperationResult<LobbyMatchSnapshot> result = envelope.MessageType switch
                    {
                        LobbyWireMessageType.JoinMatch => service.JoinMatch(peer.Session, request, now),
                        LobbyWireMessageType.LeaveMatch => service.LeaveMatch(peer.Session, request, now),
                        LobbyWireMessageType.CloseMatch => service.CloseMatch(peer.Session, request, now),
                        _ => service.GetMatch(peer.Session, request, now)
                    };
                    LobbyEventType eventType = envelope.MessageType switch
                    {
                        LobbyWireMessageType.JoinMatch => LobbyEventType.MemberJoined,
                        LobbyWireMessageType.LeaveMatch => LobbyEventType.MemberLeft,
                        LobbyWireMessageType.CloseMatch => LobbyEventType.MatchClosed,
                        _ => LobbyEventType.MatchUpdated
                    };
                    ReplyAndBroadcast(peer, request.RequestId, eventType, result);
                    break;
                }
                case LobbyWireMessageType.SetReady:
                {
                    SetReadyRequest request = serializer.Deserialize<SetReadyRequest>(envelope.Payload);
                    ReplyAndBroadcast(peer, request.RequestId, LobbyEventType.ReadyChanged,
                        service.SetReady(peer.Session, request, now));
                    break;
                }
                case LobbyWireMessageType.StartMatch:
                {
                    LobbyMatchRequest request = serializer.Deserialize<LobbyMatchRequest>(envelope.Payload);
                    LobbyOperationResult<MatchReservation> result = service.StartMatch(peer.Session, request, now);
                    if (!result.Accepted) { Reject(peer, request.RequestId, result.Reason); break; }
                    LobbyOperationResult<LobbyMatchSnapshot> updated = service.GetMatch(peer.Session,
                        new LobbyMatchRequest(Guid.NewGuid().ToString("N"), request.LobbyMatchId), now);
                    if (updated.Accepted) BroadcastUpdate(request.RequestId, LobbyEventType.MatchStarting, updated.Value);
                    foreach (ServicePeer memberPeer in peers)
                    {
                        if (!memberPeer.Authenticated
                            || !result.Value.TryGetGrant(memberPeer.Session.PlayerAccountId, out MatchAdmissionGrant grant)) continue;
                        SendService(memberPeer, LobbyWireMessageType.AdmissionGranted,
                            new LobbyAdmissionGrantedMessage(request.RequestId, grant));
                    }
                    break;
                }
                case LobbyWireMessageType.Ping:
                    SendService(peer, LobbyWireMessageType.Pong, envelope.Payload);
                    break;
                case LobbyWireMessageType.Disconnect:
                    RemovePeer(peer, "Client requested disconnect");
                    break;
            }
        }

        private void ReplyAndBroadcast(ServicePeer peer, string requestId, LobbyEventType eventType,
            LobbyOperationResult<LobbyMatchSnapshot> result)
        {
            if (!result.Accepted) { Reject(peer, requestId, result.Reason); return; }
            BroadcastUpdate(requestId, eventType, result.Value);
        }

        private void BroadcastUpdate(string requestId, LobbyEventType eventType, LobbyMatchSnapshot match)
        {
            LobbyMatchUpdatedMessage message = new(requestId, eventType, Sanitize(match));
            foreach (ServicePeer peer in peers)
                if (peer.Authenticated) SendService(peer, LobbyWireMessageType.MatchUpdated, message);
        }

        private void ReceiveClient(DataStreamReader reader)
        {
            if (!TryReadEnvelope(reader, out LobbyNetworkEnvelope envelope)
                || envelope.ProtocolVersion != LobbyProtocol.CurrentVersion
                || !clientInboundSequence.TryAccept(envelope.Sequence)
                || !LobbyNetworkRules.IsAllowedFromService(envelope.MessageType)) return;
            switch (envelope.MessageType)
            {
                case LobbyWireMessageType.AuthAccepted:
                {
                    AuthAcceptedMessage value = serializer.Deserialize<AuthAcceptedMessage>(envelope.Payload);
                    ClientSession = new LobbyPlayerSession(value.PlayerAccountId, value.AuthSessionId,
                        value.SessionExpiryUnixMilliseconds);
                    AuthenticationState = AuthenticationClientState.Authenticated;
                    ConnectionState = NetworkConnectionState.Connected;
                    AuthenticationAccepted?.Invoke(value);
                    break;
                }
                case LobbyWireMessageType.AuthRejected:
                {
                    AuthRejectedMessage value = serializer.Deserialize<AuthRejectedMessage>(envelope.Payload);
                    AuthenticationState = AuthenticationClientState.Rejected;
                    AuthenticationRejected?.Invoke(value);
                    break;
                }
                case LobbyWireMessageType.MatchList:
                {
                    LobbyMatchListMessage value = serializer.Deserialize<LobbyMatchListMessage>(envelope.Payload);
                    LatestList = value.Matches;
                    MatchListReceived?.Invoke(value);
                    break;
                }
                case LobbyWireMessageType.MatchUpdated:
                {
                    LobbyMatchUpdatedMessage value = serializer.Deserialize<LobbyMatchUpdatedMessage>(envelope.Payload);
                    if (value.Match != null && snapshotStore.TryApply(value.Match)) LatestMatch = value.Match;
                    MatchUpdated?.Invoke(value);
                    break;
                }
                case LobbyWireMessageType.AdmissionGranted:
                {
                    LobbyAdmissionGrantedMessage value = serializer.Deserialize<LobbyAdmissionGrantedMessage>(envelope.Payload);
                    AdmissionGranted?.Invoke(value);
                    break;
                }
                case LobbyWireMessageType.OperationRejected:
                {
                    LobbyOperationRejectedMessage value = serializer.Deserialize<LobbyOperationRejectedMessage>(envelope.Payload);
                    LastRejection = value.Reason;
                    OperationRejected?.Invoke(value);
                    break;
                }
                case LobbyWireMessageType.Ping:
                    SendClient(LobbyWireMessageType.Pong, envelope.Payload, true);
                    break;
                case LobbyWireMessageType.Disconnect:
                    ConnectionState = NetworkConnectionState.Disconnected;
                    Disconnected?.Invoke("Lobby service closed connection");
                    break;
            }
        }

        private void Reject(ServicePeer peer, string requestId, LobbyRejectReason reason) =>
            SendService(peer, LobbyWireMessageType.OperationRejected,
                new LobbyOperationRejectedMessage(requestId, reason));

        private void RemovePeer(ServicePeer peer, string reason)
        {
            if (!peers.Remove(peer)) return;
            authRegistry?.RemoveConnection(peer.Connection.GetHashCode());
            if (peer.Authenticated && service.Disconnect(peer.Session, Now(), out LobbyMatchSnapshot changed))
                BroadcastUpdate(string.Empty, changed.State == LobbyMatchState.Closed
                    ? LobbyEventType.MatchClosed : LobbyEventType.MemberLeft, changed);
            if (peer.Connection.IsCreated) peer.Connection.Disconnect(driver);
            Log($"[M17][Lobby] Peer removed: {reason}");
        }

        private bool SendClient<T>(LobbyWireMessageType type, T payload, bool allowBeforeAuth = false) =>
            SendClient(type, serializer.Serialize(payload), allowBeforeAuth);

        private bool SendClient(LobbyWireMessageType type, string payload, bool allowBeforeAuth = false)
        {
            if (role != LobbyNetworkRole.Client || !clientConnection.IsCreated
                || !allowBeforeAuth && AuthenticationState != AuthenticationClientState.Authenticated) return false;
            return Send(clientConnection, new LobbyNetworkEnvelope(type, ++clientOutboundSequence, payload));
        }

        private bool SendService<T>(ServicePeer peer, LobbyWireMessageType type, T payload) =>
            SendService(peer, type, serializer.Serialize(payload));

        private bool SendService(ServicePeer peer, LobbyWireMessageType type, string payload) =>
            peer != null && Send(peer.Connection, new LobbyNetworkEnvelope(type, ++peer.OutboundSequence, payload));

        private bool Send(NetworkConnection connection, LobbyNetworkEnvelope envelope)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(serializer.Serialize(envelope));
            if (bytes.Length > OnlineProtocol.MaximumPayloadBytes) return false;
            NativeArray<byte> native = new(bytes, Allocator.Temp);
            int begin = driver.BeginSend(pipeline, connection, out DataStreamWriter writer, bytes.Length);
            if (begin != 0) { native.Dispose(); return false; }
            bool wrote = writer.WriteBytes(native);
            native.Dispose();
            if (!wrote) { driver.AbortSend(writer); return false; }
            if (driver.EndSend(writer) < 0) return false;
            SentBytes += bytes.Length;
            return true;
        }

        private bool TryReadEnvelope(DataStreamReader reader, out LobbyNetworkEnvelope envelope)
        {
            envelope = default;
            if (reader.Length <= 0 || reader.Length > OnlineProtocol.MaximumPayloadBytes) return false;
            byte[] bytes = new byte[reader.Length];
            reader.ReadBytes(bytes);
            ReceivedBytes += bytes.Length;
            try { envelope = serializer.Deserialize<LobbyNetworkEnvelope>(Encoding.UTF8.GetString(bytes)); return true; }
            catch (Exception) { return false; }
        }

        private void CreateDriver()
        {
            NetworkSettings settings = new();
            settings.WithNetworkConfigParameters(connectTimeoutMS: 1000,
                maxConnectAttempts: Mathf.CeilToInt(handshakeTimeout), disconnectTimeoutMS: 30_000,
                heartbeatTimeoutMS: 500, receiveQueueCapacity: 256, sendQueueCapacity: 256);
            settings.WithFragmentationStageParameters(OnlineProtocol.MaximumPayloadBytes);
            driver = NetworkDriver.Create(new WebSocketNetworkInterface(), settings);
            pipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        }

        public void Shutdown()
        {
            if (driver.IsCreated)
            {
                if (clientConnection.IsCreated) clientConnection.Disconnect(driver);
                foreach (ServicePeer peer in peers)
                    if (peer.Connection.IsCreated) peer.Connection.Disconnect(driver);
                driver.ScheduleUpdate().Complete();
                driver.Dispose();
            }
            peers.Clear();
            authRegistry?.Reset();
            service = null;
            clientConnection = default;
            clientInboundSequence.Reset();
            snapshotStore.Reset();
            role = LobbyNetworkRole.None;
            ConnectionState = NetworkConnectionState.Offline;
            AuthenticationState = string.IsNullOrWhiteSpace(credential)
                ? AuthenticationClientState.None : AuthenticationClientState.CredentialReady;
            ClientSession = default;
            LatestMatch = null;
            LatestList = Array.Empty<LobbyMatchSnapshot>();
            clientOutboundSequence = 0;
            clientElapsed = 0f;
        }

        private static LobbyMatchSnapshot[] Sanitize(LobbyMatchSnapshot[] values)
        {
            LobbyMatchSnapshot[] result = new LobbyMatchSnapshot[values?.Length ?? 0];
            for (int index = 0; index < result.Length; index++) result[index] = Sanitize(values[index]);
            return result;
        }

        private static LobbyMatchSnapshot Sanitize(LobbyMatchSnapshot value)
        {
            if (value == null) return null;
            LobbyMatchMember[] members = new LobbyMatchMember[value.Members.Length];
            for (int index = 0; index < members.Length; index++)
            {
                LobbyMatchMember member = value.Members[index];
                members[index] = new LobbyMatchMember(default, member.DisplayAlias, member.SlotIndex,
                    member.ReadyState, member.IsOwner);
            }
            return new LobbyMatchSnapshot(value.LobbyMatchId, value.DisplayName, value.MaxPlayers,
                value.State, value.HoleId, value.CreatedAtUnixMilliseconds, value.Visibility,
                value.Joinable, value.Revision, value.GameMatchId, members);
        }

        private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private void Log(string message) { if (verbose) Debug.Log(message, this); }
    }
}

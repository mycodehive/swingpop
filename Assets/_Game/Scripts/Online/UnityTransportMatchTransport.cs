using System;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

namespace SwingPop.Online
{
    /// <summary>
    /// M13 localhost prototype adapter. The host alone owns MatchAuthorityCore; clients submit
    /// commands and may play approved shots visually, but their result messages never mutate authority.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityTransportMatchTransport : MonoBehaviour, IMatchTransport
    {
        private static readonly MatchPlayerId RemotePlayer = new("player-b");

        [SerializeField] private LocalMatchAuthority authority;
        [SerializeField] private bool verboseLogging;

        private readonly JsonMatchMessageSerializer serializer = new();
        private readonly NetworkSequenceGuard inboundSequence = new();
        private readonly ConnectionPlayerRegistry playerRegistry = new();
        private readonly NetworkConnectionStateMachine connectionState = new();
        private readonly NetworkDesyncTelemetry desyncTelemetry = new();
        private NetworkDriver driver;
        private NetworkPipeline reliablePipeline;
        private NetworkConnection connection;
        private NetworkRole role;
        private MatchPlayerId assignedPlayer;
        private string address = "127.0.0.1";
        private ushort port = 7777;
        private float timeoutSeconds = 8f;
        private float stateElapsed;
        private float pingElapsed;
        private float rateWindowElapsed;
        private int submissionsInWindow;
        private long outboundSequence;
        private long sentBytes;
        private long receivedBytes;
        private string remoteSnapshotHash = string.Empty;

        public event Action<ApprovedShot> ShotApprovedReceived;
        public event Action<ShotRejection> ShotRejectedReceived;
        public event Action<MatchSnapshot> SnapshotReceived;
        public event Action<MatchPlayerId> PlayerAssigned;
        public event Action RemotePlayerReady;
        public event Action<TurnChangedMessage> TurnChangedReceived;
        public event Action<string> Disconnected;

        public int PendingMessageCount => 0;
        public int MessageCount { get; private set; }
        public int LastShotSubmissionBytes { get; private set; }
        public int LastSnapshotBytes { get; private set; }
        public NetworkRole Role => role;
        public NetworkConnectionState ConnectionState => connectionState.State;
        public MatchPlayerId AssignedPlayer => assignedPlayer;
        public string Address => address;
        public ushort Port => port;
        public long SentBytes => sentBytes;
        public long ReceivedBytes => receivedBytes;
        public float RoundTripTimeMilliseconds { get; private set; }
        public ShotRejectReason LastRejectionReason { get; private set; }
        public int RejectedMessageCount { get; private set; }
        public int DesyncCount { get; private set; }
        public NetworkDesyncReport LastDesyncReport => desyncTelemetry.LastReport;
        public string LocalSnapshotHash { get; private set; } = string.Empty;
        public string RemoteSnapshotHash => remoteSnapshotHash;
        public long RemoteSnapshotVersion { get; private set; } = -1;
        public long OutboundSequence => outboundSequence;
        public long InboundSequence => inboundSequence.LastAcceptedSequence;
        public bool IsCreated => driver.IsCreated;
        public bool IsReady => connectionState.State == NetworkConnectionState.InMatch;

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            CancelPending();
        }

        public void Configure(LocalMatchAuthority matchAuthority, bool verbose)
        {
            authority = matchAuthority;
            verboseLogging = verbose;
        }

        public bool StartHost(string bindAddress, ushort networkPort, float connectionTimeoutSeconds)
        {
            ShutdownInternal(false);
            role = NetworkRole.Host;
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
                Fail($"Host bind/listen failed ({bindResult}) on {address}:{port}.");
                return false;
            }

            connectionState.TryTransition(NetworkConnectionState.Listening);
            Log($"HOST LISTENING {address}:{port}");
            return true;
        }

        public bool StartClient(string hostAddress, ushort networkPort, float connectionTimeoutSeconds)
        {
            ShutdownInternal(false);
            role = NetworkRole.Client;
            address = string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress.Trim();
            port = networkPort;
            timeoutSeconds = Mathf.Clamp(connectionTimeoutSeconds, 5f, 10f);
            connectionState.TryTransition(NetworkConnectionState.Starting);
            CreateDriver();
            if (!NetworkEndpoint.TryParse(address, port, out NetworkEndpoint endpoint))
            {
                Fail($"Invalid client endpoint {address}:{port}.");
                return false;
            }

            connection = driver.Connect(endpoint);
            connectionState.TryTransition(NetworkConnectionState.Connecting);
            Log($"CLIENT CONNECTING {address}:{port}");
            return connection.IsCreated;
        }

        public void BeginHostedMatch(MatchSnapshot initialSnapshot)
        {
            if (role != NetworkRole.Host || connectionState.State != NetworkConnectionState.Connected || initialSnapshot == null)
                return;
            connectionState.TryTransition(NetworkConnectionState.InMatch);
            Send(NetworkMessageType.MatchStarted, initialSnapshot.MatchId, serializer.Serialize(initialSnapshot));
            PublishSnapshot(initialSnapshot);
        }

        public void ConfigureLatency(int milliseconds)
        {
            // Real transport latency is observed, never simulated by this adapter.
        }

        public bool SubmitShot(ShotSubmission submission)
        {
            string payload = serializer.Serialize(submission);
            LastShotSubmissionBytes = Encoding.UTF8.GetByteCount(payload);
            if (role == NetworkRole.Client)
                return IsReady && Send(NetworkMessageType.ShotSubmission, submission.MatchId, payload);
            if (role != NetworkRole.Host || !IsReady || authority == null) return false;
            ProcessAuthoritativeSubmission(submission, true);
            return true;
        }

        public bool SubmitShotResult(NetworkShotResult result)
        {
            if (role == NetworkRole.Client)
                return IsReady && Send(NetworkMessageType.PredictedShotResult, result.MatchId, serializer.Serialize(result));
            if (role != NetworkRole.Host || authority == null || !authority.ResolveShot(result)) return false;
            CompareAuthoritativeResult(result);
            PublishSnapshot(authority.CurrentSnapshot);
            Send(NetworkMessageType.TurnChanged, result.MatchId,
                serializer.Serialize(new TurnChangedMessage(authority.CurrentSnapshot)));
            return true;
        }

        public void PublishSnapshot(MatchSnapshot snapshot)
        {
            if (snapshot == null) return;
            string payload = serializer.Serialize(snapshot);
            LastSnapshotBytes = Encoding.UTF8.GetByteCount(payload);
            LocalSnapshotHash = MatchSnapshotHash.Compute(snapshot);
            SnapshotReceived?.Invoke(snapshot);
            if (role == NetworkRole.Host && connection.IsCreated)
                Send(NetworkMessageType.Snapshot, snapshot.MatchId, payload);
        }

        public void Tick(float deltaTime)
        {
            if (!driver.IsCreated) return;
            driver.ScheduleUpdate().Complete();
            float safeDelta = Mathf.Max(0f, deltaTime);
            stateElapsed += safeDelta;
            rateWindowElapsed += safeDelta;
            pingElapsed += safeDelta;
            if (rateWindowElapsed >= 1f)
            {
                rateWindowElapsed = 0f;
                submissionsInWindow = 0;
            }

            if (role == NetworkRole.Host) PollHost();
            else if (role == NetworkRole.Client) PollClient();

            if (connectionState.State is NetworkConnectionState.Connecting or NetworkConnectionState.Handshaking
                && stateElapsed >= timeoutSeconds)
            {
                Fail($"Connection handshake timed out after {timeoutSeconds:F1}s.");
                return;
            }

            if (connection.IsCreated && connectionState.State is NetworkConnectionState.Connected or NetworkConnectionState.InMatch
                && pingElapsed >= 1f)
            {
                pingElapsed = 0f;
                Send(NetworkMessageType.Ping, default, serializer.Serialize(new PingMessage(NowMilliseconds())));
            }
        }

        public void CancelPending()
        {
            ShutdownInternal(true);
        }

        public void RecordRemoteSnapshotHash(long snapshotVersion, string hash)
        {
            remoteSnapshotHash = hash ?? string.Empty;
            RemoteSnapshotVersion = snapshotVersion;
            MatchSnapshot current = authority != null ? authority.CurrentSnapshot : null;
            if (current != null && current.Version == snapshotVersion
                && !string.IsNullOrEmpty(LocalSnapshotHash) && !string.IsNullOrEmpty(remoteSnapshotHash)
                && !string.Equals(LocalSnapshotHash, remoteSnapshotHash, StringComparison.Ordinal))
                DesyncCount++;
        }

        private void CreateDriver()
        {
            NetworkSettings networkSettings = new();
            networkSettings.WithNetworkConfigParameters(
                connectTimeoutMS: 1000,
                maxConnectAttempts: Mathf.CeilToInt(timeoutSeconds),
                disconnectTimeoutMS: Mathf.RoundToInt(timeoutSeconds * 1000f),
                heartbeatTimeoutMS: 500,
                receiveQueueCapacity: 128,
                sendQueueCapacity: 128);
            networkSettings.WithFragmentationStageParameters(OnlineProtocol.MaximumPayloadBytes);
            driver = NetworkDriver.Create(networkSettings);
            reliablePipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        }

        private void PollHost()
        {
            NetworkConnection accepted;
            while ((accepted = driver.Accept()) != default)
            {
                if (connection.IsCreated)
                {
                    accepted.Disconnect(driver);
                    Log("Late/additional connection rejected; M13 supports one client only.");
                    continue;
                }
                connection = accepted;
                stateElapsed = 0f;
                connectionState.TryTransition(NetworkConnectionState.Handshaking);
                Log("HOST ACCEPTED CLIENT; awaiting ClientHello.");
            }
            PollConnectionEvents();
        }

        private void PollClient()
        {
            PollConnectionEvents();
        }

        private void PollConnectionEvents()
        {
            if (!connection.IsCreated) return;
            NetworkEvent.Type eventType;
            while ((eventType = connection.PopEvent(driver, out DataStreamReader reader)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Connect:
                        stateElapsed = 0f;
                        connectionState.TryTransition(NetworkConnectionState.Handshaking);
                        if (role == NetworkRole.Client)
                            Send(NetworkMessageType.ClientHello, default,
                                serializer.Serialize(new ClientHelloMessage(Application.version)));
                        break;
                    case NetworkEvent.Type.Data:
                        Receive(reader);
                        break;
                    case NetworkEvent.Type.Disconnect:
                        HandleDisconnect("Remote peer disconnected.");
                        break;
                }
            }
        }

        private void Receive(DataStreamReader reader)
        {
            if (!NetworkMessageRules.IsPayloadSizeValid(reader.Length))
            {
                RejectMalformed(ShotRejectReason.PayloadTooLarge);
                return;
            }
            byte[] bytes = new byte[reader.Length];
            reader.ReadBytes(bytes);
            receivedBytes += bytes.Length;
            MessageCount++;
            NetworkMessageEnvelope envelope;
            try
            {
                envelope = serializer.Deserialize<NetworkMessageEnvelope>(Encoding.UTF8.GetString(bytes));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[M13][Transport] Malformed envelope: {exception.Message}", this);
                RejectMalformed(ShotRejectReason.InvalidCommand);
                return;
            }

            ShotRejectReason ruleResult = NetworkMessageRules.ValidateEnvelope(envelope, inboundSequence);
            if (ruleResult != ShotRejectReason.None)
            {
                RejectMalformed(ruleResult);
                return;
            }
            Dispatch(envelope);
        }

        private void Dispatch(NetworkMessageEnvelope envelope)
        {
            switch (envelope.MessageType)
            {
                case NetworkMessageType.ClientHello when role == NetworkRole.Host:
                    HandleClientHello();
                    break;
                case NetworkMessageType.PlayerAssigned when role == NetworkRole.Client:
                    assignedPlayer = serializer.Deserialize<PlayerAssignedMessage>(envelope.Payload).PlayerId;
                    connectionState.TryTransition(NetworkConnectionState.Connected);
                    stateElapsed = 0f;
                    PlayerAssigned?.Invoke(assignedPlayer);
                    break;
                case NetworkMessageType.MatchStarted when role == NetworkRole.Client:
                {
                    connectionState.TryTransition(NetworkConnectionState.InMatch);
                    MatchSnapshot initial = serializer.Deserialize<MatchSnapshot>(envelope.Payload);
                    LocalSnapshotHash = MatchSnapshotHash.Compute(initial);
                    SnapshotReceived?.Invoke(initial);
                    SendSnapshotHash(initial);
                    break;
                }
                case NetworkMessageType.ShotSubmission when role == NetworkRole.Host:
                    HandleRemoteSubmission(serializer.Deserialize<ShotSubmission>(envelope.Payload));
                    break;
                case NetworkMessageType.ShotApproved:
                    ShotApprovedReceived?.Invoke(serializer.Deserialize<ApprovedShot>(envelope.Payload));
                    break;
                case NetworkMessageType.ShotRejected:
                {
                    ShotRejection rejection = serializer.Deserialize<ShotRejection>(envelope.Payload);
                    LastRejectionReason = rejection.Reason;
                    ShotRejectedReceived?.Invoke(rejection);
                    break;
                }
                case NetworkMessageType.Snapshot when role == NetworkRole.Client:
                {
                    MatchSnapshot snapshot = serializer.Deserialize<MatchSnapshot>(envelope.Payload);
                    LocalSnapshotHash = MatchSnapshotHash.Compute(snapshot);
                    SnapshotReceived?.Invoke(snapshot);
                    SendSnapshotHash(snapshot);
                    break;
                }
                case NetworkMessageType.SnapshotHash when role == NetworkRole.Host:
                {
                    SnapshotHashMessage hash = serializer.Deserialize<SnapshotHashMessage>(envelope.Payload);
                    RecordRemoteSnapshotHash(hash.SnapshotVersion, hash.Hash);
                    break;
                }
                case NetworkMessageType.TurnChanged:
                    TurnChangedReceived?.Invoke(serializer.Deserialize<TurnChangedMessage>(envelope.Payload));
                    break;
                case NetworkMessageType.PredictedShotResult when role == NetworkRole.Host:
                    // Diagnostic only. The host deliberately ignores client result authority.
                    if (desyncTelemetry.RecordPredicted(serializer.Deserialize<NetworkShotResult>(envelope.Payload)))
                        ReportDesync();
                    break;
                case NetworkMessageType.Ping:
                    Send(NetworkMessageType.Pong, envelope.MatchId, envelope.Payload);
                    break;
                case NetworkMessageType.Pong:
                {
                    PingMessage pong = serializer.Deserialize<PingMessage>(envelope.Payload);
                    RoundTripTimeMilliseconds = Mathf.Max(0f, NowMilliseconds() - pong.TimestampMilliseconds);
                    break;
                }
                case NetworkMessageType.DisconnectNotice:
                    HandleDisconnect(serializer.Deserialize<DisconnectNoticeMessage>(envelope.Payload).Reason);
                    break;
                case NetworkMessageType.ConnectionRejected when role == NetworkRole.Client:
                {
                    ConnectionRejectedMessage rejection = serializer.Deserialize<ConnectionRejectedMessage>(envelope.Payload);
                    LastRejectionReason = rejection.Reason;
                    RejectedMessageCount++;
                    // Capacity/protocol refusal is an expected handshake outcome, not a runtime fault.
                    Fail(string.IsNullOrWhiteSpace(rejection.Detail) ? rejection.Reason.ToString() : rejection.Detail, false);
                    break;
                }
            }
        }

        private void HandleClientHello()
        {
            if (connectionState.State != NetworkConnectionState.Handshaking) return;
            int connectionId = connection.GetHashCode();
            if (!playerRegistry.TryBind(connectionId, RemotePlayer))
            {
                RejectMalformed(ShotRejectReason.UnknownPlayer);
                return;
            }
            assignedPlayer = RemotePlayer;
            Send(NetworkMessageType.PlayerAssigned, default, serializer.Serialize(new PlayerAssignedMessage(RemotePlayer)));
            connectionState.TryTransition(NetworkConnectionState.Connected);
            stateElapsed = 0f;
            RemotePlayerReady?.Invoke();
        }

        private void SendSnapshotHash(MatchSnapshot snapshot)
        {
            Send(NetworkMessageType.SnapshotHash, snapshot.MatchId,
                serializer.Serialize(new SnapshotHashMessage(snapshot.Version, LocalSnapshotHash)));
        }

        private void HandleRemoteSubmission(ShotSubmission submission)
        {
            if (!playerRegistry.IsBoundPlayer(connection.GetHashCode(), submission.PlayerId))
            {
                RejectSubmission(submission, ShotRejectReason.PlayerSpoofing);
                return;
            }
            submissionsInWindow++;
            if (submissionsInWindow > 8)
            {
                RejectSubmission(submission, ShotRejectReason.RateLimited);
                return;
            }
            ProcessAuthoritativeSubmission(submission, false);
        }

        private void ProcessAuthoritativeSubmission(ShotSubmission submission, bool localHostSubmission)
        {
            ShotSubmissionDecision decision = authority.SubmitShot(submission);
            if (!decision.Accepted)
            {
                LastRejectionReason = decision.Rejection.Reason;
                RejectedMessageCount++;
                if (localHostSubmission) ShotRejectedReceived?.Invoke(decision.Rejection);
                else Send(NetworkMessageType.ShotRejected, submission.MatchId, serializer.Serialize(decision.Rejection));
                return;
            }

            if (!authority.BeginShotPlayback(decision.Approved))
            {
                RejectSubmission(submission, ShotRejectReason.InvalidTurn);
                return;
            }

            Send(NetworkMessageType.ShotApproved, submission.MatchId, serializer.Serialize(decision.Approved));
            ShotApprovedReceived?.Invoke(decision.Approved);
            PublishSnapshot(authority.CurrentSnapshot);
        }

        private void RejectSubmission(ShotSubmission submission, ShotRejectReason reason)
        {
            LastRejectionReason = reason;
            RejectedMessageCount++;
            ShotRejection rejection = new(submission, reason);
            Send(NetworkMessageType.ShotRejected, submission.MatchId, serializer.Serialize(rejection));
        }

        private void RejectMalformed(ShotRejectReason reason)
        {
            LastRejectionReason = reason;
            RejectedMessageCount++;
            Log($"REJECTED MESSAGE: {reason}");
        }

        private void CompareAuthoritativeResult(NetworkShotResult result)
        {
            if (desyncTelemetry.RecordAuthoritative(result)) ReportDesync();
        }

        private void ReportDesync()
        {
            NetworkDesyncReport report = desyncTelemetry.LastReport;
            if (!report.IsMismatch) return;
            DesyncCount++;
            Debug.LogWarning($"[M13][Desync] position={report.PositionError:F3}m " +
                             $"lie={report.LieMatches} stroke={report.StrokeMatches} penalty={report.PenaltyMatches}", this);
        }

        private bool Send(NetworkMessageType messageType, MatchId matchId, string payload)
        {
            if (!driver.IsCreated || !connection.IsCreated) return false;
            NetworkMessageEnvelope envelope = new(messageType, matchId, ++outboundSequence, payload);
            byte[] bytes = Encoding.UTF8.GetBytes(serializer.Serialize(envelope));
            if (!NetworkMessageRules.IsPayloadSizeValid(bytes.Length))
            {
                LastRejectionReason = ShotRejectReason.PayloadTooLarge;
                RejectedMessageCount++;
                return false;
            }
            NativeArray<byte> nativeBytes = new(bytes, Allocator.Temp);
            int beginResult = driver.BeginSend(reliablePipeline, connection, out DataStreamWriter writer, bytes.Length);
            if (beginResult != 0)
            {
                nativeBytes.Dispose();
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
            if (endResult < 0) return false;
            sentBytes += bytes.Length;
            MessageCount++;
            return true;
        }

        private void HandleDisconnect(string reason)
        {
            bool rejectionAlreadyReported = connectionState.State == NetworkConnectionState.Failed;
            playerRegistry.Clear();
            assignedPlayer = default;
            RemoteSnapshotVersion = -1;
            connection = default;
            if (!rejectionAlreadyReported)
            {
                connectionState.TryTransition(NetworkConnectionState.Disconnected);
                Disconnected?.Invoke(reason ?? "Disconnected");
            }
            Log($"DISCONNECTED: {reason}");
        }

        private void Fail(string reason, bool logAsError = true)
        {
            connectionState.TryTransition(NetworkConnectionState.Failed);
            if (logAsError) Debug.LogError($"[M13][Transport] {reason}", this);
            else Debug.LogWarning($"[M13][Transport] {reason}", this);
            Disconnected?.Invoke(reason);
        }

        private void ShutdownInternal(bool notifyRemote)
        {
            if (driver.IsCreated)
            {
                if (notifyRemote && connection.IsCreated)
                    Send(NetworkMessageType.DisconnectNotice, default,
                        serializer.Serialize(new DisconnectNoticeMessage("Local shutdown")));
                if (connection.IsCreated)
                {
                    connection.Disconnect(driver);
                    // Flush the disconnect datagram before disposing the client driver.
                    driver.ScheduleUpdate().Complete();
                }
                driver.Dispose();
            }
            connection = default;
            playerRegistry.Clear();
            inboundSequence.Reset();
            assignedPlayer = default;
            outboundSequence = 0;
            stateElapsed = 0f;
            pingElapsed = 0f;
            rateWindowElapsed = 0f;
            submissionsInWindow = 0;
            role = NetworkRole.None;
            connectionState.Reset();
        }

        private static long NowMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private void Log(string message)
        {
            if (verboseLogging) Debug.Log($"[M13][Transport] {message}", this);
        }
    }
}

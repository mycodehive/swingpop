using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace SwingPop.Online
{
    [Serializable]
    public struct NetworkMessageEnvelope
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private NetworkMessageType messageType;
        [SerializeField] private MatchId matchId;
        [SerializeField] private long sequence;
        [SerializeField] private string payload;

        public NetworkMessageEnvelope(NetworkMessageType messageType, MatchId matchId, long sequence, string payload)
        {
            protocolVersion = OnlineProtocol.CurrentVersion;
            this.messageType = messageType;
            this.matchId = matchId;
            this.sequence = sequence;
            this.payload = payload ?? string.Empty;
        }

        public int ProtocolVersion => protocolVersion;
        public NetworkMessageType MessageType => messageType;
        public MatchId MatchId => matchId;
        public long Sequence => sequence;
        public string Payload => payload ?? string.Empty;
    }

    [Serializable]
    public struct ClientHelloMessage
    {
        [SerializeField] private string clientBuild;

        public ClientHelloMessage(string clientBuild)
        {
            this.clientBuild = clientBuild ?? string.Empty;
        }

        public string ClientBuild => clientBuild ?? string.Empty;
    }

    [Serializable]
    public struct PlayerAssignedMessage
    {
        [SerializeField] private MatchPlayerId playerId;

        public PlayerAssignedMessage(MatchPlayerId playerId) => this.playerId = playerId;
        public MatchPlayerId PlayerId => playerId;
    }

    [Serializable]
    public struct TurnChangedMessage
    {
        [SerializeField] private MatchPlayerId currentPlayer;
        [SerializeField] private int turnIndex;
        [SerializeField] private long snapshotVersion;

        public TurnChangedMessage(MatchSnapshot snapshot)
        {
            currentPlayer = snapshot != null ? snapshot.CurrentTurnPlayer : default;
            turnIndex = snapshot != null ? snapshot.TurnIndex : -1;
            snapshotVersion = snapshot != null ? snapshot.Version : -1;
        }

        public MatchPlayerId CurrentPlayer => currentPlayer;
        public int TurnIndex => turnIndex;
        public long SnapshotVersion => snapshotVersion;
    }

    [Serializable]
    public struct PingMessage
    {
        [SerializeField] private long timestampMilliseconds;
        public PingMessage(long timestampMilliseconds) => this.timestampMilliseconds = timestampMilliseconds;
        public long TimestampMilliseconds => timestampMilliseconds;
    }

    [Serializable]
    public struct SnapshotHashMessage
    {
        [SerializeField] private long snapshotVersion;
        [SerializeField] private string hash;

        public SnapshotHashMessage(long snapshotVersion, string hash)
        {
            this.snapshotVersion = snapshotVersion;
            this.hash = hash ?? string.Empty;
        }

        public long SnapshotVersion => snapshotVersion;
        public string Hash => hash ?? string.Empty;
    }

    [Serializable]
    public struct DisconnectNoticeMessage
    {
        [SerializeField] private string reason;
        public DisconnectNoticeMessage(string reason) => this.reason = reason ?? string.Empty;
        public string Reason => reason ?? string.Empty;
    }

    public readonly struct NetworkLaunchOptions
    {
        public NetworkLaunchOptions(MultiplayerDevelopmentMode mode, string address, ushort port)
        {
            Mode = mode;
            Address = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address;
            Port = port;
        }

        public MultiplayerDevelopmentMode Mode { get; }
        public string Address { get; }
        public ushort Port { get; }
        public bool HasNetworkOverride => Mode is MultiplayerDevelopmentMode.NetworkHost or MultiplayerDevelopmentMode.NetworkClient;

        public static NetworkLaunchOptions Parse(string[] arguments, string defaultAddress = "127.0.0.1", ushort defaultPort = 7777)
        {
            MultiplayerDevelopmentMode mode = MultiplayerDevelopmentMode.OfflineSingle;
            string address = defaultAddress;
            ushort port = defaultPort;
            if (arguments == null) return new NetworkLaunchOptions(mode, address, port);

            foreach (string argument in arguments)
            {
                if (string.Equals(argument, "-swingpopHost", StringComparison.OrdinalIgnoreCase))
                    mode = MultiplayerDevelopmentMode.NetworkHost;
                else if (string.Equals(argument, "-swingpopClient", StringComparison.OrdinalIgnoreCase))
                    mode = MultiplayerDevelopmentMode.NetworkClient;
                else if (argument != null && argument.StartsWith("-swingpopAddress=", StringComparison.OrdinalIgnoreCase))
                    address = argument.Substring("-swingpopAddress=".Length).Trim();
                else if (argument != null && argument.StartsWith("-swingpopPort=", StringComparison.OrdinalIgnoreCase)
                         && ushort.TryParse(argument.Substring("-swingpopPort=".Length), NumberStyles.None,
                             CultureInfo.InvariantCulture, out ushort parsedPort) && parsedPort > 0)
                    port = parsedPort;
            }

            return new NetworkLaunchOptions(mode, address, port);
        }
    }

    public sealed class NetworkSequenceGuard
    {
        public long LastAcceptedSequence { get; private set; }

        public void Reset() => LastAcceptedSequence = 0;

        public bool TryAccept(long sequence)
        {
            if (sequence <= LastAcceptedSequence || sequence <= 0) return false;
            LastAcceptedSequence = sequence;
            return true;
        }
    }

    public sealed class ConnectionPlayerRegistry
    {
        private readonly Dictionary<int, MatchPlayerId> players = new();

        public int Count => players.Count;

        public bool TryBind(int connectionId, MatchPlayerId playerId)
        {
            if (connectionId < 0 || !playerId.IsValid || players.ContainsKey(connectionId)) return false;
            players.Add(connectionId, playerId);
            return true;
        }

        public bool IsBoundPlayer(int connectionId, MatchPlayerId claimedPlayer)
        {
            return players.TryGetValue(connectionId, out MatchPlayerId bound) && bound == claimedPlayer;
        }

        public bool Remove(int connectionId) => players.Remove(connectionId);
        public void Clear() => players.Clear();
    }

    public sealed class NetworkConnectionStateMachine
    {
        public NetworkConnectionState State { get; private set; } = NetworkConnectionState.Offline;

        public bool TryTransition(NetworkConnectionState next)
        {
            if (!CanTransition(State, next)) return false;
            State = next;
            return true;
        }

        public void Reset() => State = NetworkConnectionState.Offline;

        public static bool CanTransition(NetworkConnectionState current, NetworkConnectionState next)
        {
            if (current == next) return true;
            if (next == NetworkConnectionState.Failed || next == NetworkConnectionState.Disconnected) return true;
            return current switch
            {
                NetworkConnectionState.Offline => next == NetworkConnectionState.Starting,
                NetworkConnectionState.Starting => next is NetworkConnectionState.Listening or NetworkConnectionState.Connecting,
                NetworkConnectionState.Listening => next == NetworkConnectionState.Handshaking,
                NetworkConnectionState.Connecting => next == NetworkConnectionState.Handshaking,
                NetworkConnectionState.Handshaking => next == NetworkConnectionState.Connected,
                NetworkConnectionState.Connected => next is NetworkConnectionState.InMatch or NetworkConnectionState.Disconnecting,
                NetworkConnectionState.InMatch => next == NetworkConnectionState.Disconnecting,
                NetworkConnectionState.Disconnecting => next == NetworkConnectionState.Disconnected,
                NetworkConnectionState.Disconnected => next == NetworkConnectionState.Starting,
                NetworkConnectionState.Failed => next == NetworkConnectionState.Starting,
                _ => false
            };
        }
    }

    public static class NetworkMessageRules
    {
        public static bool IsPayloadSizeValid(int byteCount) => byteCount >= 0 && byteCount <= OnlineProtocol.MaximumPayloadBytes;

        public static ShotRejectReason ValidateEnvelope(NetworkMessageEnvelope envelope, NetworkSequenceGuard sequenceGuard)
        {
            if (envelope.ProtocolVersion != OnlineProtocol.CurrentVersion) return ShotRejectReason.UnsupportedVersion;
            if (Encoding.UTF8.GetByteCount(envelope.Payload) > OnlineProtocol.MaximumPayloadBytes)
                return ShotRejectReason.PayloadTooLarge;
            if (sequenceGuard == null || !sequenceGuard.TryAccept(envelope.Sequence)) return ShotRejectReason.StaleMessage;
            return ShotRejectReason.None;
        }
    }

    public static class MatchSnapshotHash
    {
        public static string Compute(MatchSnapshot snapshot)
        {
            if (snapshot == null) return "0000000000000000";
            string json = JsonUtility.ToJson(snapshot);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            ulong hash = 14695981039346656037UL;
            for (int index = 0; index < bytes.Length; index++)
            {
                hash ^= bytes[index];
                hash *= 1099511628211UL;
            }
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }
    }

    public readonly struct NetworkDesyncReport
    {
        public NetworkDesyncReport(float positionError, bool lieMatches, bool strokeMatches, bool penaltyMatches)
        {
            PositionError = positionError;
            LieMatches = lieMatches;
            StrokeMatches = strokeMatches;
            PenaltyMatches = penaltyMatches;
        }

        public float PositionError { get; }
        public bool LieMatches { get; }
        public bool StrokeMatches { get; }
        public bool PenaltyMatches { get; }
        public bool IsMismatch => PositionError > NetworkDesyncTelemetry.PositionWarningThreshold
                                  || !LieMatches || !StrokeMatches || !PenaltyMatches;
    }

    public sealed class NetworkDesyncTelemetry
    {
        public const float PositionWarningThreshold = 0.25f;
        private bool hasPredicted;
        private bool hasAuthority;
        private NetworkShotResult predicted;
        private NetworkShotResult authoritative;

        public NetworkDesyncReport LastReport { get; private set; }
        public int ComparisonCount { get; private set; }
        public int MismatchCount { get; private set; }

        public bool RecordPredicted(NetworkShotResult result) => Record(result, false);
        public bool RecordAuthoritative(NetworkShotResult result) => Record(result, true);

        private bool Record(NetworkShotResult result, bool isAuthority)
        {
            if (isAuthority)
            {
                authoritative = result;
                hasAuthority = true;
            }
            else
            {
                predicted = result;
                hasPredicted = true;
            }
            if (!hasPredicted || !hasAuthority || predicted.MatchId != authoritative.MatchId
                || predicted.PlayerId != authoritative.PlayerId || predicted.ShotSequence != authoritative.ShotSequence)
                return false;

            LastReport = new NetworkDesyncReport(
                Vector3.Distance(predicted.FinalPosition.ToUnity(), authoritative.FinalPosition.ToUnity()),
                predicted.FinalLie == authoritative.FinalLie,
                predicted.StrokeCount == authoritative.StrokeCount,
                predicted.PenaltyCount == authoritative.PenaltyCount);
            ComparisonCount++;
            if (LastReport.IsMismatch) MismatchCount++;
            hasPredicted = false;
            hasAuthority = false;
            return true;
        }
    }
}

using System;
using UnityEngine;

namespace SwingPop.Online
{
    [Serializable]
    public struct LobbyMatchId : IEquatable<LobbyMatchId>
    {
        [SerializeField] private string value;

        public LobbyMatchId(string value) => this.value = value?.Trim() ?? string.Empty;
        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value) && Value.Length <= 64;
        public bool Equals(LobbyMatchId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LobbyMatchId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(LobbyMatchId left, LobbyMatchId right) => left.Equals(right);
        public static bool operator !=(LobbyMatchId left, LobbyMatchId right) => !left.Equals(right);
    }

    public enum LobbyMatchState
    {
        Created,
        WaitingForPlayers,
        Full,
        Starting,
        InGame,
        Completed,
        Closed
    }

    public enum LobbyVisibility
    {
        Public,
        Private,
        Unlisted
    }

    public enum LobbyReadyState
    {
        NotReady,
        Ready
    }

    public enum LobbyRejectReason
    {
        None,
        AuthenticationRequired,
        SessionExpired,
        SessionRevoked,
        InvalidRequest,
        InvalidDisplayName,
        UnsupportedPlayerCount,
        UnsupportedHole,
        MatchNotFound,
        MatchFull,
        MatchNotJoinable,
        AlreadyInMatch,
        AlreadyMember,
        NotMember,
        NotOwner,
        PlayersNotReady,
        MatchStarting,
        MatchClosed,
        RateLimited,
        DuplicateRequest,
        AllocationFailed,
        AdmissionRejected
    }

    public enum MatchAdmissionRejectReason
    {
        None,
        MissingTicket,
        InvalidTicket,
        UnknownMatch,
        WrongMatch,
        AccountMismatch,
        Expired,
        Consumed,
        PlayerAlreadyConnected
    }

    [Serializable]
    public struct LobbyPlayerSession
    {
        [SerializeField] private PlayerAccountId playerAccountId;
        [SerializeField] private AuthSessionId authSessionId;
        [SerializeField] private long expiresAtUnixMilliseconds;
        [SerializeField] private bool connected;
        [SerializeField] private bool revoked;

        public LobbyPlayerSession(PlayerAccountId playerAccountId, AuthSessionId authSessionId,
            long expiresAtUnixMilliseconds, bool connected = true, bool revoked = false)
        {
            this.playerAccountId = playerAccountId;
            this.authSessionId = authSessionId;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
            this.connected = connected;
            this.revoked = revoked;
        }

        public PlayerAccountId PlayerAccountId => playerAccountId;
        public AuthSessionId AuthSessionId => authSessionId;
        public long ExpiresAtUnixMilliseconds => expiresAtUnixMilliseconds;
        public bool Connected => connected;
        public bool Revoked => revoked;
        public bool IsAuthenticated(long nowMilliseconds) => connected && playerAccountId.IsValid
            && authSessionId.IsValid && !revoked && expiresAtUnixMilliseconds > nowMilliseconds;
    }

    [Serializable]
    public struct LobbyMatchMember
    {
        [SerializeField] private PlayerAccountId accountId;
        [SerializeField] private string displayAlias;
        [SerializeField] private int slotIndex;
        [SerializeField] private LobbyReadyState readyState;
        [SerializeField] private bool owner;

        public LobbyMatchMember(PlayerAccountId accountId, string displayAlias, int slotIndex,
            LobbyReadyState readyState, bool owner)
        {
            this.accountId = accountId;
            this.displayAlias = displayAlias ?? string.Empty;
            this.slotIndex = slotIndex;
            this.readyState = readyState;
            this.owner = owner;
        }

        public PlayerAccountId AccountId => accountId;
        public string DisplayAlias => displayAlias ?? string.Empty;
        public int SlotIndex => slotIndex;
        public LobbyReadyState ReadyState => readyState;
        public bool IsOwner => owner;

        public LobbyMatchMember WithReady(LobbyReadyState next) =>
            new(accountId, displayAlias, slotIndex, next, owner);
    }

    [Serializable]
    public sealed class LobbyMatchSnapshot
    {
        [SerializeField] private LobbyMatchId lobbyMatchId;
        [SerializeField] private string displayName;
        [SerializeField] private int maxPlayers;
        [SerializeField] private LobbyMatchState state;
        [SerializeField] private string holeId;
        [SerializeField] private long createdAtUnixMilliseconds;
        [SerializeField] private LobbyVisibility visibility;
        [SerializeField] private bool joinable;
        [SerializeField] private long revision;
        [SerializeField] private MatchId gameMatchId;
        [SerializeField] private LobbyMatchMember[] members;

        public LobbyMatchSnapshot(LobbyMatchId lobbyMatchId, string displayName, int maxPlayers,
            LobbyMatchState state, string holeId, long createdAtUnixMilliseconds,
            LobbyVisibility visibility, bool joinable, long revision, MatchId gameMatchId,
            LobbyMatchMember[] members)
        {
            this.lobbyMatchId = lobbyMatchId;
            this.displayName = displayName ?? string.Empty;
            this.maxPlayers = maxPlayers;
            this.state = state;
            this.holeId = holeId ?? string.Empty;
            this.createdAtUnixMilliseconds = createdAtUnixMilliseconds;
            this.visibility = visibility;
            this.joinable = joinable;
            this.revision = revision;
            this.gameMatchId = gameMatchId;
            this.members = members ?? Array.Empty<LobbyMatchMember>();
        }

        public LobbyMatchId LobbyMatchId => lobbyMatchId;
        public string DisplayName => displayName ?? string.Empty;
        public int CurrentPlayers => members?.Length ?? 0;
        public int MaxPlayers => maxPlayers;
        public LobbyMatchState State => state;
        public string HoleId => holeId ?? string.Empty;
        public long CreatedAtUnixMilliseconds => createdAtUnixMilliseconds;
        public LobbyVisibility Visibility => visibility;
        public bool Joinable => joinable;
        public long Revision => revision;
        public MatchId GameMatchId => gameMatchId;
        public LobbyMatchMember[] Members => members ?? Array.Empty<LobbyMatchMember>();

        public bool TryGetMember(PlayerAccountId accountId, out LobbyMatchMember member)
        {
            foreach (LobbyMatchMember value in Members)
            {
                if (value.AccountId != accountId) continue;
                member = value;
                return true;
            }
            member = default;
            return false;
        }
    }

    [Serializable]
    public struct CreateMatchRequest
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private string requestId;
        [SerializeField] private string displayName;
        [SerializeField] private int maxPlayers;
        [SerializeField] private string holeId;
        [SerializeField] private LobbyVisibility visibility;

        public CreateMatchRequest(string requestId, string displayName, int maxPlayers,
            string holeId, LobbyVisibility visibility)
        {
            protocolVersion = LobbyProtocol.CurrentVersion;
            this.requestId = requestId ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.maxPlayers = maxPlayers;
            this.holeId = holeId ?? string.Empty;
            this.visibility = visibility;
        }

        public int ProtocolVersion => protocolVersion;
        public string RequestId => requestId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public int MaxPlayers => maxPlayers;
        public string HoleId => holeId ?? string.Empty;
        public LobbyVisibility Visibility => visibility;
    }

    [Serializable]
    public struct ListMatchesRequest
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private string requestId;
        [SerializeField] private bool joinableOnly;

        public ListMatchesRequest(string requestId, bool joinableOnly = true)
        {
            protocolVersion = LobbyProtocol.CurrentVersion;
            this.requestId = requestId ?? string.Empty;
            this.joinableOnly = joinableOnly;
        }

        public int ProtocolVersion => protocolVersion;
        public string RequestId => requestId ?? string.Empty;
        public bool JoinableOnly => joinableOnly;
    }

    [Serializable]
    public struct LobbyMatchRequest
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private string requestId;
        [SerializeField] private LobbyMatchId lobbyMatchId;

        public LobbyMatchRequest(string requestId, LobbyMatchId lobbyMatchId)
        {
            protocolVersion = LobbyProtocol.CurrentVersion;
            this.requestId = requestId ?? string.Empty;
            this.lobbyMatchId = lobbyMatchId;
        }

        public int ProtocolVersion => protocolVersion;
        public string RequestId => requestId ?? string.Empty;
        public LobbyMatchId LobbyMatchId => lobbyMatchId;
    }

    [Serializable]
    public struct SetReadyRequest
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private string requestId;
        [SerializeField] private LobbyMatchId lobbyMatchId;
        [SerializeField] private bool ready;

        public SetReadyRequest(string requestId, LobbyMatchId lobbyMatchId, bool ready)
        {
            protocolVersion = LobbyProtocol.CurrentVersion;
            this.requestId = requestId ?? string.Empty;
            this.lobbyMatchId = lobbyMatchId;
            this.ready = ready;
        }

        public int ProtocolVersion => protocolVersion;
        public string RequestId => requestId ?? string.Empty;
        public LobbyMatchId LobbyMatchId => lobbyMatchId;
        public bool Ready => ready;
    }

    [Serializable]
    public struct MatchJoinTicket
    {
        [SerializeField] private MatchId gameMatchId;
        [SerializeField] private string secret;
        [SerializeField] private long expiresAtUnixMilliseconds;

        public MatchJoinTicket(MatchId gameMatchId, string secret, long expiresAtUnixMilliseconds)
        {
            this.gameMatchId = gameMatchId;
            this.secret = secret ?? string.Empty;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
        }

        public MatchId GameMatchId => gameMatchId;
        public string Secret => secret ?? string.Empty;
        public long ExpiresAtUnixMilliseconds => expiresAtUnixMilliseconds;
        public bool IsValid => gameMatchId.IsValid && !string.IsNullOrWhiteSpace(Secret)
            && Secret.Length <= 512 && expiresAtUnixMilliseconds > 0;
    }

    [Serializable]
    public struct MatchAdmissionGrant
    {
        [SerializeField] private LobbyMatchId lobbyMatchId;
        [SerializeField] private MatchId gameMatchId;
        [SerializeField] private PlayerAccountId playerAccountId;
        [SerializeField] private MatchPlayerId matchPlayerId;
        [SerializeField] private string serverAddress;
        [SerializeField] private int serverPort;
        [SerializeField] private MatchJoinTicket joinTicket;
        [SerializeField] private MatchConnectivityDescriptor connectivity;

        public MatchAdmissionGrant(LobbyMatchId lobbyMatchId, MatchId gameMatchId,
            PlayerAccountId playerAccountId, MatchPlayerId matchPlayerId, string serverAddress, ushort serverPort,
            MatchJoinTicket joinTicket)
        {
            this.lobbyMatchId = lobbyMatchId;
            this.gameMatchId = gameMatchId;
            this.playerAccountId = playerAccountId;
            this.matchPlayerId = matchPlayerId;
            this.serverAddress = serverAddress ?? string.Empty;
            this.serverPort = serverPort;
            this.joinTicket = joinTicket;
            connectivity = new MatchConnectivityDescriptor(MatchConnectivityMode.Direct,
                ConnectivityProtocol.DirectProvider, this.serverAddress, serverPort,
                "direct-" + gameMatchId.Value, string.Empty, 0L);
        }

        public MatchAdmissionGrant(LobbyMatchId lobbyMatchId, MatchId gameMatchId,
            PlayerAccountId playerAccountId, MatchPlayerId matchPlayerId,
            MatchConnectivityDescriptor connectivity, MatchJoinTicket joinTicket)
        {
            this.lobbyMatchId = lobbyMatchId;
            this.gameMatchId = gameMatchId;
            this.playerAccountId = playerAccountId;
            this.matchPlayerId = matchPlayerId;
            this.connectivity = connectivity;
            serverAddress = connectivity.Address;
            serverPort = connectivity.Port;
            this.joinTicket = joinTicket;
        }

        public LobbyMatchId LobbyMatchId => lobbyMatchId;
        public MatchId GameMatchId => gameMatchId;
        public PlayerAccountId PlayerAccountId => playerAccountId;
        public MatchPlayerId MatchPlayerId => matchPlayerId;
        public string ServerAddress => serverAddress ?? string.Empty;
        public ushort ServerPort => (ushort)Mathf.Clamp(serverPort, 1, 65535);
        public MatchJoinTicket JoinTicket => joinTicket;
        public MatchConnectivityDescriptor Connectivity => connectivity;
        public bool IsValid => lobbyMatchId.IsValid && gameMatchId.IsValid && playerAccountId.IsValid && matchPlayerId.IsValid
            && !string.IsNullOrWhiteSpace(ServerAddress) && serverPort > 0 && joinTicket.IsValid
            && connectivity.IsValidAt(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    [Serializable]
    public sealed class MatchReservation
    {
        [SerializeField] private LobbyMatchId lobbyMatchId;
        [SerializeField] private MatchId gameMatchId;
        [SerializeField] private string serverAddress;
        [SerializeField] private int serverPort;
        [SerializeField] private long expiresAtUnixMilliseconds;
        [SerializeField] private MatchAdmissionGrant[] grants;
        [SerializeField] private MatchConnectivityDescriptor connectivity;

        public MatchReservation(LobbyMatchId lobbyMatchId, MatchId gameMatchId, string serverAddress,
            ushort serverPort, long expiresAtUnixMilliseconds, MatchAdmissionGrant[] grants)
        {
            this.lobbyMatchId = lobbyMatchId;
            this.gameMatchId = gameMatchId;
            this.serverAddress = serverAddress ?? string.Empty;
            this.serverPort = serverPort;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
            this.grants = grants ?? Array.Empty<MatchAdmissionGrant>();
            connectivity = new MatchConnectivityDescriptor(MatchConnectivityMode.Direct,
                ConnectivityProtocol.DirectProvider, this.serverAddress, serverPort,
                "direct-" + gameMatchId.Value, string.Empty, 0L);
        }

        public MatchReservation(LobbyMatchId lobbyMatchId, MatchId gameMatchId,
            ServerConnectivityDescriptor server, long expiresAtUnixMilliseconds,
            MatchConnectivityDescriptor connectivity, MatchAdmissionGrant[] grants)
        {
            this.lobbyMatchId = lobbyMatchId;
            this.gameMatchId = gameMatchId;
            serverAddress = server.BindAddress;
            serverPort = server.BindPort;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
            this.connectivity = connectivity;
            this.grants = grants ?? Array.Empty<MatchAdmissionGrant>();
        }

        public LobbyMatchId LobbyMatchId => lobbyMatchId;
        public MatchId GameMatchId => gameMatchId;
        public string ServerAddress => serverAddress ?? string.Empty;
        public ushort ServerPort => (ushort)Mathf.Clamp(serverPort, 1, 65535);
        public long ExpiresAtUnixMilliseconds => expiresAtUnixMilliseconds;
        public MatchAdmissionGrant[] Grants => grants ?? Array.Empty<MatchAdmissionGrant>();
        public MatchConnectivityDescriptor Connectivity => connectivity;

        public bool TryGetGrant(PlayerAccountId accountId, out MatchAdmissionGrant grant)
        {
            foreach (MatchAdmissionGrant value in Grants)
            {
                if (!value.IsValid || value.PlayerAccountId != accountId) continue;
                grant = value;
                return true;
            }
            grant = default;
            return false;
        }
    }

    public readonly struct LobbyOperationResult<T>
    {
        private LobbyOperationResult(bool accepted, LobbyRejectReason reason, T value)
        {
            Accepted = accepted;
            Reason = reason;
            Value = value;
        }

        public bool Accepted { get; }
        public LobbyRejectReason Reason { get; }
        public T Value { get; }
        public static LobbyOperationResult<T> Accept(T value) => new(true, LobbyRejectReason.None, value);
        public static LobbyOperationResult<T> Reject(LobbyRejectReason reason) => new(false, reason, default);
    }

    public readonly struct MatchAdmissionValidationResult
    {
        private MatchAdmissionValidationResult(bool accepted, MatchAdmissionRejectReason reason,
            MatchPlayerId playerId)
        {
            Accepted = accepted;
            Reason = reason;
            PlayerId = playerId;
        }

        public bool Accepted { get; }
        public MatchAdmissionRejectReason Reason { get; }
        public MatchPlayerId PlayerId { get; }
        public static MatchAdmissionValidationResult Accept(MatchPlayerId playerId) =>
            new(true, MatchAdmissionRejectReason.None, playerId);
        public static MatchAdmissionValidationResult Reject(MatchAdmissionRejectReason reason) =>
            new(false, reason, default);
    }

    public static class LobbyProtocol
    {
        public const int CurrentVersion = 1;
        public const int MatchPlayerCapacity = 2;
        public const int DefaultMaximumRooms = 32;
        public const int MaximumDisplayNameLength = 32;
        public const string SupportedHoleId = "hole-01";
    }
}

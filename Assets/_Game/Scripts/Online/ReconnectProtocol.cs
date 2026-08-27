using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SwingPop.Online
{
    public enum ReconnectRejectReason
    {
        None,
        InvalidTicket,
        ExpiredTicket,
        UnknownMatch,
        UnknownPlayer,
        PlayerAlreadyConnected,
        MatchEnded,
        ProtocolMismatch,
        SlotExpired,
        RateLimited
    }

    public enum ReconnectClientState
    {
        None,
        TicketReady,
        ConnectionLost,
        Reconnecting,
        Reconnected,
        ReconnectFailed,
        Ended
    }

    [Serializable]
    public struct ReconnectTicket
    {
        [SerializeField] private MatchId matchId;
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private int sessionGeneration;
        [SerializeField] private string secret;
        [SerializeField] private long issuedAtUnixMilliseconds;
        [SerializeField] private long expiresAtUnixMilliseconds;

        public ReconnectTicket(MatchId matchId, MatchPlayerId playerId, int sessionGeneration, string secret,
            long issuedAtUnixMilliseconds, long expiresAtUnixMilliseconds)
        {
            this.matchId = matchId;
            this.playerId = playerId;
            this.sessionGeneration = sessionGeneration;
            this.secret = secret ?? string.Empty;
            this.issuedAtUnixMilliseconds = issuedAtUnixMilliseconds;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
        }

        public MatchId MatchId => matchId;
        public MatchPlayerId PlayerId => playerId;
        public int SessionGeneration => sessionGeneration;
        public string Secret => secret ?? string.Empty;
        public long IssuedAtUnixMilliseconds => issuedAtUnixMilliseconds;
        public long ExpiresAtUnixMilliseconds => expiresAtUnixMilliseconds;
        public bool IsValid => matchId.IsValid && playerId.IsValid && sessionGeneration > 0
                               && !string.IsNullOrWhiteSpace(Secret);
    }

    [Serializable]
    public struct ReconnectTicketIssuedMessage
    {
        [SerializeField] private ReconnectTicket ticket;
        public ReconnectTicketIssuedMessage(ReconnectTicket ticket) => this.ticket = ticket;
        public ReconnectTicket Ticket => ticket;
    }

    [Serializable]
    public struct ReconnectRequestMessage
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private MatchId matchId;
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private int sessionGeneration;
        [SerializeField] private string secret;
        [SerializeField] private long lastKnownSnapshotVersion;

        public ReconnectRequestMessage(ReconnectTicket ticket, long lastKnownSnapshotVersion)
        {
            protocolVersion = OnlineProtocol.CurrentVersion;
            matchId = ticket.MatchId;
            playerId = ticket.PlayerId;
            sessionGeneration = ticket.SessionGeneration;
            secret = ticket.Secret;
            this.lastKnownSnapshotVersion = lastKnownSnapshotVersion;
        }

        public int ProtocolVersion => protocolVersion;
        public MatchId MatchId => matchId;
        public MatchPlayerId PlayerId => playerId;
        public int SessionGeneration => sessionGeneration;
        public string Secret => secret ?? string.Empty;
        public long LastKnownSnapshotVersion => lastKnownSnapshotVersion;
    }

    [Serializable]
    public struct ReconnectAcceptedMessage
    {
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private MatchId matchId;
        [SerializeField] private ReconnectTicket rotatedTicket;
        [SerializeField] private long snapshotVersion;
        [SerializeField] private MatchPlayerId currentTurnPlayer;

        public ReconnectAcceptedMessage(MatchPlayerId playerId, MatchId matchId, ReconnectTicket rotatedTicket,
            long snapshotVersion, MatchPlayerId currentTurnPlayer)
        {
            this.playerId = playerId;
            this.matchId = matchId;
            this.rotatedTicket = rotatedTicket;
            this.snapshotVersion = snapshotVersion;
            this.currentTurnPlayer = currentTurnPlayer;
        }

        public MatchPlayerId PlayerId => playerId;
        public MatchId MatchId => matchId;
        public ReconnectTicket RotatedTicket => rotatedTicket;
        public long SnapshotVersion => snapshotVersion;
        public MatchPlayerId CurrentTurnPlayer => currentTurnPlayer;
    }

    [Serializable]
    public struct ReconnectRejectedMessage
    {
        [SerializeField] private ReconnectRejectReason reason;
        [SerializeField] private string detail;

        public ReconnectRejectedMessage(ReconnectRejectReason reason, string detail)
        {
            this.reason = reason;
            this.detail = detail ?? string.Empty;
        }

        public ReconnectRejectReason Reason => reason;
        public string Detail => detail ?? string.Empty;
    }

    [Serializable]
    public struct MatchLifecycleChangedMessage
    {
        [SerializeField] private DedicatedMatchLifecycleState lifecycleState;
        [SerializeField] private MatchPlayerId affectedPlayer;
        [SerializeField] private PlayerConnectionState playerConnectionState;
        [SerializeField] private long graceDeadlineUnixMilliseconds;
        [SerializeField] private string reason;

        public MatchLifecycleChangedMessage(DedicatedMatchLifecycleState lifecycleState,
            MatchPlayerId affectedPlayer, PlayerConnectionState playerConnectionState,
            long graceDeadlineUnixMilliseconds, string reason)
        {
            this.lifecycleState = lifecycleState;
            this.affectedPlayer = affectedPlayer;
            this.playerConnectionState = playerConnectionState;
            this.graceDeadlineUnixMilliseconds = graceDeadlineUnixMilliseconds;
            this.reason = reason ?? string.Empty;
        }

        public DedicatedMatchLifecycleState LifecycleState => lifecycleState;
        public MatchPlayerId AffectedPlayer => affectedPlayer;
        public PlayerConnectionState PlayerConnectionState => playerConnectionState;
        public long GraceDeadlineUnixMilliseconds => graceDeadlineUnixMilliseconds;
        public string Reason => reason ?? string.Empty;
    }

    public interface IServerClock
    {
        long UtcNowMilliseconds { get; }
    }

    public sealed class SystemServerClock : IServerClock
    {
        public long UtcNowMilliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public interface IReconnectTokenSource
    {
        string CreateSecret();
    }

    public sealed class CryptographicReconnectTokenSource : IReconnectTokenSource
    {
        public string CreateSecret()
        {
            byte[] bytes = new byte[32];
            using RandomNumberGenerator generator = RandomNumberGenerator.Create();
            generator.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    public readonly struct ReconnectValidationResult
    {
        private ReconnectValidationResult(bool accepted, ReconnectRejectReason reason, ReconnectTicket ticket)
        {
            Accepted = accepted;
            Reason = reason;
            RotatedTicket = ticket;
        }

        public bool Accepted { get; }
        public ReconnectRejectReason Reason { get; }
        public ReconnectTicket RotatedTicket { get; }
        public static ReconnectValidationResult Accept(ReconnectTicket ticket) => new(true, ReconnectRejectReason.None, ticket);
        public static ReconnectValidationResult Reject(ReconnectRejectReason reason) => new(false, reason, default);
    }

    /// <summary>
    /// Server-owned, in-memory reconnect credential registry. Only hashes are retained server-side.
    /// It is deliberately not an authentication or durable session store.
    /// </summary>
    public sealed class ReconnectSessionRegistry
    {
        private sealed class Session
        {
            public MatchId MatchId;
            public MatchPlayerId PlayerId;
            public int Generation;
            public byte[] SecretHash;
            public PlayerConnectionState State;
            public long GraceDeadline;
        }

        private readonly Dictionary<MatchPlayerId, Session> sessions = new();
        private readonly IReconnectTokenSource tokenSource;
        private MatchId matchId;
        private bool matchEnded;

        public ReconnectSessionRegistry(IReconnectTokenSource tokenSource = null)
        {
            this.tokenSource = tokenSource ?? new CryptographicReconnectTokenSource();
        }

        public int Count => sessions.Count;
        public MatchId MatchId => matchId;
        public bool HasPlayerInGrace
        {
            get
            {
                foreach (Session session in sessions.Values)
                    if (session.State == PlayerConnectionState.ReconnectGrace) return true;
                return false;
            }
        }

        public ReconnectTicket Register(MatchId newMatchId, MatchPlayerId playerId, long nowMilliseconds)
        {
            if (!newMatchId.IsValid || !playerId.IsValid) throw new ArgumentException("Valid match and player ids are required.");
            if (matchId.IsValid && matchId != newMatchId) throw new InvalidOperationException("Registry already belongs to another match.");
            matchId = newMatchId;
            matchEnded = false;
            return Rotate(playerId, nowMilliseconds, PlayerConnectionState.Connected, 0L, 1);
        }

        public bool TryEnterGrace(MatchPlayerId playerId, long nowMilliseconds, long graceDurationMilliseconds,
            out long deadlineMilliseconds)
        {
            deadlineMilliseconds = 0L;
            if (!sessions.TryGetValue(playerId, out Session session) || session.State != PlayerConnectionState.Connected)
                return false;
            session.State = PlayerConnectionState.ReconnectGrace;
            session.GraceDeadline = nowMilliseconds + Math.Max(1L, graceDurationMilliseconds);
            deadlineMilliseconds = session.GraceDeadline;
            return true;
        }

        public ReconnectValidationResult ValidateAndRotate(ReconnectRequestMessage request, long nowMilliseconds,
            bool hasActiveConnection)
        {
            if (request.ProtocolVersion != OnlineProtocol.CurrentVersion)
                return ReconnectValidationResult.Reject(ReconnectRejectReason.ProtocolMismatch);
            if (matchEnded) return ReconnectValidationResult.Reject(ReconnectRejectReason.MatchEnded);
            if (!matchId.IsValid || request.MatchId != matchId)
                return ReconnectValidationResult.Reject(ReconnectRejectReason.UnknownMatch);
            if (!sessions.TryGetValue(request.PlayerId, out Session session))
                return ReconnectValidationResult.Reject(ReconnectRejectReason.UnknownPlayer);
            if (session.State == PlayerConnectionState.Expired)
                return ReconnectValidationResult.Reject(ReconnectRejectReason.SlotExpired);
            if (hasActiveConnection || session.State == PlayerConnectionState.Connected)
                return ReconnectValidationResult.Reject(ReconnectRejectReason.PlayerAlreadyConnected);
            if (session.State != PlayerConnectionState.ReconnectGrace)
                return ReconnectValidationResult.Reject(ReconnectRejectReason.InvalidTicket);
            if (nowMilliseconds > session.GraceDeadline)
                return ReconnectValidationResult.Reject(ReconnectRejectReason.ExpiredTicket);
            if (request.SessionGeneration != session.Generation
                || !FixedTimeEquals(session.SecretHash, Hash(request.Secret)))
                return ReconnectValidationResult.Reject(ReconnectRejectReason.InvalidTicket);

            ReconnectTicket rotated = Rotate(request.PlayerId, nowMilliseconds,
                PlayerConnectionState.Connected, 0L, session.Generation + 1);
            return ReconnectValidationResult.Accept(rotated);
        }

        public bool TryExpire(long nowMilliseconds, out MatchPlayerId playerId)
        {
            foreach (Session session in sessions.Values)
            {
                if (session.State != PlayerConnectionState.ReconnectGrace || nowMilliseconds <= session.GraceDeadline)
                    continue;
                session.State = PlayerConnectionState.Expired;
                session.SecretHash = Array.Empty<byte>();
                playerId = session.PlayerId;
                return true;
            }
            playerId = default;
            return false;
        }

        public bool TryGet(MatchPlayerId playerId, out PlayerConnectionState state, out int generation,
            out long graceDeadlineMilliseconds)
        {
            if (sessions.TryGetValue(playerId, out Session session))
            {
                state = session.State;
                generation = session.Generation;
                graceDeadlineMilliseconds = session.GraceDeadline;
                return true;
            }
            state = PlayerConnectionState.Disconnected;
            generation = 0;
            graceDeadlineMilliseconds = 0L;
            return false;
        }

        public void MarkMatchEnded()
        {
            matchEnded = true;
            foreach (Session session in sessions.Values) session.SecretHash = Array.Empty<byte>();
        }

        public void Reset()
        {
            foreach (Session session in sessions.Values) session.SecretHash = Array.Empty<byte>();
            sessions.Clear();
            matchId = default;
            matchEnded = false;
        }

        public static string Fingerprint(string secret)
        {
            byte[] hash = Hash(secret);
            return hash.Length < 4 ? "none" : BitConverter.ToString(hash, 0, 4).Replace("-", string.Empty);
        }

        private ReconnectTicket Rotate(MatchPlayerId playerId, long nowMilliseconds,
            PlayerConnectionState state, long deadline, int generation)
        {
            string secret = tokenSource.CreateSecret();
            Session session = new()
            {
                MatchId = matchId,
                PlayerId = playerId,
                Generation = generation,
                SecretHash = Hash(secret),
                State = state,
                GraceDeadline = deadline
            };
            sessions[playerId] = session;
            return new ReconnectTicket(matchId, playerId, generation, secret, nowMilliseconds, deadline);
        }

        private static byte[] Hash(string secret)
        {
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(secret ?? string.Empty));
        }

        private static bool FixedTimeEquals(byte[] expected, byte[] actual)
        {
            if (expected == null || actual == null || expected.Length != actual.Length) return false;
            int difference = 0;
            for (int index = 0; index < expected.Length; index++) difference |= expected[index] ^ actual[index];
            return difference == 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SwingPop.Online
{
    public interface ILobbyService
    {
        LobbyOperationResult<LobbyMatchSnapshot> CreateMatch(LobbyPlayerSession session,
            CreateMatchRequest request, long nowMilliseconds);
        LobbyOperationResult<LobbyMatchSnapshot[]> ListMatches(LobbyPlayerSession session,
            ListMatchesRequest request, long nowMilliseconds);
        LobbyOperationResult<LobbyMatchSnapshot> GetMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds);
        LobbyOperationResult<LobbyMatchSnapshot> JoinMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds);
        LobbyOperationResult<LobbyMatchSnapshot> LeaveMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds);
        LobbyOperationResult<LobbyMatchSnapshot> SetReady(LobbyPlayerSession session,
            SetReadyRequest request, long nowMilliseconds);
        LobbyOperationResult<MatchReservation> StartMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds);
        LobbyOperationResult<LobbyMatchSnapshot> CloseMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds);
        bool Disconnect(LobbyPlayerSession session, long nowMilliseconds,
            out LobbyMatchSnapshot changedMatch);
        int CleanupClosedMatches(long nowMilliseconds, long retentionMilliseconds);
    }

    public interface IGameServerAllocator
    {
        bool TryAllocate(LobbyMatchSnapshot match, long nowMilliseconds,
            out MatchReservation reservation, out string failure);
    }

    /// <summary>
    /// Thread-safe development registry. The lock owns validation and membership mutation together,
    /// so two joins can never reserve the same final slot.
    /// </summary>
    public sealed class InMemoryLobbyService : ILobbyService
    {
        private sealed class MatchRecord
        {
            public LobbyMatchId Id;
            public PlayerAccountId Owner;
            public string DisplayName;
            public int MaxPlayers;
            public LobbyMatchState State;
            public string HoleId;
            public long CreatedAt;
            public long ClosedAt;
            public LobbyVisibility Visibility;
            public long Revision;
            public MatchId GameMatchId;
            public MatchReservation Reservation;
            public readonly List<LobbyMatchMember> Members = new(LobbyProtocol.MatchPlayerCapacity);
        }

        private readonly object gate = new();
        private readonly Dictionary<LobbyMatchId, MatchRecord> matches = new();
        private readonly Dictionary<PlayerAccountId, LobbyMatchId> membership = new();
        private readonly Dictionary<string, MatchReservation> startRequests = new(StringComparer.Ordinal);
        private readonly IGameServerAllocator allocator;
        private readonly int maximumRooms;
        private long identifierSequence;

        public InMemoryLobbyService(IGameServerAllocator allocator,
            int maximumRooms = LobbyProtocol.DefaultMaximumRooms)
        {
            this.allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            this.maximumRooms = Math.Max(1, maximumRooms);
        }

        public int MatchCount
        {
            get { lock (gate) return matches.Count; }
        }

        public LobbyOperationResult<LobbyMatchSnapshot> CreateMatch(LobbyPlayerSession session,
            CreateMatchRequest request, long nowMilliseconds)
        {
            LobbyRejectReason auth = ValidateSession(session, nowMilliseconds);
            if (auth != LobbyRejectReason.None) return LobbyOperationResult<LobbyMatchSnapshot>.Reject(auth);
            if (request.ProtocolVersion != LobbyProtocol.CurrentVersion || !IsRequestIdValid(request.RequestId))
                return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.InvalidRequest);
            if (!IsDisplayNameValid(request.DisplayName))
                return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.InvalidDisplayName);
            if (request.MaxPlayers != LobbyProtocol.MatchPlayerCapacity)
                return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.UnsupportedPlayerCount);
            if (!string.Equals(request.HoleId, LobbyProtocol.SupportedHoleId, StringComparison.Ordinal))
                return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.UnsupportedHole);

            lock (gate)
            {
                if (membership.ContainsKey(session.PlayerAccountId))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.AlreadyInMatch);
                int activeRooms = matches.Values.Count(value => value.State != LobbyMatchState.Closed
                    && value.State != LobbyMatchState.Completed);
                if (activeRooms >= maximumRooms)
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.RateLimited);

                LobbyMatchId id = new($"lobby-{nowMilliseconds:x}-{++identifierSequence:x}");
                MatchRecord record = new()
                {
                    Id = id,
                    Owner = session.PlayerAccountId,
                    DisplayName = request.DisplayName.Trim(),
                    MaxPlayers = request.MaxPlayers,
                    State = LobbyMatchState.WaitingForPlayers,
                    HoleId = request.HoleId,
                    CreatedAt = nowMilliseconds,
                    Visibility = request.Visibility,
                    Revision = 1
                };
                record.Members.Add(CreateMember(session.PlayerAccountId, 0, true));
                matches.Add(id, record);
                membership.Add(session.PlayerAccountId, id);
                return LobbyOperationResult<LobbyMatchSnapshot>.Accept(Snapshot(record));
            }
        }

        public LobbyOperationResult<LobbyMatchSnapshot[]> ListMatches(LobbyPlayerSession session,
            ListMatchesRequest request, long nowMilliseconds)
        {
            LobbyRejectReason auth = ValidateSession(session, nowMilliseconds);
            if (auth != LobbyRejectReason.None) return LobbyOperationResult<LobbyMatchSnapshot[]>.Reject(auth);
            if (request.ProtocolVersion != LobbyProtocol.CurrentVersion || !IsRequestIdValid(request.RequestId))
                return LobbyOperationResult<LobbyMatchSnapshot[]>.Reject(LobbyRejectReason.InvalidRequest);
            lock (gate)
            {
                LobbyMatchSnapshot[] values = matches.Values
                    .Where(value => value.Visibility == LobbyVisibility.Public)
                    .Where(value => !request.JoinableOnly || IsJoinable(value))
                    .OrderBy(value => value.CreatedAt)
                    .Select(Snapshot)
                    .ToArray();
                return LobbyOperationResult<LobbyMatchSnapshot[]>.Accept(values);
            }
        }

        public LobbyOperationResult<LobbyMatchSnapshot> GetMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds)
        {
            LobbyRejectReason auth = ValidateSession(session, nowMilliseconds);
            if (auth != LobbyRejectReason.None) return LobbyOperationResult<LobbyMatchSnapshot>.Reject(auth);
            lock (gate)
            {
                if (!matches.TryGetValue(request.LobbyMatchId, out MatchRecord record))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchNotFound);
                return LobbyOperationResult<LobbyMatchSnapshot>.Accept(Snapshot(record));
            }
        }

        public LobbyOperationResult<LobbyMatchSnapshot> JoinMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds)
        {
            LobbyRejectReason auth = ValidateSession(session, nowMilliseconds);
            if (auth != LobbyRejectReason.None) return LobbyOperationResult<LobbyMatchSnapshot>.Reject(auth);
            if (!IsMatchRequestValid(request))
                return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.InvalidRequest);
            lock (gate)
            {
                if (!matches.TryGetValue(request.LobbyMatchId, out MatchRecord record))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchNotFound);
                if (record.Members.Any(value => value.AccountId == session.PlayerAccountId))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.AlreadyMember);
                if (membership.ContainsKey(session.PlayerAccountId))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.AlreadyInMatch);
                if (record.State == LobbyMatchState.Starting)
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchStarting);
                if (record.State is LobbyMatchState.Closed or LobbyMatchState.Completed)
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchClosed);
                if (!IsJoinable(record))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(
                        record.Members.Count >= record.MaxPlayers ? LobbyRejectReason.MatchFull : LobbyRejectReason.MatchNotJoinable);

                ResetReady(record);
                record.Members.Add(CreateMember(session.PlayerAccountId, record.Members.Count, false));
                membership.Add(session.PlayerAccountId, record.Id);
                record.State = record.Members.Count == record.MaxPlayers
                    ? LobbyMatchState.Full : LobbyMatchState.WaitingForPlayers;
                record.Revision++;
                return LobbyOperationResult<LobbyMatchSnapshot>.Accept(Snapshot(record));
            }
        }

        public LobbyOperationResult<LobbyMatchSnapshot> LeaveMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds)
        {
            LobbyRejectReason auth = ValidateSession(session, nowMilliseconds);
            if (auth != LobbyRejectReason.None) return LobbyOperationResult<LobbyMatchSnapshot>.Reject(auth);
            lock (gate)
            {
                if (!matches.TryGetValue(request.LobbyMatchId, out MatchRecord record))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchNotFound);
                int index = record.Members.FindIndex(value => value.AccountId == session.PlayerAccountId);
                if (index < 0) return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.NotMember);
                if (record.State is LobbyMatchState.Starting or LobbyMatchState.InGame)
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchStarting);
                if (record.Owner == session.PlayerAccountId)
                {
                    CloseRecord(record, nowMilliseconds);
                    return LobbyOperationResult<LobbyMatchSnapshot>.Accept(Snapshot(record));
                }

                record.Members.RemoveAt(index);
                membership.Remove(session.PlayerAccountId);
                ResetReady(record);
                record.State = LobbyMatchState.WaitingForPlayers;
                record.Revision++;
                return LobbyOperationResult<LobbyMatchSnapshot>.Accept(Snapshot(record));
            }
        }

        public LobbyOperationResult<LobbyMatchSnapshot> SetReady(LobbyPlayerSession session,
            SetReadyRequest request, long nowMilliseconds)
        {
            LobbyRejectReason auth = ValidateSession(session, nowMilliseconds);
            if (auth != LobbyRejectReason.None) return LobbyOperationResult<LobbyMatchSnapshot>.Reject(auth);
            if (request.ProtocolVersion != LobbyProtocol.CurrentVersion || !IsRequestIdValid(request.RequestId))
                return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.InvalidRequest);
            lock (gate)
            {
                if (!matches.TryGetValue(request.LobbyMatchId, out MatchRecord record))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchNotFound);
                if (record.State is LobbyMatchState.Starting or LobbyMatchState.InGame)
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchStarting);
                int index = record.Members.FindIndex(value => value.AccountId == session.PlayerAccountId);
                if (index < 0) return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.NotMember);
                LobbyReadyState next = request.Ready ? LobbyReadyState.Ready : LobbyReadyState.NotReady;
                if (record.Members[index].ReadyState != next)
                {
                    record.Members[index] = record.Members[index].WithReady(next);
                    record.Revision++;
                }
                return LobbyOperationResult<LobbyMatchSnapshot>.Accept(Snapshot(record));
            }
        }

        public LobbyOperationResult<MatchReservation> StartMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds)
        {
            LobbyRejectReason auth = ValidateSession(session, nowMilliseconds);
            if (auth != LobbyRejectReason.None) return LobbyOperationResult<MatchReservation>.Reject(auth);
            if (!IsMatchRequestValid(request))
                return LobbyOperationResult<MatchReservation>.Reject(LobbyRejectReason.InvalidRequest);
            lock (gate)
            {
                string idempotencyKey = session.PlayerAccountId.Value + ":" + request.RequestId;
                if (startRequests.TryGetValue(idempotencyKey, out MatchReservation previous))
                    return LobbyOperationResult<MatchReservation>.Accept(previous);
                if (!matches.TryGetValue(request.LobbyMatchId, out MatchRecord record))
                    return LobbyOperationResult<MatchReservation>.Reject(LobbyRejectReason.MatchNotFound);
                if (record.Owner != session.PlayerAccountId)
                    return LobbyOperationResult<MatchReservation>.Reject(LobbyRejectReason.NotOwner);
                if (record.State == LobbyMatchState.InGame && record.Reservation != null)
                    return LobbyOperationResult<MatchReservation>.Accept(record.Reservation);
                if (record.State == LobbyMatchState.Starting)
                    return LobbyOperationResult<MatchReservation>.Reject(LobbyRejectReason.MatchStarting);
                if (record.State is LobbyMatchState.Closed or LobbyMatchState.Completed)
                    return LobbyOperationResult<MatchReservation>.Reject(LobbyRejectReason.MatchClosed);
                if (record.Members.Count != record.MaxPlayers)
                    return LobbyOperationResult<MatchReservation>.Reject(LobbyRejectReason.MatchFull);
                if (record.Members.Any(value => value.ReadyState != LobbyReadyState.Ready))
                    return LobbyOperationResult<MatchReservation>.Reject(LobbyRejectReason.PlayersNotReady);

                record.State = LobbyMatchState.Starting;
                record.Revision++;
                LobbyMatchSnapshot startingSnapshot = Snapshot(record);
                if (!allocator.TryAllocate(startingSnapshot, nowMilliseconds,
                        out MatchReservation reservation, out _))
                {
                    record.State = LobbyMatchState.Full;
                    record.Revision++;
                    return LobbyOperationResult<MatchReservation>.Reject(LobbyRejectReason.AllocationFailed);
                }

                record.Reservation = reservation;
                record.GameMatchId = reservation.GameMatchId;
                record.State = LobbyMatchState.InGame;
                record.Revision++;
                startRequests[idempotencyKey] = reservation;
                return LobbyOperationResult<MatchReservation>.Accept(reservation);
            }
        }

        public LobbyOperationResult<LobbyMatchSnapshot> CloseMatch(LobbyPlayerSession session,
            LobbyMatchRequest request, long nowMilliseconds)
        {
            LobbyRejectReason auth = ValidateSession(session, nowMilliseconds);
            if (auth != LobbyRejectReason.None) return LobbyOperationResult<LobbyMatchSnapshot>.Reject(auth);
            lock (gate)
            {
                if (!matches.TryGetValue(request.LobbyMatchId, out MatchRecord record))
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.MatchNotFound);
                if (record.Owner != session.PlayerAccountId)
                    return LobbyOperationResult<LobbyMatchSnapshot>.Reject(LobbyRejectReason.NotOwner);
                CloseRecord(record, nowMilliseconds);
                return LobbyOperationResult<LobbyMatchSnapshot>.Accept(Snapshot(record));
            }
        }

        public bool Disconnect(LobbyPlayerSession session, long nowMilliseconds,
            out LobbyMatchSnapshot changedMatch)
        {
            changedMatch = null;
            lock (gate)
            {
                if (!membership.TryGetValue(session.PlayerAccountId, out LobbyMatchId id)
                    || !matches.TryGetValue(id, out MatchRecord record)
                    || record.State is LobbyMatchState.InGame or LobbyMatchState.Completed or LobbyMatchState.Closed)
                    return false;
                if (record.Owner == session.PlayerAccountId) CloseRecord(record, nowMilliseconds);
                else
                {
                    record.Members.RemoveAll(value => value.AccountId == session.PlayerAccountId);
                    membership.Remove(session.PlayerAccountId);
                    ResetReady(record);
                    record.State = LobbyMatchState.WaitingForPlayers;
                    record.Revision++;
                }
                changedMatch = Snapshot(record);
                return true;
            }
        }

        public int CleanupClosedMatches(long nowMilliseconds, long retentionMilliseconds)
        {
            lock (gate)
            {
                LobbyMatchId[] remove = matches.Values
                    .Where(value => value.State is LobbyMatchState.Closed or LobbyMatchState.Completed)
                    .Where(value => nowMilliseconds - value.ClosedAt >= Math.Max(0L, retentionMilliseconds))
                    .Select(value => value.Id).ToArray();
                foreach (LobbyMatchId id in remove) matches.Remove(id);
                return remove.Length;
            }
        }

        private void CloseRecord(MatchRecord record, long nowMilliseconds)
        {
            foreach (LobbyMatchMember member in record.Members) membership.Remove(member.AccountId);
            record.State = LobbyMatchState.Closed;
            record.ClosedAt = nowMilliseconds;
            record.Revision++;
        }

        private static void ResetReady(MatchRecord record)
        {
            for (int index = 0; index < record.Members.Count; index++)
                record.Members[index] = record.Members[index].WithReady(LobbyReadyState.NotReady);
        }

        private static LobbyMatchMember CreateMember(PlayerAccountId accountId, int slot, bool owner) =>
            new(accountId, $"PLAYER {(char)('A' + slot)}", slot, LobbyReadyState.NotReady, owner);

        private static LobbyMatchSnapshot Snapshot(MatchRecord record) => new(record.Id,
            record.DisplayName, record.MaxPlayers, record.State, record.HoleId, record.CreatedAt,
            record.Visibility, IsJoinable(record), record.Revision, record.GameMatchId,
            record.Members.ToArray());

        private static bool IsJoinable(MatchRecord record) =>
            record.State == LobbyMatchState.WaitingForPlayers && record.Members.Count < record.MaxPlayers;

        private static LobbyRejectReason ValidateSession(LobbyPlayerSession session, long nowMilliseconds)
        {
            if (!session.PlayerAccountId.IsValid || !session.AuthSessionId.IsValid || !session.Connected)
                return LobbyRejectReason.AuthenticationRequired;
            if (session.Revoked) return LobbyRejectReason.SessionRevoked;
            return session.ExpiresAtUnixMilliseconds <= nowMilliseconds
                ? LobbyRejectReason.SessionExpired : LobbyRejectReason.None;
        }

        private static bool IsDisplayNameValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string trimmed = value.Trim();
            if (trimmed.Length is < 1 or > LobbyProtocol.MaximumDisplayNameLength) return false;
            foreach (char character in trimmed)
                if (char.IsControl(character)) return false;
            return true;
        }

        private static bool IsRequestIdValid(string value) =>
            !string.IsNullOrWhiteSpace(value) && value.Length <= 64;

        private static bool IsMatchRequestValid(LobbyMatchRequest request) =>
            request.ProtocolVersion == LobbyProtocol.CurrentVersion && IsRequestIdValid(request.RequestId)
            && request.LobbyMatchId.IsValid;
    }

    public sealed class LobbySnapshotStore
    {
        private readonly Dictionary<LobbyMatchId, LobbyMatchSnapshot> snapshots = new();

        public bool TryApply(LobbyMatchSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.LobbyMatchId.IsValid) return false;
            if (snapshots.TryGetValue(snapshot.LobbyMatchId, out LobbyMatchSnapshot current)
                && snapshot.Revision <= current.Revision) return false;
            snapshots[snapshot.LobbyMatchId] = snapshot;
            return true;
        }

        public bool TryGet(LobbyMatchId id, out LobbyMatchSnapshot snapshot) => snapshots.TryGetValue(id, out snapshot);
        public void Reset() => snapshots.Clear();
    }

    public interface IMatchAdmissionRegistry
    {
        MatchId GameMatchId { get; }
        MatchAdmissionValidationResult ValidateAndConsume(MatchId requestedMatch,
            PlayerAccountId accountId, string secret, long nowMilliseconds, bool playerAlreadyConnected);
    }

    /// <summary>Bounded, one-time initial admission credentials. Reconnect credentials are not accepted here.</summary>
    public sealed class DevelopmentMatchAdmissionRegistry : IMatchAdmissionRegistry
    {
        private sealed class Entry
        {
            public PlayerAccountId AccountId;
            public MatchPlayerId PlayerId;
            public byte[] SecretHash;
            public long ExpiresAt;
            public bool Consumed;
        }

        private readonly Dictionary<PlayerAccountId, Entry> entries = new();

        public DevelopmentMatchAdmissionRegistry(MatchId gameMatchId) => GameMatchId = gameMatchId;
        public MatchId GameMatchId { get; }
        public int Count => entries.Count;

        public MatchJoinTicket Register(PlayerAccountId accountId, MatchPlayerId playerId,
            long expiresAtMilliseconds)
        {
            if (!GameMatchId.IsValid || !accountId.IsValid || !playerId.IsValid)
                throw new ArgumentException("Valid match, account, and player identifiers are required.");
            byte[] bytes = new byte[32];
            using RandomNumberGenerator generator = RandomNumberGenerator.Create();
            generator.GetBytes(bytes);
            string secret = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            entries[accountId] = new Entry
            {
                AccountId = accountId,
                PlayerId = playerId,
                SecretHash = Hash(secret),
                ExpiresAt = expiresAtMilliseconds
            };
            return new MatchJoinTicket(GameMatchId, secret, expiresAtMilliseconds);
        }

        public void Import(PlayerAccountId accountId, MatchPlayerId playerId, string secretHashBase64,
            long expiresAtMilliseconds, bool consumed = false)
        {
            entries[accountId] = new Entry
            {
                AccountId = accountId,
                PlayerId = playerId,
                SecretHash = Convert.FromBase64String(secretHashBase64),
                ExpiresAt = expiresAtMilliseconds,
                Consumed = consumed
            };
        }

        public MatchAdmissionValidationResult ValidateAndConsume(MatchId requestedMatch,
            PlayerAccountId accountId, string secret, long nowMilliseconds, bool playerAlreadyConnected)
        {
            if (!requestedMatch.IsValid || requestedMatch != GameMatchId)
                return MatchAdmissionValidationResult.Reject(requestedMatch.IsValid
                    ? MatchAdmissionRejectReason.WrongMatch : MatchAdmissionRejectReason.UnknownMatch);
            if (string.IsNullOrWhiteSpace(secret))
                return MatchAdmissionValidationResult.Reject(MatchAdmissionRejectReason.MissingTicket);
            if (!entries.TryGetValue(accountId, out Entry entry))
                return MatchAdmissionValidationResult.Reject(MatchAdmissionRejectReason.AccountMismatch);
            if (entry.Consumed) return MatchAdmissionValidationResult.Reject(MatchAdmissionRejectReason.Consumed);
            if (entry.ExpiresAt <= nowMilliseconds)
                return MatchAdmissionValidationResult.Reject(MatchAdmissionRejectReason.Expired);
            if (playerAlreadyConnected)
                return MatchAdmissionValidationResult.Reject(MatchAdmissionRejectReason.PlayerAlreadyConnected);
            byte[] provided = Hash(secret);
            if (!FixedTimeEquals(entry.SecretHash, provided))
                return MatchAdmissionValidationResult.Reject(MatchAdmissionRejectReason.InvalidTicket);
            entry.Consumed = true;
            return MatchAdmissionValidationResult.Accept(entry.PlayerId);
        }

        public string ExportHash(PlayerAccountId accountId)
        {
            return entries.TryGetValue(accountId, out Entry entry)
                ? Convert.ToBase64String(entry.SecretHash) : string.Empty;
        }

        private static byte[] Hash(string value)
        {
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static bool FixedTimeEquals(byte[] expected, byte[] actual)
        {
            if (expected == null || actual == null || expected.Length != actual.Length) return false;
            int difference = 0;
            for (int index = 0; index < expected.Length; index++) difference |= expected[index] ^ actual[index];
            return difference == 0;
        }
    }

    public sealed class PrelaunchedGameServerAllocator : IGameServerAllocator
    {
        private readonly string address;
        private readonly ushort port;
        private readonly long ticketLifetimeMilliseconds;
        private long sequence;

        public PrelaunchedGameServerAllocator(string address, ushort port, long ticketLifetimeMilliseconds = 60_000L)
        {
            this.address = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
            this.port = port;
            this.ticketLifetimeMilliseconds = Math.Max(5_000L, ticketLifetimeMilliseconds);
        }

        public DevelopmentMatchAdmissionRegistry LastAdmissionRegistry { get; private set; }

        public bool TryAllocate(LobbyMatchSnapshot match, long nowMilliseconds,
            out MatchReservation reservation, out string failure)
        {
            MatchId gameMatchId = new($"game-{nowMilliseconds:x}-{++sequence:x}");
            DevelopmentMatchAdmissionRegistry registry = new(gameMatchId);
            long expiry = nowMilliseconds + ticketLifetimeMilliseconds;
            MatchAdmissionGrant[] grants = new MatchAdmissionGrant[match.Members.Length];
            for (int index = 0; index < match.Members.Length; index++)
            {
                LobbyMatchMember member = match.Members[index];
                MatchPlayerId playerId = new(index == 0 ? "player-a" : "player-b");
                MatchJoinTicket ticket = registry.Register(member.AccountId, playerId, expiry);
                grants[index] = new MatchAdmissionGrant(match.LobbyMatchId, gameMatchId,
                    member.AccountId, playerId, address, port, ticket);
            }
            LastAdmissionRegistry = registry;
            reservation = new MatchReservation(match.LobbyMatchId, gameMatchId, address, port, expiry, grants);
            failure = string.Empty;
            return true;
        }
    }
}

using System;
using System.IO;
using UnityEngine;

namespace SwingPop.Online
{
    [Serializable]
    public struct MatchAdmissionRequestMessage
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private MatchId gameMatchId;
        [SerializeField] private string joinTicketSecret;

        public MatchAdmissionRequestMessage(MatchJoinTicket ticket)
        {
            protocolVersion = OnlineProtocol.CurrentVersion;
            gameMatchId = ticket.GameMatchId;
            joinTicketSecret = ticket.Secret;
        }

        public int ProtocolVersion => protocolVersion;
        public MatchId GameMatchId => gameMatchId;
        public string JoinTicketSecret => joinTicketSecret ?? string.Empty;
    }

    [Serializable]
    public struct MatchAdmissionRejectedMessage
    {
        [SerializeField] private MatchAdmissionRejectReason reason;
        [SerializeField] private string detail;

        public MatchAdmissionRejectedMessage(MatchAdmissionRejectReason reason, string detail)
        {
            this.reason = reason;
            this.detail = detail ?? string.Empty;
        }

        public MatchAdmissionRejectReason Reason => reason;
        public string Detail => detail ?? string.Empty;
    }

    [Serializable]
    public struct MatchAdmissionFileEntry
    {
        [SerializeField] private PlayerAccountId accountId;
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private string ticketHashBase64;
        [SerializeField] private long expiresAtUnixMilliseconds;

        public MatchAdmissionFileEntry(PlayerAccountId accountId, MatchPlayerId playerId,
            string ticketHashBase64, long expiresAtUnixMilliseconds)
        {
            this.accountId = accountId;
            this.playerId = playerId;
            this.ticketHashBase64 = ticketHashBase64 ?? string.Empty;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
        }

        public PlayerAccountId AccountId => accountId;
        public MatchPlayerId PlayerId => playerId;
        public string TicketHashBase64 => ticketHashBase64 ?? string.Empty;
        public long ExpiresAtUnixMilliseconds => expiresAtUnixMilliseconds;
    }

    [Serializable]
    public sealed class MatchReservationFileDocument
    {
        [SerializeField] private int documentVersion;
        [SerializeField] private LobbyMatchId lobbyMatchId;
        [SerializeField] private MatchId gameMatchId;
        [SerializeField] private string serverAddress;
        [SerializeField] private int serverPort;
        [SerializeField] private long expiresAtUnixMilliseconds;
        [SerializeField] private MatchAdmissionFileEntry[] entries;

        public MatchReservationFileDocument(int documentVersion, LobbyMatchId lobbyMatchId,
            MatchId gameMatchId, string serverAddress, ushort serverPort, long expiresAtUnixMilliseconds,
            MatchAdmissionFileEntry[] entries)
        {
            this.documentVersion = documentVersion;
            this.lobbyMatchId = lobbyMatchId;
            this.gameMatchId = gameMatchId;
            this.serverAddress = serverAddress ?? string.Empty;
            this.serverPort = serverPort;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
            this.entries = entries ?? Array.Empty<MatchAdmissionFileEntry>();
        }

        public int DocumentVersion => documentVersion;
        public LobbyMatchId LobbyMatchId => lobbyMatchId;
        public MatchId GameMatchId => gameMatchId;
        public string ServerAddress => serverAddress ?? string.Empty;
        public ushort ServerPort => (ushort)Mathf.Clamp(serverPort, 1, 65535);
        public long ExpiresAtUnixMilliseconds => expiresAtUnixMilliseconds;
        public MatchAdmissionFileEntry[] Entries => entries ?? Array.Empty<MatchAdmissionFileEntry>();
    }

    public static class MatchReservationFile
    {
        public const int DocumentVersion = 1;
        public const string ReservationFileArgument = "-swingpopMatchReservationFile=";
        public const string ReadyFileArgument = "-swingpopServerReadyFile=";

        public static MatchReservationFileDocument Create(MatchReservation reservation,
            DevelopmentMatchAdmissionRegistry registry)
        {
            if (reservation == null || registry == null) throw new ArgumentNullException();
            MatchAdmissionFileEntry[] entries = new MatchAdmissionFileEntry[reservation.Grants.Length];
            for (int index = 0; index < reservation.Grants.Length; index++)
            {
                MatchAdmissionGrant grant = reservation.Grants[index];
                entries[index] = new MatchAdmissionFileEntry(grant.PlayerAccountId, grant.MatchPlayerId,
                    registry.ExportHash(grant.PlayerAccountId), reservation.ExpiresAtUnixMilliseconds);
            }
            return new MatchReservationFileDocument(DocumentVersion, reservation.LobbyMatchId,
                reservation.GameMatchId, reservation.ServerAddress, reservation.ServerPort,
                reservation.ExpiresAtUnixMilliseconds, entries);
        }

        public static void Write(string path, MatchReservationFileDocument document)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Reservation path is required.", nameof(path));
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(document, true));
        }

        public static bool TryLoad(string path, out MatchReservationFileDocument document,
            out DevelopmentMatchAdmissionRegistry registry)
        {
            document = null;
            registry = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
                document = JsonUtility.FromJson<MatchReservationFileDocument>(File.ReadAllText(path));
                if (document == null || document.DocumentVersion != DocumentVersion
                    || !document.LobbyMatchId.IsValid || !document.GameMatchId.IsValid
                    || document.Entries.Length != LobbyProtocol.MatchPlayerCapacity) return false;
                registry = new DevelopmentMatchAdmissionRegistry(document.GameMatchId);
                foreach (MatchAdmissionFileEntry entry in document.Entries)
                {
                    if (!entry.AccountId.IsValid || !entry.PlayerId.IsValid
                        || string.IsNullOrWhiteSpace(entry.TicketHashBase64)) return false;
                    registry.Import(entry.AccountId, entry.PlayerId, entry.TicketHashBase64,
                        entry.ExpiresAtUnixMilliseconds);
                }
                return true;
            }
            catch (Exception)
            {
                document = null;
                registry = null;
                return false;
            }
        }

        public static string ReadArgument(string[] arguments, string prefix)
        {
            if (arguments == null) return string.Empty;
            foreach (string argument in arguments)
                if (argument != null && argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return argument.Substring(prefix.Length).Trim().Trim('"');
            return string.Empty;
        }

        public static bool TryWriteReadyMarker(string[] arguments, MatchId gameMatchId, string endpoint)
        {
            string path = ReadArgument(arguments, ReadyFileArgument);
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(path, $"READY game={gameMatchId} endpoint={endpoint}");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>One-scene control-plane handoff. It contains no gameplay state and is consumed once.</summary>
    public static class MatchAdmissionHandoff
    {
        private static MatchAdmissionGrant pending;
        public static bool HasPending => pending.IsValid;

        public static void Set(MatchAdmissionGrant grant)
        {
            if (!grant.IsValid) throw new ArgumentException("A valid admission grant is required.", nameof(grant));
            pending = grant;
        }

        public static bool TryPeek(out MatchAdmissionGrant grant)
        {
            grant = pending;
            return grant.IsValid;
        }

        public static bool TryConsume(out MatchAdmissionGrant grant)
        {
            grant = pending;
            if (!grant.IsValid) return false;
            pending = default;
            return true;
        }

        public static void Clear() => pending = default;
    }
}

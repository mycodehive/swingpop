using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SwingPop.Online
{
    [Serializable]
    public struct PlayerAccountId : IEquatable<PlayerAccountId>
    {
        [SerializeField] private string value;

        public PlayerAccountId(string value) => this.value = value?.Trim() ?? string.Empty;
        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value) && Value.Length <= 64;
        public bool Equals(PlayerAccountId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PlayerAccountId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(PlayerAccountId left, PlayerAccountId right) => left.Equals(right);
        public static bool operator !=(PlayerAccountId left, PlayerAccountId right) => !left.Equals(right);
    }

    [Serializable]
    public struct AuthSessionId : IEquatable<AuthSessionId>
    {
        [SerializeField] private string value;

        public AuthSessionId(string value) => this.value = value?.Trim() ?? string.Empty;
        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value) && Value.Length <= 64;
        public bool Equals(AuthSessionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AuthSessionId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(AuthSessionId left, AuthSessionId right) => left.Equals(right);
        public static bool operator !=(AuthSessionId left, AuthSessionId right) => !left.Equals(right);
    }

    public enum AuthenticationClientState
    {
        None,
        CredentialReady,
        Authenticating,
        Authenticated,
        Rejected,
        Disconnected
    }

    public enum AuthenticationRejectReason
    {
        None,
        MissingCredential,
        InvalidCredential,
        InvalidSignature,
        ExpiredCredential,
        UnsupportedVersion,
        SessionRevoked,
        AlreadyAuthenticated,
        SessionConflict,
        RateLimited,
        AuthenticationRequired,
        AccountOwnershipMismatch,
        DevelopmentProviderDisabled
    }

    [Serializable]
    public struct AuthenticationTokenClaims
    {
        [SerializeField] private int tokenVersion;
        [SerializeField] private string issuer;
        [SerializeField] private PlayerAccountId playerAccountId;
        [SerializeField] private AuthSessionId authSessionId;
        [SerializeField] private long issuedAtUnixMilliseconds;
        [SerializeField] private long expiresAtUnixMilliseconds;
        [SerializeField] private string nonce;

        public AuthenticationTokenClaims(int tokenVersion, string issuer, PlayerAccountId playerAccountId,
            AuthSessionId authSessionId, long issuedAtUnixMilliseconds, long expiresAtUnixMilliseconds, string nonce)
        {
            this.tokenVersion = tokenVersion;
            this.issuer = issuer ?? string.Empty;
            this.playerAccountId = playerAccountId;
            this.authSessionId = authSessionId;
            this.issuedAtUnixMilliseconds = issuedAtUnixMilliseconds;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
            this.nonce = nonce ?? string.Empty;
        }

        public int TokenVersion => tokenVersion;
        public string Issuer => issuer ?? string.Empty;
        public PlayerAccountId PlayerAccountId => playerAccountId;
        public AuthSessionId AuthSessionId => authSessionId;
        public long IssuedAtUnixMilliseconds => issuedAtUnixMilliseconds;
        public long ExpiresAtUnixMilliseconds => expiresAtUnixMilliseconds;
        public string Nonce => nonce ?? string.Empty;
    }

    [Serializable]
    public struct AuthRequestMessage
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private string credential;
        [SerializeField] private string clientNonce;

        public AuthRequestMessage(string credential, string clientNonce)
        {
            protocolVersion = OnlineProtocol.CurrentVersion;
            this.credential = credential ?? string.Empty;
            this.clientNonce = clientNonce ?? string.Empty;
        }

        public int ProtocolVersion => protocolVersion;
        public string Credential => credential ?? string.Empty;
        public string ClientNonce => clientNonce ?? string.Empty;
    }

    [Serializable]
    public struct AuthAcceptedMessage
    {
        [SerializeField] private PlayerAccountId playerAccountId;
        [SerializeField] private AuthSessionId authSessionId;
        [SerializeField] private long sessionExpiryUnixMilliseconds;

        public AuthAcceptedMessage(PlayerAccountId playerAccountId, AuthSessionId authSessionId,
            long sessionExpiryUnixMilliseconds)
        {
            this.playerAccountId = playerAccountId;
            this.authSessionId = authSessionId;
            this.sessionExpiryUnixMilliseconds = sessionExpiryUnixMilliseconds;
        }

        public PlayerAccountId PlayerAccountId => playerAccountId;
        public AuthSessionId AuthSessionId => authSessionId;
        public long SessionExpiryUnixMilliseconds => sessionExpiryUnixMilliseconds;
    }

    [Serializable]
    public struct AuthRejectedMessage
    {
        [SerializeField] private AuthenticationRejectReason reason;
        [SerializeField] private string detail;

        public AuthRejectedMessage(AuthenticationRejectReason reason, string detail)
        {
            this.reason = reason;
            this.detail = detail ?? string.Empty;
        }

        public AuthenticationRejectReason Reason => reason;
        public string Detail => detail ?? string.Empty;
    }

    public readonly struct AuthenticationValidationResult
    {
        private AuthenticationValidationResult(bool accepted, AuthenticationRejectReason reason,
            AuthenticationTokenClaims claims)
        {
            Accepted = accepted;
            Reason = reason;
            Claims = claims;
        }

        public bool Accepted { get; }
        public AuthenticationRejectReason Reason { get; }
        public AuthenticationTokenClaims Claims { get; }
        public static AuthenticationValidationResult Accept(AuthenticationTokenClaims claims) => new(true, AuthenticationRejectReason.None, claims);
        public static AuthenticationValidationResult Reject(AuthenticationRejectReason reason) => new(false, reason, default);
    }

    public interface IAuthenticationService
    {
        AuthenticationValidationResult ValidateCredential(string credential, long nowMilliseconds);
    }

    /// <summary>
    /// Development-only HMAC provider. The signing key is supplied at runtime and is never serialized in a Unity asset.
    /// A production provider can replace this interface without changing match or gameplay code.
    /// </summary>
    public sealed class DevelopmentAuthenticationProvider : IAuthenticationService
    {
        public const int TokenVersion = 1;
        private const long FutureIssueToleranceMilliseconds = 60_000L;
        private readonly byte[] signingKey;
        private readonly string issuer;

        public DevelopmentAuthenticationProvider(byte[] signingKey, string issuer)
        {
            if (signingKey == null || signingKey.Length < 32)
                throw new ArgumentException("Development signing key must contain at least 256 bits.", nameof(signingKey));
            this.signingKey = (byte[])signingKey.Clone();
            this.issuer = string.IsNullOrWhiteSpace(issuer) ? "swingpop-development" : issuer.Trim();
        }

        public string IssueCredential(PlayerAccountId accountId, long nowMilliseconds, long lifetimeMilliseconds)
        {
            if (!accountId.IsValid) throw new ArgumentException("A valid fake development account is required.", nameof(accountId));
            long expiry = nowMilliseconds + Math.Max(1_000L, lifetimeMilliseconds);
            AuthenticationTokenClaims claims = new(TokenVersion, issuer, accountId,
                new AuthSessionId(CreateRandomIdentifier()), nowMilliseconds, expiry, CreateRandomIdentifier());
            return IssueCredential(claims);
        }

        public string IssueCredential(AuthenticationTokenClaims claims)
        {
            string payload = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonUtility.ToJson(claims)));
            return payload + "." + Base64UrlEncode(Sign(payload));
        }

        public AuthenticationValidationResult ValidateCredential(string credential, long nowMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(credential) || credential.Length > 4096)
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.MissingCredential);
            string[] parts = credential.Split('.');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.InvalidCredential);
            byte[] providedSignature;
            byte[] payloadBytes;
            try
            {
                payloadBytes = Base64UrlDecode(parts[0]);
                providedSignature = Base64UrlDecode(parts[1]);
            }
            catch (FormatException)
            {
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.InvalidCredential);
            }
            if (!FixedTimeEquals(Sign(parts[0]), providedSignature))
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.InvalidSignature);

            AuthenticationTokenClaims claims;
            try
            {
                claims = JsonUtility.FromJson<AuthenticationTokenClaims>(Encoding.UTF8.GetString(payloadBytes));
            }
            catch (Exception)
            {
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.InvalidCredential);
            }
            if (claims.TokenVersion != TokenVersion)
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.UnsupportedVersion);
            if (!string.Equals(claims.Issuer, issuer, StringComparison.Ordinal)
                || !claims.PlayerAccountId.IsValid || !claims.AuthSessionId.IsValid
                || string.IsNullOrWhiteSpace(claims.Nonce))
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.InvalidCredential);
            if (claims.IssuedAtUnixMilliseconds > nowMilliseconds + FutureIssueToleranceMilliseconds)
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.InvalidCredential);
            if (claims.ExpiresAtUnixMilliseconds <= nowMilliseconds
                || claims.ExpiresAtUnixMilliseconds <= claims.IssuedAtUnixMilliseconds)
                return AuthenticationValidationResult.Reject(AuthenticationRejectReason.ExpiredCredential);
            return AuthenticationValidationResult.Accept(claims);
        }

        public static byte[] CreateSigningKey()
        {
            byte[] key = new byte[32];
            using RandomNumberGenerator generator = RandomNumberGenerator.Create();
            generator.GetBytes(key);
            return key;
        }

        public static string Fingerprint(string value)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            return BitConverter.ToString(hash, 0, 4).Replace("-", string.Empty);
        }

        private byte[] Sign(string payload)
        {
            using HMACSHA256 hmac = new(signingKey);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }

        private static string CreateRandomIdentifier()
        {
            byte[] bytes = new byte[16];
            using RandomNumberGenerator generator = RandomNumberGenerator.Create();
            generator.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        private static bool FixedTimeEquals(byte[] expected, byte[] actual)
        {
            if (expected == null || actual == null || expected.Length != actual.Length) return false;
            int difference = 0;
            for (int index = 0; index < expected.Length; index++) difference |= expected[index] ^ actual[index];
            return difference == 0;
        }

        private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string value)
        {
            string padded = value.Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => string.Empty };
            return Convert.FromBase64String(padded);
        }
    }

    public readonly struct AuthenticationBindingResult
    {
        private AuthenticationBindingResult(bool accepted, AuthenticationRejectReason reason, AuthenticatedPlayerSession session)
        {
            Accepted = accepted;
            Reason = reason;
            Session = session;
        }

        public bool Accepted { get; }
        public AuthenticationRejectReason Reason { get; }
        public AuthenticatedPlayerSession Session { get; }
        public static AuthenticationBindingResult Accept(AuthenticatedPlayerSession session) => new(true, AuthenticationRejectReason.None, session);
        public static AuthenticationBindingResult Reject(AuthenticationRejectReason reason) => new(false, reason, default);
    }

    public readonly struct AuthenticatedPlayerSession
    {
        public AuthenticatedPlayerSession(PlayerAccountId accountId, AuthSessionId sessionId,
            long createdAtMilliseconds, long expiresAtMilliseconds, bool revoked)
        {
            AccountId = accountId;
            SessionId = sessionId;
            CreatedAtMilliseconds = createdAtMilliseconds;
            ExpiresAtMilliseconds = expiresAtMilliseconds;
            Revoked = revoked;
        }

        public PlayerAccountId AccountId { get; }
        public AuthSessionId SessionId { get; }
        public long CreatedAtMilliseconds { get; }
        public long ExpiresAtMilliseconds { get; }
        public bool Revoked { get; }
    }

    /// <summary>Server source of truth for connection/account/session binding. Gameplay state is deliberately absent.</summary>
    public sealed class AuthenticatedConnectionRegistry
    {
        private sealed class SessionRecord
        {
            public PlayerAccountId AccountId;
            public AuthSessionId SessionId;
            public long CreatedAt;
            public long ExpiresAt;
            public bool Revoked;
            public int? ConnectionId;
        }

        private readonly Dictionary<AuthSessionId, SessionRecord> sessions = new();
        private readonly Dictionary<int, SessionRecord> connections = new();
        private readonly Dictionary<PlayerAccountId, SessionRecord> activeAccounts = new();
        private readonly IAuthenticationService authenticationService;
        private readonly long maximumSessionLifetimeMilliseconds;

        public AuthenticatedConnectionRegistry(IAuthenticationService authenticationService,
            long maximumSessionLifetimeMilliseconds)
        {
            this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            this.maximumSessionLifetimeMilliseconds = Math.Max(1_000L, maximumSessionLifetimeMilliseconds);
        }

        public int ActiveConnectionCount => connections.Count;
        public int SessionCount => sessions.Count;

        public AuthenticationBindingResult Authenticate(int connectionId, string credential, long nowMilliseconds)
        {
            if (connections.ContainsKey(connectionId))
                return AuthenticationBindingResult.Reject(AuthenticationRejectReason.AlreadyAuthenticated);
            AuthenticationValidationResult validation = authenticationService.ValidateCredential(credential, nowMilliseconds);
            if (!validation.Accepted) return AuthenticationBindingResult.Reject(validation.Reason);
            AuthenticationTokenClaims claims = validation.Claims;
            if (activeAccounts.ContainsKey(claims.PlayerAccountId))
                return AuthenticationBindingResult.Reject(AuthenticationRejectReason.SessionConflict);

            if (!sessions.TryGetValue(claims.AuthSessionId, out SessionRecord record))
            {
                record = new SessionRecord
                {
                    AccountId = claims.PlayerAccountId,
                    SessionId = claims.AuthSessionId,
                    CreatedAt = nowMilliseconds,
                    ExpiresAt = Math.Min(claims.ExpiresAtUnixMilliseconds,
                        nowMilliseconds + maximumSessionLifetimeMilliseconds)
                };
                sessions.Add(record.SessionId, record);
            }
            else if (record.AccountId != claims.PlayerAccountId)
            {
                return AuthenticationBindingResult.Reject(AuthenticationRejectReason.InvalidCredential);
            }

            if (record.Revoked) return AuthenticationBindingResult.Reject(AuthenticationRejectReason.SessionRevoked);
            if (record.ExpiresAt <= nowMilliseconds)
                return AuthenticationBindingResult.Reject(AuthenticationRejectReason.ExpiredCredential);
            record.ConnectionId = connectionId;
            connections[connectionId] = record;
            activeAccounts[record.AccountId] = record;
            return AuthenticationBindingResult.Accept(ToValue(record));
        }

        public bool TryGetConnection(int connectionId, out AuthenticatedPlayerSession session)
        {
            if (connections.TryGetValue(connectionId, out SessionRecord record) && !record.Revoked)
            {
                session = ToValue(record);
                return true;
            }
            session = default;
            return false;
        }

        public bool TryGetConnection(int connectionId, long nowMilliseconds, out AuthenticatedPlayerSession session)
        {
            if (TryGetConnection(connectionId, out session) && session.ExpiresAtMilliseconds > nowMilliseconds)
                return true;
            session = default;
            return false;
        }

        public bool RemoveConnection(int connectionId)
        {
            if (!connections.TryGetValue(connectionId, out SessionRecord record)) return false;
            connections.Remove(connectionId);
            if (record.ConnectionId == connectionId) record.ConnectionId = null;
            activeAccounts.Remove(record.AccountId);
            return true;
        }

        public bool Revoke(AuthSessionId sessionId)
        {
            if (!sessions.TryGetValue(sessionId, out SessionRecord record)) return false;
            record.Revoked = true;
            if (record.ConnectionId.HasValue) connections.Remove(record.ConnectionId.Value);
            activeAccounts.Remove(record.AccountId);
            record.ConnectionId = null;
            return true;
        }

        public void Reset()
        {
            connections.Clear();
            activeAccounts.Clear();
            sessions.Clear();
        }

        private static AuthenticatedPlayerSession ToValue(SessionRecord record) => new(record.AccountId,
            record.SessionId, record.CreatedAt, record.ExpiresAt, record.Revoked);
    }

    public sealed class MatchPlayerOwnershipRegistry
    {
        private readonly Dictionary<MatchPlayerId, PlayerAccountId> owners = new();

        public int Count => owners.Count;
        public bool TryBind(MatchPlayerId playerId, PlayerAccountId accountId)
        {
            if (!playerId.IsValid || !accountId.IsValid || owners.ContainsKey(playerId)) return false;
            foreach (PlayerAccountId owner in owners.Values)
                if (owner == accountId) return false;
            owners[playerId] = accountId;
            return true;
        }

        public bool IsOwner(MatchPlayerId playerId, PlayerAccountId accountId) =>
            owners.TryGetValue(playerId, out PlayerAccountId owner) && owner == accountId;

        public bool TryGetOwner(MatchPlayerId playerId, out PlayerAccountId accountId) => owners.TryGetValue(playerId, out accountId);
        public void Reset() => owners.Clear();
    }

    public static class AuthenticationMessagePolicy
    {
        public static bool IsAllowedBeforeAuthentication(NetworkMessageType messageType) =>
            messageType is NetworkMessageType.AuthRequest or NetworkMessageType.Ping
                or NetworkMessageType.DisconnectNotice or NetworkMessageType.ConnectivityRequest;
    }
}

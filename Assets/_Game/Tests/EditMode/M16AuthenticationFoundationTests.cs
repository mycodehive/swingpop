using NUnit.Framework;
using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Tests
{
    public sealed class M16AuthenticationFoundationTests
    {
        private const long Now = 1_900_000_000_000L;
        private byte[] key;
        private DevelopmentAuthenticationProvider provider;

        [SetUp]
        public void SetUp()
        {
            key = DevelopmentAuthenticationProvider.CreateSigningKey();
            provider = new DevelopmentAuthenticationProvider(key, "m16-tests");
        }

        [Test] public void A_AccountAndSessionIdsHaveSeparateValueSemantics()
        {
            Assert.That(new PlayerAccountId(" account-a ").Value, Is.EqualTo("account-a"));
            Assert.That(new AuthSessionId("session-a"), Is.Not.EqualTo(new AuthSessionId("session-b")));
        }

        [Test] public void B_TokenClaimsRoundTripThroughJson()
        {
            AuthenticationTokenClaims claims = Claims(1, Now, Now + 60_000L);
            AuthenticationTokenClaims restored = JsonUtility.FromJson<AuthenticationTokenClaims>(JsonUtility.ToJson(claims));
            Assert.That(restored.PlayerAccountId, Is.EqualTo(claims.PlayerAccountId));
            Assert.That(restored.AuthSessionId, Is.EqualTo(claims.AuthSessionId));
        }

        [Test] public void C_ValidSignedCredentialIsAccepted()
        {
            Assert.That(provider.ValidateCredential(provider.IssueCredential(Claims(1, Now, Now + 60_000L)), Now).Accepted, Is.True);
        }

        [Test] public void D_TamperedCredentialIsRejectedBySignature()
        {
            string token = provider.IssueCredential(Claims(1, Now, Now + 60_000L));
            token = token.Substring(0, token.Length - 1) + (token.EndsWith("A") ? "B" : "A");
            Assert.That(provider.ValidateCredential(token, Now).Reason, Is.EqualTo(AuthenticationRejectReason.InvalidSignature));
        }

        [Test] public void E_ExpiredCredentialIsRejected()
        {
            string token = provider.IssueCredential(Claims(1, Now - 120_000L, Now - 60_000L));
            Assert.That(provider.ValidateCredential(token, Now).Reason, Is.EqualTo(AuthenticationRejectReason.ExpiredCredential));
        }

        [Test] public void F_UnsupportedTokenVersionIsRejected()
        {
            string token = provider.IssueCredential(Claims(2, Now, Now + 60_000L));
            Assert.That(provider.ValidateCredential(token, Now).Reason, Is.EqualTo(AuthenticationRejectReason.UnsupportedVersion));
        }

        [Test] public void G_AuthenticationCreatesServerOwnedConnectionBinding()
        {
            AuthenticatedConnectionRegistry registry = Registry();
            AuthenticationBindingResult result = registry.Authenticate(7, Token("account-a", "session-a"), Now);
            Assert.That(result.Accepted, Is.True);
            Assert.That(registry.TryGetConnection(7, out AuthenticatedPlayerSession session), Is.True);
            Assert.That(session.AccountId.Value, Is.EqualTo("account-a"));
        }

        [Test] public void H_DuplicateActiveAccountIsRejected()
        {
            AuthenticatedConnectionRegistry registry = Registry();
            Assert.That(registry.Authenticate(1, Token("account-a", "session-a"), Now).Accepted, Is.True);
            Assert.That(registry.Authenticate(2, Token("account-a", "session-b"), Now).Reason,
                Is.EqualTo(AuthenticationRejectReason.SessionConflict));
        }

        [Test] public void I_DisconnectReleasesConnectionButPreservesSession()
        {
            AuthenticatedConnectionRegistry registry = Registry();
            string token = Token("account-a", "session-a");
            registry.Authenticate(1, token, Now);
            Assert.That(registry.RemoveConnection(1), Is.True);
            Assert.That(registry.SessionCount, Is.EqualTo(1));
            Assert.That(registry.Authenticate(2, token, Now + 1_000L).Accepted, Is.True);
        }

        [Test] public void J_RevokedSessionCannotReauthenticate()
        {
            AuthenticatedConnectionRegistry registry = Registry();
            string token = Token("account-a", "session-a");
            AuthenticationBindingResult first = registry.Authenticate(1, token, Now);
            Assert.That(registry.Revoke(first.Session.SessionId), Is.True);
            Assert.That(registry.Authenticate(2, token, Now + 1_000L).Reason,
                Is.EqualTo(AuthenticationRejectReason.SessionRevoked));
        }

        [Test] public void K_MatchPlayerOwnershipIsOneAccountPerSlot()
        {
            MatchPlayerOwnershipRegistry ownership = new();
            Assert.That(ownership.TryBind(new MatchPlayerId("player-a"), new PlayerAccountId("account-a")), Is.True);
            Assert.That(ownership.TryBind(new MatchPlayerId("player-b"), new PlayerAccountId("account-a")), Is.False);
            Assert.That(ownership.IsOwner(new MatchPlayerId("player-a"), new PlayerAccountId("account-a")), Is.True);
        }

        [Test] public void L_ReconnectRequiresSameAuthenticatedAccount()
        {
            ReconnectSessionRegistry reconnect = new(new FixedTokenSource());
            PlayerAccountId owner = new("account-a");
            ReconnectTicket ticket = reconnect.Register(new MatchId("m16"), new MatchPlayerId("player-a"), owner, Now);
            reconnect.TryEnterGrace(ticket.PlayerId, Now + 1_000L, 30_000L, out _);
            ReconnectValidationResult result = reconnect.ValidateAndRotate(new ReconnectRequestMessage(ticket, 0),
                Now + 2_000L, false, owner);
            Assert.That(result.Accepted, Is.True);
        }

        [Test] public void M_StolenReconnectTicketIsRejectedForDifferentAccount()
        {
            ReconnectSessionRegistry reconnect = new(new FixedTokenSource());
            ReconnectTicket ticket = reconnect.Register(new MatchId("m16"), new MatchPlayerId("player-a"),
                new PlayerAccountId("account-a"), Now);
            reconnect.TryEnterGrace(ticket.PlayerId, Now + 1_000L, 30_000L, out _);
            ReconnectValidationResult result = reconnect.ValidateAndRotate(new ReconnectRequestMessage(ticket, 0),
                Now + 2_000L, false, new PlayerAccountId("account-b"));
            Assert.That(result.Reason, Is.EqualTo(ReconnectRejectReason.AccountOwnershipMismatch));
        }

        [Test] public void N_AuthenticationMessagesHaveStrictDirections()
        {
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.AuthRequest), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.AuthAccepted), Is.False);
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.AuthAccepted), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.AuthRequest), Is.False);
        }

        [Test] public void O_UnauthenticatedShotAndReconnectAreDenied()
        {
            Assert.That(AuthenticationMessagePolicy.IsAllowedBeforeAuthentication(NetworkMessageType.ShotSubmission), Is.False);
            Assert.That(AuthenticationMessagePolicy.IsAllowedBeforeAuthentication(NetworkMessageType.ReconnectRequest), Is.False);
            Assert.That(AuthenticationMessagePolicy.IsAllowedBeforeAuthentication(NetworkMessageType.AuthRequest), Is.True);
        }

        private AuthenticationTokenClaims Claims(int version, long issued, long expires) =>
            new(version, "m16-tests", new PlayerAccountId("account-a"), new AuthSessionId("session-a"), issued, expires, "nonce-a");

        private string Token(string account, string session) => provider.IssueCredential(new AuthenticationTokenClaims(
            1, "m16-tests", new PlayerAccountId(account), new AuthSessionId(session), Now, Now + 120_000L, "nonce-" + session));

        private AuthenticatedConnectionRegistry Registry() => new(provider, 120_000L);

        private sealed class FixedTokenSource : IReconnectTokenSource
        {
            private int sequence;
            public string CreateSecret() => "m16-fixed-token-" + ++sequence + "-abcdefghijklmnopqrstuvwxyz";
        }
    }
}

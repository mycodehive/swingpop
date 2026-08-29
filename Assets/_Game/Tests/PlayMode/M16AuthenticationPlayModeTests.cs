using System;
using System.Collections;
using NUnit.Framework;
using SwingPop.Gameplay.Course;
using SwingPop.Online;
using UnityEngine;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class M16AuthenticationPlayModeTests
    {
        private static int nextPort = 19260;
        private readonly byte[] key = DevelopmentAuthenticationProvider.CreateSigningKey();
        private GameObject serverObject;
        private GameObject aObject;
        private GameObject bObject;
        private GameObject replacementObject;
        private LocalMatchAuthority authority;
        private DedicatedServerMatchTransport server;
        private UnityTransportMatchTransport clientA;
        private UnityTransportMatchTransport clientB;
        private UnityTransportMatchTransport replacement;
        private DevelopmentAuthenticationProvider provider;
        private ReconnectTicket ticketA;

        [SetUp]
        public void SetUp() => provider = new DevelopmentAuthenticationProvider(key, "m16-play");

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (replacementObject != null) UnityEngine.Object.Destroy(replacementObject);
            if (bObject != null) UnityEngine.Object.Destroy(bObject);
            if (aObject != null) UnityEngine.Object.Destroy(aObject);
            if (serverObject != null) UnityEngine.Object.Destroy(serverObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_DistinctAccountsAuthenticateBeforeMatchAssignment()
        {
            yield return ConnectAuthenticatedMatch();
            Assert.That(clientA.AuthenticationState, Is.EqualTo(AuthenticationClientState.Authenticated));
            Assert.That(clientB.AuthenticationState, Is.EqualTo(AuthenticationClientState.Authenticated));
            Assert.That(clientA.AuthenticatedAccountId.Value, Is.EqualTo("dev-player-a"));
            Assert.That(clientB.AuthenticatedAccountId.Value, Is.EqualTo("dev-player-b"));
            Assert.That(server.TryGetMatchOwner(new MatchPlayerId("player-a"), out PlayerAccountId owner), Is.True);
            Assert.That(owner.Value, Is.EqualTo("dev-player-a"));
        }

        [UnityTest]
        public IEnumerator B_TamperedCredentialIsRejectedBeforePlayerAssignment()
        {
            StartServer();
            CreateClientA(Tampered(Token("dev-player-a", "session-a")));
            AuthenticationRejectReason reason = AuthenticationRejectReason.None;
            clientA.AuthenticationRejected += value => reason = value.Reason;
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != AuthenticationRejectReason.None, 3f, "tamper rejection");
            Assert.That(reason, Is.EqualTo(AuthenticationRejectReason.InvalidSignature));
            Assert.That(clientA.AssignedPlayer.IsValid, Is.False);
            Assert.That(server.ReservedPlayerCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator C_UnauthenticatedClientCannotEnterMatch()
        {
            StartServer();
            CreateClientA(null);
            AuthenticationRejectReason reason = AuthenticationRejectReason.None;
            clientA.AuthenticationRejected += value => reason = value.Reason;
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != AuthenticationRejectReason.None, 3f, "authentication required");
            Assert.That(reason, Is.EqualTo(AuthenticationRejectReason.AuthenticationRequired));
            Assert.That(server.ConnectedPlayerCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator D_DuplicateActiveAccountIsRejected()
        {
            StartServer();
            CreateClientA(Token("dev-player-a", "session-a"));
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => clientA.AssignedPlayer.IsValid, 3f, "first account admitted");
            CreateClientB(Token("dev-player-a", "session-b"));
            AuthenticationRejectReason reason = AuthenticationRejectReason.None;
            clientB.AuthenticationRejected += value => reason = value.Reason;
            clientB.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != AuthenticationRejectReason.None, 3f, "duplicate rejection");
            Assert.That(reason, Is.EqualTo(AuthenticationRejectReason.SessionConflict));
            Assert.That(server.ConnectedPlayerCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator E_ReconnectWithSameAccountRestoresSamePlayer()
        {
            yield return ConnectAuthenticatedMatch();
            clientA.CancelPending();
            yield return WaitFor(() => server.IsMatchSuspended, 3f, "reconnect grace");
            CreateReplacement(Token("dev-player-a", "session-a-reconnect"), ticketA);
            replacement.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => replacement.IsReady, 4f, "same-account reconnect");
            Assert.That(replacement.AssignedPlayer.Value, Is.EqualTo("player-a"));
            Assert.That(replacement.AuthenticatedAccountId.Value, Is.EqualTo("dev-player-a"));
        }

        [UnityTest]
        public IEnumerator F_StolenReconnectTicketIsRejectedForOtherAccount()
        {
            yield return ConnectAuthenticatedMatch();
            clientA.CancelPending();
            yield return WaitFor(() => server.IsMatchSuspended, 3f, "reconnect grace");
            CreateReplacement(Token("dev-player-c", "session-c"), ticketA);
            ReconnectRejectReason reason = ReconnectRejectReason.None;
            replacement.ReconnectRejected += value => reason = value.Reason;
            replacement.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != ReconnectRejectReason.None, 4f, "ownership rejection");
            Assert.That(reason, Is.EqualTo(ReconnectRejectReason.AccountOwnershipMismatch));
            Assert.That(server.IsMatchSuspended, Is.True);
        }

        private IEnumerator ConnectAuthenticatedMatch()
        {
            StartServer();
            CreateClientA(Token("dev-player-a", "session-a"));
            CreateClientB(Token("dev-player-b", "session-b"));
            clientA.ReconnectTicketChanged += value => ticketA = value;
            server.AllPlayersReady += () => server.BeginDedicatedMatch(authority.StartMatch(
                new MatchId("m16-play"), "hole-01", InitialPlayers()));
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => clientA.AssignedPlayer.IsValid, 3f, "A assigned");
            clientB.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => server.IsReady && clientA.IsReady && clientB.IsReady && ticketA.IsValid,
                4f, "authenticated match ready");
        }

        private void StartServer()
        {
            serverObject = new GameObject("M16 Server");
            authority = serverObject.AddComponent<LocalMatchAuthority>();
            server = serverObject.AddComponent<DedicatedServerMatchTransport>();
            server.Configure(authority, 2, false);
            server.ConfigureReconnectPolicy(30f);
            server.ConfigureAuthentication(true, key, "m16-play", 300f, 8f);
            ushort port = (ushort)nextPort++;
            Assert.That(server.StartDedicatedServer("127.0.0.1", port, 5f), Is.True);
        }

        private void CreateClientA(string token)
        {
            aObject = new GameObject("M16 Client A");
            clientA = aObject.AddComponent<UnityTransportMatchTransport>();
            clientA.Configure(null, false);
            if (!string.IsNullOrWhiteSpace(token)) Assert.That(clientA.SetAuthenticationCredential(token), Is.True);
        }

        private void CreateClientB(string token)
        {
            bObject = new GameObject("M16 Client B");
            clientB = bObject.AddComponent<UnityTransportMatchTransport>();
            clientB.Configure(null, false);
            Assert.That(clientB.SetAuthenticationCredential(token), Is.True);
        }

        private void CreateReplacement(string token, ReconnectTicket ticket)
        {
            replacementObject = new GameObject("M16 Replacement");
            replacement = replacementObject.AddComponent<UnityTransportMatchTransport>();
            replacement.Configure(null, false);
            Assert.That(replacement.SetAuthenticationCredential(token), Is.True);
            Assert.That(replacement.SetPendingReconnectTicket(ticket), Is.True);
        }

        private string Token(string account, string session)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return provider.IssueCredential(new AuthenticationTokenClaims(1, "m16-play",
                new PlayerAccountId(account), new AuthSessionId(session), now, now + 300_000L, "nonce-" + session));
        }

        private static string Tampered(string token) => (token[0] == 'A' ? "B" : "A") + token.Substring(1);

        private static IEnumerator WaitFor(Func<bool> predicate, float timeout, string label)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True, label);
        }

        private static PlayerSnapshot[] InitialPlayers()
        {
            NetworkVector3 tee = new(0f, 0.2f, 0f);
            return new[]
            {
                new PlayerSnapshot(new MatchPlayerId("player-a"), "A", 0, 0, false,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false),
                new PlayerSnapshot(new MatchPlayerId("player-b"), "B", 1, 1, false,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false)
            };
        }
    }
}

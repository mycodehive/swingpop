using System;
using System.Collections;
using NUnit.Framework;
using SwingPop.Gameplay.Course;
using SwingPop.Online;
using UnityEngine;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class M18ConnectivityPlayModeTests
    {
        private static int nextPort = 21180;
        private readonly byte[] key = DevelopmentAuthenticationProvider.CreateSigningKey();
        private GameObject serverObject;
        private GameObject aObject;
        private GameObject bObject;
        private GameObject replacementObject;
        private DedicatedServerMatchTransport server;
        private LocalMatchAuthority authority;
        private DevelopmentAuthenticationProvider auth;
        private DevelopmentMatchAdmissionRegistry admission;
        private MatchConnectivityDescriptor connectivity;
        private MatchJoinTicket ticketA;
        private MatchJoinTicket ticketB;
        private UnityTransportMatchTransport clientA;
        private UnityTransportMatchTransport clientB;
        private ReconnectTicket reconnectA;

        [SetUp]
        public void SetUp()
        {
            auth = new DevelopmentAuthenticationProvider(key, "m18-play");
            StartServer();
        }

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
        public IEnumerator A_RelayCredentialPrecedesAuthenticationAndAdmission()
        {
            CreateA(Token("dev-player-a", "session-a"), ticketA, connectivity);
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => clientA.AssignedPlayer.IsValid, 4f, "relay admission");
            Assert.That(clientA.ConnectivityState, Is.EqualTo(ConnectivityClientState.Accepted));
            Assert.That(clientA.AuthenticationState, Is.EqualTo(AuthenticationClientState.Authenticated));
            Assert.That(clientA.AssignedPlayer.Value, Is.EqualTo("player-a"));
        }

        [UnityTest]
        public IEnumerator B_WrongRelayCredentialDoesNotConsumeJoinTicket()
        {
            MatchConnectivityDescriptor wrong = new(MatchConnectivityMode.Relay, connectivity.Provider,
                connectivity.Address, connectivity.Port, connectivity.AllocationId,
                ConnectivitySecurity.CreateCredential(), connectivity.ExpiresAtUnixMilliseconds);
            CreateA(Token("dev-player-a", "bad-relay"), ticketA, wrong);
            ConnectivityRejectReason reason = ConnectivityRejectReason.None;
            clientA.ConnectivityRejected += value => reason = value.Reason;
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != ConnectivityRejectReason.None, 4f, "relay rejection");
            Assert.That(reason, Is.EqualTo(ConnectivityRejectReason.InvalidCredential));
            UnityEngine.Object.Destroy(aObject);
            yield return null;
            CreateA(Token("dev-player-a", "retry"), ticketA, connectivity);
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => clientA.AssignedPlayer.IsValid, 4f, "ticket remains usable");
            Assert.That(clientA.AssignedPlayer.Value, Is.EqualTo("player-a"));
        }

        [UnityTest]
        public IEnumerator C_RelayDoesNotReplaceAuthentication()
        {
            CreateA(null, ticketA, connectivity);
            AuthenticationRejectReason reason = AuthenticationRejectReason.None;
            clientA.AuthenticationRejected += value => reason = value.Reason;
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != AuthenticationRejectReason.None, 4f, "auth required after relay");
            Assert.That(reason, Is.EqualTo(AuthenticationRejectReason.AuthenticationRequired));
            Assert.That(clientA.AssignedPlayer.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator D_RelayDoesNotReplaceAccountBoundJoinTicket()
        {
            CreateA(Token("dev-player-c", "stolen"), ticketA, connectivity);
            MatchAdmissionRejectReason reason = MatchAdmissionRejectReason.None;
            clientA.MatchAdmissionRejected += value => reason = value.Reason;
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != MatchAdmissionRejectReason.None, 4f, "stolen ticket");
            Assert.That(reason, Is.EqualTo(MatchAdmissionRejectReason.AccountMismatch));
        }

        [UnityTest]
        public IEnumerator E_ReconnectRepeatsRelayHandshakeAndRestoresSamePlayer()
        {
            CreateA(Token("dev-player-a", "session-a"), ticketA, connectivity);
            CreateB(Token("dev-player-b", "session-b"), ticketB, connectivity);
            clientA.ReconnectTicketChanged += value => reconnectA = value;
            server.AllPlayersReady += BeginMatch;
            clientA.StartClient("127.0.0.1", server.Port, 5f);
            clientB.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => clientA.IsReady && clientB.IsReady && reconnectA.IsValid, 5f, "match ready");
            clientA.CancelPending();
            yield return WaitFor(() => server.IsMatchSuspended, 3f, "grace");
            replacementObject = new GameObject("M18 Replacement");
            UnityTransportMatchTransport replacement = replacementObject.AddComponent<UnityTransportMatchTransport>();
            replacement.Configure(null, false);
            replacement.SetConnectivityDescriptor(connectivity);
            replacement.SetAuthenticationCredential(Token("dev-player-a", "replacement"));
            replacement.SetPendingReconnectTicket(reconnectA);
            replacement.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => replacement.IsReady, 5f, "relay reconnect");
            Assert.That(replacement.ConnectivityState, Is.EqualTo(ConnectivityClientState.Accepted));
            Assert.That(replacement.AssignedPlayer.Value, Is.EqualTo("player-a"));
        }

        private void StartServer()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            MatchId game = new("m18-match");
            connectivity = new MatchConnectivityDescriptor(MatchConnectivityMode.Relay,
                ConnectivityProtocol.LocalRelayProvider, "127.0.0.1", (ushort)nextPort,
                "m18-allocation", ConnectivitySecurity.CreateCredential(), now + 300_000L);
            admission = new DevelopmentMatchAdmissionRegistry(game);
            ticketA = admission.Register(new PlayerAccountId("dev-player-a"), new MatchPlayerId("player-a"), now + 300_000L);
            ticketB = admission.Register(new PlayerAccountId("dev-player-b"), new MatchPlayerId("player-b"), now + 300_000L);
            serverObject = new GameObject("M18 Server");
            authority = serverObject.AddComponent<LocalMatchAuthority>();
            server = serverObject.AddComponent<DedicatedServerMatchTransport>();
            server.Configure(authority, 2, false);
            server.ConfigureReconnectPolicy(30f);
            server.ConfigureAuthentication(true, key, "m18-play", 300f, 8f);
            server.ConfigureMatchAdmission(admission);
            server.ConfigureConnectivity(new ConnectivityCredentialRegistry(connectivity.AllocationId,
                connectivity.Credential, connectivity.ExpiresAtUnixMilliseconds));
            Assert.That(server.StartDedicatedServer("127.0.0.1", (ushort)nextPort++, 5f), Is.True);
        }

        private void CreateA(string credential, MatchJoinTicket ticket, MatchConnectivityDescriptor descriptor)
        {
            aObject = new GameObject("M18 Client A");
            clientA = aObject.AddComponent<UnityTransportMatchTransport>();
            ConfigureClient(clientA, credential, ticket, descriptor);
        }

        private void CreateB(string credential, MatchJoinTicket ticket, MatchConnectivityDescriptor descriptor)
        {
            bObject = new GameObject("M18 Client B");
            clientB = bObject.AddComponent<UnityTransportMatchTransport>();
            ConfigureClient(clientB, credential, ticket, descriptor);
        }

        private static void ConfigureClient(UnityTransportMatchTransport client, string credential,
            MatchJoinTicket ticket, MatchConnectivityDescriptor descriptor)
        {
            client.Configure(null, false);
            Assert.That(client.SetConnectivityDescriptor(descriptor), Is.True);
            if (!string.IsNullOrWhiteSpace(credential)) Assert.That(client.SetAuthenticationCredential(credential), Is.True);
            Assert.That(client.SetMatchJoinTicket(ticket), Is.True);
        }

        private string Token(string account, string session)
        {
            long issued = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return auth.IssueCredential(new AuthenticationTokenClaims(1, "m18-play",
                new PlayerAccountId(account), new AuthSessionId(session), issued, issued + 300_000L, "nonce-" + session));
        }

        private void BeginMatch() => server.BeginDedicatedMatch(authority.StartMatch(
            new MatchId("m18-match"), "hole-01", InitialPlayers()));

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

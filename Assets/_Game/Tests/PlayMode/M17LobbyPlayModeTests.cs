using System;
using System.Collections;
using NUnit.Framework;
using SwingPop.Gameplay.Course;
using SwingPop.Online;
using UnityEngine;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class M17LobbyPlayModeTests
    {
        private static int nextPort = 20320;
        private readonly byte[] key = DevelopmentAuthenticationProvider.CreateSigningKey();
        private DevelopmentAuthenticationProvider provider;
        private GameObject serviceObject;
        private GameObject aObject;
        private GameObject bObject;
        private GameObject extraObject;
        private LobbyNetworkTransport lobbyServiceTransport;
        private LobbyNetworkTransport lobbyA;
        private LobbyNetworkTransport lobbyB;
        private PrelaunchedGameServerAllocator lobbyAllocator;
        private DedicatedServerMatchTransport matchServer;
        private LocalMatchAuthority matchAuthority;
        private UnityTransportMatchTransport matchA;
        private UnityTransportMatchTransport matchB;

        [SetUp]
        public void SetUp() => provider = new DevelopmentAuthenticationProvider(key, "m17-play");

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (extraObject != null) UnityEngine.Object.Destroy(extraObject);
            if (bObject != null) UnityEngine.Object.Destroy(bObject);
            if (aObject != null) UnityEngine.Object.Destroy(aObject);
            if (serviceObject != null) UnityEngine.Object.Destroy(serviceObject);
            yield return null;
            MatchAdmissionHandoff.Clear();
        }

        [UnityTest]
        public IEnumerator A_AuthenticatedClientEntersLobby()
        {
            yield return StartLobby(false);
            Assert.That(lobbyA.AuthenticationState, Is.EqualTo(AuthenticationClientState.Authenticated));
            Assert.That(lobbyA.ClientSession.PlayerAccountId.Value, Is.EqualTo("dev-player-a"));
            Assert.That(lobbyServiceTransport.ConnectedPeerCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator B_CreateListJoinSynchronizesTwoPlayerRoom()
        {
            yield return StartLobby(true);
            LobbyMatchSnapshot room = null;
            lobbyA.MatchUpdated += value => { if (value.EventType == LobbyEventType.MatchCreated) room = value.Match; };
            Assert.That(lobbyA.CreateMatch(new CreateMatchRequest("create", "Play Room", 2,
                LobbyProtocol.SupportedHoleId, LobbyVisibility.Public)), Is.True);
            yield return WaitFor(() => room != null, 3f, "room created");
            LobbyMatchSnapshot[] list = null;
            lobbyB.MatchListReceived += value => list = value.Matches;
            lobbyB.ListMatches(new ListMatchesRequest("list", true));
            yield return WaitFor(() => list != null && list.Length == 1, 3f, "room listed");
            LobbyMatchSnapshot joined = null;
            lobbyB.MatchUpdated += value => { if (value.EventType == LobbyEventType.MemberJoined) joined = value.Match; };
            lobbyB.JoinMatch(new LobbyMatchRequest("join", room.LobbyMatchId));
            yield return WaitFor(() => joined != null && joined.CurrentPlayers == 2, 3f, "room joined");
            Assert.That(joined.State, Is.EqualTo(LobbyMatchState.Full));
        }

        [UnityTest]
        public IEnumerator C_ReadyOwnerStartGeneratesAccountBoundAdmissionForBothClients()
        {
            yield return StartLobby(true);
            LobbyMatchSnapshot room = null;
            lobbyA.MatchUpdated += value => { if (value.EventType == LobbyEventType.MatchCreated) room = value.Match; };
            lobbyA.CreateMatch(new CreateMatchRequest("create", "Start Room", 2,
                LobbyProtocol.SupportedHoleId, LobbyVisibility.Public));
            yield return WaitFor(() => room != null, 3f, "create");
            lobbyB.JoinMatch(new LobbyMatchRequest("join", room.LobbyMatchId));
            yield return WaitFor(() => lobbyA.LatestMatch != null && lobbyA.LatestMatch.CurrentPlayers == 2, 3f, "join");
            lobbyA.SetReady(new SetReadyRequest("ready-a", room.LobbyMatchId, true));
            lobbyB.SetReady(new SetReadyRequest("ready-b", room.LobbyMatchId, true));
            yield return WaitFor(() => lobbyA.LatestMatch.Members[0].ReadyState == LobbyReadyState.Ready
                                       && lobbyA.LatestMatch.Members[1].ReadyState == LobbyReadyState.Ready,
                3f, "both ready");
            MatchAdmissionGrant grantA = default;
            MatchAdmissionGrant grantB = default;
            lobbyA.AdmissionGranted += value => grantA = value.Grant;
            lobbyB.AdmissionGranted += value => grantB = value.Grant;
            lobbyA.StartMatch(new LobbyMatchRequest("start", room.LobbyMatchId));
            yield return WaitFor(() => grantA.IsValid && grantB.IsValid, 3f, "admission grants");
            Assert.That(grantA.MatchPlayerId.Value, Is.EqualTo("player-a"));
            Assert.That(grantB.MatchPlayerId.Value, Is.EqualTo("player-b"));
            Assert.That(grantA.GameMatchId, Is.EqualTo(grantB.GameMatchId));
        }

        [UnityTest]
        public IEnumerator D_OwnerDisconnectClosesWaitingRoomForOtherClient()
        {
            yield return StartLobby(true);
            LobbyMatchSnapshot room = null;
            lobbyA.MatchUpdated += value => { if (value.EventType == LobbyEventType.MatchCreated) room = value.Match; };
            lobbyA.CreateMatch(new CreateMatchRequest("create", "Leave Room", 2,
                LobbyProtocol.SupportedHoleId, LobbyVisibility.Public));
            yield return WaitFor(() => room != null, 3f, "create");
            lobbyB.JoinMatch(new LobbyMatchRequest("join", room.LobbyMatchId));
            yield return WaitFor(() => lobbyB.LatestMatch != null && lobbyB.LatestMatch.CurrentPlayers == 2, 3f, "join");
            LobbyMatchState observed = LobbyMatchState.Full;
            lobbyB.MatchUpdated += value => observed = value.Match.State;
            lobbyA.Shutdown();
            yield return WaitFor(() => observed == LobbyMatchState.Closed, 3f, "owner disconnect closes room");
        }

        [UnityTest]
        public IEnumerator E_ValidJoinTicketsAssignReservedPlayersAndStartGameMatch()
        {
            MatchReservation reservation = CreateReservation(out DevelopmentMatchAdmissionRegistry registry);
            yield return StartDedicatedAdmissionServer(registry, reservation, true);
            Assert.That(matchA.AssignedPlayer.Value, Is.EqualTo("player-a"));
            Assert.That(matchB.AssignedPlayer.Value, Is.EqualTo("player-b"));
            Assert.That(matchServer.IsReady, Is.True);
            Assert.That(matchAuthority.CurrentSnapshot.MatchId, Is.EqualTo(reservation.GameMatchId));
        }

        [UnityTest]
        public IEnumerator F_StolenJoinTicketIsRejectedBeforePlayerAssignment()
        {
            MatchReservation reservation = CreateReservation(out DevelopmentMatchAdmissionRegistry registry);
            StartDedicatedServer(registry, reservation);
            CreateMatchClientA(Token("dev-player-c", "session-c"), reservation.Grants[0].JoinTicket);
            MatchAdmissionRejectReason reason = MatchAdmissionRejectReason.None;
            matchA.MatchAdmissionRejected += value => reason = value.Reason;
            matchA.StartClient("127.0.0.1", matchServer.Port, 5f);
            matchA.SetMatchJoinTicket(reservation.Grants[0].JoinTicket);
            yield return WaitFor(() => reason != MatchAdmissionRejectReason.None, 4f, "stolen ticket rejection");
            Assert.That(reason, Is.EqualTo(MatchAdmissionRejectReason.AccountMismatch));
            Assert.That(matchA.AssignedPlayer.IsValid, Is.False);
        }

        [UnityTest]
        public IEnumerator G_ConsumedJoinTicketReplayIsRejected()
        {
            MatchReservation reservation = CreateReservation(out DevelopmentMatchAdmissionRegistry registry);
            StartDedicatedServer(registry, reservation);
            CreateMatchClientA(Token("dev-player-a", "session-a"), reservation.Grants[0].JoinTicket);
            matchA.StartClient("127.0.0.1", matchServer.Port, 5f);
            matchA.SetMatchJoinTicket(reservation.Grants[0].JoinTicket);
            yield return WaitFor(() => matchA.AssignedPlayer.IsValid, 4f, "first admission");
            matchA.CancelPending();
            yield return WaitFor(() => matchServer.ConnectedPlayerCount == 0, 3f, "first disconnect");
            extraObject = new GameObject("M17 Replay Client");
            UnityTransportMatchTransport replay = extraObject.AddComponent<UnityTransportMatchTransport>();
            replay.Configure(null, false);
            replay.SetAuthenticationCredential(Token("dev-player-a", "session-a2"));
            MatchAdmissionRejectReason reason = MatchAdmissionRejectReason.None;
            replay.MatchAdmissionRejected += value => reason = value.Reason;
            replay.StartClient("127.0.0.1", matchServer.Port, 5f);
            replay.SetMatchJoinTicket(reservation.Grants[0].JoinTicket);
            yield return WaitFor(() => reason != MatchAdmissionRejectReason.None, 4f, "ticket replay rejection");
            Assert.That(reason, Is.EqualTo(MatchAdmissionRejectReason.Consumed));
        }

        private IEnumerator StartLobby(bool includeB)
        {
            serviceObject = new GameObject("M17 Lobby Service");
            lobbyServiceTransport = serviceObject.AddComponent<LobbyNetworkTransport>();
            lobbyAllocator = new PrelaunchedGameServerAllocator("127.0.0.1", 20999);
            InMemoryLobbyService service = new(lobbyAllocator);
            ushort port = (ushort)nextPort++;
            Assert.That(lobbyServiceTransport.StartService("127.0.0.1", port, 8, 5f,
                provider, 300_000L, service, false), Is.True);
            aObject = new GameObject("M17 Lobby A");
            lobbyA = aObject.AddComponent<LobbyNetworkTransport>();
            Assert.That(lobbyA.StartClient("127.0.0.1", port, 5f, Token("dev-player-a", "session-a"), false), Is.True);
            if (includeB)
            {
                bObject = new GameObject("M17 Lobby B");
                lobbyB = bObject.AddComponent<LobbyNetworkTransport>();
                Assert.That(lobbyB.StartClient("127.0.0.1", port, 5f, Token("dev-player-b", "session-b"), false), Is.True);
            }
            yield return WaitFor(() => lobbyA.AuthenticationState == AuthenticationClientState.Authenticated
                                       && (!includeB || lobbyB.AuthenticationState == AuthenticationClientState.Authenticated),
                4f, "lobby authentication");
        }

        private MatchReservation CreateReservation(out DevelopmentMatchAdmissionRegistry registry)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            LobbyMatchMember[] members =
            {
                new(new PlayerAccountId("dev-player-a"), "PLAYER A", 0, LobbyReadyState.Ready, true),
                new(new PlayerAccountId("dev-player-b"), "PLAYER B", 1, LobbyReadyState.Ready, false)
            };
            LobbyMatchSnapshot snapshot = new(new LobbyMatchId("room"), "Room", 2, LobbyMatchState.Starting,
                "hole-01", now, LobbyVisibility.Public, false, 4, default, members);
            PrelaunchedGameServerAllocator allocator = new("127.0.0.1", (ushort)nextPort++, 60_000L);
            Assert.That(allocator.TryAllocate(snapshot, now, out MatchReservation reservation, out _), Is.True);
            registry = allocator.LastAdmissionRegistry;
            return reservation;
        }

        private IEnumerator StartDedicatedAdmissionServer(DevelopmentMatchAdmissionRegistry registry,
            MatchReservation reservation, bool includeB)
        {
            StartDedicatedServer(registry, reservation);
            CreateMatchClientA(Token("dev-player-a", "session-a"), reservation.Grants[0].JoinTicket);
            matchA.StartClient("127.0.0.1", matchServer.Port, 5f);
            matchA.SetMatchJoinTicket(reservation.Grants[0].JoinTicket);
            if (includeB)
            {
                CreateMatchClientB(Token("dev-player-b", "session-b"), reservation.Grants[1].JoinTicket);
                matchB.StartClient("127.0.0.1", matchServer.Port, 5f);
                matchB.SetMatchJoinTicket(reservation.Grants[1].JoinTicket);
            }
            yield return WaitFor(() => matchA.AssignedPlayer.IsValid
                                       && (!includeB || matchB.AssignedPlayer.IsValid) && matchServer.IsReady,
                5f, "dedicated admission");
        }

        private void StartDedicatedServer(DevelopmentMatchAdmissionRegistry registry, MatchReservation reservation)
        {
            serviceObject = new GameObject("M17 Match Server");
            matchAuthority = serviceObject.AddComponent<LocalMatchAuthority>();
            matchServer = serviceObject.AddComponent<DedicatedServerMatchTransport>();
            matchServer.Configure(matchAuthority, 2, false);
            matchServer.ConfigureReconnectPolicy(30f);
            matchServer.ConfigureAuthentication(true, key, "m17-play", 300f, 8f);
            matchServer.ConfigureMatchAdmission(registry);
            matchServer.AllPlayersReady += () => matchServer.BeginDedicatedMatch(
                matchAuthority.StartMatch(reservation.GameMatchId, "hole-01", InitialPlayers()));
            Assert.That(matchServer.StartDedicatedServer("127.0.0.1", reservation.ServerPort, 5f), Is.True);
        }

        private void CreateMatchClientA(string token, MatchJoinTicket ticket)
        {
            aObject = new GameObject("M17 Match A");
            matchA = aObject.AddComponent<UnityTransportMatchTransport>();
            matchA.Configure(null, false);
            Assert.That(matchA.SetAuthenticationCredential(token), Is.True);
        }

        private void CreateMatchClientB(string token, MatchJoinTicket ticket)
        {
            bObject = new GameObject("M17 Match B");
            matchB = bObject.AddComponent<UnityTransportMatchTransport>();
            matchB.Configure(null, false);
            Assert.That(matchB.SetAuthenticationCredential(token), Is.True);
        }

        private string Token(string account, string session)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return provider.IssueCredential(new AuthenticationTokenClaims(1, "m17-play",
                new PlayerAccountId(account), new AuthSessionId(session), now, now + 300_000L,
                "nonce-" + session));
        }

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

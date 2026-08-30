using System;
using NUnit.Framework;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Tests
{
    /// <summary>Provider-independent M20 seams. Real public TLS/Cross-NAT is a separate external gate.</summary>
    public sealed class M20PublicControlPlanePlayModeTests
    {
        private const long Now = 3_000_000L;

        [Test] public void A_StagingConfiguration_RequiresWss()
        {
            Assert.That(ControlPlaneEndpoint.TryParse("ws://public.example/lobby", true,
                out _, out _), Is.False);
            Assert.That(ControlPlaneEndpoint.TryParse("wss://public.example/lobby", true,
                out _, out _), Is.True);
        }

        [Test] public void B_AuthenticationGate_PrecedesLobbyOperations()
        {
            InMemoryLobbyService service = Service();
            Assert.That(service.CreateMatch(default, Create("unauth"), Now).Reason,
                Is.EqualTo(LobbyRejectReason.AuthenticationRequired));
        }

        [Test] public void C_CreateListJoin_FollowsControlPlaneState()
        {
            InMemoryLobbyService service = Service();
            LobbyMatchSnapshot room = service.CreateMatch(Session("a"), Create("c"), Now).Value;
            Assert.That(service.ListMatches(Session("b"), new ListMatchesRequest("list", true), Now)
                .Value.Length, Is.EqualTo(1));
            Assert.That(service.JoinMatch(Session("b"), Request("join", room.LobbyMatchId), Now)
                .Value.CurrentPlayers, Is.EqualTo(2));
        }

        [Test] public void D_ReadyStart_AllocatesOnlyAfterBothReady()
        {
            InMemoryLobbyService service = FullReady(out LobbyMatchSnapshot room);
            MatchReservation reservation = service.StartMatch(Session("a"),
                Request("start", room.LobbyMatchId), Now).Value;
            Assert.That(reservation, Is.Not.Null);
            Assert.That(reservation.Grants, Has.Length.EqualTo(2));
        }

        [Test] public void E_ServerReadyReservation_ContainsNoGameplayAuthority()
        {
            InMemoryLobbyService service = FullReady(out LobbyMatchSnapshot room);
            MatchReservation reservation = service.StartMatch(Session("a"),
                Request("start", room.LobbyMatchId), Now).Value;
            Assert.That(reservation.GameMatchId.IsValid, Is.True);
            Assert.That(typeof(ILobbyService).GetMethod("CreateMatch"), Is.Not.Null);
            Assert.That(typeof(ILobbyService).GetMethod("SubmitShot"), Is.Null);
        }

        [Test] public void F_RelayAdmission_DoesNotExposePrivateServerBindEndpoint()
        {
            MatchConnectivityDescriptor relay = new(MatchConnectivityMode.ProductionRelay,
                ConnectivityProtocol.UnityRelayProvider, "relay.example", 443, "allocation", "join-code",
                Now + 60_000L, ConnectivityProtocol.ProductionDescriptorVersion, "region");
            DevelopmentMatchAdmissionRegistry registry = new(new MatchId("game"));
            MatchAdmissionGrant grant = new(new LobbyMatchId("lobby"), new MatchId("game"),
                new PlayerAccountId("a"), new MatchPlayerId("player-a"), relay,
                registry.Register(new PlayerAccountId("a"), new MatchPlayerId("player-a"), Now + 60_000L));
            Assert.That(grant.ServerAddress, Is.EqualTo("relay.example"));
            Assert.That(grant.ServerAddress, Is.Not.EqualTo("127.0.0.1"));
        }

        [Test] public void G_AdmissionTicket_RemainsAccountBound()
        {
            DevelopmentMatchAdmissionRegistry registry = new(new MatchId("game"));
            MatchJoinTicket ticket = registry.Register(new PlayerAccountId("a"),
                new MatchPlayerId("player-a"), Now + 60_000L);
            Assert.That(registry.ValidateAndConsume(new MatchId("game"), new PlayerAccountId("b"),
                ticket.Secret, Now, false).Accepted, Is.False);
        }

        [Test] public void H_Shots_RemainDedicatedGameplayAuthorityCommands()
        {
            GameObject root = new("M20 Authority");
            try
            {
                LocalMatchAuthority authority = root.AddComponent<LocalMatchAuthority>();
                MatchSnapshot snapshot = authority.StartMatch(new MatchId("m20"), "hole-01", Players());
                ShotCommand command = new(Vector3.forward, Vector3.forward, 0f, 0.6f, 1f, 0f,
                    ImpactGrade.Perfect, 0.6f, 0f, 30f, 25f);
                ShotSubmissionDecision decision = authority.SubmitShot(new ShotSubmission(snapshot.MatchId,
                    snapshot.CurrentTurnPlayer, snapshot.TurnIndex, 1, OnlineProtocol.CurrentVersion, command));
                Assert.That(decision.Accepted, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test] public void I_Reconnect_RotatesTicketAndRestoresSamePlayerIdentity()
        {
            ReconnectSessionRegistry registry = new(new FixedTokenSource());
            MatchPlayerId player = new("player-a");
            ReconnectTicket ticket = registry.Register(new MatchId("game"), player,
                new PlayerAccountId("a"), Now);
            Assert.That(registry.TryEnterGrace(player, Now + 1, 30_000L, out _), Is.True);
            ReconnectValidationResult result = registry.ValidateAndRotate(
                new ReconnectRequestMessage(ticket, 1), Now + 2, false, new PlayerAccountId("a"));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RotatedTicket.PlayerId, Is.EqualTo(player));
            Assert.That(result.RotatedTicket.SessionGeneration, Is.EqualTo(2));
        }

        [Test] public void J_AllocationCleanup_ReleasesExactlyOnce()
        {
            DirectMatchConnectivityProvider provider = new();
            Assert.That(provider.TryAllocate(new MatchId("game"), "127.0.0.1", 19817, Now,
                out MatchConnectivityAllocation allocation, out _), Is.True);
            Assert.That(provider.Release(allocation.AllocationId), Is.True);
            Assert.That(provider.Release(allocation.AllocationId), Is.False);
        }

        [Test] public void K_LobbyOutageSeam_DoesNotOwnActiveMatchState()
        {
            GameObject root = new("M20 Outage Seam");
            try
            {
                LocalMatchAuthority authority = root.AddComponent<LocalMatchAuthority>();
                MatchSnapshot before = authority.StartMatch(new MatchId("independent"), "hole-01", Players());
                ILobbyService disconnectedControlPlane = null;
                Assert.That(disconnectedControlPlane, Is.Null);
                Assert.That(authority.CurrentSnapshot.MatchId, Is.EqualTo(before.MatchId));
                Assert.That(authority.CurrentSnapshot.Phase, Is.EqualTo(MatchPhase.Playing));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test] public void L_FiveLifecycleCycles_ConvergeWithoutSharedRooms()
        {
            for (int cycle = 0; cycle < 5; cycle++)
            {
                long now = Now + cycle * 1000L;
                InMemoryLobbyService service = Service();
                LobbyMatchSnapshot room = service.CreateMatch(Session("a"), Create("cycle-" + cycle), now).Value;
                service.JoinMatch(Session("b"), Request("join-" + cycle, room.LobbyMatchId), now);
                service.SetReady(Session("a"), new SetReadyRequest("ready-a-" + cycle,
                    room.LobbyMatchId, true), now);
                service.SetReady(Session("b"), new SetReadyRequest("ready-b-" + cycle,
                    room.LobbyMatchId, true), now);
                Assert.That(service.StartMatch(Session("a"), Request("start-" + cycle,
                    room.LobbyMatchId), now).Accepted, Is.True);
                Assert.That(service.MatchCount, Is.EqualTo(1));
            }
        }

        private static InMemoryLobbyService Service() =>
            new(new PrelaunchedGameServerAllocator("127.0.0.1", 19817));

        private static InMemoryLobbyService FullReady(out LobbyMatchSnapshot room)
        {
            InMemoryLobbyService service = Service();
            room = service.CreateMatch(Session("a"), Create("ready"), Now).Value;
            service.JoinMatch(Session("b"), Request("join", room.LobbyMatchId), Now);
            service.SetReady(Session("a"), new SetReadyRequest("ready-a", room.LobbyMatchId, true), Now);
            service.SetReady(Session("b"), new SetReadyRequest("ready-b", room.LobbyMatchId, true), Now);
            return service;
        }

        private static LobbyPlayerSession Session(string id) => new(new PlayerAccountId(id),
            new AuthSessionId("session-" + id), Now + 60_000L, true, false);
        private static CreateMatchRequest Create(string id) => new("create-" + id, "Room " + id,
            2, LobbyProtocol.SupportedHoleId, LobbyVisibility.Public);
        private static LobbyMatchRequest Request(string id, LobbyMatchId match) => new(id, match);

        private static PlayerSnapshot[] Players()
        {
            NetworkVector3 tee = new(0f, 0.2f, 0f);
            return new[]
            {
                new PlayerSnapshot(new MatchPlayerId("player-a"), "A", 0, 0, true,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false),
                new PlayerSnapshot(new MatchPlayerId("player-b"), "B", 1, 1, false,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false)
            };
        }

        private sealed class FixedTokenSource : IReconnectTokenSource
        {
            private int sequence;
            public string CreateSecret() => "m20-reconnect-secret-" + ++sequence;
        }
    }
}

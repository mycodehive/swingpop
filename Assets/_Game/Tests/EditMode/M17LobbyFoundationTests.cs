using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SwingPop.Online;

namespace SwingPop.Tests.EditMode
{
    public sealed class M17LobbyFoundationTests
    {
        private const long Now = 1_000_000L;
        private static LobbyPlayerSession Session(string id, long expiry = Now + 60_000L,
            bool revoked = false) => new(new PlayerAccountId(id), new AuthSessionId("session-" + id),
            expiry, true, revoked);
        private static CreateMatchRequest CreateRequest(string request = "create-1", string name = "Room") =>
            new(request, name, 2, LobbyProtocol.SupportedHoleId, LobbyVisibility.Public);
        private static LobbyMatchRequest MatchRequest(LobbyMatchId id, string request = "request-1") => new(request, id);
        private static InMemoryLobbyService Service(out PrelaunchedGameServerAllocator allocator)
        {
            allocator = new PrelaunchedGameServerAllocator("127.0.0.1", 19817);
            return new InMemoryLobbyService(allocator);
        }
        private static LobbyMatchSnapshot CreateRoom(InMemoryLobbyService service, LobbyPlayerSession owner)
        {
            LobbyOperationResult<LobbyMatchSnapshot> result = service.CreateMatch(owner, CreateRequest(), Now);
            Assert.That(result.Accepted, Is.True);
            return result.Value;
        }

        [Test] public void CreateRequiresAuthentication()
        {
            InMemoryLobbyService service = Service(out _);
            Assert.That(service.CreateMatch(default, CreateRequest(), Now).Reason,
                Is.EqualTo(LobbyRejectReason.AuthenticationRequired));
        }

        [Test] public void CreateSucceedsAndOwnerOccupiesFirstSlot()
        {
            LobbyMatchSnapshot room = CreateRoom(Service(out _), Session("a"));
            Assert.That(room.CurrentPlayers, Is.EqualTo(1));
            Assert.That(room.Members[0].IsOwner, Is.True);
            Assert.That(room.Members[0].SlotIndex, Is.Zero);
        }

        [Test] public void InvalidRoomNameIsRejected()
        {
            InMemoryLobbyService service = Service(out _);
            Assert.That(service.CreateMatch(Session("a"), CreateRequest(name: "bad\nname"), Now).Reason,
                Is.EqualTo(LobbyRejectReason.InvalidDisplayName));
        }

        [Test] public void ListReturnsOnlyJoinablePublicRooms()
        {
            InMemoryLobbyService service = Service(out _);
            CreateRoom(service, Session("a"));
            LobbyMatchSnapshot privateRoom = service.CreateMatch(Session("b"),
                new CreateMatchRequest("private", "Private", 2, LobbyProtocol.SupportedHoleId,
                    LobbyVisibility.Private), Now).Value;
            LobbyMatchSnapshot[] rooms = service.ListMatches(Session("c"),
                new ListMatchesRequest("list", true), Now).Value;
            Assert.That(rooms.Length, Is.EqualTo(1));
            Assert.That(rooms[0].LobbyMatchId, Is.Not.EqualTo(privateRoom.LobbyMatchId));
        }

        [Test] public void JoinSucceedsAndRoomBecomesFull()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            LobbyOperationResult<LobbyMatchSnapshot> result = service.JoinMatch(Session("b"),
                MatchRequest(room.LobbyMatchId, "join"), Now);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Value.State, Is.EqualTo(LobbyMatchState.Full));
        }

        [Test] public void FullRoomRejectsThirdMember()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            service.JoinMatch(Session("b"), MatchRequest(room.LobbyMatchId, "join-b"), Now);
            Assert.That(service.JoinMatch(Session("c"), MatchRequest(room.LobbyMatchId, "join-c"), Now).Reason,
                Is.EqualTo(LobbyRejectReason.MatchFull));
        }

        [Test] public void ConcurrentFinalSlotAllowsExactlyOneJoin()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            ConcurrentBag<bool> outcomes = new();
            Parallel.Invoke(
                () => outcomes.Add(service.JoinMatch(Session("b"), MatchRequest(room.LobbyMatchId, "join-b"), Now).Accepted),
                () => outcomes.Add(service.JoinMatch(Session("c"), MatchRequest(room.LobbyMatchId, "join-c"), Now).Accepted));
            Assert.That(outcomes.Count(value => value), Is.EqualTo(1));
        }

        [Test] public void DuplicateMembershipIsRejected()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            Assert.That(service.JoinMatch(Session("a"), MatchRequest(room.LobbyMatchId), Now).Reason,
                Is.EqualTo(LobbyRejectReason.AlreadyMember));
        }

        [Test] public void AccountCanBelongToOnlyOneActiveRoom()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot first = CreateRoom(service, Session("a"));
            LobbyMatchSnapshot second = service.CreateMatch(Session("b"), CreateRequest("create-b", "B"), Now).Value;
            Assert.That(service.JoinMatch(Session("a"), MatchRequest(second.LobbyMatchId), Now).Reason,
                Is.EqualTo(LobbyRejectReason.AlreadyInMatch));
            Assert.That(first.CurrentPlayers, Is.EqualTo(1));
        }

        [Test] public void ReadyChangesOnlyRequestingMember()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            service.JoinMatch(Session("b"), MatchRequest(room.LobbyMatchId, "join"), Now);
            LobbyMatchSnapshot ready = service.SetReady(Session("b"),
                new SetReadyRequest("ready-b", room.LobbyMatchId, true), Now).Value;
            Assert.That(ready.Members[0].ReadyState, Is.EqualTo(LobbyReadyState.NotReady));
            Assert.That(ready.Members[1].ReadyState, Is.EqualTo(LobbyReadyState.Ready));
        }

        [Test] public void NonOwnerCannotStart()
        {
            InMemoryLobbyService service = FullReadyRoom(out LobbyMatchSnapshot room);
            Assert.That(service.StartMatch(Session("b"), MatchRequest(room.LobbyMatchId, "start-b"), Now).Reason,
                Is.EqualTo(LobbyRejectReason.NotOwner));
        }

        [Test] public void StartBeforeFullIsRejected()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            Assert.That(service.StartMatch(Session("a"), MatchRequest(room.LobbyMatchId, "start"), Now).Accepted,
                Is.False);
        }

        [Test] public void StartBeforeAllReadyIsRejected()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            service.JoinMatch(Session("b"), MatchRequest(room.LobbyMatchId, "join"), Now);
            service.SetReady(Session("a"), new SetReadyRequest("ready-a", room.LobbyMatchId, true), Now);
            Assert.That(service.StartMatch(Session("a"), MatchRequest(room.LobbyMatchId, "start"), Now).Reason,
                Is.EqualTo(LobbyRejectReason.PlayersNotReady));
        }

        [Test] public void OwnerAndAllReadyCreatesReservation()
        {
            InMemoryLobbyService service = FullReadyRoom(out LobbyMatchSnapshot room);
            LobbyOperationResult<MatchReservation> result = service.StartMatch(Session("a"),
                MatchRequest(room.LobbyMatchId, "start"), Now);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Value.Grants.Length, Is.EqualTo(2));
        }

        [Test] public void DuplicateStartRequestIsIdempotent()
        {
            InMemoryLobbyService service = FullReadyRoom(out LobbyMatchSnapshot room);
            LobbyMatchRequest request = MatchRequest(room.LobbyMatchId, "same-start");
            MatchReservation first = service.StartMatch(Session("a"), request, Now).Value;
            MatchReservation second = service.StartMatch(Session("a"), request, Now + 1).Value;
            Assert.That(second.GameMatchId, Is.EqualTo(first.GameMatchId));
        }

        [Test] public void JoinDuringStartingIsRejected()
        {
            InMemoryLobbyService service = FullReadyRoom(out LobbyMatchSnapshot room);
            service.StartMatch(Session("a"), MatchRequest(room.LobbyMatchId, "start"), Now);
            LobbyOperationResult<LobbyMatchSnapshot> join = service.JoinMatch(Session("c"),
                MatchRequest(room.LobbyMatchId, "join-c"), Now);
            Assert.That(join.Accepted, Is.False);
        }

        [Test] public void OwnerLeaveClosesWaitingRoom()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            service.JoinMatch(Session("b"), MatchRequest(room.LobbyMatchId, "join"), Now);
            Assert.That(service.LeaveMatch(Session("a"), MatchRequest(room.LobbyMatchId, "leave"), Now).Value.State,
                Is.EqualTo(LobbyMatchState.Closed));
        }

        [Test] public void StaleLobbyRevisionIsRejected()
        {
            LobbySnapshotStore store = new();
            LobbyMatchId id = new("room");
            LobbyMatchSnapshot newer = new(id, "Room", 2, LobbyMatchState.WaitingForPlayers,
                "hole-01", Now, LobbyVisibility.Public, true, 2, default, Array.Empty<LobbyMatchMember>());
            LobbyMatchSnapshot stale = new(id, "Room", 2, LobbyMatchState.WaitingForPlayers,
                "hole-01", Now, LobbyVisibility.Public, true, 1, default, Array.Empty<LobbyMatchMember>());
            Assert.That(store.TryApply(newer), Is.True);
            Assert.That(store.TryApply(stale), Is.False);
        }

        [Test] public void JoinTicketIsAccountBound()
        {
            DevelopmentMatchAdmissionRegistry registry = Registry(out MatchJoinTicket ticket);
            Assert.That(registry.ValidateAndConsume(ticket.GameMatchId, new PlayerAccountId("b"),
                ticket.Secret, Now, false).Reason, Is.EqualTo(MatchAdmissionRejectReason.AccountMismatch));
        }

        [Test] public void JoinTicketRejectsWrongMatch()
        {
            DevelopmentMatchAdmissionRegistry registry = Registry(out MatchJoinTicket ticket);
            Assert.That(registry.ValidateAndConsume(new MatchId("other"), new PlayerAccountId("a"),
                ticket.Secret, Now, false).Reason, Is.EqualTo(MatchAdmissionRejectReason.WrongMatch));
        }

        [Test] public void JoinTicketRejectsExpiry()
        {
            DevelopmentMatchAdmissionRegistry registry = Registry(out MatchJoinTicket ticket, Now - 1);
            Assert.That(registry.ValidateAndConsume(ticket.GameMatchId, new PlayerAccountId("a"),
                ticket.Secret, Now, false).Reason, Is.EqualTo(MatchAdmissionRejectReason.Expired));
        }

        [Test] public void JoinTicketReplayIsRejected()
        {
            DevelopmentMatchAdmissionRegistry registry = Registry(out MatchJoinTicket ticket);
            Assert.That(registry.ValidateAndConsume(ticket.GameMatchId, new PlayerAccountId("a"),
                ticket.Secret, Now, false).Accepted, Is.True);
            Assert.That(registry.ValidateAndConsume(ticket.GameMatchId, new PlayerAccountId("a"),
                ticket.Secret, Now, false).Reason, Is.EqualTo(MatchAdmissionRejectReason.Consumed));
        }

        [Test] public void RevokedSessionOperationIsRejected()
        {
            InMemoryLobbyService service = Service(out _);
            Assert.That(service.CreateMatch(Session("a", revoked: true), CreateRequest(), Now).Reason,
                Is.EqualTo(LobbyRejectReason.SessionRevoked));
        }

        [Test] public void ClosedRoomCleanupRemovesRecord()
        {
            InMemoryLobbyService service = Service(out _);
            LobbyMatchSnapshot room = CreateRoom(service, Session("a"));
            service.CloseMatch(Session("a"), MatchRequest(room.LobbyMatchId, "close"), Now);
            Assert.That(service.CleanupClosedMatches(Now + 31_000L, 30_000L), Is.EqualTo(1));
            Assert.That(service.MatchCount, Is.Zero);
        }

        private static InMemoryLobbyService FullReadyRoom(out LobbyMatchSnapshot room)
        {
            InMemoryLobbyService service = Service(out _);
            room = CreateRoom(service, Session("a"));
            service.JoinMatch(Session("b"), MatchRequest(room.LobbyMatchId, "join"), Now);
            service.SetReady(Session("a"), new SetReadyRequest("ready-a", room.LobbyMatchId, true), Now);
            service.SetReady(Session("b"), new SetReadyRequest("ready-b", room.LobbyMatchId, true), Now);
            return service;
        }

        private static DevelopmentMatchAdmissionRegistry Registry(out MatchJoinTicket ticket,
            long expiry = Now + 10_000L)
        {
            DevelopmentMatchAdmissionRegistry registry = new(new MatchId("game"));
            ticket = registry.Register(new PlayerAccountId("a"), new MatchPlayerId("player-a"), expiry);
            return registry;
        }

    }
}

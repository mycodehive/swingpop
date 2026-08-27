using System;
using System.Collections;
using NUnit.Framework;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEngine;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class M15ReconnectPlayModeTests
    {
        private static int nextPort = 19150;
        private GameObject serverObject;
        private GameObject aObject;
        private GameObject bObject;
        private GameObject replacementObject;
        private LocalMatchAuthority authority;
        private DedicatedServerMatchTransport server;
        private UnityTransportMatchTransport clientA;
        private UnityTransportMatchTransport clientB;
        private UnityTransportMatchTransport replacement;
        private MatchSnapshot latestA;
        private MatchSnapshot latestB;
        private MatchSnapshot latestReplacement;
        private ReconnectTicket ticketA;

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
        public IEnumerator A_InitialPlayersReceiveReconnectTickets()
        {
            yield return ConnectMatch();
            Assert.That(ticketA.IsValid, Is.True);
            Assert.That(ticketA.PlayerId, Is.EqualTo(clientA.AssignedPlayer));
            Assert.That(ticketA.MatchId, Is.EqualTo(latestA.MatchId));
            Assert.That(ticketA.SessionGeneration, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator B_DisconnectSuspendsMatchAndReservesPlayerState()
        {
            yield return ConnectMatch();
            MatchSnapshot before = authority.CurrentSnapshot;
            clientA.CancelPending();
            yield return WaitFor(() => server.LifecycleState == DedicatedMatchLifecycleState.ReconnectGrace,
                3f, "server reconnect grace");
            Assert.That(server.ConnectedPlayerCount, Is.EqualTo(1));
            Assert.That(server.ReservedPlayerCount, Is.EqualTo(2));
            Assert.That(authority.CurrentSnapshot.PlayerCount, Is.EqualTo(2));
            Assert.That(authority.CurrentSnapshot.GetPlayer(0).BallPosition, Is.EqualTo(before.GetPlayer(0).BallPosition));
            Assert.That(authority.CurrentSnapshot.GetPlayer(0).ConnectionState,
                Is.EqualTo(PlayerConnectionState.ReconnectGrace));
        }

        [UnityTest]
        public IEnumerator C_RemainingClientShotIsRejectedWhileSuspended()
        {
            yield return ConnectMatch();
            clientA.CancelPending();
            yield return WaitFor(() => server.IsMatchSuspended, 3f, "suspension");
            ShotRejectReason rejection = ShotRejectReason.None;
            clientB.ShotRejectedReceived += value => rejection = value.Reason;
            Assert.That(clientB.SubmitShot(Submission(latestB, clientB.AssignedPlayer)), Is.True);
            yield return WaitFor(() => rejection != ShotRejectReason.None, 3f, "suspended rejection");
            Assert.That(rejection, Is.EqualTo(ShotRejectReason.MatchSuspended));
        }

        [UnityTest]
        public IEnumerator D_NewTransportReconnectsSamePlayerAndRotatesTicket()
        {
            yield return ConnectMatch();
            ReconnectTicket original = ticketA;
            clientA.CancelPending();
            yield return WaitFor(() => server.IsMatchSuspended, 3f, "suspension");
            CreateReplacement(original);
            ReconnectAcceptedMessage accepted = default;
            replacement.ReconnectAccepted += value => accepted = value;
            replacement.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => replacement.IsReady && latestReplacement != null, 4f, "reconnect accepted");
            Assert.That(replacement.AssignedPlayer.Value, Is.EqualTo("player-a"));
            Assert.That(accepted.RotatedTicket.SessionGeneration, Is.EqualTo(2));
            Assert.That(accepted.RotatedTicket.Secret, Is.Not.EqualTo(original.Secret));
            Assert.That(server.LifecycleState, Is.EqualTo(DedicatedMatchLifecycleState.Playing));
            Assert.That(latestReplacement.GetPlayer(0).ConnectionState, Is.EqualTo(PlayerConnectionState.Connected));
        }

        [UnityTest]
        public IEnumerator E_WrongTicketIsRejectedWithoutTakingReservedSlot()
        {
            yield return ConnectMatch();
            clientA.CancelPending();
            yield return WaitFor(() => server.IsMatchSuspended, 3f, "suspension");
            ReconnectTicket wrong = new(ticketA.MatchId, ticketA.PlayerId, ticketA.SessionGeneration,
                "wrong-secret", ticketA.IssuedAtUnixMilliseconds, ticketA.ExpiresAtUnixMilliseconds);
            CreateReplacement(wrong);
            ReconnectRejectReason reason = ReconnectRejectReason.None;
            replacement.ReconnectRejected += value => reason = value.Reason;
            replacement.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != ReconnectRejectReason.None, 3f, "wrong ticket rejection");
            Assert.That(reason, Is.EqualTo(ReconnectRejectReason.InvalidTicket));
            Assert.That(server.ReservedPlayerCount, Is.EqualTo(2));
            Assert.That(server.ConnectedPlayerCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator F_ActivePlayerTicketCannotCreateDuplicateBinding()
        {
            yield return ConnectMatch();
            CreateReplacement(ticketA);
            ReconnectRejectReason reason = ReconnectRejectReason.None;
            replacement.ReconnectRejected += value => reason = value.Reason;
            replacement.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => reason != ReconnectRejectReason.None, 3f, "duplicate rejection");
            Assert.That(reason, Is.EqualTo(ReconnectRejectReason.PlayerAlreadyConnected));
            Assert.That(server.ConnectedPlayerCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator G_DisconnectDuringApprovedShotSettlesThenReconnectRestoresNewTurn()
        {
            yield return ConnectMatch();
            ApprovedShot approved = ApproveA();
            clientA.CancelPending();
            yield return WaitFor(() => server.IsMatchSuspended, 3f, "shot suspension");
            Vector3 final = new(6f, 0.2f, 24f);
            Assert.That(server.SubmitShotResult(Result(approved, final)), Is.True);
            Assert.That(authority.CurrentSnapshot.CurrentTurnPlayer.Value, Is.EqualTo("player-b"));
            Assert.That(authority.CurrentSnapshot.GetPlayer(0).BallPosition.ToUnity(), Is.EqualTo(final));
            CreateReplacement(ticketA);
            replacement.StartClient("127.0.0.1", server.Port, 5f);
            yield return WaitFor(() => replacement.IsReady && latestReplacement != null
                                      && latestReplacement.CurrentTurnPlayer.Value == "player-b", 4f, "restored next turn");
            Assert.That(latestReplacement.GetPlayer(0).BallPosition.ToUnity(), Is.EqualTo(final));
            Assert.That(latestReplacement.ShotSequence, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator H_GraceExpiryAbortsAndExpiresSlotWithoutDeadlock()
        {
            yield return ConnectMatch(3f);
            clientA.CancelPending();
            yield return WaitFor(() => server.IsMatchSuspended, 3f, "short grace");
            yield return WaitFor(() => authority.CurrentSnapshot.Phase == MatchPhase.Aborted, 5f, "grace expiry abort");
            yield return WaitFor(() => latestB != null && latestB.Phase == MatchPhase.Aborted,
                3f, "remaining client final snapshot");
            Assert.That(authority.CurrentSnapshot.TurnState, Is.EqualTo(TurnState.TurnComplete));
            Assert.That(authority.CurrentSnapshot.GetPlayer(0).ConnectionState, Is.EqualTo(PlayerConnectionState.Expired));
            Assert.That(latestB.Phase, Is.EqualTo(MatchPhase.Aborted));
        }

        private IEnumerator ConnectMatch(float graceSeconds = 30f)
        {
            serverObject = new GameObject("M15 Server");
            authority = serverObject.AddComponent<LocalMatchAuthority>();
            server = serverObject.AddComponent<DedicatedServerMatchTransport>();
            server.Configure(authority, 2, false);
            server.ConfigureReconnectPolicy(graceSeconds);
            aObject = new GameObject("M15 Client A");
            clientA = aObject.AddComponent<UnityTransportMatchTransport>();
            clientA.Configure(null, false);
            bObject = new GameObject("M15 Client B");
            clientB = bObject.AddComponent<UnityTransportMatchTransport>();
            clientB.Configure(null, false);
            clientA.ReconnectTicketChanged += value => ticketA = value;
            clientA.SnapshotReceived += value => latestA = value;
            clientB.SnapshotReceived += value => latestB = value;
            server.AllPlayersReady += () => server.BeginDedicatedMatch(authority.StartMatch(
                new MatchId("m15-play"), "hole-01", InitialPlayers()));
            ushort port = (ushort)nextPort++;
            Assert.That(server.StartDedicatedServer("127.0.0.1", port, 5f), Is.True);
            Assert.That(clientA.StartClient("127.0.0.1", port, 5f), Is.True);
            yield return WaitFor(() => clientA.AssignedPlayer.IsValid, 3f, "A assigned");
            Assert.That(clientB.StartClient("127.0.0.1", port, 5f), Is.True);
            yield return WaitFor(() => server.IsReady && clientA.IsReady && clientB.IsReady
                                      && ticketA.IsValid && latestA != null && latestB != null, 4f, "match ready");
        }

        private void CreateReplacement(ReconnectTicket ticket)
        {
            replacementObject = new GameObject("M15 Replacement Client");
            replacement = replacementObject.AddComponent<UnityTransportMatchTransport>();
            replacement.Configure(null, false);
            Assert.That(replacement.SetPendingReconnectTicket(ticket), Is.True);
            replacement.SnapshotReceived += value => latestReplacement = value;
        }

        private ApprovedShot ApproveA()
        {
            ShotSubmission submission = Submission(latestA, clientA.AssignedPlayer);
            Assert.That(clientA.SubmitShot(submission), Is.True);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (authority.CurrentSnapshot.TurnState != TurnState.ShotPlaying
                   && Time.realtimeSinceStartup < deadline)
            {
                server.Tick(0.016f);
                clientA.Tick(0.016f);
                clientB.Tick(0.016f);
            }
            Assert.That(authority.CurrentSnapshot.TurnState, Is.EqualTo(TurnState.ShotPlaying));
            return new ApprovedShot(submission, authority.CurrentSnapshot.ShotSequence);
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

        private static ShotSubmission Submission(MatchSnapshot snapshot, MatchPlayerId player)
        {
            ShotCommand command = new(Vector3.forward, Vector3.forward, 0f, 0.6f, 1f, 0f,
                ImpactGrade.Perfect, 0.6f, 0f, 22f, 35f, ShotSpin.None);
            command = command.WithClub(ClubType.Driver, 22f, 35f, 1f, 1f);
            return new ShotSubmission(snapshot.MatchId, player, snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, command);
        }

        private static NetworkShotResult Result(ApprovedShot approved, Vector3 position)
        {
            NetworkVector3 final = NetworkVector3.FromUnity(position);
            return new NetworkShotResult(approved.MatchId, approved.PlayerId, approved.TurnIndex,
                approved.ShotSequence, final, final, TerrainSurfaceType.Fairway, 1, 0, false, false);
        }
    }
}

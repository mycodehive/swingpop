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
    public sealed class M14DedicatedAuthorityPlayModeTests
    {
        private static int nextPort = 18820;
        private GameObject serverObject;
        private GameObject clientAObject;
        private GameObject clientBObject;
        private GameObject clientCObject;
        private LocalMatchAuthority authority;
        private DedicatedServerMatchTransport server;
        private UnityTransportMatchTransport clientA;
        private UnityTransportMatchTransport clientB;
        private UnityTransportMatchTransport clientC;
        private MatchSnapshot latestA;
        private MatchSnapshot latestB;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (clientCObject != null) UnityEngine.Object.Destroy(clientCObject);
            if (clientBObject != null) UnityEngine.Object.Destroy(clientBObject);
            if (clientAObject != null) UnityEngine.Object.Destroy(clientAObject);
            if (serverObject != null) UnityEngine.Object.Destroy(serverObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_DedicatedServerBootstrapListensWithoutLocalPlayer()
        {
            CreateServer();
            Assert.That(server.StartDedicatedServer("127.0.0.1", NextPort(), 5f), Is.True);
            yield return null;
            Assert.That(server.ConnectionState, Is.EqualTo(NetworkConnectionState.Listening));
            Assert.That(server.ConnectedPlayerCount, Is.Zero);
            Assert.That(server.SubmitShot(default), Is.False);
        }

        [UnityTest]
        public IEnumerator B_FirstClientReceivesPlayerA()
        {
            CreateServer();
            CreateClientA();
            ushort port = NextPort();
            server.StartDedicatedServer("127.0.0.1", port, 5f);
            clientA.StartClient("127.0.0.1", port, 5f);
            yield return WaitFor(() => clientA.AssignedPlayer.IsValid, 3f, "Player A assignment");
            Assert.That(clientA.AssignedPlayer.Value, Is.EqualTo("player-a"));
            Assert.That(server.IsReady, Is.False);
        }

        [UnityTest]
        public IEnumerator C_SecondClientReceivesPlayerB()
        {
            yield return ConnectThree();
            Assert.That(clientA.AssignedPlayer.Value, Is.EqualTo("player-a"));
            Assert.That(clientB.AssignedPlayer.Value, Is.EqualTo("player-b"));
            Assert.That(server.ConnectedPlayerCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator D_TwoClientsStartSameMatchSnapshot()
        {
            yield return ConnectThree();
            Assert.That(latestA, Is.Not.Null);
            Assert.That(latestB, Is.Not.Null);
            Assert.That(latestA.MatchId, Is.EqualTo(latestB.MatchId));
            Assert.That(MatchSnapshotHash.Compute(latestA), Is.EqualTo(MatchSnapshotHash.Compute(latestB)));
            Assert.That(MatchSnapshotHash.Compute(latestA), Is.EqualTo(MatchSnapshotHash.Compute(authority.CurrentSnapshot)));
        }

        [UnityTest]
        public IEnumerator E_ClientAShotIsApprovedByServer()
        {
            yield return ConnectThree();
            int approvalsA = 0;
            int approvalsB = 0;
            clientA.ShotApprovedReceived += _ => approvalsA++;
            clientB.ShotApprovedReceived += _ => approvalsB++;
            Assert.That(clientA.SubmitShot(Submission(latestA, clientA.AssignedPlayer)), Is.True);
            yield return WaitFor(() => approvalsA == 1 && approvalsB == 1, 3f, "A approved broadcast");
            Assert.That(authority.CurrentSnapshot.TurnState, Is.EqualTo(TurnState.ShotPlaying));
        }

        [UnityTest]
        public IEnumerator F_ClientBShotRunsAfterServerResultAdvancesTurn()
        {
            yield return ConnectThree();
            ApprovedShot approvedA = ApproveFrom(clientA, latestA);
            Assert.That(server.SubmitShotResult(Result(approvedA, new Vector3(2f, 0.2f, 12f))), Is.True);
            yield return WaitFor(() => latestB.CurrentTurnPlayer == clientB.AssignedPlayer
                                      && latestB.TurnState == TurnState.PreparingShot, 3f, "B turn");
            int bApprovals = 0;
            clientB.ShotApprovedReceived += approved => { if (approved.PlayerId == clientB.AssignedPlayer) bApprovals++; };
            Assert.That(clientB.SubmitShot(Submission(latestB, clientB.AssignedPlayer)), Is.True);
            yield return WaitFor(() => bApprovals == 1, 3f, "B shot approval");
        }

        [UnityTest]
        public IEnumerator G_ClientPredictedResultCannotMutateServerAuthority()
        {
            yield return ConnectThree();
            ApprovedShot approved = ApproveFrom(clientA, latestA);
            long beforeVersion = authority.CurrentSnapshot.Version;
            Assert.That(clientA.SubmitShotResult(Result(approved, new Vector3(99f, 0.2f, 99f))), Is.True);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(authority.CurrentSnapshot.Version, Is.EqualTo(beforeVersion));
            Assert.That(authority.CurrentSnapshot.TurnState, Is.EqualTo(TurnState.ShotPlaying));
        }

        [UnityTest]
        public IEnumerator H_ServerSnapshotCorrectsBothClientsToSameState()
        {
            yield return ConnectThree();
            ApprovedShot approved = ApproveFrom(clientA, latestA);
            Vector3 final = new(5f, 0.2f, 18f);
            server.SubmitShotResult(Result(approved, final));
            yield return WaitFor(() => latestA != null && latestB != null
                                      && latestA.Version == authority.CurrentSnapshot.Version
                                      && latestB.Version == authority.CurrentSnapshot.Version, 3f, "snapshot correction");
            Assert.That(latestA.GetPlayer(0).BallPosition.ToUnity(), Is.EqualTo(final));
            Assert.That(MatchSnapshotHash.Compute(latestA), Is.EqualTo(MatchSnapshotHash.Compute(latestB)));
        }

        [UnityTest]
        public IEnumerator I_WrongTurnSubmissionIsRejectedWithoutStateChange()
        {
            yield return ConnectThree();
            ShotRejectReason reason = ShotRejectReason.None;
            clientB.ShotRejectedReceived += rejection => reason = rejection.Reason;
            long beforeVersion = authority.CurrentSnapshot.Version;
            Assert.That(clientB.SubmitShot(Submission(latestB, clientB.AssignedPlayer)), Is.True);
            yield return WaitFor(() => reason != ShotRejectReason.None, 3f, "wrong turn rejection");
            Assert.That(reason, Is.EqualTo(ShotRejectReason.NotYourTurn));
            Assert.That(authority.CurrentSnapshot.Version, Is.EqualTo(beforeVersion));
        }

        [UnityTest]
        public IEnumerator J_ClientDisconnectAbortsMatchAndKeepsOtherClientAlive()
        {
            yield return ConnectThree();
            clientB.CancelPending();
            yield return WaitFor(() => authority.CurrentSnapshot.Phase == MatchPhase.Aborted, 3f, "disconnect abort");
            yield return WaitFor(() => latestA != null && latestA.Phase == MatchPhase.Aborted, 3f, "remaining client snapshot");
            Assert.That(clientA.ConnectionState, Is.EqualTo(NetworkConnectionState.InMatch));
            Assert.That(latestA.GetPlayer(1).ConnectionState, Is.EqualTo(PlayerConnectionState.Disconnected));
        }

        [UnityTest]
        public IEnumerator K_ServerCompletesMatchAndThreeHashesConverge()
        {
            yield return ConnectThree();
            ApprovedShot approvedA = ApproveFrom(clientA, latestA);
            server.SubmitShotResult(HoledResult(approvedA, 1));
            yield return WaitFor(() => latestB.CurrentTurnPlayer == clientB.AssignedPlayer, 3f, "B after A holed");
            ApprovedShot approvedB = ApproveFrom(clientB, latestB);
            server.SubmitShotResult(HoledResult(approvedB, 1));
            yield return WaitFor(() => latestA.Phase == MatchPhase.HoleComplete
                                      && latestB.Phase == MatchPhase.HoleComplete, 3f, "match complete");
            string serverHash = MatchSnapshotHash.Compute(authority.CurrentSnapshot);
            Assert.That(MatchSnapshotHash.Compute(latestA), Is.EqualTo(serverHash));
            Assert.That(MatchSnapshotHash.Compute(latestB), Is.EqualTo(serverHash));
            Assert.That(server.LifecycleState, Is.EqualTo(DedicatedMatchLifecycleState.HoleComplete));
        }

        [UnityTest]
        public IEnumerator L_ThirdClientIsRejectedAsMatchFullWithoutJoiningAuthority()
        {
            yield return ConnectThree();
            clientCObject = new GameObject("M14 Test Client C");
            clientC = clientCObject.AddComponent<UnityTransportMatchTransport>();
            clientC.Configure(null, false);
            string disconnectReason = string.Empty;
            clientC.Disconnected += reason => disconnectReason = reason;

            Assert.That(clientC.StartClient("127.0.0.1", server.Port, 5f), Is.True);
            yield return WaitFor(() => clientC.ConnectionState == NetworkConnectionState.Failed,
                3f, "third client MatchFull rejection");

            Assert.That(disconnectReason, Is.EqualTo(ShotRejectReason.MatchFull.ToString()));
            Assert.That(clientC.AssignedPlayer.IsValid, Is.False);
            Assert.That(server.ConnectedPlayerCount, Is.EqualTo(2));
            Assert.That(authority.CurrentSnapshot.PlayerCount, Is.EqualTo(2));
        }

        private IEnumerator ConnectThree()
        {
            CreateServer();
            CreateClientA();
            CreateClientB();
            server.AllPlayersReady += () => server.BeginDedicatedMatch(authority.StartMatch(
                new MatchId("m14-playmode"), "hole-01", InitialPlayers()));
            clientA.SnapshotReceived += snapshot => latestA = snapshot;
            clientB.SnapshotReceived += snapshot => latestB = snapshot;
            ushort port = NextPort();
            Assert.That(server.StartDedicatedServer("127.0.0.1", port, 5f), Is.True);
            Assert.That(clientA.StartClient("127.0.0.1", port, 5f), Is.True);
            yield return WaitFor(() => clientA.AssignedPlayer.IsValid, 3f, "first assignment");
            Assert.That(clientB.StartClient("127.0.0.1", port, 5f), Is.True);
            yield return WaitFor(() => server.IsReady && clientA.IsReady && clientB.IsReady, 4f, "three-way match start");
        }

        private void CreateServer()
        {
            serverObject = new GameObject("M14 Test Dedicated Server");
            authority = serverObject.AddComponent<LocalMatchAuthority>();
            server = serverObject.AddComponent<DedicatedServerMatchTransport>();
            server.Configure(authority, 2, false);
        }

        private void CreateClientA()
        {
            clientAObject = new GameObject("M14 Test Client A");
            clientA = clientAObject.AddComponent<UnityTransportMatchTransport>();
            clientA.Configure(null, false);
        }

        private void CreateClientB()
        {
            clientBObject = new GameObject("M14 Test Client B");
            clientB = clientBObject.AddComponent<UnityTransportMatchTransport>();
            clientB.Configure(null, false);
        }

        private ApprovedShot ApproveFrom(UnityTransportMatchTransport client, MatchSnapshot snapshot)
        {
            ShotSubmission submission = Submission(snapshot, client.AssignedPlayer);
            Assert.That(client.SubmitShot(submission), Is.True);
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
            return new ShotSubmission(snapshot.MatchId, player, snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, Command());
        }

        private static NetworkShotResult Result(ApprovedShot approved, Vector3 position)
        {
            NetworkVector3 final = NetworkVector3.FromUnity(position);
            return new NetworkShotResult(approved.MatchId, approved.PlayerId, approved.TurnIndex,
                approved.ShotSequence, final, final, TerrainSurfaceType.Fairway, 1, 0, false, false);
        }

        private static NetworkShotResult HoledResult(ApprovedShot approved, int strokes)
        {
            NetworkVector3 cup = new(0f, 0.05f, 100f);
            return new NetworkShotResult(approved.MatchId, approved.PlayerId, approved.TurnIndex,
                approved.ShotSequence, cup, cup, TerrainSurfaceType.Green, strokes, 0, true, true, -3, "EAGLE");
        }

        private static ShotCommand Command()
        {
            ShotCommand command = new(Vector3.forward, Vector3.forward, 0f, 0.55f, 1f, 0f,
                ImpactGrade.Perfect, 0.55f, 0f, 22f, 35f, ShotSpin.None);
            return command.WithClub(ClubType.Driver, 22f, 35f, 1f, 1f);
        }

        private static ushort NextPort() => (ushort)nextPort++;

        private static IEnumerator WaitFor(Func<bool> condition, float timeout, string label)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, $"Timed out waiting for {label}.");
        }
    }
}

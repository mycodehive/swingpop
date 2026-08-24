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
    public sealed class M13NetworkPrototypePlayModeTests
    {
        private static int nextPort = 18770;
        private GameObject hostObject;
        private GameObject clientObject;
        private LocalMatchAuthority hostAuthority;
        private UnityTransportMatchTransport host;
        private UnityTransportMatchTransport client;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (clientObject != null) Object.Destroy(clientObject);
            if (hostObject != null) Object.Destroy(hostObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_HostBootstrapEntersListeningState()
        {
            CreateHost();
            Assert.That(host.StartHost("127.0.0.1", NextPort(), 5f), Is.True);
            yield return null;
            Assert.That(host.Role, Is.EqualTo(NetworkRole.Host));
            Assert.That(host.ConnectionState, Is.EqualTo(NetworkConnectionState.Listening));
        }

        [UnityTest]
        public IEnumerator B_ClientBootstrapConnectsAndReceivesServerAssignedPlayerB()
        {
            yield return ConnectPair();
            Assert.That(client.Role, Is.EqualTo(NetworkRole.Client));
            Assert.That(client.AssignedPlayer.Value, Is.EqualTo("player-b"));
        }

        [UnityTest]
        public IEnumerator C_InitialAssignmentStartsSameAuthoritativeSnapshot()
        {
            MatchSnapshot clientSnapshot = null;
            yield return ConnectPair(snapshot => clientSnapshot = snapshot);
            Assert.That(clientSnapshot, Is.Not.Null);
            Assert.That(clientSnapshot.PlayerCount, Is.EqualTo(2));
            Assert.That(clientSnapshot.CurrentTurnPlayer.Value, Is.EqualTo("player-a"));
            Assert.That(MatchSnapshotHash.Compute(clientSnapshot),
                Is.EqualTo(MatchSnapshotHash.Compute(hostAuthority.CurrentSnapshot)));
        }

        [UnityTest]
        public IEnumerator D_WrongTurnClientSubmissionIsRejectedByHost()
        {
            ShotRejection rejection = default;
            bool received = false;
            yield return ConnectPair();
            client.ShotRejectedReceived += value => { rejection = value; received = true; };
            MatchSnapshot snapshot = hostAuthority.CurrentSnapshot;
            ShotSubmission submission = new(snapshot.MatchId, new MatchPlayerId("player-b"), snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, Command());
            Assert.That(client.SubmitShot(submission), Is.True);
            yield return WaitFor(() => received, 2f, "wrong-turn rejection");
            Assert.That(rejection.Reason, Is.EqualTo(ShotRejectReason.NotYourTurn));
        }

        [UnityTest]
        public IEnumerator E_ClientWaitsForApprovalAndApprovedPlaybackMessageArrivesOnce()
        {
            int approvals = 0;
            yield return ConnectPair();
            client.ShotApprovedReceived += _ => approvals++;
            MatchSnapshot snapshot = hostAuthority.CurrentSnapshot;
            ShotSubmission submission = new(snapshot.MatchId, new MatchPlayerId("player-a"), snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, Command());
            Assert.That(host.SubmitShot(submission), Is.True);
            Assert.That(approvals, Is.Zero);
            yield return WaitFor(() => approvals == 1, 2f, "approved shot");
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(approvals, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator F_HostSnapshotIsAppliedAfterAuthoritativeResult()
        {
            MatchSnapshot latestClient = null;
            yield return ConnectPair(snapshot => latestClient = snapshot);
            ApprovedShot approved = ApproveHostA();
            yield return WaitFor(() => latestClient != null && latestClient.TurnState == TurnState.ShotPlaying,
                2f, "shot-playing snapshot");
            Assert.That(host.SubmitShotResult(Result(approved, new Vector3(4f, 0.2f, 12f))), Is.True);
            yield return WaitFor(() => latestClient.Version == hostAuthority.CurrentSnapshot.Version,
                2f, "authoritative result snapshot");
            Assert.That(latestClient.CurrentTurnPlayer.Value, Is.EqualTo("player-b"));
        }

        [UnityTest]
        public IEnumerator G_BothPlayersAdvanceTurnsThroughHostAuthority()
        {
            MatchSnapshot latestClient = null;
            yield return ConnectPair(snapshot => latestClient = snapshot);
            ApprovedShot a = ApproveHostA();
            host.SubmitShotResult(Result(a, new Vector3(4f, 0.2f, 12f)));
            yield return WaitFor(() => latestClient != null && latestClient.CurrentTurnPlayer.Value == "player-b",
                2f, "player B turn");
            ShotSubmission bSubmission = new(latestClient.MatchId, new MatchPlayerId("player-b"), latestClient.TurnIndex,
                latestClient.ShotSequence + 1, OnlineProtocol.CurrentVersion, Command());
            Assert.That(client.SubmitShot(bSubmission), Is.True);
            yield return WaitFor(() => hostAuthority.CurrentSnapshot.TurnState == TurnState.ShotPlaying
                                      && hostAuthority.CurrentSnapshot.CurrentTurnPlayer.Value == "player-b",
                2f, "player B approval");
            MatchSnapshot playing = hostAuthority.CurrentSnapshot;
            ApprovedShot b = new(bSubmission, playing.ShotSequence);
            Assert.That(host.SubmitShotResult(Result(b, new Vector3(-3f, 0.2f, 15f))), Is.True);
            yield return WaitFor(() => latestClient.CurrentTurnPlayer.Value == "player-a"
                                      && latestClient.TurnIndex == 2, 2f, "player A restored turn");
        }

        [UnityTest]
        public IEnumerator H_StaleAuthoritativeSnapshotCannotOverwriteNewerState()
        {
            yield return null;
            MatchSnapshotStore store = new();
            MatchSnapshot newer = Snapshot(4);
            MatchSnapshot stale = Snapshot(3);
            Assert.That(store.TryApply(newer), Is.True);
            Assert.That(store.TryApply(stale), Is.False);
            Assert.That(store.Current.Version, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator I_ClientDisconnectCleansHostConnectionState()
        {
            bool disconnected = false;
            yield return ConnectPair();
            host.Disconnected += _ => disconnected = true;
            client.CancelPending();
            yield return WaitFor(() => disconnected, 3f, "host disconnect cleanup");
            Assert.That(host.ConnectionState, Is.EqualTo(NetworkConnectionState.Disconnected));
        }

        [UnityTest]
        public IEnumerator J_HostCanRestartListenerAfterDisconnectCleanup()
        {
            yield return ConnectPair();
            ushort port = NextPort();
            client.CancelPending();
            yield return WaitFor(() => host.ConnectionState == NetworkConnectionState.Disconnected,
                3f, "disconnect before restart");
            Assert.That(host.StartHost("127.0.0.1", port, 5f), Is.True);
            yield return null;
            Assert.That(host.ConnectionState, Is.EqualTo(NetworkConnectionState.Listening));
        }

        private IEnumerator ConnectPair(System.Action<MatchSnapshot> clientSnapshot = null)
        {
            CreateHost();
            CreateClient();
            ushort port = NextPort();
            host.RemotePlayerReady += () => host.BeginHostedMatch(hostAuthority.StartMatch(
                new MatchId("m13-playmode"), "hole-01", InitialPlayers()));
            if (clientSnapshot != null) client.SnapshotReceived += clientSnapshot;
            Assert.That(host.StartHost("127.0.0.1", port, 5f), Is.True);
            Assert.That(client.StartClient("127.0.0.1", port, 5f), Is.True);
            yield return WaitFor(() => host.IsReady && client.IsReady, 3f, "host/client handshake");
        }

        private void CreateHost()
        {
            hostObject = new GameObject("M13 Test Host");
            hostAuthority = hostObject.AddComponent<LocalMatchAuthority>();
            host = hostObject.AddComponent<UnityTransportMatchTransport>();
            host.Configure(hostAuthority, false);
        }

        private void CreateClient()
        {
            clientObject = new GameObject("M13 Test Client");
            client = clientObject.AddComponent<UnityTransportMatchTransport>();
            client.Configure(null, false);
        }

        private ApprovedShot ApproveHostA()
        {
            MatchSnapshot snapshot = hostAuthority.CurrentSnapshot;
            ShotSubmission submission = new(snapshot.MatchId, new MatchPlayerId("player-a"), snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, Command());
            Assert.That(host.SubmitShot(submission), Is.True);
            return new ApprovedShot(submission, hostAuthority.CurrentSnapshot.ShotSequence);
        }

        private static PlayerSnapshot[] InitialPlayers()
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

        private static NetworkShotResult Result(ApprovedShot approved, Vector3 position)
        {
            NetworkVector3 final = NetworkVector3.FromUnity(position);
            return new NetworkShotResult(approved.MatchId, approved.PlayerId, approved.TurnIndex,
                approved.ShotSequence, final, final, TerrainSurfaceType.Fairway, 1, 0, false, false);
        }

        private static ShotCommand Command()
        {
            ShotCommand command = new(Vector3.forward, Vector3.forward, 0f, 0.6f, 1f, 0f,
                ImpactGrade.Perfect, 0.6f, 0f, 22f, 35f, ShotSpin.None);
            return command.WithClub(ClubType.Driver, 22f, 35f, 1f, 1f);
        }

        private static MatchSnapshot Snapshot(long version)
        {
            PlayerSnapshot[] players = InitialPlayers();
            return new MatchSnapshot(new MatchId("stale-test"), OnlineProtocol.CurrentVersion, version, "hole-01",
                MatchPhase.Playing, TurnState.PreparingShot, 0, 0, players[0].PlayerId, players);
        }

        private static ushort NextPort() => (ushort)nextPort++;

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout, string label)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, $"Timed out waiting for {label}.");
        }
    }
}

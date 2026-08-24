using System.Collections;
using NUnit.Framework;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class M12OnlineFoundationPlayModeTests
    {
        private const string SceneName = "Hole01_SkyIsland";

        [UnityTest]
        public IEnumerator OfflineSinglePreservesImmediateExistingShotFlow()
        {
            yield return LoadScene();
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            session.StartDevelopmentMatch(MultiplayerDevelopmentMode.OfflineSingle, 0);
            yield return null;
            Assert.That(session.RequiresApproval, Is.False);
            Assert.That(shotFlow.TryCommitShot(0.55f, 0f), Is.True);
            Assert.That(shotFlow.State, Is.EqualTo(ShotFlowState.ShotCommitted));
            yield return WaitFor(() => ball.State != BallState.Ready, 2f, "offline ball launch");
        }

        [UnityTest]
        public IEnumerator LocalTwoPlayerAdvancesFromAToBAndBlocksLocalInput()
        {
            yield return LoadScene();
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            session.StartDevelopmentMatch(MultiplayerDevelopmentMode.LocalTwoPlayer, 0);
            yield return WaitFor(() => session.CurrentSnapshot != null, 1f, "initial snapshot");

            Assert.That(shotFlow.TryCommitShot(0.45f, 0f), Is.True);
            Assert.That(shotFlow.State, Is.EqualTo(ShotFlowState.AwaitingApproval));
            Assert.That(ball.State, Is.EqualTo(BallState.Ready));
            yield return WaitFor(() => session.CurrentSnapshot.TurnState == TurnState.ShotPlaying, 1f, "A approval");

            ball.PrepareNextShot(new Vector3(8f, 0.2f, 14f), ball.CurrentSurfaceData);
            Assert.That(session.ApplyAuthoritativeShotResult(CreateResult(session.CurrentSnapshot,
                new Vector3(8f, 0.2f, 14f), TerrainSurfaceType.Fairway, 1)), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.CurrentTurnPlayer.Value == "player-b"
                                      && session.CurrentSnapshot.TurnState == TurnState.PreparingShot,
                1f, "B turn");

            Assert.That(session.CanSubmitShot, Is.False);
            ShotFlowState before = shotFlow.State;
            shotFlow.ConfirmCurrentStep();
            Assert.That(shotFlow.State, Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator ApprovedRemoteShotUsesExistingShotFlowAndLaunchesOnce()
        {
            yield return LoadScene();
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            session.StartDevelopmentMatch(MultiplayerDevelopmentMode.LocalTwoPlayer, 0);
            yield return AdvanceAtoB(session, ball);

            int launches = 0;
            ball.Launched += CountLaunch;
            Assert.That(session.SubmitSimulatedRemoteShotNow(), Is.True);
            yield return WaitFor(() => launches == 1, 2f, "remote launch");
            yield return new WaitForSeconds(0.2f);
            Assert.That(launches, Is.EqualTo(1));
            ball.Launched -= CountLaunch;

            void CountLaunch() => launches++;
        }

        [UnityTest]
        public IEnumerator PlayerBallAndScoreRestoreAfterAlternatingTurns()
        {
            yield return LoadScene();
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            session.StartDevelopmentMatch(MultiplayerDevelopmentMode.LocalTwoPlayer, 0);
            yield return AdvanceAtoB(session, ball);

            Assert.That(session.SubmitSimulatedRemoteShotNow(), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.TurnState == TurnState.ShotPlaying
                                      && session.CurrentSnapshot.CurrentTurnPlayer.Value == "player-b",
                1f, "B approval");
            ball.PrepareNextShot(new Vector3(-5f, 0.2f, 18f), ball.CurrentSurfaceData);
            Assert.That(session.ApplyAuthoritativeShotResult(CreateResult(session.CurrentSnapshot,
                new Vector3(-5f, 0.2f, 18f), TerrainSurfaceType.Fairway, 1)), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.CurrentTurnPlayer.Value == "player-a"
                                      && session.CurrentSnapshot.TurnState == TurnState.PreparingShot,
                1f, "A restored turn");

            Assert.That(ball.PhysicsPosition.x, Is.EqualTo(8f).Within(0.02f));
            Assert.That(ball.PhysicsPosition.z, Is.EqualTo(14f).Within(0.02f));
            Assert.That(session.CurrentSnapshot.TryGetPlayer(new MatchPlayerId("player-a"), out PlayerSnapshot playerA), Is.True);
            Assert.That(playerA.StrokeCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DuplicateApprovedPlaybackCannotLaunchBallTwice()
        {
            yield return LoadScene();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            session.StartDevelopmentMatch(MultiplayerDevelopmentMode.OfflineSingle, 0);
            yield return null;
            Assert.That(shotFlow.TryCreateShotCommand(0.5f, 0f, out ShotCommand command), Is.True);
            int launches = 0;
            ball.Launched += CountLaunch;
            Assert.That(shotFlow.TryExecuteApprovedShot(command), Is.True);
            Assert.That(shotFlow.TryExecuteApprovedShot(command), Is.False);
            yield return WaitFor(() => launches == 1, 2f, "single approved playback");
            Assert.That(launches, Is.EqualTo(1));
            ball.Launched -= CountLaunch;

            void CountLaunch() => launches++;
        }

        [UnityTest]
        public IEnumerator TwoHundredMillisecondLoopbackWaitsForApprovalAndLaunchesOnce()
        {
            yield return LoadScene();
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            session.StartDevelopmentMatch(MultiplayerDevelopmentMode.LocalTwoPlayer, 200);
            yield return WaitFor(() => session.CurrentSnapshot != null, 1f, "latency initial snapshot");
            int launches = 0;
            ball.Launched += CountLaunch;
            Assert.That(shotFlow.TryCommitShot(0.5f, 0f), Is.True);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(launches, Is.Zero);
            Assert.That(ball.State, Is.EqualTo(BallState.Ready));
            yield return WaitFor(() => launches == 1, 2f, "latency approved launch");
            Assert.That(launches, Is.EqualTo(1));
            ball.Launched -= CountLaunch;

            void CountLaunch() => launches++;
        }

        [UnityTest]
        public IEnumerator GreenPutterStateRestoresOnlyForOwningPlayer()
        {
            yield return LoadScene();
            MatchSessionController session = Object.FindAnyObjectByType<MatchSessionController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            session.StartDevelopmentMatch(MultiplayerDevelopmentMode.LocalTwoPlayer, 0);
            yield return WaitFor(() => session.CurrentSnapshot != null && session.CurrentSnapshot.PlayerCount == 2,
                1f, "two-player initial snapshot");

            Assert.That(shotFlow.TryCommitShot(0.45f, 0f), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.TurnState == TurnState.ShotPlaying, 1f, "A approval");
            Vector3 greenPosition = new(3f, 0.2f, 24f);
            ball.PrepareNextShot(greenPosition, ball.CurrentSurfaceData);
            Assert.That(session.ApplyAuthoritativeShotResult(CreateResult(session.CurrentSnapshot,
                greenPosition, TerrainSurfaceType.Green, 1)), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.CurrentTurnPlayer.Value == "player-b"
                                      && session.CurrentSnapshot.TurnState == TurnState.PreparingShot,
                1f, "B turn after A green");
            Assert.That(shotFlow.CurrentClub.ClubType, Is.EqualTo(ClubType.Driver));
            Assert.That(session.CurrentSnapshot.TryGetPlayer(new MatchPlayerId("player-b"), out PlayerSnapshot playerB), Is.True);
            Assert.That(playerB.Lie, Is.EqualTo(TerrainSurfaceType.Tee));

            Assert.That(session.SubmitSimulatedRemoteShotNow(), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.TurnState == TurnState.ShotPlaying, 1f, "B approval");
            Vector3 fairwayPosition = new(-2f, 0.2f, 17f);
            ball.PrepareNextShot(fairwayPosition, ball.CurrentSurfaceData);
            Assert.That(session.ApplyAuthoritativeShotResult(CreateResult(session.CurrentSnapshot,
                fairwayPosition, TerrainSurfaceType.Fairway, 1)), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.CurrentTurnPlayer.Value == "player-a"
                                      && session.CurrentSnapshot.TurnState == TurnState.PreparingShot,
                1f, "A green restore");

            Assert.That(ball.CurrentLie, Is.EqualTo(TerrainSurfaceType.Green));
            Assert.That(shotFlow.CurrentClub.ClubType, Is.EqualTo(ClubType.Putter));
            Assert.That(ball.PhysicsPosition.x, Is.EqualTo(greenPosition.x).Within(0.02f));
        }

        private static IEnumerator AdvanceAtoB(MatchSessionController session, GolfBallController ball)
        {
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            yield return WaitFor(() => session.CurrentSnapshot != null, 1f, "initial snapshot");
            Assert.That(shotFlow.TryCommitShot(0.45f, 0f), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.TurnState == TurnState.ShotPlaying, 1f, "A approval");
            ball.PrepareNextShot(new Vector3(8f, 0.2f, 14f), ball.CurrentSurfaceData);
            Assert.That(session.ApplyAuthoritativeShotResult(CreateResult(session.CurrentSnapshot,
                new Vector3(8f, 0.2f, 14f), TerrainSurfaceType.Fairway, 1)), Is.True);
            yield return WaitFor(() => session.CurrentSnapshot.CurrentTurnPlayer.Value == "player-b"
                                      && session.CurrentSnapshot.TurnState == TurnState.PreparingShot,
                1f, "B turn");
        }

        private static NetworkShotResult CreateResult(MatchSnapshot snapshot, Vector3 position,
            TerrainSurfaceType lie, int strokes)
        {
            NetworkVector3 networkPosition = NetworkVector3.FromUnity(position);
            return new NetworkShotResult(snapshot.MatchId, snapshot.CurrentTurnPlayer, snapshot.TurnIndex,
                snapshot.ShotSequence, networkPosition, networkPosition, lie, strokes, 0, false, false);
        }

        private static IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            Assert.That(Object.FindAnyObjectByType<MatchSessionController>(), Is.Not.Null);
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout, string label)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, $"Timed out waiting for {label}.");
        }
    }
}

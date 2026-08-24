using NUnit.Framework;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Tests
{
    public sealed class M12OnlineFoundationTests
    {
        private static readonly MatchId Match = new("test-match");
        private static readonly MatchPlayerId A = new("a");
        private static readonly MatchPlayerId B = new("b");

        [Test]
        public void MatchStartsWithPlayerA()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            Assert.That(authority.CurrentSnapshot.CurrentTurnPlayer, Is.EqualTo(A));
            Assert.That(authority.CurrentSnapshot.TurnState, Is.EqualTo(TurnState.PreparingShot));
        }

        [Test]
        public void WrongPlayerShotIsRejected()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ShotSubmissionDecision decision = authority.SubmitShot(Submission(authority, B));
            Assert.That(decision.Accepted, Is.False);
            Assert.That(decision.Rejection.Reason, Is.EqualTo(ShotRejectReason.NotYourTurn));
        }

        [Test]
        public void CorrectPlayerShotIsAccepted()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            Assert.That(authority.SubmitShot(Submission(authority, A)).Accepted, Is.True);
        }

        [Test]
        public void DuplicateShotIsRejected()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ShotSubmission submission = Submission(authority, A);
            Assert.That(authority.SubmitShot(submission).Accepted, Is.True);
            ShotSubmissionDecision duplicate = authority.SubmitShot(submission);
            Assert.That(duplicate.Rejection.Reason, Is.EqualTo(ShotRejectReason.DuplicateShot));
        }

        [Test]
        public void InvalidPowerIsRejected()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ShotCommand invalid = CreateCommand(1.5f, ShotSpin.None, ClubType.Driver);
            ShotSubmissionDecision decision = authority.SubmitShot(Submission(authority, A, invalid));
            Assert.That(decision.Rejection.Reason, Is.EqualTo(ShotRejectReason.InvalidCommand));
        }

        [Test]
        public void InvalidSpinIsRejected()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ShotCommand invalid = CreateCommand(0.7f, new ShotSpin(float.NaN, 0f), ClubType.Driver);
            ShotSubmissionDecision decision = authority.SubmitShot(Submission(authority, A, invalid));
            Assert.That(decision.Rejection.Reason, Is.EqualTo(ShotRejectReason.InvalidCommand));
        }

        [Test]
        public void UnsupportedVersionIsRejected()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            MatchSnapshot snapshot = authority.CurrentSnapshot;
            ShotSubmission submission = new(Match, A, snapshot.TurnIndex, snapshot.ShotSequence + 1, 99, CreateCommand());
            Assert.That(authority.SubmitShot(submission).Rejection.Reason,
                Is.EqualTo(ShotRejectReason.UnsupportedVersion));
        }

        [Test]
        public void AcceptedShotSequenceIncrements()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ShotSubmissionDecision decision = authority.SubmitShot(Submission(authority, A));
            Assert.That(decision.Approved.ShotSequence, Is.EqualTo(1));
            Assert.That(authority.CurrentSnapshot.ShotSequence, Is.EqualTo(1));
        }

        [Test]
        public void ShotResultUpdatesPlayerSnapshot()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ApprovedShot approved = ApproveAndBegin(authority, A);
            Assert.That(authority.ResolveShot(Result(approved, 1, 0, new Vector3(12f, 1f, 20f), TerrainSurfaceType.Fairway)), Is.True);
            Assert.That(authority.CurrentSnapshot.TryGetPlayer(A, out PlayerSnapshot player), Is.True);
            Assert.That(player.StrokeCount, Is.EqualTo(1));
            Assert.That(player.BallPosition.X, Is.EqualTo(12f));
        }

        [Test]
        public void TurnAdvancesFromAToB()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ResolveCurrent(authority, A, 1, false);
            Assert.That(authority.CurrentSnapshot.CurrentTurnPlayer, Is.EqualTo(B));
        }

        [Test]
        public void TurnAdvancesFromBToA()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ResolveCurrent(authority, A, 1, false);
            ResolveCurrent(authority, B, 1, false);
            Assert.That(authority.CurrentSnapshot.CurrentTurnPlayer, Is.EqualTo(A));
            Assert.That(authority.CurrentSnapshot.TurnIndex, Is.EqualTo(2));
        }

        [Test]
        public void HoledPlayerStateIsPreservedAndSkipped()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ResolveCurrent(authority, A, 2, true);
            ResolveCurrent(authority, B, 1, false);
            Assert.That(authority.CurrentSnapshot.CurrentTurnPlayer, Is.EqualTo(B));
            Assert.That(authority.CurrentSnapshot.TryGetPlayer(A, out PlayerSnapshot playerA) && playerA.Holed, Is.True);
        }

        [Test]
        public void AllPlayersHoledCompletesHole()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ResolveCurrent(authority, A, 2, true);
            ResolveCurrent(authority, B, 3, true);
            Assert.That(authority.CurrentSnapshot.Phase, Is.EqualTo(MatchPhase.HoleComplete));
        }

        [Test]
        public void StaleSnapshotIsIgnored()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            MatchSnapshot first = authority.CurrentSnapshot;
            ApprovedShot approved = ApproveAndBegin(authority, A);
            MatchSnapshot newer = authority.CurrentSnapshot;
            MatchSnapshotStore store = new();
            Assert.That(store.TryApply(newer), Is.True);
            Assert.That(store.TryApply(first), Is.False);
            Assert.That(store.Current.Version, Is.EqualTo(newer.Version));
            Assert.That(approved.ShotSequence, Is.EqualTo(1));
        }

        [Test]
        public void SerializationRoundTripPreservesEnvelopeAndSnapshot()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            JsonMatchMessageSerializer serializer = new();
            ShotSubmission submission = Submission(authority, A);
            ShotSubmission roundTrip = serializer.Deserialize<ShotSubmission>(serializer.Serialize(submission));
            MatchSnapshot snapshot = serializer.Deserialize<MatchSnapshot>(serializer.Serialize(authority.CurrentSnapshot));
            Assert.That(roundTrip.MatchId, Is.EqualTo(Match));
            Assert.That(roundTrip.Command.Power01, Is.EqualTo(submission.Command.Power01).Within(0.0001f));
            Assert.That(snapshot.PlayerCount, Is.EqualTo(2));
            Assert.That(snapshot.CurrentTurnPlayer, Is.EqualTo(A));
        }

        [Test]
        public void GreenRequiresPutter()
        {
            NetworkVector3 tee = NetworkVector3.FromUnity(Vector3.zero);
            PlayerSnapshot greenPlayer = new(A, "A", 0, 0, true, PlayerConnectionState.Connected,
                1, 0, tee, tee, TerrainSurfaceType.Green, false);
            MatchAuthorityCore authority = new();
            authority.StartMatch(Match, "hole-01", new[] { greenPlayer });
            ShotSubmissionDecision decision = authority.SubmitShot(Submission(authority, A, CreateCommand()));
            Assert.That(decision.Rejection.Reason, Is.EqualTo(ShotRejectReason.InvalidClub));
        }

        [Test]
        public void GreenAcceptsPutter()
        {
            NetworkVector3 green = NetworkVector3.FromUnity(new Vector3(3f, 0f, 8f));
            PlayerSnapshot greenPlayer = new(A, "A", 0, 0, true, PlayerConnectionState.Connected,
                1, 0, green, green, TerrainSurfaceType.Green, false);
            MatchAuthorityCore authority = new();
            authority.StartMatch(Match, "hole-01", new[] { greenPlayer });
            ShotSubmissionDecision decision = authority.SubmitShot(
                Submission(authority, A, CreateCommand(club: ClubType.Putter)));
            Assert.That(decision.Accepted, Is.True);
        }

        [Test]
        public void HazardPenaltyAndLieStayPlayerSpecific()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            ApprovedShot approved = ApproveAndBegin(authority, A);
            Assert.That(authority.ResolveShot(Result(approved, 2, 1,
                new Vector3(4f, 0f, 9f), TerrainSurfaceType.Fairway)), Is.True);
            Assert.That(authority.CurrentSnapshot.TryGetPlayer(A, out PlayerSnapshot playerA), Is.True);
            Assert.That(authority.CurrentSnapshot.TryGetPlayer(B, out PlayerSnapshot playerB), Is.True);
            Assert.That(playerA.StrokeCount, Is.EqualTo(2));
            Assert.That(playerA.PenaltyCount, Is.EqualTo(1));
            Assert.That(playerA.Lie, Is.EqualTo(TerrainSurfaceType.Fairway));
            Assert.That(playerB.StrokeCount, Is.Zero);
            Assert.That(playerB.PenaltyCount, Is.Zero);
            Assert.That(playerB.Lie, Is.EqualTo(TerrainSurfaceType.Tee));
        }

        [Test]
        public void SnapshotStoreResetAcceptsNewMatchVersionOne()
        {
            MatchAuthorityCore authority = StartTwoPlayer();
            MatchSnapshotStore store = new();
            Assert.That(store.TryApply(authority.CurrentSnapshot), Is.True);
            store.Reset();
            Assert.That(store.TryApply(authority.StartMatch(Match, "hole-01", new[]
            {
                authority.CurrentSnapshot.GetPlayer(0), authority.CurrentSnapshot.GetPlayer(1)
            })), Is.True);
            Assert.That(store.Current.Version, Is.EqualTo(1));
        }

        private static MatchAuthorityCore StartTwoPlayer()
        {
            NetworkVector3 tee = NetworkVector3.FromUnity(Vector3.zero);
            PlayerSnapshot a = new(A, "A", 0, 0, true, PlayerConnectionState.Connected,
                0, 0, tee, tee, TerrainSurfaceType.Tee, false);
            PlayerSnapshot b = new(B, "B", 1, 1, false, PlayerConnectionState.Connected,
                0, 0, tee, tee, TerrainSurfaceType.Tee, false);
            MatchAuthorityCore authority = new();
            authority.StartMatch(Match, "hole-01", new[] { a, b });
            return authority;
        }

        private static ShotSubmission Submission(MatchAuthorityCore authority, MatchPlayerId player, ShotCommand? command = null)
        {
            MatchSnapshot snapshot = authority.CurrentSnapshot;
            return new ShotSubmission(Match, player, snapshot.TurnIndex, snapshot.ShotSequence + 1,
                OnlineProtocol.CurrentVersion, command ?? CreateCommand());
        }

        private static ApprovedShot ApproveAndBegin(MatchAuthorityCore authority, MatchPlayerId player)
        {
            ShotSubmissionDecision decision = authority.SubmitShot(Submission(authority, player));
            Assert.That(decision.Accepted, Is.True);
            Assert.That(authority.BeginShotPlayback(decision.Approved), Is.True);
            return decision.Approved;
        }

        private static void ResolveCurrent(MatchAuthorityCore authority, MatchPlayerId player, int strokes, bool holed)
        {
            ApprovedShot approved = ApproveAndBegin(authority, player);
            Assert.That(authority.ResolveShot(Result(approved, strokes, 0, new Vector3(strokes, 0f, strokes * 3f),
                holed ? TerrainSurfaceType.Green : TerrainSurfaceType.Fairway, holed)), Is.True);
        }

        private static NetworkShotResult Result(ApprovedShot approved, int strokes, int penalties,
            Vector3 position, TerrainSurfaceType lie, bool holed = false)
        {
            NetworkVector3 networkPosition = NetworkVector3.FromUnity(position);
            return new NetworkShotResult(Match, approved.PlayerId, approved.TurnIndex, approved.ShotSequence,
                networkPosition, networkPosition, lie, strokes, penalties, holed, holed,
                holed ? strokes - 4 : 0, holed ? "PAR" : string.Empty);
        }

        private static ShotCommand CreateCommand(float power = 0.7f, ShotSpin? spin = null,
            ClubType club = ClubType.Driver)
        {
            ShotCommand command = new(Vector3.forward, Vector3.forward, 0f, power, 1f, 0f,
                ImpactGrade.Perfect, Mathf.Clamp01(power), 0f, 22f, club == ClubType.Putter ? 0f : 35f,
                spin ?? ShotSpin.None);
            return command.WithClub(club, 22f, club == ClubType.Putter ? 0f : 35f, 1f, 1f);
        }
    }
}

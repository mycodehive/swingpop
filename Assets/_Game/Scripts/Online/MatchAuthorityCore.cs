using System;
using System.Collections.Generic;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;

namespace SwingPop.Online
{
    public interface ITurnOrderPolicy
    {
        int FindNextPlayerIndex(PlayerSnapshot[] players, int currentIndex);
    }

    public sealed class RoundRobinTurnOrderPolicy : ITurnOrderPolicy
    {
        public int FindNextPlayerIndex(PlayerSnapshot[] players, int currentIndex)
        {
            if (players == null || players.Length == 0) return -1;
            for (int offset = 1; offset <= players.Length; offset++)
            {
                int candidate = (currentIndex + offset) % players.Length;
                if (!players[candidate].Holed
                    && players[candidate].ConnectionState == PlayerConnectionState.Connected)
                    return candidate;
            }
            return -1;
        }
    }

    public readonly struct ShotSubmissionDecision
    {
        private ShotSubmissionDecision(bool accepted, ApprovedShot approved, ShotRejection rejection)
        {
            Accepted = accepted;
            Approved = approved;
            Rejection = rejection;
        }

        public bool Accepted { get; }
        public ApprovedShot Approved { get; }
        public ShotRejection Rejection { get; }

        public static ShotSubmissionDecision Accept(ApprovedShot approved) => new(true, approved, default);
        public static ShotSubmissionDecision Reject(ShotSubmission submission, ShotRejectReason reason) =>
            new(false, default, new ShotRejection(submission, reason));
    }

    public interface IMatchAuthority
    {
        MatchSnapshot CurrentSnapshot { get; }
        MatchSnapshot StartMatch(MatchId matchId, string holeId, PlayerSnapshot[] players);
        ShotSubmissionDecision SubmitShot(ShotSubmission submission);
        bool BeginShotPlayback(ApprovedShot approved);
        bool ResolveShot(NetworkShotResult result);
    }

    public sealed class MatchAuthorityCore : IMatchAuthority
    {
        private readonly ITurnOrderPolicy turnOrderPolicy;
        private readonly Queue<string> processedOrder = new();
        private readonly HashSet<string> processedShots = new(StringComparer.Ordinal);
        private MatchId matchId;
        private string holeId = string.Empty;
        private MatchPhase phase = MatchPhase.Waiting;
        private TurnState turnState = TurnState.WaitingForPlayer;
        private int turnIndex;
        private int shotSequence;
        private long snapshotVersion;
        private int currentPlayerIndex = -1;
        private PlayerSnapshot[] players = Array.Empty<PlayerSnapshot>();

        public MatchAuthorityCore(ITurnOrderPolicy turnOrderPolicy = null)
        {
            this.turnOrderPolicy = turnOrderPolicy ?? new RoundRobinTurnOrderPolicy();
        }

        public MatchSnapshot CurrentSnapshot => CreateSnapshot();

        public MatchSnapshot StartMatch(MatchId newMatchId, string newHoleId, PlayerSnapshot[] initialPlayers)
        {
            if (!newMatchId.IsValid) throw new ArgumentException("MatchId must be stable and non-empty.", nameof(newMatchId));
            if (string.IsNullOrWhiteSpace(newHoleId)) throw new ArgumentException("HoleId is required.", nameof(newHoleId));
            if (initialPlayers == null || initialPlayers.Length == 0 || initialPlayers.Length > OnlineProtocol.MaximumPlayers)
                throw new ArgumentException($"Player count must be 1-{OnlineProtocol.MaximumPlayers}.", nameof(initialPlayers));

            HashSet<MatchPlayerId> ids = new();
            foreach (PlayerSnapshot player in initialPlayers)
            {
                if (!player.PlayerId.IsValid || !ids.Add(player.PlayerId))
                    throw new ArgumentException("Players require unique stable ids.", nameof(initialPlayers));
            }

            matchId = newMatchId;
            holeId = newHoleId;
            players = (PlayerSnapshot[])initialPlayers.Clone();
            phase = MatchPhase.Playing;
            turnState = TurnState.PreparingShot;
            turnIndex = 0;
            shotSequence = 0;
            snapshotVersion = 1;
            currentPlayerIndex = 0;
            processedOrder.Clear();
            processedShots.Clear();
            return CreateSnapshot();
        }

        public ShotSubmissionDecision SubmitShot(ShotSubmission submission)
        {
            string key = BuildShotKey(submission);
            if (processedShots.Contains(key))
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.DuplicateShot);
            if (phase != MatchPhase.Playing)
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.MatchNotPlaying);
            if (submission.MatchId != matchId)
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.InvalidMatch);
            if (!TryFindPlayer(submission.PlayerId, out int playerIndex))
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.UnknownPlayer);
            if (playerIndex != currentPlayerIndex)
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.NotYourTurn);
            if (submission.TurnIndex != turnIndex || turnState != TurnState.PreparingShot)
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.InvalidTurn);
            if (submission.RequestedShotSequence != shotSequence + 1)
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.InvalidSequence);
            if (submission.CommandVersion != OnlineProtocol.CurrentVersion)
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.UnsupportedVersion);
            if (!IsCommandValid(submission.Command))
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.InvalidCommand);
            if (!IsClubAllowed(players[currentPlayerIndex].Lie, submission.Command.ClubType))
                return ShotSubmissionDecision.Reject(submission, ShotRejectReason.InvalidClub);

            RememberProcessed(key);
            shotSequence++;
            turnState = TurnState.ShotApproved;
            snapshotVersion++;
            return ShotSubmissionDecision.Accept(new ApprovedShot(submission, shotSequence));
        }

        public bool BeginShotPlayback(ApprovedShot approved)
        {
            if (phase != MatchPhase.Playing || turnState != TurnState.ShotApproved
                || approved.MatchId != matchId || approved.PlayerId != players[currentPlayerIndex].PlayerId
                || approved.TurnIndex != turnIndex || approved.ShotSequence != shotSequence)
                return false;

            turnState = TurnState.ShotPlaying;
            snapshotVersion++;
            return true;
        }

        public bool ResolveShot(NetworkShotResult result)
        {
            if (phase != MatchPhase.Playing || turnState != TurnState.ShotPlaying
                || result.MatchId != matchId || result.PlayerId != players[currentPlayerIndex].PlayerId
                || result.TurnIndex != turnIndex || result.ShotSequence != shotSequence
                || !IsResultValid(result))
                return false;

            turnState = TurnState.ResolvingShot;
            players[currentPlayerIndex] = players[currentPlayerIndex].WithShotResult(result);

            bool allHoled = true;
            for (int index = 0; index < players.Length; index++)
                allHoled &= players[index].Holed;

            if (allHoled)
            {
                phase = MatchPhase.HoleComplete;
                turnState = TurnState.TurnComplete;
            }
            else
            {
                int next = turnOrderPolicy.FindNextPlayerIndex(players, currentPlayerIndex);
                if (next < 0)
                {
                    phase = MatchPhase.Aborted;
                    turnState = TurnState.TurnComplete;
                }
                else
                {
                    currentPlayerIndex = next;
                    turnIndex++;
                    turnState = TurnState.PreparingShot;
                }
            }

            snapshotVersion++;
            return true;
        }

        public bool AbortForDisconnect(MatchPlayerId playerId)
        {
            if (!TryFindPlayer(playerId, out int playerIndex)) return false;
            if (players[playerIndex].ConnectionState == PlayerConnectionState.Disconnected) return false;

            players[playerIndex] = players[playerIndex].WithConnectionState(PlayerConnectionState.Disconnected);
            phase = MatchPhase.Aborted;
            turnState = TurnState.TurnComplete;
            snapshotVersion++;
            return true;
        }

        private MatchSnapshot CreateSnapshot()
        {
            MatchPlayerId current = currentPlayerIndex >= 0 && currentPlayerIndex < players.Length
                ? players[currentPlayerIndex].PlayerId
                : default;
            return new MatchSnapshot(matchId, OnlineProtocol.CurrentVersion, snapshotVersion, holeId,
                phase, turnState, turnIndex, shotSequence, current, players);
        }

        private bool TryFindPlayer(MatchPlayerId playerId, out int index)
        {
            for (index = 0; index < players.Length; index++)
                if (players[index].PlayerId == playerId) return true;
            index = -1;
            return false;
        }

        private static bool IsClubAllowed(TerrainSurfaceType lie, ClubType club)
        {
            return lie == TerrainSurfaceType.Green ? club == ClubType.Putter : club == ClubType.Driver;
        }

        private static bool IsCommandValid(ShotCommand command)
        {
            return IsFinite(command.AimDirection.x) && IsFinite(command.AimDirection.y) && IsFinite(command.AimDirection.z)
                   && IsFinite(command.FinalDirection.x) && IsFinite(command.FinalDirection.y) && IsFinite(command.FinalDirection.z)
                   && command.AimDirection.sqrMagnitude > 0.0001f && command.FinalDirection.sqrMagnitude > 0.0001f
                   && InRange(command.AimAngleDegrees, -180f, 180f)
                   && InRange(command.Power01, 0f, 1f)
                   && InRange(command.ImpactAccuracy01, 0f, 1f)
                   && InRange(command.ImpactOffset, -1f, 1f)
                   && InRange(command.EffectivePower01, 0f, 1f)
                   && InRange(command.Spin.VerticalSpin, -1f, 1f)
                   && InRange(command.Spin.SideSpin, -1f, 1f)
                   && InRange(command.BaseLaunchSpeed, 0f, 200f)
                   && InRange(command.LoftDegrees, 0f, 89f)
                   && InRange(command.SurfacePowerModifier, 0f, 2f)
                   && InRange(command.CarryModifier, 0f, 10f)
                   && InRange(command.RollModifier, 0.01f, 10f)
                   && Enum.IsDefined(typeof(ClubType), command.ClubType)
                   && Enum.IsDefined(typeof(ImpactGrade), command.ImpactGrade);
        }

        private static bool IsResultValid(NetworkShotResult result)
        {
            return result.FinalPosition.IsFinite && result.LastValidPosition.IsFinite
                   && result.StrokeCount >= 0 && result.PenaltyCount >= 0
                   && result.PenaltyCount <= result.StrokeCount
                   && Enum.IsDefined(typeof(TerrainSurfaceType), result.FinalLie);
        }

        private static bool InRange(float value, float min, float max)
        {
            return IsFinite(value) && value >= min && value <= max;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string BuildShotKey(ShotSubmission submission)
        {
            return $"{submission.MatchId.Value}|{submission.PlayerId.Value}|{submission.TurnIndex}|{submission.RequestedShotSequence}";
        }

        private void RememberProcessed(string key)
        {
            processedShots.Add(key);
            processedOrder.Enqueue(key);
            while (processedOrder.Count > OnlineProtocol.MaximumProcessedShotHistory)
                processedShots.Remove(processedOrder.Dequeue());
        }
    }

    public sealed class MatchSnapshotStore
    {
        public MatchSnapshot Current { get; private set; }

        public void Reset()
        {
            Current = null;
        }

        public bool TryApply(MatchSnapshot snapshot)
        {
            if (snapshot == null || snapshot.ProtocolVersion != OnlineProtocol.CurrentVersion)
                return false;
            if (Current != null && (snapshot.MatchId != Current.MatchId || snapshot.Version <= Current.Version))
                return false;
            Current = snapshot;
            return true;
        }
    }
}

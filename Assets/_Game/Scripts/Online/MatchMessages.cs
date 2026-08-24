using System;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Online
{
    [Serializable]
    public struct PlayerSnapshot
    {
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private string displayName;
        [SerializeField] private int displayOrder;
        [SerializeField] private int slotIndex;
        [SerializeField] private bool localPresentationHint;
        [SerializeField] private PlayerConnectionState connectionState;
        [SerializeField] private int strokeCount;
        [SerializeField] private int penaltyCount;
        [SerializeField] private NetworkVector3 ballPosition;
        [SerializeField] private NetworkVector3 lastValidPosition;
        [SerializeField] private TerrainSurfaceType lie;
        [SerializeField] private bool holed;
        [SerializeField] private int relativeToPar;
        [SerializeField] private string scoreLabel;

        public PlayerSnapshot(
            MatchPlayerId playerId,
            string displayName,
            int displayOrder,
            int slotIndex,
            bool localPresentationHint,
            PlayerConnectionState connectionState,
            int strokeCount,
            int penaltyCount,
            NetworkVector3 ballPosition,
            NetworkVector3 lastValidPosition,
            TerrainSurfaceType lie,
            bool holed,
            int relativeToPar = 0,
            string scoreLabel = "")
        {
            this.playerId = playerId;
            this.displayName = displayName ?? string.Empty;
            this.displayOrder = displayOrder;
            this.slotIndex = slotIndex;
            this.localPresentationHint = localPresentationHint;
            this.connectionState = connectionState;
            this.strokeCount = Mathf.Max(0, strokeCount);
            this.penaltyCount = Mathf.Clamp(penaltyCount, 0, this.strokeCount);
            this.ballPosition = ballPosition;
            this.lastValidPosition = lastValidPosition;
            this.lie = lie;
            this.holed = holed;
            this.relativeToPar = relativeToPar;
            this.scoreLabel = scoreLabel ?? string.Empty;
        }

        public MatchPlayerId PlayerId => playerId;
        public string DisplayName => displayName ?? string.Empty;
        public int DisplayOrder => displayOrder;
        public int SlotIndex => slotIndex;
        public bool LocalPresentationHint => localPresentationHint;
        public PlayerConnectionState ConnectionState => connectionState;
        public int StrokeCount => strokeCount;
        public int PenaltyCount => penaltyCount;
        public NetworkVector3 BallPosition => ballPosition;
        public NetworkVector3 LastValidPosition => lastValidPosition;
        public TerrainSurfaceType Lie => lie;
        public bool Holed => holed;
        public int RelativeToPar => relativeToPar;
        public string ScoreLabel => scoreLabel ?? string.Empty;

        public PlayerSnapshot WithShotResult(NetworkShotResult result)
        {
            return new PlayerSnapshot(
                playerId,
                displayName,
                displayOrder,
                slotIndex,
                localPresentationHint,
                connectionState,
                result.StrokeCount,
                result.PenaltyCount,
                result.FinalPosition,
                result.LastValidPosition,
                result.FinalLie,
                result.Holed,
                result.RelativeToPar,
                result.ScoreLabel);
        }
    }

    [Serializable]
    public sealed class MatchSnapshot
    {
        [SerializeField] private MatchId matchId;
        [SerializeField] private int protocolVersion;
        [SerializeField] private long version;
        [SerializeField] private string holeId;
        [SerializeField] private MatchPhase phase;
        [SerializeField] private TurnState turnState;
        [SerializeField] private int turnIndex;
        [SerializeField] private int shotSequence;
        [SerializeField] private MatchPlayerId currentTurnPlayer;
        [SerializeField] private PlayerSnapshot[] players;

        public MatchSnapshot(
            MatchId matchId,
            int protocolVersion,
            long version,
            string holeId,
            MatchPhase phase,
            TurnState turnState,
            int turnIndex,
            int shotSequence,
            MatchPlayerId currentTurnPlayer,
            PlayerSnapshot[] players)
        {
            this.matchId = matchId;
            this.protocolVersion = protocolVersion;
            this.version = version;
            this.holeId = holeId ?? string.Empty;
            this.phase = phase;
            this.turnState = turnState;
            this.turnIndex = turnIndex;
            this.shotSequence = shotSequence;
            this.currentTurnPlayer = currentTurnPlayer;
            this.players = players != null ? (PlayerSnapshot[])players.Clone() : Array.Empty<PlayerSnapshot>();
        }

        public MatchId MatchId => matchId;
        public int ProtocolVersion => protocolVersion;
        public long Version => version;
        public string HoleId => holeId ?? string.Empty;
        public MatchPhase Phase => phase;
        public TurnState TurnState => turnState;
        public int TurnIndex => turnIndex;
        public int ShotSequence => shotSequence;
        public MatchPlayerId CurrentTurnPlayer => currentTurnPlayer;
        public int PlayerCount => players?.Length ?? 0;

        public PlayerSnapshot GetPlayer(int index)
        {
            if (players == null || index < 0 || index >= players.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return players[index];
        }

        public bool TryGetPlayer(MatchPlayerId id, out PlayerSnapshot player)
        {
            if (players != null)
            {
                for (int index = 0; index < players.Length; index++)
                {
                    if (players[index].PlayerId == id)
                    {
                        player = players[index];
                        return true;
                    }
                }
            }
            player = default;
            return false;
        }

        internal PlayerSnapshot[] CopyPlayers()
        {
            return players != null ? (PlayerSnapshot[])players.Clone() : Array.Empty<PlayerSnapshot>();
        }
    }

    [Serializable]
    public struct ShotSubmission
    {
        [SerializeField] private MatchId matchId;
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private int turnIndex;
        [SerializeField] private int requestedShotSequence;
        [SerializeField] private int commandVersion;
        [SerializeField] private ShotCommand command;

        public ShotSubmission(MatchId matchId, MatchPlayerId playerId, int turnIndex,
            int requestedShotSequence, int commandVersion, ShotCommand command)
        {
            this.matchId = matchId;
            this.playerId = playerId;
            this.turnIndex = turnIndex;
            this.requestedShotSequence = requestedShotSequence;
            this.commandVersion = commandVersion;
            this.command = command;
        }

        public MatchId MatchId => matchId;
        public MatchPlayerId PlayerId => playerId;
        public int TurnIndex => turnIndex;
        public int RequestedShotSequence => requestedShotSequence;
        public int CommandVersion => commandVersion;
        public ShotCommand Command => command;
    }

    [Serializable]
    public struct ApprovedShot
    {
        [SerializeField] private MatchId matchId;
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private int turnIndex;
        [SerializeField] private int shotSequence;
        [SerializeField] private int commandVersion;
        [SerializeField] private ShotCommand command;

        public ApprovedShot(ShotSubmission submission, int authorityShotSequence)
        {
            matchId = submission.MatchId;
            playerId = submission.PlayerId;
            turnIndex = submission.TurnIndex;
            shotSequence = authorityShotSequence;
            commandVersion = submission.CommandVersion;
            command = submission.Command;
        }

        public MatchId MatchId => matchId;
        public MatchPlayerId PlayerId => playerId;
        public int TurnIndex => turnIndex;
        public int ShotSequence => shotSequence;
        public int CommandVersion => commandVersion;
        public ShotCommand Command => command;
    }

    [Serializable]
    public struct ShotRejection
    {
        [SerializeField] private MatchId matchId;
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private int turnIndex;
        [SerializeField] private int requestedShotSequence;
        [SerializeField] private ShotRejectReason reason;

        public ShotRejection(ShotSubmission submission, ShotRejectReason reason)
        {
            matchId = submission.MatchId;
            playerId = submission.PlayerId;
            turnIndex = submission.TurnIndex;
            requestedShotSequence = submission.RequestedShotSequence;
            this.reason = reason;
        }

        public MatchId MatchId => matchId;
        public MatchPlayerId PlayerId => playerId;
        public int TurnIndex => turnIndex;
        public int RequestedShotSequence => requestedShotSequence;
        public ShotRejectReason Reason => reason;
    }

    [Serializable]
    public struct NetworkShotResult
    {
        [SerializeField] private MatchId matchId;
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private int turnIndex;
        [SerializeField] private int shotSequence;
        [SerializeField] private NetworkVector3 finalPosition;
        [SerializeField] private NetworkVector3 lastValidPosition;
        [SerializeField] private TerrainSurfaceType finalLie;
        [SerializeField] private int strokeCount;
        [SerializeField] private int penaltyCount;
        [SerializeField] private bool holed;
        [SerializeField] private bool holeComplete;
        [SerializeField] private int relativeToPar;
        [SerializeField] private string scoreLabel;

        public NetworkShotResult(MatchId matchId, MatchPlayerId playerId, int turnIndex, int shotSequence,
            NetworkVector3 finalPosition, NetworkVector3 lastValidPosition, TerrainSurfaceType finalLie,
            int strokeCount, int penaltyCount, bool holed, bool holeComplete,
            int relativeToPar = 0, string scoreLabel = "")
        {
            this.matchId = matchId;
            this.playerId = playerId;
            this.turnIndex = turnIndex;
            this.shotSequence = shotSequence;
            this.finalPosition = finalPosition;
            this.lastValidPosition = lastValidPosition;
            this.finalLie = finalLie;
            this.strokeCount = strokeCount;
            this.penaltyCount = penaltyCount;
            this.holed = holed;
            this.holeComplete = holeComplete;
            this.relativeToPar = relativeToPar;
            this.scoreLabel = scoreLabel ?? string.Empty;
        }

        public MatchId MatchId => matchId;
        public MatchPlayerId PlayerId => playerId;
        public int TurnIndex => turnIndex;
        public int ShotSequence => shotSequence;
        public NetworkVector3 FinalPosition => finalPosition;
        public NetworkVector3 LastValidPosition => lastValidPosition;
        public TerrainSurfaceType FinalLie => finalLie;
        public int StrokeCount => strokeCount;
        public int PenaltyCount => penaltyCount;
        public bool Holed => holed;
        public bool HoleComplete => holeComplete;
        public int RelativeToPar => relativeToPar;
        public string ScoreLabel => scoreLabel ?? string.Empty;
    }

    [Serializable]
    public struct PlayerResult
    {
        [SerializeField] private MatchPlayerId playerId;
        [SerializeField] private int strokes;
        [SerializeField] private int penalties;
        [SerializeField] private int relativeToPar;
        [SerializeField] private string label;

        public PlayerResult(PlayerSnapshot player)
        {
            playerId = player.PlayerId;
            strokes = player.StrokeCount;
            penalties = player.PenaltyCount;
            relativeToPar = player.RelativeToPar;
            label = player.ScoreLabel;
        }

        public MatchPlayerId PlayerId => playerId;
        public int Strokes => strokes;
        public int Penalties => penalties;
        public int RelativeToPar => relativeToPar;
        public string Label => label ?? string.Empty;
    }
}

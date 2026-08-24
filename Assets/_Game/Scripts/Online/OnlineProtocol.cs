namespace SwingPop.Online
{
    public static class OnlineProtocol
    {
        public const int CurrentVersion = 1;
        public const int MaximumPlayers = 4;
        public const int MaximumProcessedShotHistory = 64;
    }

    public enum MultiplayerDevelopmentMode
    {
        OfflineSingle,
        LocalTwoPlayer
    }

    public enum MatchPhase
    {
        Waiting,
        Starting,
        Playing,
        HoleComplete,
        MatchComplete,
        Disconnected,
        Aborted
    }

    public enum TurnState
    {
        WaitingForPlayer,
        PreparingShot,
        ShotSubmitted,
        ShotApproved,
        ShotPlaying,
        ResolvingShot,
        TurnComplete
    }

    public enum PlayerConnectionState
    {
        Connected,
        Disconnected
    }

    public enum ShotRejectReason
    {
        None,
        InvalidMatch,
        UnknownPlayer,
        NotYourTurn,
        InvalidTurn,
        InvalidSequence,
        DuplicateShot,
        InvalidCommand,
        UnsupportedVersion,
        MatchNotPlaying,
        InvalidClub
    }
}

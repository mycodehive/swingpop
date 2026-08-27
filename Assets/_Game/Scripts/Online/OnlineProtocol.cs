namespace SwingPop.Online
{
    public static class OnlineProtocol
    {
        public const int CurrentVersion = 2;
        public const int MaximumPlayers = 4;
        public const int DedicatedServerPlayerCapacity = 2;
        public const int MaximumProcessedShotHistory = 64;
        public const int MaximumPayloadBytes = 64 * 1024;
    }

    public enum MultiplayerDevelopmentMode
    {
        OfflineSingle,
        LocalTwoPlayer,
        NetworkHost,
        NetworkClient,
        DedicatedServer
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
        Disconnected,
        ReconnectGrace,
        Expired
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
        InvalidClub,
        PlayerSpoofing,
        StaleMessage,
        PayloadTooLarge,
        RateLimited,
        ConnectionNotReady,
        MatchSuspended,
        MatchFull,
        MessageDirectionNotAllowed
    }

    public enum NetworkRole
    {
        None,
        Host,
        Client,
        DedicatedServer
    }

    public enum NetworkConnectionState
    {
        Offline,
        Starting,
        Listening,
        Connecting,
        Handshaking,
        Connected,
        InMatch,
        Disconnecting,
        Disconnected,
        Failed
    }

    public enum NetworkMessageType
    {
        ClientHello,
        PlayerAssigned,
        MatchStarted,
        ShotSubmission,
        ShotApproved,
        ShotRejected,
        Snapshot,
        SnapshotHash,
        TurnChanged,
        PredictedShotResult,
        Ping,
        Pong,
        DisconnectNotice,
        ConnectionRejected,
        ReconnectTicketIssued,
        ReconnectRequest,
        ReconnectAccepted,
        ReconnectRejected,
        MatchLifecycleChanged
    }

    public enum ClientRequestedRole
    {
        Player
    }

    public enum DedicatedMatchLifecycleState
    {
        WaitingForPlayers,
        Starting,
        Playing,
        ReconnectGrace,
        HoleComplete,
        Ended,
        Aborted
    }
}

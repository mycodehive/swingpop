using System;

namespace SwingPop.Gameplay.Shot
{
    /// <summary>
    /// Optional approval boundary used by coordinated sessions. Offline gameplay leaves this unset.
    /// </summary>
    public interface IShotCommitGate
    {
        bool RequiresApproval { get; }
        bool CanSubmitShot { get; }
        event Action<ShotCommand> ShotApproved;
        event Action ShotRejected;
        bool TrySubmitShot(ShotCommand command);
    }
}

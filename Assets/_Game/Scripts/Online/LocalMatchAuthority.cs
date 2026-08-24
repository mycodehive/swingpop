using UnityEngine;

namespace SwingPop.Online
{
    [DisallowMultipleComponent]
    public sealed class LocalMatchAuthority : MonoBehaviour, IMatchAuthority
    {
        private MatchAuthorityCore core;

        public MatchSnapshot CurrentSnapshot => EnsureCore().CurrentSnapshot;

        public MatchSnapshot StartMatch(MatchId matchId, string holeId, PlayerSnapshot[] players)
        {
            return EnsureCore().StartMatch(matchId, holeId, players);
        }

        public ShotSubmissionDecision SubmitShot(ShotSubmission submission)
        {
            return EnsureCore().SubmitShot(submission);
        }

        public bool BeginShotPlayback(ApprovedShot approved)
        {
            return EnsureCore().BeginShotPlayback(approved);
        }

        public bool ResolveShot(NetworkShotResult result)
        {
            return EnsureCore().ResolveShot(result);
        }

        private MatchAuthorityCore EnsureCore()
        {
            return core ??= new MatchAuthorityCore(new RoundRobinTurnOrderPolicy());
        }
    }
}

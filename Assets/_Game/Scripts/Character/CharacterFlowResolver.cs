using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Shot;

namespace SwingPop.CharacterSystem
{
    public static class CharacterFlowResolver
    {
        public static CharacterState ResolveShotState(ShotFlowState shotState, bool isPutter)
        {
            return shotState switch
            {
                ShotFlowState.Aiming or ShotFlowState.PowerSelecting => isPutter
                    ? CharacterState.PuttAddress
                    : CharacterState.Address,
                ShotFlowState.ImpactSelecting => isPutter
                    ? CharacterState.PuttBackSwing
                    : CharacterState.BackSwing,
                ShotFlowState.ShotCommitted => isPutter
                    ? CharacterState.PuttSwing
                    : CharacterState.Swing,
                _ => CharacterState.Idle
            };
        }

        public static CharacterState ResolveCelebration(ScoreResult result)
        {
            if (result.Strokes == 1)
            {
                return CharacterState.HoleInOneCelebration;
            }
            if (result.RelativeToPar <= -2)
            {
                return CharacterState.EagleCelebration;
            }
            if (result.RelativeToPar == -1)
            {
                return CharacterState.BirdieCelebration;
            }
            return result.RelativeToPar <= 0 ? CharacterState.Happy : CharacterState.Sad;
        }

        public static CharacterClubVisualType ResolveClubVisual(ClubType clubType)
        {
            return clubType == ClubType.Putter
                ? CharacterClubVisualType.Putter
                : CharacterClubVisualType.Driver;
        }
    }
}

using SwingPop.Data;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;

namespace SwingPop.UI
{
    public static class HudSkinStyleMapper
    {
        public static HudSkinTone ForImpact(ImpactGrade grade)
        {
            return grade switch
            {
                ImpactGrade.Perfect => HudSkinTone.Gold,
                ImpactGrade.Great => HudSkinTone.Cyan,
                ImpactGrade.Good => HudSkinTone.Mint,
                _ => HudSkinTone.Coral
            };
        }

        public static HudSkinTone ForLie(TerrainSurfaceType lie)
        {
            return lie switch
            {
                TerrainSurfaceType.Fairway or TerrainSurfaceType.Tee => HudSkinTone.Fairway,
                TerrainSurfaceType.Rough => HudSkinTone.Rough,
                TerrainSurfaceType.Bunker => HudSkinTone.Bunker,
                TerrainSurfaceType.Green => HudSkinTone.Green,
                TerrainSurfaceType.Water or TerrainSurfaceType.OutOfBounds => HudSkinTone.Coral,
                _ => HudSkinTone.SecondaryText
            };
        }

        public static HudSkinTone ForResult(ScoreResult result)
        {
            if (result.RelativeToPar < 0) return HudSkinTone.Gold;
            if (result.RelativeToPar == 0) return HudSkinTone.Cyan;
            return HudSkinTone.Coral;
        }

        public static HudSkinTone ForAction(ShotFlowState state)
        {
            return state switch
            {
                ShotFlowState.PowerSelecting => HudSkinTone.Mint,
                ShotFlowState.ImpactSelecting => HudSkinTone.Gold,
                _ => HudSkinTone.Cyan
            };
        }
    }
}

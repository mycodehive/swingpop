using SwingPop.AudioSystem;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;

namespace SwingPop.Presentation
{
    public static class ShotPresentationResolver
    {
        public static ShotPresentationLevel ResolveImpact(ImpactGrade grade)
        {
            return grade == ImpactGrade.Perfect
                ? ShotPresentationLevel.Perfect
                : ShotPresentationLevel.Normal;
        }

        public static TrailPresentationProfile ResolveTrail(
            ShotPresentationLevel level,
            float normalLifetime,
            float normalWidth,
            float perfectLifetime,
            float perfectWidth)
        {
            return level == ShotPresentationLevel.Perfect
                ? new TrailPresentationProfile(perfectLifetime, perfectWidth)
                : new TrailPresentationProfile(normalLifetime, normalWidth);
        }

        public static LandingEffectType ResolveLanding(TerrainSurfaceType surface)
        {
            return surface switch
            {
                TerrainSurfaceType.Rough => LandingEffectType.Rough,
                TerrainSurfaceType.Bunker => LandingEffectType.Sand,
                TerrainSurfaceType.Water => LandingEffectType.Water,
                TerrainSurfaceType.OutOfBounds => LandingEffectType.OutOfBounds,
                _ => LandingEffectType.Grass
            };
        }

        public static CelebrationPresentationLevel ResolveCelebration(ScoreResult result)
        {
            return result.RelativeToPar switch
            {
                <= -2 => CelebrationPresentationLevel.Strongest,
                -1 => CelebrationPresentationLevel.Strong,
                0 => CelebrationPresentationLevel.Normal,
                _ => CelebrationPresentationLevel.Subdued
            };
        }

        public static GameplayAudioCue ResolveLandingAudio(TerrainSurfaceType surface)
        {
            return surface switch
            {
                TerrainSurfaceType.Rough => GameplayAudioCue.RoughLanding,
                TerrainSurfaceType.Bunker => GameplayAudioCue.BunkerLanding,
                TerrainSurfaceType.Green => GameplayAudioCue.GreenLanding,
                _ => GameplayAudioCue.FairwayLanding
            };
        }

        public static GameplayAudioCue ResolveHazardAudio(TerrainSurfaceType hazard)
        {
            return hazard == TerrainSurfaceType.Water
                ? GameplayAudioCue.WaterHazard
                : GameplayAudioCue.OutOfBounds;
        }
    }
}

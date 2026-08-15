using SwingPop.Gameplay.Course;

namespace SwingPop.Gameplay.Hole
{
    public static class CupCaptureRules
    {
        public static bool IsEligible(
            float horizontalDistance,
            float horizontalSpeed,
            float heightDifference,
            TerrainSurfaceType lie,
            float captureRadius,
            float maximumCaptureSpeed,
            float maximumHeightDifference)
        {
            return lie == TerrainSurfaceType.Green
                   && horizontalDistance <= System.Math.Max(0f, captureRadius)
                   && horizontalSpeed <= System.Math.Max(0f, maximumCaptureSpeed)
                   && System.Math.Abs(heightDifference) <= System.Math.Max(0f, maximumHeightDifference);
        }
    }
}

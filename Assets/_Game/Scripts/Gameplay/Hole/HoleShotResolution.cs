using SwingPop.Gameplay.Course;
using UnityEngine;

namespace SwingPop.Gameplay.Hole
{
    public readonly struct HoleShotResolution
    {
        public HoleShotResolution(
            Vector3 ballPosition,
            Vector3 lastValidPosition,
            TerrainSurfaceType lie,
            int strokeCount,
            int penaltyCount,
            bool wasHazard)
        {
            BallPosition = ballPosition;
            LastValidPosition = lastValidPosition;
            Lie = lie;
            StrokeCount = strokeCount;
            PenaltyCount = penaltyCount;
            WasHazard = wasHazard;
        }

        public Vector3 BallPosition { get; }
        public Vector3 LastValidPosition { get; }
        public TerrainSurfaceType Lie { get; }
        public int StrokeCount { get; }
        public int PenaltyCount { get; }
        public bool WasHazard { get; }
    }
}

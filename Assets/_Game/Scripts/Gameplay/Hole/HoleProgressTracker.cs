using SwingPop.Gameplay.Course;
using UnityEngine;

namespace SwingPop.Gameplay.Hole
{
    public sealed class HoleProgressTracker
    {
        public HoleFlowState State { get; private set; } = HoleFlowState.HoleStart;
        public int StrokeCount { get; private set; }
        public int PenaltyCount { get; private set; }
        public Vector3 LastValidPosition { get; private set; }
        public TerrainSurfaceType LastValidLie { get; private set; } = TerrainSurfaceType.Tee;
        public ScoreResult Result { get; private set; }

        public void StartHole(Vector3 teePosition)
        {
            StrokeCount = 0;
            PenaltyCount = 0;
            LastValidPosition = teePosition;
            LastValidLie = TerrainSurfaceType.Tee;
            Result = default;
            State = HoleFlowState.Playing;
        }

        public void RecordCommittedShot()
        {
            if (State == HoleFlowState.Playing)
            {
                StrokeCount++;
            }
        }

        public void RecordValidStop(Vector3 position, TerrainSurfaceType lie)
        {
            if (State != HoleFlowState.Playing || IsHazard(lie))
            {
                return;
            }

            LastValidPosition = position;
            LastValidLie = lie;
        }

        public void RecordHazardPenalty()
        {
            if (State != HoleFlowState.Playing)
            {
                return;
            }

            StrokeCount++;
            PenaltyCount++;
        }

        public void RestorePlaying(
            int strokeCount,
            int penaltyCount,
            Vector3 lastValidPosition,
            TerrainSurfaceType lastValidLie)
        {
            StrokeCount = Mathf.Max(0, strokeCount);
            PenaltyCount = Mathf.Clamp(penaltyCount, 0, StrokeCount);
            LastValidPosition = lastValidPosition;
            LastValidLie = IsHazard(lastValidLie) ? TerrainSurfaceType.Fairway : lastValidLie;
            Result = default;
            State = HoleFlowState.Playing;
        }

        public ScoreResult Complete(int par)
        {
            if (State != HoleFlowState.HoleComplete)
            {
                Result = ScoreCalculator.Calculate(par, StrokeCount);
                State = HoleFlowState.HoleComplete;
            }

            return Result;
        }

        private static bool IsHazard(TerrainSurfaceType lie)
        {
            return lie is TerrainSurfaceType.Water or TerrainSurfaceType.OutOfBounds;
        }
    }
}

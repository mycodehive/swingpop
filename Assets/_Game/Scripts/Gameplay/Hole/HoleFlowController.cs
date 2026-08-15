using System;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Gameplay.Hole
{
    [DefaultExecutionOrder(-100)]
    public sealed class HoleFlowController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private HoleData hole;
        [SerializeField] private GolfBallController ball;
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private ClubData normalClub;
        [SerializeField] private ClubData putter;
        [SerializeField] private TerrainSurfaceData teeSurface;

        private readonly HoleProgressTracker progress = new();
        private TerrainSurfaceData lastValidSurface;
        private bool shotInProgress;
        private bool pendingNextShot;
        private bool automaticFlowSuspended;
        private Vector3 pendingPosition;
        private TerrainSurfaceData pendingSurface;

        public event Action<HoleFlowState, HoleFlowState> StateChanged;
        public event Action<int> StrokeChanged;
        public event Action<ScoreResult> HoleCompleted;

        public HoleData Hole => hole;
        public HoleFlowState State => progress.State;
        public int StrokeCount => progress.StrokeCount;
        public int PenaltyCount => progress.PenaltyCount;
        public Vector3 LastValidPosition => progress.LastValidPosition;
        public TerrainSurfaceType CurrentLie => ball != null ? ball.CurrentLie : progress.LastValidLie;
        public ClubData CurrentClub => shotFlow != null ? shotFlow.CurrentClub : null;
        public ScoreResult Result => progress.Result;
        public float RemainingDistance => hole != null && ball != null
            ? Vector3.Distance(ball.PhysicsPosition, hole.CupPosition)
            : 0f;
        public float HeightDifference => hole != null && ball != null
            ? hole.CupPosition.y - ball.PhysicsPosition.y
            : 0f;

        private void OnEnable()
        {
            if (shotFlow != null)
            {
                shotFlow.ShotCommitted += OnShotCommitted;
                shotFlow.DebugResetRequested += DebugResetHole;
            }

            if (ball != null)
            {
                ball.StateChanged += OnBallStateChanged;
            }
        }

        private void Start()
        {
            BeginHole();
        }

        private void Update()
        {
            if (!pendingNextShot || automaticFlowSuspended)
            {
                return;
            }

            pendingNextShot = false;
            ball.PrepareNextShot(pendingPosition, pendingSurface);
            PrepareShotFromCurrentLie();
        }

        private void OnDisable()
        {
            if (shotFlow != null)
            {
                shotFlow.ShotCommitted -= OnShotCommitted;
                shotFlow.DebugResetRequested -= DebugResetHole;
            }

            if (ball != null)
            {
                ball.StateChanged -= OnBallStateChanged;
            }
        }

        public void BeginHole()
        {
            if (hole == null || ball == null || shotFlow == null)
            {
                Debug.LogError("HoleFlowController requires HoleData, GolfBallController, and ShotFlowController.");
                return;
            }

            HoleFlowState previous = progress.State;
            progress.StartHole(hole.TeePosition);
            shotInProgress = false;
            pendingNextShot = false;
            lastValidSurface = teeSurface;
            ball.SetResetPose(hole.TeePosition, Quaternion.identity, teeSurface, true);
            shotFlow.PrepareNextShot(hole.CupPosition - hole.TeePosition, normalClub);
            StateChanged?.Invoke(previous, progress.State);
            StrokeChanged?.Invoke(progress.StrokeCount);
        }

        public void DebugResetHole()
        {
            BeginHole();
        }

        public bool TryCompleteHole(GolfBallController candidate)
        {
            if (candidate == null || candidate != ball || progress.State != HoleFlowState.Playing)
            {
                return false;
            }

            HoleFlowState previous = progress.State;
            shotInProgress = false;
            pendingNextShot = false;
            ball.HoleBall(hole.CupPosition);
            ScoreResult result = progress.Complete(hole.Par);
            StateChanged?.Invoke(previous, progress.State);
            HoleCompleted?.Invoke(result);
            return true;
        }

        public void SetAutomaticFlowSuspended(bool suspended)
        {
            automaticFlowSuspended = suspended;
        }

        private void OnShotCommitted(ShotCommand command)
        {
            if (progress.State != HoleFlowState.Playing)
            {
                return;
            }

            shotInProgress = true;
            progress.RecordCommittedShot();
            StrokeChanged?.Invoke(progress.StrokeCount);
        }

        private void OnBallStateChanged(BallState previousState, BallState nextState)
        {
            if (nextState != BallState.Stopped
                || !shotInProgress
                || progress.State != HoleFlowState.Playing
                || automaticFlowSuspended)
            {
                return;
            }

            shotInProgress = false;
            if (ball.HasLastHazard)
            {
                progress.RecordHazardPenalty();
                StrokeChanged?.Invoke(progress.StrokeCount);
                pendingPosition = progress.LastValidPosition;
                pendingSurface = lastValidSurface;
            }
            else
            {
                progress.RecordValidStop(ball.PhysicsPosition, ball.CurrentLie);
                lastValidSurface = ball.CurrentSurfaceData ?? lastValidSurface;
                pendingPosition = ball.PhysicsPosition;
                pendingSurface = lastValidSurface;
            }

            pendingNextShot = true;
        }

        private void PrepareShotFromCurrentLie()
        {
            ClubData nextClub = ball.CurrentLie == TerrainSurfaceType.Green ? putter : normalClub;
            Vector3 cupDirection = hole.CupPosition - ball.PhysicsPosition;
            shotFlow.PrepareNextShot(cupDirection, nextClub);
        }
    }
}

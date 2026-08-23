using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.VfxSystem;
using UnityEngine;

namespace SwingPop.Presentation
{
    public sealed class ShotPresentationController : MonoBehaviour
    {
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private GolfBallController ball;
        [SerializeField] private HoleFlowController holeFlow;
        [SerializeField] private ImpactVfxController impactVfx;
        [SerializeField] private BallTrailController ballTrail;
        [SerializeField] private LandingVfxController landingVfx;
        [SerializeField] private HoleInVfxController holeInVfx;

        private readonly ImpactPresentationGate impactGate = new();

        public int ImpactPresentationCount { get; private set; }
        public int SurfacePresentationCount => landingVfx != null ? landingVfx.PlayCount : 0;
        public int HolePresentationCount => holeInVfx != null ? holeInVfx.PlayCount : 0;
        public ShotPresentationLevel LastImpactLevel { get; private set; }
        public LandingEffectType LastLandingEffect => landingVfx != null
            ? landingVfx.LastEffect
            : LandingEffectType.Grass;
        public int ReusableEffectObjectCount => GetComponentsInChildren<ParticleSystem>(true).Length
                                                + GetComponentsInChildren<TrailRenderer>(true).Length;

        private void OnEnable()
        {
            if (shotFlow != null)
            {
                shotFlow.ShotCommitted += OnShotCommitted;
            }
            if (ball != null)
            {
                ball.Launched += OnBallLaunched;
                ball.SurfaceContacted += OnSurfaceContacted;
                ball.HazardEntered += OnHazardEntered;
                ball.ResetPerformed += OnBallReset;
                ball.StateChanged += OnBallStateChanged;
            }
            if (holeFlow != null)
            {
                holeFlow.HoleCompleted += OnHoleCompleted;
            }
        }

        private void OnDisable()
        {
            if (shotFlow != null)
            {
                shotFlow.ShotCommitted -= OnShotCommitted;
            }
            if (ball != null)
            {
                ball.Launched -= OnBallLaunched;
                ball.SurfaceContacted -= OnSurfaceContacted;
                ball.HazardEntered -= OnHazardEntered;
                ball.ResetPerformed -= OnBallReset;
                ball.StateChanged -= OnBallStateChanged;
            }
            if (holeFlow != null)
            {
                holeFlow.HoleCompleted -= OnHoleCompleted;
            }
        }

        private void OnShotCommitted(ShotCommand command)
        {
            impactGate.Arm();
        }

        private void OnBallLaunched()
        {
            if (!impactGate.TryConsume() || !shotFlow.HasLastShotCommand)
            {
                return;
            }

            ShotCommand command = shotFlow.LastShotCommand;
            LastImpactLevel = ShotPresentationResolver.ResolveImpact(command.ImpactGrade);
            ImpactPresentationCount++;
            impactVfx?.Play(ball.PhysicsPosition, command.FinalDirection, LastImpactLevel);
            ballTrail?.Begin(LastImpactLevel, command.Spin);
        }

        private void OnSurfaceContacted(BallSurfaceContact contact)
        {
            landingVfx?.PlayContact(contact);
        }

        private void OnHazardEntered(TerrainSurfaceType hazard)
        {
            landingVfx?.PlayHazard(ball.PhysicsPosition, hazard);
            ballTrail?.StopAndClear();
        }

        private void OnBallStateChanged(BallState previous, BallState next)
        {
            if (next is BallState.Ready or BallState.Stopped or BallState.Holed)
            {
                ballTrail?.StopAndClear();
            }
        }

        private void OnBallReset()
        {
            impactGate.Reset();
            ballTrail?.StopAndClear();
        }

        private void OnHoleCompleted(ScoreResult result)
        {
            holeInVfx?.Play(
                holeFlow.Hole.CupPosition,
                ShotPresentationResolver.ResolveCelebration(result));
        }
    }
}

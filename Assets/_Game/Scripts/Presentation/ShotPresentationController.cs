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
        private bool holePresentationPlayed;

        public int ImpactPresentationCount { get; private set; }
        public int SurfacePresentationCount => landingVfx != null ? landingVfx.PlayCount : 0;
        public int HolePresentationCount => holeInVfx != null ? holeInVfx.PlayCount : 0;
        public ShotPresentationLevel LastImpactLevel { get; private set; }
        public bool LastImpactWasPutter { get; private set; }
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
            LastImpactWasPutter = command.IsPutter;
            ImpactPresentationCount++;
            // Ball position at the authoritative launch event is the contact anchor. Presentation never
            // reconstructs aim or samples the camera/character root for impact placement or direction.
            impactVfx?.Play(ball.PhysicsPosition, command.FinalDirection, LastImpactLevel, command.IsPutter);
            ballTrail?.Begin(LastImpactLevel, command.Spin, command.IsPutter);
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
            holePresentationPlayed = false;
            ballTrail?.StopAndClear();
        }

        private void OnHoleCompleted(ScoreResult result)
        {
            if (holePresentationPlayed)
            {
                return;
            }
            holePresentationPlayed = true;
            holeInVfx?.Play(
                holeFlow.Hole.CupPosition,
                ShotPresentationResolver.ResolveCelebration(result));
        }
    }
}

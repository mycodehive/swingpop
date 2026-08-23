using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using UnityEngine;

namespace SwingPop.VfxSystem
{
    public sealed class BallTrailController : MonoBehaviour
    {
        [SerializeField] private GolfBallController ball;
        [SerializeField] private ShotPresentationTuningData tuning;
        [SerializeField] private TrailRenderer coreTrail;
        [SerializeField] private TrailRenderer accentTrail;

        private bool active;
        private bool accentEnabled;

        public ShotPresentationLevel CurrentLevel { get; private set; }
        public bool IsEmitting => coreTrail != null && coreTrail.emitting;
        public float CurrentWidth { get; private set; }
        public float CurrentLifetime { get; private set; }

        private void Update()
        {
            if (ball != null)
            {
                transform.position = ball.PhysicsPosition;
            }

            bool shouldEmit = active
                              && ball != null
                              && ball.Speed >= (tuning != null ? tuning.MinimumTrailSpeed : 0.35f)
                              && ball.State is BallState.Airborne or BallState.Bouncing;
            if (coreTrail != null)
            {
                coreTrail.emitting = shouldEmit;
            }
            if (accentTrail != null)
            {
                accentTrail.emitting = shouldEmit && accentEnabled;
            }
        }

        public void Begin(ShotPresentationLevel level, ShotSpin spin)
        {
            if (tuning == null)
            {
                return;
            }

            CurrentLevel = level;
            bool perfect = level == ShotPresentationLevel.Perfect;
            TrailPresentationProfile profile = ShotPresentationResolver.ResolveTrail(
                level,
                tuning.NormalTrailTime,
                tuning.NormalTrailWidth,
                tuning.PerfectTrailTime,
                tuning.PerfectTrailWidth);
            CurrentWidth = profile.Width;
            CurrentLifetime = profile.Lifetime;
            Color coreColor = perfect ? tuning.PerfectTrailColor : tuning.NormalTrailColor;
            Color accentColor = ResolveSpinAccent(spin, coreColor);
            accentEnabled = perfect || Mathf.Abs(spin.VerticalSpin) > 0.01f || Mathf.Abs(spin.SideSpin) > 0.01f;

            ConfigureTrail(coreTrail, CurrentLifetime, CurrentWidth, coreColor, 1f);
            ConfigureTrail(accentTrail, CurrentLifetime * 0.7f, CurrentWidth * 0.45f, accentColor, 0.82f);
            coreTrail?.Clear();
            accentTrail?.Clear();
            active = true;
        }

        public void StopAndClear()
        {
            active = false;
            if (coreTrail != null)
            {
                coreTrail.emitting = false;
                coreTrail.Clear();
            }
            if (accentTrail != null)
            {
                accentTrail.emitting = false;
                accentTrail.Clear();
            }
        }

        private static void ConfigureTrail(TrailRenderer trail, float lifetime, float width, Color color, float alpha)
        {
            if (trail == null)
            {
                return;
            }

            trail.time = lifetime;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.startColor = new Color(color.r, color.g, color.b, color.a * alpha);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.minVertexDistance = 0.08f;
        }

        private static Color ResolveSpinAccent(ShotSpin spin, Color fallback)
        {
            if (spin.SideSpin < -0.01f)
            {
                return new Color(0.65f, 0.42f, 1f, 1f);
            }
            if (spin.SideSpin > 0.01f)
            {
                return new Color(1f, 0.42f, 0.88f, 1f);
            }
            if (spin.VerticalSpin > 0.01f)
            {
                return new Color(0.32f, 1f, 0.62f, 1f);
            }
            if (spin.VerticalSpin < -0.01f)
            {
                return new Color(0.35f, 0.7f, 1f, 1f);
            }
            return fallback;
        }
    }
}

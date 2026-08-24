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
        [SerializeField] private TrailRenderer outerTrail;
        [SerializeField] private TrailRenderer coreTrail;
        [SerializeField] private TrailRenderer accentTrail;
        [SerializeField] private ParticleSystem speedStreaks;

        private bool active;
        private bool accentEnabled;
        private bool putterShot;

        public ShotPresentationLevel CurrentLevel { get; private set; }
        public bool IsEmitting => coreTrail != null && coreTrail.emitting;
        public bool IsSpeedStreaking => speedStreaks != null && speedStreaks.isPlaying;
        public bool HasOuterTrail => outerTrail != null;
        public float CurrentWidth { get; private set; }
        public float CurrentCoreWidth { get; private set; }
        public float CurrentLifetime { get; private set; }

        private void Update()
        {
            if (ball != null)
            {
                transform.position = ball.PhysicsPosition;
                Vector3 velocity = ball.Velocity;
                if (velocity.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
                }
            }

            bool shouldEmit = active
                              && !putterShot
                              && ball != null
                              && ball.Speed >= (tuning != null ? tuning.MinimumTrailSpeed : 0.35f)
                              && ball.State is BallState.Airborne or BallState.Bouncing;
            if (outerTrail != null)
            {
                outerTrail.emitting = shouldEmit;
            }
            if (coreTrail != null)
            {
                coreTrail.emitting = shouldEmit;
            }
            if (accentTrail != null)
            {
                accentTrail.emitting = shouldEmit && accentEnabled;
            }

            bool shouldSpeedStreak = shouldEmit
                                     && tuning != null
                                     && ball.Speed >= tuning.SpeedStreakMinimumSpeed;
            SetParticleEmission(speedStreaks, shouldSpeedStreak);
        }

        public void Begin(ShotPresentationLevel level, ShotSpin spin, bool isPutter = false)
        {
            if (tuning == null)
            {
                return;
            }

            CurrentLevel = level;
            putterShot = isPutter;
            TrailPresentationProfile profile = ShotPresentationResolver.ResolveTrail(
                level,
                tuning.NormalTrailTime,
                tuning.NormalTrailWidth,
                tuning.GreatTrailTime,
                tuning.GreatTrailWidth,
                tuning.PerfectTrailTime,
                tuning.PerfectTrailWidth);
            CurrentWidth = profile.Width;
            CurrentCoreWidth = outerTrail != null
                ? CurrentWidth * tuning.TrailCoreWidthMultiplier
                : CurrentWidth;
            CurrentLifetime = profile.Lifetime;
            Color outerColor = ResolveTrailColor(level);
            Color coreColor = Color.Lerp(outerColor, Color.white, 0.78f);
            Color fallbackAccent = level switch
            {
                ShotPresentationLevel.Perfect => tuning.PerfectTrailAccentColor,
                ShotPresentationLevel.Great => tuning.GreatImpactAccentColor,
                _ => outerColor
            };
            Color accentColor = ResolveSpinAccent(spin, fallbackAccent);
            accentEnabled = level is ShotPresentationLevel.Great or ShotPresentationLevel.Perfect
                            || Mathf.Abs(spin.VerticalSpin) > 0.01f
                            || Mathf.Abs(spin.SideSpin) > 0.01f;

            ConfigureTrail(outerTrail, CurrentLifetime, CurrentWidth, outerColor, tuning.TrailOuterAlpha);
            ConfigureTrail(coreTrail, CurrentLifetime * 0.86f, CurrentCoreWidth, coreColor, tuning.TrailCoreAlpha);
            ConfigureTrail(accentTrail, CurrentLifetime * 0.62f,
                CurrentWidth * tuning.TrailSpinWidthMultiplier, accentColor, tuning.TrailSpinAlpha);
            if (outerTrail != null) outerTrail.Clear();
            if (coreTrail != null) coreTrail.Clear();
            if (accentTrail != null) accentTrail.Clear();
            ConfigureSpeedStreakEmission();
            if (speedStreaks != null)
            {
                speedStreaks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            active = !isPutter;
        }

        public void StopAndClear()
        {
            active = false;
            putterShot = false;
            if (outerTrail != null)
            {
                outerTrail.emitting = false;
                outerTrail.Clear();
            }
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
            if (speedStreaks != null)
            {
                speedStreaks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static void ConfigureTrail(TrailRenderer trail, float lifetime, float width, Color color, float alpha)
        {
            if (trail == null)
            {
                return;
            }

            trail.time = lifetime;
            trail.widthMultiplier = width;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f, 0f, -0.55f),
                new Keyframe(0.45f, 0.72f, -0.7f, -0.7f),
                new Keyframe(1f, 0f, -0.9f, 0f));
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.3f), 0f),
                    new GradientColorKey(color, 0.35f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(color.a * alpha, 0f),
                    new GradientAlphaKey(color.a * alpha * 0.72f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;
            trail.minVertexDistance = 0.08f;
        }

        private Color ResolveTrailColor(ShotPresentationLevel level)
        {
            return level switch
            {
                ShotPresentationLevel.Perfect => tuning.PerfectTrailColor,
                ShotPresentationLevel.Great => tuning.GreatTrailColor,
                _ => tuning.NormalTrailColor
            };
        }

        private void ConfigureSpeedStreakEmission()
        {
            if (speedStreaks == null || tuning == null)
            {
                return;
            }
            ParticleSystem.EmissionModule emission = speedStreaks.emission;
            emission.rateOverTime = tuning.SpeedStreakEmissionRate;
        }

        private static void SetParticleEmission(ParticleSystem effect, bool shouldEmit)
        {
            if (effect == null)
            {
                return;
            }
            if (shouldEmit)
            {
                if (!effect.isPlaying) effect.Play();
            }
            else if (effect.isPlaying || effect.particleCount > 0)
            {
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
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

using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Presentation;
using UnityEngine;

namespace SwingPop.VfxSystem
{
    public sealed class LandingVfxController : MonoBehaviour
    {
        [SerializeField] private ShotPresentationTuningData tuning;
        [SerializeField] private ParticleSystem groundBurst;
        [SerializeField] private ParticleSystem waterSplash;

        public int PlayCount { get; private set; }
        public LandingEffectType LastEffect { get; private set; }
        public float LastIntensity { get; private set; }
        public int ActiveParticleCount => Count(groundBurst) + Count(waterSplash);

        public bool PlayContact(BallSurfaceContact contact)
        {
            if (tuning == null || contact.ImpactSpeed < tuning.MinimumLandingSpeed)
            {
                return false;
            }

            float intensity;
            if (contact.IsFirstLanding)
            {
                intensity = 1f;
            }
            else if (contact.Sequence == 2 && contact.ImpactSpeed >= tuning.MinimumSecondaryBounceSpeed)
            {
                intensity = tuning.SecondaryBounceIntensity;
            }
            else
            {
                return false;
            }

            LandingEffectType effect = ShotPresentationResolver.ResolveLanding(contact.SurfaceType);
            return Play(contact.Position, effect, intensity);
        }

        public bool PlayHazard(Vector3 position, TerrainSurfaceType hazard)
        {
            LandingEffectType effect = ShotPresentationResolver.ResolveLanding(hazard);
            return Play(position, effect, 1f);
        }

        public bool Play(Vector3 position, LandingEffectType effect, float intensity)
        {
            if (tuning == null || effect == LandingEffectType.OutOfBounds)
            {
                LastEffect = effect;
                LastIntensity = 0f;
                return false;
            }

            PlayCount++;
            LastEffect = effect;
            LastIntensity = Mathf.Clamp01(intensity);
            transform.position = position + Vector3.up * 0.03f;

            ParticleSystem selected = effect == LandingEffectType.Water ? waterSplash : groundBurst;
            ParticleSystem other = selected == waterSplash ? groundBurst : waterSplash;
            other?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Color color = ResolveColor(effect);
            int count = Mathf.Max(1, Mathf.RoundToInt(ResolveCount(effect) * LastIntensity));
            selected.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = selected.main;
            main.startColor = color;
            main.startSize = Mathf.Lerp(0.05f, effect == LandingEffectType.Water ? 0.22f : 0.14f, LastIntensity);
            selected.Emit(count);
            return true;
        }

        private Color ResolveColor(LandingEffectType effect)
        {
            return effect switch
            {
                LandingEffectType.Rough => tuning.RoughColor,
                LandingEffectType.Sand => tuning.SandColor,
                LandingEffectType.Water => tuning.WaterColor,
                _ => tuning.GrassColor
            };
        }

        private int ResolveCount(LandingEffectType effect)
        {
            return effect switch
            {
                LandingEffectType.Rough => tuning.RoughParticleCount,
                LandingEffectType.Sand => tuning.SandParticleCount,
                LandingEffectType.Water => tuning.WaterParticleCount,
                _ => tuning.GrassParticleCount
            };
        }

        private static int Count(ParticleSystem effect)
        {
            return effect != null ? effect.particleCount : 0;
        }
    }
}

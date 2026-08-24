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
        [SerializeField] private ParticleSystem groundRing;
        [SerializeField] private ParticleSystem waterSplash;
        [SerializeField] private ParticleSystem waterRing;
        [SerializeField] private ParticleSystem waterDroplets;

        public int PlayCount { get; private set; }
        public LandingEffectType LastEffect { get; private set; }
        public float LastIntensity { get; private set; }
        public int LayerCount => CountReference(groundBurst) + CountReference(groundRing)
                                 + CountReference(waterSplash) + CountReference(waterRing)
                                 + CountReference(waterDroplets);
        public int ActiveParticleCount => Count(groundBurst) + Count(groundRing) + Count(waterSplash)
                                          + Count(waterRing) + Count(waterDroplets);

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

            Color color = ResolveColor(effect);
            int count = Mathf.Max(1, Mathf.RoundToInt(ResolveCount(effect) * LastIntensity));
            float size = ResolveSize(effect) * Mathf.Lerp(0.45f, 1f, LastIntensity);
            if (effect == LandingEffectType.Water)
            {
                StopGroundLayers();
                ConfigureAndEmit(waterSplash, color, size, count);
                ConfigureAndEmit(waterRing, Color.Lerp(color, Color.white, 0.5f), size * 4.2f, 1);
                ConfigureAndEmit(waterDroplets, Color.Lerp(color, Color.white, 0.72f), size * 0.42f,
                    Mathf.Max(4, Mathf.RoundToInt(count * 0.42f)));
            }
            else
            {
                StopWaterLayers();
                ConfigureAndEmit(groundBurst, color, size, count);
                ConfigureAndEmit(groundRing, Color.Lerp(color, Color.white, 0.28f), size * 3.2f,
                    LastIntensity >= 0.95f ? 1 : 0);
            }
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

        private float ResolveSize(LandingEffectType effect)
        {
            return effect switch
            {
                LandingEffectType.Rough => tuning.RoughLandingSize,
                LandingEffectType.Sand => tuning.SandLandingSize,
                LandingEffectType.Water => tuning.WaterLandingSize,
                _ => tuning.GrassLandingSize
            };
        }

        private static void ConfigureAndEmit(ParticleSystem effect, Color color, float size, int count)
        {
            if (effect == null)
            {
                return;
            }
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (count <= 0)
            {
                return;
            }
            ParticleSystem.MainModule main = effect.main;
            main.startColor = color;
            main.startSize = Mathf.Max(0.01f, size);
            effect.Emit(count);
        }

        private void StopGroundLayers()
        {
            if (groundBurst != null) groundBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (groundRing != null) groundRing.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void StopWaterLayers()
        {
            if (waterSplash != null) waterSplash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (waterRing != null) waterRing.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (waterDroplets != null) waterDroplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static int Count(ParticleSystem effect)
        {
            return effect != null ? effect.particleCount : 0;
        }

        private static int CountReference(ParticleSystem effect)
        {
            return effect != null ? 1 : 0;
        }
    }
}

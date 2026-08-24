using SwingPop.Data;
using SwingPop.Presentation;
using UnityEngine;
using UnityEngine.Serialization;

namespace SwingPop.VfxSystem
{
    public sealed class ImpactVfxController : MonoBehaviour
    {
        [SerializeField] private ShotPresentationTuningData tuning;
        [FormerlySerializedAs("flash")]
        [SerializeField] private ParticleSystem coreFlash;
        [SerializeField] private ParticleSystem radialRing;
        [SerializeField] private ParticleSystem radialBurst;
        [SerializeField] private ParticleSystem directionalStreak;
        [SerializeField] private ParticleSystem accentSparkles;

        public int PlayCount { get; private set; }
        public ShotPresentationLevel LastLevel { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public bool LastWasPutter { get; private set; }
        public int LayerCount => CountReference(coreFlash) + CountReference(radialRing) + CountReference(radialBurst)
                                 + CountReference(directionalStreak) + CountReference(accentSparkles);
        public int ActiveParticleCount => ParticleCount(coreFlash) + ParticleCount(radialRing)
                                          + ParticleCount(radialBurst) + ParticleCount(directionalStreak)
                                          + ParticleCount(accentSparkles);

        public void Play(Vector3 position, Vector3 direction, ShotPresentationLevel level, bool isPutter = false)
        {
            if (tuning == null)
            {
                return;
            }

            PlayCount++;
            LastLevel = level;
            LastPosition = position;
            LastWasPutter = isPutter;
            transform.SetPositionAndRotation(
                position,
                direction.sqrMagnitude > Mathf.Epsilon
                    ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                    : Quaternion.identity);

            Color color = ResolveCoreColor(level);
            Color accent = ResolveAccentColor(level);
            float scale = ResolveScale(level);
            int count = ResolveParticleCount(level);
            if (isPutter)
            {
                scale *= tuning.PutterImpactScaleMultiplier;
                count = Mathf.Max(1, Mathf.RoundToInt(count * tuning.PutterImpactParticleMultiplier));
            }

            ConfigureAndEmit(coreFlash, Color.Lerp(color, Color.white, 0.72f),
                scale * tuning.CoreFlashSizeMultiplier, 1);
            ConfigureAndEmit(radialRing, accent, scale * tuning.RadialRingSizeMultiplier, isPutter ? 0 : 1);
            ConfigureAndEmit(radialBurst, color, scale * tuning.RadialBurstSizeMultiplier,
                isPutter ? 1 : count);
            ConfigureAndEmit(directionalStreak, Color.Lerp(color, Color.white, 0.4f),
                scale * tuning.DirectionalStreakSizeMultiplier,
                isPutter ? 1 : Mathf.RoundToInt(count * tuning.DirectionalParticleRatio));
            ConfigureAndEmit(accentSparkles, accent, scale * tuning.AccentSparkSizeMultiplier,
                isPutter ? 1 : Mathf.RoundToInt(count * tuning.AccentParticleRatio));
        }

        public void Preview(ShotPresentationLevel level, bool isPutter = false)
        {
            Play(transform.position, Vector3.forward, level, isPutter);
        }

        private static void ConfigureAndEmit(ParticleSystem effect, Color color, float size, int count)
        {
            if (effect == null)
            {
                return;
            }

            if (count <= 0)
            {
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }

            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = effect.main;
            main.startColor = color;
            main.startSize = Mathf.Max(0.01f, size);
            effect.Emit(Mathf.Max(1, count));
        }

        private Color ResolveCoreColor(ShotPresentationLevel level)
        {
            return level switch
            {
                ShotPresentationLevel.Perfect => tuning.PerfectImpactColor,
                ShotPresentationLevel.Great => tuning.GreatImpactColor,
                _ => tuning.NormalImpactColor
            };
        }

        private Color ResolveAccentColor(ShotPresentationLevel level)
        {
            return level switch
            {
                ShotPresentationLevel.Perfect => tuning.PerfectImpactAccentColor,
                ShotPresentationLevel.Great => tuning.GreatImpactAccentColor,
                _ => tuning.NormalImpactAccentColor
            };
        }

        private float ResolveScale(ShotPresentationLevel level)
        {
            return level switch
            {
                ShotPresentationLevel.Perfect => tuning.PerfectImpactScale,
                ShotPresentationLevel.Great => tuning.GreatImpactScale,
                _ => tuning.NormalImpactScale
            };
        }

        private int ResolveParticleCount(ShotPresentationLevel level)
        {
            return level switch
            {
                ShotPresentationLevel.Perfect => tuning.PerfectImpactParticles,
                ShotPresentationLevel.Great => tuning.GreatImpactParticles,
                _ => tuning.NormalImpactParticles
            };
        }

        private static int ParticleCount(ParticleSystem effect)
        {
            return effect != null ? effect.particleCount : 0;
        }

        private static int CountReference(ParticleSystem effect)
        {
            return effect != null ? 1 : 0;
        }
    }
}

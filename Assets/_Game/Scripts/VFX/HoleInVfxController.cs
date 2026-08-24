using SwingPop.Data;
using SwingPop.Presentation;
using UnityEngine;

namespace SwingPop.VfxSystem
{
    public sealed class HoleInVfxController : MonoBehaviour
    {
        [SerializeField] private ShotPresentationTuningData tuning;
        [SerializeField] private ParticleSystem cupFlash;
        [SerializeField] private ParticleSystem upwardSparkles;
        [SerializeField] private ParticleSystem ringBurst;
        [SerializeField] private ParticleSystem celebrationSparkles;

        public int PlayCount { get; private set; }
        public CelebrationPresentationLevel LastLevel { get; private set; }
        public int LayerCount => CountReference(cupFlash) + CountReference(upwardSparkles)
                                 + CountReference(ringBurst) + CountReference(celebrationSparkles);
        public int ActiveParticleCount => Count(cupFlash) + Count(upwardSparkles) + Count(ringBurst)
                                          + Count(celebrationSparkles);

        public void Play(Vector3 cupPosition, CelebrationPresentationLevel level)
        {
            if (tuning == null)
            {
                return;
            }

            PlayCount++;
            LastLevel = level;
            transform.position = cupPosition + Vector3.up * 0.08f;
            float scale = tuning.GetCelebrationScale(level);
            int sparkleCount = Mathf.Max(1, Mathf.RoundToInt(tuning.HoleSparkleCount * scale));
            ConfigureAndEmit(cupFlash, new Color(0.82f, 1f, 1f, 1f), 0.42f * scale, 1);
            ConfigureAndEmit(upwardSparkles, new Color(0.35f, 1f, 0.85f, 1f), 0.1f * scale, sparkleCount);
            ConfigureAndEmit(ringBurst, new Color(1f, 0.84f, 0.2f, 1f), 0.08f * scale, Mathf.Max(6, sparkleCount / 2));
            ConfigureAndEmit(celebrationSparkles, new Color(0.86f, 1f, 1f, 1f), 0.07f * scale,
                Mathf.Max(4, sparkleCount / 2));
        }

        private static void ConfigureAndEmit(ParticleSystem effect, Color color, float size, int count)
        {
            if (effect == null)
            {
                return;
            }
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = effect.main;
            main.startColor = color;
            main.startSize = size;
            effect.Emit(count);
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

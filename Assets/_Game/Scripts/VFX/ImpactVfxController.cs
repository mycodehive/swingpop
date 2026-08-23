using SwingPop.Data;
using SwingPop.Presentation;
using UnityEngine;

namespace SwingPop.VfxSystem
{
    public sealed class ImpactVfxController : MonoBehaviour
    {
        [SerializeField] private ShotPresentationTuningData tuning;
        [SerializeField] private ParticleSystem flash;
        [SerializeField] private ParticleSystem radialBurst;
        [SerializeField] private ParticleSystem directionalStreak;

        public int PlayCount { get; private set; }
        public ShotPresentationLevel LastLevel { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public int ActiveParticleCount => ParticleCount(flash) + ParticleCount(radialBurst) + ParticleCount(directionalStreak);

        public void Play(Vector3 position, Vector3 direction, ShotPresentationLevel level)
        {
            if (tuning == null)
            {
                return;
            }

            PlayCount++;
            LastLevel = level;
            LastPosition = position;
            transform.SetPositionAndRotation(
                position,
                direction.sqrMagnitude > Mathf.Epsilon
                    ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                    : Quaternion.identity);

            bool perfect = level == ShotPresentationLevel.Perfect;
            Color color = perfect ? tuning.PerfectImpactColor : tuning.NormalImpactColor;
            float scale = perfect ? tuning.PerfectImpactScale : tuning.NormalImpactScale;
            int count = perfect ? tuning.PerfectImpactParticles : tuning.NormalImpactParticles;

            ConfigureAndEmit(flash, color, scale * 0.55f, 1);
            ConfigureAndEmit(radialBurst, color, scale * 0.11f, count);
            ConfigureAndEmit(directionalStreak, Color.Lerp(color, Color.white, 0.35f), scale * 0.075f, perfect ? count / 2 : 4);
        }

        public void Preview(ShotPresentationLevel level)
        {
            Play(transform.position, Vector3.forward, level);
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
            main.startSize = Mathf.Max(0.01f, size);
            effect.Emit(Mathf.Max(1, count));
        }

        private static int ParticleCount(ParticleSystem effect)
        {
            return effect != null ? effect.particleCount : 0;
        }
    }
}

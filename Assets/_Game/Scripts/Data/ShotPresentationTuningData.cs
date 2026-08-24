using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "ShotPresentationTuning", menuName = "SwingPop/Shot Presentation Tuning")]
    public sealed class ShotPresentationTuningData : ScriptableObject
    {
        [Header("Impact VFX")]
        [SerializeField] private Color normalImpactColor = new(0.35f, 0.95f, 1f, 1f);
        [SerializeField] private Color greatImpactColor = new(0.55f, 1f, 0.92f, 1f);
        [SerializeField] private Color perfectImpactColor = new(0.82f, 1f, 1f, 1f);
        [SerializeField] private Color normalImpactAccentColor = new(0.3f, 0.82f, 1f, 1f);
        [SerializeField] private Color greatImpactAccentColor = new(0.92f, 0.88f, 0.42f, 1f);
        [SerializeField] private Color perfectImpactAccentColor = new(1f, 0.78f, 0.18f, 1f);
        [SerializeField, Min(0.01f)] private float normalImpactScale = 0.7f;
        [SerializeField, Min(0.01f)] private float greatImpactScale = 0.98f;
        [SerializeField, Min(0.01f)] private float perfectImpactScale = 1.25f;
        [SerializeField, Min(1)] private int normalImpactParticles = 10;
        [SerializeField, Min(1)] private int greatImpactParticles = 16;
        [SerializeField, Min(1)] private int perfectImpactParticles = 22;
        [SerializeField, Range(0.05f, 1f)] private float putterImpactScaleMultiplier = 0.32f;
        [SerializeField, Range(0.05f, 1f)] private float putterImpactParticleMultiplier = 0.25f;

        [Header("Impact Layers")]
        [SerializeField, Min(0.01f)] private float coreFlashSizeMultiplier = 0.58f;
        [SerializeField, Min(0.01f)] private float radialRingSizeMultiplier = 1.05f;
        [SerializeField, Min(0.01f)] private float radialBurstSizeMultiplier = 0.1f;
        [SerializeField, Min(0.01f)] private float directionalStreakSizeMultiplier = 0.085f;
        [SerializeField, Min(0.01f)] private float accentSparkSizeMultiplier = 0.06f;
        [SerializeField, Range(0.05f, 1f)] private float directionalParticleRatio = 0.45f;
        [SerializeField, Range(0.05f, 1f)] private float accentParticleRatio = 0.35f;

        [Header("Ball Trail")]
        [SerializeField, Min(0.01f)] private float normalTrailTime = 0.42f;
        [SerializeField, Min(0.001f)] private float normalTrailWidth = 0.075f;
        [SerializeField, Min(0.01f)] private float greatTrailTime = 0.56f;
        [SerializeField, Min(0.001f)] private float greatTrailWidth = 0.1f;
        [SerializeField, Min(0.01f)] private float perfectTrailTime = 0.72f;
        [SerializeField, Min(0.001f)] private float perfectTrailWidth = 0.14f;
        [SerializeField] private Color normalTrailColor = new(0.35f, 0.95f, 1f, 0.88f);
        [SerializeField] private Color greatTrailColor = new(0.55f, 1f, 0.9f, 0.92f);
        [SerializeField] private Color perfectTrailColor = new(0.78f, 1f, 1f, 0.96f);
        [SerializeField] private Color perfectTrailAccentColor = new(1f, 0.82f, 0.3f, 0.9f);
        [SerializeField, Range(0.1f, 1f)] private float trailCoreWidthMultiplier = 0.42f;
        [SerializeField, Range(0.1f, 1f)] private float trailSpinWidthMultiplier = 0.24f;
        [SerializeField, Range(0f, 1f)] private float trailOuterAlpha = 0.42f;
        [SerializeField, Range(0f, 1f)] private float trailCoreAlpha = 0.95f;
        [SerializeField, Range(0f, 1f)] private float trailSpinAlpha = 0.55f;
        [SerializeField, Min(0f)] private float minimumTrailSpeed = 0.35f;
        [SerializeField, Min(0f)] private float speedStreakMinimumSpeed = 18f;
        [SerializeField, Min(0f)] private float speedStreakEmissionRate = 14f;

        [Header("Landing VFX")]
        [SerializeField, Min(0f)] private float minimumLandingSpeed = 1.4f;
        [SerializeField, Range(0f, 1f)] private float secondaryBounceIntensity = 0.3f;
        [SerializeField, Min(0f)] private float minimumSecondaryBounceSpeed = 3f;
        [SerializeField, Min(1)] private int grassParticleCount = 8;
        [SerializeField, Min(1)] private int roughParticleCount = 13;
        [SerializeField, Min(1)] private int sandParticleCount = 20;
        [SerializeField, Min(1)] private int waterParticleCount = 28;
        [SerializeField] private Color grassColor = new(0.45f, 0.88f, 0.34f, 1f);
        [SerializeField] private Color roughColor = new(0.28f, 0.62f, 0.2f, 1f);
        [SerializeField] private Color sandColor = new(1f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color waterColor = new(0.25f, 0.78f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float grassLandingSize = 0.13f;
        [SerializeField, Min(0.01f)] private float roughLandingSize = 0.17f;
        [SerializeField, Min(0.01f)] private float sandLandingSize = 0.22f;
        [SerializeField, Min(0.01f)] private float waterLandingSize = 0.24f;

        [Header("Hole In")]
        [SerializeField, Min(1)] private int holeSparkleCount = 18;
        [SerializeField, Min(0.1f)] private float subduedCelebrationScale = 0.75f;
        [SerializeField, Min(0.1f)] private float normalCelebrationScale = 1f;
        [SerializeField, Min(0.1f)] private float strongCelebrationScale = 1.25f;
        [SerializeField, Min(0.1f)] private float strongestCelebrationScale = 1.55f;

        [Header("Audio Mix")]
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float swingVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float impactVolume = 0.72f;
        [SerializeField, Range(0f, 1f)] private float perfectAccentVolume = 0.6f;
        [SerializeField, Range(0f, 2f)] private float greatImpactVolumeMultiplier = 1.08f;
        [SerializeField, Range(0f, 1f)] private float putterImpactVolumeMultiplier = 0.34f;
        [SerializeField, Range(0f, 1f)] private float terrainVolume = 0.48f;
        [SerializeField, Range(0f, 1f)] private float hazardVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float holeVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float resultVolume = 0.6f;

        [Header("Replaceable Audio Clips")]
        [SerializeField] private AudioClip uiConfirmClip;
        [SerializeField] private AudioClip swingClip;
        [SerializeField] private AudioClip puttSwingClip;
        [SerializeField] private AudioClip normalImpactClip;
        [SerializeField] private AudioClip perfectImpactClip;
        [SerializeField] private AudioClip fairwayLandingClip;
        [SerializeField] private AudioClip roughLandingClip;
        [SerializeField] private AudioClip bunkerLandingClip;
        [SerializeField] private AudioClip greenLandingClip;
        [SerializeField] private AudioClip waterHazardClip;
        [SerializeField] private AudioClip outOfBoundsClip;
        [SerializeField] private AudioClip holeInClip;
        [SerializeField] private AudioClip resultClip;

        public Color NormalImpactColor => normalImpactColor;
        public Color GreatImpactColor => greatImpactColor;
        public Color PerfectImpactColor => perfectImpactColor;
        public Color NormalImpactAccentColor => normalImpactAccentColor;
        public Color GreatImpactAccentColor => greatImpactAccentColor;
        public Color PerfectImpactAccentColor => perfectImpactAccentColor;
        public float NormalImpactScale => normalImpactScale;
        public float GreatImpactScale => greatImpactScale;
        public float PerfectImpactScale => perfectImpactScale;
        public int NormalImpactParticles => normalImpactParticles;
        public int GreatImpactParticles => greatImpactParticles;
        public int PerfectImpactParticles => perfectImpactParticles;
        public float PutterImpactScaleMultiplier => putterImpactScaleMultiplier;
        public float PutterImpactParticleMultiplier => putterImpactParticleMultiplier;
        public float CoreFlashSizeMultiplier => coreFlashSizeMultiplier;
        public float RadialRingSizeMultiplier => radialRingSizeMultiplier;
        public float RadialBurstSizeMultiplier => radialBurstSizeMultiplier;
        public float DirectionalStreakSizeMultiplier => directionalStreakSizeMultiplier;
        public float AccentSparkSizeMultiplier => accentSparkSizeMultiplier;
        public float DirectionalParticleRatio => directionalParticleRatio;
        public float AccentParticleRatio => accentParticleRatio;
        public float NormalTrailTime => normalTrailTime;
        public float NormalTrailWidth => normalTrailWidth;
        public float GreatTrailTime => greatTrailTime;
        public float GreatTrailWidth => greatTrailWidth;
        public float PerfectTrailTime => perfectTrailTime;
        public float PerfectTrailWidth => perfectTrailWidth;
        public Color NormalTrailColor => normalTrailColor;
        public Color GreatTrailColor => greatTrailColor;
        public Color PerfectTrailColor => perfectTrailColor;
        public Color PerfectTrailAccentColor => perfectTrailAccentColor;
        public float TrailCoreWidthMultiplier => trailCoreWidthMultiplier;
        public float TrailSpinWidthMultiplier => trailSpinWidthMultiplier;
        public float TrailOuterAlpha => trailOuterAlpha;
        public float TrailCoreAlpha => trailCoreAlpha;
        public float TrailSpinAlpha => trailSpinAlpha;
        public float MinimumTrailSpeed => minimumTrailSpeed;
        public float SpeedStreakMinimumSpeed => speedStreakMinimumSpeed;
        public float SpeedStreakEmissionRate => speedStreakEmissionRate;
        public float MinimumLandingSpeed => minimumLandingSpeed;
        public float SecondaryBounceIntensity => secondaryBounceIntensity;
        public float MinimumSecondaryBounceSpeed => minimumSecondaryBounceSpeed;
        public int GrassParticleCount => grassParticleCount;
        public int RoughParticleCount => roughParticleCount;
        public int SandParticleCount => sandParticleCount;
        public int WaterParticleCount => waterParticleCount;
        public Color GrassColor => grassColor;
        public Color RoughColor => roughColor;
        public Color SandColor => sandColor;
        public Color WaterColor => waterColor;
        public float GrassLandingSize => grassLandingSize;
        public float RoughLandingSize => roughLandingSize;
        public float SandLandingSize => sandLandingSize;
        public float WaterLandingSize => waterLandingSize;
        public int HoleSparkleCount => holeSparkleCount;
        public float UiVolume => uiVolume;
        public float SwingVolume => swingVolume;
        public float ImpactVolume => impactVolume;
        public float PerfectAccentVolume => perfectAccentVolume;
        public float GreatImpactVolumeMultiplier => greatImpactVolumeMultiplier;
        public float PutterImpactVolumeMultiplier => putterImpactVolumeMultiplier;
        public float TerrainVolume => terrainVolume;
        public float HazardVolume => hazardVolume;
        public float HoleVolume => holeVolume;
        public float ResultVolume => resultVolume;
        public AudioClip UiConfirmClip => uiConfirmClip;
        public AudioClip SwingClip => swingClip;
        public AudioClip PuttSwingClip => puttSwingClip;
        public AudioClip NormalImpactClip => normalImpactClip;
        public AudioClip PerfectImpactClip => perfectImpactClip;
        public AudioClip FairwayLandingClip => fairwayLandingClip;
        public AudioClip RoughLandingClip => roughLandingClip;
        public AudioClip BunkerLandingClip => bunkerLandingClip;
        public AudioClip GreenLandingClip => greenLandingClip;
        public AudioClip WaterHazardClip => waterHazardClip;
        public AudioClip OutOfBoundsClip => outOfBoundsClip;
        public AudioClip HoleInClip => holeInClip;
        public AudioClip ResultClip => resultClip;

        public float GetCelebrationScale(Presentation.CelebrationPresentationLevel level)
        {
            return level switch
            {
                Presentation.CelebrationPresentationLevel.Subdued => subduedCelebrationScale,
                Presentation.CelebrationPresentationLevel.Strong => strongCelebrationScale,
                Presentation.CelebrationPresentationLevel.Strongest => strongestCelebrationScale,
                _ => normalCelebrationScale
            };
        }
    }
}

using NUnit.Framework;
using SwingPop.AudioSystem;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using UnityEngine;

namespace SwingPop.Tests
{
    public sealed class ShotPresentationResolverTests
    {
        [TestCase(ImpactGrade.Perfect, ShotPresentationLevel.Perfect)]
        [TestCase(ImpactGrade.Great, ShotPresentationLevel.Normal)]
        [TestCase(ImpactGrade.Good, ShotPresentationLevel.Normal)]
        [TestCase(ImpactGrade.Miss, ShotPresentationLevel.Normal)]
        public void ResolveImpact_OnlyPerfectUsesPerfectPresentation(
            ImpactGrade grade,
            ShotPresentationLevel expected)
        {
            Assert.That(ShotPresentationResolver.ResolveImpact(grade), Is.EqualTo(expected));
        }

        [TestCase(TerrainSurfaceType.Fairway, LandingEffectType.Grass)]
        [TestCase(TerrainSurfaceType.Green, LandingEffectType.Grass)]
        [TestCase(TerrainSurfaceType.Rough, LandingEffectType.Rough)]
        [TestCase(TerrainSurfaceType.Bunker, LandingEffectType.Sand)]
        [TestCase(TerrainSurfaceType.Water, LandingEffectType.Water)]
        [TestCase(TerrainSurfaceType.OutOfBounds, LandingEffectType.OutOfBounds)]
        public void ResolveLanding_MapsTerrainToPresentation(
            TerrainSurfaceType surface,
            LandingEffectType expected)
        {
            Assert.That(ShotPresentationResolver.ResolveLanding(surface), Is.EqualTo(expected));
        }

        [TestCase(-3, CelebrationPresentationLevel.Strongest)]
        [TestCase(-1, CelebrationPresentationLevel.Strong)]
        [TestCase(0, CelebrationPresentationLevel.Normal)]
        [TestCase(2, CelebrationPresentationLevel.Subdued)]
        public void ResolveCelebration_UsesRelativeScore(int relativeToPar, CelebrationPresentationLevel expected)
        {
            ScoreResult result = new(3, 4, relativeToPar, "TEST");
            Assert.That(ShotPresentationResolver.ResolveCelebration(result), Is.EqualTo(expected));
        }

        [Test]
        public void ResolveTrail_PerfectProfileIsDistinctAndStronger()
        {
            TrailPresentationProfile normal = ShotPresentationResolver.ResolveTrail(
                ShotPresentationLevel.Normal, 0.4f, 0.07f, 0.7f, 0.14f);
            TrailPresentationProfile perfect = ShotPresentationResolver.ResolveTrail(
                ShotPresentationLevel.Perfect, 0.4f, 0.07f, 0.7f, 0.14f);

            Assert.That(perfect.Lifetime, Is.GreaterThan(normal.Lifetime));
            Assert.That(perfect.Width, Is.GreaterThan(normal.Width));
        }

        [TestCase(TerrainSurfaceType.Fairway, GameplayAudioCue.FairwayLanding)]
        [TestCase(TerrainSurfaceType.Rough, GameplayAudioCue.RoughLanding)]
        [TestCase(TerrainSurfaceType.Bunker, GameplayAudioCue.BunkerLanding)]
        [TestCase(TerrainSurfaceType.Green, GameplayAudioCue.GreenLanding)]
        public void ResolveLandingAudio_MapsSurfaceCue(TerrainSurfaceType surface, GameplayAudioCue expected)
        {
            Assert.That(ShotPresentationResolver.ResolveLandingAudio(surface), Is.EqualTo(expected));
        }

        [TestCase(TerrainSurfaceType.Water, GameplayAudioCue.WaterHazard)]
        [TestCase(TerrainSurfaceType.OutOfBounds, GameplayAudioCue.OutOfBounds)]
        public void ResolveHazardAudio_MapsHazardCue(TerrainSurfaceType hazard, GameplayAudioCue expected)
        {
            Assert.That(ShotPresentationResolver.ResolveHazardAudio(hazard), Is.EqualTo(expected));
        }

        [Test]
        public void ImpactGate_ConsumesOneLaunchPerCommittedShot()
        {
            ImpactPresentationGate gate = new();

            gate.Arm();

            Assert.That(gate.TryConsume(), Is.True);
            Assert.That(gate.TryConsume(), Is.False);
        }

        [Test]
        public void SurfaceContact_ClampsUnsafeTelemetry()
        {
            BallSurfaceContact contact = new(Vector3.one, TerrainSurfaceType.Bunker, -4f, 0, true);

            Assert.That(contact.ImpactSpeed, Is.Zero);
            Assert.That(contact.Sequence, Is.EqualTo(1));
            Assert.That(contact.SurfaceType, Is.EqualTo(TerrainSurfaceType.Bunker));
        }
    }
}

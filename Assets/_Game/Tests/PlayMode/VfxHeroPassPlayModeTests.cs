using System.Collections;
using NUnit.Framework;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using SwingPop.VfxSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class VfxHeroPassPlayModeTests
    {
        [UnityTest]
        public IEnumerator Hole01_HeroVfx_ProfilesLayersCleanupAndOneShotGateRemainStable()
        {
            yield return SceneManager.LoadSceneAsync("Hole01_SkyIsland", LoadSceneMode.Single);
            yield return null;
            yield return null;

            ShotPresentationController presentation = Object.FindAnyObjectByType<ShotPresentationController>();
            ImpactVfxController impact = Object.FindAnyObjectByType<ImpactVfxController>();
            BallTrailController trail = Object.FindAnyObjectByType<BallTrailController>();
            LandingVfxController landing = Object.FindAnyObjectByType<LandingVfxController>();
            HoleInVfxController holeIn = Object.FindAnyObjectByType<HoleInVfxController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();

            Assert.That(presentation, Is.Not.Null);
            Assert.That(impact, Is.Not.Null);
            Assert.That(trail, Is.Not.Null);
            Assert.That(landing, Is.Not.Null);
            Assert.That(holeIn, Is.Not.Null);
            Assert.That(ball, Is.Not.Null);
            Assert.That(holeFlow, Is.Not.Null);
            Assert.That(shotFlow, Is.Not.Null);
            Assert.That(impact.LayerCount, Is.EqualTo(5));
            Assert.That(landing.LayerCount, Is.EqualTo(5));
            Assert.That(holeIn.LayerCount, Is.EqualTo(4));
            Assert.That(trail.HasOuterTrail, Is.True);

            int particleObjectsBefore = Object.FindObjectsByType<ParticleSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int trailObjectsBefore = Object.FindObjectsByType<TrailRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            Vector3 position = ball.PhysicsPosition;
            impact.Play(position, Vector3.forward, ShotPresentationLevel.Normal);
            int normalParticles = impact.ActiveParticleCount;
            impact.Play(position, Vector3.forward, ShotPresentationLevel.Great);
            int greatParticles = impact.ActiveParticleCount;
            impact.Play(position, Vector3.forward, ShotPresentationLevel.Perfect);
            int perfectParticles = impact.ActiveParticleCount;
            impact.Play(position, Vector3.forward, ShotPresentationLevel.Perfect, true);
            int putterParticles = impact.ActiveParticleCount;

            Assert.That(normalParticles, Is.GreaterThan(0));
            Assert.That(greatParticles, Is.GreaterThan(normalParticles));
            Assert.That(perfectParticles, Is.GreaterThan(greatParticles));
            Assert.That(putterParticles, Is.LessThan(normalParticles));
            Assert.That(impact.LastWasPutter, Is.True);

            trail.Begin(ShotPresentationLevel.Normal, ShotSpin.None);
            float normalWidth = trail.CurrentWidth;
            float normalLifetime = trail.CurrentLifetime;
            trail.Begin(ShotPresentationLevel.Great, ShotSpin.None);
            float greatWidth = trail.CurrentWidth;
            float greatLifetime = trail.CurrentLifetime;
            trail.Begin(ShotPresentationLevel.Perfect, ShotSpin.None);
            float perfectWidth = trail.CurrentWidth;
            float perfectLifetime = trail.CurrentLifetime;
            Assert.That(greatWidth, Is.GreaterThan(normalWidth));
            Assert.That(perfectWidth, Is.GreaterThan(greatWidth));
            Assert.That(greatLifetime, Is.GreaterThan(normalLifetime));
            Assert.That(perfectLifetime, Is.GreaterThan(greatLifetime));
            trail.StopAndClear();
            yield return null;
            Assert.That(trail.IsEmitting, Is.False);
            Assert.That(trail.IsSpeedStreaking, Is.False);

            Assert.That(landing.Play(position, LandingEffectType.Grass, 1f), Is.True);
            Assert.That(landing.Play(position, LandingEffectType.Rough, 1f), Is.True);
            Assert.That(landing.Play(position, LandingEffectType.Sand, 1f), Is.True);
            Assert.That(landing.LastEffect, Is.EqualTo(LandingEffectType.Sand));
            Assert.That(landing.ActiveParticleCount, Is.GreaterThan(0));
            Assert.That(landing.Play(position, LandingEffectType.Water, 1f), Is.True);
            Assert.That(landing.LastEffect, Is.EqualTo(LandingEffectType.Water));
            Assert.That(landing.ActiveParticleCount, Is.GreaterThan(0));

            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            int holeBefore = holeIn.PlayCount;
            Assert.That(holeFlow.TryCompleteHole(ball), Is.True);
            Assert.That(holeFlow.TryCompleteHole(ball), Is.False);
            yield return null;
            Assert.That(holeIn.PlayCount, Is.EqualTo(holeBefore + 1));
            Assert.That(presentation.HolePresentationCount, Is.EqualTo(holeBefore + 1));

            Assert.That(Object.FindObjectsByType<ParticleSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(particleObjectsBefore));
            Assert.That(Object.FindObjectsByType<TrailRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(trailObjectsBefore));

            holeFlow.SetAutomaticFlowSuspended(false);
            holeFlow.DebugResetHole();
        }
    }
}

using System.Collections;
using NUnit.Framework;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using SwingPop.UI;
using SwingPop.VfxSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class M9ShotFeelPlayModeTests
    {
        [UnityTest]
        public IEnumerator Foundation_NormalAndPerfectShotFeel_UsesOneReusablePresentationGraph()
        {
            yield return SceneManager.LoadSceneAsync("Foundation", LoadSceneMode.Single);
            yield return null;

            ShotPresentationController presentation = Object.FindAnyObjectByType<ShotPresentationController>();
            GameplayAudioController audio = Object.FindAnyObjectByType<GameplayAudioController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            CameraDirector cameraDirector = Object.FindAnyObjectByType<CameraDirector>();
            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            ImpactVfxController impact = Object.FindAnyObjectByType<ImpactVfxController>();
            BallTrailController trail = Object.FindAnyObjectByType<BallTrailController>();
            LandingVfxController landing = Object.FindAnyObjectByType<LandingVfxController>();
            HoleInVfxController holeIn = Object.FindAnyObjectByType<HoleInVfxController>();
            ShotInputController input = Object.FindAnyObjectByType<ShotInputController>();

            Assert.That(presentation, Is.Not.Null);
            Assert.That(audio, Is.Not.Null);
            Assert.That(shotFlow, Is.Not.Null);
            Assert.That(ball, Is.Not.Null);
            Assert.That(holeFlow, Is.Not.Null);
            Assert.That(cameraDirector, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(impact, Is.Not.Null);
            Assert.That(trail, Is.Not.Null);
            Assert.That(landing, Is.Not.Null);
            Assert.That(holeIn, Is.Not.Null);

            if (input != null)
            {
                input.enabled = false;
            }
            holeFlow.DebugResetHole();
            cameraDirector.SkipIntro();
            yield return null;

            int reusableCount = presentation.ReusableEffectObjectCount;
            int particleCount = Object.FindObjectsByType<ParticleSystem>().Length;
            int trailCount = Object.FindObjectsByType<TrailRenderer>().Length;

            Assert.That(shotFlow.TryCommitShot(0.55f, 0.5f), Is.True);
            yield return WaitUntilOrFail(() => ball.State != BallState.Ready, 3f, "Normal launch");
            yield return null;

            Assert.That(presentation.ImpactPresentationCount, Is.EqualTo(1));
            Assert.That(presentation.LastImpactLevel, Is.EqualTo(ShotPresentationLevel.Normal));
            Assert.That(impact.LastLevel, Is.EqualTo(ShotPresentationLevel.Normal));
            Assert.That(audio.GetCueCount(GameplayAudioCue.NormalImpact), Is.EqualTo(1));
            Assert.That(audio.GetCueCount(GameplayAudioCue.PerfectImpact), Is.Zero);
            Assert.That(hud.View.ImpactMessage, Is.Not.EqualTo("PERFECT"));
            float normalWidth = trail.CurrentWidth;
            float normalLifetime = trail.CurrentLifetime;

            holeFlow.DebugResetHole();
            yield return null;
            Assert.That(shotFlow.TryCommitShot(0.68f, 0f), Is.True);
            yield return WaitUntilOrFail(() => presentation.ImpactPresentationCount == 2, 3f, "Perfect launch");
            yield return null;

            Assert.That(presentation.LastImpactLevel, Is.EqualTo(ShotPresentationLevel.Perfect));
            Assert.That(impact.LastLevel, Is.EqualTo(ShotPresentationLevel.Perfect));
            Assert.That(audio.GetCueCount(GameplayAudioCue.NormalImpact), Is.EqualTo(2));
            Assert.That(audio.GetCueCount(GameplayAudioCue.PerfectImpact), Is.EqualTo(1));
            Assert.That(trail.CurrentWidth, Is.GreaterThan(normalWidth));
            Assert.That(trail.CurrentLifetime, Is.GreaterThan(normalLifetime));
            Assert.That(hud.View.ImpactMessage, Is.EqualTo("PERFECT"));
            Assert.That(cameraDirector.CurrentMode, Is.EqualTo(CameraMode.Impact));

            yield return new WaitForSeconds(0.12f);
            Assert.That(presentation.ImpactPresentationCount, Is.EqualTo(2), "Impact must fire once per shot.");

            int landingBefore = landing.PlayCount;
            Assert.That(landing.Play(ball.PhysicsPosition, LandingEffectType.Grass, 1f), Is.True);
            Assert.That(landing.Play(ball.PhysicsPosition, LandingEffectType.Rough, 1f), Is.True);
            Assert.That(landing.Play(ball.PhysicsPosition, LandingEffectType.Sand, 1f), Is.True);
            Assert.That(landing.Play(ball.PhysicsPosition, LandingEffectType.Water, 1f), Is.True);
            Assert.That(landing.PlayCount, Is.EqualTo(landingBefore + 4));
            Assert.That(landing.LastEffect, Is.EqualTo(LandingEffectType.Water));

            audio.Preview(GameplayAudioCue.BunkerLanding);
            Assert.That(audio.GetCueCount(GameplayAudioCue.BunkerLanding), Is.EqualTo(1));
            Assert.That(audio.GeneratedFallbackCount, Is.GreaterThan(0));

            TerrainSurface water = FindSurface(TerrainSurfaceType.Water);
            TerrainSurface fairway = FindSurface(TerrainSurfaceType.Fairway);
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green);
            ClubData driver = FindClub(false);
            ClubData putter = FindClub(true);
            Assert.That(water, Is.Not.Null);
            Assert.That(fairway, Is.Not.Null);
            Assert.That(green, Is.Not.Null);
            Assert.That(driver, Is.Not.Null);
            Assert.That(putter, Is.Not.Null);

            holeFlow.DebugResetHole();
            yield return null;
            Bounds waterBounds = water.GetComponent<Collider>().bounds;
            Vector3 hazardStart = new(waterBounds.center.x, 0.2f, waterBounds.min.z - 1.5f);
            ball.PrepareNextShot(hazardStart, fairway.Data);
            shotFlow.PrepareNextShot(Vector3.forward, driver);
            int waterAudioBefore = audio.GetCueCount(GameplayAudioCue.WaterHazard);
            Assert.That(shotFlow.TryCommitShot(0.65f, 0f), Is.True);
            yield return WaitUntilOrFail(() => ball.State == BallState.Ready && ball.HasLastHazard, 12f, "Water hazard recovery");
            Assert.That(landing.LastEffect, Is.EqualTo(LandingEffectType.Water));
            Assert.That(audio.GetCueCount(GameplayAudioCue.WaterHazard), Is.EqualTo(waterAudioBefore + 1));

            holeFlow.SetAutomaticFlowSuspended(true);
            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 puttStart = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(puttStart, green.Data);
            shotFlow.PrepareNextShot(cup - puttStart, putter);
            int holeVfxBefore = holeIn.PlayCount;
            int holeAudioBefore = audio.GetCueCount(GameplayAudioCue.HoleIn);
            Assert.That(shotFlow.TryCommitShot(0.45f, 0f), Is.True);
            yield return WaitUntilOrFail(() => holeFlow.State == HoleFlowState.HoleComplete, 12f, "Hole-In flow");
            yield return null;
            Assert.That(holeIn.PlayCount, Is.EqualTo(holeVfxBefore + 1));
            Assert.That(audio.GetCueCount(GameplayAudioCue.HoleIn), Is.EqualTo(holeAudioBefore + 1));
            Assert.That(audio.GetCueCount(GameplayAudioCue.Result), Is.EqualTo(1));
            Assert.That(hud.View.ResultView.IsVisible, Is.True);
            Assert.That(cameraDirector.CurrentMode, Is.EqualTo(CameraMode.HoleComplete).Or.EqualTo(CameraMode.Result));

            Assert.That(presentation.ReusableEffectObjectCount, Is.EqualTo(reusableCount));
            Assert.That(Object.FindObjectsByType<ParticleSystem>().Length, Is.EqualTo(particleCount));
            Assert.That(Object.FindObjectsByType<TrailRenderer>().Length, Is.EqualTo(trailCount));
        }

        private static IEnumerator WaitUntilOrFail(System.Func<bool> condition, float timeout, string label)
        {
            float started = Time.realtimeSinceStartup;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(timeout), $"{label} timed out.");
                yield return null;
            }
        }

        private static TerrainSurface FindSurface(TerrainSurfaceType type)
        {
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>())
            {
                if (surface.SurfaceType == type)
                {
                    return surface;
                }
            }
            return null;
        }

        private static ClubData FindClub(bool putter)
        {
            foreach (ClubData club in Resources.FindObjectsOfTypeAll<ClubData>())
            {
                if (club.IsPutter == putter)
                {
                    return club;
                }
            }
            return null;
        }
    }
}

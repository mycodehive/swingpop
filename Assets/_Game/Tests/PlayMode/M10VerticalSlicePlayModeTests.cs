using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.Data;
using SwingPop.Debugging;
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
    public sealed class M10VerticalSlicePlayModeTests
    {
        [UnityTest]
        public IEnumerator SkyIslandScene_FullPlayableFlow_PreservesM1ThroughM9Integration()
        {
            yield return SceneManager.LoadSceneAsync("Hole01_SkyIsland", LoadSceneMode.Single);
            yield return null;

            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            ShotInputController input = Object.FindAnyObjectByType<ShotInputController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            CameraDirector cameraDirector = Object.FindAnyObjectByType<CameraDirector>();
            GameplayHudPresenter hud = Object.FindAnyObjectByType<GameplayHudPresenter>();
            ShotPresentationController presentation = Object.FindAnyObjectByType<ShotPresentationController>();
            GameplayAudioController audio = Object.FindAnyObjectByType<GameplayAudioController>();
            HoleInVfxController holeInVfx = Object.FindAnyObjectByType<HoleInVfxController>();
            ShotDebugOverlay overlay = Object.FindAnyObjectByType<ShotDebugOverlay>();
            BallTrajectoryDebug trajectory = Object.FindAnyObjectByType<BallTrajectoryDebug>();
            SkyIslandEnvironmentMotion environmentMotion = Object.FindAnyObjectByType<SkyIslandEnvironmentMotion>();
            SkyIslandAmbienceController ambience = Object.FindAnyObjectByType<SkyIslandAmbienceController>();

            Assert.That(shotFlow, Is.Not.Null);
            Assert.That(ball, Is.Not.Null);
            Assert.That(holeFlow, Is.Not.Null);
            Assert.That(cameraDirector, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(audio, Is.Not.Null);
            Assert.That(holeInVfx, Is.Not.Null);
            Assert.That(overlay, Is.Not.Null);
            Assert.That(trajectory, Is.Not.Null);
            Assert.That(overlay.IsOverlayVisible, Is.False, "M10 presentation starts with debug overlay hidden.");
            Assert.That(trajectory.IsTrajectoryVisible, Is.False, "M10 presentation starts with trajectory hidden.");

            Assert.That(environmentMotion, Is.Not.Null);
            Assert.That(environmentMotion.HasTuning, Is.True);
            Assert.That(environmentMotion.HasWindmillRotor, Is.True);
            Assert.That(environmentMotion.DriftingCloudCount, Is.GreaterThanOrEqualTo(5),
                "The presentation pass may add centrally animated clouds, but must preserve the established sky depth.");
            Assert.That(ambience, Is.Not.Null);
            Assert.That(ambience.HasTuning, Is.True);
            Assert.That(ambience.HasAmbientSource, Is.True);

            GameObject cupTarget = GameObject.Find("Cup Target");
            Assert.That(cupTarget, Is.Not.Null);
            Assert.That(Vector3.Distance(cupTarget.transform.position, holeFlow.Hole.CupPosition), Is.LessThan(0.01f),
                "HoleData cup position and scene Cup Target must agree.");

            int missingComponentCount = 0;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) missingComponentCount++;
                }
            }
            Assert.That(missingComponentCount, Is.Zero, "Scene contains a Missing Script component.");

            GameObject artRoot = GameObject.Find("M10 Sky Island Art");
            Assert.That(artRoot, Is.Not.Null);
            Assert.That(artRoot.GetComponentsInChildren<Collider>(true), Is.Empty,
                "Decorative environment must not alter gameplay physics.");
            Assert.That(artRoot.GetComponentsInChildren<Renderer>(true).Length, Is.InRange(70, 180));
            Assert.That(Object.FindObjectsByType<ParticleSystem>().Length, Is.LessThanOrEqualTo(16),
                "VFX Hero Pass uses a fixed 15-system reusable graph and must not grow per shot.");
            Assert.That(Object.FindObjectsByType<AudioSource>().Length, Is.LessThanOrEqualTo(6));

            Dictionary<TerrainSurfaceType, TerrainSurface> surfaces = FindSurfaces();
            foreach (TerrainSurfaceType required in new[]
                     {
                         TerrainSurfaceType.Tee, TerrainSurfaceType.Fairway, TerrainSurfaceType.Rough,
                         TerrainSurfaceType.Bunker, TerrainSurfaceType.Green, TerrainSurfaceType.Water,
                         TerrainSurfaceType.OutOfBounds
                     })
            {
                Assert.That(surfaces.ContainsKey(required), Is.True, $"Missing {required} gameplay surface.");
                Assert.That(surfaces[required].Data, Is.Not.Null);
            }

            if (input != null) input.enabled = false;
            holeFlow.DebugResetHole();
            cameraDirector.SkipIntro();
            yield return null;

            int impactBefore = presentation.ImpactPresentationCount;
            Assert.That(shotFlow.TryCommitShot(0.58f, 0.42f), Is.True);
            yield return WaitUntilOrFail(() => ball.State != BallState.Ready, 3f, "Normal launch");
            Assert.That(presentation.ImpactPresentationCount, Is.EqualTo(impactBefore + 1));

            holeFlow.DebugResetHole();
            yield return null;
            Assert.That(shotFlow.TryCommitShot(0.68f, 0f), Is.True);
            yield return WaitUntilOrFail(() => presentation.ImpactPresentationCount == impactBefore + 2, 3f, "Perfect launch");
            Assert.That(audio.GetCueCount(GameplayAudioCue.PerfectImpact), Is.GreaterThanOrEqualTo(1));

            TerrainSurface water = surfaces[TerrainSurfaceType.Water];
            TerrainSurface fairway = surfaces[TerrainSurfaceType.Fairway];
            TerrainSurface green = surfaces[TerrainSurfaceType.Green];
            ClubData driver = FindClub(false);
            ClubData putter = FindClub(true);
            Assert.That(driver, Is.Not.Null);
            Assert.That(putter, Is.Not.Null);

            holeFlow.DebugResetHole();
            yield return null;
            Bounds waterBounds = water.GetComponent<Collider>().bounds;
            ball.PrepareNextShot(new Vector3(waterBounds.center.x, 0.2f, waterBounds.min.z - 1.5f), fairway.Data);
            shotFlow.PrepareNextShot(Vector3.forward, driver);
            Assert.That(shotFlow.TryCommitShot(0.65f, 0f), Is.True);
            yield return WaitUntilOrFail(() => ball.State == BallState.Ready && ball.HasLastHazard, 12f, "Water recovery");

            holeFlow.SetAutomaticFlowSuspended(true);
            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 puttStart = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(puttStart, green.Data);
            shotFlow.PrepareNextShot(cup - puttStart, putter);
            int holeVfxBefore = holeInVfx.PlayCount;
            Assert.That(shotFlow.TryCommitShot(0.45f, 0f), Is.True);
            yield return WaitUntilOrFail(() => holeFlow.State == HoleFlowState.HoleComplete, 12f, "Hole-In flow");
            yield return null;

            Assert.That(holeInVfx.PlayCount, Is.EqualTo(holeVfxBefore + 1));
            Assert.That(hud.View.ResultView.IsVisible, Is.True);
            Assert.That(cameraDirector.CurrentMode, Is.EqualTo(CameraMode.HoleComplete).Or.EqualTo(CameraMode.Result));
        }

        private static Dictionary<TerrainSurfaceType, TerrainSurface> FindSurfaces()
        {
            Dictionary<TerrainSurfaceType, TerrainSurface> result = new();
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>())
            {
                result.TryAdd(surface.SurfaceType, surface);
            }
            return result;
        }

        private static ClubData FindClub(bool putter)
        {
            foreach (ClubData club in Resources.FindObjectsOfTypeAll<ClubData>())
            {
                if (club.IsPutter == putter) return club;
            }
            return null;
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
    }
}

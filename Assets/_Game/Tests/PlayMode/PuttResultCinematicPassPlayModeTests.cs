using System.Collections;
using NUnit.Framework;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.CharacterSystem;
using SwingPop.Gameplay.Ball;
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
    public sealed class PuttResultCinematicPassPlayModeTests
    {
        [UnityTest]
        public IEnumerator Hole01_HoleInSequence_IsSingleSourceStagedAndAllocationStable()
        {
            yield return SceneManager.LoadSceneAsync("Hole01_SkyIsland", LoadSceneMode.Single);
            yield return null;
            yield return null;

            PuttResultCinematicController controller = Object.FindAnyObjectByType<PuttResultCinematicController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            CameraDirector camera = Object.FindAnyObjectByType<CameraDirector>();
            CharacterGolfController character = Object.FindAnyObjectByType<CharacterGolfController>();
            GameplayAudioController audio = Object.FindAnyObjectByType<GameplayAudioController>();
            HoleInVfxController holeVfx = Object.FindAnyObjectByType<HoleInVfxController>();
            HudResultView resultView = Object.FindAnyObjectByType<HudResultView>(FindObjectsInactive.Include);

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(holeFlow, Is.Not.Null);
            Assert.That(ball, Is.Not.Null);
            Assert.That(shotFlow, Is.Not.Null);
            Assert.That(camera.CinematicTuning, Is.SameAs(controller.Tuning));
            Assert.That(character.CinematicTuning, Is.SameAs(controller.Tuning));
            Assert.That(audio.CinematicTuning, Is.SameAs(controller.Tuning));
            Assert.That(holeVfx.CinematicTuning, Is.SameAs(controller.Tuning));
            Assert.That(resultView.HasStagedGroups, Is.True);

            int particleObjects = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length;
            int canvasObjects = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length;
            int holeAudioBefore = audio.GetCueCount(GameplayAudioCue.HoleIn);
            int resultAudioBefore = audio.GetCueCount(GameplayAudioCue.Result);
            int vfxBefore = holeVfx.PlayCount;

            holeFlow.SetAutomaticFlowSuspended(true);
            holeFlow.DebugResetHole();
            Assert.That(holeFlow.TryCompleteHole(ball), Is.True);
            Assert.That(holeFlow.TryCompleteHole(ball), Is.False);
            Assert.That(controller.StartCount, Is.EqualTo(1));
            Assert.That(controller.Phase, Is.EqualTo(PuttResultCinematicPhase.HoleInMoment));
            Assert.That(camera.CurrentMode, Is.EqualTo(CameraMode.HoleComplete));
            Assert.That(audio.GetCueCount(GameplayAudioCue.HoleIn), Is.EqualTo(holeAudioBefore + 1));
            Assert.That(audio.GetCueCount(GameplayAudioCue.Result), Is.EqualTo(resultAudioBefore));

            yield return new WaitForSeconds(controller.Tuning.CelebrationDelay + 0.05f);
            Assert.That(controller.CharacterReactionCount, Is.EqualTo(1));
            Assert.That(controller.Phase, Is.EqualTo(PuttResultCinematicPhase.CharacterReaction));

            float untilResult = controller.Tuning.ResultRevealDelay - controller.Tuning.CelebrationDelay + 0.08f;
            yield return new WaitForSeconds(untilResult);
            Assert.That(controller.ResultRevealCount, Is.EqualTo(1));
            Assert.That(camera.CurrentMode, Is.EqualTo(CameraMode.Result));
            Assert.That(resultView.IsVisible, Is.True);
            Assert.That(resultView.ResultLabel, Is.EqualTo(HudPresentationMapper.FormatResultRelative(holeFlow.Result)));
            Assert.That(audio.GetCueCount(GameplayAudioCue.Result), Is.EqualTo(resultAudioBefore + 1));

            yield return new WaitForSeconds(controller.Tuning.ResultFrameDuration + 0.08f);
            Assert.That(controller.Phase, Is.EqualTo(PuttResultCinematicPhase.ResultHold));
            Assert.That(controller.StartCount, Is.EqualTo(1));
            Assert.That(controller.CharacterReactionCount, Is.EqualTo(1));
            Assert.That(controller.ResultRevealCount, Is.EqualTo(1));
            Assert.That(holeVfx.PlayCount, Is.EqualTo(vfxBefore + 1));
            Assert.That(holeVfx.RingSequenceCount, Is.GreaterThan(0));
            Assert.That(holeVfx.CelebrationSequenceCount, Is.GreaterThan(0));
            Assert.That(shotFlow.TryCommitShot(0.5f, 0f), Is.False);
            Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Length,
                Is.EqualTo(particleObjects));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Length,
                Is.EqualTo(canvasObjects));

            holeFlow.SetAutomaticFlowSuspended(false);
            holeFlow.DebugResetHole();
        }
    }
}

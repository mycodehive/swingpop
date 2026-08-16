using NUnit.Framework;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.UI;
using UnityEngine;

namespace SwingPop.Tests
{
    public sealed class HudPresentationMapperTests
    {
        [TestCase(ShotFlowState.Aiming, "START SHOT")]
        [TestCase(ShotFlowState.PowerSelecting, "SET POWER")]
        [TestCase(ShotFlowState.ImpactSelecting, "IMPACT")]
        public void PrimaryAction_MapsInteractiveShotStates(ShotFlowState state, string expected)
        {
            HudActionPresentation result = HudPresentationMapper.MapPrimaryAction(state, HoleFlowState.Playing);

            Assert.That(result.Visible, Is.True);
            Assert.That(result.Interactable, Is.True);
            Assert.That(result.Label, Is.EqualTo(expected));
        }

        [Test]
        public void PrimaryAction_HidesDuringCommittedShotAndHoleComplete()
        {
            Assert.That(
                HudPresentationMapper.MapPrimaryAction(ShotFlowState.ShotCommitted, HoleFlowState.Playing).Visible,
                Is.False);
            Assert.That(
                HudPresentationMapper.MapPrimaryAction(ShotFlowState.Aiming, HoleFlowState.HoleComplete).Visible,
                Is.False);
        }

        [TestCase(SpinPreset.NoSpin, "NO SPIN  --")]
        [TestCase(SpinPreset.TopSpin, "TOP SPIN  ^")]
        [TestCase(SpinPreset.BackSpin, "BACK SPIN  v")]
        [TestCase(SpinPreset.LeftSideSpin, "LEFT SPIN  <")]
        [TestCase(SpinPreset.RightSideSpin, "RIGHT SPIN  >")]
        public void Spin_MapsEveryPreset(SpinPreset preset, string expected)
        {
            Assert.That(HudPresentationMapper.FormatSpin(preset, true), Is.EqualTo(expected));
        }

        [Test]
        public void Spin_ShowsDisabledForPutter()
        {
            Assert.That(
                HudPresentationMapper.FormatSpin(SpinPreset.RightSideSpin, false),
                Is.EqualTo("SPIN DISABLED"));
        }

        [TestCase(0f, "LEVEL  0.0 m")]
        [TestCase(4.24f, "▲ +4.2 m")]
        [TestCase(-2.06f, "▼ -2.1 m")]
        public void HeightDifference_FormatsDirection(float meters, string expected)
        {
            Assert.That(HudPresentationMapper.FormatHeightDifference(meters), Is.EqualTo(expected));
        }

        [Test]
        public void WindArrow_UsesWorldDirectionConsistently()
        {
            Assert.That(HudPresentationMapper.WindArrowAngle(Vector3.forward), Is.EqualTo(0f).Within(0.01f));
            Assert.That(HudPresentationMapper.WindArrowAngle(Vector3.right), Is.EqualTo(-90f).Within(0.01f));
            Assert.That(HudPresentationMapper.WindArrowAngle(Vector3.left), Is.EqualTo(90f).Within(0.01f));
            Assert.That(Mathf.Abs(HudPresentationMapper.WindArrowAngle(Vector3.back)), Is.EqualTo(180f).Within(0.01f));
        }

        [Test]
        public void Result_UsesScoreCalculatorResult()
        {
            ScoreResult birdie = ScoreCalculator.Calculate(4, 3);

            Assert.That(HudPresentationMapper.FormatResultRelative(birdie), Is.EqualTo("BIRDIE  -1"));
        }

        [TestCase(TerrainSurfaceType.Water, "WATER HAZARD\n+1 PENALTY")]
        [TestCase(TerrainSurfaceType.OutOfBounds, "OUT OF BOUNDS\n+1 PENALTY")]
        public void Hazard_FormatsPenaltyMessage(TerrainSurfaceType hazard, string expected)
        {
            Assert.That(HudPresentationMapper.FormatHazard(hazard), Is.EqualTo(expected));
        }
    }
}

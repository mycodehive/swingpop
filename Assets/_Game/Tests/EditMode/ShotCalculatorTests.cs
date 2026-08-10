using NUnit.Framework;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Tests.EditMode
{
    public sealed class ShotCalculatorTests
    {
        [TestCase(-50f, -30f)]
        [TestCase(12f, 12f)]
        [TestCase(50f, 30f)]
        public void ClampAimAngle_RespectsConfiguredRange(float input, float expected)
        {
            Assert.That(ShotCalculator.ClampAimAngle(input, -30f, 30f), Is.EqualTo(expected));
        }

        [TestCase(-10f, 0f)]
        [TestCase(50f, 0.5f)]
        [TestCase(120f, 1f)]
        public void NormalizePowerPercent_ReturnsZeroToOne(float percent, float expected)
        {
            Assert.That(ShotCalculator.NormalizePowerPercent(percent), Is.EqualTo(expected));
        }

        [TestCase(0.1f, ImpactGrade.Perfect)]
        [TestCase(0.3f, ImpactGrade.Great)]
        [TestCase(-0.6f, ImpactGrade.Good)]
        [TestCase(0.9f, ImpactGrade.Miss)]
        public void ClassifyImpact_UsesConfiguredZones(float offset, ImpactGrade expected)
        {
            Assert.That(ShotCalculator.ClassifyImpact(offset, 0.15f, 0.35f, 0.65f), Is.EqualTo(expected));
        }

        [Test]
        public void CalculateDispersionDegrees_IsDeterministicAndSigned()
        {
            float leftMiss = ShotCalculator.CalculateDispersionDegrees(-0.9f, ImpactGrade.Miss, 1.5f, 4f, 9f);
            float rightMiss = ShotCalculator.CalculateDispersionDegrees(0.9f, ImpactGrade.Miss, 1.5f, 4f, 9f);

            Assert.That(leftMiss, Is.EqualTo(-9f));
            Assert.That(rightMiss, Is.EqualTo(9f));
        }

        [Test]
        public void CreateCommand_CapturesAimPowerImpactAndFinalDirection()
        {
            ShotCommand command = CreateCommand(20f, 0.8f, 0f);

            Assert.That(command.AimAngleDegrees, Is.EqualTo(20f));
            Assert.That(command.Power01, Is.EqualTo(0.8f));
            Assert.That(command.ImpactGrade, Is.EqualTo(ImpactGrade.Perfect));
            Assert.That(command.EffectivePower01, Is.EqualTo(0.8f));
            Assert.That(Vector3.Angle(Vector3.forward, command.FinalDirection), Is.EqualTo(20f).Within(0.01f));
        }

        [Test]
        public void CreateCommand_MissLosesPowerAndChangesDirection()
        {
            ShotCommand perfect = CreateCommand(0f, 1f, 0f);
            ShotCommand miss = CreateCommand(0f, 1f, 0.9f);

            Assert.That(miss.ImpactGrade, Is.EqualTo(ImpactGrade.Miss));
            Assert.That(miss.EffectivePower01, Is.LessThan(perfect.EffectivePower01));
            Assert.That(Vector3.Angle(perfect.FinalDirection, miss.FinalDirection), Is.EqualTo(9f).Within(0.01f));
        }

        [Test]
        public void CreateCommand_CapturesNormalizedSpinData()
        {
            ShotCommand command = ShotCalculator.CreateCommand(
                Vector3.forward,
                0f,
                1f,
                0f,
                0.15f,
                0.35f,
                0.65f,
                1f,
                0.98f,
                0.9f,
                0.72f,
                1.5f,
                4f,
                9f,
                18f,
                35f,
                new ShotSpin(2f, -2f));

            Assert.That(command.Spin.VerticalSpin, Is.EqualTo(1f));
            Assert.That(command.Spin.SideSpin, Is.EqualTo(-1f));
        }

        private static ShotCommand CreateCommand(float aimAngle, float power, float impactOffset)
        {
            return ShotCalculator.CreateCommand(
                Vector3.forward,
                aimAngle,
                power,
                impactOffset,
                0.15f,
                0.35f,
                0.65f,
                1f,
                0.98f,
                0.9f,
                0.72f,
                1.5f,
                4f,
                9f,
                18f,
                35f);
        }
    }
}

using NUnit.Framework;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Tests.EditMode
{
    public sealed class BallFlightModelTests
    {
        [Test]
        public void ShotSpin_ClampsNormalizedValues()
        {
            ShotSpin spin = new(2f, -3f);

            Assert.That(spin.VerticalSpin, Is.EqualTo(1f));
            Assert.That(spin.SideSpin, Is.EqualTo(-1f));
        }

        [TestCase(SpinPreset.NoSpin, 0f, 0f)]
        [TestCase(SpinPreset.TopSpin, 1f, 0f)]
        [TestCase(SpinPreset.BackSpin, -1f, 0f)]
        [TestCase(SpinPreset.LeftSideSpin, 0f, -1f)]
        [TestCase(SpinPreset.RightSideSpin, 0f, 1f)]
        public void ShotSpin_FromPreset_ReturnsExpectedValues(
            SpinPreset preset,
            float expectedVertical,
            float expectedSide)
        {
            ShotSpin spin = ShotSpin.FromPreset(preset);

            Assert.That(spin.VerticalSpin, Is.EqualTo(expectedVertical));
            Assert.That(spin.SideSpin, Is.EqualTo(expectedSide));
        }

        [Test]
        public void DecaySpin_MovesTowardZeroWithoutCrossing()
        {
            Assert.That(BallFlightModel.DecaySpin(0.5f, 2f, 0.1f), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(BallFlightModel.DecaySpin(-0.1f, 2f, 0.1f), Is.Zero);
        }

        [Test]
        public void SideSpinAcceleration_IsRelativeToTravelDirectionAndSigned()
        {
            Vector3 left = CalculateAirAcceleration(new ShotSpin(0f, -1f));
            Vector3 right = CalculateAirAcceleration(new ShotSpin(0f, 1f));

            Assert.That(left.x, Is.LessThan(0f));
            Assert.That(right.x, Is.GreaterThan(0f));
            Assert.That(left.x, Is.EqualTo(-right.x).Within(0.0001f));
        }

        [Test]
        public void RollingDeceleration_UsesNeutralTopAndBackModifiers()
        {
            float neutral = BallGroundResponse.CalculateRollingDeceleration(2f, 0f, 0.5f, 4f);
            float top = BallGroundResponse.CalculateRollingDeceleration(2f, 1f, 0.5f, 4f);
            float back = BallGroundResponse.CalculateRollingDeceleration(2f, -1f, 0.5f, 4f);

            Assert.That(neutral, Is.EqualTo(2f));
            Assert.That(top, Is.LessThan(neutral));
            Assert.That(back, Is.GreaterThan(neutral));
        }

        [Test]
        public void FirstLanding_TopSpinBoostsAndBackSpinBrakesForwardVelocity()
        {
            Vector3 neutral = Landing(ShotSpin.None);
            Vector3 top = Landing(new ShotSpin(1f, 0f));
            Vector3 back = Landing(new ShotSpin(-1f, 0f));

            Assert.That(top.z, Is.GreaterThan(neutral.z));
            Assert.That(back.z, Is.LessThan(neutral.z));
            Assert.That(back.z, Is.LessThan(0f));
        }

        private static Vector3 CalculateAirAcceleration(ShotSpin spin)
        {
            return BallFlightModel.CalculateAirAcceleration(
                Vector3.forward * 18f,
                spin,
                0f,
                0f,
                3f,
                18f,
                Vector3.zero);
        }

        private static Vector3 Landing(ShotSpin spin)
        {
            return BallGroundResponse.CalculateFirstLandingPlanarVelocity(
                Vector3.forward * 10f,
                Vector3.forward,
                spin,
                3f,
                0.9f,
                2f,
                0.3f);
        }
    }
}

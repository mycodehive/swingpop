using NUnit.Framework;
using SwingPop.Gameplay.Wind;
using UnityEngine;

namespace SwingPop.Tests.EditMode
{
    public sealed class WindPhysicsTests
    {
        [Test]
        public void CalmWind_ReturnsZeroAcceleration()
        {
            Vector3 acceleration = Calculate(Vector3.zero);

            Assert.That(acceleration, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void TailwindAndHeadwind_HaveOppositeForwardInfluence()
        {
            Vector3 tailwind = Calculate(Vector3.forward * 5f);
            Vector3 headwind = Calculate(Vector3.back * 5f);

            Assert.That(tailwind.z, Is.GreaterThan(0f));
            Assert.That(headwind.z, Is.LessThan(0f));
            Assert.That(tailwind.z, Is.EqualTo(-headwind.z).Within(0.0001f));
        }

        [Test]
        public void Crosswind_UsesWorldDirectionAndConfiguredMultiplier()
        {
            Vector3 left = Calculate(Vector3.left * 5f);
            Vector3 right = Calculate(Vector3.right * 5f);

            Assert.That(left.x, Is.LessThan(0f));
            Assert.That(right.x, Is.GreaterThan(0f));
            Assert.That(left.x, Is.EqualTo(-right.x).Within(0.0001f));
            Assert.That(Mathf.Abs(right.x), Is.EqualTo(2f).Within(0.0001f));
        }

        private static Vector3 Calculate(Vector3 windVelocity)
        {
            return WindPhysics.CalculateAcceleration(
                windVelocity,
                Vector3.forward * 18f,
                0.2f,
                1f,
                2f);
        }
    }
}

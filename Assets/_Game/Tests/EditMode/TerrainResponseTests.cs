using System.Reflection;
using NUnit.Framework;
using SwingPop.Data;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Tests.EditMode
{
    public sealed class TerrainResponseTests
    {
        private TerrainSurfaceData fairway;
        private TerrainSurfaceData rough;
        private TerrainSurfaceData bunker;
        private TerrainSurfaceData green;

        [SetUp]
        public void SetUp()
        {
            fairway = CreateSurface(TerrainSurfaceType.Fairway, 1f, 1f, 1f, 1f, 1f);
            rough = CreateSurface(TerrainSurfaceType.Rough, 0.9f, 1.35f, 0.65f, 0.7f, 1.35f);
            bunker = CreateSurface(TerrainSurfaceType.Bunker, 0.65f, 2f, 0.25f, 0.3f, 1.8f);
            green = CreateSurface(TerrainSurfaceType.Green, 1f, 0.7f, 0.2f, 1.25f, 0.65f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(fairway);
            Object.DestroyImmediate(rough);
            Object.DestroyImmediate(bunker);
            Object.DestroyImmediate(green);
        }

        [Test]
        public void Fairway_IsNeutralBaseline()
        {
            Assert.That(TerrainResponse.ApplyPowerModifier(0.8f, fairway), Is.EqualTo(0.8f));
            Assert.That(TerrainResponse.CalculateRollingDeceleration(2f, fairway), Is.EqualTo(2f));
            Assert.That(TerrainResponse.ApplyBounceModifier(3f, fairway), Is.EqualTo(3f));
            Assert.That(TerrainResponse.ApplySpinResponse(0.5f, fairway), Is.EqualTo(0.5f));
        }

        [Test]
        public void RoughAndBunker_ProgressivelyIncreaseRollingResistance()
        {
            float fairwayDeceleration = TerrainResponse.CalculateRollingDeceleration(1f, fairway);
            float roughDeceleration = TerrainResponse.CalculateRollingDeceleration(1f, rough);
            float bunkerDeceleration = TerrainResponse.CalculateRollingDeceleration(1f, bunker);

            Assert.That(roughDeceleration, Is.GreaterThan(fairwayDeceleration));
            Assert.That(bunkerDeceleration, Is.GreaterThan(roughDeceleration));
        }

        [Test]
        public void GreenBounce_IsLowerThanFairway()
        {
            Assert.That(
                TerrainResponse.ApplyBounceModifier(4f, green),
                Is.LessThan(TerrainResponse.ApplyBounceModifier(4f, fairway)));
        }

        [Test]
        public void LiePowerModifier_IsCapturedInShotCommand()
        {
            ShotCommand command = new(
                Vector3.forward,
                Vector3.forward,
                0f,
                1f,
                1f,
                0f,
                ImpactGrade.Perfect,
                1f,
                0f,
                18f,
                35f);

            ShotCommand modified = ShotCalculator.ApplySurfacePowerModifier(command, bunker.PowerModifier);

            Assert.That(modified.SurfacePowerModifier, Is.EqualTo(0.65f));
            Assert.That(modified.EffectivePower01, Is.EqualTo(0.65f));
        }

        [Test]
        public void RoughAndBunker_SuppressSpinComparedWithFairway()
        {
            float fairwaySpin = TerrainResponse.ApplySpinResponse(1f, fairway);
            float roughSpin = TerrainResponse.ApplySpinResponse(1f, rough);
            float bunkerSpin = TerrainResponse.ApplySpinResponse(1f, bunker);

            Assert.That(roughSpin, Is.LessThan(fairwaySpin));
            Assert.That(bunkerSpin, Is.LessThan(roughSpin));
        }

        [Test]
        public void TerrainSurfaceComponent_ReturnsAssignedDataWithoutTags()
        {
            GameObject surfaceObject = new("Surface", typeof(BoxCollider));
            TerrainSurface component = surfaceObject.AddComponent<TerrainSurface>();
            SetField(component, "data", rough);

            Assert.That(component.Data, Is.SameAs(rough));
            Assert.That(component.SurfaceType, Is.EqualTo(TerrainSurfaceType.Rough));

            Object.DestroyImmediate(surfaceObject);
        }

        private static TerrainSurfaceData CreateSurface(
            TerrainSurfaceType type,
            float power,
            float friction,
            float bounce,
            float spin,
            float rollingResistance)
        {
            TerrainSurfaceData data = ScriptableObject.CreateInstance<TerrainSurfaceData>();
            SetField(data, "surfaceType", type);
            SetField(data, "powerModifier", power);
            SetField(data, "friction", friction);
            SetField(data, "bounceModifier", bounce);
            SetField(data, "spinResponse", spin);
            SetField(data, "rollingResistance", rollingResistance);
            return data;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            typeof(TerrainSurfaceData).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static void SetField(TerrainSurface target, string fieldName, object value)
        {
            typeof(TerrainSurface).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }
}

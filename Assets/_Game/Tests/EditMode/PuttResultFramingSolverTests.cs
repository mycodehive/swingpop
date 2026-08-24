using NUnit.Framework;
using SwingPop.CameraSystem;
using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Tests
{
    public sealed class PuttResultFramingSolverTests
    {
        [Test]
        public void AddressFraming_IncludesCharacterBallAndCupRelationship()
        {
            PuttResultCinematicTuningData tuning = ScriptableObject.CreateInstance<PuttResultCinematicTuningData>();
            try
            {
                CameraFraming framing = PuttResultFramingSolver.ResolvePutt(
                    tuning,
                    Vector3.zero,
                    Vector3.forward * 4f,
                    Vector3.left * 1.2f,
                    Vector3.forward,
                    false,
                    false);

                Assert.That(framing.TargetName, Is.EqualTo("Putt Character Ball and Cup"));
                Assert.That(framing.Target.x, Is.LessThan(0f));
                Assert.That(framing.Target.z, Is.GreaterThan(0f).And.LessThan(4f));
                Assert.That(framing.FieldOfView, Is.EqualTo(tuning.PuttAddressFieldOfView));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void ApproachFraming_PrioritizesCupWithoutChangingGameplayRadius()
        {
            PuttResultCinematicTuningData tuning = ScriptableObject.CreateInstance<PuttResultCinematicTuningData>();
            try
            {
                Vector3 cup = new(0f, 0f, 8f);
                CameraFraming framing = PuttResultFramingSolver.ResolvePutt(
                    tuning, Vector3.forward * 7f, cup, Vector3.left, Vector3.forward, true, true);

                Assert.That(framing.TargetName, Is.EqualTo("Cup Approach"));
                Assert.That(framing.Target.z, Is.EqualTo(cup.z).Within(0.001f));
                Assert.That(framing.FieldOfView, Is.EqualTo(tuning.ApproachFieldOfView));
                Assert.That(tuning.ApproachDistance, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void ResultFraming_KeepsCharacterAndCupInSharedComposition()
        {
            PuttResultCinematicTuningData tuning = ScriptableObject.CreateInstance<PuttResultCinematicTuningData>();
            try
            {
                Vector3 character = new(-1.2f, 0f, 5f);
                Vector3 cup = new(0f, 0f, 8f);
                CameraFraming framing = PuttResultFramingSolver.ResolveResult(
                    tuning, character, cup, Vector3.forward);

                Assert.That(framing.TargetName, Is.EqualTo("Character Cup Result"));
                Assert.That(framing.Target.x, Is.InRange(character.x, cup.x));
                Assert.That(framing.Target.z, Is.InRange(character.z, cup.z));
                Assert.That(framing.FieldOfView, Is.EqualTo(tuning.ResultFieldOfView));
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}

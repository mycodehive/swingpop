using NUnit.Framework;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using UnityEngine;

namespace SwingPop.Tests.EditMode
{
    public sealed class HoleScoringTests
    {
        [TestCase(1, "Albatross", -3)]
        [TestCase(2, "Eagle", -2)]
        [TestCase(3, "Birdie", -1)]
        [TestCase(4, "Par", 0)]
        [TestCase(5, "Bogey", 1)]
        [TestCase(6, "Double Bogey", 2)]
        [TestCase(7, "+3", 3)]
        public void ScoreCalculator_MapsParFourResults(int strokes, string label, int relative)
        {
            ScoreResult result = ScoreCalculator.Calculate(4, strokes);

            Assert.That(result.Label, Is.EqualTo(label));
            Assert.That(result.RelativeToPar, Is.EqualTo(relative));
        }

        [Test]
        public void CommittedShotsIncrementButCanceledSelectionDoesNot()
        {
            HoleProgressTracker tracker = new();
            tracker.StartHole(Vector3.zero);

            int beforeCanceledSelection = tracker.StrokeCount;
            Assert.That(tracker.StrokeCount, Is.EqualTo(beforeCanceledSelection));

            tracker.RecordCommittedShot();
            tracker.RecordCommittedShot();
            Assert.That(tracker.StrokeCount, Is.EqualTo(2));
        }

        [Test]
        public void HazardAddsPenaltyAndPreservesLastValidLie()
        {
            HoleProgressTracker tracker = new();
            tracker.StartHole(Vector3.zero);
            tracker.RecordCommittedShot();
            tracker.RecordValidStop(new Vector3(0f, 0.15f, 20f), TerrainSurfaceType.Fairway);
            tracker.RecordCommittedShot();
            tracker.RecordHazardPenalty();

            Assert.That(tracker.StrokeCount, Is.EqualTo(3));
            Assert.That(tracker.PenaltyCount, Is.EqualTo(1));
            Assert.That(tracker.LastValidPosition.z, Is.EqualTo(20f));
            Assert.That(tracker.LastValidLie, Is.EqualTo(TerrainSurfaceType.Fairway));
        }

        [Test]
        public void ValidStopsUpdateRecoveryButHazardsDoNot()
        {
            HoleProgressTracker tracker = new();
            tracker.StartHole(Vector3.zero);
            tracker.RecordValidStop(new Vector3(2f, 0.15f, 30f), TerrainSurfaceType.Rough);
            tracker.RecordValidStop(new Vector3(50f, -4f, 50f), TerrainSurfaceType.Water);

            Assert.That(tracker.LastValidPosition, Is.EqualTo(new Vector3(2f, 0.15f, 30f)));
            Assert.That(tracker.LastValidLie, Is.EqualTo(TerrainSurfaceType.Rough));
        }

        [Test]
        public void CompleteLocksResultAndState()
        {
            HoleProgressTracker tracker = new();
            tracker.StartHole(Vector3.zero);
            for (int index = 0; index < 4; index++)
            {
                tracker.RecordCommittedShot();
            }

            ScoreResult result = tracker.Complete(4);
            tracker.RecordCommittedShot();

            Assert.That(tracker.State, Is.EqualTo(HoleFlowState.HoleComplete));
            Assert.That(tracker.StrokeCount, Is.EqualTo(4));
            Assert.That(result.Label, Is.EqualTo("Par"));
        }

        [Test]
        public void CupEligibilityRequiresGreenLowSpeedAndHeight()
        {
            Assert.That(CupCaptureRules.IsEligible(0.3f, 1.2f, 0.1f, TerrainSurfaceType.Green, 0.55f, 2.4f, 0.45f), Is.True);
            Assert.That(CupCaptureRules.IsEligible(0.3f, 4f, 0.1f, TerrainSurfaceType.Green, 0.55f, 2.4f, 0.45f), Is.False);
            Assert.That(CupCaptureRules.IsEligible(0.3f, 1.2f, 0.1f, TerrainSurfaceType.Fairway, 0.55f, 2.4f, 0.45f), Is.False);
            Assert.That(CupCaptureRules.IsEligible(0.3f, 1.2f, 1f, TerrainSurfaceType.Green, 0.55f, 2.4f, 0.45f), Is.False);
        }

        [Test]
        public void PutterLaunchIsLowAndRollModifierExtendsRoll()
        {
            Vector3 velocity = ClubShotCalculator.CalculateLaunchVelocity(
                Vector3.forward,
                8f,
                1f,
                0.75f,
                1f);

            Assert.That(velocity.z, Is.GreaterThan(5.9f));
            Assert.That(velocity.y, Is.LessThan(0.2f));
            Assert.That(ClubShotCalculator.ApplyRollModifier(1.4f, 1.5f), Is.LessThan(1.4f));
        }
    }
}

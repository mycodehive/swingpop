using NUnit.Framework;
using System;
using System.Collections.Generic;
using SwingPop.CharacterSystem;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace SwingPop.Tests
{
    public sealed class CharacterFlowTests
    {
        [Test]
        public void AddressPosition_UsesShotRelativeLateralBackwardAndHeightOffsets()
        {
            Vector3 position = CharacterPlacementCalculator.CalculateAddressPosition(
                new Vector3(2f, 0.15f, 3f),
                Vector3.forward,
                -0.9f,
                -0.2f,
                -0.15f);

            Assert.That(
                position,
                Is.EqualTo(new Vector3(1.1f, 0f, 2.8f)).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void AimRotation_FollowsPlanarAimDirection()
        {
            Quaternion rotation = CharacterPlacementCalculator.CalculateAimRotation(Vector3.right, Quaternion.identity);

            Assert.That(Vector3.Angle(rotation * Vector3.forward, Vector3.right), Is.LessThan(0.01f));
        }

        [Test]
        public void ImpactGate_FiresOnceAtConfiguredNormalizedTime()
        {
            ImpactEventGate gate = new();
            gate.Begin(0.4f, 0.5f);

            Assert.That(gate.Tick(0.19f), Is.False);
            Assert.That(gate.Tick(0.02f), Is.True);
            Assert.That(gate.Tick(1f), Is.False);
            Assert.That(gate.TryFire(), Is.False);
        }

        [Test]
        public void FallbackGuard_ExpiresOnceAfterDelay()
        {
            ShotImpactDelayGuard guard = new();
            guard.Begin(0.6f);

            Assert.That(guard.Tick(0.59f), Is.False);
            Assert.That(guard.Tick(0.02f), Is.True);
            Assert.That(guard.Tick(1f), Is.False);
        }

        [TestCase(ShotFlowState.Aiming, false, CharacterState.Address)]
        [TestCase(ShotFlowState.PowerSelecting, true, CharacterState.PuttAddress)]
        [TestCase(ShotFlowState.ImpactSelecting, false, CharacterState.BackSwing)]
        [TestCase(ShotFlowState.ImpactSelecting, true, CharacterState.PuttBackSwing)]
        [TestCase(ShotFlowState.ShotCommitted, false, CharacterState.Swing)]
        [TestCase(ShotFlowState.ShotCommitted, true, CharacterState.PuttSwing)]
        public void ShotState_MapsToCharacterPresentation(
            ShotFlowState shotState,
            bool isPutter,
            CharacterState expected)
        {
            Assert.That(CharacterFlowResolver.ResolveShotState(shotState, isPutter), Is.EqualTo(expected));
        }

        [TestCase(ClubType.Driver, CharacterClubVisualType.Driver)]
        [TestCase(ClubType.Putter, CharacterClubVisualType.Putter)]
        public void ClubType_SelectsMatchingPlaceholderVisual(ClubType club, CharacterClubVisualType expected)
        {
            Assert.That(CharacterFlowResolver.ResolveClubVisual(club), Is.EqualTo(expected));
        }

        [Test]
        public void ScoreResult_MapsToCelebrationHooks()
        {
            Assert.That(
                CharacterFlowResolver.ResolveCelebration(new ScoreResult(1, 4, -3, "Hole In One")),
                Is.EqualTo(CharacterState.HoleInOneCelebration));
            Assert.That(
                CharacterFlowResolver.ResolveCelebration(new ScoreResult(2, 4, -2, "Eagle")),
                Is.EqualTo(CharacterState.EagleCelebration));
            Assert.That(
                CharacterFlowResolver.ResolveCelebration(new ScoreResult(3, 4, -1, "Birdie")),
                Is.EqualTo(CharacterState.BirdieCelebration));
            Assert.That(
                CharacterFlowResolver.ResolveCelebration(new ScoreResult(5, 4, 1, "Bogey")),
                Is.EqualTo(CharacterState.Sad));
        }

        [Test]
        public void VerticalAim_FallsBackWithoutInvalidRotation()
        {
            Vector3 forward = CharacterPlacementCalculator.ResolvePlanarForward(Vector3.up, Vector3.left);

            Assert.That(forward, Is.EqualTo(Vector3.left).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void AnimatorContract_MapsEveryCharacterStateToUniqueStableNameAndHash()
        {
            HashSet<string> names = new();
            HashSet<int> hashes = new();
            foreach (CharacterState state in Enum.GetValues(typeof(CharacterState)))
            {
                Assert.That(names.Add(CharacterAnimatorContract.GetStateName(state)), Is.True, state.ToString());
                Assert.That(hashes.Add(CharacterAnimatorContract.GetStateHash(state)), Is.True, state.ToString());
            }
        }

        [Test]
        public void MissingAnimator_UsesProceduralFallback()
        {
            Assert.That(
                CharacterAnimationController.ResolveAnimationMode(null),
                Is.EqualTo(CharacterAnimationMode.ProceduralFallback));
        }
    }
}

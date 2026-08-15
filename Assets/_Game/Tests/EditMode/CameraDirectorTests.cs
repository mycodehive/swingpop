using NUnit.Framework;
using SwingPop.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace SwingPop.Tests
{
    public sealed class CameraDirectorTests
    {
        [Test]
        public void ModeStateMachine_RejectsDuplicateRequest()
        {
            CameraModeStateMachine state = new();
            state.Reset(CameraMode.Address);

            Assert.That(state.Request(CameraMode.Address, 0.5f), Is.False);
            Assert.That(state.Current, Is.EqualTo(CameraMode.Address));
        }

        [Test]
        public void ModeStateMachine_TracksPreviousAndTransitionProgress()
        {
            CameraModeStateMachine state = new();
            state.Reset(CameraMode.Aim);

            Assert.That(state.Request(CameraMode.Impact, 0.5f), Is.True);
            state.Tick(0.25f);

            Assert.That(state.Previous, Is.EqualTo(CameraMode.Aim));
            Assert.That(state.Current, Is.EqualTo(CameraMode.Impact));
            Assert.That(state.TransitionProgress, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(state.IsTransitioning, Is.True);
        }

        [Test]
        public void ModeStateMachine_ClampsNegativeDeltaAndCompletes()
        {
            CameraModeStateMachine state = new();
            state.Request(CameraMode.BallFollow, 0.4f);
            state.Tick(-2f);
            Assert.That(state.TransitionProgress, Is.Zero);

            state.Tick(2f);
            Assert.That(state.TransitionProgress, Is.EqualTo(1f));
            Assert.That(state.IsTransitioning, Is.False);
        }

        [Test]
        public void CameraPose_LerpInterpolatesPositionRotationAndFov()
        {
            CameraPose from = new(Vector3.zero, Quaternion.identity, 40f);
            CameraPose to = new(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 60f);

            CameraPose midpoint = CameraPose.Lerp(from, to, 0.5f);

            Assert.That(midpoint.Position.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(midpoint.FieldOfView, Is.EqualTo(50f).Within(0.001f));
            Assert.That(Quaternion.Angle(Quaternion.identity, midpoint.Rotation), Is.EqualTo(45f).Within(0.01f));
        }

        [Test]
        public void LocalOffset_UsesShotRelativeRightUpForwardAxes()
        {
            Vector3 position = CameraMath.LocalOffset(Vector3.zero, Vector3.forward, new Vector3(2f, 3f, -4f));

            Assert.That(position, Is.EqualTo(new Vector3(2f, 3f, -4f)).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void ResolvePlanarForward_FallsBackForVerticalVelocity()
        {
            Vector3 forward = CameraMath.ResolvePlanarForward(Vector3.up * 12f, Vector3.right);

            Assert.That(forward, Is.EqualTo(Vector3.right).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void FollowDistance_ClampsSpeedExtension()
        {
            Assert.That(CameraMath.FollowDistance(10f, 0.2f, 8f), Is.EqualTo(2f));
            Assert.That(CameraMath.FollowDistance(100f, 0.2f, 8f), Is.EqualTo(8f));
            Assert.That(CameraMath.FollowDistance(-5f, 0.2f, 8f), Is.Zero);
        }

        [Test]
        public void ExponentialBlend_IsFrameRateIndependentAndBounded()
        {
            float blend = CameraMath.ExponentialBlend(8f, 1f / 60f);

            Assert.That(blend, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(CameraMath.ExponentialBlend(8f, 0f), Is.Zero);
        }
    }
}

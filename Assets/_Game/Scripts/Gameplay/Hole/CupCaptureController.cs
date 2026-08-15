using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using UnityEngine;

namespace SwingPop.Gameplay.Hole
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class CupCaptureController : MonoBehaviour
    {
        [SerializeField] private HoleData hole;
        [SerializeField] private HoleFlowController holeFlow;

        private void Awake()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            if (hole != null)
            {
                trigger.radius = hole.AssistRadius;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            GolfBallController ball = other.GetComponentInParent<GolfBallController>();
            if (ball == null || hole == null || holeFlow == null || holeFlow.State != HoleFlowState.Playing)
            {
                return;
            }

            Vector3 toCup = hole.CupPosition - ball.PhysicsPosition;
            Vector3 planarToCup = Vector3.ProjectOnPlane(toCup, Vector3.up);
            float horizontalDistance = planarToCup.magnitude;
            float horizontalSpeed = Vector3.ProjectOnPlane(ball.Velocity, Vector3.up).magnitude;
            if (CupCaptureRules.IsEligible(
                    horizontalDistance,
                    horizontalSpeed,
                    toCup.y,
                    ball.CurrentLie,
                    hole.CaptureRadius,
                    hole.MaximumCaptureSpeed,
                    hole.MaximumHeightDifference))
            {
                holeFlow.TryCompleteHole(ball);
                return;
            }

            bool canAssist = ball.CurrentLie == TerrainSurfaceType.Green
                             && ball.State is BallState.Bouncing or BallState.Rolling
                             && horizontalDistance <= hole.AssistRadius
                             && horizontalSpeed <= hole.AssistMaximumSpeed
                             && planarToCup.sqrMagnitude > Mathf.Epsilon;
            if (canAssist)
            {
                ball.ApplyExternalAcceleration(planarToCup.normalized * hole.AssistAcceleration);
            }
        }
    }
}

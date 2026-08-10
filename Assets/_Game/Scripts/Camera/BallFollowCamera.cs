using SwingPop.Gameplay.Ball;
using UnityEngine;

namespace SwingPop.CameraSystem
{
    public sealed class BallFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private GolfBallController ball;

        [Header("Framing")]
        [SerializeField] private Vector3 followOffset = new(7f, 4.5f, -9f);
        [SerializeField] private Vector3 lookOffset = new(0f, 0.5f, 1.5f);

        [Header("Smoothing")]
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.22f;
        [SerializeField, Min(0.01f)] private float rotationSharpness = 8f;
        [SerializeField, Min(0.1f)] private float maximumFollowSpeed = 80f;

        private Vector3 followVelocity;

        private void OnEnable()
        {
            if (ball != null)
            {
                ball.ResetPerformed += SnapToBall;
                SnapToBall();
            }
        }

        private void OnDisable()
        {
            if (ball != null)
            {
                ball.ResetPerformed -= SnapToBall;
            }
        }

        private void LateUpdate()
        {
            if (ball == null)
            {
                return;
            }

            Vector3 targetPosition = ball.transform.position + followOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref followVelocity,
                positionSmoothTime,
                maximumFollowSpeed,
                Time.deltaTime);

            RotateTowardBall(Time.deltaTime);
        }

        private void SnapToBall()
        {
            if (ball == null)
            {
                return;
            }

            followVelocity = Vector3.zero;
            transform.position = ball.transform.position + followOffset;
            RotateTowardBall(1f);
        }

        private void RotateTowardBall(float deltaTime)
        {
            Vector3 lookDirection = ball.transform.position + lookOffset - transform.position;
            if (lookDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
        }
    }
}

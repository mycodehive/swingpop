using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "BallTuning", menuName = "SwingPop/Ball Tuning")]
    public sealed class BallTuningData : ScriptableObject
    {
        [Header("Launch")]
        [SerializeField, Min(0.1f)] private float launchSpeed = 18f;
        [SerializeField, Range(1f, 89f)] private float launchAngleDegrees = 35f;

        [Header("Rigidbody")]
        [SerializeField, Min(0.001f)] private float mass = 0.045f;
        [SerializeField, Min(0f)] private float linearDamping = 0.02f;
        [SerializeField, Min(0f)] private float angularDamping = 0.35f;
        [SerializeField, Min(0.1f)] private float maximumAngularVelocity = 60f;
        [SerializeField, Min(0.1f)] private float gravityScale = 1f;

        [Header("Ground Response")]
        [Tooltip("Planar speed removed per second while the ball is rolling.")]
        [SerializeField, Min(0f)] private float rollingDeceleration = 1.4f;
        [Tooltip("A grounded ball enters Rolling below this vertical speed.")]
        [SerializeField, Min(0f)] private float bounceToRollVerticalSpeed = 0.8f;
        [Tooltip("Collision normal must point upward by at least this amount to count as ground.")]
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.55f;

        [Header("Stop Detection")]
        [SerializeField, Min(0.001f)] private float stopLinearSpeed = 0.12f;
        [SerializeField, Min(0.001f)] private float stopAngularSpeed = 0.8f;
        [SerializeField, Min(0f)] private float stopStableDuration = 0.6f;

        public float LaunchSpeed => launchSpeed;
        public float LaunchAngleDegrees => launchAngleDegrees;
        public float Mass => mass;
        public float LinearDamping => linearDamping;
        public float AngularDamping => angularDamping;
        public float MaximumAngularVelocity => maximumAngularVelocity;
        public float GravityScale => gravityScale;
        public float RollingDeceleration => rollingDeceleration;
        public float BounceToRollVerticalSpeed => bounceToRollVerticalSpeed;
        public float MinimumGroundNormalY => minimumGroundNormalY;
        public float StopLinearSpeed => stopLinearSpeed;
        public float StopAngularSpeed => stopAngularSpeed;
        public float StopStableDuration => stopStableDuration;

        public Vector3 CalculateLaunchVelocity(Vector3 forward)
        {
            return CalculateLaunchVelocity(forward, 1f);
        }

        public Vector3 CalculateLaunchVelocity(Vector3 forward, float powerScale)
        {
            Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (planarForward.sqrMagnitude <= Mathf.Epsilon)
            {
                planarForward = Vector3.forward;
            }

            float scaledLaunchSpeed = launchSpeed * Mathf.Clamp01(powerScale);
            float angleRadians = launchAngleDegrees * Mathf.Deg2Rad;
            Vector3 horizontalVelocity = planarForward * (scaledLaunchSpeed * Mathf.Cos(angleRadians));
            Vector3 verticalVelocity = Vector3.up * (scaledLaunchSpeed * Mathf.Sin(angleRadians));
            return horizontalVelocity + verticalVelocity;
        }
    }
}

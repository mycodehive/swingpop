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

        [Header("Arcade Airborne Physics")]
        [Tooltip("Velocity-proportional airborne drag acceleration. This is separate from Rigidbody damping.")]
        [SerializeField, Min(0f)] private float airDragStrength = 0.035f;
        [Tooltip("Backspin lift / topspin downforce strength. Landing response remains the main vertical-spin effect.")]
        [SerializeField, Min(0f)] private float spinLiftStrength = 0.65f;
        [Tooltip("Lateral acceleration caused by full side spin relative to the current travel direction.")]
        [SerializeField, Min(0f)] private float sideSpinCurveStrength = 2.8f;
        [SerializeField, Min(0.1f)] private float airForceReferenceSpeed = 18f;
        [SerializeField, Min(0f)] private float airVerticalSpinDecay = 0.05f;
        [SerializeField, Min(0f)] private float airSideSpinDecay = 0.08f;

        [Header("Ground Response")]
        [Tooltip("Planar speed removed per second while the ball is rolling.")]
        [SerializeField, Min(0f)] private float rollingDeceleration = 1.4f;
        [Tooltip("A grounded ball enters Rolling below this vertical speed.")]
        [SerializeField, Min(0f)] private float bounceToRollVerticalSpeed = 0.8f;
        [Tooltip("Collision normal must point upward by at least this amount to count as ground.")]
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.55f;
        [Tooltip("Airborne ground contacts above this upward speed are takeoff contacts, not landings.")]
        [SerializeField, Min(0f)] private float maximumUpwardLandingSpeed = 0.1f;
        [Tooltip("Physics Material bounciness used as the single vertical bounce owner.")]
        [SerializeField, Range(0f, 1f)] private float bounceRetention = 0.48f;
        [SerializeField, Min(0f)] private float topSpinLandingBoost = 2.8f;
        [SerializeField, Range(0f, 1f)] private float backSpinLandingBrake = 0.65f;
        [SerializeField, Min(0f)] private float backSpinRollbackSpeed = 1.8f;
        [SerializeField, Min(0f)] private float sideSpinLandingResponse = 0.35f;
        [Tooltip("Rolling deceleration multiplier at full topspin. Values below one increase rollout.")]
        [SerializeField, Min(0f)] private float topSpinRollingDecelerationMultiplier = 0.42f;
        [Tooltip("Rolling deceleration multiplier at full backspin.")]
        [SerializeField, Min(0f)] private float backSpinRollingDecelerationMultiplier = 3.2f;
        [SerializeField, Min(0f)] private float groundVerticalSpinDecay = 0.38f;
        [SerializeField, Min(0f)] private float groundSideSpinDecay = 0.8f;

        [Header("Stop Detection")]
        [SerializeField, Min(0.001f)] private float stopLinearSpeed = 0.12f;
        [SerializeField, Min(0.001f)] private float stopAngularSpeed = 0.8f;
        [SerializeField, Min(0f)] private float stopStableDuration = 0.6f;

        [Header("Hazard Recovery")]
        [Tooltip("A moving ball below this world-space height is treated as Out Of Bounds and safely stopped.")]
        [SerializeField] private float outOfBoundsHeight = -8f;

        public float LaunchSpeed => launchSpeed;
        public float LaunchAngleDegrees => launchAngleDegrees;
        public float Mass => mass;
        public float LinearDamping => linearDamping;
        public float AngularDamping => angularDamping;
        public float MaximumAngularVelocity => maximumAngularVelocity;
        public float GravityScale => gravityScale;
        public float AirDragStrength => airDragStrength;
        public float SpinLiftStrength => spinLiftStrength;
        public float SideSpinCurveStrength => sideSpinCurveStrength;
        public float AirForceReferenceSpeed => airForceReferenceSpeed;
        public float AirVerticalSpinDecay => airVerticalSpinDecay;
        public float AirSideSpinDecay => airSideSpinDecay;
        public float RollingDeceleration => rollingDeceleration;
        public float BounceToRollVerticalSpeed => bounceToRollVerticalSpeed;
        public float MinimumGroundNormalY => minimumGroundNormalY;
        public float MaximumUpwardLandingSpeed => maximumUpwardLandingSpeed;
        public float BounceRetention => bounceRetention;
        public float TopSpinLandingBoost => topSpinLandingBoost;
        public float BackSpinLandingBrake => backSpinLandingBrake;
        public float BackSpinRollbackSpeed => backSpinRollbackSpeed;
        public float SideSpinLandingResponse => sideSpinLandingResponse;
        public float TopSpinRollingDecelerationMultiplier => topSpinRollingDecelerationMultiplier;
        public float BackSpinRollingDecelerationMultiplier => backSpinRollingDecelerationMultiplier;
        public float GroundVerticalSpinDecay => groundVerticalSpinDecay;
        public float GroundSideSpinDecay => groundSideSpinDecay;
        public float StopLinearSpeed => stopLinearSpeed;
        public float StopAngularSpeed => stopAngularSpeed;
        public float StopStableDuration => stopStableDuration;
        public float OutOfBoundsHeight => outOfBoundsHeight;

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

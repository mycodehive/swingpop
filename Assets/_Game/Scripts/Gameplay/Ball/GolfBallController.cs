using System;
using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Gameplay.Ball
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class GolfBallController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Rigidbody ballBody;
        [SerializeField] private SphereCollider ballCollider;
        [SerializeField] private BallTuningData tuning;
        [SerializeField] private Transform launchDirectionReference;

        [Header("Collision")]
        [Tooltip("Layers treated as a surface that can bounce, roll, and stop the ball.")]
        [SerializeField] private LayerMask groundLayers = 1;

        private readonly BallStopDetector stopDetector = new();
        private Vector3 resetPosition;
        private Quaternion resetRotation;
        private bool isGrounded;
        private BallState state = BallState.Ready;

        public event Action<BallState, BallState> StateChanged;
        public event Action Launched;
        public event Action ResetPerformed;

        public BallState State => state;
        public bool IsGrounded => isGrounded;
        public float Speed => ballBody != null ? ballBody.linearVelocity.magnitude : 0f;
        public float AngularSpeed => ballBody != null ? ballBody.angularVelocity.magnitude : 0f;
        public Vector3 Velocity => ballBody != null ? ballBody.linearVelocity : Vector3.zero;
        public Vector3 ResetPosition => resetPosition;
        public BallTuningData Tuning => tuning;

        private void Awake()
        {
            if (ballBody == null)
            {
                ballBody = GetComponent<Rigidbody>();
            }

            if (ballCollider == null)
            {
                ballCollider = GetComponent<SphereCollider>();
            }

            resetPosition = transform.position;
            resetRotation = transform.rotation;
            ApplyTuning();
            ResetBall();
        }

        private void FixedUpdate()
        {
            if (state is BallState.Ready or BallState.Stopped || tuning == null)
            {
                return;
            }

            ApplyGravityScale();

            if (isGrounded && state == BallState.Bouncing
                && Mathf.Abs(ballBody.linearVelocity.y) <= tuning.BounceToRollVerticalSpeed)
            {
                ChangeState(BallState.Rolling);
            }

            if (isGrounded && state == BallState.Rolling)
            {
                ApplyRollingDeceleration();
            }

            if (state == BallState.Rolling && stopDetector.Sample(
                    isGrounded,
                    Speed,
                    AngularSpeed,
                    Time.fixedDeltaTime,
                    tuning.StopLinearSpeed,
                    tuning.StopAngularSpeed,
                    tuning.StopStableDuration))
            {
                StopBall();
            }
        }

        public bool Launch()
        {
            if (state != BallState.Ready || tuning == null)
            {
                return false;
            }

            Vector3 forward = launchDirectionReference != null
                ? launchDirectionReference.forward
                : Vector3.forward;

            isGrounded = false;
            stopDetector.Reset();
            ballBody.isKinematic = false;
            ballBody.useGravity = true;
            ballBody.WakeUp();
            ballBody.linearVelocity = tuning.CalculateLaunchVelocity(forward);
            ChangeState(BallState.Airborne);
            Launched?.Invoke();
            return true;
        }

        public void ResetBall()
        {
            stopDetector.Reset();
            isGrounded = false;

            ballBody.useGravity = false;
            if (!ballBody.isKinematic)
            {
                ballBody.linearVelocity = Vector3.zero;
                ballBody.angularVelocity = Vector3.zero;
            }

            ballBody.isKinematic = true;
            ballBody.position = resetPosition;
            ballBody.rotation = resetRotation;
            transform.SetPositionAndRotation(resetPosition, resetRotation);

            ChangeState(BallState.Ready);
            ResetPerformed?.Invoke();
        }

        public Vector3 GetPreviewLaunchVelocity()
        {
            if (tuning == null)
            {
                return Vector3.zero;
            }

            Vector3 forward = launchDirectionReference != null
                ? launchDirectionReference.forward
                : Vector3.forward;
            return tuning.CalculateLaunchVelocity(forward);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!TryReadGroundContact(collision, out _))
            {
                return;
            }

            isGrounded = true;
            stopDetector.Reset();

            if (state == BallState.Airborne)
            {
                ChangeState(BallState.Bouncing);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (TryReadGroundContact(collision, out _))
            {
                isGrounded = true;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (IsGroundLayer(collision.gameObject.layer))
            {
                isGrounded = false;
                stopDetector.Reset();
            }
        }

        private bool TryReadGroundContact(Collision collision, out ContactPoint groundContact)
        {
            groundContact = default;
            if (tuning == null || !IsGroundLayer(collision.gameObject.layer))
            {
                return false;
            }

            for (int index = 0; index < collision.contactCount; index++)
            {
                ContactPoint contact = collision.GetContact(index);
                if (contact.normal.y >= tuning.MinimumGroundNormalY)
                {
                    groundContact = contact;
                    return true;
                }
            }

            return false;
        }

        private bool IsGroundLayer(int layer)
        {
            return (groundLayers.value & (1 << layer)) != 0;
        }

        private void ApplyTuning()
        {
            if (tuning == null || ballBody == null)
            {
                return;
            }

            ballBody.mass = tuning.Mass;
            ballBody.linearDamping = tuning.LinearDamping;
            ballBody.angularDamping = tuning.AngularDamping;
            ballBody.maxAngularVelocity = tuning.MaximumAngularVelocity;
            ballBody.interpolation = RigidbodyInterpolation.Interpolate;
            ballBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void ApplyGravityScale()
        {
            if (!ballBody.useGravity || Mathf.Approximately(tuning.GravityScale, 1f))
            {
                return;
            }

            ballBody.AddForce(
                Physics.gravity * (tuning.GravityScale - 1f),
                ForceMode.Acceleration);
        }

        private void ApplyRollingDeceleration()
        {
            Vector3 velocity = ballBody.linearVelocity;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            Vector3 slowedPlanarVelocity = Vector3.MoveTowards(
                planarVelocity,
                Vector3.zero,
                tuning.RollingDeceleration * Time.fixedDeltaTime);

            ballBody.linearVelocity = slowedPlanarVelocity + Vector3.up * velocity.y;
        }

        private void StopBall()
        {
            ballBody.linearVelocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.Sleep();
            ChangeState(BallState.Stopped);
        }

        private void ChangeState(BallState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            BallState previousState = state;
            state = nextState;
            StateChanged?.Invoke(previousState, nextState);
        }
    }
}

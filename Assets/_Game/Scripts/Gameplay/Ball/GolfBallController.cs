using System;
using SwingPop.Data;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
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
        [SerializeField] private WindController wind;
        [SerializeField] private TerrainSurfaceData defaultSurface;

        [Header("Collision")]
        [Tooltip("Layers treated as a surface that can bounce, roll, and stop the ball.")]
        [SerializeField] private LayerMask groundLayers = 1;

        private readonly BallStopDetector stopDetector = new();
        private readonly BallSpinState spinState = new();
        private Vector3 resetPosition;
        private Quaternion resetRotation;
        private bool isGrounded;
        private BallState state = BallState.Ready;
        private Vector3 launchForward = Vector3.forward;
        private bool firstLandingResponseApplied;
        private bool backSpinRollbackApplied;
        private TerrainSurfaceData currentSurface;
        private TerrainSurfaceType lastHazard;
        private bool hasLastHazard;
        private float lastFixedVerticalVelocity;
        private float activeRollModifier = 1f;

        public event Action<BallState, BallState> StateChanged;
        public event Action Launched;
        public event Action ResetPerformed;
        public event Action<TerrainSurfaceType> HazardEntered;

        public BallState State => state;
        public bool IsGrounded => isGrounded;
        public float Speed => ballBody != null ? ballBody.linearVelocity.magnitude : 0f;
        public float AngularSpeed => ballBody != null ? ballBody.angularVelocity.magnitude : 0f;
        public Vector3 Velocity => ballBody != null ? ballBody.linearVelocity : Vector3.zero;
        public Vector3 PhysicsPosition => ballBody != null ? ballBody.position : transform.position;
        public Vector3 ResetPosition => resetPosition;
        public BallTuningData Tuning => tuning;
        public ShotSpin CurrentSpin => spinState.Current;
        public Vector3 LaunchForward => launchForward;
        public TerrainSurfaceData CurrentSurfaceData => currentSurface;
        public TerrainSurfaceType CurrentLie => currentSurface != null
            ? currentSurface.SurfaceType
            : TerrainSurfaceType.Fairway;
        public bool HasLastHazard => hasLastHazard;
        public TerrainSurfaceType LastHazard => lastHazard;

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
            if (state is BallState.Ready or BallState.Stopped or BallState.Holed || tuning == null)
            {
                return;
            }

            if (PhysicsPosition.y <= tuning.OutOfBoundsHeight)
            {
                HandleHazard(TerrainSurfaceType.OutOfBounds, null);
                return;
            }

            lastFixedVerticalVelocity = ballBody.linearVelocity.y;
            ApplyGravityScale();

            if (!isGrounded && state is BallState.Airborne or BallState.Bouncing)
            {
                ApplyAirbornePhysics();
                spinState.Decay(
                    tuning.AirVerticalSpinDecay,
                    tuning.AirSideSpinDecay,
                    Time.fixedDeltaTime);
            }
            else if (isGrounded)
            {
                spinState.Decay(
                    tuning.GroundVerticalSpinDecay,
                    tuning.GroundSideSpinDecay,
                    Time.fixedDeltaTime);
            }

            if (isGrounded && state == BallState.Bouncing
                && Mathf.Abs(ballBody.linearVelocity.y) <= tuning.BounceToRollVerticalSpeed)
            {
                ApplyBackSpinRollback();
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

            return LaunchVelocity(tuning.CalculateLaunchVelocity(forward), ShotSpin.None);
        }

        public bool Launch(ShotCommand command)
        {
            if (state != BallState.Ready || tuning == null)
            {
                return false;
            }

            Vector3 velocity = ClubShotCalculator.CalculateLaunchVelocity(
                command.FinalDirection,
                command.BaseLaunchSpeed,
                command.LoftDegrees,
                command.EffectivePower01,
                command.CarryModifier);
            activeRollModifier = command.RollModifier;
            return command.IsPutter
                ? LaunchPutt(velocity)
                : LaunchVelocity(velocity, command.Spin);
        }

        public void ResetBall()
        {
            stopDetector.Reset();
            spinState.Reset();
            isGrounded = false;
            firstLandingResponseApplied = false;
            backSpinRollbackApplied = false;
            lastFixedVerticalVelocity = 0f;
            activeRollModifier = 1f;
            currentSurface = defaultSurface;

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

        public void SetResetPose(Vector3 position, Quaternion rotation, TerrainSurfaceData surface, bool resetNow)
        {
            resetPosition = position;
            resetRotation = rotation;
            defaultSurface = surface ?? defaultSurface;
            if (resetNow)
            {
                ResetBall();
            }
        }

        public void PrepareNextShot(Vector3 position, TerrainSurfaceData surface)
        {
            if (state == BallState.Holed)
            {
                return;
            }

            stopDetector.Reset();
            spinState.Reset();
            firstLandingResponseApplied = false;
            backSpinRollbackApplied = false;
            activeRollModifier = 1f;
            isGrounded = false;
            currentSurface = surface ?? currentSurface ?? defaultSurface;
            ballBody.useGravity = false;
            ballBody.isKinematic = true;
            ballBody.position = position;
            transform.position = position;
            ChangeState(BallState.Ready);
        }

        public void ApplyExternalAcceleration(Vector3 acceleration)
        {
            if (!ballBody.isKinematic && state is BallState.Bouncing or BallState.Rolling)
            {
                ballBody.AddForce(acceleration, ForceMode.Acceleration);
            }
        }

        public void HoleBall(Vector3 cupPosition)
        {
            stopDetector.Reset();
            spinState.Reset();
            isGrounded = false;
            ballBody.linearVelocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.useGravity = false;
            ballBody.isKinematic = true;
            ballBody.position = cupPosition;
            transform.position = cupPosition;
            ChangeState(BallState.Holed);
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
            if (!TryReadGroundContact(collision, out _, out TerrainSurfaceData surface))
            {
                return;
            }

            currentSurface = surface ?? currentSurface ?? defaultSurface;

            if (state == BallState.Airborne
                && ballBody.linearVelocity.y > tuning.MaximumUpwardLandingSpeed)
            {
                isGrounded = false;
                return;
            }

            isGrounded = true;
            stopDetector.Reset();

            if (!firstLandingResponseApplied && state == BallState.Airborne)
            {
                ApplyFirstLandingResponse();
                firstLandingResponseApplied = true;
            }

            ApplySurfaceBounceFromIncomingVelocity();

            if (state == BallState.Airborne)
            {
                ChangeState(BallState.Bouncing);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (TryReadGroundContact(collision, out _, out TerrainSurfaceData surface))
            {
                currentSurface = surface ?? currentSurface ?? defaultSurface;
                isGrounded = state != BallState.Airborne
                             || ballBody.linearVelocity.y <= tuning.MaximumUpwardLandingSpeed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TerrainSurface surface = other.GetComponentInParent<TerrainSurface>();
            if (surface != null && surface.IsHazard)
            {
                HandleHazard(surface.SurfaceType, surface.Data);
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

        private bool TryReadGroundContact(
            Collision collision,
            out ContactPoint groundContact,
            out TerrainSurfaceData surfaceData)
        {
            groundContact = default;
            TerrainSurface surface = collision.collider.GetComponentInParent<TerrainSurface>();
            surfaceData = surface != null ? surface.Data : defaultSurface;
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
            if (ballCollider.sharedMaterial != null)
            {
                ballCollider.material.bounciness = tuning.BounceRetention;
            }
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
            float surfaceDeceleration = TerrainResponse.CalculateRollingDeceleration(
                tuning.RollingDeceleration,
                currentSurface);
            surfaceDeceleration = ClubShotCalculator.ApplyRollModifier(
                surfaceDeceleration,
                activeRollModifier);
            float surfaceSpin = TerrainResponse.ApplySpinResponse(
                spinState.VerticalSpin,
                currentSurface);
            Vector3 slowedPlanarVelocity = Vector3.MoveTowards(
                planarVelocity,
                Vector3.zero,
                BallGroundResponse.CalculateRollingDeceleration(
                    surfaceDeceleration,
                    surfaceSpin,
                    tuning.TopSpinRollingDecelerationMultiplier,
                    tuning.BackSpinRollingDecelerationMultiplier) * Time.fixedDeltaTime);

            ballBody.linearVelocity = slowedPlanarVelocity + Vector3.up * velocity.y;
        }

        private void StopBall()
        {
            ballBody.linearVelocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.Sleep();
            spinState.Reset();
            ChangeState(BallState.Stopped);
        }

        private bool LaunchVelocity(Vector3 velocity, ShotSpin spin)
        {
            isGrounded = false;
            firstLandingResponseApplied = false;
            stopDetector.Reset();
            lastFixedVerticalVelocity = velocity.y;
            hasLastHazard = false;
            spinState.Set(spin);
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            launchForward = planarVelocity.sqrMagnitude > Mathf.Epsilon
                ? planarVelocity.normalized
                : Vector3.forward;
            ballBody.isKinematic = false;
            ballBody.useGravity = true;
            ballBody.WakeUp();
            ballBody.linearVelocity = velocity;
            ChangeState(BallState.Airborne);
            Launched?.Invoke();
            return true;
        }

        private bool LaunchPutt(Vector3 velocity)
        {
            isGrounded = true;
            firstLandingResponseApplied = true;
            backSpinRollbackApplied = true;
            stopDetector.Reset();
            lastFixedVerticalVelocity = 0f;
            hasLastHazard = false;
            spinState.Reset();
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            launchForward = planarVelocity.sqrMagnitude > Mathf.Epsilon
                ? planarVelocity.normalized
                : Vector3.forward;
            ballBody.isKinematic = false;
            ballBody.useGravity = true;
            ballBody.WakeUp();
            ballBody.linearVelocity = planarVelocity;
            ChangeState(BallState.Rolling);
            Launched?.Invoke();
            return true;
        }

        private void ApplyAirbornePhysics()
        {
            Vector3 acceleration = BallFlightModel.CalculateAirAcceleration(
                ballBody.linearVelocity,
                spinState.Current,
                tuning.AirDragStrength,
                tuning.SpinLiftStrength,
                tuning.SideSpinCurveStrength,
                tuning.AirForceReferenceSpeed,
                wind != null ? wind.CalculateBallAcceleration(ballBody.linearVelocity) : Vector3.zero);
            ballBody.AddForce(acceleration, ForceMode.Acceleration);
        }

        private void ApplyFirstLandingResponse()
        {
            Vector3 velocity = ballBody.linearVelocity;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            Vector3 adjustedPlanarVelocity = BallGroundResponse.CalculateFirstLandingPlanarVelocity(
                planarVelocity,
                launchForward,
                new ShotSpin(
                    TerrainResponse.ApplySpinResponse(spinState.VerticalSpin, currentSurface),
                    TerrainResponse.ApplySpinResponse(spinState.SideSpin, currentSurface)),
                tuning.TopSpinLandingBoost,
                tuning.BackSpinLandingBrake,
                0f,
                tuning.SideSpinLandingResponse);
            ballBody.linearVelocity = adjustedPlanarVelocity + Vector3.up * velocity.y;
        }

        private void ApplyBackSpinRollback()
        {
            if (backSpinRollbackApplied || spinState.VerticalSpin >= 0f)
            {
                return;
            }

            backSpinRollbackApplied = true;
            Vector3 velocity = ballBody.linearVelocity;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float forwardSpeed = Vector3.Dot(planarVelocity, launchForward);
            Vector3 lateralVelocity = planarVelocity - launchForward * forwardSpeed;
            float rollbackSpeed = -spinState.VerticalSpin * tuning.BackSpinRollbackSpeed;
            rollbackSpeed *= currentSurface != null ? currentSurface.SpinResponse : 1f;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.linearVelocity = lateralVelocity
                                      - launchForward * rollbackSpeed
                                      + Vector3.up * velocity.y;
        }

        private void ApplySurfaceBounceFromIncomingVelocity()
        {
            Vector3 velocity = ballBody.linearVelocity;
            float incomingDownwardSpeed = Mathf.Max(0f, -lastFixedVerticalVelocity);
            float baseReboundSpeed = incomingDownwardSpeed * tuning.BounceRetention;
            velocity.y = TerrainResponse.ApplyBounceModifier(baseReboundSpeed, currentSurface);
            ballBody.linearVelocity = velocity;
        }

        private void HandleHazard(TerrainSurfaceType hazard, TerrainSurfaceData surface)
        {
            if (state is BallState.Ready or BallState.Stopped)
            {
                return;
            }

            currentSurface = surface ?? currentSurface;
            lastHazard = hazard;
            hasLastHazard = true;
            stopDetector.Reset();
            spinState.Reset();
            isGrounded = false;
            ballBody.linearVelocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
            ballBody.useGravity = false;
            ballBody.isKinematic = true;
            ChangeState(BallState.Stopped);
            HazardEntered?.Invoke(hazard);
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

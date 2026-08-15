using System;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEngine;
using UnityCamera = UnityEngine.Camera;

namespace SwingPop.CameraSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityCamera))]
    public sealed class CameraDirector : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private UnityCamera controlledCamera;
        [SerializeField] private GolfBallController ball;
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private HoleFlowController holeFlow;
        [SerializeField] private CameraTuningData tuning;

        [Header("Debug")]
        [SerializeField] private bool drawCompositionGizmos = true;

        private readonly CameraModeStateMachine modeState = new();
        private CameraPose transitionStartPose;
        private Vector3 currentTarget;
        private string currentTargetName = "None";
        private Vector3 lastPlanarDirection = Vector3.forward;
        private float modeElapsed;
        private float impactShakeStrength;
        private bool hasStarted;

        public event Action<CameraMode, CameraMode> ModeChanged;

        public CameraMode CurrentMode => modeState.Current;
        public CameraMode PreviousMode => modeState.Previous;
        public bool IsTransitioning => modeState.IsTransitioning;
        public string CurrentTargetName => currentTargetName;
        public Vector3 CurrentTarget => currentTarget;
        public float CurrentFieldOfView => controlledCamera != null ? controlledCamera.fieldOfView : 0f;
        public float CurrentFollowDistance => ball != null ? Vector3.Distance(transform.position, ball.PhysicsPosition) : 0f;

        private void Awake()
        {
            controlledCamera ??= GetComponent<UnityCamera>();
        }

        private void OnEnable()
        {
            if (shotFlow != null)
            {
                shotFlow.StateChanged += OnShotFlowStateChanged;
                shotFlow.ShotCommitted += OnShotCommitted;
            }

            if (ball != null)
            {
                ball.StateChanged += OnBallStateChanged;
                ball.ResetPerformed += OnBallReset;
            }

            if (holeFlow != null)
            {
                holeFlow.StateChanged += OnHoleFlowStateChanged;
                holeFlow.HoleCompleted += OnHoleCompleted;
            }
        }

        private void Start()
        {
            if (!HasDependencies())
            {
                enabled = false;
                return;
            }

            hasStarted = true;
            BeginHoleIntro(true);
        }

        private void OnDisable()
        {
            if (shotFlow != null)
            {
                shotFlow.StateChanged -= OnShotFlowStateChanged;
                shotFlow.ShotCommitted -= OnShotCommitted;
            }

            if (ball != null)
            {
                ball.StateChanged -= OnBallStateChanged;
                ball.ResetPerformed -= OnBallReset;
            }

            if (holeFlow != null)
            {
                holeFlow.StateChanged -= OnHoleFlowStateChanged;
                holeFlow.HoleCompleted -= OnHoleCompleted;
            }
        }

        private void Update()
        {
            modeElapsed += Time.deltaTime;
            modeState.Tick(Time.deltaTime);

            switch (CurrentMode)
            {
                case CameraMode.HoleIntro when modeElapsed >= tuning.HoleIntroDuration:
                    RequestReadyCamera();
                    break;
                case CameraMode.Address when modeElapsed >= tuning.AddressHoldDuration:
                    RequestMode(IsPutterReady() ? CameraMode.Putt : CameraMode.Aim);
                    break;
                case CameraMode.Impact when modeElapsed >= tuning.ImpactHoldDuration:
                    RequestMode(IsPutterShot() ? CameraMode.Putt : CameraMode.BallFollow);
                    break;
                case CameraMode.NextShot when modeElapsed >= tuning.NextShotHoldDuration && ball.State == BallState.Ready:
                    RequestReadyCamera();
                    break;
                case CameraMode.HoleComplete when modeElapsed >= tuning.HoleCompleteHoldDuration:
                    RequestMode(CameraMode.Result);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (!HasDependencies())
            {
                return;
            }

            CameraPose desired = EvaluateDesiredPose();
            if (modeState.IsTransitioning)
            {
                ApplyPose(CameraPose.Lerp(transitionStartPose, desired, modeState.TransitionProgress));
            }
            else
            {
                float positionBlend = CameraMath.ExponentialBlend(tuning.FollowPositionSharpness, Time.deltaTime);
                float rotationBlend = CameraMath.ExponentialBlend(tuning.FollowRotationSharpness, Time.deltaTime);
                float fovBlend = CameraMath.ExponentialBlend(tuning.FollowFovSharpness, Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, desired.Position, positionBlend);
                transform.rotation = Quaternion.Slerp(transform.rotation, desired.Rotation, rotationBlend);
                controlledCamera.fieldOfView = Mathf.Lerp(controlledCamera.fieldOfView, desired.FieldOfView, fovBlend);
            }
        }

        public void SkipIntro()
        {
            if (CurrentMode == CameraMode.HoleIntro)
            {
                RequestReadyCamera();
            }
        }

        public bool RequestDebugMode(CameraMode mode)
        {
            return RequestMode(mode);
        }

        private bool HasDependencies()
        {
            if (controlledCamera != null && ball != null && shotFlow != null && holeFlow != null && tuning != null)
            {
                return true;
            }

            Debug.LogError("CameraDirector requires Camera, Ball, ShotFlow, HoleFlow, and CameraTuningData.", this);
            return false;
        }

        private void BeginHoleIntro(bool snap)
        {
            modeElapsed = 0f;
            impactShakeStrength = 0f;
            modeState.Reset(CameraMode.HoleIntro);
            CameraPose pose = EvaluateDesiredPose();
            if (snap)
            {
                ApplyPose(pose);
            }
        }

        private void RequestReadyCamera()
        {
            RequestMode(IsPutterReady() ? CameraMode.Putt : CameraMode.Address);
        }

        private bool RequestMode(CameraMode next)
        {
            if (tuning == null || next == CurrentMode)
            {
                return false;
            }

            CameraMode previous = CurrentMode;
            transitionStartPose = ReadCurrentPose();
            if (!modeState.Request(next, tuning.GetTransitionDuration(next)))
            {
                return false;
            }

            modeElapsed = 0f;
            ModeChanged?.Invoke(previous, next);
            return true;
        }

        private void OnShotFlowStateChanged(ShotFlowState previous, ShotFlowState next)
        {
            if (!hasStarted || CurrentMode is CameraMode.HoleIntro or CameraMode.NextShot or CameraMode.HoleComplete or CameraMode.Result)
            {
                return;
            }

            if (next is ShotFlowState.PowerSelecting or ShotFlowState.ImpactSelecting)
            {
                RequestMode(CameraMode.Swing);
            }
            else if (next == ShotFlowState.Aiming && ball.State == BallState.Ready)
            {
                RequestMode(IsPutterReady() ? CameraMode.Putt : CameraMode.Aim);
            }
        }

        private void OnShotCommitted(ShotCommand command)
        {
            impactShakeStrength = command.ImpactGrade == ImpactGrade.Perfect
                ? tuning.PerfectImpactShake
                : tuning.NormalImpactShake;
            RequestMode(CameraMode.Impact);
        }

        private void OnBallStateChanged(BallState previous, BallState next)
        {
            if (!hasStarted || CurrentMode is CameraMode.HoleComplete or CameraMode.Result)
            {
                return;
            }

            switch (next)
            {
                case BallState.Airborne when CurrentMode != CameraMode.Impact
                                             && shotFlow.State == ShotFlowState.ShotCommitted:
                    RequestMode(CameraMode.BallFollow);
                    break;
                case BallState.Bouncing:
                    RequestMode(CameraMode.Landing);
                    break;
                case BallState.Rolling when CurrentMode == CameraMode.Putt || IsPutterReady() || IsPutterShot():
                    RequestMode(CameraMode.Putt);
                    break;
                case BallState.Rolling:
                    RequestMode(CameraMode.Landing);
                    break;
                case BallState.Stopped:
                    RequestMode(CameraMode.NextShot);
                    break;
                case BallState.Holed:
                    RequestMode(CameraMode.HoleComplete);
                    break;
            }
        }

        private void OnBallReset()
        {
            if (hasStarted && holeFlow.State == HoleFlowState.Playing)
            {
                BeginHoleIntro(false);
            }
        }

        private void OnHoleFlowStateChanged(HoleFlowState previous, HoleFlowState next)
        {
            if (hasStarted && next == HoleFlowState.HoleComplete)
            {
                RequestMode(CameraMode.HoleComplete);
            }
        }

        private void OnHoleCompleted(ScoreResult result)
        {
            RequestMode(CameraMode.HoleComplete);
        }

        private CameraPose EvaluateDesiredPose()
        {
            Vector3 ballPosition = ball.PhysicsPosition;
            Vector3 cupPosition = holeFlow.Hole.CupPosition;
            Vector3 aimForward = CameraMath.ResolvePlanarForward(shotFlow.AimDirection, lastPlanarDirection);
            Vector3 velocityForward = CameraMath.ResolvePlanarForward(ball.Velocity, ball.LaunchForward);
            if (ball.Speed > 0.2f)
            {
                lastPlanarDirection = velocityForward;
            }
            CameraFraming framing = CameraFramingSolver.Resolve(
                CurrentMode,
                tuning,
                ballPosition,
                ball.ResetPosition,
                cupPosition,
                aimForward,
                ball.Velocity,
                ball.LaunchForward,
                lastPlanarDirection,
                ball.Speed,
                modeElapsed);
            currentTarget = framing.Target;
            currentTargetName = framing.TargetName;
            Vector3 position = ResolveCollision(currentTarget, framing.Position);
            Quaternion rotation = CameraMath.LookRotation(position, currentTarget, transform.rotation);
            ApplyShake(ref position, ref rotation);
            return new CameraPose(position, rotation, framing.FieldOfView);
        }

        private Vector3 ResolveCollision(Vector3 target, Vector3 desired)
        {
            Vector3 direction = desired - target;
            float distance = direction.magnitude;
            if (distance <= tuning.MinimumTargetDistance)
            {
                return desired;
            }

            direction /= distance;
            if (Physics.SphereCast(target, tuning.CollisionRadius, direction, out RaycastHit hit,
                    distance, tuning.CollisionLayers, QueryTriggerInteraction.Ignore))
            {
                float safeDistance = Mathf.Max(tuning.MinimumTargetDistance, hit.distance - tuning.CollisionPadding);
                return target + direction * safeDistance;
            }

            return desired;
        }

        private void ApplyShake(ref Vector3 position, ref Quaternion rotation)
        {
            float strength = CurrentMode == CameraMode.Impact
                ? impactShakeStrength * (1f - Mathf.Clamp01(modeElapsed / tuning.ImpactHoldDuration))
                : CurrentMode == CameraMode.HoleComplete ? tuning.HoleCompleteShake : 0f;
            if (strength <= 0f)
            {
                return;
            }

            float phase = Time.unscaledTime * tuning.ImpactShakeFrequency;
            Vector3 localShake = new(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f), 0f);
            position += transform.TransformDirection(localShake) * strength;
            rotation *= Quaternion.Euler(localShake.y * strength * 9f, localShake.x * strength * 9f, 0f);
        }

        private CameraPose ReadCurrentPose()
        {
            return new CameraPose(transform.position, transform.rotation, controlledCamera.fieldOfView);
        }

        private void ApplyPose(CameraPose pose)
        {
            transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            controlledCamera.fieldOfView = pose.FieldOfView;
        }

        private bool IsPutterReady()
        {
            return shotFlow.CurrentClub != null && shotFlow.CurrentClub.IsPutter;
        }

        private bool IsPutterShot()
        {
            return shotFlow.HasLastShotCommand && shotFlow.LastShotCommand.IsPutter;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawCompositionGizmos || ball == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTarget);
            Gizmos.DrawWireSphere(currentTarget, 0.25f);
            if (holeFlow != null && holeFlow.Hole != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(ball.PhysicsPosition, holeFlow.Hole.CupPosition);
                Gizmos.DrawWireSphere(holeFlow.Hole.CupPosition, 0.35f);
            }
        }
    }
}

using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.CharacterSystem
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class CharacterGolfController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GolfBallController ball;
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private HoleFlowController holeFlow;
        [SerializeField] private CharacterAnimationController animationController;
        [SerializeField] private CharacterPresentation presentation;
        [SerializeField] private CharacterTuningData tuning;

        private Vector3 desiredPosition;
        private Quaternion desiredRotation = Quaternion.identity;
        private bool hasPoseTarget;
        private bool currentShotIsPutter;
        private bool impactEventFired;

        public CharacterState CurrentState => animationController != null
            ? animationController.State
            : CharacterState.Idle;
        public string AnimationState => CurrentState.ToString();
        public bool ImpactEventFired => impactEventFired;
        public string CurrentClubVisual => presentation != null ? presentation.CurrentClubVisual : "None";
        public float CharacterAimAngle => Vector3.SignedAngle(Vector3.forward, desiredRotation * Vector3.forward, Vector3.up);

        private void OnEnable()
        {
            if (shotFlow != null)
            {
                shotFlow.StateChanged += OnShotStateChanged;
                shotFlow.ShotCommitted += OnShotCommitted;
                shotFlow.ClubChanged += OnClubChanged;
            }
            if (ball != null)
            {
                ball.Launched += OnBallLaunched;
                ball.StateChanged += OnBallStateChanged;
                ball.ResetPerformed += OnBallReset;
            }
            if (holeFlow != null)
            {
                holeFlow.HoleCompleted += OnHoleCompleted;
            }
            if (animationController != null)
            {
                animationController.ImpactReached += OnAnimationImpactReached;
                animationController.StateFinished += OnAnimationStateFinished;
            }
        }

        private void Start()
        {
            if (!HasDependencies())
            {
                enabled = false;
                return;
            }

            shotFlow.ConfigureImpactTiming(true, tuning.FallbackImpactDelay);
            presentation.ConfigureClubSocket(tuning.ClubSocketOffset);
            PrepareAddress(true);
        }

        private void Update()
        {
            if (!hasPoseTarget || tuning == null)
            {
                return;
            }

            if (CurrentState is CharacterState.Address or CharacterState.PuttAddress
                or CharacterState.BackSwing or CharacterState.PuttBackSwing)
            {
                UpdateAddressTarget();
            }
            else if (CurrentState == CharacterState.WatchBall && ball.State is not BallState.Ready and not BallState.Stopped)
            {
                Vector3 watchDirection = ball.PhysicsPosition - transform.position;
                desiredRotation = CharacterPlacementCalculator.CalculateAimRotation(watchDirection, desiredRotation);
            }

            float positionBlend = 1f - Mathf.Exp(-tuning.PositionSharpness * Time.deltaTime);
            float rotationBlend = 1f - Mathf.Exp(-tuning.RotationSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionBlend);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
        }

        private void OnDisable()
        {
            if (shotFlow != null)
            {
                shotFlow.StateChanged -= OnShotStateChanged;
                shotFlow.ShotCommitted -= OnShotCommitted;
                shotFlow.ClubChanged -= OnClubChanged;
                shotFlow.ConfigureImpactTiming(false, 0.01f);
            }
            if (ball != null)
            {
                ball.Launched -= OnBallLaunched;
                ball.StateChanged -= OnBallStateChanged;
                ball.ResetPerformed -= OnBallReset;
            }
            if (holeFlow != null)
            {
                holeFlow.HoleCompleted -= OnHoleCompleted;
            }
            if (animationController != null)
            {
                animationController.ImpactReached -= OnAnimationImpactReached;
                animationController.StateFinished -= OnAnimationStateFinished;
            }
        }

        private bool HasDependencies()
        {
            if (ball != null && shotFlow != null && holeFlow != null
                && animationController != null && presentation != null && tuning != null)
            {
                return true;
            }

            Debug.LogError(
                "CharacterGolfController requires Ball, ShotFlow, HoleFlow, CharacterAnimationController, CharacterPresentation, and CharacterTuningData.",
                this);
            return false;
        }

        private void OnShotStateChanged(ShotFlowState previous, ShotFlowState next)
        {
            bool isPutter = shotFlow.CurrentClub != null && shotFlow.CurrentClub.IsPutter;
            switch (next)
            {
                case ShotFlowState.Aiming:
                case ShotFlowState.PowerSelecting:
                    PrepareAddress(false);
                    break;
                case ShotFlowState.ImpactSelecting:
                    UpdateAddressTarget();
                    animationController.PlayBackSwing(isPutter);
                    break;
            }
        }

        private void OnShotCommitted(ShotCommand command)
        {
            currentShotIsPutter = command.IsPutter;
            impactEventFired = false;
            presentation.SetClub(command.ClubType);
            animationController.PlaySwing(currentShotIsPutter);
        }

        private void OnClubChanged(ClubData club)
        {
            if (ball != null && ball.State == BallState.Ready && holeFlow.State == HoleFlowState.Playing)
            {
                PrepareAddress(false);
            }
        }

        private void OnAnimationImpactReached()
        {
            if (impactEventFired)
            {
                return;
            }

            impactEventFired = shotFlow.TryLaunchCommittedShot();
        }

        private void OnBallLaunched()
        {
            animationController.PlayFollowThrough(currentShotIsPutter);
        }

        private void OnAnimationStateFinished(CharacterState finishedState)
        {
            if (finishedState is CharacterState.FollowThrough or CharacterState.PuttFollowThrough)
            {
                animationController.PlayWatchBall();
            }
        }

        private void OnBallStateChanged(BallState previous, BallState next)
        {
            if (next == BallState.Ready && holeFlow.State == HoleFlowState.Playing)
            {
                PrepareAddress(false);
            }
        }

        private void OnBallReset()
        {
            impactEventFired = false;
            PrepareAddress(true);
        }

        private void OnHoleCompleted(ScoreResult result)
        {
            animationController.PlayCelebration(CharacterFlowResolver.ResolveCelebration(result));
        }

        private void PrepareAddress(bool snap)
        {
            if (ball == null || shotFlow == null || presentation == null || animationController == null || tuning == null)
            {
                return;
            }

            currentShotIsPutter = shotFlow.CurrentClub != null && shotFlow.CurrentClub.IsPutter;
            presentation.SetClub(currentShotIsPutter ? ClubType.Putter : ClubType.Driver);
            UpdateAddressTarget();
            animationController.PlayAddress(currentShotIsPutter);
            if (snap)
            {
                transform.SetPositionAndRotation(desiredPosition, desiredRotation);
            }
            hasPoseTarget = true;
        }

        private void UpdateAddressTarget()
        {
            Vector3 aimDirection = shotFlow.AimDirection;
            desiredPosition = CharacterPlacementCalculator.CalculateAddressPosition(
                ball.PhysicsPosition,
                aimDirection,
                tuning.AddressLateralOffset,
                tuning.AddressBackwardOffset,
                tuning.AddressHeightOffset);
            desiredRotation = CharacterPlacementCalculator.CalculateAimRotation(aimDirection, desiredRotation);
        }
    }
}

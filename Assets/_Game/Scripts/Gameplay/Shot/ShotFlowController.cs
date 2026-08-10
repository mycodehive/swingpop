using System;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using UnityEngine;

namespace SwingPop.Gameplay.Shot
{
    public sealed class ShotFlowController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GolfBallController ball;
        [SerializeField] private BallTuningData ballTuning;
        [SerializeField] private ShotTuningData shotTuning;
        [SerializeField] private Transform aimDirectionReference;

        private ShotFlowState state = ShotFlowState.Preparing;
        private Quaternion baseAimRotation;
        private Vector3 baseAimForward = Vector3.forward;
        private float aimInput;
        private float aimAngleDegrees;
        private float power01;
        private float confirmedPower01;
        private float impactCursor = -1f;
        private float gaugeElapsedSeconds;
        private ShotCommand lastShotCommand;
        private bool hasLastShotCommand;
        private SpinPreset selectedSpinPreset;

        public event Action<ShotFlowState, ShotFlowState> StateChanged;
        public event Action<ShotCommand> ShotCommitted;

        public ShotFlowState State => state;
        public float AimAngleDegrees => aimAngleDegrees;
        public float Power01 => state == ShotFlowState.PowerSelecting ? power01 : confirmedPower01;
        public float ImpactCursor => impactCursor;
        public ImpactGrade PreviewImpactGrade => EvaluateCurrentImpactGrade();
        public bool HasLastShotCommand => hasLastShotCommand;
        public ShotCommand LastShotCommand => lastShotCommand;
        public Vector3 AimDirection => Quaternion.AngleAxis(aimAngleDegrees, Vector3.up) * baseAimForward;
        public SpinPreset SelectedSpinPreset => selectedSpinPreset;
        public ShotSpin SelectedSpin => ShotSpin.FromPreset(selectedSpinPreset);

        private void Awake()
        {
            if (aimDirectionReference != null)
            {
                baseAimRotation = aimDirectionReference.rotation;
                baseAimForward = Vector3.ProjectOnPlane(aimDirectionReference.forward, Vector3.up).normalized;
            }
        }

        private void OnEnable()
        {
            if (ball != null)
            {
                ball.ResetPerformed += OnBallReset;
            }
        }

        private void Start()
        {
            BeginAiming();
        }

        private void Update()
        {
            if (shotTuning == null)
            {
                return;
            }

            switch (state)
            {
                case ShotFlowState.Aiming:
                    UpdateAim(Time.deltaTime);
                    break;
                case ShotFlowState.PowerSelecting:
                    gaugeElapsedSeconds += Time.deltaTime;
                    power01 = ShotCalculator.EvaluatePingPong01(
                        gaugeElapsedSeconds,
                        shotTuning.PowerSweepSpeed);
                    break;
                case ShotFlowState.ImpactSelecting:
                    gaugeElapsedSeconds += Time.deltaTime;
                    impactCursor = ShotCalculator.EvaluateImpactCursor(
                        gaugeElapsedSeconds,
                        shotTuning.ImpactSweepSpeed);
                    break;
            }
        }

        private void OnDisable()
        {
            if (ball != null)
            {
                ball.ResetPerformed -= OnBallReset;
            }
        }

        public void SetAimInput(float horizontalInput)
        {
            aimInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        }

        public void ConfirmCurrentStep()
        {
            switch (state)
            {
                case ShotFlowState.Aiming:
                    gaugeElapsedSeconds = 0f;
                    power01 = 0f;
                    ChangeState(ShotFlowState.PowerSelecting);
                    break;
                case ShotFlowState.PowerSelecting:
                    confirmedPower01 = Mathf.Clamp01(power01);
                    gaugeElapsedSeconds = 0f;
                    impactCursor = -1f;
                    ChangeState(ShotFlowState.ImpactSelecting);
                    break;
                case ShotFlowState.ImpactSelecting:
                    CommitShot();
                    break;
            }
        }

        public void CancelToAiming()
        {
            if (state is ShotFlowState.PowerSelecting or ShotFlowState.ImpactSelecting)
            {
                BeginAiming();
            }
        }

        public void ForcePerfectImpactAndCommit()
        {
            if (state != ShotFlowState.ImpactSelecting)
            {
                return;
            }

            impactCursor = 0f;
            CommitShot();
        }

        public void SetSpinPreset(SpinPreset preset)
        {
            if (ball == null || ball.State != BallState.Ready)
            {
                return;
            }

            selectedSpinPreset = preset;
        }

        public void ResetShot()
        {
            if (ball != null)
            {
                ball.ResetBall();
            }
            else
            {
                BeginAiming();
            }
        }

        private void UpdateAim(float deltaTime)
        {
            aimAngleDegrees = ShotCalculator.ClampAimAngle(
                aimAngleDegrees + aimInput * shotTuning.AimRotationSpeed * deltaTime,
                shotTuning.MinimumAimAngle,
                shotTuning.MaximumAimAngle);
            ApplyAimRotation();
        }

        private void CommitShot()
        {
            if (ball == null || ballTuning == null || shotTuning == null)
            {
                return;
            }

            ShotCommand command = ShotCalculator.CreateCommand(
                baseAimForward,
                aimAngleDegrees,
                confirmedPower01,
                impactCursor,
                shotTuning.PerfectMaximumOffset,
                shotTuning.GreatMaximumOffset,
                shotTuning.GoodMaximumOffset,
                shotTuning.PerfectPowerMultiplier,
                shotTuning.GreatPowerMultiplier,
                shotTuning.GoodPowerMultiplier,
                shotTuning.MissPowerMultiplier,
                shotTuning.GreatDispersionDegrees,
                shotTuning.GoodDispersionDegrees,
                shotTuning.MissDispersionDegrees,
                ballTuning.LaunchSpeed,
                ballTuning.LaunchAngleDegrees,
                SelectedSpin);

            float surfacePowerModifier = ball.CurrentSurfaceData != null
                ? ball.CurrentSurfaceData.PowerModifier
                : 1f;
            command = ShotCalculator.ApplySurfacePowerModifier(command, surfacePowerModifier);

            if (!ball.Launch(command))
            {
                return;
            }

            lastShotCommand = command;
            hasLastShotCommand = true;
            ChangeState(ShotFlowState.ShotCommitted);
            ShotCommitted?.Invoke(command);
        }

        private ImpactGrade EvaluateCurrentImpactGrade()
        {
            if (shotTuning == null)
            {
                return ImpactGrade.Miss;
            }

            return ShotCalculator.ClassifyImpact(
                impactCursor,
                shotTuning.PerfectMaximumOffset,
                shotTuning.GreatMaximumOffset,
                shotTuning.GoodMaximumOffset);
        }

        private void OnBallReset()
        {
            BeginAiming();
        }

        private void BeginAiming()
        {
            aimInput = 0f;
            aimAngleDegrees = 0f;
            power01 = 0f;
            confirmedPower01 = 0f;
            impactCursor = -1f;
            gaugeElapsedSeconds = 0f;
            ApplyAimRotation();
            ChangeState(ShotFlowState.Aiming);
        }

        private void ApplyAimRotation()
        {
            if (aimDirectionReference != null)
            {
                aimDirectionReference.rotation = baseAimRotation * Quaternion.Euler(0f, aimAngleDegrees, 0f);
            }
        }

        private void ChangeState(ShotFlowState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            ShotFlowState previousState = state;
            state = nextState;
            StateChanged?.Invoke(previousState, nextState);
        }
    }
}

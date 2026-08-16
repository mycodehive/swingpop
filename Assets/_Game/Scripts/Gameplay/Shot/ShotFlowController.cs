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
        [SerializeField] private ClubData defaultClub;

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
        private ClubData currentClub;
        private readonly ShotImpactDelayGuard fallbackImpactGuard = new();
        private ShotCommand pendingShotCommand;
        private bool hasPendingBallLaunch;
        private bool deferBallLaunchUntilImpact;
        private float fallbackImpactDelay = 0.45f;
        private bool lastBallLaunchUsedFallback;
        private int lastConfirmFrame = -1;

        public event Action<ShotFlowState, ShotFlowState> StateChanged;
        public event Action<ShotCommand> ShotCommitted;
        public event Action<ClubData> ClubChanged;
        public event Action<SpinPreset> SpinChanged;
        public event Action DebugResetRequested;

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
        public ClubData CurrentClub => currentClub != null ? currentClub : defaultClub;
        public ShotTuningData Tuning => shotTuning;
        public bool HasPendingBallLaunch => hasPendingBallLaunch;
        public bool LastBallLaunchUsedFallback => lastBallLaunchUsedFallback;

        private void Awake()
        {
            currentClub = defaultClub;
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
                case ShotFlowState.ShotCommitted when hasPendingBallLaunch && deferBallLaunchUntilImpact:
                    if (fallbackImpactGuard.Tick(Time.deltaTime))
                    {
                        lastBallLaunchUsedFallback = true;
                        Debug.LogWarning(
                            $"Shot impact signal did not arrive within {fallbackImpactDelay:F2}s. Launching through the M7 fallback path.",
                            this);
                        TryLaunchCommittedShot();
                    }
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
            if (lastConfirmFrame == Time.frameCount)
            {
                return;
            }
            lastConfirmFrame = Time.frameCount;

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

        public bool TryCommitShot(float selectedPower01, float selectedImpactOffset)
        {
            if (state != ShotFlowState.Aiming || ball == null || ball.State != BallState.Ready)
            {
                return false;
            }

            confirmedPower01 = Mathf.Clamp01(selectedPower01);
            impactCursor = Mathf.Clamp(selectedImpactOffset, -1f, 1f);
            return CommitShot();
        }

        public void SetSpinPreset(SpinPreset preset)
        {
            if (ball == null || ball.State != BallState.Ready)
            {
                return;
            }

            SpinPreset nextPreset = CurrentClub != null && CurrentClub.IsPutter
                ? SpinPreset.NoSpin
                : preset;
            if (selectedSpinPreset == nextPreset)
            {
                return;
            }

            selectedSpinPreset = nextPreset;
            SpinChanged?.Invoke(selectedSpinPreset);
        }

        public void ResetShot()
        {
            if (DebugResetRequested != null)
            {
                DebugResetRequested.Invoke();
            }
            else if (ball != null)
            {
                ball.ResetBall();
            }
            else
            {
                BeginAiming();
            }
        }

        public void PrepareNextShot(Vector3 defaultDirection, ClubData club)
        {
            SetClub(club);
            Vector3 planarDirection = Vector3.ProjectOnPlane(defaultDirection, Vector3.up).normalized;
            if (planarDirection.sqrMagnitude > Mathf.Epsilon)
            {
                baseAimForward = planarDirection;
                baseAimRotation = Quaternion.LookRotation(baseAimForward, Vector3.up);
            }
            BeginAiming();
        }

        public void SetClub(ClubData club)
        {
            ClubData nextClub = club != null ? club : defaultClub;
            bool changed = currentClub != nextClub;
            SpinPreset previousSpin = selectedSpinPreset;
            currentClub = nextClub;
            if (currentClub != null && currentClub.IsPutter)
            {
                selectedSpinPreset = SpinPreset.NoSpin;
            }
            if (changed)
            {
                ClubChanged?.Invoke(currentClub);
            }
            if (previousSpin != selectedSpinPreset)
            {
                SpinChanged?.Invoke(selectedSpinPreset);
            }
        }

        /// <summary>
        /// Presentation adapters can defer launch until their impact marker. Without an adapter,
        /// commits retain the M1-M6 immediate-launch behavior.
        /// </summary>
        public void ConfigureImpactTiming(bool deferUntilImpact, float fallbackDelaySeconds)
        {
            deferBallLaunchUntilImpact = deferUntilImpact;
            fallbackImpactDelay = Mathf.Max(0.01f, fallbackDelaySeconds);
            if (!deferBallLaunchUntilImpact && hasPendingBallLaunch)
            {
                TryLaunchCommittedShot();
            }
        }

        public bool TryLaunchCommittedShot()
        {
            if (!hasPendingBallLaunch || ball == null || state != ShotFlowState.ShotCommitted)
            {
                return false;
            }

            ShotCommand command = pendingShotCommand;
            hasPendingBallLaunch = false;
            if (ball.Launch(command))
            {
                return true;
            }

            hasPendingBallLaunch = true;
            return false;
        }

        private void UpdateAim(float deltaTime)
        {
            aimAngleDegrees = ShotCalculator.ClampAimAngle(
                aimAngleDegrees + aimInput * shotTuning.AimRotationSpeed * deltaTime,
                shotTuning.MinimumAimAngle,
                shotTuning.MaximumAimAngle);
            ApplyAimRotation();
        }

        private bool CommitShot()
        {
            if (ball == null || ball.State != BallState.Ready || ballTuning == null || shotTuning == null)
            {
                return false;
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
            command = ShotCalculator.ApplyClub(command, CurrentClub);

            lastShotCommand = command;
            hasLastShotCommand = true;
            pendingShotCommand = command;
            hasPendingBallLaunch = true;
            fallbackImpactGuard.Begin(fallbackImpactDelay);
            lastBallLaunchUsedFallback = false;
            ChangeState(ShotFlowState.ShotCommitted);
            ShotCommitted?.Invoke(command);
            return deferBallLaunchUntilImpact || TryLaunchCommittedShot();
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
            hasPendingBallLaunch = false;
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

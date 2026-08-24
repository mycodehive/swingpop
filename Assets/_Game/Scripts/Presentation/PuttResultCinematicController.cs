using System;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.UI;
using UnityEngine;

namespace SwingPop.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PuttResultCinematicController : MonoBehaviour
    {
        [Header("Gameplay Sources (Read Only)")]
        [SerializeField] private GolfBallController ball;
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private HoleFlowController holeFlow;

        [Header("Presentation Targets")]
        [SerializeField] private CameraDirector cameraDirector;
        [SerializeField] private CharacterGolfController character;
        [SerializeField] private GameplayHudPresenter hud;
        [SerializeField] private GameplayAudioController audioController;
        [SerializeField] private PuttResultCinematicTuningData tuning;

        private PuttResultCinematicPhase phase;
        private float holeElapsed;
        private ScoreResult result;
        private bool cinematicStarted;
        private bool characterRequested;
        private bool resultRequested;

        public event Action<PuttResultCinematicPhase, PuttResultCinematicPhase> PhaseChanged;

        public PuttResultCinematicPhase Phase => phase;
        public int StartCount { get; private set; }
        public int CharacterReactionCount { get; private set; }
        public int ResultRevealCount { get; private set; }
        public bool IsApproaching => phase == PuttResultCinematicPhase.CupApproach;
        public PuttResultCinematicTuningData Tuning => tuning;
        public bool IsConfigured => ball != null && shotFlow != null && holeFlow != null
                                    && cameraDirector != null && character != null && hud != null
                                    && audioController != null && tuning != null;

        private void OnEnable()
        {
            if (ball != null)
            {
                ball.StateChanged += OnBallStateChanged;
                ball.ResetPerformed += OnBallReset;
            }
            if (shotFlow != null)
            {
                shotFlow.ClubChanged += OnClubChanged;
            }
            if (holeFlow != null)
            {
                holeFlow.HoleCompleted += OnHoleCompleted;
            }
        }

        private void Start()
        {
            RefreshPuttReady();
        }

        private void Update()
        {
            if (tuning == null || ball == null || holeFlow == null)
            {
                return;
            }

            if (phase == PuttResultCinematicPhase.PuttRolling)
            {
                float distance = Vector3.ProjectOnPlane(
                    holeFlow.Hole.CupPosition - ball.PhysicsPosition,
                    Vector3.up).magnitude;
                if (distance <= tuning.ApproachDistance)
                {
                    cameraDirector?.SetCupApproachPresentation(true);
                    SetPhase(PuttResultCinematicPhase.CupApproach);
                }
            }

            if (!cinematicStarted)
            {
                return;
            }

            holeElapsed += Time.deltaTime;
            if (!characterRequested && holeElapsed >= tuning.CelebrationDelay)
            {
                characterRequested = true;
                CharacterReactionCount++;
                character?.PlayCelebrationForResult(result);
                cameraDirector?.RequestPresentationMode(CameraMode.Result);
                SetPhase(PuttResultCinematicPhase.CharacterReaction);
            }

            if (!resultRequested && holeElapsed >= tuning.ResultRevealDelay)
            {
                resultRequested = true;
                ResultRevealCount++;
                hud?.ShowHoleResult(result);
                audioController?.PlayResultCue(holeFlow.Hole.CupPosition);
                cameraDirector?.RequestPresentationMode(CameraMode.Result);
                SetPhase(PuttResultCinematicPhase.ResultReveal);
            }
            else if (resultRequested && phase == PuttResultCinematicPhase.ResultReveal
                     && holeElapsed >= tuning.ResultRevealDelay + tuning.ResultFrameDuration)
            {
                SetPhase(PuttResultCinematicPhase.ResultHold);
            }
        }

        private void OnDisable()
        {
            if (ball != null)
            {
                ball.StateChanged -= OnBallStateChanged;
                ball.ResetPerformed -= OnBallReset;
            }
            if (shotFlow != null)
            {
                shotFlow.ClubChanged -= OnClubChanged;
            }
            if (holeFlow != null)
            {
                holeFlow.HoleCompleted -= OnHoleCompleted;
            }
        }

        private void OnClubChanged(SwingPop.Data.ClubData club)
        {
            RefreshPuttReady();
        }

        private void OnBallStateChanged(BallState previous, BallState next)
        {
            if (next == BallState.Rolling && IsLastShotPutter())
            {
                cameraDirector?.SetCupApproachPresentation(false);
                SetPhase(PuttResultCinematicPhase.PuttRolling);
            }
            else if (next == BallState.Ready)
            {
                RefreshPuttReady();
            }
            else if (next == BallState.Holed)
            {
                cameraDirector?.SetCupApproachPresentation(false);
            }
        }

        private void OnHoleCompleted(ScoreResult completedResult)
        {
            BeginHoleInSequence(completedResult);
        }

        public void PreviewPuttReady()
        {
            cameraDirector?.SetCupApproachPresentation(false);
            cameraDirector?.RequestPresentationMode(CameraMode.Putt);
            SetPhase(PuttResultCinematicPhase.PuttReady);
        }

        public void PreviewCupApproach()
        {
            cameraDirector?.SetCupApproachPresentation(true);
            cameraDirector?.RequestPresentationMode(CameraMode.Putt);
            SetPhase(PuttResultCinematicPhase.CupApproach);
        }

        public void PreviewHoleInSequence(ScoreResult previewResult)
        {
            BeginHoleInSequence(previewResult);
        }

        private void BeginHoleInSequence(ScoreResult completedResult)
        {
            if (cinematicStarted)
            {
                return;
            }

            cinematicStarted = true;
            result = completedResult;
            holeElapsed = 0f;
            characterRequested = false;
            resultRequested = false;
            StartCount++;
            cameraDirector?.SetCupApproachPresentation(false);
            cameraDirector?.RequestPresentationMode(CameraMode.HoleComplete);
            audioController?.PlayHoleInCue(holeFlow.Hole.CupPosition);
            SetPhase(PuttResultCinematicPhase.HoleInMoment);
        }

        private void OnBallReset()
        {
            cinematicStarted = false;
            holeElapsed = 0f;
            characterRequested = false;
            resultRequested = false;
            cameraDirector?.SetCupApproachPresentation(false);
            SetPhase(PuttResultCinematicPhase.Idle);
            RefreshPuttReady();
        }

        private void RefreshPuttReady()
        {
            if (!cinematicStarted && shotFlow != null && ball != null
                && shotFlow.CurrentClub != null && shotFlow.CurrentClub.IsPutter
                && ball.State == BallState.Ready)
            {
                SetPhase(PuttResultCinematicPhase.PuttReady);
            }
        }

        private bool IsLastShotPutter()
        {
            return shotFlow != null && shotFlow.HasLastShotCommand && shotFlow.LastShotCommand.IsPutter;
        }

        private void SetPhase(PuttResultCinematicPhase next)
        {
            if (phase == next)
            {
                return;
            }
            PuttResultCinematicPhase previous = phase;
            phase = next;
            PhaseChanged?.Invoke(previous, next);
        }
    }
}

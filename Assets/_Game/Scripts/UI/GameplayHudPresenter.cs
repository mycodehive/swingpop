using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
using UnityEngine;

namespace SwingPop.UI
{
    public sealed class GameplayHudPresenter : MonoBehaviour
    {
        [Header("Gameplay Sources")]
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private GolfBallController ball;
        [SerializeField] private WindController wind;
        [SerializeField] private HoleFlowController holeFlow;

        [Header("Presentation")]
        [SerializeField] private GameplayHudView view;
        [SerializeField] private HudTuningData tuning;
        [SerializeField] private PuttResultCinematicTuningData cinematicTuning;
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private string playerName = "PLAYER";

        private int displayedDistanceTenths = int.MinValue;
        private int displayedHeightTenths = int.MinValue;
        private ShotCommand pendingImpactCommand;
        private bool hasPendingImpactFeedback;

        public GameplayHudView View => view;
        public PuttResultCinematicTuningData CinematicTuning => cinematicTuning;

        private void OnEnable()
        {
            if (shotFlow != null)
            {
                shotFlow.StateChanged += OnShotStateChanged;
                shotFlow.ShotCommitted += OnShotCommitted;
                shotFlow.ClubChanged += OnClubChanged;
                shotFlow.SpinChanged += OnSpinChanged;
            }
            if (ball != null)
            {
                ball.StateChanged += OnBallStateChanged;
                ball.Launched += OnBallLaunched;
                ball.HazardEntered += OnHazardEntered;
                ball.ResetPerformed += OnBallReset;
            }
            if (wind != null)
            {
                wind.WindChanged += OnWindChanged;
            }
            if (holeFlow != null)
            {
                holeFlow.StateChanged += OnHoleStateChanged;
                holeFlow.StrokeChanged += OnStrokeChanged;
                holeFlow.HoleCompleted += OnHoleCompleted;
            }
            if (view != null && view.ActionButton != null)
            {
                view.ActionButton.onClick.AddListener(OnPrimaryActionClicked);
            }
        }

        private void Start()
        {
            if (shotFlow != null && shotFlow.Tuning != null && view != null && view.GaugeView != null)
            {
                view.GaugeView.ConfigureImpactZones(
                    shotFlow.Tuning.PerfectMaximumOffset,
                    shotFlow.Tuning.GreatMaximumOffset,
                    shotFlow.Tuning.GoodMaximumOffset);
            }

            RefreshAll();
        }

        private void Update()
        {
            if (view == null)
            {
                return;
            }

            if (shotFlow != null && view.GaugeView != null)
            {
                if (shotFlow.State == ShotFlowState.PowerSelecting)
                {
                    view.GaugeView.SetPower(shotFlow.Power01);
                }
                else if (shotFlow.State == ShotFlowState.ImpactSelecting)
                {
                    view.GaugeView.SetImpact(shotFlow.ImpactCursor, shotFlow.PreviewImpactGrade);
                }
            }

            RefreshAimData(false);
            UpdateAimMarker();

            float breathingScale = tuning != null ? tuning.ButtonBreathingScale : 0.025f;
            float breathingSpeed = tuning != null ? tuning.ButtonBreathingSpeed : 2.2f;
            view.Tick(Time.unscaledDeltaTime, Time.unscaledTime, breathingScale, breathingSpeed);
        }

        private void OnDisable()
        {
            if (shotFlow != null)
            {
                shotFlow.StateChanged -= OnShotStateChanged;
                shotFlow.ShotCommitted -= OnShotCommitted;
                shotFlow.ClubChanged -= OnClubChanged;
                shotFlow.SpinChanged -= OnSpinChanged;
            }
            if (ball != null)
            {
                ball.StateChanged -= OnBallStateChanged;
                ball.Launched -= OnBallLaunched;
                ball.HazardEntered -= OnHazardEntered;
                ball.ResetPerformed -= OnBallReset;
            }
            if (wind != null)
            {
                wind.WindChanged -= OnWindChanged;
            }
            if (holeFlow != null)
            {
                holeFlow.StateChanged -= OnHoleStateChanged;
                holeFlow.StrokeChanged -= OnStrokeChanged;
                holeFlow.HoleCompleted -= OnHoleCompleted;
            }
            if (view != null && view.ActionButton != null)
            {
                view.ActionButton.onClick.RemoveListener(OnPrimaryActionClicked);
            }
        }

        public void OnPrimaryActionClicked()
        {
            shotFlow?.ConfirmCurrentStep();
        }

        private void RefreshAll()
        {
            if (view == null || holeFlow == null || holeFlow.Hole == null)
            {
                return;
            }

            RefreshPlayerAndHole();
            RefreshWind();
            RefreshClubAndSpin();
            RefreshState();
            RefreshAimData(true);
            if (holeFlow.State != HoleFlowState.HoleComplete)
            {
                view.ResultView?.HideImmediate();
            }
        }

        private void RefreshPlayerAndHole()
        {
            view.SetPlayer(playerName, holeFlow.StrokeCount, holeFlow.PenaltyCount);
            view.SetHole(
                holeFlow.Hole.HoleNumber,
                holeFlow.Hole.Par,
                holeFlow.StrokeCount,
                holeFlow.PenaltyCount);
        }

        private void RefreshWind()
        {
            if (wind == null)
            {
                return;
            }

            view.SetWind(
                wind.CurrentPreset.ToString().ToUpperInvariant(),
                wind.Strength,
                HudPresentationMapper.WindArrowAngle(wind.Direction));
        }

        private void RefreshClubAndSpin()
        {
            if (shotFlow == null || ball == null)
            {
                return;
            }

            ClubData club = shotFlow.CurrentClub;
            bool spinEnabled = club == null || !club.IsPutter;
            string clubName = club == null
                ? "NO CLUB"
                : club.IsPutter
                    ? "PUTTER"
                    : club.ClubType.ToString().ToUpperInvariant();
            view.SetClub(
                clubName,
                HudPresentationMapper.FormatLie(ball.CurrentLie),
                HudPresentationMapper.FormatSpin(shotFlow.SelectedSpinPreset, spinEnabled),
                spinEnabled,
                ball.CurrentLie,
                shotFlow.SelectedSpinPreset,
                club != null && club.IsPutter);
        }

        private void RefreshState()
        {
            if (shotFlow == null || holeFlow == null)
            {
                return;
            }

            bool complete = holeFlow.State == HoleFlowState.HoleComplete;
            view.GaugeView?.SetState(shotFlow.State, complete);
            view.SetAimVisible(!complete && shotFlow.State is ShotFlowState.Aiming
                or ShotFlowState.PowerSelecting
                or ShotFlowState.ImpactSelecting);
            view.SetPrimaryAction(
                HudPresentationMapper.MapPrimaryAction(shotFlow.State, holeFlow.State),
                shotFlow.State);
        }

        private void RefreshAimData(bool force)
        {
            if (holeFlow == null || view == null)
            {
                return;
            }

            int distanceTenths = Mathf.RoundToInt(holeFlow.RemainingDistance * 10f);
            int heightTenths = Mathf.RoundToInt(holeFlow.HeightDifference * 10f);
            if (force || distanceTenths != displayedDistanceTenths || heightTenths != displayedHeightTenths)
            {
                displayedDistanceTenths = distanceTenths;
                displayedHeightTenths = heightTenths;
                view.SetAimInfo(distanceTenths * 0.1f, heightTenths * 0.1f);
            }
        }

        private void UpdateAimMarker()
        {
            if (view == null || view.AimMarker == null
                || safeArea == null || worldCamera == null || ball == null || shotFlow == null)
            {
                return;
            }

            float markerDistance = tuning != null ? tuning.AimMarkerDistance : 22f;
            Vector3 target = ball.PhysicsPosition + shotFlow.AimDirection.normalized
                * Mathf.Min(markerDistance, Mathf.Max(2f, holeFlow.RemainingDistance));
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(target);
            if (screenPoint.z <= 0f)
            {
                return;
            }

            Camera eventCamera = hudCanvas != null && hudCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? hudCanvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    safeArea,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            float margin = tuning != null ? tuning.AimScreenMargin : 90f;
            Rect rect = safeArea.rect;
            localPoint.x = Mathf.Clamp(localPoint.x, rect.xMin + margin, rect.xMax - margin);
            localPoint.y = Mathf.Clamp(localPoint.y, rect.yMin + margin, rect.yMax - margin);
            view.AimMarker.anchoredPosition = localPoint;
        }

        private void OnShotStateChanged(ShotFlowState previous, ShotFlowState next)
        {
            RefreshState();
        }

        private void OnShotCommitted(ShotCommand command)
        {
            pendingImpactCommand = command;
            hasPendingImpactFeedback = true;
            RefreshPlayerAndHole();
        }

        private void OnBallLaunched()
        {
            if (!hasPendingImpactFeedback)
            {
                return;
            }

            hasPendingImpactFeedback = false;
            float duration = tuning != null ? tuning.ImpactFeedbackDuration : 1.1f;
            float fade = tuning != null ? tuning.PopupFadeDuration : 0.18f;
            view.ShowImpact(
                pendingImpactCommand.ImpactGrade.ToString().ToUpperInvariant(),
                view.ResolveTone(HudSkinStyleMapper.ForImpact(pendingImpactCommand.ImpactGrade)),
                duration,
                fade,
                pendingImpactCommand.ImpactGrade == ImpactGrade.Perfect);
        }

        private void OnClubChanged(ClubData club)
        {
            RefreshClubAndSpin();
        }

        private void OnSpinChanged(SpinPreset preset)
        {
            RefreshClubAndSpin();
        }

        private void OnBallStateChanged(BallState previous, BallState next)
        {
            RefreshState();
            RefreshAimData(true);
            if (previous == BallState.Stopped && next == BallState.Ready && !ball.HasLastHazard)
            {
                float duration = tuning != null ? tuning.LieFeedbackDuration : 1.2f;
                float fade = tuning != null ? tuning.PopupFadeDuration : 0.18f;
                view.ShowLie(
                HudPresentationMapper.FormatLie(ball.CurrentLie),
                    view.ResolveTone(HudSkinStyleMapper.ForLie(ball.CurrentLie)),
                    duration,
                    fade);
            }
            RefreshClubAndSpin();
        }

        private void OnHazardEntered(TerrainSurfaceType hazard)
        {
            float duration = tuning != null ? tuning.HazardFeedbackDuration : 2.6f;
            float fade = tuning != null ? tuning.PopupFadeDuration : 0.18f;
            view.ShowHazard(
                HudPresentationMapper.FormatHazard(hazard),
                view.ResolveTone(HudSkinTone.Coral),
                duration,
                fade);
        }

        private void OnBallReset()
        {
            displayedDistanceTenths = int.MinValue;
            displayedHeightTenths = int.MinValue;
            hasPendingImpactFeedback = false;
            view.HideTransientFeedback();
            RefreshAll();
        }

        private void OnWindChanged()
        {
            RefreshWind();
        }

        private void OnHoleStateChanged(HoleFlowState previous, HoleFlowState next)
        {
            if (next != HoleFlowState.HoleComplete)
            {
                view.ResultView?.HideImmediate();
            }
            RefreshAll();
        }

        private void OnStrokeChanged(int strokes)
        {
            RefreshPlayerAndHole();
        }

        private void OnHoleCompleted(ScoreResult result)
        {
            RefreshState();
            if (cinematicTuning == null)
            {
                ShowHoleResult(result);
            }
        }

        public void ShowHoleResult(ScoreResult result)
        {
            if (view == null || holeFlow == null)
            {
                return;
            }

            float duration = cinematicTuning != null
                ? cinematicTuning.ResultFrameDuration
                : tuning != null ? tuning.ResultShowDuration : 0.42f;
            float scoreDelay = cinematicTuning != null ? cinematicTuning.ResultScoreDelay : 0f;
            float detailDelay = cinematicTuning != null ? cinematicTuning.ResultDetailDelay : 0f;
            view.ResultView?.Show(holeFlow.Hole.HoleNumber, result, duration, scoreDelay, detailDelay);
        }
    }
}

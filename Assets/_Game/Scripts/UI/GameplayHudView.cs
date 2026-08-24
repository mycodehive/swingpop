using SwingPop.Data;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using UnityEngine;
using UnityEngine.UI;

namespace SwingPop.UI
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        [Header("Skin")]
        [SerializeField] private HudSkinData skin;
        [SerializeField] private Image playerPortraitImage;
        [SerializeField] private Image clubIconImage;
        [SerializeField] private Image lieAccentImage;
        [SerializeField] private Image spinIconImage;
        [SerializeField] private Image actionButtonImage;
        [SerializeField] private Image actionAccentImage;

        [Header("Player")]
        [SerializeField] private Text playerNameText;
        [SerializeField] private Text strokeText;
        [SerializeField] private Text penaltyText;

        [Header("Hole")]
        [SerializeField] private Text holeText;
        [SerializeField] private Text parText;
        [SerializeField] private Text scoreText;

        [Header("Wind")]
        [SerializeField] private RectTransform windArrow;
        [SerializeField] private Text windPresetText;
        [SerializeField] private Text windStrengthText;

        [Header("Aim")]
        [SerializeField] private GameObject aimRoot;
        [SerializeField] private RectTransform aimMarker;
        [SerializeField] private Text distanceText;
        [SerializeField] private Text heightText;

        [Header("Club")]
        [SerializeField] private Text clubInitialText;
        [SerializeField] private Text clubText;
        [SerializeField] private Text lieText;
        [SerializeField] private GameObject spinRoot;
        [SerializeField] private Text spinText;

        [Header("Interaction")]
        [SerializeField] private HudGaugeView gaugeView;
        [SerializeField] private GameObject actionRoot;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionText;

        [Header("Feedback")]
        [SerializeField] private HudPopupView impactPopup;
        [SerializeField] private HudPopupView hazardPopup;
        [SerializeField] private HudPopupView liePopup;
        [SerializeField] private HudResultView resultView;

        private float targetWindAngle;
        private Vector3 actionBaseScale = Vector3.one;

        public HudGaugeView GaugeView => gaugeView;
        public HudResultView ResultView => resultView;
        public Button ActionButton => actionButton;
        public RectTransform AimMarker => aimMarker;
        public bool IsAimVisible => aimRoot != null && aimRoot.activeSelf;
        public bool IsActionVisible => actionRoot != null && actionRoot.activeSelf;
        public string ActionLabel => actionText != null ? actionText.text : string.Empty;
        public string ClubLabel => clubText != null ? clubText.text : string.Empty;
        public string LieLabel => lieText != null ? lieText.text : string.Empty;
        public string SpinLabel => spinText != null ? spinText.text : string.Empty;
        public string WindLabel => windStrengthText != null ? windStrengthText.text : string.Empty;
        public string DistanceLabel => distanceText != null ? distanceText.text : string.Empty;
        public string HeightLabel => heightText != null ? heightText.text : string.Empty;
        public string HazardMessage => hazardPopup != null ? hazardPopup.Message : string.Empty;
        public string ImpactMessage => impactPopup != null ? impactPopup.Message : string.Empty;
        public HudSkinData Skin => skin;

        private void Awake()
        {
            if (actionRoot != null)
            {
                actionBaseScale = actionRoot.transform.localScale;
            }

            if (skin != null && playerPortraitImage != null)
            {
                playerPortraitImage.sprite = skin.PlayerIcon;
                playerPortraitImage.color = skin.Cyan;
            }
        }

        public void SetPlayer(string playerName, int strokes, int penalties)
        {
            playerNameText.text = playerName;
            strokeText.text = $"STROKE  {strokes}";
            penaltyText.gameObject.SetActive(penalties > 0);
            penaltyText.text = $"PENALTY  +{penalties}";
        }

        public void SetHole(int holeNumber, int par, int strokes, int penalties)
        {
            holeText.text = $"HOLE {holeNumber}";
            parText.text = $"PAR {par}";
            scoreText.text = HudPresentationMapper.FormatLiveScore(strokes, penalties);
        }

        public void SetWind(string preset, float strength, float arrowAngle)
        {
            windPresetText.text = preset;
            windStrengthText.text = $"{strength:0.0} m/s";
            targetWindAngle = arrowAngle;
        }

        public void SetAimInfo(float distance, float heightDifference)
        {
            distanceText.text = $"{distance:0.0} m";
            heightText.text = HudPresentationMapper.FormatHeightDifference(heightDifference);
        }

        public void SetAimVisible(bool visible)
        {
            aimRoot?.SetActive(visible);
        }

        public void SetClub(
            string clubName,
            string lie,
            string spin,
            bool spinEnabled,
            TerrainSurfaceType lieType,
            SpinPreset spinPreset,
            bool isPutter)
        {
            clubText.text = clubName;
            lieText.text = lie;
            spinRoot?.SetActive(true);
            spinText.text = spin;
            spinText.color = spinEnabled
                ? ResolveTone(HudSkinTone.PrimaryText)
                : ResolveTone(HudSkinTone.Disabled);
            clubInitialText.text = string.IsNullOrEmpty(clubName) ? "?" : clubName.Substring(0, 1);

            if (lieAccentImage != null)
            {
                lieAccentImage.color = ResolveTone(HudSkinStyleMapper.ForLie(lieType));
            }
            if (lieText != null)
            {
                lieText.color = ResolveTone(HudSkinStyleMapper.ForLie(lieType));
            }
            if (clubIconImage != null && skin != null)
            {
                clubIconImage.sprite = isPutter ? skin.PutterIcon : skin.DriverIcon;
                clubIconImage.color = skin.PrimaryText;
                if (clubInitialText != null) clubInitialText.gameObject.SetActive(clubIconImage.sprite == null);
            }
            if (spinIconImage != null && skin != null)
            {
                spinIconImage.sprite = ResolveSpinSprite(spinPreset, spinEnabled);
                spinIconImage.color = spinEnabled ? skin.Cyan : skin.Disabled;
            }
        }

        public void SetPrimaryAction(HudActionPresentation presentation, ShotFlowState state)
        {
            actionRoot?.SetActive(presentation.Visible);
            if (actionButton != null)
            {
                actionButton.interactable = presentation.Interactable;
            }
            if (actionText != null)
            {
                actionText.text = presentation.Label;
            }

            Color accent = ResolveTone(HudSkinStyleMapper.ForAction(state));
            if (actionAccentImage != null) actionAccentImage.color = accent;
            if (actionButtonImage != null)
            {
                actionButtonImage.color = presentation.Interactable
                    ? new Color(accent.r * 0.62f, accent.g * 0.72f, accent.b * 0.78f, 0.98f)
                    : ResolveTone(HudSkinTone.Disabled);
            }
        }

        public void ShowImpact(string message, Color color, float duration, float fadeDuration, bool strong)
        {
            impactPopup?.Show(message, color, duration, fadeDuration, strong);
        }

        public void ShowHazard(string message, Color color, float duration, float fadeDuration)
        {
            hazardPopup?.Show(message, color, duration, fadeDuration, true);
        }

        public void ShowLie(string message, Color color, float duration, float fadeDuration)
        {
            liePopup?.Show(message, color, duration, fadeDuration, false);
        }

        public void HideTransientFeedback()
        {
            impactPopup?.HideImmediate();
            hazardPopup?.HideImmediate();
            liePopup?.HideImmediate();
        }

        public void Tick(float unscaledDeltaTime, float unscaledTime, float breathingScale, float breathingSpeed)
        {
            if (windArrow != null)
            {
                float current = windArrow.localEulerAngles.z;
                float smoothed = Mathf.LerpAngle(current, targetWindAngle, 1f - Mathf.Exp(-unscaledDeltaTime * 8f));
                windArrow.localRotation = Quaternion.Euler(0f, 0f, smoothed);
            }

            if (actionRoot != null && actionRoot.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(unscaledTime * breathingSpeed) * breathingScale;
                actionRoot.transform.localScale = actionBaseScale * pulse;
            }

            gaugeView?.Tick(unscaledTime);
            impactPopup?.Tick(unscaledDeltaTime);
            hazardPopup?.Tick(unscaledDeltaTime);
            liePopup?.Tick(unscaledDeltaTime);
            resultView?.Tick(unscaledDeltaTime);
        }

        public static Color ImpactColor(ImpactGrade grade)
        {
            return grade switch
            {
                ImpactGrade.Perfect => new Color(1f, 0.86f, 0.2f),
                ImpactGrade.Great => new Color(0.28f, 1f, 0.9f),
                ImpactGrade.Good => new Color(0.45f, 0.82f, 1f),
                _ => new Color(1f, 0.38f, 0.24f)
            };
        }

        public Color ResolveTone(HudSkinTone tone)
        {
            return skin != null ? skin.Resolve(tone) : tone switch
            {
                HudSkinTone.Gold => new Color(1f, 0.86f, 0.2f),
                HudSkinTone.Mint => new Color(0.28f, 1f, 0.72f),
                HudSkinTone.Coral => new Color(1f, 0.38f, 0.24f),
                HudSkinTone.Disabled => new Color(0.68f, 0.76f, 0.8f),
                _ => new Color(0.2f, 0.9f, 1f)
            };
        }

        private Sprite ResolveSpinSprite(SpinPreset preset, bool enabled)
        {
            if (!enabled) return skin.SpinNoneIcon;
            return preset switch
            {
                SpinPreset.TopSpin => skin.SpinTopIcon,
                SpinPreset.BackSpin => skin.SpinBackIcon,
                SpinPreset.LeftSideSpin => skin.SpinLeftIcon,
                SpinPreset.RightSideSpin => skin.SpinRightIcon,
                _ => skin.SpinNoneIcon
            };
        }
    }
}

using SwingPop.Data;
using SwingPop.Gameplay.Shot;
using UnityEngine;
using UnityEngine.UI;

namespace SwingPop.UI
{
    public sealed class HudGaugeView : MonoBehaviour
    {
        [Header("Skin")]
        [SerializeField] private HudSkinData skin;

        [Header("Power")]
        [SerializeField] private GameObject powerRoot;
        [SerializeField] private Image powerFill;
        [SerializeField] private RectTransform powerCursor;
        [SerializeField] private Text powerPercentText;
        [SerializeField] private CanvasGroup powerGlow;
        [SerializeField] private Image powerCursorImage;

        [Header("Impact")]
        [SerializeField] private GameObject impactRoot;
        [SerializeField] private RectTransform impactCursor;
        [SerializeField] private RectTransform goodZone;
        [SerializeField] private RectTransform greatZone;
        [SerializeField] private RectTransform perfectZone;
        [SerializeField] private Text impactPreviewText;
        [SerializeField] private Image impactCursorImage;
        [SerializeField] private Image perfectZoneImage;

        private int displayedPowerPercent = -1;

        public bool IsPowerVisible => powerRoot != null && powerRoot.activeSelf;
        public bool IsImpactVisible => impactRoot != null && impactRoot.activeSelf;
        public float PerfectZoneFraction { get; private set; }

        public void ConfigureImpactZones(float perfectMaximumOffset, float greatMaximumOffset, float goodMaximumOffset)
        {
            PerfectZoneFraction = Mathf.Clamp01(perfectMaximumOffset);
            SetCenteredZone(goodZone, goodMaximumOffset);
            SetCenteredZone(greatZone, greatMaximumOffset);
            SetCenteredZone(perfectZone, perfectMaximumOffset);
        }

        public void SetState(ShotFlowState state, bool holeComplete)
        {
            bool powerVisible = !holeComplete && state == ShotFlowState.PowerSelecting;
            bool impactVisible = !holeComplete && state == ShotFlowState.ImpactSelecting;
            powerRoot?.SetActive(powerVisible);
            impactRoot?.SetActive(impactVisible);
        }

        public void SetPower(float value01)
        {
            float normalized = Mathf.Clamp01(value01);
            if (powerFill != null)
            {
                powerFill.fillAmount = normalized;
                if (skin != null)
                {
                    powerFill.color = normalized < 0.82f
                        ? Color.Lerp(skin.Cyan, skin.Mint, normalized / 0.82f)
                        : Color.Lerp(skin.Mint, skin.Gold, (normalized - 0.82f) / 0.18f);
                    if (powerCursorImage != null) powerCursorImage.color = powerFill.color;
                }
            }
            SetHorizontalCursor(powerCursor, normalized);

            int percent = Mathf.RoundToInt(normalized * 100f);
            if (powerPercentText != null && displayedPowerPercent != percent)
            {
                displayedPowerPercent = percent;
                powerPercentText.text = $"{percent}%";
            }
        }

        public void SetImpact(float cursor, ImpactGrade grade)
        {
            SetHorizontalCursor(impactCursor, (Mathf.Clamp(cursor, -1f, 1f) + 1f) * 0.5f);
            if (impactPreviewText != null)
            {
                impactPreviewText.text = grade.ToString().ToUpperInvariant();
                if (skin != null)
                {
                    impactPreviewText.color = skin.Resolve(HudSkinStyleMapper.ForImpact(grade));
                }
            }
            if (impactCursorImage != null && skin != null)
            {
                impactCursorImage.color = skin.Resolve(HudSkinStyleMapper.ForImpact(grade));
            }
        }

        public void Tick(float unscaledTime)
        {
            if (powerGlow != null && IsPowerVisible)
            {
                powerGlow.alpha = 0.68f + Mathf.Sin(unscaledTime * 4f) * 0.18f;
            }
            if (perfectZoneImage != null && IsImpactVisible && skin != null)
            {
                Color color = skin.Gold;
                color.a = 0.78f + Mathf.Sin(unscaledTime * 5.5f) * 0.14f;
                perfectZoneImage.color = color;
            }
        }

        private static void SetCenteredZone(RectTransform zone, float maximumOffset)
        {
            if (zone == null)
            {
                return;
            }

            float fraction = Mathf.Clamp01(maximumOffset);
            zone.anchorMin = new Vector2(0.5f - fraction * 0.5f, 0f);
            zone.anchorMax = new Vector2(0.5f + fraction * 0.5f, 1f);
            zone.offsetMin = Vector2.zero;
            zone.offsetMax = Vector2.zero;
        }

        private static void SetHorizontalCursor(RectTransform cursor, float normalized)
        {
            if (cursor == null)
            {
                return;
            }

            Vector2 anchor = new(Mathf.Clamp01(normalized), 0.5f);
            cursor.anchorMin = anchor;
            cursor.anchorMax = anchor;
            cursor.anchoredPosition = Vector2.zero;
        }
    }
}

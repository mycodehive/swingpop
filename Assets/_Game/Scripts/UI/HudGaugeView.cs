using SwingPop.Gameplay.Shot;
using UnityEngine;
using UnityEngine.UI;

namespace SwingPop.UI
{
    public sealed class HudGaugeView : MonoBehaviour
    {
        [Header("Power")]
        [SerializeField] private GameObject powerRoot;
        [SerializeField] private Image powerFill;
        [SerializeField] private RectTransform powerCursor;
        [SerializeField] private Text powerPercentText;
        [SerializeField] private CanvasGroup powerGlow;

        [Header("Impact")]
        [SerializeField] private GameObject impactRoot;
        [SerializeField] private RectTransform impactCursor;
        [SerializeField] private RectTransform goodZone;
        [SerializeField] private RectTransform greatZone;
        [SerializeField] private RectTransform perfectZone;
        [SerializeField] private Text impactPreviewText;

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
            }
        }

        public void Tick(float unscaledTime)
        {
            if (powerGlow != null && IsPowerVisible)
            {
                powerGlow.alpha = 0.68f + Mathf.Sin(unscaledTime * 4f) * 0.18f;
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

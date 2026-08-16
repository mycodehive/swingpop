using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "HudTuning", menuName = "SwingPop/HUD Tuning")]
    public sealed class HudTuningData : ScriptableObject
    {
        [Header("Popup Durations")]
        [SerializeField, Min(0.1f)] private float impactFeedbackDuration = 1.1f;
        [SerializeField, Min(0.1f)] private float hazardFeedbackDuration = 2.6f;
        [SerializeField, Min(0.1f)] private float lieFeedbackDuration = 1.2f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float buttonBreathingScale = 0.025f;
        [SerializeField, Min(0.1f)] private float buttonBreathingSpeed = 2.2f;
        [SerializeField, Min(0.01f)] private float popupFadeDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float resultShowDuration = 0.42f;

        [Header("Aim Presentation")]
        [SerializeField, Min(1f)] private float aimMarkerDistance = 22f;
        [SerializeField, Min(0f)] private float aimScreenMargin = 90f;

        public float ImpactFeedbackDuration => impactFeedbackDuration;
        public float HazardFeedbackDuration => hazardFeedbackDuration;
        public float LieFeedbackDuration => lieFeedbackDuration;
        public float ButtonBreathingScale => buttonBreathingScale;
        public float ButtonBreathingSpeed => buttonBreathingSpeed;
        public float PopupFadeDuration => popupFadeDuration;
        public float ResultShowDuration => resultShowDuration;
        public float AimMarkerDistance => aimMarkerDistance;
        public float AimScreenMargin => aimScreenMargin;
    }
}

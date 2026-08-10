using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "ShotTuning", menuName = "SwingPop/Shot Tuning")]
    public sealed class ShotTuningData : ScriptableObject
    {
        [Header("Aim")]
        [SerializeField] private float minimumAimAngle = -30f;
        [SerializeField] private float maximumAimAngle = 30f;
        [SerializeField, Min(1f)] private float aimRotationSpeed = 45f;

        [Header("Power")]
        [Tooltip("Normalized gauge units per second. A value of 0.8 reaches 100% in 1.25 seconds.")]
        [SerializeField, Min(0.05f)] private float powerSweepSpeed = 0.8f;

        [Header("Impact")]
        [Tooltip("Normalized cursor sweep speed across one half of its -1 to +1 range.")]
        [SerializeField, Min(0.05f)] private float impactSweepSpeed = 1.1f;
        [SerializeField, Range(0f, 1f)] private float perfectMaximumOffset = 0.15f;
        [SerializeField, Range(0f, 1f)] private float greatMaximumOffset = 0.35f;
        [SerializeField, Range(0f, 1f)] private float goodMaximumOffset = 0.65f;

        [Header("Impact Power Multipliers")]
        [SerializeField, Range(0f, 1f)] private float perfectPowerMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float greatPowerMultiplier = 0.98f;
        [SerializeField, Range(0f, 1f)] private float goodPowerMultiplier = 0.9f;
        [SerializeField, Range(0f, 1f)] private float missPowerMultiplier = 0.72f;

        [Header("Deterministic Dispersion")]
        [SerializeField, Min(0f)] private float greatDispersionDegrees = 1.5f;
        [SerializeField, Min(0f)] private float goodDispersionDegrees = 4f;
        [SerializeField, Min(0f)] private float missDispersionDegrees = 9f;

        public float MinimumAimAngle => minimumAimAngle;
        public float MaximumAimAngle => maximumAimAngle;
        public float AimRotationSpeed => aimRotationSpeed;
        public float PowerSweepSpeed => powerSweepSpeed;
        public float ImpactSweepSpeed => impactSweepSpeed;
        public float PerfectMaximumOffset => perfectMaximumOffset;
        public float GreatMaximumOffset => greatMaximumOffset;
        public float GoodMaximumOffset => goodMaximumOffset;
        public float PerfectPowerMultiplier => perfectPowerMultiplier;
        public float GreatPowerMultiplier => greatPowerMultiplier;
        public float GoodPowerMultiplier => goodPowerMultiplier;
        public float MissPowerMultiplier => missPowerMultiplier;
        public float GreatDispersionDegrees => greatDispersionDegrees;
        public float GoodDispersionDegrees => goodDispersionDegrees;
        public float MissDispersionDegrees => missDispersionDegrees;
    }
}

using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "WindTuning", menuName = "SwingPop/Wind Tuning")]
    public sealed class WindTuningData : ScriptableObject
    {
        [Header("Preset Strengths (m/s)")]
        [SerializeField, Min(0f)] private float tailwindStrength = 5f;
        [SerializeField, Min(0f)] private float headwindStrength = 5f;
        [SerializeField, Min(0f)] private float crosswindStrength = 5f;

        [Header("Ball Influence")]
        [Tooltip("Converts wind velocity in m/s into acceleration applied to an airborne ball.")]
        [SerializeField, Min(0f)] private float windForceMultiplier = 0.32f;
        [SerializeField, Min(0f)] private float headTailMultiplier = 1f;
        [SerializeField, Min(0f)] private float crosswindMultiplier = 1.15f;

        public float TailwindStrength => tailwindStrength;
        public float HeadwindStrength => headwindStrength;
        public float CrosswindStrength => crosswindStrength;
        public float WindForceMultiplier => windForceMultiplier;
        public float HeadTailMultiplier => headTailMultiplier;
        public float CrosswindMultiplier => crosswindMultiplier;
    }
}

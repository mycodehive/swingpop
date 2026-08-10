using System;
using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Gameplay.Wind
{
    public sealed class WindController : MonoBehaviour
    {
        [SerializeField] private WindTuningData tuning;
        [SerializeField] private WindPreset initialPreset = WindPreset.Calm;

        private WindPreset currentPreset;
        private Vector3 direction;
        private float strength;

        public event Action WindChanged;

        public WindPreset CurrentPreset => currentPreset;
        public Vector3 Direction => direction;
        public float Strength => strength;
        public Vector3 Velocity => direction * strength;
        public WindTuningData Tuning => tuning;

        private void Awake()
        {
            SetPreset(initialPreset);
        }

        public void SetPreset(WindPreset preset)
        {
            currentPreset = preset;
            (direction, strength) = preset switch
            {
                WindPreset.Tailwind => (Vector3.forward, tuning != null ? tuning.TailwindStrength : 0f),
                WindPreset.Headwind => (Vector3.back, tuning != null ? tuning.HeadwindStrength : 0f),
                WindPreset.LeftCrosswind => (Vector3.left, tuning != null ? tuning.CrosswindStrength : 0f),
                WindPreset.RightCrosswind => (Vector3.right, tuning != null ? tuning.CrosswindStrength : 0f),
                _ => (Vector3.zero, 0f)
            };
            WindChanged?.Invoke();
        }

        public Vector3 CalculateBallAcceleration(Vector3 ballVelocity)
        {
            if (tuning == null || strength <= 0f)
            {
                return Vector3.zero;
            }

            return WindPhysics.CalculateAcceleration(
                Velocity,
                ballVelocity,
                tuning.WindForceMultiplier,
                tuning.HeadTailMultiplier,
                tuning.CrosswindMultiplier);
        }
    }
}

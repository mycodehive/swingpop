using System;
using UnityEngine;

namespace SwingPop.Gameplay.Shot
{
    /// <summary>
    /// Serializable value payload with no scene, UI, camera, or MonoBehaviour references.
    /// </summary>
    [Serializable]
    public struct ShotCommand
    {
        [SerializeField] private Vector3 aimDirection;
        [SerializeField] private Vector3 finalDirection;
        [SerializeField] private float aimAngleDegrees;
        [SerializeField] private float power01;
        [SerializeField] private float impactAccuracy01;
        [SerializeField] private float impactOffset;
        [SerializeField] private ImpactGrade impactGrade;
        [SerializeField] private float effectivePower01;
        [SerializeField] private float dispersionDegrees;
        [SerializeField] private float baseLaunchSpeed;
        [SerializeField] private float loftDegrees;
        [SerializeField] private ShotSpin spin;
        [SerializeField] private float surfacePowerModifier;

        public ShotCommand(
            Vector3 aimDirection,
            Vector3 finalDirection,
            float aimAngleDegrees,
            float power01,
            float impactAccuracy01,
            float impactOffset,
            ImpactGrade impactGrade,
            float effectivePower01,
            float dispersionDegrees,
            float baseLaunchSpeed,
            float loftDegrees)
            : this(
                aimDirection,
                finalDirection,
                aimAngleDegrees,
                power01,
                impactAccuracy01,
                impactOffset,
                impactGrade,
                effectivePower01,
                dispersionDegrees,
                baseLaunchSpeed,
                loftDegrees,
                ShotSpin.None)
        {
        }

        public ShotCommand(
            Vector3 aimDirection,
            Vector3 finalDirection,
            float aimAngleDegrees,
            float power01,
            float impactAccuracy01,
            float impactOffset,
            ImpactGrade impactGrade,
            float effectivePower01,
            float dispersionDegrees,
            float baseLaunchSpeed,
            float loftDegrees,
            ShotSpin spin)
        {
            this.aimDirection = aimDirection;
            this.finalDirection = finalDirection;
            this.aimAngleDegrees = aimAngleDegrees;
            this.power01 = power01;
            this.impactAccuracy01 = impactAccuracy01;
            this.impactOffset = impactOffset;
            this.impactGrade = impactGrade;
            this.effectivePower01 = effectivePower01;
            this.dispersionDegrees = dispersionDegrees;
            this.baseLaunchSpeed = baseLaunchSpeed;
            this.loftDegrees = loftDegrees;
            this.spin = spin;
            surfacePowerModifier = 1f;
        }

        public Vector3 AimDirection => aimDirection;
        public Vector3 FinalDirection => finalDirection;
        public float AimAngleDegrees => aimAngleDegrees;
        public float Power01 => power01;
        public float ImpactAccuracy01 => impactAccuracy01;
        public float ImpactOffset => impactOffset;
        public ImpactGrade ImpactGrade => impactGrade;
        public float EffectivePower01 => effectivePower01;
        public float DispersionDegrees => dispersionDegrees;
        public float BaseLaunchSpeed => baseLaunchSpeed;
        public float LoftDegrees => loftDegrees;
        public ShotSpin Spin => spin;
        public float SurfacePowerModifier => surfacePowerModifier;

        public ShotCommand WithSurfacePowerModifier(float modifier)
        {
            ShotCommand modified = this;
            modified.surfacePowerModifier = Mathf.Max(0f, modifier);
            modified.effectivePower01 = Mathf.Clamp01(effectivePower01 * modified.surfacePowerModifier);
            return modified;
        }

        public override string ToString()
        {
            return $"Aim {aimAngleDegrees:+0.0;-0.0;0.0}°, Power {power01 * 100f:0}% " +
                   $"Impact {impactGrade} ({impactAccuracy01 * 100f:0}%), " +
                   $"Dispersion {dispersionDegrees:+0.0;-0.0;0.0}°, Spin [{spin}]";
        }
    }
}

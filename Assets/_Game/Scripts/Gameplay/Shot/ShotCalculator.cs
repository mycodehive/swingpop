using UnityEngine;

namespace SwingPop.Gameplay.Shot
{
    public static class ShotCalculator
    {
        public static float ClampAimAngle(float angleDegrees, float minimumAngle, float maximumAngle)
        {
            return Mathf.Clamp(angleDegrees, minimumAngle, maximumAngle);
        }

        public static float NormalizePowerPercent(float powerPercent)
        {
            return Mathf.Clamp01(powerPercent / 100f);
        }

        public static float EvaluatePingPong01(float elapsedSeconds, float sweepSpeed)
        {
            return Mathf.PingPong(Mathf.Max(0f, elapsedSeconds) * Mathf.Max(0f, sweepSpeed), 1f);
        }

        public static float EvaluateImpactCursor(float elapsedSeconds, float sweepSpeed)
        {
            return Mathf.Lerp(-1f, 1f, EvaluatePingPong01(elapsedSeconds, sweepSpeed));
        }

        public static ImpactGrade ClassifyImpact(
            float impactOffset,
            float perfectMaximumOffset,
            float greatMaximumOffset,
            float goodMaximumOffset)
        {
            float absoluteOffset = Mathf.Abs(impactOffset);
            if (absoluteOffset <= perfectMaximumOffset)
            {
                return ImpactGrade.Perfect;
            }

            if (absoluteOffset <= greatMaximumOffset)
            {
                return ImpactGrade.Great;
            }

            if (absoluteOffset <= goodMaximumOffset)
            {
                return ImpactGrade.Good;
            }

            return ImpactGrade.Miss;
        }

        public static float CalculateImpactAccuracy(float impactOffset)
        {
            return 1f - Mathf.Clamp01(Mathf.Abs(impactOffset));
        }

        public static float GetPowerMultiplier(
            ImpactGrade grade,
            float perfectMultiplier,
            float greatMultiplier,
            float goodMultiplier,
            float missMultiplier)
        {
            return grade switch
            {
                ImpactGrade.Perfect => perfectMultiplier,
                ImpactGrade.Great => greatMultiplier,
                ImpactGrade.Good => goodMultiplier,
                _ => missMultiplier
            };
        }

        public static float CalculateDispersionDegrees(
            float impactOffset,
            ImpactGrade grade,
            float greatDispersionDegrees,
            float goodDispersionDegrees,
            float missDispersionDegrees)
        {
            if (grade == ImpactGrade.Perfect)
            {
                return 0f;
            }

            float magnitude = grade switch
            {
                ImpactGrade.Great => greatDispersionDegrees,
                ImpactGrade.Good => goodDispersionDegrees,
                _ => missDispersionDegrees
            };
            return Mathf.Sign(impactOffset) * magnitude;
        }

        public static ShotCommand CreateCommand(
            Vector3 baseForward,
            float aimAngleDegrees,
            float power01,
            float impactOffset,
            float perfectMaximumOffset,
            float greatMaximumOffset,
            float goodMaximumOffset,
            float perfectPowerMultiplier,
            float greatPowerMultiplier,
            float goodPowerMultiplier,
            float missPowerMultiplier,
            float greatDispersionDegrees,
            float goodDispersionDegrees,
            float missDispersionDegrees,
            float baseLaunchSpeed,
            float loftDegrees)
        {
            return CreateCommand(
                baseForward,
                aimAngleDegrees,
                power01,
                impactOffset,
                perfectMaximumOffset,
                greatMaximumOffset,
                goodMaximumOffset,
                perfectPowerMultiplier,
                greatPowerMultiplier,
                goodPowerMultiplier,
                missPowerMultiplier,
                greatDispersionDegrees,
                goodDispersionDegrees,
                missDispersionDegrees,
                baseLaunchSpeed,
                loftDegrees,
                ShotSpin.None);
        }

        public static ShotCommand CreateCommand(
            Vector3 baseForward,
            float aimAngleDegrees,
            float power01,
            float impactOffset,
            float perfectMaximumOffset,
            float greatMaximumOffset,
            float goodMaximumOffset,
            float perfectPowerMultiplier,
            float greatPowerMultiplier,
            float goodPowerMultiplier,
            float missPowerMultiplier,
            float greatDispersionDegrees,
            float goodDispersionDegrees,
            float missDispersionDegrees,
            float baseLaunchSpeed,
            float loftDegrees,
            ShotSpin spin)
        {
            Vector3 planarForward = Vector3.ProjectOnPlane(baseForward, Vector3.up).normalized;
            if (planarForward.sqrMagnitude <= Mathf.Epsilon)
            {
                planarForward = Vector3.forward;
            }

            float normalizedPower = Mathf.Clamp01(power01);
            float clampedOffset = Mathf.Clamp(impactOffset, -1f, 1f);
            ImpactGrade grade = ClassifyImpact(
                clampedOffset,
                perfectMaximumOffset,
                greatMaximumOffset,
                goodMaximumOffset);
            float powerMultiplier = GetPowerMultiplier(
                grade,
                perfectPowerMultiplier,
                greatPowerMultiplier,
                goodPowerMultiplier,
                missPowerMultiplier);
            float dispersionDegrees = CalculateDispersionDegrees(
                clampedOffset,
                grade,
                greatDispersionDegrees,
                goodDispersionDegrees,
                missDispersionDegrees);

            Vector3 aimDirection = Quaternion.AngleAxis(aimAngleDegrees, Vector3.up) * planarForward;
            Vector3 finalDirection = Quaternion.AngleAxis(dispersionDegrees, Vector3.up) * aimDirection;

            return new ShotCommand(
                aimDirection.normalized,
                finalDirection.normalized,
                aimAngleDegrees,
                normalizedPower,
                CalculateImpactAccuracy(clampedOffset),
                clampedOffset,
                grade,
                Mathf.Clamp01(normalizedPower * powerMultiplier),
                dispersionDegrees,
                baseLaunchSpeed,
                loftDegrees,
                spin);
        }
    }
}

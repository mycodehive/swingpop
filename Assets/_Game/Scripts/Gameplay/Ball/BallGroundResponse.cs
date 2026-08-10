using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Gameplay.Ball
{
    public static class BallGroundResponse
    {
        public static Vector3 CalculateFirstLandingPlanarVelocity(
            Vector3 planarVelocity,
            Vector3 launchForward,
            ShotSpin spin,
            float topSpinLandingBoost,
            float backSpinLandingBrake,
            float backSpinRollbackSpeed,
            float sideSpinLandingResponse)
        {
            Vector3 forward = Vector3.ProjectOnPlane(launchForward, Vector3.up).normalized;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = planarVelocity.sqrMagnitude > Mathf.Epsilon
                    ? planarVelocity.normalized
                    : Vector3.forward;
            }

            if (spin.VerticalSpin > 0f)
            {
                planarVelocity += forward * (spin.VerticalSpin * Mathf.Max(0f, topSpinLandingBoost));
            }
            else if (spin.VerticalSpin < 0f)
            {
                float backSpin = -spin.VerticalSpin;
                float retainedSpeed = 1f - Mathf.Clamp01(backSpinLandingBrake) * backSpin;
                planarVelocity *= retainedSpeed;
                planarVelocity -= forward * (backSpin * Mathf.Max(0f, backSpinRollbackSpeed));
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            planarVelocity += right * (spin.SideSpin * Mathf.Max(0f, sideSpinLandingResponse));
            return planarVelocity;
        }

        public static float CalculateRollingDeceleration(
            float baseDeceleration,
            float verticalSpin,
            float topSpinMultiplier,
            float backSpinMultiplier)
        {
            float clampedSpin = Mathf.Clamp(verticalSpin, -1f, 1f);
            if (clampedSpin > 0f)
            {
                return Mathf.Max(0f, baseDeceleration)
                       * Mathf.Lerp(1f, Mathf.Max(0f, topSpinMultiplier), clampedSpin);
            }

            if (clampedSpin < 0f)
            {
                return Mathf.Max(0f, baseDeceleration)
                       * Mathf.Lerp(1f, Mathf.Max(0f, backSpinMultiplier), -clampedSpin);
            }

            return Mathf.Max(0f, baseDeceleration);
        }
    }
}

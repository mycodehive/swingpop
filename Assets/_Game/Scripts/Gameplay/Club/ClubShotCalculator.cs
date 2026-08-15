using UnityEngine;

namespace SwingPop.Gameplay.Club
{
    public static class ClubShotCalculator
    {
        public static Vector3 CalculateLaunchVelocity(
            Vector3 direction,
            float basePower,
            float loftDegrees,
            float effectivePower01,
            float carryModifier)
        {
            Vector3 planarDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                planarDirection = Vector3.forward;
            }

            float speed = Mathf.Max(0f, basePower)
                          * Mathf.Clamp01(effectivePower01)
                          * Mathf.Max(0f, carryModifier);
            float angleRadians = Mathf.Clamp(loftDegrees, 0f, 89f) * Mathf.Deg2Rad;
            return planarDirection * (speed * Mathf.Cos(angleRadians))
                   + Vector3.up * (speed * Mathf.Sin(angleRadians));
        }

        public static float ApplyRollModifier(float rollingDeceleration, float rollModifier)
        {
            return Mathf.Max(0f, rollingDeceleration) / Mathf.Max(0.01f, rollModifier);
        }
    }
}

using UnityEngine;

namespace SwingPop.Gameplay.Wind
{
    public static class WindPhysics
    {
        public static Vector3 CalculateAcceleration(
            Vector3 windVelocity,
            Vector3 ballVelocity,
            float forceMultiplier,
            float headTailMultiplier,
            float crosswindMultiplier)
        {
            Vector3 planarWind = Vector3.ProjectOnPlane(windVelocity, Vector3.up);
            Vector3 travelForward = Vector3.ProjectOnPlane(ballVelocity, Vector3.up).normalized;
            if (planarWind.sqrMagnitude <= Mathf.Epsilon || travelForward.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            Vector3 headTailWind = Vector3.Project(planarWind, travelForward);
            Vector3 crosswind = planarWind - headTailWind;
            return Mathf.Max(0f, forceMultiplier)
                   * (headTailWind * Mathf.Max(0f, headTailMultiplier)
                      + crosswind * Mathf.Max(0f, crosswindMultiplier));
        }
    }
}

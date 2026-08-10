using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Gameplay.Course
{
    public static class TerrainResponse
    {
        public static float ApplyPowerModifier(float power01, TerrainSurfaceData surface)
        {
            return Mathf.Clamp01(power01 * (surface != null ? surface.PowerModifier : 1f));
        }

        public static float CalculateRollingDeceleration(float baseDeceleration, TerrainSurfaceData surface)
        {
            if (surface == null)
            {
                return Mathf.Max(0f, baseDeceleration);
            }

            return Mathf.Max(0f, baseDeceleration)
                   * surface.Friction
                   * surface.RollingResistance;
        }

        public static float ApplyBounceModifier(float upwardVelocity, TerrainSurfaceData surface)
        {
            return Mathf.Max(0f, upwardVelocity)
                   * (surface != null ? surface.BounceModifier : 1f);
        }

        public static float ApplySpinResponse(float spin, TerrainSurfaceData surface)
        {
            return Mathf.Clamp(spin, -1f, 1f)
                   * (surface != null ? surface.SpinResponse : 1f);
        }
    }
}

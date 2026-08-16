using UnityEngine;

namespace SwingPop.CharacterSystem
{
    public static class CharacterPlacementCalculator
    {
        public static Vector3 ResolvePlanarForward(Vector3 direction, Vector3 fallback)
        {
            Vector3 planar = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (planar.sqrMagnitude > 0.0001f)
            {
                return planar.normalized;
            }

            planar = Vector3.ProjectOnPlane(fallback, Vector3.up);
            return planar.sqrMagnitude > 0.0001f ? planar.normalized : Vector3.forward;
        }

        public static Vector3 CalculateAddressPosition(
            Vector3 ballPosition,
            Vector3 aimDirection,
            float lateralOffset,
            float backwardOffset,
            float heightOffset = 0f)
        {
            Vector3 forward = ResolvePlanarForward(aimDirection, Vector3.forward);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            return ballPosition + right * lateralOffset + forward * backwardOffset + Vector3.up * heightOffset;
        }

        public static Quaternion CalculateAimRotation(Vector3 aimDirection, Quaternion fallback)
        {
            Vector3 forward = ResolvePlanarForward(aimDirection, fallback * Vector3.forward);
            return Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}

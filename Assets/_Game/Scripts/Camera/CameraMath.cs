using UnityEngine;

namespace SwingPop.CameraSystem
{
    public static class CameraMath
    {
        public static float SmoothStep01(float value)
        {
            float clamped = Mathf.Clamp01(value);
            return clamped * clamped * (3f - 2f * clamped);
        }

        public static float ExponentialBlend(float sharpness, float deltaTime)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0f, sharpness) * Mathf.Max(0f, deltaTime));
        }

        public static Vector3 ResolvePlanarForward(Vector3 preferred, Vector3 fallback)
        {
            Vector3 planar = Vector3.ProjectOnPlane(preferred, Vector3.up);
            if (planar.sqrMagnitude <= Mathf.Epsilon)
            {
                planar = Vector3.ProjectOnPlane(fallback, Vector3.up);
            }

            return planar.sqrMagnitude > Mathf.Epsilon ? planar.normalized : Vector3.forward;
        }

        public static Vector3 LocalOffset(Vector3 origin, Vector3 forward, Vector3 offset)
        {
            Vector3 safeForward = ResolvePlanarForward(forward, Vector3.forward);
            Vector3 right = Vector3.Cross(Vector3.up, safeForward).normalized;
            return origin + right * offset.x + Vector3.up * offset.y + safeForward * offset.z;
        }

        public static float FollowDistance(float speed, float scale, float maximumExtension)
        {
            return Mathf.Min(Mathf.Max(0f, speed) * Mathf.Max(0f, scale), Mathf.Max(0f, maximumExtension));
        }

        public static Quaternion LookRotation(Vector3 position, Vector3 target, Quaternion fallback)
        {
            Vector3 direction = target - position;
            return direction.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : fallback;
        }
    }
}

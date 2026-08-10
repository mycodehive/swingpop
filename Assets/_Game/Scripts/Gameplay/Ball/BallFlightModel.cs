using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Gameplay.Ball
{
    public static class BallFlightModel
    {
        public static Vector3 CalculateAirAcceleration(
            Vector3 velocity,
            ShotSpin spin,
            float dragStrength,
            float liftStrength,
            float sideCurveStrength,
            float referenceSpeed,
            Vector3 externalAcceleration)
        {
            Vector3 acceleration = externalAcceleration - velocity * Mathf.Max(0f, dragStrength);
            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            if (planarVelocity.sqrMagnitude <= 0.0001f)
            {
                return acceleration;
            }

            float safeReferenceSpeed = Mathf.Max(0.1f, referenceSpeed);
            float speedFactor = Mathf.Clamp(velocity.magnitude / safeReferenceSpeed, 0f, 1.5f);
            Vector3 travelForward = planarVelocity.normalized;
            Vector3 travelRight = Vector3.Cross(Vector3.up, travelForward).normalized;

            // Backspin creates modest lift and topspin modest downforce. Landing response
            // remains the primary visual difference so topspin does not simply dive.
            acceleration += Vector3.up * (-spin.VerticalSpin * Mathf.Max(0f, liftStrength) * speedFactor);
            acceleration += travelRight * (spin.SideSpin * Mathf.Max(0f, sideCurveStrength) * speedFactor);
            return acceleration;
        }

        public static float DecaySpin(float spin, float decayPerSecond, float deltaTime)
        {
            return Mathf.MoveTowards(spin, 0f, Mathf.Max(0f, decayPerSecond) * Mathf.Max(0f, deltaTime));
        }
    }
}

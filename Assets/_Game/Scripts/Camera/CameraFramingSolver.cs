using SwingPop.Data;
using UnityEngine;

namespace SwingPop.CameraSystem
{
    public static class CameraFramingSolver
    {
        public static CameraFraming Resolve(
            CameraMode mode,
            CameraTuningData tuning,
            Vector3 ballPosition,
            Vector3 resetPosition,
            Vector3 cupPosition,
            Vector3 aimForward,
            Vector3 velocity,
            Vector3 launchForward,
            Vector3 previousPlanarDirection,
            float ballSpeed,
            float modeElapsed)
        {
            Vector3 velocityForward = CameraMath.ResolvePlanarForward(velocity, launchForward);
            return mode switch
            {
                CameraMode.HoleIntro => ResolveIntro(tuning, resetPosition, cupPosition, modeElapsed),
                CameraMode.Address => ResolveAddress(tuning, ballPosition, aimForward, aimForward, "Ball Address"),
                CameraMode.Aim => ResolveAddress(
                    tuning,
                    ballPosition,
                    aimForward,
                    Vector3.Slerp(previousPlanarDirection, aimForward, tuning.AimYawResponse).normalized,
                    "Aim Line"),
                CameraMode.Swing => ResolveSwing(tuning, ballPosition, aimForward, false),
                CameraMode.Impact => ResolveSwing(tuning, ballPosition, aimForward, true),
                CameraMode.BallFollow => ResolveFollow(
                    tuning,
                    ballPosition,
                    resetPosition,
                    velocity,
                    velocityForward,
                    ballSpeed),
                CameraMode.Landing => ResolveLanding(tuning, ballPosition, velocityForward),
                CameraMode.NextShot => ResolveNextShot(tuning, ballPosition, cupPosition),
                CameraMode.Putt => ResolvePutt(tuning, ballPosition, cupPosition, aimForward),
                CameraMode.HoleComplete => ResolveHoleComplete(tuning, cupPosition, previousPlanarDirection),
                _ => ResolveResult(tuning, resetPosition, cupPosition)
            };
        }

        private static CameraFraming ResolveIntro(
            CameraTuningData tuning,
            Vector3 resetPosition,
            Vector3 cupPosition,
            float elapsed)
        {
            float progress = tuning.HoleIntroDuration <= 0f
                ? 1f
                : CameraMath.SmoothStep01(elapsed / tuning.HoleIntroDuration);
            Vector3 courseForward = cupPosition - resetPosition;
            Vector3 position = Vector3.Lerp(
                CameraMath.LocalOffset(resetPosition, courseForward, tuning.IntroTeeOffset),
                CameraMath.LocalOffset(cupPosition, courseForward, tuning.IntroCupOffset),
                progress);
            Vector3 target = Vector3.Lerp(
                resetPosition + Vector3.up,
                cupPosition + Vector3.up * 0.5f,
                progress);
            return new CameraFraming(position, target, tuning.IntroFieldOfView, "Hole Route");
        }

        private static CameraFraming ResolveAddress(
            CameraTuningData tuning,
            Vector3 ballPosition,
            Vector3 aimForward,
            Vector3 cameraForward,
            string targetName)
        {
            return new CameraFraming(
                CameraMath.LocalOffset(ballPosition, cameraForward, tuning.AddressOffset),
                CameraMath.LocalOffset(ballPosition, aimForward, tuning.AddressLookOffset),
                tuning.AddressFieldOfView,
                targetName);
        }

        private static CameraFraming ResolveSwing(
            CameraTuningData tuning,
            Vector3 ballPosition,
            Vector3 aimForward,
            bool isImpact)
        {
            return new CameraFraming(
                CameraMath.LocalOffset(ballPosition, aimForward, tuning.SwingOffset),
                ballPosition + Vector3.up * 0.45f + aimForward * 2.5f,
                tuning.SwingFieldOfView + (isImpact ? tuning.ImpactFovKick : 0f),
                isImpact ? "Impact Point" : "Swing Address");
        }

        private static CameraFraming ResolveFollow(
            CameraTuningData tuning,
            Vector3 ballPosition,
            Vector3 resetPosition,
            Vector3 velocity,
            Vector3 velocityForward,
            float ballSpeed)
        {
            float extension = CameraMath.FollowDistance(
                ballSpeed,
                tuning.SpeedDistanceScale,
                tuning.MaximumDistanceExtension);
            Vector3 offset = tuning.FollowOffset + new Vector3(0f, 0f, -extension);
            if (ballPosition.y - resetPosition.y >= tuning.ApexWideHeight)
            {
                offset.y += tuning.ApexHeightExtension;
            }

            float fov = Mathf.Lerp(
                tuning.FollowFieldOfView,
                tuning.MaximumFollowFieldOfView,
                Mathf.Clamp01(ballSpeed / tuning.FovSpeedReference));
            return new CameraFraming(
                CameraMath.LocalOffset(ballPosition, velocityForward, offset),
                ballPosition + velocity * tuning.FollowLookAheadSeconds,
                fov,
                "Ball Velocity Look-Ahead");
        }

        private static CameraFraming ResolveLanding(
            CameraTuningData tuning,
            Vector3 ballPosition,
            Vector3 velocityForward)
        {
            return new CameraFraming(
                CameraMath.LocalOffset(ballPosition, velocityForward, tuning.LandingOffset),
                ballPosition + Vector3.up * 0.25f + velocityForward * 2f,
                tuning.LandingFieldOfView,
                "Landing Ball");
        }

        private static CameraFraming ResolveNextShot(
            CameraTuningData tuning,
            Vector3 ballPosition,
            Vector3 cupPosition)
        {
            return new CameraFraming(
                CameraMath.LocalOffset(ballPosition, cupPosition - ballPosition, tuning.NextShotOffset),
                ballPosition + Vector3.up * 0.35f,
                tuning.AddressFieldOfView,
                "Next Lie");
        }

        private static CameraFraming ResolvePutt(
            CameraTuningData tuning,
            Vector3 ballPosition,
            Vector3 cupPosition,
            Vector3 aimForward)
        {
            Vector3 cupForward = CameraMath.ResolvePlanarForward(cupPosition - ballPosition, aimForward);
            Vector3 midpoint = Vector3.Lerp(ballPosition, cupPosition, 0.5f);
            float puttDistance = Vector3.ProjectOnPlane(cupPosition - ballPosition, Vector3.up).magnitude;
            float distanceExtension = Mathf.Min(
                puttDistance * tuning.PuttDistanceScale,
                tuning.PuttMaximumDistanceExtension);
            Vector3 adaptiveOffset = tuning.PuttOffset + new Vector3(
                0f,
                puttDistance * tuning.PuttHeightScale,
                -distanceExtension);
            float adaptiveFov = Mathf.Min(
                tuning.PuttMaximumFieldOfView,
                tuning.PuttFieldOfView + puttDistance * tuning.PuttFovPerMeter);
            return new CameraFraming(
                CameraMath.LocalOffset(midpoint, cupForward, adaptiveOffset),
                midpoint + Vector3.up * 0.18f,
                adaptiveFov,
                "Ball and Cup");
        }

        private static CameraFraming ResolveHoleComplete(
            CameraTuningData tuning,
            Vector3 cupPosition,
            Vector3 previousPlanarDirection)
        {
            return new CameraFraming(
                CameraMath.LocalOffset(cupPosition, previousPlanarDirection, tuning.HoleCompleteOffset),
                cupPosition + Vector3.up * 0.2f,
                tuning.HoleCompleteFieldOfView,
                "Cup");
        }

        private static CameraFraming ResolveResult(
            CameraTuningData tuning,
            Vector3 resetPosition,
            Vector3 cupPosition)
        {
            return new CameraFraming(
                CameraMath.LocalOffset(cupPosition, cupPosition - resetPosition, tuning.ResultOffset),
                cupPosition + Vector3.up * 0.75f,
                tuning.ResultFieldOfView,
                "Hole Result");
        }
    }
}

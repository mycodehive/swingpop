using SwingPop.Data;
using UnityEngine;

namespace SwingPop.CameraSystem
{
    public static class PuttResultFramingSolver
    {
        public static CameraFraming ResolvePutt(
            PuttResultCinematicTuningData tuning,
            Vector3 ballPosition,
            Vector3 cupPosition,
            Vector3 characterPosition,
            Vector3 fallbackForward,
            bool isRolling,
            bool isApproaching)
        {
            Vector3 forward = CameraMath.ResolvePlanarForward(cupPosition - ballPosition, fallbackForward);
            if (isApproaching)
            {
                return new CameraFraming(
                    CameraMath.LocalOffset(cupPosition, forward, tuning.ApproachOffset),
                    cupPosition + Vector3.up * tuning.ApproachLookHeight,
                    tuning.ApproachFieldOfView,
                    "Cup Approach");
            }

            float cupBias = isRolling ? tuning.PuttRollingCupBias : tuning.PuttAddressCupBias;
            Vector3 ballCupTarget = Vector3.Lerp(ballPosition, cupPosition, cupBias);
            Vector3 target = isRolling
                ? ballCupTarget
                : Vector3.Lerp(characterPosition + Vector3.up * 0.85f, ballCupTarget, 0.68f);
            Vector3 offset = isRolling ? tuning.PuttRollingOffset : tuning.PuttAddressOffset;
            float lookHeight = isRolling ? tuning.PuttRollingLookHeight : tuning.PuttAddressLookHeight;
            float fov = isRolling ? tuning.PuttRollingFieldOfView : tuning.PuttAddressFieldOfView;
            return new CameraFraming(
                CameraMath.LocalOffset(ballCupTarget, forward, offset),
                target + Vector3.up * lookHeight,
                fov,
                isRolling ? "Rolling Ball and Cup" : "Putt Character Ball and Cup");
        }

        public static CameraFraming ResolveHoleIn(
            PuttResultCinematicTuningData tuning,
            Vector3 cupPosition,
            Vector3 courseForward)
        {
            Vector3 forward = CameraMath.ResolvePlanarForward(courseForward, Vector3.forward);
            return new CameraFraming(
                CameraMath.LocalOffset(cupPosition, forward, tuning.HoleInOffset),
                cupPosition + Vector3.up * tuning.HoleInLookHeight,
                tuning.HoleInFieldOfView,
                "Hole-In Cup");
        }

        public static CameraFraming ResolveResult(
            PuttResultCinematicTuningData tuning,
            Vector3 characterPosition,
            Vector3 cupPosition,
            Vector3 courseForward)
        {
            Vector3 forward = CameraMath.ResolvePlanarForward(courseForward, Vector3.forward);
            Vector3 target = Vector3.Lerp(characterPosition, cupPosition, tuning.ResultCupBias)
                             + Vector3.up * tuning.ResultLookHeight;
            return new CameraFraming(
                CameraMath.LocalOffset(target, forward, tuning.ResultOffset),
                target,
                tuning.ResultFieldOfView,
                "Character Cup Result");
        }
    }
}

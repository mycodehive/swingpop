using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.UI
{
    public readonly struct HudActionPresentation
    {
        public HudActionPresentation(string label, bool visible, bool interactable)
        {
            Label = label;
            Visible = visible;
            Interactable = interactable;
        }

        public string Label { get; }
        public bool Visible { get; }
        public bool Interactable { get; }
    }

    public static class HudPresentationMapper
    {
        public static HudActionPresentation MapPrimaryAction(
            ShotFlowState shotState,
            HoleFlowState holeState)
        {
            if (holeState == HoleFlowState.HoleComplete)
            {
                return new HudActionPresentation(string.Empty, false, false);
            }

            return shotState switch
            {
                ShotFlowState.Aiming => new HudActionPresentation("START SHOT", true, true),
                ShotFlowState.PowerSelecting => new HudActionPresentation("SET POWER", true, true),
                ShotFlowState.ImpactSelecting => new HudActionPresentation("IMPACT", true, true),
                _ => new HudActionPresentation(string.Empty, false, false)
            };
        }

        public static string FormatSpin(SpinPreset preset, bool spinEnabled)
        {
            if (!spinEnabled)
            {
                return "SPIN DISABLED";
            }

            return preset switch
            {
                SpinPreset.TopSpin => "TOP SPIN  ^",
                SpinPreset.BackSpin => "BACK SPIN  v",
                SpinPreset.LeftSideSpin => "LEFT SPIN  <",
                SpinPreset.RightSideSpin => "RIGHT SPIN  >",
                _ => "NO SPIN  --"
            };
        }

        public static string FormatHeightDifference(float meters)
        {
            if (Mathf.Abs(meters) < 0.05f)
            {
                return "LEVEL  0.0 m";
            }

            return meters > 0f ? $"▲ +{meters:0.0} m" : $"▼ {meters:0.0} m";
        }

        public static string FormatLiveScore(int strokes, int penalties)
        {
            return penalties > 0
                ? $"STROKE {strokes}  +{penalties} PEN"
                : $"STROKE {strokes}";
        }

        public static string FormatResultRelative(ScoreResult result)
        {
            string relative = result.RelativeToPar switch
            {
                > 0 => $"+{result.RelativeToPar}",
                0 => "EVEN",
                _ => result.RelativeToPar.ToString()
            };
            return $"{result.Label.ToUpperInvariant()}  {relative}";
        }

        public static float WindArrowAngle(Vector3 worldDirection)
        {
            Vector3 planar = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
            if (planar.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            planar.Normalize();
            return -Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
        }

        public static string FormatHazard(TerrainSurfaceType hazard)
        {
            return hazard switch
            {
                TerrainSurfaceType.Water => "WATER HAZARD\n+1 PENALTY",
                TerrainSurfaceType.OutOfBounds => "OUT OF BOUNDS\n+1 PENALTY",
                _ => $"{FormatLie(hazard)}\n+1 PENALTY"
            };
        }

        public static string FormatLie(TerrainSurfaceType lie)
        {
            return lie == TerrainSurfaceType.OutOfBounds
                ? "OUT OF BOUNDS"
                : lie.ToString().ToUpperInvariant();
        }
    }
}

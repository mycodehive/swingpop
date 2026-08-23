using SwingPop.CameraSystem;
using SwingPop.CharacterSystem;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Shot;
using SwingPop.Gameplay.Wind;
using SwingPop.Gameplay.Hole;
using SwingPop.AudioSystem;
using SwingPop.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SwingPop.Debugging
{
    public sealed class ShotDebugOverlay : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private GolfBallController ball;
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private WindController wind;
        [SerializeField] private HoleFlowController holeFlow;
        [SerializeField] private CameraDirector cameraDirector;
        [SerializeField] private CharacterGolfController character;
        [SerializeField] private ShotPresentationController shotPresentation;
        [SerializeField] private GameplayAudioController gameplayAudio;
        [SerializeField] private BallTrajectoryDebug trajectoryDebug;

        [Header("Presentation")]
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private bool startHidden;
        [SerializeField] private bool syncTrajectoryVisibility;
        [SerializeField] private bool showAimLine = true;
        [SerializeField, Min(1f)] private float aimLineLength = 24f;
        [SerializeField] private Vector2 overlayPosition = new(16f, 16f);
        [SerializeField] private Vector2 overlaySize = new(570f, 680f);

        private GUIStyle headingStyle;
        private GUIStyle labelStyle;

        public bool IsOverlayVisible => showOverlay;

        private void Awake()
        {
            if (startHidden)
            {
                SetOverlayVisible(false);
            }
        }

        private void LateUpdate()
        {
            if (Keyboard.current != null
                && (Keyboard.current.f1Key.wasPressedThisFrame || Keyboard.current.hKey.wasPressedThisFrame))
            {
                SetOverlayVisible(!showOverlay);
            }
            UpdateAimLine();
        }

        public void SetOverlayVisible(bool visible)
        {
            showOverlay = visible;
            if (syncTrajectoryVisibility && trajectoryDebug != null)
            {
                trajectoryDebug.SetTrajectoryVisible(visible);
            }
        }

        private void OnGUI()
        {
            if (!showOverlay || shotFlow == null || ball == null)
            {
                return;
            }

            EnsureStyles();
            GUILayout.BeginArea(new Rect(overlayPosition, overlaySize), GUI.skin.box);
            GUILayout.Label("SWINGPOP GAMEPLAY DEBUG", headingStyle);
            GUILayout.Label("H: Toggle Debug Overlay", labelStyle);
            if (cameraDirector != null)
            {
                GUILayout.Label(
                    $"Camera: {cameraDirector.CurrentMode}  Previous: {cameraDirector.PreviousMode}  Transition: {(cameraDirector.IsTransitioning ? "Blending" : "Stable")}",
                    labelStyle);
                GUILayout.Label(
                    $"Camera Target: {cameraDirector.CurrentTargetName}  FOV: {cameraDirector.CurrentFieldOfView:F1}  Follow Distance: {cameraDirector.CurrentFollowDistance:F1} m",
                    labelStyle);
            }
            if (holeFlow != null && holeFlow.Hole != null)
            {
                GUILayout.Label(
                    $"Hole {holeFlow.Hole.HoleNumber}: {holeFlow.Hole.DisplayName}  Par {holeFlow.Hole.Par}",
                    labelStyle);
                GUILayout.Label(
                    $"Hole State: {holeFlow.State}  Stroke: {holeFlow.StrokeCount}  Penalty: {holeFlow.PenaltyCount}",
                    labelStyle);
                GUILayout.Label(
                    $"Current Club: {(holeFlow.CurrentClub != null ? holeFlow.CurrentClub.DisplayName : "None")}",
                    labelStyle);
                GUILayout.Label(
                    $"Remaining / Cup Distance: {holeFlow.RemainingDistance:F1} m  Height Difference: {holeFlow.HeightDifference:+0.0;-0.0;0.0} m",
                    labelStyle);
                GUILayout.Label(
                    holeFlow.State == HoleFlowState.HoleComplete
                        ? $"RESULT: {holeFlow.Result}"
                        : "Score Result: Playing",
                    labelStyle);
            }
            GUILayout.Label($"Shot State: {shotFlow.State}    Ball: {ball.State}", labelStyle);
            if (shotPresentation != null)
            {
                GUILayout.Label(
                    $"Impact VFX: {shotPresentation.LastImpactLevel} x{shotPresentation.ImpactPresentationCount}  Landing: {shotPresentation.LastLandingEffect} x{shotPresentation.SurfacePresentationCount}",
                    labelStyle);
                GUILayout.Label(
                    $"Hole VFX: x{shotPresentation.HolePresentationCount}  Reusable FX Objects: {shotPresentation.ReusableEffectObjectCount}",
                    labelStyle);
            }
            if (gameplayAudio != null)
            {
                GUILayout.Label(
                    $"Audio: {gameplayAudio.LastCue} x{gameplayAudio.TotalCueCount}  Procedural Fallbacks: {gameplayAudio.GeneratedFallbackCount}",
                    labelStyle);
            }
            if (character != null)
            {
                GUILayout.Label(
                    $"Character: {character.CurrentState}  Animation: {character.AnimationState}  Aim: {character.CharacterAimAngle:+0.0;-0.0;0.0}°",
                    labelStyle);
                GUILayout.Label(
                    $"Impact Event: {(character.ImpactEventFired ? "Fired" : "Waiting/Idle")}  Pending Launch: {shotFlow.HasPendingBallLaunch}  Club Visual: {character.CurrentClubVisual}",
                    labelStyle);
                GUILayout.Label(
                    $"Fallback Launch Used: {shotFlow.LastBallLaunchUsedFallback}",
                    labelStyle);
            }
            GUILayout.Label($"Aim: {shotFlow.AimAngleDegrees:+0.0;-0.0;0.0}°", labelStyle);
            DrawMeter("Power", shotFlow.Power01, new Color(0.2f, 0.9f, 0.45f));
            GUILayout.Label($"Power: {shotFlow.Power01 * 100f:0}%", labelStyle);
            DrawImpactMeter();
            GUILayout.Label(
                $"Impact: {shotFlow.PreviewImpactGrade}  Cursor {shotFlow.ImpactCursor:+0.00;-0.00;0.00}",
                labelStyle);
            GUILayout.Label($"Ball Speed: {ball.Speed:F2} m/s", labelStyle);
            GUILayout.Label($"Velocity: {ball.Velocity.x:+0.0;-0.0;0.0}, {ball.Velocity.y:+0.0;-0.0;0.0}, {ball.Velocity.z:+0.0;-0.0;0.0}", labelStyle);
            GUILayout.Label($"Selected Spin: {shotFlow.SelectedSpinPreset} [{shotFlow.SelectedSpin}]", labelStyle);
            GUILayout.Label($"Active Spin: [{ball.CurrentSpin}]", labelStyle);
            GUILayout.Label(
                wind != null
                    ? $"Wind: {wind.CurrentPreset}  {wind.Strength:F1} m/s  Dir ({wind.Direction.x:+0.0;-0.0;0.0}, {wind.Direction.z:+0.0;-0.0;0.0})"
                    : "Wind: Not connected",
                labelStyle);
            GUILayout.Label($"Surface / Lie: {ball.CurrentLie}", labelStyle);
            GUILayout.Label(
                ball.CurrentSurfaceData != null
                    ? $"Surface: Power x{ball.CurrentSurfaceData.PowerModifier:F2}  Friction {ball.CurrentSurfaceData.Friction:F2}  Bounce x{ball.CurrentSurfaceData.BounceModifier:F2}  Spin x{ball.CurrentSurfaceData.SpinResponse:F2}  Roll x{ball.CurrentSurfaceData.RollingResistance:F2}"
                    : "Surface modifiers: default neutral",
                labelStyle);
            GUILayout.Label(
                ball.HasLastHazard ? $"Last Hazard: {ball.LastHazard} (automatic +1 recovery)" : "Last Hazard: None",
                labelStyle);
            GUILayout.Label("A/D or ←/→: Aim   Space: Confirm   R: Reset   Esc: Cancel", labelStyle);
            GUILayout.Label("P during Impact: Force PERFECT (Debug)", labelStyle);
            GUILayout.Label("Spin: 1 None  2 Top  3 Back  4 Left  5 Right", labelStyle);
            GUILayout.Label("Wind: 6 Calm  7 Tail  8 Head  9 Left  0 Right", labelStyle);
            GUILayout.Label("Stopped balls continue from the lie automatically. R: debug restart Hole 1", labelStyle);
            GUILayout.Label(
                shotFlow.HasLastShotCommand
                    ? $"Last: {shotFlow.LastShotCommand}"
                    : "Last: No ShotCommand yet",
                labelStyle);
            GUILayout.EndArea();
        }

        private void UpdateAimLine()
        {
            if (aimLine == null || shotFlow == null || ball == null)
            {
                return;
            }

            bool shouldShow = showAimLine && shotFlow.State != ShotFlowState.ShotCommitted;
            aimLine.enabled = shouldShow;
            if (!shouldShow)
            {
                return;
            }

            Vector3 origin = ball.transform.position + Vector3.up * 0.08f;
            Vector3 direction = shotFlow.AimDirection.normalized;
            aimLine.SetPosition(0, origin);
            aimLine.SetPosition(1, origin + direction * aimLineLength);
            Debug.DrawRay(origin, direction * aimLineLength, Color.cyan);
        }

        private void DrawMeter(string label, float value01, Color fillColor)
        {
            Rect rect = GUILayoutUtility.GetRect(420f, 18f);
            GUI.Box(rect, GUIContent.none);
            Rect fillRect = new(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(value01), rect.height - 4f);
            Color previousColor = GUI.color;
            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(rect, label, labelStyle);
        }

        private void DrawImpactMeter()
        {
            Rect rect = GUILayoutUtility.GetRect(420f, 20f);
            GUI.Box(rect, GUIContent.none);
            float centerX = rect.x + rect.width * 0.5f;
            Color previousColor = GUI.color;
            GUI.color = new Color(0.25f, 1f, 0.75f);
            GUI.DrawTexture(new Rect(centerX - 3f, rect.y + 2f, 6f, rect.height - 4f), Texture2D.whiteTexture);
            GUI.color = Color.yellow;
            float cursorX = Mathf.Lerp(rect.x + 3f, rect.xMax - 3f, (shotFlow.ImpactCursor + 1f) * 0.5f);
            GUI.DrawTexture(new Rect(cursorX - 2f, rect.y, 4f, rect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            headingStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = Color.white }
            };
        }
    }
}

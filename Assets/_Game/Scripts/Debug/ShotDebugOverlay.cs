using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Shot;
using UnityEngine;

namespace SwingPop.Debugging
{
    public sealed class ShotDebugOverlay : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private GolfBallController ball;
        [SerializeField] private LineRenderer aimLine;

        [Header("Presentation")]
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private bool showAimLine = true;
        [SerializeField, Min(1f)] private float aimLineLength = 24f;
        [SerializeField] private Vector2 overlayPosition = new(16f, 16f);
        [SerializeField] private Vector2 overlaySize = new(460f, 300f);

        private GUIStyle headingStyle;
        private GUIStyle labelStyle;

        private void LateUpdate()
        {
            UpdateAimLine();
        }

        private void OnGUI()
        {
            if (!showOverlay || shotFlow == null || ball == null)
            {
                return;
            }

            EnsureStyles();
            GUILayout.BeginArea(new Rect(overlayPosition, overlaySize), GUI.skin.box);
            GUILayout.Label("M2 SHOT DEBUG", headingStyle);
            GUILayout.Label($"Shot State: {shotFlow.State}    Ball: {ball.State}", labelStyle);
            GUILayout.Label($"Aim: {shotFlow.AimAngleDegrees:+0.0;-0.0;0.0}°", labelStyle);
            DrawMeter("Power", shotFlow.Power01, new Color(0.2f, 0.9f, 0.45f));
            GUILayout.Label($"Power: {shotFlow.Power01 * 100f:0}%", labelStyle);
            DrawImpactMeter();
            GUILayout.Label(
                $"Impact: {shotFlow.PreviewImpactGrade}  Cursor {shotFlow.ImpactCursor:+0.00;-0.00;0.00}",
                labelStyle);
            GUILayout.Label($"Ball Speed: {ball.Speed:F2} m/s", labelStyle);
            GUILayout.Label("A/D or ←/→: Aim   Space: Confirm   R: Reset   Esc: Cancel", labelStyle);
            GUILayout.Label("P during Impact: Force PERFECT (Debug)", labelStyle);
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

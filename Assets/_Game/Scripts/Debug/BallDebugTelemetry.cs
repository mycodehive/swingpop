using SwingPop.Gameplay.Ball;
using UnityEngine;

namespace SwingPop.Debugging
{
    public sealed class BallDebugTelemetry : MonoBehaviour
    {
        [SerializeField] private GolfBallController ball;
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private bool showLaunchVector = true;
        [SerializeField, Min(0.1f)] private float launchVectorScale = 0.5f;
        [SerializeField] private Vector2 overlayPosition = new(16f, 16f);
        [SerializeField] private Vector2 overlaySize = new(310f, 178f);

        private GUIStyle labelStyle;

        private void Update()
        {
            if (showLaunchVector && ball != null && ball.State == BallState.Ready)
            {
                Debug.DrawRay(
                    ball.transform.position,
                    ball.GetPreviewLaunchVelocity() * launchVectorScale,
                    Color.yellow);
            }
        }

        private void OnGUI()
        {
            if (!showOverlay || ball == null)
            {
                return;
            }

            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            Rect area = new(overlayPosition, overlaySize);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("M1 BALL TELEMETRY", labelStyle);
            GUILayout.Label($"State: {ball.State}", labelStyle);
            GUILayout.Label($"Speed: {ball.Speed:F2} m/s", labelStyle);
            GUILayout.Label($"Velocity: {ball.Velocity:F2}", labelStyle);
            GUILayout.Label($"Grounded: {ball.IsGrounded}", labelStyle);
            GUILayout.Label("Space: Launch    R: Reset", labelStyle);
            GUILayout.EndArea();
        }
    }
}

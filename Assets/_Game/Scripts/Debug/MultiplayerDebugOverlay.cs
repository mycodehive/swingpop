using SwingPop.Online;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SwingPop.Debugging
{
    [DisallowMultipleComponent]
    public sealed class MultiplayerDebugOverlay : MonoBehaviour
    {
        [SerializeField] private MatchSessionController session;
        [SerializeField] private bool visible;
        [SerializeField] private Key toggleKey = Key.F2;
        [SerializeField] private Rect panelRect = new(12f, 250f, 390f, 220f);

        public bool Visible => visible;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || session == null) return;
            MatchSnapshot snapshot = session.CurrentSnapshot;
            Rect expandedRect = new(panelRect.x, panelRect.y, panelRect.width, Mathf.Max(panelRect.height, 290f));
            GUILayout.BeginArea(expandedRect, GUI.skin.box);
            GUILayout.Label("M12 ONLINE FOUNDATION");
            GUILayout.Label($"Mode: {session.ActiveMode}");
            GUILayout.Label($"Local: {session.LocalPlayerId}");
            if (snapshot == null)
            {
                GUILayout.Label("Snapshot: waiting");
            }
            else
            {
                GUILayout.Label($"Match: {snapshot.MatchId} / v{snapshot.ProtocolVersion}");
                GUILayout.Label($"Phase: {snapshot.Phase} / Turn: {snapshot.TurnState}");
                GUILayout.Label($"Current: {snapshot.CurrentTurnPlayer}");
                GUILayout.Label($"TurnIndex: {snapshot.TurnIndex} / ShotSequence: {snapshot.ShotSequence}");
                GUILayout.Label($"SnapshotVersion: {snapshot.Version}");
                for (int index = 0; index < snapshot.PlayerCount; index++)
                {
                    PlayerSnapshot player = snapshot.GetPlayer(index);
                    GUILayout.Label(
                        $"P{index + 1}: {player.PlayerId} | {player.Lie} | S{player.StrokeCount} P{player.PenaltyCount} | Holed={player.Holed}");
                }
            }
            LocalLoopbackTransport transport = session.Transport;
            if (transport != null)
            {
                GUILayout.Label($"Transport: LocalLoopback ({transport.SimulatedLatencyMs} ms)");
                GUILayout.Label($"Messages: {transport.MessageCount} / Pending: {transport.PendingMessageCount}");
                GUILayout.Label($"Payload: last shot {transport.LastShotSubmissionBytes} B / snapshot {transport.LastSnapshotBytes} B");
            }
            GUILayout.EndArea();
        }
    }
}

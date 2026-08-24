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

        public void SetVisible(bool value) => visible = value;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || session == null) return;
            MatchSnapshot snapshot = session.CurrentSnapshot;
            Rect expandedRect = new(panelRect.x, panelRect.y, Mathf.Max(panelRect.width, 430f), Mathf.Max(panelRect.height, 420f));
            GUILayout.BeginArea(expandedRect, GUI.skin.box);
            GUILayout.Label("M13 REAL NETWORK PROTOTYPE");
            GUILayout.Label($"Mode: {session.ActiveMode}");
            GUILayout.Label($"Local: {session.LocalPlayerId}");
            UnityTransportMatchTransport network = session.NetworkTransport;
            if (network != null && session.ActiveMode is MultiplayerDevelopmentMode.NetworkHost or MultiplayerDevelopmentMode.NetworkClient)
            {
                GUILayout.Label($"Role / Connection: {network.Role} / {network.ConnectionState}");
                string remoteId = session.ActiveMode == MultiplayerDevelopmentMode.NetworkHost ? "player-b" : "player-a";
                GUILayout.Label($"Endpoint: {network.Address}:{network.Port} | Remote: {(network.IsReady ? remoteId : "WAITING")}");
                GUILayout.Label($"RTT: {network.RoundTripTimeMilliseconds:F0} ms | Msg: {network.MessageCount}");
                GUILayout.Label($"Envelope Seq: TX {network.OutboundSequence} / RX {network.InboundSequence}");
                GUILayout.Label($"Bytes: TX {network.SentBytes} / RX {network.ReceivedBytes}");
                GUILayout.Label($"Reject: {network.RejectedMessageCount} ({network.LastRejectionReason})");
                GUILayout.Label($"Hash: {network.LocalSnapshotHash} | Remote v{network.RemoteSnapshotVersion} | Desync: {network.DesyncCount}");
                GUILayout.Label($"Predicted error: {network.LastDesyncReport.PositionError:F3} m");
            }
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
            if (transport != null && session.ActiveMode is MultiplayerDevelopmentMode.OfflineSingle or MultiplayerDevelopmentMode.LocalTwoPlayer)
            {
                GUILayout.Label($"Transport: LocalLoopback ({transport.SimulatedLatencyMs} ms)");
                GUILayout.Label($"Messages: {transport.MessageCount} / Pending: {transport.PendingMessageCount}");
                GUILayout.Label($"Payload: last shot {transport.LastShotSubmissionBytes} B / snapshot {transport.LastSnapshotBytes} B");
            }
            GUILayout.EndArea();
        }
    }
}

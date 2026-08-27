using UnityEngine;
using UnityEngine.UI;

namespace SwingPop.Online
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class MultiplayerTurnPresenter : MonoBehaviour
    {
        [SerializeField] private MatchSessionController session;
        [SerializeField] private GameObject root;
        [SerializeField] private Text turnLabel;
        [SerializeField] private Text playerAScore;
        [SerializeField] private Text playerBScore;
        [SerializeField] private Button gameplayActionButton;

        public string TurnLabel => turnLabel != null ? turnLabel.text : string.Empty;
        public bool IsVisible => root != null && root.activeSelf;

        private void OnEnable()
        {
            if (session != null) session.SnapshotChanged += OnSnapshotChanged;
        }

        private void Start()
        {
            Refresh(session != null ? session.CurrentSnapshot : null);
        }

        private void LateUpdate()
        {
            if (session == null || session.ActiveMode == MultiplayerDevelopmentMode.OfflineSingle) return;
            if (gameplayActionButton != null) gameplayActionButton.interactable = session.CanSubmitShot;
        }

        private void OnDisable()
        {
            if (session != null) session.SnapshotChanged -= OnSnapshotChanged;
        }

        private void OnSnapshotChanged(MatchSnapshot snapshot)
        {
            Refresh(snapshot);
        }

        private void Refresh(MatchSnapshot snapshot)
        {
            bool visible = session != null && session.ActiveMode != MultiplayerDevelopmentMode.OfflineSingle;
            root?.SetActive(visible);
            if (!visible) return;
            if (snapshot == null)
            {
                if (turnLabel != null)
                    turnLabel.text = session.ActiveMode == MultiplayerDevelopmentMode.NetworkHost
                        ? "HOST WAITING" : "CONNECTING";
                if (playerAScore != null) playerAScore.text = "WAITING FOR MATCH";
                if (playerBScore != null) playerBScore.text = string.Empty;
                return;
            }

            if (turnLabel != null)
            {
                ReconnectClientState reconnectState = session.ReconnectController != null
                    ? session.ReconnectController.State : ReconnectClientState.None;
                turnLabel.text = reconnectState == ReconnectClientState.ConnectionLost
                    ? "CONNECTION LOST"
                    : reconnectState == ReconnectClientState.Reconnecting
                        ? "RECONNECTING"
                    : reconnectState == ReconnectClientState.ReconnectFailed
                        ? "RECONNECT FAILED"
                    : session.IsMatchSuspended
                        ? "WAITING FOR PLAYER"
                    : snapshot.Phase == MatchPhase.HoleComplete
                    ? "MATCH COMPLETE"
                    : snapshot.CurrentTurnPlayer == session.LocalPlayerId
                        ? snapshot.TurnState == TurnState.PreparingShot ? "YOUR TURN" : "COMMITTED"
                        : "OPPONENT TURN";
            }

            if (snapshot.PlayerCount > 0 && playerAScore != null)
                playerAScore.text = FormatPlayer(snapshot.GetPlayer(0), snapshot.CurrentTurnPlayer);
            if (snapshot.PlayerCount > 1 && playerBScore != null)
                playerBScore.text = FormatPlayer(snapshot.GetPlayer(1), snapshot.CurrentTurnPlayer);
        }

        private static string FormatPlayer(PlayerSnapshot player, MatchPlayerId current)
        {
            string marker = player.PlayerId == current ? "●" : "○";
            string done = player.Holed ? "  HOLED" : string.Empty;
            string connection = player.ConnectionState == PlayerConnectionState.Connected
                ? string.Empty : $"  {player.ConnectionState.ToString().ToUpperInvariant()}";
            return $"{marker} {player.DisplayName}   {player.StrokeCount}{done}{connection}";
        }
    }
}

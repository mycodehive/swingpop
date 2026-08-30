using UnityEngine;
using UnityEngine.InputSystem;

namespace SwingPop.Online
{
    /// <summary>Minimal development-only Lobby presentation. It never mutates Lobby state directly.</summary>
    [DisallowMultipleComponent]
    public sealed class LobbyDevelopmentView : MonoBehaviour
    {
        [SerializeField] private LobbyDevelopmentController controller;
        [SerializeField] private string roomName = "SwingPop Room 01";
        [SerializeField] private bool showDebug;
        private Vector2 scroll;
        private TextMesh captureTelemetry;

        public void Configure(LobbyDevelopmentController value) => controller = value;

        private void Awake()
        {
            GameObject telemetryObject = new("Lobby Capture Telemetry");
            telemetryObject.transform.SetParent(transform, false);
            telemetryObject.transform.position = new Vector3(-4.7f, 2.7f, 0f);
            captureTelemetry = telemetryObject.AddComponent<TextMesh>();
            captureTelemetry.anchor = TextAnchor.UpperLeft;
            captureTelemetry.alignment = TextAlignment.Left;
            captureTelemetry.fontSize = 32;
            captureTelemetry.characterSize = 0.065f;
            captureTelemetry.color = new Color(0.75f, 0.95f, 1f, 1f);
        }

        private void Update()
        {
            if (Keyboard.current?.f2Key.wasPressedThisFrame == true) showDebug = !showDebug;
            UpdateCaptureTelemetry();
        }

        public void RefreshCaptureTelemetry() => UpdateCaptureTelemetry();

        private void UpdateCaptureTelemetry()
        {
            if (captureTelemetry == null || controller == null) return;
            string content = "SWINGPOP DEVELOPMENT LOBBY\n" + controller.Status + "\n";
            LobbyMatchSnapshot room = controller.CurrentMatch;
            if (controller.IsInRoom && room != null)
            {
                content += $"\n{room.DisplayName}  {room.CurrentPlayers}/{room.MaxPlayers}  {room.State}\n";
                foreach (LobbyMatchMember member in room.Members)
                    content += $"{member.DisplayAlias} {(member.IsOwner ? "OWNER" : string.Empty)}  {member.ReadyState}\n";
                content += "\nREADY   START   LEAVE";
            }
            else
            {
                content += "\nCREATE ROOM   REFRESH\n";
                foreach (LobbyMatchSnapshot listed in controller.MatchList)
                    content += $"{listed.DisplayName}  {listed.CurrentPlayers}/{listed.MaxPlayers}  {listed.HoleId}  JOIN\n";
            }
            captureTelemetry.text = content;
        }

        private void OnGUI()
        {
            if (controller == null) return;
            GUI.Box(new Rect(20f, 20f, 520f, Screen.height - 40f), "SWINGPOP DEVELOPMENT LOBBY");
            GUILayout.BeginArea(new Rect(40f, 55f, 480f, Screen.height - 80f));
            GUILayout.Label(controller.Status);
            GUI.enabled = controller.IsAuthenticated && !controller.RequestPending;

            if (!controller.IsInRoom)
            {
                GUILayout.Space(12f);
                GUILayout.Label("ROOM NAME");
                roomName = GUILayout.TextField(roomName, LobbyProtocol.MaximumDisplayNameLength);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("CREATE ROOM", GUILayout.Height(36f))) controller.CreateRoom(roomName);
                if (GUILayout.Button("REFRESH", GUILayout.Height(36f))) controller.RefreshRooms();
                GUILayout.EndHorizontal();
                GUILayout.Space(12f);
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(Mathf.Max(100f, Screen.height - 260f)));
                LobbyMatchSnapshot[] rooms = controller.MatchList;
                if (rooms.Length == 0) GUILayout.Label("NO JOINABLE ROOMS");
                foreach (LobbyMatchSnapshot room in rooms)
                {
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label($"{room.DisplayName}   {room.CurrentPlayers}/{room.MaxPlayers}   {room.HoleId}   {room.State}");
                    if (GUILayout.Button("JOIN", GUILayout.Width(72f))) controller.JoinRoom(room.LobbyMatchId);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            else
            {
                LobbyMatchSnapshot room = controller.CurrentMatch;
                GUILayout.Space(12f);
                GUILayout.Label($"{room.DisplayName}   {room.CurrentPlayers}/{room.MaxPlayers}");
                foreach (LobbyMatchMember member in room.Members)
                    GUILayout.Label($"{member.DisplayAlias} {(member.IsOwner ? "OWNER" : string.Empty)}   {member.ReadyState}");
                GUILayout.Space(12f);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(controller.CurrentReady ? "NOT READY" : "READY", GUILayout.Height(36f)))
                    controller.ToggleReady();
                if (GUILayout.Button("START", GUILayout.Height(36f))) controller.StartRoomMatch();
                if (GUILayout.Button("LEAVE", GUILayout.Height(36f))) controller.LeaveRoom();
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;
            if (showDebug && controller.Transport != null)
            {
                GUILayout.Space(18f);
                LobbyNetworkTransport transport = controller.Transport;
                GUILayout.Label($"[M17] Auth={transport.AuthenticationState} Connection={transport.ConnectionState}");
                GUILayout.Label($"LobbyMatch={controller.CurrentMatchId} Revision={controller.CurrentMatch?.Revision ?? 0}");
                GUILayout.Label($"Membership={(controller.IsInRoom ? "MEMBER" : "BROWSER")} Ready={controller.CurrentReady}");
                GUILayout.Label($"TX={transport.SentBytes} RX={transport.ReceivedBytes} LastReject={transport.LastRejection}");
                GUILayout.Label("Join ticket secrets are intentionally hidden.");
            }
            GUILayout.EndArea();
        }
    }
}

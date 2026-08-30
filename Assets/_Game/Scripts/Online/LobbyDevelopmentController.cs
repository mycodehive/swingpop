using System;
using System.IO;
using SwingPop.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwingPop.Online
{
    /// <summary>Coordinates development Lobby commands and scene handoff. It owns no Lobby or gameplay authority.</summary>
    [DisallowMultipleComponent]
    public sealed class LobbyDevelopmentController : MonoBehaviour
    {
        public const string LobbyServiceArgument = "-swingpopLobbyService";
        public const string LobbyClientArgument = "-swingpopLobbyClient";
        public const string MatchServerExecutableArgument = "-swingpopMatchServerExecutable=";
        public const string EvidenceDirectoryArgument = "-swingpopM17EvidenceDirectory=";
        public const string ConnectivityModeArgument = "-swingpopConnectivityMode=";
        public const string RelayExecutableArgument = "-swingpopRelayExecutable=";

        [SerializeField] private LobbyDevelopmentSettings settings;
        [SerializeField] private MultiplayerDevelopmentSettings authenticationSettings;
        [SerializeField] private ConnectivityDevelopmentSettings connectivitySettings;
        [SerializeField] private LobbyNetworkTransport transport;
        [SerializeField] private LobbyDevelopmentView view;

        private bool requestPending;
        private string status = "NOT STARTED";
        private LobbyMatchId currentMatchId;
        private bool currentReady;
        private string pendingRequestId = string.Empty;
        private int localSlot = -1;

        public LobbyNetworkTransport Transport => transport;
        public string Status => status;
        public bool RequestPending => requestPending;
        public LobbyMatchId CurrentMatchId => currentMatchId;
        public LobbyMatchSnapshot CurrentMatch => transport != null ? transport.LatestMatch : null;
        public LobbyMatchSnapshot[] MatchList => transport != null ? transport.LatestList : Array.Empty<LobbyMatchSnapshot>();
        public bool IsAuthenticated => transport != null
            && transport.AuthenticationState == AuthenticationClientState.Authenticated;
        public bool IsInRoom => currentMatchId.IsValid && CurrentMatch != null
            && CurrentMatch.LobbyMatchId == currentMatchId && CurrentMatch.State != LobbyMatchState.Closed;
        public bool CurrentReady => currentReady;

        private void OnEnable()
        {
            if (transport == null) return;
            transport.AuthenticationAccepted += OnAuthenticationAccepted;
            transport.AuthenticationRejected += OnAuthenticationRejected;
            transport.MatchListReceived += OnMatchList;
            transport.MatchUpdated += OnMatchUpdated;
            transport.AdmissionGranted += OnAdmissionGranted;
            transport.OperationRejected += OnOperationRejected;
            transport.Disconnected += OnDisconnected;
        }

        private void OnDisable()
        {
            if (transport == null) return;
            transport.AuthenticationAccepted -= OnAuthenticationAccepted;
            transport.AuthenticationRejected -= OnAuthenticationRejected;
            transport.MatchListReceived -= OnMatchList;
            transport.MatchUpdated -= OnMatchUpdated;
            transport.AdmissionGranted -= OnAdmissionGranted;
            transport.OperationRejected -= OnOperationRejected;
            transport.Disconnected -= OnDisconnected;
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (HasArgument(args, LobbyServiceArgument)) StartService(args);
            else if (HasArgument(args, LobbyClientArgument)) StartClient(args);
            else
            {
                status = "SELECT A DEVELOPMENT ROLE VIA BUILD/LAUNCH TOOL";
                Debug.Log("[M17][Lobby] No Lobby role argument. Use -swingpopLobbyService or -swingpopLobbyClient.", this);
            }
        }

        public void CreateRoom(string displayName)
        {
            if (!CanRequest()) return;
            pendingRequestId = RequestId();
            requestPending = transport.CreateMatch(new CreateMatchRequest(pendingRequestId, displayName,
                LobbyProtocol.MatchPlayerCapacity, LobbyProtocol.SupportedHoleId, LobbyVisibility.Public));
            if (requestPending) status = "CREATING ROOM";
        }

        public void RefreshRooms()
        {
            if (!CanRequest()) return;
            pendingRequestId = RequestId();
            requestPending = transport.ListMatches(new ListMatchesRequest(pendingRequestId, true));
            if (requestPending) status = "REFRESHING";
        }

        public void JoinRoom(LobbyMatchId id)
        {
            if (!CanRequest() || !id.IsValid) return;
            pendingRequestId = RequestId();
            requestPending = transport.JoinMatch(new LobbyMatchRequest(pendingRequestId, id));
            if (requestPending) status = "JOINING";
        }

        public void LeaveRoom()
        {
            if (!CanRequest() || !currentMatchId.IsValid) return;
            pendingRequestId = RequestId();
            requestPending = transport.LeaveMatch(new LobbyMatchRequest(pendingRequestId, currentMatchId));
            if (requestPending) status = "LEAVING";
        }

        public void ToggleReady()
        {
            if (!CanRequest() || !currentMatchId.IsValid) return;
            pendingRequestId = RequestId();
            requestPending = transport.SetReady(new SetReadyRequest(pendingRequestId, currentMatchId, !currentReady));
            if (requestPending) status = "UPDATING READY";
        }

        public void StartRoomMatch()
        {
            if (!CanRequest() || !currentMatchId.IsValid) return;
            pendingRequestId = RequestId();
            requestPending = transport.StartMatch(new LobbyMatchRequest(pendingRequestId, currentMatchId));
            if (requestPending) status = "STARTING MATCH";
        }

        private bool CanRequest() => transport != null && IsAuthenticated && !requestPending;

        private void StartService(string[] args)
        {
            if (view != null) view.enabled = false;
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsInactive.Include)) camera.enabled = false;
            if (settings == null || authenticationSettings == null
                || !AuthenticationController.TryLoadServerSigningKey(args, out byte[] key))
            {
                status = "LOBBY SERVICE AUTH KEY MISSING";
                Debug.LogError("[M17][Lobby] Service requires -swingpopAuthKeyFile.", this);
                return;
            }
            string executable = MatchReservationFile.ReadArgument(args, MatchServerExecutableArgument);
            if (string.IsNullOrWhiteSpace(executable)) executable = ResolveProjectRelative(settings.MatchServerExecutable);
            string authKeyPath = MatchReservationFile.ReadArgument(args, AuthenticationController.ServerKeyFileArgument);
            string evidence = MatchReservationFile.ReadArgument(args, EvidenceDirectoryArgument);
            if (string.IsNullOrWhiteSpace(evidence)) evidence = Path.Combine(Path.GetTempPath(), "SwingPop", "M17");
            MatchConnectivityMode connectivityMode = ReadConnectivityMode(args,
                connectivitySettings != null ? connectivitySettings.DefaultMode : MatchConnectivityMode.Direct);
            IMatchConnectivityProvider connectivityProvider;
            if (connectivityMode == MatchConnectivityMode.Relay)
            {
                if (connectivitySettings == null)
                {
                    status = "RELAY SETTINGS MISSING";
                    Debug.LogError("[M18][Lobby] Relay mode requires M18 connectivity settings.", this);
                    return;
                }
                string relayExecutable = MatchReservationFile.ReadArgument(args, RelayExecutableArgument);
                if (string.IsNullOrWhiteSpace(relayExecutable))
                    relayExecutable = ResolveProjectRelative("Builds/M18Relay/SwingPopRelay.exe");
                connectivityProvider = new LocalRelayConnectivityProvider(relayExecutable,
                    connectivitySettings.RelayAddress, connectivitySettings.FirstRelayPort,
                    connectivitySettings.MaximumAllocations, connectivitySettings.AllocationTimeoutSeconds,
                    Mathf.RoundToInt(connectivitySettings.CredentialLifetimeSeconds * 1000f), evidence);
            }
            else connectivityProvider = new DirectMatchConnectivityProvider();
            DevelopmentGameServerAllocator allocator = new(executable, settings.MatchServerAddress,
                settings.FirstMatchServerPort, settings.MaximumActiveMatches,
                Mathf.RoundToInt(settings.JoinTicketLifetimeSeconds * 1000f),
                settings.ServerReadyTimeoutSeconds, authKeyPath, evidence,
                connectivityProvider: connectivityProvider);
            InMemoryLobbyService lobbyService = new(allocator, settings.MaximumRooms);
            DevelopmentAuthenticationProvider auth = new(key, authenticationSettings.DevelopmentAuthenticationIssuer);
            bool started = transport.StartService(settings.LobbyAddress, settings.LobbyPort,
                settings.MaximumConnections, settings.LobbyHandshakeTimeoutSeconds, auth,
                Mathf.RoundToInt(authenticationSettings.AuthenticationSessionLifetimeSeconds * 1000f),
                lobbyService, settings.VerboseLogging);
            status = started ? $"LOBBY SERVICE {settings.LobbyAddress}:{settings.LobbyPort}" : "LOBBY SERVICE FAILED";
            if (started) Debug.Log($"[M18][Lobby] Connectivity mode={connectivityMode}", this);
        }

        private void StartClient(string[] args)
        {
            string credentialPath = MatchReservationFile.ReadArgument(args,
                AuthenticationController.CredentialFileArgument);
            if (settings == null || string.IsNullOrWhiteSpace(credentialPath) || !File.Exists(credentialPath))
            {
                status = "CLIENT CREDENTIAL MISSING";
                Debug.LogError("[M17][Lobby] Client requires -swingpopAuthCredentialFile.", this);
                return;
            }
            string credential = File.ReadAllText(credentialPath).Trim();
            bool started = transport.StartClient(settings.LobbyAddress, settings.LobbyPort,
                settings.LobbyHandshakeTimeoutSeconds, credential, settings.VerboseLogging);
            status = started ? "CONNECTING TO LOBBY" : "LOBBY CONNECTION FAILED";
        }

        private void OnAuthenticationAccepted(AuthAcceptedMessage _)
        {
            requestPending = false;
            status = "LOBBY BROWSER";
            RefreshRooms();
        }

        private void OnAuthenticationRejected(AuthRejectedMessage message)
        {
            requestPending = false;
            status = "AUTH REJECTED: " + message.Reason;
        }

        private void OnMatchList(LobbyMatchListMessage message)
        {
            requestPending = false;
            status = $"ROOMS {message.Matches.Length}";
        }

        private void OnMatchUpdated(LobbyMatchUpdatedMessage message)
        {
            requestPending = false;
            if (message.Match == null) return;
            bool responseToLocalRequest = !string.IsNullOrEmpty(pendingRequestId)
                                          && string.Equals(message.RequestId, pendingRequestId, StringComparison.Ordinal);
            if (responseToLocalRequest && message.EventType == LobbyEventType.MatchCreated)
            {
                currentMatchId = message.Match.LobbyMatchId;
                localSlot = 0;
            }
            else if (responseToLocalRequest && message.EventType == LobbyEventType.MemberJoined)
            {
                currentMatchId = message.Match.LobbyMatchId;
                localSlot = message.Match.Members.Length - 1;
            }
            if (message.Match.LobbyMatchId == currentMatchId)
            {
                if (message.Match.State == LobbyMatchState.Closed)
                {
                    currentMatchId = default;
                    currentReady = false;
                    localSlot = -1;
                    status = "ROOM CLOSED";
                }
                else
                {
                    status = $"ROOM {message.Match.CurrentPlayers}/{message.Match.MaxPlayers} {message.Match.State}";
                    if (localSlot >= 0 && localSlot < message.Match.Members.Length)
                        currentReady = message.Match.Members[localSlot].ReadyState == LobbyReadyState.Ready;
                }
            }
        }

        private void OnAdmissionGranted(LobbyAdmissionGrantedMessage message)
        {
            requestPending = false;
            if (!message.Grant.IsValid)
            {
                status = "INVALID MATCH ADMISSION";
                return;
            }
            status = $"CONNECTING TO MATCH {message.Grant.GameMatchId}";
            MatchAdmissionHandoff.Set(message.Grant);
            SceneManager.LoadScene("Hole01_SkyIsland", LoadSceneMode.Single);
        }

        private void OnOperationRejected(LobbyOperationRejectedMessage message)
        {
            requestPending = false;
            status = "REJECTED: " + message.Reason;
        }

        private void OnDisconnected(string reason)
        {
            requestPending = false;
            status = "DISCONNECTED: " + reason;
        }

        private static string RequestId() => Guid.NewGuid().ToString("N");

        private static bool HasArgument(string[] args, string expected)
        {
            foreach (string value in args)
                if (string.Equals(value, expected, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string ResolveProjectRelative(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value)) return value ?? string.Empty;
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), value));
        }

        public static MatchConnectivityMode ReadConnectivityMode(string[] args, MatchConnectivityMode fallback)
        {
            string value = MatchReservationFile.ReadArgument(args, ConnectivityModeArgument);
            return Enum.TryParse(value, true, out MatchConnectivityMode parsed) ? parsed : fallback;
        }
    }
}

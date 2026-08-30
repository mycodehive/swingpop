using System;
using System.IO;
using System.Threading.Tasks;
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
        public const string EnableRealRelayTestsArgument = "-swingpopEnableRealRelayTests";
        public const string UnityEnvironmentArgument = "-swingpopUnityEnvironment=";
        public const string RelayRegionArgument = "-swingpopRelayRegion=";
        public const string RelayConnectionTypeArgument = "-swingpopRelayConnectionType=";
        public const string ControlPlaneEnvironmentArgument = "-swingpopControlPlaneEnvironment=";
        public const string LobbyEndpointArgument = "-swingpopLobbyEndpoint=";
        public const string LobbyBindAddressArgument = "-swingpopLobbyBindAddress=";
        public const string LobbyBindPortArgument = "-swingpopLobbyBindPort=";
        public const string HealthPortArgument = "-swingpopHealthPort=";

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
        private DevelopmentGameServerAllocator activeAllocator;
        private InMemoryLobbyService activeLobbyService;
        private ControlPlaneHealthServer healthServer;
        private float maintenanceElapsed;

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
            healthServer?.Dispose();
            healthServer = null;
            if (transport == null) return;
            transport.AuthenticationAccepted -= OnAuthenticationAccepted;
            transport.AuthenticationRejected -= OnAuthenticationRejected;
            transport.MatchListReceived -= OnMatchList;
            transport.MatchUpdated -= OnMatchUpdated;
            transport.AdmissionGranted -= OnAdmissionGranted;
            transport.OperationRejected -= OnOperationRejected;
            transport.Disconnected -= OnDisconnected;
        }

        private void Update()
        {
            if (activeAllocator == null) return;
            maintenanceElapsed += Time.unscaledDeltaTime;
            if (maintenanceElapsed < 5f) return;
            maintenanceElapsed = 0f;
            int reaped = activeAllocator.Reap(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (reaped > 0) Debug.Log($"[M20][Allocation] Reaped {reaped} expired/exited match server(s).", this);
        }

        private async void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (HasArgument(args, LobbyServiceArgument)) await StartServiceAsync(args);
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

        private async Task StartServiceAsync(string[] args)
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
            if (string.IsNullOrWhiteSpace(evidence)) evidence = Path.Combine(Path.GetTempPath(), "SwingPop", "M20");
            ControlPlaneEnvironment targetEnvironment = ReadEnvironment(args, settings.Environment);
            MatchConnectivityMode connectivityMode = ReadConnectivityMode(args,
                connectivitySettings != null ? connectivitySettings.DefaultMode : MatchConnectivityMode.Direct);
            if (targetEnvironment == ControlPlaneEnvironment.Staging
                && connectivityMode != MatchConnectivityMode.ProductionRelay)
            {
                status = "STAGING REQUIRES PRODUCTION RELAY";
                Debug.LogError("[M20][Security] Staging requires ProductionRelay; Direct/LocalRelay remain " +
                               "development-only and no fallback was attempted.", this);
                return;
            }
            IMatchConnectivityProvider connectivityProvider;
            if (connectivityMode == MatchConnectivityMode.LocalRelay)
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
            else if (connectivityMode == MatchConnectivityMode.ProductionRelay)
            {
                bool optIn = HasArgument(args, EnableRealRelayTestsArgument)
                             || connectivitySettings != null && connectivitySettings.EnableRealRelayTests;
                if (connectivitySettings == null || !optIn)
                {
                    status = "PRODUCTION RELAY BLOCKED: EXPLICIT OPT-IN REQUIRED";
                    Debug.LogError("[M19][Relay] Production Relay requires linked UGS configuration and " +
                                   "-swingpopEnableRealRelayTests. No Direct fallback was attempted.", this);
                    return;
                }
                string environment = MatchReservationFile.ReadArgument(args, UnityEnvironmentArgument);
                if (string.IsNullOrWhiteSpace(environment))
                    environment = connectivitySettings.UnityServicesEnvironment;
                string region = MatchReservationFile.ReadArgument(args, RelayRegionArgument);
                if (string.IsNullOrWhiteSpace(region)) region = connectivitySettings.ProductionRelayRegion;
                string connectionType = MatchReservationFile.ReadArgument(args, RelayConnectionTypeArgument);
                if (string.IsNullOrWhiteSpace(connectionType))
                    connectionType = connectivitySettings.ProductionRelayConnectionType;
                UnityRelayConnectivityProvider production = new(environment, region, connectionType,
                    connectivitySettings.RetryCount, connectivitySettings.RetryDelaySeconds,
                    connectivitySettings.AllocationTimeoutSeconds,
                    Mathf.RoundToInt(connectivitySettings.CredentialLifetimeSeconds * 1000f));
                status = "INITIALIZING PRODUCTION RELAY";
                if (!await production.PrepareAsync())
                {
                    status = "PRODUCTION RELAY BLOCKED: " + production.LastFailure.Error;
                    Debug.LogError($"[M19][Relay] Production Relay preparation failed safely: " +
                                   $"{production.LastFailure}. No Direct fallback was attempted.", this);
                    return;
                }
                connectivityProvider = production;
            }
            else connectivityProvider = new DirectMatchConnectivityProvider();
            MatchServerLaunchPolicy launchPolicy = targetEnvironment == ControlPlaneEnvironment.Staging
                ? MatchServerLaunchPolicy.Staging(settings.StagingServerMaximumLifetimeSeconds,
                    settings.ServerCompletionShutdownSeconds)
                : MatchServerLaunchPolicy.Development;
            DevelopmentGameServerAllocator allocator = new(executable, settings.MatchServerAddress,
                settings.FirstMatchServerPort, settings.MaximumActiveMatches,
                Mathf.RoundToInt(settings.JoinTicketLifetimeSeconds * 1000f),
                settings.ServerReadyTimeoutSeconds, authKeyPath, evidence,
                connectivityProvider: connectivityProvider, launchPolicy: launchPolicy);
            InMemoryLobbyService lobbyService = new(allocator, settings.MaximumRooms);
            DevelopmentAuthenticationProvider auth = new(key, authenticationSettings.DevelopmentAuthenticationIssuer);
            string bindAddress = ReadArgument(args, LobbyBindAddressArgument, settings.LobbyAddress);
            ushort bindPort = ReadPort(args, LobbyBindPortArgument, settings.LobbyPort);
            if (targetEnvironment == ControlPlaneEnvironment.Staging && !IsLoopback(bindAddress))
            {
                status = "STAGING LOBBY BIND MUST BE LOOPBACK";
                Debug.LogError("[M20][Security] Caddy is the only public listener; Lobby WS must bind loopback.", this);
                return;
            }
            string endpointValue = ReadArgument(args, LobbyEndpointArgument, settings.PublicLobbyEndpoint);
            string path = "/";
            ControlPlaneEndpoint publicEndpoint = default;
            if (targetEnvironment == ControlPlaneEnvironment.Staging
                && !ControlPlaneEndpoint.TryParse(endpointValue, true, out publicEndpoint,
                    out string endpointFailure))
            {
                status = "INVALID PUBLIC LOBBY ENDPOINT";
                Debug.LogError("[M20][TLS] " + endpointFailure, this);
                return;
            }
            if (targetEnvironment == ControlPlaneEnvironment.Staging) path = publicEndpoint.Path;
            ControlPlaneTelemetry telemetry = new();
            bool started = transport.StartService(bindAddress, bindPort,
                settings.MaximumConnections, settings.LobbyHandshakeTimeoutSeconds, auth,
                Mathf.RoundToInt(authenticationSettings.AuthenticationSessionLifetimeSeconds * 1000f),
                lobbyService, settings.VerboseLogging, path, settings.CreateRateLimitPolicy(), telemetry);
            status = started ? $"LOBBY SERVICE {bindAddress}:{bindPort}{path}" : "LOBBY SERVICE FAILED";
            if (!started) return;
            activeAllocator = allocator;
            activeLobbyService = lobbyService;
            ushort healthPort = ReadPort(args, HealthPortArgument, settings.HealthPort);
            healthServer = new ControlPlaneHealthServer(() => new ControlPlaneHealthSnapshot(
                transport != null && transport.ConnectionState == NetworkConnectionState.Listening,
                transport != null ? transport.ConnectedPeerCount : 0,
                activeLobbyService?.MatchCount ?? 0,
                activeAllocator?.ActiveProcessCount ?? 0,
                activeAllocator?.ActiveAllocationCount ?? 0));
            if (!healthServer.Start(healthPort))
                Debug.LogError($"[M20][ControlPlane] Loopback health endpoint failed on port {healthPort}.", this);
            Debug.Log($"[M20][ControlPlane] environment={targetEnvironment} connectivity={connectivityMode} " +
                      $"parentBound={launchPolicy.BindToAllocatorParent} health=127.0.0.1:{healthPort}/healthz", this);
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
            ControlPlaneEnvironment targetEnvironment = ReadEnvironment(args, settings.Environment);
            string endpointValue = ReadArgument(args, LobbyEndpointArgument,
                targetEnvironment == ControlPlaneEnvironment.Staging
                    ? settings.PublicLobbyEndpoint
                    : $"ws://{settings.LobbyAddress}:{settings.LobbyPort}/");
            bool valid = ControlPlaneEndpoint.TryParse(endpointValue,
                targetEnvironment == ControlPlaneEnvironment.Staging, out ControlPlaneEndpoint endpoint,
                out string failure);
            if (!valid)
            {
                status = "INVALID LOBBY ENDPOINT";
                Debug.LogError("[M20][TLS] " + failure, this);
                return;
            }
            bool started = transport.StartClient(endpoint, settings.LobbyHandshakeTimeoutSeconds,
                credential, settings.VerboseLogging);
            status = started ? "CONNECTING TO LOBBY" : "LOBBY CONNECTION FAILED";
            if (started) Debug.Log($"[M20][TLS] Lobby client endpoint={endpoint.SafeLabel} " +
                                   $"certificateValidation={(endpoint.IsSecure ? "system-trust-required" : "development-plaintext")}", this);
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

        public static ControlPlaneEnvironment ReadEnvironment(string[] args, ControlPlaneEnvironment fallback)
        {
            string value = MatchReservationFile.ReadArgument(args, ControlPlaneEnvironmentArgument);
            return Enum.TryParse(value, true, out ControlPlaneEnvironment parsed) ? parsed : fallback;
        }

        private static string ReadArgument(string[] args, string prefix, string fallback)
        {
            string value = MatchReservationFile.ReadArgument(args, prefix);
            return string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value.Trim();
        }

        private static ushort ReadPort(string[] args, string prefix, ushort fallback)
        {
            string value = MatchReservationFile.ReadArgument(args, prefix);
            return ushort.TryParse(value, out ushort parsed) && parsed > 0 ? parsed : fallback;
        }

        private static bool IsLoopback(string value) => string.Equals(value, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                                                        || string.Equals(value, "localhost", StringComparison.OrdinalIgnoreCase)
                                                        || string.Equals(value, "::1", StringComparison.OrdinalIgnoreCase);
    }
}

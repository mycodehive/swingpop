using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "MultiplayerDevelopmentSettings", menuName = "SwingPop/Online/Development Settings")]
    public sealed class MultiplayerDevelopmentSettings : ScriptableObject
    {
        [SerializeField] private MultiplayerDevelopmentMode mode = MultiplayerDevelopmentMode.OfflineSingle;
        [SerializeField, Range(0, 1000)] private int simulatedLatencyMs;
        [SerializeField] private bool verboseLogging;
        [SerializeField, Min(0.1f)] private float simulatedRemoteShotDelay = 1.2f;
        [SerializeField, Range(0.1f, 1f)] private float simulatedRemotePower = 0.62f;
        [Header("M13 Network Prototype")]
        [SerializeField] private string hostAddress = "127.0.0.1";
        [SerializeField, Range(1024, 65535)] private int port = 7777;
        [SerializeField, Range(5f, 10f)] private float connectionTimeoutSeconds = 8f;
        [SerializeField, Range(15f, 120f)] private float connectionLivenessTimeoutSeconds = 30f;
        [Header("M14 Dedicated Authority")]
        [SerializeField, Range(2, 2)] private int dedicatedServerMaxPlayers = 2;
        [SerializeField] private bool disableServerPresentation = true;
        [SerializeField, Min(0f)] private float desyncWarningThreshold = 0.25f;
        [Header("M15 Match Lifecycle / Reconnect")]
        [SerializeField, Range(3f, 120f)] private float reconnectGraceSeconds = 30f;
        [SerializeField] private bool autoReconnectEnabled = true;
        [SerializeField, Range(1, 5)] private int reconnectAttemptLimit = 3;
        [SerializeField, Range(0.25f, 5f)] private float reconnectRetryDelaySeconds = 1f;
        [Header("M16 Authentication / Player Session")]
        [SerializeField] private bool developmentAuthenticationEnabled = true;
        [SerializeField] private string developmentAuthenticationIssuer = "swingpop-development";
        [SerializeField, Range(60f, 3600f)] private float authenticationTokenLifetimeSeconds = 900f;
        [SerializeField, Range(60f, 7200f)] private float authenticationSessionLifetimeSeconds = 1800f;
        [SerializeField, Range(5f, 10f)] private float authenticationTimeoutSeconds = 8f;
        [SerializeField] private bool verboseAuthenticationLogging;

        public MultiplayerDevelopmentMode Mode => mode;
        public int SimulatedLatencyMs => simulatedLatencyMs;
        public bool VerboseLogging => verboseLogging;
        public float SimulatedRemoteShotDelay => simulatedRemoteShotDelay;
        public float SimulatedRemotePower => simulatedRemotePower;
        public string HostAddress => string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress;
        public ushort Port => (ushort)Mathf.Clamp(port, 1, 65535);
        public float ConnectionTimeoutSeconds => connectionTimeoutSeconds;
        public float ConnectionLivenessTimeoutSeconds => Mathf.Clamp(connectionLivenessTimeoutSeconds, 15f, 120f);
        public int DedicatedServerMaxPlayers => Mathf.Clamp(dedicatedServerMaxPlayers, 2, 2);
        public bool DisableServerPresentation => disableServerPresentation;
        public float DesyncWarningThreshold => Mathf.Max(0f, desyncWarningThreshold);
        public float ReconnectGraceSeconds => Mathf.Clamp(reconnectGraceSeconds, 3f, 120f);
        public bool AutoReconnectEnabled => autoReconnectEnabled;
        public int ReconnectAttemptLimit => Mathf.Clamp(reconnectAttemptLimit, 1, 5);
        public float ReconnectRetryDelaySeconds => Mathf.Clamp(reconnectRetryDelaySeconds, 0.25f, 5f);
        public bool DevelopmentAuthenticationEnabled => developmentAuthenticationEnabled;
        public string DevelopmentAuthenticationIssuer => string.IsNullOrWhiteSpace(developmentAuthenticationIssuer)
            ? "swingpop-development" : developmentAuthenticationIssuer.Trim();
        public float AuthenticationTokenLifetimeSeconds => Mathf.Clamp(authenticationTokenLifetimeSeconds, 60f, 3600f);
        public float AuthenticationSessionLifetimeSeconds => Mathf.Clamp(authenticationSessionLifetimeSeconds, 60f, 7200f);
        public float AuthenticationTimeoutSeconds => Mathf.Clamp(authenticationTimeoutSeconds, 5f, 10f);
        public bool VerboseAuthenticationLogging => verboseAuthenticationLogging;

        public void ConfigureForDevelopment(
            MultiplayerDevelopmentMode developmentMode,
            int latencyMs,
            bool verbose,
            float remoteDelay = 1.2f,
            float remotePower = 0.62f)
        {
            mode = developmentMode;
            simulatedLatencyMs = Mathf.Clamp(latencyMs, 0, 1000);
            verboseLogging = verbose;
            simulatedRemoteShotDelay = Mathf.Max(0.1f, remoteDelay);
            simulatedRemotePower = Mathf.Clamp(remotePower, 0.1f, 1f);
        }

        public void ConfigureNetwork(string address, ushort networkPort, float timeoutSeconds = 8f)
        {
            hostAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
            port = networkPort < 1024 ? 7777 : networkPort;
            connectionTimeoutSeconds = Mathf.Clamp(timeoutSeconds, 5f, 10f);
        }

        public void ConfigureConnectionLiveness(float timeoutSeconds = 30f)
        {
            connectionLivenessTimeoutSeconds = Mathf.Clamp(timeoutSeconds, 15f, 120f);
        }

        public void ConfigureDedicatedServer(int maxPlayers = 2, bool disablePresentation = true,
            float positionDesyncThreshold = 0.25f)
        {
            dedicatedServerMaxPlayers = Mathf.Clamp(maxPlayers, 2, 2);
            disableServerPresentation = disablePresentation;
            desyncWarningThreshold = Mathf.Max(0f, positionDesyncThreshold);
        }

        public void ConfigureReconnect(float graceSeconds = 30f, bool autoReconnect = true,
            int attemptLimit = 3, float retryDelaySeconds = 1f)
        {
            reconnectGraceSeconds = Mathf.Clamp(graceSeconds, 3f, 120f);
            autoReconnectEnabled = autoReconnect;
            reconnectAttemptLimit = Mathf.Clamp(attemptLimit, 1, 5);
            reconnectRetryDelaySeconds = Mathf.Clamp(retryDelaySeconds, 0.25f, 5f);
        }

        public void ConfigureAuthentication(bool enabled = true, string issuer = "swingpop-development",
            float tokenLifetimeSeconds = 900f, float sessionLifetimeSeconds = 1800f,
            float timeoutSeconds = 8f, bool verbose = false)
        {
            developmentAuthenticationEnabled = enabled;
            developmentAuthenticationIssuer = string.IsNullOrWhiteSpace(issuer) ? "swingpop-development" : issuer.Trim();
            authenticationTokenLifetimeSeconds = Mathf.Clamp(tokenLifetimeSeconds, 60f, 3600f);
            authenticationSessionLifetimeSeconds = Mathf.Clamp(sessionLifetimeSeconds, 60f, 7200f);
            authenticationTimeoutSeconds = Mathf.Clamp(timeoutSeconds, 5f, 10f);
            verboseAuthenticationLogging = verbose;
        }
    }
}

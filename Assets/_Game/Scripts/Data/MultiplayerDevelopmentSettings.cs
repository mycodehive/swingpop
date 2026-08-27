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
        [Header("M14 Dedicated Authority")]
        [SerializeField, Range(2, 2)] private int dedicatedServerMaxPlayers = 2;
        [SerializeField] private bool disableServerPresentation = true;
        [SerializeField, Min(0f)] private float desyncWarningThreshold = 0.25f;

        public MultiplayerDevelopmentMode Mode => mode;
        public int SimulatedLatencyMs => simulatedLatencyMs;
        public bool VerboseLogging => verboseLogging;
        public float SimulatedRemoteShotDelay => simulatedRemoteShotDelay;
        public float SimulatedRemotePower => simulatedRemotePower;
        public string HostAddress => string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress;
        public ushort Port => (ushort)Mathf.Clamp(port, 1, 65535);
        public float ConnectionTimeoutSeconds => connectionTimeoutSeconds;
        public int DedicatedServerMaxPlayers => Mathf.Clamp(dedicatedServerMaxPlayers, 2, 2);
        public bool DisableServerPresentation => disableServerPresentation;
        public float DesyncWarningThreshold => Mathf.Max(0f, desyncWarningThreshold);

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

        public void ConfigureDedicatedServer(int maxPlayers = 2, bool disablePresentation = true,
            float positionDesyncThreshold = 0.25f)
        {
            dedicatedServerMaxPlayers = Mathf.Clamp(maxPlayers, 2, 2);
            disableServerPresentation = disablePresentation;
            desyncWarningThreshold = Mathf.Max(0f, positionDesyncThreshold);
        }
    }
}

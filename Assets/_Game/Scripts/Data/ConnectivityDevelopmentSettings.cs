using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(menuName = "SwingPop/Online/M18 Connectivity Development Settings",
        fileName = "M18ConnectivityDevelopmentSettings")]
    public sealed class ConnectivityDevelopmentSettings : ScriptableObject
    {
        [Header("Default")]
        [SerializeField] private MatchConnectivityMode defaultMode = MatchConnectivityMode.Direct;
        [SerializeField] private string relayProvider = ConnectivityProtocol.LocalRelayProvider;
        [SerializeField] private string relayRegion = "local";

        [Header("Local Relay Proxy")]
        [SerializeField] private string relayAddress = "127.0.0.1";
        [SerializeField, Range(1024, 65535)] private int firstRelayPort = 20817;
        [SerializeField, Range(1, 8)] private int maximumAllocations = 4;
        [SerializeField, Range(5f, 60f)] private float allocationTimeoutSeconds = 30f;
        [SerializeField, Range(60f, 7200f)] private float credentialLifetimeSeconds = 1800f;

        [Header("Client")]
        [SerializeField, Range(1, 5)] private int retryCount = 3;
        [SerializeField, Range(0.1f, 10f)] private float retryDelaySeconds = 1f;
        [SerializeField] private bool verboseConnectivityLogging;

        public MatchConnectivityMode DefaultMode => defaultMode;
        public string RelayProvider => relayProvider;
        public string RelayRegion => relayRegion;
        public string RelayAddress => relayAddress;
        public ushort FirstRelayPort => (ushort)Mathf.Clamp(firstRelayPort, 1024, 65535);
        public int MaximumAllocations => Mathf.Clamp(maximumAllocations, 1, 8);
        public float AllocationTimeoutSeconds => Mathf.Clamp(allocationTimeoutSeconds, 5f, 60f);
        public float CredentialLifetimeSeconds => Mathf.Clamp(credentialLifetimeSeconds, 60f, 7200f);
        public int RetryCount => Mathf.Clamp(retryCount, 1, 5);
        public float RetryDelaySeconds => Mathf.Clamp(retryDelaySeconds, 0.1f, 10f);
        public bool VerboseConnectivityLogging => verboseConnectivityLogging;

#if UNITY_EDITOR
        public void Configure(MatchConnectivityMode mode, string address, ushort port, string provider,
            string region, int maxAllocations, float allocationTimeout, float credentialLifetime,
            int retries, float retryDelay)
        {
            defaultMode = mode;
            relayAddress = address;
            firstRelayPort = port;
            relayProvider = provider;
            relayRegion = region;
            maximumAllocations = maxAllocations;
            allocationTimeoutSeconds = allocationTimeout;
            credentialLifetimeSeconds = credentialLifetime;
            retryCount = retries;
            retryDelaySeconds = retryDelay;
        }
#endif
    }
}

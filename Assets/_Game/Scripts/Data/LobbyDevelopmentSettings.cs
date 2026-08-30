using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Data
{
    [CreateAssetMenu(fileName = "LobbyDevelopmentSettings", menuName = "SwingPop/Online/Lobby Development Settings")]
    public sealed class LobbyDevelopmentSettings : ScriptableObject
    {
        [Header("Lobby Control Plane")]
        [SerializeField] private string lobbyAddress = "127.0.0.1";
        [SerializeField, Range(1024, 65535)] private int lobbyPort = 18817;
        [SerializeField, Range(2, 64)] private int maximumConnections = 16;
        [SerializeField, Range(1, 128)] private int maximumRooms = LobbyProtocol.DefaultMaximumRooms;
        [SerializeField, Range(5f, 20f)] private float lobbyHandshakeTimeoutSeconds = 8f;
        [SerializeField] private bool verboseLogging;

        [Header("Local Match Allocation")]
        [SerializeField] private string matchServerExecutable = "Builds/M17MatchServer/SwingPopServer.exe";
        [SerializeField] private string matchServerAddress = "127.0.0.1";
        [SerializeField, Range(1024, 65535)] private int firstMatchServerPort = 19817;
        [SerializeField, Range(1, 8)] private int maximumActiveMatches = 4;
        [SerializeField, Range(5f, 30f)] private float serverReadyTimeoutSeconds = 15f;
        [SerializeField, Range(10f, 300f)] private float joinTicketLifetimeSeconds = 90f;

        public string LobbyAddress => string.IsNullOrWhiteSpace(lobbyAddress) ? "127.0.0.1" : lobbyAddress.Trim();
        public ushort LobbyPort => (ushort)Mathf.Clamp(lobbyPort, 1, 65535);
        public int MaximumConnections => Mathf.Clamp(maximumConnections, 2, 64);
        public int MaximumRooms => Mathf.Clamp(maximumRooms, 1, 128);
        public float LobbyHandshakeTimeoutSeconds => Mathf.Clamp(lobbyHandshakeTimeoutSeconds, 5f, 20f);
        public bool VerboseLogging => verboseLogging;
        public string MatchServerExecutable => matchServerExecutable?.Trim() ?? string.Empty;
        public string MatchServerAddress => string.IsNullOrWhiteSpace(matchServerAddress) ? "127.0.0.1" : matchServerAddress.Trim();
        public ushort FirstMatchServerPort => (ushort)Mathf.Clamp(firstMatchServerPort, 1, 65535);
        public int MaximumActiveMatches => Mathf.Clamp(maximumActiveMatches, 1, 8);
        public float ServerReadyTimeoutSeconds => Mathf.Clamp(serverReadyTimeoutSeconds, 5f, 30f);
        public float JoinTicketLifetimeSeconds => Mathf.Clamp(joinTicketLifetimeSeconds, 10f, 300f);

        public void Configure(string address, ushort port, int maxConnections = 16, int maxRooms = 32,
            bool verbose = false)
        {
            lobbyAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
            lobbyPort = port;
            maximumConnections = Mathf.Clamp(maxConnections, 2, 64);
            maximumRooms = Mathf.Clamp(maxRooms, 1, 128);
            verboseLogging = verbose;
        }

        public void ConfigureAllocator(string executable, string address, ushort firstPort,
            int maxActiveMatches = 4, float readyTimeout = 15f, float ticketLifetime = 90f)
        {
            matchServerExecutable = executable?.Trim() ?? string.Empty;
            matchServerAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
            firstMatchServerPort = firstPort;
            maximumActiveMatches = Mathf.Clamp(maxActiveMatches, 1, 8);
            serverReadyTimeoutSeconds = Mathf.Clamp(readyTimeout, 5f, 30f);
            joinTicketLifetimeSeconds = Mathf.Clamp(ticketLifetime, 10f, 300f);
        }
    }
}

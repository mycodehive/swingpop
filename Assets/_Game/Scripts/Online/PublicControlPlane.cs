using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SwingPop.Online
{
    public enum ControlPlaneEnvironment
    {
        Development,
        Staging
    }

    /// <summary>Validated endpoint value. Staging callers must require TLS.</summary>
    public readonly struct ControlPlaneEndpoint
    {
        private ControlPlaneEndpoint(Uri uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant();
            Host = uri.DnsSafeHost;
            Port = (ushort)uri.Port;
            Path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
        }

        public string Scheme { get; }
        public string Host { get; }
        public ushort Port { get; }
        public string Path { get; }
        public bool IsSecure => string.Equals(Scheme, "wss", StringComparison.Ordinal);
        public string SafeLabel => $"{Scheme}://{Host}:{Port}{Path}";

        public static bool TryParse(string value, bool requireTls, out ControlPlaneEndpoint endpoint,
            out string failure)
        {
            endpoint = default;
            failure = string.Empty;
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
            {
                failure = "Lobby endpoint must be an absolute ws:// or wss:// URI.";
                return false;
            }
            bool websocket = string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase);
            if (!websocket)
            {
                failure = "Lobby endpoint scheme must be ws or wss.";
                return false;
            }
            if (requireTls && !string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase))
            {
                failure = "Staging Lobby endpoint requires wss; plaintext ws is development-only.";
                return false;
            }
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment))
            {
                failure = "Lobby endpoint cannot contain credentials, query parameters, or fragments.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(uri.DnsSafeHost) || uri.Port <= 0 || uri.Port > ushort.MaxValue)
            {
                failure = "Lobby endpoint host or port is invalid.";
                return false;
            }
            string path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
            if (!path.StartsWith("/", StringComparison.Ordinal) || path.Length > 120)
            {
                failure = "Lobby WebSocket path must start with '/' and be at most 120 characters.";
                return false;
            }
            endpoint = new ControlPlaneEndpoint(uri);
            return true;
        }
    }

    public enum ControlPlaneOperation
    {
        Authenticate,
        List,
        Create,
        Join,
        Start,
        Other
    }

    public sealed class ControlPlaneRateLimitPolicy
    {
        private readonly Dictionary<ControlPlaneOperation, int> limits = new()
        {
            [ControlPlaneOperation.Authenticate] = 4,
            [ControlPlaneOperation.List] = 8,
            [ControlPlaneOperation.Create] = 2,
            [ControlPlaneOperation.Join] = 4,
            [ControlPlaneOperation.Start] = 2,
            [ControlPlaneOperation.Other] = 12
        };

        public int WindowMilliseconds { get; }

        public ControlPlaneRateLimitPolicy(int windowMilliseconds = 1000) =>
            WindowMilliseconds = Math.Clamp(windowMilliseconds, 250, 60_000);

        public int GetLimit(ControlPlaneOperation operation) => limits[operation];

        public void SetLimit(ControlPlaneOperation operation, int maximum) =>
            limits[operation] = Math.Clamp(maximum, 1, 120);

        public static ControlPlaneOperation Map(LobbyWireMessageType type) => type switch
        {
            LobbyWireMessageType.AuthRequest => ControlPlaneOperation.Authenticate,
            LobbyWireMessageType.ListMatches => ControlPlaneOperation.List,
            LobbyWireMessageType.CreateMatch => ControlPlaneOperation.Create,
            LobbyWireMessageType.JoinMatch => ControlPlaneOperation.Join,
            LobbyWireMessageType.StartMatch => ControlPlaneOperation.Start,
            _ => ControlPlaneOperation.Other
        };
    }

    public sealed class ControlPlanePeerRateLimiter
    {
        private readonly ControlPlaneRateLimitPolicy policy;
        private readonly Dictionary<ControlPlaneOperation, int> counts = new();
        private long windowStartedAt;

        public ControlPlanePeerRateLimiter(ControlPlaneRateLimitPolicy policy, long nowMilliseconds)
        {
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            windowStartedAt = nowMilliseconds;
        }

        public bool TryConsume(ControlPlaneOperation operation, long nowMilliseconds)
        {
            if (nowMilliseconds - windowStartedAt >= policy.WindowMilliseconds
                || nowMilliseconds < windowStartedAt)
            {
                counts.Clear();
                windowStartedAt = nowMilliseconds;
            }
            counts.TryGetValue(operation, out int count);
            if (count >= policy.GetLimit(operation)) return false;
            counts[operation] = count + 1;
            return true;
        }
    }

    public sealed class ControlPlaneTelemetry
    {
        private long authenticationRejects;
        private long rateLimitRejects;
        private long matchStarts;
        private long matchCompletes;
        private long reconnects;
        private long failures;

        public long AuthenticationRejects => Interlocked.Read(ref authenticationRejects);
        public long RateLimitRejects => Interlocked.Read(ref rateLimitRejects);
        public long MatchStarts => Interlocked.Read(ref matchStarts);
        public long MatchCompletes => Interlocked.Read(ref matchCompletes);
        public long Reconnects => Interlocked.Read(ref reconnects);
        public long Failures => Interlocked.Read(ref failures);
        public void RecordAuthenticationReject() => Interlocked.Increment(ref authenticationRejects);
        public void RecordRateLimitReject() => Interlocked.Increment(ref rateLimitRejects);
        public void RecordMatchStart() => Interlocked.Increment(ref matchStarts);
        public void RecordMatchComplete() => Interlocked.Increment(ref matchCompletes);
        public void RecordReconnect() => Interlocked.Increment(ref reconnects);
        public void RecordFailure() => Interlocked.Increment(ref failures);
    }

    public readonly struct ControlPlaneHealthSnapshot
    {
        public ControlPlaneHealthSnapshot(bool ready, int activeConnections, int activeRooms,
            int activeServers, int activeAllocations)
        {
            Ready = ready;
            ActiveConnections = Math.Max(0, activeConnections);
            ActiveRooms = Math.Max(0, activeRooms);
            ActiveServers = Math.Max(0, activeServers);
            ActiveAllocations = Math.Max(0, activeAllocations);
        }

        public bool Ready { get; }
        public int ActiveConnections { get; }
        public int ActiveRooms { get; }
        public int ActiveServers { get; }
        public int ActiveAllocations { get; }
        public string ToSafeJson() =>
            $"{{\"status\":\"{(Ready ? "ready" : "not-ready")}\",\"connections\":{ActiveConnections}," +
            $"\"rooms\":{ActiveRooms},\"servers\":{ActiveServers},\"allocations\":{ActiveAllocations}}}";
    }

    /// <summary>Loopback-only health endpoint. It deliberately exposes no identities, endpoints, or secrets.</summary>
    public sealed class ControlPlaneHealthServer : IDisposable
    {
        private readonly Func<ControlPlaneHealthSnapshot> snapshot;
        private TcpListener listener;
        private Thread thread;
        private volatile bool running;

        public ControlPlaneHealthServer(Func<ControlPlaneHealthSnapshot> snapshot) =>
            this.snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));

        public bool Start(ushort port)
        {
            if (running || port == 0) return false;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start(4);
                running = true;
                thread = new Thread(Run) { IsBackground = true, Name = "SwingPop M20 Health" };
                thread.Start();
                return true;
            }
            catch (SocketException)
            {
                Dispose();
                return false;
            }
        }

        private void Run()
        {
            while (running)
            {
                try
                {
                    if (!listener.Pending()) { Thread.Sleep(50); continue; }
                    using TcpClient client = listener.AcceptTcpClient();
                    client.ReceiveTimeout = 1000;
                    client.SendTimeout = 1000;
                    byte[] request = new byte[2048];
                    int length = client.GetStream().Read(request, 0, request.Length);
                    string line = length > 0 ? Encoding.ASCII.GetString(request, 0, length) : string.Empty;
                    bool health = line.StartsWith("GET /healthz ", StringComparison.Ordinal);
                    string body = health ? snapshot().ToSafeJson() : "{\"status\":\"not-found\"}";
                    string status = health ? "200 OK" : "404 Not Found";
                    byte[] response = Encoding.UTF8.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: application/json\r\n" +
                        $"Cache-Control: no-store\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n{body}");
                    client.GetStream().Write(response, 0, response.Length);
                }
                catch (Exception)
                {
                    if (running) Thread.Sleep(50);
                }
            }
        }

        public void Dispose()
        {
            running = false;
            try { listener?.Stop(); } catch (SocketException) { }
            if (thread != null && thread.IsAlive) thread.Join(500);
            listener = null;
            thread = null;
        }
    }

    public static class ControlPlaneLogSafety
    {
        public static string Redact(string value, params string[] secrets)
        {
            string result = value ?? string.Empty;
            foreach (string secret in secrets ?? Array.Empty<string>())
                if (!string.IsNullOrWhiteSpace(secret))
                    result = result.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
            return result;
        }
    }
}

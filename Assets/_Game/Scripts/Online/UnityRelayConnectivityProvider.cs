using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using UnityEngine;

namespace SwingPop.Online
{
    public enum ConnectivityProviderError
    {
        None,
        ConfigurationMissing,
        AuthenticationFailed,
        AllocationFailed,
        ConnectionFailed,
        ExpiredCredential,
        ServiceUnavailable,
        Timeout,
        Cancelled,
        InvalidResponse
    }

    public readonly struct ConnectivityProviderFailure
    {
        public ConnectivityProviderFailure(ConnectivityProviderError error, string safeDetail)
        {
            Error = error;
            SafeDetail = ConnectivityLogRedactor.Redact(safeDetail);
        }

        public ConnectivityProviderError Error { get; }
        public string SafeDetail { get; }
        public bool IsFailure => Error != ConnectivityProviderError.None;
        public override string ToString() => IsFailure ? $"{Error}: {SafeDetail}" : "None";
    }

    public static class ConnectivityLogRedactor
    {
        private static readonly string[] SensitiveKeys =
        {
            "token", "secret", "credential", "joincode", "join code", "authorization", "ticket", "key="
        };

        public static string Redact(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string safe = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            foreach (string key in SensitiveKeys)
            {
                int index = safe.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (index >= 0) return safe.Substring(0, index) + "[REDACTED]";
            }
            return safe.Length <= 256 ? safe : safe.Substring(0, 256);
        }
    }

    [Serializable]
    public sealed class ProductionRelayServerPayload
    {
        [SerializeField] private int version = ConnectivityProtocol.ProductionDescriptorVersion;
        [SerializeField] private string host;
        [SerializeField] private int port;
        [SerializeField] private string allocationIdBase64;
        [SerializeField] private string connectionDataBase64;
        [SerializeField] private string hostConnectionDataBase64;
        [SerializeField] private string keyBase64;
        [SerializeField] private bool secure;
        [SerializeField] private bool webSocket;
        [SerializeField] private string region;

        public ProductionRelayServerPayload(string host, ushort port, byte[] allocationId,
            byte[] connectionData, byte[] hostConnectionData, byte[] key, bool secure,
            bool webSocket, string region)
        {
            this.host = host ?? string.Empty;
            this.port = port;
            allocationIdBase64 = Convert.ToBase64String(allocationId ?? Array.Empty<byte>());
            connectionDataBase64 = Convert.ToBase64String(connectionData ?? Array.Empty<byte>());
            hostConnectionDataBase64 = Convert.ToBase64String(hostConnectionData ?? Array.Empty<byte>());
            keyBase64 = Convert.ToBase64String(key ?? Array.Empty<byte>());
            this.secure = secure;
            this.webSocket = webSocket;
            this.region = region ?? string.Empty;
        }

        public int Version => version;
        public string Host => host ?? string.Empty;
        public ushort Port => (ushort)Mathf.Clamp(port, 1, ushort.MaxValue);
        public string Region => region ?? string.Empty;
        public bool Secure => secure;
        public bool WebSocket => webSocket;
        public bool IsValid => version == ConnectivityProtocol.ProductionDescriptorVersion
            && !string.IsNullOrWhiteSpace(Host) && port is > 0 and <= ushort.MaxValue
            && TryDecode(allocationIdBase64, 16, out _)
            && TryDecode(connectionDataBase64, 1, out _)
            && TryDecode(hostConnectionDataBase64, 1, out _)
            && TryDecode(keyBase64, 1, out _);

        public string Serialize() => JsonUtility.ToJson(this);

        public RelayServerData ToRelayServerData()
        {
            if (!IsValid) throw new InvalidOperationException("Production Relay server payload is invalid.");
            return new RelayServerData(Host, Port, Convert.FromBase64String(allocationIdBase64),
                Convert.FromBase64String(connectionDataBase64), Convert.FromBase64String(hostConnectionDataBase64),
                Convert.FromBase64String(keyBase64), secure, webSocket);
        }

        public static bool TryDeserialize(string json, out ProductionRelayServerPayload payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(json) || json.Length > ConnectivityProtocol.MaximumProviderPayloadLength)
                return false;
            try
            {
                payload = JsonUtility.FromJson<ProductionRelayServerPayload>(json);
                return payload != null && payload.IsValid;
            }
            catch (Exception)
            {
                payload = null;
                return false;
            }
        }

        private static bool TryDecode(string value, int minimumLength, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            try
            {
                bytes = Convert.FromBase64String(value ?? string.Empty);
                return bytes.Length >= minimumLength;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public sealed class UnityRelayHostAllocation
    {
        public UnityRelayHostAllocation(string allocationId, string joinCode,
            ProductionRelayServerPayload serverPayload, long expiresAtUnixMilliseconds)
        {
            AllocationId = allocationId ?? string.Empty;
            JoinCode = joinCode ?? string.Empty;
            ServerPayload = serverPayload;
            ExpiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
        }

        public string AllocationId { get; }
        public string JoinCode { get; }
        public ProductionRelayServerPayload ServerPayload { get; }
        public long ExpiresAtUnixMilliseconds { get; }
        public bool IsValidAt(long now) => !string.IsNullOrWhiteSpace(AllocationId)
            && !string.IsNullOrWhiteSpace(JoinCode) && ServerPayload != null && ServerPayload.IsValid
            && ExpiresAtUnixMilliseconds > now;
    }

    public interface IUnityRelaySdkAdapter
    {
        Task InitializeAndAuthenticateAsync(string environmentName);
        Task<UnityRelayHostAllocation> CreateHostAllocationAsync(int maximumConnections,
            string region, string connectionType, long expiresAtUnixMilliseconds);
        Task<ProductionRelayServerPayload> JoinAllocationAsync(string joinCode, string connectionType);
    }

    public static class UnityRelaySdkAdapterRegistry
    {
        private static Func<IUnityRelaySdkAdapter> factory;

        public static void Register(Func<IUnityRelaySdkAdapter> adapterFactory)
        {
            factory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        }

        public static IUnityRelaySdkAdapter Create() => factory?.Invoke()
            ?? throw new InvalidOperationException(
                "Unity Relay provider adapter is unavailable. Verify com.unity.services.multiplayer and the provider assembly.");
    }

    /// <summary>
    /// Production-capable provider behind the M18 seam. Cloud calls are explicitly prepared
    /// asynchronously; the synchronous M17 allocator only consumes an already prepared allocation.
    /// </summary>
    public sealed class UnityRelayConnectivityProvider : IMatchConnectivityProvider
    {
        private readonly IUnityRelaySdkAdapter sdk;
        private readonly string environment;
        private readonly string region;
        private readonly string connectionType;
        private readonly float timeoutSeconds;
        private readonly long credentialLifetimeMilliseconds;
        private readonly ConnectivityRetryPolicy retryPolicy;
        private readonly Dictionary<string, MatchConnectivityAllocation> active = new();
        private UnityRelayHostAllocation prepared;
        private bool preparing;

        public UnityRelayConnectivityProvider(string environment, string region, string connectionType,
            int maximumAttempts, float retryDelaySeconds, float timeoutSeconds,
            long credentialLifetimeMilliseconds, IUnityRelaySdkAdapter sdk = null)
        {
            this.environment = environment ?? string.Empty;
            this.region = region ?? string.Empty;
            this.connectionType = string.Equals(connectionType, "wss", StringComparison.OrdinalIgnoreCase)
                ? "wss" : "dtls";
            this.timeoutSeconds = Mathf.Clamp(timeoutSeconds, 5f, 60f);
            this.credentialLifetimeMilliseconds = Math.Max(60_000L, credentialLifetimeMilliseconds);
            retryPolicy = new ConnectivityRetryPolicy(maximumAttempts, retryDelaySeconds);
            this.sdk = sdk ?? UnityRelaySdkAdapterRegistry.Create();
        }

        public MatchConnectivityMode Mode => MatchConnectivityMode.ProductionRelay;
        public ConnectivityProviderFailure LastFailure { get; private set; }
        public bool HasPreparedAllocation => prepared != null
            && prepared.IsValidAt(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        public async Task<bool> PrepareAsync()
        {
            if (preparing) return false;
            preparing = true;
            LastFailure = default;
            try
            {
                await WithTimeout(sdk.InitializeAndAuthenticateAsync(environment));
                for (int attempt = 1; attempt <= retryPolicy.MaximumAttempts; attempt++)
                {
                    try
                    {
                        long expiry = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                      + credentialLifetimeMilliseconds;
                        prepared = await WithTimeout(sdk.CreateHostAllocationAsync(
                            LobbyProtocol.MatchPlayerCapacity, region, connectionType, expiry));
                        if (prepared != null && prepared.IsValidAt(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
                            return true;
                        LastFailure = new ConnectivityProviderFailure(
                            ConnectivityProviderError.InvalidResponse, "Relay allocation response was invalid.");
                    }
                    catch (Exception exception)
                    {
                        LastFailure = ConnectivityProviderErrorMapper.Map(exception, true);
                    }
                    if (!retryPolicy.CanRetry(attempt)) break;
                    await Task.Delay(TimeSpan.FromSeconds(retryPolicy.DelayForAttempt(attempt)));
                }
                return false;
            }
            catch (Exception exception)
            {
                LastFailure = ConnectivityProviderErrorMapper.Map(exception, true);
                return false;
            }
            finally
            {
                preparing = false;
            }
        }

        public bool TryAllocate(MatchId gameMatchId, string serverAddress, ushort serverPort,
            long nowMilliseconds, out MatchConnectivityAllocation allocation, out string failure)
        {
            allocation = null;
            failure = string.Empty;
            UnityRelayHostAllocation value = prepared;
            if (value == null || !value.IsValidAt(nowMilliseconds))
            {
                failure = LastFailure.IsFailure ? LastFailure.ToString()
                    : "Production Relay allocation was not prepared or expired.";
                return false;
            }

            prepared = null;
            ProductionRelayServerPayload payload = value.ServerPayload;
            ServerConnectivityDescriptor server = new(serverAddress, serverPort,
                MatchConnectivityMode.ProductionRelay, ConnectivityProtocol.UnityRelayProvider,
                ConnectivityProtocol.ProductionDescriptorVersion, payload.Serialize(), payload.Region);
            MatchConnectivityDescriptor client = new(MatchConnectivityMode.ProductionRelay,
                ConnectivityProtocol.UnityRelayProvider, payload.Host, payload.Port, value.AllocationId,
                value.JoinCode, value.ExpiresAtUnixMilliseconds,
                ConnectivityProtocol.ProductionDescriptorVersion, payload.Region);
            if (!server.IsValid || !client.IsValidAt(nowMilliseconds))
            {
                failure = "Production Relay descriptor mapping failed.";
                return false;
            }
            allocation = new MatchConnectivityAllocation(value.AllocationId, server, client,
                value.ExpiresAtUnixMilliseconds);
            active.Add(value.AllocationId, allocation);
            return true;
        }

        public bool MarkServerReady(string allocationId)
        {
            if (!active.TryGetValue(allocationId ?? string.Empty, out MatchConnectivityAllocation value)) return false;
            value.MarkServerReady();
            value.MarkInUse();
            return true;
        }

        public bool Release(string allocationId)
        {
            if (!active.Remove(allocationId ?? string.Empty, out MatchConnectivityAllocation value)) return false;
            value.MarkReleased();
            return true;
        }

        private async Task WithTimeout(Task operation)
        {
            Task completed = await Task.WhenAny(operation, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
            if (completed != operation) throw new TimeoutException("Unity Relay operation timed out.");
            await operation;
        }

        private async Task<T> WithTimeout<T>(Task<T> operation)
        {
            Task completed = await Task.WhenAny(operation, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
            if (completed != operation) throw new TimeoutException("Unity Relay operation timed out.");
            return await operation;
        }
    }

    public static class UnityRelayClientConnector
    {
        public static async Task<(ProductionRelayServerPayload Payload, ConnectivityProviderFailure Failure)> JoinAsync(
            MatchConnectivityDescriptor descriptor, string environment, string connectionType,
            float timeoutSeconds, IUnityRelaySdkAdapter sdk = null)
        {
            if (descriptor.Mode != MatchConnectivityMode.ProductionRelay
                || !descriptor.IsValidAt(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                || !string.Equals(descriptor.Provider, ConnectivityProtocol.UnityRelayProvider,
                    StringComparison.Ordinal))
                return (null, new ConnectivityProviderFailure(
                    ConnectivityProviderError.ConfigurationMissing, "Production Relay descriptor is invalid."));
            IUnityRelaySdkAdapter adapter = sdk ?? UnityRelaySdkAdapterRegistry.Create();
            try
            {
                Task operation = JoinCore(adapter, descriptor.Credential, environment, connectionType);
                Task completed = await Task.WhenAny(operation,
                    Task.Delay(TimeSpan.FromSeconds(Mathf.Clamp(timeoutSeconds, 5f, 60f))));
                if (completed != operation)
                    return (null, new ConnectivityProviderFailure(ConnectivityProviderError.Timeout,
                        "Unity Relay join timed out."));
                ProductionRelayServerPayload payload = await (Task<ProductionRelayServerPayload>)operation;
                return payload != null && payload.IsValid
                    ? (payload, default)
                    : (null, new ConnectivityProviderFailure(ConnectivityProviderError.InvalidResponse,
                        "Unity Relay join response was invalid."));
            }
            catch (Exception exception)
            {
                return (null, ConnectivityProviderErrorMapper.Map(exception, false));
            }
        }

        private static async Task<ProductionRelayServerPayload> JoinCore(IUnityRelaySdkAdapter adapter,
            string joinCode, string environment, string connectionType)
        {
            await adapter.InitializeAndAuthenticateAsync(environment);
            return await adapter.JoinAllocationAsync(joinCode, connectionType);
        }
    }

    public static class ConnectivityProviderErrorMapper
    {
        public static ConnectivityProviderFailure Map(Exception exception, bool allocation)
        {
            Exception actual = exception is AggregateException aggregate
                ? aggregate.GetBaseException() : exception;
            string type = actual?.GetType().Name ?? string.Empty;
            string message = actual?.Message ?? string.Empty;
            ConnectivityProviderError error = actual switch
            {
                TimeoutException => ConnectivityProviderError.Timeout,
                OperationCanceledException => ConnectivityProviderError.Cancelled,
                _ when type.IndexOf("Authentication", StringComparison.OrdinalIgnoreCase) >= 0 =>
                    ConnectivityProviderError.AuthenticationFailed,
                _ when message.IndexOf("expired", StringComparison.OrdinalIgnoreCase) >= 0 =>
                    ConnectivityProviderError.ExpiredCredential,
                _ when message.IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0
                       || message.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0 =>
                    ConnectivityProviderError.ServiceUnavailable,
                _ => allocation ? ConnectivityProviderError.AllocationFailed
                    : ConnectivityProviderError.ConnectionFailed
            };
            return new ConnectivityProviderFailure(error, type + ": " + message);
        }
    }
}

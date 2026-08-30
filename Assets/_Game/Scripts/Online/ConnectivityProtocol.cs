using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SwingPop.Online
{
    public static class ConnectivityProtocol
    {
        public const int CurrentVersion = 1;
        public const int MaximumCredentialLength = 512;
        public const string DirectProvider = "direct";
        public const string LocalRelayProvider = "local-relay-proxy";
    }

    public enum MatchConnectivityMode
    {
        Direct,
        Relay
    }

    public enum ConnectivityAllocationState
    {
        None,
        Allocating,
        RelayReady,
        ServerReady,
        InUse,
        Released,
        Failed
    }

    public enum ConnectivityRejectReason
    {
        None,
        NotRequired,
        MissingCredential,
        InvalidDescriptor,
        UnknownAllocation,
        Expired,
        InvalidCredential,
        AlreadyAccepted,
        Unavailable
    }

    public enum ConnectivityClientState
    {
        None,
        DescriptorReady,
        Connecting,
        Accepted,
        Rejected,
        Disconnected
    }

    [Serializable]
    public struct MatchConnectivityDescriptor
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private MatchConnectivityMode mode;
        [SerializeField] private string provider;
        [SerializeField] private string address;
        [SerializeField] private int port;
        [SerializeField] private string allocationId;
        [SerializeField] private string credential;
        [SerializeField] private long expiresAtUnixMilliseconds;

        public MatchConnectivityDescriptor(MatchConnectivityMode mode, string provider, string address,
            ushort port, string allocationId, string credential, long expiresAtUnixMilliseconds)
        {
            protocolVersion = ConnectivityProtocol.CurrentVersion;
            this.mode = mode;
            this.provider = provider ?? string.Empty;
            this.address = address ?? string.Empty;
            this.port = port;
            this.allocationId = allocationId ?? string.Empty;
            this.credential = credential ?? string.Empty;
            this.expiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
        }

        public int ProtocolVersion => protocolVersion;
        public MatchConnectivityMode Mode => mode;
        public string Provider => provider ?? string.Empty;
        public string Address => address ?? string.Empty;
        public ushort Port => (ushort)Mathf.Clamp(port, 1, ushort.MaxValue);
        public string AllocationId => allocationId ?? string.Empty;
        public string Credential => credential ?? string.Empty;
        public long ExpiresAtUnixMilliseconds => expiresAtUnixMilliseconds;
        public bool RequiresCredential => mode == MatchConnectivityMode.Relay;

        public bool IsValidAt(long nowMilliseconds)
        {
            if (protocolVersion != ConnectivityProtocol.CurrentVersion || string.IsNullOrWhiteSpace(Address)
                || port is < 1 or > ushort.MaxValue || string.IsNullOrWhiteSpace(Provider)) return false;
            if (mode == MatchConnectivityMode.Direct) return true;
            return !string.IsNullOrWhiteSpace(AllocationId)
                   && !string.IsNullOrWhiteSpace(Credential)
                   && Credential.Length <= ConnectivityProtocol.MaximumCredentialLength
                   && expiresAtUnixMilliseconds > nowMilliseconds;
        }

        public string SafeLabel => mode == MatchConnectivityMode.Direct
            ? $"DIRECT {Address}:{Port}"
            : $"RELAY {Provider} allocation={ConnectivitySecurity.Fingerprint(AllocationId)}";
    }

    [Serializable]
    public struct ServerConnectivityDescriptor
    {
        [SerializeField] private string bindAddress;
        [SerializeField] private int bindPort;

        public ServerConnectivityDescriptor(string bindAddress, ushort bindPort)
        {
            this.bindAddress = bindAddress ?? string.Empty;
            this.bindPort = bindPort;
        }

        public string BindAddress => bindAddress ?? string.Empty;
        public ushort BindPort => (ushort)Mathf.Clamp(bindPort, 1, ushort.MaxValue);
        public bool IsValid => !string.IsNullOrWhiteSpace(BindAddress) && bindPort is > 0 and <= ushort.MaxValue;
    }

    public sealed class MatchConnectivityAllocation
    {
        public MatchConnectivityAllocation(string allocationId, ServerConnectivityDescriptor server,
            MatchConnectivityDescriptor client, long expiresAtUnixMilliseconds, int resourceProcessId = 0)
        {
            AllocationId = allocationId ?? string.Empty;
            Server = server;
            Client = client;
            ExpiresAtUnixMilliseconds = expiresAtUnixMilliseconds;
            ResourceProcessId = resourceProcessId;
            State = ConnectivityAllocationState.RelayReady;
        }

        public string AllocationId { get; }
        public ServerConnectivityDescriptor Server { get; }
        public MatchConnectivityDescriptor Client { get; }
        public long ExpiresAtUnixMilliseconds { get; }
        public int ResourceProcessId { get; }
        public ConnectivityAllocationState State { get; private set; }
        public void MarkServerReady() => State = ConnectivityAllocationState.ServerReady;
        public void MarkInUse() => State = ConnectivityAllocationState.InUse;
        public void MarkReleased() => State = ConnectivityAllocationState.Released;
        public void MarkFailed() => State = ConnectivityAllocationState.Failed;
    }

    public interface IMatchConnectivityProvider
    {
        MatchConnectivityMode Mode { get; }
        bool TryAllocate(MatchId gameMatchId, string serverAddress, ushort serverPort, long nowMilliseconds,
            out MatchConnectivityAllocation allocation, out string failure);
        bool MarkServerReady(string allocationId);
        bool Release(string allocationId);
    }

    public sealed class DirectMatchConnectivityProvider : IMatchConnectivityProvider
    {
        private readonly Dictionary<string, MatchConnectivityAllocation> allocations = new();
        public MatchConnectivityMode Mode => MatchConnectivityMode.Direct;

        public bool TryAllocate(MatchId gameMatchId, string serverAddress, ushort serverPort,
            long nowMilliseconds, out MatchConnectivityAllocation allocation, out string failure)
        {
            failure = string.Empty;
            string id = "direct-" + gameMatchId.Value;
            ServerConnectivityDescriptor server = new(serverAddress, serverPort);
            MatchConnectivityDescriptor client = new(MatchConnectivityMode.Direct,
                ConnectivityProtocol.DirectProvider, serverAddress, serverPort, id, string.Empty, 0L);
            allocation = new MatchConnectivityAllocation(id, server, client, long.MaxValue);
            allocations[id] = allocation;
            return server.IsValid && client.IsValidAt(nowMilliseconds);
        }

        public bool MarkServerReady(string allocationId)
        {
            if (!allocations.TryGetValue(allocationId ?? string.Empty, out MatchConnectivityAllocation value)) return false;
            value.MarkServerReady();
            value.MarkInUse();
            return true;
        }

        public bool Release(string allocationId)
        {
            if (!allocations.Remove(allocationId ?? string.Empty, out MatchConnectivityAllocation value)) return false;
            value.MarkReleased();
            return true;
        }
    }

    [Serializable]
    public struct ConnectivityRequestMessage
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private string allocationId;
        [SerializeField] private string credential;

        public ConnectivityRequestMessage(MatchConnectivityDescriptor descriptor)
        {
            protocolVersion = ConnectivityProtocol.CurrentVersion;
            allocationId = descriptor.AllocationId;
            credential = descriptor.Credential;
        }

        public int ProtocolVersion => protocolVersion;
        public string AllocationId => allocationId ?? string.Empty;
        public string Credential => credential ?? string.Empty;
    }

    [Serializable]
    public struct ConnectivityAcceptedMessage
    {
        [SerializeField] private string allocationFingerprint;
        public ConnectivityAcceptedMessage(string allocationId) =>
            allocationFingerprint = ConnectivitySecurity.Fingerprint(allocationId);
        public string AllocationFingerprint => allocationFingerprint ?? string.Empty;
    }

    [Serializable]
    public struct ConnectivityRejectedMessage
    {
        [SerializeField] private ConnectivityRejectReason reason;
        public ConnectivityRejectedMessage(ConnectivityRejectReason reason) => this.reason = reason;
        public ConnectivityRejectReason Reason => reason;
    }

    public readonly struct ConnectivityValidationResult
    {
        private ConnectivityValidationResult(bool accepted, ConnectivityRejectReason reason)
        {
            Accepted = accepted;
            Reason = reason;
        }
        public bool Accepted { get; }
        public ConnectivityRejectReason Reason { get; }
        public static ConnectivityValidationResult Accept() => new(true, ConnectivityRejectReason.None);
        public static ConnectivityValidationResult Reject(ConnectivityRejectReason reason) => new(false, reason);
    }

    public sealed class ConnectivityCredentialRegistry
    {
        private readonly string allocationId;
        private readonly byte[] credentialHash;
        private readonly long expiresAt;

        public ConnectivityCredentialRegistry(string allocationId, string credential, long expiresAt)
            : this(allocationId, ConnectivitySecurity.Hash(credential), expiresAt) { }

        public ConnectivityCredentialRegistry(string allocationId, byte[] credentialHash, long expiresAt)
        {
            this.allocationId = allocationId ?? string.Empty;
            this.credentialHash = credentialHash ?? Array.Empty<byte>();
            this.expiresAt = expiresAt;
        }

        public string AllocationId => allocationId;
        public long ExpiresAtUnixMilliseconds => expiresAt;
        public string ExportHashBase64() => Convert.ToBase64String(credentialHash);

        public ConnectivityValidationResult Validate(ConnectivityRequestMessage request, long nowMilliseconds)
        {
            if (request.ProtocolVersion != ConnectivityProtocol.CurrentVersion)
                return ConnectivityValidationResult.Reject(ConnectivityRejectReason.InvalidDescriptor);
            if (!string.Equals(request.AllocationId, allocationId, StringComparison.Ordinal))
                return ConnectivityValidationResult.Reject(ConnectivityRejectReason.UnknownAllocation);
            if (expiresAt <= nowMilliseconds)
                return ConnectivityValidationResult.Reject(ConnectivityRejectReason.Expired);
            if (string.IsNullOrWhiteSpace(request.Credential))
                return ConnectivityValidationResult.Reject(ConnectivityRejectReason.MissingCredential);
            return ConnectivitySecurity.FixedTimeEquals(credentialHash, ConnectivitySecurity.Hash(request.Credential))
                ? ConnectivityValidationResult.Accept()
                : ConnectivityValidationResult.Reject(ConnectivityRejectReason.InvalidCredential);
        }
    }

    public static class ConnectivitySecurity
    {
        public static string CreateCredential()
        {
            byte[] bytes = new byte[32];
            using RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static byte[] Hash(string value)
        {
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        public static bool FixedTimeEquals(byte[] expected, byte[] actual)
        {
            if (expected == null || actual == null || expected.Length != actual.Length) return false;
            int difference = 0;
            for (int index = 0; index < expected.Length; index++) difference |= expected[index] ^ actual[index];
            return difference == 0;
        }

        public static string Fingerprint(string value)
        {
            byte[] hash = Hash(value);
            return BitConverter.ToString(hash, 0, 4).Replace("-", string.Empty);
        }
    }

    public readonly struct ConnectivityRetryPolicy
    {
        public ConnectivityRetryPolicy(int maximumAttempts, float delaySeconds)
        {
            MaximumAttempts = Mathf.Clamp(maximumAttempts, 1, 5);
            DelaySeconds = Mathf.Clamp(delaySeconds, 0.1f, 10f);
        }
        public int MaximumAttempts { get; }
        public float DelaySeconds { get; }
        public bool CanRetry(int completedAttempts) => completedAttempts < MaximumAttempts;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace SwingPop.Online.Providers.UnityRelay
{
    /// <summary>All Unity Gaming Services SDK references are isolated in this provider assembly.</summary>
    public sealed class UnityRelaySdkAdapter : IUnityRelaySdkAdapter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() =>
            UnityRelaySdkAdapterRegistry.Register(() => new UnityRelaySdkAdapter());

        public async Task InitializeAndAuthenticateAsync(string environmentName)
        {
            InitializationOptions options = new();
            if (!string.IsNullOrWhiteSpace(environmentName)) options.SetEnvironmentName(environmentName.Trim());
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(options);
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public async Task<UnityRelayHostAllocation> CreateHostAllocationAsync(int maximumConnections,
            string region, string connectionType, long expiresAtUnixMilliseconds)
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(
                Mathf.Clamp(maximumConnections, 1, 150), string.IsNullOrWhiteSpace(region) ? null : region.Trim());
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            ProductionRelayServerPayload payload = FromAllocation(allocation.ServerEndpoints,
                allocation.AllocationIdBytes, allocation.ConnectionData, allocation.ConnectionData,
                allocation.Key, allocation.Region, connectionType);
            return new UnityRelayHostAllocation(allocation.AllocationId.ToString("N"), joinCode,
                payload, expiresAtUnixMilliseconds);
        }

        public async Task<ProductionRelayServerPayload> JoinAllocationAsync(string joinCode, string connectionType)
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return FromAllocation(allocation.ServerEndpoints, allocation.AllocationIdBytes,
                allocation.ConnectionData, allocation.HostConnectionData, allocation.Key,
                allocation.Region, connectionType);
        }

        private static ProductionRelayServerPayload FromAllocation(
            IEnumerable<RelayServerEndpoint> endpoints, byte[] allocationId, byte[] connectionData,
            byte[] hostConnectionData, byte[] key, string region, string connectionType)
        {
            string requested = string.IsNullOrWhiteSpace(connectionType) ? "dtls" : connectionType.Trim();
            RelayServerEndpoint endpoint = endpoints?.FirstOrDefault(value =>
                string.Equals(value.ConnectionType, requested, StringComparison.OrdinalIgnoreCase));
            if (endpoint == null) throw new InvalidOperationException(
                "Relay response omitted the requested endpoint type.");
            bool webSocket = string.Equals(requested, "wss", StringComparison.OrdinalIgnoreCase);
            return new ProductionRelayServerPayload(endpoint.Host, (ushort)endpoint.Port,
                allocationId, connectionData, hostConnectionData, key, endpoint.Secure, webSocket, region);
        }
    }
}

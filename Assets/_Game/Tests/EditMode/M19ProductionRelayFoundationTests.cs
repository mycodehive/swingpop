using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SwingPop.Gameplay.Ball;
using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Tests.EditMode
{
    public sealed class M19ProductionRelayFoundationTests
    {
        private long now;

        [SetUp]
        public void SetUp() => now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [Test] public void ProductionRelayDescriptor_SerializesAndValidates()
        {
            MatchConnectivityDescriptor value = Descriptor();
            MatchConnectivityDescriptor roundTrip = JsonUtility.FromJson<MatchConnectivityDescriptor>(
                JsonUtility.ToJson(value));
            Assert.That(roundTrip.Mode, Is.EqualTo(MatchConnectivityMode.ProductionRelay));
            Assert.That(roundTrip.Provider, Is.EqualTo(ConnectivityProtocol.UnityRelayProvider));
            Assert.That(roundTrip.Region, Is.EqualTo("asia-northeast1"));
            Assert.That(roundTrip.IsValidAt(now), Is.True);
        }

        [Test] public void ProductionRelayPayload_RoundTripsIntoGenericServerDescriptor()
        {
            ProductionRelayServerPayload payload = Payload();
            ServerConnectivityDescriptor descriptor = new("127.0.0.1", 19817,
                MatchConnectivityMode.ProductionRelay, ConnectivityProtocol.UnityRelayProvider,
                ConnectivityProtocol.ProductionDescriptorVersion, payload.Serialize(), payload.Region);
            Assert.That(descriptor.IsValid, Is.True);
            Assert.That(ProductionRelayServerPayload.TryDeserialize(descriptor.ProviderPayload, out var restored), Is.True);
            Assert.That(restored.Region, Is.EqualTo(payload.Region));
        }

        [Test] public void CredentialRedaction_RemovesSensitiveSuffix()
        {
            string safe = ConnectivityLogRedactor.Redact("request failed token=plain-secret-value");
            Assert.That(safe, Does.Contain("[REDACTED]"));
            Assert.That(safe, Does.Not.Contain("plain-secret-value"));
        }

        [Test] public async Task ProviderAdapter_MapsPreparedAllocation()
        {
            FakeSdk sdk = new(Payload());
            UnityRelayConnectivityProvider provider = Provider(sdk);
            Assert.That(await provider.PrepareAsync(), Is.True);
            Assert.That(provider.TryAllocate(new MatchId("game"), "127.0.0.1", 19817, now,
                out MatchConnectivityAllocation allocation, out _), Is.True);
            Assert.That(allocation.Client.Mode, Is.EqualTo(MatchConnectivityMode.ProductionRelay));
            Assert.That(allocation.Server.Mode, Is.EqualTo(MatchConnectivityMode.ProductionRelay));
        }

        [Test] public async Task AllocationFailure_IsTypedAndDoesNotFallback()
        {
            FakeSdk sdk = new(Payload()) { AllocationFailure = new InvalidOperationException("service unavailable") };
            UnityRelayConnectivityProvider provider = Provider(sdk);
            Assert.That(await provider.PrepareAsync(), Is.False);
            Assert.That(provider.LastFailure.Error, Is.EqualTo(ConnectivityProviderError.ServiceUnavailable));
            Assert.That(provider.TryAllocate(new MatchId("game"), "127.0.0.1", 19817, now,
                out _, out _), Is.False);
            Assert.That(provider.Mode, Is.EqualTo(MatchConnectivityMode.ProductionRelay));
        }

        [Test] public void Timeout_IsMappedWithoutProviderSecret()
        {
            ConnectivityProviderFailure failure = ConnectivityProviderErrorMapper.Map(
                new TimeoutException("credential=do-not-log"), true);
            Assert.That(failure.Error, Is.EqualTo(ConnectivityProviderError.Timeout));
            Assert.That(failure.SafeDetail, Does.Not.Contain("do-not-log"));
        }

        [Test] public async Task Release_IsIdempotentByReturningFalseOnSecondCall()
        {
            UnityRelayConnectivityProvider provider = Provider(new FakeSdk(Payload()));
            await provider.PrepareAsync();
            provider.TryAllocate(new MatchId("game"), "127.0.0.1", 19817, now,
                out MatchConnectivityAllocation allocation, out _);
            Assert.That(provider.Release(allocation.AllocationId), Is.True);
            Assert.That(provider.Release(allocation.AllocationId), Is.False);
            Assert.That(allocation.State, Is.EqualTo(ConnectivityAllocationState.Released));
        }

        [Test] public void DirectFallback_IsDisabledWhenProductionAllocationIsNotPrepared()
        {
            UnityRelayConnectivityProvider provider = Provider(new FakeSdk(Payload()));
            Assert.That(provider.TryAllocate(new MatchId("game"), "203.0.113.2", 19817, now,
                out _, out string failure), Is.False);
            Assert.That(failure, Does.Not.Contain("direct").IgnoreCase);
        }

        [Test] public void MatchJoinTicket_RemainsSeparateFromRelayJoinCode()
        {
            MatchJoinTicket ticket = new(new MatchId("game"), "match-ticket", now + 60_000L);
            Assert.That(Descriptor().Credential, Is.Not.EqualTo(ticket.Secret));
        }

        [Test] public void ReconnectTicket_RemainsSeparateFromRelayJoinCode()
        {
            ReconnectTicket ticket = new(new MatchId("game"), new MatchPlayerId("player-a"), 1,
                "reconnect-ticket", now, now + 60_000L);
            Assert.That(Descriptor().Credential, Is.Not.EqualTo(ticket.Secret));
        }

        [Test] public void ExpiredProviderCredential_IsRejected()
        {
            MatchConnectivityDescriptor expired = new(MatchConnectivityMode.ProductionRelay,
                ConnectivityProtocol.UnityRelayProvider, "relay.example", 9999, "allocation", "join-code",
                now - 1, ConnectivityProtocol.ProductionDescriptorVersion, "region");
            Assert.That(expired.IsValidAt(now), Is.False);
        }

        [Test] public async Task ServerReadyGating_RequiresExplicitTransition()
        {
            UnityRelayConnectivityProvider provider = Provider(new FakeSdk(Payload()));
            await provider.PrepareAsync();
            provider.TryAllocate(new MatchId("game"), "127.0.0.1", 19817, now,
                out MatchConnectivityAllocation allocation, out _);
            Assert.That(allocation.State, Is.EqualTo(ConnectivityAllocationState.RelayReady));
            Assert.That(provider.MarkServerReady(allocation.AllocationId), Is.True);
            Assert.That(allocation.State, Is.EqualTo(ConnectivityAllocationState.InUse));
        }

        [Test] public void RetryPolicy_IsBoundedAndExponential()
        {
            ConnectivityRetryPolicy retry = new(99, 1f);
            Assert.That(retry.MaximumAttempts, Is.EqualTo(5));
            Assert.That(retry.DelayForAttempt(1), Is.EqualTo(1f));
            Assert.That(retry.DelayForAttempt(3), Is.EqualTo(4f));
            Assert.That(retry.DelayForAttempt(9), Is.EqualTo(10f));
        }

        [Test] public void StaleDescriptorVersion_IsRejected()
        {
            string json = JsonUtility.ToJson(Descriptor()).Replace("\"protocolVersion\":2", "\"protocolVersion\":1");
            MatchConnectivityDescriptor stale = JsonUtility.FromJson<MatchConnectivityDescriptor>(json);
            Assert.That(stale.IsValidAt(now), Is.False);
        }

        [Test] public void ConnectionError_IsMappedToGenericBoundary()
        {
            ConnectivityProviderFailure failure = ConnectivityProviderErrorMapper.Map(
                new InvalidOperationException("join rejected"), false);
            Assert.That(failure.Error, Is.EqualTo(ConnectivityProviderError.ConnectionFailed));
        }

        [Test] public async Task ClientJoin_UsesProviderAdapterAndReturnsOpaquePayload()
        {
            FakeSdk sdk = new(Payload());
            var joined = await UnityRelayClientConnector.JoinAsync(Descriptor(), "development", "dtls", 5f, sdk);
            Assert.That(joined.Failure.IsFailure, Is.False);
            Assert.That(joined.Payload.IsValid, Is.True);
            Assert.That(sdk.JoinCalls, Is.EqualTo(1));
        }

        [Test] public async Task ClientJoinFailure_DoesNotExposeJoinCode()
        {
            FakeSdk sdk = new(Payload()) { JoinFailure = new InvalidOperationException("join code=join-code") };
            var joined = await UnityRelayClientConnector.JoinAsync(Descriptor(), "development", "dtls", 5f, sdk);
            Assert.That(joined.Failure.IsFailure, Is.True);
            Assert.That(joined.Failure.SafeDetail, Does.Not.Contain("join-code"));
        }

        [Test] public void ProductionReservation_StoresHostPayloadOnlyInConsumableTempDocument()
        {
            MatchReservation reservation = Reservation(out DevelopmentMatchAdmissionRegistry admission);
            MatchReservationFileDocument document = MatchReservationFile.Create(reservation, admission);
            Assert.That(document.ServerProviderPayload, Is.Not.Empty);
            Assert.That(document.ConnectivityMode, Is.EqualTo(MatchConnectivityMode.ProductionRelay));
        }

        [Test] public void SensitiveProductionReservation_IsDeletedAfterServerLoad()
        {
            MatchReservation reservation = Reservation(out DevelopmentMatchAdmissionRegistry admission);
            string path = Path.Combine(Path.GetTempPath(), "SwingPop-M19-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                MatchReservationFile.Write(path, MatchReservationFile.Create(reservation, admission));
                Assert.That(MatchReservationFile.TryLoad(path, out MatchReservationFileDocument loaded,
                    out _, out ConnectivityCredentialRegistry connectivity), Is.True);
                Assert.That(connectivity, Is.Not.Null,
                    "Production Relay must retain the provider credential handshake boundary.");
                Assert.That(MatchReservationFile.TryDeleteSensitiveReservation(path, loaded), Is.True);
                Assert.That(File.Exists(path), Is.False);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test] public void ProviderCredential_DoesNotReplaceAdmissionCredentialHash()
        {
            MatchReservation reservation = Reservation(out DevelopmentMatchAdmissionRegistry admission);
            string json = JsonUtility.ToJson(MatchReservationFile.Create(reservation, admission));
            Assert.That(json, Does.Not.Contain(reservation.Connectivity.Credential));
            Assert.That(json, Does.Contain("connectivityCredentialHashBase64"));
            Assert.That(json, Does.Contain("ticketHashBase64"));
        }

        [Test] public void GameplayRuntimeAssembly_HasNoUnityServicesReference()
        {
            string[] references = typeof(GolfBallController).Assembly.GetReferencedAssemblies()
                .Select(value => value.Name).ToArray();
            Assert.That(references.Any(value => value.StartsWith("Unity.Services", StringComparison.Ordinal)), Is.False);
        }

        private MatchConnectivityDescriptor Descriptor() => new(MatchConnectivityMode.ProductionRelay,
            ConnectivityProtocol.UnityRelayProvider, "relay.example", 9999, "allocation", "join-code",
            now + 60_000L, ConnectivityProtocol.ProductionDescriptorVersion, "asia-northeast1");

        private static ProductionRelayServerPayload Payload() => new("relay.example", 9999,
            Enumerable.Repeat((byte)1, 16).ToArray(), Enumerable.Repeat((byte)2, 255).ToArray(),
            Enumerable.Repeat((byte)3, 255).ToArray(), Enumerable.Repeat((byte)4, 64).ToArray(),
            true, false, "asia-northeast1");

        private UnityRelayConnectivityProvider Provider(IUnityRelaySdkAdapter sdk) => new(
            "development", "asia-northeast1", "dtls", 1, 0.1f, 5f, 60_000L, sdk);

        private MatchReservation Reservation(out DevelopmentMatchAdmissionRegistry admission)
        {
            MatchId game = new("game");
            LobbyMatchId lobby = new("lobby");
            MatchConnectivityDescriptor descriptor = Descriptor();
            admission = new DevelopmentMatchAdmissionRegistry(game);
            MatchAdmissionGrant[] grants = new MatchAdmissionGrant[2];
            for (int index = 0; index < grants.Length; index++)
            {
                PlayerAccountId account = new("account-" + index);
                MatchPlayerId player = new(index == 0 ? "player-a" : "player-b");
                grants[index] = new MatchAdmissionGrant(lobby, game, account, player, descriptor,
                    admission.Register(account, player, now + 60_000L));
            }
            ProductionRelayServerPayload payload = Payload();
            ServerConnectivityDescriptor server = new("127.0.0.1", 19817,
                MatchConnectivityMode.ProductionRelay, ConnectivityProtocol.UnityRelayProvider,
                ConnectivityProtocol.ProductionDescriptorVersion, payload.Serialize(), payload.Region);
            return new MatchReservation(lobby, game, server, now + 60_000L, descriptor, grants);
        }

        private sealed class FakeSdk : IUnityRelaySdkAdapter
        {
            private readonly ProductionRelayServerPayload payload;
            public FakeSdk(ProductionRelayServerPayload payload) => this.payload = payload;
            public Exception AllocationFailure { get; set; }
            public Exception JoinFailure { get; set; }
            public int JoinCalls { get; private set; }

            public Task InitializeAndAuthenticateAsync(string environmentName) => Task.CompletedTask;

            public Task<UnityRelayHostAllocation> CreateHostAllocationAsync(int maximumConnections,
                string region, string connectionType, long expiresAtUnixMilliseconds)
            {
                if (AllocationFailure != null)
                    return Task.FromException<UnityRelayHostAllocation>(AllocationFailure);
                return Task.FromResult(new UnityRelayHostAllocation("allocation", "join-code",
                    payload, expiresAtUnixMilliseconds));
            }

            public Task<ProductionRelayServerPayload> JoinAllocationAsync(string joinCode, string connectionType)
            {
                JoinCalls++;
                if (JoinFailure != null) return Task.FromException<ProductionRelayServerPayload>(JoinFailure);
                return Task.FromResult(payload);
            }
        }
    }
}

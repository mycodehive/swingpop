using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEngine;
using UnityEngine.TestTools;

namespace SwingPop.Tests
{
    public sealed class M19ProductionRelayPlayModeTests
    {
        private long now;
        private FakeSdk sdk;
        private UnityRelayConnectivityProvider provider;

        [SetUp]
        public void SetUp()
        {
            now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            sdk = new FakeSdk(Payload());
            provider = new UnityRelayConnectivityProvider("development", "test-region", "dtls",
                1, 0.1f, 5f, 60_000L, sdk);
        }

        [UnityTest] public IEnumerator A_ProductionRelayBootstrap_IsOptInAndPrepared()
        {
            Task<bool> task = provider.PrepareAsync();
            yield return Wait(task);
            Assert.That(task.Result, Is.True);
            Assert.That(provider.Mode, Is.EqualTo(MatchConnectivityMode.ProductionRelay));
        }

        [UnityTest] public IEnumerator B_Allocation_ProducesSeparateServerAndClientDescriptors()
        {
            Task<bool> task = provider.PrepareAsync();
            yield return Wait(task);
            Assert.That(provider.TryAllocate(new MatchId("m19"), "127.0.0.1", 19817, now,
                out MatchConnectivityAllocation allocation, out _), Is.True);
            Assert.That(allocation.Client.Credential, Is.Not.Empty);
            Assert.That(allocation.Server.ProviderPayload, Is.Not.Empty);
            Assert.That(allocation.Client.SafeLabel, Does.Not.Contain(allocation.Client.Credential));
        }

        [Test] public void C_DedicatedServerBind_AcceptsOpaqueRelayPayload()
        {
            GameObject root = new("M19 Fake Dedicated");
            try
            {
                DedicatedServerMatchTransport server = root.AddComponent<DedicatedServerMatchTransport>();
                Assert.That(server.SetProductionRelayServerPayload(Payload()), Is.True);
                Assert.That(server.UsesProductionRelay, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test] public void D_ClientAConnect_RequiresProviderJoinBeforeTransportConfiguration()
        {
            GameObject root = new("M19 Fake A");
            try
            {
                UnityTransportMatchTransport client = root.AddComponent<UnityTransportMatchTransport>();
                Assert.That(client.SetConnectivityDescriptor(Descriptor()), Is.True);
                Assert.That(client.SetProductionRelayServerPayload(Payload()), Is.True);
                Assert.That(client.ConnectivityMode, Is.EqualTo(MatchConnectivityMode.ProductionRelay));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test] public void E_ClientBConnect_UsesDistinctProviderAllocationData()
        {
            ProductionRelayServerPayload a = Payload(2);
            ProductionRelayServerPayload b = Payload(7);
            Assert.That(a.Serialize(), Is.Not.EqualTo(b.Serialize()));
            Assert.That(a.Region, Is.EqualTo(b.Region));
        }

        [Test] public void F_Admission_RemainsAccountAndTicketBound()
        {
            DevelopmentMatchAdmissionRegistry admission = new(new MatchId("m19"));
            MatchJoinTicket ticket = admission.Register(new PlayerAccountId("account-a"),
                new MatchPlayerId("player-a"), now + 60_000L);
            MatchAdmissionValidationResult stolen = admission.ValidateAndConsume(new MatchId("m19"),
                new PlayerAccountId("account-b"), ticket.Secret, now, false);
            Assert.That(stolen.Accepted, Is.False);
            Assert.That(stolen.Reason, Is.EqualTo(MatchAdmissionRejectReason.AccountMismatch));
        }

        [Test] public void G_ShotPath_StillUsesExistingAuthorityCommand()
        {
            GameObject root = new("M19 Authority");
            try
            {
                LocalMatchAuthority authority = root.AddComponent<LocalMatchAuthority>();
                MatchSnapshot initial = authority.StartMatch(new MatchId("m19"), "hole-01", Players());
                ShotCommand command = new(Vector3.forward, Vector3.forward, 0f, 0.6f, 1f, 0f,
                    ImpactGrade.Perfect, 0.6f, 0f, 30f, 25f);
                ShotSubmissionDecision decision = authority.SubmitShot(new ShotSubmission(initial.MatchId,
                    initial.CurrentTurnPlayer, initial.TurnIndex, initial.ShotSequence + 1,
                    OnlineProtocol.CurrentVersion, command));
                Assert.That(decision.Accepted, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test] public void H_FailureCleanup_DoesNotConsumeMatchJoinTicket()
        {
            DevelopmentMatchAdmissionRegistry admission = new(new MatchId("m19"));
            MatchJoinTicket ticket = admission.Register(new PlayerAccountId("account-a"),
                new MatchPlayerId("player-a"), now + 60_000L);
            ConnectivityCredentialRegistry connectivity = new("allocation", "relay-code", now + 60_000L);
            MatchConnectivityDescriptor wrong = new(MatchConnectivityMode.ProductionRelay,
                ConnectivityProtocol.UnityRelayProvider, "relay.example", 9999, "allocation", "wrong",
                now + 60_000L, ConnectivityProtocol.ProductionDescriptorVersion, "test-region");
            Assert.That(connectivity.Validate(new ConnectivityRequestMessage(wrong), now).Accepted, Is.False);
            Assert.That(admission.ValidateAndConsume(new MatchId("m19"), new PlayerAccountId("account-a"),
                ticket.Secret, now, false).Accepted, Is.True);
        }

        [UnityTest] public IEnumerator I_Reconnect_ProviderCredentialCanBeRejoinedWithoutBecomingReconnectTicket()
        {
            Task<(ProductionRelayServerPayload Payload, ConnectivityProviderFailure Failure)> task =
                UnityRelayClientConnector.JoinAsync(Descriptor(), "development", "dtls", 5f, sdk);
            yield return Wait(task);
            Assert.That(task.Result.Failure.IsFailure, Is.False);
            ReconnectTicket reconnect = new(new MatchId("m19"), new MatchPlayerId("player-a"), 1,
                "reconnect-secret", now, now + 60_000L);
            Assert.That(Descriptor().Credential, Is.Not.EqualTo(reconnect.Secret));
        }

        [UnityTest] public IEnumerator J_MatchCleanup_ReleasesExactlyOneAllocation()
        {
            Task<bool> task = provider.PrepareAsync();
            yield return Wait(task);
            provider.TryAllocate(new MatchId("m19"), "127.0.0.1", 19817, now,
                out MatchConnectivityAllocation allocation, out _);
            Assert.That(provider.Release(allocation.AllocationId), Is.True);
            Assert.That(provider.Release(allocation.AllocationId), Is.False);
        }

        private MatchConnectivityDescriptor Descriptor() => new(MatchConnectivityMode.ProductionRelay,
            ConnectivityProtocol.UnityRelayProvider, "relay.example", 9999, "allocation", "relay-code",
            now + 60_000L, ConnectivityProtocol.ProductionDescriptorVersion, "test-region");

        private static ProductionRelayServerPayload Payload(byte seed = 1) => new("relay.example", 9999,
            Enumerable.Repeat(seed, 16).ToArray(), Enumerable.Repeat((byte)(seed + 1), 255).ToArray(),
            Enumerable.Repeat((byte)(seed + 2), 255).ToArray(), Enumerable.Repeat((byte)(seed + 3), 64).ToArray(),
            true, false, "test-region");

        private static IEnumerator Wait(Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception ?? new Exception("Task failed.");
        }

        private static PlayerSnapshot[] Players()
        {
            NetworkVector3 tee = new(0f, 0.2f, 0f);
            return new[]
            {
                new PlayerSnapshot(new MatchPlayerId("player-a"), "A", 0, 0, true,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false),
                new PlayerSnapshot(new MatchPlayerId("player-b"), "B", 1, 1, false,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false)
            };
        }

        private sealed class FakeSdk : IUnityRelaySdkAdapter
        {
            private readonly ProductionRelayServerPayload payload;
            public FakeSdk(ProductionRelayServerPayload payload) => this.payload = payload;
            public Task InitializeAndAuthenticateAsync(string environmentName) => Task.CompletedTask;
            public Task<UnityRelayHostAllocation> CreateHostAllocationAsync(int maximumConnections,
                string region, string connectionType, long expiresAtUnixMilliseconds) =>
                Task.FromResult(new UnityRelayHostAllocation("allocation", "relay-code", payload,
                    expiresAtUnixMilliseconds));
            public Task<ProductionRelayServerPayload> JoinAllocationAsync(string joinCode, string connectionType) =>
                Task.FromResult(payload);
        }
    }
}

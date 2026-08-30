using System;
using System.IO;
using NUnit.Framework;
using SwingPop.Online;

namespace SwingPop.Tests.EditMode
{
    public sealed class M20PublicControlPlaneTests
    {
        private const long Now = 2_000_000L;

        [Test] public void WssEndpoint_IsAcceptedForStaging()
        {
            Assert.That(ControlPlaneEndpoint.TryParse("wss://lobby.example.com/lobby", true,
                out ControlPlaneEndpoint value, out _), Is.True);
            Assert.That(value.IsSecure, Is.True);
        }

        [Test] public void HttpsEndpoint_IsRejected()
        {
            Assert.That(ControlPlaneEndpoint.TryParse("https://lobby.example.com/lobby", true,
                out _, out _), Is.False);
        }

        [Test] public void PlaintextEndpoint_IsRejectedForStaging()
        {
            Assert.That(ControlPlaneEndpoint.TryParse("ws://lobby.example.com/lobby", true,
                out _, out string failure), Is.False);
            Assert.That(failure, Does.Contain("requires wss"));
        }

        [Test] public void PlaintextEndpoint_RemainsAvailableForDevelopment()
        {
            Assert.That(ControlPlaneEndpoint.TryParse("ws://127.0.0.1:18817/", false,
                out ControlPlaneEndpoint value, out _), Is.True);
            Assert.That(value.IsSecure, Is.False);
        }

        [Test] public void EndpointCredential_IsRejected()
        {
            Assert.That(ControlPlaneEndpoint.TryParse("wss://user:secret@lobby.example.com/lobby", true,
                out _, out _), Is.False);
        }

        [Test] public void EndpointQuery_IsRejected()
        {
            Assert.That(ControlPlaneEndpoint.TryParse("wss://lobby.example.com/lobby?ticket=secret", true,
                out _, out _), Is.False);
        }

        [Test] public void EndpointFragment_IsRejected()
        {
            Assert.That(ControlPlaneEndpoint.TryParse("wss://lobby.example.com/lobby#secret", true,
                out _, out _), Is.False);
        }

        [Test] public void EndpointPath_IsBounded()
        {
            string path = new('a', 121);
            Assert.That(ControlPlaneEndpoint.TryParse("wss://lobby.example.com/" + path, true,
                out _, out _), Is.False);
        }

        [TestCase(LobbyWireMessageType.AuthRequest, ControlPlaneOperation.Authenticate)]
        [TestCase(LobbyWireMessageType.ListMatches, ControlPlaneOperation.List)]
        [TestCase(LobbyWireMessageType.CreateMatch, ControlPlaneOperation.Create)]
        [TestCase(LobbyWireMessageType.JoinMatch, ControlPlaneOperation.Join)]
        [TestCase(LobbyWireMessageType.StartMatch, ControlPlaneOperation.Start)]
        public void OperationMapping_IsExplicit(LobbyWireMessageType message, ControlPlaneOperation expected) =>
            Assert.That(ControlPlaneRateLimitPolicy.Map(message), Is.EqualTo(expected));

        [Test] public void AuthenticationRateLimit_IsBounded()
        {
            ControlPlaneRateLimitPolicy policy = new();
            ControlPlanePeerRateLimiter limiter = new(policy, Now);
            for (int index = 0; index < policy.GetLimit(ControlPlaneOperation.Authenticate); index++)
                Assert.That(limiter.TryConsume(ControlPlaneOperation.Authenticate, Now), Is.True);
            Assert.That(limiter.TryConsume(ControlPlaneOperation.Authenticate, Now), Is.False);
        }

        [Test] public void RateLimits_AreIndependentPerOperation()
        {
            ControlPlaneRateLimitPolicy policy = new();
            policy.SetLimit(ControlPlaneOperation.Create, 1);
            ControlPlanePeerRateLimiter limiter = new(policy, Now);
            Assert.That(limiter.TryConsume(ControlPlaneOperation.Create, Now), Is.True);
            Assert.That(limiter.TryConsume(ControlPlaneOperation.Create, Now), Is.False);
            Assert.That(limiter.TryConsume(ControlPlaneOperation.List, Now), Is.True);
        }

        [Test] public void RateLimitWindow_Resets()
        {
            ControlPlaneRateLimitPolicy policy = new(1000);
            policy.SetLimit(ControlPlaneOperation.Start, 1);
            ControlPlanePeerRateLimiter limiter = new(policy, Now);
            Assert.That(limiter.TryConsume(ControlPlaneOperation.Start, Now), Is.True);
            Assert.That(limiter.TryConsume(ControlPlaneOperation.Start, Now + 1000), Is.True);
        }

        [Test] public void RateLimitConfiguration_IsClamped()
        {
            ControlPlaneRateLimitPolicy policy = new(1);
            policy.SetLimit(ControlPlaneOperation.Create, 999);
            Assert.That(policy.WindowMilliseconds, Is.EqualTo(250));
            Assert.That(policy.GetLimit(ControlPlaneOperation.Create), Is.EqualTo(120));
        }

        [Test] public void HealthPayload_ContainsOnlyBoundedCounters()
        {
            string json = new ControlPlaneHealthSnapshot(true, 2, 3, 1, 1).ToSafeJson();
            Assert.That(json, Does.Contain("\"status\":\"ready\""));
            Assert.That(json, Does.Not.Contain("endpoint"));
            Assert.That(json, Does.Not.Contain("account"));
            Assert.That(json, Does.Not.Contain("ticket"));
        }

        [Test] public void HealthPayload_ClampsNegativeCounters()
        {
            string json = new ControlPlaneHealthSnapshot(false, -1, -2, -3, -4).ToSafeJson();
            Assert.That(json, Does.Contain("\"connections\":0"));
            Assert.That(json, Does.Contain("\"allocations\":0"));
        }

        [Test] public void LogSafety_RedactsEveryProvidedSecret()
        {
            string output = ControlPlaneLogSafety.Redact("credential=A ticket=B", "A", "B");
            Assert.That(output, Does.Not.Contain("credential=A"));
            Assert.That(output, Does.Not.Contain("ticket=B"));
        }

        [Test] public void StagingServer_IsNotBoundToLobbyParent()
        {
            MatchServerLaunchPolicy policy = MatchServerLaunchPolicy.Staging(900f);
            Assert.That(policy.BindToAllocatorParent, Is.False);
            Assert.That(policy.MaximumLifetimeSeconds, Is.EqualTo(900f));
        }

        [Test] public void DevelopmentServer_PreservesParentBoundCleanup()
        {
            Assert.That(MatchServerLaunchPolicy.Development.BindToAllocatorParent, Is.True);
        }

        [Test] public void EnvironmentArgument_ParsesStaging()
        {
            Assert.That(LobbyDevelopmentController.ReadEnvironment(
                new[] { "-swingpopControlPlaneEnvironment=Staging" }, ControlPlaneEnvironment.Development),
                Is.EqualTo(ControlPlaneEnvironment.Staging));
        }

        [Test] public void InvalidEnvironmentArgument_FallsBack()
        {
            Assert.That(LobbyDevelopmentController.ReadEnvironment(
                new[] { "-swingpopControlPlaneEnvironment=invalid" }, ControlPlaneEnvironment.Development),
                Is.EqualTo(ControlPlaneEnvironment.Development));
        }

        [Test] public void UnauthenticatedList_IsRejectedByService()
        {
            InMemoryLobbyService service = new(new PrelaunchedGameServerAllocator("127.0.0.1", 19817));
            LobbyOperationResult<LobbyMatchSnapshot[]> result = service.ListMatches(default,
                new ListMatchesRequest("list", true), Now);
            Assert.That(result.Reason, Is.EqualTo(LobbyRejectReason.AuthenticationRequired));
        }

        [Test] public void RoomCap_IsEnforced()
        {
            InMemoryLobbyService service = new(new PrelaunchedGameServerAllocator("127.0.0.1", 19817), 1);
            Assert.That(service.CreateMatch(Session("a"), Create("a"), Now).Accepted, Is.True);
            Assert.That(service.CreateMatch(Session("b"), Create("b"), Now).Reason,
                Is.EqualTo(LobbyRejectReason.RateLimited));
        }

        [Test] public void FailedServerReady_RollsBackProcessAndAllocation()
        {
            FakeLauncher launcher = new();
            FakeConnectivityProvider provider = new(false);
            DevelopmentGameServerAllocator allocator = Allocator(launcher, provider,
                MatchServerLaunchPolicy.Staging(300f));
            Assert.That(allocator.TryAllocate(FullRoom(), Now, out _, out _), Is.False);
            Assert.That(launcher.Stopped, Is.EqualTo(1));
            Assert.That(provider.Released, Is.EqualTo(1));
            Assert.That(allocator.ActiveAllocationCount, Is.Zero);
        }

        [Test] public void ReadyServer_IsTrackedWithinAllocationCap()
        {
            FakeLauncher launcher = new();
            FakeConnectivityProvider provider = new(true);
            DevelopmentGameServerAllocator allocator = Allocator(launcher, provider,
                MatchServerLaunchPolicy.Staging(300f));
            Assert.That(allocator.TryAllocate(FullRoom(), Now, out _, out _), Is.True);
            Assert.That(allocator.ActiveAllocationCount, Is.EqualTo(1));
            Assert.That(allocator.ActiveProcessCount, Is.EqualTo(1));
            allocator.Release(provider.LastAllocationId);
        }

        [Test] public void AllocationCap_RejectsSecondActiveServer()
        {
            FakeLauncher launcher = new();
            FakeConnectivityProvider provider = new(true);
            DevelopmentGameServerAllocator allocator = Allocator(launcher, provider,
                MatchServerLaunchPolicy.Staging(300f));
            Assert.That(allocator.TryAllocate(FullRoom(), Now, out _, out _), Is.True);
            Assert.That(allocator.TryAllocate(FullRoom(), Now + 1, out _, out string failure), Is.False);
            Assert.That(failure, Does.Contain("limit"));
            allocator.Release(provider.LastAllocationId);
        }

        [Test] public void Reaper_StopsExpiredServerAndReleasesConnectivity()
        {
            FakeLauncher launcher = new();
            FakeConnectivityProvider provider = new(true);
            DevelopmentGameServerAllocator allocator = Allocator(launcher, provider,
                MatchServerLaunchPolicy.Staging(300f));
            Assert.That(allocator.TryAllocate(FullRoom(), Now, out _, out _), Is.True);
            Assert.That(allocator.Reap(Now + 301_000L), Is.EqualTo(1));
            Assert.That(launcher.Stopped, Is.EqualTo(1));
            Assert.That(provider.Released, Is.EqualTo(1));
            Assert.That(allocator.ActiveProcessCount, Is.Zero);
        }

        [Test] public void AdmissionTicket_RemainsAccountBound()
        {
            DevelopmentMatchAdmissionRegistry registry = new(new MatchId("game"));
            MatchJoinTicket ticket = registry.Register(new PlayerAccountId("a"),
                new MatchPlayerId("player-a"), Now + 60_000L);
            Assert.That(registry.ValidateAndConsume(new MatchId("game"), new PlayerAccountId("b"),
                ticket.Secret, Now, false).Reason, Is.EqualTo(MatchAdmissionRejectReason.AccountMismatch));
        }

        [Test] public void ProductionRelayGrant_DoesNotLeakPrivateBindEndpoint()
        {
            MatchConnectivityDescriptor relay = new(MatchConnectivityMode.ProductionRelay,
                ConnectivityProtocol.UnityRelayProvider, "relay.example", 443, "allocation", "join-code",
                Now + 60_000L, ConnectivityProtocol.ProductionDescriptorVersion, "region");
            DevelopmentMatchAdmissionRegistry registry = new(new MatchId("game"));
            MatchAdmissionGrant grant = new(new LobbyMatchId("lobby"), new MatchId("game"),
                new PlayerAccountId("a"), new MatchPlayerId("player-a"), relay,
                registry.Register(new PlayerAccountId("a"), new MatchPlayerId("player-a"), Now + 60_000L));
            Assert.That(grant.ServerAddress, Is.EqualTo("relay.example"));
            Assert.That(grant.ServerAddress, Is.Not.EqualTo("127.0.0.1"));
        }

        [Test] public void ReconnectTicket_RemainsSeparateFromRelayAndJoinTicket()
        {
            ReconnectTicket reconnect = new(new MatchId("game"), new MatchPlayerId("player-a"), 1,
                "reconnect-secret", Now, Now + 60_000L);
            DevelopmentMatchAdmissionRegistry registry = new(new MatchId("game"));
            MatchJoinTicket join = registry.Register(new PlayerAccountId("a"),
                new MatchPlayerId("player-a"), Now + 60_000L);
            Assert.That(reconnect.Secret, Is.Not.EqualTo(join.Secret));
            Assert.That(reconnect.Secret, Is.Not.EqualTo("relay-join-code"));
        }

        [Test] public void LobbyOutagePolicy_DoesNotParentBindStagingMatch()
        {
            MatchServerLaunchPolicy policy = MatchServerLaunchPolicy.Staging(600f);
            Assert.That(policy.BindToAllocatorParent, Is.False);
            Assert.That(policy.MaximumLifetimeSeconds, Is.GreaterThan(0f));
        }

        [Test] public void Telemetry_UsesCountersWithoutIdentityLabels()
        {
            ControlPlaneTelemetry telemetry = new();
            telemetry.RecordAuthenticationReject();
            telemetry.RecordRateLimitReject();
            telemetry.RecordMatchStart();
            telemetry.RecordFailure();
            Assert.That(telemetry.AuthenticationRejects, Is.EqualTo(1));
            Assert.That(telemetry.RateLimitRejects, Is.EqualTo(1));
            Assert.That(telemetry.MatchStarts, Is.EqualTo(1));
            Assert.That(telemetry.Failures, Is.EqualTo(1));
        }

        [Test] public void LobbyControlPlane_DoesNotAcceptGameplayMessages()
        {
            Assert.That(LobbyNetworkRules.IsAllowedFromClient(LobbyWireMessageType.StartMatch), Is.True);
            Assert.That(LobbyNetworkRules.IsAllowedFromService(LobbyWireMessageType.AdmissionGranted), Is.True);
            Assert.That(Enum.IsDefined(typeof(LobbyWireMessageType), "ShotCommand"), Is.False);
        }

        [Test] public void RequestPayloadCap_Remains64KiB()
        {
            Assert.That(OnlineProtocol.MaximumPayloadBytes, Is.EqualTo(64 * 1024));
        }

        private static LobbyPlayerSession Session(string value) => new(new PlayerAccountId(value),
            new AuthSessionId("session-" + value), Now + 60_000L, true, false);

        private static CreateMatchRequest Create(string value) => new("create-" + value, "Room " + value,
            2, LobbyProtocol.SupportedHoleId, LobbyVisibility.Public);

        private static LobbyMatchSnapshot FullRoom()
        {
            LobbyMatchMember[] members =
            {
                new(new PlayerAccountId("a"), "A", 0, LobbyReadyState.Ready, true),
                new(new PlayerAccountId("b"), "B", 1, LobbyReadyState.Ready, false)
            };
            return new LobbyMatchSnapshot(new LobbyMatchId("room"), "Room", 2, LobbyMatchState.Starting,
                LobbyProtocol.SupportedHoleId, Now, LobbyVisibility.Public, false, 4, default, members);
        }

        private static DevelopmentGameServerAllocator Allocator(FakeLauncher launcher,
            FakeConnectivityProvider provider, MatchServerLaunchPolicy policy)
        {
            string directory = Path.Combine(Path.GetTempPath(), "SwingPop", "M20-Tests", Guid.NewGuid().ToString("N"));
            return new DevelopmentGameServerAllocator("fake-server", "127.0.0.1", 25000, 1,
                60_000L, 5f, "fake-auth", directory, launcher, provider, policy);
        }

        private sealed class FakeLauncher : ILocalMatchServerLauncher
        {
            public int Stopped { get; private set; }
            public bool TryLaunch(string executablePath, string address, ushort port, string authenticationKeyPath,
                string reservationPath, string readyPath, float timeoutSeconds, out int processId, out string failure)
            {
                processId = 4242;
                failure = string.Empty;
                return true;
            }
            public bool TryStop(int processId) { Stopped++; return true; }
        }

        private sealed class FakeConnectivityProvider : IMatchConnectivityProvider
        {
            private readonly bool ready;
            public FakeConnectivityProvider(bool ready) => this.ready = ready;
            public MatchConnectivityMode Mode => MatchConnectivityMode.ProductionRelay;
            public int Released { get; private set; }
            public string LastAllocationId { get; private set; }
            public bool TryAllocate(MatchId gameMatchId, string serverAddress, ushort serverPort,
                long nowMilliseconds, out MatchConnectivityAllocation allocation, out string failure)
            {
                LastAllocationId = "relay-" + gameMatchId.Value;
                failure = string.Empty;
                ServerConnectivityDescriptor server = new(serverAddress, serverPort,
                    MatchConnectivityMode.ProductionRelay, ConnectivityProtocol.UnityRelayProvider,
                    ConnectivityProtocol.ProductionDescriptorVersion, "opaque-server-payload", "test");
                MatchConnectivityDescriptor client = new(MatchConnectivityMode.ProductionRelay,
                    ConnectivityProtocol.UnityRelayProvider, "relay.example", 443, LastAllocationId,
                    "opaque-join-code", nowMilliseconds + 60_000L,
                    ConnectivityProtocol.ProductionDescriptorVersion, "test");
                allocation = new MatchConnectivityAllocation(LastAllocationId, server, client,
                    nowMilliseconds + 60_000L);
                return true;
            }
            public bool MarkServerReady(string allocationId) => ready;
            public bool Release(string allocationId) { Released++; return true; }
        }
    }
}

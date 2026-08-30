using System;
using System.IO;
using NUnit.Framework;
using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Tests.EditMode
{
    public sealed class M18ConnectivityFoundationTests
    {
        private long now;

        [SetUp]
        public void SetUp() => now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [Test] public void DirectDescriptor_IsValidWithoutCredential()
        {
            MatchConnectivityDescriptor value = Direct();
            Assert.That(value.IsValidAt(now), Is.True);
            Assert.That(value.RequiresCredential, Is.False);
        }

        [Test] public void RelayDescriptor_RequiresCredential()
        {
            Assert.That(Relay().RequiresCredential, Is.True);
        }

        [Test] public void RelayDescriptor_Expires()
        {
            MatchConnectivityDescriptor value = Relay(now + 1);
            Assert.That(value.IsValidAt(now + 2), Is.False);
        }

        [Test] public void RelayCredential_IsHighEntropyAndUrlSafe()
        {
            string a = ConnectivitySecurity.CreateCredential();
            string b = ConnectivitySecurity.CreateCredential();
            Assert.That(a, Has.Length.GreaterThanOrEqualTo(40));
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a, Does.Not.Contain("+"));
        }

        [Test] public void CredentialRegistry_AcceptsCorrectCredential()
        {
            MatchConnectivityDescriptor descriptor = Relay();
            ConnectivityCredentialRegistry registry = Registry(descriptor);
            Assert.That(registry.Validate(new ConnectivityRequestMessage(descriptor), now).Accepted, Is.True);
        }

        [Test] public void CredentialRegistry_RejectsWrongCredential()
        {
            MatchConnectivityDescriptor descriptor = Relay();
            MatchConnectivityDescriptor wrong = new(MatchConnectivityMode.Relay,
                descriptor.Provider, descriptor.Address, descriptor.Port, descriptor.AllocationId,
                ConnectivitySecurity.CreateCredential(), descriptor.ExpiresAtUnixMilliseconds);
            Assert.That(Registry(descriptor).Validate(new ConnectivityRequestMessage(wrong), now).Reason,
                Is.EqualTo(ConnectivityRejectReason.InvalidCredential));
        }

        [Test] public void CredentialRegistry_RejectsWrongAllocation()
        {
            MatchConnectivityDescriptor descriptor = Relay();
            MatchConnectivityDescriptor wrong = new(MatchConnectivityMode.Relay,
                descriptor.Provider, descriptor.Address, descriptor.Port, "wrong",
                descriptor.Credential, descriptor.ExpiresAtUnixMilliseconds);
            Assert.That(Registry(descriptor).Validate(new ConnectivityRequestMessage(wrong), now).Reason,
                Is.EqualTo(ConnectivityRejectReason.UnknownAllocation));
        }

        [Test] public void CredentialRegistry_RejectsExpiredCredential()
        {
            MatchConnectivityDescriptor descriptor = Relay(now + 1);
            Assert.That(Registry(descriptor).Validate(new ConnectivityRequestMessage(descriptor), now + 2).Reason,
                Is.EqualTo(ConnectivityRejectReason.Expired));
        }

        [Test] public void CredentialRegistry_DoesNotConsumeCredential()
        {
            MatchConnectivityDescriptor descriptor = Relay();
            ConnectivityCredentialRegistry registry = Registry(descriptor);
            Assert.That(registry.Validate(new ConnectivityRequestMessage(descriptor), now).Accepted, Is.True);
            Assert.That(registry.Validate(new ConnectivityRequestMessage(descriptor), now).Accepted, Is.True);
        }

        [Test] public void Fingerprint_DoesNotExposeSecret()
        {
            string secret = ConnectivitySecurity.CreateCredential();
            Assert.That(ConnectivitySecurity.Fingerprint(secret), Has.Length.EqualTo(8));
            Assert.That(ConnectivitySecurity.Fingerprint(secret), Does.Not.Contain(secret));
        }

        [Test] public void RetryPolicy_IsBounded()
        {
            ConnectivityRetryPolicy policy = new(99, 0f);
            Assert.That(policy.MaximumAttempts, Is.EqualTo(5));
            Assert.That(policy.DelaySeconds, Is.EqualTo(0.1f));
        }

        [Test] public void ConnectivityModeArgument_ParsesRelay()
        {
            Assert.That(LobbyDevelopmentController.ReadConnectivityMode(
                new[] { "-swingpopConnectivityMode=Relay" }, MatchConnectivityMode.Direct),
                Is.EqualTo(MatchConnectivityMode.Relay));
        }

        [Test] public void ConnectivityModeArgument_FallsBackToDirect()
        {
            Assert.That(LobbyDevelopmentController.ReadConnectivityMode(
                new[] { "-swingpopConnectivityMode=bad" }, MatchConnectivityMode.Direct),
                Is.EqualTo(MatchConnectivityMode.Direct));
        }

        [Test] public void ConnectivityRequest_IsClientOnly()
        {
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ConnectivityRequest), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ConnectivityRequest), Is.False);
        }

        [Test] public void ConnectivityResponses_AreServerOnly()
        {
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ConnectivityAccepted), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ConnectivityRejected), Is.False);
        }

        [Test] public void ConnectivityRequest_IsAllowedBeforeAuthentication()
        {
            Assert.That(AuthenticationMessagePolicy.IsAllowedBeforeAuthentication(
                NetworkMessageType.ConnectivityRequest), Is.True);
        }

        [Test] public void LocalRelayProvider_AllocatesClientProxyAndPrivateServerDescriptors()
        {
            FakeRelayLauncher launcher = new();
            LocalRelayConnectivityProvider provider = Provider(launcher, 2);
            Assert.That(provider.TryAllocate(new MatchId("match"), "10.0.0.5", 19817, now,
                out MatchConnectivityAllocation value, out _), Is.True);
            Assert.That(value.Server.BindAddress, Is.EqualTo("10.0.0.5"));
            Assert.That(value.Client.Address, Is.EqualTo("127.0.0.1"));
            Assert.That(value.Client.Address, Is.Not.EqualTo(value.Server.BindAddress));
        }

        [Test] public void LocalRelayProvider_EnforcesAllocationLimit()
        {
            LocalRelayConnectivityProvider provider = Provider(new FakeRelayLauncher(), 1);
            Assert.That(provider.TryAllocate(new MatchId("one"), "127.0.0.1", 19817, now, out _, out _), Is.True);
            Assert.That(provider.TryAllocate(new MatchId("two"), "127.0.0.1", 19818, now, out _, out _), Is.False);
        }

        [Test] public void LocalRelayProvider_ReleaseStopsExactProcess()
        {
            FakeRelayLauncher launcher = new();
            LocalRelayConnectivityProvider provider = Provider(launcher, 1);
            provider.TryAllocate(new MatchId("one"), "127.0.0.1", 19817, now,
                out MatchConnectivityAllocation allocation, out _);
            Assert.That(provider.Release(allocation.AllocationId), Is.True);
            Assert.That(launcher.StoppedProcessId, Is.EqualTo(42));
            Assert.That(allocation.State, Is.EqualTo(ConnectivityAllocationState.Released));
        }

        [Test] public void ReservationFile_HashesBothTicketAndRelayCredential()
        {
            MatchConnectivityDescriptor descriptor = Relay();
            DevelopmentMatchAdmissionRegistry admission = new(new MatchId("game"));
            MatchJoinTicket ticket = admission.Register(new PlayerAccountId("account-a"),
                new MatchPlayerId("player-a"), now + 60_000L);
            MatchAdmissionGrant grant = new(new LobbyMatchId("lobby"), new MatchId("game"),
                new PlayerAccountId("account-a"), new MatchPlayerId("player-a"), descriptor, ticket);
            MatchReservation reservation = new(new LobbyMatchId("lobby"), new MatchId("game"),
                new ServerConnectivityDescriptor("10.0.0.5", 19817), now + 60_000L, descriptor,
                new[] { grant, new MatchAdmissionGrant(new LobbyMatchId("lobby"), new MatchId("game"),
                    new PlayerAccountId("account-b"), new MatchPlayerId("player-b"), descriptor,
                    admission.Register(new PlayerAccountId("account-b"), new MatchPlayerId("player-b"), now + 60_000L)) });
            string json = JsonUtility.ToJson(MatchReservationFile.Create(reservation, admission));
            Assert.That(json, Does.Not.Contain(descriptor.Credential));
            Assert.That(json, Does.Not.Contain(ticket.Secret));
        }

        [Test] public void ReservationFile_RoundTripsRelayCredentialRegistry()
        {
            MatchConnectivityDescriptor descriptor = Relay();
            MatchReservation reservation = Reservation(descriptor, out DevelopmentMatchAdmissionRegistry admission);
            string path = Path.Combine(Path.GetTempPath(), "SwingPop-M18-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                MatchReservationFile.Write(path, MatchReservationFile.Create(reservation, admission));
                Assert.That(MatchReservationFile.TryLoad(path, out MatchReservationFileDocument document,
                    out DevelopmentMatchAdmissionRegistry loadedAdmission,
                    out ConnectivityCredentialRegistry loadedConnectivity), Is.True);
                Assert.That(document.ConnectivityMode, Is.EqualTo(MatchConnectivityMode.Relay));
                Assert.That(loadedAdmission.Count, Is.EqualTo(2));
                Assert.That(loadedConnectivity.Validate(new ConnectivityRequestMessage(descriptor), now).Accepted, Is.True);
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        private MatchConnectivityDescriptor Direct() => new(MatchConnectivityMode.Direct,
            ConnectivityProtocol.DirectProvider, "127.0.0.1", 19817, "direct", string.Empty, 0L);

        private MatchConnectivityDescriptor Relay(long expiry = 0L) => new(MatchConnectivityMode.Relay,
            ConnectivityProtocol.LocalRelayProvider, "127.0.0.1", 20817, "allocation",
            ConnectivitySecurity.CreateCredential(), expiry == 0L ? now + 60_000L : expiry);

        private static ConnectivityCredentialRegistry Registry(MatchConnectivityDescriptor descriptor) =>
            new(descriptor.AllocationId, descriptor.Credential, descriptor.ExpiresAtUnixMilliseconds);

        private static LocalRelayConnectivityProvider Provider(FakeRelayLauncher launcher, int maximum) =>
            new("fake-relay.exe", "127.0.0.1", 20817, maximum, 1f, 60_000L,
                Path.GetTempPath(), launcher);

        private MatchReservation Reservation(MatchConnectivityDescriptor descriptor,
            out DevelopmentMatchAdmissionRegistry admission)
        {
            MatchId game = new("game");
            LobbyMatchId lobby = new("lobby");
            admission = new DevelopmentMatchAdmissionRegistry(game);
            MatchAdmissionGrant[] grants = new MatchAdmissionGrant[2];
            for (int index = 0; index < 2; index++)
            {
                PlayerAccountId account = new("account-" + index);
                MatchPlayerId player = new(index == 0 ? "player-a" : "player-b");
                grants[index] = new MatchAdmissionGrant(lobby, game, account, player, descriptor,
                    admission.Register(account, player, now + 60_000L));
            }
            return new MatchReservation(lobby, game,
                new ServerConnectivityDescriptor("10.0.0.5", 19817), now + 60_000L, descriptor, grants);
        }

        private sealed class FakeRelayLauncher : ILocalRelayProcessLauncher
        {
            public int StoppedProcessId { get; private set; }
            public bool TryLaunch(string executablePath, string listenAddress, ushort listenPort,
                string targetAddress, ushort targetPort, string readyPath, float timeoutSeconds,
                int parentProcessId, float lifetimeSeconds, out int processId, out string failure)
            {
                processId = 42;
                failure = string.Empty;
                return true;
            }

            public bool TryStop(int processId)
            {
                StoppedProcessId = processId;
                return true;
            }
        }
    }
}

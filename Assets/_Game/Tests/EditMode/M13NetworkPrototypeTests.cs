using NUnit.Framework;
using SwingPop.Online;

namespace SwingPop.Tests
{
    public sealed class M13NetworkPrototypeTests
    {
        [Test]
        public void CommandLineDefaultsToOfflineSingle()
        {
            NetworkLaunchOptions options = NetworkLaunchOptions.Parse(System.Array.Empty<string>());
            Assert.That(options.Mode, Is.EqualTo(MultiplayerDevelopmentMode.OfflineSingle));
            Assert.That(options.Address, Is.EqualTo("127.0.0.1"));
            Assert.That(options.Port, Is.EqualTo(7777));
        }

        [Test]
        public void CommandLineParsesExactHostClientAddressAndPortOptions()
        {
            NetworkLaunchOptions host = NetworkLaunchOptions.Parse(new[]
                { "-swingpopHost", "-swingpopAddress=127.0.0.1", "-swingpopPort=17777" });
            Assert.That(host.Mode, Is.EqualTo(MultiplayerDevelopmentMode.NetworkHost));
            Assert.That(host.Port, Is.EqualTo(17777));
            NetworkLaunchOptions client = NetworkLaunchOptions.Parse(new[] { "-swingpopClient" });
            Assert.That(client.Mode, Is.EqualTo(MultiplayerDevelopmentMode.NetworkClient));
        }

        [Test]
        public void HostBindsConnectionToServerAssignedPlayer()
        {
            ConnectionPlayerRegistry registry = new();
            MatchPlayerId playerB = new("player-b");
            Assert.That(registry.TryBind(42, playerB), Is.True);
            Assert.That(registry.IsBoundPlayer(42, playerB), Is.True);
            Assert.That(registry.TryBind(42, new MatchPlayerId("spoof")), Is.False);
        }

        [Test]
        public void SpoofedPlayerDoesNotMatchConnectionBinding()
        {
            ConnectionPlayerRegistry registry = new();
            registry.TryBind(7, new MatchPlayerId("player-b"));
            Assert.That(registry.IsBoundPlayer(7, new MatchPlayerId("player-a")), Is.False);
        }

        [Test]
        public void StaleAndDuplicateEnvelopeSequencesAreRejected()
        {
            NetworkSequenceGuard guard = new();
            Assert.That(guard.TryAccept(1), Is.True);
            Assert.That(guard.TryAccept(1), Is.False);
            Assert.That(guard.TryAccept(0), Is.False);
            Assert.That(guard.TryAccept(2), Is.True);
        }

        [Test]
        public void OversizedPayloadIsRejectedAtSixtyFourKilobytesBoundary()
        {
            Assert.That(NetworkMessageRules.IsPayloadSizeValid(OnlineProtocol.MaximumPayloadBytes), Is.True);
            Assert.That(NetworkMessageRules.IsPayloadSizeValid(OnlineProtocol.MaximumPayloadBytes + 1), Is.False);
        }

        [Test]
        public void ProtocolMismatchIsRejectedBeforeDispatch()
        {
            string json = UnityEngine.JsonUtility.ToJson(new NetworkMessageEnvelope(
                NetworkMessageType.Ping, default, 1, "{}"));
            json = json.Replace($"\"protocolVersion\":{OnlineProtocol.CurrentVersion}", "\"protocolVersion\":999");
            NetworkMessageEnvelope mismatch = UnityEngine.JsonUtility.FromJson<NetworkMessageEnvelope>(json);
            Assert.That(NetworkMessageRules.ValidateEnvelope(mismatch, new NetworkSequenceGuard()),
                Is.EqualTo(ShotRejectReason.UnsupportedVersion));
        }

        [Test]
        public void ConnectionStateMachineAllowsExpectedLifecycleAndRestart()
        {
            NetworkConnectionStateMachine state = new();
            Assert.That(state.TryTransition(NetworkConnectionState.Starting), Is.True);
            Assert.That(state.TryTransition(NetworkConnectionState.Listening), Is.True);
            Assert.That(state.TryTransition(NetworkConnectionState.Handshaking), Is.True);
            Assert.That(state.TryTransition(NetworkConnectionState.Connected), Is.True);
            Assert.That(state.TryTransition(NetworkConnectionState.InMatch), Is.True);
            Assert.That(state.TryTransition(NetworkConnectionState.Disconnected), Is.True);
            Assert.That(state.TryTransition(NetworkConnectionState.Starting), Is.True);
        }

        [Test]
        public void InvalidConnectionStateJumpIsBlocked()
        {
            NetworkConnectionStateMachine state = new();
            Assert.That(state.TryTransition(NetworkConnectionState.InMatch), Is.False);
            Assert.That(state.State, Is.EqualTo(NetworkConnectionState.Offline));
        }

        [Test]
        public void SnapshotHashIsStableAndChangesWithAuthoritativeVersion()
        {
            MatchSnapshot one = Snapshot(1);
            MatchSnapshot same = Snapshot(1);
            MatchSnapshot two = Snapshot(2);
            Assert.That(MatchSnapshotHash.Compute(one), Is.EqualTo(MatchSnapshotHash.Compute(same)));
            Assert.That(MatchSnapshotHash.Compute(one), Is.Not.EqualTo(MatchSnapshotHash.Compute(two)));
        }

        [Test]
        public void DisconnectCleanupRemovesConnectionBinding()
        {
            ConnectionPlayerRegistry registry = new();
            registry.TryBind(9, new MatchPlayerId("player-b"));
            Assert.That(registry.Remove(9), Is.True);
            Assert.That(registry.Count, Is.Zero);
        }

        [Test]
        public void NewerAuthoritativeSnapshotOverwritesOlderClientSnapshot()
        {
            MatchSnapshotStore store = new();
            Assert.That(store.TryApply(Snapshot(3)), Is.True);
            Assert.That(store.TryApply(Snapshot(2)), Is.False);
            Assert.That(store.TryApply(Snapshot(4)), Is.True);
            Assert.That(store.Current.Version, Is.EqualTo(4));
        }

        private static MatchSnapshot Snapshot(long version)
        {
            MatchPlayerId player = new("player-a");
            PlayerSnapshot initial = new(player, "A", 0, 0, true, PlayerConnectionState.Connected,
                0, 0, new NetworkVector3(0f, 0f, 0f), new NetworkVector3(0f, 0f, 0f),
                SwingPop.Gameplay.Course.TerrainSurfaceType.Tee, false);
            return new MatchSnapshot(new MatchId("m13-test"), OnlineProtocol.CurrentVersion, version, "hole-01",
                MatchPhase.Playing, TurnState.PreparingShot, 0, 0, player, new[] { initial });
        }
    }
}

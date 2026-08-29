using NUnit.Framework;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Shot;
using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Tests
{
    public sealed class M14DedicatedAuthorityTests
    {
        [Test]
        public void CommandLineParsesDedicatedServerWithoutChangingDefault()
        {
            Assert.That(NetworkLaunchOptions.Parse(System.Array.Empty<string>()).Mode,
                Is.EqualTo(MultiplayerDevelopmentMode.OfflineSingle));
            NetworkLaunchOptions server = NetworkLaunchOptions.Parse(new[] { "-swingpopServer", "-swingpopPort=17777" });
            Assert.That(server.Mode, Is.EqualTo(MultiplayerDevelopmentMode.DedicatedServer));
            Assert.That(server.Port, Is.EqualTo(17777));
        }

        [Test]
        public void DedicatedServerTransportCannotSubmitAsLocalPlayer()
        {
            GameObject gameObject = new("Dedicated Transport Rule");
            try
            {
                DedicatedServerMatchTransport transport = gameObject.AddComponent<DedicatedServerMatchTransport>();
                Assert.That(transport.SubmitShot(default), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void FirstClientGetsASecondGetsBAndThirdIsRejected()
        {
            DedicatedPlayerSlotAllocator slots = new();
            Assert.That(slots.TryAssign(out MatchPlayerId first), Is.True);
            Assert.That(first.Value, Is.EqualTo("player-a"));
            Assert.That(slots.TryAssign(out MatchPlayerId second), Is.True);
            Assert.That(second.Value, Is.EqualTo("player-b"));
            Assert.That(slots.TryAssign(out _), Is.False);
        }

        [Test]
        public void DisconnectReleasesPlayerSlot()
        {
            DedicatedPlayerSlotAllocator slots = new();
            slots.TryAssign(out MatchPlayerId playerA);
            slots.TryAssign(out _);
            Assert.That(slots.Release(playerA), Is.True);
            Assert.That(slots.TryAssign(out MatchPlayerId replacement), Is.True);
            Assert.That(replacement.Value, Is.EqualTo("player-a"));
        }

        [Test]
        public void ConnectionBindingRejectsPlayerSpoofing()
        {
            ConnectionPlayerRegistry registry = new();
            registry.TryBind(11, new MatchPlayerId("player-a"));
            Assert.That(registry.IsBoundPlayer(11, new MatchPlayerId("player-b")), Is.False);
        }

        [Test]
        public void ClientMessageDirectionIsWhitelisted()
        {
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ShotSubmission), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.MatchStarted), Is.False);
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ShotApproved), Is.False);
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.Snapshot), Is.False);
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.TurnChanged), Is.False);
        }

        [Test]
        public void ServerMessageDirectionExcludesShotSubmission()
        {
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.PlayerAssigned), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.Snapshot), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ShotSubmission), Is.False);
        }

        [Test]
        public void DedicatedLifecycleRequiresWaitingStartingPlayingOrder()
        {
            DedicatedMatchLifecycle lifecycle = new();
            Assert.That(lifecycle.TryTransition(DedicatedMatchLifecycleState.Playing), Is.False);
            Assert.That(lifecycle.TryTransition(DedicatedMatchLifecycleState.Starting), Is.True);
            Assert.That(lifecycle.TryTransition(DedicatedMatchLifecycleState.Playing), Is.True);
            Assert.That(lifecycle.TryTransition(DedicatedMatchLifecycleState.HoleComplete), Is.True);
            Assert.That(lifecycle.TryTransition(DedicatedMatchLifecycleState.Ended), Is.True);
        }

        [Test]
        public void DisconnectAbortsAuthorityAndMarksOnlyBoundPlayerDisconnected()
        {
            MatchAuthorityCore authority = StartedAuthority();
            Assert.That(authority.AbortForDisconnect(new MatchPlayerId("player-b")), Is.True);
            MatchSnapshot snapshot = authority.CurrentSnapshot;
            Assert.That(snapshot.Phase, Is.EqualTo(MatchPhase.Aborted));
            Assert.That(snapshot.GetPlayer(0).ConnectionState, Is.EqualTo(PlayerConnectionState.Connected));
            Assert.That(snapshot.GetPlayer(1).ConnectionState, Is.EqualTo(PlayerConnectionState.Disconnected));
        }

        [Test]
        public void ClientPredictedResultCannotResolveAuthority()
        {
            MatchAuthorityCore authority = StartedAuthority();
            ShotSubmission submission = Submission(authority.CurrentSnapshot, new MatchPlayerId("player-a"));
            ShotSubmissionDecision decision = authority.SubmitShot(submission);
            Assert.That(decision.Accepted, Is.True);
            Assert.That(authority.BeginShotPlayback(decision.Approved), Is.True);
            MatchSnapshot before = authority.CurrentSnapshot;
            Assert.That(before.TurnState, Is.EqualTo(TurnState.ShotPlaying));
            Assert.That(before.Version, Is.EqualTo(authority.CurrentSnapshot.Version));
        }

        [Test]
        public void PresentationPolicyDisablesCameraButNeverClassifiesCollider()
        {
            GameObject gameObject = new("Presentation Policy");
            try
            {
                Camera camera = gameObject.AddComponent<Camera>();
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                Assert.That(DedicatedServerPresentationPolicy.IsPresentationBehaviour(camera), Is.True);
                Assert.That(collider.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ProtocolTwoRemainsCompatibleAndCapacityIsTwo()
        {
            Assert.That(OnlineProtocol.CurrentVersion, Is.GreaterThanOrEqualTo(2));
            Assert.That(OnlineProtocol.DedicatedServerPlayerCapacity, Is.EqualTo(2));
            Assert.That(OnlineProtocol.MaximumPayloadBytes, Is.EqualTo(65536));
        }

        [Test]
        public void PerPlayerGreenAndFairwayStateDoNotBleed()
        {
            MatchAuthorityCore authority = StartedAuthority();
            ShotSubmission submission = Submission(authority.CurrentSnapshot, new MatchPlayerId("player-a"));
            ShotSubmissionDecision decision = authority.SubmitShot(submission);
            authority.BeginShotPlayback(decision.Approved);
            NetworkVector3 green = new(4f, 0.2f, 40f);
            Assert.That(authority.ResolveShot(new NetworkShotResult(decision.Approved.MatchId,
                decision.Approved.PlayerId, decision.Approved.TurnIndex, decision.Approved.ShotSequence,
                green, green, TerrainSurfaceType.Green, 1, 0, false, false)), Is.True);
            MatchSnapshot snapshot = authority.CurrentSnapshot;
            Assert.That(snapshot.GetPlayer(0).Lie, Is.EqualTo(TerrainSurfaceType.Green));
            Assert.That(snapshot.GetPlayer(1).Lie, Is.EqualTo(TerrainSurfaceType.Tee));
        }

        private static MatchAuthorityCore StartedAuthority()
        {
            MatchAuthorityCore authority = new();
            NetworkVector3 tee = new(0f, 0.2f, 0f);
            authority.StartMatch(new MatchId("m14-edit"), "hole-01", new[]
            {
                new PlayerSnapshot(new MatchPlayerId("player-a"), "A", 0, 0, false,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false),
                new PlayerSnapshot(new MatchPlayerId("player-b"), "B", 1, 1, false,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false)
            });
            return authority;
        }

        private static ShotSubmission Submission(MatchSnapshot snapshot, MatchPlayerId player)
        {
            ShotCommand command = new(Vector3.forward, Vector3.forward, 0f, 0.6f, 1f, 0f,
                ImpactGrade.Perfect, 0.6f, 0f, 22f, 35f, ShotSpin.None);
            command = command.WithClub(ClubType.Driver, 22f, 35f, 1f, 1f);
            return new ShotSubmission(snapshot.MatchId, player, snapshot.TurnIndex,
                snapshot.ShotSequence + 1, OnlineProtocol.CurrentVersion, command);
        }
    }
}

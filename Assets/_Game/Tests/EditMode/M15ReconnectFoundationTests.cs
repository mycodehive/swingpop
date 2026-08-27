using System;
using System.Collections.Generic;
using NUnit.Framework;
using SwingPop.Gameplay.Course;
using SwingPop.Online;
using UnityEngine;

namespace SwingPop.Tests
{
    public sealed class M15ReconnectFoundationTests
    {
        private sealed class TokenSource : IReconnectTokenSource
        {
            private int value;
            public string CreateSecret() => $"test-secret-{++value:D2}-with-sufficient-entropy-placeholder";
        }

        [Test]
        public void InitialTicketContainsStableMatchPlayerAndGeneration()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket ticket);
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(ticket.MatchId.Value, Is.EqualTo("m15-match"));
            Assert.That(ticket.PlayerId.Value, Is.EqualTo("player-a"));
            Assert.That(ticket.SessionGeneration, Is.EqualTo(1));
            Assert.That(ticket.Secret, Is.Not.Empty);
        }

        [Test]
        public void GraceKeepsReservedSessionAndUsesServerDeadline()
        {
            ReconnectSessionRegistry registry = Registry(out _);
            Assert.That(registry.TryEnterGrace(PlayerA, 1_000, 30_000, out long deadline), Is.True);
            Assert.That(deadline, Is.EqualTo(31_000));
            Assert.That(registry.TryGet(PlayerA, out PlayerConnectionState state, out _, out long stored), Is.True);
            Assert.That(state, Is.EqualTo(PlayerConnectionState.ReconnectGrace));
            Assert.That(stored, Is.EqualTo(deadline));
        }

        [Test]
        public void ValidTicketReconnectsSamePlayerAndRotatesGeneration()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket ticket);
            registry.TryEnterGrace(PlayerA, 1_000, 30_000, out _);
            ReconnectValidationResult result = registry.ValidateAndRotate(new ReconnectRequestMessage(ticket, 7), 2_000, false);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RotatedTicket.PlayerId, Is.EqualTo(PlayerA));
            Assert.That(result.RotatedTicket.SessionGeneration, Is.EqualTo(2));
            Assert.That(result.RotatedTicket.Secret, Is.Not.EqualTo(ticket.Secret));
        }

        [Test]
        public void WrongSecretIsRejected()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket ticket);
            registry.TryEnterGrace(PlayerA, 1_000, 30_000, out _);
            ReconnectTicket wrong = new(ticket.MatchId, ticket.PlayerId, ticket.SessionGeneration,
                "wrong", ticket.IssuedAtUnixMilliseconds, ticket.ExpiresAtUnixMilliseconds);
            Assert.That(registry.ValidateAndRotate(new ReconnectRequestMessage(wrong, 0), 2_000, false).Reason,
                Is.EqualTo(ReconnectRejectReason.InvalidTicket));
        }

        [Test]
        public void WrongPlayerIsRejected()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket ticket);
            registry.TryEnterGrace(PlayerA, 1_000, 30_000, out _);
            ReconnectTicket wrong = new(ticket.MatchId, new MatchPlayerId("player-b"), ticket.SessionGeneration,
                ticket.Secret, 0, 0);
            Assert.That(registry.ValidateAndRotate(new ReconnectRequestMessage(wrong, 0), 2_000, false).Reason,
                Is.EqualTo(ReconnectRejectReason.UnknownPlayer));
        }

        [Test]
        public void WrongMatchIsRejected()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket ticket);
            registry.TryEnterGrace(PlayerA, 1_000, 30_000, out _);
            ReconnectTicket wrong = new(new MatchId("other"), ticket.PlayerId, ticket.SessionGeneration,
                ticket.Secret, 0, 0);
            Assert.That(registry.ValidateAndRotate(new ReconnectRequestMessage(wrong, 0), 2_000, false).Reason,
                Is.EqualTo(ReconnectRejectReason.UnknownMatch));
        }

        [Test]
        public void ExpiredGraceRejectsTicketThenExpiresSlot()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket ticket);
            registry.TryEnterGrace(PlayerA, 1_000, 3_000, out _);
            Assert.That(registry.ValidateAndRotate(new ReconnectRequestMessage(ticket, 0), 4_001, false).Reason,
                Is.EqualTo(ReconnectRejectReason.ExpiredTicket));
            Assert.That(registry.TryExpire(4_001, out MatchPlayerId expired), Is.True);
            Assert.That(expired, Is.EqualTo(PlayerA));
        }

        [Test]
        public void OldTicketCannotReplayAfterRotation()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket first);
            registry.TryEnterGrace(PlayerA, 1_000, 30_000, out _);
            ReconnectValidationResult accepted = registry.ValidateAndRotate(new ReconnectRequestMessage(first, 0), 2_000, false);
            registry.TryEnterGrace(PlayerA, 3_000, 30_000, out _);
            Assert.That(registry.ValidateAndRotate(new ReconnectRequestMessage(first, 0), 4_000, false).Reason,
                Is.EqualTo(ReconnectRejectReason.InvalidTicket));
            Assert.That(accepted.RotatedTicket.SessionGeneration, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateActiveBindingIsRejected()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket ticket);
            registry.TryEnterGrace(PlayerA, 1_000, 30_000, out _);
            Assert.That(registry.ValidateAndRotate(new ReconnectRequestMessage(ticket, 0), 2_000, true).Reason,
                Is.EqualTo(ReconnectRejectReason.PlayerAlreadyConnected));
        }

        [Test]
        public void EndedMatchInvalidatesTicket()
        {
            ReconnectSessionRegistry registry = Registry(out ReconnectTicket ticket);
            registry.TryEnterGrace(PlayerA, 1_000, 30_000, out _);
            registry.MarkMatchEnded();
            Assert.That(registry.ValidateAndRotate(new ReconnectRequestMessage(ticket, 0), 2_000, false).Reason,
                Is.EqualTo(ReconnectRejectReason.MatchEnded));
        }

        [Test]
        public void AuthorityGracePreservesBallLieScorePenaltyAndTurn()
        {
            MatchAuthorityCore authority = Authority();
            MatchSnapshot before = authority.CurrentSnapshot;
            Assert.That(authority.EnterReconnectGrace(PlayerA), Is.True);
            MatchSnapshot after = authority.CurrentSnapshot;
            Assert.That(after.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(after.TurnIndex, Is.EqualTo(before.TurnIndex));
            Assert.That(after.ShotSequence, Is.EqualTo(before.ShotSequence));
            Assert.That(after.GetPlayer(0).BallPosition, Is.EqualTo(before.GetPlayer(0).BallPosition));
            Assert.That(after.GetPlayer(0).Lie, Is.EqualTo(before.GetPlayer(0).Lie));
            Assert.That(after.GetPlayer(0).StrokeCount, Is.EqualTo(before.GetPlayer(0).StrokeCount));
            Assert.That(after.GetPlayer(0).PenaltyCount, Is.EqualTo(before.GetPlayer(0).PenaltyCount));
        }

        [Test]
        public void AuthorityReconnectChangesOnlyConnectionAndVersion()
        {
            MatchAuthorityCore authority = Authority();
            authority.EnterReconnectGrace(PlayerA);
            MatchSnapshot grace = authority.CurrentSnapshot;
            Assert.That(authority.ReconnectPlayer(PlayerA), Is.True);
            MatchSnapshot restored = authority.CurrentSnapshot;
            Assert.That(restored.GetPlayer(0).ConnectionState, Is.EqualTo(PlayerConnectionState.Connected));
            Assert.That(restored.Version, Is.EqualTo(grace.Version + 1));
            Assert.That(restored.CurrentTurnPlayer, Is.EqualTo(grace.CurrentTurnPlayer));
        }

        [Test]
        public void GraceExpiryAbortsAuthorityWithoutDeletingPlayer()
        {
            MatchAuthorityCore authority = Authority();
            authority.EnterReconnectGrace(PlayerA);
            Assert.That(authority.ExpireReconnectGrace(PlayerA), Is.True);
            Assert.That(authority.CurrentSnapshot.Phase, Is.EqualTo(MatchPhase.Aborted));
            Assert.That(authority.CurrentSnapshot.PlayerCount, Is.EqualTo(2));
            Assert.That(authority.CurrentSnapshot.GetPlayer(0).ConnectionState, Is.EqualTo(PlayerConnectionState.Expired));
        }

        [Test]
        public void ReconnectMessageDirectionsCannotBeSpoofed()
        {
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ReconnectRequest), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromClient(NetworkMessageType.ReconnectAccepted), Is.False);
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ReconnectAccepted), Is.True);
            Assert.That(NetworkMessageRules.IsAllowedFromServer(NetworkMessageType.ReconnectRequest), Is.False);
        }

        [Test]
        public void TicketJsonRoundTripKeepsCredentialForClientHandoff()
        {
            Registry(out ReconnectTicket ticket);
            ReconnectTicket restored = JsonUtility.FromJson<ReconnectTicket>(JsonUtility.ToJson(ticket));
            Assert.That(restored.IsValid, Is.True);
            Assert.That(restored.Secret, Is.EqualTo(ticket.Secret));
            Assert.That(restored.PlayerId, Is.EqualTo(ticket.PlayerId));
        }

        private static readonly MatchPlayerId PlayerA = new("player-a");

        private static ReconnectSessionRegistry Registry(out ReconnectTicket ticket)
        {
            ReconnectSessionRegistry registry = new(new TokenSource());
            ticket = registry.Register(new MatchId("m15-match"), PlayerA, 500);
            return registry;
        }

        private static MatchAuthorityCore Authority()
        {
            NetworkVector3 tee = new(1f, 0.2f, 2f);
            MatchAuthorityCore authority = new();
            authority.StartMatch(new MatchId("m15-authority"), "hole-01", new[]
            {
                new PlayerSnapshot(PlayerA, "A", 0, 0, false, PlayerConnectionState.Connected,
                    2, 1, tee, tee, TerrainSurfaceType.Rough, false),
                new PlayerSnapshot(new MatchPlayerId("player-b"), "B", 1, 1, false,
                    PlayerConnectionState.Connected, 0, 0, tee, tee, TerrainSurfaceType.Tee, false)
            });
            return authority;
        }
    }
}

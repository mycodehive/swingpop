# M15 Match Lifecycle / Reconnect Architecture

## Goals

- Preserve a two-player dedicated match when one client process disappears temporarily.
- Keep the disconnected player's stable ID, slot, ball position, last-valid position, lie, strokes, penalties, holed flag, turn, shot sequence, and snapshot version under server authority.
- Suspend new gameplay commands for both players, permit an already approved server shot to settle, and resume from the latest authoritative snapshot.
- Prove replacement of a killed client with a new executable process using a rotated development reconnect credential.

## Non-Goals

- Authentication, account identity, secure credential storage, relay/NAT, lobby, matchmaking, database persistence, server crash recovery, multi-hole continuation, and M16 work are not included.
- The reconnect ticket is not a login token and is not suitable for an untrusted production network.

## Match Lifecycle

Lifecycle is orthogonal to `MatchPhase`; the server remains the single transition owner.

```text
WaitingForPlayers -> Starting -> Playing -> HoleComplete -> Ended
                                |       ^
                                v       |
                         ReconnectGrace
                                |
                                v
                             Aborted -> Ended
```

`Playing -> ReconnectGrace` suspends submissions without discarding the gameplay phase. Successful rebind returns to `Playing`; deadline expiry publishes `Aborted`, then closes the lifecycle as `Ended`.

## Player Connection Lifecycle

```text
Connected -> ReconnectGrace -> Connected
                       |
                       v
                    Expired
```

`Disconnected` remains available for older M12-M14 contracts. M15 dedicated matches use `ReconnectGrace` and `Expired` explicitly.

## Reconnect Grace

`MultiplayerDevelopmentSettings.ReconnectGraceSeconds` owns the value. The production-facing default is 30 seconds; `-swingpopReconnectGrace=3..120` is an explicit development-test override. `SystemServerClock` supplies the deadline. Client clocks only display remaining time and never decide validity.

## Reconnect Credential

`ReconnectTicket` contains MatchId, PlayerId, SessionGeneration, a 256-bit cryptographically generated secret, issue time, and optional expiry. The client holds the plaintext in memory. The server retains only SHA-256 and compares hashes in fixed time. `-swingpopReconnectOutput` and `-swingpopReconnectFile` provide a temporary-file bridge solely for killing and replacing a development client process. Secrets are never written to logs or the repository.

## Connection Binding

The initial connection receives `PlayerAssigned` plus `ReconnectTicketIssued`. A replacement sends `ReconnectRequest(protocol, match, player, generation, secret, lastSnapshotVersion)`. The server verifies protocol, IDs, grace state/deadline, hash, generation, and absence of an active binding before binding the new UTP connection to the same PlayerId. Reserved slots cannot be filled by a normal `ClientHello`.

## Ticket Rotation

Each successful reconnect increments SessionGeneration and creates a new 256-bit secret before `ReconnectAccepted` is sent. Replaying the prior generation or secret returns `InvalidTicket`. Ending or expiring a match erases stored hashes.

## Disconnect During Shot

The server rejects every new `ShotSubmission` while suspended, including commands from the remaining player. An `ApprovedShot` already running on the server is allowed to settle through Rigidbody/HoleFlow. Its authoritative result is saved once; reconnect never replays the approval and therefore cannot double-launch the ball.

## Snapshot Restore

```text
New Client A       Dedicated Server                    Client B
    | ReconnectRequest    |                                |
    |-------------------->| validate + rotate + rebind     |
    |<-- ReconnectAccepted|                                |
    |<------ Snapshot ----|---------- Snapshot ----------->|
    |  restore vN         | lifecycle Playing              |
    |<-- LifecycleChanged |------ LifecycleChanged ------->|
```

The accepted client receives the current authoritative snapshot after rebinding. `MatchSnapshotStore` rejects stale/duplicate versions. Ball/HoleFlow restore occurs only at a stable `PreparingShot` boundary.

## Match Suspend / Resume

Pause-on-disconnect is whole-match policy. `MatchSessionController.IsMatchSuspended` gates the existing shot commit path and HUD action. The remaining client displays `WAITING FOR PLAYER`. Resume occurs only when every grace player is connected again.

## Grace Expiry

The dedicated transport checks its two bounded session entries once per frame. Expiry marks the player `Expired`, invalidates all match ticket hashes, publishes the final `Aborted / TurnComplete` snapshot, notifies the remaining client, and transitions lifecycle to `Ended` after a short bounded presentation window. No turn waits on an expired slot.

## Cleanup

- At most two registered player sessions and two normal peer bindings exist.
- At most two additional unhandshaken sockets are admitted; handshake timeout removes them.
- Reconnect requests are limited per peer and per one-second server window.
- Process shutdown clears peers, rejected sockets, bindings, slots, token hashes, timers, and lifecycle state.
- Client retries are limited to three by default and do not create overlapping drivers or subscriptions.

## Headless Safety

The dedicated process still uses the typed M14 presentation-disable policy. Camera, Canvas, Audio, Animator, input, VFX, and debug presentation are not authority dependencies; Ball/HoleFlow and colliders remain active. Network processes explicitly run in the background.

## Security Boundary

M15 provides random bearer credentials, server-side hashes, rotation, generation replay protection, direction allowlists, payload/sequence validation, connection binding, duplicate rejection, and basic rate limiting. It provides no identity authentication, TLS policy, secure client storage, backend revocation, durable audit, DDoS protection, or server-crash persistence.

For Windows standalone localhost acceptance, M15 uses Unity Transport's `WebSocketNetworkInterface`, which is TCP-backed and isolates each peer's OS connection. The M12-M14 UTP envelope, reliable ordering pipeline, fragmentation, and 64 KiB cap remain unchanged. Relay/provider transport selection is future work.

## Message Direction Matrix

| Message | Client -> Server | Server -> Client | Validation |
|---|---:|---:|---|
| ClientHello | Yes | No | Player role, capacity, match not started |
| PlayerAssigned | No | Yes | Stable server slot |
| ReconnectTicketIssued | No | Yes | Bound player only |
| ReconnectRequest | Yes | No | Protocol, IDs, grace, deadline, generation, hash, rate |
| ReconnectAccepted | No | Yes | Same player, rotated ticket, snapshot version |
| ReconnectRejected | No | Yes | Typed reason; socket closed |
| MatchLifecycleChanged | No | Yes | Server-owned lifecycle/deadline |
| ShotSubmission | Yes | No | Binding, rate, turn, sequence, not suspended |
| ShotApproved / ShotRejected | No | Yes | Server authority only |
| Snapshot / TurnChanged | No | Yes | Versioned authority state |
| PredictedShotResult / SnapshotHash | Yes | No | Telemetry only |
| Ping / Pong / DisconnectNotice | Yes | Yes | Envelope and direction checks |

## Production Gaps

- Authenticated account-to-player sessions and protected credential storage
- TLS/encryption policy and backend-issued, revocable credentials
- Persistent match state and dedicated-server crash recovery
- Relay/NAT, lobby/match creation, region routing, deployment and observability
- WAN loss/jitter/roaming, soak/load/chaos and adversarial security testing
- Production reconnect UI, accessibility, localization, forfeit and multi-hole policies


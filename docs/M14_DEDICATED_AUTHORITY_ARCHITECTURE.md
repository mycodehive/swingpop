# M14 Dedicated Authority Architecture

## Goals

- Run the existing Hole01 match authority, ball physics, terrain, hazard, cup, and scoring flow in an independent server process.
- Connect exactly two remote clients and assign Player A/B by accepted connection order.
- Keep M12/M13 DTOs, `IMatchAuthority`, `IMatchTransport`, protocol envelope, snapshot hash, and client playback contract intact.
- Keep `OfflineSingle` as the default mode and retain M13 Host/Client for regression testing.

## Non-Goals

- No lobby, matchmaking, relay/NAT traversal, reconnect, authentication, encryption, persistence, ranking, multi-hole rotation, deployment orchestration, or production anti-cheat.
- No M15 implementation and no change to gameplay balance or presentation quality.

## M13 vs M14

| Concern | M13 | M14 |
|---|---|---|
| Authority process | Player A host | Independent dedicated server |
| Server player | Host is Player A | Server has no player |
| Remote capacity | One client | Two remote clients |
| Player IDs | Host A, assigned client B | Connection order assigns A, then B |
| Simulation | Host scene physics | Server scene physics |
| Client contract | Approved playback + snapshots | Preserved |
| Protocol | Version 2 | Version 2, additive messages only |

## Dedicated Authority Model

`MatchSessionController` selects `DedicatedServerMatchTransport` only in `DedicatedServer` mode. The server owns `LocalMatchAuthority`, validates submissions, begins the approved shot, runs the existing gameplay graph, accepts only its local `NetworkShotResult`, and publishes the next snapshot. Neither client can mutate authority with a predicted result.

```text
Client A                 Dedicated Server                 Client B
   | ShotSubmission             |                            |
   |--------------------------->| validate turn/id/sequence  |
   |                            | approve + simulate         |
   |<------ ApprovedShot -------|------- ApprovedShot ------>|
   | approved playback          | Ball -> HoleFlow           | approved playback
   |<--------- Snapshot --------|--------- Snapshot -------->|
   | hash acknowledgement       | authoritative result       | hash acknowledgement
```

## Server Bootstrap

`-swingpopServer` selects dedicated mode before normal scene startup. `DedicatedServerBootstrap` runs first, enables background execution, and applies a typed presentation-disable policy. A separate development-only `-swingpopAutomatedDedicatedTest` probe is required for automated process shots/captures; ordinary builds never auto-play.

## Server Scene

The server loads the same `Hole01_SkyIsland` scene so Ball, HoleFlow, cup, hazard, terrain, and collider geometry cannot drift from the client scene. It disables cameras, canvases, audio sources, lights, animators, renderers, particles, input, character presentation, camera presentation, HUD, VFX, and debug presentation by component type. Gameplay colliders and physics behaviours remain enabled.

The verified headless runtime reported 0 active cameras, 0 canvases, 0 audio sources, 9 gameplay colliders, and 356 disabled renderers.

## Server Shot Execution

After authority accepts a `ShotSubmission`, the server broadcasts `ApprovedShot` and calls `ShotFlowController.TryExecuteAuthoritativeShot`. That entry point reuses the committed-shot calculation and existing Rigidbody launch but bypasses Character Animator/impact-marker timing. Ball settle, hazard recovery, lie selection, strokes, penalties, putter choice, cup capture, and score still originate in the existing Ball/HoleFlow graph.

## Client Playback

Clients cannot launch while awaiting approval or while it is the other player's turn. An approved command enters the existing client presentation path once. Client ball flight is visual prediction; the authoritative settle snapshot restores position, last valid position, lie, strokes, penalties, holed state, current player, turn index, and shot sequence.

## Result Authority

- Allowed authority result source: server-local Ball/HoleFlow only.
- Client `PredictedShotResult`: telemetry/desync comparison only.
- Server cannot originate a player `ShotSubmission`.
- Wrong player, wrong turn, duplicate sequence, spoofed player ID, rate excess, oversized payload, invalid direction, or incompatible envelope is rejected without authority mutation.

## Snapshot Sync

Every authoritative boundary publishes a versioned full match snapshot. All processes compute `MatchSnapshotHash`; clients return version/hash telemetry. The three-process acceptance run converged at MatchComplete version 13 with hash `5132ECAFAE0F1155` and desync count 0 on both clients and server.

## Player Assignment

`DedicatedPlayerSlotAllocator` assigns `player-a` to the first completed handshake and `player-b` to the second. The server starts only after both slots are bound. A third socket receives `ConnectionRejected(MatchFull)` and is disconnected without entering authority state.

## Match Lifecycle

```text
Idle -> WaitingForPlayers -> Starting -> Playing -> HoleComplete
                                      \-> Aborted (player disconnect)
```

The server is not represented in the player array. Two player snapshots are created only when A/B are ready. `HoleComplete` requires both player snapshots to be holed.

## Disconnect

A client disconnect releases its socket/player binding. During a started match, authority marks that player disconnected, changes the match to `Aborted`, increments the snapshot version, and broadcasts the state to the remaining client. If the server disappears, both clients receive a remote-disconnect callback and leave cleanly. Reconnect and grace periods remain future work.

## Headless Safety

The verified command is:

```powershell
Builds\M14Server\SwingPopServer.exe -swingpopServer -batchmode -nographics -swingpopAddress=127.0.0.1 -swingpopPort=7777
```

The installed Unity editor has no Dedicated Server build-target variation. M14 therefore produces a Windows Development Player fallback and runs it headlessly with the command above. A native Dedicated Server target remains a production gap.

## Security Boundary

M14 adds basic trust-boundary enforcement: server-assigned identity, per-connection binding, protocol/version/sequence checks, message direction allowlists, payload cap, rate guard, command validation, and server-only results. It is not production security. Authentication, secure sessions, transport encryption policy, hardened replay/rate controls, DDoS protection, backend trust, audit, secrets, and deployment isolation are not implemented.

## Message Direction Matrix

| Message | Client -> Server | Server -> Client |
|---|---:|---:|
| ClientHello | Yes | No |
| PlayerAssigned | No | Yes |
| ConnectionRejected | No | Yes |
| MatchStarted / Snapshot / TurnChanged | No | Yes |
| ShotSubmission | Yes | No |
| ShotApproved / ShotRejected | No | Yes |
| PredictedShotResult / SnapshotHash | Yes, telemetry | No |
| Ping / Pong / DisconnectNotice | Yes | Yes |

## Performance

Server presentation work is disabled and no server Animator is required. Physics uses the current project fixed-step and the same Hole01 collider graph. The completed four-approved-shot automation run reported server TX 41,606 bytes, RX 15,918 bytes, 204 transport messages, and desync 0. This is a functional foundation measurement, not a load/soak/Profiler certification.

## Production Gaps

- Native Dedicated Server build target/module and deployment image
- Reconnect, grace/forfeit policy, session recovery, lobby, relay, matchmaking, auth
- WAN packet loss/jitter/latency tests, soak/load/chaos tests, metrics and alerting
- Encryption and hardened security controls
- Multi-hole lifecycle and persistent results
- Production bandwidth optimization (delta snapshots/compression)


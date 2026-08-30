# SwingPop Online TODO

## Completed Foundations

### M12 Online Foundation

- [x] `IMatchAuthority` / `IMatchTransport` boundary
- [x] Serializable shot/result/snapshot DTOs
- [x] Approval-before-launch input gate
- [x] LocalLoopback transport and LocalTwoPlayer simulation
- [x] Turn, duplicate, snapshot-version, and player-state rules
- [x] OfflineSingle remains the default

### M13 Real Network Prototype

- [x] Official Unity Transport adapter under `IMatchTransport`
- [x] Independent localhost Host/Client processes
- [x] Server-assigned Player B connection binding and spoof rejection
- [x] Protocol-2 envelope, reliable ordering, fragmentation, and 64KB cap
- [x] Host-authoritative approval, simulation result, snapshot, and turn
- [x] Client approved playback and settle-boundary correction
- [x] Snapshot hash and predicted-result desync telemetry
- [x] Timeout, disconnect cleanup, and listener restart
- [x] Development build, command-line roles, validator, tests, and captures

### M14 Dedicated Authority Foundation

- [x] Independent dedicated server role with no local player
- [x] Two remote slots with connection-order Player A/B assignment
- [x] Third-client `MatchFull` rejection and message-direction enforcement
- [x] Server-only approval, Rigidbody simulation, result, snapshot, and turn authority
- [x] Animator-independent server shot entry point using existing Ball/HoleFlow
- [x] Shared Hole01 scene with typed headless presentation shutdown and collider retention
- [x] Client approved playback, settle correction, snapshot version/hash convergence
- [x] Disconnect abort/cleanup, server-termination handling, and listener restart
- [x] Server/client builds, validator, tests, three-process logs, and captures

### M15 Match Lifecycle / Reconnect Foundation

- [x] Server-owned match and player connection lifecycles
- [x] Whole-match reconnect grace with preserved slot and authoritative player snapshot
- [x] Cryptographic development reconnect ticket, server-side hash, generation rotation, and replay rejection
- [x] Same-PlayerId connection rebind from a newly launched client process
- [x] Disconnect-during-shot settle, suspended input gate, latest snapshot restore, and resume
- [x] Grace expiry to Aborted/Ended without turn deadlock
- [x] Minimal HUD/F2 status, validator, automated tests, real-process logs, and A-G captures

### M16 Authentication / Player Session Foundation

- [x] Separate account, auth-session, connection, match-player, and reconnect-ticket identities
- [x] Development-only HMAC-SHA256 provider with runtime-generated 256-bit key
- [x] Authentication-before-admission handshake and unauthenticated message allowlist
- [x] Server-owned connection/account/session and match-player/account bindings
- [x] One active match connection per account and separate `MatchFull` admission result
- [x] Same-account reconnect requirement and stolen-ticket wrong-account rejection
- [x] Token tamper, expiry, revocation, rate-limit seam, safe F2 telemetry, tests, builds, and real-process evidence

### M17 Lobby / Match Creation Foundation

- [x] Backend-replaceable `ILobbyService` with authenticated create/list/get/join/leave/ready/start/close
- [x] Two-player atomic membership, one active room per account, owner authorization, revision ordering, and Ready reset
- [x] Separate LobbyMatchId -> gameplay MatchId reservation mapping
- [x] Bounded localhost Dedicated Match Server allocation with reservation and ready handshake
- [x] Account/match-bound, expiring, one-time MatchJoinTicket and reserved player assignment
- [x] Minimal Lobby UI, F2-safe telemetry, build/launch tools, tests, real-process flow, and captures

### M18 Relay / NAT Traversal Prototype

- [x] Replaceable `IMatchConnectivityProvider` with Direct and Relay modes
- [x] Separate client/proxy and server/private connectivity descriptors
- [x] Standalone localhost TCP relay-proxy with real byte forwarding telemetry
- [x] Relay credential before Authentication and MatchJoinTicket admission
- [x] Relay reconnect using the existing rotating account-bound ReconnectTicket
- [x] Bounded allocation, timeout, failure release, parent-linked cleanup, and secret-safe reservation
- [x] Direct regression, tests, builds, real A/B shots, reconnect, hashes, and captures

### M19 Production Relay Provider Integration

- [x] Add explicit `ProductionRelay` mode while preserving Direct default and LocalRelay regression mode
- [x] Pin unified Multiplayer Services 2.3.1 and isolate `Unity.Services.*` in a provider assembly
- [x] Integrate real Unity Relay allocate/join with DTLS and dedicated server host authority
- [x] Preserve Relay credential -> Authentication -> MatchJoinTicket/ReconnectTicket admission order
- [x] Verify one real cloud allocation in `asia-northeast1`, two A/B shots, matching hashes, and reconnect generation 2
- [x] Delete the temporary server provider payload after dedicated-process consumption
- [x] Add M19 validator, 21 EditMode tests, 10 PlayMode tests, builds, logs, captures, and quality-gate docs
- [ ] Verify two-router Cross-NAT, controlled WAN profiles, five lifecycle cycles, 30-minute soak, Profiler, and bandwidth/cost

### M20 Public Control Plane Foundation

- [x] Add configurable trusted-WSS public Lobby endpoint without certificate bypass
- [x] Add loopback-only Unity Lobby/health behind a Caddy TLS reverse proxy template
- [x] Add bounded operation rates, connection/room/allocation caps, safe counters, and log redaction
- [x] Separate staging match process lifetime from Lobby PID and add maximum-lifetime/allocator reaping
- [x] Preserve ProductionRelay/admission/reconnect/authority boundaries with no Direct fallback
- [x] Add deployment scripts, tests, quality gate, test log, and capture checklist
- [ ] Deploy to a real VM/domain and verify trusted public TLS
- [ ] Verify different-network Cross-NAT A/B flow and Lobby-outage independence
- [ ] Run WAN Profiles B/C, five real lifecycle cleanups, 30-minute soak, Profiler, bandwidth/cost, and provider audit

## Future Production Work

### Relay / NAT

- [x] Choose the M18 provider seam and validate a local standalone proxy topology
- [x] Prototype bounded allocation, credential expiry, failure release, and parent cleanup
- [x] Integrate a production-capable Unity Relay provider and encrypted DTLS transport
- [x] Verify real cloud allocation/join/gameplay/reconnect on one physical PC
- [ ] Verify two-router Cross-NAT, CGNAT/symmetric NAT, WAN regions, and provider outage/failover
- [ ] Add allocation replenishment/completion release for repeated production matches

### Lobby

- [x] Development create/list/join/leave, readiness, capacity, and room-owner start model
- [ ] Production distributed service, persistence, pagination, owner migration, invites/private rooms
- [ ] Late-join, backfill, and spectator policy

### Matchmaking

- [ ] Queue, region, rule/skill filters, cancellation, timeout
- [ ] Backfill and service availability behavior

### Authentication

- [x] Development account identity to `MatchPlayerId` binding foundation
- [x] In-memory development session expiry/revocation seam
- [ ] Production identity provider and backend-issued tokens
- [ ] Secure platform credential storage, refresh, distributed revocation, logout/kick UX, and audit

### Production Reconnect

- [ ] Backend-authenticated identity proof, secure credential storage, revocation, and server-crash persistence
- [ ] WAN retry/backoff, mobile roaming, forfeit policy, localization/accessibility, and production UX

### Dedicated Deployment / Operations

- [ ] Install and validate Unity Linux Build Support / native Dedicated Server build target
- [ ] Container/image packaging, regions, scaling, health checks, monitoring, and alerting
- [ ] Load, soak, packet-loss/jitter, chaos, and cost/bandwidth gates
- [x] Public TLS Lobby/control-plane deployment foundation and configuration
- [ ] Actual public VM/DNS/certificate deployment and Cross-NAT verification

### Multi-Hole

- [ ] Hole rotation, between-hole state, aggregate score, match completion
- [ ] Course/content/protocol compatibility and result persistence

### Security

- [x] M20 staging TLS/WSS policy with system trust and no validation bypass
- [ ] Production identity, hardened anti-replay, WAF/DDoS, and secret manager
- [ ] Production rate limiting, validation, audit, privacy, abuse, DDoS response
- [ ] Signed result receipt and server-side integrity checks

### Deployment

- [ ] Staging/production environments, regions, scaling, observability
- [ ] Load, packet-loss/jitter, soak, chaos, penetration, LAN, and WAN gates

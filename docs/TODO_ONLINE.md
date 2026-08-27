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

## Future Production Work

### Relay / NAT

- [ ] Choose Relay/provider topology and NAT traversal policy
- [ ] Region selection, allocation lifecycle, failure/expiry handling

### Lobby

- [ ] Create/join/leave, readiness, invite, and room ownership model
- [ ] Late-join and spectator policy

### Matchmaking

- [ ] Queue, region, rule/skill filters, cancellation, timeout
- [ ] Backfill and service availability behavior

### Authentication

- [ ] Account identity to `MatchPlayerId` binding
- [ ] Session token lifecycle, secure storage, expiry, revocation

### Production Reconnect

- [ ] Backend-authenticated identity proof, secure credential storage, revocation, and server-crash persistence
- [ ] WAN retry/backoff, mobile roaming, forfeit policy, localization/accessibility, and production UX

### Dedicated Deployment / Operations

- [ ] Install and validate Unity's native Dedicated Server build target
- [ ] Container/image packaging, regions, scaling, health checks, monitoring, and alerting
- [ ] Load, soak, packet-loss/jitter, chaos, and cost/bandwidth gates

### Multi-Hole

- [ ] Hole rotation, between-hole state, aggregate score, match completion
- [ ] Course/content/protocol compatibility and result persistence

### Security

- [ ] TLS/encryption policy and hardened anti-replay
- [ ] Production rate limiting, validation, audit, privacy, abuse, DDoS response
- [ ] Signed result receipt and server-side integrity checks

### Deployment

- [ ] Staging/production environments, regions, scaling, observability
- [ ] Load, packet-loss/jitter, soak, chaos, penetration, LAN, and WAN gates

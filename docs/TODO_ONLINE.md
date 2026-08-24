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

### Reconnect

- [ ] Grace period, latest-snapshot request, pending-shot reconciliation
- [ ] Rejoin identity proof, timeout, forfeit, and user-facing UX

### Dedicated Authority

- [ ] Move host authority/gameplay simulation to a trusted dedicated process
- [ ] Headless simulation validation, deployment, monitoring, host migration policy

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

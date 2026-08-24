# SwingPop Online TODO

### M12 Foundation

- [x] `IMatchAuthority` / `IMatchTransport` 경계
- [x] protocol version 1 command/result/snapshot DTO
- [x] 승인 전 launch 차단
- [x] LocalLoopback와 simulated latency
- [x] Local 2P A -> B -> A turn flow
- [x] player별 ball/lie/stroke/penalty/holed snapshot
- [x] OfflineSingle 기본값과 기존 Hole01 호환

### Production Transport

- [ ] 실제 networking SDK/transport 선정
- [ ] reliable delivery, ordering, retry, timeout 구현
- [ ] server telemetry와 protocol observability

### Lobby

- [ ] room create/join/leave와 readiness model
- [ ] invite와 party UX

### Matchmaking

- [ ] queue, region, skill/rule filters
- [ ] cancellation과 timeout policy

### Reconnect

- [ ] latest snapshot resync
- [ ] pending shot reconciliation과 grace period
- [ ] disconnect/forfeit UX

### Authentication

- [ ] account identity와 `MatchPlayerId` binding
- [ ] token lifecycle과 secure storage

### Security

- [ ] server-authoritative result verification/simulation
- [ ] rate limit, replay protection, payload limits
- [ ] abuse, audit, privacy policy

### Match Result

- [ ] multiplayer winner/tie/forfeit resolution
- [ ] result persistence와 signed receipt

### Multi-Hole

- [ ] hole rotation, aggregate score, between-hole snapshot
- [ ] course/version compatibility

### Spectator

- [ ] read-only snapshot stream와 delayed view

### Replay

- [ ] approved command/result log
- [ ] physics-independent presentation replay policy

### Deployment

- [ ] server topology, region, scaling, monitoring
- [ ] staging/load/chaos/security test gates

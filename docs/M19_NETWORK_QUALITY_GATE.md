# M19 Network Quality Gate

## Gate Decision

- `M19 PRODUCTION RELAY INTEGRATION: CONDITIONAL GO`
- `REAL PRODUCTION-CAPABLE RELAY PROVIDER: VERIFIED`
- `WAN NETWORK QUALITY GATE: NO-GO`
- `PRODUCTION ONLINE MULTIPLAYER: NO-GO`

The adapter is acceptable as an opt-in integration foundation. It is not an approval to ship public online multiplayer.

## Scorecard

| Gate | Required evidence | M19 result | Decision |
|---|---|---|---|
| Package/API compatibility | pinned supported package, compile/build | MPS 2.3.1; compile and 3 builds pass | GO |
| Architecture isolation | provider behind seam, gameplay SDK-free | validator/test pass | GO |
| Real provider flow | allocate, host bind, two joins, gameplay | one cloud allocation passed | GO |
| Authority/admission | server authority, account/ticket ownership | two shots + matching hashes pass | GO |
| Relay reconnect | allocation rejoin + rotating account ticket | generation 2 pass | GO |
| No fallback/security | no Direct fallback, redacted secrets, DTLS | verified in code/logs/tests | GO |
| Cross-NAT | independent routers/networks | not run | NO-GO |
| Typical/Bad WAN | controlled RTT/jitter/loss profiles | not run | NO-GO |
| 30-minute soak | continuous match and reconnect stability | not run | NO-GO |
| Repeated lifecycle | at least 5 allocation/cleanup cycles | not run | NO-GO |
| Performance | Profiler 1080p/60, GC and CPU evidence | not run | NO-GO |
| Bandwidth/cost | real payload and provider usage measurement | not measured | NO-GO |
| Production control plane | public TLS Lobby, durable orchestration | local in-memory only | NO-GO |
| Production identity/ops | identity, monitoring, quotas, incident path | not implemented | NO-GO |

## Acceptance Thresholds for a Future WAN Gate

These are gate targets, not M19 results:

- Profile A Clean: two external networks complete Lobby-to-Hole01, two shots, matching hashes, and reconnect.
- Profile B Typical WAN: controlled RTT/jitter/loss remains playable with no duplicate shot, turn divergence, or ticket corruption.
- Profile C Bad WAN: bounded failure/recovery, clear UI, no deadlock, no authority divergence, and no silent fallback.
- Profile D Disconnect: reconnect within the configured grace, same account/player restoration, rotated ticket, newest snapshot.
- At least five create/join/play/reconnect/cleanup cycles with no orphan process/allocation.
- At least 30 minutes without unbounded memory/GC growth, allocation leak, snapshot divergence, or Console error.
- 1080p target remains 60 FPS with measured main-thread/network overhead and documented bandwidth per player/turn.

Numerical RTT/jitter/loss budgets must be approved after representative remote-region tests; inventing thresholds without product feel evidence is intentionally avoided.

## Blocking Items

1. Deploy or replace the local M17 Lobby with a remotely reachable TLS control plane.
2. Add configuration for remote Lobby address/environment without storing secrets in assets.
3. Run two-router and CGNAT/symmetric-NAT scenarios.
4. Add a controlled network shaper and execute Profiles A-D.
5. Add allocation replenishment and a completion signal for repeated service-side lifecycles.
6. Run 5-cycle cleanup, 30-minute soak, Profiler, payload/bandwidth, quota/cost, and provider outage tests.
7. Replace development authentication with production identity/token storage/revocation and operational monitoring.

## Recommendation

Keep `Direct` as the default and keep ProductionRelay behind explicit opt-in. Retain Unity Relay/MPS 2.3.1 as the selected provider adapter. Do not expose production online matchmaking until every blocking gate above has evidence.


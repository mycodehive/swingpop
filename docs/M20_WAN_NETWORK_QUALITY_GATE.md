# M20 WAN Network Quality Gate

## Decision

- `M20 PUBLIC CONTROL PLANE FOUNDATION: CONDITIONAL GO`
- `PUBLIC TLS LOBBY: NOT VERIFIED`
- `CROSS-NAT FULL FLOW: NOT VERIFIED`
- `REAL WAN GAMEPLAY: NOT VERIFIED`
- `REAL WAN RECONNECT: NOT VERIFIED`
- `SIMULATED TYPICAL WAN: NOT VERIFIED`
- `SIMULATED BAD WAN: NOT VERIFIED`
- `30-MINUTE SOAK: NOT VERIFIED`
- `5-CYCLE RESOURCE CLEANUP: NOT VERIFIED`
- `WAN NETWORK QUALITY GATE: NO-GO`
- `PRODUCTION ONLINE MULTIPLAYER: NO-GO`

The repository now contains a deployable, bounded public-control-plane foundation. No public VM/domain was available, so a local PlayMode run cannot upgrade any public/WAN label to verified.

Local foundation evidence: M20 validator PASS, EditMode 316/316, PlayMode 85/85, and all three Windows Development validation builds PASS. Linux staging artifacts were not produced because the Unity Linux Build Support module is absent.

## Required Topology

Public/staging Caddy + Lobby + remote match allocation, real Unity Relay, Client A on Network A, and Client B on Network B. No client port forwarding is allowed.

## Profiles

| Profile | Target | Current result |
|---|---|---|
| A Clean Cross-NAT | two independent networks, natural A/B shots, hash convergence, reconnect | NOT VERIFIED |
| B Typical simulated | added 80–120 ms latency, 20–40 ms jitter, 1% loss | NOT VERIFIED |
| C Bad simulated | added 200–300 ms latency, 50–100 ms jitter, 3–5% loss | NOT VERIFIED |

Simulation must state the shaping point/direction/tool. Added delay is not the same as measured end-to-end RTT. Report RTT P50/P95 only when timestamped RTT samples actually exist.

## Pass Criteria

- Trusted public TLS with no validation bypass
- Authenticated Create/List/Join/Ready/Start through the public control plane
- ProductionRelay server ready and A/B admission without Direct fallback or port forwarding
- One natural authoritative shot per player
- Duplicate approvals 0, stale snapshot overwrite 0, turn deadlock 0, final snapshot/hash convergence
- Same-account/same-player reconnect with rotated ticket and newest snapshot
- Active match continues during a tested Lobby outage
- Five full lifecycle/cleanup cycles without orphan process/allocation/temp ticket
- At least 30 actual elapsed minutes for soak verification

## Measurement

Capture shot submission→approval, approval→snapshot apply, snapshot→turn enable, and reconnect start→restore. Separate application elapsed timing from transport RTT. Record real vs simulated conditions. Do not infer packet loss or jitter from feel alone.

## Security Failure Conditions

Plaintext public Lobby, certificate bypass, unauthenticated operations, unbounded public resources, private Direct endpoint leakage, port-forwarding dependence, ticket bypass, Lobby gameplay authority, active match corruption on Lobby loss, persistent orphan server, Relay regression, or M12–M19 regression makes the M20 gate `NO-GO`.

## Performance and Cost

Profiler 1080p/60, GC/main-thread overhead, bytes per match/shot, Relay usage, and cost are `NOT VERIFIED`. Provider price/usage claims require the actual official dashboard or current official provider documentation; none was inspected in this work.

## Recommendation

Deploy to one controlled staging VM, execute clean Cross-NAT first, then shaped Profiles B/C, five lifecycle cycles, the full 30-minute soak, Profiler, bandwidth, outage, and provider cleanup audit. Keep public online multiplayer disabled until this gate is no longer No-Go.

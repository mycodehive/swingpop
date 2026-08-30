# M20 Cross-NAT / WAN Test Log

Date: 2026-08-31  
Staging environment: not provisioned  
Result type: implementation/automated validation only; no public deployment

## Decision Labels

- `PUBLIC TLS LOBBY: NOT VERIFIED`
- `CROSS-NAT FULL FLOW: NOT VERIFIED`
- `REAL WAN GAMEPLAY: NOT VERIFIED`
- `REAL WAN RECONNECT: NOT VERIFIED`
- `SIMULATED TYPICAL WAN: NOT VERIFIED`
- `SIMULATED BAD WAN: NOT VERIFIED`
- `30-MINUTE SOAK: NOT VERIFIED`
- `5-CYCLE RESOURCE CLEANUP: NOT VERIFIED`

## Environment

| Item | Evidence |
|---|---|
| Public staging host | unavailable |
| Trusted TLS certificate | not issued/checked |
| Client A network type | not run |
| Client B network type | not run |
| Relay region | not allocated for M20 |
| Port forwarding | not applicable; test not run |

No public IP, account ID, credential, or ticket is recorded.

## Flow

| Step | Result |
|---|---|
| Public Lobby connect | NOT VERIFIED |
| Create / remote List / Join | NOT VERIFIED |
| 2/2 Ready / Start | NOT VERIFIED |
| Remote server ready | NOT VERIFIED |
| Unity Relay A/B join | NOT VERIFIED |
| Natural A shot | NOT VERIFIED |
| Natural B shot | NOT VERIFIED |
| Authoritative hash convergence | NOT VERIFIED |
| Cross-NAT reconnect | NOT VERIFIED |
| Control-plane outage during match | NOT VERIFIED |
| Invalid auth rejection on public endpoint | NOT VERIFIED |
| Stolen join ticket rejection on public endpoint | NOT VERIFIED |

## Timing / Network Profiles

- Shot approval timing: not measured
- Snapshot apply timing: not measured
- Turn enable timing: not measured
- Reconnect restore timing: not measured
- RTT P50/P95: not measured
- Typical simulated WAN: not run
- Bad simulated WAN: not run
- Packet loss/jitter/reorder: not injected or measured

## Soak / Lifecycle / Resources

- Actual soak duration: 0 minutes
- Completed real lifecycle count: 0/5
- Profiler: not run
- Bandwidth per match/shot: not measured
- Relay/provider dashboard: not inspected
- Public-process/orphan audit: not run
- Temporary reservation/ticket audit on staging: not run

## Automated Validation

Unity 6000.5.7f1 batch validation on the local Windows workstation:

- M20 foundation validator: PASS
- C# compilation: PASS; no compiler errors
- Full EditMode: 316/316 passed, 0 failed, 0 skipped (M19 baseline 279 retained; M20 +37 cases)
- Full PlayMode: 85/85 passed, 0 failed, 0 skipped (M19 baseline 73 retained; provider-independent M20 A–L +12)
- Windows WAN Client Development build: PASS
- Windows Lobby validation Development build: PASS
- Windows dedicated server validation Development build: PASS
- Local Windows Lobby process health check: PASS; safe response reported ready with all counters at zero
- Linux staging Lobby/server builds: NOT BUILT; Unity Linux Build Support is not installed
- Local evidence: `Library/M20/*-final.xml`, validator/build/test logs (not committed production artifacts)

These tests do not substitute for public TLS or Cross-NAT evidence.

## Captures

No M20 public/WAN screenshots were generated. `docs/review-captures/m20-wan/README.md` lists the required A–I evidence names; fabricated captures are intentionally absent.

# M19 Production Relay / WAN Network Quality Test Log

Date: 2026-08-30  
Host: Windows 11, one physical PC  
Unity: 6000.5.7f1  
Provider: Unity Relay (`com.unity.services.multiplayer` 2.3.1)  
Transport: Unity Transport 6.5.0, DTLS  
Assigned region: `asia-northeast1`

## Decision Labels

- `M19 PRODUCTION RELAY INTEGRATION: CONDITIONAL GO`
- `REAL PRODUCTION-CAPABLE RELAY PROVIDER: VERIFIED`
- `CROSS-NAT CONNECTIVITY: NOT VERIFIED`
- `WAN GAMEPLAY: NOT VERIFIED`
- `WAN RECONNECT: NOT VERIFIED`
- `30-MINUTE SOAK: NOT VERIFIED`
- `WAN NETWORK QUALITY GATE: NO-GO`
- `PRODUCTION ONLINE MULTIPLAYER: NO-GO`

The integration is a Conditional Go because a real cloud allocation/join/gameplay/reconnect path passed. The WAN quality gate remains No-Go because the test did not cross independent NATs or execute shaped adverse-network and soak profiles.

## Provider / Configuration

- Cloud Project link detected: yes
- Real Relay test opt-in default: disabled
- Successful UGS environment: `production`
- Requested region: automatic
- Assigned region: `asia-northeast1`
- Requested connection type: `dtls`
- Direct fallback during provider tests: none
- Secrets in repository/assets/logs: none found

An earlier opt-in attempt used `development`. Provider authentication returned HTTP 400 `invalid environment name`; the flow stopped with a generic authentication failure and no Direct fallback. The configured default was corrected to the existing `production` environment.

## Automated Validation

- Unity batch compile: PASS, no C# errors
- M19 foundation validator: PASS
- EditMode: 279/279 passed, 0 failed, 0 skipped
- PlayMode: 73/73 passed, 0 failed, 0 skipped
- M19-specific addition: 21 EditMode tests and 10 PlayMode tests
- Windows development builds: Lobby PASS, Dedicated Server PASS, Client PASS
- Direct remains the serialized default: PASS
- Gameplay runtime assembly has no `Unity.Services.*` reference: PASS

The tests cover DTO serialization/version/expiry, opaque provider payload, SDK isolation, redaction, bounded retry/timeout, failure without Direct fallback, server-ready gating, credential/ticket separation, production reservation consumption/deletion, provider-independent allocation/join, account-bound admission, authority shot command preservation, reconnect separation, and cleanup state.

## Real Relay Multi-Process Test

Processes used: Lobby Service, Dedicated Match Server, Client A, Client B. The server and both clients established outbound connections to a real Unity Relay allocation.

- Lobby Create/List/Join/Ready/Owner Start: PASS
- Unity Relay allocate + join code: PASS
- Dedicated Relay bind/listen + established ready marker: PASS
- Client A JoinAllocation + admission: PASS
- Client B JoinAllocation + admission: PASS
- Reserved `player-a` / `player-b` ownership: PASS
- Client A natural shot accepted: PASS
- Client B natural shot accepted: PASS
- Final authoritative turn: 2
- Client A final snapshot hash: `7DA20979070610D1`
- Client B final snapshot hash: `7DA20979070610D1`
- Forced Client A disconnect: PASS
- Relay rejoin and same-account reconnect: PASS
- Rotated reconnect generation: 2
- Reconnect request to accepted event: approximately 3.34 seconds
- Match scene load to first shared snapshot: approximately 2.14 seconds
- Host temporary reservation remaining after consume: none
- Lobby/dedicated service after explicit cleanup: stopped

The first successful cloud allocation exposed a server-side production credential registry bug (`Connectivity rejected: NotRequired`). The server reservation loader accepted only LocalRelay credentials. The condition was corrected to all non-Direct modes, protected by EditMode coverage, rebuilt, and the final real-provider run passed.

## Cross-NAT Test

`NOT VERIFIED`.

No second router, remote PC, CGNAT, symmetric NAT, or mobile network was used. The gameplay data plane used a real cloud Relay, but all SwingPop processes and the M17 Lobby control plane were on one PC.

## WAN Quality Profiles

| Profile | Result | Evidence |
|---|---|---|
| A Clean | Partial only | real cloud Relay, one PC, no shaper |
| B Typical WAN | Not run | no controlled RTT/jitter/loss profile |
| C Bad WAN | Not run | no high impairment profile |
| D Disconnect/Reconnect | Partial only | forced reconnect passed, not independent WAN/NAT |

## Latency / Jitter / Loss

Provider-flow elapsed timings above are application observations, not RTT measurements. RTT percentile, jitter, packet loss, reorder, bandwidth cap, and queue behavior were not measured or injected. No claim is made for shot-input feel under WAN impairment.

## Soak / Resource Leak

- One real cloud allocation lifecycle: PASS
- Temporary provider payload file consumed/deleted: PASS
- Explicit process cleanup: PASS
- Five repeated allocation/reconnect/cleanup cycles: NOT RUN
- 30-minute match soak: NOT RUN
- orphan allocation/provider dashboard audit: NOT RUN

## Performance / Bandwidth

No Unity Profiler session, 1080p/60 frame-time capture, GC allocation capture, actual packet payload trace, or provider bandwidth/cost report was recorded. Existing 64KB message cap and bounded snapshot/shot DTO rules remain, but real M19 bytes-per-shot/turn are `NOT MEASURED`.

## Regression

- Full EditMode and PlayMode baselines: PASS
- Direct default/config validator: PASS
- M18 Direct real-process evidence remains at `Library/M18/direct/`
- M18 LocalRelay real-process evidence remains at `Library/M18/relay/`
- No Ball/Shot/Physics/Camera/Character/HUD/VFX/Audio service SDK dependency was added

## Evidence

- final real-provider logs: `Library/M19/production-relay-final-20260830-233017/`
- failed invalid-environment logs: `Library/M19/production-relay-20260830-232522/`
- discovered credential-registry failure logs: `Library/M19/production-relay-20260830-232615/`
- final EditMode XML: `Library/M19/editmode-results-final.xml`
- final PlayMode XML: `Library/M19/playmode-results-final.xml`
- final build log: `Library/M19/build-all-final.log`
- captures: `docs/review-captures/m19-wan-quality/`

`Library/` evidence is local and intentionally not a committed production artifact. Captures contain no credentials.


# M18 Relay / NAT Traversal Prototype Test Log

Date: 2026-08-30  
Environment: Windows 11, Unity 6000.5.7f1, Unity Transport 6.5.0

## Decision labels

- `M18 RELAY / NAT TRAVERSAL FOUNDATION: GO`
- `REAL RELAY TRANSPORT: VERIFIED` (standalone localhost proxy traffic)
- `CROSS-NAT CONNECTIVITY: NOT VERIFIED`
- `RELAY GAMEPLAY A/B SHOT: VERIFIED`
- `RELAY RECONNECT: VERIFIED`
- `PRODUCTION RELAY: NO-GO`
- `PRODUCTION ONLINE MULTIPLAYER: NO-GO`

## Automated validation

- M18 validator: PASS
- Full EditMode: 258/258 passed, 0 failed
- Full PlayMode: 63/63 passed, 0 failed
- Focused M18 PlayMode rerun: 5/5 passed, 0 failed
- Windows development builds: Lobby, Relay, Dedicated Server, Client all PASS

Tests cover descriptor validation/expiry, credential hashing and comparison, wrong credential/allocation, reusable reconnect credential, bounded policy, message directions, Direct default, allocation limit/release, private versus proxy endpoints, secret-redacted reservation round-trip, Relay-before-Auth ordering, failed Relay without join-ticket consumption, authentication and account-bound admission preservation, and Relay reconnect.

## Real multi-process Relay run

Processes: Lobby Service, standalone Relay proxy, Dedicated Match Server, Client A, and Client B.

- Lobby Create/List/Join/Ready/Owner Start: PASS
- Relay and Dedicated ready markers: PASS
- Client A/B Relay descriptors and reserved assignments: PASS
- Client A/B natural shots: PASS
- Client A connection loss and Relay reconnect: PASS
- Rotated reconnect generation: 2
- Final authoritative turn: 2
- Final A hash: `8156347662055417`
- Final B hash: `8156347662055417`
- Relay connections: 3 (A, B, reconnect A)
- Relay forwarded bytes observed: 55,991
- Parent Lobby termination released both Relay and Dedicated child processes: PASS

## Real Direct regression

- Client A/B direct admission and natural shots: PASS
- Final authoritative turn: 2
- Final A hash: `4145F7314FDAE9B3`
- Final B hash: `4145F7314FDAE9B3`
- Parent Lobby termination released the Dedicated child process: PASS

No secret/token value was written to logs or captures. The server reservation contained only ticket and connectivity credential hashes.

## Evidence

- Runtime logs and traffic telemetry: `Library/M18/relay/`
- EditMode XML: `Library/M18/editmode-results.xml`
- PlayMode XML: `Library/M18/playmode-results.xml`
- Captures: `docs/review-captures/m18-relay/`

The first run used a 10-second Relay startup timeout while three development players launched concurrently. The proxy did not reach its ready marker before the bound and allocation was correctly rejected. The data-backed timeout was adjusted to 30 seconds; subsequent runs passed. Failure evidence is retained in `Library/M18/relay-attempt1-timeout/`.

## Not verified

- Two physical networks or routers
- CGNAT/symmetric NAT
- Public WAN and region routing
- Production Unity Relay allocation/join flow
- TLS/end-to-end encryption
- Packet loss, jitter, bandwidth shaping, long soak, chaos, or mobile roaming
- Unity Profiler 1080p/60 capture
- Production orchestration and billing/cost behavior

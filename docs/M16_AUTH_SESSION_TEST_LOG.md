# M16 Authentication / Player Session Test Log

## Test Identity

- Date: 2026-08-29 (Asia/Seoul)
- Unity: 6000.5.7f1
- Unity Transport: 6.5.0
- Protocol: 3
- Scene: `Assets/_Game/Scenes/Hole01_SkyIsland.unity`
- Endpoint: `127.0.0.1:19817`
- Server build: `Builds/M16Server/SwingPopServer.exe`
- Client build: `Builds/M16Client/SwingPop.exe`

## Automated Results

- Compile: PASS, batchmode exit 0
- `SwingPop > Online > Validate M16 Authentication`: PASS
- Validator chain M12 -> M13 -> M14 -> M15 -> M16: PASS
- Full EditMode: 213/213 PASS (M15 baseline 198 + M16 15)
- Full PlayMode: 51/51 PASS (M15 baseline 45 + M16 6)
- Missing Script / required Scene graph: PASS through validator chain
- Foundation scene diff: none

M16 EditMode covers ID separation, JSON claims, valid MAC, tamper, expiry, unsupported token version, server-owned binding, duplicate account, disconnect/rebind, revocation, match ownership, same-account reconnect, stolen-ticket rejection, message directions, and the unauthenticated allowlist.

M16 PlayMode uses real UTP loopback drivers for distinct-account admission, invalid-signature rejection, unauthenticated admission rejection, duplicate-account rejection, same-account reconnect, and wrong-account reconnect-ticket rejection.

## Real Three-Process Authentication

The accepted run used one headless dedicated server plus two independent Windows client processes.

| Observation | Result |
|---|---|
| Client A authentication | Accepted, account fingerprint `AB4D39D6`, assigned `player-a` |
| Client B authentication | Accepted, account fingerprint `E2B282C4`, assigned `player-b` |
| Auth sessions | Distinct fingerprints; plaintext credentials absent from logs |
| Match start | Snapshot v1 received by A and B with identical hash |
| Natural gameplay | A natural shot advanced to turn 1; B natural shot advanced to turn 2 |
| Third unique account | Authentication accepted, match admission separately rejected `MatchFull` |
| Duplicate A account | Rejected `SessionConflict` before match assignment |
| Tampered credential | Rejected `InvalidSignature` before match assignment |

## Reconnect Ownership

- A was force-stopped after both natural shots settled.
- Server and B entered `ReconnectGrace`; the reserved `player-a` slot remained.
- Account C authenticated successfully, then presented A's valid ticket and was rejected `AccountOwnershipMismatch`.
- A new process authenticated as account A, presented the same ticket, restored `player-a`, and received generation 2.
- Restored snapshot: v9, Playing / PreparingShot, current `player-a`, hash `333AA247CF67933B`.
- Rotated ticket was written only to the local generated evidence directory.

## Token Tamper / Expiry / Revocation

- Tamper: real-process `InvalidSignature` plus EditMode coverage, VERIFIED.
- Expiry: signed expired claims rejected as `ExpiredCredential` in EditMode, VERIFIED.
- Revocation: server session revocation removes the connection binding and reauthentication returns `SessionRevoked` in EditMode, VERIFIED.
- Production distributed revocation, refresh, and persistent session storage were not tested and are not implemented.

## Performance and Payload

- Authentication/session registries are bounded to connected peers and observed sessions; the dedicated match capacity remains two.
- Auth request counters reset every second and do not retain request history.
- The generated development credential measured 391 UTF-8 bytes. JSON envelope overhead remains far below the existing 65,536-byte cap.
- Recorded Development build reports were approximately 176.9 MB per target. Final build logs under `Library/M16/` are authoritative.
- No WAN, packet-loss, soak, load, Profiler allocation, encryption, or penetration test was executed.

## Secret Audit

- Exact generated Client A credential matches under `Assets`, `docs`, `ProjectSettings`, and `Packages`: 0.
- Runtime signing key and credentials were generated under `%TEMP%/SwingPop/M16/...`.
- Logs and captures contain fingerprints and file paths only, never plaintext token/key/ticket values.

## Captures

`docs/review-captures/m16-authentication/`

- `A-Client-A-Authenticated.png`
- `B-Client-B-Authenticated.png`
- `C-Match-Started.png`
- `D-Auth-Failure.png`
- `E-Disconnect-Reauth.png`
- `F-Reconnect-Same-Player.png`
- `G-Match-Gameplay.png`

The captures are offscreen Camera renders because acceptance processes remain hidden. Authentication decisions are proven by the paired process logs under local `Library/M16/real/`, not by embedding secrets or debug text in images.


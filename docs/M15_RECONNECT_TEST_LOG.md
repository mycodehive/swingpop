# M15 Reconnect Test Log

## Test Identity

- Date: 2026-08-28 (Asia/Seoul)
- Unity: 6000.5.7f1
- Unity Transport: 6.5.0, `WebSocketNetworkInterface` (TCP-backed on Windows standalone)
- Protocol: 2, additive M15 messages
- Scene: `Assets/_Game/Scenes/Hole01_SkyIsland.unity`
- Server build: `Builds/M15Server/SwingPopServer.exe`
- Client build: `Builds/M15Client/SwingPop.exe`
- Final replacement-process endpoint: `127.0.0.1:19787`
- Match: `server-hole01-20260827212632244`

## Real Replacement-Process Result

| Field | Observed |
|---|---|
| Disconnected player | `player-a` |
| Disconnect point | After A natural Rigidbody shot settled; turn 1, `player-b` preparing |
| Grace policy | 30 seconds from the development settings asset |
| Process action | Original PID 10964 was force-stopped; new PID 14732 launched from the same client build |
| Old generation | 1 |
| New generation | 2 |
| Player after reconnect | `player-a` (unchanged) |
| Snapshot before disconnect | v4, `A4E87328240A7E7A` |
| Grace snapshot | v5, `6C2C470F04D9B287` |
| Restored snapshot | v6, `6134CEDD51E8A274` |
| Restored state | Playing / PreparingShot, current `player-b`; ball/lie/score fields preserved by full snapshot |
| Resume result | B submitted a natural shot; server advanced through v8 ShotPlaying to v9 `37AE82785EB7D892`, current `player-a` |
| Desync / duplicate launch | No duplicate approval or second A launch observed |
| Secret exposure | No plaintext ticket found in process logs; only an 8-hex hash fingerprint is logged server-side |
| Errors | 0 exception, NullReference, protocol-direction, or send failure markers in accepted run |

## Grace Expiry Result

- Endpoint: `127.0.0.1:19786`
- Development grace: 3 seconds
- Match state: v4 Playing -> v5 ReconnectGrace -> v6 Aborted / TurnComplete -> lifecycle Ended
- Expired player: `player-a`
- Final hash on server and B: `A5F8896AFF6F23FC`
- Remaining client received the final snapshot and did not deadlock on the disconnected turn.

## Automated Security / State Cases

- Valid ticket / same PlayerId / generation rotation: PASS
- Wrong secret, wrong player, wrong match: PASS
- Expired ticket and expired slot: PASS
- Old token replay after rotation: PASS
- Active duplicate connection: PASS
- Match-ended ticket invalidation: PASS
- Reconnect message direction spoofing: PASS
- Disconnect during approved shot, authoritative settle, restored next turn: PASS
- Remaining-client shot while suspended: `MatchSuspended`, PASS

## Test Counts

- M15 focused EditMode: 15/15 PASS
- M15 focused PlayMode: 8/8 PASS
- Full EditMode: 198/198 PASS (M14 baseline 183 + M15 15)
- Full PlayMode: 45/45 PASS (M14 baseline 37 + M15 8)
- M12 -> M13 -> M14 -> M15 validator chain: PASS
- Final graphics structural gate: PASS, required validators 7/7, Missing Scripts 0

## Performance / Payload

- Graphics structural scan: 20 `Update` behaviours; M15 adds one bounded two-session deadline scan and one thin client reconnect coordinator.
- Session registry capacity: two player sessions; pending unauthenticated peers: two; client retry limit: three.
- Compact representative JSON payloads with a 44-character 256-bit Base64 secret: ticket 236 B, ticket-issued 247 B, reconnect-request 214 B, reconnect-accepted 401 B, lifecycle-change 161 B. UTP envelope overhead is additional and all remain far below the 65,536 B cap.
- Existing validator snapshot sample: approximately 830 B. Final accepted run ended at server TX/RX counters without unbounded history or peer growth.
- A profiler allocation capture was not executed. Reconnect is a rare transition that allocates the driver restart, JSON payloads, and rotated ticket once per bounded attempt; this still requires WAN/soak profiling before production.

## Build Result

- M15 Server final build report: 176,877,189 bytes
- M15 Client final build report: 176,877,178 bytes
- Final server-build acceptance logs: `Library/M15/final-server-build-acceptance/` (local generated evidence, not committed)

## Captures

`docs/review-captures/m15-reconnect/`

- `A-Match-Playing.png`
- `B-Player-Disconnected.png`
- `C-Waiting-For-Reconnect.png`
- `D-Reconnect-Accepted.png`
- `E-State-Restored.png`
- `F-Match-Resumed.png`
- `G-Grace-Expired-Aborted.png`

The development ticket handoff files were written under the OS temporary directory and are not part of the repository.

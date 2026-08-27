# M14 Dedicated Server Test Log

## Test Identity

- Date: 2026-08-27 (Asia/Seoul)
- Unity: 6000.5.7f1
- Protocol: 2
- Scene: `Assets/_Game/Scenes/Hole01_SkyIsland.unity`
- Server executable: `Builds/M14Server/SwingPopServer.exe` (Windows Development fallback, 176,843,868 bytes final build report)
- Client executable: `Builds/M14Client/SwingPop.exe` (Windows Development, 176,843,869 bytes final build report)
- Three-process endpoint: `127.0.0.1:18883`
- Server mode: `DedicatedServer`, `-batchmode -nographics`

## Three-Process Result

| Check | Result |
|---|---|
| Server listening | PASS |
| First connection assignment | `player-a` |
| Second connection assignment | `player-b` |
| Same initial match | PASS, version 1 |
| A natural shot | Approved sequence 1; resolved to turn 1 |
| B natural shot | Approved sequence 2; resolved to turn 2 |
| Completion automation | Only after both natural shots; one forced cup completion per player |
| MatchComplete | PASS, snapshot version 13 |
| Server/A/B final hash | `5132ECAFAE0F1155` |
| Desync count | server 0, A 0, B 0 |
| Errors/exceptions | 0 in the accepted three-process run |
| Process exit | server 0, A 0, B 0 |

The automation performs at least one real Rigidbody shot for A and B. It then uses the existing server HoleFlow cup-completion entry point to keep the full-hole duration bounded. A completely natural multi-shot hole remains a manual quality test.

## Snapshot Trace

| Version | Turn | Current | Phase / Turn State | Hash |
|---:|---:|---|---|---|
| 1 | 0 | player-a | Playing / PreparingShot | `4983DE9A0B2EDB38` |
| 3 | 0 | player-a | Playing / ShotPlaying | `E86E72471B1A009C` |
| 4 | 1 | player-b | Playing / PreparingShot | `E6EA83CE413B0B1C` |
| 6 | 1 | player-b | Playing / ShotPlaying | `B089FBA13F026386` |
| 7 | 2 | player-a | Playing / PreparingShot | `C47BD9C20D402CB6` |
| 13 | 3 | player-b | HoleComplete / TurnComplete | `5132ECAFAE0F1155` |
| 14 | 3 | player-b | Aborted after B disconnect | `91CB087A2F1FDCDE` |

## Transport Telemetry

| Process | TX | RX | Messages | Final observed hash | Desync |
|---|---:|---:|---:|---|---:|
| Server | 41,606 B | 15,918 B | 204 | `C4C7F942E35B9978` after both cleanup snapshots | 0 |
| Client A | 8,700 B | 22,173 B | 114 | `91CB087A2F1FDCDE` | 0 |
| Client B | 6,980 B | 19,433 B | 88 | `5132ECAFAE0F1155` | 0 |

- Configured maximum network envelope: 65,536 bytes.
- Validator serialized snapshot sample: approximately 830 bytes.
- This telemetry includes ping/pong and full snapshots; it is not a production bandwidth benchmark.

## Headless Result

- `Application.isBatchMode`: true
- no-graphics command detected: true
- Active cameras: 0
- Active canvases: 0
- Active audio sources: 0
- Gameplay colliders retained: 9
- Renderers disabled: 356
- Server Character Animator dependency: none

## Disconnect / Capacity Result

- Server killed immediately after A shot approval: both clients reported `Remote peer disconnected`, desync 0, and exited with code 0.
- Third real client test at `127.0.0.1:18887`: one `MatchFull` disconnect callback, no assignment, no native queue error, no Console Error, exit code 0.
- Client B close after MatchComplete: server published `Aborted` version 14 to remaining Client A and later cleaned Client A.

## Automated Regression Result

- M14 focused EditMode: 13/13 PASS (included in final full EditMode run).
- M14 focused PlayMode: 12/12 PASS.
- Final full EditMode: 183/183 PASS.
- Final full PlayMode: 37/37 PASS.
- M12 -> M13 -> M14 structural validation: PASS; protocol 2, Missing Scripts 0, snapshot sample 830 bytes.
- Final graphics structural gate: PASS; required validators 7/7 and resolution captures 12/12.

## Final Rebuilt-Binary Acceptance

The post-fix server/client binaries were run again at `127.0.0.1:18888`. Server, A, and B exited with code 0; both natural shots resolved; MatchComplete version 13 converged to `27D1B5FCA8B4736C` on all three processes; desync remained 0; and the Unity logs contained no `Exception`, `NullReference`, native event-queue reset, or Error marker.

## Captures

`docs/review-captures/m14-dedicated-authority/`

- `A-Client-A-Assigned.png`
- `B-Client-B-Assigned.png`
- `C-A-Turn.png`
- `D-B-Turn.png`
- `E-Same-Match-Complete.png`
- `E-Client-B-Match-Complete.png`
- `F-Disconnect-State.png`

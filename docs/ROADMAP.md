# SwingPop Roadmap

## Phase A — Vertical Slice

### M0 Project Foundation — Complete (2026-08-10)
Unity 6000.5.7f1 project, URP 17.5.0, Input System 1.20.0, `_Game` folders, Foundation scene/prefab, PC 1080p defaults, Unity `.gitignore`.

### M1 Ball Launch — Complete (2026-08-10)
Rigidbody launch, bounce, roll, stable stop, reset, telemetry, and minimal camera follow are scene-connected and treated as the completed M2 baseline.

### M2 Aim / Power / Impact — Implementation Complete (2026-08-10)
Yaw aim, three-step Space flow, power/impact gauges, grade-based power loss and dispersion, serializable `ShotCommand`, debug telemetry, and M1 physics integration are scene-connected. Automated tests pass; manual keyboard feel validation remains.

### M3 Ball Flight — Implementation Complete (2026-08-10)
Tuneable arcade drag/lift/side curve, Top/Back/Side spin, spin-aware landing/roll, decay, stable stop, actual trajectory trace, tests and automated Rigidbody comparison are complete. Manual input/visual feel validation remains.

### M4 Wind / Terrain — Implementation Complete (2026-08-11)
Single-source world-space wind, airborne carry/drift, data-driven Tee/Fairway/Rough/Bunker/Green response, Water/OOB recovery, debug presets/vector/telemetry, Foundation test course, EditMode tests and automated Rigidbody comparisons are complete. Manual keyboard and visual feel validation remains.

### M5 Hole / Scoring — Implementation Complete (2026-08-15)
HoleData/Par 4, continuous next-shot flow, stroke/penalty count, last-valid Water/OOB recovery, data-driven Driver/Putter, Green putting, tunable cup capture, HoleComplete/result calculation, expanded debug telemetry, EditMode tests and automated PlayMode/regression validation are complete. Manual keyboard and visual feel validation remains.

### M6 Camera Director — Implementation Complete (2026-08-15)
Address/Aim/Swing/Impact/BallFollow/Landing/NextShot/Putt/HoleComplete/Result modes, data-driven framing/blending/collision, automated validation and M1–M5 regression are complete. Manual visual comfort validation remains.

### M7 Character — Implementation Complete (2026-08-16)
Primitive replaceable golfer prefab, Ball-relative Address/reposition, Aim rotation, procedural Driver/Putter flow, Impact-delayed single Ball launch with fallback, FollowThrough/WatchBall, club visual and celebration hooks are scene-connected. EditMode and M1–M7 PlayMode regression validations pass; manual motion/feel review remains.

### M8 HUD
Full gameplay HUD.

### M9 VFX / Audio
Impact, trail, landing, basic sound.

### M10 Hole 1 Vertical Slice
Sky Island Hole 1 full flow.

### M11 Polish / Quality Gate
Reference comparison, UX polish, performance, bugs.

## Phase B — After Vertical Slice Approval

- 3 Holes
- More clubs
- Improved spin
- Better environment
- Local alternating turns

## Phase C — Multiplayer

Vertical Slice와 local turns가 안정된 후 별도 PRD 작성.

- Sessions
- Lobby
- 1v1
- Authoritative shot result
- Reconnect/error handling

## Phase D — Live Content

멀티플레이 검증 이후에만 논의.

- characters
- cosmetics
- economy
- progression
- ranking
- seasons

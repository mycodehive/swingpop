# SwingPop Gameplay Specification v0.1

## 1. Primary Interaction

핵심 입력:

1. Aim
2. Select club
3. Select power
4. Select impact
5. Observe result
6. Adjust next shot

## 2. Shot State

권장 흐름:

```text
Preparing
→ Aiming
→ PowerSelecting
→ ImpactSelecting
→ Swinging
→ BallFlying
→ BallBouncing
→ BallRolling
→ BallStopped
→ Preparing
```

Hole In:

```text
BallRolling / BallStopped
→ HoleComplete
```

### M1 Ball Launch State

M1에서는 이후 shot flow를 구현하지 않고 ball simulation 상태만 다음처럼 명시적으로 관리한다.

```text
Ready
→ Airborne
→ Bouncing
→ Rolling
→ Stopped
→ Reset
→ Ready
```

- Space / gamepad south button: 고정된 launch velocity로 발사
- R / gamepad north button: 초기 위치와 `Ready` 상태로 reset
- launch speed와 angle은 `M1BallTuning.asset`에서 조정한다.
- bounce/friction은 `GolfBallPhysics.asset`, Rigidbody/roll/stop 값은 `M1BallTuning.asset`에서 조정한다.
- Aim, Power, Impact, Wind, Spin은 M1에 포함하지 않는다.

## 3. Power

Normalized 0–1 또는 percentage 0–100.

Power가 실제 velocity에 바로 단순 곱만 되는 구조보다 클럽별 tuning curve를 허용하는 구조가 좋다.

초기에는 단순 구현 후 필요 시 AnimationCurve 적용.

## 4. Impact

입력 위치에 따른 grade:

- Perfect
- Great
- Good
- Miss

Impact는 다음에 영향을 줄 수 있다.

- horizontal dispersion
- power loss
- spin consistency
- VFX level

초기 버전에서는 Perfect가 명확히 유리하되 Miss가 지나치게 처벌적이지 않게 한다.

## 5. Aim

Aim yaw를 중심으로 구현한다.

Pitch/loft는 클럽 데이터에서 시작한다.

Debug trajectory는 실제 current tuning을 사용해야 한다.

## 6. Spin

v0.1에서 UI가 복잡해지면 spin은 debug/키 입력으로 먼저 구현 가능하다.

- TopSpin
- BackSpin
- SideSpin

범위는 normalized value로 유지하는 것을 권장한다.

## 7. Wind

Wind는 World-space direction + scalar strength.

UI arrow와 Ball calculation이 같은 source of truth를 사용한다.

## 8. Lie

Ball이 멈춘 surface에 따라 다음 Shot의 modifier 결정.

- Fairway: neutral
- Rough: slight power/accuracy penalty
- Bunker: stronger penalty
- Green: putting
- Water: penalty handling
- OOB: penalty handling

구체 밸런스 수치는 플레이 테스트로 결정.

## 9. Putt

Putter 사용 시 비행을 최소화하고 rolling 중심으로 동작한다.

Green slope 지원은 v0.1 후반 또는 Polish에서 단계적으로 적용 가능.

## 10. Scoring

기본 Stroke Play.

- Stroke count
- Par
- Relative score display

Birdie/Eagle 등 텍스트는 Hole 완료 시 계산한다.

## 11. Perfect Shot Feedback

조건:
Impact grade = Perfect

반응:

- stronger hit sound
- short hit stop
- stronger ball trail
- small flash
- UI “PERFECT”
- reduced dispersion

## 12. Failure / Recovery

Water/OOB는 공이 무한히 떨어지거나 게임이 멈추지 않도록 명시적 recovery flow를 둔다.

개발 초기에는 단순 drop/reset도 허용한다.

## 13. Feel Tuning

우선순위:

1. Impact timing feel
2. Ball launch speed
3. Arc readability
4. Landing response
5. Roll duration
6. Camera
7. VFX

실제 물리 정확성보다 체감 튜닝을 우선한다.

## 14. M2 Playable Shot Flow

```text
Aiming
  → Space: PowerSelecting
  → Space: ImpactSelecting
  → Space: ShotCommitted
  → Ball Airborne → Bouncing → Rolling → Stopped
  → R: Aiming + Ball Ready
```

- A/D, 좌우 방향키: yaw aim. 기본 범위는 -30도에서 +30도다.
- Space: 현재 단계 확정.
- Escape: Power 또는 Impact 선택을 취소하고 Aiming으로 돌아간다.
- P: ImpactSelecting에서 검증용 PERFECT를 즉시 확정한다.
- R: 언제든 공을 시작 위치로 reset하고 Aiming으로 돌아간다.
- power는 0~1 왕복 gauge이며 실제 launch speed scale로 전달된다.
- impact cursor의 중앙 오차 절댓값으로 Perfect/Great/Good/Miss를 결정한다.
- 낮은 grade는 power multiplier와 deterministic horizontal dispersion을 적용한다.
- 현재 M2에는 wind, spin, surface modifier, club/putter, hole 판정이 없다.

기본 데이터는 `M2ShotTuning.asset`에 있으며 Debug overlay는 최종 HUD가 아니라 상태 확인 도구다.

## 15. M3 Arcade Spin / Flight

Spin preset:

- `1`: No Spin
- `2`: Top Spin — landing boost와 낮은 rolling 감속으로 rollout 증가
- `3`: Back Spin — landing brake, 높은 rolling 감속, 짧은 rollback
- `4`: Left Side Spin — 현재 이동 방향 기준 왼쪽으로 공중 curve
- `5`: Right Side Spin — 현재 이동 방향 기준 오른쪽으로 공중 curve

Aim은 초기 launch direction이고 SideSpin은 비행 중 방향을 계속 휘게 하므로 서로 다른 값이다.

공중에서는 gravity, launch velocity, tunable drag, 작은 vertical-spin lift/downforce, velocity-relative side curve가 적용된다. Spin은 공중과 지면에서 서로 다른 속도로 감소한다. 첫 bounce 이후 TopSpin은 전진을 보강하고 BackSpin은 rollout을 줄인다. Stop 시 spin과 velocity를 zero로 만들어 미세 움직임이 지속되지 않게 한다.

최근 실제 trajectory는 Game View의 line으로 남으며 Reset 또는 다음 launch 때 초기화된다. M3에는 predicted trajectory, Wind force, terrain modifier가 없다.

## 16. M4 Wind / Terrain

Wind debug preset:

- `6`: Calm
- `7`: Tailwind
- `8`: Headwind
- `9`: Left Crosswind
- `0`: Right Crosswind

Wind는 world-space direction과 m/s strength로 표시되며 공중에서만 ball acceleration에 반영된다. Tailwind는 carry를 늘리고 Headwind는 줄이며 Crosswind는 해당 world direction으로 drift시킨다.

Surface lie:

- Tee/Fairway: 기준에 가까운 response
- Rough: bounce와 spin이 줄고 rolling resistance가 증가하며 power modifier는 0.90
- Bunker: 매우 낮은 bounce/spin, 강한 rolling resistance, power modifier는 0.65
- Green: 낮은 bounce와 예측 가능한 긴 rolling 기반. Putter와 slope는 아직 없다.
- Water/OutOfBounds: shot을 즉시 안전하게 Stopped로 만들고 hazard를 기록한다. R reset으로 회복한다.

M4 단계의 Debug overlay는 current wind, current surface/lie, surface modifier, last hazard를 표시했다. 당시 Water/OOB penalty, hole/scoring, putter는 제외했으며 현재 구현은 아래 M5 규칙으로 확장됐다.

## 17. M5 Hole / Scoring / Continuous Shot Flow

정상 Hole 1 흐름:

```text
Tee → Aim → Shot → Stop → Lie 확정 → 같은 위치에서 Aim
    → 반복 → Green → 자동 Putter → Putt → Hole In → Result
```

- Shot이 실제 commit될 때만 Stroke가 1 증가한다. Aim/Power/Impact 취소는 stroke가 아니다.
- 정상 surface에서 공이 멈추면 위치와 lie를 Last Valid Position으로 기록하고 `R` 없이 다음 shot을 준비한다.
- 다음 shot 기본 조준 방향은 Cup 방향이며 A/D 또는 좌우키로 다시 조정할 수 있다.
- Fairway/Rough/Bunker의 power modifier가 다음 shot carry에 적용된다.
- Green에서 Current Club은 Putter로 자동 전환된다. Putter는 spin을 제거하고 airborne arc 없이 rolling을 시작한다.
- Cup은 Green, capture radius, 낮은 horizontal speed, height tolerance가 모두 맞아야 Hole In이다. 외곽 assist는 가까운 저속 공에만 작게 적용된다.
- Hole In 시 Ball은 `Holed`, Hole은 `HoleComplete`가 되고 입력은 더 이상 shot을 commit하지 않는다.
- Par 4 대비 total stroke로 Albatross/Eagle/Birdie/Par/Bogey/Double Bogey/+N 결과를 계산한다.
- Water/OOB shot은 committed shot 1회에 +1 penalty stroke를 더하고 Last Valid Position/Lie로 자동 복구한다.
- `R`은 정상 shot 진행용이 아니라 Hole 1 전체를 다시 시작하는 debug 기능이다.

## 18. M6 Camera Flow

정상 shot presentation:

```text
HoleIntro → Address → Aim → Swing → Impact → BallFollow
          → Landing(Bounce/Roll) → NextShot → Address/Aim
```

Green/Hole In presentation:

```text
Putt Address → Impact → Putt Follow → HoleComplete → Result
```

- Camera mode는 gameplay 상태를 변경하지 않는 presentation 상태다.
- HoleIntro는 기본 2.8초 동안 Tee에서 Cup 방향의 코스 흐름을 보여 준다.
- Address/Aim은 공과 목표 방향을 동시에 읽게 하며 aim yaw 반응을 제한한다.
- Impact는 FOV kick과 Perfect/Normal 강도 차이가 있는 짧은 shake hook을 사용한다.
- BallFollow는 속도 방향, look-ahead, 속도 기반 거리/FOV를 사용하고 높은 arc에서는 시야를 넓힌다.
- Landing은 첫 충돌부터 Bounce/Roll을 이어 보며, Stop 후 NextShot blend를 거쳐 새 lie의 Address로 돌아간다.
- Putt는 Ball-Cup 중간점과 남은 거리를 기준으로 카메라 거리·높이·FOV를 조절해 공과 Cup을 함께 구성하며, Hole In 이후 Cup/Result를 보여 준다.
- 모든 feel 값은 `M6CameraTuning.asset`에서 조정한다. M6에는 Character, HUD, VFX/Audio 완성 연출이 포함되지 않는다.

## 19. M7 Character / Animation Flow

정상 Driver shot:

```text
Aiming/PowerSelecting → Address
ImpactSelecting → BackSwing
ShotCommitted → Swing
Animation Impact marker → Ball Launch + Camera Impact
→ FollowThrough → WatchBall
Ball Stopped/NextShot → 새 Ball 위치 Address
```

Green Putter:

```text
PuttAddress → PuttBackSwing → PuttSwing
→ Impact marker → Ball Rolling
→ PuttFollowThrough → WatchBall → Celebration
```

- Shot commit은 command/stroke 확정 시점이고 Ball launch는 animation Impact 시점이다.
- Impact signal은 한 swing에서 한 번만 launch할 수 있으며 누락 시 0.65초 fallback이 soft lock을 방지한다.
- Character는 현재 Ball 위치와 Aim direction을 기준으로 배치·회전하며 Fairway/Rough/Bunker/Green의 다음 shot을 따라간다.
- Driver와 Putter는 서로 다른 placeholder visual과 motion 크기를 사용한다.
- Hole result는 Happy/Sad/Birdie/Eagle/HoleInOne celebration hook으로 매핑된다.
- 현재 procedural motion은 architecture 검증용이며 최종 humanoid rig/clip/IK가 아니다.

## 20. M8 Gameplay HUD Flow

상태별 주요 HUD:

```text
Aiming          → Hole/Stroke/Wind/Aim/Distance/Height/Club/Lie/Spin + START SHOT
PowerSelecting  → Power gauge + SET POWER 강조
ImpactSelecting → gameplay threshold 기반 Impact zones + IMPACT 강조
ShotCommitted   → timing/action 숨김 + Impact grade popup
Ball flight     → 상단 gameplay context 유지, timing interaction 비활성
Next shot       → 새 Distance/Lie/Club/Spin 상태 갱신
HoleComplete    → timing/action 숨김 + ScoreCalculator 기반 Result panel
```

- 좌상단은 PLAYER placeholder, Stroke, penalty를 표시한다.
- 중앙 상단은 Hole/Par와 live Stroke를 표시하고 완료 전 결과 명칭을 미리 계산하지 않는다.
- 우상단 Wind arrow와 m/s는 `WindController.Direction/Strength`를 사용한다.
- Aim marker는 현재 Ball과 `ShotFlowController.AimDirection`을 world-to-screen projection하며 Remaining Distance/Height Difference는 `HoleFlowController` 값을 표시한다.
- 하단 Club/Lie는 현재 `ClubData`와 Ball lie를 표시한다. Driver에서 숫자 1~5 Spin preset을 표시하고 Putter에서는 `SPIN DISABLED`로 바뀐다.
- uGUI Primary button click과 Keyboard Space는 같은 ShotFlow command를 사용한다.
- Impact 확정 시 Perfect/Great/Good/Miss popup, Water/OOB 시 +1 penalty popup, 새 정상 lie에서는 작은 lie feedback을 표시한다.
- Result panel은 `ScoreCalculator`가 만든 `ScoreResult`만 표시한다.
- 기존 IMGUI `ShotDebugOverlay`는 개발용으로 유지하며 정식 HUD와 독립적이다.

## 21. M9 Shot Feel Flow

정상 Driver shot presentation:

```text
Commit → Character Swing/Whoosh → Impact marker → Ball Launch
       → Normal Impact VFX/Audio + Camera Impact + HUD grade
       → speed-gated Ball Trail → first Landing puff/audio
       → optional reduced second Bounce → Rolling(no continuous particles) → Next Shot
```

Perfect는 같은 gameplay/physics 흐름을 사용하고 `ImpactGrade.Perfect` 하나에서 brighter/larger impact, directional streak, stronger trail, layered accent audio, 기존 Perfect camera/HUD 강도를 선택한다.

- Ball trail은 Airborne/Bouncing이며 최소 속도 이상일 때만 emit한다. Ready/Stopped/Holed/Hazard/Reset에서는 즉시 끄고 clear한다.
- Top/Back/Side Spin은 최종 궤적 계산을 바꾸지 않고 기존 spin physics를 그대로 사용한다. presentation은 accent trail 색으로만 미세하게 구분한다.
- 첫 surface contact는 impact speed threshold를 통과하면 100% 표시한다. sequence 2는 더 높은 threshold와 축소 intensity를 사용하고 이후 bounce particle은 생략한다.
- Fairway/Green은 grass, Rough는 짙은 grass, Bunker는 sand, Water는 splash profile을 사용한다. surface 판정은 기존 `TerrainSurfaceData`가 source of truth다.
- Hazard presentation은 기존 penalty/recovery를 계산하지 않는다. Ball/Hole flow가 계산한 Water/OOB event를 VFX/Audio/HUD가 각각 표시한다.
- Hole-In은 기존 CupCapture/HoleFlow 완료 이후 cup sparkle/ring, success/result audio, Camera Result, HUD Result, Character celebration이 같은 완료 event에 반응한다.
- 지속 roll particle/audio loop는 이번 단계에서 생략했다. 화면 노이즈와 source stop 문제를 피하고 impact/landing/hole의 우선순위를 유지한다.

## 22. M10 Hole01 Vertical Slice Flow

```text
Hole01_SkyIsland load → 3s HoleIntro → Address
→ A/D Aim → Space Power → Space Impact → Space Commit
→ Character Swing/Impact → Ball Flight → Surface Landing/Bounce/Roll
→ Next Lie/Address → Green/Putter → Putt → Hole-In → Celebration/Result
```

- 기본 presentation은 debug overlay와 trajectory가 숨겨진 상태다. H/F1은 개발 telemetry와 trajectory를 함께 toggle한다.
- Tee/Fairway/Rough/Bunker/Green/Water/OOB 판정과 모든 score/penalty rule은 M4/M5 데이터를 그대로 사용한다.
- M10 environment art는 physics에 참여하지 않으므로 Ball collision, aim line, Cup capture, Hazard recovery를 방해하지 않는다.
- Build Settings 첫 scene은 `Hole01_SkyIsland`, 두 번째 scene은 회귀용 `Foundation`이다.

## 23. M11 Readability Contract

- Address에서 Character, Ball, Aim guide, playable corridor, Flag와 최소 2개 landmark가 동시에 읽혀야 한다.
- M11 course visual은 collider가 없으며 shot, terrain response, hazard, cup capture와 score rule을 바꾸지 않는다.
- Driver/Putter player-facing label은 club type을 사용하고 Power/Impact/Spin/Wind 입력 흐름은 M8–M10과 동일하다.
- debug overlay/trajectory는 기본 숨김이고 개발자가 H/F1로만 켠다.
- 16:9 layout은 corner HUD와 lower-right Primary Action을 safe area 안에 유지한다.
- full-hole 수동 검증은 Address→Normal/Perfect→Flight→Landing/Hazard→Green/Putter→Hole-In→Result 순서로 수행한다.

## M12 Local Turn-Based Foundation

```text
Player A Ready
-> submit ShotCommand
-> authority approval
-> existing shot/physics/presentation
-> result snapshot
-> Player B state restore
-> simulated remote approved shot
-> result snapshot
-> Player A state restore
```

- `OfflineSingle`에서는 기존 입력과 즉시 commit 동작이 바뀌지 않는다.
- `LocalTwoPlayer`에서는 현재 local player가 아니거나 approval을 기다리는 동안 shot 입력과 action button을 비활성화한다.
- 승인 전에는 공이 발사되지 않는다. 승인된 local/remote command 모두 동일한 Character impact -> Ball launch 경로를 통과한다.
- Bounce, roll, stop, hazard, lie, stroke, penalty, hole-in은 기존 gameplay가 계산한다.
- 턴이 바뀌면 해당 player의 ball position, last valid position, lie, strokes, penalties를 복원한다. Green이면 Putter, 그 외 lie면 Driver를 선택한다.
- Holed player는 이후 turn에서 제외되며 모든 player가 holed이면 match snapshot이 `HoleComplete`가 된다.
- F2는 M12 match/turn/player/transport telemetry를 표시한다. 기존 H/F1 debug와 분리되어 있다.
- 개발 메뉴 `SwingPop > Online > Run Local 2P Simulation`은 one-way 200 ms LocalLoopback 시뮬레이션을 시작한다.
- 이는 같은 프로세스의 foundation 검증이며 실제 두 기기 또는 production online match가 아니다.

## 24. VFX Hero Pass Presentation Contract

- shot gameplay flow와 `ImpactGrade` 판정은 변경하지 않는다. Good/Miss 계열은 restrained Normal presentation, Great는 중간 profile, Perfect는 Hero profile을 사용한다.
- Character impact marker 이후 실제 `Ball.Launched`에서 Impact VFX, HUD grade, Camera kick, Audio가 함께 시작된다.
- Driver의 launch 방향은 확정된 `ShotCommand.FinalDirection`과 Ball velocity를 사용한다. VFX가 Aim 또는 shot calculation을 다시 수행하지 않는다.
- Ball trail은 Airborne/Bouncing의 속도 조건에서만 보이며 Ready/Stopped/Holed/Hazard/Reset에서 clear된다. Putter는 ground trail을 사용하지 않는다.
- 첫 landing과 제한된 secondary bounce만 표시한다. Fairway/Rough/Bunker/Water profile 선택은 기존 terrain event를 그대로 따른다.
- Hole-In presentation은 기존 `HoleCompleted` event 이후 한 번만 실행되며 score, cup capture, result flow를 소유하지 않는다.
- Normal/Great/Perfect, landing surface, Putter, Hole-In preview는 Editor menu 전용이며 gameplay input을 추가하지 않는다.

## 25. M13 Network Gameplay Contract

- OfflineSingle remains immediate and unchanged; LocalTwoPlayer continues to use LocalLoopback.
- NetworkHost is Player A and NetworkClient receives Player B from the host. A client-supplied player identity is not trusted.
- A local network shot stays in `AwaitingApproval` until `ApprovedShot`; the approved command enters existing ShotFlow once on both processes.
- The host simulates and resolves every approved shot through existing Ball/HoleFlow. Client prediction cannot change match state.
- Host snapshots restore ball position, last valid position, lie, strokes, penalties, holed state, club selection, current turn, and sequence.
- Network-mode debug reset is blocked. F2 telemetry is presentation-only.

## 26. M14 Dedicated Gameplay Contract

- The dedicated server is not Player A or Player B and cannot submit a player shot.
- The first remote connection is `player-a`; the second is `player-b`; a third is rejected as `MatchFull`.
- Only the current player can submit. The server validates identity/turn/sequence, broadcasts approval, and launches the existing server Rigidbody shot without waiting for Character Animator.
- Client ball motion is presentation prediction. Server Ball/HoleFlow owns settle position, lie, hazard penalty/recovery, strokes, putter state, hole-in, next turn, and MatchComplete.
- A disconnected player marks the match `Aborted`; reconnect/forfeit UX is not part of M14.
- OfflineSingle remains the default and M13 Host/Client behaviour remains available.

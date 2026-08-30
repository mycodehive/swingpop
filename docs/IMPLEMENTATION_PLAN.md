# SwingPop Implementation Plan

## Current State

2026-08-10 M0 Repository Audit 기준:

- Repository root: `C:/Users/Dodari/Documents/GitHub/swingpop`
- Git: `main` 브랜치의 신규 Repository이며 아직 commit이 없다. 감사 시점에는 전체 프로젝트가 untracked였고 `.gitignore`가 없었다.
- Unity project: `Assets/`, `Packages/`, `ProjectSettings/`가 있는 정상 Unity 프로젝트다.
- Unity Editor: `6000.5.7f1` (`ProjectSettings/ProjectVersion.txt` 기준)
- Render pipeline: Universal Render Pipeline `17.5.0`; `GraphicsSettings.asset`의 active pipeline은 `Assets/Settings/PC_RPAsset.asset`을 참조한다.
- Input: Input System `1.20.0`; Player Settings의 Active Input Handling은 새 Input System이며 템플릿 `InputSystem_Actions.inputactions`가 존재한다.
- Cinemachine: 설치되어 있지 않다. M6 감사에서 Unity 6용 정식 3.1.5를 확인했으나, 현재 단일 Main Camera의 명시적 모드 전환에는 새 패키지가 필요하지 않아 custom `CameraDirector`를 선택했다.
- Other direct packages: AI Navigation `2.0.14`, Test Framework `1.7.0`, Timeline `1.8.12`, uGUI `2.5.0` 등. 정확한 전체 목록은 `Packages/manifest.json`과 lock file이 source of truth다.
- Existing scene before M0: `Assets/Scenes/SampleScene.unity` 한 개. URP 템플릿 카메라, Directional Light, Global Volume만 있고 ground/gameplay object는 없었다.
- Existing scripts before M0: Unity 템플릿 Readme/Editor 스크립트만 존재했다.
- Existing prefabs/tests before M0: 없음.
- Existing art: Unity URP 템플릿 설정 및 tutorial icon 외 게임용 art 없음. 품질 참고 이미지는 `docs/reference/target-quality.png`에 존재한다.
- Compile/configuration risk: 감사 시점에 사용자 gameplay assembly가 사실상 비어 있어 코드 컴파일 위험은 낮았으나, `.gitignore` 부재로 `Library/`, `Temp/`, IDE 산출물이 commit될 위험이 컸다. Asset Import Worker log의 `-noUpm`/licensing 메시지는 worker 프로세스 문맥이며 main Editor Console 검증을 대체하지 않는다.

## M0 Project Foundation Plan

1. 기존 Unity 6.5/URP/Input System 설정을 유지하고 실제 참조를 검증한다.
2. `Assets/_Game` 아래에 gameplay, presentation, data, debug, tests 교체 지점을 마련한다.
3. `Foundation` scene에 ground, lighting, camera와 별도 prefab 기반 Input System probe를 연결한다.
4. PC 초기 기준을 1920x1080으로 설정하고 최소 build scene을 등록한다.
5. Unity 생성물과 IDE 파일을 제외하는 `.gitignore`를 추가한다.
6. Unity compile, missing reference, PlayMode input 반응을 검증하고 결과를 기록한다.

M0에서는 golf physics, shot flow, full HUD, character, Cinemachine을 구현하지 않는다.

## Target

1개 Hole에서:

Aim → Power → Impact → Swing → Flight → Landing → Roll → Stop → Next Shot → Putt → Hole In → Result

전체 흐름이 동작하는 Vertical Slice.

## Milestones

| ID | Milestone | Exit Criteria |
|---|---|---|
| M0 | Project Foundation | Unity가 오류 없이 열리고 base scene 실행 |
| M1 | Ball Launch | 공 발사→지면→정지, camera follow |
| M2 | Aim/Power/Impact | 사용자가 샷 입력 완료 가능 |
| M3 | Ball Flight | arcade flight/bounce/roll 안정 |
| M4 | Wind/Terrain | 바람과 surface 차이 체감 |
| M5 | Hole/Scoring | Hole In 및 stroke/result |
| M6 | Camera Director | 상태별 카메라 전환 |
| M7 | Character | Swing/FollowThrough/WatchBall |
| M8 | HUD | 필수 HUD 전체 연결 |
| M9 | VFX/Audio | Normal/Perfect 차이 명확 |
| M10 | Hole 1 Vertical Slice | 전체 gameplay flow |
| M11 | Polish | 품질 비교/성능/버그 gate 통과 |

## Global Dependencies

- Unity Editor
- URP
- Input System
- optional Cinemachine
- optional placeholder assets

Package 추가는 실제 버전 확인 후 수행한다.

## Risks

### Scope Creep
대응: AGENTS.md Do Not Implement Yet 준수.

### Art Availability
대응: Placeholder + TODO_ART.md.

### Physics Tuning
대응: 모든 핵심 coefficient를 data/Inspector로 노출.

### Camera Complexity
대응: CameraDirector로 책임 통합.

### Codex Environment Cannot Play Unity
대응: 가능한 compile/static check를 수행하고 명시적인 Editor validation checklist 제공. 실행하지 않은 검증을 완료했다고 주장하지 않는다.

## Validation

매 milestone:

- compile
- scene wiring
- console errors
- manual play steps
- tests where appropriate
- docs updated

## M0 Manual Play Validation — Complete (2026-08-10)

1. Unity `6000.5.7f1`로 프로젝트를 열고 script compilation이 끝날 때까지 기다린다.
2. `Assets/_Game/Scenes/Foundation.unity`를 연다.
3. Console을 Clear한 뒤 Play한다.
4. WASD/방향키 또는 gamepad left stick 입력 시 capsule 위 작은 indicator가 입력 방향으로 이동하는지 확인한다.
5. Space 또는 gamepad south button을 누르는 동안 capsule 색상이 cyan에서 gold로 변하는지 확인한다.
6. Hierarchy에서 Missing Script가 없고 `FoundationInputProbe`의 두 scene reference가 연결되어 있는지 확인한다.
7. Play 종료 후 Console Error가 0인지 확인한다.

## M0 Result (2026-08-10)

- `Assets/_Game` folder structure를 생성했다. Unity template asset은 불필요하게 이동하지 않았다.
- `Assets/_Game/Scenes/Foundation.unity`에 primitive ground, URP lighting/volume, main camera를 연결했다.
- `FoundationInputProbe.prefab`을 생성하고 Input System 기반 WASD/방향키/gamepad stick 및 Space/gamepad south button debug 반응을 구현했다. Golf/shot behavior는 포함하지 않는다.
- Build Settings의 유일한 enabled scene을 Foundation으로 설정했다.
- Player 기본 해상도를 1920x1080, product/company 표시를 SwingPop, run in background를 enabled로 설정했다.
- 새 Layer/Tag는 현재 M0 scene에 필요하지 않아 추가하지 않았다.
- Unity `.gitignore`를 추가해 `Library`, `Temp`, `Logs`, `UserSettings`, IDE 산출물을 제외했다.
- Unity Editor가 runtime/editor assembly를 성공적으로 컴파일했고 Foundation scene Play Mode 진입 및 URP 렌더링을 확인했다. 해당 Play 구간의 Editor log에서 C# compile error, `NullReferenceException`, `MissingReferenceException`은 발견되지 않았다.
- 사용자 수동 검증에서 WASD/방향키 indicator 반응, Space confirm 색상 반응, Console Error 0을 확인했다. M0 수동 검증 절차를 모두 통과했다.

## Next Action

M8 HUD의 수동 visual/readability, mouse click, 1920×1080/1600×900/1280×720 anchor 검증을 완료한다. M9 VFX/Audio는 별도 요청 전까지 시작하지 않는다.

## M1 Ball Launch Implementation (2026-08-10)

- `GolfBall.prefab`: 0.3m placeholder sphere, `Rigidbody`, `SphereCollider`, `GolfBallController` 연결
- `M1BallTuning.asset`: launch speed/angle, mass, damping, gravity scale, rolling deceleration, bounce-to-roll threshold, stop thresholds/duration
- `GolfBallPhysics.asset`: bounce와 static/dynamic friction data
- `TemporaryBallInput`: Space/gamepad south launch, R/gamepad north reset
- `BallFollowCamera`: Cinemachine package 추가 없이 최소 smooth follow
- `BallDebugTelemetry`: current state, speed, velocity, grounded, control hint 표시 및 launch vector debug ray
- `BallStopDetector`: grounded 상태에서 linear/angular threshold를 일정 시간 만족해야만 Stop 처리
- Foundation scene의 M0 probe instance를 제거하고 M1 ball, launch direction, input/debug system, follow camera를 실제 연결
- Aim, Power Gauge, Impact Timing, Wind, Character, HUD는 구현하지 않음

## M1 Automated Validation (2026-08-10)

- Unity runtime/editor/test assemblies compile 성공
- Unity Test Framework EditMode: `BallStopDetectorTests` 3 passed, 0 failed
- Foundation scene Play Mode physics validation:
  `Ready → Airborne → Bouncing → Rolling → Stopped → Reset/Ready`
- 최종 자동 Play run: 9.63초, stop position `(0.00, 0.15, 64.73)`
- 최종 Play run 구간에서 kinematic velocity warning, C# error, `NullReferenceException`, `MissingReferenceException` 없음
- 자동 검증은 `GolfBallController.Launch()`를 직접 호출했으므로 실제 Space/R Input System command는 아래 수동 절차로 확인해야 한다.

## M1 Manual Play Validation — Complete

1. Unity 상단 메뉴에서 `Window > General > Console`을 클릭해 Console 창을 연다.
2. Console 왼쪽 위의 `Clear`를 클릭한다.
3. Project 창에서 `Assets > _Game > Scenes`를 차례로 열고 `Foundation` scene을 더블클릭한다.
4. Unity 화면 상단 중앙의 삼각형 `Play` 버튼을 클릭한다.
5. `Game` 탭 왼쪽 위 telemetry가 `State: Ready`, `Speed: 0.00 m/s`인지 확인한다.
6. Game 탭 안쪽을 한 번 클릭해 키보드 포커스를 준 뒤 Space를 한 번 누른다.
7. 공이 앞으로 포물선 비행하고 ground에서 bounce한 뒤 roll하는지 확인한다. 카메라가 공을 놓치지 않고 따라가야 한다.
8. telemetry가 최종적으로 `State: Stopped`, `Speed: 0.00 m/s` 근처가 되는지 확인한다.
9. R을 누르고 공과 카메라가 시작 위치로 돌아오며 `State: Ready`가 되는지 확인한다.
10. Space를 다시 눌러 두 번째 launch가 가능한지 확인한다.
11. 상단 중앙의 파란색 Play 버튼을 다시 클릭해 Play Mode를 종료한다.
12. Console의 빨간 Error 카운트가 0인지 확인한다.

튜닝 값을 확인하려면 Project 창에서 `Assets > _Game > ScriptableObjects > M1BallTuning`을 클릭한다. 값을 바꿀 필요는 없으며, 변경했다면 Play Mode를 종료한 상태에서 수정한다.

## M2 Aim / Power / Impact Implementation (2026-08-10)

- `ShotFlowController`가 `Aiming → PowerSelecting → ImpactSelecting → ShotCommitted` 전이를 단일 소유한다.
- A/D 또는 좌우 방향키로 ±30도 yaw 조준, Space로 각 단계를 확정하고 Escape로 선택 중 취소한다.
- `ShotCalculator`가 power 정규화, impact grade, power loss, deterministic dispersion을 순수 계산한다.
- 직렬화 가능한 `ShotCommand`가 aim, power, impact, 최종 방향과 launch 기준값을 캡처한다.
- `GolfBallController`는 `ShotCommand`의 최종 방향과 유효 power를 기존 M1 Rigidbody launch에 전달한다.
- `M2ShotTuning.asset`에서 aim 속도/범위, gauge 속도, grade 구간, power multiplier, dispersion을 조정한다.
- Debug overlay와 aim line은 현재 shot state, aim, power, impact cursor/grade, ball state/speed, 마지막 command를 표시한다. 최종 HUD가 아니다.
- Impact 단계에서 P를 누르면 검증용 PERFECT shot을 즉시 확정한다.
- M1의 bounce, roll, stable stop, reset, follow camera를 그대로 유지한다.
- 구현하지 않은 항목: wind, spin, terrain 차이, club system, putter, hole/scoring, character, Cinemachine, 최종 HUD/VFX/audio.

## M2 Validation (2026-08-10)

- Unity Test Framework EditMode: 총 16 passed, 0 failed (`ShotCalculatorTests` 포함).
- 자동 Play Mode 통합 검증 최종 통과: shot flow 확정, `Ready → Airborne → Bouncing → Rolling → Stopped`, reset 후 `Aiming/Ready` 복구.
- 동일 aim의 35% shot은 9.74m, 90% shot은 52.94m로 power 차이가 실제 정지 거리에 반영되었다.
- 반대 aim의 lateral 위치와 MISS의 power loss/9도 dispersion이 적용되었다.
- 실제 키보드 A/D/방향키/Space/Escape/R 입력 체감은 아래 최종 보고의 수동 절차로 사용자 확인이 필요하다.

## M3 Arcade Ball Flight Implementation (2026-08-10)

- `ShotSpin`은 `VerticalSpin -1..+1`과 `SideSpin -1..+1`을 직렬화한다. Vertical 음수는 BackSpin, 양수는 TopSpin이다.
- 숫자 1~5로 No/Top/Back/Left/Right spin preset을 선택하고 현재 선택/활성 spin을 Debug overlay에서 확인한다.
- `BallFlightModel`은 velocity-relative drag, lift/downforce, side curve, spin decay를 순수 계산한다. 외부 가속도 인자는 향후 Wind hook이며 M3에서는 항상 zero다.
- `BallGroundResponse`는 첫 landing 감속/boost와 rolling deceleration modifier를 계산한다.
- Physics Material이 vertical bounce retention을 단독 소유하고 custom spin response는 planar velocity만 조정해 에너지 충돌을 피한다.
- 상승 중 출발 지면 접촉을 landing으로 오인하지 않도록 `Maximum Upward Landing Speed` 판정을 추가했다.
- BackSpin rollback은 `Rolling` 전환 때 한 번만 적용하고 angular velocity를 정리해 무한 역회전을 방지한다.
- `BallTrajectoryDebug`가 최근 실제 궤적을 LineRenderer로 기록한다.
- 기존 M1/M2 state, reset, stable stop, shot flow, input, camera follow는 유지한다.
- Wind 실제 시스템, terrain 차이, club/putter, hole/scoring, character, final HUD/VFX/audio는 구현하지 않았다.

## M3 Automated Validation (2026-08-10)

- Unity Test Framework EditMode: 총 27 passed, 0 failed. 기존 16개와 spin clamp/preset/decay/side direction/landing/rollout 테스트를 포함한다.
- 최종 동일 Aim/Power Rigidbody 비교: NoSpin 44.26m, TopSpin 49.04m, BackSpin 30.88m, BackSpin rollback 0.20m, Left X -12.28m, Right X +12.28m.
- 다섯 preset 모두 `Ready → Airborne → Bouncing → Rolling → Stopped → Reset/Aiming`을 통과했다.
- M2 회귀 검증도 통과했다: low power 9.39m, high power 46.48m, MISS 27.66m/X 13.41m, Reset/Aiming 복구.
- 실제 숫자키 및 trajectory 육안 체감은 사용자 수동 검증이 필요하다.

## M4 Wind / Terrain Implementation (2026-08-11)

- `WindController`가 world-space direction, strength(m/s), preset의 단일 source of truth다. Ball physics와 debug presentation은 동일 controller를 읽는다.
- `M4WindTuning.asset`에서 preset strength와 `WindForceMultiplier`, `HeadTailMultiplier`, `CrosswindMultiplier`를 조정한다.
- 숫자 `6~0`으로 Calm/Tailwind/Headwind/Left Crosswind/Right Crosswind를 선택한다. 상단 숫자열과 숫자패드를 모두 지원한다.
- Wind는 공중에서만 적용되며 Tail/Head는 carry, Crosswind는 lateral drift에 영향을 준다.
- 각 surface collider의 `TerrainSurface` component가 `TerrainSurfaceData`를 제공한다. Tag 문자열 비교는 사용하지 않는다.
- Tee/Fairway/Rough/Bunker/Green/Water/OutOfBounds data asset을 추가했다. Power, friction, bounce, spin response, rolling resistance가 asset에서 조정 가능하다.
- Ball은 contact surface를 current lie로 기록하고 surface rolling/bounce/spin response를 적용한다. `ShotFlowController`는 ShotCommand에 current lie power modifier를 반영하는 seam을 사용한다.
- Water/OOB 진입 또는 설정 높이 아래 추락 시 ball을 안전하게 `Stopped`로 만들고 hazard를 기록한다. `R` reset으로 `Ready/Aiming`을 복구한다. Stroke penalty/scoring은 M5 범위이므로 구현하지 않았다.
- Foundation 씬에 색상 placeholder Tee/Fairway/Rough/Bunker/Green과 Water/OOB trigger zone, wind vector, 확장 debug overlay를 연결했다.

## M4 Automated Validation (2026-08-11)

- Unity Test Framework EditMode: 총 41 passed, 0 failed. 기존 32개와 wind 방향/부호, surface response, lie power, spin response 테스트를 포함한다.
- 동일 Rigidbody 샷 착지: Headwind 24.06m < Calm 30.19m < Tailwind 37.80m.
- Crosswind 착지: Left X -7.02m, Right X +7.02m.
- 동일 65% 샷 surface 이동: Fairway 29.99m > Rough 25.69m > Bunker 22.62m.
- 첫 bounce 상승 속도: Green 0.00m/s < Fairway 0.75m/s.
- Water/OOB는 안전하게 shot을 종료했고 Reset 후 Ball Ready / ShotFlow Aiming을 복구했다.
- M3 회귀: NoSpin 44.27m, TopSpin 50.06m, BackSpin 30.85m/rollback 0.23m, SideSpin X -11.43m/+11.43m; 전체 state sequence 통과.

## M4 Manual Play Validation

1. Project 창에서 `Assets > _Game > Scenes > Foundation`을 더블클릭한다.
2. `Window > General > Console`을 열고 왼쪽 위 `Clear`를 클릭한다.
3. 상단 중앙의 `Play` 버튼을 누른 뒤 `Game` 탭 안쪽을 한 번 클릭한다.
4. Debug overlay가 `M4 SHOT / WIND / TERRAIN DEBUG`, `Wind: Calm`, `Surface / Lie: Tee`를 표시하는지 확인한다.
5. 숫자 `6, 7, 8, 9, 0`을 누를 때 Wind preset/direction/strength가 각각 바뀌고 magenta wind vector가 나타나는지 확인한다.
6. 숫자 `1~5`로 spin preset이 여전히 변경되는지 확인한다.
7. A/D 또는 좌우키로 가운데 Fairway, 왼쪽 Rough, 오른쪽 Bunker 방향을 조준하고 Space 3회로 샷한다. Rough와 Bunker에서 더 짧게 구르는지 확인한다.
8. 강한 중앙 샷으로 먼 밝은 Green에 도달했을 때 bounce가 낮고 lie가 Green으로 표시되는지 확인한다.
9. 왼쪽 파란 Water 또는 오른쪽 빨간 OOB trigger로 공을 보내 `Ball: Stopped`, `Last Hazard`가 표시되는지 확인한다.
10. `R`을 눌러 Ball `Ready`, Shot `Aiming`, Lie `Tee`로 돌아오는지 확인한다.
11. Play를 종료하고 Console의 빨간 Error가 0인지 확인한다.

튜닝 위치:

- Wind: `Assets/_Game/ScriptableObjects/M4WindTuning.asset`
- Surface: `Assets/_Game/ScriptableObjects/Terrain/*.asset`
- Ball fallback OOB height: `Assets/_Game/ScriptableObjects/M1BallTuning.asset > Hazard Recovery`

## M5 Hole / Scoring / Continuous Shot Flow Implementation (2026-08-15)

- `Hole01.asset`이 Hole 1 / Par 4 / Tee `(0, 0.15, 0)` / Cup `(0, 0.05, 78)` / 78m와 cup capture tuning을 소유한다.
- `HoleFlowController`가 hole 시작, committed shot/penalty stroke, last valid lie/position, 다음 shot 준비, hole complete/result를 단일 소유한다.
- 정상 shot이 `Stopped`가 되면 공 위치와 lie를 유지한 채 다음 프레임 `Ready/Aiming`으로 전환한다. 정상 진행에는 `R`이 필요하지 않다.
- `R`은 Hole 1 전체를 Tee/0 stroke/0 penalty로 되돌리는 debug restart로 남긴다.
- `TemporaryDriver.asset`과 `Putter.asset`의 최소 ClubData를 추가했다. Green에서는 Putter로 자동 전환하며 Putter는 spin/wind 비행 대신 즉시 rolling을 시작한다.
- `ShotCommand`는 club type/carry/roll modifier를 직렬화하고, 기존 surface power modifier와 club data가 실제 launch velocity에 반영된다.
- `CupCaptureController`가 Green, horizontal distance/speed, height 조건을 모두 검사한다. 작은 outer assist 영역에서 저속 공에만 inward acceleration을 적용한다.
- Water/OOB는 committed shot과 별도로 +1 penalty stroke를 더하고 마지막 정상 정지 위치/surface로 자동 복구한다.
- `ScoreCalculator`는 Albatross/Eagle/Birdie/Par/Bogey/Double Bogey 및 +3 이상을 순수 계산한다.
- Debug overlay에 Hole/Par/Stroke/Penalty/Lie/Club/Remaining Distance/Height Difference/Hole State/Result/Cup Distance/Last Hazard를 추가했다.
- Foundation의 M4 blockout은 `M5 Hole 1 Placeholder Course`로 이어 사용하며 Green에 placeholder Cup/Flag trigger를 연결했다.

## M5 Automated Validation (2026-08-15)

- Unity Test Framework EditMode: 총 54 passed, 0 failed. 기존 41개와 score mapping, stroke/penalty, last valid position, cup eligibility, putter launch/roll tests를 포함한다.
- M5 PlayMode: 첫 shot이 `(0.00, 0.15, 17.40)`에서 멈춘 뒤 teleport/reset 없이 그 위치에서 `Ready/Aiming`으로 복귀했다.
- 동일 62% shot 실제 이동: Fairway `25.59m` > Rough `19.19m` > Bunker `9.19m`.
- Green test position에서 Putter 자동 선택, rolling putt, `Ball Holed`, `HoleComplete`, Par 4 결과 계산을 통과했다.
- Water/OOB가 각각 +1 penalty를 추가하고 Last Valid Position으로 복귀한 뒤 다음 shot 가능 상태가 됐다.
- 회귀: M1 physics flow, M2 Aim/Power/Impact, M3 5개 spin, M4 wind/terrain/hazard validation이 모두 통과했다.

## M5 Manual Play Validation

1. Project 창에서 `Assets > _Game > Scenes > Foundation`을 더블클릭한다.
2. `Window > General > Console`을 열고 `Clear`를 클릭한 뒤 상단 `Play`를 누른다.
3. Game 탭을 클릭하고 overlay의 `Hole 1`, `Par 4`, `Stroke 0`, `Current Club: Temporary Driver`, `Hole State: Playing`을 확인한다.
4. A/D 또는 좌우키로 조준하고 Space 3회로 첫 shot을 완료한다. 공이 멈춘 뒤 `R`을 누르지 않고 같은 위치에서 `Aiming`이 되는지 확인한다.
5. 같은 방식으로 두 번째/세 번째 shot을 진행하고 Stroke가 committed shot마다 1씩 증가하는지 확인한다. Escape로 Power/Impact를 취소했을 때는 증가하지 않아야 한다.
6. Green에 공이 멈추면 `Current Club: Putter`와 `Current Lie: Green`을 확인한다. 다음 Space 3회 putt은 거의 뜨지 않고 Green 위를 굴러야 한다.
7. 저속 putt을 Cup/Flag 쪽으로 보내 `Ball: Holed`, `Hole State: HoleComplete`, `RESULT`를 확인한다. 완료 후 Space를 눌러도 새 shot이 시작되지 않아야 한다.
8. `R`로 Hole 1을 debug restart한 뒤 왼쪽 Water 또는 오른쪽 OOB로 shot한다. Penalty가 1 증가하고 이전 정상 정지 위치로 자동 복구되는지 확인한다.
9. 숫자 1~5 spin, 6~0 wind, trajectory, camera follow가 계속 동작하는지 확인한다.
10. Play를 종료하고 Console의 빨간 Error가 0인지 확인한다.

튜닝 위치:

- Hole/cup: `Assets/_Game/ScriptableObjects/Holes/Hole01.asset`
- Clubs: `Assets/_Game/ScriptableObjects/Clubs/TemporaryDriver.asset`, `Putter.asset`
- Lie response: `Assets/_Game/ScriptableObjects/Terrain/*.asset`
- Ball/putt stop: `Assets/_Game/ScriptableObjects/M1BallTuning.asset`
- Aim/Power/Impact: `Assets/_Game/ScriptableObjects/M2ShotTuning.asset`

## M6 Camera Director Implementation (2026-08-15)

- Main Camera의 기존 `BallFollowCamera`는 삭제하지 않고 비활성화했으며, `CameraDirector`만 transform/rotation/FOV를 제어한다.
- 명시적 모드: `HoleIntro`, `Address`, `Aim`, `Swing`, `Impact`, `BallFollow`, `Landing`, `NextShot`, `Putt`, `HoleComplete`, `Result`.
- Shot/Ball/Hole 이벤트를 관찰해 presentation 상태만 전환한다. Camera는 gameplay state, physics, scoring을 변경하지 않는다.
- `M6CameraTuning.asset`이 모드별 offset/FOV/hold, transition, follow look-ahead, speed distance, apex extension, shake, collision sphere cast를 소유한다.
- 위치/회전/FOV는 transition pose 보간 후 exponential damping으로 이어지며, BallFollow는 현재 velocity를 기준으로 방향과 거리를 계산한다.
- Debug overlay는 현재/이전 Camera Mode, transition 여부, target, FOV, follow distance를 표시한다.
- Putt 구도는 Ball-Cup 중간점을 기준으로 남은 거리에 따라 후퇴 거리, 높이, FOV를 확장해 긴 Green putt에서도 두 대상을 함께 유지한다.
- Cinemachine 3.1.5는 Unity 6의 정식 선택지지만 현재 요구에 비해 virtual camera/package 구조가 불필요하여 설치하지 않았다.

## M6 Automated Validation (2026-08-15)

- Unity Test Framework EditMode: 총 63 passed, 0 failed. 기존 54개와 Camera mode state, transition, pose/FOV interpolation, offset, velocity fallback, follow-distance, 장거리 Putt framing 테스트 9개를 포함한다.
- M6 PlayMode: `HoleIntro → Address → Aim → Swing → Impact → BallFollow → Landing → NextShot → Address → Putt → Impact → Putt → HoleComplete → Result` 순서를 통과했다.
- 관측 FOV 범위는 42.0–60.3, BallFollow 최대 거리는 14.1m였고 일반 shot 정지 후 같은 lie에서 다음 Address/Aim으로 복귀했다.
- 12m Green putt에서 Ball과 Cup이 모두 safe viewport 안에 있는지 자동 검사한다.
- M1, M2, M3, M4, M5 PlayMode validation을 다시 실행해 모두 PASS했다.

## M6 Manual Camera Validation

1. Project 창에서 `Assets > _Game > Scenes > Foundation`을 더블클릭한다.
2. 상단 메뉴에서 `Window > General > Console`을 열고 왼쪽 위 `Clear`를 클릭한다.
3. 상단 중앙 `Play`를 클릭한다. Game 탭에서 코스를 훑는 약 2.8초 `HoleIntro`와 `Camera: HoleIntro` 표시를 확인한다.
4. Intro가 끝나면 공 뒤·옆의 `Address`, 이어 `Aim`으로 부드럽게 전환되고 공과 목표 방향이 함께 보이는지 확인한다.
5. Game 탭을 클릭하고 A/D 또는 좌우키로 조준한다. 카메라가 제한적으로 반응하며 급회전하지 않는지 확인한다.
6. Space를 세 번 눌러 Power/Impact를 확정한다. `Swing → Impact → BallFollow`가 이어지고 Perfect shot은 일반 shot보다 짧은 반응이 더 강한지 확인한다.
7. 비행 중 공이 화면에서 사라지지 않고, 최고점 부근에서 시야가 답답하지 않으며, 착지 때 `Landing`으로 전환되는지 확인한다.
8. Bounce/Roll 후 `NextShot`을 거쳐 같은 lie의 `Address/Aim`으로 돌아오며 순간이동처럼 보이지 않는지 확인한다.
9. Green에서 Club이 Putter가 되면 `Putt` 구도로 공과 Cup이 함께 읽히는지 확인한다. Hole In 후 `HoleComplete → Result`를 확인한다.
10. 카메라와 공 사이에 course geometry가 들어올 때 카메라가 지형을 관통하지 않는지 확인한다.
11. `R`을 눌러 HoleIntro부터 재시작되는지 확인하고 Play를 종료한 뒤 Console의 빨간 Error가 0인지 확인한다.

Green까지 직접 이동하지 않고 Putt 구도만 확인하려면 Play Mode에서 상단 메뉴 `SwingPop > M6 > Preview 12m Putt Camera`를 클릭한다. 공을 Cup 12m 앞 Green에 배치하고 개발용 overlay를 숨긴 상태로 Putt 카메라를 보여 준다. Play Mode를 종료하면 원래 상태로 복구된다.

튜닝 위치:

- 전체 카메라: `Assets/_Game/ScriptableObjects/Camera/M6CameraTuning.asset`
- Main Camera wiring: `Assets/_Game/Scenes/Foundation.unity > Presentation > Main Camera > Camera Director`

## M7 Character / Animation Implementation (2026-08-16)

- 저장소에는 라이선스가 확인되는 gameplay character/humanoid asset이 없어 외부 다운로드 없이 primitive hierarchy 기반 `PlaceholderGolfer.prefab`을 제작했다.
- `CharacterGolfController`는 Shot/Ball/Hole event를 Character presentation state로 연결하고, `CharacterAnimationController`는 procedural pose와 향후 Animator 교체 지점을 소유한다.
- 상태는 `Address`, `BackSwing`, `Swing`, `FollowThrough`, `WatchBall`, `PuttAddress`, `PuttBackSwing`, `PuttSwing`, `PuttFollowThrough`와 결과 celebration hook을 포함한다.
- Address 위치는 현재 Ball과 Aim direction 기준 lateral/backward/height offset으로 계산한다. Aim 변경은 smoothing된 character orientation에 반영되고 다음 lie에서는 캐릭터가 새 Ball 위치로 이동한다.
- Shot commit은 더 이상 즉시 Ball을 발사하지 않는다. 캐릭터 Swing의 normalized Impact marker가 `ShotFlowController.TryLaunchCommittedShot()`을 한 번 호출하며, 중복 신호는 차단된다.
- Character adapter가 없으면 기존 즉시 launch를 유지하고, 연결된 adapter의 Impact 신호가 누락되면 data 기반 timeout fallback이 launch를 보장한다.
- Camera는 Shot commit에서 Swing을 유지하고 실제 `Ball.Launched` 시점에 Impact로 전환해 `Impact → BallFollow/Putt` 순서를 animation과 동기화한다.
- `ClubSocket`과 교체 가능한 Driver/Putter primitive visual을 추가했다. Green에서 Club 변경 event가 Putter visual과 낮은 putt motion을 즉시 선택한다.
- Debug overlay는 Character/Animation state, Impact fired 여부, pending launch, fallback 사용 여부, club visual, character aim angle을 표시한다. `H`로 overlay를 숨기거나 다시 표시한다.

## M7 Automated Validation (2026-08-16)

- Unity Test Framework EditMode: 총 77 passed, 0 failed. 기존 63개와 character placement/orientation, state mapping, Impact single-fire, fallback delay, club visual, celebration mapping 테스트 14개를 포함한다.
- M7 PlayMode: `BackSwing → Swing → FollowThrough → WatchBall → Address → PuttAddress → PuttSwing → PuttFollowThrough → WatchBall → EagleCelebration`을 통과했다.
- Shot commit 직후 Ball이 Ready인 구간, primary Impact event 이후 단일 launch, fallback 미사용, 다음 lie character reposition을 확인했다.
- Camera integration은 `HoleIntro → Address → Swing → Impact → BallFollow → Landing → NextShot → Putt → HoleComplete → Result`를 통과했다.
- M1, M2, M3, M4, M5, M6 PlayMode regression validation을 다시 실행해 모두 PASS했다.

## M7 Manual Character Validation

1. Project 창에서 `Assets > _Game > Scenes > Foundation`을 더블클릭한다.
2. Unity 상단 메뉴에서 `Window > General > Console`을 클릭하고 Console 왼쪽 위 `Clear`를 클릭한다.
3. 상단 중앙의 ▶ `Play` 버튼을 누른다. 약 2.8초 HoleIntro 뒤 Golfer가 공 옆 Address 위치에 보이는지 확인한다.
4. Game 탭 내부를 클릭하고 `H`를 눌러 큰 debug overlay를 숨긴다. 다시 `H`를 누르면 telemetry가 나타난다.
5. `A/D` 또는 좌우 방향키로 조준하고 Golfer가 aim 방향을 부드럽게 따라 회전하는지 확인한다.
6. Space를 한 번 눌러 PowerSelecting에서 Address pose가 유지되는지 확인한다.
7. Space를 두 번째로 눌러 ImpactSelecting에서 BackSwing 준비 pose가 나타나는지 확인한다.
8. Space를 세 번째로 눌러 `Swing → Impact에서 Ball Launch → FollowThrough → WatchBall` 순서인지 확인한다. 공이 club impact보다 먼저 출발하면 안 된다.
9. 공이 멈춘 뒤 캐릭터가 새 Ball 위치로 이동해 Address로 돌아오는지 확인한다.
10. Green에서 Current Club이 Putter가 되면 짧은 `PuttAddress/PuttBackSwing/PuttSwing/PuttFollowThrough`와 납작한 Putter head가 보이는지 확인한다.
11. Hole In 후 결과에 맞는 placeholder Happy/Sad/Birdie/Eagle/HoleInOne reaction이 보이는지 확인한다.
12. Play를 종료한 뒤 Console의 빨간 Error가 0인지 확인한다.

튜닝 위치:

- Character placement/timeline/fallback/socket: `Assets/_Game/ScriptableObjects/Character/M7CharacterTuning.asset`
- Placeholder prefab/attachment: `Assets/_Game/Prefabs/Characters/PlaceholderGolfer.prefab`
- Scene wiring: `Assets/_Game/Scenes/Foundation.unity > Presentation > Placeholder Golfer`

## M8 Gameplay HUD Implementation (2026-08-16)

- UI technology는 이미 설치된 uGUI 2.5.0을 선택했다. Unity 6000.5.7f1에서 runtime HUD, CanvasScaler, Input System pointer, 간단한 animation, prefab 교체 흐름을 추가 package 없이 구성할 수 있기 때문이다.
- `GameplayHUD.prefab`은 1920×1080 reference resolution, Match 0.5, full-screen `Safe Area` wrapper, corner/center/bottom anchor를 사용한다.
- Player/Stroke/Penalty, Hole/Par/Live Stroke, Wind arrow/m/s, projected Aim marker, Remaining Distance/Height Difference, Club/Lie/Spin을 gameplay source에서 표시한다.
- `GameplayHudPresenter`가 Shot/Ball/Wind/Hole event를 구독하고 `GameplayHudView`, `HudGaugeView`, `HudPopupView`, `HudResultView`에 presentation만 전달한다.
- Power와 Impact cursor는 기존 ShotFlow의 현재 값을 표시한다. Perfect/Great/Good zone은 `M2ShotTuning.asset` threshold에서 구성해 판정과 시각 구간이 일치한다.
- Primary button click과 Keyboard Space는 `ShotFlowController.ConfirmCurrentStep()`의 동일 command path를 사용한다. 같은 frame 중복 confirm은 차단한다.
- Impact grade popup, Water/OOB +1 penalty popup, 작은 next-lie feedback, Result fade/scale, button breathing, power glow, wind rotation smoothing을 추가했다.
- Green/Putter에서는 Spin을 `SPIN DISABLED`로 표시한다. 기존 숫자 1~5, 6~0, H debug overlay, trajectory와 M1~M7 gameplay는 유지한다.
- `ShotDebugOverlay`는 삭제하지 않았고 gameplay HUD와 독립적인 개발 도구로 유지했다.

## M8 Automated Validation (2026-08-16)

- Unity Test Framework EditMode: 총 94 passed, 0 failed. 기존 77개와 Action state, Spin label, Height, Wind arrow, Score result, Hazard message presentation mapping 17개를 포함한다.
- M8 PlayMode: initial HUD, mouse Primary Action, Power/Impact gauge, data-backed Perfect zone, Spin 5종, Wind 5종, Ball flight interaction hiding, next-shot Lie, Water +1 popup/recovery, Green Putter/Spin disabled, Result panel을 통과했다.
- CanvasScaler는 `Scale With Screen Size`, reference `1920×1080`, Match `0.5`로 자동 확인했다.
- M1, M2, M3, M4, M5, M6, M7 PlayMode regression validation을 다시 실행해 모두 PASS했다.
- 실제 Game View에서의 미세 spacing, button pointer click 체감, 1920×1080/1600×900/1280×720 visual anchor 검증은 아래 수동 절차가 필요하다.

## M8 Manual HUD Validation

1. Project 창에서 `Assets > _Game > Scenes > Foundation`을 더블클릭한다.
2. 상단 메뉴 `Window > General > Console`을 열고 Console 왼쪽 위 `Clear`를 클릭한다.
3. `Game` 탭을 연다. Game 탭 상단 왼쪽 해상도 dropdown을 클릭하고 `1920x1080`을 선택한다. 목록에 없다면 `+`를 눌러 Type `Fixed Resolution`, Width `1920`, Height `1080`, Label `PC 1920x1080`으로 추가한다.
4. 상단 중앙 ▶ `Play`를 클릭하고 Game 탭 내부를 한 번 클릭한다.
5. `H`를 눌러 큰 Debug Overlay를 숨긴 뒤 좌상단 Player/Stroke, 상단 Hole/Par, 우상단 Wind가 보이는지 확인한다.
6. `A/D` 또는 좌우 방향키로 Aim하고 중앙 marker, Remaining Distance, Height Difference가 읽히는지 확인한다.
7. 숫자 `1~5`를 차례로 눌러 `NO/TOP/BACK/LEFT/RIGHT SPIN` 표시가 모두 바뀌는지 확인한다.
8. 숫자 `6~0`을 차례로 눌러 Wind preset, arrow 방향, m/s가 함께 바뀌는지 확인한다.
9. Space 또는 우하단 `START SHOT`을 클릭하고 Power Gauge와 `SET POWER`를 확인한다.
10. 다시 Space 또는 버튼을 클릭하고 Impact Gauge의 MISS/GOOD/GREAT/PERFECT 색 구간과 moving cursor를 확인한다.
11. 세 번째 Space 또는 버튼으로 확정하고 Impact grade popup이 잠깐 보이는지 확인한다. Perfect는 더 강하게 pulse한다.
12. Ball flight 중 Power/Impact와 Shot button이 숨고 상단 gameplay 정보가 안정적으로 남는지 확인한다.
13. 공이 멈춘 뒤 새 Lie/Club/Distance가 갱신되고 `START SHOT`이 다시 나타나는지 확인한다.
14. Green에서 Club이 `PUTTER`, Lie가 `GREEN`, Spin이 `SPIN DISABLED`인지 확인한다. Putt 카메라에서 Ball과 Cup이 함께 보여야 한다.
15. 왼쪽 Water 또는 오른쪽 OOB로 보내 `WATER HAZARD`/`OUT OF BOUNDS`와 `+1 PENALTY` popup을 확인한다.
16. Hole In 후 timing/action이 숨고 중앙 Result panel의 Hole/Par/Strokes/Score label이 실제 결과와 일치하는지 확인한다.
17. Play를 종료하고 Game 탭 해상도를 `1600x900`, `1280x720`으로 각각 바꿔 다시 Play한다. Top/Bottom panel이 화면 밖으로 나가거나 중앙 course/character를 과도하게 가리지 않는지 확인한다.
18. 마지막으로 Play를 종료하고 Console의 빨간 Error가 0인지 확인한다.

튜닝 위치:

- HUD popup/motion/aim marker: `Assets/_Game/ScriptableObjects/UI/M8HudTuning.asset`
- Power/Impact timing과 grade zone: `Assets/_Game/ScriptableObjects/M2ShotTuning.asset`
- HUD prefab/anchors/style: `Assets/_Game/Prefabs/UI/GameplayHUD.prefab`
- Scene wiring: `Assets/_Game/Scenes/Foundation.unity > Presentation > Gameplay HUD`

## M9 VFX / Audio / Shot Feel Implementation (2026-08-23)

- 실제 공 발사 이벤트 `GolfBallController.Launched`를 Impact presentation의 단일 시점으로 사용한다. Character marker가 launch를 발생시키고 Camera, HUD, VFX, Audio가 동일한 launch event 결과에 반응한다.
- `ShotPresentationController`는 gameplay event를 관찰해 Impact, Ball Trail, Landing, Hazard, Hole-In controller로 전달한다. 물리·점수·입력은 소유하지 않는다.
- Normal은 cyan compact flash/burst와 얇은 trail, 기본 impact cue를 사용한다. Perfect는 gold accent, 더 큰 flash/burst/streak, 더 굵고 긴 trail, layered accent cue를 사용하며 physics는 변경하지 않는다.
- `GolfBallController.SurfaceContacted`는 surface, speed, contact sequence, first-landing 여부만 전달한다. 첫 landing은 100%, 두 번째 충분히 빠른 bounce는 data 기반 축소 강도, 이후 접촉은 생략한다.
- Fairway/Green, Rough, Bunker, Water는 재사용 ParticleSystem의 색·형태·count profile로 구분한다. OOB는 particle 없이 HUD/audio 중심이다.
- Swing/Putt, Normal/Perfect Impact, surface landing, Water/OOB, Hole-In/Result cue는 네 개의 category AudioSource로 분리했다. clip slot이 비어 있으면 runtime procedural placeholder tone을 한 번 생성해 재사용한다.
- VFX Graph와 Cinemachine은 추가하지 않았다. 현재 범위는 URP Particle System, TrailRenderer, 기존 CameraDirector로 충분하다.
- 모든 tuning과 교체 AudioClip slot은 `M9ShotPresentationTuning.asset`에 있다. Shot마다 effect GameObject를 Instantiate/Destroy하지 않는다.

## M9 Automated Validation (2026-08-23)

- Unity Test Framework EditMode: 117 passed, 0 failed. Impact/profile, terrain/effect, result/intensity, audio cue, trail profile, duplicate impact gate mapping을 포함한다.
- Unity Test Framework PlayMode: 1 passed, 0 failed. Foundation Scene에서 Character marker 기반 Normal/Perfect launch, impact one-shot gate, trail 강도 차이, HUD/Camera 연동, terrain/hazard/hole audio route, reusable ParticleSystem/TrailRenderer object count를 확인했다.
- M9 Scene/Prefab builder batch 실행은 exit 0이며 Missing serialized property 또는 compile error가 없었다.
- 실제 speaker 출력의 음색·볼륨, Game View에서 VFX 가독성, Bunker/Water/Hole-In의 체감 품질은 수동 검증 대상이다.

## M9 Manual Quality Validation

1. `Assets > _Game > Scenes > Foundation`을 더블클릭한다.
2. `Window > General > Console`을 열고 왼쪽 위 `Clear`를 클릭한다.
3. `Game` 탭의 해상도 dropdown에서 `1920x1080`을 선택한다. 없으면 `+ > Fixed Resolution`, Width `1920`, Height `1080`으로 만든다.
4. 상단 중앙 ▶ `Play`를 누르고 Game 화면을 한 번 클릭한 뒤 `H`로 Debug Overlay를 숨긴다.
5. 일반 timing으로 Driver shot을 실행해 Swing whoosh → club impact → 공 출발이 한 흐름인지, cyan compact impact와 얇은 trail이 보이는지 확인한다.
6. 다음 shot에서 Impact 단계 중 `P`를 눌러 Perfect를 강제한다. gold flash/streak, 더 굵고 긴 trail, layered impact sound, Camera kick, PERFECT HUD가 동시에 반응하는지 일반 shot과 비교한다.
7. 숫자 `2`, `3`, `4`, `5`로 Top/Back/Left/Right Spin을 선택해 accent trail 색이 바뀌되 공을 가리지 않는지 확인한다.
8. 공이 Fairway/Rough에 착지할 때 작은 grass puff와 첫 landing sound가 나고, 두 번째 bounce 효과가 더 작으며 반복 contact에서 particle이 계속 발생하지 않는지 본다.
9. Bunker 방향으로 shot해 sand 색 puff와 bunker landing cue를 확인한다.
10. Water로 shot해 splash, hazard cue, `WATER HAZARD +1 PENALTY`, recovery가 같은 사건처럼 보이는지 확인한다. OOB는 particle보다 HUD/failure cue 중심인지 확인한다.
11. Green에서 Putter가 선택되면 Ball과 Cup이 함께 보이는지 확인하고 putt한다. Hole-In 시 upward sparkle/ring, success cue, Result HUD, Camera, Character celebration이 연결되는지 본다.
12. 여러 shot을 반복한 뒤 Hierarchy의 `Presentation > M9 Shot Feel Presentation` 아래 effect object 수가 늘지 않는지 확인한다.
13. Play를 종료하고 Console의 빨간 Error가 0인지 확인한다.

튜닝 위치:

- VFX/Trail/Landing/Hole-In/Audio volume 및 clip: `Assets/_Game/ScriptableObjects/Presentation/M9ShotPresentationTuning.asset`
- 교체 가능한 presentation prefab: `Assets/_Game/Prefabs/VFX/ShotFeelPresentation.prefab`
- Scene wiring: `Assets/_Game/Scenes/Foundation.unity > Presentation > M9 Shot Feel Presentation`
- Camera impact 강도: `Assets/_Game/ScriptableObjects/Camera/M6CameraTuning.asset`
- HUD impact pulse: `Assets/_Game/ScriptableObjects/UI/M8HudTuning.asset`

## M10 Sky Island Hole 1 Vertical Slice Implementation (2026-08-23)

- Foundation을 그대로 보존하고 `Assets/_Game/Scenes/Hole01_SkyIsland.unity`를 독립적으로 생성했다.
- 기존 gameplay course collider/surface/cup을 유지하면서 M10 shared material로 visual separation을 적용했다.
- collider 없는 floating-island silhouette, edge trees/flowers, five drifting cloud clusters, four distant islands, waterfall island와 rotating windmill을 prefab 기반으로 추가했다.
- procedural skybox, soft-shadow directional light, trilight ambient, restrained linear fog로 밝은 daytime fantasy palette를 구성했다.
- 기존 CameraDirector/HUD/M9 ShotFeel graph는 scene 전용 tuning clone만 연결했다. 새 camera, shot, score, character, HUD system은 만들지 않았다.
- Debug overlay/trajectory는 M10에서 기본 숨김이고 H/F1로 복원된다. Foundation 기본값은 변경하지 않았다.
- M10 HUD 하단에는 `A/D AIM   SPACE / CLICK SHOT   1-5 SPIN` control hint를 scene override로 표시한다.
- `M10VerticalSlicePlayModeTests`는 필수 object/reference, surface mapping, art collider 0, environment budget, Normal/Perfect, Water recovery, Putter Hole-In/Result를 확인한다.
- 자동 결과: EditMode 117 passed, PlayMode 2 passed(Foundation M9 + Hole01 M10), compile/test error 0.
- 실제 Game View/해상도/스피커/Profiler 수동 검증은 `M10_VERTICAL_SLICE_REVIEW.md`와 최종 보고 절차에 남긴다.
- M11은 시작하지 않는다.

튜닝 위치:

- Environment motion/ambience: `Assets/_Game/ScriptableObjects/Environment/M10SkyIslandEnvironmentTuning.asset`
- Hole01 camera: `Assets/_Game/ScriptableObjects/Environment/M10CameraTuning.asset`
- Hole01 HUD: `Assets/_Game/ScriptableObjects/Environment/M10HudTuning.asset`
- Hole01 VFX/trail/audio: `Assets/_Game/ScriptableObjects/Environment/M10ShotPresentationTuning.asset`
- Reusable environment prefabs: `Assets/_Game/Prefabs/Environment/`
- Shared environment materials: `Assets/_Game/Materials/Environment/`

## M11 Polish / Quality Gate Implementation (2026-08-23)

- 실제 M10 Address baseline을 캡처한 뒤 character scale, course silhouette/depth, landmark clusters, camera framing, HUD hierarchy, ball/aim readability, material/lighting을 M11 범위에서 개선했다.
- `M11 Visual Polish`는 collider 없는 presentation layer이며 기존 Tee/Fairway/Rough/Bunker/Green/Water/OOB gameplay data와 M1–M10 event graph를 재사용한다.
- Hole01은 전용 `M11CameraTuning.asset`, shared polish material, organic mesh asset과 isolated `M11Skybox.mat`을 사용한다. Foundation은 변경하지 않았다.
- structural audit: GameObjects 328, Renderers 191, SharedMaterials 54, Transparent slots 4, ShadowCasters 77, Colliders 10, Particles 7, AudioSources 5, Update behaviours 12, Missing Scripts 0.
- 자동 검증: EditMode 117 passed, PlayMode 3 passed. 3개 16:9 Address render의 HUD fit을 확인했다.
- Profiler 1080p 60 FPS와 full-hole visual/audio/comfort는 실행하지 않았으며 수동 quality gate에 남긴다.
- 이후 milestone은 시작하지 않는다. 상세 절차와 판정은 `docs/M11_QUALITY_GATE_REVIEW.md`를 따른다.

튜닝 위치:

- M11 camera: `Assets/_Game/ScriptableObjects/Polish/M11CameraTuning.asset`
- M11 material/skybox: `Assets/_Game/Materials/Polish/`
- M11 course mesh: `Assets/_Game/Art/Courses/M11/`
- scene/composition builder: `Assets/_Game/Scripts/Editor/M11PolishSceneBuilder.cs`

## M13 Real Network Transport Prototype (2026-08-25)

- [x] Add official Unity Transport 6.5.0 as a real `IMatchTransport` adapter.
- [x] Add `OfflineSingle`, `LocalTwoPlayer`, `NetworkHost`, and `NetworkClient` modes while keeping OfflineSingle default.
- [x] Add protocol-2 envelope, reliable/fragmented delivery, host player binding, timeout, payload/rate guards, and disconnect cleanup.
- [x] Keep host approval and host gameplay result authoritative; client result remains telemetry only.
- [x] Sync versioned snapshots/turns and restore per-player ball, lie, stroke, penalty, and holed state.
- [x] Add command-line launch, Development build, F2 telemetry, validator, tests, localhost two-process evidence, and captures.
- [x] Preserve OfflineSingle/LocalTwoPlayer and graphics validation baselines.
- [ ] Do not start M14 in this milestone.

## M14 Dedicated Authority / Server Simulation Foundation (2026-08-27)

- [x] Preserve protocol 2, DTOs, authority/transport interfaces, OfflineSingle default, and M13 modes.
- [x] Add independent `DedicatedServer` mode, command-line bootstrap, and two-player server transport.
- [x] Assign remote A/B by connection order and reject a third connection as `MatchFull`.
- [x] Execute authoritative shots with existing Rigidbody Ball/HoleFlow without Character Animator.
- [x] Keep clients on approved playback plus authoritative snapshot correction.
- [x] Synchronize turn, ball, lie, stroke, penalty, hazard, putter, holed, lifecycle, version, and hash state.
- [x] Disable server presentation by typed policy while retaining shared Hole01 colliders.
- [x] Add build/validator tools, EditMode/PlayMode coverage, headless and three-process evidence, captures, and documentation.
- [ ] Do not start M15 in this milestone.

# M15 Completion Addendum

M15 Match Lifecycle / Reconnect Foundation is implemented on top of M14 dedicated authority: reserved player slots, server-clock grace, hashed rotating tickets, same-player connection rebind, suspended input, latest-snapshot restore, expiry/abort cleanup, status presentation, validator, automated coverage, and real replacement-process acceptance.

## M16 Authentication / Player Session Foundation (2026-08-29)

- [x] Separate Account, AuthSession, Connection, MatchPlayer, and ReconnectTicket identity layers.
- [x] Add development-only HMAC-SHA256 credentials with runtime-only signing key.
- [x] Require authentication before dedicated match admission or reconnect.
- [x] Bind verified account/session to connection and verified account to match player.
- [x] Reject tampered, expired, revoked, duplicate-account, unauthenticated, and wrong-owner flows.
- [x] Preserve M15 reconnect ticket hashing/rotation and require same-account ownership.
- [x] Add F2-safe telemetry, validator, automated coverage, builds, real-process evidence, captures, and documentation.
- [x] Keep OfflineSingle default and preserve M12-M15 regression baselines.
- [ ] Do not start M17 or production authentication in this milestone.

## M17 Lobby / Match Creation Foundation (2026-08-30)

- [x] Preserve M16 account/session/connection/player/reconnect ownership and gameplay protocol 3.
- [x] Add an independent Lobby protocol 1 and backend-replaceable `ILobbyService` boundary.
- [x] Implement authenticated Create/List/Join/Leave/Ready/Owner Start with 2-player atomic capacity.
- [x] Keep LobbyMatchId and gameplay MatchId separate through a MatchReservation.
- [x] Add bounded localhost Dedicated Server allocation and ready handshake.
- [x] Add account/match-bound, expiring, one-time MatchJoinTicket admission without plaintext reservation storage.
- [x] Transition both clients from Lobby to Hole01 and retain M12~M16 authoritative gameplay.
- [x] Add minimal Lobby UI, F2 telemetry, validators, builds, 24 EditMode tests, 7 PlayMode tests, real 4-process evidence, captures, and documentation.
- [x] Keep OfflineSingle default and Foundation.unity unchanged.
- [x] M17 closed without starting production Lobby, matchmaking, Relay/NAT, or production authentication.

## M18 Relay / NAT Traversal Prototype (2026-08-30)

- [x] Preserve M12–M17 authority, identity, Lobby, ticket, and reconnect boundaries.
- [x] Add `IMatchConnectivityProvider`, Direct mode, and a replaceable Relay descriptor.
- [x] Add a standalone local TCP relay-proxy and verify real forwarded traffic.
- [x] Keep private server bind data server-side in Relay mode.
- [x] Validate Relay credential before Authentication and MatchJoinTicket/ReconnectTicket admission.
- [x] Add bounded allocation, failure release, expiry, and parent-linked child cleanup.
- [x] Verify A/B natural shots, matching hashes, and Relay reconnect in separate processes.
- [x] Add settings, F2 telemetry, build/launch tools, 21 EditMode tests, 5 PlayMode tests, captures, and docs.
- [ ] Cross-NAT/WAN, production Relay, TLS, soak/network-quality gates, and production online remain future work.
- [ ] Do not start M19 in this milestone.

## M19 Production Relay Provider Integration / WAN Quality Gate (2026-08-30)

- [x] Preserve M12-M18 authority, identity, Lobby, ticket, reconnect, Direct default, and LocalRelay mode.
- [x] Pin unified Multiplayer Services 2.3.1 and isolate provider SDK references from gameplay runtime.
- [x] Add opt-in `ProductionRelay` allocate/join through Unity Relay with DTLS and no Direct fallback.
- [x] Bind the dedicated authority through Relay and gate the ready marker on provider establishment.
- [x] Keep provider credential, Authentication, MatchJoinTicket, and ReconnectTicket as separate ordered gates.
- [x] Verify real cloud allocation in `asia-northeast1`, two clients, A/B shots, final matching hash, and reconnect generation 2.
- [x] Add generic failure mapping/redaction, timeout/retry policy, temporary payload consumption, validator, tests, builds, logs, captures, and quality-gate docs.
- [ ] Cross-NAT, public Lobby, WAN Profiles B/C, 5-cycle lifecycle, 30-minute soak, Profiler, bandwidth/cost, and production operations are not complete.
- [x] Stop at M19; no M20 implementation was started.

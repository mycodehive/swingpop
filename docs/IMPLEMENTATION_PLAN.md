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

M6 카메라의 수동 시각/멀미/가림 검증을 완료한다. M7 Character는 별도 요청 전까지 시작하지 않는다.

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

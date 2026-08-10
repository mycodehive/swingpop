# SwingPop Implementation Plan

## Current State

2026-08-10 M0 Repository Audit 기준:

- Repository root: `C:/Users/Dodari/Documents/GitHub/swingpop`
- Git: `main` 브랜치의 신규 Repository이며 아직 commit이 없다. 감사 시점에는 전체 프로젝트가 untracked였고 `.gitignore`가 없었다.
- Unity project: `Assets/`, `Packages/`, `ProjectSettings/`가 있는 정상 Unity 프로젝트다.
- Unity Editor: `6000.5.7f1` (`ProjectSettings/ProjectVersion.txt` 기준)
- Render pipeline: Universal Render Pipeline `17.5.0`; `GraphicsSettings.asset`의 active pipeline은 `Assets/Settings/PC_RPAsset.asset`을 참조한다.
- Input: Input System `1.20.0`; Player Settings의 Active Input Handling은 새 Input System이며 템플릿 `InputSystem_Actions.inputactions`가 존재한다.
- Cinemachine: 설치되어 있지 않다. M0에는 필요하지 않으며 M6에서 실제 호환 버전과 필요성을 다시 판단한다.
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

M1 수동 입력 검증을 완료한 뒤에만 `prompts/milestones/03-M2-aim-power-impact.md`를 진행한다.

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

## M1 Manual Play Validation — Pending

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

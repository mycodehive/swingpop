# SwingPop Architecture v0.1

## Goal

아키텍처의 목적은 복잡한 프레임워크를 만드는 것이 아니라 Gameplay, Presentation, Data를 분리해 빠른 반복 튜닝이 가능하게 만드는 것이다.

## High-Level

```text
Input
  ↓
ShotInputController
  ↓
ShotFlow / State Machine
  ↓
ShotCommand
  ↓
ShotCalculator
  ↓
BallSimulation
  ↓
Ball Events
  ├─ CameraDirector
  ├─ HUD
  ├─ VFX
  ├─ Audio
  └─ Hole/Game Flow
```

## Modules

### Core

책임:
- game flow
- shot state
- hole state
- restart

후보:
- GameFlowController
- ShotStateMachine

### Gameplay/Shot

책임:
- aim
- power
- impact
- spin selection
- ShotCommand 생성

### Gameplay/Ball

책임:
- launch
- airborne update
- bounce
- roll
- stop
- in-hole transition

물리 계산과 MonoBehaviour presentation은 가능한 한 분리한다.

M1 구현:

- `GolfBallController`: Rigidbody 설정, launch/reset command, collision 기반 상태 전이의 단일 소유자
- `BallState`: `Ready`, `Airborne`, `Bouncing`, `Rolling`, `Stopped`
- `BallStopDetector`: Unity lifecycle과 분리된 누적 시간 기반 순수 정지 판정
- `BallTuningData`: launch/Rigidbody/roll/stop tuning의 ScriptableObject source of truth
- `TemporaryBallInput`: M1 전용 Space/R command adapter. Aim/Power/Impact는 포함하지 않음

### Gameplay/Club

ScriptableObject 중심.

### Gameplay/Wind

Wind state를 제공한다.
Ball이 UI를 직접 읽지 않는다.

### Gameplay/Course

Terrain surface query 및 Hole 관련 정보 제공.

### Camera

CameraDirector가 현재 게임 상태에 맞는 카메라 모드를 선택한다.

Gameplay Controller가 개별 Cinemachine Camera 세부 설정을 직접 만지지 않는다.

M1의 `BallFollowCamera`는 Cinemachine 없이 동작하는 최소 presentation component다.
Ball physics나 game flow를 소유하지 않고 target position만 `LateUpdate`에서 부드럽게 추적한다.

### UI

UI는 게임 상태를 표시하고 사용자 입력을 전달한다.
핵심 물리 계산을 포함하지 않는다.

### Character

CharacterAnimationController 또는 Adapter를 통해 Animator를 제어한다.

### Audio/VFX

Gameplay Event를 구독하는 presentation layer로 유지한다.

## Data Objects

예상:

- ClubData
- TerrainSurfaceData
- WindTuningData
- BallTuningData
- CameraTuningData
- VfxProfile

## Future Networking Seam

향후:

```text
Client Input
→ ShotCommand
→ Authority
→ Simulation
→ ShotResult
→ Clients
```

따라서 `ShotCommand`를 명확한 값 객체로 유지한다.

현재 네트워크 코드는 작성하지 않는다.

## Scene Strategy

현재 M1:

- `Assets/_Game/Scenes/Foundation.unity`: M1 Ball Launch 검증 scene
- `GolfBall.prefab`: Rigidbody, SphereCollider, `GolfBallController`가 연결된 교체 가능한 placeholder
- `M1 Systems`: 임시 입력과 debug telemetry 연결
- M0 `FoundationInputProbe` instance는 scene에서 제거했고 prefab asset만 보존한다.

향후 권장:

- `Bootstrap` (필요한 경우)
- `Hole01_SkyIsland`

프로젝트 규모가 작을 때 불필요하게 Scene을 많이 분리하지 않는다.
M1 이후 실제 gameplay scene이 준비되면 Foundation probe는 제거하거나 Debug 전용 scene으로 유지한다.

## Dependency Direction

권장:

```text
Data ← Gameplay ← Flow
          ↑
Presentation(UI/Camera/VFX/Audio)
```

Presentation이 Gameplay를 소유하지 않는다.

## Events

이벤트 예:

- ShotPrepared
- PowerConfirmed
- ImpactConfirmed
- BallLaunched
- BallLanded
- BallStopped
- BallHoled
- HoleCompleted
- PerfectShot

이벤트 버스 하나로 모든 것을 연결하지 않는다.
명시적인 C# event 또는 작은 범위의 signal 구조를 우선한다.

## State Ownership

Shot State는 한 곳에서 소유한다.

UI, Camera, Character는 state를 관찰하거나 명시적 command/event를 받는다.

## Testing Boundary

순수 로직 후보:

- Power normalization
- Impact grade mapping
- Club calculation
- Wind force abstraction
- Terrain modifiers
- Score calculation

Unity Component 통합:

- Ball Rigidbody
- Collider surfaces
- Camera
- Animator
- HUD

M1 EditMode test:

- `BallStopDetectorTests`: stable duration 누적, airborne reset, speed threshold 거부

## M2 Implemented Shot Boundary

```text
Input System
  → ShotInputController
  → ShotFlowController (single state owner)
  → ShotCalculator (pure calculation)
  → ShotCommand (serializable value)
  → GolfBallController / M1 Rigidbody physics

ShotDebugOverlay ← reads ShotFlowController + GolfBallController
```

- `ShotFlowController`만 M2 shot state를 전이한다. Ball state는 계속 `GolfBallController`가 소유한다.
- `ShotCommand`에는 scene object, UI, camera reference가 없으며 향후 기록/네트워크 경계로 사용할 수 있다.
- `ShotTuningData`와 `BallTuningData`를 분리해 shot 입력 감각과 ball physics를 독립 조정한다.
- Debug overlay와 aim line은 presentation/debug 계층이며 gameplay 계산을 변경하지 않는다.
- M1 `TemporaryBallInput` asset은 기록상 남아 있지만 Foundation scene에서는 `ShotInputController`로 교체되어 중복 입력하지 않는다.

M2 EditMode test:

- aim clamp, power normalization, impact grade 경계
- deterministic dispersion
- `ShotCommand` 값 캡처 및 MISS power/direction 반영

## M3 Arcade Flight Boundary

```text
ShotCommand + ShotSpin
  → GolfBallController (Rigidbody/state owner)
      → BallFlightModel (air force + decay, pure)
      → BallSpinState (current decaying spin)
      → BallGroundResponse (landing/roll modifier, pure)

BallTrajectoryDebug + ShotDebugOverlay ← read-only presentation/debug
```

- `ShotSpin.VerticalSpin`: `-1=Back`, `0=None`, `+1=Top`; `SideSpin`: `-1=Left`, `+1=Right`.
- `GolfBallController`는 lifecycle, collision, Rigidbody 적용과 기존 BallState만 소유한다.
- 공중/착지/rolling 계산은 별도 순수 모델로 분리되어 EditMode test가 가능하다.
- Physics Material은 vertical bounce, custom landing response는 planar spin 효과만 담당한다.
- Wind는 `BallFlightModel.CalculateAirAcceleration`의 `externalAcceleration` seam만 있고 실제 provider는 M4까지 없다.
- trajectory와 overlay는 gameplay 결과를 변경하지 않는다.

## M4 Wind / Terrain Boundary

```text
WindDebugInputController → WindController ← WindTuningData
                              ↓ read-only
                       GolfBallController

Collider → TerrainSurface → TerrainSurfaceData
                              ↓ contact query
                       GolfBallController
                              ↓ current lie modifier
                       ShotFlowController → ShotCommand

ShotDebugOverlay + WindDebugVisualizer ← read-only state
```

- `WindController`만 current wind preset/direction/strength를 소유한다. UI/debug는 값을 복제하지 않는다.
- `WindPhysics`는 travel direction 기준 head/tail과 crosswind 가속도를 계산하는 순수 함수다.
- `TerrainSurface`는 collider와 data의 adapter이며 type별 수치는 `TerrainSurfaceData`에만 있다.
- `TerrainResponse`는 power, rolling, bounce, spin modifier를 계산하는 순수 경계다.
- `GolfBallController`는 collision/trigger lifecycle, current lie, hazard 종료를 소유한다. Water/OOB scoring은 소유하지 않는다.
- `ShotCommand.SurfacePowerModifier`는 향후 lie/club 계산 확장을 위한 직렬화 seam이다.
- Surface별 bounce는 충돌 직전 하강 속도에 Ball base retention과 surface modifier를 적용해 PhysX callback 순서에 의존하지 않는다.

## M5 Hole / Scoring Boundary

```text
ShotFlowController --ShotCommitted--> HoleFlowController --> HoleProgressTracker
        |                                      |                    |
        |                                      |                    +--> ScoreCalculator
        v                                      v
  ShotCommand + ClubData              next position / lie / club
        |                                      |
        +--------------> GolfBallController <--+
                               |
Collider trigger --> CupCaptureController --> HoleFlowController.TryCompleteHole

ShotDebugOverlay ← read-only HoleFlow / ShotFlow / Ball state
```

- `HoleFlowController`만 stroke, penalty, last valid position, next-shot setup, hole state와 result 전이를 소유한다.
- `HoleProgressTracker`, `ScoreCalculator`, `CupCaptureRules`, `ClubShotCalculator`는 MonoBehaviour lifecycle과 분리된 순수 로직이다.
- `GolfBallController`는 `Ready/Airborne/Bouncing/Rolling/Stopped/Holed` 물리 상태와 Rigidbody만 소유한다. Score UI나 hole result를 계산하지 않는다.
- `ShotFlowController`는 입력 상태와 `ShotCommand` 생성만 소유하며 `HoleFlowController`가 선택한 ClubData를 command 값으로 캡처한다.
- `CupCaptureController`는 trigger adapter다. Green/거리/속도/높이 조건을 순수 규칙에 전달하고 성공 시 HoleFlow에 완료 command를 보낸다.
- Water/OOB detection은 계속 Ball에 있고, penalty/recovery 정책은 HoleFlow가 맡는다.
- `ValidationRequestRunner`는 Game View가 shortcut을 소비하는 환경에서 PlayMode 검증을 시작하는 Editor-only 도구이며 player build에 포함되지 않는다.

## M6 Camera Director Boundary

```text
ShotFlowController ----state/commit event---+
GolfBallController ----state/velocity event-+--> CameraDirector --> Main Camera pose/FOV
HoleFlowController ----state/hole event------+        |
                                                   CameraTuningData

ShotDebugOverlay <--- read-only camera telemetry
```

- `CameraDirector`만 active Main Camera pose와 FOV를 쓴다. M1 `BallFollowCamera`는 교체 이력을 위해 남겨 두되 scene에서는 disabled다.
- `CameraModeStateMachine`, `CameraMath`, `CameraPose`, `CameraFramingSolver`는 mode/transition/framing 계산을 MonoBehaviour lifecycle과 분리한다.
- Camera는 Shot/Ball/Hole 상태를 관찰할 뿐 상태 전이, physics, stroke, result를 소유하지 않는다.
- 모드별 offset/FOV/hold/transition/follow/collision 값은 `CameraTuningData`에서만 조정한다.
- Geometry 회피는 target-to-camera sphere cast이며 trigger는 무시한다. 향후 복잡한 occluder가 필요할 때 layer를 전용 CameraCollision layer로 분리할 수 있다.
- Cinemachine은 설치하지 않았다. 여러 virtual camera rig, rail, procedural composition 요구가 생길 때 Unity 6 호환 버전을 다시 평가한다.

## M7 Character / Animation Boundary

```text
ShotFlowController --state/commit/club event--> CharacterGolfController
        |                                             |
        |                                             v
        |                              CharacterAnimationController
        |                                             |
        |                                  single Impact signal
        |                                             v
        +<---------------------- TryLaunchCommittedShot()
                                                      |
GolfBallController --launch/state event---------------+
        |
        +--> CameraDirector (Impact/BallFollow)
        +--> Character FollowThrough/WatchBall

HoleFlowController --HoleCompleted--> Character celebration hook
CharacterTuningData -------------> placement/timeline/fallback/socket
```

- `ShotFlowController`가 ShotCommand와 pending Ball launch를 계속 소유한다. Character는 물리 velocity나 stroke를 계산하지 않는다.
- `CharacterGolfController`는 gameplay event를 character state로 매핑하는 adapter이며 Animator state 문자열을 Shot/Ball/Camera 코드에 노출하지 않는다.
- `CharacterAnimationController`만 Animator/procedural pose 실행과 Impact marker single-fire를 소유한다. 현재 prefab은 procedural placeholder이며 향후 clip의 Animation Event가 동일 method를 호출할 수 있다.
- `CharacterPresentation`은 address transform, primitive rig, `ClubSocket`, Driver/Putter visual 교체 지점만 소유한다.
- Character adapter가 연결되지 않은 scene은 즉시 launch compatibility를 유지한다. 연결된 adapter의 Impact가 누락되면 `ShotImpactDelayGuard`가 제한시간 후 launch해 soft lock을 방지한다.
- Camera는 캐릭터를 제어하지 않으며 실제 `Ball.Launched` event에서 Impact mode로 전환한다.

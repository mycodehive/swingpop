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

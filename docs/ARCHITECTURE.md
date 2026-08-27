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

## M8 Gameplay HUD Boundary

```text
ShotFlowController --state/commit/club/spin event--+
GolfBallController --state/hazard event------------+--> GameplayHudPresenter --> GameplayHudView
WindController -----wind event---------------------+             |                    |
HoleFlowController -stroke/state/result event------+             |                    +--> uGUI widgets
                                                                HudTuningData          +--> Gauge/Popup/Result views

Primary uGUI Button --> GameplayHudPresenter --> ShotFlowController.ConfirmCurrentStep()
Keyboard Space ------> ShotInputController -----> same command
```

- Unity 6000.5.7f1 프로젝트에 이미 포함된 uGUI 2.5.0을 사용한다. TMP나 추가 UI package는 설치하지 않았다.
- `GameplayHudPresenter`는 gameplay source of truth의 event를 구독하며 physics, score, wind, lie 값을 다시 계산하지 않는다.
- `GameplayHudView`, `HudGaugeView`, `HudPopupView`, `HudResultView`는 표시와 작은 presentation motion만 소유한다.
- Power/Impact cursor와 world-to-screen Aim marker, 이동 중 거리만 frame update한다. Hole/Club/Spin/Wind/Stroke/Result는 event 기반으로 갱신한다.
- Impact zone 폭은 `ShotFlowController.Tuning`의 Perfect/Great/Good threshold를 직접 읽어 실제 gameplay 판정과 일치한다.
- Primary button과 Space는 모두 `ConfirmCurrentStep()`을 호출하며 같은 frame 중복 confirm은 ShotFlow가 차단한다.
- `GameplayHUD.prefab`은 scene dependency가 없는 교체 가능한 presentation asset이고, Foundation instance가 Shot/Ball/Wind/Hole/Main Camera를 명시적으로 참조한다.
- Canvas는 1920×1080 reference, width/height match 0.5, corner anchor와 full-screen `Safe Area` wrapper를 사용한다.
- 기존 `ShotDebugOverlay`와 trajectory/aim debug는 독립적으로 유지되며 `H`/`F1`로 표시를 전환한다.

## M9 Presentation / Audio Boundary

```text
CharacterAnimationController --Impact marker--> ShotFlowController.TryLaunchCommittedShot()
                                                    |
                                                    v
GolfBallController --Launched/SurfaceContact/Hazard/State event--+
ShotFlowController --ShotCommitted-------------------------------+--> ShotPresentationController
HoleFlowController --HoleCompleted-------------------------------+          |
                                                                           +--> ImpactVfxController
                                                                           +--> BallTrailController
                                                                           +--> LandingVfxController
                                                                           +--> HoleInVfxController

Character/Ball/Hole events --> GameplayAudioController --> category AudioSources
Presentation controllers <-- ShotPresentationTuningData
```

- `Ball.Launched`가 Camera Impact, HUD grade, impact VFX, impact audio의 실제 동기화 지점이다. `ShotCommitted`는 command/profile을 준비만 한다.
- `ImpactPresentationGate`는 commit 하나가 launch presentation 하나만 소비하도록 하며 물리 launch 판단은 계속 ShotFlow/Character boundary에 남긴다.
- `BallSurfaceContact`는 presentation용 immutable event data다. `GolfBallController`는 ParticleSystem이나 AudioClip을 알지 못한다.
- `ShotPresentationResolver`는 ImpactGrade, TerrainSurfaceType, ScoreResult를 presentation level/effect/audio cue로 매핑하는 순수 경계다.
- VFX component는 scene에 있는 ParticleSystem/TrailRenderer를 재설정·재사용한다. shot마다 GameObject/Material을 생성하거나 제거하지 않는다.
- `GameplayAudioController`는 cue dispatch와 category source 선택만 소유한다. gameplay state를 변경하지 않고 최종 clip은 tuning asset slot으로 교체한다.
- CameraDirector, GameplayHudPresenter, CharacterGolfController는 서로를 직접 호출하지 않으며 동일 gameplay event를 각자 관찰한다.

## M10 Vertical Slice Environment Boundary

```text
Hole01_SkyIsland scene
├─ M1–M9 gameplay/presentation graph (source of truth)
├─ TerrainSurfaceData + gameplay colliders
└─ M10 Sky Island Art (no gameplay collider)
   ├─ shared materials / reusable environment prefabs
   ├─ SkyIslandEnvironmentMotion (clouds + windmill only)
   └─ SkyIslandAmbienceController --> replaceable ambient clip hook
```

- Foundation을 복제한 별도 scene만 art integration 대상으로 삼아 회귀 기준 씬을 보존한다.
- 시각 material은 `TerrainSurfaceData`의 physics/lie 판정을 소유하지 않는다.
- 반복 tree/flower/cloud/island/windmill은 prefab과 shared material을 사용하며 renderer별 material instance를 만들지 않는다.
- 모든 장식 primitive collider는 제거한다. gameplay surface/hazard/cup collider만 M1–M9 flow에 참여한다.
- 환경 동작은 단일 coordinator가 구름과 풍차를 갱신하며 Camera/HUD/GameFlow를 참조하지 않는다.
- M10 전용 Camera/HUD/ShotPresentation tuning clone은 Foundation tuning asset을 변경하지 않는다.

## M11 Presentation Polish Boundary

```text
M1–M10 gameplay/data/event graph (unchanged)
                |
                +--> Hole01 gameplay colliders / TerrainSurfaceData
                |
                +--> M11 Visual Polish (Renderer/Mesh only, Collider 0)
                +--> M11CameraTuning (Hole01 instance reference)
                +--> M11 shared materials + isolated skybox
```

- organic course mesh, flower accent와 character silhouette addition은 presentation-only object다.
- 시각 mesh는 TerrainSurface type, score, physics, camera state 또는 shot input을 소유하지 않는다.
- 기존 rectangular gameplay collider와 hazard volume만 physics source of truth로 유지한다.
- M11 builder는 Hole01 instance override와 교체 가능한 asset을 생성하며 Foundation scene/tuning을 변경하지 않는다.
- capture/quality validation 도구는 Editor assembly에만 있고 runtime build의 game flow에 참여하지 않는다.

## Character Identity Presentation Boundary

```text
Final Humanoid FBX / Avatar / Animator
                  |
          CharacterVisualAdapter
          /       |        \
 CharacterVisualProfile  Sockets  CharacterAnimationController
          |                   |                |
 bounds/scale/ground/       ClubVisual     hashed state contract
 framing metadata                           + single Impact gate
```

- `CharacterVisualAdapter`가 visual root, Animator/Avatar, left/right hand, ClubSocket, ImpactAnchor, HeadLookTarget를 Inspector reference로 받는다. vendor bone name/path 탐색은 하지 않는다.
- `CharacterVisualProfile`은 visual height/bounds, scale, ground/address/camera/socket offset과 portrait hook만 보유한다. gameplay 수치는 포함하지 않는다.
- `CharacterAnimatorContract`가 모든 CharacterState 이름과 hash를 중앙 관리한다. Shot/Ball/Camera code는 clip 이름을 알지 않는다.
- valid Humanoid Avatar와 Controller가 모두 있을 때 Animator mode, 없거나 invalid하면 procedural fallback을 사용한다.
- Swing/Putt Impact는 Animation Event가 우선이며 normalized fallback과 Shot guard가 누락을 보완한다. 모든 경로는 기존 single-fire gate를 거쳐 Ball을 한 번만 launch한다.
- `HumanoidGolferTemplate.prefab`은 최종 mesh 없이 integration hierarchy와 socket seam만 제공한다. `PlaceholderGolfer.prefab`의 gameplay root/controller는 보존한다.
- profile의 camera metadata와 M11 tuning 조정은 framing reference일 뿐 Camera state machine을 변경하지 않는다.

## HUD Skin Presentation Boundary

```text
Gameplay source events
  ShotFlow / Ball / Wind / Hole
              |
              v
    GameplayHudPresenter
              |
              v
      GameplayHudView
       /      |      \
 HudGauge  HudPopup  HudResult
       \      |      /
          HudSkinData
              |
      shared sprites/colors
```

- `GameplayHudPresenter`, `GameplayHudView`, `HudGaugeView`, `HudPopupView`, `HudResultView`, `HudPresentationMapper`, `M8HudTuning`, Safe Area, CanvasScaler와 Shot Button command path를 보존한다.
- `HudSkinStyleMapper`는 기존 ImpactGrade, TerrainSurfaceType, ScoreResult, ShotFlowState를 presentation tone으로만 변환한다. gameplay value, threshold, score, physics를 계산하지 않는다.
- `HudSkinData`는 palette와 shared sprite reference만 소유하며 gameplay data를 포함하지 않는다.
- `GameplayHUD_SwingPopSkin.prefab`은 Hole01 전용 isolated visual prefab이다. 기존 `GameplayHUD.prefab`과 `Foundation.unity`는 변경하지 않는다.
- Skin HUD는 Canvas 1, default shared UI material 1, layout component 0, per-frame update behaviour 1을 유지한다. Shot Button만 raycast target이다.
- Editor builder, validator, capture tool은 runtime gameplay graph에 참여하지 않는다.

## M12 Online Multiplayer Foundation Boundary

```text
Local/Remote intent
      |
      v
IMatchTransport <-> IMatchAuthority
      |
 ApprovedShot / MatchSnapshot
      |
      v
Existing ShotFlow -> Character -> Ball -> HoleFlow
      |
 Existing Camera / HUD / VFX / Audio events
```

- 기본 실행은 `OfflineSingle`이며 기존 Hole01 shot flow를 그대로 사용한다.
- `LocalTwoPlayer`에서만 `IShotCommitGate`가 승인 전 발사를 차단한다.
- Authority는 match/player/turn/sequence/version, duplicate, 수치 범위와 lie별 club을 검증한다.
- Transform을 매 프레임 전송하지 않는다. 승인된 `ShotCommand`, gameplay가 계산한 `NetworkShotResult`, versioned `MatchSnapshot`만 동기화한다.
- Player A/B는 각각 ball position, last valid position, lie, strokes, penalties, holed 상태를 소유한다. 하나의 visual ball은 현재 turn player 상태로 복원된다.
- Rigidbody lockstep determinism은 보장하지 않는다. Production authority/result verification은 별도 과제다.
- Camera, character, HUD, VFX, audio는 network DTO와 authority에 포함되지 않는다.
- 상세 설계와 위험은 `docs/M12_ONLINE_MULTIPLAYER_ARCHITECTURE.md`, 후속 범위는 `docs/TODO_ONLINE.md`를 따른다.

## VFX Hero Pass Presentation Boundary

```text
ShotCommand / ImpactGrade ──prepare──> ShotPresentationController
Ball.Launched ───────────────single sync point──────────────┐
Ball.SurfaceContact / Hazard ───────────────────────────────┼─> reusable VFX controllers
HoleFlow.HoleCompleted ─────────────────────────────────────┘
                                                            |
                    Impact / Trail / Landing / Hole-In / Audio
                                                            |
                             VfxHeroShotPresentationTuning.asset
```

- `ImpactGrade`의 기존 Normal/Great/Perfect 결과만 presentation profile로 매핑한다. threshold, shot command, launch velocity, spin, terrain, score는 재계산하지 않는다.
- `Ball.Launched`는 Camera kick, HUD popup, impact VFX, impact audio가 공유하는 실제 동기화 지점이다. VFX가 Aim 또는 physics를 다시 계산하지 않는다.
- `ImpactVfxController`, `BallTrailController`, `LandingVfxController`, `HoleInVfxController`, `GameplayAudioController`의 책임 분리를 유지한다.
- Hole01은 `ShotFeelPresentation_Hero.prefab`과 전용 tuning asset을 사용한다. 기존 M9 prefab과 `Foundation.unity`는 변경하지 않는다.
- 15개 ParticleSystem, 3개 TrailRenderer, 6개 shared VFX material은 scene에 한 번 생성되어 재사용된다. shot마다 effect object 또는 material을 만들지 않는다.
- Editor builder, preview, validator, capture/telemetry 도구는 runtime gameplay graph와 분리한다.

## M13 Real Network Transport Boundary

`UnityTransportMatchTransport` is a second `IMatchTransport` adapter beside M12's `LocalLoopbackTransport`. `NetworkHost` alone owns `LocalMatchAuthority` and the authoritative gameplay result. `NetworkClient` submits commands, waits for approval, plays approved commands for presentation, and accepts versioned host snapshots at the settle boundary.

No network dependency was added to Camera, Character, HUD, VFX, or Audio cores. See `docs/M13_REAL_NETWORK_TRANSPORT_ARCHITECTURE.md`.

## M14 Dedicated Authority Boundary

`DedicatedServerMatchTransport` is an additional `IMatchTransport` adapter; it does not replace M12 LocalLoopback or M13 Unity Transport Host/Client. The dedicated process has no player, assigns two remote player IDs, owns `LocalMatchAuthority`, executes approved commands through the existing Rigidbody Ball/HoleFlow graph without Character Animator, and alone publishes results/snapshots. Clients remain approved-playback and snapshot-correction consumers.

The shared Hole01 server scene disables presentation by explicit component type while keeping physics colliders. Protocol version 2 and M13 DTO/interfaces remain compatible. See `docs/M14_DEDICATED_AUTHORITY_ARCHITECTURE.md`.
# M15 Match Lifecycle Addendum

M15 adds an orthogonal server-owned dedicated lifecycle (`Playing`, `ReconnectGrace`, `Aborted`, `Ended`) and per-player connection lifecycle (`Connected`, `ReconnectGrace`, `Expired`). A disconnect no longer deletes the slot or aborts immediately. `ReconnectSessionRegistry` retains only a bounded in-memory SHA-256 ticket mapping; `ReconnectController` is a thin client session adapter. Gameplay authority and snapshot restoration remain in the M12-M14 boundaries. See `M15_MATCH_LIFECYCLE_RECONNECT_ARCHITECTURE.md`.

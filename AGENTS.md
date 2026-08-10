# AGENTS.md — SwingPop

## 1. Project Goal

SwingPop은 밝고 화려한 Anime / Stylized 3D 비주얼을 가진 캐주얼 판타지 골프 게임이다.

현재 개발 목표는 전체 온라인 서비스가 아니라 **1개 Hole의 상용 수준 Vertical Slice**다.

핵심 성공 기준:

- 조준이 이해하기 쉽다.
- Power / Impact 입력이 재미있다.
- 공을 때리는 순간 타격감이 있다.
- 공 비행 궤적이 명확하다.
- 카메라가 역동적이지만 멀미를 유발하지 않는다.
- 착지, Bounce, Roll이 만족스럽다.
- UI가 밝고 즉시 읽힌다.
- 다시 한 번 공을 치고 싶다.

## 2. Quality Reference

그래픽 및 화면 구성의 Quality Bar:

`docs/reference/target-quality.png`

이 이미지는 다음 항목의 품질 기준으로만 사용한다.

- 화면 밀도
- 캐릭터와 카메라의 상대적 크기
- 밝고 화려한 색감
- 스타일라이즈드 환경
- HUD 정보량
- Aim / Distance / Wind 표현
- Power / Shot UI의 존재감
- VFX와 화면 반응성

기존 게임의 고유 캐릭터, UI, 맵, 로고, 아이콘, 명칭, 사운드, 연출을 복제하지 않는다.
SwingPop은 Original IP로 제작한다.

## 3. Technology Direction

- Engine: Unity
- Language: C#
- Rendering: URP
- Input: Unity Input System
- Camera: Cinemachine 사용 가능
- Animation: Unity Animator
- VFX: Particle System 우선, 필요 시 Shader Graph / VFX Graph
- Data: ScriptableObject 적극 활용
- Initial Target: PC 1080p / 60 FPS
- Future: Android / iOS 확장을 막지 않는 구조

현재 프로젝트의 Unity / Package 버전을 먼저 확인한 뒤 호환되는 API를 사용한다.
Deprecated API를 임의로 사용하지 않는다.

## 4. Development Order

반드시 아래 순서를 지킨다.

1. Project Foundation
2. Ball Launch
3. Aim / Power / Impact
4. Ball Flight / Bounce / Roll
5. Wind / Terrain
6. Hole / Scoring
7. Camera Director
8. Character Animation
9. HUD
10. VFX / Audio
11. Hole 1 Vertical Slice
12. Polish / Quality Gate

현재 단계가 완료되지 않은 상태에서 다음 대형 시스템으로 넘어가지 않는다.

## 5. Do Not Implement Yet

M11 완료 전 다음 기능을 구현하지 않는다.

- Online multiplayer
- Account/login
- Matchmaking
- Shop
- Economy
- Ranking
- Guild
- Season
- Battle Pass
- Character collection
- Pet gameplay
- Complex skill system
- Multiple courses
- Live service backend

향후 네트워크화를 위해 `ShotCommand` 같은 데이터 구조를 직렬화 가능한 형태로 설계하는 것은 허용한다.

## 6. Architecture Rules

### Gameplay와 Presentation 분리

예:

- Shot 계산 코드가 UI를 직접 조작하지 않는다.
- Ball Physics가 Score UI를 직접 변경하지 않는다.
- Camera가 Game Flow를 소유하지 않는다.
- Animator State를 Gameplay 코드 곳곳에서 문자열로 호출하지 않는다.

### 권장 책임 분리

- Core: Game flow / state
- Gameplay: Shot / Ball / Club / Wind / Terrain / Hole
- Character: Character presentation / animation
- Camera: Camera modes / transitions
- UI: HUD / gauges / result presentation
- Audio: event-driven playback
- Data: ScriptableObject definitions
- Debug: trajectory and telemetry

### 금지

- God Object
- 한 클래스에 모든 게임 로직 집중
- 수십 개 bool로 게임 상태 관리
- Magic Number 남발
- Scene object 이름을 Find로 반복 탐색
- Public mutable field 남발
- Gameplay 값 hard coding
- Package/API 버전 추측

## 7. State Machine

샷 흐름은 명시적 상태로 관리한다.

예:

- Preparing
- Aiming
- PowerSelecting
- ImpactSelecting
- Swinging
- BallFlying
- BallBouncing
- BallRolling
- BallStopped
- HoleComplete

구체 구현은 개선 가능하지만 상태 전이의 단일 책임 지점을 유지한다.

## 8. Data Rules

클럽, 바람 밸런스, Terrain, VFX 튜닝 값은 가능한 한 데이터로 분리한다.

예:

`ClubData`
- Name
- Type
- BasePower
- Loft
- Accuracy
- Spin
- CarryModifier
- RollModifier

`TerrainSurfaceData`
- SurfaceType
- PowerModifier
- Friction
- Bounce
- SpinResponse

## 9. Ball Physics Policy

초기 구현은 Rigidbody 기반 Arcade Physics다.

목표는 실제 골프 시뮬레이션 정확도가 아니라:

**Predictable + Readable + Fun**

지원 대상:

- Launch velocity
- Gravity
- Wind
- Drag
- Top spin
- Back spin
- Side spin
- Bounce
- Rolling
- Terrain friction
- Stop detection
- In-hole detection

향후 Custom Ballistics Solver로 교체할 수 있도록 계산 책임을 분리한다.

## 10. Placeholder Policy

최종 Art가 없어도 개발을 멈추지 않는다.

사용 가능:

- Capsule character
- Sphere golf ball
- Primitive terrain
- Simple materials
- Temporary HUD
- Placeholder audio
- Placeholder particles

단 모든 Placeholder는 실제 Asset으로 쉽게 교체되도록 Prefab/Adapter 기반으로 만든다.

교체가 필요한 항목은 `docs/TODO_ART.md`에 기록한다.

## 11. Folder Rules

게임 코드/리소스는 가능하면 아래 구조로 정리한다.

```text
Assets/
└─ _Game/
   ├─ Art/
   │  ├─ Characters/
   │  ├─ Courses/
   │  ├─ Props/
   │  ├─ UI/
   │  └─ VFX/
   ├─ Audio/
   ├─ Animations/
   ├─ Materials/
   ├─ Prefabs/
   │  ├─ Characters/
   │  ├─ Golf/
   │  ├─ Course/
   │  ├─ UI/
   │  └─ VFX/
   ├─ Scenes/
   ├─ Scripts/
   │  ├─ Core/
   │  ├─ Gameplay/
   │  │  ├─ Ball/
   │  │  ├─ Shot/
   │  │  ├─ Club/
   │  │  ├─ Course/
   │  │  ├─ Wind/
   │  │  └─ Hole/
   │  ├─ Character/
   │  ├─ Camera/
   │  ├─ UI/
   │  ├─ Audio/
   │  ├─ Data/
   │  └─ Debug/
   ├─ ScriptableObjects/
   ├─ Settings/
   └─ Tests/
```

기존 프로젝트 구조가 이미 합리적이면 무리하게 전부 이동하지 않는다.

## 12. Coding Rules

- 명확한 namespace 사용
- `[SerializeField] private` 우선
- dependency는 Inspector / constructor-like initialization / explicit setup으로 전달
- 계산 로직은 MonoBehaviour lifecycle과 가능한 한 분리
- 불필요한 Singleton 금지
- 이벤트 구독/해제 명확히 관리
- Null Reference 방지
- Runtime allocation을 무의미하게 반복하지 않는다
- 불필요한 Update 남발 금지
- 주요 튜닝값에 Tooltip / Header 등 Inspector 가독성 제공
- 복잡한 수식에는 왜 그렇게 계산하는지 주석 작성
- 코드보다 이름으로 의도가 읽히게 한다

## 13. Debug Requirements

개발 중 다음 기능을 지원하는 방향으로 설계한다.

- Show trajectory
- Show launch vector
- Show wind vector
- Show predicted landing point
- Show ball speed
- Show spin
- Show current terrain surface
- Show shot data
- Reset ball
- Force perfect shot
- Quick restart hole

Debug 기능은 Release presentation과 분리한다.

## 14. Testing Rules

가능한 순수 계산 로직은 Unit Test 가능하게 작성한다.

우선 테스트 대상:

- Shot calculation
- Power normalization
- Impact accuracy mapping
- Wind calculation
- Club modifiers
- Terrain modifiers
- Score calculation
- State transitions where practical

Scene 통합이 필요한 기능은 PlayMode validation을 수행한다.

## 15. Compile / Validation Rules

한 milestone이 끝날 때:

1. 컴파일 오류가 없어야 한다.
2. Missing Script가 없어야 한다.
3. Inspector Missing Reference가 없어야 한다.
4. Scene에서 기능이 실제 연결되어야 한다.
5. 플레이 절차가 문서에 있어야 한다.
6. Console Error가 없어야 한다.
7. 알려진 문제를 숨기지 않는다.

CLI만으로 Unity PlayMode 확인이 불가능하면 그 사실을 명확히 보고하고, 사용자에게 필요한 Unity Editor 검증 절차를 구체적으로 작성한다.

실행하지 않은 테스트를 실행했다고 말하지 않는다.

## 16. Documentation Rules

구현 변경 시 관련 문서를 함께 갱신한다.

주요 문서:

- `docs/PRD.md`
- `docs/ARCHITECTURE.md`
- `docs/GAMEPLAY.md`
- `docs/ART_DIRECTION.md`
- `docs/ROADMAP.md`
- `docs/IMPLEMENTATION_PLAN.md`
- `docs/TODO_ART.md`

문서와 실제 코드가 충돌하면 실제 코드를 확인한 뒤 문서를 갱신한다.

## 17. Work Reporting Format

각 작업 종료 시 다음 형식으로 보고한다.

```markdown
## Completed
- ...

## Files Changed
- ...

## Validation
- ...

## Current Result
- ...

## Known Issues
- ...

## Next
- ...
```

## 18. Definition of Done

기능은 코드 파일 생성만으로 완료가 아니다.

완료 조건:

- Scene/Prefab에 실제 연결
- 사용자 입력으로 실행 가능
- 컴파일 가능
- Console Error 없음
- 설정 가능 값은 Inspector/Data로 노출
- 최소 검증 완료
- 관련 문서 갱신

## 19. Most Important Rule

코드 양을 성과로 취급하지 않는다.

SwingPop의 핵심 질문은 항상 다음이다.

> 공을 쳤을 때 타격감이 있고, 시원하게 날아가며, 카메라와 VFX가 반응하고, 착지가 만족스러워 다시 치고 싶은가?

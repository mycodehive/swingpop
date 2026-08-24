# Final Graphics Quality Gate

검토 기준일: 2026-08-24  
대상 Scene: `Assets/_Game/Scenes/Hole01_SkyIsland.unity`  
품질 기준: `docs/reference/target-quality.png`의 화면 밀도·가독성·색감·카메라 관계만 참고하며, 기존 IP의 고유 표현은 복제하지 않는다.

## Executive Summary

현재 Hole01은 Intro부터 Result까지 하나의 밝은 캐주얼 판타지 골프 경험으로 읽히는 **완성도 높은 placeholder vertical slice**다. Character, Course, HUD, VFX, Putt/Result가 같은 cyan/white/pink/navy 언어를 사용하고, Address·Power·Impact·Flight·Lie·Putt·Result의 정보 우선순위가 16:9 세 해상도에서 유지된다.

자동 Play Mode에서 19장의 1920×1080 master capture와 8장의 보조 해상도 capture를 생성했다. 실제 Hole-In, Character Reaction, Result reveal 이벤트가 각각 1회 발생했고 런타임 오브젝트 수는 전후 동일했다. 필수 구조 validator 7개, EditMode 139개, PlayMode 8개가 모두 통과했다.

현재 남은 큰 시각 차이는 코드나 primitive를 더 조정해서 해결할 문제가 아니다. 최종 Character FBX/animation, authored environment, licensed font와 UI art, authored VFX/audio가 필요하다. 따라서 placeholder polish는 여기서 멈추고 Final Asset Production으로 이동하는 것이 비용 대비 효과가 가장 높다.

## Gate Result

- **VERTICAL SLICE GRAPHICS: CONDITIONAL GO**
- **COMMERCIAL ART: NO-GO**
- 조건: Unity Editor에서 실제 한 홀 연속 플레이의 카메라 편안함, 오디오 밸런스, Console Error 0, 10초 이상 Profiler 기록을 사용자가 수동 확인해야 한다.
- 조건의 의미: 자동 검증에서 발견된 repository-fixable P0/P1은 없다. 미확인 항목은 정지 화면과 batch Play Mode가 증명할 수 없는 감각·성능 항목이다.

## Full Hole Review

| 구간 | 판정 | 핵심 관찰 |
|---|---|---|
| Hole Intro | B- | 코스 진행 방향과 섬 실루엣은 읽히나 gameplay HUD/aim이 함께 보여 cinematic 순도는 제한적이다. |
| Clean Address | B | Character/Ball/Fairway/Flag과 좌우 환경이 안정적으로 분리된다. |
| Aim | B | 방향 변화와 target marker가 읽히며 Character silhouette가 유지된다. |
| Power | B | 하단 gauge가 중심 행동으로 보이고 월드 시야를 과도하게 가리지 않는다. |
| Impact | B | timing zone/cursor/판정 문구의 계층이 명확하다. |
| Perfect Impact | B- | `PERFECT`는 강하지만 world impact art는 generated placeholder 한계가 보인다. |
| Normal / Perfect Flight | B- | 공과 trajectory가 배경에서 읽힌다. 두 등급의 authored visual 차이는 더 필요하다. |
| Landing | C+ | 상태 전환은 읽히지만 grass/sand/water 접촉의 재질감은 최종 particle art가 필요하다. |
| Fairway / Rough / Bunker / Water | B | 색·명도·HUD lie로 표면이 구분된다. Water master는 검증용 연출 프레임이며 실제 hazard 연속 흐름의 대체물이 아니다. |
| Green / Putter | B | Character, Ball, Cup, Flag, Green이 한 화면에 함께 보인다. |
| Putt / Cup Approach | B- | Ball-Cup 관계를 유지하지만 최종 Character 체형 기준 재프레이밍이 필요하다. |
| Hole-In / Reaction | B- / C+ | 순서와 주목점은 명확하다. celebration animation/VFX의 authored richness는 부족하다. |
| Result | B | score-first hierarchy와 Character-left/Card-right 구성이 명확하다. 최종 emblem/font가 필요하다. |

Master capture는 `docs/review-captures/final-graphics-quality-gate/`에 저장했다. 영상 capture는 생성하지 않았다.

## Highest Impact Issues

1. 최종 Character FBX와 authored animation 부재가 화면의 prototype 인상을 가장 크게 만든다.
2. terrain/vegetation/landmark가 production-friendly placeholder라 근접·중경의 재질 밀도가 상용 기준보다 낮다.
3. Perfect Impact, Landing, Hole-In의 world-space VFX가 gameplay 판독에는 충분하지만 hero moment의 감정적 보상은 약하다.
4. 현재 font/icon/panel은 기능적이고 일관되지만 branded commercial UI 수준은 아니다.
5. 실제 연속 플레이 카메라 comfort, 오디오 믹스, Development Build 성능은 정지 capture로 승인할 수 없다.

## Fixes Applied

- Final gate 전용 자동 Play Mode capture 도구를 추가해 19개 master 상태와 3개 해상도 핵심 상태를 재현 가능하게 만들었다.
- Water 검증 capture가 이전 lie 상태를 남기지 않도록 `WATER` 상태와 hazard feedback을 명시하고 불필요한 aim/action 표시를 제거했다.
- Flight와 Hole-In capture 시점을 조정해 ball/trajectory/cup event가 실제 재생 프레임에서 보이게 했다.
- 최종 구조 validator를 추가해 필수 validator 7개, scene 누락·중복, material slot, 성능 proxy, capture 해상도를 한 번에 검사한다.
- Putt/Result 패스가 정식으로 교체한 Camera tuning과 HUD prefab을 이전 M11/HUD validator가 허용하도록 검사기 호환성을 갱신했다.
- 런타임 gameplay, Foundation Scene, physics, score, input, online 기능은 변경하지 않았다.

## Character

등급: **B-**

- Address와 Result에서 silhouette, hair mass, cyan/white/pink/navy palette, Driver/Putter 차이가 읽힌다.
- Ball과 Cup을 가리는 치명적 framing은 master set에서 발견되지 않았다.
- 현재 segmented placeholder는 identity와 교체 seam 검증용이다. 얼굴, 손, 의상 재질, deformation, animation nuance는 상용 캐릭터 판단 대상이 아니다.
- `BLOCKED BY FINAL CHARACTER ASSET`: 최종 FBX, Avatar, proportions, authored idle/address/swing/putt/reaction clips가 필요하다.

## Course

등급: **B**

- Tee/Fairway/Rough/Green/Bunker/Water의 value hierarchy와 경로 안내가 일관적이다.
- Bunker와 Water는 특히 색과 형태로 즉시 구분된다.
- collider를 추가하지 않은 presentation layer 구조와 surface data 분리는 유지된다.
- `BLOCKED BY AUTHORED ENVIRONMENT ASSET`: sculpted mesh, final terrain material, cliff breakup, water shader가 필요하다.

## Environment

등급: **B-**

- Windmill, Waterfall island, cloud/floating island가 깊이와 fantasy identity를 만든다.
- foreground/midground/background 분리는 안정적이지만 foliage와 prop variation은 반복감이 남는다.
- final tree/flower/stone/landmark asset, LOD, authored ambient motion이 필요하다.

## HUD

등급: **B**

- Player/Hole/Wind/Club 정보와 Power/Impact/Result의 우선순위가 명확하다.
- 1920×1080, 1600×900, 1280×720 capture에서 핵심 clipping·overlap은 발견되지 않았다.
- `BLOCKED BY FINAL FONT`: 현재 typography는 functional placeholder다.
- `BLOCKED BY AUTHORED UI ASSET`: final portrait, 9-slice, icon, gauge, result emblem이 필요하다.

## VFX

등급: **C+**

- Normal < Great < Perfect의 크기·색·밝기 계층과 surface별 landing mapping은 존재한다.
- flight trail은 공을 가리는 굵은 막대가 아니며 sky/terrain 양쪽에서 판독된다.
- 정지 master에서 Perfect Impact와 Hole-In의 world effect는 HUD 문구보다 약하다.
- generated sprite를 더 확대하는 placeholder 조정은 overdraw와 화면 방해를 늘릴 가능성이 높아 중단한다.
- `BLOCKED BY AUTHORED VFX / AUDIO ASSET`: flipbook/shader/trail texture/surface particles/cup celebration과 licensed audio가 필요하다.

## Putt / Result

등급: **B-**

- Green에서 Putter 선택 시 Character/Ball/Cup/Flag/Green이 함께 보이는 이전 요구를 충족한다.
- rolling과 cup approach가 Ball-Cup 관계를 중심으로 유지된다.
- Hole-In → Character Reaction → Result 순서가 자동 Play Mode에서 각각 1회 실행됐다.
- Result는 score를 먼저 보여주고 secondary detail이 뒤따르는 구조다.
- master Result의 `ALBATROSS -3`은 composition 검증을 위한 실제 one-stroke putt setup 결과이며 전체 hole score 밸런스 샘플은 아니다.

## Camera

등급: **C+ (motion NOT VERIFIED)**

- 정지 프레이밍은 Intro/Address/Flight/Putt/Result에서 주 피사체와 진행 방향을 유지한다.
- Camera 1개와 central `CameraDirector` 구조가 유지되고 duplicate가 없다.
- transition smoothness, shake comfort, motion sickness, 실제 obstruction recovery는 수동 연속 플레이가 필요하다.
- 최종 Character FBX와 환경 mesh가 들어오면 offset/collision을 마지막으로 다시 맞춰야 한다.

## Performance

- 구조 proxy: GameObjects 570, Renderers 384/Active 121, Materials 88, Transparent Renderers 27/Active 21, Shadow Casters 145/Active 47.
- ParticleSystems 15, AudioSources 5, UpdateBehaviours 14, Cameras 1, Canvas 1, EventSystems 1.
- Missing Scripts 0, Missing Material Slots 0, duplicate HUD/Camera/EventSystem/AudioController 0.
- capture 전후 GameObject/Particle/Camera/Canvas/EventSystem/AudioSource 수가 동일했다.
- **Unity Profiler 실제 frame time, GC Alloc, Batches, SetPass, GPU, memory: NOT VERIFIED.**
- **PC Development Build 1080p/60 FPS: NOT VERIFIED.**

## Regression

- Unity 6000.5.7f1 batch EditMode: **139/139 PASS**.
- Unity 6000.5.7f1 batch PlayMode: **8/8 PASS**.
- Required validators: **7/7 PASS**.
  - M10 Vertical Slice
  - M11 Quality Gate
  - Course Environment
  - HUD Skin
  - VFX Hero
  - Character Identity
  - Putt / Result Cinematic
- batch capture/validation/test 로그의 critical compile/runtime pattern: 0.
- Foundation Scene SHA-1은 HEAD와 동일: `ff98a2c403448c9f4c60b551b5afa685e26f350f`.
- Unity GUI Console Error 0과 Inspector Missing Reference 전체 수동 확인: **NOT VERIFIED**.

## Resolution

| 상태 | 1920×1080 | 1600×900 | 1280×720 |
|---|---|---|---|
| Address | PASS | PASS | PASS |
| Power | PASS | PASS | PASS |
| Putt | PASS | PASS | PASS |
| Result | PASS | PASS | PASS |

자동 PNG decode, pixel dimension, 주요 safe-area와 clipping을 확인했다. 초광폭·4:3·모바일 notch는 이번 PC 16:9 vertical slice 범위가 아니다.

## Asset Boundary Matrix

| Area | Current Grade | Can Improve Now? | Requires Final Asset? | Recommended Action |
|---|---|---|---|---|
| Character | C | Limited | Yes | Final FBX/Avatar/authored clips 제작 후 `CharacterVisualAdapter`로 교체 |
| Character animation | C | Limited | Yes | Address/Swing/Putt/Reaction authored clip과 deformation review |
| Course materials | B | No | Yes | collider를 유지한 sculpted visual mesh와 final material 적용 |
| Vegetation / Props | C | No | Yes | reusable variants와 LOD budget을 포함해 제작 |
| Water / Waterfall | C | Limited | Yes | URP water/foam shader와 mobile fallback 제작 |
| Typography | C | No | Yes | licensed/localization-safe font와 한글·영문 overflow test |
| HUD panels / icons | B | Limited | Yes | 기존 view/presenter seam에 authored sprite를 교체 |
| Perfect Impact VFX | B | Limited | Partially | generated sprite 확대 대신 authored flipbook/shader 제작 |
| Landing VFX | C | Limited | Yes | surface mapping을 유지하고 grass/sand/water art만 교체 |
| Hole-In reward | B | Limited | Yes | coordinator timing을 유지해 VFX/animation/audio 교체 |
| Camera motion | C | Yes, after observation | Partially | full-hole 3회 수동 플레이 후 offset/shake만 소폭 수정 |
| Performance | Not graded | Yes | No | Development Build와 Profiler로 CPU/GPU/GC/Batches 기록 |
| Audio | Not graded | Validation only | Yes | licensed clip/AudioMixer 적용 후 headphone/speaker 승인 |

## Quality Scorecard

| 항목 | 등급 | 판정 근거 |
|---|---:|---|
| Hole Intro | B | 코스 소개는 명확, HUD 동시 노출은 cinematic 집중도를 낮춤 |
| Address | B | 플레이 준비 정보와 목표 방향이 즉시 읽힘 |
| Character readability | B | silhouette/palette는 명확, final anatomy/face 부재 |
| Course surfaces | B | 모든 gameplay surface가 명도·색·HUD로 구분됨 |
| Environment depth | B | landmark와 layer 분리가 안정적 |
| Vegetation | C | 기능적이나 variation/LOD/art density 부족 |
| Landmarks | B | 방향 표식은 강함, authored detail 필요 |
| Lighting | B | 밝고 일관되며 피사체를 잃지 않음 |
| HUD hierarchy | B | 행동과 정보의 우선순위가 안정적 |
| Typography | C | readable placeholder, final font 미적용 |
| Power | B | 중심 행동이 선명하고 월드 가림이 제한적 |
| Impact | B | zone/cursor/result hierarchy가 명확 |
| Perfect Impact | B | 판정 문구는 강함, world VFX는 약함 |
| Perfect Flight | B | ball/line 판독 가능, authored distinction 부족 |
| Ball readability | B | sky/terrain/green에서 대부분 유지 |
| Landing VFX | C | surface mapping은 맞으나 재질감 부족 |
| Putt | B | Ball/Cup 관계와 green reading 유지 |
| Cup Approach | B | 목표는 명확, 최종 체형 기반 framing 필요 |
| Hole-In | B | timing/순서는 명확, reward art 부족 |
| Character Reaction | C | 이벤트는 읽히나 placeholder animation 한계 |
| Result | B | score-first 구조와 좌우 composition이 좋음 |
| Camera Flow | C | 정지 framing 통과, 실제 motion 미검증 |
| Visual Cohesion | B | palette와 shape language 일관 |
| Game Feel | C | 시각 신호는 있음, 오디오/실시간 감각 미검증 |
| Game vs Prototype | C | coherent game slice이나 authored asset 부재가 명확 |
| Slice Completeness | B | Intro→Result 모든 핵심 시각 상태가 존재 |

## Remaining P0

- 자동 capture와 구조/회귀 검증에서 발견된 P0: **없음**.
- 단, 실제 Editor full-hole run에서 진행 중단, Console Error, 피사체 상실이 발견되면 즉시 P0로 재분류한다.

## Remaining P1

- repository에서 즉시 수정 가능한 확정 P1: **없음**.
- 승인 전 검증 P1: 실제 연속 플레이의 camera comfort/transition과 audio balance가 아직 `NOT VERIFIED`다.
- Perfect/Hole-In richness는 중요하지만 원인이 final authored asset 부재이므로 P1 코드 tweak이 아니라 asset production 항목으로 분리한다.

## Asset-Blocked Items

- `BLOCKED BY FINAL CHARACTER ASSET`: character anatomy, face, deformation, authored animation, 최종 camera retune.
- `BLOCKED BY AUTHORED ENVIRONMENT ASSET`: terrain/cliff/water/foliage/landmark의 commercial detail과 LOD.
- `BLOCKED BY FINAL FONT`: commercial typography, 한글/영문 localization과 overflow.
- `BLOCKED BY AUTHORED UI ASSET`: portrait, 9-slice, icon, gauge, result badge.
- `BLOCKED BY AUTHORED VFX / AUDIO ASSET`: impact/landing/hole-in effect, licensed SFX, ambience, mix/master.

## Stop Placeholder Polish Decision

**STOP PLACEHOLDER POLISH: YES.**

현재 placeholder는 gameplay readability, state hierarchy, asset replacement seam, 16:9 composition을 검증하는 목적을 달성했다. primitive mesh, generated sprite, temporary font를 더 다듬는 작업은 상용 품질 차이를 줄이지 못하고 final asset의 크기·비율·재질이 들어올 때 재작업될 가능성이 높다. 예외는 수동 full-hole 검증에서 새로 발견되는 P0/P1 기능·가독성 회귀뿐이다.

## Commercial Art Decision

**COMMERCIAL ART: NO-GO.**

출시/마케팅/스토어 캡처용 품질로 승인하지 않는다. 이유는 final Character, authored environment, branded UI/font, authored VFX/audio, Development Build 성능 증거가 아직 없기 때문이다. 이는 현재 구현 실패가 아니라 다음 제작 단계의 명확한 입력 조건이다.

## Online Readiness

- `ShotCommand` 직렬화 가능한 명령 경계와 input/gameplay 분리는 향후 authority 설계에 유리하다.
- Score/Hole flow, Camera, HUD, Character, VFX, Audio가 이벤트/adapter 경계를 사용해 presentation 분리가 양호하다.
- 현재 Rigidbody physics는 플랫폼 간 lockstep 결정성을 보장하지 않는다. online 단계에서는 server-authoritative result 또는 shot simulation authority 정책을 별도로 결정해야 한다.
- 네트워크 지연, reconciliation, rollback, host migration, anti-cheat, serialization versioning은 구현·검증되지 않았다.
- 결론: **online architecture exploration은 가능하지만 online production-ready는 아니다.** 이번 gate에서는 online 코드를 추가하지 않는다.

## Recommendation

다음 로드맵은 **Final Asset Production**이다.

권장 순서는 Character → Environment → UI/Font → VFX/Audio → final camera retune → Development Build performance gate다. 각 자산은 기존 adapter/data/event seam을 유지한 채 교체하고, 한 영역의 자산이 들어올 때마다 동일 master capture 세트로 before/after를 비교한다.

## Next

Unity 초보자용 최종 수동 승인 절차:

1. Unity Hub를 엽니다.
2. `Projects` 탭에서 `swingpop` 프로젝트를 클릭합니다.
3. Unity가 열리면 Project 창에서 `Assets > _Game > Scenes > Hole01_SkyIsland`을 더블 클릭합니다.
4. 상단 메뉴 `Window > General > Console`을 클릭해 Console 창을 엽니다.
5. Console 창 왼쪽 위 `Clear`를 클릭합니다.
6. Game 탭을 클릭합니다.
7. Game 탭 왼쪽 위 해상도 드롭다운에서 `1920x1080` 또는 `Full HD`를 선택합니다. 없으면 `+`를 눌러 Width 1920, Height 1080을 추가합니다.
8. Game 탭 오른쪽 위 `Stats`를 꺼서 통계 overlay를 숨깁니다.
9. 같은 위치의 `Gizmos`를 꺼서 개발용 선이 캡처에 섞이지 않게 합니다.
10. Unity 상단 중앙의 ▶ Play 버튼을 클릭합니다.
11. Hole Intro 동안 코스, tee, green, flag, landmark가 순서대로 읽히고 갑작스러운 화면 점프가 없는지 봅니다.
12. Address에서 Character, Ball, 진행 방향, Wind/Hole/Club 정보가 동시에 읽히는지 확인합니다.
13. A/D 또는 방향 입력으로 Aim을 바꾸고 Character/aim line/target marker가 화면 밖으로 나가지 않는지 확인합니다.
14. 숫자 `1`~`5`를 차례로 눌러 No Spin, Top Spin, Back Spin, Left Side Spin, Right Side Spin으로 표시가 바뀌는지 확인합니다.
15. Space 또는 화면 Shot 버튼으로 Power를 시작하고 gauge가 하단에서 잘리지 않는지 확인합니다.
16. 다시 입력해 Impact를 진행하고 cursor, timing zone, 판정 문구가 겹치지 않는지 확인합니다.
17. 일반 판정 Shot을 1회 실행해 Flight에서 공이 하늘과 지형 양쪽에서 계속 보이는지 확인합니다.
18. ImpactSelecting에서 `P`를 눌러 검증용 Perfect Shot을 1회 실행하고 impact/trail/camera 반응이 Normal보다 강하지만 눈부시거나 화면을 가리지 않는지 확인합니다.
19. Fairway, Rough, Bunker 착지 후 lie 문구와 실제 표면이 일치하는지 확인합니다.
20. Water hazard를 발생시켜 splash, hazard 문구, reset 흐름이 끊기지 않는지 확인합니다.
21. Green에 도달해 Putter가 선택되면 Character, Ball, Cup, Flag, Green이 한 화면에 함께 보이는지 확인합니다.
22. Putt 중 camera가 Ball과 Cup을 모두 유지하고 과도한 흔들림이나 급격한 회전이 없는지 확인합니다.
23. Cup Approach에서 공이 Cup에 가까워질수록 Cup이 UI나 Character에 가려지지 않는지 확인합니다.
24. Hole-In 후 Cup effect → Character Reaction → Result가 중복 없이 한 번씩 재생되는지 확인합니다.
25. Result에서 score가 가장 먼저 읽히고 Character와 Result card가 서로 가리지 않는지 확인합니다.
26. Play 버튼을 다시 눌러 Play Mode를 종료합니다.
27. 해상도를 `1600x900`으로 바꾸고 12, 15, 21, 25번 상태를 다시 확인합니다.
28. 해상도를 `1280x720`으로 바꾸고 같은 네 상태에서 clipping과 text overflow를 확인합니다.
29. Windows File Explorer에서 프로젝트의 `docs > review-captures > final-graphics-quality-gate` 폴더를 엽니다.
30. A1~F3 master 19장과 R1~R8 보조 해상도 8장이 있는지 확인하고 실제 Game View와 나란히 비교합니다.
31. 상단 메뉴 `Window > Analysis > Profiler`를 클릭합니다.
32. Profiler에서 `CPU Usage`, `Rendering`, `Memory`, `UI` 모듈을 켜고 `Deep Profile`은 끕니다.
33. Profiler 왼쪽 위 Record 원이 켜진 상태로 Play Mode에서 Shot 1회를 포함해 최소 10초간 기록합니다.
34. CPU frame time, GC Alloc, Batches, SetPass Calls, memory spike를 캡처하거나 수치로 기록합니다.
35. Play Mode를 종료하고 Console의 Error 필터를 클릭합니다.
36. Error 0인지 확인하고, Hierarchy/Inspector에 `Missing (Mono Script)` 또는 `Missing` reference가 없는지 확인합니다.
37. 스피커와 헤드폰에서 각각 1회 플레이해 UI/impact/landing/hazard/Hole-In/result 음량 순서와 clipping을 확인합니다.
38. 발견한 P0/P1만 수정 대상으로 등록하고, primitive/generator 기반 미세 미화는 추가하지 않습니다.
39. 위 항목이 통과하면 Final Character Asset 제작부터 시작합니다. M12/online 구현은 별도 승인 전 시작하지 않습니다.

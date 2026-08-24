# Course Material & Environment Pass Review

## Goal

`Hole01_SkyIsland`의 gameplay graph를 유지한 채 Course Surface, Water, Cliff, Vegetation, Landmark, Sky, Lighting을 하나의 밝은 fantasy golf world로 정리한다. 이번 패스는 presentation-only이며 Ball physics, Shot/Aim/Power/Impact/Spin/Wind 계산, Terrain response, Hazard, Cup/Hole flow, Camera state machine, HUD binding을 변경하지 않는다.

판정 기준은 “색을 입힌 blockout”에서 벗어나 한 코스의 통일된 visual language와 전경/중경/배경 깊이가 읽히는가이다. 최종 상용 아트 완료를 선언하는 단계는 아니다.

## Baseline

Character Identity Pass 캡처에서 확인한 주요 문제는 다음과 같았다.

- Fairway, Rough, Green의 값과 표면 차이가 작아 넓은 단색 면처럼 보였다.
- Water가 얕고 균일한 cyan plane처럼 보였으며 shoreline/depth 구분이 약했다.
- Bunker가 flat yellow patch에 가까웠다.
- Main cliff와 distant island가 단일 purple mass로 읽혔다.
- 나무와 꽃은 변형이 적고 배치 반복감이 컸다.
- Green 근접 시 표면 정보와 Cup 대비가 부족했다.
- 화면 밝기는 확보했지만 foreground/midground/background의 value hierarchy가 약했다.

구조 baseline은 GameObjects 476, Renderers 318, Active Renderers 205, Shared Materials 56, Transparent Renderer Slots 7, Shadow Casters 137, Active Shadow Casters 87, Colliders 10, Particle Systems 7, Audio Sources 5, Update Behaviours 12였다.

## Fairway / Rough

- Fairway는 3개의 제한된 green tone을 사용하는 넓은 mowing band mesh로 교체했다.
- 밴드는 aim corridor를 가리지 않도록 낮은 대비로 구성했고, 작은 checker/stripe 반복을 사용하지 않았다.
- Rough는 더 어둡고 덜 포화된 shared material로 분리했다.
- 직선 polygon 경계를 완화하기 위해 irregular fringe mesh와 선택적 grass accent를 추가했다.
- 기존 `TerrainSurface`와 Collider는 그대로 유지하고 새 표면 mesh에는 Collider를 추가하지 않았다.

## Green

- Green은 Fairway보다 밝고 깨끗한 2-tone concentric mowing으로 구성했다.
- Rough → Fringe → Green의 3단 경계를 시각적으로 분리했다.
- Green에 실제 slope physics를 추가하지 않았으며 변화는 모두 presentation-only다.
- Putter Address 캡처에서 Ball, Cup, flag pole을 같은 프레임에서 확인할 수 있다.

## Bunker

- 밝은 outer sand, warm sand, 어두운 inner shade의 3단 concentric mesh를 적용했다.
- grass rim과 소량의 grain mark를 추가해 flat yellow patch 인상을 줄였다.
- 기존 Bunker surface/collider와 hazard response는 변경하지 않았다.

## Water

- opaque deep body, opaque shallow cyan band, pale shoreline, 제한된 transparent highlight의 4단 구조로 정리했다.
- highlight motion은 기존 `SkyIslandEnvironmentMotion`에 연결해 per-object `Update`를 추가하지 않았다.
- shoreline stone/grass framing을 소량 배치했다.
- SSR, reflection, refraction, full-screen transparency는 사용하지 않았다.

## Cliff / Island

- Main island를 grass top, brighter upper rock, mid rock, dark underside의 4-submesh shell로 교체했다.
- upper/lower ledge와 irregular outline으로 섬 두께와 실루엣을 보강했다.
- background island는 폭, 깊이, scale, rotation이 다른 A/B/C prefab variant를 사용한다.
- 이전 시각 root는 삭제하지 않고 inactive 상태로 보존했으며 gameplay collider는 유지했다.

## Vegetation

- Tree A/B/C는 trunk thickness, canopy height/width/blob count, light/dark foliage 비율이 다르다.
- 나무는 aim corridor를 비우고 좌우 framing과 midground cluster 위주로 재배치했다.
- flower patch는 leaf base와 pink/yellow/white head를 하나의 combined mesh로 만들었다.
- tee, rough, water, green edge에만 선택적 grass clump와 작은 stone을 사용했다.
- 꽃, grass, cloud, distant decoration은 shadow casting을 끄고 가까운 주요 나무만 선택적으로 켰다.

## Landmarks

- Windmill은 base/upper body, roof, door/window trim, cyan accent blade를 가진 authored combined mesh prefab으로 교체했다.
- Waterfall island는 source pool, layered cliff, tapered water ribbon, lower mist, top tree로 분리했다.
- rotor/cloud/water highlight는 기존 중앙 environment motion controller에서 함께 갱신된다.
- Green camera를 가리지 않도록 Windmill과 Waterfall의 위치와 scale을 조정했다.

## Sky / Lighting

- blue top과 pale cyan horizon이 읽히는 URP skybox palette를 적용했다.
- fog는 distant landmark만 완만하게 분리하고 Ball/Flag/Aim 가독성을 해치지 않는 범위로 유지했다.
- Directional Light와 ambient sky/equator/ground 값을 밝은 daylight와 soft stylized shadow 기준으로 조정했다.
- heavy bloom, motion blur, chromatic aberration, dark cinematic grading은 추가하지 않았다.

## Performance

최종 Editor 구조 검증 결과:

| Metric | Baseline | After | Review |
| --- | ---: | ---: | --- |
| GameObjects | 476 | 543 | 이전 root를 inactive로 보존해 total 증가 |
| Renderers | 318 | 375 | inactive legacy renderer 포함 |
| Active Renderers | 205 | 112 | combined mesh와 legacy visual 비활성화로 93 감소 |
| Shared Materials | 56 | 85 | 제한된 환경 palette asset 30개 추가, runtime material 없음 |
| Transparent Renderer Slots | 7 | 9 | 전체 기준, active transparent slots는 3 |
| Shadow Casters | 137 | 145 | inactive 포함 |
| Active Shadow Casters | 87 | 47 | 작은 장식/배경 shadow OFF로 40 감소 |
| Colliders | 10 | 10 | Course pass Collider 0 |
| Particle Systems | 7 | 7 | 변화 없음 |
| Audio Sources | 5 | 5 | 변화 없음 |
| Update Behaviours | 12 | 12 | per-object Update 증가 없음 |

PC 1920×1080 / 60 FPS는 목표다. 이번 작업에서는 Unity Profiler나 Development Build를 실행하지 않았으므로 CPU/GPU frame time, GC Alloc, Batches, SetPass, memory의 실제 성능 판정은 하지 않는다.

## Regression

- Unity 6000.5.7f1 batch compilation/build: 성공.
- Course Art structure validation: PASS.
- Missing Scripts 0, Missing Meshes 0, Missing Material Slots 0, Non-asset Materials 0, Duplicate Material Assets 0, Unexpected Shadow Casters 0.
- EditMode: 121 passed / 121 total.
- PlayMode: 5 passed / 5 total.
- 새 Course Environment Pass 아래 Collider는 0이고 전체 gameplay collider는 baseline 10을 유지한다.
- `Foundation.unity`는 이번 패스에서 변경하지 않았다.
- 수동 Unity Play Mode, Console, Profiler 검증은 아직 실행하지 않았다.

## Before / After

Before 기준은 [Character Identity Pass Address](review-captures/character-identity-pass/A-Address.png), After 기준은 [Course Pass Address](review-captures/course-environment-pass/A-Address.png)다.

| Area | Before | After |
| --- | --- | --- |
| Fairway | 넓은 단색 면 | 낮은 대비의 3-tone broad mowing과 fringe |
| Rough | Fairway와 값 차이 약함 | darker/desaturated framing surface |
| Green | 밝은 flat disk | fine concentric mowing, fringe, Cup contrast |
| Bunker | flat yellow patch | rim/light sand/inner shade/grain의 층위 |
| Water | 얕은 cyan plane | shoreline/shallow/deep/highlight 분리 |
| Cliff | 단일 purple mass | grass rim/upper/mid/dark underside와 ledge |
| Trees | 반복되는 소수 silhouette | A/B/C family, scale/rotation/cluster variation |
| Flowers | 작은 colored dot | leaf base가 있는 combined patch silhouette |
| Landmark | blockout landmark | authored Windmill/Waterfall prefab hierarchy |
| Lighting | 균일한 밝기 | daylight, soft shadow, horizon haze 분리 |
| Depth | 전경과 배경의 값이 비슷함 | foreground framing, midground hazard, hazy islands/clouds |
| Prototype feel | procedural blockout가 강함 | 제한된 palette의 coherent fantasy course foundation |

추가 After 캡처: [Perfect Flight](review-captures/course-environment-pass/D-Perfect-Flight.png), [Putter Address](review-captures/course-environment-pass/F-Putter-Address.png), [Result](review-captures/course-environment-pass/H-Result.png), [Water](review-captures/course-environment-pass/W-Water.png), [Bunker](review-captures/course-environment-pass/B-Bunker.png).

## Quality Scorecard

| Target | Grade | Evidence |
| --- | :---: | --- |
| Fairway | B | broad mowing과 corridor readability 확보 |
| Rough | B | 명확한 value/saturation 분리 |
| Green | B | near-camera variation과 fringe 확보 |
| Bunker | B | 3-tone depression과 grain 확보 |
| Water | B | shallow/deep/shoreline/highlight 분리 |
| Cliff | B | 4-tone thickness와 ledge 확보 |
| Trees | B | A/B/C silhouette와 cluster 구성 |
| Flowers / low vegetation | B | combined patch와 제한된 accent palette |
| Windmill | B | authored silhouette와 trim hierarchy |
| Waterfall | B | source/body/mist/cliff framing 분리 |
| Floating islands | B | 3 prefab variants와 transform variation |
| Sky | B | blue/cyan horizon gradient와 haze |
| Lighting | B | bright daylight와 soft shadow 유지 |
| Depth | B | foreground/midground/background 구분 개선 |
| Address composition | B | fairway, water, bunker, landmark hierarchy 확인 가능 |
| Putt composition | B | Ball과 Cup 동시 가독성 확보 |
| Ball readability | B | green/sky/aim line 위 흰 Ball 대비 유지 |
| Overall environment cohesion | B | 제한된 palette와 rounded low-poly language 통일 |
| Overall game-vs-prototype impression | C | prototype 단계는 완화했으나 authored texture/model/LOD가 아직 없음 |

## Remaining Gaps

- authored grass texture/shader와 surface roughness detail
- sculpted terrain mesh와 gameplay collider alignment 검증
- final mobile-compatible water shader
- final cliff texture/material breakup
- final tree/foliage set와 LOD
- final flowers, stones, course props
- final Windmill, floating island, Waterfall assets
- flag/waterfall/foliage final ambient animation
- environment audio와 AudioMixer tuning
- Development Build Profiler/Frame Debugger 기준의 CPU/GPU/GC/Batches/SetPass/memory 검증
- 최종 Character FBX가 들어온 뒤 landmark/foliage scale과 camera occlusion 재검토

## Recommendation

**Course Environment Foundation: GO. Commercial Environment Art: NO-GO.**

현재 상태는 surface와 landmark가 구분되는 coherent fantasy golf course foundation으로 다음 제작 판단에 사용할 수 있다. 다만 procedural/generated mesh와 flat-color shared material 비중이 높아 상용 최종 아트로 승인할 수는 없다. 다음 단계는 이번 범위 밖이며, 수동 Play Mode/Profiler 확인 후 별도 승인을 받아 진행해야 한다.

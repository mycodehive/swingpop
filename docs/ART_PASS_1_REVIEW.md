# ART PASS 1 — Character & Environment Foundation Review

## Goal

M11에서 완성된 gameplay, camera state, HUD binding을 유지한 채 primitive character와 flat/repetitive environment를 한 단계 개선하고, 향후 최종 Humanoid character와 authored environment asset으로 안전하게 교체할 수 있는 presentation seam을 만드는 작업이다. Shot physics, Aim, Power, Impact timing, Spin, Wind, Lie, Club calculation, Hazard, Putter physics, Cup/Score/Hole flow는 변경하지 않았다.

## Baseline

기준 이미지는 [M11PolishedAddress.png](review-captures/M11PolishedAddress.png)이다.

- Character는 큰 primitive 덩어리와 단색 torso 때문에 mannequin 인상이 강했다.
- Fairway/Rough/Green이 분리되지만 texture와 값 변화가 약해 flat surface처럼 보였다.
- 동일한 tree/flower silhouette가 반복되어 prototype 반복감이 컸다.
- Windmill/Waterfall/Cloud는 기능적인 landmark였지만 hierarchy와 depth가 부족했다.
- M11 기준 구조 수치는 Renderer 191, shared material 54, transparent slot 4, shadow caster 77, Collider 10, Particle 7, Audio 5, Update-declaring behaviour 12였다.

## Character Changes

- tapered jacket mesh, pelvis, neck, collar, accent belt를 추가해 torso를 단순 box에서 분리했다.
- 상/하완, cuff, hand와 상/하퇴, sock, shoe/sole를 구분했다.
- 머리는 slightly oversized 비율을 유지하면서 back/side/front 5-part hair chunk와 최소 eye/brow/mouth hint를 추가했다.
- hair/skin/top/accent/bottom/shoe/club 재질의 hue와 smoothness를 분리해 grass 배경에서 silhouette가 읽히도록 조정했다.
- 기존 procedural state motion과 impact timing은 그대로 유지했다.

## Character Replacement Pipeline

`Placeholder Golfer` root에 `CharacterVisualAdapter`를 연결했다.

```text
CharacterRoot (Placeholder Golfer / gameplay controllers)
├─ Visual Root
├─ ClubSocket
├─ AP1 Hand Socket
├─ AP1 Impact Anchor
└─ AP1 Head Look Target
```

- `CharacterVisualAdapter`: GameplayRoot, VisualRoot, optional Animator, ClubSocket, HandSocket, ImpactAnchor, HeadLookTarget, profile reference를 보유한다.
- `CharacterVisualProfile`: display name, visual height 3.15, local bounds, presentation offset을 보유한다.
- `CharacterPresentation`은 adapter에서 VisualRoot/ClubSocket을 받되 기존 procedural placeholder hierarchy를 유지한다.
- `CharacterAnimationController`는 Animator Controller가 연결되면 procedural pose를 적용하지 않고 Animator state를 사용한다. Animator가 없으면 기존 procedural path가 유지된다.
- 최종 모델 교체 시 gameplay controller와 timing 코드를 다시 작성하지 않고 `Visual Root`, Animator, socket/anchor, profile만 교체한다.

## Course Material Changes

- existing shared M11 materials의 base color/smoothness를 조정해 Rough를 더 어둡고 덜 포화되게, Fairway는 2-tone band가 읽히게, Green은 밝고 낮은 noise로 분리했다.
- Tee/Fringe/Cliff rim/body/Water shallow/deep의 value 차이를 강화했다.
- Bunker에는 collider 없는 inner shade mesh와 별도 shared sand shade material을 추가했다.
- Water에는 collider 없는 transparent highlight ring을 추가하고 기존 `SkyIslandEnvironmentMotion` coordinator가 천천히 이동시킨다.
- Cup 주변에는 collider 없는 readability ring을 추가했다.

## Vegetation

- trunk height, canopy blob 수/scale/color가 다른 Tree A/B/C prefab을 만들었다.
- 가까운 canopy 일부와 trunk만 shadow를 유지하고 나머지 canopy, flowers, clouds는 shadow를 껐다.
- flower patch는 5개 flower/leaf silhouette를 한 mesh와 3개 shared material slot으로 구성해 object 증가를 제한했다.
- foreground flowers, midground tree clusters, background tree/landmark 순으로 배치하고 central shot corridor는 비웠다.

## Environment

- Cloud A/B/C는 blob 수, scale, rotation이 다르며 shadow를 cast하지 않는다.
- Windmill은 tapered body, roof, door/window, hub와 4개 blade로 silhouette를 분리했다.
- Waterfall island는 narrow top/wider lower waterfall, cliff frame, bottom mist accent로 단순 cyan rectangle 인상을 줄였다.
- 기존 M10 tree/flower/cloud/windmill/waterfall visual은 비활성화하고 AP1 prefab instance를 presentation root에 배치했다.
- decorative additions에는 Collider가 없다.

## Lighting

- Directional Light를 warm daylight 방향으로 조정하고 soft shadow strength를 낮췄다.
- ambient sky/equator/ground와 linear fog를 조정해 upper sky, light horizon, distant depth가 구분되도록 했다.
- character 전용 realtime light는 추가하지 않았다.

## Performance

Unity Editor structural audit 결과다. `Total`은 비활성화된 superseded M10 visual까지 포함하며, runtime relevance를 위해 active 수치를 별도로 기록했다.

| Metric | M11 | ART PASS 1 | Assessment |
|---|---:|---:|---|
| GameObjects | 328 | 475 | reusable character/environment hierarchy 증가 |
| Renderers (total) | 191 | 318 | 비활성 구형 visual 포함 |
| Renderers (active) | 미기록 | 205 | 현재 runtime hierarchy |
| Shared materials | 54 | 56 | 신규 material은 sand shade/water highlight 2개 |
| Transparent renderer slots | 4 | 7 | water/highlight 범위 내 증가 |
| Shadow casters (total) | 77 | 137 | 비활성 구형 caster 포함 |
| Shadow casters (active) | 미기록 | 87 | 가까운 character/tree/landmark 중심 |
| Colliders | 10 | 10 | gameplay collider 변화 없음 |
| Art colliders | 0 | 0 | presentation-only 유지 |
| Particle systems | 7 | 7 | 증가 없음 |
| Audio sources | 5 | 5 | 증가 없음 |
| Update-declaring behaviours | 12 | 12 | water motion은 기존 coordinator 재사용 |
| Missing Scripts / Materials | 0 / 미기록 | 0 / 0 | 구조 검사 통과 |

Unity Profiler의 CPU/GPU frame time, GC Alloc, Batches/SetPass는 실행하지 않았다. 따라서 1080p 60 FPS를 보증하지 않으며 사용자/Development Build 측정이 남아 있다.

## Regression

- Unity script compilation: 성공, compile error 0.
- ART PASS 1 structure validation: 통과.
- EditMode: 118 passed, 0 failed, 0 skipped. 새 `CharacterVisualAdapterTests` 포함.
- PlayMode: 4 passed, 0 failed, 0 skipped, 6.08 s. 기존 M9/M10/M11과 새 AP1 scene wiring test 포함.
- gameplay Collider 10, Particle 7, Audio 5, Update 12로 M11 flow budget 유지.
- `Foundation.unity`와 gameplay physics/state/controller 파일은 변경하지 않았다.
- 자동 캡처에서는 A/D/F/H route를 실행했지만 사용자가 직접 조작하는 전체 Hole feel과 Console Error 0 확인은 별도다.

## Screenshot Comparison

| State | Capture | Observation |
|---|---|---|
| Before | [M11 Address](review-captures/M11PolishedAddress.png) | mannequin body, repeated blob trees, flat value separation |
| A | [ART PASS 1 Address](review-captures/art-pass-1/A-Address.png) | segmented character/hair/color, Tree variants, foreground flowers, deeper landmark hierarchy |
| D | [Perfect Flight](review-captures/art-pass-1/D-Perfect-Flight.png) | stronger gold trail/impact and readable PERFECT response |
| F | [Putter Address](review-captures/art-pass-1/F-Putter-Address.png) | Ball, 3.0 m Cup/flag direction, Putter/Green HUD가 함께 보임 |
| H | [Result](review-captures/art-pass-1/H-Result.png) | 강제 완료를 사용한 composition capture. Result panel, character, flag/landmark background가 유지되며 0-stroke score 값은 정상 플레이 결과를 대표하지 않음 |

개선되지 않은 점도 분명하다. Course는 authored terrain texture가 없어 여전히 큰 flat polygon 면으로 보이며, character는 final face/rig/model이 아닌 constructed primitive placeholder다. Putter/Result close camera에서는 character와 background landmark가 크게 잘리는 구도가 남는다.

## Quality Score

A=final target에 가까움, B=vertical-slice 사용 가능, C=placeholder/추가 art 필요, D=blocker.

| Item | Grade | Reason |
|---|:---:|---|
| Character silhouette | B | segmented body와 hair outline이 사람으로 읽힘 |
| Character material/color | B | hair/skin/top/accent/bottom 분리 |
| Character replacement readiness | B | adapter/profile/socket/anchor seam과 Animator fallback |
| Fairway material | C | band separation은 개선, authored texture/shader 없음 |
| Green material | B | Fairway와 분리되고 Ball contrast 유지 |
| Bunker | B | rim/inner shade/depression hierarchy |
| Water | C | shallow/deep/highlight는 있으나 final shader/shoreline 없음 |
| Trees | B | A/B/C silhouette/color/scale variation |
| Vegetation | C | composition은 개선, mesh detail은 placeholder |
| Landmark | B | Windmill/Waterfall hierarchy와 distance anchor 강화 |
| Lighting | B | bright readability와 soft depth 유지 |
| Depth | B | foreground/midground/background 구분 개선 |
| Address composition | B | Character/Ball/corridor/flag/landmark 동시 가독성 |
| Perfect readability | B | gold trail/impact와 HUD가 명확함 |
| Putt visual readability | B | Ball/Cup/flag direction이 한 화면에 보임 |
| Overall game-vs-prototype impression | C | noticeably improved placeholder지만 commercial authored art와 차이가 큼 |

## Remaining Gaps

- original anime character model, readable face/hair/outfit, Humanoid rig, authored animation clips와 portrait.
- final Driver/Putter model과 hand fitting.
- sculpted terrain/cliff, grass/green/bunker/water authored textures/shaders와 shoreline.
- detailed tree/flower/prop/landmark/floating-island set, LOD와 batching.
- final UI skin, VFX textures/shaders, licensed audio/mix.
- Putter/Result close framing의 character/landmark crop polish.
- Development Build Profiler와 1080p 60 FPS, GC/Batches/SetPass 검증.

## Recommendation

**ART PASS 1 Foundation: GO. Commercial Art: NO-GO.**

기존 gameplay를 건드리지 않고 character replacement seam과 environment prefab/material foundation을 만들었고, Address 화면은 M11보다 더 게임다운 placeholder로 개선됐다. 다음 작업은 새로운 gameplay milestone이 아니라 최종 character/environment asset 제작 또는 ART PASS 2 asset integration이어야 한다. 실제 asset 투입 전 `docs/TODO_ART.md`의 socket, animation event, collider, profile checklist를 먼저 사용한다.

후속 Character Identity Pass에서 Humanoid template, Avatar/left-right hand socket/profile metadata, central Animator contract, validator와 integration guide를 추가했다. 최신 character pipeline 판정과 A/D/F/H capture는 `docs/CHARACTER_IDENTITY_PASS_REVIEW.md`를 따른다.

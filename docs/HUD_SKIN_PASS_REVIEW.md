# HUD Skin Pass Review

## Goal

기능적으로 완성된 M8 HUD의 gameplay source of truth와 command path를 유지하면서, `Hole01_SkyIsland`의 개발용 uGUI 인상을 SwingPop의 밝고 캐주얼한 판타지 골프 UI로 발전시키는 presentation-only pass다. Aim, Power, Impact, ShotCommand, Spin, Wind, Lie, Club, Hazard, Score, Hole flow, Camera, Character, Ball physics는 변경 대상이 아니다.

## Baseline

- 기존 HUD는 정보 구조와 상태 전이가 명확하고 16:9 anchor, Safe Area, keyboard/mouse 공통 command path를 이미 갖고 있었다.
- 반면 큰 직사각형 panel, 기본 uGUI 도형, 약한 icon/typography hierarchy 때문에 polished course 위에서 prototype 인상이 강했다.
- Power/Impact gauge와 Shot Button은 기능은 명확했지만 주요 arcade interaction으로서의 존재감이 부족했다.
- Result, popup, club/spin 표시는 서로 다른 기능 블록처럼 보여 하나의 UI family로 묶일 필요가 있었다.

## Design System

- `HudSkinData`가 deep navy/teal panel, cyan/aqua, mint, pink, gold, coral, disabled blue-gray와 공통 sprite reference를 중앙 관리한다.
- panel은 반투명 navy base, cyan rim, 얇은 highlight와 최소한의 outline을 공유한다. 화면 중앙을 비우고 corner widget의 시각 질량을 줄였다.
- project-safe 64 px generated sprite family는 rounded panel, capsule, circle, diamond, triangle, portrait, wind, driver, putter, spin, target silhouette를 제공한다.
- `HudSkinStyleMapper`는 ImpactGrade, TerrainSurfaceType, ScoreResult, ShotFlowState를 presentation tone으로만 변환한다. gameplay 값을 만들거나 수정하지 않는다.
- 최종 authored 9-slice, portrait, icon, font를 같은 data/prefab seam에서 교체할 수 있다.

## Player / Hole / Wind

- Player HUD는 portrait slot이 돌출된 compact card로 바꾸고 PLAYER, STROKE, PENALTY의 hierarchy를 분리했다. penalty가 없을 때 불필요한 강조를 줄였다.
- Hole HUD는 HOLE을 가장 크게, PAR와 STROKE를 secondary 정보로 정리한 중앙 scoreboard badge다.
- Wind HUD는 방향 arrow를 중심으로 strength, unit, state를 묶었다. arrow rotation과 표시 값은 기존 Wind source를 그대로 사용한다.
- 세 widget 모두 밝은 sky, dark tree, green, water 위에서 white/cyan text가 읽히도록 translucent backplate를 유지한다.

## Aim

- Distance badge의 배경 질량을 줄이고 diamond target marker를 추가해 aim endpoint와 같은 visual family로 정리했다.
- distance와 height delta 값, world-to-screen 위치, aim line 자체는 기존 gameplay/presenter 경로를 유지한다.
- center view를 크게 가리지 않으며 ball, character, landing corridor가 계속 보인다.

## Club / Spin

- DRIVER와 PUTTER에 서로 다른 silhouette icon을 연결했다.
- Fairway, Rough, Bunker, Green은 기존 lie 판정을 유지하며 presentation accent만 green/deep green/sand gold/mint로 바뀐다.
- No/Top/Back/Left/Right Spin은 같은 크기의 directional icon family로 표시한다.
- Putter에서는 기존 gameplay 제한을 그대로 따르고 `SPIN DISABLED`를 저채도 blue-gray로 조용하게 표시한다.

## Power

- 화면 하단 중앙에 outer frame, track, fill, cursor, value를 묶은 compact arcade gauge를 구성했다.
- 기존 0–100 power 값을 그대로 쓰고 fill 색만 cyan에서 mint, maximum 근처 gold로 보간한다.
- cursor가 실제 gauge value를 따라가며 별도의 gameplay power 계산은 없다.

## Impact

- Power와 같은 frame/track/cursor family를 사용하고 기존 M2 threshold로 Perfect/Great/Good/Miss zone을 표시한다.
- Perfect는 얇은 gold core와 제한적인 pulse, Great는 cyan, Good은 mint/blue, Miss는 coral tone이다.
- zone 판정과 grade는 기존 ShotFlow source를 사용하며 UI가 threshold를 재계산하지 않는다.

## Shot Button

- lower-right hero interaction으로 rounded capsule, cyan edge, layered fill, main label과 `SPACE / CLICK` 보조 label을 사용한다.
- START SHOT, SET POWER, IMPACT 상태는 기존 `ConfirmCurrentStep()` command path를 유지한다.
- Aiming/Power는 cyan/mint, Impact는 gold, disabled는 blue-gray presentation을 사용한다. Button ColorBlock으로 hover brightness와 press feedback을 제공한다.
- Flying/HoleComplete visibility와 interactable 여부는 기존 HUD state가 결정한다.

## Popups

- PERFECT/GREAT/GOOD/MISS는 공통 panel과 accent bar를 쓰며 grade별 gold/cyan/mint/coral tone을 적용한다.
- WATER HAZARD/OOB/PENALTY는 coral danger family, lie는 작은 informational badge family로 정리했다.
- popup은 Character face, impact effect, ball flight 중심부를 가리지 않도록 기존 위치와 duration contract를 유지한다.

## Result

- Result를 layered central card, thin cyan rim, score emblem, 큰 score result, secondary hole/par/stroke 정보 구조로 바꿨다.
- Albatross/Eagle/Birdie는 gold/cyan, Par는 white/cyan, Bogey 이상은 coral accent를 사용한다.
- 기존 score/result event와 fade/scale presentation 경로를 보존했다. 캡처의 `ALBATROSS -4`는 composition 확인용 강제 sample이며 실제 플레이 결과를 주장하지 않는다.

## Resolution

- 1920×1080에서 Address, Power, Impact, Perfect Flight, Putter, Water Hazard, Result를 GPU batch capture했다.
- 1600×900과 1280×720에서 Address 상태를 추가 capture했고 Player, Hole, Wind, Club, Shot Button, Aim의 clipping/overlap이 없음을 이미지로 확인했다.
- alternate resolution의 모든 상태를 수동 순회한 것은 아니다. 최종 승인 전 Power/Impact/Result까지 Editor Game View에서 반복 확인해야 한다.

## Performance

| Metric | 기존 HUD | Skin HUD | 판정 |
| --- | ---: | ---: | --- |
| GameObjects | 66 | 82 | visual layer 16개 추가 |
| Graphics | 58 | 74 | icon/frame 16개 추가 |
| Raycast targets | 15 | 1 | Shot Button만 유지 |
| Outlines | 35 | 35 | 증가 없음 |
| Shadows | - | 0 | 새 Shadow 없음 |
| Canvas | 1 | 1 | 증가 없음 |
| UI materials | 1 | 1 | instance 증가 없음 |
| LayoutGroups / ContentSizeFitters | 0 / 0 | 0 / 0 | rebuild source 없음 |
| Update behaviours | 1 | 1 | 증가 없음 |

shared sprites와 default UI material을 재사용한다. validator 기준 per-object update, layout rebuild component, material instance는 증가하지 않았다. Unity Profiler의 frame time/GC Alloc/Batches/SetPass 측정은 아직 수동으로 실행하지 않았다.

## Regression

- HUD validator: PASS. Canvas 1, EventSystem 1, Raycast target 1, Missing Sprite 0, Missing Script 0, Safe Area/CanvasScaler/Shot Button binding 정상.
- EditMode: 136/136 PASS. 추가된 style mapping test 15개 포함.
- PlayMode: 6/6 PASS. Safe Area, scaler, canvas/layout/raycast/skin과 실제 Shot Button → PowerSelecting path 포함.
- Course Art validator: PASS. GameObjects 559, Renderers 375, Colliders 10, Course Art Colliders 0, ParticleSystems 7, AudioSources 5, UpdateBehaviours 12, Missing Scripts/Meshes/Materials 0.
- `Foundation.unity`는 변경하지 않았고 `Hole01_SkyIsland.unity`만 새 isolated HUD prefab을 사용한다.
- Unity Editor에서의 수동 전체 shot/hazard/hole flow, Console Error 0 확인은 실행하지 않았다.

## Before / After

- Before: `docs/review-captures/course-environment-pass/A-Address.png`
- After: `docs/review-captures/hud-skin-pass/A-Address.png`
- panel 면적과 시각 질량은 감소했고 typography와 icon family, gauge, lower-right hero action은 더 명확해졌다.
- course/character/ball line-of-sight를 유지하면서 개발용 rectangle의 인상을 줄였다.
- 아직 final font, character portrait, authored icon/9-slice가 없어 상용 UI와의 간극은 남는다.

## Quality Scorecard

| 항목 | 등급 | 근거 |
| --- | :---: | --- |
| Player HUD | B | compact hierarchy와 portrait seam, final portrait 미완료 |
| Hole HUD | B | scoreboard hierarchy 개선, authored badge 미완료 |
| Wind HUD | B | 방향 중심 구성과 source 일치 |
| Aim / Distance | B | target family와 시야 보존 |
| Club / Lie | B | icon/lie accent 구분 |
| Spin | B | 5방향/disabled presentation 구분 |
| Power Gauge | B | arcade hierarchy 개선, final skin/VFX 미완료 |
| Impact Gauge | B | threshold 보존과 zone hierarchy |
| Shot Button | B | hero interaction과 state 구분 |
| Impact Popup | B | grade family 일관성 |
| Hazard Popup | B | danger hierarchy 명확 |
| Result Panel | B | 결과 우선 hierarchy, final badge 미완료 |
| Typography hierarchy | C | LegacyRuntime placeholder |
| Icon consistency | B | 한 family지만 generated placeholder |
| Environment visibility | B | 중앙 시야와 course depth 보존 |
| Resolution scaling | B | 3개 16:9 capture, 전체 상태 수동 검증 필요 |
| Overall HUD cohesion | B | palette/shape/state family 통합 |
| Game-vs-prototype impression | B | prototype 인상 감소, authored asset 전 commercial 아님 |

## Remaining Gaps

- 라이선스와 한글/영문 localization을 검증한 최종 font 및 text spacing
- 최종 original character portrait와 portrait frame
- authored scalable 9-slice panel/capsule asset
- Wind/Club/Spin/Aim/Stroke/Penalty의 최종 icon set
- Power/Impact gauge texture, cursor, restrained glow polish
- Shot Button authored skin, hover/press motion과 UI audio
- Result emblem/badge, hazard badge, score celebration polish
- 색각/고대비/localization overflow 검증
- 모든 HUD state의 3해상도 수동 검증과 Unity Profiler 측정

## Recommendation

`HUD Skin Foundation: GO`. 기존 gameplay architecture를 보존하면서 교체 가능한 skin/data/prefab 경계를 만들었고 Hole01에서 prototype 인상을 의미 있게 줄였다.

`Commercial UI Approval: NO-GO`. 최종 typography, portrait, authored 9-slice/icon/gauge/button/result asset과 수동 접근성·Profiler 검증이 남아 있다. 다음 대형 시스템은 이번 pass에서 시작하지 않는다.

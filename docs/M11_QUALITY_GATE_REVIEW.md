# M11 Polish / Quality Gate Review

## Scope

M11은 `Assets/_Game/Scenes/Hole01_SkyIsland.unity`의 기존 M1–M10 gameplay를 변경하지 않고, 현재 placeholder 자산으로 가능한 composition, readability, material, camera, HUD, presentation 및 technical quality gate만 수행했다. 신규 club, course, gameplay rule, online 기능은 추가하지 않았다. `Foundation.unity`는 HEAD와 동일한 blob hash로 보존됐다.

## Baseline

실제 M10 Address render는 [M11BaselineAddress.png](review-captures/M11BaselineAddress.png)에 저장했다.

- 전경이 넓은 단색 mint 평면으로 보이고 코스가 직선형 사각 띠처럼 읽혔다.
- Fairway/Rough/Green/Bunker의 경계와 높이 차가 약했다.
- 캐릭터가 화면에 비해 작고 가는 primitive silhouette이었다.
- Player/Hole/Club panel과 Shot button이 크고 직사각형이며, Primary Action의 잘못된 anchor 때문에 우상단에 나타났다.
- Windmill이 화면 우측에 잘리고, 배경 landmark와 수목은 sparse하게 분산됐다.
- Ball과 aim line은 기능적으로 읽히지만 debug line에 가까웠다.

## Character Polish

- Address visual root scale을 1.27로 키우고 torso/head/arms/legs 비율을 정리했다.
- dark hair/bottom, blue outfit, pink accent, skin, shoe, club shaft/head로 역할별 shared material을 분리했다.
- 손, 신발, hair tuft를 collider 없는 교체 가능한 visual child로 추가했다.
- gameplay placement, animation state, Impact marker, Driver/Putter socket은 변경하지 않았다.
- 최종 anime character model/rig/animation을 대체하지 않으며 현재 평가는 polished placeholder다.

## Camera / Composition

- Hole01 전용 `M11CameraTuning.asset`을 만들고 Address/Swing/Follow/Landing FOV와 offset을 조정했다.
- Address에서 Character, Ball, aim line, fairway corridor, flag, waterfall island, windmill을 한 프레임에 읽도록 구성했다.
- tree cluster를 좌우 frame edge로 모아 중앙 shot corridor를 비웠다.
- Camera collision 로직과 mode state machine은 그대로 유지했다.
- 실제 BallFollow/Putt/Result motion comfort는 수동 Play Mode 검증이 필요하다.

## Course / Environment Polish

- collider 없는 organic island shell, cliff rim, curved fairway/fringe, raised tee, organic green/fringe, bunker rim/depression, water shallow/deep visual mesh를 추가했다.
- 기존 rectangular `Course Visual Layers`는 비활성화하되 Tee/Fairway/Rough/Bunker/Green/Water/OOB gameplay surface와 data는 유지했다.
- Fairway는 폭과 중심이 변하는 ribbon으로 만들고 alternate stripe를 사용해 깊이를 보강했다.
- Water와 Bunker gameplay volume은 새 시각 배치에 맞게 Hole01 안에서 재배치했다. recovery/scoring rule은 변경하지 않았다.
- Windmill, waterfall island, distant islands, clouds, tree/flower cluster를 foreground/midground/background로 재구성했다.
- M11 art root에는 Collider가 0개다.

## HUD Polish

- Player/Hole/Wind/Club panel을 축소하고 alpha, outline, corner spacing을 정리했다.
- Primary Action을 실제 lower-right anchor로 수정하고 Shot button과 `SPACE / CLICK` hint를 한 panel 안에 정리했다.
- player-facing club label은 asset의 임시 이름 대신 `DRIVER`/`PUTTER` club type을 표시한다.
- Aim marker와 Distance panel의 화면 점유를 줄이고 기존 Power/Impact/Result presentation graph는 유지했다.
- 3개 16:9 Address capture에서 corner HUD clipping/overlap은 발견되지 않았다.

## Materials / Lighting

- Rough/Fringe/Fairway/Tee/Green/Sand/Water/Cliff와 Character/Ball/Aim용 M11 shared material palette를 분리했다.
- M10 skybox를 직접 수정하지 않고 `M11Skybox.mat` 복제본을 Hole01에만 연결했다.
- directional light, trilight ambient, fog range와 color를 낮은 채도의 blue/green depth 방향으로 조정했다.
- Water는 투명 material 2개로 제한했고, decorative surface와 distant element의 shadow casting을 껐다.
- 최종 authored texture, water shader, terrain shader, color-grading pass는 여전히 필요하다.

## VFX / Presentation

- 기존 M9 Normal/Perfect Impact, trail, landing, hazard, Hole-In VFX/Audio event route를 보존했다.
- Ball material을 더 밝게 만들고 aim line을 얇은 cyan-to-mint guide로 조정했다.
- debug overlay와 trajectory는 기본 숨김 상태를 유지한다.
- Address 캡처 외 실제 Normal/Perfect/Flight/Landing/Putt/Result 육안 비교와 스피커 청취는 수행하지 않았다.

## Performance Review

Unity Editor structural audit 결과:

| Metric | M10 baseline | M11 | Assessment |
|---|---:|---:|---|
| GameObjects | 289 | 328 | visual layer와 character silhouette 증가 |
| Renderers | 157 | 191 | organic course layer 증가 |
| Shared materials | 34 | 54 | M11 palette 증가; renderer별 runtime instance는 만들지 않음 |
| Transparent renderer slots | 2 | 4 | shallow/deep water와 기존 투명 표현 범위 |
| Shadow casters | 145 | 77 | decorative/distant/surface shadow 제거 |
| Colliders | 10 | 10 | gameplay collider budget 유지 |
| M11 art colliders | 0 | 0 | presentation-only 유지 |
| Particle systems | 7 | 7 | 증가 없음 |
| Audio sources | 5 | 5 | 증가 없음 |
| Update-declaring behaviours | 12 | 12 | 증가 없음 |
| Missing Scripts | 0 | 0 | 통과 |

Unity Profiler의 CPU/GPU frame time, GC Alloc, Batches/SetPass는 측정하지 않았다. 따라서 1080p 60 FPS는 아직 승인하지 않는다. Editor batch PlayMode 통과는 기능 회귀 증거이지 성능 보증이 아니다.

## Resolution Validation

Address 상태를 실제 Play Mode camera + uGUI로 렌더하고 육안 확인했다.

- [1920×1080](review-captures/M11PolishedAddress.png): corner HUD와 aim/character composition 정상.
- [1600×900](review-captures/M11PolishedAddress_1600x900.png): clipping/overlap 없음.
- [1280×720](review-captures/M11PolishedAddress_1280x720.png): clipping/overlap 없음, 작은 hint는 최종 font asset 적용 때 재검토 필요.

Power/Impact/Hazard/Putt/Result 상태를 각 해상도에서 수동으로 전환한 검증은 아직 수행하지 않았다.

## Regression Validation

- Unity compile: 성공, C# compile error 0.
- M11 structural quality gate: 통과.
- EditMode: 117 passed, 0 failed, 0 skipped.
- PlayMode: 3 passed, 0 failed, 0 skipped. M9 Foundation flow, M10 Hole01 full flow, M11 presentation-only/HUD anchor를 포함한다.
- PlayMode 회귀는 Normal, Perfect, Water recovery, Green/Putter, Hole-In/Result route를 포함한다.
- `Foundation.unity`: HEAD/worktree git blob hash `ff98a2c403448c9f4c60b551b5afa685e26f350f` 일치.
- 사용자가 직접 실행하는 Console Error 0, Missing Reference, full-hole feel 검증은 남아 있다.

## Quality Scorecard

등급은 A=목표 충족, B=vertical-slice 사용 가능, C=placeholder/추가 검증 필요, D=상용 blocker다.

| 항목 | 등급 | 근거 |
|---|:---:|---|
| Address composition | B | Character/Ball/course/landmark 동시 가독성 확보 |
| Character scale/presence | B | 크기와 color block 개선 |
| Character final identity | D | primitive placeholder, face/rig/final animation 없음 |
| Fairway/Rough/Green/Bunker shape | B | organic layered mesh와 bunker depression 적용 |
| Course depth/elevation | B | tee/rim/cliff/layer/landmark 깊이 개선 |
| Landmark/readable route | B | windmill/flag/waterfall과 shot corridor 분리 |
| Ball/aim readability | B | bright ball, 얇은 cyan guide, distance 유지 |
| HUD hierarchy | B | compact corner layout, lower-right action 정정 |
| 16:9 Address fit | A | 3개 해상도 렌더에서 clipping/overlap 없음 |
| Camera mode comfort | C | Address는 확인, Flight/Putt/Result 수동 검증 필요 |
| Normal/Perfect distinction | C | 구조 회귀 통과, 이번 M11 육안 캡처 없음 |
| Flight trail/landing feedback | C | 구조 보존, 실제 visual/audio review 필요 |
| Putt/Hole-In/Result presentation | C | 자동 flow 통과, 육안 검증 필요 |
| Technical structure/regression | A | 117+3 tests, Missing Script 0, art collider 0 |
| Performance confidence | C | shadow/update budget 개선, Profiler 미측정 |

## Before / After Assessment

| Before M11 | After M11 |
|---|---|
| 넓은 단색 전경과 직선 사각 course | curved fairway, fringe, tee/green/bunker/water의 유기적 layer |
| sparse prop 나열 | 좌우 tree cluster와 전·중·후경 landmark hierarchy |
| 작은 가는 primitive golfer | 더 큰 silhouette, 역할별 palette, 손/신발/hair 보강 |
| Primary Action이 우상단에 잘못 표시 | lower-right safe-area action으로 수정 |
| `Temporary Driver` 성격의 표시 | player-facing `DRIVER`/`PUTTER` |
| 145 shadow casters | 77 shadow casters |
| M10 skybox 직접 공유 | Hole01 전용 M11 skybox clone |

실제 최종 캡처는 [M11PolishedAddress.png](review-captures/M11PolishedAddress.png)다. 구조적 prototype에서 읽기 좋은 polished placeholder로 상승했지만, reference의 authored anime character, terrain detail, bespoke UI/VFX 수준에는 도달하지 않았다.

## Remaining Commercial Art Gaps

- original anime character model, face, hair/outfit detail, humanoid rig와 authored animation.
- sculpted terrain/cliff, stylized texture/shader, foliage variation, shoreline/waterfall/water shader.
- original landmark, cup/flag/signage, prop set.
- fantasy 9-slice HUD, portrait, icons, licensed font와 localization QA.
- authored impact/trail/landing/Hole-In VFX texture와 licensed audio/mix.
- low-poly visual-matched gameplay collider와 final LOD/batching/lighting pass.

전체 교체 목록은 `docs/TODO_ART.md`를 source of truth로 유지한다.

## Go / No-Go Recommendation

**CONDITIONAL GO — 다음 개발 단계 논의용 Vertical Slice로는 사용 가능, Commercial Release는 NO-GO.**

조건:

- EditMode/PlayMode/structure regression은 통과했다.
- Address target screenshot과 3개 16:9 fit은 확인했다.
- full-hole 수동 visual/audio review, Camera comfort, Console Error 0, Profiler 1080p 60 FPS를 사용자가 확인해야 한다.
- final character/environment/UI/VFX/audio 자산이 없으므로 상용 최종 품질로 승인하지 않는다.

## User Validation Checklist

1. Unity Hub를 열고 왼쪽 `Projects`에서 `swingpop`을 클릭한다.
2. Unity가 열리면 아래 `Project` 창에서 `Assets > _Game > Scenes`를 차례로 연다.
3. `Hole01_SkyIsland`를 더블클릭한다. `Foundation`은 열지 않는다.
4. 상단 메뉴에서 `Window > General > Console`을 클릭한다.
5. Console 창 왼쪽 위 `Clear`를 클릭한다.
6. `Game` 탭을 클릭한다. 탭이 없으면 `Window > General > Game`을 클릭한다.
7. Game 탭 위쪽 해상도 dropdown을 클릭하고 `1920x1080`을 선택한다.
8. 항목이 없으면 dropdown의 `+`를 클릭하고 `Fixed Resolution`, Width `1920`, Height `1080`을 입력해 추가한다.
9. Game 탭 위쪽 `Stats` 버튼이 켜져 있으면 클릭해 끈다.
10. Game 탭 위쪽 `Gizmos`도 꺼 screenshot에 개발 표시가 섞이지 않게 한다.
11. Unity 상단 중앙의 삼각형 `Play` 버튼을 클릭한다.
12. 약 3초 intro가 끝나고 Address가 되면 `H`와 `F1`을 한 번씩 눌러 debug/trajectory가 꺼져 있는지 확인한다. 이미 꺼져 있으면 다시 누르지 않는다.
13. Character, Ball, cyan aim guide, Fairway, Flag, Windmill이 동시에 보이는지 확인하고 Screenshot A를 저장한다.
14. `A/D` 또는 좌우 방향키로 조준한다. Aim marker, `78.0 m` 같은 Distance와 높이 표기가 읽히는지 확인한다.
15. 숫자 `1`, `2`, `3`, `4`, `5`를 차례로 눌러 `NO/TOP/BACK/LEFT/RIGHT SPIN` 표시가 바뀌는지 확인한다.
16. `Space` 또는 우하단 `START SHOT`을 클릭하고 Power gauge가 나타날 때 Screenshot B를 저장한다.
17. 다시 `Space`로 Power를 정하고 Impact gauge가 나타나는지 확인한다.
18. 일반 timing에서 세 번째 `Space`를 눌러 Normal Shot을 친다. Swing→Launch→Flight→Landing→Bounce→Roll→Stop→Next Address를 끝까지 확인하고 Flight Screenshot C를 저장한다.
19. 다음 샷의 Impact 단계에서 `P`를 눌러 Perfect를 강제한다. gold/bright Impact, 더 강한 trail, PERFECT HUD와 sound 차이를 확인하고 Screenshot D를 저장한다.
20. `A/D`로 왼쪽 Water 또는 오른쪽 Bunker 방향을 조준해 surface landing/hazard popup과 recovery를 확인하고 Screenshot E를 저장한다.
21. Green에 도달할 때까지 계속 플레이한다. Club이 `PUTTER`, Lie가 `GREEN`, Spin이 `SPIN DISABLED`가 되는지 확인한다.
22. Putter Address에서 Ball과 Cup이 한 화면에 함께 보이는지 확인하고 Screenshot F를 저장한다.
23. Putt로 Cup에 넣고 sparkle/ring, Character reaction, success audio, Result panel을 확인해 Screenshot G와 H를 저장한다.
24. Play 중 Game 탭의 `Stats`를 클릭해 켜고 Batches/SetPass/Tris/Verts 위치가 화면을 가리지 않는지 확인한 뒤 다시 끈다.
25. 상단 메뉴 `Window > Analysis > Profiler`를 클릭한다. 왼쪽에서 `CPU Usage`, `Rendering`, `Memory`를 활성화하고 `Deep Profile`은 끈다.
26. Profiler의 빨간 Record 버튼이 켜진 상태로 10초 이상 Address→Shot→Flight→Landing을 플레이한다. CPU main-thread frame, GC Alloc, Batches/SetPass를 기록한다. Editor overhead가 크면 `File > Build Profiles > Windows > Build And Run`의 Development Build에서 다시 측정한다.
27. Play를 종료하고 Game 해상도를 `1600x900`으로 바꾼 뒤 Play한다. Address/Power/Impact/Result에서 네 모서리 panel clipping과 중앙 시야 가림을 확인한다.
28. 같은 방법으로 `1280x720`을 추가/선택하고 동일 상태를 확인한다.
29. Play를 종료하고 Console의 빨간 Error count가 `0`인지 확인한다. Hierarchy object에 `Missing (Mono Script)` 또는 Inspector의 `None (Object)` 필수 reference가 없는지도 확인한다.

## Screenshot Targets

- A: 1920×1080 clean Address — Character/Ball/Fairway/Flag/Landmarks/HUD.
- B: Aim + Power 또는 Impact gauge — 중앙 interaction readability.
- C: Normal Ball Flight — camera/framing/trail.
- D: Perfect Impact/Flight — Normal과 명확한 차이.
- E: Bunker landing 또는 Water hazard — surface/hazard feedback.
- F: Green Putter Address — Ball과 Cup 동시 노출.
- G: Hole-In 순간 — cup VFX/character/camera.
- H: Result — score/result HUD와 clean background.

## Tuning Locations

- M11 rebuild/composition source: `Assets/_Game/Scripts/Editor/M11PolishSceneBuilder.cs`
- M11 camera: `Assets/_Game/ScriptableObjects/Polish/M11CameraTuning.asset`
- M11 materials/skybox: `Assets/_Game/Materials/Polish/`
- M11 organic mesh assets: `Assets/_Game/Art/Courses/M11/`
- scene wiring/instance overrides: `Assets/_Game/Scenes/Hole01_SkyIsland.unity`
- HUD motion/feedback: `Assets/_Game/ScriptableObjects/Environment/M10HudTuning.asset`
- VFX/trail/audio: `Assets/_Game/ScriptableObjects/Environment/M10ShotPresentationTuning.asset`
- gameplay shot/physics/terrain values: 기존 M2–M5 ScriptableObjects. M11에서는 변경하지 않았다.


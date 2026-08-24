# SwingPop VFX Hero Pass Review

## Goal

기존 M9 presentation architecture와 gameplay 결과를 유지하면서 Normal / Great / Perfect를 즉시 구분하고, Perfect Shot의 contact–launch–flight 0.5~1.0초를 SwingPop의 Hero Moment로 강화한다. 작업 범위는 `Hole01_SkyIsland`의 VFX와 기존 procedural audio balance에 한정한다.

## Baseline

- 기존 M9에는 Normal/Perfect 구분, impact burst, trail, surface landing, water, Hole-In과 event 동기화가 있었다.
- impact는 단일 burst처럼 보여 방향성과 등급 차이가 약했다.
- trail은 단순 선에 가까웠고 속도감과 tail taper가 부족했다.
- landing과 Hole-In은 기능적으로 반응했지만 surface/reward identity가 약했다.
- HUD Skin Pass의 중앙 gameplay 시야와 기존 Camera/HUD/Audio binding은 유지해야 했다.

## Visual Language

- 공통: bright fantasy, sporty energy, clean, directional, compact, readable.
- 기본 색: cyan / white / mint. Perfect만 pale gold accent를 더한다.
- Hazard: water는 cyan/light blue, miss/OOB는 기존 HUD/Audio 중심이다.
- Hole-In: cyan / white / gold.
- 6종 project-safe sprite(soft glow, streak, ring, sparkle, dust, splash)와 6개 shared URP particle material을 사용한다.

## Normal

- 작은 cyan/white core flash, compact ring/burst, 짧은 launch-direction streak를 사용한다.
- hit 확인은 가능하지만 화면과 Character를 덮지 않도록 가장 낮은 scale/count/trail profile을 사용한다.
- Good/Miss gameplay 판정을 바꾸지 않고 Normal presentation family로만 수렴시킨다.

## Great

- 기존 `ImpactGrade.Great`를 별도 presentation level로 명시했다.
- Normal보다 조금 밝고 크며 trail lifetime/width가 증가한다.
- Perfect의 gold accent, 최대 scale, 최대 sparkle 수에는 도달하지 않는다.

## Perfect

- 가장 밝은 contact core, cyan/white ring과 radial sparkle, 확정 launch 방향 streak, pale gold accent를 조합한다.
- `Ball.Launched`에서 HUD PERFECT, Camera impact, impact/audio와 동시에 시작한다.
- full-screen flash나 지속 폭발 없이 contact point와 첫 비행 구간만 강조한다.

## Trail

- Outer / Core / Accent의 3개 TrailRenderer를 재사용한다.
- width curve로 head에서 tail까지 zero taper를 적용하고 gradient alpha를 줄인다.
- Normal < Great < Perfect 순서로 lifetime과 width가 증가한다.
- 고속일 때만 짧은 speed streak가 추가되며 ball은 계속 보인다.
- Putter, Ready, Stopped, Holed, Hazard, Reset에서는 emit을 끄고 clear한다.

## Landing

- Fairway/Green: 작은 mint grass puff와 soft ring.
- Rough: 더 큰 dark green burst로 무게감을 높인다.
- Bunker: warm sand upward/outward dust와 ring, 조건부 작은 secondary bounce만 허용한다.
- surface 선택은 기존 `TerrainSurfaceType` event를 사용하며 VFX는 terrain을 재판정하지 않는다.

## Water

- vertical splash, outward ring, droplets의 3-layer profile을 사용한다.
- cyan/white/light blue palette를 Course Environment Pass water와 맞췄다.
- complex simulation이나 gameplay hazard 변경은 없다. OOB는 기존 HUD/Audio 중심을 유지한다.

## Putter

- Driver profile에 비해 scale과 particle count를 낮춘 clean contact spark만 사용한다.
- Perfect 판정이어도 별도 Perfect audio accent와 flight trail을 사용하지 않는다.
- Green에서 Ball/Cup visibility를 우선하며 rolling glow나 ground trail을 추가하지 않았다.

## Hole-In

- Hole completion 이후 cup flash → upward sparkle + soft ring → 짧은 celebration sparkle로 실행한다.
- `ShotPresentationController`의 guard가 같은 Hole completion의 중복 hero event를 막는다.
- Result panel, Camera, Character celebration과 같은 완료 event를 관찰하지만 VFX가 flow를 직접 제어하지 않는다.

## Audio Sync

- 기존 category AudioSource 구조를 유지했다.
- Normal < Great < Perfect impact level을 사용하며 Perfect accent는 Driver에만 더한다.
- Putter impact volume은 축소했고 기존 Bunker/Water/Hole-In cue route를 유지했다.
- 최종 clip, AudioMixer, loudness mastering은 아직 placeholder다.

## Performance

자동 capture/validator 기준:

| Metric | Result |
| --- | ---: |
| Particle Systems | 15 |
| Sampled peak active particles | 69 |
| Configured particle capacity | 632 |
| VFX renderers | 18 |
| Transparent VFX renderers | 18 |
| Shared VFX materials | 6 |
| Effect objects | 18 |
| Trail renderers | 3 |
| Scene GameObjects | 568 |
| Scene renderers | 384 |
| Scene active transparent slots | 21 |
| Scene AudioSources | 5 |
| Scene Update behaviours | 12 |

15개 ParticleSystem과 3개 TrailRenderer는 고정 prefab graph이며 반복 shot 후 object count가 증가하지 않는 것을 PlayMode test로 확인했다. runtime code에서 shot마다 Material/Gradient/effect GameObject를 만들지 않는다. Unity Profiler의 CPU/GPU frame time, GC Alloc, overdraw, Batches/SetPass는 수동 측정하지 않았다.

## Regression

- VFX Hero validator: PASS — ParticleSystems 15, Trails 3, Materials 6, Impact 5 layers, Landing 5 layers, Hole-In 4 layers, Collider 0, Missing Script/Reference/Sprite 0, duplicate controller 0.
- EditMode: 136/136 PASS.
- PlayMode: 7/7 PASS — grade profile, trail strength/cleanup, surface profiles, Putter reduction, Hole-In single event, repeated-shot object stability 포함.
- HUD validator: PASS — Canvas 1, EventSystem 1, Raycast target 1, Missing Sprite/Script 0.
- Course validator: PASS — Colliders 10, Course Art Collider 0, ParticleSystems 15, AudioSources 5, Missing Script/Mesh/Material 0.
- M10/M11 structure validators: PASS — gameplay collider, camera tuning, HUD, surface set, presentation budgets 유지.
- `Foundation.unity`와 기존 M9 presentation prefab은 변경하지 않았다.
- Unity Editor의 사람이 직접 조작한 전체 Hole Play Mode, Console Error 0, Profiler 검증은 실행하지 않았다.

## Before / After

- Before reference: `docs/review-captures/hud-skin-pass/I-Impact.png`, `D-Perfect-Flight.png`, `H-Result.png`.
- After: `docs/review-captures/vfx-hero-pass/N/G/P/D/B/W/F/H` 8장.
- contact는 단일 burst에서 core/ring/directional/accent의 층으로 바뀌었고 Great가 중간 단계로 분리됐다.
- Perfect flight는 단일 선에서 bright core, soft outer, tapered tail, speed accent로 바뀌었다.
- Bunker/Water는 색뿐 아니라 dust와 splash/ring/droplet의 motion family로 구분된다.
- 개선 후에도 impact world effect가 HUD popup보다 시각적으로 작고 Hole-In sparkle가 Result panel 뒤에서 약하게 읽히는 한계가 있다.

## Quality Scorecard

| 항목 | 등급 | 근거 |
| --- | :---: | --- |
| Normal Impact | B | restrained contact와 clear cyan family |
| Great Impact | B | Normal과 Perfect 사이 scale/brightness 확보 |
| Perfect Impact | B | gold/directional/layer 차이, authored flash는 미완료 |
| Perfect Hero Moment | C | sync는 명확하지만 world VFX보다 HUD 존재감이 큼 |
| Trail | B | core/outer/accent와 taper 확보 |
| Speed feeling | B | speed gate와 directional streak, shader distortion 미완료 |
| Fairway landing | C | profile/자동 검증 완료, 최종 grass shape 미완료 |
| Rough landing | C | 별도 tone/scale, authored vegetation particle 미완료 |
| Bunker | B | warm dust와 ring으로 surface identity 명확 |
| Water | B | splash/ring/droplet 구분, final sheet/shader 미완료 |
| Putt impact | B | 축소 contact와 trail 제거로 목적에 맞음 |
| Hole-In | C | 4-layer/중복 방지는 완료, Result 구도에서 보상감 약함 |
| HUD/VFX sync | B | 동일 Ball.Launched/HoleCompleted event 사용 |
| Camera/VFX sync | B | 기존 Camera event 유지, 새 직접 제어 없음 |
| Visual consistency | B | 공통 sprite/palette/material family |
| Screen readability | B | Character/Ball/HUD를 덮지 않음 |
| Overall game-feel | B | prototype feedback보다 명확, final asset polish 필요 |
| Game-vs-prototype impression | C | generated sprites/procedural audio가 여전히 보임 |

## Remaining Gaps

- authored impact sprite/flipbook 또는 stylized shader와 더 명확한 Perfect contact silhouette
- sky/ground 양쪽에서 일관된 최종 trail texture, shader, distortion/soft intersection
- grass/sand/water의 authored particle sheet와 surface shader interaction
- Hole-In을 Result panel과 함께 읽히게 하는 timing/framing 및 score별 intensity polish
- subtle spin accent art
- licensed final audio, AudioMixer routing, loudness/limiter
- PC Development Build Profiler, overdraw, flash safety와 low/mobile quality 검증
- 실제 full-hole manual pass와 Console Error 0 확인

## Recommendation

`VFX Hero Foundation: GO`. 기존 gameplay와 M9 presentation boundary를 유지하면서 Normal/Great/Perfect, trail, landing, water, Putter, Hole-In을 교체 가능한 data/prefab 구조로 한 단계 끌어올렸다.

`Commercial VFX Approval: NO-GO`. Perfect/Hole-In의 최종 authored effect, trail/surface shader, licensed audio, 수동 full-hole/Profiler/flash-safety 검증이 남아 있다. 다음 기능 또는 Putt/Result Cinematic Pass는 이번 작업에서 시작하지 않는다.

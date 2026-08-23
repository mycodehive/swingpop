# M10 Sky Island Hole 1 Vertical Slice Review

## Overview

`Assets/_Game/Scenes/Hole01_SkyIsland.unity`는 Foundation의 M1–M9 gameplay/presentation graph를 보존하면서 밝은 fantasy sky-island 환경으로 통합한 M10 플레이 씬이다. Foundation은 개발 회귀 검증용으로 유지되며 Build Settings에는 Hole01이 첫 씬, Foundation이 두 번째 씬으로 등록된다.

구조적 Vertical Slice 통합은 완료됐지만 최종 character, authored environment, UI skin, VFX texture, licensed audio가 없는 placeholder 단계다. Game View 육안 승인, 실제 스피커 청취, 해상도별 캡처와 Profiler 60 FPS 검증도 아직 사용자 수동 검증이 필요하다.

## Gameplay Completeness

- HoleIntro → Address → Aim → Power → Impact → Character Swing → Ball Flight → Landing/Bounce/Roll → Next Shot → Green/Putter → Hole-In → Celebration/Result 구조가 한 씬에 연결돼 있다.
- Tee/Fairway/Rough/Bunker/Green/Water/OOB는 기존 `TerrainSurfaceData`를 그대로 source of truth로 사용한다.
- 자동 PlayMode 테스트에서 Normal/Perfect launch, 실제 Water recovery, 실제 Putter Hole-In과 Result를 통과했다.
- 장식 환경 root 아래에는 Collider가 없어 art가 gameplay collision을 변경하지 않는다.

## Art Quality

- Procedural skybox, cyan daylight, shared green/sand/water/cliff palette를 사용한다.
- 반복 자산은 `StylizedTree`, `FlowerPatch`, `CloudCluster`, `FloatingIsland`, `Windmill` prefab으로 관리한다.
- main floating-island silhouette, edge vegetation, 네 개 원경 부유섬, waterfall island, windmill landmark로 prototype의 빈 화면을 줄였다.
- 지형과 모든 props는 primitive placeholder다. authored silhouette, terrain sculpt, foliage variation, texture detail은 최종 아트 작업이 필요하다.

## Camera Quality

- 기존 M6 CameraDirector만 사용하며 별도 camera system이나 Cinemachine을 추가하지 않았다.
- Hole01 전용 `M10CameraTuning.asset`은 3초 intro, Address 46 FOV, Follow 52 FOV, Landing 47 FOV를 사용한다.
- 자동 검증은 mode transition과 Putter Hole-In framing의 기능 연결을 확인했다. Character/Ball/Cup/landmark가 실제 Game View에서 이상적으로 구성되는지는 육안 검증 전이므로 승인하지 않았다.

## UI Quality

- M8 uGUI HUD 구조, 1920×1080 reference resolution, keyboard/mouse 공용 command path를 유지한다.
- M10 scene의 작은 keyboard hint는 `A/D AIM   SPACE / CLICK SHOT   1-5 SPIN`으로 확장하고 Aim/Distance/Wind/Power/Impact/Shot hierarchy는 그대로 사용한다.
- M10 전용 HUD tuning은 aim screen margin과 impact feedback duration만 조정한다.
- Debug overlay와 trajectory는 기본 숨김이며 H/F1로 함께 켜고 끈다.

## VFX / Audio Quality

- M9 Impact, Perfect, trail, landing, Water, Hole-In VFX와 category AudioSource route를 재사용한다.
- 밝은 sky에서 공을 찾기 쉽도록 Hole01 전용 trail 폭/lifetime을 소폭 늘렸다.
- ambient audio hook와 전용 AudioSource는 추가했으나 clip은 비어 있어 자동 재생되는 복잡한 가짜 ambience를 만들지 않았다.
- 최종 sprite/flipbook/shader와 licensed clip은 아직 없다.

## Reference Comparison

직접 Game View screenshot 비교를 수행하지 않았기 때문에 아래 등급은 구조와 설정을 기준으로 한 잠정 평가다.

| Category | Rating | Evidence / Gap |
|---|---|---|
| Character scale | Needs Work | primitive character이며 실제 1080p screenshot 검증 전 |
| Character silhouette | Acceptable | cyan/pink/dark color block은 있으나 최종 anime silhouette 아님 |
| Camera distance | Acceptable | data-tuned M6 composition, 육안 승인 전 |
| Course depth | Acceptable | 78m hole, foreground/mid/background islands 구성 |
| Course density | Acceptable | edge trees/flowers/landmarks 추가, authored variation 부족 |
| Sky | Acceptable | bright procedural cyan sky, texture/HDRI 없음 |
| Lighting | Acceptable | one soft-shadow directional light와 trilight ambient |
| Landmark | Acceptable | windmill와 waterfall island가 있으나 primitive |
| Environment variety | Acceptable | five reusable prefab types, final set dressing 부족 |
| UI hierarchy | Acceptable | M8 기능 hierarchy 유지, final skin/font 없음 |
| Wind readability | Acceptable | HUD/data 연결 자동 검증, 밝은 sky 대비 육안 검증 전 |
| Distance readability | Acceptable | HUD/data 연결 자동 검증, 실제 screenshot 검증 전 |
| Shot UI presence | Acceptable | Power/Impact/primary action 연결, final authored skin 없음 |
| Trail readability | Acceptable | M10 폭/lifetime 보강, Game View 육안 검증 전 |
| Impact presentation | Acceptable | Normal/Perfect 분리 자동 검증, 최종 VFX/청취 승인 전 |
| Color palette | Good | sky blue/green/white base와 pink/cyan/gold/mint accent를 shared material로 일관 적용 |

## Performance

- Editor 구조 감사 결과: GameObject 289, Renderer 157, 전체 shared material 34, transparent renderer slot 2, shadow caster 145, Collider 10, ParticleSystem 7, AudioSource 5, 자체 `Update` method 보유 behaviour 12, Missing Script 0이다.
- M10 PlayMode 구조 검증에서 환경 renderer 70–180, ParticleSystem 8 이하, AudioSource 6 이하 budget도 확인한다.
- 환경 prefab의 primitive Collider는 생성 시 제거되며 장식 root 전체 Collider 수는 0이다.
- 구름 다섯 개와 풍차 rotor는 각 오브젝트별 Update가 아니라 `SkyIslandEnvironmentMotion` 하나가 갱신한다.
- cloud는 opaque shared material을 사용해 layered transparency overdraw를 피했다. Water/Waterfall만 transparent material이다.
- realtime shadow caster 145개는 최종 asset/Profiler 단계에서 우선 검토할 비용 후보다.
- 실제 draw call, GPU/CPU frame time, shadow cost, 1920×1080 60 FPS는 Profiler로 측정하지 않았다.

## Known Issues

- 최종 character/environment/UI/VFX/audio asset이 없다.
- flat blockout collider를 보존했으므로 visual shaping은 실제 slope physics가 아니다.
- ambient loop clip이 비어 있어 환경 ambience는 무음이다.
- transparent water는 단순 색/alpha이며 파동, shoreline, reflection이 없다.
- foliage LOD, batching, occlusion, baked lighting은 최종 자산 기준으로 다시 설계해야 한다.
- Game View screenshot, camera comfort, VFX/audio 체감, 세 해상도, Profiler 검증이 남아 있다.

## Final Art Missing

상용 품질 이전 필수 교체 항목은 `docs/TODO_ART.md`의 `Required Before Commercial Quality`에 정리했다. 현재 결과는 structural quality complete에 가까운 art placeholder이며 final art complete가 아니다.

## M11 Priorities

M11은 이번 작업에서 시작하지 않았다. 다음 milestone 승인 후 우선순위 후보는 실제 Game View screenshot review, camera occlusion/comfort 수정, 16:9 HUD spacing, final asset 교체, VFX/audio mix, Profiler 기반 성능 budget, bug/quality gate다.

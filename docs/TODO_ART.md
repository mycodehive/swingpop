# SwingPop Final Art Replacement Checklist

ART PASS 1은 최종 아트가 아니라 교체 가능한 Character / Environment foundation이다. 아래 항목이 완료되기 전에는 Commercial Art 완료로 판정하지 않는다.

## Final Character

- [ ] SwingPop 고유 IP의 anime-style character FBX/model
- [ ] readable face, authored hair, outfit, hands, shoes, portrait
- [ ] Humanoid rig, Avatar, skin weights, final material/texture
- [ ] 모델 pivot, visual height/bounds, CharacterScale, GroundOffset를 캐릭터별 `CharacterVisualProfile`에 기록
- [ ] Character prefab의 `VisualRoot` 아래에 최종 visual을 배치
- [ ] `CharacterVisualAdapter`의 Animator, valid Humanoid Avatar, ClubSocket, Hand/LeftHand/RightHandSocket, ImpactAnchor, HeadLookTarget 연결
- [ ] `PortraitReference`용 original portrait asset 연결
- [ ] Driver/Putter가 손과 겹치지 않고 Address/Swing/Putt에서 읽히는지 확인
- [ ] character visual hierarchy 안 Collider 0 확인

Character Identity Pass pipeline 완료 항목:

- [x] VisualHeight/bounds/scale/ground/address/camera/socket/portrait metadata를 가진 `CharacterVisualProfile`
- [x] Animator/Avatar와 optional left/right hand socket을 지원하는 `CharacterVisualAdapter`
- [x] `HumanoidGolferTemplate.prefab` integration hierarchy
- [x] `SwingPop > Character > Validate Character Setup` validator
- [x] gameplay launch 없는 Character state preview window
- [x] original identity, rig, delivery 및 초보자 integration guide

위 항목은 final model/Avatar/clip이 들어왔다는 뜻이 아니다.

교체 위치: `Assets/_Game/Prefabs/Characters/PlaceholderGolfer.prefab`의 `Visual Root`. Gameplay root와 controller는 유지하고 visual, Animator, profile reference만 교체한다.

## Final Animation

- [ ] Idle
- [ ] Address
- [ ] BackSwing
- [ ] Swing
- [ ] FollowThrough
- [ ] WatchBall
- [ ] PuttAddress / PuttBackSwing / PuttSwing / PuttFollowThrough
- [ ] Happy / Sad / Birdie / Eagle / Hole-In-One celebration
- [ ] Swing/Putt clip에 단일 Impact Animation Event 연결
- [ ] `CharacterAnimationController.NotifyImpactAnimationEvent()` 호출 확인
- [ ] Animator state name과 `CharacterState` mapping 검증
- [ ] `CharacterAnimatorContract`의 15개 state 이름과 Controller Base Layer state 일치
- [ ] procedural placeholder를 끈 뒤 launch timing regression 확인

## Environment Models

- [ ] authored sky-island terrain/cliff/underside mesh와 gameplay collider 정합
- [ ] Tree A/B/C 최종 모델, LOD, pivot, shared materials
- [ ] flower/low foliage patch, cloud variants, distant floating islands
- [ ] original fantasy windmill, waterfall island, waterfall mist
- [ ] cup, flag, flag animation, course signage
- [ ] foreground / midground / background composition 최종 배치
- [ ] flowers/clouds/distant props shadow OFF, 가까운 큰 tree/landmark만 selective shadow
- [ ] 모든 decorative prefab Collider 0, Missing Script 0

## Course Textures

- [ ] Tee/Fairway/Rough/Green/Bunker authored stylized textures/materials
- [ ] Fairway 2~3 tone band와 Rough 경계, Green의 낮은 visual noise
- [ ] bunker rim/inner sand/depression detail
- [ ] cliff upper rim/body/underside tone separation
- [ ] water shallow/deep/highlight, shoreline, waterfall tone consistency
- [ ] URP/mobile-compatible shader, material batching, texture memory 검증
- [ ] Aim/Ball/Cup readability를 가리지 않는 detail density 확인

## UI Skin

- [ ] original fantasy HUD 9-slice panels, portrait/frame, icon set
- [ ] Wind/Club/Spin/Aim icons와 Power/Impact/Result skin
- [ ] licensed/localization-safe font와 한글/영문 outdoor readability
- [ ] 1920×1080, 1600×900, 1280×720 safe-area 검증

## VFX

- [ ] authored Normal/Perfect impact sprites, flipbook 또는 shader
- [ ] Normal/Perfect trail, camera-safe overdraw 검증
- [ ] Fairway/Green/Rough/Bunker landing과 Water splash
- [ ] Hole-In ring/sparkle/celebration VFX
- [ ] final flag/waterfall/foliage ambient motion

## Audio

- [ ] licensed UI confirm, swing, putt, normal/perfect impact
- [ ] surface landing/roll, Water/OOB hazard
- [ ] Hole-In, result stinger, wind/water/course ambience
- [ ] AudioMixer groups, limiter, mobile/PC loudness 확인

## Technical Art Gate

- [ ] PC 1080p Development Build에서 60 FPS target 검증
- [ ] CPU/GPU frame time, GC Alloc, Batches, SetPass, memory 기록
- [ ] final Renderer/Material/Transparent/Shadow/Particle/Update budget 기록
- [ ] A/D/F/H final screenshot 재캡처 및 target-quality와 비교
- [ ] Console Error 0, Missing Script 0, Missing Material 0, Missing Reference 0

## ART PASS 1 Completed Placeholders

- [x] `CharacterVisualAdapter` / `CharacterVisualProfile` replacement seam
- [x] segmented placeholder body, face hint, 5-part hair silhouette, Driver/Putter distinction
- [x] Tree A/B/C, flower patch, cloud A/B/C reusable prefab family
- [x] fairway/rough/green/bunker/cliff/water palette tuning과 moving water highlight
- [x] windmill/waterfall landmark hierarchy, lighting/fog pass
- [x] presentation art Collider 0, shared material reuse, four 1920×1080 review captures

## Course Material & Environment Pass Completed Placeholders

- [x] Fairway 3-tone broad mowing, irregular fringe, Rough value separation
- [x] Green fine mowing/fringe와 Cup dark interior/bright rim
- [x] Bunker rim/light sand/inner shade/grain visual hierarchy
- [x] Water shoreline/shallow/deep/limited moving highlight
- [x] Main island grass/upper/mid/dark cliff hierarchy와 ledges
- [x] Tree A/B/C, flower patch, grass accent, stone, cloud/island prefab variants
- [x] authored-placeholder Windmill과 source/body/mist를 구분한 Waterfall island
- [x] central environment motion reuse, Course pass Collider 0, per-object Update 증가 0
- [x] `SwingPop > Environment > Validate Course Art` validator
- [x] A/D/F/H/W/B 1920×1080 review capture set

Course pass 이후에도 다음은 최종 상용 아트 미완료 항목이다.

- [ ] authored grass texture/shader
- [ ] sculpted terrain mesh와 gameplay collider alignment
- [ ] final water shader
- [ ] final cliff texture/material breakup
- [ ] final tree/foliage asset set와 LOD
- [ ] final flowers, stones, course props
- [ ] final Windmill, floating island, Waterfall assets
- [ ] final flag/waterfall/foliage ambient animation
- [ ] environment audio와 AudioMixer tuning

이 완료 목록은 최종 character/environment asset 완료를 뜻하지 않는다. 최종 교체 source of truth는 위 미완료 checklist다.

## HUD Skin Pass Completed Placeholders

- [x] `HudSkinData`에 panel/accent/text/state palette와 shared sprite reference 집중
- [x] Hole01 전용 `GameplayHUD_SwingPopSkin.prefab`으로 Foundation HUD와 분리
- [x] rounded panel/capsule과 Player/Wind/Driver/Putter/Spin/Target generated icon family
- [x] Impact/Lie/Score/Shot state의 presentation-only tone mapping
- [x] compact Player/Hole/Wind/Club, Power/Impact gauge, hero Shot Button, popup/result card
- [x] Shot Button 단일 raycast target, Canvas/Material/Update behaviour 증가 방지
- [x] `SwingPop > UI > Validate Gameplay HUD` validator
- [x] A/P/I/D/F/W/H 1920×1080 및 Address 1600×900/1280×720 review capture

HUD Skin Pass 이후에도 다음은 최종 상용 UI 미완료 항목이다.

- [ ] licensed/localization-safe final font와 한글/영문 spacing/overflow 검증
- [ ] final original character portrait와 portrait frame
- [ ] authored scalable 9-slice panel/capsule asset
- [ ] final Wind/Club/Spin/Aim/Stroke/Penalty icon set
- [ ] final Power/Impact gauge, cursor, restrained glow asset
- [ ] final Shot Button skin, hover/press motion과 UI audio
- [ ] final Result emblem/badge, Hazard/Penalty badge
- [ ] 색각/고대비/accessibility 검증
- [ ] Power/Impact/Result를 포함한 3개 16:9 해상도 수동 순회
- [ ] Unity Profiler CPU/UI/Rendering/Memory, GC Alloc, Batches/SetPass 기록

위 완료 목록은 상용 HUD 승인이나 final authored UI asset 완료를 뜻하지 않는다. 상세 판정은 `HUD_SKIN_PASS_REVIEW.md`를 따른다.

## Putt / Result Cinematic Pass Completed Placeholders

- [x] Character/Ball/Cup/Flag/Green을 함께 읽는 Putt Address framing
- [x] Ball/Cup 중심 Rolling framing과 presentation-only Cup Approach threshold
- [x] Hole-In cup flash → ring/upward sparkle → celebration sparkle timing
- [x] Hole-In → Character reaction → Result reveal/hold coordinator와 duplicate guard
- [x] Character-left / Result-right composition과 score-first 3-step Result reveal
- [x] 기존 ScoreResult, Character celebration mapping, CameraDirector, HUD, VFX, Audio 경로 재사용
- [x] Putt aim line의 Cup 거리 제한과 HoleComplete visibility 정리
- [x] `SwingPop > Presentation > Validate Putt Result Cinematic` validator
- [x] F1/F2/F3/H1/H2/H3 1920×1080 review capture set

Putt / Result Cinematic Pass 이후에도 다음은 최종 상용 presentation 미완료 항목이다.

- [ ] final Putt/celebration authored animation과 final Character FBX 기준 camera retune
- [ ] authored Result badge/emblem, localization-safe font, typography polish
- [ ] Hole-In score별 authored VFX variation
- [ ] licensed Cup drop/success/result audio와 AudioMixer mastering
- [ ] 1280×720/1600×900 safe-area와 여러 Green 경사/거리 camera collision 검증
- [ ] PC Development Build Profiler CPU/GPU/GC/Batches/SetPass 기록

이 완료 목록은 presentation foundation 완료를 뜻하며 최종 art/audio 승인을 뜻하지 않는다. 상세 평가는 `PUTT_RESULT_CINEMATIC_PASS_REVIEW.md`를 따른다.

## Final Graphics Quality Gate Backlog

이 구역은 2026-08-24 Final Graphics Quality Gate 이후의 최종 source of truth다. 이미 완료된 placeholder pass 기록은 아래에 보존하지만, 추가 primitive/generated-asset polish는 수동 검증에서 새 P0/P1이 발견될 때만 수행한다.

### Must Fix Before Next Stage

- [ ] `Hole01_SkyIsland`을 1920x1080에서 처음부터 Result까지 수동 3회 플레이하고 camera cut/occlusion/motion comfort를 승인
- [ ] Unity Console Error 0, Missing Script 0, Missing Reference 0을 GUI Editor에서 최종 확인
- [ ] Normal/Perfect, Water, Bunker, Green/Putter, Hole-In/Reaction/Result 연속 흐름을 직접 확인
- [ ] 1600x900과 1280x720에서 Address/Power/Putt/Result clipping과 overflow를 수동 확인
- [ ] Profiler 10초 이상과 PC 1080p Development Build 60 FPS 근거를 기록

### Final Character Asset

- [ ] original SwingPop female golfer final FBX, Avatar, skin weights, LOD
- [ ] final face/hair/hands/outfit/shoes/glove/belt/accessory materials와 textures
- [ ] Driver/Putter final model과 grip/contact alignment
- [ ] authored Idle, Address, Aim, Swing, Putt, BallWatching, Celebration, Disappointed animation set
- [ ] final Character proportions 기준 Address/Putt/Cup/Result camera retune

### Final Environment Asset

- [ ] authored Tee/Fairway/Rough/Green/Bunker terrain mesh와 stylized material set
- [ ] cliff breakup, shoreline, water/waterfall shader와 foam treatment
- [ ] Tree/grass/flower/stone reusable variants와 LOD/mobile fallback
- [ ] final Windmill, floating island, Waterfall landmark models
- [ ] gameplay collider를 변경하지 않는 visual-only replacement와 Ball/Aim/Cup readability 확인

### Final UI Asset

- [ ] licensed/localization-safe final font와 한글/영문 glyph/overflow test
- [ ] final original character portrait와 portrait frame
- [ ] authored scalable 9-slice panel/capsule set
- [ ] Wind/Club/Spin/Aim/Stroke/Penalty icon set
- [ ] final Power/Impact gauge, cursor, Shot Button, Result emblem/badge
- [ ] color sensitivity, contrast, hover/press feedback 검증

### Final VFX / Audio

- [ ] authored Normal/Great/Perfect impact sprite, flipbook 또는 stylized shader
- [ ] final tapered flight trail과 sky/terrain contrast profile
- [ ] authored grass/sand/water landing particle set
- [ ] score별 Hole-In ring/sparkle/celebration variation
- [ ] licensed UI/swing/putt/impact/landing/hazard/Hole-In/result SFX
- [ ] wind/water/course ambience, AudioMixer groups, limiter, speaker/headphone mastering

### Performance

- [ ] Unity Profiler CPU/GPU frame time, GC Alloc, Batches, SetPass, memory 기록
- [ ] PC 1920x1080 Development Build에서 60 FPS target 검증
- [ ] transparent overdraw, particle peak, shadow caster budget 재검증
- [ ] final Character/Environment LOD와 low-quality/mobile fallback 검증

### Commercial Polish

- [ ] final asset 적용 후 19장 master capture before/after 비교
- [ ] final animation/VFX/audio 적용 후 camera timing과 shake comfort 재승인
- [ ] localization, safe area, flash safety, color accessibility 검증
- [ ] store/marketing capture는 `COMMERCIAL ART: GO` 재판정 후에만 제작
- [ ] online 작업 전 Rigidbody authority/determinism/reconciliation 정책 별도 설계

## VFX Hero Pass Completed Placeholders

- [x] Normal / Great / Perfect의 명시적 impact presentation profile
- [x] core flash, radial ring/burst, directional streak, accent sparkle의 layered impact
- [x] bright core, soft outer fade, tapered tail, Perfect gold accent를 갖는 3-layer trail
- [x] speed-gated streak와 Ready/Stopped/Holed/Hazard/Reset cleanup
- [x] Fairway/Green/Rough/Bunker 및 Water splash/ring/droplet surface mapping
- [x] reduced Putter contact와 Putter trail disable
- [x] cup flash/upward sparkle/ring/celebration Hole-In graph와 duplicate event guard
- [x] soft glow/streak/ring/sparkle/dust/splash generated sprite family와 shared URP materials
- [x] `SwingPop > VFX > Validate Hero VFX` validator와 Editor preview/capture set
- [x] N/G/P/D/B/W/F/H 1920×1080 review capture

VFX Hero Pass 이후에도 다음은 최종 상용 VFX 미완료 항목이다.

- [ ] authored Normal/Great/Perfect impact sprite, flipbook 또는 stylized shader
- [ ] final tapered Trail texture/shader와 sky/terrain별 contrast tuning
- [ ] authored grass blade/leaf landing particle
- [ ] authored sand dust/grain particle와 secondary puff tuning
- [ ] authored water splash sheet, droplet shape와 water shader interaction
- [ ] Hole-In score별 authored ring/sparkle/celebration effect
- [ ] subtle spin-specific secondary accent art
- [ ] licensed final swing/putt/impact/landing/hazard/Hole-In/result audio와 AudioMixer
- [ ] PC Development Build Profiler CPU/GPU/GC/Batches/SetPass/overdraw 검증
- [ ] color sensitivity, flash safety, low-quality/mobile profile 검증

위 완료 목록은 commercial VFX 승인이나 최종 audio 승인을 뜻하지 않는다. 상세 판정은 `VFX_HERO_PASS_REVIEW.md`를 따른다.

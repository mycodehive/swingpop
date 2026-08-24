# SwingPop Art Direction v0.1

## Vision

Bright Fantasy + Anime Character + Stylized 3D Golf.

참고 이미지:
`docs/reference/target-quality.png`

참고 이미지는 품질과 화면 밀도 기준이며 기존 IP의 구체 리소스를 복제하지 않는다.

## Color

Base:

- bright sky blue
- vivid grass green
- white clouds

Accent:

- pink
- mint
- yellow/gold
- cyan
- violet

Danger/feedback:

- orange/red는 Impact/Miss 등 필요한 곳에 제한적으로 사용

## Environment

Sky Island Golf Club:

- lush stylized grass
- pink flowering trees
- windmill landmark
- small water feature
- waterfall
- floating islands
- fantasy house silhouettes
- moving clouds

## Character

초기 Placeholder 단계:
- Capsule 또는 무료/라이선스 확인된 base character

최종 방향:
- anime-inspired
- readable face
- slightly exaggerated proportions
- clear silhouette
- oversized but believable swing gesture
- colorful outfit

## Camera Composition

Address:
- 캐릭터가 화면 좌/우 한쪽에 크게 위치
- 공과 목표 방향이 동시에 보임
- 코스 원경 랜드마크가 읽힘

Ball Flight:
- 공이 작아져도 Trail로 위치를 잃지 않음
- horizon과 target area가 보이도록 구성

## Materials

- URP compatible
- stylized diffuse/specular
- realistic PBR 값을 그대로 쓰기보다 형태 읽힘 우선
- grass/fairway/green의 색과 roughness 차이를 분명하게

## Lighting

- bright daylight
- soft shadows
- strong readability
- overly contrasty cinematic lighting 금지
- mobile scalability 고려

## UI Style

- rounded
- glossy/soft glow는 적당히
- cyan/green 기반
- 큰 primary action
- 작은 정보는 그룹화
- text outline/shadow로 outdoor background에서 가독성 유지

## Motion

UI는 정적 이미지가 아니라 작은 반응을 가진다.

- Button breathing
- Gauge glow
- Perfect pulse
- Wind arrow motion
- Score number tween

모든 요소를 계속 움직이지 않는다.

## VFX

Normal:
- compact

Perfect:
- stronger but clean

공 궤적은 gameplay readability 요소이므로 너무 투명하게 만들지 않는다.

## Quality Review Checklist

- Character scale
- Camera distance
- Course depth
- Landmark visibility
- Sky quality
- Color saturation
- Surface distinction
- UI hierarchy
- Aim guide readability
- Wind readability
- Trail readability
- Impact response
- Landing feedback

## M6 Placeholder Camera Composition

- Address/Aim은 reference의 화면 밀도와 third-person 방향성을 기준으로 공과 fairway depth를 함께 읽게 한다. Character 자리는 화면 한쪽에 남겨 두되 M7 전에는 가짜 character를 만들지 않는다.
- Flight는 속도 기반 거리/FOV와 높은 arc의 추가 높이로 공 궤적을 읽게 하고, Landing은 지면과 공을 함께 유지한다.
- Putt는 낮고 가까운 구도에서 ball-to-cup line을 우선하며, HoleComplete/Result는 cup을 presentation anchor로 사용한다.
- 현재 course와 ball은 blockout이므로 최종 색감·landmark·character scale 품질은 평가 대상이 아니다. M6 평가는 framing, continuity, readability, motion comfort에 한정한다.

## M7 Placeholder Character Direction

- 외부 asset 없이 body/head/arms/legs/ClubSocket primitive hierarchy로 gameplay scale과 silhouette를 검증한다.
- cyan outfit, pink accent, dark hair/legs로 밝은 course에서 캐릭터 형태를 구분한다. 이는 최종 anime design이 아니다.
- Address camera에서 Character, Ball, Aim line, fairway depth가 동시에 읽혀야 한다. Debug overlay가 가리면 `H`로 숨긴다.
- Driver swing은 torso/arm/club의 큰 회전, Putter는 낮고 짧은 motion으로 구분한다.
- 최종 character는 같은 prefab adapter와 ClubSocket을 유지하면서 licensed model, humanoid rig, clips로 교체한다.

## M8 Placeholder HUD Direction

- reference의 상단 정보 밀도, 중앙 Aim/Distance 가독성, 하단 timing interaction 존재감만 quality bar로 사용하고 고유 디자인은 복제하지 않는다.
- cyan/mint/white/blue를 기본으로 하고 Perfect에는 gold, hazard/miss에는 orange-red를 제한적으로 사용한다.
- 어두운 반투명 rounded-style panel, outline/shadow, 큰 typography hierarchy로 밝은 outdoor 배경에서도 읽히게 한다.
- Player/Hole/Wind를 세 모서리 그룹으로 분리하고, Club/Spin과 Power/Impact/Primary Action을 하단에 두어 Character와 course 중앙 시야를 남긴다.
- 기본 Unity uGUI sprite와 `LegacyRuntime.ttf`만 사용한 placeholder이며 최종 portrait/icon/gauge/button/font asset으로 교체 가능하다.
- motion은 action breathing, active power glow, Impact popup pulse, wind 방향 smoothing, Result fade/scale에 한정한다.

## M9 Placeholder VFX / Audio Direction

- target-quality의 밝은 fantasy 반응성, 공 궤적 가독성, Character/UI/World effect 균형만 참고하고 고유 effect 디자인은 복제하지 않는다.
- Normal impact는 compact cyan/white, Perfect는 cyan/white에 gold accent를 더한다. Perfect 차이는 particle 수만이 아니라 brightness, scale, streak, trail lifetime/width, audio layer, 기존 camera/HUD 강도로 만든다.
- trail은 공을 찾기 위한 gameplay readability 요소다. Normal은 얇고 짧게, Perfect는 더 밝고 굵지만 Character와 course silhouette를 가리지 않는다.
- landing은 최종 texture 없이 color/shape로 grass, rough, sand, water를 구분한다. rolling 중 지속 particle은 사용하지 않는다.
- Hole-In은 대형 fireworks가 아닌 cup 중심의 짧은 upward sparkle과 ring으로 Result/Character/Camera를 보조한다.
- 현재 procedural tone과 Unity particle material은 architecture/feel 검증용 placeholder다. 최종 licensed audio, flipbook/sprite, stylized shader로 같은 prefab/data seam에서 교체한다.

## M10 Sky Island Placeholder Direction

- Sky blue/grass green/white를 base로, pink/cyan/gold/mint를 제한적 accent로 사용한다.
- course 중앙 line-of-sight는 비우고 tree/flower를 edge에 배치해 character, ball, fairway, flag, landmark가 함께 읽히게 한다.
- windmill과 waterfall island는 진행 방향을 기억하게 하는 visual anchor이며 gameplay logic이나 collider를 갖지 않는다.
- 원경 floating island의 크기와 높이를 달리해 depth를 만들고, opaque cloud prefab으로 transparency overdraw를 억제한다.
- main island underside와 cliff 색으로 OOB box visual을 숨기되 실제 OOB trigger는 보존한다.
- 이 구성은 최종 environment art가 아니다. authored terrain shape, silhouette, vegetation, water, lighting, texture detail은 `TODO_ART.md`의 필수 교체 대상이다.

## M11 Polished Placeholder Direction

- 직선 course blockout을 organic island, curved fairway/fringe, raised tee, green rim, bunker depression과 water layers로 재구성한다.
- 중앙 shot corridor는 비우고 tree/flower는 좌우 cluster, windmill/waterfall/distant island는 mid/background anchor로 사용한다.
- character는 더 큰 blue/pink/dark color block으로 읽히되 final anime design을 흉내 내지 않는다.
- HUD는 dark teal/cyan의 compact corner grouping과 lower-right action을 유지하고 중앙 Ball/Aim/Distance를 가리지 않는다.
- foreground/midground/background의 value와 saturation 차이, restrained fog, selective soft shadow로 깊이를 만든다.
- M11 결과는 authored final art가 아니라 reference와 비교 가능한 polished placeholder다. 최종 asset gap은 `TODO_ART.md`에 숨김없이 유지한다.

## Character Identity Pass Direction

- SwingPop 골퍼는 Bright / Friendly / Energetic한 original stylized anime character로 정의한다.
- 약 1:6–1:7 비율, slightly oversized head, compact torso, slim limbs, readable hands/shoes와 큰 hair silhouette를 사용한다.
- dark navy hair, warm skin, cyan/blue top, pink/white accent, deep navy bottom, white accent shoes를 기본 palette로 한다.
- 골프 fashion과 fantasy casual을 결합하되 타 IP의 얼굴, 의상, emblem, club design을 복제하지 않는다.
- Address에서 full character, Ball, Club, Aim line, face/hair와 fairway가 함께 읽히는 것을 우선한다.
- 현재 `Mira` 호칭과 primitive face/hair/outfit은 pipeline 확인용 placeholder이며 final character asset이 아니다.
- 세부 silhouette, rig, animation, delivery 규격은 `CHARACTER_IDENTITY_GUIDE.md`를 source of truth로 사용한다.

## HUD Skin Pass Direction

- Bright / Casual / Fantasy / Sporty / Rounded / Lightweight를 공통 keyword로 사용한다.
- deep navy/teal translucent panel을 base로 하고 cyan/aqua와 white를 주 정보, pink를 활력 accent, gold를 Perfect/positive score, coral을 hazard/miss에 제한적으로 사용한다.
- Player/Hole/Wind/Club은 compact corner widget으로 묶고 Character, Ball, Aim, course horizon이 있는 중앙 시야는 비운다.
- Power와 Impact는 같은 arcade timing family를 사용하고 Shot Button은 lower-right hero interaction으로 읽히게 한다.
- panel radius, 얇은 rim, opacity, icon silhouette, uppercase hierarchy를 공유해 HUD가 하나의 product family처럼 보이게 한다.
- 현재 generated sprite와 `LegacyRuntime.ttf`는 project-safe placeholder다. final authored 9-slice, portrait, icon, localized font로 `HudSkinData`와 prefab seam에서 교체한다.
- 특정 게임의 UI shape, logo, icon, ornament를 복제하지 않고 target-quality는 정보 밀도, 상호작용 존재감, world visibility 기준으로만 사용한다.

## VFX Hero Pass Direction

- Bright Fantasy / Sporty Energy / Clean / Directional / Compact를 공통 언어로 사용한다.
- Normal은 cyan/white의 작은 contact flash, Great는 밝기·크기·trail을 한 단계 올리고, Perfect만 pale gold accent와 가장 강한 directional layer를 사용한다.
- impact는 core flash, ring/radial burst, launch 방향 streak, sparkle, trail transition의 시간차로 읽히며 full-screen white-out은 사용하지 않는다.
- trail은 밝은 core, 부드러운 outer fade, zero로 좁아지는 tail을 사용한다. 공 silhouette와 sky/course horizon을 덮지 않는 것을 우선한다.
- Fairway/Green은 작은 mint grass, Rough는 더 짙고 무거운 green, Bunker는 warm sand dust, Water는 cyan/white vertical splash·ring·droplet으로 구분한다.
- Putter는 driver VFX를 축소 복제하지 않고 tiny clean contact만 남기며 지속 ground trail은 끈다.
- Hole-In은 cup flash, upward sparkle, soft ring, 제한된 celebration sparkle로 Result/Camera/Character를 보조한다. 대형 fireworks는 사용하지 않는다.
- 현재 6종 generated sprite와 URP particle material은 project-safe authored-placeholder다. 최종 flipbook, trail shader, surface particle, audio asset은 같은 prefab/data seam에서 교체한다.

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

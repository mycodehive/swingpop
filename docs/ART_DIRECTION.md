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

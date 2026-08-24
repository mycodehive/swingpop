# SwingPop Character Identity Guide

Production concept reference: [`swingpop-player-character-concept-v1.png`](reference/swingpop-player-character-concept-v1.png)

이 이미지는 첫 original player character의 modeling reference v1이다. 최종 FBX 제작 시 정면/측면/후면 비율, hair mass, outfit color block, Driver/Putter silhouette를 우선 기준으로 삼고 작은 seam과 accessory는 topology 검토 후 정리한다.

## SwingPop Character Direction

SwingPop의 골퍼는 **Bright / Friendly / Energetic**한 stylized anime 캐릭터다. 현실 골프 선수의 축소 복제보다, 멀리서도 자세와 감정이 읽히는 original fantasy-casual golfer를 목표로 한다. 현재 placeholder 호칭 `Mira`는 pipeline 검증용이며 최종 이름·서사·디자인을 확정하지 않는다.

핵심 인상은 밝은 표정, 자신감 있는 Address, 과장되지만 명확한 Swing, 공을 끝까지 바라보는 활력이다. 타 IP의 얼굴, 의상, 헤어, 로고, 소품을 복제하지 않는다.

## Silhouette

- 머리, 앞머리와 뒷머리 덩어리, 손, 신발, 클럽 헤드가 1080p Address camera에서 분리되어야 한다.
- 상체는 compact하게, 팔과 다리는 slim하게 만들어 Swing arc가 읽히게 한다.
- Driver와 Putter는 길이와 head silhouette가 즉시 구분되어야 한다.
- 팔과 클럽이 몸통 안으로 겹치는 pose를 피하고, Address에서 양손과 grip 사이에 깨끗한 접점이 있어야 한다.

## Proportions

- 전체 비율 기준은 약 1:6–1:7 head-to-body다.
- 머리는 현실 비율보다 약간 크게, 몸통은 짧고 compact하게 유지한다.
- 팔과 다리는 가늘되 관절 변형이 무너지지 않을 두께를 확보한다.
- 손은 grip을 읽을 수 있게, 신발은 ground contact를 읽을 수 있게 약간 크게 허용한다.
- gameplay root scale은 바꾸지 않고 `CharacterVisualProfile.CharacterScale`, `GroundOffset`, `VisualHeight`로 visual만 정규화한다.

## Face

- 큰 눈, 간결한 코와 입, 야외의 밝은 조명에서도 읽히는 eyebrow/eye value contrast를 사용한다.
- Idle/Address에서는 friendly focus, Happy/Celebration에서는 명확한 positive emotion, Sad에서는 과도한 비극보다 아쉬운 반응을 표현한다.
- facial rig나 blendshape는 권장하지만 이번 pass의 필수 runtime dependency는 아니다.
- Camera 가까이에서 보이는 face material은 skin과 hair에 묻히지 않아야 한다.

## Hair

- 기본 방향은 dark navy 또는 charcoal이다.
- 앞머리, side lock, back mass가 각각 큰 덩어리로 읽혀야 한다.
- 얇은 card를 과도하게 겹치기보다 mobile-friendly opaque/alpha-clipped stylized chunk를 우선한다.
- Swing 중 머리 silhouette가 얼굴 전체를 가리거나 club과 혼동되지 않게 한다.

## Outfit

- golf fashion에 fantasy casual accent를 더한다.
- fitted jacket 또는 sporty top, compact bottom, 읽기 쉬운 cuff/glove, chunky golf shoes를 권장한다.
- 큰 color block을 사용하고 작은 장식은 얼굴·손·클럽 가독성을 해치지 않는 범위로 제한한다.
- 최종 logo와 emblem은 SwingPop original IP로 제작한다.

## Color Palette

| Part | Direction |
|---|---|
| Hair | dark navy / charcoal |
| Skin | warm peach |
| Top | bright cyan / blue |
| Accent | pink / white |
| Bottom | deep navy |
| Shoes | white + cyan/pink accent |
| Club | metallic dark shaft + cyan accent |

환경의 vivid grass와 cyan HUD 위에서도 skin, hair, top, bottom이 서로 다른 value로 읽혀야 한다. Perfect gold는 샷 피드백 색이므로 의상에서 넓게 사용하지 않는다.

## Driver / Putter

- Driver는 긴 shaft와 둥글고 큰 head, Putter는 짧고 수평적인 head를 사용한다.
- 현재 `CharacterPresentation.SetClub()`과 Driver/Putter visual 교체 흐름을 유지한다.
- Humanoid용 권장 hierarchy는 `HandSocket > RightHandSocket > ClubSocket > ClubVisual`이다.
- `LeftHandSocket`은 향후 two-hand constraint/IK seam일 뿐 이번 pass에서는 복잡한 IK나 장비 시스템을 추가하지 않는다.
- 최종 club pivot은 grip 중심, local forward/up은 캐릭터 prefab에서 문서화한다.

## Animation Requirements

Animator Base Layer의 state 계약은 다음 이름을 정확히 사용한다.

- `Idle`
- `Address`
- `BackSwing`
- `Swing`
- `FollowThrough`
- `WatchBall`
- `PuttAddress`
- `PuttBackSwing`
- `PuttSwing`
- `PuttFollowThrough`
- `Happy`
- `Sad`
- `BirdieCelebration`
- `EagleCelebration`
- `HoleInOneCelebration`

`CharacterAnimatorContract`가 이름과 hash를 중앙 관리한다. Gameplay는 clip 이름을 알지 않는다. Swing/PuttSwing에는 공과 club이 접촉하는 frame에 `NotifyImpactAnimationEvent()` Animation Event를 정확히 한 번 배치한다. Event가 누락되면 `CharacterTuningData`의 normalized timing과 Shot fallback guard가 soft lock을 막으며, duplicate launch는 gate가 차단한다.

## Humanoid Rig Requirements

- Unity `Animation Type = Humanoid`와 valid Human Avatar가 필수다.
- Avatar Configure에서 Head, Spine/Chest, Hips, 좌우 Upper/Lower Arm, Hand, Upper/Lower Leg, Foot 매핑을 확인한다.
- T-pose 또는 Unity가 허용하는 valid pose, 깨끗한 skin weight, 손목/팔꿈치/어깨/골반/무릎 변형이 필요하다.
- vendor bone 이름과 hierarchy path를 runtime code에 hard coding하지 않는다.
- Animator, Avatar, hand/socket/anchor reference는 `CharacterVisualAdapter` Inspector에서 명시적으로 연결한다.
- Root Motion은 끈다. 캐릭터 이동과 Address 배치는 기존 `CharacterGolfController`가 소유한다.

## Camera Requirements

- Address: full character, Ball, Club, Aim line, face/hair silhouette, playable fairway가 함께 읽혀야 한다.
- Putt: Ball과 Cup, character 일부, Green context가 함께 보여야 한다.
- Result: Character, Cup/Flag, Result panel이 서로 겹치지 않아야 한다.
- top-left HUD가 머리를 가리지 않도록 profile의 `VisualHeight`, bounds, `CameraFramingOffset`을 기록한다.
- `CharacterVisualProfile`은 camera composition metadata만 제공하며 Camera state machine을 소유하거나 변경하지 않는다.

## Asset Delivery Specification

- Format: FBX, Unity scale 1 unit = 1 meter, Y-up, forward convention을 delivery note에 명시한다.
- Rig: Unity Humanoid, valid Avatar, Root Motion off.
- Geometry: body/face/hair/outfit/hands/shoes/club 접점이 Address와 Swing에서 깨끗해야 한다.
- Materials: 최소 skin, hair, outfit, shoes/accessories를 교체 가능한 slot으로 분리한다.
- Textures: 2K Base Color baseline. 필요 시 Normal/Mask를 추가하되 URP/mobile 확장을 막지 않는다.
- Animation clips: Animation Requirements의 15개 state를 모두 제공한다.
- Pivot: 발바닥 ground 기준, model visual height와 bounds를 delivery sheet에 기록한다.
- Collision: visual hierarchy에는 Collider를 넣지 않는다.
- Portrait: optional `PortraitReference` Sprite를 별도 납품한다.

## Unity Integration Procedure

1. `Assets/_Game/Art/Characters` 아래 final character 폴더에 FBX와 texture를 넣는다.
2. FBX를 선택하고 Inspector의 `Rig` 탭에서 `Animation Type > Humanoid`, `Avatar Definition > Create From This Model`을 선택한 뒤 `Apply`를 누른다.
3. `Configure...`를 눌러 필수 bone이 모두 초록색인지 확인하고 `Done`을 누른다.
4. `HumanoidGolferTemplate.prefab`을 복제하고 `VisualRoot` 아래에 model prefab을 배치한다.
5. `CharacterVisualAdapter`의 VisualRoot, Animator, Avatar, Hand/LeftHand/RightHandSocket, ClubSocket, ImpactAnchor, HeadLookTarget, Profile을 Inspector에서 연결한다.
6. Animator Controller에 정확한 state 계약을 만들고 Swing/PuttSwing clip에 단일 Impact Animation Event를 추가한다.
7. `SwingPop > Character > Validate Character Setup`을 실행한다.
8. Hole01 Play Mode에서 `SwingPop > Character > Preview Character States`로 Address/Swing/Putt/Celebration pose를 확인한다.
9. `PlaceholderGolfer.prefab`의 gameplay controller는 유지하고 VisualRoot/Animator/profile reference만 교체한다.
10. Shot commit→Impact Event→Ball launch, Driver/Putter switching, next-shot reposition, Putt/Result camera와 celebration을 전체 회귀 검증한다.

초보자용 세부 클릭 절차는 `docs/CHARACTER_INTEGRATION_GUIDE.md`를 따른다.

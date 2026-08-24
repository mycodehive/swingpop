# Character Identity Pass Review

## Scope

ART PASS 1 이후의 Character presentation/rigger/animation integration seam만 확장했다. Ball physics, Aim, Power, Impact grade, Spin, Wind, Terrain/Lie, Club calculation, Hole/Score/Hazard, Putter physics, Cup capture, Shot Flow, Camera mode state machine, HUD binding은 새 기능을 추가하지 않았다. Camera는 Putt/Result composition data와 Result look target만 소폭 조정했다.

## Baseline

- `PlaceholderGolfer`는 segmented procedural character와 기본 Adapter/Profile seam을 갖고 있었다.
- Adapter에는 explicit Avatar, left/right hand socket, scale/ground/camera metadata가 없었다.
- Animator state hash가 controller 내부에서 state 이름으로 계산됐고 final contract/validator/template 문서가 없었다.
- Putt capture는 Ball/Cup이 보이지만 character/context crop이 컸고, Result panel 뒤에 character가 거의 숨었다.
- Perfect trail은 gold width 0.15로 flight screenshot에서 두꺼운 막대처럼 보였다.

## Implemented Character Pipeline

- `CharacterVisualProfile`: portrait hook, VisualHeight/bounds/center, CharacterScale, GroundOffset, AddressOffset, CameraFramingOffset, Club/Impact/Head offset을 presentation metadata로 제공한다.
- `CharacterVisualAdapter`: VisualRoot, Animator, explicit Avatar, legacy/right/left hand socket, ClubSocket, ImpactAnchor, HeadLookTarget와 profile을 받는다.
- profile 적용은 Awake에서 한 번 baseline을 cache하며 gameplay root scale/position을 바꾸지 않는다.
- `HumanoidGolferTemplate.prefab`은 final mesh 없이 `VisualRoot > HandSocket > RightHandSocket > ClubSocket > ClubVisual`, LeftHandSocket, ImpactAnchor, HeadLookTarget seam을 제공한다.
- `PlaceholderGolfer`는 기존 procedural hierarchy와 Driver/Putter visual을 유지하면서 left/right hand reference를 추가했다.

## Animation / Impact Contract

- `CharacterAnimatorContract`가 15개 required state 이름/hash와 Animator parameter hash를 중앙 관리한다.
- valid Humanoid Avatar와 Animator Controller가 함께 있을 때만 Humanoid Animator mode를 사용한다.
- 누락/invalid/Generic Avatar는 명확한 warning과 procedural fallback으로 처리한다.
- Swing/PuttSwing Animation Event의 `NotifyImpactAnimationEvent()`가 primary marker다.
- normalized fallback과 Shot fallback guard를 보존했고 `impactArmed + ImpactEventGate + TryLaunchCommittedShot` 경계가 duplicate launch를 막는다.
- Editor preview Swing/Putt는 Impact를 disarm해 Ball/Stroke/Shot Flow를 변경하지 않는다.

## Club / Socket Contract

- placeholder는 기존 root-relative ClubSocket을 유지해 procedural pose를 보존한다.
- Humanoid template은 HandSocket/RightHandSocket 아래 ClubSocket을 사용한다.
- LeftHandSocket은 향후 two-hand constraint/IK seam만 제공하며 이번 pass에 IK나 equipment system은 추가하지 않았다.
- Driver/Putter switching은 기존 `CharacterPresentation.SetClub()` 경로를 그대로 사용한다.

## Framing / Trail Adjustment

- Putt tuning은 camera distance/height/FOV만 소폭 넓혀 Ball, Cup, full placeholder와 Green context를 한 화면에 유지했다.
- Result에는 data-driven `ResultLookOffset`을 추가해 Camera mode/state 전이 변경 없이 Character를 panel 왼쪽, Flag/Cup context를 배경에 남겼다.
- Perfect trail은 time 0.72, width 0.095, pale gold alpha 0.92로 조정해 Ball을 가리지 않는 얇은 core로 만들었다.

## Validation

- Unity 6000.5.7f1 script compile/build: exit code 0.
- Character setup validator: PlaceholderGolfer와 HumanoidGolferTemplate PASS. 둘 다 final Avatar/Controller가 없으므로 procedural fallback으로 보고했다.
- ART PASS 1 structure: GameObjects 476, Renderers 318/active 205, shared materials 56, transparent slots 7, shadow casters 137/active 87, Colliders 10, Art Colliders 0, Particle Systems 7, Audio Sources 5, Update behaviours 12, Missing Scripts 0, Missing Materials 0.
- EditMode: 121 passed, 0 failed, 0 skipped.
- PlayMode: 4 passed, 0 failed, 0 skipped, 5.79 s.
- final capture run에서 compile error/exception/kinematic warning 없이 A/D/F/H 1920×1080 PNG를 생성했다.

PlayMode는 ART PASS 1 wiring, M10 full playable flow, M11 presentation/HUD, Foundation Normal/Perfect shot presentation graph를 포함한다. 테스트 결과는 자동 회귀 증거이며 사용자가 직접 듣고 조작하는 feel/comfort와 final model skin deformation 검증을 대신하지 않는다.

## Review Captures

- [A — Address](review-captures/character-identity-pass/A-Address.png): full placeholder, Ball, Club, Aim, fairway와 landmarks.
- [D — Perfect Flight](review-captures/character-identity-pass/D-Perfect-Flight.png): Ball이 보이는 얇은 pale-gold trail.
- [F — Putter Address](review-captures/character-identity-pass/F-Putter-Address.png): Ball, 3 m Cup/flag, full character, Green context.
- [H — Result](review-captures/character-identity-pass/H-Result.png): Character가 panel 왼쪽에 남고 Flag/Cup context와 Result panel이 보임. 강제 완료의 0-stroke score는 정상 플레이 결과를 대표하지 않는다.

## Performance

- runtime Update-declaring behaviour는 12로 ART PASS 1과 동일하다.
- runtime bone 이름 검색, `Find`, per-frame `GetComponent`, per-frame skeleton traversal을 추가하지 않았다.
- Animator state hash는 static cache, adapter baseline은 Awake cache, profile bounds는 authored data를 사용한다.
- Unity Profiler CPU/GPU/GC/Batches 측정은 이번 pass에서 실행하지 않았으므로 1080p 60 FPS를 새로 승인하지 않는다.

## Remaining Gaps

- original final character FBX, valid Humanoid Avatar, authored 15 clips, face/hair/outfit textures, portrait가 없다.
- final Driver/Putter model, hand fit, two-hand constraint/IK는 미완료다.
- final model이 들어온 뒤 skin deformation, Animation Event frame, camera head clearance, clip transition과 result celebration을 수동 검증해야 한다.
- current `Mira` name/design은 placeholder이고 narrative/selection system은 없다.

## Recommendation

**Character Integration Pipeline: GO. Final Character Art: NO-GO.**

좋은 final Humanoid character asset이 도착하면 gameplay controller 재작성 없이 template/profile/adapter/Animator/Event 연결로 투입할 수 있다. final asset이 없으므로 Humanoid integration 자체가 완료됐다고 판정하지 않는다.

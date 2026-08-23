# SwingPop Art Replacement List

Codex는 Placeholder를 만들거나 발견할 때 이 문서를 갱신한다.

## M0 Foundation Placeholder

- [ ] `FoundationGround` primitive를 실제 Hole 1 course blockout 또는 최종 course asset으로 교체
- [x] `FoundationInputProbe` instance를 M1 scene에서 제거함 (prefab asset은 M0 기록용으로 보존)

## M1 Ball Placeholder

- [ ] `GolfBall` primitive sphere mesh를 최종 golf ball mesh로 교체
- [ ] `GolfBallPlaceholder` 단색 URP material을 최종 golf ball material/texture로 교체

## Character

- [ ] CHARACTER_MODEL_01
- [ ] CHARACTER_TEXTURE_01
- [ ] CHARACTER_HUMANOID_RIG
- [ ] IDLE_ANIMATION
- [ ] ADDRESS_ANIMATION
- [ ] BACKSWING_ANIMATION
- [ ] SWING_ANIMATION
- [ ] FOLLOW_THROUGH_ANIMATION
- [ ] WATCH_BALL_ANIMATION
- [ ] PUTT_ADDRESS_ANIMATION
- [ ] PUTT_BACKSWING_ANIMATION
- [ ] PUTT_SWING_ANIMATION
- [ ] PUTT_FOLLOW_THROUGH_ANIMATION
- [ ] HAPPY_ANIMATION
- [ ] SAD_ANIMATION
- [ ] BIRDIE_CELEBRATION_ANIMATION
- [ ] EAGLE_CELEBRATION_ANIMATION
- [ ] HOLE_IN_CELEBRATION
- [ ] DRIVER_MODEL
- [ ] PUTTER_MODEL

## Course

- [ ] FAIRWAY_MATERIAL
- [ ] ROUGH_MATERIAL
- [ ] GREEN_MATERIAL
- [ ] BUNKER_MATERIAL
- [ ] WATER_MATERIAL
- [ ] STYLIZED_TREE_PACK
- [ ] FLOWERING_TREE_PACK
- [ ] WINDMILL_PROP
- [ ] FANTASY_BUILDING_SET
- [ ] FLOATING_ISLAND_BACKGROUND
- [ ] WATERFALL_VFX
- [ ] CLOUD_SET

## UI

- [ ] PLAYER_PORTRAIT_FRAME
- [ ] PLAYER_PORTRAIT
- [ ] WIND_WIDGET
- [ ] WIND_ARROW_ICON
- [ ] POWER_GAUGE_SKIN
- [ ] IMPACT_GAUGE_SKIN
- [ ] SHOT_BUTTON
- [ ] CLUB_ICONS
- [ ] SPIN_ICONS
- [ ] RESULT_POPUP
- [ ] HAZARD_POPUP
- [ ] AIM_MARKER_AND_GUIDE
- [ ] UI_TYPOGRAPHY_FONT

## VFX

- [ ] NORMAL_IMPACT
- [ ] PERFECT_IMPACT
- [ ] NORMAL_TRAIL
- [ ] PERFECT_TRAIL
- [ ] LANDING_DUST
- [ ] HOLE_IN_EFFECT

## Audio

- [ ] SWING_SFX
- [ ] IMPACT_SFX
- [ ] PERFECT_SFX
- [ ] LANDING_SFX
- [ ] ROLL_SFX
- [ ] HOLE_IN_SFX
- [ ] RESULT_STINGER
- [ ] COURSE_AMBIENCE

## M2 Debug Placeholder

- [ ] `M2AimLine` 단색 debug line을 이후 trajectory/aim presentation asset으로 교체
- [ ] IMGUI `ShotDebugOverlay`를 M8 최종 HUD 구현 시 정식 power/impact gauge skin으로 교체

## M3 Flight Debug Placeholder

- [ ] `M3TrajectoryLine` 단색 actual-trace material을 이후 정식 ball trail/VFX로 교체
- [ ] 숫자키 spin preset 안내를 M8 이후 정식 spin presentation으로 교체

## M4 Wind / Terrain Placeholder

- [ ] `M4Tee/Fairway/Rough/Bunker/Green/Water/OutOfBounds` 단색 material을 Hole 1 stylized course material로 교체
- [ ] `M4WindVector` debug line을 M8 정식 wind widget 및 M9 environment wind presentation으로 교체
- [ ] Foundation의 직사각형 surface strip/trigger layout을 Hole 1 최종 terrain mesh와 collider로 교체

## M5 Hole / Scoring Placeholder

- [ ] `M5Cup`, `M5FlagPole`, `M5Flag` primitive geometry/material을 최종 cup/flag asset으로 교체
- [ ] `TemporaryDriver`/`Putter` 텍스트 debug 표시를 M8 정식 club icon/presentation으로 교체
- [ ] IMGUI Hole/Stroke/Result 정보를 M8 정식 HUD와 result popup으로 교체

## M7 Character / Animation Placeholder

- [ ] `PlaceholderGolfer.prefab` primitive body hierarchy를 최종 licensed anime-style character model/texture/rig로 교체
- [ ] procedural pose를 Idle/Address/BackSwing/Swing/FollowThrough/WatchBall humanoid clips로 교체하고 단일 Impact Animation Event 연결
- [ ] procedural Putt pose를 PuttAddress/PuttBackSwing/PuttSwing/PuttFollowThrough clips로 교체
- [ ] placeholder celebration motion을 Happy/Sad/Birdie/Eagle/HoleInOne clips로 교체
- [ ] primitive Driver/Putter visual을 최종 club model로 교체하되 `ClubSocket` attachment seam 유지

## M8 Gameplay HUD Placeholder

- [ ] `GameplayHUD.prefab`의 기본 uGUI panel/background를 최종 rounded fantasy HUD skin과 9-slice sprite로 교체
- [ ] `P` 문자 portrait와 원형 frame을 최종 Player portrait/frame으로 교체
- [ ] 도형 기반 Wind arrow와 Club initial을 최종 Wind/Driver/Putter icon으로 교체
- [ ] ASCII 기반 Spin 방향 표시를 최종 No/Top/Back/Left/Right Spin icon으로 교체
- [ ] Power/Impact bar, cursor, Perfect zone을 최종 gauge skin과 animation으로 교체
- [ ] Primary Action, Impact/Hazard/Lie popup, Result panel을 최종 UI art로 교체
- [ ] `LegacyRuntime.ttf` placeholder를 라이선스 확인된 영문/한글 대응 typography로 교체

## M9 VFX / Audio Placeholder

- [ ] cyan/white Normal Impact particle을 최종 stylized flash/burst sprite 또는 flipbook으로 교체
- [ ] gold-accent Perfect Impact flash/streak를 최종 authored VFX로 교체
- [ ] Normal/Perfect Trail material을 공 가독성과 URP overdraw를 검증한 최종 trail shader/texture로 교체
- [ ] Fairway/Green landing grass/dust, Rough grass, Bunker sand, Water splash particle texture를 제작
- [ ] Hole-In sparkle/ring을 최종 cup success VFX로 교체
- [ ] procedural `SWING_SFX`, `PUTT_SFX`, `IMPACT_SFX`, `PERFECT_SFX`를 licensed final clip으로 교체
- [ ] Fairway/Rough/Bunker/Green/Water/OOB surface·hazard SFX를 제작/구매 후 clip slot에 연결
- [ ] `HOLE_IN_SFX`, `RESULT_STINGER`, `UI_CONFIRM` 최종 음원을 연결
- [ ] Ambient course audio는 M10 이후 환경 통합 범위에서 별도 설계
- [ ] Unity AudioMixer가 필요해질 경우 UI/Swing/Impact/Terrain/Hazard/Hole/Result group과 limiter를 추가 검토

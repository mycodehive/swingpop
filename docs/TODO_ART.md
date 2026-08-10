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
- [ ] IDLE_ANIMATION
- [ ] ADDRESS_ANIMATION
- [ ] BACKSWING_ANIMATION
- [ ] SWING_ANIMATION
- [ ] FOLLOW_THROUGH_ANIMATION
- [ ] WATCH_BALL_ANIMATION
- [ ] HAPPY_ANIMATION
- [ ] SAD_ANIMATION
- [ ] HOLE_IN_CELEBRATION

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
- [ ] WIND_WIDGET
- [ ] POWER_GAUGE_SKIN
- [ ] IMPACT_GAUGE_SKIN
- [ ] SHOT_BUTTON
- [ ] CLUB_ICONS
- [ ] RESULT_POPUP

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

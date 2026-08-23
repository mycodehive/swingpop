# SwingPop Art Replacement List

M10 `Hole01_SkyIsland`는 플레이 가능한 구조와 교체 지점을 완성한 아트 플레이스홀더다. 아래 항목을 완료하기 전에는 상용 최종 품질로 판정하지 않는다.

## Required Before Commercial Quality

### Character / Animation

- [ ] 최종 original anime-style character model, texture, humanoid rig
- [ ] Idle, Address, BackSwing, Swing, FollowThrough, WatchBall clip과 단일 Impact Animation Event
- [ ] PuttAddress, PuttBackSwing, PuttSwing, PuttFollowThrough clip
- [ ] Happy, Sad, Birdie, Eagle, Hole-In-One celebration clip
- [ ] 최종 Driver/Putter model을 기존 `ClubSocket` seam에 연결

### Course / Environment

- [ ] Tee/Fairway/Rough/Green/Bunker용 authored terrain mesh와 stylized material
- [ ] Water hazard shader와 authored shoreline
- [ ] Sky-island cliff mesh, edge treatment, waterfall VFX
- [ ] 최종 tree/flower/cloud/floating-island set
- [ ] 최종 windmill 또는 original fantasy landmark model
- [ ] Cup/flag model, flag cloth/animation, course signage
- [ ] 단순 직사각 gameplay collider를 시각 지형과 일치하는 안정적인 low-poly collider로 교체

### UI

- [ ] 최종 fantasy HUD skin, 9-slice panel, player portrait/frame
- [ ] Wind/club/spin/aim icon set
- [ ] 최종 Power/Impact gauge, primary shot button, popup/result panel
- [ ] 라이선스가 확인된 영문/한글 typography와 outdoor readability 검증

### VFX

- [ ] authored Normal/Perfect impact sprite, flipbook 또는 shader
- [ ] URP overdraw를 검증한 Normal/Perfect ball trail
- [ ] Fairway/Green/Rough/Bunker landing 및 Water splash texture/VFX
- [ ] 최종 Hole-In celebration VFX

### Audio

- [ ] licensed UI confirm, swing, putt, impact, perfect accent clip
- [ ] surface별 landing, roll, Water/OOB hazard clip
- [ ] Hole-In, result stinger, wind/water/distant course ambience
- [ ] 필요 시 AudioMixer category group과 limiter

### Quality / Technical Art

- [ ] 1920×1080, 1600×900, 1280×720 실제 Game View composition 승인
- [ ] PC 1080p target에서 Unity Profiler 60 FPS 검증
- [ ] draw call, shadow distance, transparency, material batching 최종 점검
- [ ] final screenshot quality review 및 색각/텍스트 대비 점검

## Optional Polish

- [ ] Butterflies 또는 작은 ambient creature
- [ ] extra flowers와 grass variation
- [ ] animated flag와 foliage sway
- [ ] distant birds
- [ ] secondary waterfalls와 mist accent
- [ ] cloud variation/slow parallax 추가
- [ ] distant fantasy settlement silhouette
- [ ] subtle water sparkle와 landing decal variation

## Preserved Development Placeholders

- `GolfBall` primitive sphere와 단색 material
- `PlaceholderGolfer.prefab` primitive hierarchy와 procedural motion
- `GameplayHUD.prefab` 기본 uGUI 도형/font
- `ShotFeelPresentation.prefab` ParticleSystem/TrailRenderer/procedural tone
- `Hole01_SkyIsland` primitive environment prefabs와 shared materials
- `ShotDebugOverlay`, aim/trajectory/wind debug visual은 개발 도구이며 M10에서는 기본 숨김

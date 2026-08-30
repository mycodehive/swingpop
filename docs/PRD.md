# SwingPop PRD v0.1

## 1. Product Summary

**Project Name:** SwingPop  
**Genre:** Casual Fantasy Golf  
**Primary Platform:** PC  
**Future Platform:** Android / iOS  
**Development Stage:** Vertical Slice

## M16 Authentication / Player Session Status (2026-08-29)

- Dedicated match admission requires a verified development authentication session.
- Account identity, auth session, transport connection, match player, and reconnect credential are separate layers.
- Reconnect requires both original account ownership and the rotating M15 ticket.
- This is a development foundation only. Production authentication, personal data, secure storage, lobby, matchmaking, relay, ranking, economy, and M17 work are not approved or implemented.

SwingPop은 현실적인 골프 시뮬레이터가 아니라 캐릭터성과 타격감, 판타지 코스, 명확한 UI, 역동적인 카메라 연출을 중심으로 한 아케이드 골프 게임이다.

현재 v0.1의 목적은 온라인 서비스를 완성하는 것이 아니라 **한 개 Hole에서 Tee Shot부터 Hole In까지 충분히 재미있고 보기 좋은 경험을 완성하는 것**이다.

## 2. Product Vision

한 문장:

> 밝은 판타지 세계에서 누구나 쉽게 조준하고, 타이밍을 맞춰 화려한 샷을 날릴 수 있는 캐주얼 골프 게임.

핵심 감정:

> “한 번만 더 쳐보고 싶다.”

## 3. Quality Bar

`docs/reference/target-quality.png`를 화면 품질 참고 자료로 사용한다.

참고 대상:

- 캐릭터가 플레이 화면에서 충분히 크게 보이는 구도
- 밝은 색상과 스타일라이즈드 자연환경
- 한눈에 읽히는 거리/바람/조준 정보
- 존재감 있는 Shot UI
- 공 비행 궤적의 시각적 명확성
- 판타지 랜드마크와 코스 밀도
- 샷 순간의 화면 반응

복제하지 않을 대상:

- 기존 캐릭터 디자인
- UI 그래픽
- 로고/명칭
- 아이콘
- 맵 구조
- 고유 스킬
- 사운드/음악
- 고유 연출

## 4. Target User

초기 타깃:

- 골프 규칙을 깊게 몰라도 플레이 가능한 게임을 원하는 사용자
- Anime / Stylized 캐릭터 게임을 좋아하는 사용자
- 짧은 세션에서도 성취감을 원하는 사용자
- 계산보다 직관적 조작과 타이밍을 좋아하는 사용자

## 5. Design Pillars

### 5.1 Easy to Understand

플레이어가 처음 30초 안에 다음을 이해해야 한다.

- 어디를 향해 치는가
- 얼마나 멀리 남았는가
- 바람이 어느 방향으로 부는가
- 얼마나 강하게 칠 것인가
- 언제 임팩트를 맞출 것인가

### 5.2 Satisfying Shot

샷 순간은 게임에서 가장 중요한 인터랙션이다.

Perfect Shot은 다음을 명확하게 강화한다.

- Impact
- Sound
- Camera response
- Trail
- Accuracy
- Feedback text

### 5.3 Readable Arcade Physics

물리 결과는 현실보다 단순해도 좋지만 플레이어가 이해할 수 있어야 한다.

- BackSpin은 눈에 보이게 뒤로 움직인다.
- TopSpin은 명확하게 앞으로 구른다.
- SideSpin은 궤적이 읽히게 휜다.
- 강한 바람은 궤적에 분명한 영향을 준다.

### 5.4 Bright Fantasy

코스는 현실적인 골프장 복제가 아니다.

- Sky Island
- Waterfall
- Windmill
- Floating rock
- Fantasy architecture
- Stylized trees
- Animated clouds

등을 활용한다.

### 5.5 Character Presence

최종 캐릭터가 적용되면 캐릭터는 배경 소품이 아니라 핵심 콘텐츠다.

- Address
- Swing
- Follow Through
- Watch Ball
- Celebration

이 화면에서 잘 읽혀야 한다.

## 6. v0.1 Scope

### Included

- Hole 1
- 1 Player
- 1 Placeholder or final-ready character integration point
- Driver
- Iron
- Wedge
- Putter
- Aim
- Power timing
- Impact timing
- Wind
- Height difference display
- Ball flight
- Bounce
- Roll
- Fairway
- Rough
- Bunker
- Green
- Water
- Out of Bounds
- Cup detection
- Stroke count
- Par result
- Camera direction
- HUD
- Basic VFX
- Basic audio hooks
- Debug trajectory

### Excluded

- Online multiplayer
- Login
- Store
- Currency
- Ranking
- Guild
- Season
- Battle pass
- Character gacha
- Equipment enhancement
- Complex skills
- Pet gameplay
- Multiple courses

## 7. Core Loop

```text
Hole Intro
→ Address
→ Aim
→ Club Select
→ Power Select
→ Impact Select
→ Swing
→ Ball Launch
→ Flight Camera
→ Landing
→ Bounce
→ Roll
→ Stop
→ Evaluate Lie / Result
→ Next Shot
→ Hole In
→ Hole Result
```

## 8. Shot Input

### Aim

플레이어는 좌/우 조준 방향을 변경한다.

UI:

- Hole direction
- Current aim
- Remaining distance
- Estimated distance
- Height difference
- Wind

### Power

0–100% 범위.

초기 구현은 왕복 또는 진행형 게이지 중 플레이 테스트를 통해 선택한다.

### Impact

임팩트 타이밍 구간:

- MISS
- GOOD
- GREAT
- PERFECT

Perfect 구간은 좁지만 초기 난이도는 과도하게 어렵지 않게 한다.

## 9. Shot Data

샷 결과는 가능한 한 하나의 명시적 데이터 구조로 전달한다.

예:

```text
ShotCommand / ShotData

Club
AimDirection
Power
ImpactAccuracy
Loft
TopSpin
BackSpin
SideSpin
WindDirection
WindStrength
Lie
TerrainSlope
```

향후 온라인 권위 시뮬레이션으로 옮길 수 있도록 데이터 구조를 명확하게 유지한다.

## 10. Ball Simulation

초기 Rigidbody 기반.

상태:

- Ready
- Airborne
- Bouncing
- Rolling
- Stopped
- InHole

영향 요소:

- gravity
- launch velocity
- aerodynamic drag abstraction
- wind
- spin
- bounce
- friction
- terrain response

## 11. Clubs

초기:

- Driver
- Iron
- Wedge
- Putter

`ClubData`는 데이터 기반.

필수 튜닝 값 예:

- BasePower
- Loft
- Accuracy
- Spin
- CarryModifier
- RollModifier

## 12. Terrain Surfaces

- Tee
- Fairway
- Rough
- Bunker
- Green
- Water
- OutOfBounds

Surface별:

- power modifier
- friction
- bounce response
- spin response

## 13. Wind

표시:

- direction arrow
- strength in m/s

바람은 실제 공 비행에 영향을 준다.

물리 계수는 코드 상수가 아니라 튜닝 가능한 데이터여야 한다.

## 14. Camera

필수 Camera Mode:

- HoleIntro
- Address
- Aim
- Swing
- Impact
- BallFollow
- Landing
- Result

샷 순간:

```text
Swing
→ Impact
→ short hit stop
→ camera reaction
→ trail/VFX
→ ball follow
```

## 15. HUD

### Top Left
- Player
- Character portrait placeholder
- Stroke

### Top Center
- Hole
- Par
- Score

### Top Right
- Wind arrow
- Wind speed

### Center
- Aim marker
- Distance
- Height difference

### Bottom Left
- Club
- Spin controls placeholder

### Bottom Center
- Power
- Impact

### Bottom Right
- Shot interaction

## 16. First Course

### Sky Island Golf Club — Hole 1

목표:

첫 화면부터 밝고 기억에 남는 코스.

구성 예:

- Tee는 약간 높은 곳
- Fairway는 S 또는 완만한 곡선
- 한쪽에 작은 Water Hazard
- Green 근처에 Bunker
- 중경에 Windmill
- 원경에 Floating Island와 Waterfall
- 움직이는 구름
- 꽃/나무로 색상 포인트

초기 Gameplay 테스트에서는 Primitive/Terrain으로 제작한다.

## 17. Art Direction

- Anime-inspired character
- Stylized 3D
- Bright fantasy
- Soft but readable lighting
- Saturated accents
- Clear silhouettes
- Mobile-friendly readability

피해야 할 방향:

- Photorealistic
- Muted/dark palette
- Simulation broadcast presentation
- UI가 작고 텍스트 중심인 PC 시뮬레이터 스타일

## 18. VFX

Normal Shot:

- small impact
- thin trail
- landing puff

Perfect Shot:

- stronger impact flash
- short camera shake
- stronger trail
- Perfect UI
- slightly enhanced landing effect

VFX는 Gameplay 정보를 가리지 않는다.

## 19. Audio

초기 Audio Hook:

- UI select
- Power timing
- Impact timing
- Club swing
- Ball impact
- Ground impact
- Roll
- Hole in
- Result
- Ambient wind/water

Placeholder 사용 가능.

## 20. Performance

Initial target:

- 1920x1080
- 60 FPS

방향:

- URP
- 과도한 realtime lighting 피하기
- 적절한 baked/mixed approach 고려
- VFX particle count 관리
- texture/material 관리
- LOD 확장 가능
- 불필요한 Update/GC 최소화

## 21. Vertical Slice Acceptance Criteria

다음 흐름 전체가 한 Scene 또는 명확한 Scene Flow에서 동작해야 한다.

```text
Game Start
→ Hole 1 Intro
→ Character Address
→ Aim
→ Power
→ Impact
→ Swing
→ Ball Launch
→ Ball Follow
→ Wind influence
→ Landing
→ Bounce
→ Roll
→ Stop
→ Next shot
→ Green
→ Putt
→ Hole In
→ Result
```

추가 조건:

- Console Error 0
- Missing Script 0
- Critical Missing Reference 0
- Aim / Wind / Distance가 읽힘
- Power와 Impact가 플레이 가능
- 공 비행을 카메라가 놓치지 않음
- Perfect Shot 피드백이 일반 Shot보다 명확함
- Quality Review 문서에 남은 Gap을 기록

## 22. Post-v0.1

Vertical Slice 승인 이후에만 다음 단계 논의:

- 3 Holes
- Local alternating turns
- Online 1v1
- Lobby
- Match result synchronization
- Character customization
- Additional courses

## 25. M17 Lobby / Match Creation Foundation Status (2026-08-30)

- 인증된 두 사용자의 Create/List/Join/Leave/Ready/Owner Start 흐름과 2인 capacity를 별도 Lobby control plane으로 구현했다.
- Owner Start는 bounded localhost allocator를 통해 Dedicated Match Server 한 개를 실행하고, account/match-bound one-time `MatchJoinTicket`을 각 사용자에게만 전달한다.
- Dedicated Server가 ticket과 인증 계정을 검증한 뒤 `player-a`/`player-b`를 배정하며, 기존 M12~M16 Hole01 gameplay authority와 reconnect 경계는 유지된다.
- 실제 Lobby + Dedicated Server + Client A + Client B process 흐름에서 양쪽 natural shot과 동일 snapshot hash를 확인했다.
- 이는 in-memory localhost development foundation이다. Production Lobby, matchmaking, Relay/NAT, production auth와 cloud allocator는 아직 No-Go다.

## 23. M10 Vertical Slice Status (2026-08-23)

- 기본 플레이 씬은 `Hole01_SkyIsland.unity`이며 Foundation은 회귀/개발 검증 씬으로 보존한다.
- M1–M9의 Ball, Shot, Wind/Terrain, Hole/Score, Camera, Character, HUD, VFX/Audio 전체 흐름을 한 Sky Island Hole 1에 연결했다.
- 밝은 procedural sky, floating island silhouette, clouds, waterfall, windmill, trees/flowers와 shared stylized palette를 placeholder로 구성했다.
- 자동 검증은 117 EditMode와 Foundation/Hole01 PlayMode 2개를 통과했다.
- 최종 아트 품질, 3개 해상도 Game View, 실제 음향, Profiler 60 FPS는 아직 승인되지 않았다. M11은 시작하지 않았다.

## 24. M11 Polish / Quality Gate Status (2026-08-23)

- M1–M10 gameplay contract를 유지한 채 Hole01의 Address composition, organic course layer, character presence, HUD hierarchy, material/lighting과 shadow budget을 개선했다.
- 최종 reference를 복제하지 않고 현재 primitive/shared asset seam 안에서 original polished placeholder를 만들었다.
- 자동 검증은 EditMode 117개와 PlayMode 3개를 통과했고, 1920×1080/1600×900/1280×720 Address render에서 HUD clipping을 발견하지 않았다.
- 1080p 60 FPS Profiler, full-hole 수동 visual/audio/comfort review와 final commercial art는 미승인이다.
- 판정은 다음 개발 단계 논의용 **Conditional Go**, Commercial Release **No-Go**다. 상세 근거는 `docs/M11_QUALITY_GATE_REVIEW.md`에 기록한다.

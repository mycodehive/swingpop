# SwingPop Putt / Result Cinematic Pass Review

## Goal

Green의 마지막 플레이 구간을 `Putt Address → Ball Roll → Cup Approach → Hole-In → Character Reaction → Result Reveal → Result Hold`의 하나의 읽기 쉬운 프레젠테이션 흐름으로 정리한다. Putter 물리, Cup 판정, Stroke, Score, ShotCommand 등 게임플레이 규칙은 변경하지 않는다.

## Baseline

- Putt, Hole-In VFX, 축하 동작, Result UI, 관련 Camera mode는 이미 존재했다.
- 기존 Putt 카메라는 Ball/Cup 중심이라 Character의 준비 자세와 Green 문맥이 약했다.
- Cup 접근 전용 구도와 거리 임계값이 없었고, Hole-In 직후 VFX·반응·Result가 같은 순간에 겹쳤다.
- Result 카드가 중앙을 크게 가려 Character/Cup/Flag와 경쟁했고, 점수보다 Hole/Par/Strokes가 먼저 읽혔다.
- Aim debug line이 Putt에서 Cup을 지나쳤으며 HoleComplete 뒤에도 표시될 수 있었다.

## Putt Address

- Character, Ball, Cup, Flag, Green을 한 프레임에서 읽도록 `PuttResultFramingSolver`의 Address 구도를 추가했다.
- Putter HUD, Remaining Distance, Aim marker는 기존 데이터 바인딩을 그대로 사용한다.
- Putter용 world aim line은 실제 남은 거리까지만 표시하며, HoleComplete에서는 숨긴다. 조준 계산에는 영향을 주지 않는다.

## Putt Camera

- Address는 Character 존재감을 유지하는 47° FOV의 낮은 side 구도를 사용한다.
- Rolling은 Ball/Cup 관계를 우선하는 43° FOV 구도로 바뀐다.
- 기존 `CameraDirector`에 presentation request와 Cup-approach flag만 추가했으며 gameplay camera state 전이 소유권은 유지했다.
- 카메라 충돌은 기존 sphere cast 경로를 그대로 사용한다.

## Cup Approach

- Ball이 Cup의 평면 거리 1.6m 안으로 들어오면 presentation-only `CupApproach`로 전환한다.
- 판정 반경, 포획 보정, Rigidbody 속도에는 관여하지 않는다.
- 42° FOV, 낮은 높이, Cup 우선 target으로 Ball과 Cup 사이의 긴장감을 읽게 한다.

## Hole-In

- 실제 `HoleFlowController.HoleCompleted`가 발생한 뒤에만 시퀀스를 시작한다.
- 같은 Hole 완료 신호에 대해 coordinator가 한 번만 시작되도록 guard한다.
- Cup flash는 즉시, ring/upward sparkle은 0.06초 뒤, celebration sparkle은 0.22초 뒤 시작한다.
- global `Time.timeScale` 기반 slow motion은 사용하지 않는다.

## Character Reaction

- Hole-In 0.34초 뒤 기존 `CharacterGolfController.PlayCelebrationForResult` 경로를 호출한다.
- 축하 종류는 기존 `CharacterFlowResolver`의 ScoreResult 매핑을 그대로 사용한다.
- 반응 시작 시 Result camera 구도로 먼저 이동해 UI가 나타나기 전에 Character pose를 읽을 시간을 만든다.

## Result Camera

- Character는 화면 왼쪽, Result card는 오른쪽, Cup/Flag/Green은 중경에 남도록 공통 구도를 사용한다.
- 49° FOV와 넓은 환경 여백으로 Green만 가득 차지 않게 했다.
- Result UI는 카메라 렌더링을 추가하지 않으며 기존 단일 Camera를 사용한다.

## Result UI

- `HudResultView`를 frame fade → score pop → detail fade의 3단계 reveal로 확장했다.
- ScoreResult는 기존 `GameplayHudPresenter`와 `HoleFlowController.Result`에서만 온다. 재계산하지 않는다.
- 결과 점수를 카드의 hero 정보로 올리고 Hole/Par/Strokes를 아래 detail group으로 정리했다.
- 카드 위치는 화면 오른쪽으로 이동했으며 기존 Canvas와 HUD skin 체계를 재사용한다.

## Audio

- Hole-In cue는 완료 순간, Result cue는 0.74초 뒤 Result reveal과 함께 재생한다.
- Putt swing/impact와 surface cue는 기존 `GameplayAudioController` 경로를 유지한다.
- 새 AudioSource 또는 audio architecture는 추가하지 않았다.

## Validation

- Unity 6000.5.7f1 batch Editor compile/build: PASS.
- Putt/Result structure validator: PASS. Camera 1, Canvas 1, ParticleSystem 15, AudioSource 5, Missing Script 0.
- EditMode: 139/139 PASS.
- PlayMode: 8/8 PASS.
- 자동 capture: 6/6 PASS, 모두 1920×1080.
- 자동화는 실제 Unity Editor 사용자의 전체 Play Mode 조작, Console Error 0, Profiler frame timing을 대신하지 않는다.

수동 확인 절차:

1. Unity Hub에서 SwingPop 프로젝트를 열고 Project 창에서 `Assets > _Game > Scenes > Hole01_SkyIsland`를 더블 클릭한다.
2. Game 탭 상단 해상도 드롭다운을 `1920x1080` 또는 `Full HD`로 맞춘다.
3. 상단 중앙의 Play 버튼을 누른다.
4. 공이 Green에서 멈출 때까지 플레이하거나, 기존 debug 절차로 Green/Putter 상태를 준비한다.
5. Character, Ball, Cup, Flag, Remaining Distance, Putter HUD가 동시에 보이는지 확인한다.
6. Space를 눌러 Putt하고 Ball/Cup 구도가 유지되다가 Cup 근처에서 조금 낮고 가까워지는지 확인한다.
7. Hole-In 직후 Cup flash/ring이 먼저, Character reaction이 다음, Result card가 마지막에 나타나는지 확인한다.
8. Result 카드가 실제 Stroke/Par와 일치하며 Character를 완전히 가리지 않는지 확인한다.
9. Window > General > Console을 열어 Error가 0인지 확인한다.
10. Play 버튼을 다시 눌러 종료한다.

## Regression

- Putter physics, Cup capture, Hole-In 판정, Stroke/Score, Hazard, Wind, Spin, Terrain, Lie는 변경하지 않았다.
- HoleComplete에서 `TryCommitShot`이 거절되는 기존 input lock을 PlayMode test로 확인했다.
- 반복 완료 요청은 false를 반환하며 cinematic/VFX/Character/Result count가 한 번만 증가한다.
- 기존 M10 통합 테스트는 즉시 Result 대신 의도된 delayed reveal을 기다리도록 갱신했다.
- `Foundation.unity`는 수정하지 않았다.

## Performance

- 추가 Camera 0, Canvas 0, AudioSource 0, ParticleSystem 0.
- coordinator 1개와 tuning asset 2개를 추가했다.
- Cup distance 확인은 PuttRolling 동안만 수행하며 allocation 없는 Vector3 계산이다.
- Coroutine을 만들지 않고 단일 Update timing으로 진행하며 sequence 종료 뒤 추가 작업을 하지 않는다.
- PlayMode test에서 ParticleSystem/Canvas object count가 시퀀스 전후 동일함을 확인했다.
- Unity Profiler의 CPU/GPU frame time, GC Alloc, Batches/SetPass는 수동 측정이 남아 있다.

## Before / After

- Before 기준: `docs/review-captures/vfx-hero-pass/F-Putter-Impact.png`, `H-Hole-In-Result.png`.
- After: `docs/review-captures/putt-result-cinematic-pass/F1/F2/F3/H1/H2/H3`.
- Before는 Hole-In과 Result가 한 장면에 빠르게 겹쳤다. After는 Cup, VFX, Character, Score에 각각 읽을 시간을 준다.
- After F1은 Aim line을 Cup까지 제한하고, F2/F3은 Ball/Cup 관계를 단계적으로 강조한다.
- After H3은 Character 왼쪽/Result 오른쪽의 분할 구도와 score-first hierarchy를 사용한다.
- 동영상은 안정적인 프로젝트 내 녹화 경로가 없어 생성하지 않았다.

## Quality Scorecard

| 항목 | 등급 | 근거 |
| --- | :---: | --- |
| Putt Address readability | B | Character/Ball/Cup/Flag/HUD 동시 식별 |
| Putt rolling readability | B | Ball/Cup 관계 유지, transient popup 제거 |
| Cup approach tension | B- | 전용 낮은 구도와 FOV, placeholder Character 비율 한계 |
| Hole-In clarity | B | Cup focus와 단계형 4-layer VFX |
| Character reaction | B- | 기존 procedural pose를 별도 hold에서 표시 |
| Result composition | B | Character-left/card-right/Flag 배경 유지 |
| Result hierarchy | B | score hero + staged details |
| Audio sequencing | B- | cue 간격 분리, 최종 음원/믹싱 미완료 |
| Gameplay safety | A | gameplay source read-only, 관련 회귀 test 통과 |
| Performance structure | A- | 추가 렌더 카메라/Canvas/effect 없음 |
| Commercial finish | C+ | 최종 character animation, font, VFX/audio asset 미완료 |

## Remaining Gaps

- 최종 FBX Character와 authored Putt/celebration animation.
- Hole-In score별 authored VFX variation.
- 최종 Result emblem, localized font, badge/typography polish.
- licensed Cup drop/success/result audio와 AudioMixer mastering.
- Cup Approach에서 placeholder Character가 프레임 가장자리에 크게 보이는 구간의 최종 모델 기준 재튜닝.
- 여러 Green 경사·거리·카메라 충돌 조건의 수동 검증.
- Development Build Profiler와 1280×720/1600×900 safe-area 확인.

## Recommendation

`Putt / Result Cinematic Foundation: GO`.

기존 게임플레이를 보존하면서 마지막 Putt에서 Result까지 단일 presentation rhythm, data-driven timing, 중복 방지, score-source 보존, 자동 capture/validation 경로를 확보했다.

`Commercial Presentation Approval: NO-GO`.

최종 캐릭터 애니메이션, authored UI/VFX/audio, 다중 해상도 및 Development Build Profiler 검증이 남아 있다. 다음 단계 기능은 이 작업에서 시작하지 않는다.

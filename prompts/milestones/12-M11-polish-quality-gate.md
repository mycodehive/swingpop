# M11 — Polish / Quality Gate

## Goal

기능 프로토타입을 "상용 Vertical Slice 후보" 수준으로 다듬는다.

## First

`docs/reference/target-quality.png`와 현재 Game View를 항목별로 비교한다.

복제는 하지 말고 Quality Gap을 찾는다.

## Review

- character screen presence
- camera composition
- lighting
- sky
- course density
- color saturation
- fairway/rough/green distinction
- HUD hierarchy
- aim line
- wind UI
- power/impact feedback
- trail
- impact VFX
- landing feedback
- animation timing
- camera transitions
- audio timing
- restart/replay convenience

## Performance

가능한 환경에서:

- obvious GC spikes
- excessive Update
- material/particle abuse
- missing references
- console warnings/errors

를 점검한다.

성급한 micro optimization은 하지 않는다.

## Bug Sweep

최소:

- repeated shots
- reset/restart
- water
- OOB
- bunker
- green
- putt
- perfect
- miss
- hole in

## Documentation

갱신:

- IMPLEMENTATION_PLAN
- TODO_ART
- ARCHITECTURE
- GAMEPLAY
- ART_DIRECTION

그리고 `docs/VERTICAL_SLICE_REVIEW.md`를 생성한다.

내용:

- What works
- Quality achieved
- Remaining art gaps
- Remaining gameplay gaps
- Known bugs
- Performance risks
- Recommended next phase

## Exit Criteria

Vertical Slice를 다른 사람에게 보여주고,
설명 없이 Aim→Shot→Hole In을 이해할 수 있는 상태를 목표로 한다.

온라인 구현은 시작하지 않는다.

표준 보고 형식으로 종료하라.

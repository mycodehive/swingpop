# M2 — Aim / Power / Impact

M1 기능을 깨뜨리지 말고 핵심 Shot Input Loop를 만든다.

## Goal

사용자가 방향을 조준하고 Power와 Impact를 결정하여 `ShotCommand`를 생성할 수 있게 한다.

## Implement

- explicit shot state flow
- Aim yaw control
- Power selection
- Impact timing
- Impact grade mapping
- ShotCommand / ShotData
- temporary club data if needed
- debug UI acceptable
- force perfect debug option
- state transition validation

## Input Flow

Preparing
→ Aiming
→ PowerSelecting
→ ImpactSelecting
→ Shot Confirmed

ShotCommand는 Ball launch layer에 전달한다.

## Impact Grades

- PERFECT
- GREAT
- GOOD
- MISS

수치는 tuning 가능하게 한다.

## Important

UI는 이번 단계에서 아름답지 않아도 된다.
기능 검증을 우선한다.

수십 개 bool로 flow를 만들지 않는다.

## Exit Criteria

반복해서:

Aim → Power → Impact → Launch

가 가능하고 값이 ShotCommand에 정확히 반영된다.

Power/Impact 계산은 가능한 한 Unit Test 가능하게 작성한다.

표준 보고 형식으로 종료하라.

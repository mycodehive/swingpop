# M1 — Ball Launch

AGENTS.md와 IMPLEMENTATION_PLAN을 읽고 현재 M0 완료 상태를 검증하라.

## Goal

Placeholder golf ball을 실제 입력으로 발사하고, 카메라가 공을 추적하며, 공이 지면에서 bounce/roll 후 안정적으로 정지한다.

## Implement

- GolfBall prefab
- Rigidbody/Collider
- BallController 또는 책임이 명확한 동등 구조
- Temporary launch command/input
- tunable launch speed
- ground collision
- bounce
- rolling friction
- stopped detection
- reset ball debug action
- simple follow camera 또는 현재 카메라 시스템에 맞는 최소 추적
- basic debug telemetry

## Important

이번 단계에서는 아직 Power Gauge / Impact Timing을 만들지 않는다.
입력은 debug key/button으로 고정된 shot을 발사해도 된다.

Ball physics 값은 Inspector/Data에서 조정 가능하게 한다.

## Exit Criteria

Play Mode에서:

1. 공이 정지 상태로 시작
2. 입력
3. 공 발사
4. 포물선 비행
5. 지면 충돌
6. Bounce
7. Roll
8. Stop
9. Reset 후 반복 가능

Console Error가 없어야 한다.

가능한 계산 로직에는 최소 테스트를 추가한다.

표준 보고 형식으로 종료하라.

# M5 — Hole / Scoring

## Goal

Hole 1의 게임 규칙과 완료 조건을 만든다.

## Implement

- Hole definition
- Tee position
- Cup/flag target
- cup collider/trigger strategy
- stroke count
- par value
- in-hole detection
- next shot setup
- hole complete state
- relative score calculation
- basic result presentation hook
- putter support / green behavior

## In-hole

공이 컵 근처를 지나가기만 해도 무조건 들어가는 지나친 판정은 피한다.
그러나 초기 Casual game답게 지나치게 어려운 컵 물리도 피한다.

## Exit Criteria

Tee에서 시작해 여러 Shot 후 Putt으로 Hole In하고,
Stroke와 Par 대비 결과를 계산할 수 있다.

표준 보고 형식으로 종료하라.

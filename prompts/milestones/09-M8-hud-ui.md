# M8 — Gameplay HUD

## Goal

참고 이미지 수준의 정보 구조를 지향하는 밝고 읽기 쉬운 HUD를 만든다.

최종 아트가 없으면 도형/텍스트 기반 polished placeholder를 사용한다.

## Layout

Top Left:
- Player
- Stroke

Top Center:
- Hole
- Par
- Score

Top Right:
- Wind direction
- Wind m/s

Center:
- Aim
- Remaining distance
- Height difference

Bottom Left:
- Club
- spin status/control

Bottom Center:
- Power
- Impact

Bottom Right:
- Primary Shot action

## Motion

- power feedback
- impact perfect pulse
- wind arrow readable motion
- result popup tween

과도한 perpetual animation은 피한다.

## Technical

HUD가 physics 값을 복제 계산하지 않는다.
Gameplay source of truth를 표시한다.

## Exit Criteria

플레이 중 필요한 핵심 정보가 한눈에 읽히고,
Aim→Power→Impact 흐름을 UI만 보고 수행 가능하다.

표준 보고 형식으로 종료하라.

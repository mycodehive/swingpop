# M6 — Camera Director

## Goal

공을 치는 재미를 강화하는 상태 기반 Camera System을 만든다.

현재 프로젝트의 Cinemachine 버전/사용 여부를 먼저 확인하고 실제 API에 맞게 구현한다.

## Camera Modes

- HoleIntro
- Address
- Aim
- Swing
- Impact
- BallFollow
- Landing
- Result

## Presentation

Impact:
- very short reaction
- optional subtle shake
- transition into ball follow

Ball:
- target not lost
- trail visible
- target/course context readable

Landing:
- landing point를 보기 좋은 framing

## Important

Gameplay logic가 개별 camera component 세부값을 직접 조절하지 않는다.
CameraDirector 또는 동등한 책임 지점을 둔다.

멀미를 유발하는 과도한 shake/rapid rotation을 피한다.

## Exit Criteria

한 Shot 동안 카메라 전환이 자연스럽고,
공 위치를 잃지 않으며,
Address와 Ball flight 모두 읽기 좋다.

표준 보고 형식으로 종료하라.

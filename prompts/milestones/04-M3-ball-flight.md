# M3 — Arcade Ball Flight

## Goal

Golf Ball의 flight/bounce/rolling을 SwingPop다운 읽기 쉽고 튜닝 가능한 Arcade Physics로 발전시킨다.

## Implement

- launch calculation separated from presentation
- club loft hook
- drag tuning
- top/back/side spin data hooks
- visually readable side curve
- backspin landing response
- topspin forward roll
- bounce tuning
- rolling tuning
- stable stop detection
- trajectory debug
- predicted landing debug if practical

## Policy

현실 시뮬레이션 논문 수준의 모델을 구현하는 것이 목적이 아니다.

Predictable + Fun + Tuneable.

## Debug

- trajectory
- current speed
- spin
- launch vector

## Exit Criteria

동일 입력은 유사한 결과를 만들고,
Top/Back/Side spin의 차이를 테스트 장면에서 육안으로 확인 가능해야 한다.

표준 보고 형식으로 종료하라.

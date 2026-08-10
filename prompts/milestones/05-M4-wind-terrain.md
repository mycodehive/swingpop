# M4 — Wind / Terrain

## Goal

바람과 Lie/Surface가 실제 Shot 결과에 영향을 주게 한다.

## Wind

- single source of truth
- direction
- strength
- tunable influence
- debug vector
- HUD 연동 가능한 read-only state

## Terrain

최소:

- Tee
- Fairway
- Rough
- Bunker
- Green
- Water
- OutOfBounds

Surface data:

- power modifier
- friction
- bounce
- spin response

TerrainType if/switch가 코드 전체에 퍼지지 않게 한다.

## Water/OOB

무한 낙하/soft lock이 발생하지 않는 최소 recovery 구현.

## Exit Criteria

같은 샷이라도:

- wind on/off
- fairway/rough/bunker
- green rolling

에서 체감 가능한 차이가 발생한다.

표준 보고 형식으로 종료하라.

# SwingPop Gameplay Specification v0.1

## 1. Primary Interaction

핵심 입력:

1. Aim
2. Select club
3. Select power
4. Select impact
5. Observe result
6. Adjust next shot

## 2. Shot State

권장 흐름:

```text
Preparing
→ Aiming
→ PowerSelecting
→ ImpactSelecting
→ Swinging
→ BallFlying
→ BallBouncing
→ BallRolling
→ BallStopped
→ Preparing
```

Hole In:

```text
BallRolling / BallStopped
→ HoleComplete
```

## 3. Power

Normalized 0–1 또는 percentage 0–100.

Power가 실제 velocity에 바로 단순 곱만 되는 구조보다 클럽별 tuning curve를 허용하는 구조가 좋다.

초기에는 단순 구현 후 필요 시 AnimationCurve 적용.

## 4. Impact

입력 위치에 따른 grade:

- Perfect
- Great
- Good
- Miss

Impact는 다음에 영향을 줄 수 있다.

- horizontal dispersion
- power loss
- spin consistency
- VFX level

초기 버전에서는 Perfect가 명확히 유리하되 Miss가 지나치게 처벌적이지 않게 한다.

## 5. Aim

Aim yaw를 중심으로 구현한다.

Pitch/loft는 클럽 데이터에서 시작한다.

Debug trajectory는 실제 current tuning을 사용해야 한다.

## 6. Spin

v0.1에서 UI가 복잡해지면 spin은 debug/키 입력으로 먼저 구현 가능하다.

- TopSpin
- BackSpin
- SideSpin

범위는 normalized value로 유지하는 것을 권장한다.

## 7. Wind

Wind는 World-space direction + scalar strength.

UI arrow와 Ball calculation이 같은 source of truth를 사용한다.

## 8. Lie

Ball이 멈춘 surface에 따라 다음 Shot의 modifier 결정.

- Fairway: neutral
- Rough: slight power/accuracy penalty
- Bunker: stronger penalty
- Green: putting
- Water: penalty handling
- OOB: penalty handling

구체 밸런스 수치는 플레이 테스트로 결정.

## 9. Putt

Putter 사용 시 비행을 최소화하고 rolling 중심으로 동작한다.

Green slope 지원은 v0.1 후반 또는 Polish에서 단계적으로 적용 가능.

## 10. Scoring

기본 Stroke Play.

- Stroke count
- Par
- Relative score display

Birdie/Eagle 등 텍스트는 Hole 완료 시 계산한다.

## 11. Perfect Shot Feedback

조건:
Impact grade = Perfect

반응:

- stronger hit sound
- short hit stop
- stronger ball trail
- small flash
- UI “PERFECT”
- reduced dispersion

## 12. Failure / Recovery

Water/OOB는 공이 무한히 떨어지거나 게임이 멈추지 않도록 명시적 recovery flow를 둔다.

개발 초기에는 단순 drop/reset도 허용한다.

## 13. Feel Tuning

우선순위:

1. Impact timing feel
2. Ball launch speed
3. Arc readability
4. Landing response
5. Roll duration
6. Camera
7. VFX

실제 물리 정확성보다 체감 튜닝을 우선한다.

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

### M1 Ball Launch State

M1에서는 이후 shot flow를 구현하지 않고 ball simulation 상태만 다음처럼 명시적으로 관리한다.

```text
Ready
→ Airborne
→ Bouncing
→ Rolling
→ Stopped
→ Reset
→ Ready
```

- Space / gamepad south button: 고정된 launch velocity로 발사
- R / gamepad north button: 초기 위치와 `Ready` 상태로 reset
- launch speed와 angle은 `M1BallTuning.asset`에서 조정한다.
- bounce/friction은 `GolfBallPhysics.asset`, Rigidbody/roll/stop 값은 `M1BallTuning.asset`에서 조정한다.
- Aim, Power, Impact, Wind, Spin은 M1에 포함하지 않는다.

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

## 14. M2 Playable Shot Flow

```text
Aiming
  → Space: PowerSelecting
  → Space: ImpactSelecting
  → Space: ShotCommitted
  → Ball Airborne → Bouncing → Rolling → Stopped
  → R: Aiming + Ball Ready
```

- A/D, 좌우 방향키: yaw aim. 기본 범위는 -30도에서 +30도다.
- Space: 현재 단계 확정.
- Escape: Power 또는 Impact 선택을 취소하고 Aiming으로 돌아간다.
- P: ImpactSelecting에서 검증용 PERFECT를 즉시 확정한다.
- R: 언제든 공을 시작 위치로 reset하고 Aiming으로 돌아간다.
- power는 0~1 왕복 gauge이며 실제 launch speed scale로 전달된다.
- impact cursor의 중앙 오차 절댓값으로 Perfect/Great/Good/Miss를 결정한다.
- 낮은 grade는 power multiplier와 deterministic horizontal dispersion을 적용한다.
- 현재 M2에는 wind, spin, surface modifier, club/putter, hole 판정이 없다.

기본 데이터는 `M2ShotTuning.asset`에 있으며 Debug overlay는 최종 HUD가 아니라 상태 확인 도구다.

## 15. M3 Arcade Spin / Flight

Spin preset:

- `1`: No Spin
- `2`: Top Spin — landing boost와 낮은 rolling 감속으로 rollout 증가
- `3`: Back Spin — landing brake, 높은 rolling 감속, 짧은 rollback
- `4`: Left Side Spin — 현재 이동 방향 기준 왼쪽으로 공중 curve
- `5`: Right Side Spin — 현재 이동 방향 기준 오른쪽으로 공중 curve

Aim은 초기 launch direction이고 SideSpin은 비행 중 방향을 계속 휘게 하므로 서로 다른 값이다.

공중에서는 gravity, launch velocity, tunable drag, 작은 vertical-spin lift/downforce, velocity-relative side curve가 적용된다. Spin은 공중과 지면에서 서로 다른 속도로 감소한다. 첫 bounce 이후 TopSpin은 전진을 보강하고 BackSpin은 rollout을 줄인다. Stop 시 spin과 velocity를 zero로 만들어 미세 움직임이 지속되지 않게 한다.

최근 실제 trajectory는 Game View의 line으로 남으며 Reset 또는 다음 launch 때 초기화된다. M3에는 predicted trajectory, Wind force, terrain modifier가 없다.

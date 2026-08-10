# M9 — VFX / Audio Feel Pass

## Goal

Normal Shot과 Perfect Shot의 체감 차이를 명확하게 만든다.

## VFX

Normal:
- impact
- thin ball trail
- landing puff

Perfect:
- stronger impact
- flash
- stronger trail
- Perfect UI pulse
- optional small camera shake
- optional very short hit stop

## Audio Hooks

- UI timing
- swing
- impact
- perfect
- landing
- rolling
- hole in
- result

라이선스가 불명확한 외부 음원을 임의로 추가하지 않는다.
필요하면 placeholder generation/empty hooks로 남기되 실제 코드 경로는 동작하게 한다.

## Object Lifetime

Particle/VFX가 계속 누적되지 않게 한다.
필요하면 pooling 또는 명확한 destruction lifecycle 사용.

## Exit Criteria

눈을 감고 듣거나,
소리를 끄고 보더라도,
Normal과 Perfect의 차이를 구분할 수 있는 수준의 피드백을 목표로 한다.

표준 보고 형식으로 종료하라.

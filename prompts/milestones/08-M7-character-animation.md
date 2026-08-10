# M7 — Character / Animation

## Goal

Placeholder라도 실제 Golfer가 Shot flow에 참여하게 한다.

## Implement

- Character prefab integration point
- CharacterGolfController or adapter
- Animator wrapper
- Address
- BackSwing
- Swing
- FollowThrough
- WatchBall
- Happy/Sad hooks
- Hole complete celebration hook

Asset이 없다면 primitive/temporary humanoid로 flow를 구성하고 `docs/TODO_ART.md`를 갱신한다.

## Important

Gameplay 코드에서 Animator State 문자열을 여기저기 직접 호출하지 않는다.
Animation adapter/controller를 사용한다.

Ball launch timing이 Swing impact event와 연결될 수 있는 구조로 만든다.
Temporary animation에서는 fallback timing 허용.

## Exit Criteria

Aim 준비 시 Address,
Shot 확정 시 Swing,
Impact 시 공 발사,
이후 WatchBall 상태가 자연스럽게 연결된다.

표준 보고 형식으로 종료하라.

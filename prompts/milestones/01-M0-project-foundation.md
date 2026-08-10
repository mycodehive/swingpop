# M0 — Project Foundation

AGENTS.md와 docs/IMPLEMENTATION_PLAN.md를 다시 읽어라.

이번 작업은 M0만 수행한다.

## Goal

Unity 프로젝트가 안정적으로 실행되고 이후 Gameplay 작업을 추가할 기반을 만든다.

## Required

- Unity version/package 상태 확인
- URP 상태 확인
- Input System 상태 확인
- `Assets/_Game` 구조를 현재 프로젝트에 맞게 생성
- 첫 gameplay scene 생성 또는 기존 scene 선정
- 기본 ground / lighting / camera를 이용해 Play 가능한 Scene 확보
- 필요한 경우 Layer/Tag 최소 정의
- Unity용 `.gitignore` 확인
- docs 갱신

## Do Not

- Golf physics 구현
- Full HUD 구현
- Character asset 탐색
- Online 구현
- 상점/랭킹 구현

## Exit Criteria

- project opens without known compile errors
- gameplay test scene exists
- folder structure usable
- required packages are known/documented
- next M1 작업 위치가 명확함

실행 불가능한 검증은 구체적인 Unity Editor 수동 검증 절차를 작성하라.

작업 후 표준 보고 형식으로 종료하라.

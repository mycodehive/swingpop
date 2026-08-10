# Codex Bootstrap Prompt — SwingPop

먼저 프로젝트 루트의 `AGENTS.md`를 읽고 반드시 준수하라.

그 다음 아래 문서를 읽어라.

- docs/PRD.md
- docs/ARCHITECTURE.md
- docs/GAMEPLAY.md
- docs/ART_DIRECTION.md
- docs/ROADMAP.md
- docs/IMPLEMENTATION_PLAN.md
- docs/TODO_ART.md

참고 이미지가 존재하면 확인한다.

- docs/reference/target-quality.png

## 이번 작업의 목적

코드를 대량 생성하는 것이 아니다.

현재 Workspace를 정확히 파악하고 SwingPop 개발을 시작할 수 있는 상태로 정리한다.

## STEP 1 — Repository Audit

다음을 실제 파일 기준으로 확인한다.

- Repository root
- Unity project 여부
- Unity version
- Render pipeline
- Packages
- Input System
- Cinemachine 존재 여부
- Scenes
- Scripts
- Prefabs
- Existing assets
- Tests
- Git state / gitignore
- obvious compile/configuration risk

Unity 프로젝트가 아직 생성되지 않았다면 임의로 가짜 `Assets/`만 만드는 방식으로 완료 처리하지 말고, 현재 환경에서 실제 Unity 프로젝트 생성이 가능한지 판단해서 필요한 절차를 보고하라.

## STEP 2 — Documentation Sync

`docs/IMPLEMENTATION_PLAN.md`의 Current State를 실제 Repository 상태로 갱신한다.

기존 문서와 실제 프로젝트 상태가 다르면 실제 프로젝트를 기준으로 문서를 수정한다.

## STEP 3 — M0 Plan

M0 Project Foundation을 실제 작업 단위로 나눈다.

예:

- folder structure
- base scene
- input
- game layer/tag conventions if needed
- project settings
- URP verification
- initial test object
- gitignore verification

단, 현재 프로젝트에 이미 존재하는 것을 불필요하게 재구현하지 않는다.

## STEP 4 — Execute M0 Only

M0만 구현한다.

M1 이후 기능은 구현하지 않는다.

## Quality / Safety

- Package/API 버전을 추측하지 않는다.
- 실행하지 않은 Unity PlayMode 검증을 했다고 말하지 않는다.
- 변경 이유가 없는 대규모 폴더 이동을 하지 않는다.
- 외부 Asset을 자동 다운로드하지 않는다.
- 라이선스 불명 리소스를 추가하지 않는다.

## 완료 보고

다음 형식:

## Completed
## Files Changed
## Validation
## Current Result
## Known Issues
## Next

Next는 M1이어야 한다.

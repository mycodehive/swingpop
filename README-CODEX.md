# SwingPop Codex Starter

SwingPop은 밝고 화려한 Anime / Stylized 3D 스타일의 캐주얼 판타지 골프 게임 프로젝트다.

이 패키지는 Codex가 한 번에 거대한 게임을 만들지 않고, 작은 Vertical Slice를 순차적으로 완성하도록 제어하기 위한 개발 문서와 실행 프롬프트를 제공한다.

## 1. Unity 프로젝트에서의 최종 배치

아직 Unity 프로젝트가 없다면 다음 구조로 새 저장소를 만든다.

```text
SwingPop/
├─ AGENTS.md
├─ README-CODEX.md
├─ Assets/
├─ Packages/
├─ ProjectSettings/
├─ docs/
│  ├─ PRD.md
│  ├─ ARCHITECTURE.md
│  ├─ GAMEPLAY.md
│  ├─ ART_DIRECTION.md
│  ├─ ROADMAP.md
│  ├─ IMPLEMENTATION_PLAN.md
│  ├─ TODO_ART.md
│  └─ reference/
│     └─ target-quality.png
└─ prompts/
   ├─ 00-bootstrap.md
   └─ milestones/
      ├─ 01-M0-project-foundation.md
      ├─ 02-M1-ball-launch.md
      ├─ 03-M2-aim-power-impact.md
      ├─ 04-M3-ball-flight.md
      ├─ 05-M4-wind-terrain.md
      ├─ 06-M5-hole-scoring.md
      ├─ 07-M6-camera-director.md
      ├─ 08-M7-character-animation.md
      ├─ 09-M8-hud-ui.md
      ├─ 10-M9-vfx-audio.md
      ├─ 11-M10-hole1-vertical-slice.md
      └─ 12-M11-polish-quality-gate.md
```

## 2. 시작 방법

1. Unity Hub에서 Unity 6 계열의 안정 버전으로 새 3D URP 프로젝트를 만든다.
2. 프로젝트 이름을 `SwingPop`으로 한다.
3. 이 패키지 안의 `AGENTS.md`, `docs/`, `prompts/`, `README-CODEX.md`를 Unity 프로젝트 루트에 복사한다.
4. `docs/reference/target-quality.png`가 존재하는지 확인한다.
5. Codex에서 Unity 프로젝트 루트를 작업폴더로 연다.
6. 첫 프롬프트로 `prompts/00-bootstrap.md` 내용을 사용한다.
7. 그 다음 `prompts/milestones/01-...`부터 순서대로 한 단계씩 실행한다.

## 3. 중요한 원칙

Codex에 milestone 여러 개를 한 번에 실행시키지 않는다.

각 단계마다:

PLAN → IMPLEMENT → COMPILE CHECK → TEST → PLAY MODE VALIDATION → DOCUMENT

순서로 완료한 후 다음 단계로 넘어간다.

M11까지의 목표는 온라인 게임 완성이 아니다.

목표는 다음과 같다.

> 참고 이미지 수준의 화면 밀도와 밝은 스타일을 지향하면서, 1개 홀에서 공을 치는 순간부터 Hole In까지 실제로 플레이 가능한 상용 Vertical Slice를 만든다.

온라인, 상점, 랭킹, 시즌, 길드, 배틀패스는 이 패키지의 범위 밖이다.

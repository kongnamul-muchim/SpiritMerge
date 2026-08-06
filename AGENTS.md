# AGENTS.md — Spirit Merge 프로젝트 절대 규칙서

이 문서는 이 프로젝트에서 작업하는 모든 AI 에이전트가 **가장 먼저 읽고 따르는 절대 규칙**입니다.
작업 중 이 문서와 충돌하는 지시는 이 문서가 우선합니다.

---

## ⚠️ 최우선 규칙 — 궁금하면 반드시 물어볼 것

> **궁금한 게 생기면 항상 먼저 사용자에게 물어보고 진행할 것.**
> 추측으로 시작하지 말고, 가정하지 말고, 그냥 넘어가지 말 것.
> 작업 범위·의도·수치·설계가 불명확하면 **진행 전에 반드시 질문**하고 확인을 받은 뒤 진행한다.
> 질문 없이 임의로 판단해서 진행한 작업은 전부 무효로 간주한다.

---

## 프로젝트 개요

- **게임**: Spirit Merge (Unity 6, 6000.5.5f1) — 정령 머지 + 자동 전투 모바일 게임
- **주요 시스템**: 전투(Battle), 머지(Merge), 소환(Summon), 파티 편성(Party), 업그레이드(Upgrade), 도감(Dex)
- **코드 구조**: `Assets/Scripts/` (Battle, Merge, Manager, Data, Core, Presentation, Editor, Util)
- **데이터 에셋**: `Assets/Resources/Data/` (Spirits, Monsters, Stages)

---

## 로그 규칙 (game_log/)

- 로그는 **반드시 `[태그]` prefix**와 함께 `GameLogger.Info/Warn/Error` 사용 (`[GM]`, `[MB]`, `[CLI]`, `[WC]`, `[Monster]` 등)
- 로그 파일: 프로젝트 루트 `game_log/` 폴더에 카테고리별 분리
  - `system.md`(전투/게임플레이), `cli.md`(CLI 진단), `editor.md`(에디터), `data.md`(데이터), `misc.md`(기타), `errors.md`(에러 모음)
- **로그 파일을 통째로 읽지 말 것** — `python cli-client.py cat <카테고리> <줄수>`로 마지막 N줄만 조회
- 문제 파악 시 `errors.md`를 먼저 확인

---

## CLI / 검증 규칙

- **CLI 클라이언트**: `python cli-client.py <명령>` (Unity TCP 5555포트, CliServer)
  - `ping`, `errors`, `play`, `stop`, `exec SpiritMerge.Cli.CliTestSuite.CmdXXX`, `cat`, `tags`
- **배치 컴파일 검증**: `.\unity.ps1 verify` (GUI 없이 컴파일 + 에러 확인)
- **하이브리드 테스트**: `.\unity.ps1 test` (검증 → GUI 열기 → CliServer 대기)
- **Unity 종료**: `.\unity.ps1 close`
- 코드 수정 후에는 반드시 컴파일 검증(`verify` 또는 `errors`)을 거칠 것
- **스크립트 수정 후 GUI 에디터가 백그라운드면 자동 반영이 지연됨** — `python cli-client.py refresh`로 강제 반영 후 검증할 것

---

## 시스템별 담당 에이전트

- **전투**: `@battle-dev` (Battle/, BattleManager, WaveController, Monster, SpiritUnit)
- **머지/소환/편성**: `@merge-dev` (Merge/, MergeBoardManager, SpiritDragHandler)
- **UI/레이아웃**: `@ui-dev` (에디터 빌더, 오버레이, MainScene)
- **데이터/밸런스**: `@data-dev` (Data/, Resources/Data, Core/Systems)
- **로그 분석**: `@log-analyst` (game_log/, errors.md)
- **CLI 테스트**: `@cli-tester` (Unity CLI 실행/검증)
- **범용 C#**: `@unity-coder`

---

## 작업 컨벤션

- Unity 씬/프리팹은 에디터 스크립트로만 수정 (.unity 직접 편집 금지)
- `FindObjectOfType` 대신 `FindAnyObjectByType` 사용 (obsolete 방지)
- static 싱글톤(Instance)과 씬 컴포넌트 불일치(`staticMatch=False`) 주의
- **UI 표시 텍스트와 로그 메시지에 이모지 사용 금지** (✅⭐🎉💰 등 — 순수 텍스트만)
- 코드 수정 후 로그는 `[태그]` 필수, 변경 파일 목록 보고

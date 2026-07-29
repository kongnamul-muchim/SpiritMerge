# Spirit Merge — 프로젝트 구조

> **최종 업데이트:** 2026-07-29

---

## 📁 폴더 구조

```
Assets/
├── Editor/
│   └── SpiritMergeSetup.asset          ← 에디터 설정
│
├── Resources/
│   ├── Data/
│   │   ├── Spirits/                    ← SpiritData 30종 (.asset)
│   │   ├── Stages/                     ← StageData 50종 (.asset)
│   │   └── Monsters/                   ← MonsterData (런타임 생성)
│   └── Fonts & Materials/
│       └── *.asset                     ← 폰트/머티리얼
│
├── Scenes/
│   └── MainScene.unity                 ← 메인 게임 씬 (유일)
│
├── Scripts/
│   ├── Battle/
│   │   ├── Monster.cs                  ← 몬스터 전투 유닛
│   │   ├── MonsterSpawner.cs           ← SpawnPoint + 몬스터 생성
│   │   ├── SpiritUnit.cs               ← 정령 전투 유닛
│   │   ├── WaveController.cs           ← 웨이브 진행
│   │   └── WaveAnimator.cs             ← 웨이브 표시 애니메이션
│   │
│   ├── Data/
│   │   ├── Enums.cs                    ← ElementType, AnimalType
│   │   ├── MonsterData.cs              ← 몬스터 데이터 SO
│   │   ├── SpiritData.cs               ← 정령 데이터 SO
│   │   └── StageData.cs                ← 스테이지 데이터 SO
│   │
│   ├── Editor/                         ← 에디터 전용 스크립트
│   │   ├── AddEventSystem.cs           ← EventSystem 생성
│   │   ├── AddGameManagerToScene.cs    ← GameManager 추가
│   │   ├── ApplySceneComponents.cs     ← 모든 컴포넌트 추가
│   │   ├── BattleUIBuilder.cs          ← BattleArea UI 리빌드
│   │   ├── ConnectAllSprites.cs        ← 스프라이트 → SO 연결
│   │   ├── GameSetupWizard.cs          ← Full Game Setup
│   │   ├── GNBBuilder.cs               ← GNB 리빌드
│   │   ├── MergeUIRebuilder.cs         ← MergeArea 리빌드
│   │   ├── RebuildAll.cs               ← UI 5in1 리빌드
│   │   ├── SceneCleanup.cs             ← 씬 정리/중복제거
│   │   ├── SpiritSpriteConnector.cs    ← 정력 스프라이트 연결
│   │   ├── SpriteOrganizer.cs          ← 스프라이트 폴더 정리
│   │   ├── StageDataGenerator.cs       ← 스테이지 50개 생성
│   │   └── TopBarBuilder.cs            ← TopBar 리빌드
│   │
│   ├── Manager/
│   │   ├── BattleManager.cs            ← 전투 시스템 관리
│   │   └── GameManager.cs              ← 게임 메인 매니저
│   │
│   ├── Merge/
│   │   └── MergeBoardManager.cs        ← 머지보드 (소환/이동/합성)
│   │
│   ├── Presentation/
│   │   ├── Common/
│   │   │   └── GameEntryPoint.cs       ← VContainer 진입점
│   │   └── UI/
│   │       └── HUD/
│   │           └── MergeUI.cs          ← 머지 UI (구버전/미사용)
│   │
│   └── Util/
│       ├── GameLogger.cs               ← 로깅 시스템
│       └── WaveCalculator.cs           ← 웨이브 분배 계산
│
├── Sprites/
│   ├── Icons/                          ← 속성 아이콘 4종
│   ├── Enemies/
│   │   ├── Chapter1_Fire/              ← 불의 숲 적
│   │   ├── Chapter2_Water/             ← 물의 숲 적
│   │   ├── Chapter3_Nature/            ← 숲의 길 적
│   │   └── Chapter4_Thunder/           ← 번개의 숲 적
│   └── Spirits/
│       ├── Fire/                       ← 불 정령 (5종)
│       ├── Water/                      ← 물 정령 (5종)
│       ├── Nature/                     ← 자연 정령 (5종)
│       ├── Thunder/                    ← 번개 정령 (5종)
│       ├── Light/                      ← 빛 정령 (5종)
│       └── Dark/                       ← 어둠 정령 (5종)
│
└── docs/
    ├── AI_PROMPTS.md                   ← ComfyUI 프롬프트 가이드
    ├── GAME_DESIGN.md                  ← 게임 디자인 (v2)
    └── project/
        ├── 01_GAME_OVERVIEW.md         ← 게임 개요
        ├── 02_FULL_DESIGN.md           ← 전체 기획서
        ├── 03_ROADMAP.md               ← 로드맵
        ├── 04_ARCHITECTURE.md          ← 이 파일
        ├── 05_SYSTEMS.md               ← 시스템 상세
        └── 06_TODO.md                  ← 할 일 목록
```

---

## 🧩 의존성 그래프

```
GameEntryPoint (VContainer Startable)
  └── 초기화 완료 (로그만)

GameManager (MainCanvas에 부착)
  ├── BattleManager (BattleArea)
  │   ├── WaveController (웨이브)
  │   └── MonsterSpawner (몬스터)
  │       └── Monster (프리팹)
  ├── MergeBoardManager (MergeArea)
  │   └── SpiritItemData (각 슬롯의 정령)
  └── SpiritUnit (BattleArea 정령)

GameLogger (정적 클래스)
  └── game_log.md (파일 출력)
```

---

## 🎮 Unity Editor 메뉴 구조

```
SpiritMerge
├── UI
│   ├── Rebuild All              ← 5in1 (Cleanup+TopBar+Battle+Merge+GNB)
│   ├── Rebuild Merge UI
│   ├── Rebuild Battle UI
│   ├── Rebuild GNB
│   └── Rebuild TopBar
├── Data
│   └── Create All Stages        ← 50개 StageData 생성
└── Setup
    ├── Full Game Setup          ← 프리팹 + 버튼 생성
    ├── Organize All Sprites     ← 폴더 정리
    ├── Fix Sprite Import Settings
    ├── Cleanup Scene            ← 중복 제거 + 레이아웃
    ├── Connect All Sprites      ← PNG → SO 연결
    ├── Apply All Components     ← 컴포넌트 부착
    └── Add EventSystem          ← UI 클릭 활성화
```

---

## 🔑 핵심 스크립트 설명

### GameManager.cs
**역할:** 게임 전체 초기화 및 조율
**위치:** MainCanvas (AddComponent)
**주요 기능:**
- 싱글톤 Instance
- Gold/Ruby 관리 (SpendGold/SpendRuby)
- SetupBattleSystem / SetupMergeSystem / SetupGNBTabs / SetupTopBar
- GameLogger로 모든 액션 로깅

### MergeBoardManager.cs
**역할:** 머지보드 제어 (소환/이동/합성)
**위치:** MergeArea (AddComponent)
**주요 기능:**
- 16개 슬롯 관리 (slotItems 배열)
- TrySummon (GameManager가 호출)
- OnItemClicked / MoveItem / MergeItems
- HighlightSlot (색상 피드백)
- SpiritItemData (각 정령 아이템 데이터)

### GameLogger.cs
**역할:** 모든 로그를 파일로 출력
**위치:** 정적 클래스 (어디서나 호출)
**주요 기능:**
- Info / Warn / Error 정적 메서드
- game_log.md 파일 출력 (Markdown 테이블)
- Application.logMessageReceived (모든 Unity 로그 캡처)
- 예외 발생 시 스택트레이스 기록

---

## 🐛 알려진 이슈

### 미해결
1. **색상 복귀 버그** — 클릭 시 노란색 → 선택 해제 시 원래 속성색으로 안 돌아옴
   - 원인: Button ColorTint가 img.color를 덮어씀
   - 시도한 수정: Selectable.Transition.None 설정
   - 상태: 🔄 테스트 필요
   
2. **합성 조건 버그** — 같은 element + 같은 level인데 합성 안 됨
   - 원인: 불명 (spiritName → element로 변경 완료)
   - 시도한 수정: 디버그 로그 추가 ("합성 불가" 상세 출력)
   - 상태: 🔄 테스트 필요

3. **몬스터/정령 프리팹 없음** — MonsterSpawner에 prefab 참조 없음
   - `Full Game Setup` 실행 필요 (GameSetupWizard)

4. **전투 자동 시작 안 됨** — 스테이지 선택/전투 트리거 미구현

### 해결됨
- ~~SummonBtn 클릭 안 됨~~ → EventSystem 추가 + InputSystemUIInputModule
- ~~GameManager가 Tab_도감에 붙어있음~~ → 씬 파일 수정
- ~~BattleArea/MergeArea 겹침~~ → 1% 갭 조정
- ~~한글 □□□ 표시~~ → NotoSansKR + LiberationSans Fallback
- ~~스프라이트 안 보임~~ → TextureType=Sprite로 변경
- ~~머지 조건 spiritName 비교~~ → element 비교로 변경
- ~~씬 파일 잘림~~ → git checkout으로 복구

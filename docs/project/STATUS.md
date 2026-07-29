# Spirit Merge — 프로젝트 현황 문서

> 최종 업데이트: 2026-07-29
> 프로젝트 루트: `C:\Users\user\Spiritia`

---

## 📋 TODO (진행 상황)

### ✅ 완료 (Done)

#### 1. UI 레이아웃 (100%)
- [x] TopBar (StageInfo + Gold/Ruby) 
- [x] BattleArea (적 3슬롯 + 정령 4슬롯 + WaveInfo)
- [x] MergeArea (4×4 반응형 그리드 + MergeBoard + SummonBtn)
- [x] GNB (5탭: 전투/파티/업그레드/도감/의뢰)
- [x] 전체화면 배경 (ScreenBackground, #0C0F1F)
- [x] 구역간 1% 갭 (TopBar 93~100%, Battle 55~92%, Merge 0~54%+GNB)
- [x] `Rebuild All` 단일 메뉴 (5단계 자동 실행)

#### 2. 폰트 시스템 (100%)
- [x] NotoSansKR-VariableFont_wght SDF (주 폰트)
- [x] LiberationSans SDF (Fallback 폰트)
- [x] 한글 + 영문/숫자 모두 표시 가능
- [x] 중간점(·) 사용 금지 → 공백으로 대체

#### 3. 스프라이트 (100%)
- [x] PNG → Sprite 변환
- [x] Icons/ (속성 아이콘 4종)
- [x] Enemies/Chapter{1-5}_{Fire|Water|Nature|Thunder} (적 12종)
- [x] Spirits/{Fire|Water|Nature|Thunder|Light|Dark} (정령 30종)
- [x] removebg-preview 중복 제거
- [x] Connect All Sprites (SpiritData + MonsterData 연결)
- [x] MonsterData에 sprite 필드 추가

#### 4. 게임 데이터 (100%)
- [x] StageData 50개 (5챕터 × 10스테이지)
- [x] 스테이지 이름: 불의 숲/물의 숲/숲의 길/번개의 숲/불의 성소
- [x] WaveCalculator (웨이브 분배 공식)
- [x] 공식: totalMonsters = chapter × 5 + stage / summonCost = 500+(ch-1)×300
- [x] 보스 HP 10배, 공격력 동일
- [x] 웨이브 수 = chapter + 4

#### 5. 게임 매니저 (100%)
- [x] GameManager (싱글톤, 자동 초기화)
- [x] Gold/Ruby 시스템 (SpendGold/SpendRuby)
- [x] TopBar Gold/Ruby 테스트 클릭 (+100/+10)
- [x] GNB 5탭 클릭 이벤트 연결
- [x] GameLogger (Info/Warn/Error + game_log.txt 파일 출력)

#### 6. 머지 시스템 (100%)
- [x] MergeBoardManager (소환/선택/이동/합성)
- [x] 소환: 골드 소모 → 랜덤 정령 생성
- [x] 선택/이동: 클릭 → 선택 → 빈슬롯 클릭 → 이동
- [x] 합성: 같은 정령 + 같은 레벨 → 레벨업
- [x] 속성별 색상 + 한글 이름 표시
- [x] SummonBtn raycastTarget 수정

#### 7. 전투 시스템 (80%)
- [x] BattleManager (전투 흐름 제어)
- [x] WaveController (웨이브 진행)
- [x] MonsterSpawner (SpawnPoint 기반 생성)
- [x] Monster.cs (HP/ATK/DEF, SpriteRenderer, HP바)
- [x] SpiritUnit.cs (자동 공격, HP바, 속성)
- [x] MonsterData + spirit 필드 연결

#### 8. 씬/에디터 (100%)
- [x] SceneCleanup (중복 MainCanvas 제거, 구역 리사이즈)
- [x] ApplySceneComponents (GameManager → MainCanvas 등)
- [x] AddEventSystem (UI 클릭용)

---

### 🔄 진행 중 (In Progress)

#### 전투 시스템 연동
- [ ] MonsterPrefab 생성 (Full Game Setup)
- [ ] SpiritPrefab 생성  
- [ ] SpawnPointPrefab 생성
- [ ] 전투 자동 시작 (스테이지 진입)
- [ ] 웨이브 표시 (WaveAnimator)
- [ ] HP바 업데이트
- [ ] 전투 결과 (승리/패배)

---

### ⏳ 해야 할 일 (Todo)

#### 1. 전투 시스템 완성
- [ ] Monster/Spirit 프리팹 생성 및 연결
- [ ] WaveController ↔ SpawnPoint ↔ Monster 연동
- [ ] 전투 시작 → 웨이브 진행 → 승리/패배
- [ ] 보상 지급 (Gold/Ruby)

#### 2. 파티 시스템
- [ ] 보유 정령 목록 UI
- [ ] 파티 편성 (최대 4슬롯)
- [ ] 파티 → BattleArea 정령 배치

#### 3. 머지 고도화
- [ ] 머지 애니메이션
- [ ] 소환 비용 스테이지 연동
- [ ] 다이아몬드 업그레이드

#### 4. 스테이지 진행
- [ ] StageSelect UI
- [ ] 챕터 진행 (1-1 → 1-2 → ... → 5-10)
- [ ] 재화/보상 시스템

#### 5. GNB 탭 기능
- [ ] 파티 탭: 정령 편성 화면
- [ ] 업그레드 탭: 성장 시스템
- [ ] 도감 탭: 수집한 정령 목록
- [ ] 의뢰 탭: 일일 미션

#### 6. 데이터/저장
- [ ] 저장/불러오기
- [ ] Firebase 연동 (옵션)

---

## 🏗 프로젝트 구조

```
Assets/
├── Resources/
│   ├── Data/
│   │   ├── Spirits/       ← SpiritData 30종 (.asset)
│   │   ├── Stages/        ← StageData 50종 (.asset)
│   │   └── Monsters/      ← MonsterData (런타임 생성)
│   └── Fonts & Materials/
│       ├── NotoSansKR-VariableFont_wght SDF.asset
│       └── LiberationSans SDF.asset (Fallback)
├── Scenes/
│   └── MainScene.unity
├── Scripts/
│   ├── Battle/
│   │   ├── Monster.cs           ← 몬스터 전투 유닛
│   │   ├── MonsterSpawner.cs    ← SpawnPoint + 몬스터 생성
│   │   ├── SpiritUnit.cs        ← 정령 전투 유닛
│   │   ├── WaveController.cs    ← 웨이브 진행
│   │   └── WaveAnimator.cs      ← 웨이브 표시 애니메이션
│   ├── Data/
│   │   ├── Enums.cs             ← ElementType, AnimalType 등
│   │   ├── MonsterData.cs       ← 몬스터 데이터
│   │   ├── SpiritData.cs        ← 정령 데이터
│   │   └── StageData.cs         ← 스테이지 데이터
│   ├── Editor/
│   │   ├── AddEventSystem.cs       ← EventSystem 생성
│   │   ├── AddGameManagerToScene.cs← GameManager 추가
│   │   ├── ApplySceneComponents.cs ← 모든 컴포넌트 추가
│   │   ├── BattleUIBuilder.cs      ← BattleArea UI 리빌드
│   │   ├── ConnectAllSprites.cs    ← 스프라이트 연결
│   │   ├── GNBBuilder.cs           ← GNB 리빌드
│   │   ├── GameSetupWizard.cs      ← Full Game Setup
│   │   ├── MergeUIRebuilder.cs     ← MergeArea 리빌드
│   │   ├── RebuildAll.cs           ← 전체 UI 리빌드 (5in1)
│   │   ├── SceneCleanup.cs         ← 씬 정리
│   │   ├── SpiritSpriteConnector.cs← 정력 스프라이트 연결
│   │   ├── SpriteOrganizer.cs      ← 스프라이트 폴더 정리
│   │   ├── StageDataGenerator.cs   ← 스테이지 50개 생성
│   │   └── TopBarBuilder.cs        ← TopBar 리빌드
│   ├── Manager/
│   │   ├── BattleManager.cs     ← 전투 시스템 관리
│   │   └── GameManager.cs       ← 게임 메인 매니저
│   ├── Merge/
│   │   └── MergeBoardManager.cs ← 머지보드 (소환/이동/합성)
│   └── Util/
│       ├── GameLogger.cs        ← 로깅 시스템
│       └── WaveCalculator.cs    ← 웨이브 분배 계산
└── Sprites/
    ├── Icons/           ← 속성 아이콘 (icon_*.png)
    ├── Enemies/
    │   ├── Chapter1_Fire/
    │   ├── Chapter2_Water/
    │   ├── Chapter3_Nature/
    │   └── Chapter4_Thunder/
    └── Spirits/
        ├── Fire/        ← 불 속성 정령
        ├── Water/       ← 물 속성 정령
        ├── Nature/      ← 자연 속성 정령
        ├── Thunder/     ← 번개 속성 정령
        ├── Light/       ← 빛 속성 정령
        └── Dark/        ← 어둠 속성 정령
docs/
├── AI_PROMPTS.md        ← ComfyUI 프롬프트 가이드
├── GAME_DESIGN.md       ← 게임 디자인 문서
└── project/
    └── STATUS.md        ← 이 파일 (프로젝트 현황)
```

---

## 🔧 알려진 이슈

### 해결됨
- ~~SummonBtn 클릭 안 됨~~ → MergeBoard Image raycastTarget=false + SummonBtn targetGraphic 설정
- ~~GameManager가 Tab_도감에 붙어있음~~ → 씬 파일 직접 수정으로 MainCanvas로 이동
- ~~BattleArea/MergeArea 겹침~~ → 1% 갭 조정
- ~~Lv.1 텍스트가 빈 슬롯에 표시~~ → LevelText 비움
- ~~한글 □□□ 표시~~ → NotoSansKR + LiberationSans Fallback
- ~~Sprite 참조 오류~~ → elementType → element 수정
- ~~씬 파일 잘림~~ → git checkout으로 복구

### 미해결
1. **EventSystem 없음** → `SpiritMerge > Setup > Add EventSystem` 실행 후 Play
2. **Monster/Spirit 프리팹 없음** → `SpiritMerge > Setup > Full Game Setup` 실행 필요
3. **전투 자동 시작 안 됨** → 스테이지 선택/전투 트리거 미구현
4. **MonsterData 에셋 없음** → ConnectAllSprites가 런타임 생성

---

## 🎮 플레이 방법 (현재)

1. Unity Editor 열기
2. `SpiritMerge > Setup > Add EventSystem` (최초 1회)
3. `SpiritMerge > Setup > Apply All Components` (최초 1회)
4. Scene 저장 (Ctrl+S)
5. Play!
6. TopBar GoldText 클릭 → 골드 획득
7. 정령 소환 버튼 클릭 → 정령 생성
8. 정령 클릭 → 선택 → 빈 슬롯/같은레벨 정령 클릭 → 이동/합성
9. 테스트 완료 후 `game_log.txt` 내용 전송

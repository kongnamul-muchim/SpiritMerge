# Spirit Merge — 시스템 상세 문서

> **최종 업데이트:** 2026-07-29

---

## 1. 로깅 시스템 (GameLogger)

### 개요
모든 게임 액션과 Unity 로그를 `game_log.md`에 기록.

### 로그 포맷
```
| 시각 | 종류 | 메시지 |
|------|------|--------|
| 23:09:36.556 | INFO | [GameLogger] 로깅 시스템 시작 |
| 23:09:36.560 | INFO | [GM] GameManager 시작 |
| 23:09:36.562 | WARN | [GM] 골드 부족: 100/500 |
| 23:09:36.563 | ERROR | [GM] SpiritData 없음! |
```

### 분류 체계
| 레벨 | 설명 |
|------|------|
| `INFO` | 일반 로그 (버튼 클릭, 소환 성공, 합성 등) |
| `WARN` | 경고 (골드 부족, 빈 슬롯 없음, 파일 없음 등) |
| `ERROR` | 오류 (NullReference, 데이터 없음, 예외 등) |

### 기술 구현
- **클래스:** `GameLogger` (정적 클래스)
- **출력 파일:** `Assets/../game_log.md` (프로젝트 루트)
- **파일 모드:** Append (이어쓰기)
- **AutoFlush:** true (즉시 디스크 기록)
- **추가 캡처:** `Application.logMessageReceived` (모든 Unity 로그)
- **스택트레이스:** ERROR/Exception 발생 시 기록

---

## 2. 머지 시스템 (MergeBoardManager)

### 개요
4×4 그리드 위에서 정령을 소환/이동/합성하는 핵심 시스템.

### 슬롯 구조
```
MergeArea
└── MergeBoard (Image, raycastTarget=false)
    ├── Slot_0
    │   ├── Inner (Image) — 빈 슬롯 배경
    │   ├── LevelText (TMP) — "불 Lv.2"
    │   └── SpiritItem (Image+Button) — 정령 아이콘
    ├── Slot_1
    └── ...
```

### 동작 흐름
```
1. 소환 버튼 클릭
   → GameManager.OnSummonClicked()
   → MergeBoardManager.TrySummon(SpiritData)
   → 첫 번째 빈 슬롯에 SpiritItem 생성
   
2. 정령 클릭 (첫 번째)
   → OnItemClicked(idx)
   → selectedSlot = idx, Highlight(yellow)
   
3. 빈 슬롯 클릭 (두 번째)
   → MoveItem(from, to)
   → 정령 이동, 색상 복원
   
4. 같은 element + 같은 level 정령 클릭
   → MergeItems(from, to)
   → from 파괴, to 레벨업
```

### 합성 조건 (C#)
```csharp
fromData.element == toData.element &&    // 같은 속성
fromData.level == toData.level &&        // 같은 레벨
fromData.level < maxLevel                // 최대 레벨 미만
```

---

## 3. 전투 시스템 (Battle)

### 개요
웨이브 기반 자동 전투, 정령들이 자동으로 적을 공격.

### 구성 요소
| 컴포넌트 | 역할 |
|---------|------|
| BattleManager | 전투 흐름 제어, SpiritSpawnRoot/EnemySpawnRoot 관리 |
| WaveController | 웨이브 진행, WaveCalculator 사용 |
| MonsterSpawner | SpawnPoint에서 몬스터 생성 |
| Monster | 몬스터 유닛 (HP/ATK/DEF, SpriteRenderer, HP바) |
| SpiritUnit | 정령 전투 유닛 (자동 공격, HP바, 속성) |

### 웨이브 계산
```
totalMonsters = chapter × 5 + stage
waves = chapter + 4
보스: n-5, n-10 스테이지 (HP 10배)
```

---

## 4. 데이터 구조 (ScriptableObject)

### SpiritData
| 필드 | 타입 | 설명 |
|------|------|------|
| spiritName | string | 정령 이름 |
| element | ElementType | 속성 (Fire/Water/Earth/Wind/Dark/Light) |
| animalType | AnimalType | 동물 타입 |
| sprite | Sprite | 정령 이미지 |
| atk | int | 공격력 |
| def | int | 방어력 |
| hp | int | 체력 |

### MonsterData
| 필드 | 타입 | 설명 |
|------|------|------|
| monsterName | string | 몬스터 이름 |
| element | ElementType | 속성 |
| sprite | Sprite | 몬스터 이미지 |
| atk | int | 공격력 |
| def | int | 방어력 |
| hp | int | 체력 |

### StageData
| 필드 | 타입 | 설명 |
|------|------|------|
| stageId | string | "1-1", "2-5" 등 |
| chapter | int | 챕터 (1~5) |
| stage | int | 스테이지 (1~10) |
| spawnCount | int | 총 몬스터 수 |

---

## 5. UI 구조 (Unity Canvas)

```
MainCanvas (Canvas, GraphicRaycaster, GameManager)
├── ScreenBackground (Image) — #0C0F1F
├── TopBar (RectTransform, 0~7%)
│   ├── StageInfo (TMP) — "1-1  불의 숲"
│   ├── GoldIcon + GoldText (TMP+Button) — "500 Gold"
│   └── RubyIcon + RubyText (TMP+Button) — "100 Ruby"
├── BattleArea (RectTransform, 55~92%)
│   ├── BattleBackground (Image)
│   ├── SpiritSpawnRoot
│   ├── EnemySpawnRoot
│   └── WaveInfo (TMP)
├── MergeArea (RectTransform, 0~54%, offsetMin=0,60)
│   ├── MergeSectionHeader (TMP) — "합성"
│   ├── MergeBoard
│   │   ├── Slot_0 ~ Slot_15
│   │   └── ...
│   └── SummonBtn (Image+Button) — "정령 소환 (500 Gold)"
└── BottomMenu (GNB, 하단 60px)
    ├── Tab_0 — 전투
    ├── Tab_1 — 파티
    ├── Tab_2 — 업그레드
    ├── Tab_3 — 도감
    └── Tab_4 — 의뢰
```

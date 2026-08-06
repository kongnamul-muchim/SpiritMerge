# 의뢰 시스템 (파견 / 미션 / 레이드)

> 작성: 2026-08-05 | GNB 의뢰 탭(4)에서 표시, RequestUI 3패널
> 관련 코드: `Core/Systems/{MissionService, DispatchService, RaidService}.cs`, `Presentation/UI/HUD/RequestUI.cs`

---

## 1. 파견 (Dispatch) — 정령 소멸형

**목적**: 필요 없는 정령을 보내 머지칸을 비우는 용도. 보상은 보너스 개념.

### 동작
1. 의뢰 생성: `DispatchService.GenerateRequest()` — 슬롯 1~3개(조건: 속성+최소성급), 보상(골드/루비), 시간(1~3h)
2. 파견: 보드 정령이 조건 1개 이상 매칭되면 파견 가능
   - `MergeBoardManager.RemoveBoardSpirit(i)` → **정령 소멸** (보드에서 제거, 파티 배치 시 자동 해제)
   - `DispatchService.TryStart(req, element, grade, name, durationScale)`
3. 경과: `GameManager.Update()` → `dispatch.Tick(deltaTime)` — 완료 감지 (Notified 1회)
4. 보상: `Claim(i)` → 골드/루비 지급
   - **조건 매칭 보너스**: 매칭 슬롯 비율만큼 보상 증가 `MatchBonus()`

### 시간 처리
- `dispatchTimeScale`: 1.0 = 실시간 (1~3시간), 축소 = 테스트 (예: 0.0001 → 즉시 완료)
- ⚠️ 축소는 `TryStart`의 `RemainingSeconds` 계산에만 적용, `Tick`은 실제 deltaTime (이중 축소 금지)

### 파견 슬롯
- 기본 2칸 (`DispatchService.MaxActive = 2`), GDD 기준 업그레이드 연동은 TODO

---

## 2. 미션 (Mission) — 일일/주간 각 10종

**목적**: 방치형 필수 — 일일/주간 미션, 루비 수급.

### 10종 (주간 = 일일 수치 × 5, 보상 2~3배)
| # | 미션 | 일일 목표 | 보상(일일) |
|---|------|----------|-----------|
| 1 | 몬스터 n마리 처치 | 100 | 루비 20 |
| 2 | 업그레이드 n회 | 5 | 루비 15 |
| 3 | 보스 n회 처치 | 3 | 루비 25 |
| 4 | 소환 n회 | 10 | 루비 15 |
| 5 | 머지 n회 | 5 | 루비 20 |
| 6 | 파견 n회 | 3 | 루비 20 |
| 7 | 골드 n 획득 | 5,000 | 골드 500 + 루비 10 |
| 8 | 스테이지 n회 클리어 | 10 | 루비 20 |
| 9 | 레벨업 n회 | 1 | 루비 10 |
| 10 | 로그인 n회 | 1 | 루비 10 |

### 이벤트 훅 (GameManager)
- `OnMonsterKilled` ← Monster.Die
- `OnSpiritSummoned` ← SummonSpirit
- `OnSpiritMerged` ← MergeBoardManager.MergeItems
- `OnUpgraded` ← OnGoldUpgrade / OnRubyUpgrade
- `OnDispatched` ← 파견 시작
- `OnStageCleared(isBoss)` ← WaveController.OnBattleWon (보스 스테이지면 BossKill도)
- `OnGoldEarned` ← AddGold
- `OnLevelUp` ← AddPlayerExp 레벨업
- 로그인 ← GameManager.Start

---

## 3. 레이드 (Raid) — 주간 속성 보스 + 점수 단계

**목적**: 매주 바뀌는 속성 보스를 60초 동안 공격, 점수 경쟁.

### 동작
1. 주간 보스: `RaidService.RollWeeklyBoss()` — 6속성 중 랜덤
2. 전투: `RequestUI.RaidLoop()` 코루틴 — 60초
   - 파티 공격력 합(`SpiritUnit.atk`) 기반 데미지 (실제 전투 스탯 사용)
   - 데미지 → 점수(`AddDamage`) + 보스 HP 감소
3. 단계: 점수 누적 기준 `GetStageByScore` — 1~10단계
   - 단계 오를수록 보스 스탯 증가: `GetBossHP(1000+stage²×800)`, `GetBossATK(100+stage×60)`
   - 보스 처치 시 새 보스 등장 (단계 상승)
4. 종료: `EndRaid(score)` — 최고 점수 갱신(신기록)

### 보상
- 단계 보상: `TryClaimStageReward(stage)` — 주간 1회, 골드 500×단계 / 루비 5×단계
- 랭킹 보상: `GetRankTier()` — 최고 점수 구간별 티어 (1: 10만+ 루비100, 2: 3만+ 루비50, 3: 참여 루비20)
- 주간 초기화: `RollWeeklyBoss()` (메모리 기준 — 저장 연동 시 날짜 기준 리셋)

---

## CLI 검증 명령 (CliTestSuite)
- `CmdShowRequestTab` — 의뢰 탭 + 스크린샷
- `CmdDispatchTest` — 파견 시작 (시간 축소 0.0001)
- `CmdDispatchClaim` — 완료 파견 보상 수령
- `CmdMissionCheck` — 일일/주간 미션 진행도
- `CmdRaidStart` — 레이드 시작 (RequestUI 활성화 포함)

---

## 🔶 저장/불러오기 연동 TODO (메모리 → 영속화)

현재 의뢰 시스템은 **메모리 기반** — Play 재시작 시 초기화됨. 저장 시스템(DataManager/SaveData) 구현 시 아래 데이터를 영속화할 것:

| 데이터 | 필드 | 저장 시점 |
|--------|------|----------|
| 미션 진행도 | `MissionService.DailyProgress/WeeklyProgress/Claimed` | 주기/종료 시 |
| 파견 상태 | `DispatchService.Active` (요청+남은시간) | 주기/종료 시 |
| 파견 누적 | `DispatchService.TotalDispatchCount` | — |
| 레이드 | `RaidService.WeeklyBossElement/Stage/TotalDamage/BestScore/StageRewardClaimed` | 주기/종료 시 |

**리셋 기준**: 일일 미션 = 날짜 변경, 주간 미션·레이드 = 주(월요일) 변경. `SaveData`에 저장 시각 필드로 판별.

### DataManager 배치 (Pending)
- `DataManager` 클래스는 존재하지만 씬 미배치 (`ProjectSetup.cs`가 프리팹에 추가하도록 설계됨)
- GameManager에 `SaveGame()` 트리거 + Play 시작 시 `LoadGame()` 복원 필요

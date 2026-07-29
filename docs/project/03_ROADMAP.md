# Spirit Merge — 개발 로드맵

> **최종 업데이트:** 2026-07-29
> **레전드:** ✅ 완료 | 🔧 진행중 | ⏳ 대기 | ❌ 미구현

---

## Phase 1: 프로젝트 기반 (✅ 완료)

- [x] Unity 6000.5.5f1 프로젝트 생성
- [x] VContainer DI 셋업
- [x] GitHub 저장소 연결
- [x] Input System 패키지 설치 (v1.19.0)

---

## Phase 2: UI 레이아웃 (✅ 완료)

- [x] TopBar (StageInfo + Gold/Ruby)
- [x] BattleArea (적 3슬롯 + 정령 4슬롯 + WaveInfo)
- [x] MergeArea (4×4 반응형 그리드 + MergeBoard + SummonBtn)
- [x] GNB (5탭: 전투/파티/업그레드/도감/의뢰)
- [x] 전체화면 배경
- [x] 구역간 1% 갭 (겹침 방지)
- [x] `Rebuild All` 단일 메뉴 (5단계 자동 실행)

---

## Phase 3: 한글 폰트 시스템 (✅ 완료)

- [x] NotoSansKR-VariableFont_wght SDF (주 폰트)
- [x] LiberationSans SDF (Fallback, 영문 지원)
- [x] 중간점(·) 사용 금지 → 공백으로 대체
- [x] 모든 UI 텍스트 폰트 통일

---

## Phase 4: 스프라이트 (✅ 완료)

- [x] PNG → Sprite 변환
- [x] Icons/ (속성 아이콘 4종)
- [x] Enemies/ (챕터별 적 12종)
- [x] Spirits/ (속성별 정령 30종)
- [x] Connect All Sprites (ScriptableObject 연결)
- [x] MonsterData에 sprite 필드 추가
- [x] removebg-preview 중복 제거

---

## Phase 5: 게임 데이터 (✅ 완료)

- [x] StageData 50개 (5챕터 × 10스테이지)
- [x] 챕터별 이름 (불의 숲/물의 숲/숲의 길/번개의 숲/불의 성소)
- [x] WaveCalculator (웨이브 분배)
- [x] MonsterData (몬스터 스탯)
- [x] SpiritData (정령 스탯)

---

## Phase 6: 게임 매니저 (✅ 완료)

- [x] GameManager (싱글톤, 자동 초기화)
- [x] Gold/Ruby 시스템
- [x] GameLogger (Info/Warn/Error + game_log.md)
- [x] 모든 Unity 로그 캡처 (Application.logMessageReceived)
- [x] Add EventSystem → 버튼 클릭 활성화

---

## Phase 7: 머지 시스템 (🔧 진행중)

### 완료
- [x] MergeBoardManager (소환/선택/이동/합성)
- [x] 소환: 골드 소모 → 랜덤 정령 생성
- [x] 선택/이동: 클릭 → 선택 → 빈 슬롯 클릭 → 이동
- [x] 합성 조건: element + level (spiritName 대체)
- [x] Button ColorTint → None (색상 충돌 해결)
- [x] 정령 아이템 생성 시 속성 색상 표시

### 버그 수정중
- [ ] 클릭 색상 복귀 (노란색 → 원래 속성색)
- [ ] 합성 조건 디버깅 (왜 element+level 같아도 안 되는지)

### 예정
- [ ] 드래그 드랍 합성
- [ ] 머지 애니메이션
- [ ] 합성 시 이펙트

---

## Phase 8: 전투 시스템 (🔧 진행중)

### 완료
- [x] BattleManager (전투 흐름 제어)
- [x] WaveController (웨이브 진행)
- [x] MonsterSpawner (SpawnPoint 기반)
- [x] Monster.cs (HP/ATK/DEF, SpriteRenderer, HP바)
- [x] SpiritUnit.cs (자동 공격, HP바, 속성 아이콘)

### 예정
- [ ] Monster/Spirit 프리팹 생성
- [ ] 전투 자동 시작 트리거
- [ ] 웨이브 표시 UI (WaveAnimator)
- [ ] HP바 업데이트
- [ ] 전투 결과 (승리/패배)
- [ ] 보상 지급

---

## Phase 9: 파티 시스템 (⏳ 대기)

- [ ] 보유 정령 목록 UI
- [ ] 파티 편성 (4슬롯 드래그)
- [ ] 파티 정령 → BattleArea 정령 배치
- [ ] 속성 시너지 계산

---

## Phase 10: 스테이지 진행 (⏳ 대기)

- [ ] StageSelect UI
- [ ] 챕터 진행 (1-1 → 1-2 → ... → 5-10)
- [ ] 재화/보상 시스템
- [ ] 클리어 조건 / 별점

---

## Phase 11: 업그레이드 / 의뢰 / 도감 (❌ 미구현)

- [ ] 업그레이드 트리 UI
- [ ] 의뢰(파견) 시스템
- [ ] 도감 시스템
- [ ] 저장/불러오기

---

## Phase 12: 폴리싱 (❌ 미구현)

- [ ] 합성/소환/전투 이펙트
- [ ] BGM/SFX
- [ ] 튜토리얼
- [ ] 최적화
- [ ] 빌드 (Android/iOS)

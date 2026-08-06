using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SpiritMerge.Battle;
using SpiritMerge.Core.Systems;
using SpiritMerge.Merge;

namespace SpiritMerge
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        // ⭐ 도메인 리로드(재컴파일) 후 static이 리셋돼도 씬 컴포넌트를 자동 재연결 (staticMatch 유지)
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                return _instance;
            }
            private set { _instance = value; }
        }

        public Canvas mainCanvas;
        public BattleManager battleManager;

        public int currentStage = 1;
        public int gold = 500;
        public int ruby = 100;

        // ⭐ 스테이지 진행: 1-1부터 시작, 클리어 시 다음 스테이지 (정렬된 배열 인덱스)
        private int currentStageIndex = 0;

        // ⭐ 반복 모드: true = 실패 시 이전 스테이지로 이동 / false = 현재 스테이지 재도전 (등반)
        public bool repeatMode = false;

        // ⭐ 저장 시스템 (모바일 자동 저장 — JSON + 백업)
        private DataManager dataManager;
        private float _saveTimer;
        private const float AutoSaveInterval = 30f;
        private bool saveLoaded = false;   // 저장 로드 여부 (스테이지 인덱스 덮어쓰기 방지)

        // ⭐ 도감: 획득한 정령 등록 (정령 asset 이름 기준 — 속성×등급 30종)
        public readonly HashSet<string> unlockedSpirits = new HashSet<string>();

        /// <summary>정령 도감 등록 (소환/합성/시작 시)</summary>
        public void UnlockSpirit(SpiritData data)
        {
            if (data == null) return;
            if (unlockedSpirits.Add(data.name))
                GameLogger.Info($"[GM] 도감 등록: {data.spiritName} ({(int)data.grade}성)");
        }

        /// <summary>속성+등급 기준 도감 등록 (합성 결과 등)</summary>
        public void UnlockSpiritByGrade(ElementType element, int grade)
        {
            var all = Resources.LoadAll<SpiritData>("Data/Spirits");
            foreach (var s in all)
                if (s != null && s.element == element && (int)s.grade == grade)
                {
                    UnlockSpirit(s);
                    break;
                }
        }

        // 파티 편성 오버레이 연동
        private MergeBoardManager board;
        private PartyFormationUI formationUI;
        private WaveController waveCtrl;

        // ⭐ 업그레이드 시스템 (경험치 → 레벨업 → SP → 스킬트리)
        public PlayerService player = new PlayerService();
        private UpgradeUI upgradeUI;

        // ⭐ 의뢰 시스템 (파견/미션/레이드)
        //   도메인 리로드 후 직렬화되지 않는 타입 필드가 null로 남는 문제 방지 (자동 재생성)
        private MissionService _missions;
        public MissionService missions
        {
            get { if (_missions == null) _missions = new MissionService(); return _missions; }
        }

        private DispatchService _dispatch;
        public DispatchService dispatch
        {
            get { if (_dispatch == null) _dispatch = new DispatchService(); return _dispatch; }
        }

        private RaidService _raid;
        public RaidService raid
        {
            get { if (_raid == null) _raid = new RaidService(); return _raid; }
        }

        public float dispatchTimeScale = 1f;   // 파견 시간 배율 (1=실시간, 축소=테스트)
        public bool raidActive = false;        // 레이드 진행 중
        private RequestUI requestUI;
        public DispatchFormationUI dispatchFormation; // 파견 배치 오버레이 (BattleArea)
        private DexUI dexUI;

        // ⭐ 속성 조합 시너지 (파티 구성 기반 보너스 — 1슬롯 기준)
        private float synergyAtkPct;      // 불: 공격력
        private float synergySpdPct;      // 물: 공격속도
        private float synergyCritPct;     // 바람: 치명타
        private float synergyVampPct;     // 어둠: 흡혈
        private float synergyHealPct;     // 빛: 웨이브 회복
        private float synergyDefPct;      // 빛: 파티 방어
        private float synergyAtkFlatPct;  // 어둠: 파티 공격
        private int synergyEarthCount;    // 대지: 통합 HP 보너스 (시너지 수치)
        private int synergyFireCount, synergyWaterCount, synergyWindCount, synergyDarkCount, synergyLightCount; // 표시용 카운트

        // 배치된 전투 정령 추적 (SpiritGroup 슬롯 기반 배치 후 재배치 시 정리용)
        // ⭐ readonly 제거 — 도메인 리로드 후 null이 되는 케이스 방어
        private List<GameObject> deployedSpirits = new List<GameObject>();

        void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        void Start()
        {
            GameLogger.Info("[GM] GameManager 시작");
            ScaleSceneFonts(); // ⭐ 씬 전체 TMP 텍스트 폰트 30% 확대 (런타임 생성 UI 전에)

            mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas == null) { GameLogger.Error("[GM] MainCanvas 없음!"); return; }

            SetupBattleSystem();
            SetupMergeSystem();
            SetupGNBTabs();
            SetupTopBar();

            // ⭐ 저장 로드 (보드 준비 후, 자동 전투 전 — 파티/스테이지/의뢰 복원)
            LoadSavedGame();

            // ⭐ 게임 시작 시 자동 전투
            StartAutoBattle();

            // ⭐ 파티 편성 오버레이 (자동 전투 이후 — 시작 정령 자동 파티 배치가 끝난 뒤 생성/구독)
            SetupFormationSystem();

            // ⭐ 업그레이드 오버레이 (머지+배틀 전체 패널) + 자동 소환 루프
            SetupUpgradeSystem();

            // ⭐ 도감 오버레이 (정령 30종 획득 현황)
            SetupDexSystem();

            // ⭐ 화면 전체 클릭 → MergeBoard 선택 해제
            SetupGlobalDeselect();

            // ⭐ 의뢰 시스템 (파견/미션/레이드 오버레이)
            SetupRequestSystem();

            missions?.Progress(MissionType.Login); // 로그인 미션
            GameLogger.Info("[GM] 모든 시스템 초기화 완료!");
        }

        /// <summary>파견 시간 경과 + 완료 감지 (미션 진행도 연동) + 자동 저장 주기</summary>
        void Update()
        {
            if (dispatch == null) return;
            dispatch.Tick(Time.deltaTime);
            // ⭐ 완료 파견 알림 (Completed로 이동 — 보상받기 버튼으로 수령)
            for (int i = 0; i < dispatch.Completed.Count; i++)
            {
                if (!dispatch.Completed[i].Notified)
                {
                    dispatch.Completed[i].Notified = true;
                    GameLogger.Info($"[GM] 파견 완료: {dispatch.Completed[i].Spirit1Name} 외 1마리 — 보상 수령 가능");
                    if (requestUI != null) requestUI.Refresh();
                    SaveNow(); // ⭐ 파견 완료 시 즉시 저장 (강제종료 대비)
                }
            }

            // ⭐ 자동 저장 (30초 주기 — 강제종료 대비 빽빽한 저장)
            _saveTimer += Time.deltaTime;
            if (_saveTimer >= AutoSaveInterval)
            {
                _saveTimer = 0f;
                SaveNow();
            }
        }

        // ⭐ 모바일 자동 저장: 백그라운드 전환/종료 시 즉시 저장
        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        // ── 미션 이벤트 훅 (호출 지점: Monster/MergeBoard/전투 등) ──

        public void OnMonsterKilled() => missions?.Progress(MissionType.KillMonster);
        public void OnSpiritSummoned()
        {
            missions?.Progress(MissionType.Summon);
            SaveNow(); // ⭐ 소환 시 즉시 저장 (보드 상태)
        }
        public void OnSpiritMerged()
        {
            missions?.Progress(MissionType.Merge);
            SaveNow(); // ⭐ 머지 시 즉시 저장 (성급 상승)
        }
        public void OnUpgraded()
        {
            missions?.Progress(MissionType.Upgrade);
            SaveNow(); // ⭐ 업그레이드 시 즉시 저장 (SP/골드/루비 강화)
        }
        public void OnDispatched()
        {
            missions?.Progress(MissionType.Dispatch);
            SaveNow(); // ⭐ 파견 시작 시 즉시 저장
        }

        /// <summary>스테이지 클리어 — 보스 스테이지면 보스 처치도 기록</summary>
        public void OnStageCleared(bool isBoss)
        {
            missions?.Progress(MissionType.StageClear);
            if (isBoss) missions?.Progress(MissionType.BossKill);
            SaveNow(); // ⭐ 스테이지 클리어 시 즉시 저장 (스테이지 진행)
        }

        public void OnGoldEarned(int amount) => missions?.Progress(MissionType.GainGold, amount);
        public void OnLevelUp() => missions?.Progress(MissionType.LevelUp);

        private bool _battleStarted = false;

        /// <summary>
        /// ⭐ 씬에 저장된 모든 TMP 텍스트 폰트 30% 확대 (Hierarchy 전체)
        /// - 파티/업그레이드 같은 런타임 생성 UI 전에 호출 (중복 방지)
        /// </summary>
        void ScaleSceneFonts()
        {
            int count = 0;
            foreach (var tmp in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            {
                tmp.fontSize = Mathf.Min(20, Mathf.RoundToInt(tmp.fontSize * 1.3f)); // ⭐ 최대 20폰트
                if (tmp.enableAutoSizing)
                {
                    tmp.fontSizeMin = Mathf.Min(20, Mathf.RoundToInt(tmp.fontSizeMin * 1.3f));
                    tmp.fontSizeMax = Mathf.Min(20, Mathf.RoundToInt(tmp.fontSizeMax * 1.3f));
                }
                count++;
            }
            GameLogger.Info($"[GM] 씬 TMP 폰트 30% 확대(최대 20): {count}개");
        }

        /// <summary>
        /// 게임 시작 시 자동 전투 실행 (방치형)
        /// 1. 기본 정령 지급 → 2. 배틀 배치 → 3. 전투 시작
        /// </summary>
        void StartAutoBattle()
        {
            if (_battleStarted) return;
            _battleStarted = true;

            GiveStarterSpirit();
            DeploySpiritsToBattle();

            var allStages = GetSortedStages();
            if (allStages.Length == 0) { GameLogger.Error("[GM] StageData 없음! 전투 불가"); return; }
            if (!saveLoaded) currentStageIndex = 0; // ⭐ 1-1부터 시작 (저장 로드 시 로드값 유지)
            var stage = allStages[currentStageIndex];
            UpdateStageInfo(stage); // ⭐ TopBar 스테이지 표시 갱신

            var spawner = FindAnyObjectByType<MonsterSpawner>();
            if (spawner != null) spawner.InitializeEnemySlots(stage.spawnPointCount);

            waveCtrl = FindAnyObjectByType<WaveController>();
            if (waveCtrl == null) { GameLogger.Error("[GM] WaveController 없음!"); return; }

            // ⭐ 스테이지 클리어 → R(반복): 현재 유지 / C(등반): 다음 스테이지
            waveCtrl.OnBattleWon += () =>
            {
                var sorted = GetSortedStages();
                if (sorted.Length == 0) return;

                OnStageCleared(GetCurrentStage()?.isBossStage ?? false); // ⭐ 미션 진행도 (클리어/보스)

                if (!repeatMode) // C(등반)만 다음 스테이지로
                    currentStageIndex = (currentStageIndex + 1) % sorted.Length;

                var next = sorted[currentStageIndex];
                GameLogger.Info($"[GM] ⭐ 스테이지 클리어! ({(repeatMode ? "반복 유지" : "다음 스테이지")}): {next.stageName}");
                UpdateStageInfo(next); // ⭐ TopBar 스테이지 표시 갱신

                // ⭐ Clear 배너 (Battle Area 정중앙)
                var animator = waveCtrl != null ? waveCtrl.GetComponentInChildren<WaveAnimator>() : null;
                if (animator != null) animator.ShowClear($"{next.region}-{next.stageNumber}");

                waveCtrl.StartBattle(next);
            };

            // 💀 파티 HP 0 → ⭐ 둘 다 이전 스테이지로 이동 (1-1이면 유지)
            waveCtrl.OnBattleLost += () =>
            {
                var sorted = GetSortedStages();
                if (sorted.Length == 0) return;

                currentStageIndex = Mathf.Max(0, currentStageIndex - 1);

                var retry = sorted[currentStageIndex];
                GameLogger.Info($"[GM] 💀 파티 HP 0 → {retry.stageName} 재도전! (모드: {(repeatMode ? "반복" : "등반")})");
                UpdateStageInfo(retry); // ⭐ TopBar 스테이지 표시 갱신

                // ⭐ Fail 배너 (Battle Area 정중앙)
                var animator = waveCtrl != null ? waveCtrl.GetComponentInChildren<WaveAnimator>() : null;
                if (animator != null) animator.ShowFail();

                DeploySpiritsToBattle();
                waveCtrl.StartBattle(retry);
            };

            GameLogger.Info($"[GM] 전투 자동 시작: {stage.stageName}");
            waveCtrl.StartBattle(stage);
        }

        /// <summary>
        /// 스테이지 정렬 로드 (region → stageNumber 오름차순: 1-1, 1-2, 2-1, ...)
        /// Resources.LoadAll은 정렬 순서를 보장하지 않으므로 명시적 정렬
        /// </summary>
        static StageData[] GetSortedStages()
        {
            var all = Resources.LoadAll<StageData>("Data/Stages");
            System.Array.Sort(all, (a, b) =>
            {
                int c = a.region.CompareTo(b.region);
                return c != 0 ? c : a.stageNumber.CompareTo(b.stageNumber);
            });
            return all;
        }

        /// <summary>
        /// TopBar의 StageInfo 텍스트를 현재 스테이지에 맞게 갱신
        /// (예: "Stage 1-1 불의 숲")
        /// </summary>
        void UpdateStageInfo(StageData stage)
        {
            if (stage == null) return;
            var st = GameObject.Find("StageInfo")?.GetComponent<TextMeshProUGUI>();
            if (st == null)
            {
                GameLogger.Warn("[GM] StageInfo 없음 — 스테이지 표시 갱신 스킵");
                return;
            }
            st.text = $"Stage {stage.stageName}";
            GameLogger.Info($"[GM] StageInfo 갱신: {stage.stageName}");

            UpdateSummonButtonText(); // ⭐ 소환 버튼도 현재 챕터 비용으로 갱신
        }

        // ── 파티 편성 시스템 ──

        /// <summary>
        /// 파티 편성 오버레이 생성 + 파티 변경 이벤트 구독
        /// (자동 전투가 끝난 뒤 호출 — 시작 정령 자동 배치의 이벤트가 새지 않도록)
        /// </summary>
        void SetupFormationSystem()
        {
            var ba = GameObject.Find("BattleArea");
            if (ba == null) { GameLogger.Warn("[GM] BattleArea 없음, 편성 오버레이 스킵"); return; }
            if (board == null) { GameLogger.Warn("[GM] MergeBoardManager 없음, 편성 오버레이 스킵"); return; }

            formationUI = PartyFormationUI.Create(ba.transform, board);
            board.OnPartyChanged += OnPartyChangedHandler;
            formationUI.Refresh();
            GameLogger.Info("[GM] 파티 편성 오버레이 생성 완료");
        }

        // ── 업그레이드 시스템 ──

        /// <summary>
        /// 도감 오버레이 생성 (Canvas 최상위) — 정령 30종 획득 현황
        /// </summary>
        void SetupDexSystem()
        {
            Transform parent = mainCanvas != null ? mainCanvas.transform : null;
            if (parent == null) { GameLogger.Warn("[GM] 캔버스 없음, 도감 오버레이 스킵"); return; }

            dexUI = DexUI.Create(parent, () => OnTabClicked(0)); // ⭐ 닫기 → 전투 탭

            // GNB가 항상 위에 보이도록 (업그레이드와 동일)
            var bm = GameObject.Find("BottomMenu");
            if (bm != null) dexUI.transform.SetSiblingIndex(bm.transform.GetSiblingIndex());

            GameLogger.Info("[GM] 도감 오버레이 생성 완료");
        }

        /// <summary>
        /// 의뢰 오버레이 생성 (Canvas 최상위) — 파견/미션/레이드 3패널
        /// </summary>
        void SetupRequestSystem()
        {
            Transform parent = mainCanvas != null ? mainCanvas.transform : null;
            if (parent == null)
            {
                var ba = GameObject.Find("BattleArea");
                if (ba != null) parent = ba.transform;
            }
            if (parent == null)
            {
                GameLogger.Warn("[GM] 캔버스/BattleArea 없음, 의뢰 오버레이 스킵");
                return;
            }

            requestUI = RequestUI.Create(parent, this);

            // GNB가 항상 위에 보이도록
            var bm = GameObject.Find("BottomMenu");
            if (bm != null) requestUI.transform.SetSiblingIndex(bm.transform.GetSiblingIndex());

            requestUI.gameObject.SetActive(false); // 초기엔 전투 탭 — 숨김
            GameLogger.Info("[GM] 의뢰 오버레이 생성 완료");

            // ⭐ 레이드 실전투 (BattleArea에 부착 — 전투 화면에서 보스 전투)
            var raidBa = GameObject.Find("BattleArea");
            if (raidBa != null && raidBa.GetComponent<RaidBattle>() == null)
                raidBa.AddComponent<RaidBattle>();

            // ⭐ 파견 배치 오버레이 — 파티 편성과 동일하게 BattleArea에 배치
            //    (MergeArea는 하단, BattleArea는 중단 — 서로 안 겹치므로 보드가 안 가려짐)
            var ba2 = GameObject.Find("BattleArea");
            if (ba2 != null && ba2.GetComponentInChildren<DispatchFormationUI>() == null)
                dispatchFormation = DispatchFormationUI.Create(ba2.transform, this, null);
        }

        /// <summary>
        /// 업그레이드 오버레이 생성 (⭐ 머지+배틀 전체 화면 — BattleArea가 아닌 Canvas 최상위에) + 자동 소환 루프 시작
        /// </summary>
        void SetupUpgradeSystem()
        {
            // ⭐ 파티 편성 UI와 달리 업그레이드는 머지+배틀 전체를 한눈에 봐야 하므로
            //    BattleArea가 아니라 Canvas 최상위(전체 화면)에 오버레이를 올린다.
            Transform parent = null;
            if (mainCanvas != null) parent = mainCanvas.transform;
            if (parent == null)
            {
                var ba = GameObject.Find("BattleArea");
                if (ba != null) parent = ba.transform;
            }
            if (parent == null)
            {
                GameLogger.Warn("[GM] 캔버스/BattleArea 없음, 업그레이드 오버레이 스킵");
                return;
            }

            upgradeUI = UpgradeUI.Create(parent, player, OnGoldUpgrade, OnRubyUpgrade, () =>
            {
                // ⭐ 강화 즉시 반영: 소환 버튼 비용 + 배치된 전투 정령 스탯 재계산
                UpdateSummonButtonText();
                DeploySpiritsToBattle();
            }, () => OnTabClicked(0)); // ⭐ 닫기(X) → 전투 탭으로 복귀

            // ⭐ GNB(BottomMenu)가 오버레이보다 위에 항상 보이도록 배치 —
            //    오버레이가 전체 화면을 덮어도 하단 탭으로 다른 화면 전환 가능
            var bm = GameObject.Find("BottomMenu");
            if (bm != null)
                upgradeUI.transform.SetSiblingIndex(bm.transform.GetSiblingIndex());

            GameLogger.Info("[GM] 업그레이드 오버레이 생성 완료 (Canvas 최상위 — 머지+배틀 전체)");

            StartCoroutine(AutoSummonLoop());
            GameLogger.Info("[GM] 자동 소환 루프 시작");
        }

        /// <summary>
        /// 자동 소환 루프 — 스킬트리[9] 레벨이 0이면 비활성, 켜지면 주기마다 무료 소환
        /// </summary>
        IEnumerator AutoSummonLoop()
        {
            while (true)
            {
                float interval = player.GetAutoSummonInterval();
                if (interval > 0)
                {
                    yield return new WaitForSeconds(interval);
                    if (board != null)
                    {
                        bool ok = SummonSpirit(board, true);
                        GameLogger.Info($"[GM] 자동 소환{(ok ? " 성공" : " 실패(슬롯 없음)")} (주기 {interval:0}s)");
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        /// <summary>
        /// 경험치 획득 (몬스터 처치) — 경험치 획득량 증가 보너스 적용 후 레벨업/SP 반영
        /// </summary>
        public void AddPlayerExp(int amount)
        {
            if (player == null) return;
            int gained = Mathf.RoundToInt(amount * (1 + player.GetExpBonusPct()));
            int before = player.Level;
            player.AddExp(gained);
            GameLogger.Info($"[GM] 경험치 +{gained} (보너스 +{(int)(player.GetExpBonusPct() * 100)}%)");
            if (player.Level > before)
            {
                GameLogger.Info($"[GM] ⭐ 레벨업! Lv.{player.Level} → SP {player.SkillPoints}");
                OnLevelUp(); // ⭐ 미션 진행도 (레벨업)
                if (upgradeUI != null) upgradeUI.Refresh();
            }
            SaveNow(); // ⭐ 경험치/레벨 변동 시 저장
        }

        /// <summary>
        /// 전투 골드 획득 — 골드 획득량 증가 보너스 적용
        /// </summary>
        public void AddBattleGold(int amount)
        {
            if (player == null) { AddGold(amount); return; }
            int gained = Mathf.RoundToInt(amount * (1 + player.GetGoldBonusPct()));
            AddGold(gained);
            GameLogger.Info($"[GM] 전투 골드 +{gained} (보너스 +{(int)(player.GetGoldBonusPct() * 100)}%)");
        }

        /// <summary>
        /// 현재 진행 중인 스테이지 (정렬 배열 기준) — RaidBattle 전투 재개용으로 public
        /// </summary>
        public StageData GetCurrentStage()
        {
            var sorted = GetSortedStages();
            if (sorted.Length == 0) return null;
            return sorted[Mathf.Clamp(currentStageIndex, 0, sorted.Length - 1)];
        }

        /// <summary>
        /// 정령 소환 — 생성비용 감소 + 2성 확률 적용
        /// free=true면 골드 소모 없음 (자동 소환)
        /// </summary>
        bool SummonSpirit(MergeBoardManager board, bool free)
        {
            if (board == null) return false;

            var stage = GetCurrentStage();
            int baseCost = stage != null ? stage.summonCost : board.summonCost;
            int cost = free ? 0 : Mathf.Max(0, Mathf.RoundToInt(baseCost * (1 - player.GetSummonCostDiscount())));

            if (!free && gold < cost)
            {
                GameLogger.Warn($"[GM] 골드 부족: {gold}/{cost}");
                return false;
            }

            // ⭐ 성급 확률 (스킬트리): 3성 확률 → 2성 확률 → 1성 순서로 판정
            //    3성/2성 확률은 각각 최대 25% (2성 만렙 시 3성 해금)
            var all = Resources.LoadAll<SpiritData>("Data/Spirits");
            float roll = Random.value;
            float three = player.GetThreeStarChance();
            float two = player.GetTwoStarChance();
            SpiritGrade target;
            if (roll < three) target = SpiritGrade.ThreeStar;
            else if (roll < three + two) target = SpiritGrade.TwoStar;
            else target = SpiritGrade.OneStar;

            var pool = new List<SpiritData>();
            foreach (var s in all)
                if (s != null && s.grade == target) pool.Add(s);

            if (pool.Count == 0)
            {
                // 폴백: 1성 풀
                foreach (var s in all)
                    if (s != null && s.grade == SpiritGrade.OneStar) pool.Add(s);
                if (pool.Count == 0) { GameLogger.Error("[GM] 소환 풀 없음!"); return false; }
                target = SpiritGrade.OneStar;
            }

            var sd = pool[Random.Range(0, pool.Count)];
            GameLogger.Info($"[GM] 소환 선택: {sd.name} ({target}성 풀 {pool.Count}종, 3성확률 {three:0%}/2성확률 {two:0%})");

            if (board.TrySummon(sd) >= 0)
            {
                if (!free) { gold -= cost; UpdateGoldDisplay(); }
                GameLogger.Info($"[GM] 소환 성공: {sd.name} (비용: {(free ? "무료" : cost + "G")})");
                UpdateSummonButtonText();
                OnSpiritSummoned(); // ⭐ 미션 진행도 (소환)
                return true;
            }
            GameLogger.Warn("[GM] 소환 실패: 빈 슬롯 없음");
            return false;
        }

        /// <summary>
        /// 소환 버튼 텍스트 — 현재 스테이지/챕터 비용(생성비용 감소 반영) 표시
        /// </summary>
        void UpdateSummonButtonText()
        {
            var stage = GetCurrentStage();
            if (stage == null) return;
            int cost = Mathf.Max(0, Mathf.RoundToInt(stage.summonCost * (1 - player.GetSummonCostDiscount())));
            var btn = GameObject.Find("SummonBtn");
            if (btn == null) return;
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = $"소환 {cost}G";
        }

        /// <summary>
        /// 골드 업그레이드 구매 (UpgradeUI 콜백) — 골드 차감 + 노드 강화. 성공 시 true
        /// </summary>
        bool OnGoldUpgrade(int idx)
        {
            if (player == null || player.IsLocked(idx))
            {
                GameLogger.Warn("[GM] 잠금 상태 — 강화 불가");
                return false;
            }
            if (player.GetUpgradeLevel(idx) >= PlayerService.MaxLevelFor(idx))
            {
                GameLogger.Warn("[GM] 최대 레벨 — 강화 불가");
                return false;
            }
            int cost = player.GetGoldCost(idx);
            if (gold < cost)
            {
                GameLogger.Warn($"[GM] 골드 부족: {gold}/{cost}");
                return false;
            }
            gold -= cost;
            UpdateGoldDisplay();
            player.ApplyGoldUpgrade(idx);
            GameLogger.Info($"[GM] 골드 업그레이드: {PlayerService.UpgradeNames[idx]} Lv.{player.GetUpgradeLevel(idx)} (-{cost}G)");
            OnUpgraded(); // ⭐ 미션 진행도 (업그레이드)
            return true;
        }

        /// <summary>
        /// 골드 업그레이드 시도 (CLI/외부 검증용)
        /// </summary>
        public bool TryGoldUpgrade(int idx) => OnGoldUpgrade(idx);

        /// <summary>
        /// 루비 업그레이드 구매 (UpgradeUI 콜백) — 루비 차감 + 노드 강화. 성공 시 true
        /// </summary>
        bool OnRubyUpgrade(int idx)
        {
            if (player == null || player.IsLocked(idx))
            {
                GameLogger.Warn("[GM] 잠금 상태 — 루비 강화 불가");
                return false;
            }
            if (player.GetUpgradeLevel(idx) >= PlayerService.MaxLevelFor(idx))
            {
                GameLogger.Warn("[GM] 최대 레벨 — 루비 강화 불가");
                return false;
            }
            int cost = player.GetRubyCost(idx);
            if (ruby < cost)
            {
                GameLogger.Warn($"[GM] 루비 부족: {ruby}/{cost}");
                return false;
            }
            ruby -= cost;
            UpdateRubyDisplay();
            player.ApplyRubyUpgrade(idx);
            GameLogger.Info($"[GM] 루비 업그레이드: {PlayerService.UpgradeNames[idx]} Lv.{player.GetUpgradeLevel(idx)} (-{cost}R)");
            OnUpgraded(); // ⭐ 미션 진행도 (업그레이드)
            return true;
        }

        /// <summary>
        /// 루비 업그레이드 시도 (CLI/외부 검증용)
        /// </summary>
        public bool TryRubyUpgrade(int idx) => OnRubyUpgrade(idx);

        /// <summary>
        /// 머지 보너스 — 머지 성공 시 추가 골드/경험치 (머지 보너스 업그레이드 적용)
        /// </summary>
        public void AddMergeReward()
        {
            if (player == null) return;
            float bonus = player.GetMergeBonusPct();
            if (bonus <= 0f) return;
            int goldReward = Mathf.RoundToInt(10f * (1f + bonus));
            int expReward = Mathf.RoundToInt(5f * (1f + bonus));
            AddBattleGold(goldReward);
            AddPlayerExp(expReward);
            GameLogger.Info($"[GM] 머지 보너스: 골드 +{goldReward}, 경험치 +{expReward} (+{bonus:0%})");
        }

        /// <summary>
        /// 파티 구성/전투력 변경 시: 오버레이 갱신 + 전투 재배치/재시작
        /// </summary>
        void OnPartyChangedHandler()
        {
            if (formationUI != null) formationUI.Refresh();
            RestartBattleForParty();
        }

        /// <summary>
        /// 파티 변경 반영: 정령 재배치 후 웨이브 1부터 재시작
        /// 빈 파티면 정령 0기 → 전투 중지 상태 (안내 배너는 오버레이에서 표시)
        /// </summary>
        void RestartBattleForParty()
        {
            if (battleManager == null) return;
            DeploySpiritsToBattle();

            if (waveCtrl == null)
            {
                waveCtrl = FindAnyObjectByType<WaveController>();
                if (waveCtrl == null) return;
            }
            var allStages = GetSortedStages();
            if (allStages.Length == 0) return;
            var stage = allStages[Mathf.Clamp(currentStageIndex, 0, allStages.Length - 1)];
            UpdateStageInfo(stage); // ⭐ TopBar 스테이지 표시 갱신

            waveCtrl.StartBattle(stage);
            GameLogger.Info("[GM] 🔄 파티 변경 → 웨이브 1부터 재시작");
        }

        /// <summary>
        /// 기본 정령 지급 (MergeBoard가 비어있을 때만)
        /// </summary>
        void GiveStarterSpirit()
        {
            if (board == null) return;
            if (board.GetActiveSpiritData().Length > 0) return; // 이미 정령 있음

            var allSpirits = Resources.LoadAll<SpiritData>("Data/Spirits");
            var fire = System.Array.Find(allSpirits, s => s.element == ElementType.Fire);
            if (fire != null)
            {
                int slotIdx = board.TrySummon(fire);
                if (slotIdx >= 0)
                {
                    GameLogger.Info($"[GM] ⭐ 기본 정령 지급: {fire.spiritName}");
                    // 시작 구성: 불 Lv.1 한 마리 → 파티 P1 자동 배치
                    board.AutoAssignFirstToParty(slotIdx);
                    GameLogger.Info("[GM] ⭐ 시작 정령 파티 P1 자동 배치 완료");
                }
            }
        }

        /// <summary>
        /// 파티에 편성된 정령 → SpiritGroup/SpiritSlot_0..N 슬롯에 배치 (슬롯 자체를 유닛으로 사용)
        /// 클론 생성 없음 — 빈 파티면 0기 → 전투 중지 상태
        /// ⭐ 시너지 계산 + 파티 통합 HP 설정 (개별 HP 대신 파티 전체 체력)
        /// </summary>
        void DeploySpiritsToBattle()
        {
            // ⭐ 도메인 리로드 후 null 방어
            if (deployedSpirits == null) deployedSpirits = new List<GameObject>();

            // 기존 배치된 슬롯 모두 비활성화 (재배치 시 중복 방지)
            foreach (var sp in deployedSpirits)
                if (sp != null) sp.SetActive(false);
            deployedSpirits.Clear();

            // ⭐ 레벨 포함 데이터로 배치 (레벨별 스프라이트 해석용)
            var partyItems = board != null ? board.GetPartyItems() : new SpiritItemData[0];

            // ⭐ 속성 시너지 계산 (파티 구성 기반)
            CalcSynergy(partyItems);

            // SpiritGroup/SpiritSlot_0..N UI 슬롯 기반 배치
            var slotRects = FindSpiritSlots();
            if (slotRects.Count == 0)
            {
                GameLogger.Error("[GM] SpiritGroup/SpiritSlot 없음 → 정령 배치 불가!");
                return;
            }

            // ⭐ 사용하지 않는 슬롯은 모두 비활성화 (빈 슬롯 표시 방지)
            foreach (var rect in slotRects)
                rect.gameObject.SetActive(false);

            if (partyItems.Length == 0)
            {
                GameLogger.Info("[GM] 파티가 비어 있음 → 전투 정령 없음 (전투 중지)");
                return;
            }

            int slotCount = Mathf.Min(partyItems.Length, slotRects.Count);
            for (int i = 0; i < slotCount; i++)
            {
                DeploySpiritToSlot(slotRects[i].gameObject, partyItems[i].spiritData, partyItems[i].level);
            }
            GameLogger.Info($"[GM] 전투 정령 {deployedSpirits.Count}기 배치 완료 (SpiritGroup 슬롯 {slotCount}개)");

            // ⭐ 파티 통합 HP 설정 (개별 HP 합 × 대지 시너지 + 업그레이드, 방어는 빛 시너지)
            int totalHP = 0, totalDEF = 0;
            foreach (var sp in deployedSpirits)
            {
                var unit = sp != null ? sp.GetComponent<SpiritUnit>() : null;
                if (unit != null) { totalHP += unit.maxHp; totalDEF += unit.def; }
            }
            totalHP = Mathf.RoundToInt(totalHP * (1f + EarthSynergy(synergyEarthCount)));
            totalDEF = Mathf.RoundToInt(totalDEF * (1f + synergyDefPct));
            if (battleManager != null)
            {
                battleManager.SetupParty(totalHP, totalDEF);
                GameLogger.Info($"[GM] 파티 통합 HP: {totalHP} (대지 시너지 +{EarthSynergy(synergyEarthCount):0%}), 방어 {totalDEF}");
            }
        }

        /// <summary>
        /// SpiritSlot에 정령 배치 (슬롯 자체에 SpiritUnit 얹어 재사용)
        /// </summary>
        void DeploySpiritToSlot(GameObject slotGo, SpiritData data, int level)
        {
            slotGo.SetActive(true);

            var unit = slotGo.GetComponent<SpiritUnit>();
            if (unit == null) unit = slotGo.AddComponent<SpiritUnit>();
            unit.slotMode = true;
            unit.displayImage = SlotUiHelper.FindDisplayImage(slotGo.transform);
            unit.hpSlider = null; // ⭐ 통합 HP로 전환 — 개별 HP바 미사용
            unit.cdSlider = SlotUiHelper.FindCdSlider(slotGo.transform);
            unit.Initialize(data, level);

            // ⭐ 통합 HP로 전환: 개별 HPBar 비활성화 (정령은 개별로 죽지 않음, 파티 HP만 깎임)
            var hpBar = slotGo.transform.Find("HPBar");
            if (hpBar != null) hpBar.gameObject.SetActive(false);

            // ⭐ 업그레이드 스탯 보너스 적용 (공/방/체 flat + % / 공격속도 %)
            ApplyUpgradeBonus(unit);

            deployedSpirits.Add(slotGo);
        }

        /// <summary>
        /// 플레이어 업그레이드 스탯 보너스를 전투 정령에 반영
        /// flat(+N) 먼저, % 보너스는 그 다음 곱연산
        /// </summary>
        void ApplyUpgradeBonus(SpiritUnit unit)
        {
            if (player == null || unit == null) return;
            unit.atk += player.GetFlatATK();
            unit.atk = Mathf.RoundToInt(unit.atk * (1 + player.GetATKBonusPct()));
            unit.def += player.GetFlatDEF();
            unit.def = Mathf.RoundToInt(unit.def * (1 + player.GetDEFBonusPct()));
            unit.maxHp += player.GetFlatHP();
            unit.maxHp = Mathf.RoundToInt(unit.maxHp * (1 + player.GetHPBonusPct()));
            unit.hp = unit.maxHp;
            unit.atkSpeed *= (1 + player.GetSPDBonusPct());
            unit.critRate += player.GetCritRateBonus();
            unit.critRate = Mathf.Min(1f, unit.critRate);
            unit.critDmg += player.GetCritDmgBonus();

            // ⭐ 속성 시너지 반영 (불 공격 / 물 속도 / 바람 치명타 / 어둠 공격)
            unit.atk = Mathf.RoundToInt(unit.atk * (1f + synergyAtkPct + synergyAtkFlatPct));
            unit.atkSpeed *= (1f + synergySpdPct);
            unit.critRate += synergyCritPct;
            unit.critRate = Mathf.Min(1f, unit.critRate);

            if (unit.hpSlider != null) unit.hpSlider.value = 1f;
        }

        // ── 속성 조합 시너지 ──

        /// <summary>같은 속성 슬롯 수에 따른 시너지 보너스</summary>
        static float SynergyByCount(int c, float b1, float b2, float b3, float b4)
            => c >= 4 ? b4 : c == 3 ? b3 : c == 2 ? b2 : c == 1 ? b1 : 0f;

        /// <summary>대지 시너지 (통합 HP 보너스) — ⭐ 2기부터 발동</summary>
        static float EarthSynergy(int c) => c >= 4 ? 0.7f : c == 3 ? 0.5f : c == 2 ? 0.3f : 0f;

        /// <summary>
        /// 파티 속성 시너지 계산 (1슬롯 기준)
        /// - 같은 속성 슬롯 수: 1기 기본 / 2기 / 3기 / 4기
        /// - 빛·어둠 1슬롯 → 가장 많은 4속성(불·물·대지·바람) +1 (동률 우선순위 불>물>대지>바람)
        /// - 빛: 파티 방어+10% + 웨이브 회복 5% / 어둠: 파티 공격+10% + 흡혈 5%
        /// </summary>
        void CalcSynergy(SpiritItemData[] items)
        {
            int fire = 0, water = 0, wind = 0, earth = 0, dark = 0, light = 0;
            foreach (var it in items)
            {
                if (it == null) continue;
                switch (it.element)
                {
                    case ElementType.Fire: fire++; break;
                    case ElementType.Water: water++; break;
                    case ElementType.Wind: wind++; break;
                    case ElementType.Earth: earth++; break;
                    case ElementType.Dark: dark++; break;
                    case ElementType.Light: light++; break;
                }
            }

            // ⭐ 빛/어둠 1슬롯 → 가장 많은 4속성 +1 (동률 우선순위: 불>물>대지>바람)
            if (light > 0 || dark > 0)
            {
                if (fire >= water && fire >= earth && fire >= wind) fire++;
                else if (water >= earth && water >= wind) water++;
                else if (earth >= wind) earth++;
                else wind++;
            }

            synergyAtkPct = SynergyByCount(fire, 0f, 0.20f, 0.35f, 0.50f);   // ⭐ 2기부터
            synergySpdPct = SynergyByCount(water, 0f, 0.15f, 0.25f, 0.35f);
            synergyCritPct = SynergyByCount(wind, 0f, 0.15f, 0.25f, 0.35f);
            synergyEarthCount = earth;
            synergyVampPct = dark > 0 ? 0.05f : 0f;
            synergyHealPct = light > 0 ? 0.05f : 0f;
            synergyDefPct = light > 0 ? 0.10f : 0f;
            synergyAtkFlatPct = dark > 0 ? 0.10f : 0f;
            synergyFireCount = fire;
            synergyWaterCount = water;
            synergyWindCount = wind;
            synergyDarkCount = dark;
            synergyLightCount = light;

            GameLogger.Info($"[GM] 시너지: 불{fire} 물{water} 대지{earth} 바람{wind} " +
                $"공+{synergyAtkPct:0%} 속+{synergySpdPct:0%} 치명+{synergyCritPct:0%} 대지HP+{EarthSynergy(synergyEarthCount):0%} " +
                $"{(light > 0 ? $"빛: 방어+{synergyDefPct:0%} 회복+{synergyHealPct:0%}" : "")}" +
                $"{(dark > 0 ? $"어둠: 공격+{synergyAtkFlatPct:0%} 흡혈+{synergyVampPct:0%}" : "")}");
        }

        /// <summary>빛 시너지 — 웨이브 클리어 시 파티 HP 회복</summary>
        public void HealPartyBySynergy()
        {
            if (battleManager == null || synergyHealPct <= 0f) return;
            int amount = Mathf.RoundToInt(battleManager.partyMaxHP * synergyHealPct);
            battleManager.HealParty(amount);
            if (amount > 0) GameLogger.Info($"[GM] ✨ 빛 회복: 파티 HP +{amount} ({synergyHealPct:0%})");
        }

        /// <summary>어둠 시너지 — 아군 공격 데미지의 %만큼 파티 HP 회복 (흡혈)</summary>
        public void HealPartyByVamp(int damage)
        {
            if (battleManager == null || synergyVampPct <= 0f) return;
            int amount = Mathf.RoundToInt(damage * synergyVampPct);
            if (amount > 0) battleManager.HealParty(amount);
        }

        /// <summary>
        /// 현재 파티 시너지 요약/상세 (파티 편성 UI 표시용)
        /// </summary>
        public (string summary, string detail) GetSynergySummary()
        {
            var lines = new System.Collections.Generic.List<string>();
            var details = new System.Collections.Generic.List<string>();

            // ⭐ 간략: 시너지 속성 이름만 표시 (효과 설명은 툴팁에서)
            if (synergyFireCount >= 2) lines.Add(RepeatName("불", synergyFireCount));
            if (synergyWaterCount >= 2) lines.Add(RepeatName("물", synergyWaterCount));
            if (synergyEarthCount >= 2) lines.Add(RepeatName("대지", synergyEarthCount));
            if (synergyWindCount >= 2) lines.Add(RepeatName("바람", synergyWindCount));
            if (synergyDarkCount > 0) lines.Add("어둠");
            if (synergyLightCount > 0) lines.Add("빛");

            if (lines.Count == 0)
            {
                lines.Add("시너지 없음");
                details.Add("같은 속성 정령 2기 이상 편성하면 시너지가 발동됩니다.");
            }

            return (string.Join("  ", lines), string.Join("\n", details));
        }

        /// <summary>속성 이름을 수만큼 반복 ("불 불")</summary>
        static string RepeatName(string name, int count)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++) { if (i > 0) sb.Append(" "); sb.Append(name); }
            return sb.ToString();
        }

        /// <summary>
        /// 활성 시너지 항목 목록 — ToolTip 속성 블록 표시용 (속성, 수, 보너스 설명)
        /// </summary>
        public System.Collections.Generic.List<(ElementType element, int count, string text)> GetSynergyItems()
        {
            var items = new System.Collections.Generic.List<(ElementType, int, string)>();
            if (synergyFireCount >= 2)
                items.Add((ElementType.Fire, synergyFireCount, $"공격력 +{(int)(synergyAtkPct * 100)}%"));
            if (synergyWaterCount >= 2)
                items.Add((ElementType.Water, synergyWaterCount, $"공격속도 +{(int)(synergySpdPct * 100)}%"));
            if (synergyEarthCount >= 2)
                items.Add((ElementType.Earth, synergyEarthCount, $"파티 HP +{(int)(EarthSynergy(synergyEarthCount) * 100)}%"));
            if (synergyWindCount >= 2)
                items.Add((ElementType.Wind, synergyWindCount, $"치명타 +{(int)(synergyCritPct * 100)}%"));
            if (synergyDarkCount > 0)
                items.Add((ElementType.Dark, synergyDarkCount, "흡혈5% 공격+10%"));
            if (synergyLightCount > 0)
                items.Add((ElementType.Light, synergyLightCount, "재생5% 방어력+10%"));
            return items;
        }

        /// <summary>
        /// SpiritGroup 하위 SpiritSlot_0..N RectTransform 목록 조회 (중간에 없으면 중단)
        /// </summary>
        List<RectTransform> FindSpiritSlots()
        {
            var result = new List<RectTransform>();
            var spiritGroup = GameObject.Find("SpiritGroup");
            if (spiritGroup == null) return result;

            for (int i = 0; i < 4; i++)
            {
                var slotGo = spiritGroup.transform.Find("SpiritSlot_" + i);
                if (slotGo == null) break;
                var rect = slotGo.GetComponent<RectTransform>();
                if (rect != null) result.Add(rect);
            }
            return result;
        }

        // ── Gold/Ruby ──
        public bool SpendGold(int amount)
        {
            if (gold < amount) { GameLogger.Warn($"[GM] 골드 부족: {gold}/{amount}"); return false; }
            gold -= amount;
            UpdateGoldDisplay();
            GameLogger.Info($"[GM] 골드 사용: -{amount} (잔액: {gold})");
            SaveNow(); // ⭐ 재화 소모 시 저장
            return true;
        }

        public void AddGold(int amount)
        {
            gold += amount;
            UpdateGoldDisplay();
            if (amount > 0) OnGoldEarned(amount); // ⭐ 미션 진행도 (골드 획득)
            SaveNow(); // ⭐ 재화 획득 시 저장
        }

        public void AddRuby(int amount)
        {
            ruby += amount;
            UpdateRubyDisplay();
            SaveNow(); // ⭐ 재화 획득 시 저장
        }

        public bool SpendRuby(int amount)
        {
            if (ruby < amount) { GameLogger.Warn($"[GM] 루비 부족: {ruby}/{amount}"); return false; }
            ruby -= amount;
            UpdateRubyDisplay();
            GameLogger.Info($"[GM] 루비 사용: -{amount} (잔액: {ruby})");
            SaveNow(); // ⭐ 재화 소모 시 저장
            return true;
        }

        void UpdateGoldDisplay()
        {
            var gt = GameObject.Find("GoldText")?.GetComponent<TextMeshProUGUI>();
            if (gt != null) gt.text = $"{gold} Gold";
        }

        void UpdateRubyDisplay()
        {
            var rt = GameObject.Find("RubyText")?.GetComponent<TextMeshProUGUI>();
            if (rt != null) rt.text = $"{ruby} Ruby";
        }

        void SetupBattleSystem()
        {
            var ba = GameObject.Find("BattleArea");
            if (ba == null) { GameLogger.Warn("[GM] BattleArea 없음, 전투 시스템 스킵"); return; }

            battleManager = ba.GetComponent<BattleManager>();
            if (battleManager == null) battleManager = ba.AddComponent<BattleManager>();
            // ⭐ SpiritSpawnRoot/EnemySpawnRoot 하드코딩 루트 제거:
            // 정령은 SpiritGroup/SpiritSlot, 몬스터는 EnemyGroup/EnemySlot UI 슬롯 기반으로 배치한다.

            var spawner = ba.GetComponent<MonsterSpawner>();
            if (spawner == null) ba.AddComponent<MonsterSpawner>();
            var wc = ba.GetComponent<WaveController>();
            if (wc == null) ba.AddComponent<WaveController>();

            SetupPartyHpBar(); // ⭐ 파티 통합 HP바 UI

            GameLogger.Info("[GM] 전투 시스템 준비 완료");
        }

        /// <summary>
        /// 파티 통합 HP바 + HP 텍스트 생성 (Battle Area 상단)
        /// 정령 개별 HP바 대신 파티 전체 체력을 표시
        /// </summary>
        void SetupPartyHpBar()
        {
            if (battleManager == null) return;
            var ba = GameObject.Find("BattleArea");
            if (ba == null) return;

            // 배경이 이미 있으면 스킵 (재호출 방지)
            if (ba.transform.Find("PartyHPBar") != null) return;

            var barGo = new GameObject("PartyHPBar", typeof(RectTransform), typeof(UnityEngine.UI.Slider));
            barGo.transform.SetParent(ba.transform, false);
            var rt = barGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.03f);  // ⭐ 하단 배치
            rt.anchorMax = new Vector2(0.85f, 0.08f);  // 두껍게
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var slider = barGo.GetComponent<UnityEngine.UI.Slider>();
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(barGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.2f, 0.1f, 0.3f, 0.9f);
            slider.targetGraphic = bgImg;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barGo.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(2, 2); faRt.offsetMax = new Vector2(-2, -2);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.color = new Color(0.3f, 0.9f, 0.5f, 1f);
            slider.fillRect = fillRt;

            battleManager.partyHpSlider = slider;

            var txtGo = new GameObject("PartyHPText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(ba.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.15f, 0.03f);  // ⭐ 하단 배치
            txtRt.anchorMax = new Vector2(0.85f, 0.08f);
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 18;   // ⭐ 폰트 30% 확대 (14→18)
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font != null) tmp.font = font;
            battleManager.partyHpText = tmp;

            battleManager.UpdatePartyBar();
            GameLogger.Info("[GM] 파티 통합 HP바 생성 완료");
        }

        // ── 머지 시스템 ──
        void SetupMergeSystem()
        {
            var ma = GameObject.Find("MergeArea");
            if (ma == null) { GameLogger.Error("[GM] MergeArea 없음!"); return; }

            board = ma.GetComponent<MergeBoardManager>();
            if (board == null) board = ma.AddComponent<MergeBoardManager>();

            // MergeBoard Image raycastTarget 끄기 (클릭 블로킹 방지)
            var mergeBoard = ma.transform.Find("MergeBoard");
            if (mergeBoard != null)
            {
                var boardImg = mergeBoard.GetComponent<Image>();
                if (boardImg != null) boardImg.raycastTarget = false;
            }
            GameLogger.Info("[GM] 머지 시스템 준비 완료");

            var sb = ma.transform.Find("SummonBtn");
            if (sb != null)
            {
                var b = sb.GetComponent<Button>();
                if (b == null) b = sb.gameObject.AddComponent<Button>();

                var sbImg = sb.GetComponent<Image>();
                if (sbImg != null) { sbImg.raycastTarget = true; b.targetGraphic = sbImg; }
                sb.SetAsLastSibling();

                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => OnSummonClicked(board));
                GameLogger.Info("[GM] SummonBtn 연결 완료");
            }
            else GameLogger.Warn("[GM] SummonBtn 없음!");
        }

        void OnSummonClicked(MergeBoardManager board)
        {
            GameLogger.Info("[GM] 소환 버튼 클릭!");
            SummonSpirit(board, false);
        }

        // ── GNB ──
        static readonly string[] TabNames = { "전투", "파티", "업그레이드", "도감", "의뢰" };

        void SetupGNBTabs()
        {
            var bm = GameObject.Find("BottomMenu");
            if (bm == null) { GameLogger.Warn("[GM] BottomMenu 없음"); return; }
            for (int i = 0; i < TabNames.Length; i++)
            {
                // 실제 씬 탭 이름 (Tab_전투 등) 우선, 구형 인덱스 이름(Tab_0) 폴백
                Transform t = bm.transform.Find($"Tab_{TabNames[i]}");
                if (t == null) t = bm.transform.Find($"Tab_{i}");
                if (t == null) continue;

                // ⭐ Label(아이콘과 중복 텍스트) 제거 + Icon(이름)을 탭 전체에 가운데 정렬
                var label = t.Find("Label");
                if (label != null) label.gameObject.SetActive(false);
                var icon = t.Find("Icon");
                if (icon != null)
                {
                    var irt = icon.GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0, 0);
                    irt.anchorMax = new Vector2(1, 1);
                    irt.offsetMin = Vector2.zero;
                    irt.offsetMax = Vector2.zero;
                }

                var b = t.GetComponent<Button>();
                if (b == null) b = t.gameObject.AddComponent<Button>();
                int idx = i;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => OnTabClicked(idx));
            }
            GameLogger.Info("[GM] GNB 탭 연결 완료");
        }

        void OnTabClicked(int idx)
        {
            string n = idx < TabNames.Length ? TabNames[idx] : $"Tab_{idx}";
            GameLogger.Info($"[GM] GNB 탭 클릭: {n}");
            board?.DeselectCurrent(); // 탭 전환 시 머지 보드 선택 해제

            // 🛡️ 파티 탭(1) → 편성 오버레이, 업그레이드 탭(2) → 업그레이드 오버레이,
            //    도감 탭(3) → 도감, 의뢰 탭(4) → 의뢰, 나머지 → 모두 숨김
            bool party = idx == 1;
            bool upgrade = idx == 2;
            bool dex = idx == 3;
            bool request = idx == 4;

            if (formationUI != null)
                formationUI.gameObject.SetActive(party);
            if (upgradeUI != null)
            {
                upgradeUI.gameObject.SetActive(upgrade);
                if (upgrade) upgradeUI.Refresh();
            }
            if (dexUI != null)
                dexUI.gameObject.SetActive(dex);
            if (requestUI != null)
            {
                requestUI.gameObject.SetActive(request);
                if (request) requestUI.Refresh();
            }
            // ⭐ 의뢰 탭이 아니면 파견 배치 오버레이 닫기 (파견 UI가 다른 탭에 남는 문제)
            if (dispatchFormation != null && !request)
                dispatchFormation.gameObject.SetActive(false);
            if (!party && !upgrade && !dex && !request)
                board?.SetFormationMode(false);

            UpdateTabHighlight(idx);
        }

        /// <summary>
        /// 파티 편성 탭 표시 (CLI/외부에서 호출 — 검증용)
        /// </summary>
        public void ShowPartyTab() => OnTabClicked(1);

        /// <summary>
        /// 전투 탭 표시 (의뢰 닫기 등에서 호출)
        /// </summary>
        public void ShowBattleTab() => OnTabClicked(0);

        /// <summary>
        /// 의뢰 탭 표시 (CLI/외부에서 호출 — 검증용)
        /// </summary>
        public void ShowRequestTab() => OnTabClicked(4);

        /// <summary>
        /// 업그레이드 탭 표시 (CLI/외부에서 호출 — 검증용)
        /// </summary>
        public void ShowUpgradeTab() => OnTabClicked(2);

        /// <summary>
        /// 도감 탭 표시 (CLI/외부에서 호출 — 검증용)
        /// </summary>
        public void ShowDexTab() => OnTabClicked(3);

        /// <summary>현재 스테이지 인덱스 (CLI 검증용)</summary>
        public int CurrentStageIndex => currentStageIndex;

        /// <summary>현재 스테이지 이름 (CLI 검증용)</summary>
        public string CurrentStageName => GetCurrentStage()?.stageName ?? "?";

        /// <summary>
        /// 배치된 전투 정령 스탯 재계산 (업그레이드 강화 즉시 반영 — 검증/재배치용)
        /// </summary>
        public void RefreshBattleSpirits() => DeploySpiritsToBattle();

        /// <summary>
        /// 소환 버튼 텍스트 즉시 갱신 (검증용)
        /// </summary>
        public void RefreshSummonButton() => UpdateSummonButtonText();

        /// <summary>
        /// GNB 탭 하이라이트 갱신 (배경/인디케이터/아이콘/라벨 색상)
        /// </summary>
        void UpdateTabHighlight(int activeIdx)
        {
            var bm = GameObject.Find("BottomMenu");
            if (bm == null) return;
            for (int i = 0; i < TabNames.Length; i++)
            {
                var tab = bm.transform.Find($"Tab_{TabNames[i]}");
                if (tab == null) tab = bm.transform.Find($"Tab_{i}");
                if (tab == null) continue;

                bool active = i == activeIdx;
                var img = tab.GetComponent<Image>();
                if (img != null) img.color = active ? new Color(0.08f, 0.10f, 0.20f) : new Color(0, 0, 0, 0);

                // ActiveIndicator (없으면 동적으로 생성)
                var ind = tab.Find("ActiveIndicator");
                if (ind == null)
                {
                    var indGo = new GameObject("ActiveIndicator", typeof(RectTransform), typeof(Image));
                    indGo.transform.SetParent(tab, false);
                    var indRect = indGo.GetComponent<RectTransform>();
                    indRect.anchorMin = new Vector2(0.25f, 0.92f);
                    indRect.anchorMax = new Vector2(0.75f, 1.0f);
                    indRect.offsetMin = Vector2.zero;
                    indRect.offsetMax = Vector2.zero;
                    indGo.GetComponent<Image>().color = new Color(0.5f, 0.7f, 1f);
                    ind = indGo.transform;
                }
                ind.gameObject.SetActive(active);

                var icon = tab.Find("Icon")?.GetComponent<TextMeshProUGUI>();
                if (icon != null) icon.color = active ? new Color(0.5f, 0.7f, 1f) : new Color(0.4f, 0.5f, 0.7f, 0.4f);
                var label = tab.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (label != null) label.color = active ? new Color(0.5f, 0.7f, 1f) : new Color(0.4f, 0.5f, 0.7f, 0.4f);
            }
        }

        // ── TopBar ──
        void SetupTopBar()
        {
            UpdateGoldDisplay();
            UpdateRubyDisplay();
            SetupRepeatModeButton();

            // ⭐ TopBar 텍스트 세로 중앙 정렬 (텍스트 bounds 중심을 rect 중앙에 — 폰트 글리프 차이 무시)
            StartCoroutine(CenterTopBarTexts());

            var gt = GameObject.Find("GoldText");
            if (gt != null)
            {
                var b = gt.GetComponent<Button>();
                if (b == null) b = gt.AddComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => { gold += 100; UpdateGoldDisplay(); GameLogger.Info($"[GM] 골드 +100 (테스트, 잔액: {gold})"); });
            }

            var rt = GameObject.Find("RubyText");
            if (rt != null)
            {
                var b = rt.GetComponent<Button>();
                if (b == null) b = rt.AddComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => { ruby += 10; UpdateRubyDisplay(); GameLogger.Info($"[GM] 루비 +10 (테스트, 잔액: {ruby})"); });
            }
        }

        /// <summary>
        /// ⭐ TopBar 텍스트 세로 중앙 정렬 — 텍스트가 렌더된 뒤(1프레임 후) 각 텍스트의
        /// textBounds 중심이 rect 중앙이 되도록 anchoredPosition.y 보정 (폰트 글리프 차이로 어긋남 방지)
        /// </summary>
        System.Collections.IEnumerator CenterTopBarTexts()
        {
            yield return null;
            Canvas.ForceUpdateCanvases(); // ⭐ 텍스트 메시 강제 갱신 (textBounds 준비)
            yield return null;
            CenterOne("StageInfo");
            CenterOne("GoldText");
            CenterOne("RubyText");
        }

        void CenterOne(string name)
        {
            var tmp = GameObject.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;
            var rt = tmp.GetComponent<RectTransform>();
            var b = tmp.textBounds;
            // textBounds.center.y가 rect 중심(0) 기준 텍스트 중심 오프셋 → 반대로 보정
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -b.center.y);
        }

        /// <summary>반복 모드 토글 버튼 (TopBar 중앙, 정사각형)</summary>
        void SetupRepeatModeButton()
        {
            var topBar = GameObject.Find("TopBar");
            if (topBar == null)
            {
                GameLogger.Warn("[GM] TopBar 없음 — 반복 모드 버튼 생성 스킵");
                return;
            }
            if (topBar.transform.Find("RepeatModeBtn") != null) return;

            var btnGo = new GameObject("RepeatModeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(topBar.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(44, 44);
            rt.anchoredPosition = Vector2.zero;
            var img = btnGo.GetComponent<Image>();
            var btn = btnGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { repeatMode = !repeatMode; UpdateRepeatModeButton(); });

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(btnGo.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18;
            label.color = Color.white;
            label.raycastTarget = false;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font != null) label.font = font;

            UpdateRepeatModeButton();
        }

        /// <summary>반복 모드 버튼 상태 갱신 (색 + R/C 라벨)</summary>
        void UpdateRepeatModeButton()
        {
            var topBar = GameObject.Find("TopBar");
            if (topBar == null) return;
            var btn = topBar.transform.Find("RepeatModeBtn");
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            var label = btn.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (img != null)
                img.color = repeatMode ? new Color(0.25f, 0.45f, 0.9f, 1f) : new Color(0.2f, 0.6f, 0.3f, 1f);
            if (label != null)
                label.text = repeatMode ? "R" : "C";
            GameLogger.Info($"[GM] 모드: {(repeatMode ? "반복 (실패 시 이전 스테이지)" : "등반 (현재 스테이지 재도전)")}");
        }

        // ── 화면 전체 클릭 → MergeBoard 선택 해제 ──
        void SetupGlobalDeselect()
        {
            if (board == null) return;

            // 1. ScreenBackground (전체화면 배경) → 빈 공간 클릭 처리
            var bg = GameObject.Find("ScreenBackground");
            if (bg != null)
            {
                var bgImg = bg.GetComponent<Image>();
                if (bgImg != null) bgImg.raycastTarget = true;

                var bgBtn = bg.GetComponent<Button>();
                if (bgBtn == null) bgBtn = bg.AddComponent<Button>();
                bgBtn.transition = Selectable.Transition.None;
                bgBtn.onClick.AddListener(() =>
                {
                    // ⭐ 파견 배치 화면이 열려있으면 선택 유지 (파견 슬롯 클릭이 선택 취소로 이어지는 문제 방지)
                    if (dispatchFormation != null && dispatchFormation.gameObject.activeSelf) return;
                    board.DeselectCurrent();
                });
                GameLogger.Info("[GM] ScreenBackground → 빈 공간 클릭 감지");
            }

            // 2. GNB 각 탭에 DeselectCurrent 추가
            var bm = GameObject.Find("BottomMenu");
            if (bm != null)
            {
                for (int i = 0; i < TabNames.Length; i++)
                {
                    var tab = bm.transform.Find($"Tab_{TabNames[i]}");
                    if (tab == null) tab = bm.transform.Find($"Tab_{i}");
                    if (tab != null)
                    {
                        var tabBtn = tab.GetComponent<Button>();
                        if (tabBtn != null) tabBtn.onClick.AddListener(() => board.DeselectCurrent());
                    }
                }
            }

            // 3. TopBar GoldText/RubyText
            var gt = GameObject.Find("GoldText")?.GetComponent<Button>();
            if (gt != null) gt.onClick.AddListener(() => board.DeselectCurrent());
            var rt = GameObject.Find("RubyText")?.GetComponent<Button>();
            if (rt != null) rt.onClick.AddListener(() => board.DeselectCurrent());

            GameLogger.Info("[GM] 화면 전체 클릭 → MergeBoard 선택 해제 설정 완료");
        }

        // ══════════════════════════════════════════
        // 저장/불러오기 시스템 (모바일 자동 저장 — JSON + 백업)
        // ══════════════════════════════════════════

        DataManager GetDataManager()
        {
            if (dataManager == null)
                dataManager = FindAnyObjectByType<DataManager>();
            if (dataManager == null)
                dataManager = gameObject.AddComponent<DataManager>();
            return dataManager;
        }

        /// <summary>즉시 저장 — 현재 게임 상태를 JSON으로 기록 (원자적 쓰기 + 백업)</summary>
        public void SaveNow()
        {
            try
            {
                var dm = GetDataManager();
                if (dm != null) dm.SaveGame(BuildSaveData());
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[GM] 저장 실패: {e.Message}");
            }
        }

        /// <summary>현재 게임 상태 → SaveData (직렬화)</summary>
        public SaveData BuildSaveData()
        {
            var d = new SaveData();
            d.saveTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            d.gold = gold;
            d.ruby = ruby;
            d.playerLevel = player != null ? player.Level : 1;
            d.playerExp = player != null ? player.Exp : 0;
            d.skillPoints = player != null ? player.SkillPoints : 0;
            if (player != null) d.upgradeLevels = (int[])player.UpgradeLevels.Clone();
            d.stageIndex = currentStageIndex;
            d.repeatMode = repeatMode;

            // 보드 정령 16슬롯 + 파티 배치
            if (board != null)
            {
                for (int i = 0; i < 16; i++)
                {
                    var it = board.GetItemData(i);
                    if (it != null && it.spiritData != null)
                        d.boardSpirits.Add(new SavedSpirit { slotIndex = i, dataId = it.spiritData.name, level = it.level });
                }
                d.partySlots = board.GetPartySlots();
            }

            // 의뢰 (파견)
            foreach (var o in dispatch.offers) d.offers.Add(ToSavedRequest(o));
            foreach (var a in dispatch.Active) d.activeDispatches.Add(ToSavedDispatch(a));
            foreach (var c in dispatch.Completed) d.completedDispatches.Add(ToSavedDispatch(c));
            d.requestCooldownTimer = dispatch.requestCooldownTimer;
            d.totalDispatchCount = dispatch.TotalDispatchCount;

            // 미션
            d.dailyProgress = (int[])missions.DailyProgress.Clone();
            d.dailyClaimed = (bool[])missions.DailyClaimed.Clone();
            d.weeklyProgress = (int[])missions.WeeklyProgress.Clone();
            d.weeklyClaimed = (bool[])missions.WeeklyClaimed.Clone();

            // 레이드
            d.raidStage = raid.Stage;
            d.raidTotalDamage = raid.TotalDamage;
            d.raidBestScore = raid.BestScore;
            d.raidStageRewardClaimed = raid.StageRewardClaimed != null ? (bool[])raid.StageRewardClaimed.Clone() : null;
            d.weeklyBossElement = raid.WeeklyBossElement;

            // 도감 해금
            d.dexUnlocked = new List<string>(unlockedSpirits);

            return d;
        }

        /// <summary>SaveData → 게임 상태 복원</summary>
        public void ApplySaveData(SaveData d)
        {
            if (d == null) return;
            saveLoaded = true;
            gold = d.gold;
            ruby = d.ruby;
            if (player != null) player.LoadFrom(d.playerLevel, d.playerExp, d.skillPoints, d.upgradeLevels);
            currentStageIndex = Mathf.Max(0, d.stageIndex);
            repeatMode = d.repeatMode;

            // 보드 정령 복원
            if (board != null)
            {
                foreach (var s in d.boardSpirits)
                {
                    var sd = Resources.Load<SpiritData>($"Data/Spirits/{s.dataId}");
                    if (sd != null) board.TrySummonAt(sd, s.slotIndex, s.level);
                }
                // 파티 배치 복원
                if (d.partySlots != null)
                    for (int p = 0; p < 4 && p < d.partySlots.Length; p++)
                        if (d.partySlots[p] >= 0)
                            board.AssignToParty(p, d.partySlots[p]);
            }

            // 의뢰 복원
            var offers = new List<DispatchRequest>();
            foreach (var s in d.offers) offers.Add(ToRequest(s));
            var active = new List<DispatchService.DispatchEntry>();
            foreach (var s in d.activeDispatches) active.Add(ToDispatch(s));
            var completed = new List<DispatchService.DispatchEntry>();
            foreach (var s in d.completedDispatches) completed.Add(ToDispatch(s));
            dispatch.LoadFrom(offers, active, completed, d.requestCooldownTimer, d.totalDispatchCount);

            // ⭐ 오프라인 경과 반영 — 파견 남은시간/의뢰 쿨다운 차감 (완료 전환 포함)
            long elapsed = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - d.saveTimestamp;
            if (elapsed > 0)
            {
                GameLogger.Info($"[GM] 오프라인 경과 {elapsed}초 → 파견/쿨다운 반영");
                dispatch.Tick(elapsed);
            }

            // 미션 복원
            missions.LoadFrom(d.dailyProgress, d.dailyClaimed, d.weeklyProgress, d.weeklyClaimed);

            // 레이드 복원
            raid.LoadFrom(d.raidStage, d.raidTotalDamage, d.raidBestScore, d.raidStageRewardClaimed, d.weeklyBossElement);

            // 도감 복원
            unlockedSpirits.Clear();
            if (d.dexUnlocked != null)
                foreach (var n in d.dexUnlocked) if (!string.IsNullOrEmpty(n)) unlockedSpirits.Add(n);

            // UI 갱신 (도감은 탭 열 때 자동 표시)
            if (formationUI != null) formationUI.Refresh();
            if (upgradeUI != null) upgradeUI.Refresh();
            if (requestUI != null) requestUI.Refresh();

            GameLogger.Info($"[GM] 저장 데이터 복원 완료 — 골드 {gold}, 루비 {ruby}, Lv.{player?.Level}, 스테이지 {CurrentStageName}");
        }

        /// <summary>게임 시작 시 저장 파일 로드</summary>
        void LoadSavedGame()
        {
            var dm = GetDataManager();
            if (dm == null) return;
            var data = dm.LoadGame();
            if (data != null)
            {
                GameLogger.Info($"[GM] 저장 파일 발견 → 복원 시도 (저장 시각 {data.saveTimestamp})");
                ApplySaveData(data);
            }
            else
            {
                GameLogger.Info("[GM] 저장 파일 없음 — 새 게임 시작");
            }
        }

        // ── SaveData ↔ 서비스 변환 헬퍼 ──

        static SavedDispatchRequest ToSavedRequest(DispatchRequest r)
        {
            var s = new SavedDispatchRequest
            {
                id = r.id, durationHours = r.durationHours,
                goldReward = r.goldReward, rubyReward = r.rubyReward
            };
            if (r.slots != null)
                foreach (var sl in r.slots)
                    s.slots.Add(new SavedOfferSlot { requiredElement = sl.requiredElement, minGrade = sl.minGrade });
            return s;
        }

        static DispatchRequest ToRequest(SavedDispatchRequest s)
        {
            var r = new DispatchRequest
            {
                id = s.id, durationHours = s.durationHours,
                goldReward = s.goldReward, rubyReward = s.rubyReward,
                slots = new DispatchSlot[s.slots != null ? s.slots.Count : 0]
            };
            if (s.slots != null)
                for (int i = 0; i < s.slots.Count; i++)
                    r.slots[i] = new DispatchSlot { requiredElement = s.slots[i].requiredElement, minGrade = s.slots[i].minGrade };
            return r;
        }

        static SavedDispatch ToSavedDispatch(DispatchService.DispatchEntry e)
        {
            return new SavedDispatch
            {
                request = ToSavedRequest(e.Request),
                spirit1Name = e.Spirit1Name, spirit1Element = e.Spirit1Element, spirit1Grade = e.Spirit1Grade,
                spirit2Name = e.Spirit2Name, spirit2Element = e.Spirit2Element, spirit2Grade = e.Spirit2Grade,
                remainingSeconds = e.RemainingSeconds, notified = e.Notified
            };
        }

        static DispatchService.DispatchEntry ToDispatch(SavedDispatch s)
        {
            return new DispatchService.DispatchEntry
            {
                Request = ToRequest(s.request),
                Spirit1Name = s.spirit1Name, Spirit1Element = s.spirit1Element, Spirit1Grade = s.spirit1Grade,
                Spirit2Name = s.spirit2Name, Spirit2Element = s.spirit2Element, Spirit2Grade = s.spirit2Grade,
                RemainingSeconds = s.remainingSeconds, Notified = s.notified
            };
        }
    }
}


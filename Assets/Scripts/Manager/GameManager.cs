using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SpiritMerge.Battle;
using SpiritMerge.Merge;

namespace SpiritMerge
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public Canvas mainCanvas;
        public BattleManager battleManager;

        public int currentStage = 1;
        public int gold = 500;
        public int ruby = 100;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            GameLogger.Info("[GM] GameManager 시작");
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null) { GameLogger.Error("[GM] MainCanvas 없음!"); return; }

            SetupBattleSystem();
            SetupMergeSystem();
            SetupGNBTabs();
            SetupTopBar();

            // ⭐ 게임 시작 시 자동 전투
            StartAutoBattle();

            // ⭐ 화면 전체 클릭 → MergeBoard 선택 해제
            SetupGlobalDeselect();

            GameLogger.Info("[GM] 모든 시스템 초기화 완료!");
        }

        private bool _battleStarted = false;

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

            var allStages = Resources.LoadAll<StageData>("Data/Stages");
            if (allStages.Length == 0) { GameLogger.Error("[GM] StageData 없음! 전투 불가"); return; }
            var stage = allStages[0];

            var spawner = FindObjectOfType<MonsterSpawner>();
            if (spawner != null) spawner.InitializeSpawnPoints(stage.spawnPointCount);

            var waveCtrl = FindObjectOfType<WaveController>();
            if (waveCtrl == null) { GameLogger.Error("[GM] WaveController 없음!"); return; }

            GameLogger.Info($"[GM] 전투 자동 시작: {stage.stageName}");
            waveCtrl.StartBattle(stage);
        }

        /// <summary>
        /// 기본 정령 지급 (MergeBoard가 비어있을 때만)
        /// </summary>
        void GiveStarterSpirit()
        {
            var board = FindObjectOfType<MergeBoardManager>();
            if (board == null) return;
            if (board.GetActiveSpiritData().Length > 0) return; // 이미 정령 있음

            var allSpirits = Resources.LoadAll<SpiritData>("Data/Spirits");
            var fire = System.Array.Find(allSpirits, s => s.element == ElementType.Fire);
            if (fire != null)
            {
                board.TrySummon(fire);
                GameLogger.Info($"[GM] ⭐ 기본 정령 지급: {fire.spiritName}");
            }
        }

        /// <summary>
        /// MergeBoard의 정령 → BattleArea에 SpiritUnit 배치
        /// </summary>
        void DeploySpiritsToBattle()
        {
            if (battleManager == null || battleManager.spiritSpawnRoot == null) return;

            var board = FindObjectOfType<MergeBoardManager>();
            if (board == null) return;
            var spiritDatas = board.GetActiveSpiritData();
            if (spiritDatas.Length == 0) return;

            var prefab = Resources.Load<GameObject>("Prefabs/Spirit");
            for (int i = 0; i < spiritDatas.Length; i++)
            {
                SpiritUnit unit = null;
                if (prefab != null)
                {
                    var go = Instantiate(prefab, battleManager.spiritSpawnRoot);
                    go.transform.localPosition = new Vector3(-1.5f + i * 1.5f, 0, 0);
                    unit = go.GetComponent<SpiritUnit>();
                }
                else
                {
                    var go = new GameObject($"Spirit_{i}", typeof(SpriteRenderer), typeof(SpiritUnit));
                    go.transform.SetParent(battleManager.spiritSpawnRoot, false);
                    go.transform.localPosition = new Vector3(-1.5f + i * 1.5f, 0, 0);
                    unit = go.GetComponent<SpiritUnit>();
                    unit.spriteRenderer = go.GetComponent<SpriteRenderer>();
                    if (unit.spriteRenderer != null && spiritDatas[i].sprite != null)
                        unit.spriteRenderer.sprite = spiritDatas[i].sprite;
                }
                if (unit != null) unit.Initialize(spiritDatas[i]);
            }
            GameLogger.Info($"[GM] 전투 정령 {spiritDatas.Length}기 배치 완료");
        }

        // ── Gold/Ruby ──
        public bool SpendGold(int amount)
        {
            if (gold < amount) { GameLogger.Warn($"[GM] 골드 부족: {gold}/{amount}"); return false; }
            gold -= amount;
            UpdateGoldDisplay();
            GameLogger.Info($"[GM] 골드 사용: -{amount} (잔액: {gold})");
            return true;
        }

        public bool SpendRuby(int amount)
        {
            if (ruby < amount) { GameLogger.Warn($"[GM] 루비 부족: {ruby}/{amount}"); return false; }
            ruby -= amount;
            UpdateRubyDisplay();
            GameLogger.Info($"[GM] 루비 사용: -{amount} (잔액: {ruby})");
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

        // ── 전투 시스템 ──
        Transform CreateRoot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        void SetupBattleSystem()
        {
            var ba = GameObject.Find("BattleArea");
            if (ba == null) { GameLogger.Warn("[GM] BattleArea 없음, 전투 시스템 스킵"); return; }

            battleManager = ba.GetComponent<BattleManager>();
            if (battleManager == null) battleManager = ba.AddComponent<BattleManager>();
            battleManager.spiritSpawnRoot = CreateRoot(ba.transform, "SpiritSpawnRoot");
            battleManager.enemySpawnRoot = CreateRoot(ba.transform, "EnemySpawnRoot");

            var spawner = ba.GetComponent<MonsterSpawner>();
            if (spawner == null) ba.AddComponent<MonsterSpawner>();
            var wc = ba.GetComponent<WaveController>();
            if (wc == null) ba.AddComponent<WaveController>();

            GameLogger.Info("[GM] 전투 시스템 준비 완료");
        }

        // ── 머지 시스템 ──
        void SetupMergeSystem()
        {
            var ma = GameObject.Find("MergeArea");
            if (ma == null) { GameLogger.Error("[GM] MergeArea 없음!"); return; }

            var board = ma.GetComponent<MergeBoardManager>();
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
            int cost = board.summonCost;
            if (gold < cost) { GameLogger.Warn($"[GM] 골드 부족: {gold}/{cost}"); return; }

            var all = Resources.LoadAll<SpiritData>("Data/Spirits");
            if (all.Length == 0) { GameLogger.Error("[GM] SpiritData 없음! Connect All Sprites 먼저 실행"); return; }
            var sd = all[Random.Range(0, all.Length)];
            GameLogger.Info($"[GM] 소환할 정령 선택: {sd.name}");

            if (board.TrySummon(sd))
            {
                gold -= cost;
                UpdateGoldDisplay();
                GameLogger.Info($"[GM] 소환 성공: {sd.name} (골드: {gold})");
            }
            else
            {
                GameLogger.Warn("[GM] 소환 실패: 빈 슬롯 없음");
            }
        }

        // ── GNB ──
        void SetupGNBTabs()
        {
            var bm = GameObject.Find("BottomMenu");
            if (bm == null) { GameLogger.Warn("[GM] BottomMenu 없음"); return; }
            for (int i = 0; i < 5; i++)
            {
                var t = bm.transform.Find($"Tab_{i}");
                if (t == null) continue;
                var b = t.GetComponent<Button>();
                if (b == null) b = t.gameObject.AddComponent<Button>();
                int idx = i;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => OnTabClicked(idx));
            }
            GameLogger.Info("[GM] GNB 5개 탭 연결 완료");
        }

        void OnTabClicked(int idx)
        {
            string[] names = { "전투", "파티", "업그레드", "도감", "의뢰" };
            string n = idx < names.Length ? names[idx] : $"Tab_{idx}";
            GameLogger.Info($"[GM] GNB 탭 클릭: {n}");
        }

        // ── TopBar ──
        void SetupTopBar()
        {
            UpdateGoldDisplay();
            UpdateRubyDisplay();

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

        // ── 화면 전체 클릭 → MergeBoard 선택 해제 ──
        void SetupGlobalDeselect()
        {
            var board = FindObjectOfType<MergeBoardManager>();
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
                bgBtn.onClick.AddListener(() => board.DeselectCurrent());
                GameLogger.Info("[GM] ScreenBackground → 빈 공간 클릭 감지");
            }

            // 2. GNB 각 탭에 DeselectCurrent 추가
            var bm = GameObject.Find("BottomMenu");
            if (bm != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    var tab = bm.transform.Find($"Tab_{i}");
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
    }
}


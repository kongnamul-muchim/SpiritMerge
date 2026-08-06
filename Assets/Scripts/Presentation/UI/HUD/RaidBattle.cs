using SpiritMerge.Battle;
using SpiritMerge.Core.Systems;
using SpiritMerge.Merge;
using TMPro;
using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 레이드 실전투 — 일반 전투를 일시정지하고 전투 화면(BattleArea)에 보스를 실제 스폰,
    /// 파티 정령이 자동 공격(전투 시스템 재사용). 60초 or 파티 전멸 → 점수 확정 → 일반 전투 재개.
    /// </summary>
    public class RaidBattle : MonoBehaviour
    {
        private GameManager gm;
        private RaidService raid;
        private WaveController waveCtrl;
        private MonsterSpawner spawner;
        private Monster boss;
        private float timeLeft;
        private float lastBossHp;
        private long preRaidScore;
        private bool running;
        private TextMeshProUGUI _timerText;   // 전투 화면 레이드 타이머

        void Awake()
        {
            gm = GameManager.Instance;
            raid = gm != null ? gm.raid : null;
            waveCtrl = GetComponent<WaveController>();
            spawner = GetComponent<MonsterSpawner>();
            if (waveCtrl == null) waveCtrl = GetComponentInParent<WaveController>();
            if (spawner == null) spawner = GetComponentInParent<MonsterSpawner>();
        }

        public bool IsRunning => running;

        public void StartRaid()
        {
            if (running || raid == null) return;
            if (gm == null) gm = GameManager.Instance;
            if (gm != null) raid = gm.raid;
            if (raid == null) { GameLogger.Error("[RaidBattle] RaidService 없음"); return; }
            if (spawner == null) { GameLogger.Error("[RaidBattle] MonsterSpawner 없음"); return; }

            // ⭐ 일반 전투 완전 정지 — WaveController Update(패배 감지/웨이브) 차단
            //    기존 전투 상태는 0으로 리셋, 스테이지 인덱스만 유지 (레이드 종료 후 1웨이브 재시작)
            if (waveCtrl != null)
            {
                waveCtrl.enabled = false;
                waveCtrl.StopAllCoroutines();
            }
            if (spawner != null) spawner.ResetAllMonsters();
            var animator = waveCtrl != null ? waveCtrl.GetComponentInChildren<WaveAnimator>() : null;
            if (animator != null) animator.Hide(); // 일반 전투 WAVE 표시 제거

            // ⭐ 의뢰 탭 닫고 전투 화면으로 이동 (레이드 실전투를 전투 탭에서 보여주기 위해)
            if (gm != null) gm.ShowBattleTab();

            // 파티 공격 활성화 (전투 상태 유지 — SpiritUnit이 Battling일 때만 공격)
            if (BattleManager.Instance != null)
                BattleManager.Instance.state = BattleState.Battling;

            preRaidScore = raid.TotalDamage;

            // ⭐ 파티에 정령이 없으면 레이드 시작 대신 툴팁 안내 (즉시 종료하지 않음)
            bool hasPartySpirit = false;
            var board = UnityEngine.Object.FindAnyObjectByType<MergeBoardManager>();
            if (board != null) hasPartySpirit = board.GetPartyItems().Length > 0;
            if (!hasPartySpirit)
            {
                GameLogger.Warn("[Request] 파티에 정령 없음 — 레이드 시작 차단");
                ShowTooltip("파티에 정령이 없습니다");
                return;
            }

            // ⭐ 잠깐 대기 후 "레이드 시작!" 배너 → 보스 스폰 (웨이브 표시와 같은 연출)
            running = true;
            StartCoroutine(RaidStartSequence(animator));
        }

        /// <summary>레이드 시작 연출: "레이드 시작!" 배너 표시 후 보스 스폰</summary>
        System.Collections.IEnumerator RaidStartSequence(WaveAnimator animator)
        {
            if (animator != null) animator.ShowRaidStart();
            yield return new WaitForSeconds(1.5f);

            CreateTimer();
            if (_timerText != null) _timerText.gameObject.SetActive(true);
            SpawnBoss(raid.Stage);
        }

        /// <summary>전투 화면 상단에 툴팁 표시 (잠시 후 자동 제거)</summary>
        void ShowTooltip(string msg)
        {
            var old = transform.Find("RaidTooltip");
            if (old != null) Destroy(old.gameObject);

            var go = new GameObject("RaidTooltip", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(360, 44);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.4f, 0.35f, 1f);
            tmp.text = msg;
            // ⭐ 한글 폰트 (NotoSansKR) — 기본 폰트면 한글 깨짐
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font != null) tmp.font = font;

            StartCoroutine(RemoveAfter(go, 2.5f));
        }

        /// <summary>지정 시간 후 오브젝트 제거 (툴팁 등)</summary>
        System.Collections.IEnumerator RemoveAfter(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go != null) Destroy(go);
        }

        /// <summary>전투 화면 상단에 레이드 타이머 생성</summary>
        void CreateTimer()
        {
            if (_timerText != null) return;
            var go = new GameObject("RaidTimer", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(320, 30);
            _timerText = go.GetComponent<TextMeshProUGUI>();
            _timerText.fontSize = 18;
            _timerText.fontStyle = FontStyles.Bold;
            _timerText.alignment = TextAlignmentOptions.Center;
            _timerText.color = new Color(1f, 0.85f, 0.4f);
            // ⭐ 한글 폰트 (NotoSansKR) — 기본 폰트면 한글 깨짐
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font != null) _timerText.font = font;
        }

        void SpawnBoss(int stage)
        {
            if (spawner == null) return;
            spawner.SetupBattle(raid.WeeklyBossElement, 1f, 1f);

            var idx = spawner.GetCenteredSlotIndices(1);
            if (idx.Length == 0) { GameLogger.Error("[RaidBattle] 보스 스폰 슬롯 없음"); EndRaid(); return; }
            var bossGo = spawner.SpawnBossAt(idx[0]); // ⭐ 레이드 전용 보스 몬스터(isBoss) 스폰
            if (bossGo == null) { GameLogger.Error("[RaidBattle] 보스 스폰 실패"); EndRaid(); return; }

            boss = bossGo.GetComponent<Monster>();
            if (boss == null) { GameLogger.Error("[RaidBattle] Monster 컴포넌트 없음"); EndRaid(); return; }

            // ⭐ 단계 기반 보스 스탯 오버라이드
            boss.maxHp = RaidService.GetBossHP(stage);
            boss.hp = boss.maxHp;
            boss.atk = RaidService.GetBossATK(stage);
            // ⭐ HP바는 0~1 기반 (UpdateHpBar가 hp/maxHp) — maxValue 변경 금지 (연동 버그 수정)
            if (boss.hpSlider != null) { boss.hpSlider.minValue = 0f; boss.hpSlider.maxValue = 1f; boss.hpSlider.value = 1f; }
            lastBossHp = boss.maxHp;
            timeLeft = RaidService.RaidDuration;

            GameLogger.Info($"[Request] 레이드 시작: {raid.WeeklyBossElement} 보스 {stage}단계 (HP {boss.maxHp:N0}, ATK {boss.atk})");
        }

        void Update()
        {
            if (!running) return;
            // ⭐ 전투 화면 타이머 (남은 시간/페이즈/점수)
            if (_timerText != null)
                _timerText.text = $"레이드 {Mathf.Max(0, timeLeft):0}초 / 페이즈 {raid.Stage} / 점수 {raid.TotalDamage:N0}";
            if (boss == null) return;
            timeLeft -= Time.deltaTime;

            // 받은 데미지 → 점수 반영 (이전 HP - 현재 HP)
            float delta = lastBossHp - boss.hp;
            if (delta > 0f) { raid.AddDamage((long)delta); lastBossHp = boss.hp; }

            // ⭐ 페이즈 클리어 → 보스는 죽지 않고 더 강한 체력으로 리필 (점수 보너스 + 페이즈 상승)
            if (!boss.isAlive)
            {
                raid.PhaseCleared(); // 깬 페이즈 체력만큼 보너스 점수
                raid.StageUp();      // 다음 페이즈
                GameLogger.Info($"[Request] 페이즈 클리어! {raid.Stage}페이즈 보스 등장 (HP {RaidService.GetBossHP(raid.Stage):N0})");
                SpawnBoss(raid.Stage);
                return;
            }

            // 파티 전멸 → 레이드 종료
            if (BattleManager.Instance != null && BattleManager.Instance.IsPartyDead) { EndRaid(); return; }

            if (timeLeft <= 0f) EndRaid();
        }

        void EndRaid()
        {
            running = false;
            if (_timerText != null) _timerText.gameObject.SetActive(false);
            if (spawner != null) spawner.ResetAllMonsters();
            boss = null;

            long gained = raid.TotalDamage - preRaidScore;
            bool newRecord = raid.EndRaid(gained);
            GameLogger.Info($"[Request] 레이드 종료! 점수 {gained:N0} ({(newRecord ? "신기록" : "기록 갱신 없음")})");

            // ⭐ 파티 HP 풀 회복 — 레이드로 사망한 상태에서 일반 전투 재개 시 즉사 방지
            if (BattleManager.Instance != null && BattleManager.Instance.partyMaxHP > 0)
                BattleManager.Instance.HealParty(BattleManager.Instance.partyMaxHP);

            // ⭐ 일반 전투 재개 — 진행 중이던 스테이지 1웨이브부터 다시 시작
            if (gm != null && waveCtrl != null)
            {
                var stage = gm.GetCurrentStage();
                if (stage != null)
                {
                    waveCtrl.enabled = true; // ⭐ Update(패배 감지) 복구
                    GameLogger.Info("[Request] 일반 전투 재개 (1웨이브부터)");
                    waveCtrl.StartBattle(stage);
                }
            }
        }
    }
}

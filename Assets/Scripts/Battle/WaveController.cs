using System.Collections;
using UnityEngine;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// 웨이브 진행 컨트롤러
    /// - WaveCalculator를 사용하여 몬스터 분배
    /// - 웨이브 시작 → 몬스터 생성 → 모두 처치 → 다음 웨이브
    /// - 동시에 전장에 나올 수 있는 몬스터는 maxConcurrentMonsters(EnemySlot 3개)로 제한
    /// - 보스전 처리 (마지막 웨이브 보스 1마리)
    /// </summary>
    public class WaveController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private MonsterSpawner spawner;
        [SerializeField] private StageData stageData;

        [Header("웨이브 설정")]
        [SerializeField] private float waveTransitionDelay = 2f; // 웨이브 전환 딜레이
        [SerializeField] private float defeatRestartDelay = 2.5f; // 패배 후 재도전 딜레이

        private int[] waveDistribution;
        private int currentWave = 0;
        private int totalWaves = 0;
        private int monstersRemaining = 0; // 이번 전투에서 처치해야 할 남은 수 (사망 시에만 감소)
        private int aliveMonsters = 0;     // 현재 전장에 살아있는 몬스터 수
        private bool _defeatHandled = false; // 이번 전투에서 패배 처리 완료 여부 (중복 방지)

        private WaveAnimator _waveAnimator; // Battle Area 정중앙 웨이브/클리어/실패 배너

        public System.Action<int, int> OnWaveChanged;       // (current, total)
        public System.Action<int, int> OnMonstersUpdated;   // (remaining, total)
        public System.Action OnBattleStarted;
        public System.Action OnBattleWon;
        public System.Action OnBattleLost;

        void Awake()
        {
            // ⭐ WaveInfo 배너 연결 (Battle Area 하위에 존재)
            // 씬에 WaveAnimator가 없는 경우 동적으로 부착 (WaveInfo가 빌더로 생성된 씬 대응)
            _waveAnimator = GetComponentInChildren<WaveAnimator>();
            if (_waveAnimator == null)
            {
                var waveInfo = transform.Find("WaveInfo");
                if (waveInfo != null)
                    _waveAnimator = waveInfo.gameObject.AddComponent<WaveAnimator>();
            }
            if (_waveAnimator == null)
                GameLogger.Warn("[WC] WaveInfo/WaveAnimator 없음 → 웨이브 배너 미표시");

            OnWaveChanged += (cur, total) =>
            {
                if (_waveAnimator != null) _waveAnimator.ShowWave(cur, total);
            };
        }

        /// <summary>
        /// 전투 시작
        /// </summary>
        public void StartBattle(StageData data)
        {
            // 이전에 진행 중이던 웨이브 코루틴 정리 (파티 변경 등으로 재시작 시)
            StopAllCoroutines();

            // ⭐ 이전 전투의 남은 몬스터/슬롯 정리
            //    (안 하면 이전 몬스터가 EnemySlot을 점유 → 새 스폰 실패 → aliveMonsters 0 → 전투 동결)
            if (spawner != null) spawner.ResetAllMonsters();

            stageData = data;
            totalWaves = data.waveCount;

            // 웨이브별 몬스터 분배 계산
            waveDistribution = WaveCalculator.DistributeMonsters(
                data.totalMonsterCount,
                data.waveCount,
                data.isBossStage
            );

            // 유효성 검증
            bool valid = WaveCalculator.ValidateDistribution(
                waveDistribution,
                data.totalMonsterCount,
                data.isBossStage
            );
            if (!valid)
            {
                Debug.LogError("[WaveController] 몬스터 분배 검증 실패!");
                return;
            }

            // 디버그 정보
            string distStr = string.Join(", ", waveDistribution);
            Debug.Log($"[WaveController] 전투 시작: {data.stageName}");
            Debug.Log($"  총 몬스터: {data.totalMonsterCount}, 웨이브: {totalWaves}");
            Debug.Log($"  분배: [{distStr}]");

            currentWave = 0;
            monstersRemaining = data.totalMonsterCount;
            aliveMonsters = 0;
            _defeatHandled = false;

            // ⭐ 스포너에 스테이지 정보 전달 (속성 테마 필터 + HP/ATK 배율)
            spawner.SetupBattle(data.elementType, data.hpMultiplier, data.atkMultiplier);

            // 정령 자동 공격 활성화 (구식 SpiritUnit은 Battling 상태일 때만 공격)
            if (BattleManager.Instance != null)
                BattleManager.Instance.state = BattleState.Battling;

            OnBattleStarted?.Invoke();
            OnWaveChanged?.Invoke(currentWave, totalWaves);
            OnMonstersUpdated?.Invoke(monstersRemaining, data.totalMonsterCount);

            // 첫 웨이브 시작
            StartCoroutine(StartNextWave());
        }

        /// <summary>
        /// 패배 감지 — 파티 통합 HP가 0이면 즉시 패배 처리
        /// (웨이브 전환 중이어도 감지 — 한 대 더 맞지 않고 0 도달 시 즉시)
        /// </summary>
        void Update()
        {
            if (_defeatHandled) return;
            if (BattleManager.Instance?.state != BattleState.Battling) return;

            // ⭐ 파티 통합 HP 0 → 즉시 패배 (aliveMonsters 조건 제거 — 웨이브 전환 중에도 감지)
            if (BattleManager.Instance.IsPartyDead)
            {
                _defeatHandled = true;
                StartCoroutine(HandleBattleLostRoutine());
            }
        }

        /// <summary>
        /// 패배 처리 — 짧은 딜레이 후 Defeat 상태 전환 + 재도전 이벤트 발행
        /// </summary>
        IEnumerator HandleBattleLostRoutine()
        {
            yield return new WaitForSeconds(defeatRestartDelay);

            if (BattleManager.Instance != null)
                BattleManager.Instance.state = BattleState.Defeat;

            GameLogger.Info("[WC] 💀 파티 HP 0 → 패배");
            OnBattleLost?.Invoke();
        }

        /// <summary>
        /// 다음 웨이브 시작 (코루틴)
        /// 빈 SpawnPoint(EnemySlot)가 있을 때만 몬스터를 생성하므로 동시 최대 3마리 보장
        /// </summary>
        private IEnumerator StartNextWave()
        {
            if (currentWave >= totalWaves)
            {
                HandleBattleWon();
                yield break;
            }

            int monstersToSpawn = waveDistribution[currentWave];
            bool isBoss = stageData.isBossStage && currentWave == totalWaves - 1;

            string label = isBoss ? "[보스]" : $"[웨이브 {currentWave + 1}]";
            Debug.Log($"{label} 몬스터 {monstersToSpawn}마리 등장!");

            // 웨이브 전환 딜레이
            yield return new WaitForSeconds(waveTransitionDelay);

            // ⭐ 중앙정렬: 이번 웨이브 몬스터 수에 맞는 슬롯 인덱스를 미리 계산
            // 3슬롯: 1마리→[1], 2마리→[0,2], 3마리→[0,1,2]  (X O X / O X O / O O O)
            int slotCount = spawner.EnemySlotCount;
            int firstBatch = Mathf.Min(monstersToSpawn, slotCount);
            int[] centered = spawner.GetCenteredSlotIndices(firstBatch);

            // ⭐ 몬스터 동시 등장 (지연 없음) — 배치 가능한 수만큼 한 번에
            for (int i = 0; i < firstBatch; i++)
            {
                GameObject monster = spawner.SpawnMonsterAt(centered[i]);
                if (monster != null)
                {
                    aliveMonsters++;

                    // 보스인 경우 HP 배율 적용 + 보스 플래그 (이름 변경 없음 — 슬롯 이름 유지)
                    if (isBoss)
                    {
                        var m = monster.GetComponent<Monster>();
                        if (m != null)
                        {
                            m.isBoss = true;
                            m.maxHp *= stageData.bossHpMultiplier;
                            m.hp = m.maxHp;
                        }
                    }
                }
            }

            // 웨이브 몬스터가 슬롯 수보다 많으면: 슬롯이 비면 이어서 스폰 (처치 대기)
            for (int i = firstBatch; i < monstersToSpawn; i++)
            {
                // 빈 슬롯이 있을 때까지 대기 (동시 최대 3마리 제한)
                yield return new WaitUntil(() => spawner.AvailableCount > 0);

                GameObject monster = spawner.SpawnMonster();
                if (monster != null)
                {
                    aliveMonsters++;

                    // 보스인 경우 HP 배율 적용 + 보스 플래그
                    if (isBoss)
                    {
                        var m = monster.GetComponent<Monster>();
                        if (m != null)
                        {
                            m.isBoss = true;
                            m.maxHp *= stageData.bossHpMultiplier;
                            m.hp = m.maxHp;
                        }
                    }
                }
            }

            // 이번 웨이브 몬스터 전멸까지 대기 (사망 시 aliveMonsters 감소)
            yield return new WaitUntil(() => aliveMonsters <= 0);

            currentWave++;
            // ⭐ 빛 시너지: 웨이브 클리어 시 파티 HP 회복
            GameManager.Instance?.HealPartyBySynergy();
            OnWaveChanged?.Invoke(currentWave, totalWaves);

            if (currentWave < totalWaves)
            {
                StartCoroutine(StartNextWave());
            }
            else
            {
                HandleBattleWon();
            }
        }

        /// <summary>
        /// 스테이지 클리어 처리 — 보상 지급 후 이벤트 발행
        /// </summary>
        private void HandleBattleWon()
        {
            // 전투 종료 상태 전환
            if (BattleManager.Instance != null)
                BattleManager.Instance.state = BattleState.Victory;

            // ⭐ 스테이지 클리어 보상 (골드 획득량 보너스 적용)
            if (stageData != null && GameManager.Instance != null)
            {
                int reward = stageData.goldReward;
                GameManager.Instance.AddBattleGold(reward);
                GameLogger.Info($"[WC] ⭐ 스테이지 클리어! 골드 +{reward}");
            }
            OnBattleWon?.Invoke();
        }

        /// <summary>
        /// 몬스터 처치 시 호출
        /// monstersRemaining은 스폰이 아니라 처치 시에만 감소 (이중 감소 버그 수정)
        /// </summary>
        public void OnMonsterKilled()
        {
            aliveMonsters = Mathf.Max(0, aliveMonsters - 1);
            monstersRemaining = Mathf.Max(0, monstersRemaining - 1);
            OnMonstersUpdated?.Invoke(monstersRemaining, stageData.totalMonsterCount);
        }

        /// <summary>
        /// 현재 웨이브 정보
        /// </summary>
        public int CurrentWave => currentWave;
        public int TotalWaves => totalWaves;
        public int MonstersRemaining => monstersRemaining;
        public bool IsBossStage => stageData != null && stageData.isBossStage;
        public StageData StageData => stageData;
    }
}

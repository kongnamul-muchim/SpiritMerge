using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// 웨이브 진행 컨트롤러
    /// - WaveCalculator를 사용하여 몬스터 분배
    /// - 웨이브 시작 → 몬스터 생성 → 모두 처치 → 다음 웨이브
    /// - 보스전 처리 (마지막 웨이브 보스 1마리)
    /// </summary>
    public class WaveController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private MonsterSpawner spawner;
        [SerializeField] private StageData stageData;

        [Header("웨이브 설정")]
        [SerializeField] private float spawnDelay = 0.5f;      // 몬스터 간 생성 지연
        [SerializeField] private float waveTransitionDelay = 2f; // 웨이브 전환 딜레이

        private int[] waveDistribution;
        private int currentWave = 0;
        private int totalWaves = 0;
        private int monstersRemaining = 0;
        private bool isWaveActive = false;

        public System.Action<int, int> OnWaveChanged;       // (current, total)
        public System.Action<int, int> OnMonstersUpdated;   // (remaining, total)
        public System.Action OnBattleStarted;
        public System.Action OnBattleWon;
        public System.Action OnBattleLost;

        /// <summary>
        /// 전투 시작
        /// </summary>
        public void StartBattle(StageData data)
        {
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

            OnBattleStarted?.Invoke();
            OnWaveChanged?.Invoke(currentWave, totalWaves);
            OnMonstersUpdated?.Invoke(monstersRemaining, data.totalMonsterCount);

            // 첫 웨이브 시작
            StartCoroutine(StartNextWave());
        }

        /// <summary>
        /// 다음 웨이브 시작 (코루틴)
        /// </summary>
        private IEnumerator StartNextWave()
        {
            if (currentWave >= totalWaves)
            {
                OnBattleWon?.Invoke();
                yield break;
            }

            isWaveActive = true;
            int monstersToSpawn = waveDistribution[currentWave];
            bool isBoss = stageData.isBossStage && currentWave == totalWaves - 1;

            string label = isBoss ? "[보스]" : $"[웨이브 {currentWave + 1}]";
            Debug.Log($"{label} 몬스터 {monstersToSpawn}마리 등장!");

            // 웨이브 전환 딜레이
            yield return new WaitForSeconds(waveTransitionDelay);

            // 몬스터 생성 (일정 간격으로)
            for (int i = 0; i < monstersToSpawn; i++)
            {
                GameObject monster = spawner.SpawnMonster();
                if (monster != null)
                {
                    // 보스인 경우 HP 10배 적용
                    if (isBoss)
                    {
                        var m = monster.GetComponent<Monster>();
                        if (m != null)
                        {
                            m.maxHp *= stageData.bossHpMultiplier;
                            m.hp = m.maxHp;
                            monster.name = $"Boss_{monster.name}";
                        }
                    }

                    monstersRemaining--;
                    OnMonstersUpdated?.Invoke(monstersRemaining, stageData.totalMonsterCount);
                }

                if (i < monstersToSpawn - 1)
                {
                    yield return new WaitForSeconds(spawnDelay);
                }
            }

            // 모든 몬스터가 처치될 때까지 대기
            yield return new WaitUntil(() => monstersRemaining <= 0 || !isWaveActive);

            if (isWaveActive)
            {
                currentWave++;
                OnWaveChanged?.Invoke(currentWave, totalWaves);

                if (currentWave < totalWaves)
                {
                    StartCoroutine(StartNextWave());
                }
                else
                {
                    OnBattleWon?.Invoke();
                }
            }
        }

        /// <summary>
        /// 몬스터 처치 시 호출
        /// </summary>
        public void OnMonsterKilled()
        {
            monstersRemaining = Mathf.Max(0, monstersRemaining - 1);
            OnMonstersUpdated?.Invoke(monstersRemaining, stageData.totalMonsterCount);

            if (monstersRemaining <= 0 && !isWaveActive)
            {
                // 현재 웨이브의 모든 몬스터 처치
                isWaveActive = false;
            }
        }

        /// <summary>
        /// 현재 웨이브 정보
        /// </summary>
        public int CurrentWave => currentWave;
        public int TotalWaves => totalWaves;
        public int MonstersRemaining => monstersRemaining;
        public bool IsBossStage => stageData != null && stageData.isBossStage;
    }
}

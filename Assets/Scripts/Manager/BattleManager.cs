using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 전투 시스템 관리자
    /// GDD 2. 전투 시스템 기반
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance;

        [Header("설정")]
        public Transform spiritSpawnRoot;
        public Transform enemySpawnRoot;
        public Transform battleField;

        [Header("전투 상태")]
        public BattleState state = BattleState.Idle;
        public int currentWave = 0;
        public int maxWaves = 5;
        public float waveDelay = 2f;

        private List<SpiritUnit> _activeSpirits = new List<SpiritUnit>();
        private List<EnemyUnit> _activeEnemies = new List<EnemyUnit>();
        private Coroutine _battleCoroutine;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        /// <summary>
        /// 전투 시작
        /// </summary>
        public void StartBattle(int stageNumber)
        {
            if (state == BattleState.Battling) return;

            state = BattleState.Battling;
            _battleCoroutine = StartCoroutine(BattleFlow(stageNumber));
        }

        private IEnumerator BattleFlow(int stageNumber)
        {
            // 1. 정령 배치
            SpawnPartySpirits();

            // 2. 웨이브 진행
            for (currentWave = 0; currentWave < maxWaves; currentWave++)
            {
                yield return new WaitForSeconds(waveDelay);
                yield return StartCoroutine(SpawnWave(currentWave));
                yield return StartCoroutine(WaitForWaveClear());
            }

            // 3. 보스 웨이브
            yield return StartCoroutine(SpawnBossWave());
            yield return StartCoroutine(WaitForWaveClear());

            // 4. 전투 종료
            BattleEnd(true);
        }

        private void SpawnPartySpirits()
        {
            // TODO: GameManager 파티 정보 로드 → SpiritUnit 생성
            Debug.Log("[BattleManager] Party spirits spawned");
        }

        private IEnumerator SpawnWave(int waveIndex)
        {
            int enemyCount = 3 + waveIndex;
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy(waveIndex);
                yield return new WaitForSeconds(0.8f);
            }
        }

        private IEnumerator SpawnBossWave()
        {
            SpawnEnemy(-1); // -1 = 보스
            yield return new WaitForSeconds(0.5f);
        }

        private void SpawnEnemy(int waveIndex)
        {
            // TODO: 몬스터 프리팹 생성
            Debug.Log($"[BattleManager] Spawned enemy (wave {waveIndex})");
        }

        private IEnumerator WaitForWaveClear()
        {
            while (_activeEnemies.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void BattleEnd(bool victory)
        {
            state = BattleState.Idle;
            Debug.Log($"[BattleManager] Battle ended: {(victory ? "Victory" : "Defeat")}");

            if (victory)
            {
                // 보상 지급
            }
            else
            {
                // 무한 반복 모드 진입
            }
        }

        /// <summary>
        /// 데미지 계산 (GDD 2.5 공식)
        /// 최종 데미지 = (정령 공격력 × 스킬 계수) × (1 + 속성 상성 보너스) × (1 + 정령사 버프)
        /// </summary>
        public static int CalculateDamage(int attackerATK, float skillMultiplier,
            ElementType attackerElement, ElementType defenderElement,
            float critRate, float critDamage, bool isCrit = false)
        {
            float elementMultiplier = SpiritData.GetAttackElementMultiplier(attackerElement, defenderElement);

            bool crit = isCrit || UnityEngine.Random.value < critRate;
            float critMultiplier = crit ? critDamage : 1f;

            int damage = Mathf.RoundToInt(attackerATK * skillMultiplier * elementMultiplier * critMultiplier);
            return Mathf.Max(1, damage);
        }
    }

    public enum BattleState
    {
        Idle,
        Battling,
        Paused,
        Victory,
        Defeat
    }

    public class SpiritUnit : MonoBehaviour { }
    public class EnemyUnit : MonoBehaviour { }
}

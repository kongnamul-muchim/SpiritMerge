using System.Collections.Generic;
using SpiritMerge;
using SpiritMerge.Core.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 전투 시스템 (v1.1 완전 구현)
    /// SRP: 전투 흐름 + 웨이브 로직 + 데미지 계산
    /// </summary>
    public class BattleService : IBattleService
    {
        public BattleState State { get; private set; } = BattleState.Idle;
        public int CurrentWave { get; private set; }
        public int Region { get; private set; }
        public int Stage { get; private set; }

        // 웨이브당 적 수 (웨이브 진행 시 증가)
        public int EnemiesInWave => Mathf.Min(3 + CurrentWave, 8);
        public int TotalWaves => Stage % 10 == 0 ? 3 : 5; // 보스 스테이지는 3웨이브

        public void StartBattle(int region, int stage)
        {
            if (State == BattleState.Battling) return;

            Region = region;
            Stage = stage;
            CurrentWave = 0;
            State = BattleState.Battling;

            Debug.Log($"[Battle] ⚔️ Region {region}-{stage} started! (Waves: {TotalWaves})");
        }

        public void StopBattle()
        {
            State = BattleState.Idle;
            CurrentWave = 0;
        }

        public void NextWave()
        {
            if (State != BattleState.Battling) return;
            CurrentWave++;

            if (CurrentWave >= TotalWaves)
            {
                State = BattleState.Victory;
                Debug.Log($"[Battle] 🏆 Victory! Region {Region}-{Stage}");
            }
        }

        public void OnSpiritDefeated()
        {
            // 모든 정령 사망 시 패배 (외부에서 카운트 관리)
            State = BattleState.Defeat;
            Debug.Log("[Battle] 💀 Defeat!");
        }

        public float GetBattleProgress()
        {
            if (State != BattleState.Battling && State != BattleState.Victory) return 0f;
            return TotalWaves > 0 ? (float)CurrentWave / TotalWaves : 0f;
        }

        /// <summary>
        /// 이번 웨이브에 등장할 몬스터 속성 목록
        /// </summary>
        public List<MonsterSpawnInfo> GetCurrentWaveSpawns()
        {
            var list = new List<MonsterSpawnInfo>();
            int count = EnemiesInWave;

            for (int i = 0; i < count; i++)
            {
                float hpMul = 1f + (Region - 1) * 0.2f + Stage * 0.05f;
                float atkMul = 1f + (Region - 1) * 0.15f + Stage * 0.04f;

                bool isBoss = (Stage % 10 == 0) && (CurrentWave == TotalWaves - 1) && (i == 0);

                list.Add(new MonsterSpawnInfo
                {
                    element = Region switch
                    {
                        1 => ElementType.Fire,
                        2 => ElementType.Water,
                        3 => ElementType.Wind,
                        4 => ElementType.Earth,
                        5 => ElementType.Dark,
                        6 => ElementType.Light,
                        _ => (ElementType)Random.Range(0, 6)
                    },
                    hpMultiplier = hpMul,
                    atkMultiplier = atkMul,
                    isBoss = isBoss
                });
            }

            return list;
        }

        /// <summary>
        /// 데미지 계산 (정령 → 몬스터)
        /// </summary>
        public static int CalcDamageToEnemy(int atk, float skillMul, ElementType atkElement,
            ElementType defElement, int def, float critRate, float critDmg)
        {
            float elementMul = SpiritData.GetAttackElementMultiplier(atkElement, defElement);
            bool crit = Random.value < critRate;
            float critMul = crit ? critDmg : 1f;
            float defReduction = SpiritData.GetDamageReduction(def);

            float raw = atk * skillMul * elementMul * critMul * (1f - defReduction);
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        /// <summary>
        /// 데미지 계산 (몬스터 → 정령)
        /// </summary>
        public static int CalcDamageToSpirit(int atk, float skillMul, ElementType atkElement,
            ElementType defElement, int def)
        {
            float elementMul = SpiritData.GetDefenseElementMultiplier(atkElement, defElement);
            float defReduction = SpiritData.GetDamageReduction(def);
            float raw = atk * skillMul * elementMul * (1f - defReduction);
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }
    }
}

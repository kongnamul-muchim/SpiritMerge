using System.Collections.Generic;
using SpiritMerge;

namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 전투 시스템 인터페이스
    /// BattleState는 SpiritMerge 네임스페이스에 정의 (BattleManager.cs)
    /// </summary>
    public interface IBattleService
    {
        BattleState State { get; }
        int CurrentWave { get; }
        int Region { get; }
        int Stage { get; }

        void StartBattle(int region, int stage);
        void StopBattle();
        void NextWave();
        void OnSpiritDefeated();

        int EnemiesInWave { get; }
        int TotalWaves { get; }
        float GetBattleProgress();
        System.Collections.Generic.List<MonsterSpawnInfo> GetCurrentWaveSpawns();
    }

    /// <summary>
    /// 몬스터 스폰 정보
    /// </summary>
    public class MonsterSpawnInfo
    {
        public ElementType element;
        public float hpMultiplier = 1f;
        public float atkMultiplier = 1f;
        public bool isBoss;
    }
}

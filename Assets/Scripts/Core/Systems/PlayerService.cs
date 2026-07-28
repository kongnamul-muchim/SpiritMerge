using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 플레이어(정령사) 레벨/스킬트리 서비스
    /// SRP: 플레이어 성장 데이터만 관리
    /// </summary>
    public class PlayerService : IPlayerService
    {
        // 레벨당 필요 경험치 (GDD 5.1)
        private static int RequiredExp(int level) => level switch
        {
            <= 10 => 100 * level,
            <= 20 => 500 * level,
            <= 30 => 2000 * level,
            _ => 5000 * level
        };

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int SkillPoints { get; private set; }

        // [0]:ATK, [1]:DEF, [2]:HP, [3]:SPD, [4~9]:속성강화
        public int[] SkillTreeLevels { get; private set; } = new int[10];

        public void AddExp(int amount)
        {
            Exp += amount;
            while (Exp >= RequiredExp(Level) && Level < 50)
            {
                Exp -= RequiredExp(Level);
                LevelUp();
            }
        }

        public void LevelUp()
        {
            Level++;
            SkillPoints++;
        }

        public bool SpendSkillPoint(int treeIndex)
        {
            if (SkillPoints <= 0 || treeIndex < 0 || treeIndex >= SkillTreeLevels.Length)
                return false;

            SkillTreeLevels[treeIndex]++;
            SkillPoints--;
            return true;
        }

        public int GetPartyATKBonus() => Level +
            (SkillTreeLevels[0] > 0 ? SkillTreeLevels[0] * 5 : 0); // 기본 + 스킬트리

        public int GetPartyDEFBonus() => Level * 2 +
            (SkillTreeLevels[1] > 0 ? SkillTreeLevels[1] * 5 : 0);

        public int GetPartyHPBonus() => Level * 10 +
            (SkillTreeLevels[2] > 0 ? SkillTreeLevels[2] * 10 : 0);

        public float GetPartySPDBonus() =>
            SkillTreeLevels[3] > 0 ? SkillTreeLevels[3] * 0.03f : 0f;
    }
}

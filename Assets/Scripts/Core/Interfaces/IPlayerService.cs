using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 플레이어(정령사) 레벨/스킬트리 관리
    /// </summary>
    public interface IPlayerService
    {
        int Level { get; }
        int Exp { get; }
        int SkillPoints { get; }
        int[] SkillTreeLevels { get; }

        void AddExp(int amount);
        void LevelUp();
        bool SpendSkillPoint(int treeIndex);
        int GetPartyATKBonus();
        int GetPartyDEFBonus();
        int GetPartyHPBonus();
        float GetPartySPDBonus();
    }
}

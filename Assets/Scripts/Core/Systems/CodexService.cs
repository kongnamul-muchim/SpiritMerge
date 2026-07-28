using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 도감 서비스 — 최초 획득 시 영구 스탯 보너스
    /// SRP: 도감 등록/보상만 담당
    /// </summary>
    public class CodexService
    {
        private readonly System.Collections.Generic.HashSet<string> _registered = new();

        public bool IsRegistered(string dataId) => _registered.Contains(dataId);

        /// <summary>
        /// 신규 등록. 첫 등록이면 true + 스탯 보너스 자동 적용.
        /// </summary>
        public bool TryRegister(string dataId, out CodexReward reward)
        {
            reward = null;
            if (_registered.Contains(dataId)) return false;

            _registered.Add(dataId);
            reward = CalculateReward();
            return true;
        }

        public int TotalRegistered => _registered.Count;

        private CodexReward CalculateReward()
        {
            int total = TotalRegistered;
            int atk = 0, def = 0, hp = 0;

            if (total == 10) atk = 20;
            if (total == 20) { atk = 40; def = 20; }
            if (total == 30) { atk = 100; def = 50; hp = 200; }

            // 속성별 완성 보너스 (6속성 × 5종 = 30)
            int elementBonus = (total / 5) * 10; // 속성당 ATK+10

            return new CodexReward { bonusATK = atk + elementBonus, bonusDEF = def, bonusHP = hp };
        }
    }

    public class CodexReward
    {
        public int bonusATK;
        public int bonusDEF;
        public int bonusHP;
    }
}

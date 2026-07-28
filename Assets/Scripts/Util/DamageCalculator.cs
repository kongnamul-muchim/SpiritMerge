using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 데미지 계산 유틸리티 (GDD v1.1)
    /// - 공격: ATK × 스킬계수 × 속성상성 × 치명타 × 버프
    /// - 방어: DEF / (DEF + 500) 비례 피해 감소
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// 공격 최종 데미지
        /// </summary>
        public static int CalculateAttackDamage(
            int attackerATK,
            float skillMultiplier,
            ElementType attackerElement,
            ElementType defenderElement,
            int defenderDEF,
            float critRate,
            float critDamage,
            float playerBuff = 0f)
        {
            // 1. 속성 상성
            float elementMul = SpiritData.GetAttackElementMultiplier(attackerElement, defenderElement);

            // 2. 치명타
            bool isCrit = Random.value < critRate;
            float critMul = isCrit ? critDamage : 1f;

            // 3. 방어력 감소
            float defReduction = SpiritData.GetDamageReduction(defenderDEF);

            // 4. 최종 = ATK × 스킬계수 × 속성상성 × 치명타 × (1+버프) × (1-방어감소)
            float raw = attackerATK * skillMultiplier;
            float final = raw * elementMul * critMul * (1f + playerBuff) * (1f - defReduction);

            return Mathf.Max(1, Mathf.RoundToInt(final));
        }

        /// <summary>
        /// 몬스터가 내 정령에게 가하는 데미지 (방어력 적용)
        /// </summary>
        public static int CalculateReceivedDamage(
            int enemyATK,
            float skillMultiplier,
            ElementType attackerElement,
            ElementType defenderElement,
            int defenderDEF,
            float defenseBuff = 0f)
        {
            float elementMul = SpiritData.GetDefenseElementMultiplier(attackerElement, defenderElement);
            float totalDef = defenderDEF * (1f + defenseBuff);
            float defReduction = SpiritData.GetDamageReduction(Mathf.RoundToInt(totalDef));

            float raw = enemyATK * skillMultiplier;
            float final = raw * elementMul * (1f - defReduction);

            return Mathf.Max(1, Mathf.RoundToInt(final));
        }

        /// <summary>
        /// 머지 필요 마릿수
        /// </summary>
        public static int GetMergeRequiredCount(int currentGrade)
        {
            return currentGrade switch
            {
                1 => 3,
                2 => 3,
                3 => 2,
                4 => 2,
                _ => 0
            };
        }

        /// <summary>
        /// 장비 강화 비용
        /// </summary>
        public static int GetEnhanceCost(int baseCost, int currentLevel)
        {
            return baseCost * (currentLevel + 1) * 500;
        }

        /// <summary>
        /// 장비 강화 성공률
        /// </summary>
        public static float GetEnhanceSuccessRate(int currentLevel)
        {
            return Mathf.Max(0.1f, 1.0f - (currentLevel * 0.1f));
        }
    }
}

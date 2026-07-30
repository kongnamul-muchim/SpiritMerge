using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 전투 상태 관리 + 데미지 계산
    /// 실제 전투 흐름은 WaveController가 처리
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance;

        [Header("배치 위치")]
        public Transform spiritSpawnRoot;
        public Transform enemySpawnRoot;
        public Transform battleField;

        [Header("전투 상태")]
        public BattleState state = BattleState.Idle;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        /// <summary>
        /// 데미지 계산 (GDD 2.5 공식)
        /// 최종 데미지 = 공격력 × 속성보정 × 치명타
        /// </summary>
        public static int CalculateDamage(int attackerATK, float skillMultiplier,
            ElementType attackerElement, ElementType defenderElement,
            float critRate, float critDamage, bool isCrit = false)
        {
            float elementMultiplier = SpiritData.GetAttackElementMultiplier(attackerElement, defenderElement);

            bool crit = isCrit || Random.value < critRate;
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
}

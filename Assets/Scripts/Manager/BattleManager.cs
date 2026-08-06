using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpiritMerge
{
    /// <summary>
    /// 전투 상태 관리 + 데미지 계산 + 파티 통합 HP
    /// 실제 전투 흐름은 WaveController가 처리
    /// ⭐ 파티 통합 HP: 정령 개별이 아니라 파티 전체가 하나의 체력 풀을 공유
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        // ⭐ 도메인 리로드(재컴파일) 후에도 staticMatch 유지 — null이면 씬에서 자동 재연결
        private static BattleManager _instance;
        public static BattleManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = UnityEngine.Object.FindAnyObjectByType<BattleManager>();
                return _instance;
            }
            private set { _instance = value; }
        }

        [Header("배치 위치")]
        public Transform spiritSpawnRoot;
        public Transform enemySpawnRoot;
        public Transform battleField;

        [Header("전투 상태")]
        public BattleState state = BattleState.Idle;

        [Header("파티 통합 HP")]
        public int partyMaxHP = 0;   // 최대 파티 체력 (정령 HP 합 × 시너지 + 업그레이드)
        public int partyHP = 0;      // 현재 파티 체력
        public int partyDEF = 0;     // 파티 방어력 (정령 DEF 합 + 업그레이드)
        public Slider partyHpSlider; // 통합 HP바 UI
        public TextMeshProUGUI partyHpText;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// 파티 통합 HP 설정 (전투 배치/재시작 시 호출)
        /// </summary>
        public void SetupParty(int maxHP, int def)
        {
            partyMaxHP = maxHP;
            partyHP = maxHP;
            partyDEF = def;
            UpdatePartyBar();
        }

        /// <summary>파티 통합 HP 감소 (적 공격)</summary>
        public void DamageParty(int damage)
        {
            int finalDmg = Mathf.Max(1, damage - partyDEF);
            partyHP = Mathf.Max(0, partyHP - finalDmg);
            UpdatePartyBar();
        }

        /// <summary>파티 통합 HP 회복 (빛 회복/어둠 흡혈/스테이지 클리어)</summary>
        public void HealParty(int amount)
        {
            if (partyMaxHP <= 0) return;
            partyHP = Mathf.Min(partyMaxHP, partyHP + amount);
            UpdatePartyBar();
        }

        public bool IsPartyDead => partyMaxHP > 0 && partyHP <= 0;

        /// <summary>통합 HP바 UI 갱신</summary>
        public void UpdatePartyBar()
        {
            if (partyHpSlider != null)
            {
                partyHpSlider.maxValue = partyMaxHP;
                partyHpSlider.value = partyHP;
            }
            if (partyHpText != null)
                partyHpText.text = $"{partyHP} / {partyMaxHP}";
        }

        /// <summary>
        /// 데미지 계산 (GDD 2.5 공식) — ⭐ 아군 정령 공격에만 속성 상성 적용
        /// 적 몬스터는 상성 없이 공격 (Monster.TryAttack이 상성 없이 파티 HP 공격)
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

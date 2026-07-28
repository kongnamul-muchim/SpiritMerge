using UnityEngine;

namespace SpiritMerge.Presentation.Battle
{
    /// <summary>
    /// 전투 중 정령 유닛 — 자동 공격 + 피격
    /// </summary>
    public class SpiritUnit : MonoBehaviour
    {
        [SerializeField] private int atk = 10;
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int def = 5;
        [SerializeField] private float speed = 2f;
        [SerializeField] private ElementType element;

        private int _currentHp;
        private float _attackTimer;
        private bool _isAlive = true;

        public int CurrentHp => _currentHp;
        public int MaxHp => maxHp;
        public bool IsAlive => _isAlive;
        public ElementType Element => element;

        private void Awake()
        {
            _currentHp = maxHp;
        }

        private void Update()
        {
            if (!_isAlive) return;

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= speed)
            {
                _attackTimer = 0f;
                OnAutoAttack();
            }
        }

        private void OnAutoAttack()
        {
            // BattleController가 이 이벤트를 받아서 적에게 데미지
        }

        /// <summary>
        /// 몬스터에게 데미지 받음
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!_isAlive) return;

            _currentHp -= damage;
            Debug.Log($"[Spirit] {gameObject.name} took {damage} damage (HP: {_currentHp}/{maxHp})");

            if (_currentHp <= 0)
            {
                _currentHp = 0;
                _isAlive = false;
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// SpiritData 기반 초기화
        /// </summary>
        public void Initialize(SpiritData data)
        {
            atk = data.FinalATK;
            maxHp = data.FinalHP;
            def = data.FinalDEF;
            speed = data.FinalSPD;
            element = data.element;
            _currentHp = maxHp;
            _isAlive = true;
            gameObject.SetActive(true);
        }
    }
}

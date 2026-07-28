using SpiritMerge.Core.Interfaces;
using UnityEngine;

namespace SpiritMerge.Presentation.Battle
{
    /// <summary>
    /// 전투 중 몬스터 유닛 — 웨이브마다 생성
    /// </summary>
    public class EnemyUnit : MonoBehaviour
    {
        [SerializeField] private int atk = 15;
        [SerializeField] private int maxHp = 80;
        [SerializeField] private int def = 5;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private ElementType element;

        private int _currentHp;
        private float _attackTimer;
        private bool _isAlive = true;
        private Transform _target;

        public bool IsAlive => _isAlive;
        public int ATK => atk;
        public int DEF => def;
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
                OnAttack();
            }
        }

        private void OnAttack()
        {
            // BattleController가 정령에게 데미지 전달
        }

        /// <summary>
        /// 정령에게 데미지 받음
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!_isAlive) return;

            _currentHp -= damage;
            Debug.Log($"[Enemy] {gameObject.name} took {damage} damage (HP: {_currentHp}/{maxHp})");

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            _currentHp = 0;
            _isAlive = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 스폰 정보 기반 초기화
        /// </summary>
        public void Initialize(MonsterSpawnInfo info, int baseATK, int baseHP, int baseDEF)
        {
            atk = Mathf.RoundToInt(baseATK * info.atkMultiplier);
            maxHp = Mathf.RoundToInt(baseHP * info.hpMultiplier);
            def = baseDEF;
            element = info.element;
            _currentHp = maxHp;
            _isAlive = true;
            gameObject.SetActive(true);
        }
    }
}

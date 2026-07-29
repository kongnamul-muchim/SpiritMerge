using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SpiritMerge;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// 정령 전투 유닛 — 자동 공격, HP, 스프라이트 표시
    /// </summary>
    public class SpiritUnit : MonoBehaviour
    {
        [Header("데이터")]
        public SpiritData data;
        public int currentSlot;

        [Header("전투")]
        public int hp;
        public int maxHp;
        public int atk;
        public int def;
        public float atkSpeed;
        public float critRate;
        public float critDmg;
        public ElementType element;

        [Header("참조")]
        public SpriteRenderer spriteRenderer;
        public Image hpBarFill;

        [Header("상태")]
        public bool isAlive = true;

        private Monster _target;
        private float _lastAtkTime;

        /// <summary>
        /// SpiritData로 초기화
        /// </summary>
        public void Initialize(SpiritData spiritData)
        {
            data = spiritData;
            element = spiritData.element;

            maxHp = spiritData.FinalHP;
            hp = maxHp;
            atk = spiritData.FinalATK;
            def = spiritData.FinalDEF;
            atkSpeed = spiritData.FinalSPD;
            critRate = spiritData.FinalCRIT;
            critDmg = spiritData.FinalCritDMG;

            // 스프라이트
            if (spriteRenderer != null && spiritData.sprite != null)
                spriteRenderer.sprite = spiritData.sprite;

            UpdateHpBar();
            isAlive = true;
        }

        void Update()
        {
            if (!isAlive || BattleManager.Instance?.state != BattleState.Battling)
                return;

            // 공격 타이머
            _lastAtkTime += Time.deltaTime;
            if (_lastAtkTime >= atkSpeed)
            {
                _lastAtkTime = 0f;
                TryAttack();
            }
        }

        void TryAttack()
        {
            // 가장 가까운 살아있는 적 찾기
            _target = FindNearestEnemy();
            if (_target == null || !_target.isAlive) return;

            // 데미지 계산
            int dmg = BattleManager.CalculateDamage(
                atk, 1f,
                element, _target.data?.element ?? 0,
                critRate, critDmg
            );

            _target.TakeDamage(dmg);
        }

        Monster FindNearestEnemy()
        {
            Monster closest = null;
            float minDist = float.MaxValue;
            var enemies = FindObjectsOfType<Monster>();
            foreach (var e in enemies)
            {
                if (!e.isAlive) continue;
                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = e;
                }
            }
            return closest;
        }

        public void TakeDamage(int damage)
        {
            if (!isAlive) return;
            int finalDmg = Mathf.Max(1, damage - def);
            hp -= finalDmg;
            UpdateHpBar();
            if (hp <= 0) Die();
        }

        void Die()
        {
            isAlive = false;
            Destroy(gameObject, 0.3f);
        }

        void UpdateHpBar()
        {
            if (hpBarFill != null)
                hpBarFill.fillAmount = (float)hp / maxHp;
        }
    }
}

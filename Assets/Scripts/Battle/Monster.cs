using UnityEngine;
using UnityEngine.UI;
using SpiritMerge.Data;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// 몬스터 전투 유닛 — MonsterData에서 스탯을 로드하고 SpriteRenderer로 표시
    /// </summary>
    public class Monster : MonoBehaviour
    {
        [Header("데이터")]
        public MonsterData data;
        public int hp;
        public int maxHp;
        public int atk;
        public int def;
        public float speed;

        [Header("참조")]
        public SpriteRenderer spriteRenderer;
        public Image hpBarFill;
        public GameObject assignedSpawnPoint;
        public Canvas hpBarCanvas;

        [Header("상태")]
        public bool isAlive = true;
        public bool isBoss = false;

        private float _lastAttackTime;

        /// <summary>
        /// MonsterData로 초기화
        /// </summary>
        public void Initialize(MonsterData monsterData, bool boss = false)
        {
            data = monsterData;
            isBoss = boss;

            // 스탯
            maxHp = monsterData.baseHP;
            if (boss) maxHp *= 10;
            hp = maxHp;
            atk = monsterData.baseATK;
            def = monsterData.baseDEF;
            speed = monsterData.baseSpeed;

            // 스프라이트
            if (spriteRenderer != null && monsterData.sprite != null)
                spriteRenderer.sprite = monsterData.sprite;

            // HP바 업데이트
            UpdateHpBar();

            gameObject.name = boss ? $"Boss_{monsterData.name}" : monsterData.name;
            isAlive = true;
        }

        public void TakeDamage(int damage)
        {
            if (!isAlive) return;
            int finalDmg = Mathf.Max(1, damage - def);
            hp -= finalDmg;
            UpdateHpBar();

            if (hp <= 0) Die();
        }

        private void Die()
        {
            isAlive = false;
            // SpawnPoint 반환
            var spawner = FindObjectOfType<MonsterSpawner>();
            if (spawner != null && assignedSpawnPoint != null)
                spawner.ReturnSpawnPoint(assignedSpawnPoint);

            // WaveController에 알림
            var waveCtrl = FindObjectOfType<WaveController>();
            waveCtrl?.OnMonsterKilled();

            Destroy(gameObject, 0.3f);
        }

        private void UpdateHpBar()
        {
            if (hpBarFill != null)
                hpBarFill.fillAmount = (float)hp / maxHp;
        }
    }
}

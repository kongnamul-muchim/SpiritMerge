using UnityEngine;
using UnityEngine.UI;
using SpiritMerge.Data;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// 몬스터 전투 유닛 — MonsterData에서 스탯 로드
    /// 슬롯 모드(slotMode): EnemySlot UI 객체를 재사용 (자체 Image로 스프라이트 표시, Die 시 파괴 대신 비활성화)
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
        public SpriteRenderer spriteRenderer;   // 레거시(프리팹) 전용
        public Image displayImage;              // 슬롯 모드: 슬롯 자체 Image
        public Slider hpSlider;                 // HP바 (HPBar Slider, value로 조절)
        public Slider cdSlider;                 // 공격 쿨타임 바 (CDBar Slider)
        public Canvas hpBarCanvas;

        [Header("슬롯 모드")]
        public bool slotMode;                   // EnemySlot 재사용 여부
        public MonsterSpawner spawner;          // 슬롯 반환용 (slotMode)

        [Header("상태")]
        public bool isAlive = true;
        public bool isBoss = false;

        private SpiritUnit _target;
        private float _attackTimer;             // 공격 쿨타임 경과 시간
        private Coroutine _attackMotion;        // ⭐ 필드 저장 — StopCoroutine이 실제 실행 중인 코루틴을 멈추게

        /// <summary>
        /// MonsterData로 초기화
        /// </summary>
        public void Initialize(MonsterData monsterData, bool boss = false,
            float hpMultiplier = 1f, float atkMultiplier = 1f)
        {
            data = monsterData;
            isBoss = boss;

            // 스탯 (스테이지 배율 적용)
            maxHp = Mathf.RoundToInt(monsterData.baseHP * hpMultiplier);
            if (boss) maxHp *= 10;
            hp = maxHp;
            atk = Mathf.RoundToInt(monsterData.baseATK * atkMultiplier);
            def = monsterData.baseDEF;
            speed = monsterData.baseSpeed;

            // ⭐ slotMode: 몬스터 이미지를 자식 Icon으로 분리 (공격 모션 시 이미지만 스케일)
            if (slotMode) EnsureIcon();

            // 스프라이트: 슬롯 모드는 자체 Image, 레거시는 SpriteRenderer
            if (monsterData.sprite != null)
            {
                if (displayImage != null) { displayImage.sprite = monsterData.sprite; displayImage.preserveAspect = true; }
                if (spriteRenderer != null) spriteRenderer.sprite = monsterData.sprite;
            }

            // 슬롯 모드에서는 이름 변경 금지 (EnemySlot_0 등 유지)
            if (!slotMode)
                gameObject.name = boss ? $"Boss_{monsterData.name}" : monsterData.name;

            // HP/CD바 value 초기화 (Slider 기반)
            ResetBars();

            _attackTimer = 0f;
            UpdateHpBar();
            UpdateCdBar();
            isAlive = true;
        }

        void Update()
        {
            if (!isAlive || BattleManager.Instance?.state != BattleState.Battling)
                return;

            // ⭐ 대상(정령)이 있을 때만 공격 게이지가 차오름
            //    대상이 없으면 게이지를 0으로 유지 (적이 없는데 차오르던 버그 수정)
            _target = FindNearestSpirit();
            if (_target == null || !_target.isAlive)
            {
                _attackTimer = 0f;
                UpdateCdBar();
                return;
            }

            // 공격 타이머: 쿨타임(speed)이 차면 공격 후 0으로 리셋
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= speed)
            {
                _attackTimer = 0f;
                TryAttack();
            }
            UpdateCdBar();
        }

        void TryAttack()
        {
            // 공격 대상(정령)이 있는지만 확인 (정령은 개별로 죽지 않음)
            _target = FindNearestSpirit();
            if (_target == null || !_target.isAlive) return;

            // ⭐ 적 공격은 파티 통합 HP에 적용, 속성 상성 없이 (아군만 상성 보너스)
            if (BattleManager.Instance != null)
                BattleManager.Instance.DamageParty(atk);
            PlayAttackMotion(); // ⭐ 공격 모션
        }

        /// <summary>공격 시 스케일 펄스 모션 — ⭐ 몬스터 이미지(Icon)만 스케일
        /// ⭐ 코루틴 필드 저장 — StopCoroutine(새 인스턴스)는 실행 중인 코루틴을 못 멈춰
        ///    공격이 겹치면 localScale이 무한 증폭(2~4배)되던 버그 수정</summary>
        void PlayAttackMotion()
        {
            if (_attackMotion != null) StopCoroutine(_attackMotion);
            _attackMotion = StartCoroutine(AttackMotionRoutine());
        }

        System.Collections.IEnumerator AttackMotionRoutine()
        {
            Transform target = displayImage != null ? displayImage.transform : transform;
            float dur = 0.16f;
            float t = 0f;
            // ⭐ 절대값 기준 — 이전 공격이 중간에 멈춰 localScale이 커진 채 남아도 누적되지 않음 (스케일 6 6 6 방지)
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.PingPong(t / dur, 1f);
                target.localScale = Vector3.one * (1f + 0.22f * k);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        /// <summary>
        /// ⭐ 슬롯 모드: 몬스터 이미지 전용 Icon 오브젝트 분리 (HP/CD바와 분리)
        /// </summary>
        void EnsureIcon()
        {
            var slotImg = GetComponent<Image>();
            if (slotImg != null) slotImg.color = new Color(0, 0, 0, 0);

            var icon = transform.Find("MonsterIcon");
            if (icon == null)
            {
                var go = new GameObject("MonsterIcon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                go.transform.SetAsFirstSibling();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.03f, 0.13f);   // ⭐ 50% 확대
                rt.anchorMax = new Vector2(0.97f, 0.95f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                displayImage = go.GetComponent<Image>();
                displayImage.preserveAspect = true;
            }
            else
            {
                displayImage = icon.GetComponent<Image>();
            }
        }

        SpiritUnit FindNearestSpirit()
        {
            SpiritUnit closest = null;
            float minDist = float.MaxValue;
            var spirits = FindObjectsByType<SpiritUnit>();
            foreach (var s in spirits)
            {
                if (!s.isAlive) continue;
                float dist = Vector3.Distance(transform.position, s.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = s;
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

        private void Die()
        {
            // ⭐ 공격 모션 중 사망하면 localScale이 커진 채 고정되던 문제 — 스케일 리셋
            if (_attackMotion != null) StopCoroutine(_attackMotion);
            if (displayImage != null) displayImage.transform.localScale = Vector3.one;
            transform.localScale = Vector3.one;

            isAlive = false;

            // 💰 처치 보상 (골드 + 경험치 — 업그레이드 보너스 적용)
            if (GameManager.Instance != null && data != null)
            {
                int reward = isBoss ? data.goldReward * 5 : data.goldReward;
                GameManager.Instance.AddBattleGold(reward);
                GameManager.Instance.AddPlayerExp(data.expReward);
                GameLogger.Info($"[Monster] 💰 {gameObject.name} 처치! 골드 +{reward}, 경험치 +{data.expReward}");
            }

            // ⭐ 미션 진행도 (몬스터 처치)
            GameManager.Instance?.OnMonsterKilled();

            // WaveController에 알림
            var waveCtrl = FindAnyObjectByType<WaveController>();
            waveCtrl?.OnMonsterKilled();

            if (slotMode)
            {
                // 슬롯 모드: 파괴 대신 슬롯 비활성화 + 스폰 큐 반환
                if (spawner != null) spawner.ReturnEnemySlot(gameObject);
                else gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject, 0.3f);
            }
        }

        private void UpdateHpBar()
        {
            if (hpSlider != null)
                hpSlider.value = maxHp > 0 ? (float)hp / maxHp : 0f;
        }

        private void UpdateCdBar()
        {
            if (cdSlider != null)
                cdSlider.value = speed > 0 ? _attackTimer / speed : 0f;
        }

        /// <summary>
        /// 슬롯 재사용 시 Slider 값 초기화 (사망한 몬스터의 잔상 방지)
        /// </summary>
        private void ResetBars()
        {
            if (hpSlider != null) { hpSlider.minValue = 0f; hpSlider.maxValue = 1f; }
            if (cdSlider != null) { cdSlider.minValue = 0f; cdSlider.maxValue = 1f; }
        }
    }
}

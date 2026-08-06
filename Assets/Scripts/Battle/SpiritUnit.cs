using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SpiritMerge;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// 정령 전투 유닛 — 자동 공격, HP, 스프라이트 표시
    /// 슬롯 모드(slotMode): SpiritSlot UI 객체를 재사용 (자체 Image로 표시, Die 시 비활성화)
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
        public SpriteRenderer spriteRenderer;   // 레거시(프리팹) 전용
        public Image displayImage;              // 슬롯 모드: 슬롯 자체 Image
        public Slider hpSlider;                 // HP바 (HPBar Slider, value로 조절)
        public Slider cdSlider;                 // 공격 쿨타임 바 (CDBar Slider)

        [Header("슬롯 모드")]
        public bool slotMode;                   // SpiritSlot 재사용 여부

        [Header("상태")]
        public bool isAlive = true;

        private Monster _target;
        private float _lastAtkTime;
        private int _level; // ⭐ 머지 성급(level) — 스탯/공격방식 계산용 (0이면 grade 기준)
        private Coroutine _attackMotion; // ⭐ 공격 모션 참조 — 중복 실행/중단 시 스케일 오염 방지

        /// <summary>
        /// SpiritData로 초기화
        /// </summary>
        /// <param name="spiritData">정령 데이터</param>
        /// <param name="level">레벨 (⭐ 성급=level — 스탯/스프라이트 반영, 0이면 grade 기준)</param>
        public void Initialize(SpiritData spiritData, int level = 0)
        {
            data = spiritData;
            element = spiritData.element;
            _level = level;

            // ⭐ 성급(=level) 기반 스탯 — 머지로 성급이 올라가면 전투력이 실제로 증가
            bool useLevel = level > 0;
            maxHp = useLevel ? spiritData.FinalHPAt(level) : spiritData.FinalHP;
            hp = maxHp;
            atk = useLevel ? spiritData.FinalATKAt(level) : spiritData.FinalATK;
            def = useLevel ? spiritData.FinalDEFAt(level) : spiritData.FinalDEF;
            atkSpeed = useLevel ? spiritData.FinalSPDAt(level) : spiritData.FinalSPD;
            critRate = useLevel ? spiritData.FinalCRITAt(level) : spiritData.FinalCRIT;
            critDmg = useLevel ? spiritData.FinalCritDMGAt(level) : spiritData.FinalCritDMG;

            // ⭐ slotMode: 정령 이미지를 자식 Icon으로 분리 (공격 모션 시 이미지만 스케일, 바는 유지)
            if (slotMode) EnsureIcon();

            // ⭐ 재배치/재초기화 시 공격 모션 중단 흔적 제거 — 스케일 오염(커진 채 유지) 방지 (Monster와 동일 패턴)
            if (_attackMotion != null) { StopCoroutine(_attackMotion); _attackMotion = null; }
            if (displayImage != null) displayImage.transform.localScale = Vector3.one;

            // 스프라이트: ⭐ 레벨에 맞는 스프라이트 우선 (머지보드/편성 UI와 동일 기준)
            Sprite lvSprite = level > 0 ? SpiritData.ResolveLevelSprite(element, level) : null;
            if (lvSprite != null)
            {
                if (displayImage != null) { displayImage.sprite = lvSprite; displayImage.preserveAspect = true; }
                if (spriteRenderer != null) spriteRenderer.sprite = lvSprite;
            }
            else if (spiritData.sprite != null)
            {
                if (displayImage != null) { displayImage.sprite = spiritData.sprite; displayImage.preserveAspect = true; }
                if (spriteRenderer != null) spriteRenderer.sprite = spiritData.sprite;
            }

            // HP/CD바 value 초기화 (Slider 기반)
            ResetBars();

            _lastAtkTime = 0f;
            UpdateHpBar();
            UpdateCdBar();
            isAlive = true;
        }

        void Update()
        {
            if (!isAlive || BattleManager.Instance?.state != BattleState.Battling)
                return;

            // ⭐ 대상(적)이 있을 때만 공격 게이지가 차오름
            //    적이 없으면 게이지를 0으로 유지 (적이 없는데 차오르던 버그 수정)
            _target = FindNearestEnemy();
            if (_target == null || !_target.isAlive)
            {
                _lastAtkTime = 0f;
                UpdateCdBar();
                return;
            }

            // 공격 타이머
            _lastAtkTime += Time.deltaTime;
            if (_lastAtkTime >= atkSpeed)
            {
                _lastAtkTime = 0f;
                TryAttack();
            }
            UpdateCdBar();
        }

        void TryAttack()
        {
            // Update에서 갱신된 대상이 유효하지 않으면 재탐색
            if (_target == null || !_target.isAlive)
            {
                _target = FindNearestEnemy();
                if (_target == null || !_target.isAlive) return;
            }

            // 데미지 계산
            int dmg = BattleManager.CalculateDamage(
                atk, 1f,
                element, _target.data?.element ?? 0,
                critRate, critDmg
            );

            // ⭐ 속성별 공격방식 (등급별 수치 — 도감 표기와 일치)
            switch (element)
            {
                case ElementType.Water:
                    // 💧 스플래시: 대상 100% + ⭐ 좌/우 1칸 슬롯의 적에게 (등급별 20/40/60/80/100%)
                    {
                        int grade = _level > 0 ? _level : (data != null ? (int)data.grade : 1);
                        float splash = SplashFor(grade);
                        _target.TakeDamage(dmg);
                        int targetIdx = SlotIndexOf(_target);
                        foreach (var e in FindObjectsByType<Monster>())
                        {
                            if (!e.isAlive || e == _target) continue;
                            int eIdx = SlotIndexOf(e);
                            if (eIdx == targetIdx - 1 || eIdx == targetIdx + 1)
                                e.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg * splash)));
                        }
                    }
                    break;
                case ElementType.Earth:
                    // 🌍 광역: 모든 적 (등급별 30/50/70/90/100%)
                    {
                        int grade = _level > 0 ? _level : (data != null ? (int)data.grade : 1);
                        float aoe = AoeFor(grade);
                        foreach (var e in FindObjectsByType<Monster>())
                            if (e.isAlive)
                                e.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(dmg * aoe)));
                    }
                    break;
                default:
                    // 단일 (불 강공 / 바람 속공은 스탯으로 표현)
                    _target.TakeDamage(dmg);
                    break;
            }

            // ⭐ 어둠 시너지: 공격 데미지 %만큼 파티 HP 흡혈
            if (GameManager.Instance != null) GameManager.Instance.HealPartyByVamp(dmg);
            PlayAttackMotion(); // ⭐ 공격 모션
        }

        /// <summary>
        /// 공격 시 스케일 펄스 모션 — ⭐ 정령 이미지(Icon)만 스케일, HP/CD바는 유지
        /// ⭐ 코루틴 참조 저장 + 절대값 기준(누적 방지) — Monster와 동일 패턴
        /// </summary>
        void PlayAttackMotion()
        {
            if (_attackMotion != null) StopCoroutine(_attackMotion);
            _attackMotion = StartCoroutine(AttackMotionRoutine());
        }

        System.Collections.IEnumerator AttackMotionRoutine()
        {
            Transform target = displayImage != null ? displayImage.transform : transform;
            // ⭐ 절대값 기준 (Icon은 1, 레거시 transform 폴백은 원본) — 이전 펄스가 남아도 누적되지 않음
            Vector3 baseScale = displayImage != null ? Vector3.one : transform.localScale;
            float dur = 0.16f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.PingPong(t / dur, 1f);
                target.localScale = baseScale * (1f + 0.22f * k);
                yield return null;
            }
            target.localScale = baseScale;
        }

        Monster FindNearestEnemy()
        {
            Monster closest = null;
            float minDist = float.MaxValue;
            var enemies = FindObjectsByType<Monster>();
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

        /// <summary>몬스터의 EnemySlot 인덱스 (이름 "EnemySlot_2" → 2)</summary>
        static int SlotIndexOf(Monster m)
        {
            if (m == null) return -1;
            string name = m.name;
            int i = name.LastIndexOf('_');
            if (i >= 0 && int.TryParse(name.Substring(i + 1), out int v)) return v;
            return -1;
        }

        /// <summary>물 스플래시 좌우 데미지 % (1~5성: 20/40/60/80/100)</summary>
        static float SplashFor(int grade) => grade switch
        {
            1 => 0.2f, 2 => 0.4f, 3 => 0.6f, 4 => 0.8f, _ => 1.0f
        };

        /// <summary>대지 광역 데미지 % (1~5성: 30/50/70/90/100)</summary>
        static float AoeFor(int grade) => grade switch
        {
            1 => 0.3f, 2 => 0.5f, 3 => 0.7f, 4 => 0.9f, _ => 1.0f
        };

        public void TakeDamage(int damage)
        {
            if (!isAlive) return;
            // ⭐ 파티 통합 HP: 정령 개별이 아니라 파티 체력이 깎임 (정령은 죽지 않음)
            if (BattleManager.Instance != null)
                BattleManager.Instance.DamageParty(damage);
        }

        void Die()
        {
            isAlive = false;
            if (slotMode)
                gameObject.SetActive(false);   // 슬롯 재사용: 파괴 대신 비활성화
            else
                Destroy(gameObject, 0.3f);
        }

        void UpdateHpBar()
        {
            if (hpSlider != null)
                hpSlider.value = maxHp > 0 ? (float)hp / maxHp : 0f;
        }

        void UpdateCdBar()
        {
            if (cdSlider != null)
                cdSlider.value = atkSpeed > 0 ? _lastAtkTime / atkSpeed : 0f;
        }

        /// <summary>
        /// 슬롯 재사용 시 Slider 값 초기화 (사망한 정령의 잔상 방지)
        /// </summary>
        void ResetBars()
        {
            if (hpSlider != null) { hpSlider.minValue = 0f; hpSlider.maxValue = 1f; }
            if (cdSlider != null) { cdSlider.minValue = 0f; cdSlider.maxValue = 1f; }
        }

        /// <summary>
        /// ⭐ 슬롯 모드: 정령 이미지 전용 Icon 오브젝트 분리
        /// - 슬롯 자체 Image는 투명 배경, 정령 스프라이트는 Icon에 표시
        /// - 공격 모션(스케일)이 Icon에만 적용 → HP/CD바는 안 커짐
        /// </summary>
        void EnsureIcon()
        {
            var slotImg = GetComponent<Image>();
            if (slotImg != null) slotImg.color = new Color(0, 0, 0, 0);

            var icon = transform.Find("SpiritIcon");
            if (icon == null)
            {
                var go = new GameObject("SpiritIcon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                go.transform.SetAsFirstSibling(); // 바(CD)보다 아래에 배치
                var rt = go.GetComponent<RectTransform>();
                // ⭐ 정령 이미지 확대: 슬롯보다 20~30% 크게 (위아래 살짝 벗어남)
                rt.anchorMin = new Vector2(0.0f, -0.06f);
                rt.anchorMax = new Vector2(1.0f, 1.06f);
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
    }
}

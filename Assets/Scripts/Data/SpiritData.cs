using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 정령 기본 데이터 ScriptableObject
    /// GDD v1.1 — DEF 스탯 추가, 멀티플라이어 로직 개선
    /// </summary>
    [CreateAssetMenu(fileName = "SpiritData", menuName = "Game/SpiritData")]
    public class SpiritData : ScriptableObject
    {
        [Header("기본 정보")]
        public string spiritName;         // 정령 이름 (ex: 불꽃늑대)
        public SpiritGrade grade;         // 등급 (1~5)
        public ElementType element;       // 속성
        public AnimalType animalType;     // 동물 계열 (1~2성은 None)
        public string description;        // 정령 설명
        public string spriteFileName;     // 스프라이트 파일명 (확장자 제외)

        [Header("기본 스탯 (1성 기준)")]
        public int baseATK;
        public int baseHP;
        public int baseDEF;
        public float baseSpeed = 2.0f;       // 공격 간격(초). 낮을수록 빠름
        [Range(0f, 1f)] public float baseCritRate = 0.05f;
        public float baseCritDamage = 1.5f;

        [Header("스킬")]
        public SkillData activeSkill;     // 액티브 스킬
        public SkillData passiveSkill;    // 패시브 스킬

        [Header("리소스")]
        public Sprite sprite;             // 정령 이미지
        public Sprite iconSprite;         // UI용 아이콘
        public RuntimeAnimatorController animatorController;

        // ──────────────────────────────────────────────
        // 등급별 보정값 적용 (읽기 전용 프로퍼티)
        // ──────────────────────────────────────────────

        private static (float atkHpDef, float critSpeed) GetMultiplier(SpiritGrade g)
        {
            return g switch
            {
                SpiritGrade.OneStar   => (1.0f, 1.0f),
                SpiritGrade.TwoStar   => (1.5f, 1.15f),
                SpiritGrade.ThreeStar => (2.5f, 1.3f),
                SpiritGrade.FourStar  => (4.0f, 1.5f),
                SpiritGrade.FiveStar  => (6.5f, 1.8f),
                _ => (1.0f, 1.0f)
            };
        }

        public int    FinalATK  => Mathf.RoundToInt(baseATK  * GetMultiplier(grade).atkHpDef);
        public int    FinalHP   => Mathf.RoundToInt(baseHP   * GetMultiplier(grade).atkHpDef);
        public int    FinalDEF  => Mathf.RoundToInt(baseDEF  * GetMultiplier(grade).atkHpDef);
        public float  FinalSPD  => baseSpeed / GetMultiplier(grade).critSpeed; // 빠를수록 짧은 간격
        public float  FinalCRIT => baseCritRate * GetMultiplier(grade).critSpeed;
        public float  FinalCritDMG => baseCritDamage * GetMultiplier(grade).critSpeed;

        // ──────────────────────────────────────────────
        // ⭐ 성급(=머지 레벨) 기반 스탯 — 게임 내 실제 성급은 SpiritItemData.level
        //    머지로 성급이 올라가면 전투력/전투 스탯이 이 값을 사용한다
        // ──────────────────────────────────────────────

        /// <summary>머지 레벨(1~5+) → 성급 등급 매핑 (Lv.1=1성 ... Lv.5+=5성)</summary>
        public static SpiritGrade GradeOfLevel(int level) => level switch
        {
            1 => SpiritGrade.OneStar,
            2 => SpiritGrade.TwoStar,
            3 => SpiritGrade.ThreeStar,
            4 => SpiritGrade.FourStar,
            _ => SpiritGrade.FiveStar // Lv.5+
        };

        public int    FinalATKAt(int level)   => Mathf.RoundToInt(baseATK  * GetMultiplier(GradeOfLevel(level)).atkHpDef);
        public int    FinalHPAt(int level)    => Mathf.RoundToInt(baseHP   * GetMultiplier(GradeOfLevel(level)).atkHpDef);
        public int    FinalDEFAt(int level)   => Mathf.RoundToInt(baseDEF  * GetMultiplier(GradeOfLevel(level)).atkHpDef);
        public float  FinalSPDAt(int level)   => baseSpeed / GetMultiplier(GradeOfLevel(level)).critSpeed;
        public float  FinalCRITAt(int level)  => baseCritRate * GetMultiplier(GradeOfLevel(level)).critSpeed;
        public float  FinalCritDMGAt(int level) => baseCritDamage * GetMultiplier(GradeOfLevel(level)).critSpeed;

        public static float GetMultiplierAtkHpDef(SpiritGrade g) => GetMultiplier(g).atkHpDef;
        public static float GetMultiplierCritSpeed(SpiritGrade g) => GetMultiplier(g).critSpeed;

        /// <summary>
        /// 공격 시 속성 상성 계수
        /// 강함 +25%, 약함 -15%, 동일 0%
        /// </summary>
        public static float GetAttackElementMultiplier(ElementType attacker, ElementType defender)
        {
            // 강함 (1.25배): 불 > 자연, 물 > 불, 자연 > 물, 번개 > 물
            if (attacker == ElementType.Fire  && defender == ElementType.Earth) return 1.25f;
            if (attacker == ElementType.Water && defender == ElementType.Fire)  return 1.25f;
            if (attacker == ElementType.Earth && defender == ElementType.Water) return 1.25f;
            if (attacker == ElementType.Wind  && defender == ElementType.Water) return 1.25f;
            if (attacker == ElementType.Dark  && defender == ElementType.Light) return 1.25f;
            if (attacker == ElementType.Light && defender == ElementType.Dark)  return 1.25f;

            // 약함 (0.85배): 불 < 물, 물 < 자연, 자연 < 불, 번개 < 자연
            if (attacker == ElementType.Fire  && defender == ElementType.Water) return 0.85f;
            if (attacker == ElementType.Water && defender == ElementType.Earth) return 0.85f;
            if (attacker == ElementType.Earth && defender == ElementType.Fire)  return 0.85f;
            if (attacker == ElementType.Wind  && defender == ElementType.Earth) return 0.85f;
            if (attacker == ElementType.Dark  && defender == ElementType.Dark)  return 0.85f;
            if (attacker == ElementType.Light && defender == ElementType.Light) return 0.85f;

            return 1.0f;
        }

        /// <summary>
        /// 방어 시 속성 상성 계수 (받는 피해)
        /// </summary>
        public static float GetDefenseElementMultiplier(ElementType attacker, ElementType defender)
        {
            // 공격 시의 역수 관계와 동일 (약한 속성에게 더 맞음)
            return GetAttackElementMultiplier(attacker, defender);
        }

        /// <summary>
        /// 방어력 기반 피해 감소율 (GDD v1.1)
        /// 감소율 = DEF / (DEF + 500)
        /// </summary>
        public static float GetDamageReduction(int def)
        {
            return def / (def + 500f);
        }

        // ──────────────────────────────────────────────
        // 레벨별 스프라이트 해석 (머지보드/편성 UI 공용)
        // ──────────────────────────────────────────────
        private static SpiritData[] _levelSpriteCache;

        /// <summary>
        /// 속성 + 레벨에 맞는 스프라이트 반환 (Lv.1→1성, Lv.2→2성, ..., Lv.5/6→5성)
        /// 머지보드(CreateItem)와 파티 편성 UI가 동일한 기준으로 이미지를 표시하기 위한 공용 헬퍼.
        /// 못 찾으면 null (호출부에서 원본 sprite 폴백)
        /// </summary>
        public static Sprite ResolveLevelSprite(ElementType element, int level)
        {
            if (_levelSpriteCache == null)
                _levelSpriteCache = Resources.LoadAll<SpiritData>("Data/Spirits");

            SpiritGrade grade = level switch
            {
                1 => SpiritGrade.OneStar,
                2 => SpiritGrade.TwoStar,
                3 => SpiritGrade.ThreeStar,
                4 => SpiritGrade.FourStar,
                5 => SpiritGrade.FiveStar,
                _ => SpiritGrade.FiveStar // Lv.6+
            };

            foreach (var sd in _levelSpriteCache)
            {
                if (sd == null) continue;
                if (sd.element == element && sd.grade == grade && sd.sprite != null)
                    return sd.sprite;
            }
            return null;
        }
    }
}

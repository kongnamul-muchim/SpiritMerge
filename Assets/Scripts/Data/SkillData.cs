using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 스킬 데이터 ScriptableObject
    /// GDD 3.4 정령 스킬 기반
    /// </summary>
    [CreateAssetMenu(fileName = "SkillData", menuName = "Game/SkillData")]
    public class SkillData : ScriptableObject
    {
        [Header("기본 정보")]
        public string skillName;
        public string description;
        public Sprite icon;

        [Header("스킬 타입")]
        public SkillType skillType;
        public SkillTarget target;

        [Header("스탯")]
        public float damageMultiplier = 1.0f;  // 공격력 계수
        public float healMultiplier = 0f;      // 치유 계수 (0 = 데미지 스킬)
        public float buffAmount = 0f;          // 버프량 (%)
        public float debuffAmount = 0f;        // 디버프량 (%)
        public float buffDuration = 0f;        // 지속시간 (초)

        [Header("쿨타임")]
        public float cooldown = 5f;

        [Header("패시브 전용")]
        public PassiveEffectType passiveEffect;
        public float passiveValue;             // 패시브 수치 (%)
    }

    public enum SkillType
    {
        Active,   // 액티브 스킬
        Passive   // 패시브 스킬
    }

    public enum SkillTarget
    {
        SingleEnemy,     // 단일 적
        AllEnemies,      // 모든 적
        SingleAlly,      // 단일 아군
        AllAllies,       // 모든 아군
        Self             // 자신
    }

    public enum PassiveEffectType
    {
        None,
        PartyATKUp,
        PartyHPRegen,
        PartySpeedUp,
        PartyDEFUp,
        PartyCritRateUp,
        PartyHealReceiveUp
    }
}

using UnityEngine;

namespace SpiritMerge
{
    [CreateAssetMenu(fileName = "EquipmentData", menuName = "Game/EquipmentData")]
    public class EquipmentData : ScriptableObject
    {
        [Header("기본 정보")]
        public string equipmentName;
        public EquipmentType equipmentType;
        public EquipmentGrade grade;
        public Sprite icon;

        [Header("주 스탯")]
        public int mainStatValue;

        [Header("부 스탯")]
        public int subStatValue;

        [Header("강화")]
        public int maxEnhanceLevel = 3;
    }

    [CreateAssetMenu(fileName = "DropTableData", menuName = "Game/DropTableData")]
    public class DropTableData : ScriptableObject
    {
        public DropEntry[] drops;

        [System.Serializable]
        public class DropEntry
        {
            public string itemId;
            [Range(0f, 1f)] public float dropRate;
            public int minCount = 1;
            public int maxCount = 1;
        }
    }

    [CreateAssetMenu(fileName = "PlayerLevelData", menuName = "Game/PlayerLevelData")]
    public class PlayerLevelData : ScriptableObject
    {
        public LevelEntry[] levels;

        [System.Serializable]
        public class LevelEntry
        {
            public int level;
            public int requiredExp;
            public int atkBonus;
            public int hpBonus;
            public float speedBonus;
        }
    }
}

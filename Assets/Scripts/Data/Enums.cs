using System.Collections.Generic;
using UnityEngine;

namespace SpiritMerge
{
    #region Enums

    public enum ElementType
    {
        Fire,
        Water,
        Wind,
        Earth,
        Dark,
        Light
    }

    public enum AnimalType
    {
        Wolf,
        Fox,
        Hawk,
        Bear,
        Panther,
        Deer,
        None // For 1~2 star formless spirits
    }

    public enum SpiritGrade
    {
        OneStar = 1,
        TwoStar = 2,
        ThreeStar = 3,
        FourStar = 4,
        FiveStar = 5
    }

    public enum EquipmentType
    {
        Weapon,
        Armor,
        Ring,
        Necklace
    }

    public enum EquipmentGrade
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    #endregion

    #region Serializable Data Structures

    [System.Serializable]
    public class SaveData
    {
        public int playerLevel = 1;
        public int playerExp = 0;
        public int[] skillTreeLevels = new int[11]; // 패시브 스킬 트리 레벨 배열
        public int skillPoints = 0;

        public List<OwnedSpirit> spirits = new List<OwnedSpirit>();
        public List<OwnedEquipment> equipment = new List<OwnedEquipment>();

        public int gold = 0;
        public int ruby = 0;
        public int spiritStone = 0;
        public int[] elementStones = new int[6]; // 속성석 6종 (Fire, Water, Wind, Earth, Dark, Light 순)

        public int currentStage = 1;
        public int[] partySlotIds = new int[4]; // 파티 편성 (정령 UID, -1 = 빈 슬롯)

        public string lastLoginTime = "";
    }

    [System.Serializable]
    public class OwnedSpirit
    {
        public string dataId;      // SpiritData 참조 ID (ex: "Fire_1", "Fire_3")
        public int uid;            // 고유 ID (중복 구분)
        public int grade;          // 현재 등급 (1~5)
        public int level = 1;
        public int exp = 0;

        public OwnedSpirit(string dataId, int uid, int grade)
        {
            this.dataId = dataId;
            this.uid = uid;
            this.grade = grade;
        }
    }

    [System.Serializable]
    public class OwnedEquipment
    {
        public string dataId;
        public int uid;
        public int enhanceLevel = 0;
    }

    #endregion
}

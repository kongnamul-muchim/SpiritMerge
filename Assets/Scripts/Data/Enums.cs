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
        public int version = 1;
        public long saveTimestamp;              // ⭐ 저장 시각 (epoch 초) — 오프라인 경과 계산 (파견/쿨다운)

        // 재화 / 플레이어
        public int gold = 0;
        public int ruby = 0;
        public int playerLevel = 1;
        public int playerExp = 0;
        public int skillPoints = 0;
        public int[] upgradeLevels = new int[25];   // 업그레이드 25노드 (골드 0~9 / SP 10~14 / 루비 15~24)

        // 스테이지 진행
        public int stageIndex = 0;
        public bool repeatMode = false;

        // 보드 정령 16슬롯 + 파티 배치 (보드 슬롯 인덱스 기준)
        public List<SavedSpirit> boardSpirits = new List<SavedSpirit>();
        public int[] partySlots = new int[4];   // 각 파티 슬롯에 배치된 보드 슬롯 인덱스 (-1 = 빈)

        // 의뢰 (파견)
        public List<SavedDispatchRequest> offers = new List<SavedDispatchRequest>();
        public List<SavedDispatch> activeDispatches = new List<SavedDispatch>();      // 파견 중 (남은 시간 포함)
        public List<SavedDispatch> completedDispatches = new List<SavedDispatch>();   // 완료 (보상 대기)
        public float requestCooldownTimer = 0f;  // 새 의뢰 쿨다운 남은 시간
        public int totalDispatchCount = 0;

        // 미션 (일일/주간 진행도 + 수령 여부)
        public int[] dailyProgress = new int[10];
        public bool[] dailyClaimed = new bool[10];
        public int[] weeklyProgress = new int[10];
        public bool[] weeklyClaimed = new bool[10];

        // 레이드
        public int raidStage = 1;
        public long raidTotalDamage = 0;
        public long raidBestScore = 0;
        public bool[] raidStageRewardClaimed = new bool[10];
        public ElementType weeklyBossElement = ElementType.Fire;

        // 도감 해금 (정령 asset name 목록)
        public List<string> dexUnlocked = new List<string>();
    }

    /// <summary>보드 슬롯에 놓인 정령 (슬롯 인덱스 + 데이터 + 성급)</summary>
    [System.Serializable]
    public class SavedSpirit
    {
        public int slotIndex;   // 보드 슬롯 0~15
        public string dataId;   // SpiritData asset name (Resources/Data/Spirits)
        public int level;       // 성급 (머지 레벨)
    }

    /// <summary>의뢰(제안) 직렬화용</summary>
    [System.Serializable]
    public class SavedDispatchRequest
    {
        public int id;
        public float durationHours;
        public int goldReward;
        public int rubyReward;
        public List<SavedOfferSlot> slots = new List<SavedOfferSlot>();
    }

    [System.Serializable]
    public class SavedOfferSlot
    {
        public ElementType requiredElement;
        public int minGrade;
    }

    /// <summary>파견 항목 직렬화용 (파견 중/완료 공용)</summary>
    [System.Serializable]
    public class SavedDispatch
    {
        public SavedDispatchRequest request;
        public string spirit1Name;
        public ElementType spirit1Element;
        public int spirit1Grade;
        public string spirit2Name;
        public ElementType spirit2Element;
        public int spirit2Grade;
        public float remainingSeconds;
        public bool notified;
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

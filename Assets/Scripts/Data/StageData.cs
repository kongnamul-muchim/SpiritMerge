using UnityEngine;

namespace SpiritMerge
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Game/StageData")]
    public class StageData : ScriptableObject
    {
        [Header("스테이지 정보")]
        public int stageNumber;
        public string stageName;
        public int region = 1;
        public ElementType elementType;
        public bool isBossStage;

        [Header("전투")]
        public int waveCount = 5;

        [Header("보상")]
        public int goldReward = 100;
        public int rubyReward;
        public int expReward = 50;

        [Header("난이도")]
        public float hpMultiplier = 1.0f;
        public float atkMultiplier = 1.0f;
    }
}

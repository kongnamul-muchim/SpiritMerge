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

        [Header("몬스터 폰")]
        public int totalMonsterCount;      // 해당 스테이지 총 몬스터 수
        public int spawnPointCount = 5;    // 폰 포인트 수 (고정 5)

        [Header("경제")]
        public int summonCost = 500;       // 소환 비용 (챕터별 고정)
        public int bossHpMultiplier = 10;  // 보스 HP 배수
    }
}

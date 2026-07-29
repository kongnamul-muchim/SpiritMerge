using UnityEngine;

namespace SpiritMerge.Data
{
    /// <summary>
    /// 몬스터 기본 데이터 — 전투에 등장하는 적
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterData", menuName = "Game/MonsterData")]
    public class MonsterData : ScriptableObject
    {
        [Header("기본 정보")]
        public string monsterName;
        public ElementType element;
        public string spriteFileName;
        public bool isBoss;

        [Header("리소스")]
        public Sprite sprite;             // 몬스터 이미지

        [Header("스탯")]
        public int baseATK = 20;
        public int baseHP = 100;
        public int baseDEF = 10;
        public float baseSpeed = 1.5f;

        [Header("보상")]
        public int goldReward = 50;
        public int expReward = 20;
    }
}

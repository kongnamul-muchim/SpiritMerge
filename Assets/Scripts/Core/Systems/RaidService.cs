using System;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 레이드 서비스 (GDD v1.1 §14)
    /// 허수아비 타격 점수 기록 + 일간 랭킹
    /// </summary>
    public class RaidService
    {
        public long BestScore { get; private set; }
        public long LastScore { get; private set; }
        public DateTime LastPlayed { get; private set; }

        /// <summary>
        /// 레이드 종료 시 점수 기록
        /// </summary>
        public bool RecordScore(long damage)
        {
            LastScore = damage;
            LastPlayed = DateTime.Now;

            if (damage > BestScore)
            {
                BestScore = damage;
                return true; // 신기록 갱신!
            }
            return false;
        }

        /// <summary>
        /// 일간 보상 등급 계산 (1~3)
        /// </summary>
        public int GetRankTier(int totalPlayers, int betterThan)
        {
            if (totalPlayers <= 0) return 3;
            float ratio = (float)betterThan / totalPlayers;
            if (ratio >= 0.9f) return 1;  // TOP 10%
            if (ratio >= 0.7f) return 2;  // TOP 30%
            return 3;                      // 참여 보상
        }

        public int GetTierRubyReward(int tier) => tier switch
        {
            1 => 30,
            2 => 15,
            _ => 5
        };
    }
}

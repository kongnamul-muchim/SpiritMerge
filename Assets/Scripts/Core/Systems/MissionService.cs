using System;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 미션 서비스 (GDD v1.1 §12)
    /// SRP: 일일/반복 미션의 진행도 추적 + 보상 지급
    /// </summary>
    public class MissionService
    {
        public DateTime LastDailyReset { get; private set; }

        // 일일 미션 진행도 [스테이지, 소환, 골드, 로그인, 머지]
        public int[] DailyProgress { get; private set; } = new int[5];
        public bool[] DailyCompleted { get; private set; } = new bool[5];
        public bool DailyAllBonusClaimed { get; private set; }

        // 반복 미션 진행도
        public int TotalCollectionCount { get; set; }
        public int TotalStageClears { get; set; }
        public int TotalMerges { get; set; }
        public int TotalEnhanceLevel { get; set; }

        public void TickDaily()
        {
            var now = DateTime.Now.Date;
            if (LastDailyReset.Date != now)
            {
                Array.Clear(DailyProgress, 0, DailyProgress.Length);
                Array.Clear(DailyCompleted, 0, DailyCompleted.Length);
                DailyAllBonusClaimed = false;
                LastDailyReset = now;
            }
        }

        public void RecordStageClear()
        {
            DailyProgress[0]++;
            TotalStageClears++;
            TryComplete(0);
        }

        public void RecordSummon()
        {
            DailyProgress[1]++;
            TryComplete(1);
        }

        public void RecordGoldEarned(int amount)
        {
            DailyProgress[2] += amount;
            if (DailyProgress[2] >= 1000) TryComplete(2);
        }

        public void RecordLogin()
        {
            DailyProgress[3]++;
            TryComplete(3);
        }

        public void RecordMerge()
        {
            DailyProgress[4]++;
            TotalMerges++;
            TryComplete(4);
        }

        private void TryComplete(int index)
        {
            int[] thresholds = { 3, 3, 1000, 1, 1 };
            if (!DailyCompleted[index] && DailyProgress[index] >= thresholds[index])
                DailyCompleted[index] = true;
        }

        public bool IsAllDailyCompleted()
        {
            foreach (var c in DailyCompleted)
                if (!c) return false;
            return true;
        }

        public int GetDailyRubyReward()
        {
            int total = 0;
            if (DailyCompleted[0]) total += 10;
            if (DailyCompleted[1]) total += 10;
            if (DailyCompleted[2]) total += 5;
            if (DailyCompleted[3]) total += 5;
            if (DailyCompleted[4]) total += 10;
            return total;
        }
    }
}

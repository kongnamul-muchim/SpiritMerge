using System;

namespace SpiritMerge.Core.Systems
{
    /// <summary>미션 타입 (10종)</summary>
    public enum MissionType
    {
        KillMonster,  // 몬스터 n마리 처치
        Upgrade,      // 업그레이드 n회
        BossKill,     // 보스 n회 처치
        Summon,       // 소환 n회
        Merge,        // 머지 n회
        Dispatch,     // 파견 n회
        GainGold,     // 골드 n 획득
        StageClear,   // 스테이지 n회 클리어
        LevelUp,      // 레벨업 n회
        Login         // 로그인 n회
    }

    /// <summary>미션 정의 — 타입/목표/보상/설명</summary>
    [Serializable]
    public class MissionDef
    {
        public MissionType Type;
        public int Target;
        public int GoldReward;
        public int RubyReward;
        public string Desc;
    }

    /// <summary>
    /// 미션 서비스 v2 — 일일/주간 각 10종
    /// - 주간 미션: 일일 수치의 5배, 보상 2~3배
    /// - 진행도 추적 + 완료 시 루비/골드 보상 청구
    /// - GameManager 이벤트 훅에서 Progress() 호출
    /// </summary>
    public class MissionService
    {
        public const int MissionCount = 10;

        public MissionDef[] DailyDefs { get; private set; }
        public MissionDef[] WeeklyDefs { get; private set; }
        public int[] DailyProgress { get; private set; }
        public int[] WeeklyProgress { get; private set; }
        public bool[] DailyClaimed { get; private set; }
        public bool[] WeeklyClaimed { get; private set; }

        public MissionService()
        {
            DailyDefs = BuildDaily();
            WeeklyDefs = BuildWeekly();
            DailyProgress = new int[MissionCount];
            WeeklyProgress = new int[MissionCount];
            DailyClaimed = new bool[MissionCount];
            WeeklyClaimed = new bool[MissionCount];
        }

        // ── 정의 ────────────────────────────────

        static MissionDef Def(MissionType t, int target, int gold, int ruby, string desc)
            => new MissionDef { Type = t, Target = target, GoldReward = gold, RubyReward = ruby, Desc = desc };

        static MissionDef[] BuildDaily() => new[]
        {
            Def(MissionType.KillMonster, 100, 0, 20, "몬스터 100마리 처치"),
            Def(MissionType.Upgrade,      5, 0, 15, "업그레이드 5회"),
            Def(MissionType.BossKill,     3, 0, 25, "보스 3회 처치"),
            Def(MissionType.Summon,      10, 0, 15, "소환 10회"),
            Def(MissionType.Merge,        5, 0, 20, "머지 5회"),
            Def(MissionType.Dispatch,     3, 0, 20, "파견 3회"),
            Def(MissionType.GainGold,  5000, 500, 10, "골드 5000 획득"),
            Def(MissionType.StageClear,  10, 0, 20, "스테이지 10회 클리어"),
            Def(MissionType.LevelUp,      1, 0, 10, "레벨업 1회"),
            Def(MissionType.Login,        1, 0, 10, "로그인 1회"),
        };

        static MissionDef[] BuildWeekly() => new[]
        {
            Def(MissionType.KillMonster,  500, 0, 50, "몬스터 500마리 처치"),
            Def(MissionType.Upgrade,      25, 0, 40, "업그레이드 25회"),
            Def(MissionType.BossKill,     15, 0, 60, "보스 15회 처치"),
            Def(MissionType.Summon,       50, 0, 40, "소환 50회"),
            Def(MissionType.Merge,        25, 0, 50, "머지 25회"),
            Def(MissionType.Dispatch,     15, 0, 50, "파견 15회"),
            Def(MissionType.GainGold,  25000, 1500, 25, "골드 25000 획득"),
            Def(MissionType.StageClear,   50, 0, 50, "스테이지 50회 클리어"),
            Def(MissionType.LevelUp,       5, 0, 25, "레벨업 5회"),
            Def(MissionType.Login,         5, 0, 25, "로그인 5회"),
        };

        // ── 진행도 ────────────────────────────────

        /// <summary>이벤트 훅에서 호출 — 일일+주간 동시 진행</summary>
        public void Progress(MissionType type, int amount = 1)
        {
            if (amount <= 0) return;
            for (int i = 0; i < MissionCount; i++)
            {
                if (DailyDefs[i].Type == type) DailyProgress[i] += amount;
                if (WeeklyDefs[i].Type == type) WeeklyProgress[i] += amount;
            }
        }

        // ── 보상 ────────────────────────────────

        /// <summary>보상 청구 — 성공 시 골드/루비 반환 (이미 청구/미완료면 false)</summary>
        public bool TryClaim(bool weekly, int index, out int gold, out int ruby)
        {
            gold = 0; ruby = 0;
            if (index < 0 || index >= MissionCount) return false;
            var defs = weekly ? WeeklyDefs : DailyDefs;
            var progress = weekly ? WeeklyProgress : DailyProgress;
            var claimed = weekly ? WeeklyClaimed : DailyClaimed;

            if (claimed[index]) return false;               // 이미 청구
            if (progress[index] < defs[index].Target) return false; // 미완료

            claimed[index] = true;
            gold = defs[index].GoldReward;
            ruby = defs[index].RubyReward;
            return true;
        }

        public int CompletedCount(bool weekly)
        {
            var defs = weekly ? WeeklyDefs : DailyDefs;
            var progress = weekly ? WeeklyProgress : DailyProgress;
            int c = 0;
            for (int i = 0; i < MissionCount; i++)
                if (progress[i] >= defs[i].Target) c++;
            return c;
        }

        /// <summary>⭐ 저장 로드 — 일일/주간 진행도 + 수령 여부 복원</summary>
        public void LoadFrom(int[] dailyP, bool[] dailyC, int[] weeklyP, bool[] weeklyC)
        {
            if (dailyP != null && dailyP.Length == MissionCount) DailyProgress = (int[])dailyP.Clone();
            if (dailyC != null && dailyC.Length == MissionCount) DailyClaimed = (bool[])dailyC.Clone();
            if (weeklyP != null && weeklyP.Length == MissionCount) WeeklyProgress = (int[])weeklyP.Clone();
            if (weeklyC != null && weeklyC.Length == MissionCount) WeeklyClaimed = (bool[])weeklyC.Clone();
        }
    }
}

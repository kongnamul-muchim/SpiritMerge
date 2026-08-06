using System;
using UnityEngine;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 레이드 서비스 v3 — 페이즈 방식
    /// - 주간 속성 보스, 60초 실전투
    /// - 보스 HP 페이즈: 1페이즈 = 1000 × stage^stage, 소진 시 다음 페이즈(더 높은 HP)로 리필
    /// - 보스는 죽지 않고 체력만 리필되며 강해짐 (Stage = 페이즈)
    /// - 점수 = 깬 페이즈 체력 합 + 현재 페이즈에서 깎은 체력
    /// - 페이즈별 보상(주간 1회) + 역대 최고 점수 기반 랭킹 보상
    /// </summary>
    public class RaidService
    {
        public const int MaxStage = 10;
        public const float RaidDuration = 60f;

        public ElementType WeeklyBossElement { get; private set; }
        public int Stage { get; private set; } = 1;         // 현재 페이즈 (1부터)
        public long TotalDamage { get; private set; }       // 이번 주 누적 점수
        public long LastScore { get; private set; }         // 마지막 레이드 점수
        public long BestScore { get; private set; }         // 역대 최고 점수
        public bool[] StageRewardClaimed { get; private set; }

        public RaidService()
        {
            StageRewardClaimed = new bool[MaxStage + 1];
            RollWeeklyBoss();
        }

        /// <summary>새 주간 보스 속성 (주간 초기화 시 호출)</summary>
        public void RollWeeklyBoss()
        {
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Wind,
                                   ElementType.Earth, ElementType.Dark, ElementType.Light };
            WeeklyBossElement = elements[UnityEngine.Random.Range(0, elements.Length)];
            Stage = 1;
            TotalDamage = 0;
            StageRewardClaimed = new bool[MaxStage + 1];
        }

        /// <summary>⭐ 저장 로드 — 단계/점수/보상/주간보스 복원</summary>
        public void LoadFrom(int stage, long totalDamage, long bestScore, bool[] stageRewardClaimed, ElementType weeklyBoss)
        {
            WeeklyBossElement = weeklyBoss;
            Stage = Mathf.Max(1, stage);
            TotalDamage = Math.Max(0L, totalDamage);
            BestScore = Math.Max(0L, bestScore);
            if (stageRewardClaimed != null && stageRewardClaimed.Length == MaxStage + 1)
                StageRewardClaimed = (bool[])stageRewardClaimed.Clone();
        }

        // ── 페이즈 스탯 ────────────────────────────────

        /// <summary>페이즈별 보스 HP: 1000 × stage^stage (1:1000, 2:4000, 3:27000, 4:256000, ...) — int 오버플로 방지</summary>
        public static int GetBossHP(int stage)
            => stage <= 0 ? 0 : (int)Math.Min(1000d * Math.Pow(stage, stage), int.MaxValue);

        /// <summary>페이즈별 보스 공격력</summary>
        public static int GetBossATK(int stage) => 100 + stage * 60;

        // ── 전투 연동 ────────────────────────────────

        /// <summary>깎은 체력만큼 점수 추가</summary>
        public void AddDamage(long damage)
        {
            if (damage <= 0) return;
            TotalDamage += damage;
        }

        /// <summary>페이즈 클리어 — 깬 페이즈 체력만큼 보너스 점수</summary>
        public void PhaseCleared()
        {
            TotalDamage += GetBossHP(Stage);
        }

        /// <summary>다음 페이즈로 (보스 체력 리필 + 강해짐)</summary>
        public void StageUp()
        {
            if (Stage < MaxStage) Stage++;
        }

        /// <summary>레이드 종료 — 점수 기록 (신기록 여부 반환). TotalDamage는 AddDamage/PhaseCleared로 이미 누적</summary>
        public bool EndRaid(long score)
        {
            LastScore = score;
            if (TotalDamage > BestScore) { BestScore = TotalDamage; return true; }
            return false;
        }

        // ── 보상 ────────────────────────────────

        /// <summary>페이즈별 보상 청구 (주간 1회) — 이미 청구/미도달이면 false</summary>
        public bool TryClaimStageReward(int stage, out int gold, out int ruby)
        {
            gold = 0; ruby = 0;
            if (stage < 1 || stage > MaxStage) return false;
            if (StageRewardClaimed[stage]) return false;
            if (Stage < stage) return false;   // 해당 페이즈를 깬 적 있어야
            StageRewardClaimed[stage] = true;
            gold = stage * 500;
            ruby = stage * 5;
            return true;
        }

        /// <summary>랭킹 보상 — 역대 최고 점수 구간별 티어 (1~3)</summary>
        public int GetRankTier()
        {
            if (BestScore >= 100000L) return 1;
            if (BestScore >= 30000L) return 2;
            return 3;
        }

        public int GetTierRubyReward(int tier) => tier switch { 1 => 100, 2 => 50, _ => 20 };
    }
}

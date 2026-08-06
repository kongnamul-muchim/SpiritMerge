using SpiritMerge.Core.Interfaces;
using UnityEngine;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 플레이어(정령사) 레벨/스킬트리 서비스
    /// SRP: 플레이어 성장 데이터만 관리
    ///
    /// 업그레이드 노드 (UpgradeLevels[25]) — 골드 탭(0~9) / 레벨 탭(10~14) / 루비 탭(15~24)
    ///
    /// [골드 업그레이드 — 골드 지불] (기본비용 × (레벨+1), 만렙 50)
    ///   0  공격력 +3   1  방어력 +2   2  체력 +10   3  공격속도 +3%
    ///   4  공격력 +1% (잠금: 0 Lv.5)   5  방어력 +2% (잠금: 1 Lv.5)   6  체력 +2% (잠금: 2 Lv.5)
    ///   7  치명타 확률 +0.5% (최대 100% 캡)
    ///   8  골드 +5%   9  경험치 +5%
    ///
    /// [레벨 업그레이드 — SP 1 소모]
    ///   10 자동 소환 (주기 30-3×Lv, 최소 10초, 만렙 10)
    ///   11 2성 확률 +0.5% (최대 25%)   12 3성 확률 +0.5% (잠금: 2성 만렙)   13 생성비용 -1% (최대 50%)   14 머지 보너스 +1%
    ///
    /// [루비 업그레이드 — 루비 지불] (골드와 동일 구성 + 치명타 데미지, 만렙 50)
    ///   15~21: 공/방/체/속도%/공%방%체% (잠금: 루비 flat Lv.5)
    ///   22 치명타 데미지 +2% (루비 전용, 골드의 치명타 확률과 대응)
    ///   23 골드 +5%   24 경험치 +5%
    /// </summary>
    public class PlayerService : IPlayerService
    {
        private static int RequiredExp(int level) => level switch
        {
            <= 10 => 100 * level,
            <= 20 => 500 * level,
            <= 30 => 2000 * level,
            _ => 5000 * level
        };

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int SkillPoints { get; private set; }

        // 기존 VContainer용
        public int[] SkillTreeLevels { get; private set; } = new int[10];

        // ⭐ 메인 게임 업그레이드 (25종)
        public int[] UpgradeLevels { get; private set; } = new int[25];

        public const int GoldTabStart = 0;
        public const int GoldTabEnd = 10;    // 0~9
        public const int LevelTabStart = 10;
        public const int LevelTabEnd = 15;   // 10~14
        public const int RubyTabStart = 15;
        public const int TotalNodes = 25;    // 15~24
        public const int DefaultMaxLevel = 50;

        public static readonly string[] UpgradeNames =
        {
            // 골드 0~9
            "공격력", "방어력", "체력", "공격속도", "공격력 %", "방어력 %", "체력 %", "치명타 확률", "골드 획득", "경험치 획득",
            // 레벨 10~14
            "자동 소환", "2성 확률", "3성 확률", "생성 비용 감소", "머지 보너스",
            // 루비 15~24
            "공격력", "방어력", "체력", "공격속도", "공격력 %", "방어력 %", "체력 %", "치명타 데미지", "골드 획득", "경험치 획득"
        };

        public static readonly string[] UpgradeDesc =
        {
            "공격력 +{0}", "방어력 +{0}", "체력 +{0}", "공격속도 +{0}%",
            "공격력 +{0}%", "방어력 +{0}%", "체력 +{0}%", "치명타 확률 +{0}%", "골드 +{0}%", "경험치 +{0}%",
            "자동 소환 주기 {0}초", "2성 확률 {0}%", "3성 확률 {0}%", "생성 비용 -{0}%", "머지 보너스 +{0}%",
            "공격력 +{0}", "방어력 +{0}", "체력 +{0}", "공격속도 +{0}%",
            "공격력 +{0}%", "방어력 +{0}%", "체력 +{0}%", "치명타 데미지 +{0}%", "골드 +{0}%", "경험치 +{0}%"
        };

        public static bool IsGoldNode(int idx) => idx >= GoldTabStart && idx < GoldTabEnd;
        public static bool IsLevelNode(int idx) => idx >= LevelTabStart && idx < LevelTabEnd;
        public static bool IsRubyNode(int idx) => idx >= RubyTabStart && idx < TotalNodes;

        /// <summary>노드별 최대 레벨 — ⭐ 고정스탯(공/방/체)은 무한, 치명타 확률은 200(100%)</summary>
        public static int MaxLevelFor(int idx) => idx switch
        {
            0 or 1 or 2 or 15 or 16 or 17 => int.MaxValue, // ⭐ 고정스탯 무한 강화
            7 => 200, // ⭐ 치명타 확률: 0.5%/Lv × 200 = 100%
            10 => 10, // 자동 소환 (주기 한계)
            _ => DefaultMaxLevel
        };

        /// <summary>잠금 요구 노드 — -1이면 잠금 없음</summary>
        public static int LockRequireNode(int idx) => idx switch
        {
            4 => 0, 5 => 1, 6 => 2,           // 골드 %
            19 => 15, 20 => 16, 21 => 17,     // 루비 %
            12 => 11,                          // 3성 = 2성 만렙
            _ => -1
        };

        public static int LockRequireLevel(int idx) => idx switch
        {
            4 or 5 or 6 or 19 or 20 or 21 => 5,
            12 => MaxLevelFor(11),
            _ => 0
        };

        public bool IsLocked(int idx)
        {
            int req = LockRequireNode(idx);
            if (req < 0) return false;
            return GetUpgradeLevel(req) < LockRequireLevel(idx);
        }

        // ── 비용 ──

        public static int BaseCostFor(int idx) => idx switch
        {
            0 or 1 or 15 or 16 => 50,
            2 or 3 or 17 or 18 => 80,
            4 or 5 or 6 or 19 or 20 or 21 => 200,
            7 => 150, 22 => 150,
            8 or 9 or 23 or 24 => 100,
            _ => 0
        };

        /// <summary>골드 비용 = 기본 × (현재레벨+1)</summary>
        public int GetGoldCost(int idx) => BaseCostFor(idx) * (GetUpgradeLevel(idx) + 1);

        /// <summary>루비 비용 = 기본 × (현재레벨+1)</summary>
        public int GetRubyCost(int idx) => BaseCostFor(idx) * (GetUpgradeLevel(idx) + 1);

        public void AddExp(int amount)
        {
            Exp += amount;
            while (Exp >= RequiredExp(Level) && Level < 50)
            {
                Exp -= RequiredExp(Level);
                LevelUp();
            }
        }

        public void LevelUp()
        {
            Level++;
            SkillPoints++;
        }

        /// <summary>⭐ 저장 로드 — 레벨/경험치/SP/업그레이드 복원</summary>
        public void LoadFrom(int level, int exp, int sp, int[] upgradeLevels)
        {
            Level = Mathf.Max(1, level);
            Exp = Mathf.Max(0, exp);
            SkillPoints = Mathf.Max(0, sp);
            if (upgradeLevels != null && upgradeLevels.Length == UpgradeLevels.Length)
                UpgradeLevels = (int[])upgradeLevels.Clone();
        }

        public int ExpToNext => RequiredExp(Level);

        /// <summary>골드 업그레이드 적용 (골드 차감은 GameManager)</summary>
        public void ApplyGoldUpgrade(int idx)
        {
            if (!IsGoldNode(idx)) return;
            if (GetUpgradeLevel(idx) >= MaxLevelFor(idx)) return;
            UpgradeLevels[idx]++;
        }

        /// <summary>루비 업그레이드 적용 (루비 차감은 GameManager)</summary>
        public void ApplyRubyUpgrade(int idx)
        {
            if (!IsRubyNode(idx)) return;
            if (GetUpgradeLevel(idx) >= MaxLevelFor(idx)) return;
            UpgradeLevels[idx]++;
        }

        /// <summary>SP 업그레이드 (레벨 탭) — SP 1 소모</summary>
        public bool UpgradeAt(int idx)
        {
            if (!IsLevelNode(idx)) return false;
            if (SkillPoints <= 0) return false;
            if (GetUpgradeLevel(idx) >= MaxLevelFor(idx)) return false;
            if (IsLocked(idx)) return false;
            UpgradeLevels[idx]++;
            SkillPoints--;
            return true;
        }

        public bool SpendSkillPoint(int treeIndex)
        {
            if (SkillPoints <= 0 || treeIndex < 0 || treeIndex >= SkillTreeLevels.Length)
                return false;
            SkillTreeLevels[treeIndex]++;
            SkillPoints--;
            return true;
        }

        public int GetUpgradeLevel(int idx) =>
            (idx >= 0 && idx < UpgradeLevels.Length) ? UpgradeLevels[idx] : 0;

        // ── 스탯 보너스 (골드 + 루비 합산) ──

        public int GetFlatATK() => (GetUpgradeLevel(0) + GetUpgradeLevel(15)) * 3;
        public int GetFlatDEF() => (GetUpgradeLevel(1) + GetUpgradeLevel(16)) * 2;
        public int GetFlatHP() => (GetUpgradeLevel(2) + GetUpgradeLevel(17)) * 10;
        public float GetSPDBonusPct() => (GetUpgradeLevel(3) + GetUpgradeLevel(18)) * 0.01f; // 1%/Lv (하향)
        public float GetATKBonusPct() => (GetUpgradeLevel(4) + GetUpgradeLevel(19)) * 0.01f;
        public float GetDEFBonusPct() => (GetUpgradeLevel(5) + GetUpgradeLevel(20)) * 0.02f;
        public float GetHPBonusPct() => (GetUpgradeLevel(6) + GetUpgradeLevel(21)) * 0.02f;
        public float GetCritRateBonus() => Mathf.Min(1f, GetUpgradeLevel(7) * 0.005f); // 0.5%/Lv, 만렙200 = 최대 100%
        public float GetCritDmgBonus() => GetUpgradeLevel(22) * 0.02f;                  // +2%/Lv
        public float GetGoldBonusPct() => (GetUpgradeLevel(8) + GetUpgradeLevel(23)) * 0.05f;
        public float GetExpBonusPct() => (GetUpgradeLevel(9) + GetUpgradeLevel(24)) * 0.05f;

        // ── 소환 스킬트리 ──

        public float GetAutoSummonInterval()
        {
            int lv = GetUpgradeLevel(10);
            if (lv <= 0) return 0f;
            return Mathf.Max(10f, 30f - lv * 3f);
        }

        public float GetTwoStarChance() => Mathf.Min(0.25f, GetUpgradeLevel(11) * 0.005f);
        public float GetThreeStarChance() => Mathf.Min(0.25f, GetUpgradeLevel(12) * 0.005f);
        public float GetSummonCostDiscount() => Mathf.Min(0.5f, GetUpgradeLevel(13) * 0.01f);
        public float GetMergeBonusPct() => GetUpgradeLevel(14) * 0.01f;

        // ── 기존 VContainer 호환 ──

        public int GetPartyATKBonus() => Level + (SkillTreeLevels[0] > 0 ? SkillTreeLevels[0] * 5 : 0);
        public int GetPartyDEFBonus() => Level * 2 + (SkillTreeLevels[1] > 0 ? SkillTreeLevels[1] * 5 : 0);
        public int GetPartyHPBonus() => Level * 10 + (SkillTreeLevels[2] > 0 ? SkillTreeLevels[2] * 10 : 0);
        public float GetPartySPDBonus() => SkillTreeLevels[3] > 0 ? SkillTreeLevels[3] * 0.03f : 0f;
    }
}

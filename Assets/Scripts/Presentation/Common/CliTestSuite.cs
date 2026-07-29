using System.Text;
using SpiritMerge.Core.Interfaces;
using SpiritMerge.Core.Systems;
using UnityEngine;

namespace SpiritMerge.Cli
{
    /// <summary>
    /// 독립형 CLI 테스트 — DI/VContainer 없이 직접 서비스 인스턴스 생성
    /// CliServer → method: 호출로 실행
    /// </summary>
    public static class CliTestSuite
    {
        private static SpiritService _spirit;
        private static MergeService _merge;
        private static bool _initialized;

        /// <summary>간단 연결 테스트</summary>
        public static void CmdPing()
        {
            Debug.Log("[CLI] pong from CliTestSuite!");
        }

        /// <summary>
        /// 전체 테스트 실행 (1회) — 소환 → 머지 → 크로스머지
        /// </summary>
        public static void CmdTestAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine("  Spirit Merge — CLI Test Suite");
            sb.AppendLine("═══════════════════════════════════");

            EnsureInit();

            // 1. 소환 테스트
            sb.AppendLine("\n─── 1. 소환 ───");
            var f1 = _spirit.AddSpirit("Fire_1", 1);
            sb.AppendLine($"  ✅ Fire_1 (UID:{f1.uid})");
            var f2 = _spirit.AddSpirit("Fire_1", 1);
            sb.AppendLine($"  ✅ Fire_1 (UID:{f2.uid})");
            var f3 = _spirit.AddSpirit("Fire_1", 1);
            sb.AppendLine($"  ✅ Fire_1 (UID:{f3.uid})");
            var w1 = _spirit.AddSpirit("Water_1", 1);
            sb.AppendLine($"  ✅ Water_1 (UID:{w1.uid})");
            var wi1 = _spirit.AddSpirit("Wind_1", 1);
            sb.AppendLine($"  ✅ Wind_1 (UID:{wi1.uid})");

            // 2. 보유 목록
            sb.AppendLine($"\n─── 2. 보유 현황 ───");
            sb.AppendLine($"  총 {_spirit.GetAllSpirits().Count}마리");

            // 3. 동일 머지 테스트
            sb.AppendLine("\n─── 3. 동일 머지 (Fire_1 3마리 → 2성) ───");
            bool canMerge = _merge.CanMerge("Fire_1", 1);
            sb.AppendLine($"  CanMerge(Fire_1, 1) = {canMerge}");
            if (canMerge)
            {
                var result = _merge.Merge("Fire_1", 1);
                if (result != null)
                    sb.AppendLine($"  ✅ 머지 성공! → {result.dataId} {result.grade}★ (UID:{result.uid})");
                else
                    sb.AppendLine($"  ❌ 머지 실패 (null)");
            }
            sb.AppendLine($"  보유: {_spirit.GetAllSpirits().Count}마리");

            // 4. 크로스 머지 테스트 (1성 3마리)
            sb.AppendLine("\n─── 4. 크로스 머지 (Water_1 + Wind_1 + 기존 Fire_1 1성) ───");
            // Fire_1 1성이 아직 남아있는지 확인
            var remainingFire = _spirit.GetAllSpirits().Find(s => s.dataId == "Fire_1" && s.grade == 1);
            if (remainingFire != null)
            {
                var crossResult = _merge.CrossMerge(new[] { remainingFire.uid, w1.uid, wi1.uid });
                if (crossResult != null)
                    sb.AppendLine($"  ✅ 크로스머지 성공! → {crossResult.dataId} {crossResult.grade}★ (UID:{crossResult.uid})");
                else
                    sb.AppendLine($"  ❌ 크로스머지 실패 (null)");
            }

            // 5. 최종 현황
            sb.AppendLine($"\n─── 5. 최종 보유 정령 ───");
            foreach (var s in _spirit.GetAllSpirits())
                sb.AppendLine($"  #{s.uid} | {s.dataId} | {s.grade}★ | Lv.{s.level}");
            sb.AppendLine($"  총 {_spirit.GetAllSpirits().Count}마리");

            // 6. 머지 체인: 2성 → 3성 (2마리 필요)
            sb.AppendLine("\n─── 6. 2성 머지 체인 (2마리 소환 → 머지) ───");
            var fa = _spirit.AddSpirit("Fire_2", 2);
            var fb = _spirit.AddSpirit("Fire_2", 2);
            sb.AppendLine($"  소환: Fire_2 (UID:{fa.uid}), Fire_2 (UID:{fb.uid})");
            if (_merge.CanMerge("Fire_2", 2))
            {
                var up = _merge.Merge("Fire_2", 2);
                if (up != null)
                    sb.AppendLine($"  ✅ 2→3성 머지 성공! → {up.dataId} {up.grade}★");
                else
                    sb.AppendLine($"  ❌ 2→3성 머지 실패");
            }

            sb.AppendLine("\n═══════════════════════════════════");
            sb.AppendLine("  ✅ CLI Test Suite 완료!");
            sb.AppendLine("═══════════════════════════════════");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 1성 정령 소환 (간편 명령어)
        /// </summary>
        public static void CmdSummon_Fire_1() => SimpleSummon("Fire", 1);
        public static void CmdSummon_Water_1() => SimpleSummon("Water", 1);
        public static void CmdSummon_Wind_1() => SimpleSummon("Wind", 1);
        public static void CmdSummon_Earth_1() => SimpleSummon("Earth", 1);
        public static void CmdSummon_Dark_1() => SimpleSummon("Dark", 1);
        public static void CmdSummon_Light_1() => SimpleSummon("Light", 1);

        public static void CmdSummon_Fire_2() => SimpleSummon("Fire", 2);
        public static void CmdSummon_Water_2() => SimpleSummon("Water", 2);
        public static void CmdSummon_Wind_2() => SimpleSummon("Wind", 2);
        public static void CmdSummon_Earth_2() => SimpleSummon("Earth", 2);
        public static void CmdSummon_Dark_2() => SimpleSummon("Dark", 2);
        public static void CmdSummon_Light_2() => SimpleSummon("Light", 2);

        /// <summary>보유 정령 목록</summary>
        public static void CmdList()
        {
            EnsureInit();
            var sb = new StringBuilder();
            sb.AppendLine("═════ 보유 정령 ═════");
            var all = _spirit.GetAllSpirits();
            if (all.Count == 0)
                sb.AppendLine("(없음)");
            else
                foreach (var s in all)
                    sb.AppendLine($"  #{s.uid} | {s.dataId} | {s.grade}★ | Lv.{s.level}");
            sb.AppendLine($"→ 총 {all.Count}마리");
            Debug.Log(sb.ToString());
        }

        // ─── Internal ──────────────────────────────

        private static void EnsureInit()
        {
            if (_initialized) return;
            _spirit = new SpiritService();
            _merge = new MergeService(_spirit, new CurrencyService());
            _initialized = true;
            Debug.Log("[CLI] CliTestSuite initialized (standalone mode)");
        }

        private static void SimpleSummon(string element, int grade)
        {
            EnsureInit();
            string dataId = $"{element}_{grade}";
            var spirit = _spirit.AddSpirit(dataId, grade);
            Debug.Log($"[CLI] ✅ {dataId} {grade}★ 소환 (UID:{spirit.uid})");
        }
    }
}

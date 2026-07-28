using System;
using System.Linq;
using SpiritMerge;
using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 정령 머지 서비스 (OCP: 새로운 머지 규칙 추가 시 IMergeService 확장)
    /// DIP: ISpiritService를 주입받아 사용
    /// </summary>
    public class MergeService : IMergeService
    {
        private readonly ISpiritService _spiritService;
        private readonly ICurrencyService _currencyService;

        // DI: 생성자 주입
        public MergeService(ISpiritService spiritService, ICurrencyService currencyService)
        {
            _spiritService = spiritService;
            _currencyService = currencyService;
        }

        public int GetRequiredCount(int currentGrade) => currentGrade switch
        {
            1 => 3,
            2 => 3,
            3 => 2,
            4 => 2,
            _ => 0
        };

        public bool CanMerge(string dataId, int currentGrade)
        {
            int required = GetRequiredCount(currentGrade);
            return _spiritService.GetSpiritCount(dataId, currentGrade) >= required;
        }

        public OwnedSpirit Merge(string dataId, int currentGrade)
        {
            if (!CanMerge(dataId, currentGrade)) return null;

            int nextGrade = currentGrade + 1;
            if (nextGrade > 5) return null;

            int required = GetRequiredCount(currentGrade);
            int removed = 0;

            // 재료 제거
            var all = _spiritService.GetAllSpirits();
            foreach (var s in all.Where(s => s.dataId == dataId && s.grade == currentGrade))
            {
                if (removed >= required) break;
                _spiritService.RemoveSpirit(s.uid);
                removed++;
            }

            // 상위 등급 생성
            return _spiritService.AddSpirit(dataId, nextGrade);
        }

        public OwnedSpirit CrossMerge(int[] spiritUids)
        {
            if (spiritUids.Length < 3) return null;

            // 재료 등급 확인
            var first = _spiritService.GetSpirit(spiritUids[0]);
            if (first == null) return null;
            int baseGrade = first.grade;

            // 재료 제거
            foreach (int uid in spiritUids)
                _spiritService.RemoveSpirit(uid);

            // 랜덤 결과
            float roll = UnityEngine.Random.value;
            int resultGrade = roll < 0.50f ? baseGrade :
                              roll < 0.80f ? Math.Min(baseGrade + 1, 5) :
                              roll < 0.95f ? Math.Min(baseGrade + 2, 5) :
                                             Math.Min(baseGrade + 3, 5);

            string[] elements = { "Fire", "Water", "Wind", "Earth", "Dark", "Light" };
            string randomElement = elements[UnityEngine.Random.Range(0, elements.Length)];
            string randomId = $"{randomElement}_{resultGrade}";

            return _spiritService.AddSpirit(randomId, resultGrade);
        }
    }
}

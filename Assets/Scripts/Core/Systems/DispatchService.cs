using System.Collections.Generic;
using SpiritMerge;
using System.Linq;
using UnityEngine;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 의뢰(파견) 시스템 (GDD v1.1 §13)
    /// SRP: 의뢰 생성/매칭/보상만 담당
    /// </summary>
    public class DispatchService
    {
        // 활성 의뢰 목록
        private readonly List<DispatchRequest> _activeRequests = new();

        /// <summary>
        /// 새 의뢰 생성
        /// </summary>
        public DispatchRequest GenerateRequest()
        {
            var req = new DispatchRequest
            {
                id = System.Guid.NewGuid().GetHashCode(),
                durationHours = Random.Range(1, 4),
                goldReward = Random.Range(1000, 5000),
                rubyReward = Random.Range(5, 20),
                slots = GenerateSlots(Random.Range(1, 4))
            };
            _activeRequests.Add(req);
            return req;
        }

        /// <summary>
        /// 의뢰 조건 확인: 정령 배열을 받아 일치 수 반환
        /// </summary>
        public int EvaluateRequest(DispatchRequest request, int[] spiritUids)
        {
            int matchCount = 0;
            for (int i = 0; i < request.slots.Length && i < spiritUids.Length; i++)
            {
                // (실제 구현 시 ISpiritService에서 정령 데이터 조회)
                // 여기서는 단순화: uid > 0 이면 조건 충족으로 가정
                if (spiritUids[i] > 0) matchCount++;
            }
            return matchCount;
        }

        private DispatchSlot[] GenerateSlots(int count)
        {
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Wind,
                                   ElementType.Earth, ElementType.Dark, ElementType.Light };
            var slots = new DispatchSlot[count];
            for (int i = 0; i < count; i++)
            {
                slots[i] = new DispatchSlot
                {
                    requiredElement = elements[Random.Range(0, elements.Length)],
                    minGrade = Random.Range(1, 4)
                };
            }
            return slots;
        }
    }

    [System.Serializable]
    public class DispatchRequest
    {
        public int id;
        public int durationHours;
        public int goldReward;
        public int rubyReward;
        public DispatchSlot[] slots;
    }

    [System.Serializable]
    public class DispatchSlot
    {
        public ElementType requiredElement;
        public int minGrade;         // 1~3
    }
}

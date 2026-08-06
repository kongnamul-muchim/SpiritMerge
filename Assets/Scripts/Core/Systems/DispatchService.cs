using System.Collections.Generic;
using SpiritMerge;
using UnityEngine;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 의뢰(파견) 시스템 v4 — 슬롯 2개 공존
    /// - 슬롯 2개에 의뢰/파견 중/완료(보상 대기)가 공존 (MaxSlots = 2)
    /// - 파견 = 의뢰에 정령 2마리 배치 → "파견 보내기" → 시간 경과 후 보상
    /// - 조건(속성+최소성급)은 추가 보상용 (매칭 시 보너스), 아무 정령이나 파견 가능
    /// - 새 의뢰 받기: 슬롯 여유 + 30초 쿨다운
    /// </summary>
    public class DispatchService
    {
        public const int MaxSlots = 2;              // 슬롯 2개 고정
        public const float NewRequestCooldown = 30f; // 새 의뢰 쿨다운

        /// <summary>파견 항목 (정령 2마리)</summary>
        public class DispatchEntry
        {
            public DispatchRequest Request;
            public string Spirit1Name;
            public ElementType Spirit1Element;
            public int Spirit1Grade;
            public string Spirit2Name;
            public ElementType Spirit2Element;
            public int Spirit2Grade;
            public float RemainingSeconds;
            public bool Notified;
        }

        public List<DispatchRequest> offers { get; } = new();   // 의뢰 (파견 전)
        public List<DispatchEntry> Active { get; } = new();     // 파견 중
        public List<DispatchEntry> Completed { get; } = new();  // 완료 (보상 대기)
        public float requestCooldownTimer = 0f;                 // 새 의뢰 쿨다운
        public int TotalDispatchCount { get; private set; }
        public float stageMultiplier = 1f;

        /// <summary>사용 중인 슬롯 수 (의뢰 + 파견 중 + 완료)</summary>
        public int UsedSlots => offers.Count + Active.Count + Completed.Count;

        /// <summary>새 의뢰 받기 가능 여부 (슬롯 여유 + 쿨다운 완료)</summary>
        public bool CanGetNewOffer => UsedSlots < MaxSlots && requestCooldownTimer <= 0f;

        /// <summary>시간 경과 — 파견 완료 시 Completed로 이동 + 쿨다운 감소</summary>
        public void Tick(float deltaSeconds)
        {
            if (requestCooldownTimer > 0f) requestCooldownTimer -= deltaSeconds;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var e = Active[i];
                e.RemainingSeconds -= deltaSeconds;
                if (e.RemainingSeconds <= 0f)
                {
                    e.RemainingSeconds = 0f;
                    Active.RemoveAt(i);
                    Completed.Add(e);   // ⭐ 완료 → 보상 대기 (슬롯은 계속 차지, 보상 받으면 해제)
                }
            }
        }

        /// <summary>새 의뢰 받기 (쿨다운 + 슬롯 체크) — 성공 시 쿨다운 시작</summary>
        public bool TryGetNewOffer()
        {
            if (!CanGetNewOffer) return false;
            offers.Add(GenerateRequest());
            requestCooldownTimer = NewRequestCooldown;
            return true;
        }

        /// <summary>⭐ 저장 로드 — 의뢰/파견중/완료/쿨다운/총횟수 복원</summary>
        public void LoadFrom(List<DispatchRequest> loadedOffers,
            List<DispatchEntry> loadedActive, List<DispatchEntry> loadedCompleted,
            float cooldown, int totalCount)
        {
            offers.Clear();
            Active.Clear();
            Completed.Clear();
            if (loadedOffers != null) offers.AddRange(loadedOffers);
            if (loadedActive != null) Active.AddRange(loadedActive);
            if (loadedCompleted != null) Completed.AddRange(loadedCompleted);
            requestCooldownTimer = cooldown;
            TotalDispatchCount = totalCount;
        }

        /// <summary>새 의뢰 생성 (조건 슬롯 1개 — 추가 보상용)</summary>
        public DispatchRequest GenerateRequest()
        {
            var req = new DispatchRequest
            {
                id = UnityEngine.Random.Range(1, 1000000),
                durationHours = UnityEngine.Random.Range(1, 4),
                goldReward = UnityEngine.Random.Range(1000, 5000),
                rubyReward = UnityEngine.Random.Range(5, 20),
                slots = GenerateSlots(1)
            };
            return req;
        }

        /// <summary>조건 매칭 수 (추가 보상용)</summary>
        public int MatchCount(DispatchRequest req, ElementType elem, int grade)
        {
            if (req?.slots == null) return 0;
            int match = 0;
            foreach (var slot in req.slots)
                if (slot.requiredElement == elem && grade >= slot.minGrade) match++;
            return match;
        }

        /// <summary>보상 보너스 배율 — 2마리 매칭 평균 (0 ~ +100%)</summary>
        public float MatchBonus(DispatchRequest req,
            ElementType e1, int g1, ElementType e2, int g2)
        {
            if (req?.slots == null || req.slots.Length == 0) return 0f;
            float m1 = MatchCount(req, e1, g1);
            float m2 = MatchCount(req, e2, g2);
            return (m1 + m2) / (2f * req.slots.Length);
        }

        /// <summary>
        /// 파견 보내기 — 의뢰 1개 소모, 정령 2마리를 보내 파견 시작
        /// </summary>
        public bool TryStart(DispatchRequest req,
            string n1, ElementType e1, int g1,
            string n2, ElementType e2, int g2,
            float durationScale)
        {
            if (req == null || !offers.Contains(req)) return false;
            offers.Remove(req);   // ⭐ 의뢰 소모 (슬롯 1개 → 파견 중으로)
            Active.Add(new DispatchEntry
            {
                Request = req,
                Spirit1Name = n1, Spirit1Element = e1, Spirit1Grade = g1,
                Spirit2Name = n2, Spirit2Element = e2, Spirit2Grade = g2,
                RemainingSeconds = req.durationHours * 3600f * durationScale,
                Notified = false
            });
            TotalDispatchCount++;
            return true;
        }

        /// <summary>Completed에서 보상 수령 + 제거 (슬롯 해제)</summary>
        public (DispatchEntry entry, int gold, int ruby) Claim(int index)
        {
            var e = Completed[index];
            Completed.RemoveAt(index);
            float bonus = MatchBonus(e.Request,
                e.Spirit1Element, e.Spirit1Grade,
                e.Spirit2Element, e.Spirit2Grade);
            int gold = Mathf.RoundToInt(e.Request.goldReward * (1f + bonus) * stageMultiplier);
            int ruby = Mathf.RoundToInt(e.Request.rubyReward * (1f + bonus) * stageMultiplier);
            return (e, gold, ruby);
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
                    requiredElement = elements[UnityEngine.Random.Range(0, elements.Length)],
                    minGrade = UnityEngine.Random.Range(1, 4)
                };
            }
            return slots;
        }
    }

    [System.Serializable]
    public class DispatchRequest
    {
        public int id;
        public float durationHours;
        public int goldReward;
        public int rubyReward;
        public DispatchSlot[] slots;
    }

    [System.Serializable]
    public class DispatchSlot
    {
        public ElementType requiredElement;
        public int minGrade;         // 1~3 (이상)
    }
}

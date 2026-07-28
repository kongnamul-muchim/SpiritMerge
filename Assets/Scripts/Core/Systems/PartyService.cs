using System.Collections.Generic;
using System.Linq;
using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 파티 편성 서비스 (SRP: 4슬롯 파티만 관리)
    /// DIP: ISpiritService를 주입받아 정령 데이터 조회
    /// </summary>
    public class PartyService : IPartyService
    {
        private readonly ISpiritService _spiritService;

        // 4개의 파티 슬롯 (UID 저장, -1 = 빈 슬롯)
        public int[] PartySlotIds { get; private set; } = new int[4] { -1, -1, -1, -1 };

        public PartyService(ISpiritService spiritService)
        {
            _spiritService = spiritService;
        }

        public bool AssignSpirit(int slotIndex, int spiritUid)
        {
            if (slotIndex < 0 || slotIndex >= 4) return false;

            var spirit = _spiritService.GetSpirit(spiritUid);
            if (spirit == null) return false;

            // 다른 슬롯에 이미 배치된 정령이면 먼저 제거
            for (int i = 0; i < 4; i++)
            {
                if (PartySlotIds[i] == spiritUid)
                    PartySlotIds[i] = -1;
            }

            PartySlotIds[slotIndex] = spiritUid;
            return true;
        }

        public void RemoveSpirit(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < 4)
                PartySlotIds[slotIndex] = -1;
        }

        public List<OwnedSpirit> GetPartySpirits()
        {
            return PartySlotIds
                .Where(id => id >= 0)
                .Select(id => _spiritService.GetSpirit(id))
                .Where(s => s != null)
                .ToList();
        }

        public bool IsSlotEmpty(int slotIndex)
        {
            return slotIndex < 0 || slotIndex >= 4 || PartySlotIds[slotIndex] < 0;
        }

        public void SaveParty()
        {
            // GameManager/DataService가 호출할 때 커런트 상태 유지
            // (별도 저장 로직 불필요 — PartySlotIds가 메모리에 있음)
        }

        public void LoadParty(int[] savedSlotIds)
        {
            if (savedSlotIds == null || savedSlotIds.Length != 4) return;
            PartySlotIds = savedSlotIds;
        }
    }
}

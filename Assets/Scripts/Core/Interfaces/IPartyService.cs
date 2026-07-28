using System.Collections.Generic;

namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 파티 편성 시스템 인터페이스
    /// SRP: 4슬롯 파티 구성의 저장/조회만 담당
    /// </summary>
    public interface IPartyService
    {
        /// <summary> 현재 편성된 파티 슬롯 (정령 UID, -1=빈칸) </summary>
        int[] PartySlotIds { get; }

        /// <summary> 슬롯에 정령 배치 </summary>
        bool AssignSpirit(int slotIndex, int spiritUid);

        /// <summary> 슬롯에서 정령 제거 </summary>
        void RemoveSpirit(int slotIndex);

        /// <summary> 현재 파티 정령 목록 조회 </summary>
        List<OwnedSpirit> GetPartySpirits();

        /// <summary> 슬롯이 비었는지 확인 </summary>
        bool IsSlotEmpty(int slotIndex);

        /// <summary> 파티 전체 저장 </summary>
        void SaveParty();

        /// <summary> 파티 불러오기 </summary>
        void LoadParty(int[] savedSlotIds);
    }
}

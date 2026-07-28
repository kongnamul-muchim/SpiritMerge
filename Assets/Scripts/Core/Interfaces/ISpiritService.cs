using System.Collections.Generic;

namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 정령 관리 서비스 (ISP: 정령 CRUD에만 집중)
    /// </summary>
    public interface ISpiritService
    {
        OwnedSpirit AddSpirit(string dataId, int grade);
        void RemoveSpirit(int uid);
        OwnedSpirit GetSpirit(int uid);
        List<OwnedSpirit> GetAllSpirits();
        int GetSpiritCount(string dataId, int grade);
        List<OwnedSpirit> GetPartySpirits(int[] partySlotIds);
    }
}

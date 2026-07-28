using System.Collections.Generic;
using SpiritMerge;
using System.Linq;
using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 정령 관리 서비스 (SRP: 정령 소유/조회만 담당)
    /// </summary>
    public class SpiritService : ISpiritService
    {
        private readonly List<OwnedSpirit> _spirits = new();
        private int _nextUid = 1;

        public OwnedSpirit AddSpirit(string dataId, int grade)
        {
            var spirit = new OwnedSpirit(dataId, _nextUid++, grade);
            _spirits.Add(spirit);
            return spirit;
        }

        public void RemoveSpirit(int uid)
        {
            _spirits.RemoveAll(s => s.uid == uid);
        }

        public OwnedSpirit GetSpirit(int uid)
        {
            return _spirits.FirstOrDefault(s => s.uid == uid);
        }

        public List<OwnedSpirit> GetAllSpirits()
        {
            return new List<OwnedSpirit>(_spirits);
        }

        public int GetSpiritCount(string dataId, int grade)
        {
            return _spirits.Count(s => s.dataId == dataId && s.grade == grade);
        }

        public List<OwnedSpirit> GetPartySpirits(int[] partySlotIds)
        {
            return partySlotIds
                .Where(id => id >= 0)
                .Select(id => GetSpirit(id))
                .Where(s => s != null)
                .ToList();
        }
    }
}

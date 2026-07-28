using System.Collections.Generic;
using SpiritMerge;
using System.Linq;
using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 장비 인벤토리 서비스 (SRP: 장비 보유/강화만)
    /// </summary>
    public class InventoryService : IInventoryService
    {
        private readonly List<OwnedEquipment> _items = new();
        private int _nextUid = 1;

        public void AddEquipment(string dataId)
        {
            _items.Add(new OwnedEquipment
            {
                dataId = dataId,
                uid = _nextUid++,
                enhanceLevel = 0
            });
        }

        public bool RemoveEquipment(int uid)
        {
            return _items.RemoveAll(e => e.uid == uid) > 0;
        }

        public bool EnhanceEquipment(int uid)
        {
            var eq = _items.FirstOrDefault(e => e.uid == uid);
            if (eq == null) return false;
            eq.enhanceLevel++;
            return true;
        }

        public List<OwnedEquipment> GetAllEquipment()
        {
            return new List<OwnedEquipment>(_items);
        }
    }
}

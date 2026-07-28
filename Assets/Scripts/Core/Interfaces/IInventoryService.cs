using System.Collections.Generic;

namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 장비 관리
    /// </summary>
    public interface IInventoryService
    {
        void AddEquipment(string dataId);
        bool RemoveEquipment(int uid);
        bool EnhanceEquipment(int uid);
        List<OwnedEquipment> GetAllEquipment();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 인벤토리 관리
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance;

        [Header("인벤토리")]
        public List<OwnedEquipment> equipmentList = new List<OwnedEquipment>();

        private int _nextEqUid = 1;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void AddEquipment(string dataId)
        {
            OwnedEquipment eq = new OwnedEquipment
            {
                dataId = dataId,
                uid = _nextEqUid++,
                enhanceLevel = 0
            };
            equipmentList.Add(eq);
        }

        public bool RemoveEquipment(int uid)
        {
            return equipmentList.RemoveAll(e => e.uid == uid) > 0;
        }

        /// <summary>
        /// 장비 강화 (GDD 6.3)
        /// </summary>
        public bool EnhanceEquipment(int uid, int goldCost)
        {
            var eq = equipmentList.Find(e => e.uid == uid);
            if (eq == null) return false;

            if (!GameManager.Instance.SpendGold(goldCost))
                return false;

            eq.enhanceLevel++;
            Debug.Log($"[InventoryManager] Enhanced equipment {uid} to +{eq.enhanceLevel}");
            return true;
        }
    }
}

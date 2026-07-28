using SpiritMerge.Core.Interfaces;
using SpiritMerge.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 장비 UI — 장착/강화/드롭 (GDD §6)
    /// </summary>
    public class EquipmentUI : MonoBehaviour
    {
        [Header("슬롯")]
        [SerializeField] private Transform[] equipSlots; // 무기/갑옷/반지/목걸이
        [SerializeField] private Transform inventoryRoot;
        [SerializeField] private GameObject itemPrefab;

        [Header("정보")]
        [SerializeField] private TMP_Text selectedInfo;
        [SerializeField] private Button enhanceButton;
        [SerializeField] private Button equipButton;

        [Inject] private IInventoryService _inventory;
        [Inject] private ICurrencyService _currency;

        private int _selectedUid = -1;

        public void Refresh()
        {
            // 인벤토리 목록 새로고침
            foreach (Transform child in inventoryRoot) Destroy(child.gameObject);
            foreach (var eq in _inventory.GetAllEquipment())
            {
                var go = Instantiate(itemPrefab, inventoryRoot);
                int uid = eq.uid;
                // 간단 표시
                go.GetComponentInChildren<TMP_Text>().text = $"{eq.dataId} +{eq.enhanceLevel}";
            }
        }

        public void OnItemClicked(int uid)
        {
            _selectedUid = uid;
            selectedInfo.text = $"Selected: {uid}";
        }

        public void OnEnhanceClicked()
        {
            if (_selectedUid < 0) return;
            if (_inventory.EnhanceEquipment(_selectedUid))
                Refresh();
        }
    }
}

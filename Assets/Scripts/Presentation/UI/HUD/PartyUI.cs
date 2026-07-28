using System.Collections.Generic;
using SpiritMerge.Core.Interfaces;
using UnityEngine;
using VContainer;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 파티 편성 UI — 4슬롯 + 보유 정령 목록
    /// SRP: UI 표시/입력만, 로직은 PartyService에 위임
    /// </summary>
    public class PartyUI : MonoBehaviour
    {
        [SerializeField] private Transform partySlotsRoot;      // 4개 파티 슬롯 부모
        [SerializeField] private Transform spiritListRoot;      // 보유 정령 목록 부모
        [SerializeField] private GameObject slotPrefab;         // SpiritSlotWidget 프리팹

        [Inject] private IPartyService _partyService;
        [Inject] private ISpiritService _spiritService;

        private readonly List<GameObject> _partySlotObjects = new();
        private readonly List<GameObject> _spiritSlotObjects = new();
        private int _selectedSlot = -1;

        private void Start()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            ClearSlots();
            BuildPartySlots();
            BuildSpiritList();
        }

        private void BuildPartySlots()
        {
            for (int i = 0; i < 4; i++)
            {
                var go = Instantiate(slotPrefab, partySlotsRoot);
                var slot = go.GetComponent<Widgets.SpiritSlotWidget>();
                _partySlotObjects.Add(go);

                int slotIndex = i;
                if (!_partyService.IsSlotEmpty(i))
                {
                    var spirit = _spiritService.GetSpirit(_partyService.PartySlotIds[i]);
                    if (spirit != null)
                    {
                        slot.SetSpirit(spirit.uid, spirit.dataId, $"{spirit.grade}★", Color.white);
                    }
                }
                else
                {
                    slot.SetEmpty();
                }
            }
        }

        private void BuildSpiritList()
        {
            var spirits = _spiritService.GetAllSpirits();
            foreach (var spirit in spirits)
            {
                var go = Instantiate(slotPrefab, spiritListRoot);
                var slot = go.GetComponent<Widgets.SpiritSlotWidget>();
                _spiritSlotObjects.Add(go);

                slot.SetSpirit(spirit.uid, spirit.dataId, $"{spirit.grade}★", Color.white);
            }
        }

        private void ClearSlots()
        {
            foreach (var go in _partySlotObjects) Destroy(go);
            foreach (var go in _spiritSlotObjects) Destroy(go);
            _partySlotObjects.Clear();
            _spiritSlotObjects.Clear();
        }

        /// <summary>
        /// 파티 슬롯 터치 → 정렬 선택 모드
        /// </summary>
        public void OnPartySlotTapped(int slotIndex)
        {
            _selectedSlot = slotIndex;
        }

        /// <summary>
        /// 보유 정령 터치 → 선택된 파티 슬롯에 배치
        /// </summary>
        public void OnSpiritTapped(int spiritUid)
        {
            if (_selectedSlot >= 0)
            {
                _partyService.AssignSpirit(_selectedSlot, spiritUid);
                RefreshUI();
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using SpiritMerge.Core.Interfaces;
using SpiritMerge.Presentation.UI.Widgets;
using UnityEngine;
using VContainer;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 머지 보드 UI (GDD §4)
    /// SRP: 머지 보드 표시 + 유저 입력 → IMergeService에 위임
    /// </summary>
    public class MergeUI : MonoBehaviour
    {
        [Header("보유 정령 목록")]
        [SerializeField] private Transform spiritListRoot;
        [SerializeField] private GameObject spiritSlotPrefab; // SpiritSlotWidget

        [Header("머지 보드")]
        [SerializeField] private Transform mergeBoardRoot;    // 머지 재료 슬롯들
        [SerializeField] private int maxMergeSlots = 3;       // 최대 3마리 동시 배치

        [Header("버튼")]
        [SerializeField] private GameObject mergeButton;
        [SerializeField] private GameObject crossMergeButton;
        [SerializeField] private GameObject mergeResultPopup;

        [Header("정보 표시")]
        [SerializeField] private UnityEngine.UI.Slider progressBar;
        [SerializeField] private TMPro.TMP_Text infoText;

        [Inject] private ISpiritService _spiritService;
        [Inject] private IMergeService _mergeService;

        private readonly List<SpiritSlotWidget> _ownedSlots = new();
        private readonly List<MergeSlotWidget> _boardSlots = new();
        private readonly List<int> _selectedUids = new();      // 보드에 올린 정령 UID

        private void Start()
        {
            // 버튼은 비활성화 상태로 두되 안 보이진 않게
            if (mergeButton != null) mergeButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            if (crossMergeButton != null) crossMergeButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
            if (mergeResultPopup != null) mergeResultPopup.SetActive(false);
            RefreshBoard();
        }

        /// <summary>
        /// 머지 보드 새로고침
        /// </summary>
        public void RefreshBoard()
        {
            ClearSlots();
            BuildOwnedList();
            BuildBoardSlots();
            UpdateMergeButton();
        }

        private void BuildOwnedList()
        {
            if (_spiritService == null || spiritSlotPrefab == null || spiritListRoot == null) return;

            var spirits = _spiritService.GetAllSpirits()
                .Where(s => !_selectedUids.Contains(s.uid)) // 보드에 있는 정령 제외
                .ToList();

            foreach (var spirit in spirits)
            {
                var go = Instantiate(spiritSlotPrefab, spiritListRoot);
                var slot = go.GetComponent<SpiritSlotWidget>();
                int uid = spirit.uid;
                // 임시: spirit.dataId를 이름으로 사용
                slot.SetSpirit(uid, spirit.dataId, $"{spirit.grade}★", Color.white);
                _ownedSlots.Add(slot);
            }
        }

        private void BuildBoardSlots()
        {
            if (mergeBoardRoot == null) return;

            for (int i = 0; i < maxMergeSlots; i++)
            {
                var go = new GameObject($"MergeSlot_{i}", typeof(RectTransform));
                go.transform.SetParent(mergeBoardRoot, false);
                // 실제 사용 시 MergeSlotWidget 프리팹에서 Instantiate
                var slot = go.AddComponent<MergeSlotWidget>();
                slot.Clear();
                _boardSlots.Add(slot);
            }
        }

        /// <summary>
        /// 보유 정령 클릭 → 머지 보드에 올리기
        /// </summary>
        public void OnSpiritClicked(int spiritUid)
        {
            if (_selectedUids.Count >= maxMergeSlots) return;
            if (_selectedUids.Contains(spiritUid)) return;

            _selectedUids.Add(spiritUid);
            int idx = _selectedUids.Count - 1;
            var spirit = _spiritService.GetSpirit(spiritUid);
            if (spirit != null && idx < _boardSlots.Count)
            {
                _boardSlots[idx].SetSpirit(spiritUid, spirit.dataId, spirit.grade, Color.white);
            }

            RefreshOwnedList();
            UpdateMergeButton();
        }

        /// <summary>
        /// 머지 보드 슬롯 클릭 → 정령 내리기
        /// </summary>
        public void OnBoardSlotClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _selectedUids.Count) return;

            _selectedUids.RemoveAt(slotIndex);
            // 보드 슬롯 재정렬
            for (int i = 0; i < maxMergeSlots; i++)
            {
                if (i < _selectedUids.Count)
                {
                    var s = _spiritService.GetSpirit(_selectedUids[i]);
                    _boardSlots[i].SetSpirit(s.uid, s.dataId, s.grade, Color.white);
                }
                else
                {
                    _boardSlots[i].Clear();
                }
            }

            RefreshOwnedList();
            UpdateMergeButton();
        }

        /// <summary>
        /// 머지 실행 (동일 정령)
        /// </summary>
        public void ExecuteMerge()
        {
            if (_selectedUids.Count < 2) return;

            // 보드의 모든 정령이 같은 dataId, 같은 grade인지 확인
            var first = _spiritService.GetSpirit(_selectedUids[0]);
            if (first == null) return;

            int grade = first.grade;
            string dataId = first.dataId;

            // 모두 동일한지 확인
            bool allSame = _selectedUids.All(uid =>
            {
                var s = _spiritService.GetSpirit(uid);
                return s != null && s.dataId == dataId && s.grade == grade;
            });

            if (!allSame)
            {
                infoText.text = "같은 정령만 머지할 수 있습니다";
                return;
            }

            // 머지 실행
            var result = _mergeService.Merge(dataId, grade);
            if (result != null)
            {
                _selectedUids.Clear();
                foreach (var slot in _boardSlots) slot.Clear();
                RefreshOwnedList();
                UpdateMergeButton();
                ShowMergeResult(result.dataId, result.grade, null);
            }
        }

        /// <summary>
        /// 크로스 머지 실행 (다른 속성)
        /// </summary>
        public void ExecuteCrossMerge()
        {
            if (_selectedUids.Count < 3) return;

            var result = _mergeService.CrossMerge(_selectedUids.ToArray());
            if (result != null)
            {
                _selectedUids.Clear();
                foreach (var slot in _boardSlots) slot.Clear();
                RefreshOwnedList();
                UpdateMergeButton();
                ShowMergeResult(result.dataId, result.grade, null);
            }
        }

        private void ShowMergeResult(string name, int grade, Sprite icon)
        {
            if (mergeResultPopup != null)
            {
                var popup = mergeResultPopup.GetComponent<MergeResultPopup>();
                if (popup != null) popup.Show(name, grade, icon);
            }
            infoText.text = $"✨ {name} {grade}★ 생성!";
        }

        private void UpdateMergeButton()
        {
            bool hasSelection = _selectedUids.Count >= 2;
            bool isSame = hasSelection && AreAllSelectedSame();

            if (mergeButton != null)
            {
                var btn = mergeButton.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) btn.interactable = hasSelection && isSame;
                mergeButton.SetActive(true);
            }
            if (crossMergeButton != null)
            {
                var btn = crossMergeButton.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) btn.interactable = _selectedUids.Count == 3 && !isSame;
                crossMergeButton.SetActive(true);
            }

            if (infoText != null && hasSelection)
            {
                var first = _spiritService.GetSpirit(_selectedUids[0]);
                if (first != null)
                {
                    int needed = _mergeService.GetRequiredCount(first.grade);
                    infoText.text = isSame
                        ? $"{_selectedUids.Count}/{needed} — 머지 가능"
                        : "크로스 머지 (3마리)";
                }
            }
            else if (infoText != null)
            {
                infoText.text = "정령을 선택하세요";
            }
        }

        private bool AreAllSelectedSame()
        {
            if (_selectedUids.Count < 2) return false;
            var first = _spiritService.GetSpirit(_selectedUids[0]);
            if (first == null) return false;
            return _selectedUids.All(uid =>
            {
                var s = _spiritService.GetSpirit(uid);
                return s != null && s.dataId == first.dataId && s.grade == first.grade;
            });
        }

        private void RefreshOwnedList()
        {
            foreach (var slot in _ownedSlots) Destroy(slot.gameObject);
            _ownedSlots.Clear();
            BuildOwnedList();
        }

        private void ClearSlots()
        {
            foreach (var slot in _ownedSlots) Destroy(slot.gameObject);
            foreach (var slot in _boardSlots) Destroy(slot.gameObject);
            _ownedSlots.Clear();
            _boardSlots.Clear();
        }
    }
}

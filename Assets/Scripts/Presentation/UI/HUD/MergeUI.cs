using UnityEngine;
using UnityEngine.UI;
using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 머지 보드 UI — 정령 슬롯 드래그 & 머지
    /// </summary>
    public class MergeUI : MonoBehaviour
    {
        [SerializeField] private Transform boardSlotRoot;
        [SerializeField] private GameObject mergeResultPreview;
        [SerializeField] private Button mergeButton;
        [SerializeField] private Button crossMergeButton;

        // 선택된 정령 UID 목록
        private int[] _selectedSpiritIds = new int[0];

        public void OnSpiritSelected(int uid)
        {
            // TODO: 정령 선택 → 프리뷰 업데이트
        }

        public void ExecuteMerge()
        {
            // IMergeService 호출
        }

        public void ExecuteCrossMerge()
        {
            // IMergeService.CrossMerge 호출
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritMerge.Merge
{
    /// <summary>
    /// 머지 보드 슬롯 드래그 앤 드롭 핸들러
    /// - 드래그 시작: 고스트 아이콘 생성 + MergeBoardManager.OnDragBegin
    /// - 드래그 중: 고스트가 포인터를 따라감
    /// - 드롭: 고스트 제거 + MergeBoardManager.OnDragEnd(대상 슬롯 판정)
    /// </summary>
    public class SpiritDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private MergeBoardManager board;
        private int slotIndex;
        private Canvas canvas;
        private Image ghost;

        public void Setup(MergeBoardManager b, int idx, Canvas c)
        {
            board = b;
            slotIndex = idx;
            canvas = c;
        }

        /// <summary>
        /// ⭐ 정령이 다른 슬롯으로 이동됐을 때 호출 — 드래그 핸들러의 슬롯 인덱스 동기화
        /// (안 하면 이동한 정령을 다시 드래그/합성할 때 옛 슬롯을 참조해서 실패)
        /// </summary>
        public void UpdateSlotIndex(int idx) => slotIndex = idx;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (board == null || canvas == null) return;

            // 고스트 아이콘 생성 (원본 이미지 따라가기)
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            ghost = go.GetComponent<Image>();
            ghost.raycastTarget = false;
            ghost.preserveAspect = true;
            var src = GetComponent<Image>();
            if (src != null) ghost.sprite = src.sprite;
            ghost.color = new Color(1f, 1f, 1f, 0.75f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90, 90);

            board.OnDragBegin(slotIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost != null)
                ghost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
            board?.OnDragEnd(slotIndex, eventData.position);
        }
    }
}

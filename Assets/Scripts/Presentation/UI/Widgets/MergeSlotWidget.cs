using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.Widgets
{
    /// <summary>
    /// 머지 보드 슬롯 — 머지 재료를 놓는 칸
    /// </summary>
    public class MergeSlotWidget : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text gradeLabel;
        [SerializeField] private Image elementBadge;
        [SerializeField] private GameObject emptyOverlay;
        [SerializeField] private GameObject highlight;

        public int SpiritUid { get; private set; } = -1;
        public bool IsEmpty => SpiritUid < 0;

        public void SetSpirit(int uid, string name, int grade, Color elementColor)
        {
            SpiritUid = uid;
            nameLabel.text = name;
            gradeLabel.text = $"{grade}★";
            elementBadge.color = elementColor;
            emptyOverlay.SetActive(false);
            icon.gameObject.SetActive(true);
            highlight.SetActive(false);
        }

        public void Clear()
        {
            SpiritUid = -1;
            emptyOverlay.SetActive(true);
            icon.gameObject.SetActive(false);
            highlight.SetActive(false);
        }

        public void SetHighlight(bool on) => highlight.SetActive(on);

        public void OnPointerClick(PointerEventData eventData)
        {
            // MergeUI가 이벤트 수신
        }
    }
}

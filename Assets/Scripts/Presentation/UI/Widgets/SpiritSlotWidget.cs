using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.Widgets
{
    /// <summary>
    /// 정령 슬롯 위젯 — 파티 편성/머지 보드에서 사용
    /// SRP: 단일 정령 표시 + 클릭/드래그 이벤트만 담당
    /// </summary>
    public class SpiritSlotWidget : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image spiritIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private Image elementIcon;
        [SerializeField] private GameObject emptyOverlay;
        [SerializeField] private GameObject selectedHighlight;

        public int SpiritUid { get; private set; } = -1;
        public bool IsEmpty => SpiritUid < 0;

        /// <summary>
        /// 슬롯에 정령 표시
        /// </summary>
        public void SetSpirit(int uid, string name, string grade, Color elementColor)
        {
            SpiritUid = uid;
            nameText.text = name;
            gradeText.text = grade;
            elementIcon.color = elementColor;

            if (spiritIcon != null) spiritIcon.gameObject.SetActive(true);
            if (emptyOverlay != null) emptyOverlay.SetActive(false);
            if (nameText != null) nameText.gameObject.SetActive(true);
            if (gradeText != null) gradeText.gameObject.SetActive(true);

            selectedHighlight?.SetActive(false);
        }

        /// <summary>
        /// 빈 슬롯 표시
        /// </summary>
        public void SetEmpty()
        {
            SpiritUid = -1;
            if (spiritIcon != null) spiritIcon.gameObject.SetActive(false);
            if (emptyOverlay != null) emptyOverlay.SetActive(true);
            if (nameText != null) nameText.gameObject.SetActive(false);
            if (gradeText != null) gradeText.gameObject.SetActive(false);
            selectedHighlight?.SetActive(false);
        }

        public void SetSelected(bool selected)
        {
            selectedHighlight?.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 상위 UI가 이 이벤트를 받아서 처리
        }
    }
}

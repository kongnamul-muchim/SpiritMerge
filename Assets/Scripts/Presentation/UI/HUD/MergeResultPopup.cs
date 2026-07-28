using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 머지 결과 팝업 — 머지 성공 시 표시
    /// </summary>
    public class MergeResultPopup : MonoBehaviour
    {
        [SerializeField] private Image spiritIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private GameObject root;

        private void Awake() => root.SetActive(false);

        public void Show(string spiritName, int newGrade, Sprite icon)
        {
            nameText.text = spiritName;
            gradeText.text = $"{newGrade}★";
            if (spiritIcon != null) spiritIcon.sprite = icon;
            root.SetActive(true);
        }

        public void Close() => root.SetActive(false);
    }
}

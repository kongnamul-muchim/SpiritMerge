using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 도감 UI — 정령 수집 현황
    /// </summary>
    public class CodexUI : MonoBehaviour
    {
        [SerializeField] private Transform gridRoot;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private TMP_Text progressText;
    }
}

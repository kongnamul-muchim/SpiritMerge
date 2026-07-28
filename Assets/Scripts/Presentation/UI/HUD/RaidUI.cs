using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 레이드 UI — 허수아비 타격 + 점수
    /// </summary>
    public class RaidUI : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Transform damageIndicator;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text rankText;
    }
}

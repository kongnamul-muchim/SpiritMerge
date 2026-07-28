using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 소환(뽑기) UI
    /// </summary>
    public class SummonUI : MonoBehaviour
    {
        [SerializeField] private Button normalSummonButton;
        [SerializeField] private Button premiumSummonButton;
        [SerializeField] private Button pickupSummonButton;
        [SerializeField] private Transform resultPanel;
    }
}

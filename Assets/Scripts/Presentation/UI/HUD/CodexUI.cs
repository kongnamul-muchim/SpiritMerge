using SpiritMerge.Core.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 도감 UI — 정령 수집 현황 + 보상 (GDD §11)
    /// </summary>
    public class CodexUI : MonoBehaviour
    {
        [SerializeField] private Transform gridRoot;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text rewardText;

        private CodexService _codex = new CodexService();

        public void Refresh()
        {
            int total = _codex.TotalRegistered;
            progressText.text = $"수집: {total}/30";

            var reward = CalculateReward(total);
            rewardText.text = $"보너스: ATK+{reward.bonusATK} DEF+{reward.bonusDEF} HP+{reward.bonusHP}";
        }

        private (int bonusATK, int bonusDEF, int bonusHP) CalculateReward(int total)
        {
            if (total >= 30) return (100, 50, 200);
            if (total >= 20) return (40, 20, 0);
            if (total >= 10) return (20, 0, 0);
            return (0, 0, 0);
        }
    }
}

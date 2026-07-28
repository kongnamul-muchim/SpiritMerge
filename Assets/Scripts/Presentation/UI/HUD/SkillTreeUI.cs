using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpiritMerge.Core.Interfaces;
using VContainer;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 스킬 트리 UI — 공통 → 공격/방어 분기 (GDD §5.3)
    /// </summary>
    public class SkillTreeUI : MonoBehaviour
    {
        [Header("스킬 노드")]
        [SerializeField] private Button[] commonNodes;    // 4개
        [SerializeField] private Button[] attackNodes;    // 6개
        [SerializeField] private Button[] defenseNodes;   // 6개
        [SerializeField] private TMP_Text spText;
        [SerializeField] private TMP_Text levelText;

        private int[] _nodeCosts = { 1, 1, 1, 1,       // common: ATK, DEF, HP, SPD
                                     2, 2, 2, 2, 2, 2,  // attack (6 nodes)
                                     2, 2, 2, 2, 2, 2 };// defense (6 nodes)

        [Inject] private IPlayerService _player;

        private void Start() => Refresh();

        public void Refresh()
        {
            if (spText != null) spText.text = $"SP: {_player.SkillPoints}";
            if (levelText != null) levelText.text = $"Lv.{_player.Level}";
            UpdateNodeVisuals();
        }

        /// <summary>
        /// 노드 클릭 시 SP 소모 후 스탯 증가
        /// </summary>
        public void OnNodeClicked(int nodeIndex)
        {
            if (_player.SpendSkillPoint(nodeIndex))
            {
                Refresh();
            }
        }

        private void UpdateNodeVisuals()
        {
            for (int i = 0; i < _player.SkillTreeLevels.Length && i < commonNodes.Length; i++)
            {
                if (commonNodes[i] != null)
                    commonNodes[i].interactable = _player.SkillPoints > 0;
            }
        }
    }
}

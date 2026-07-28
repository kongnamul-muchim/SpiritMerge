using UnityEngine;
using UnityEngine.UI;
using SpiritMerge.Core.Interfaces;
using VContainer;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 전투 UI — MainScene 위에 오버레이되는 전투 화면
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [SerializeField] private GameObject battleRoot;
        [SerializeField] private Transform spiritSlotRoot;
        [SerializeField] private Transform enemySlotRoot;
        [SerializeField] private Button autoSkillButton;
        [SerializeField] private GameObject[] skillButtons;
        [SerializeField] private Slider progressBar;

        [Inject] private IBattleService _battle;
        [Inject] private IPlayerService _player;

        private bool _isAutoMode = true;

        private void Start()
        {
            battleRoot.SetActive(false);
        }

        public void OpenBattle(int region, int stage)
        {
            battleRoot.SetActive(true);
            _battle.StartBattle(region, stage);
            UpdateUI();
        }

        public void CloseBattle()
        {
            battleRoot.SetActive(false);
            _battle.StopBattle();
        }

        public void ToggleAutoMode()
        {
            _isAutoMode = !_isAutoMode;
            // UI 업데이트
        }

        private void UpdateUI()
        {
            if (progressBar != null)
                progressBar.value = _battle.GetBattleProgress();
        }
    }
}

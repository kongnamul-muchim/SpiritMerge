using SpiritMerge.Core.Interfaces;
using TMPro;
using UnityEngine;
using VContainer;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 메인 UI — 상단 재화바 + 하단 메뉴 + 화면 전환
    /// SRP: UI 업데이트와 패널 전환만 담당
    /// OCP: Initialize()로 의존성 주입 (UIBuilder가 호출)
    /// </summary>
    public class MainUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text rubyText;
        [SerializeField] private TMP_Text spiritStoneText;
        [SerializeField] private TMP_Text levelText;

        [SerializeField] private GameObject[] menuButtons;
        [SerializeField] private GameObject mergePanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject partyPanel;
        [SerializeField] private GameObject summonPanel;
        [SerializeField] private GameObject codexPanel;
        [SerializeField] private GameObject missionPanel;
        [SerializeField] private GameObject dispatchPanel;
        [SerializeField] private GameObject raidPanel;

        [Inject] private ICurrencyService _currency;
        [Inject] private IPlayerService _player;

        private GameObject _currentPanel;

        /// <summary>
        /// UIBuilder가 프리팹 생성 후 호출 — 모든 패널 참조 설정
        /// </summary>
        public void Initialize(GameObject merge, GameObject battle, GameObject party,
            GameObject summon, GameObject codex, GameObject mission,
            GameObject dispatch, GameObject raid,
            TMP_Text level, TMP_Text gold, TMP_Text ruby, TMP_Text stone)
        {
            mergePanel    = merge;
            battlePanel   = battle;
            partyPanel    = party;
            summonPanel   = summon;
            codexPanel    = codex;
            missionPanel  = mission;
            dispatchPanel = dispatch;
            raidPanel     = raid;
            levelText     = level;
            goldText      = gold;
            rubyText      = ruby;
            spiritStoneText = stone;
        }

        private void Start()
        {
            ShowPanel(mergePanel);
        }

        private void Update()
        {
            if (goldText != null)
                goldText.text = _currency.Gold.ToString("N0");
            if (rubyText != null)
                rubyText.text = _currency.Ruby.ToString("N0");
            if (spiritStoneText != null)
                spiritStoneText.text = _currency.SpiritStone.ToString("N0");
            if (levelText != null)
                levelText.text = $"Lv.{_player.Level}";
        }

        public void ShowPanel(GameObject panel)
        {
            if (_currentPanel == panel) return;
            if (_currentPanel != null) _currentPanel.SetActive(false);
            _currentPanel = panel;
            if (_currentPanel != null) _currentPanel.SetActive(true);
        }

        public void OnClickBattle()   => ShowPanel(battlePanel);
        public void OnClickMerge()    => ShowPanel(mergePanel);
        public void OnClickParty()    => ShowPanel(partyPanel);
        public void OnClickSummon()   => ShowPanel(summonPanel);
        public void OnClickCodex()    => ShowPanel(codexPanel);
        public void OnClickMission()  => ShowPanel(missionPanel);
        public void OnClickDispatch() => ShowPanel(dispatchPanel);
        public void OnClickRaid()     => ShowPanel(raidPanel);
    }
}

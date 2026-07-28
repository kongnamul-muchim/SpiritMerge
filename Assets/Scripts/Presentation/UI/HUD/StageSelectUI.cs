using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpiritMerge.Core.Interfaces;
using SpiritMerge.Core.Systems;
using SpiritMerge.Presentation.Battle;
using VContainer;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 스테이지 선택 UI — 지역/스테이지 이동 + 전투 시작
    /// </summary>
    public class StageSelectUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private Button battleButton;
        [SerializeField] private Button nextStageButton;
        [SerializeField] private Button prevStageButton;
        [SerializeField] private BattlePresenter battlePresenter;

        [Inject] private IStageProgressionService _progression;

        private void Start()
        {
            battleButton?.onClick.AddListener(StartBattle);
            nextStageButton?.onClick.AddListener(() => { _progression.NextStage(); Refresh(); });
            prevStageButton?.onClick.AddListener(() => { /* 이전 스테이지 */ });
            Refresh();
        }

        public void Refresh()
        {
            stageLabel.text = $"지역 {_progression.CurrentRegion} — {_progression.CurrentStageName}";
            stageLabel.text += _progression.IsBossStage ? " 👑 BOSS" : "";
        }

        public void StartBattle()
        {
            if (battlePresenter != null)
                battlePresenter.BeginBattle(_progression.CurrentRegion, _progression.CurrentStage);
        }
    }
}

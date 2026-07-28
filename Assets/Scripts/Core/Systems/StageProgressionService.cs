using SpiritMerge.Core.Interfaces;
using SpiritMerge.Presentation.Common;
using UnityEngine;
using VContainer;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 스테이지 진행 관리 — 지역/스테이지 이동, 보상, 저장
    /// SRP: 게임 진행 상태 + 보상 지급만 담당
    /// </summary>
    public class StageProgressionService : IStageProgressionService
    {
        public int CurrentRegion { get; private set; } = 1;
        public int CurrentStage { get; private set; } = 1;
        public int ClearedStage { get; private set; }

        private readonly IPlayerService _player;
        private readonly ICurrencyService _currency;

        public StageProgressionService(IPlayerService player, ICurrencyService currency)
        {
            _player = player;
            _currency = currency;
        }

        public string CurrentStageName => $"{CurrentRegion}-{CurrentStage}";
        public bool IsBossStage => CurrentStage % 10 == 0;
        public int MaxStage => 10;

        /// <summary>
        /// 다음 스테이지로 이동
        /// </summary>
        public bool NextStage()
        {
            if (CurrentStage >= MaxStage)
            {
                // 다음 지역으로
                if (CurrentRegion >= 7) return false; // 최대 지역
                CurrentRegion++;
                CurrentStage = 1;
            }
            else
            {
                CurrentStage++;
            }
            return true;
        }

        /// <summary>
        /// 전투 승리 시 보상 지급
        /// </summary>
        public void RewardClear()
        {
            int gold = 100 + CurrentStage * 20 + (CurrentRegion - 1) * 200;
            int exp = 50 + CurrentStage * 10 + (CurrentRegion - 1) * 100;

            if (IsBossStage) gold *= 2;
            if (IsBossStage) exp *= 2;

            _currency.AddGold(gold);
            _player.AddExp(exp);

            if (CurrentStage > ClearedStage)
                ClearedStage = CurrentStage;

            Debug.Log($"[Progression] {CurrentStageName} clear! Gold+{gold} Exp+{exp}");
        }

        /// <summary>
        /// 저장 데이터 복원
        /// </summary>
        public void LoadProgress(int region, int stage, int cleared)
        {
            CurrentRegion = region;
            CurrentStage = stage;
            ClearedStage = cleared;
        }
    }
}

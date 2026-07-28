namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 스테이지 진행 인터페이스
    /// </summary>
    public interface IStageProgressionService
    {
        int CurrentRegion { get; }
        int CurrentStage { get; }
        int ClearedStage { get; }
        string CurrentStageName { get; }
        bool IsBossStage { get; }
        int MaxStage { get; }

        bool NextStage();
        void RewardClear();
        void LoadProgress(int region, int stage, int cleared);
    }
}

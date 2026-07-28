namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 정령 머지(합성) 로직 (OCP: 새로운 머지 방식 추가 시 확장)
    /// </summary>
    public interface IMergeService
    {
        OwnedSpirit Merge(string dataId, int currentGrade);
        OwnedSpirit CrossMerge(int[] spiritUids);
        bool CanMerge(string dataId, int currentGrade);
        int GetRequiredCount(int currentGrade);
    }
}

namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 저장/로드 시스템 (SRP: 데이터 영속성만 담당)
    /// </summary>
    public interface IDataService
    {
        void Save(SaveData data);
        SaveData Load();
        void Delete();
        bool HasSave { get; }
    }
}

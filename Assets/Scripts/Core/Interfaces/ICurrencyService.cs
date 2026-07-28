namespace SpiritMerge.Core.Interfaces
{
    /// <summary>
    /// 재화 관리 (DIP: 구체적인 구현에 의존하지 않음)
    /// </summary>
    public interface ICurrencyService
    {
        int Gold { get; }
        int Ruby { get; }
        int SpiritStone { get; }
        int[] ElementStones { get; }

        void AddGold(int amount);
        bool SpendGold(int amount);
        void AddRuby(int amount);
        bool SpendRuby(int amount);
        void AddSpiritStone(int amount);
        bool SpendSpiritStone(int amount);
    }
}

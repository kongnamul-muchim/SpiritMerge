using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// 재화 관리 서비스 (SRP: 3종 재화 증감만 담당)
    /// </summary>
    public class CurrencyService : ICurrencyService
    {
        public int Gold { get; private set; }
        public int Ruby { get; private set; }
        public int SpiritStone { get; private set; }
        public int[] ElementStones { get; private set; } = new int[6];

        public void AddGold(int amount)
        {
            Gold = UnityEngine.Mathf.Max(0, Gold + amount);
        }

        public bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public void AddRuby(int amount)
        {
            Ruby = UnityEngine.Mathf.Max(0, Ruby + amount);
        }

        public bool SpendRuby(int amount)
        {
            if (Ruby < amount) return false;
            Ruby -= amount;
            return true;
        }

        public void AddSpiritStone(int amount)
        {
            SpiritStone = UnityEngine.Mathf.Max(0, SpiritStone + amount);
        }

        public bool SpendSpiritStone(int amount)
        {
            if (SpiritStone < amount) return false;
            SpiritStone -= amount;
            return true;
        }
    }
}

using SpiritMerge.Core.Interfaces;
using SpiritMerge.Core.Systems;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SpiritMerge.Infrastructure.DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private SpiritData[] spiritDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // ─── Services ────────────────────────────
            builder.Register<ISpiritService, SpiritService>(Lifetime.Singleton);
            builder.Register<IBattleService, BattleService>(Lifetime.Singleton);
            builder.Register<IPlayerService, PlayerService>(Lifetime.Singleton);
            builder.Register<IDataService, DataService>(Lifetime.Singleton);
            builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);
            builder.Register<ICurrencyService, CurrencyService>(Lifetime.Singleton);
            builder.Register<IMergeService, MergeService>(Lifetime.Singleton);
            builder.Register<IPartyService, PartyService>(Lifetime.Singleton);

            // ─── Entry Point ─────────────────────────
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}

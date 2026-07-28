using SpiritMerge.Core.Interfaces;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SpiritMerge.Infrastructure.DI
{
    /// <summary>
    /// 게임 진입점 — DI 컨테이너 구축 완료 후 최초 실행
    /// SRP: 게임 초기화 (저장 로드, Scene 로드)만 담당
    /// DIP: 모든 의존성을 VContainer가 주입
    /// </summary>
    public class GameEntryPoint : IStartable
    {
        private readonly IDataService _dataService;
        private readonly IPlayerService _playerService;
        private readonly ICurrencyService _currencyService;

        // 생성자 주입 — VContainer가 자동 제공
        public GameEntryPoint(
            IDataService dataService,
            IPlayerService playerService,
            ICurrencyService currencyService)
        {
            _dataService = dataService;
            _playerService = playerService;
            _currencyService = currencyService;
        }

        void IStartable.Start()
        {
            Debug.Log("[GameEntryPoint] Starting game...");

            // 저장 데이터 불러오기
            var saveData = _dataService.Load();
            if (saveData != null)
            {
                Debug.Log($"[GameEntryPoint] Save loaded: Lv.{saveData.playerLevel}");
            }
            else
            {
                Debug.Log("[GameEntryPoint] New game started");
            }
        }
    }
}

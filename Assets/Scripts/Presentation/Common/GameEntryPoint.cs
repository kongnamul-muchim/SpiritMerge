using SpiritMerge.Core.Interfaces;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SpiritMerge.Infrastructure.DI
{
    /// <summary>
    /// 게임 진입점 — DI 컨테이너 구축 완료 후 최초 실행
    /// SRP: 게임 초기화 (저장 로드, Scene 로드)
    /// DIP: 모든 의존성을 VContainer가 주입
    /// </summary>
    public class GameEntryPoint : IStartable
    {
        private readonly IDataService _dataService;

        // 생성자 주입 — VContainer가 자동 제공
        public GameEntryPoint(
            IDataService dataService)
        {
            _dataService = dataService;
        }

        void IStartable.Start()
        {
            Debug.Log("[GameEntryPoint] Starting game...");

            // 에디터 비포커스 상태에서도 게임 시간이 멈추지 않도록 설정 (CLI 자동 테스트용)
            Application.runInBackground = true;

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

            Debug.Log("[GameEntryPoint] Ready — use CliTestSuite.CmdTestAll via TCP CLI");
        }
    }
}

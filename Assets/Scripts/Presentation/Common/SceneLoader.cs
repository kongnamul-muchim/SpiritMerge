using UnityEngine;

namespace SpiritMerge.Presentation.Common
{
    /// <summary>
    /// Scene 전환 유틸리티
    /// 전투는 MainScene 안에서 UI 패널로 처리 (별도 씬 ❌)
    /// </summary>
    public static class SceneLoader
    {
        public static void LoadMain()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }

        public static void LoadInit()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("InitScene");
        }

        /// <summary>
        /// InitScene → MainScene 전환 (게임 시작)
        /// </summary>
        public static void StartGame()
        {
            LoadMain();
        }
    }
}

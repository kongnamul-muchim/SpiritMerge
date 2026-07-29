using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// TMP Essentials 설치 + 씬 기본 UI 연결
    /// </summary>
    public static class SetupHelper
    {
        /// <summary>
        /// TMP Essential Resources 임포트 (메뉴: Window > TextMeshPro > Import TMP Essential Resources)
        /// </summary>
        [MenuItem("SpiritMerge/Setup/Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            // TMP Essential Resources는 Package Manager를 통해 임포트
            // 에셋 메뉴를 호출하는 방식
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            Debug.Log("[SetupHelper] TMP Essentials import dialog opened — click Import");
        }

        /// <summary>
        /// 현재 씬을 InitScene으로 설정 (Build Settings)
        /// </summary>
        [MenuItem("SpiritMerge/Setup/Set Build Scenes")]
        public static void SetBuildScenes()
        {
            var scenes = new[]
            {
                "Assets/Scenes/InitScene.unity",
                "Assets/Scenes/MainScene.unity",
                "Assets/Scenes/BattleScene.unity"
            };
            var buildScenes = new EditorBuildSettingsScene[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
                buildScenes[i] = new EditorBuildSettingsScene(scenes[i], true);
            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("[SetupHelper] Build scenes set: InitScene, MainScene, BattleScene");
        }
    }
}

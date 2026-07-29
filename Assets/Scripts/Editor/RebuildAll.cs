using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// 모든 UI를 한 번에 리빌드
    /// 실행: SpiritMerge > UI > Rebuild All
    /// </summary>
    public static class RebuildAll
    {
        [MenuItem("SpiritMerge/UI/Rebuild All")]
        public static void Rebuild()
        {
            Debug.Log("[RebuildAll] === 전체 UI 리빌드 시작 ===");

            // 1. Cleanup Scene
            Debug.Log("[RebuildAll] 1/5: Cleanup Scene...");
            SceneCleanup.Cleanup();

            // 2. Rebuild TopBar
            Debug.Log("[RebuildAll] 2/5: Rebuild TopBar...");
            TopBarBuilder.Rebuild();

            // 3. Rebuild Merge UI
            Debug.Log("[RebuildAll] 3/5: Rebuild Merge UI...");
            MergeUIRebuilder.Rebuild();

            // 4. Rebuild Battle UI
            Debug.Log("[RebuildAll] 4/5: Rebuild Battle UI...");
            BattleUIBuilder.Rebuild();

            // 5. Rebuild GNB
            Debug.Log("[RebuildAll] 5/5: Rebuild GNB...");
            GNBBuilder.Rebuild();

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RebuildAll] ✅ 전체 UI 리빌드 완료!");
            EditorUtility.DisplayDialog("Rebuild All",
                "✅ 전체 UI 리빌드 완료!\n\n" +
                "1. Cleanup Scene\n" +
                "2. TopBar (Stage + Gold + Ruby)\n" +
                "3. Merge UI (4x4 Grid + Summon)\n" +
                "4. Battle UI (Enemy + Spirit + Wave)\n" +
                "5. GNB (5 tabs)\n\n" +
                "Scene 저장 완료.", "OK");
        }
    }
}

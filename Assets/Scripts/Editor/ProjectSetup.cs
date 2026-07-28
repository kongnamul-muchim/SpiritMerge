using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// Scene 생성 + 초기 셋업 자동화
    /// </summary>
    public static class ProjectSetup
    {
        private const string SceneDir = "Assets/Scenes";

        [MenuItem("SpiritMerge/Setup/1. Create Scenes")]
        public static void CreateScenes()
        {
            string[] scenes = { "InitScene", "MainScene" };

            foreach (string sceneName in scenes)
            {
                string path = $"{SceneDir}/{sceneName}.unity";
                if (!System.IO.File.Exists(path))
                {
                    Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    newScene.name = sceneName;
                    EditorSceneManager.SaveScene(newScene, path);
                    Debug.Log($"[Setup] Created scene: {sceneName}");
                }
                else
                {
                    Debug.Log($"[Setup] Scene already exists: {sceneName}");
                }
            }

            // SampleScene 제거 (선택)
            string samplePath = $"{SceneDir}/SampleScene.unity";
            if (System.IO.File.Exists(samplePath))
            {
                // Don't delete, just note it
                Debug.Log("[Setup] SampleScene kept for reference");
            }
        }

        [MenuItem("SpiritMerge/Setup/2. Create Manager Prefab")]
        public static void CreateManagerPrefab()
        {
            // Check if already exists
            string prefabPath = "Assets/Prefabs/UI/GameManager.prefab";
            if (System.IO.File.Exists(prefabPath))
            {
                Debug.Log("[Setup] GameManager prefab already exists");
                return;
            }

            GameObject managerGO = new GameObject("GameManager");
            managerGO.AddComponent<GameManager>();
            managerGO.AddComponent<DataManager>();
            managerGO.AddComponent<SpiritManager>();
            managerGO.AddComponent<BattleManager>();
            managerGO.AddComponent<InventoryManager>();

            PrefabUtility.SaveAsPrefabAsset(managerGO, prefabPath);
            Object.DestroyImmediate(managerGO);
            Debug.Log("[Setup] Created GameManager prefab with all managers");
        }

        [MenuItem("SpiritMerge/Setup/3. Full Setup")]
        public static void FullSetup()
        {
            CreateScenes();
            CreateManagerPrefab();

            // MainScene이 첫 씬이 되도록 설정
            var mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>($"{SceneDir}/MainScene.unity");
            if (mainScene != null)
            {
                var buildSettings = new EditorBuildSettingsScene[2];
                buildSettings[0] = new EditorBuildSettingsScene($"{SceneDir}/InitScene.unity", true);
                buildSettings[1] = new EditorBuildSettingsScene($"{SceneDir}/MainScene.unity", true);
                EditorBuildSettings.scenes = buildSettings;
                Debug.Log("[Setup] Build settings updated: InitScene + MainScene");
            }

            AssetDatabase.Refresh();
            Debug.Log("[Setup] Full setup complete!");
        }

        /// <summary>
        /// Unity CLI batchmode 실행용 메서드
        /// </summary>
        public static void BatchSetup()
        {
            Debug.Log("[BatchSetup] Starting project setup...");
            CreateScenes();
            CreateManagerPrefab();

            var mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>($"{SceneDir}/MainScene.unity");
            if (mainScene != null)
            {
                var buildSettings = new EditorBuildSettingsScene[2];
                buildSettings[0] = new EditorBuildSettingsScene($"{SceneDir}/InitScene.unity", true);
                buildSettings[1] = new EditorBuildSettingsScene($"{SceneDir}/MainScene.unity", true);
                EditorBuildSettings.scenes = buildSettings;
            }

            AssetDatabase.Refresh();
            Debug.Log("[BatchSetup] Batch setup complete!");
            EditorApplication.Exit(0);
        }
    }
}

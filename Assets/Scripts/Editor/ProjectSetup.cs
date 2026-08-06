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
        /// SpawnPoint 태그가 없으면 TagManager에 추가 (CLI exec: SpiritMerge.Editor.ProjectSetup.EnsureSpawnPointTag)
        /// </summary>
        [MenuItem("SpiritMerge/Setup/Add SpawnPoint Tag")]
        public static void EnsureSpawnPointTag()
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAsset == null || tagManagerAsset.Length == 0)
            {
                Debug.LogError("[Setup] TagManager.asset not found");
                return;
            }

            var tagManager = new SerializedObject(tagManagerAsset[0]);
            var tagsProp = tagManager.FindProperty("tags");
            if (tagsProp == null)
            {
                Debug.LogError("[Setup] tags property not found in TagManager");
                return;
            }

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == "SpawnPoint")
                {
                    Debug.Log("[Setup] SpawnPoint tag already exists");
                    return;
                }
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "SpawnPoint";
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] ✅ SpawnPoint tag added");
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
                // ⭐ 빌드 씬: MainScene만 (InitScene은 빈 씬이라 빌드에서 제외 — 회색 화면 방지)
                var buildSettings = new EditorBuildSettingsScene[1];
                buildSettings[0] = new EditorBuildSettingsScene($"{SceneDir}/MainScene.unity", true);
                EditorBuildSettings.scenes = buildSettings;
            }

            AssetDatabase.Refresh();
            Debug.Log("[BatchSetup] Batch setup complete!");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// ⭐ 배치 모드 WebGL 빌드 — unity.ps1 build-webgl
        /// (기존 build-webgl은 BatchSetup만 실행해 실제 빌드가 안 됐던 문제 수정)
        /// </summary>
        public static void BuildForWebGL()
        {
            // ⭐ WebGL 해상도는 ProjectSettings.asset(defaultScreenWidthWeb/HeightWeb)에서 424x753(9:16 모바일)로 설정됨
            string[] scenes = GetEnabledScenePaths();
            var report = BuildPipeline.BuildPlayer(scenes, "Builds/WebGL", BuildTarget.WebGL, BuildOptions.None);
            Debug.Log($"[Build] WebGL 결과: {report.summary.result} → Builds/WebGL");
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>
        /// ⭐ 배치 모드 Android APK 빌드 — unity.ps1 build-android
        /// </summary>
        public static void BuildForAndroid()
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                Debug.LogWarning("[Build] Android 타겟 전환 실패 (모듈/SDK 확인 필요)");

            string[] scenes = GetEnabledScenePaths();
            var report = BuildPipeline.BuildPlayer(scenes, "Builds/Android/SpiritMerge.apk", BuildTarget.Android, BuildOptions.None);
            Debug.Log($"[Build] Android 결과: {report.summary.result} → Builds/Android/SpiritMerge.apk");
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        static string[] GetEnabledScenePaths()
        {
            // ⭐ 빌드 씬: MainScene만 사용 (InitScene 빈 씬 제외 — 빌드 시작 시 회색 화면 방지)
            var list = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled && s.path.Contains("MainScene")) list.Add(s.path);
            // 만약 MainScene이 빌드 설정에 없으면 강제 추가
            if (list.Count == 0) list.Add("Assets/Scenes/MainScene.unity");
            return list.ToArray();
        }
    }
}

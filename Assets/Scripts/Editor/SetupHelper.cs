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
        /// <summary>한글 폰트 설치</summary>
        [MenuItem("SpiritMerge/Setup/Install Korean Font (Legacy)")]
        public static void InstallKoreanFontLegacy()
        {
            string[] candidates = {
                "C:/Windows/Fonts/NotoSansKR-VF.ttf", "C:/Windows/Fonts/malgun.ttf",
            };
            string srcFont = null;
            foreach (var c in candidates)
                if (System.IO.File.Exists(c)) { srcFont = c; break; }
            if (srcFont == null) { Debug.LogError("[SetupHelper] No Korean font"); return; }

            string dir = "Assets/Resources/Fonts & Materials";
            System.IO.Directory.CreateDirectory(dir);
            string ttfPath = $"{dir}/NotoSansKR.ttf";
            System.IO.File.Copy(srcFont, ttfPath, overwrite: true);
            AssetDatabase.ImportAsset(ttfPath);

            var unityFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            var tmpFont = TMPro.TMP_FontAsset.CreateFontAsset(unityFont);
            string tmpPath = $"{dir}/NotoSansKR SDF.asset";
            AssetDatabase.CreateAsset(tmpFont, tmpPath);

            // LiberationSans SDF에 Fallback 등록
            var libFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (libFont != null)
            {
                if (libFont.fallbackFontAssetTable == null)
                    libFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMPro.TMP_FontAsset>();
                if (!libFont.fallbackFontAssetTable.Contains(tmpFont))
                    libFont.fallbackFontAssetTable.Add(tmpFont);
                EditorUtility.SetDirty(libFont);
            }
            AssetDatabase.DeleteAsset(ttfPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SetupHelper] ✅ NotoSansKR installed as fallback");
        }

        /// <summary>
        /// TMP Essential Resources 임포트
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

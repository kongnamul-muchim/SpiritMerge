using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// 한글 폰트 설치 v7 — material 수동 설정, Fallback YAML 직접 수정
    /// </summary>
    public static class KoreanFontInstaller
    {
        [MenuItem("SpiritMerge/Setup/Install Korean Font")]
        public static void Install()
        {
            string dir = "Assets/Resources/Fonts & Materials";
            string ttfPath = $"{dir}/MalgunGothic.ttf";

            // 1. .ttf 확인 및 임포트
            if (!System.IO.File.Exists(ttfPath)) { Debug.LogError("[폰트] MalgunGothic.ttf 없음"); return; }
            AssetDatabase.ImportAsset(ttfPath);
            var unityFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (unityFont == null) { Debug.LogError("[폰트] Font 로드 실패"); return; }

            // 2. 기존 SDF 삭제
            string sdfPath = $"{dir}/MalgunGothic SDF.asset";
            if (System.IO.File.Exists(sdfPath)) AssetDatabase.DeleteAsset(sdfPath);

            // 3. TMP Font Asset 생성
            var tmpFont = TMPro.TMP_FontAsset.CreateFontAsset(unityFont);
            if (tmpFont == null) { Debug.LogError("[폰트] CreateFontAsset 실패"); return; }
            tmpFont.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;

            // 4. Material 설정 (TMP 기본 shader)
            if (tmpFont.material == null)
            {
                var mat = new Material(Shader.Find("TextMeshPro/Distance Field"));
                tmpFont.material = mat;
            }
            tmpFont.material.shader = Shader.Find("TextMeshPro/Distance Field");

            // 5. 저장
            AssetDatabase.CreateAsset(tmpFont, sdfPath);

            // Atlas 텍스처도 저장 (sub-asset)
            if (tmpFont.atlasTexture != null)
            {
                // 이미 tmpFont에 포함됨
            }

            EditorUtility.SetDirty(tmpFont);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            tmpFont.ReadFontAssetDefinition();

            // 6. LiberationSans SDF YAML에 Fallback 등록
            string libPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var lines = System.IO.File.ReadAllLines(libPath);
            string guid = AssetDatabase.AssetPathToGUID(sdfPath);
            string fallbackEntry = $"    - {{fileID: 11400000, guid: {guid}, type: 2}}";
            bool added = false;

            var newLines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                newLines.Add(lines[i]);
                if (lines[i].Contains("m_FaceInfo:"))
                {
                    newLines.Add($"  fallbackFontAssetTable:");
                    newLines.Add(fallbackEntry);
                    added = true;
                }
            }
            System.IO.File.WriteAllLines(libPath, newLines);
            AssetDatabase.ImportAsset(libPath);

            Debug.Log($"[폰트] ✅ 설치 완료 ({new System.IO.FileInfo(sdfPath).Length} byte, fallback:{added})");
        }
    }
}

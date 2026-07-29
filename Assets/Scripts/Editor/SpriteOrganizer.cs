using UnityEditor;
using UnityEngine;
using System.IO;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// Sprites 폴더 전체 정리 v2:
    /// - Icons/ → 속성/UI 아이콘
    /// - Enemies/Chapter{1-5}_{Fire|Water|Nature|Thunder}/ → 적 몬스터
    /// - Spirits/{Fire|Water|Nature|Thunder|Light|Dark}/ → 아군 정령
    /// - removebg-preview 접미사 제거 + 중복 정리
    /// - 모든 PNG Texture Type → Sprite
    /// 
    /// 실행: SpiritMerge > Setup > Organize All Sprites
    /// </summary>
    public static class SpriteOrganizer
    {
        static string baseDir = "Assets/Sprites";
        static string[] chapterElements = { "Fire", "Water", "Nature", "Thunder", "Fire" };

        [MenuItem("SpiritMerge/Setup/Organize All Sprites")]
        public static void Organize()
        {
            int moved = 0, fixed_ = 0, deleted = 0;

            // ── 1. 폴더 구조 생성 ──
            CreateDirectories();

            // ── 2. 모든 PNG 스캔 ──
            var allPngs = Directory.GetFiles(baseDir, "*.png", SearchOption.AllDirectories);

            foreach (var png in allPngs)
            {
                string path = png.Replace("\\", "/");
                string fileName = Path.GetFileNameWithoutExtension(path);
                string dir = Path.GetDirectoryName(path).Replace("\\", "/");

                // 이미 정리된 폴더에 있으면 스킵
                if (dir.Contains("/Icons") || dir.Contains("/Enemies") ||
                    dir.Contains("/Spirits/Fire") || dir.Contains("/Spirits/Water") ||
                    dir.Contains("/Spirits/Nature") || dir.Contains("/Spirits/Thunder") ||
                    dir.Contains("/Spirits/Light") || dir.Contains("/Spirits/Dark") ||
                    dir.Contains("/Spirits/Earth") || dir.Contains("/Spirits/Wind"))
                    continue;

                // 임시 이름 삭제
                if (fileName.StartsWith("image-"))
                {
                    AssetDatabase.DeleteAsset(path);
                    deleted++;
                    continue;
                }

                // ── 3. removebg 중복 정리 ──
                if (fileName.EndsWith("-removebg-preview"))
                {
                    // removebg 버전 우선 — 배경有 버전 삭제
                    string withBgPath = path.Replace("-removebg-preview.png", ".png");
                    if (File.Exists(withBgPath))
                    {
                        AssetDatabase.DeleteAsset(withBgPath);
                        deleted++;
                    }
                    fileName = fileName.Replace("-removebg-preview", "");
                }
                else
                {
                    // 배경有 버전 — removebg 버전이 있으면 삭제
                    string noBgPath = path.Replace(".png", "-removebg-preview.png");
                    if (File.Exists(noBgPath))
                    {
                        AssetDatabase.DeleteAsset(path);
                        deleted++;
                        continue;
                    }
                }

                // ── 4. 대상 폴더 결정 ──
                string targetDir = GetTargetDirectory(fileName, path);
                if (targetDir == null) continue;

                string targetPath = $"{targetDir}/{fileName}.png";
                if (path == targetPath) continue; // 이미 올바른 위치

                string error = AssetDatabase.MoveAsset(path, targetPath);
                if (string.IsNullOrEmpty(error))
                {
                    moved++;
                }
            }

            // ── 5. 모든 PNG Texture Type → Sprite ──
            fixed_ = FixAllTextureTypes();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Organize] ✅ 정리 완료! 이동={moved}, Sprite변환={fixed_}, 중복삭제={deleted}");
        }

        static void CreateDirectories()
        {
            // 기본 폴더
            foreach (var f in new[] { "Icons", "Enemies", "Spirits" })
                Directory.CreateDirectory($"{baseDir}/{f}");

            // 챕터별 폴더
            for (int ch = 1; ch <= 5; ch++)
                Directory.CreateDirectory($"{baseDir}/Enemies/Chapter{ch}_{chapterElements[ch - 1]}");

            // 속성별 폴더
            foreach (var elem in new[] { "Fire", "Water", "Nature", "Thunder", "Light", "Dark", "Earth", "Wind" })
                Directory.CreateDirectory($"{baseDir}/Spirits/{elem}");
        }

        static string GetTargetDirectory(string fileName, string path)
        {
            // Icons
            if (fileName.StartsWith("icon_"))
                return $"{baseDir}/Icons";

            // Enemies
            if (fileName.StartsWith("enemy_ch"))
            {
                int ch = int.Parse(fileName.Substring(8, 1)); // "enemy_ch1_01" → 1
                string elem = chapterElements[(ch - 1) % chapterElements.Length];
                return $"{baseDir}/Enemies/Chapter{ch}_{elem}";
            }

            // Spirits — SpiritData의 spriteFileName 기준
            // .asset 파일 스캔해서 매칭
            string dataDir = "Assets/Resources/Data/Spirits";
            if (Directory.Exists(dataDir))
            {
                foreach (var assetPath in Directory.GetFiles(dataDir, "*.asset"))
                {
                    string assetPathNorm = assetPath.Replace("\\", "/");
                    var spiritData = AssetDatabase.LoadAssetAtPath<SpiritData>(assetPathNorm);
                    if (spiritData == null) continue;
                    if (spiritData.spriteFileName == fileName)
                    {
                        string elemName = spiritData.element.ToString();
                        return $"{baseDir}/Spirits/{elemName}";
                    }
                }
            }

            // SpiritData 매칭 실패 → 접두사 기반 추정
            if (fileName.StartsWith("Fire")) return $"{baseDir}/Spirits/Fire";
            if (fileName.StartsWith("Water")) return $"{baseDir}/Spirits/Water";
            if (fileName.StartsWith("Earth") || fileName.StartsWith("Nature") || fileName.StartsWith("Gaia") || fileName.StartsWith("Rock") || fileName.StartsWith("Boulder") || fileName.StartsWith("Small Rock") || fileName.StartsWith("Iron Bear") || fileName.StartsWith("Bear"))
                return $"{baseDir}/Spirits/Nature";
            if (fileName.StartsWith("Wind") || fileName.StartsWith("Thunder") || fileName.StartsWith("Small Whirlwind") || fileName.StartsWith("Wind"))
                return $"{baseDir}/Spirits/Thunder";
            if (fileName.StartsWith("Light")) return $"{baseDir}/Spirits/Light";
            if (fileName.StartsWith("Dark") || fileName.StartsWith("Shadow") || fileName.StartsWith("Nightmare") || fileName.StartsWith("Dark Orb"))
                return $"{baseDir}/Spirits/Dark";

            // 기본값: Spirits 루트에 남김
            return null;
        }

        static int FixAllTextureTypes()
        {
            int fixed_ = 0;
            foreach (var png in Directory.GetFiles(baseDir, "*.png", SearchOption.AllDirectories))
            {
                string path = png.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType == TextureImporterType.Sprite) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                EditorUtility.SetDirty(importer);
                fixed_++;
            }
            return fixed_;
        }
    }
}

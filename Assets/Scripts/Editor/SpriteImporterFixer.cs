using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// Sprites/Spirits 폴더의 모든 PNG Texture Type을 Sprite로 변경
    /// 실행: SpiritMerge > Setup > Fix Sprite Import Settings
    /// </summary>
    public static class SpriteImporterFixer
    {
        [MenuItem("SpiritMerge/Setup/Fix Sprite Import Settings")]
        public static void FixAllSprites()
        {
            string dir = "Assets/Sprites/Spirits";
            var files = System.IO.Directory.GetFiles(dir, "*.png", System.IO.SearchOption.AllDirectories);
            int fixedCount = 0;

            foreach (var png in files)
            {
                string path = png.Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    fixedCount++;
                    Debug.Log($"[SpriteFix] ✅ {System.IO.Path.GetFileName(path)} → Sprite 설정 (재임포트 필요)");
                }
            }

            // 한 번에 Refresh
            if (fixedCount > 0)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"[SpriteFix] ✅ {fixedCount}개 PNG Texture Type → Sprite 변환 완료!");
                EditorUtility.DisplayDialog("Sprite Import Fix",
                    $"✅ {fixedCount}개 PNG를 Sprite로 변환 완료!\\n" +
                    "이제 SpiritMerge > Setup > Connect Spirit Sprites 를 실행하세요.", "OK");
            }
            else
            {
                Debug.Log("[SpriteFix] 이미 모두 Sprite 타입입니다.");
                EditorUtility.DisplayDialog("Sprite Import Fix",
                    "이미 모든 PNG가 Sprite 타입입니다.\\n" +
                    "바로 Connect Spirit Sprites를 실행하세요.", "OK");
            }
        }
    }
}

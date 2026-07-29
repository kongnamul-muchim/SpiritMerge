using UnityEditor;
using UnityEngine;
using System.IO;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// Assets/Sprites/Spirits/ 폴더의 PNG를 SpiritData 에셋의 sprite 필드에 자동 연결
    /// 실행: SpiritMerge > Setup > Connect Spirit Sprites
    /// </summary>
    public static class SpiritSpriteConnector
    {
        [MenuItem("SpiritMerge/Setup/Connect Spirit Sprites")]
        public static void Connect()
        {
            string spriteDir = "Assets/Sprites/Spirits";
            string dataDir = "Assets/Resources/Data/Spirits";

            if (!Directory.Exists(spriteDir))
            { Debug.LogError("[Sprites] Sprites/Spirits 폴더 없음!"); return; }
            if (!Directory.Exists(dataDir))
            { Debug.LogError("[Sprites] Data/Spirits 폴더 없음!"); return; }

            // 모든 PNG 로드
            var spriteFiles = Directory.GetFiles(spriteDir, "*.png");
            Debug.Log($"[Sprites] PNG 파일 {spriteFiles.Length}개 발견");

            // 모든 SpiritData 에셋 로드
            var dataFiles = Directory.GetFiles(dataDir, "*.asset");
            int connected = 0;

            foreach (var dataPath in dataFiles)
            {
                var spiritData = AssetDatabase.LoadAssetAtPath<SpiritData>(dataPath);
                if (spiritData == null) continue;

                // SpiritData의 spriteFileName과 일치하는 PNG 찾기
                string targetName = spiritData.spriteFileName;
                if (string.IsNullOrEmpty(targetName)) continue;

                string matchedPng = null;
                foreach (var png in spriteFiles)
                {
                    string pngName = Path.GetFileNameWithoutExtension(png);
                    if (pngName == targetName)
                    {
                        matchedPng = png;
                        break;
                    }
                }

                if (matchedPng == null)
                {
                    Debug.LogWarning($"[Sprites] '{spiritData.name}' → '{targetName}' 매칭 PNG 없음");
                    continue;
                }

                // Sprite 로드 및 연결
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(matchedPng);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Sprites] '{matchedPng}' Sprite 로드 실패 (Texture Type이 Sprite인지 확인)");
                    continue;
                }

                var serialized = new SerializedObject(spiritData);
                var spriteProp = serialized.FindProperty("sprite");
                var iconProp = serialized.FindProperty("iconSprite");

                if (spriteProp != null)
                {
                    spriteProp.objectReferenceValue = sprite;
                    Debug.Log($"[Sprites] ✅ {spiritData.name} → {targetName}.png (sprite)");
                }
                if (iconProp != null)
                {
                    iconProp.objectReferenceValue = sprite;
                }

                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(spiritData);
                connected++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Sprites] ✅ {connected}개 SpiritData에 스프라이트 연결 완료!");
            EditorUtility.DisplayDialog("Spirit Sprites",
                $"✅ {connected}개 SpiritData에 스프라이트 연결 완료!\n" +
                $"Assets/Resources/Data/Spirits/ 에셋 확인", "OK");
        }
    }
}

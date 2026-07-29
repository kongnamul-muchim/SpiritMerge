using UnityEditor;
using UnityEngine;
using System.IO;
using SpiritMerge;
using SpiritMerge.Data;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// 모든 스프라이트를 한 번에 연결
    /// 1. 정령 스프라이트 → SpiritData
    /// 2. 적 스프라이트 → MonsterData
    /// 3. 속성 아이콘 → BattleArea ElementIcon
    /// 4. 누락된 MonsterData 자동 생성
    /// 
    /// 실행: SpiritMerge > Setup > Connect All Sprites
    /// </summary>
    public static class ConnectAllSprites
    {
        static string spriteRoot = "Assets/Sprites";
        static string spiritDir = "Assets/Sprites/Spirits";
        static string enemyDir  = "Assets/Sprites/Enemies";
        static string iconDir   = "Assets/Sprites/Icons";
        static string dataDir   = "Assets/Resources/Data/Spirits";
        static string monsterDataDir = "Assets/Resources/Data/Monsters";

        [MenuItem("SpiritMerge/Setup/Connect All Sprites")]
        public static void ConnectAll()
        {
            int total = 0;
            total += ConnectSpirits();
            total += CreateAndConnectEnemies();
            total += ConnectIcons();
            Debug.Log($"[ConnectAll] ✅ {total}개 스프라이트 연결 완료!");
            EditorUtility.DisplayDialog("Connect All Sprites",
                $"✅ {total}개 스프라이트 연결 완료!", "OK");
        }

        // ── 1. 정령 스프라이트 연결 ──
        static int ConnectSpirits()
        {
            if (!Directory.Exists(spiritDir)) { Debug.LogWarning("[Connect] Spirits 폴더 없음"); return 0; }
            if (!Directory.Exists(dataDir))   { Debug.LogWarning("[Connect] Data/Spirits 폴더 없음"); return 0; }

            var spriteFiles = Directory.GetFiles(spiritDir, "*.png", SearchOption.AllDirectories);
            var dataFiles   = Directory.GetFiles(dataDir, "*.asset");
            int count = 0;

            foreach (var dataPath in dataFiles)
            {
                var spirit = AssetDatabase.LoadAssetAtPath<SpiritData>(dataPath);
                if (spirit == null || string.IsNullOrEmpty(spirit.spriteFileName)) continue;

                string matched = null;
                foreach (var png in spriteFiles)
                {
                    if (Path.GetFileNameWithoutExtension(png) == spirit.spriteFileName)
                    { matched = png; break; }
                }
                if (matched == null) continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(matched);
                if (sprite == null) continue;

                var so = new SerializedObject(spirit);
                var spriteProp = so.FindProperty("sprite");
                var iconProp = so.FindProperty("iconSprite");
                if (spriteProp != null)
                {
                    spriteProp.objectReferenceValue = sprite;
                    if (iconProp != null) iconProp.objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(spirit);
                    Debug.Log($"[Connect] 정령 {spirit.name} → {Path.GetFileName(matched)}");
                    count++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Connect] ✅ 정령 {count}개 연결");
            return count;
        }

        // ── 2. 적 몬스터 생성 + 연결 ──
        static int CreateAndConnectEnemies()
        {
            if (!Directory.Exists(enemyDir)) { Debug.LogWarning("[Connect] Enemies 폴더 없음"); return 0; }

            System.IO.Directory.CreateDirectory(monsterDataDir);
            var pngFiles = Directory.GetFiles(enemyDir, "*.png", SearchOption.AllDirectories);
            int count = 0;

            foreach (var png in pngFiles)
            {
                string name = Path.GetFileNameWithoutExtension(png);

                // 이미 MonsterData가 있으면 연결만
                string assetPath = $"{monsterDataDir}/{name}.asset";
                var monster = AssetDatabase.LoadAssetAtPath<MonsterData>(assetPath);

                if (monster == null)
                {
                    // MonsterData 생성 (접두사로 속성+보스 추정)
                    string elemStr = Path.GetFileName(Path.GetDirectoryName(png).Replace("\\", "/"));
                    // "Chapter1_Fire" → Fire
                    string elemName = elemStr.Contains("_") ? elemStr.Split('_')[1] : "Fire";
                    ElementType elem = elemName switch
                    {
                        "Fire"    => ElementType.Fire,
                        "Water"   => ElementType.Water,
                        "Nature"  => ElementType.Earth,
                        "Thunder" => ElementType.Wind,
                        _         => ElementType.Fire
                    };

                    bool isBoss = name.EndsWith("_03");

                    var so = ScriptableObject.CreateInstance<MonsterData>();
                    so.monsterName = name;
                    so.element = elem;
                    so.spriteFileName = name;
                    so.isBoss = isBoss;
                    AssetDatabase.CreateAsset(so, assetPath);
                    monster = so;
                    Debug.Log($"[Connect] 몬스터 생성: {name} ({elemName})");
                }

                // Sprite 연결
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(png);
                if (sprite == null) continue;

                var so2 = new SerializedObject(monster);
                var spProp = so2.FindProperty("sprite");
                if (spProp != null && spProp.objectReferenceValue == null)
                {
                    spProp.objectReferenceValue = sprite;
                    so2.ApplyModifiedProperties();
                    EditorUtility.SetDirty(monster);
                    count++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Connect] ✅ 적 {count}개 연결");
            return count;
        }

        // ── 3. 속성 아이콘 연결 (BattleArea ElementIcon 찾아서) ──
        static int ConnectIcons()
        {
            if (!Directory.Exists(iconDir)) { Debug.LogWarning("[Connect] Icons 폴더 없음"); return 0; }
            int count = 0;

            var battleArea = GameObject.Find("BattleArea");
            if (battleArea == null) return 0;

            // ElementIcon_0 ~ ElementIcon_3 찾기
            for (int i = 0; i < 4; i++)
            {
                string[] iconNames = { "icon_fire", "icon_water", "icon_nature", "icon_thunder" };
                string iconPath = $"{iconDir}/{iconNames[i]}.png";
                if (!File.Exists(iconPath)) continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (sprite == null) continue;

                // Enemy 슬롯의 ElementIcon 찾기
                var iconObj = battleArea.transform.Find($"ElementIcon_{i}");
                if (iconObj != null)
                {
                    var img = iconObj.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) { img.sprite = sprite; img.color = Color.white; count++; }
                }
            }
            Debug.Log($"[Connect] ✅ 아이콘 {count}개 연결");
            return count;
        }
    }
}

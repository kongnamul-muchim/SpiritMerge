using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// 30종 정령 SpiritData ScriptableObject 일괄 생성기
    /// GDD v1.1 + Evolution Table 기반
    /// </summary>
    public class SpiritDataGenerator
    {
        private const string DataPath = "Assets/Resources/Data/Spirits";

        [MenuItem("SpiritMerge/Data/Create All SpiritData (30)")]
        public static void CreateAllSpiritData()
        {
            System.IO.Directory.CreateDirectory(DataPath);
            AssetDatabase.StartAssetEditing();

            CreateFireSpirits();
            CreateWaterSpirits();
            CreateWindSpirits();
            CreateEarthSpirits();
            CreateDarkSpirits();
            CreateLightSpirits();

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SpiritDataGenerator] ✅ 30종 정령 데이터 생성 완료!");
            EditorUtility.DisplayDialog("SpiritData 생성", "30종 정령 데이터가 생성되었습니다.\nAssets/Resources/Data/Spirits/ 확인", "OK");
        }

        // ──────────────────────────────────────────────
        // 🔥 불 — 늑대/개 계열
        // ──────────────────────────────────────────────
        private static void CreateFireSpirits()
        {
            Create("Fire_1_Spark",      "불꽃",      SpiritGrade.OneStar,   ElementType.Fire,  AnimalType.None,  "Fire Spark",          10,  50,  5,  2.0f, 0.05f, 1.5f);
            Create("Fire_2_Orb",        "불덩이",    SpiritGrade.TwoStar,   ElementType.Fire,  AnimalType.None,  "Fire Orb",            18,  90,  9,  1.8f, 0.07f, 1.6f);
            Create("Fire_3_FlameWolf",  "불꽃늑대",  SpiritGrade.ThreeStar, ElementType.Fire,  AnimalType.Wolf,  "Flame Wolf",          35, 180, 18,  1.5f, 0.10f, 1.75f);
            Create("Fire_4_MagmaWolf",  "마그마늑대",SpiritGrade.FourStar,  ElementType.Fire,  AnimalType.Wolf,  "Magma Wolf",          70, 350, 35,  1.2f, 0.15f, 2.0f);
            Create("Fire_5_Cerberus",   "케르베로스",SpiritGrade.FiveStar,  ElementType.Fire,  AnimalType.Wolf,  "Cerberus",            130,650, 65,  0.9f, 0.20f, 2.25f);
        }

        // ──────────────────────────────────────────────
        // 💧 물 — 여우 계열
        // ──────────────────────────────────────────────
        private static void CreateWaterSpirits()
        {
            Create("Water_1_Droplet",   "물방울",      SpiritGrade.OneStar,   ElementType.Water, AnimalType.None, "Water Droplet",             8,  55,  6,  2.0f, 0.05f, 1.5f);
            Create("Water_2_Whirlpool", "소용돌이",    SpiritGrade.TwoStar,   ElementType.Water, AnimalType.None, "Whirlpool",                15, 100, 10,  1.8f, 0.07f, 1.6f);
            Create("Water_3_WaterFox",  "물여우",      SpiritGrade.ThreeStar, ElementType.Water, AnimalType.Fox,  "Water Fox",               30, 200, 20,  1.5f, 0.10f, 1.75f);
            Create("Water_4_NineTails", "구미호",      SpiritGrade.FourStar,  ElementType.Water, AnimalType.Fox,  "Nine-tailed Fox",         60, 400, 40,  1.2f, 0.15f, 2.0f);
            Create("Water_5_Millennium","천년구미호",  SpiritGrade.FiveStar,  ElementType.Water, AnimalType.Fox,  "Millennium Nine-tailed Fox",110,750, 75, 0.9f, 0.20f, 2.25f);
        }

        // ──────────────────────────────────────────────
        // 🌪️ 바람 — 맹금류 계열
        // ──────────────────────────────────────────────
        private static void CreateWindSpirits()
        {
            Create("Wind_1_Whirlwind",  "돌개바람",  SpiritGrade.OneStar,   ElementType.Wind, AnimalType.None,  "Small Whirlwind",      12,  45,  4,  1.8f, 0.06f, 1.5f);
            Create("Wind_2_WindCloud",  "바람구름",  SpiritGrade.TwoStar,   ElementType.Wind, AnimalType.None,  "Wind Cloud",           22,  80,  8,  1.6f, 0.08f, 1.6f);
            Create("Wind_3_WindHawk",   "바람매",    SpiritGrade.ThreeStar, ElementType.Wind, AnimalType.Hawk,  "Wind Hawk",            40, 160, 16,  1.3f, 0.12f, 1.75f);
            Create("Wind_4_ThunderEagle","번개독수리",SpiritGrade.FourStar,  ElementType.Wind, AnimalType.Hawk,  "Thunder Eagle",        80, 320, 32,  1.1f, 0.17f, 2.0f);
            Create("Wind_5_Thunderbird", "천둥새",   SpiritGrade.FiveStar,  ElementType.Wind, AnimalType.Hawk,  "Thunderbird",          150,600, 60,  0.8f, 0.22f, 2.25f);
        }

        // ──────────────────────────────────────────────
        // 🌍 땅 — 곰 계열
        // ──────────────────────────────────────────────
        private static void CreateEarthSpirits()
        {
            Create("Earth_1_SmallRock", "돌멩이",    SpiritGrade.OneStar,   ElementType.Earth, AnimalType.None,  "Small Rock",            9,  70,  8,  2.2f, 0.04f, 1.5f);
            Create("Earth_2_Boulder",   "바위덩이",  SpiritGrade.TwoStar,   ElementType.Earth, AnimalType.None,  "Boulder",               16, 130, 14,  2.0f, 0.06f, 1.6f);
            Create("Earth_3_RockBear",  "돌곰",      SpiritGrade.ThreeStar, ElementType.Earth, AnimalType.Bear,  "Rock Bear",             32, 260, 28,  1.7f, 0.08f, 1.75f);
            Create("Earth_4_IronBear",  "아이언베어",SpiritGrade.FourStar,  ElementType.Earth, AnimalType.Bear,  "Iron Bear",             65, 500, 55,  1.4f, 0.12f, 2.0f);
            Create("Earth_5_GaiaBear",  "가이아베어",SpiritGrade.FiveStar,  ElementType.Earth, AnimalType.Bear,  "Gaia Bear",             120,950,100,  1.1f, 0.18f, 2.25f);
        }

        // ──────────────────────────────────────────────
        // 🌑 어둠 — 표범 계열
        // ──────────────────────────────────────────────
        private static void CreateDarkSpirits()
        {
            Create("Dark_1_Shadow",      "그림자",       SpiritGrade.OneStar,   ElementType.Dark, AnimalType.None,    "Shadow",                 14,  40,  3,  1.9f, 0.06f, 1.5f);
            Create("Dark_2_DarkOrb",     "흑구",         SpiritGrade.TwoStar,   ElementType.Dark, AnimalType.None,    "Dark Orb",               25,  75,  6,  1.7f, 0.08f, 1.6f);
            Create("Dark_3_ShadowPanther","흑표범",      SpiritGrade.ThreeStar, ElementType.Dark, AnimalType.Panther,"Shadow Panther",          45, 150, 14,  1.4f, 0.12f, 1.75f);
            Create("Dark_4_ShadowLord",  "섀도우팬서",   SpiritGrade.FourStar,  ElementType.Dark, AnimalType.Panther,"Shadow Panther Lord",     90, 300, 28,  1.1f, 0.18f, 2.0f);
            Create("Dark_5_Nightmare",   "나이트메어",   SpiritGrade.FiveStar,  ElementType.Dark, AnimalType.Panther,"Nightmare Panther",       165,550, 55,  0.8f, 0.25f, 2.5f);
        }

        // ──────────────────────────────────────────────
        // ☀️ 빛 — 사슴 계열
        // ──────────────────────────────────────────────
        private static void CreateLightSpirits()
        {
            Create("Light_1_LightOrb",   "빛구체",    SpiritGrade.OneStar,   ElementType.Light, AnimalType.None,  "Light Orb",             7,  60,  7,  2.0f, 0.05f, 1.5f);
            Create("Light_2_Aura",       "오라",      SpiritGrade.TwoStar,   ElementType.Light, AnimalType.None,  "Aura",                  13, 110, 12,  1.8f, 0.07f, 1.6f);
            Create("Light_3_LightDeer",  "빛사슴",    SpiritGrade.ThreeStar, ElementType.Light, AnimalType.Deer,  "Light Deer",            28, 220, 24,  1.5f, 0.10f, 1.75f);
            Create("Light_4_AuroraDeer", "오로라사슴",SpiritGrade.FourStar,  ElementType.Light, AnimalType.Deer,  "Aurora Deer",           55, 450, 48,  1.2f, 0.14f, 2.0f);
            Create("Light_5_Celestial",  "셀레스티얼",SpiritGrade.FiveStar,  ElementType.Light, AnimalType.Deer,  "Celestial Stag",        100,850, 90,  0.9f, 0.20f, 2.25f);
        }

        // ──────────────────────────────────────────────
        // 공통 생성 메서드
        // ──────────────────────────────────────────────
        private static void Create(string fileName, string koreanName, SpiritGrade grade,
            ElementType element, AnimalType animal, string spriteFileName,
            int atk, int hp, int def, float spd, float crit, float critDmg)
        {
            var path = $"{DataPath}/{fileName}.asset";

            // 이미 존재하면 스킵
            if (System.IO.File.Exists(path))
            {
                Debug.Log($"[SKIP] {fileName} already exists");
                return;
            }

            var data = ScriptableObject.CreateInstance<SpiritData>();
            data.spiritName     = koreanName;
            data.grade          = grade;
            data.element        = element;
            data.animalType     = animal;
            data.description    = $"{koreanName} — {element} 속성 {GetGradeText(grade)} 정령";
            data.spriteFileName = spriteFileName;

            data.baseATK        = atk;
            data.baseHP         = hp;
            data.baseDEF        = def;
            data.baseSpeed      = spd;
            data.baseCritRate   = crit;
            data.baseCritDamage = critDmg;

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[CREATE] {fileName} ({koreanName})");
        }

        private static string GetGradeText(SpiritGrade grade)
        {
            return grade switch
            {
                SpiritGrade.OneStar   => "1성",
                SpiritGrade.TwoStar   => "2성",
                SpiritGrade.ThreeStar => "3성",
                SpiritGrade.FourStar  => "4성",
                SpiritGrade.FiveStar  => "5성",
                _ => ""
            };
        }
    }
}

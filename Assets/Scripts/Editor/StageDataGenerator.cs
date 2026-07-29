using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// StageData 50개 자동 생성 (5챕터 × 10스테이지)
    /// 
    /// 공식:
    /// - 총 몬스터 수: 챕터 × 10 + 테이지 × 2
    /// - 웨이브 수: 챕터 + 4
    /// - 소환 비용: 500 + (챕터-1) × 300
    /// - 보스 테이지: n-5, n-10 (마지막 웨이브에 보스 1마리)
    /// - 속성: (챕터-1) % 4 (0=Fire, 1=Water, 2=Nature, 3=Thunder)
    /// 
    /// 실행: SpiritMerge > Data > Create All Stages
    /// </summary>
    public static class StageDataGenerator
    {
        private const string Path = "Assets/Resources/Data/Stages";

        [MenuItem("SpiritMerge/Data/Create All Stages")]
        public static void CreateAllStages()
        {
            System.IO.Directory.CreateDirectory(Path);

            // 기존 스테이지 삭제
            var existing = AssetDatabase.FindAssets("t:StageData", new[] { Path });
            foreach (var guid in existing)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(assetPath);
            }

            int totalCreated = 0;

            string[] chapterNames = { "불의 숲", "물의 숲", "숲의 길", "번개의 숲", "불의 성소" };

            for (int chapter = 1; chapter <= 5; chapter++)
            {
                for (int stage = 1; stage <= 10; stage++)
                {
                    var data = ScriptableObject.CreateInstance<StageData>();

                    // 기본 정보
                    data.stageNumber = stage;
                    data.stageName = $"{chapter}-{stage} {chapterNames[chapter - 1]}";
                    data.region = chapter;
                    data.elementType = (ElementType)((chapter - 1) % 4);
                    data.isBossStage = (stage == 5 || stage == 10);

                    // 전투
                    data.waveCount = chapter + 4;
                    data.hpMultiplier = 1.0f + (chapter - 1) * 0.3f + (stage - 1) * 0.1f;
                    data.atkMultiplier = 1.0f + (chapter - 1) * 0.2f;

                    // 몬스터 스폰
                    data.totalMonsterCount = chapter * 10 + stage * 2;
                    data.spawnPointCount = 5;
                    data.bossHpMultiplier = 10;

                    // 보상
                    data.goldReward = 100 + chapter * 50 + stage * 20;
                    data.rubyReward = data.isBossStage ? 50 : (stage % 5 == 0 ? 20 : 0);
                    data.expReward = 50 + chapter * 20 + stage * 10;

                    // 경제
                    data.summonCost = 500 + (chapter - 1) * 300;

                    // 저장
                    var assetPath = $"{Path}/Stage_{chapter}-{stage}.asset";
                    AssetDatabase.CreateAsset(data, assetPath);
                    totalCreated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StageGen] ✅ {totalCreated}개 스테이지 생성 완료! (5챕터 × 10스테이지)");
            EditorUtility.DisplayDialog("Stage Generator",
                $"✅ {totalCreated}개 스이지 생성 완료!\n\n" +
                "공식:\n" +
                "• 총 몬스터: 챕터×10 + 스테이지×2\n" +
                "• 웨이브: 챕터+4\n" +
                "• 소환비: 500+(챕터-1)×300\n" +
                "• 보스: n-5, n-10", "OK");
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// Region 1 (Fire) StageData 10개 생성
    /// 사용: SpiritMerge > Data > Create Region 1 Stages
    /// </summary>
    public static class StageDataGenerator
    {
        private const string Path = "Assets/Resources/Data/Stages";

        [MenuItem("SpiritMerge/Data/Create Region 1 Stages")]
        public static void CreateRegion1()
        {
            System.IO.Directory.CreateDirectory(Path);

            for (int stage = 1; stage <= 10; stage++)
            {
                var data = ScriptableObject.CreateInstance<StageData>();
                data.stageNumber = stage;
                data.stageName = $"1-{stage}";
                data.region = 1;
                data.elementType = ElementType.Fire;
                data.isBossStage = (stage == 10);
                data.waveCount = (stage == 10) ? 3 : 5;
                data.goldReward = 100 + stage * 20;
                data.expReward = 50 + stage * 10;

                var assetPath = $"{Path}/Stage_1-{stage}.asset";
                AssetDatabase.CreateAsset(data, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StageGen] ✅ Region 1 stages created!");
        }
    }
}

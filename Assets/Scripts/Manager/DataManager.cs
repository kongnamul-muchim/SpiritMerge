using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 데이터 로드/세이브 관리
    /// JSON 저장 시스템 (GDD 11.3)
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "save.json");

        [Header("SpiritData 테이블")]
        public List<SpiritData> spiritDatabase = new List<SpiritData>();

        [Header("StageData 테이블")]
        public List<StageData> stageDatabase = new List<StageData>();

        [Header("EquipmentData 테이블")]
        public List<EquipmentData> equipmentDatabase = new List<EquipmentData>();

        // SpiritData 빠른 조회용 딕셔너리
        private Dictionary<string, SpiritData> _spiritDict = new Dictionary<string, SpiritData>();

        private void Awake()
        {
            BuildSpiritDictionary();
        }

        private void BuildSpiritDictionary()
        {
            _spiritDict.Clear();
            foreach (var spirit in spiritDatabase)
            {
                if (spirit != null && !_spiritDict.ContainsKey(spirit.name))
                {
                    _spiritDict[spirit.name] = spirit;
                }
            }
        }

        /// <summary>
        /// SpiritData ID로 조회
        /// </summary>
        public SpiritData GetSpiritData(string dataId)
        {
            if (_spiritDict.TryGetValue(dataId, out SpiritData data))
                return data;

            // Resources 폴더에서 로드 시도
            SpiritData loaded = Resources.Load<SpiritData>($"Data/{dataId}");
            if (loaded != null)
                _spiritDict[dataId] = loaded;
            return loaded;
        }

        /// <summary>
        /// StageNumber로 StageData 조회
        /// </summary>
        public StageData GetStageData(int stageNumber)
        {
            return stageDatabase.Find(s => s.stageNumber == stageNumber);
        }

        /// <summary>
        /// 게임 저장
        /// </summary>
        public void SaveGame(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[DataManager] Saved to {SaveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataManager] Save failed: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 로드
        /// </summary>
        public SaveData LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.Log("[DataManager] No save file found");
                return null;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[DataManager] Loaded save file ({json.Length} bytes)");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataManager] Load failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 세이브 데이터 삭제
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[DataManager] Save deleted");
            }
        }
    }
}

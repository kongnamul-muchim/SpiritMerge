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
        private string BackupFilePath => Path.Combine(Application.persistentDataPath, "save.backup.json");
        private string TempFilePath => Path.Combine(Application.persistentDataPath, "save.tmp");

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
        /// 게임 저장 — ⭐ 원자적 쓰기 (tmp에 쓰고 → 기존을 백업으로 → tmp를 save로 교체)
        /// 쓰는 도중 강제종료돼도 기존 save.json은 안전하고, 손상 시 백업으로 복구
        /// </summary>
        public void SaveGame(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(TempFilePath, json);

                // 기존 save.json → backup (이전 상태 보존)
                if (File.Exists(SaveFilePath))
                    File.Copy(SaveFilePath, BackupFilePath, true);

                // tmp → save 교체 (성공 시에만)
                File.Copy(TempFilePath, SaveFilePath, true);
                File.Delete(TempFilePath);

                Debug.Log($"[DataManager] Saved to {SaveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataManager] Save failed: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 로드 — 메인 손상 시 백업 파일로 복구
        /// </summary>
        public SaveData LoadGame()
        {
            var main = TryLoad(SaveFilePath);
            if (main != null) return main;

            var backup = TryLoad(BackupFilePath);
            if (backup != null)
            {
                Debug.LogWarning("[DataManager] 메인 저장 파일 손상 → 백업으로 복구");
                return backup;
            }

            Debug.Log("[DataManager] 저장 파일 없음");
            return null;
        }

        SaveData TryLoad(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.gold < 0 || data.playerLevel < 1) return null;
                Debug.Log($"[DataManager] Loaded save file ({json.Length} bytes)");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataManager] Load failed ({path}): {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 세이브 데이터 삭제
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
            if (File.Exists(BackupFilePath))
                File.Delete(BackupFilePath);
            Debug.Log("[DataManager] Save deleted");
        }
    }
}

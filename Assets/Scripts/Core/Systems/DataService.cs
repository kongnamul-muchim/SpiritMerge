using System.IO;
using SpiritMerge;
using UnityEngine;
using SpiritMerge.Core.Interfaces;

namespace SpiritMerge.Core.Systems
{
    /// <summary>
    /// JSON 저장/로드 서비스 (SRP: 데이터 영속성)
    /// DIP: IDataService 인터페이스에 의존
    /// </summary>
    public class DataService : IDataService
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "save.json");

        public bool HasSave => File.Exists(SavePath);

        public void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[DataService] Saved to {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataService] Save failed: {e.Message}");
            }
        }

        public SaveData Load()
        {
            if (!HasSave) return null;
            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataService] Load failed: {e.Message}");
                return null;
            }
        }

        public void Delete()
        {
            if (HasSave) File.Delete(SavePath);
        }
    }
}

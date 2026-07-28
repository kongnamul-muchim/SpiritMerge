using System;
using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 게임 전체 상태 관리 싱글톤
    /// GDD 11.1 GameManager 기반
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("플레이어 상태")]
        public int playerLevel = 1;
        public int playerExp = 0;
        public int skillPoints = 0;
        public int[] skillTreeLevels = new int[11];

        [Header("재화")]
        public int gold = 0;
        public int ruby = 0;
        public int spiritStone = 0;
        public int[] elementStones = new int[6];

        [Header("진행")]
        public int currentStage = 1;
        public int[] partySlotIds = new int[4] { -1, -1, -1, -1 };

        private DataManager _dataManager;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _dataManager = GetComponent<DataManager>();
            if (_dataManager == null)
                _dataManager = gameObject.AddComponent<DataManager>();
        }

        private void Start()
        {
            LoadGameData();
        }

        public void LoadGameData()
        {
            SaveData data = _dataManager.LoadGame();
            if (data != null)
            {
                playerLevel = data.playerLevel;
                playerExp = data.playerExp;
                skillPoints = data.skillPoints;
                skillTreeLevels = data.skillTreeLevels;
                gold = data.gold;
                ruby = data.ruby;
                spiritStone = data.spiritStone;
                elementStones = data.elementStones;
                currentStage = data.currentStage;
                partySlotIds = data.partySlotIds;
                Debug.Log($"[GameManager] Loaded save: Lv.{playerLevel}, Stage {currentStage}");
            }
            else
            {
                Debug.Log("[GameManager] No save data found, starting fresh game");
            }
        }

        public void SaveGameData()
        {
            SaveData data = new SaveData
            {
                playerLevel = playerLevel,
                playerExp = playerExp,
                skillPoints = skillPoints,
                skillTreeLevels = skillTreeLevels,
                gold = gold,
                ruby = ruby,
                spiritStone = spiritStone,
                elementStones = elementStones,
                currentStage = currentStage,
                partySlotIds = partySlotIds,
                lastLoginTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            _dataManager.SaveGame(data);
            Debug.Log("[GameManager] Game saved!");
        }

        public void AddGold(int amount)
        {
            gold += amount;
        }

        public bool SpendGold(int amount)
        {
            if (gold >= amount)
            {
                gold -= amount;
                return true;
            }
            return false;
        }

        public void AddRuby(int amount)
        {
            ruby += amount;
        }

        public bool SpendRuby(int amount)
        {
            if (ruby >= amount)
            {
                ruby -= amount;
                return true;
            }
            return false;
        }

        private void OnApplicationQuit()
        {
            SaveGameData();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveGameData();
        }
    }
}

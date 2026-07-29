using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpiritMerge.Data;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// SpawnPoint 관리 + 몬스터 생성
    /// - StageData의 spawnPointCount만큼 SpawnPoint 자동 생성
    /// - 웨이브 시작 시 몬스터를 빈 SpawnPoint에 생성
    /// - 몬스터 사망 시 SpawnPoint를 다시 비움
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("Prefab 참조")]
        [SerializeField] private GameObject spawnPointPrefab;
        [SerializeField] private GameObject monsterPrefab;

        [Header("배치 설정")]
        [SerializeField] private int spawnPointCount = 5;
        [SerializeField] private Vector2 startPosition = new Vector2(-3f, 2f);
        [SerializeField] private float spacing = 1.5f;

        private List<GameObject> spawnPoints = new List<GameObject>();
        private Queue<GameObject> availableSpawnPoints = new Queue<GameObject>();

        /// <summary>
        /// SpawnPoint 자동 생성 (스테이지 시작 시 호출)
        /// </summary>
        public void InitializeSpawnPoints(int count)
        {
            // 기존 SpawnPoint 정리
            foreach (var sp in spawnPoints)
            {
                if (sp != null) Destroy(sp);
            }
            spawnPoints.Clear();
            availableSpawnPoints.Clear();

            // 새 SpawnPoint 생성
            for (int i = 0; i < count; i++)
            {
                Vector2 position = new Vector2(
                    startPosition.x + (i * spacing),
                    startPosition.y
                );

                GameObject sp = Instantiate(spawnPointPrefab, position, Quaternion.identity);
                sp.name = $"SpawnPoint_{i}";
                sp.tag = "SpawnPoint";
                
                spawnPoints.Add(sp);
                availableSpawnPoints.Enqueue(sp);
            }

            Debug.Log($"[MonsterSpawner] {count}개 SpawnPoint 생성 완료");
        }

        /// <summary>
        /// 빈 SpawnPoint에 몬스터 생성
        /// </summary>
        public GameObject SpawnMonster()
        {
            if (availableSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[MonsterSpawner] 사용 가능한 SpawnPoint 없음");
                return null;
            }

            GameObject spawnPoint = availableSpawnPoints.Dequeue();
            GameObject monster = Instantiate(monsterPrefab, spawnPoint.transform.position, Quaternion.identity);
            
            // 몬스터에 SpawnPoint 참조 저장 (사망 시 반환용)
            var monsterComponent = monster.GetComponent<Monster>();
            if (monsterComponent != null)
            {
                monsterComponent.assignedSpawnPoint = spawnPoint;
            }

            // 몬스터 초기화 (MonsterData 필요)
            var stageData = FindObjectOfType<WaveController>()?.StageData;
            MonsterData monsterData = null;
            if (stageData != null)
            {
                // 챕터 속성에 맞는 MonsterData 로드 (임시: 첫 번째 MonsterData 사용)
                var allMonsters = Resources.LoadAll<MonsterData>("Data/Monsters");
                if (allMonsters.Length > 0)
                    monsterData = allMonsters[Random.Range(0, allMonsters.Length)];
            }
            if (monsterComponent != null && monsterData != null)
            {
                monsterComponent.Initialize(monsterData, false);
            }

            return monster;
        }

        /// <summary>
        /// SpawnPoint 반환 (몬스터 사망 시 호출)
        /// </summary>
        public void ReturnSpawnPoint(GameObject spawnPoint)
        {
            if (spawnPoint != null && !availableSpawnPoints.Contains(spawnPoint))
            {
                availableSpawnPoints.Enqueue(spawnPoint);
            }
        }

        /// <summary>
        /// 현재 사용 가능한 SpawnPoint 수
        /// </summary>
        public int AvailableCount => availableSpawnPoints.Count;

        /// <summary>
        /// 전체 SpawnPoint 수
        /// </summary>
        public int TotalCount => spawnPoints.Count;
    }
}

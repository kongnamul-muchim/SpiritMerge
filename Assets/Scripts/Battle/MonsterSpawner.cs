using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpiritMerge.Data;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// EnemySlot 큐 기반 몬스터 스폰 (SpawnPoint 생성 제거)
    /// - EnemyGroup/EnemySlot_0..N UI 슬롯 자체를 몬스터 유닛으로 재사용
    /// - 스폰: 빈 슬롯을 활성화 + Monster 컴포넌트 얹어 초기화
    /// - 사망: 슬롯 비활성화 후 큐로 반환
    /// - ⭐ 중앙정렬 지원: GetCenteredSlotIndices()로 웨이브 몬스터를 가운데 배치
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("EnemyGroup 슬롯")]
        [SerializeField] private int maxEnemySlots = 3;   // 사용할 EnemySlot 최대 수

        [Header("전투 설정 (WaveController가 전달)")]
        private ElementType currentElement = ElementType.Fire; // 이번 전투 몬스터 속성 테마
        private float currentHpMultiplier = 1f;               // 스테이지 HP 배율
        private float currentAtkMultiplier = 1f;              // 스테이지 ATK 배율

        private const string EnemyGroupName = "EnemyGroup";
        private const string EnemySlotNamePrefix = "EnemySlot_";

        private readonly List<GameObject> enemySlots = new List<GameObject>();
        private readonly Queue<GameObject> availableSlots = new Queue<GameObject>();

        /// <summary>
        /// EnemyGroup 하위 EnemySlot_0..N 수집 + 전부 비활성화 (전투 시작 전 호출)
        /// </summary>
        public void InitializeEnemySlots(int count)
        {
            // 기존 슬롯 정리 (재호출 대비)
            foreach (var slot in enemySlots)
                if (slot != null) slot.SetActive(false);
            enemySlots.Clear();
            availableSlots.Clear();

            var enemyGroup = GameObject.Find(EnemyGroupName);
            if (enemyGroup == null)
            {
                Debug.LogWarning("[MonsterSpawner] EnemyGroup 없음!");
                return;
            }

            int slotCount = Mathf.Min(count, maxEnemySlots);
            for (int i = 0; i < slotCount; i++)
            {
                var slotGo = enemyGroup.transform.Find(EnemySlotNamePrefix + i);
                if (slotGo == null) break;

                var go = slotGo.gameObject;
                go.SetActive(false);
                enemySlots.Add(go);
                availableSlots.Enqueue(go);
            }

            Debug.Log($"[MonsterSpawner] EnemySlot {enemySlots.Count}개 준비 완료");
        }

        /// <summary>
        /// 전투 시작 시 스테이지 정보 설정 (WaveController.StartBattle에서 호출)
        /// - 속성 테마: 이 속성과 일치하는 몬스터만 소환
        /// - 배율: 몬스터 스탯에 적용 (hpMultiplier/atkMultiplier)
        /// </summary>
        public void SetupBattle(ElementType elementType, float hpMultiplier = 1f, float atkMultiplier = 1f)
        {
            currentElement = elementType;
            currentHpMultiplier = hpMultiplier;
            currentAtkMultiplier = atkMultiplier;
        }

        /// <summary>
        /// ⭐ 이전 전투의 남은 몬스터/슬롯 정리 (재도전/재시작 시)
        /// - 활성 EnemySlot을 모두 비활성화하고 스폰 큐 재구성
        /// - 안 하면 이전 몬스터가 슬롯을 점유 → 새 몬스터 스폰 실패
        ///   → aliveMonsters가 0으로 잘못 유지 → 웨이브가 미친듯이 진행 + 패배 감지 실패(전투 동결)
        /// </summary>
        public void ResetAllMonsters()
        {
            foreach (var slot in enemySlots)
                if (slot != null) slot.SetActive(false);
            availableSlots.Clear();
            foreach (var slot in enemySlots)
                if (slot != null) availableSlots.Enqueue(slot);
        }

        /// <summary>
        /// 전체 EnemySlot 수 (현재 사용 가능 슬롯 총량)
        /// </summary>
        public int EnemySlotCount => enemySlots.Count;

        /// <summary>
        /// ⭐ 웨이브 몬스터 수에 맞는 중앙정렬 슬롯 인덱스 계산
        /// 예) 슬롯 3개: 1마리→[1], 2마리→[0,2], 3마리→[0,1,2]
        /// 예) 슬롯 5개: 2마리→[0,2,4]처럼 양 끝 대칭 (X O X O X 느낌)
        /// </summary>
        public int[] GetCenteredSlotIndices(int count)
        {
            int n = enemySlots.Count;
            if (count <= 0 || n == 0) return new int[0];
            if (count >= n)
            {
                var all = new int[n];
                for (int i = 0; i < n; i++) all[i] = i;
                return all;
            }
            if (count == 1) return new int[] { n / 2 };

            var idx = new int[count];
            for (int i = 0; i < count; i++)
                idx[i] = Mathf.RoundToInt(i * (n - 1f) / (count - 1f));
            return idx;
        }

        /// <summary>
        /// 지정 인덱스의 EnemySlot에 몬스터 스폰 (중앙정렬용)
        /// </summary>
        public GameObject SpawnMonsterAt(int index)
        {
            if (index < 0 || index >= enemySlots.Count) return null;
            var slot = enemySlots[index];
            if (slot == null || slot.activeSelf) return null; // 이미 사용 중

            // 큐에서 해당 슬롯 제거
            if (availableSlots.Contains(slot))
            {
                var tmp = new Queue<GameObject>();
                while (availableSlots.Count > 0)
                {
                    var s = availableSlots.Dequeue();
                    if (s != slot) tmp.Enqueue(s);
                }
                availableSlots.Clear();
                foreach (var s in tmp) availableSlots.Enqueue(s);
            }

            return SpawnInto(slot);
        }

        /// <summary>
        /// ⭐ 지정 슬롯에 보스 몬스터 스폰 (레이드 전용 — isBoss=true, 속성 테마 기준)
        /// </summary>
        public GameObject SpawnBossAt(int index)
        {
            if (index < 0 || index >= enemySlots.Count) return null;
            var slot = enemySlots[index];
            if (slot == null || slot.activeSelf) return null;

            // 큐에서 해당 슬롯 제거 (SpawnMonsterAt 로직 재사용)
            if (availableSlots.Contains(slot))
            {
                var tmp = new Queue<GameObject>();
                while (availableSlots.Count > 0)
                {
                    var s = availableSlots.Dequeue();
                    if (s != slot) tmp.Enqueue(s);
                }
                availableSlots.Clear();
                foreach (var s in tmp) availableSlots.Enqueue(s);
            }
            return SpawnInto(slot, true);
        }

        /// <summary>
        /// 빈 EnemySlot에 몬스터 스폰 (슬롯 활성화 + Monster 초기화)
        /// </summary>
        public GameObject SpawnMonster()
        {
            if (availableSlots.Count == 0)
            {
                Debug.LogWarning("[MonsterSpawner] 사용 가능한 EnemySlot 없음");
                return null;
            }

            GameObject slot = availableSlots.Dequeue();
            return SpawnInto(slot);
        }

        /// <summary>
        /// 슬롯 활성화 + Monster 컴포넌트 초기화 (공용)
        /// </summary>
        GameObject SpawnInto(GameObject slot, bool bossOnly = false)
        {
            slot.SetActive(true);

            // ⭐ 몬스터에는 레벨 개념이 없으므로 LvText 숨김 (하드코딩 잔상 제거)
            var lvText = SlotUiHelper.FindLvText(slot.transform);
            if (lvText != null) lvText.gameObject.SetActive(false);

            // 슬롯에 Monster 컴포넌트 준비 (재사용)
            var monster = slot.GetComponent<Monster>();
            if (monster == null) monster = slot.AddComponent<Monster>();
            monster.slotMode = true;
            monster.spawner = this;
            monster.displayImage = SlotUiHelper.FindDisplayImage(slot.transform);
            monster.hpSlider = SlotUiHelper.FindHpSlider(slot.transform);
            monster.cdSlider = SlotUiHelper.FindCdSlider(slot.transform);

            // 몬스터 초기화 (MonsterData 필요)
            // ⭐ 스테이지 속성 테마(currentElement)와 일치하는 몬스터만 소환
            //    예) 불의 숲(Fire) → enemy_ch1_*(Fire) / enemy_ch5_*(Fire)
            MonsterData monsterData = null;
            var allMonsters = Resources.LoadAll<MonsterData>("Data/Monsters");

            var candidates = new List<MonsterData>();
            foreach (var m in allMonsters)
                if (m.element == currentElement) candidates.Add(m);

            // ⭐ 레이드 보스 전용: isBoss=true만 선택
            if (bossOnly)
                candidates.RemoveAll(m => !m.isBoss);

            // 해당 속성 몬스터가 없으면 전체 폴백 (안전장치)
            if (candidates.Count == 0)
                candidates.AddRange(bossOnly ? allMonsters.Where(m => m.isBoss) : allMonsters);

            if (candidates.Count > 0)
            {
                monsterData = candidates[Random.Range(0, candidates.Count)];
                monster.Initialize(monsterData, bossOnly, currentHpMultiplier, currentAtkMultiplier);
            }
            else
            {
                Debug.LogError("[MonsterSpawner] MonsterData 없음!");
            }

            return slot;
        }

        /// <summary>
        /// EnemySlot 반환 (몬스터 사망 시) — 비활성화 후 큐에 재투입
        /// </summary>
        public void ReturnEnemySlot(GameObject slot)
        {
            if (slot == null) return;
            slot.SetActive(false);
            if (!availableSlots.Contains(slot))
                availableSlots.Enqueue(slot);
        }

        /// <summary>
        /// 현재 사용 가능한 EnemySlot 수
        /// </summary>
        public int AvailableCount => availableSlots.Count;

        /// <summary>
        /// 전체 EnemySlot 수
        /// </summary>
        public int TotalCount => enemySlots.Count;
    }
}

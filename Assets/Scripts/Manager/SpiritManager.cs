using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 정령 수집/합성 관리
    /// GDD 4. 머지 시스템 및 3. 정령 시스템
    /// </summary>
    public class SpiritManager : MonoBehaviour
    {
        public static SpiritManager Instance;

        [Header("보유 정령")]
        public List<OwnedSpirit> ownedSpirits = new List<OwnedSpirit>();

        private int _nextUid = 1;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        /// <summary>
        /// 새로운 정령 추가
        /// </summary>
        public OwnedSpirit AddSpirit(string dataId, int grade)
        {
            OwnedSpirit spirit = new OwnedSpirit(dataId, _nextUid++, grade);
            ownedSpirits.Add(spirit);
            Debug.Log($"[SpiritManager] Added {dataId} (Grade {grade}, UID {spirit.uid})");
            return spirit;
        }

        /// <summary>
        /// 정령 제거 (UID 기준)
        /// </summary>
        public void RemoveSpirit(int uid)
        {
            ownedSpirits.RemoveAll(s => s.uid == uid);
        }

        /// <summary>
        /// 보유 정령 카운트 (특정 dataId 기준)
        /// </summary>
        public int GetSpiritCount(string dataId, int grade)
        {
            return ownedSpirits.FindAll(s => s.dataId == dataId && s.grade == grade).Count;
        }

        /// <summary>
        /// 머지 실행
        /// GDD 4.1 기본 규칙: 1→2(3마리), 2→3(3마리), 3→4(2마리), 4→5(2마리)
        /// </summary>
        public OwnedSpirit MergeSpirits(string dataId, int currentGrade)
        {
            int required = GetRequiredCount(currentGrade);
            int count = GetSpiritCount(dataId, currentGrade);

            if (count < required)
            {
                Debug.LogWarning($"[SpiritManager] Not enough spirits: {count}/{required}");
                return null;
            }

            int nextGrade = currentGrade + 1;
            if (nextGrade > 5)
            {
                Debug.LogWarning("[SpiritManager] Max grade reached");
                return null;
            }

            // 재료 정령 제거
            int removed = 0;
            ownedSpirits.RemoveAll(s =>
            {
                if (s.dataId == dataId && s.grade == currentGrade && removed < required)
                {
                    removed++;
                    return true;
                }
                return false;
            });

            // 상위 등급 정령 추가 (같은 dataId 사용, grade만 증가)
            OwnedSpirit result = AddSpirit(dataId, nextGrade);
            Debug.Log($"[SpiritManager] Merge success: {dataId} Grade {currentGrade} → {nextGrade}");
            return result;
        }

        /// <summary>
        /// 크로스 머지 (랜덤)
        /// GDD 4.3: 다른 속성 정령 3마리 → 랜덤 동등급 이상
        /// </summary>
        public OwnedSpirit CrossMerge(List<int> spiritUids)
        {
            if (spiritUids.Count < 3)
                return null;

            // 재료 등급 확인 (모두 같은 등급이어야 함)
            int baseGrade = ownedSpirits.Find(s => s.uid == spiritUids[0])?.grade ?? 0;

            // 재료 제거
            foreach (int uid in spiritUids)
            {
                RemoveSpirit(uid);
            }

            // 랜덤 결과 결정
            float roll = UnityEngine.Random.value;
            int resultGrade = baseGrade;

            if (roll < 0.50f) resultGrade = baseGrade;
            else if (roll < 0.80f) resultGrade = Mathf.Min(baseGrade + 1, 5);
            else if (roll < 0.95f) resultGrade = Mathf.Min(baseGrade + 2, 5);
            else resultGrade = Mathf.Min(baseGrade + 3, 5);

            // 랜덤 속성 선택
            string randomDataId = GetRandomSpiritDataId(resultGrade);

            return AddSpirit(randomDataId, resultGrade);
        }

        /// <summary>
        /// 필요한 머지 재료 수
        /// </summary>
        public static int GetRequiredCount(int currentGrade)
        {
            return currentGrade switch
            {
                1 => 3,
                2 => 3,
                3 => 2,
                4 => 2,
                _ => 0
            };
        }

        private string GetRandomSpiritDataId(int grade)
        {
            // TODO: 모든 속성의 dataId 후보에서 랜덤 선택
            string[] elements = { "Fire", "Water", "Wind", "Earth", "Dark", "Light" };
            string element = elements[UnityEngine.Random.Range(0, elements.Length)];
            return $"{element}_{grade}";
        }
    }
}

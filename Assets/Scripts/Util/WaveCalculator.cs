using UnityEngine;

namespace SpiritMerge
{
    /// <summary>
    /// 웨이브별 몬스터 분배 계산기
    /// 
    /// 공식:
    /// - 보스전(n-5, n-10): 마지막 웨이브 = 보스 1마리, 나머지에 (총-1) 분배
    /// - 일반전: 모든 웨이브에 랜덤 분배 (1~5, 합계 = 총 몬스터 수)
    /// 
    /// 사용법:
    /// var distribution = WaveCalculator.DistributeMonsters(30, 5, true);
    /// // 결과: [6, 7, 5, 11, 1] (마지막이 보스)
    /// </summary>
    public static class WaveCalculator
    {
        /// <summary>
        /// 웨이브별 몬스터 수 분배
        /// </summary>
        /// <param name="totalMonsters">총 몬스터 수</param>
        /// <param name="waveCount">웨이브 수</param>
        /// <param name="isBossStage">보스 스테이지 여부</param>
        /// <param name="maxPerWave">1웨이브당 최대 몬스터 수 (0=자동 — 몰림 방지)</param>
        /// <returns>웨이브별 몬스터 수 배열</returns>
        public static int[] DistributeMonsters(int totalMonsters, int waveCount, bool isBossStage, int maxPerWave = 0)
        {
            // ⭐ maxPerWave 자동 계산 (웨이브 몰림 방지):
            //    분배 웨이브당 평균 + 2 (최소 5). 총 몬스터가 많아도 마지막 웨이브에 잔여가 몰리지 않도록
            if (maxPerWave <= 0)
            {
                int fillWaves = isBossStage ? Mathf.Max(1, waveCount - 1) : waveCount;
                int avg = fillWaves > 0 ? Mathf.CeilToInt(totalMonsters / (float)fillWaves) : totalMonsters;
                maxPerWave = Mathf.Max(5, avg + 2);
            }

            int[] distribution = new int[waveCount];

            if (isBossStage)
            {
                // 마지막 웨이브는 보스 1마리
                distribution[waveCount - 1] = 1;
                totalMonsters -= 1;

                // 나머지 웨이브에 분배
                int wavesToFill = waveCount - 1;
                DistributeEvenly(distribution, 0, wavesToFill, totalMonsters, maxPerWave);
            }
            else
            {
                // 모든 웨이브에 분배
                DistributeEvenly(distribution, 0, waveCount, totalMonsters, maxPerWave);
            }

            return distribution;
        }

        /// <summary>
        /// 지정된 범위에 몬스터 균등 분배 — 몫+나머지 방식 (마지막 웨이브 몰림 방지)
        /// 예) 20마리/5웨이브 = [4,4,4,4,4], 38/6 = [7,7,6,6,6,6]
        /// </summary>
        private static void DistributeEvenly(int[] arr, int startIndex, int count, int total, int maxPerWave)
        {
            if (count <= 0 || total <= 0) return;

            int baseN = total / count;
            int rem = total % count;
            for (int i = 0; i < count; i++)
            {
                int val = baseN + (i < rem ? 1 : 0);
                if (maxPerWave > 0) val = Mathf.Min(val, maxPerWave);
                arr[startIndex + i] = val;
            }
        }

        /// <summary>
        /// 시드 기반 분배 (테스트용 - 항상 같은 결과)
        /// </summary>
        public static int[] DistributeMonstersWithSeed(int totalMonsters, int waveCount, bool isBossStage, int seed, int maxPerWave = 5)
        {
            Random.InitState(seed);
            return DistributeMonsters(totalMonsters, waveCount, isBossStage, maxPerWave);
        }

        /// <summary>
        /// 특정 웨이브의 몬스터 수 조회
        /// </summary>
        public static int GetMonsterCountForWave(int[] distribution, int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= distribution.Length) return 0;
            return distribution[waveIndex];
        }

        /// <summary>
        /// 분배 결과 검증 (합계 확인)
        /// </summary>
        public static bool ValidateDistribution(int[] distribution, int expectedTotal, bool isBossStage)
        {
            int sum = 0;
            foreach (int count in distribution)
            {
                sum += count;
            }

            if (isBossStage)
            {
                // 보스전: 총 합계 = expectedTotal (보스 1마리 포함)
                return sum == expectedTotal && distribution[distribution.Length - 1] == 1;
            }
            else
            {
                // 일반전: 총 합계 = expectedTotal
                return sum == expectedTotal;
            }
        }
    }
}

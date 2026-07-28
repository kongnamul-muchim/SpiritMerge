using TMPro;
using UnityEngine;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 미션 UI (GDD §12)
    /// </summary>
    public class MissionUI : MonoBehaviour
    {
        [SerializeField] private Transform dailyRoot;
        [SerializeField] private Transform repeatRoot;
        [SerializeField] private GameObject missionItemPrefab;
        [SerializeField] private TMP_Text rewardText;

        private Core.Systems.MissionService _missions = new();

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            // 기존 항목 제거
            foreach (Transform c in dailyRoot) Destroy(c.gameObject);
            foreach (Transform c in repeatRoot) Destroy(c.gameObject);

            // 일일 미션
            string[] labels = { "스테이지 3회", "소환 3회", "골드 1000", "로그인", "머지 1회" };
            for (int i = 0; i < labels.Length; i++)
            {
                var go = Instantiate(missionItemPrefab, dailyRoot);
                go.GetComponentInChildren<TMP_Text>().text = labels[i];
            }

            rewardText.text = $"일일 보상: 루비 {_missions.GetDailyRubyReward()}";
        }
    }
}

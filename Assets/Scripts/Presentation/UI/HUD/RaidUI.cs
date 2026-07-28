using SpiritMerge.Core.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 레이드 UI — 허수아비 타격 (GDD §14)
    /// </summary>
    public class RaidUI : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Slider timerSlider;

        private RaidService _raid = new();
        private float _timeLeft;

        private void Start()
        {
            startButton?.onClick.AddListener(StartRaid);
        }

        public void StartRaid()
        {
            _timeLeft = 60f;
            startButton.interactable = false;

            // 60초간 데미지 계산 (실제 전투 대신 데미지 시뮬레이션)
            long totalDamage = 0;
            for (int i = 0; i < 60; i++)
            {
                totalDamage += Random.Range(1000, 5000);
            }

            bool isNewRecord = _raid.RecordScore(totalDamage);
            scoreText.text = $"점수: {_raid.LastScore:N0}" + (isNewRecord ? " 🏆 신기록!" : "");
            rankText.text = $"최고 기록: {_raid.BestScore:N0}";
            startButton.interactable = true;
        }

        private void Update()
        {
            if (_timeLeft > 0)
            {
                _timeLeft -= Time.deltaTime;
                timerSlider.value = _timeLeft / 60f;
            }
        }
    }
}

using UnityEngine;
using TMPro;
using SpiritMerge;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// WaveInfo 중앙 배너 — 웨이브/클리어/실패 표시 (페이드 인/아웃)
    /// - 웨이브 시작: "WAVE n / m"
    /// - 스테이지 클리어: "CLEAR! 1-2"
    /// - 패배: "FAIL"
    /// ⭐ Battle Area 정중앙에 강제 배치 (씬 저장 위치 무시)
    /// </summary>
    public class WaveAnimator : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.8f;

        private CanvasGroup _cg;
        private TextMeshProUGUI _tmp;

        void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            _tmp = GetComponent<TextMeshProUGUI>();
            _cg.alpha = 0f; // 시작 시 투명

            // ⭐ Battle Area 정중앙 배치
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.15f, 0.42f);
                rt.anchorMax = new Vector2(0.85f, 0.58f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        /// <summary>
        /// 웨이브 표시: "WAVE n / m" (n은 1부터)
        /// </summary>
        public void ShowWave(int currentWave, int totalWaves)
        {
            if (_tmp != null)
            {
                _tmp.text = $"WAVE {currentWave + 1} / {totalWaves}";
                _tmp.fontSize = 34;
                _tmp.fontStyle = FontStyles.Bold;
                _tmp.color = new Color(1f, 0.85f, 0.4f, 1f);
            }
            GameLogger.Info($"[WaveAnimator] WAVE {currentWave + 1}/{totalWaves} 표시");
            Show(1.5f);
        }

        /// <summary>
        /// 스테이지 클리어 표시: "CLEAR! 1-2"
        /// </summary>
        public void ShowClear(string stageLabel)
        {
            if (_tmp != null)
            {
                _tmp.text = $"CLEAR! {stageLabel}";
                _tmp.fontSize = 44;
                _tmp.fontStyle = FontStyles.Bold;
                _tmp.color = new Color(0.3f, 1f, 0.5f, 1f);
            }
            GameLogger.Info($"[WaveAnimator] CLEAR! {stageLabel} 표시");
            Show(2.0f);
        }

        /// <summary>
        /// 패배 표시: "FAIL"
        /// </summary>
        public void ShowFail()
        {
            if (_tmp != null)
            {
                _tmp.text = "FAIL";
                _tmp.fontSize = 44;
                _tmp.fontStyle = FontStyles.Bold;
                _tmp.color = new Color(1f, 0.3f, 0.3f, 1f);
            }
            GameLogger.Info("[WaveAnimator] FAIL 표시");
            Show(2.0f);
        }

        /// <summary>
        /// 레이드 시작 표시: "레이드 시작!" (보스 등장 전 짧은 연출)
        /// </summary>
        public void ShowRaidStart()
        {
            if (_tmp != null)
            {
                _tmp.text = "레이드 시작!";
                _tmp.fontSize = 40;
                _tmp.fontStyle = FontStyles.Bold;
                _tmp.color = new Color(1f, 0.6f, 0.25f, 1f);
            }
            GameLogger.Info("[WaveAnimator] 레이드 시작! 표시");
            Show(1.2f);
        }

        /// <summary>즉시 숨김 (레이드 시작 시 일반 전투 웨이브 배너 제거용)</summary>
        public void Hide()
        {
            StopAllCoroutines();
            if (_cg != null) _cg.alpha = 0f;
        }

        private void Show(float displayDuration)
        {
            StopAllCoroutines();
            StartCoroutine(FadeSequence(displayDuration));
        }

        private System.Collections.IEnumerator FadeSequence(float displayDuration)
        {
            // 페이드 인
            float t = 0;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                _cg.alpha = Mathf.Clamp01(t / fadeInDuration);
                yield return null;
            }
            _cg.alpha = 1f;

            // 표시 유지
            yield return new WaitForSeconds(displayDuration);

            // 페이드 아웃
            t = 0;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                _cg.alpha = 1f - Mathf.Clamp01(t / fadeOutDuration);
                yield return null;
            }
            _cg.alpha = 0f;
        }
    }
}

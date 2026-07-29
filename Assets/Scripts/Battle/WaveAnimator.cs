using UnityEngine;
using TMPro;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// WaveInfo 페이드 인/아웃 애니메이션
    /// 웨이브 시작 시 "Wave X/Y" 표시 → 2초 후 서서히 사라짐
    /// </summary>
    public class WaveAnimator : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 0.8f;

        private CanvasGroup _cg;
        private TextMeshProUGUI _tmp;

        void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            _tmp = GetComponent<TextMeshProUGUI>();
            _cg.alpha = 0f; // 시작 시 투명
        }

        /// <summary>
        /// 웨이브 표시 실행
        /// </summary>
        public void ShowWave(int currentWave, int totalWaves)
        {
            if (_tmp != null)
                _tmp.text = $"WAVE {currentWave} / {totalWaves}";

            StopAllCoroutines();
            StartCoroutine(FadeSequence());
        }

        private System.Collections.IEnumerator FadeSequence()
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

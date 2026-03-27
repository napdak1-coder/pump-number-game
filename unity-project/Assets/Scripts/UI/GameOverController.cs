using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PumpNumber.Core;

namespace PumpNumber.UI
{
    /// <summary>
    /// 게임 오버 화면 컨트롤러
    /// JS의 gameover-overlay, showGameOver(), goCountUp(), goSequentialReveal() 등을 담당
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [Header("=== 패널 ===")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private GameObject overlayPanel;

        [Header("=== 텍스트 ===")]
        [SerializeField] private TMP_Text titleText;        // "바보!"
        [SerializeField] private TMP_Text newBestText;      // "★ 신기록! ★"
        [SerializeField] private TMP_Text tauntText;        // "다시 해볼 자신 있어?"

        [Header("=== 카드 값들 ===")]
        [SerializeField] private TMP_Text scoreValueText;   // TOTAL SCORE
        [SerializeField] private TMP_Text bestValueText;    // BEST
        [SerializeField] private TMP_Text comboValueText;   // COMBO
        [SerializeField] private TMP_Text stageValueText;   // STAGE
        [SerializeField] private TMP_Text geniusValueText;  // 천재
        [SerializeField] private TMP_Text reverseValueText; // 리버스

        [Header("=== 버튼 ===")]
        [SerializeField] private Button retryButton;

        [Header("=== 순차 등장 요소들 (data-seq 순서) ===")]
        [SerializeField] private CanvasGroup[] sequentialElements;

        [Header("=== 애니메이션 ===")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float sequentialBaseDelay = 0.4f;
        [SerializeField] private float sequentialIncrement = 0.2f;
        [SerializeField] private float countUpDuration = 1.5f;

        private void Start()
        {
            overlayPanel.SetActive(false);
            retryButton.onClick.AddListener(OnRetry);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnGameOver.AddListener(Show);
                gm.OnNewBestScore.AddListener(UpdateNewBest);
                gm.OnGameStart.AddListener(Hide);
            }
        }

        /// <summary>
        /// 게임 오버 화면 표시
        /// JS: showGameOver()의 UI 부분
        /// </summary>
        public void Show()
        {
            overlayPanel.SetActive(true);
            overlayGroup.alpha = 0f;

            var state = GameManager.Instance.State;

            // 순차 등장 요소 초기화 (전부 투명)
            foreach (var cg in sequentialElements)
            {
                if (cg) { cg.alpha = 0f; cg.transform.localScale = Vector3.zero; }
            }

            // 페이드 인
            StartCoroutine(FadeIn());

            // 순차 등장 애니메이션
            StartCoroutine(SequentialReveal());

            // 카운트업 애니메이션 (1.1초 후 시작)
            StartCoroutine(DelayedCountUp(state));
        }

        public void Hide()
        {
            overlayPanel.SetActive(false);
        }

        private void OnRetry()
        {
            GameManager.Instance?.StartGame();
        }

        private void UpdateNewBest(bool isNew)
        {
            if (newBestText) newBestText.gameObject.SetActive(isNew);
        }

        // ================================================================
        // 페이드 인
        // ================================================================
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                overlayGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            overlayGroup.alpha = 1f;
        }

        // ================================================================
        // 순차 등장 (JS: goSequentialReveal)
        // ZoomBounce 효과: 스케일 0 → 1.2 → 1
        // ================================================================
        private IEnumerator SequentialReveal()
        {
            for (int i = 0; i < sequentialElements.Length; i++)
            {
                yield return new WaitForSeconds(i == 0 ? sequentialBaseDelay : sequentialIncrement);

                var cg = sequentialElements[i];
                if (cg == null) continue;

                // ZoomBounce 애니메이션
                float dur = 0.5f;
                float elapsed = 0f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / dur;
                    // easeOutBack 커브
                    float s = 1.70158f;
                    float p = t - 1f;
                    float scale = p * p * ((s + 1f) * p + s) + 1f;
                    cg.alpha = Mathf.Clamp01(t * 2f);
                    cg.transform.localScale = Vector3.one * scale;
                    yield return null;
                }
                cg.alpha = 1f;
                cg.transform.localScale = Vector3.one;
            }
        }

        // ================================================================
        // 카운트업 애니메이션 (JS: goCountUp)
        // easeOutCubic + 바운스
        // ================================================================
        private IEnumerator DelayedCountUp(Data.GameState state)
        {
            yield return new WaitForSeconds(1.1f);

            // 동시에 여러 카운트업 시작
            StartCoroutine(CountUp(scoreValueText, state.score, 1.5f));
            StartCoroutine(CountUp(bestValueText, state.bestScore, 2f));
            StartCoroutine(CountUp(comboValueText, state.maxCombo, 0.8f));

            var config = GameManager.Instance.Config;
            var tier = config.GetDifficulty(state.stageCount);
            if (stageValueText) stageValueText.text = $"{state.stageCount}/30 ({tier.tierName})";

            StartCoroutine(CountUp(geniusValueText, state.geniusCount, 0.8f));
            StartCoroutine(CountUp(reverseValueText, state.reverseClears, 0.8f));
        }

        private IEnumerator CountUp(TMP_Text text, int targetValue, float duration)
        {
            if (text == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // easeOutCubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                int current = Mathf.RoundToInt(Mathf.Lerp(0, targetValue, ease));
                text.text = current.ToString("N0");

                // 바운스 효과 (끝부분)
                if (t > 0.85f)
                {
                    float bounce = 1f + Mathf.Sin((t - 0.85f) / 0.15f * Mathf.PI) * 0.15f;
                    text.transform.localScale = Vector3.one * bounce;
                }

                yield return null;
            }

            text.text = targetValue.ToString("N0");
            text.transform.localScale = Vector3.one;
        }
    }
}

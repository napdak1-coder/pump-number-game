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

        [Header("=== 아깝다 연출 ===")]
        [SerializeField] private TMP_Text nextStageText;        // "다음 스테이지까지 N문제 남았어요!"
        [SerializeField] private Image nextStageProgressBar;     // 진행률 바
        [SerializeField] private TMP_Text bestScoreDiffText;     // "최고기록까지 N점 부족!"
        [SerializeField] private Image bestScoreDiffBar;         // 진행률 바
        [SerializeField] private TMP_Text newBestBadgeText;      // "★ NEW BEST! ★"

        [Header("=== 등급 뱃지 ===")]
        [SerializeField] private TMP_Text rankBadgeText;         // S/A/B/C/D 등급

        [Header("=== 버튼 ===")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button homeButton;              // "홈으로" 버튼

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
            if (homeButton)
                homeButton.onClick.AddListener(OnHome);

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
            // 즉시 재도전 (로딩 없음)
            GameManager.Instance?.StartGame();
        }

        private void OnHome()
        {
            // 홈 화면으로 돌아가기
            // TODO: StartScreenController.Show() 호출 또는 씬 재로드
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGame();
            }
            Hide();
            Debug.Log("홈으로 이동");
        }

        private void UpdateNewBest(bool isNew)
        {
            if (newBestText) newBestText.gameObject.SetActive(isNew);
            if (newBestBadgeText) newBestBadgeText.gameObject.SetActive(isNew);
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

            // 아깝다 연출
            yield return new WaitForSeconds(0.5f);
            ShowGameOverStats(state);

            // 랭킹 서버에 점수 제출
            SubmitScoreToRanking(state);
        }

        /// <summary>
        /// 게임오버 통계 표시 (아깝다 연출)
        /// </summary>
        private void ShowGameOverStats(Data.GameState state)
        {
            // 다음 스테이지까지 남은 문제 수
            int questionsPerStage = 5; // 또는 Config에서 가져옴
            int nextStageProblemCount = state.problemsSolvedInStage % questionsPerStage;
            int remainingProblems = questionsPerStage - nextStageProblemCount;

            if (nextStageText)
            {
                nextStageText.text = $"다음 스테이지까지 {remainingProblems}문제 남았어요!";
            }

            if (nextStageProgressBar)
            {
                float progress = (float)nextStageProblemCount / questionsPerStage;
                nextStageProgressBar.fillAmount = progress;
            }

            // 최고기록까지 남은 점수
            if (!state.isNewBest)
            {
                int previousBest = PlayerPrefs.GetInt("pumpBest", 0);
                int scoreDiff = previousBest - state.score;

                if (bestScoreDiffText)
                {
                    bestScoreDiffText.text = $"최고기록까지 {scoreDiff}점 부족!";
                    bestScoreDiffText.gameObject.SetActive(true);
                }

                if (bestScoreDiffBar)
                {
                    float progress = (float)state.score / previousBest;
                    bestScoreDiffBar.fillAmount = progress;
                    bestScoreDiffBar.gameObject.SetActive(true);
                }
            }
            else
            {
                if (bestScoreDiffText) bestScoreDiffText.gameObject.SetActive(false);
                if (bestScoreDiffBar) bestScoreDiffBar.gameObject.SetActive(false);
            }

            // 등급 뱃지 표시
            if (rankBadgeText)
            {
                char rankGrade = CalculateRankGrade(state.score);
                rankBadgeText.text = rankGrade.ToString();

                // 등급별 색상
                switch (rankGrade)
                {
                    case 'S': rankBadgeText.color = new Color(1f, 0.84f, 0f, 1f); break;    // 골드
                    case 'A': rankBadgeText.color = new Color(0.31f, 1f, 0.56f, 1f); break;  // 초록
                    case 'B': rankBadgeText.color = new Color(0.38f, 0.85f, 0.88f, 1f); break; // 청록
                    case 'C': rankBadgeText.color = new Color(0.31f, 0.69f, 1f, 1f); break;  // 파랑
                    case 'D': rankBadgeText.color = new Color(0.5f, 0.5f, 0.5f, 1f); break;  // 회색
                    default: rankBadgeText.color = Color.white; break;
                }
            }
        }

        /// <summary>
        /// 점수 기반 등급 계산 (S/A/B/C/D)
        /// </summary>
        private char CalculateRankGrade(int score)
        {
            if (score >= 10000) return 'S';
            if (score >= 5000) return 'A';
            if (score >= 2000) return 'B';
            if (score >= 1000) return 'C';
            return 'D';
        }

        /// <summary>
        /// 점수를 랭킹 서버에 제출
        /// </summary>
        private void SubmitScoreToRanking(Data.GameState state)
        {
            // TODO: 네트워크 통신으로 점수 제출
            Debug.Log($"랭킹에 점수 제출: {state.score}점, 스테이지: {state.stageCount}");
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

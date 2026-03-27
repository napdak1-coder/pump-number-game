using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PumpNumber.Core;

namespace PumpNumber.UI
{
    /// <summary>
    /// 시작 화면 컨트롤러
    /// JS의 start-screen, initStartScreen() 담당
    /// </summary>
    public class StartScreenController : MonoBehaviour
    {
        [Header("=== UI 요소 ===")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private TMP_Text titleText;        // "빼봐영"
        [SerializeField] private TMP_Text subtitleText;     // "숫 자 빼 기 게 임"
        [SerializeField] private TMP_Text bestScoreText;    // "🏆 최고기록 0"
        [SerializeField] private Button startButton;

        [Header("=== 숫자 예시 박스 (8-5-3=0) ===")]
        [SerializeField] private TMP_Text[] exampleBoxes;   // 7개 (8, -, 5, -, 3, =, 0)

        private void Start()
        {
            startButton.onClick.AddListener(OnStartClick);

            // 최고기록 로드
            int best = PlayerPrefs.GetInt("pumpBest", 0);
            if (bestScoreText) bestScoreText.text = $"🏆 최고기록 {best}";

            // 게임 시작 시 패널 숨기기
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart.AddListener(Hide);
                GameManager.Instance.OnGameOver.AddListener(ShowAfterDelay);
            }

            Show();
        }

        private void OnStartClick()
        {
            GameManager.Instance?.StartGame();
        }

        public void Show()
        {
            startPanel.SetActive(true);
            int best = PlayerPrefs.GetInt("pumpBest", 0);
            if (bestScoreText) bestScoreText.text = $"🏆 최고기록 {best}";
        }

        public void Hide()
        {
            startPanel.SetActive(false);
        }

        private void ShowAfterDelay()
        {
            // 게임오버 후 재도전 버튼으로 시작하므로
            // 시작 화면은 재도전 안 하고 나갈 때만 표시
        }
    }
}

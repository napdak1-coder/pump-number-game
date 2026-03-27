using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PumpNumber.Core;
using PumpNumber.Data;

namespace PumpNumber.UI
{
    /// <summary>
    /// 게임 HUD (점수, 콤보, 스테이지, 목숨, 타이머, 속도배율)
    /// JS의 info-bar, target-area, timer 등을 담당
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("=== 점수/콤보/스테이지 ===")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text livesText;

        [Header("=== 타겟 숫자 ===")]
        [SerializeField] private TMP_Text targetText;
        [SerializeField] private TMP_Text modeBadgeText;

        [Header("=== 타이머 ===")]
        [SerializeField] private Image timerFill;       // 원형 타이머 fill
        [SerializeField] private Image timerBarFill;     // 선형 타이머 fill (폭탄 심지)

        [Header("=== 속도 배율 ===")]
        [SerializeField] private TMP_Text speedMultText;

        [Header("=== 메시지 ===")]
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text comboMessageText;
        [SerializeField] private TMP_Text tapInfoText;

        [Header("=== 색상 ===")]
        [SerializeField] private Color normalColor = new Color(0.38f, 0.85f, 0.88f, 1f);     // #60d8e0
        [SerializeField] private Color reverseColor = new Color(1f, 0.41f, 0.47f, 1f);        // #ff6878
        [SerializeField] private Color successColor = new Color(0.31f, 1f, 0.56f, 1f);        // #50ff90
        [SerializeField] private Color failColor = new Color(1f, 0.25f, 0.38f, 1f);           // #ff4060
        [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Header("=== 등급 색상 ===")]
        [SerializeField] private Color geniusColor = new Color(0.31f, 1f, 0.56f, 1f);   // #50ff90
        [SerializeField] private Color greatColor = new Color(0.38f, 0.85f, 0.88f, 1f); // #60d8e0
        [SerializeField] private Color goodColor = new Color(0.31f, 0.69f, 1f, 1f);     // #50b0ff
        [SerializeField] private Color okColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnScoreChanged.AddListener(UpdateScore);
            gm.OnComboChanged.AddListener(UpdateCombo);
            gm.OnTargetChanged.AddListener(UpdateTarget);
            gm.OnTimerChanged.AddListener(UpdateTimer);
            gm.OnLivesChanged.AddListener(UpdateLives);
            gm.OnModeChanged.AddListener(UpdateModeBadge);
            gm.OnStageChanged.AddListener(UpdateStage);
            gm.OnMessage.AddListener(ShowMessage);
            gm.OnSpeedMultChanged.AddListener(UpdateSpeedMult);
            gm.OnRoundStart.AddListener(OnRoundStart);
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnScoreChanged.RemoveListener(UpdateScore);
            gm.OnComboChanged.RemoveListener(UpdateCombo);
            gm.OnTargetChanged.RemoveListener(UpdateTarget);
            gm.OnTimerChanged.RemoveListener(UpdateTimer);
            gm.OnLivesChanged.RemoveListener(UpdateLives);
            gm.OnModeChanged.RemoveListener(UpdateModeBadge);
            gm.OnStageChanged.RemoveListener(UpdateStage);
            gm.OnMessage.RemoveListener(ShowMessage);
            gm.OnSpeedMultChanged.RemoveListener(UpdateSpeedMult);
            gm.OnRoundStart.RemoveListener(OnRoundStart);
        }

        private void UpdateScore(int score)
        {
            if (scoreText) scoreText.text = score.ToString("N0");
            // TODO: score-pop 애니메이션 (스케일 펀치)
        }

        private void UpdateCombo(int combo)
        {
            if (comboText) comboText.text = combo.ToString();
            if (comboMessageText)
            {
                if (combo >= 3)
                {
                    comboMessageText.text = $"콤보 x{combo}!";
                    comboMessageText.gameObject.SetActive(true);
                }
                else
                {
                    comboMessageText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateTarget(int target)
        {
            if (targetText == null) return;

            int absTarget = Mathf.Abs(target);
            // 2자리 이상이면 자릿수 분리 표시
            if (absTarget >= 100)
            {
                int h = absTarget / 100, t = (absTarget % 100) / 10, o = absTarget % 10;
                targetText.text = $"{h}·{t}·{o}";
            }
            else if (absTarget >= 10)
            {
                int t = absTarget / 10, o = absTarget % 10;
                targetText.text = $"{t}·{o}";
            }
            else
            {
                targetText.text = target.ToString();
            }

            // 리버스 모드면 색상 변경
            if (GameManager.Instance.State.isReverse)
                targetText.color = reverseColor;
            else
                targetText.color = normalColor;
        }

        private void UpdateTimer(float pct)
        {
            if (timerFill) timerFill.fillAmount = pct / 100f;
            if (timerBarFill) timerBarFill.fillAmount = pct / 100f;

            // 위험 상태 (25% 이하)
            if (pct <= 25f)
            {
                if (timerFill) timerFill.color = dangerColor;
                if (timerBarFill) timerBarFill.color = dangerColor;
            }
            else
            {
                if (timerFill) timerFill.color = Color.white;
                if (timerBarFill) timerBarFill.color = Color.white;
            }
        }

        private void UpdateLives(int lives)
        {
            if (livesText == null) return;
            string filled = new string('♥', lives);
            string empty = new string('♡', 3 - lives);
            livesText.text = $"{filled} {empty}";
        }

        private void UpdateModeBadge(string text, string mode)
        {
            if (modeBadgeText == null) return;
            modeBadgeText.text = text;

            switch (mode)
            {
                case "reverse":
                    modeBadgeText.color = new Color(1f, 0.41f, 0.47f, 1f);
                    break;
                case "forbidden":
                    modeBadgeText.color = new Color(1f, 0.31f, 0.31f, 1f);
                    break;
                case "reverse-forbidden":
                    modeBadgeText.color = new Color(1f, 0.41f, 0.47f, 1f);
                    break;
                default:
                    modeBadgeText.color = new Color(0.31f, 0.72f, 0.66f, 0.4f);
                    break;
            }
        }

        private void UpdateStage(string text)
        {
            if (stageText) stageText.text = text;
        }

        private void ShowMessage(string text, string cls)
        {
            if (messageText == null) return;
            messageText.text = text;
            messageText.gameObject.SetActive(true);

            switch (cls)
            {
                case "genius": messageText.color = geniusColor; break;
                case "great": messageText.color = greatColor; break;
                case "good": messageText.color = goodColor; break;
                case "ok-rank": messageText.color = okColor; break;
                case "fail-msg": messageText.color = failColor; break;
                default: messageText.color = Color.white; break;
            }

            // 1.5초 후 숨기기
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), 1.5f);
        }

        private void HideMessage()
        {
            if (messageText) messageText.gameObject.SetActive(false);
        }

        private void UpdateSpeedMult(float mult)
        {
            if (speedMultText == null) return;
            speedMultText.text = $"x{mult:F1}";
            if (mult >= 3f) speedMultText.color = new Color(1f, 0.6f, 0.2f, 1f); // 불꽃
            else if (mult >= 2f) speedMultText.color = new Color(1f, 0.8f, 0.3f, 1f); // 뜨거움
            else speedMultText.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        }

        private void OnRoundStart()
        {
            if (tapInfoText)
                tapInfoText.text = $"최소 {GameManager.Instance.State.minTaps}번";
            HideMessage();
        }
    }
}

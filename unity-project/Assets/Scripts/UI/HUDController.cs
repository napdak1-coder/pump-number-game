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

        [Header("=== 콤보 시스템 ===")]
        [SerializeField] private Image comboMeterBar;          // 콤보 진행률 바
        [SerializeField] private TMP_Text comboNextStageText;  // "다음: 골드콤보까지 3콤보!"

        [Header("=== 피버 타임 & 천재 ===")]
        [SerializeField] private TMP_Text feverBannerText;     // "🔥 피버 타임! 지금부터 점수 두배! 🔥"
        [SerializeField] private TMP_Text geniusText;          // "★ 천재! ★"

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

        [Header("=== 콤보 단계 색상 ===")]
        [SerializeField] private Color comboGreyColor = new Color(0.5f, 0.5f, 0.5f, 1f);      // 회색
        [SerializeField] private Color comboBlueColor = new Color(0.38f, 0.85f, 0.88f, 1f);   // #60d8e0
        [SerializeField] private Color comboGoldColor = new Color(1f, 0.82f, 0.25f, 1f);     // #ffd140
        [SerializeField] private Color comboRedColor = new Color(1f, 0.41f, 0.47f, 1f);      // #ff6878

        private int currentComboStage = 0;  // 0=그레이, 1=블루, 2=골드, 3=레드, 4+=무지개

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
            if (comboText)
            {
                comboText.text = combo.ToString();
                // 콤보 단계별 색상 변경
                UpdateComboTextColor(combo);
            }

            // 콤보 메시지 업데이트
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

            // 콤보 진행률 바 업데이트
            UpdateComboMeterBar(combo);

            // 다음 단계 텍스트 업데이트
            UpdateComboNextStageText(combo);
        }

        /// <summary>
        /// 콤보 단계별 색상 변경 (회색→파랑→금색→빨강→무지개)
        /// </summary>
        private void UpdateComboTextColor(int combo)
        {
            Color targetColor = comboGreyColor;
            int newStage = 0;

            if (combo >= 50)
            {
                // 무지개 애니메이션 (시간에 따라 색상 변함)
                newStage = 4;
                float hue = (Time.time * 2f) % 1f;
                targetColor = Color.HSVToRGB(hue, 1f, 1f);
            }
            else if (combo >= 30)
            {
                targetColor = comboRedColor;
                newStage = 3;
            }
            else if (combo >= 10)
            {
                targetColor = comboGoldColor;
                newStage = 2;
            }
            else if (combo >= 5)
            {
                targetColor = comboBlueColor;
                newStage = 1;
            }
            else
            {
                targetColor = comboGreyColor;
                newStage = 0;
            }

            if (comboText)
                comboText.color = targetColor;

            // 단계 변경 시 파티클 이펙트 (나중에 추가 가능)
            if (newStage != currentComboStage && combo > 0)
            {
                currentComboStage = newStage;
                OnComboStageChanged(newStage);
            }
        }

        /// <summary>
        /// 콤보 진행률 바 업데이트
        /// </summary>
        private void UpdateComboMeterBar(int combo)
        {
            if (comboMeterBar == null) return;

            // 단계별 목표값
            int nextThreshold = 5;
            if (combo >= 5) nextThreshold = 10;
            if (combo >= 10) nextThreshold = 30;
            if (combo >= 30) nextThreshold = 50;

            int previousThreshold = 0;
            if (combo >= 5) previousThreshold = 5;
            if (combo >= 10) previousThreshold = 10;
            if (combo >= 30) previousThreshold = 30;

            float progress = (float)(combo - previousThreshold) / (nextThreshold - previousThreshold);
            comboMeterBar.fillAmount = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// 다음 단계까지 남은 콤보 수 표시
        /// </summary>
        private void UpdateComboNextStageText(int combo)
        {
            if (comboNextStageText == null) return;

            if (combo >= 50)
            {
                comboNextStageText.text = "★ 맥시멈! ★";
            }
            else if (combo >= 30)
            {
                int remaining = 50 - combo;
                comboNextStageText.text = $"다음: 레인보우콤보까지 {remaining}콤보!";
            }
            else if (combo >= 10)
            {
                int remaining = 30 - combo;
                comboNextStageText.text = $"다음: 레드콤보까지 {remaining}콤보!";
            }
            else if (combo >= 5)
            {
                int remaining = 10 - combo;
                comboNextStageText.text = $"다음: 골드콤보까지 {remaining}콤보!";
            }
            else
            {
                int remaining = 5 - combo;
                comboNextStageText.text = $"다음: 블루콤보까지 {remaining}콤보!";
            }
        }

        /// <summary>
        /// 콤보 단계 변경 시 호출
        /// </summary>
        private void OnComboStageChanged(int newStage)
        {
            // 파티클 이펙트 발생 (ParticleEffects 연동)
            if (ParticleEffects.Instance != null)
            {
                // TODO: ParticleEffects에 OnComboStageChanged 메서드 추가
            }
        }

        /// <summary>
        /// 피버 타임 배너 표시
        /// </summary>
        public void ShowFeverBanner()
        {
            if (feverBannerText == null) return;
            feverBannerText.text = "🔥 피버 타임! 지금부터 점수 두배! 🔥";
            feverBannerText.gameObject.SetActive(true);

            // 애니메이션: 펄스 또는 스케일 업
            LeanTween.cancel(feverBannerText.gameObject);
            LeanTween.scale(feverBannerText.gameObject, Vector3.one * 1.2f, 0.3f)
                .setEase(LeanTweenType.easeInOutQuad);
        }

        /// <summary>
        /// 피버 타임 배너 숨기기
        /// </summary>
        public void HideFeverBanner()
        {
            if (feverBannerText == null) return;
            feverBannerText.gameObject.SetActive(false);
            LeanTween.cancel(feverBannerText.gameObject);
        }

        /// <summary>
        /// 천재! 텍스트 표시 (플래시 애니메이션)
        /// </summary>
        public void ShowGeniusText()
        {
            if (geniusText == null) return;
            geniusText.text = "★ 천재! ★";
            geniusText.gameObject.SetActive(true);

            // 플래시 애니메이션: 색상이 밝아졌다 어두워짐
            LeanTween.cancel(geniusText.gameObject);
            var seq = LeanTween.sequence();
            seq.append(LeanTween.color(geniusText.gameObject, new Color(1f, 0.8f, 0f, 1f), 0.1f));
            seq.append(LeanTween.color(geniusText.gameObject, geniusColor, 0.2f));
            seq.append(LeanTween.delay(0.5f));
            seq.append(LeanTween.alphaText(geniusText.gameObject, 0f, 0.3f));
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PumpNumber.Gameplay
{
    /// <summary>
    /// 콤보 시각화 시스템
    /// 5가지 단계: None(0-4), Blue(5-9), Gold(10-14), Red(15-19), Rainbow(20+)
    /// 각 단계마다 색상, 글로우, 파티클, 점수배율 다름
    /// </summary>
    public class ComboVisualSystem : MonoBehaviour
    {
        #region 콤보 단계 정의
        /// <summary>
        /// 콤보 단계 구조체
        /// </summary>
        [System.Serializable]
        public struct ComboStage
        {
            public int minCombo;                    // 최소 콤보 수
            public int maxCombo;                    // 최대 콤보 수
            public Color stageColor;                // 단계 색상
            public Color glowColor;                 // 글로우 색상
            public GameObject particlePrefab;       // 파티클 프리팹
            public float scoreMultiplier;           // 점수 배율
            [TextArea(2, 3)]
            public string stageName;                // 단계 이름 (예: "블루 콤보")
        }
        #endregion

        #region 직렬화 필드
        [Header("=== 콤보 단계 설정 ===")]
        [SerializeField] private ComboStage[] comboStages = new ComboStage[5];

        [Header("=== UI 참조 ===")]
        [SerializeField] private TMP_Text comboText;                // 콤보 개수 표시
        [SerializeField] private Image comboMeter;                  // 콤보 진행 게이지
        [SerializeField] private TMP_Text comboNextLabel;           // 다음 단계 메시지
        [SerializeField] private GameObject feverBanner;            // 피버 타임 배너

        [Header("=== 파티클 및 이펙트 ===")]
        [SerializeField] private Transform particleSpawnPoint;      // 파티클 생성 위치
        [SerializeField] private float screenShakeDuration = 0.15f;
        [SerializeField] private float screenShakeIntensity = 0.05f;

        [Header("=== 피버 모드 ===")]
        [SerializeField] private int feverActivationCombo = 10;     // 피버 활성화 콤보 수
        [SerializeField] private float feverBannerDuration = 3f;    // 배너 표시 시간
        #endregion

        #region 내부 상태
        private int currentCombo = 0;
        private ComboStage currentStage;
        private bool isFeverActive = false;
        private Coroutine screenShakeCoroutine;
        #endregion

        #region 이벤트
        public delegate void ComboChangedDelegate(int newCombo);
        public event ComboChangedDelegate OnComboChanged;

        public delegate void StageChangedDelegate(ComboStage newStage);
        public event StageChangedDelegate OnStageChanged;

        public delegate void FeverActivatedDelegate();
        public event FeverActivatedDelegate OnFeverActivated;

        public delegate void FeverDeactivatedDelegate();
        public event FeverDeactivatedDelegate OnFeverDeactivated;
        #endregion

        private void Start()
        {
            // 기본값: None 단계
            currentStage = comboStages[0];
            UpdateComboDisplay();
            HideFeverBanner();
        }

        #region 콤보 관리
        /// <summary>
        /// 콤보 증가 (정답 시 호출)
        /// </summary>
        public void IncrementCombo()
        {
            currentCombo++;

            // 단계 확인 및 변경
            ComboStage newStage = GetCurrentStage();
            if (newStage.minCombo != currentStage.minCombo)
            {
                currentStage = newStage;
                OnStageChanged?.Invoke(currentStage);

                // 피버 모드 자동 활성화 (콤보 >= 10)
                if (currentCombo >= feverActivationCombo && !isFeverActive)
                {
                    ActivateFever();
                }
            }

            // 파티클 생성
            SpawnParticles();

            // UI 업데이트
            UpdateComboDisplay();
            OnComboChanged?.Invoke(currentCombo);
        }

        /// <summary>
        /// 콤보 초기화 (오답 시 호출)
        /// </summary>
        public void ResetCombo()
        {
            if (currentCombo == 0) return;

            currentCombo = 0;
            currentStage = comboStages[0];
            DeactivateFever();
            UpdateComboDisplay();
            OnComboChanged?.Invoke(0);
        }

        /// <summary>
        /// 현재 콤보에 해당하는 단계 반환
        /// </summary>
        public ComboStage GetCurrentStage()
        {
            for (int i = comboStages.Length - 1; i >= 0; i--)
            {
                if (currentCombo >= comboStages[i].minCombo)
                {
                    return comboStages[i];
                }
            }
            return comboStages[0];
        }

        /// <summary>
        /// 다음 단계 메시지 반환 (예: "다음: 골드콤보까지 3콤보!")
        /// </summary>
        public string GetNextStageMessage()
        {
            ComboStage nextStage = GetNextStage();
            if (nextStage.minCombo == currentStage.minCombo)
            {
                // 이미 최고 단계
                return $"최고 단계: {currentStage.stageName}!";
            }

            int comboNeeded = nextStage.minCombo - currentCombo;
            return $"다음: {nextStage.stageName}까지 {comboNeeded}콤보!";
        }

        /// <summary>
        /// 다음 콤보 단계 반환
        /// </summary>
        private ComboStage GetNextStage()
        {
            ComboStage current = GetCurrentStage();
            for (int i = 0; i < comboStages.Length - 1; i++)
            {
                if (comboStages[i].minCombo == current.minCombo)
                {
                    return comboStages[i + 1];
                }
            }
            return comboStages[comboStages.Length - 1];
        }

        /// <summary>
        /// 현재 콤보 개수 반환
        /// </summary>
        public int GetCurrentCombo()
        {
            return currentCombo;
        }

        /// <summary>
        /// 현재 콤보의 점수 배율 반환
        /// </summary>
        public float GetScoreMultiplier()
        {
            return currentStage.scoreMultiplier;
        }
        #endregion

        #region 피버 모드
        /// <summary>
        /// 피버 모드 활성화
        /// </summary>
        private void ActivateFever()
        {
            isFeverActive = true;
            ShowFeverBanner();
            OnFeverActivated?.Invoke();
        }

        /// <summary>
        /// 피버 모드 비활성화
        /// </summary>
        private void DeactivateFever()
        {
            if (!isFeverActive) return;

            isFeverActive = false;
            HideFeverBanner();
            OnFeverDeactivated?.Invoke();
        }

        /// <summary>
        /// 피버 타임 배너 표시
        /// </summary>
        private void ShowFeverBanner()
        {
            if (feverBanner == null) return;

            feverBanner.SetActive(true);
            StartCoroutine(FeverBannerTransition());
        }

        /// <summary>
        /// 피버 타임 배너 숨김
        /// </summary>
        private void HideFeverBanner()
        {
            if (feverBanner != null)
                feverBanner.SetActive(false);
        }

        /// <summary>
        /// 피버 배너 애니메이션 코루틴
        /// </summary>
        private IEnumerator FeverBannerTransition()
        {
            CanvasGroup canvasGroup = feverBanner.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = feverBanner.AddComponent<CanvasGroup>();

            // 페이드 인
            float elapsed = 0f;
            float fadeInDuration = 0.3f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }

            // 표시 유지
            yield return new WaitForSeconds(feverBannerDuration);

            // 페이드 아웃
            elapsed = 0f;
            float fadeOutDuration = 0.3f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }

            HideFeverBanner();
        }
        #endregion

        #region 파티클 및 이펙트
        /// <summary>
        /// 정답 시 파티클 생성 (콤보 단계에 따라 개수 달라짐)
        /// </summary>
        private void SpawnParticles()
        {
            if (particlePrefab == null || currentStage.particlePrefab == null)
                return;

            Vector3 spawnPos = particleSpawnPoint != null ?
                particleSpawnPoint.position :
                transform.position;

            // 파티클 개수는 콤보 단계에 따라 조정
            int particleCount = Mathf.RoundToInt(5 * currentStage.scoreMultiplier);

            for (int i = 0; i < particleCount; i++)
            {
                Instantiate(
                    currentStage.particlePrefab,
                    spawnPos,
                    Quaternion.identity
                );
            }
        }

        /// <summary>
        /// 오답 시 화면 흔들림
        /// </summary>
        public void PlayScreenShake()
        {
            if (screenShakeCoroutine != null)
                StopCoroutine(screenShakeCoroutine);

            screenShakeCoroutine = StartCoroutine(ScreenShakeCoroutine());
        }

        /// <summary>
        /// 화면 흔들림 코루틴
        /// </summary>
        private IEnumerator ScreenShakeCoroutine()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) yield break;

            Vector3 originalPos = mainCam.transform.position;
            float elapsed = 0f;

            while (elapsed < screenShakeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = 1f - (elapsed / screenShakeDuration);

                Vector3 randomOffset = Random.insideUnitSphere * screenShakeIntensity * progress;
                mainCam.transform.position = originalPos + randomOffset;

                yield return null;
            }

            mainCam.transform.position = originalPos;
        }
        #endregion

        #region UI 업데이트
        /// <summary>
        /// 콤보 UI 업데이트
        /// </summary>
        private void UpdateComboDisplay()
        {
            // 콤보 텍스트 업데이트
            if (comboText != null)
            {
                comboText.text = $"{currentCombo}";
                comboText.color = currentStage.stageColor;
            }

            // 콤보 게이지 업데이트
            if (comboMeter != null)
            {
                int maxComboInStage = currentStage.maxCombo + 1;
                int minComboInStage = currentStage.minCombo;
                float fillAmount = (currentCombo - minComboInStage) /
                    (float)(maxComboInStage - minComboInStage);
                comboMeter.fillAmount = Mathf.Clamp01(fillAmount);
                comboMeter.color = currentStage.glowColor;
            }

            // 다음 단계 메시지 업데이트
            if (comboNextLabel != null)
            {
                comboNextLabel.text = GetNextStageMessage();
            }
        }
        #endregion

        #region 슬롯머신 전환
        /// <summary>
        /// 숫자 변경 애니메이션 (슬롯머신 스타일)
        /// </summary>
        public IEnumerator SlotMachineTransition(int targetNumber, float duration = 0.3f)
        {
            if (comboText == null) yield break;

            float elapsed = 0f;
            int startNumber = currentCombo;
            int displayNumber = startNumber;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // 선형 보간
                displayNumber = Mathf.RoundToInt(Mathf.Lerp(startNumber, targetNumber, progress));
                comboText.text = displayNumber.ToString();

                yield return null;
            }

            // 최종 값 설정
            comboText.text = targetNumber.ToString();
        }
        #endregion

        /// <summary>
        /// 기본 콤보 단계 초기화 (에디터에서 설정하지 않은 경우)
        /// </summary>
        private void OnValidate()
        {
            if (comboStages == null || comboStages.Length == 0)
            {
                comboStages = new ComboStage[5];
            }

            // 기본값 설정
            if (comboStages[0].minCombo == 0)
            {
                comboStages[0] = new ComboStage
                {
                    minCombo = 0,
                    maxCombo = 4,
                    stageColor = Color.white,
                    glowColor = new Color(0.7f, 0.7f, 0.7f),
                    scoreMultiplier = 1f,
                    stageName = "일반"
                };

                comboStages[1] = new ComboStage
                {
                    minCombo = 5,
                    maxCombo = 9,
                    stageColor = new Color(0.5f, 0.7f, 1f),
                    glowColor = new Color(0.3f, 0.5f, 1f),
                    scoreMultiplier = 1.2f,
                    stageName = "블루"
                };

                comboStages[2] = new ComboStage
                {
                    minCombo = 10,
                    maxCombo = 14,
                    stageColor = new Color(1f, 0.9f, 0.4f),
                    glowColor = new Color(1f, 0.8f, 0f),
                    scoreMultiplier = 1.5f,
                    stageName = "골드"
                };

                comboStages[3] = new ComboStage
                {
                    minCombo = 15,
                    maxCombo = 19,
                    stageColor = new Color(1f, 0.4f, 0.4f),
                    glowColor = new Color(1f, 0.2f, 0.2f),
                    scoreMultiplier = 2f,
                    stageName = "레드"
                };

                comboStages[4] = new ComboStage
                {
                    minCombo = 20,
                    maxCombo = 999,
                    stageColor = new Color(1f, 0.4f, 0.9f),
                    glowColor = new Color(1f, 0f, 1f),
                    scoreMultiplier = 3f,
                    stageName = "레인보우"
                };
            }
        }
    }
}

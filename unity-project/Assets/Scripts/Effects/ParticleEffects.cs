using System.Collections;
using UnityEngine;
using PumpNumber.Core;

namespace PumpNumber.Effects
{
    /// <summary>
    /// 파티클 이펙트 매니저 — 배경 효과 + 게임 이펙트
    /// JS의 fxSparkle, fxPixelRain, fxStarTwinkle, fxComet, fxDust, fxHexFloat +
    /// spawnParticles, spawnFlash, goFireworks 등을 Unity Particle System으로 구현
    /// </summary>
    public class ParticleEffects : MonoBehaviour
    {
        public static ParticleEffects Instance { get; private set; }

        [Header("=== 배경 효과 (9번 풀 이펙트 피버) ===")]
        [SerializeField] private ParticleSystem starTwinklePS;
        [SerializeField] private ParticleSystem pixelRainPS;
        [SerializeField] private ParticleSystem sparklePS;
        [SerializeField] private ParticleSystem cometPS;
        [SerializeField] private ParticleSystem dustPS;
        [SerializeField] private ParticleSystem hexFloatPS;

        [Header("=== 게임 이펙트 ===")]
        [SerializeField] private ParticleSystem tapParticlePS;     // 키 탭 시 파티클
        [SerializeField] private ParticleSystem successParticlePS;  // 성공 시 파티클
        [SerializeField] private ParticleSystem failParticlePS;     // 실패 시 파티클
        [SerializeField] private ParticleSystem fireworkPS;         // 게임오버 폭죽
        [SerializeField] private ParticleSystem feverBurstPS;       // 피버 타임 활성화 폭발
        [SerializeField] private ParticleSystem comboStagePS;       // 콤보 단계 업 파티클
        [SerializeField] private ParticleSystem screenEdgeGlowPS;   // 화면 가장자리 불빛 (피버)
        [SerializeField] private ParticleSystem achievementPS;      // 업적 언락 파티클

        [Header("=== 화면 플래시 ===")]
        [SerializeField] private CanvasGroup flashOverlay;

        [Header("=== 색상 팔레트 ===")]
        public Color[] mixColors = {
            new Color(1f, 0.82f, 0.25f, 1f),   // #ffd040 (회색)
            new Color(0.38f, 0.85f, 0.88f, 1f), // #60d8e0 (파랑)
            new Color(1f, 0.82f, 0.25f, 1f),   // #ffd140 (금색)
            new Color(1f, 0.41f, 0.47f, 1f),   // #ff6878 (빨강)
        };

        [Header("=== 콤보 단계 색상 ===")]
        private Color[] comboStageColors = {
            new Color(0.5f, 0.5f, 0.5f, 1f),      // 회색
            new Color(0.38f, 0.85f, 0.88f, 1f),   // 파랑
            new Color(1f, 0.82f, 0.25f, 1f),     // 금색
            new Color(1f, 0.41f, 0.47f, 1f),     // 빨강
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnGameStart.AddListener(StartBackgroundEffects);
            gm.OnKeyPressed.AddListener(OnKeyPressed);
            gm.OnSuccess.AddListener(OnSuccess);
            gm.OnFail.AddListener(OnFail);
            gm.OnGameOver.AddListener(OnGameOver);
        }

        /// <summary>
        /// 배경 효과 시작 (peach 테마 9번 풀 이펙트 피버)
        /// </summary>
        public void StartBackgroundEffects()
        {
            if (starTwinklePS) starTwinklePS.Play();
            if (pixelRainPS) pixelRainPS.Play();
            if (sparklePS) sparklePS.Play();
            if (cometPS) cometPS.Play();
            if (dustPS) dustPS.Play();
            if (hexFloatPS) hexFloatPS.Play();
        }

        public void StopBackgroundEffects()
        {
            if (starTwinklePS) starTwinklePS.Stop();
            if (pixelRainPS) pixelRainPS.Stop();
            if (sparklePS) sparklePS.Stop();
            if (cometPS) cometPS.Stop();
            if (dustPS) dustPS.Stop();
            if (hexFloatPS) hexFloatPS.Stop();
        }

        /// <summary>
        /// 키 탭 시 파티클 발생
        /// JS: spawnParticles(cx, cy)
        /// </summary>
        private void OnKeyPressed(int num, int actualNum)
        {
            if (tapParticlePS == null) return;

            // 파티클 색상을 MIX 팔레트에서 선택
            var main = tapParticlePS.main;
            main.startColor = mixColors[num % mixColors.Length];
            tapParticlePS.Emit(8);
        }

        /// <summary>
        /// 성공 시 이펙트
        /// 콤보에 따라 파티클 수와 색상이 변함
        /// </summary>
        private void OnSuccess()
        {
            int combo = GameManager.Instance?.State.combo ?? 0;

            // 콤보에 따라 파티클 수 조정
            int particleCount = 15;  // 기본값
            if (combo >= 30) particleCount = 60;
            else if (combo >= 10) particleCount = 40;
            else if (combo >= 5) particleCount = 25;

            // 콤보 단계에 따라 색상 변경
            int stageIndex = 0;
            if (combo >= 30) stageIndex = 3;    // 빨강
            else if (combo >= 10) stageIndex = 2; // 금색
            else if (combo >= 5) stageIndex = 1;  // 파랑
            else stageIndex = 0;                // 회색

            Color particleColor = comboStageColors[stageIndex];

            if (successParticlePS)
            {
                var main = successParticlePS.main;
                main.startColor = particleColor;
                main.maxParticles = particleCount;
                successParticlePS.Play();
            }

            StartCoroutine(FlashScreen(new Color(0.31f, 1f, 0.56f, 0.15f), 0.15f));
        }

        /// <summary>
        /// 실패 시 이펙트
        /// 빨간 파티클, 아래로 향함
        /// </summary>
        private void OnFail(string reason)
        {
            if (failParticlePS)
            {
                var main = failParticlePS.main;
                main.startColor = new Color(1f, 0.41f, 0.47f, 1f); // 빨강
                failParticlePS.Play();
            }

            StartCoroutine(FlashScreen(new Color(1f, 0.13f, 0.27f, 0.2f), 0.2f));

            // 진동
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif
        }

        /// <summary>
        /// 피버 타임 활성화 - 큰 폭발 이펙트
        /// </summary>
        public void OnFeverActivated()
        {
            if (feverBurstPS)
            {
                var main = feverBurstPS.main;
                main.startColor = new Color(1f, 0.6f, 0.2f, 1f); // 주황색
                feverBurstPS.Play();
            }

            // 화면 가장자리 불빛 시작
            if (screenEdgeGlowPS)
                screenEdgeGlowPS.Play();

            // 화면 플래시
            StartCoroutine(FlashScreen(new Color(1f, 0.6f, 0.2f, 0.3f), 0.3f));
        }

        /// <summary>
        /// 피버 타임 종료
        /// </summary>
        public void OnFeverDeactivated()
        {
            if (screenEdgeGlowPS)
                screenEdgeGlowPS.Stop();
        }

        /// <summary>
        /// 콤보 단계 변경 축하 파티클
        /// </summary>
        public void OnComboStageChanged(int newStage)
        {
            if (comboStagePS)
            {
                var main = comboStagePS.main;
                if (newStage < comboStageColors.Length)
                    main.startColor = comboStageColors[newStage];
                comboStagePS.Play();
            }
        }

        /// <summary>
        /// 업적 언락 파티클 (금색 별)
        /// </summary>
        public void OnAchievementUnlocked(string achievementName)
        {
            if (achievementPS)
            {
                var main = achievementPS.main;
                main.startColor = new Color(1f, 0.84f, 0f, 1f); // 금색
                achievementPS.Play();
            }

            Debug.Log($"업적 언락: {achievementName}");
        }

        /// <summary>
        /// 게임오버 폭죽
        /// JS: goFireworks()
        /// </summary>
        private void OnGameOver()
        {
            StopBackgroundEffects();
            if (fireworkPS)
            {
                StartCoroutine(DelayedFirework());
            }
        }

        private IEnumerator DelayedFirework()
        {
            yield return new WaitForSeconds(0.6f);
            fireworkPS.Play();
        }

        /// <summary>
        /// 화면 플래시 효과
        /// JS: spawnFlash()
        /// </summary>
        private IEnumerator FlashScreen(Color color, float duration)
        {
            if (flashOverlay == null) yield break;

            // 플래시 색상 설정
            var img = flashOverlay.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = color;

            flashOverlay.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                flashOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }
            flashOverlay.alpha = 0f;
        }
    }
}

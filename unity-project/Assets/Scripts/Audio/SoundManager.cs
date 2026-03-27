using UnityEngine;
using PumpNumber.Core;

namespace PumpNumber.Audio
{
    /// <summary>
    /// 사운드 매니저 — 8비트 효과음 생성 + 재생
    /// JS의 playTone(), sfxTap(), sfxSuccess(), sfxGenius(), sfxFail(), sfxGameOver() 등을 담당
    /// ComboVisualSystem, FeverMode와 통합된 효과음 시스템
    /// Unity에서는 AudioSource + AudioClip(동적 생성) 또는 프리메이드 WAV 사용
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("=== 오디오 소스 ===")]
        [SerializeField] private AudioSource sfxSource;

        [Header("=== 프리메이드 효과음 (선택사항, 없으면 프로시저럴 생성) ===")]
        [SerializeField] private AudioClip tapClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip geniusClip;
        [SerializeField] private AudioClip failClip;
        [SerializeField] private AudioClip gameOverClip;
        [SerializeField] private AudioClip comboClip;

        // === 콤보 스테이지 변화 효과음 ===
        [SerializeField] private AudioClip comboBlueStageClip;   // 5~9 콤보 (파란색)
        [SerializeField] private AudioClip comboGoldStageClip;   // 10~14 콤보 (골드)
        [SerializeField] private AudioClip comboRedStageClip;    // 15~19 콤보 (빨강)
        [SerializeField] private AudioClip comboRainbowStageClip; // 20+ 콤보 (레인보우)

        // === 피버 모드 효과음 ===
        [SerializeField] private AudioClip feverActivateClip;
        [SerializeField] private AudioClip feverDeactivateClip;

        // === 기타 효과음 ===
        [SerializeField] private AudioClip wrongAnswerClip;      // 오답
        [SerializeField] private AudioClip screenShakeClip;      // 화면 흔들림
        [SerializeField] private AudioClip slotMachineRollClip;  // 슬롯머신 회전음
        [SerializeField] private AudioClip achievementUnlockClip; // 업적 해제

        [Header("=== 설정 ===")]
        [SerializeField] private float masterVolume = 0.5f;
        [SerializeField] private bool soundEnabled = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnKeyPressed.AddListener((num, actual) => PlayTap(num));
            gm.OnSuccess.AddListener(PlaySuccess);
            gm.OnFail.AddListener((_) => PlayFail());
            gm.OnGameOver.AddListener(PlayGameOver);
        }

        /// <summary>
        /// 프로시저럴 톤 생성 (8비트 사운드)
        /// JS: playTone(freq, duration, waveType, volume)
        /// </summary>
        public AudioClip GenerateTone(float frequency, float duration, string waveType = "sine", float volume = 0.15f)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            AudioClip clip = AudioClip.Create("tone", sampleCount, 1, sampleRate, false);

            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Lerp(volume, 0.001f, t / duration); // 감쇠

                float sample;
                switch (waveType)
                {
                    case "square":
                        sample = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));
                        break;
                    case "sawtooth":
                        sample = 2f * (t * frequency - Mathf.Floor(t * frequency + 0.5f));
                        break;
                    default: // sine
                        sample = Mathf.Sin(2f * Mathf.PI * frequency * t);
                        break;
                }

                samples[i] = sample * envelope;
            }

            clip.SetData(samples, 0);
            return clip;
        }

        // ================================================================
        // 효과음 재생
        // ================================================================

        /// <summary>
        /// 키 탭 사운드
        /// JS: sfxTap(n) → playTone(300+n*80, .12, 'square', .1) + playTone(600+n*60, .08, 'sine', .06)
        /// </summary>
        public void PlayTap(int num)
        {
            if (!soundEnabled) return;

            if (tapClip != null)
            {
                sfxSource.PlayOneShot(tapClip, masterVolume * 0.7f);
            }
            else
            {
                // 프로시저럴 8비트 사운드
                var clip = GenerateTone(300 + num * 80, 0.12f, "square", 0.1f);
                sfxSource.PlayOneShot(clip, masterVolume);
            }
        }

        /// <summary>
        /// 성공 사운드
        /// JS: sfxSuccess() → 4음 상승 시퀀스
        /// </summary>
        public void PlaySuccess()
        {
            if (!soundEnabled) return;

            if (successClip != null)
            {
                sfxSource.PlayOneShot(successClip, masterVolume);
            }
            else
            {
                StartCoroutine(PlayToneSequence(
                    new float[] { 500, 650, 800, 950 },
                    new float[] { 0, 0.08f, 0.16f, 0.24f },
                    0.2f, "sine", 0.12f
                ));
            }
        }

        public void PlayGenius()
        {
            if (!soundEnabled) return;

            if (geniusClip != null)
                sfxSource.PlayOneShot(geniusClip, masterVolume);
            else
            {
                StartCoroutine(PlayToneSequence(
                    new float[] { 400, 520, 640, 760, 880, 1000 },
                    new float[] { 0, 0.06f, 0.12f, 0.18f, 0.24f, 0.3f },
                    0.25f, "sine", 0.15f
                ));
            }
        }

        public void PlayFail()
        {
            if (!soundEnabled) return;

            if (failClip != null)
                sfxSource.PlayOneShot(failClip, masterVolume);
            else
            {
                var clip = GenerateTone(200, 0.3f, "sawtooth", 0.12f);
                sfxSource.PlayOneShot(clip, masterVolume);
            }
        }

        public void PlayGameOver()
        {
            if (!soundEnabled) return;

            if (gameOverClip != null)
                sfxSource.PlayOneShot(gameOverClip, masterVolume);
            else
            {
                StartCoroutine(PlayToneSequence(
                    new float[] { 300, 220, 140 },
                    new float[] { 0, 0.2f, 0.4f },
                    0.5f, "sawtooth", 0.1f
                ));
            }
        }

        public void PlayCombo()
        {
            if (!soundEnabled) return;

            if (comboClip != null)
                sfxSource.PlayOneShot(comboClip, masterVolume);
            else
            {
                var clip = GenerateTone(800, 0.15f, "sine", 0.1f);
                sfxSource.PlayOneShot(clip, masterVolume);
            }
        }

        /// <summary>
        /// 콤보 스테이지 변경 효과음 재생
        /// ComboVisualSystem에서 호출
        /// stageIndex: 0=Blue(5~9), 1=Gold(10~14), 2=Red(15~19), 3=Rainbow(20+)
        /// </summary>
        public void PlayComboStageChange(int stageIndex)
        {
            if (!soundEnabled) return;

            AudioClip clip = stageIndex switch
            {
                0 => comboBlueStageClip,
                1 => comboGoldStageClip,
                2 => comboRedStageClip,
                3 => comboRainbowStageClip,
                _ => null
            };

            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, masterVolume);
            }
            else
            {
                // 프로시저럴 생성 (단계별 톤)
                float freq = stageIndex switch
                {
                    0 => 600f,  // Blue: 낮음
                    1 => 800f,  // Gold: 중간
                    2 => 1000f, // Red: 높음
                    3 => 1200f, // Rainbow: 매우 높음
                    _ => 800f
                };
                var genClip = GenerateTone(freq, 0.3f, "sine", 0.15f);
                sfxSource.PlayOneShot(genClip, masterVolume);
            }
        }

        /// <summary>
        /// 피버 활성화 효과음
        /// 한국식: "피버 타임 활성화"
        /// </summary>
        public void PlayFeverActivate() // 피버_활성화음()
        {
            if (!soundEnabled) return;

            if (feverActivateClip != null)
            {
                sfxSource.PlayOneShot(feverActivateClip, masterVolume);
            }
            else
            {
                // 프로시저럴 상승 음
                StartCoroutine(PlayToneSequence(
                    new float[] { 600, 800, 1000, 1200 },
                    new float[] { 0, 0.1f, 0.2f, 0.3f },
                    0.15f, "sine", 0.15f
                ));
            }
        }

        /// <summary>
        /// 피버 해제 효과음
        /// 한국식: "피버_종료음()"
        /// </summary>
        public void PlayFeverDeactivate() // 피버_종료음()
        {
            if (!soundEnabled) return;

            if (feverDeactivateClip != null)
            {
                sfxSource.PlayOneShot(feverDeactivateClip, masterVolume);
            }
            else
            {
                // 프로시저럴 하강 음
                StartCoroutine(PlayToneSequence(
                    new float[] { 1200, 1000, 800, 600 },
                    new float[] { 0, 0.1f, 0.2f, 0.3f },
                    0.15f, "sine", 0.12f
                ));
            }
        }

        /// <summary>
        /// 오답 + 화면 흔들림 효과음
        /// 한국식: "오답음()"
        /// </summary>
        public void PlayWrongAnswer() // 오답음()
        {
            if (!soundEnabled) return;

            if (wrongAnswerClip != null)
            {
                sfxSource.PlayOneShot(wrongAnswerClip, masterVolume);
            }
            else
            {
                var clip = GenerateTone(150, 0.4f, "sawtooth", 0.15f);
                sfxSource.PlayOneShot(clip, masterVolume);
            }
        }

        /// <summary>
        /// 정답 + 콤보 피치 상승음
        /// 콤보 수에 따라 피치 변경
        /// 한국식: "정답음(콤보수)"
        /// </summary>
        public void PlayCorrectWithCombo(int comboCount) // 정답음(콤보수)
        {
            if (!soundEnabled) return;

            // 콤보 수에 따라 기본 피치 상향
            float basePitch = 500 + (comboCount * 50); // 콤보당 50Hz 상승
            basePitch = Mathf.Clamp(basePitch, 500, 1500);

            if (successClip != null)
            {
                sfxSource.pitch = 1 + (comboCount * 0.05f); // 피치 변경
                sfxSource.PlayOneShot(successClip, masterVolume);
                sfxSource.pitch = 1f; // 원래대로 복구
            }
            else
            {
                var clip = GenerateTone(basePitch, 0.2f, "sine", 0.12f);
                sfxSource.PlayOneShot(clip, masterVolume);
            }
        }

        /// <summary>
        /// 슬롯머신 회전음
        /// Collection 아이템 또는 랭크 표시 시 사용
        /// 한국식: "슬롯머신음()"
        /// </summary>
        public void PlaySlotMachineRoll() // 슬롯머신음()
        {
            if (!soundEnabled) return;

            if (slotMachineRollClip != null)
            {
                sfxSource.PlayOneShot(slotMachineRollClip, masterVolume);
            }
            else
            {
                StartCoroutine(PlayToneSequence(
                    new float[] { 800, 900, 800, 900, 800 },
                    new float[] { 0, 0.05f, 0.1f, 0.15f, 0.2f },
                    0.1f, "square", 0.1f
                ));
            }
        }

        /// <summary>
        /// 업적 해제 효과음
        /// 한국식: "업적해제음()"
        /// </summary>
        public void PlayAchievementUnlock() // 업적해제음()
        {
            if (!soundEnabled) return;

            if (achievementUnlockClip != null)
            {
                sfxSource.PlayOneShot(achievementUnlockClip, masterVolume);
            }
            else
            {
                StartCoroutine(PlayToneSequence(
                    new float[] { 800, 1000, 1200, 1400 },
                    new float[] { 0, 0.1f, 0.2f, 0.3f },
                    0.2f, "sine", 0.15f
                ));
            }
        }

        public void SetSoundEnabled(bool enabled) => soundEnabled = enabled;
        public void SetVolume(float vol) => masterVolume = Mathf.Clamp01(vol);

        private System.Collections.IEnumerator PlayToneSequence(
            float[] freqs, float[] delays, float dur, string wave, float vol)
        {
            for (int i = 0; i < freqs.Length; i++)
            {
                if (i > 0) yield return new WaitForSeconds(delays[i] - delays[i - 1]);
                var clip = GenerateTone(freqs[i], dur, wave, vol);
                sfxSource.PlayOneShot(clip, masterVolume);
            }
        }
    }
}

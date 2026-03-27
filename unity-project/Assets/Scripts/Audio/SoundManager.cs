using UnityEngine;
using PumpNumber.Core;

namespace PumpNumber.Audio
{
    /// <summary>
    /// 사운드 매니저 — 8비트 효과음 생성 + 재생
    /// JS의 playTone(), sfxTap(), sfxSuccess(), sfxGenius(), sfxFail(), sfxGameOver() 등을 담당
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

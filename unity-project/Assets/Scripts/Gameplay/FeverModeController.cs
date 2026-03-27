using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PumpNumber.Data;

namespace PumpNumber.Gameplay
{
    /// <summary>
    /// 피버 모드 컨트롤러 — 한국식 로컬라이징 피버 타임 구현
    /// 콤보 >= 10으로 활성화, 점수 2배, 시각 효과 포함
    /// </summary>
    public class FeverModeController : MonoBehaviour
    {
        // === 피버 모드 상태 ===
        private bool _isFeverActive = false;
        private int _totalFeverActivations = 0;

        // === 설정 ===
        [SerializeField] private int _feverActivationThreshold = 10; // 콤보 10 이상
        [SerializeField] private float _screenShakeIntensity = 0.3f;
        [SerializeField] private float _screenShakeDuration = 0.5f;

        // === UI 참조 ===
        [SerializeField] private GameObject _feverBannerObject;
        [SerializeField] private TextMeshProUGUI _feverBannerText;

        // === 파티클 시스템 ===
        [SerializeField] private ParticleSystem _feverParticleLeft;
        [SerializeField] private ParticleSystem _feverParticleRight;

        // === 엣지 글로우 ===
        [SerializeField] private Image _feverEdgeGlowLeft;
        [SerializeField] private Image _feverEdgeGlowRight;

        // === 카메라 및 HUD ===
        private Camera _mainCamera;
        private Image[] _hudBorderImages;

        // === 이벤트 ===
        public UnityEvent OnFeverActivated;
        public UnityEvent OnFeverDeactivated;

        // === 애니메이션 ===
        private Coroutine _feverActivationCoroutine;
        private Coroutine _feverDeactivationCoroutine;

        // === 원본 배경색 저장 ===
        private Color _originalCameraColor;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _hudBorderImages = FindObjectsOfType<Image>();

            if (_feverBannerObject == null)
                Debug.LogWarning("[FeverModeController] feverBannerObject이 할당되지 않았습니다!");

            if (_feverBannerText == null)
                Debug.LogWarning("[FeverModeController] feverBannerText가 할당되지 않았습니다!");

            // 배너 초기 상태: 비활성화
            if (_feverBannerObject != null)
                _feverBannerObject.SetActive(false);

            // 원본 카메라 색상 저장
            if (_mainCamera != null)
                _originalCameraColor = _mainCamera.backgroundColor;
        }

        private void Update()
        {
            // 편의상 피버 상태 시각적 업데이트
            if (_isFeverActive && _feverEdgeGlowLeft != null && _feverEdgeGlowRight != null)
            {
                // 엣지 글로우 펄싱 효과
                float pulse = Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f;
                Color glowColor = new Color(1f, 0.3f, 0.3f, pulse * 0.8f);
                _feverEdgeGlowLeft.color = glowColor;
                _feverEdgeGlowRight.color = glowColor;
            }
        }

        /// <summary>
        /// 피버 모드 활성화 (콤보 도달 시 호출)
        /// </summary>
        public void ActivateFever()
        {
            if (_isFeverActive)
                return;

            _isFeverActive = true;
            _totalFeverActivations++;

            Debug.Log($"[FeverModeController] 피버 활성화! (총 {_totalFeverActivations}회)");

            // 기존 코루틴 중지
            if (_feverActivationCoroutine != null)
                StopCoroutine(_feverActivationCoroutine);

            // 활성화 애니메이션 시작
            _feverActivationCoroutine = StartCoroutine(FeverActivationAnimation());

            // 스크린 쉐이크
            if (_mainCamera != null)
                StartCoroutine(ScreenShake());

            // 이벤트 발생
            OnFeverActivated?.Invoke();
        }

        /// <summary>
        /// 피버 모드 비활성화 (오답 시 호출)
        /// </summary>
        public void DeactivateFever()
        {
            if (!_isFeverActive)
                return;

            _isFeverActive = false;

            Debug.Log("[FeverModeController] 피버 종료");

            // 기존 코루틴 중지
            if (_feverActivationCoroutine != null)
                StopCoroutine(_feverActivationCoroutine);

            if (_feverDeactivationCoroutine != null)
                StopCoroutine(_feverDeactivationCoroutine);

            // 비활성화 애니메이션 시작
            _feverDeactivationCoroutine = StartCoroutine(FeverDeactivationAnimation());

            // 이벤트 발생
            OnFeverDeactivated?.Invoke();
        }

        /// <summary>
        /// 피버 활성화 애니메이션
        /// </summary>
        private IEnumerator FeverActivationAnimation()
        {
            // 배너 표시
            if (_feverBannerObject != null)
            {
                _feverBannerObject.SetActive(true);
                if (_feverBannerText != null)
                    _feverBannerText.text = "🔥 피버 타임! 지금부터 점수 두배! 🔥";
            }

            // 배경색 변경 (따뜻한 보라색/주황색)
            if (_mainCamera != null)
            {
                Color warmColor = new Color(0.6f, 0.2f, 0.4f, 1f);
                float elapsed = 0f;
                while (elapsed < 0.5f && _isFeverActive)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / 0.5f;
                    _mainCamera.backgroundColor = Color.Lerp(_originalCameraColor, warmColor, t);
                    yield return null;
                }
            }

            // 파티클 활성화
            if (_feverParticleLeft != null)
                _feverParticleLeft.Play();
            if (_feverParticleRight != null)
                _feverParticleRight.Play();

            // 엣지 글로우 시작
            if (_feverEdgeGlowLeft != null && _feverEdgeGlowRight != null)
            {
                _feverEdgeGlowLeft.color = new Color(1f, 0.3f, 0.3f, 0.8f);
                _feverEdgeGlowRight.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            }

            // HUD 테두리 글로우
            UpdateHudBorderGlow(true);

            yield return null;
        }

        /// <summary>
        /// 피버 비활성화 애니메이션
        /// </summary>
        private IEnumerator FeverDeactivationAnimation()
        {
            // 파티클 비활성화
            if (_feverParticleLeft != null)
                _feverParticleLeft.Stop();
            if (_feverParticleRight != null)
                _feverParticleRight.Stop();

            // 배경색 복귀
            if (_mainCamera != null)
            {
                float elapsed = 0f;
                Color currentColor = _mainCamera.backgroundColor;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / 0.3f;
                    _mainCamera.backgroundColor = Color.Lerp(currentColor, _originalCameraColor, t);
                    yield return null;
                }
            }

            // 엣지 글로우 비활성화
            if (_feverEdgeGlowLeft != null)
                _feverEdgeGlowLeft.color = new Color(1f, 0.3f, 0.3f, 0f);
            if (_feverEdgeGlowRight != null)
                _feverEdgeGlowRight.color = new Color(1f, 0.3f, 0.3f, 0f);

            // 배너 숨기기
            if (_feverBannerObject != null)
                _feverBannerObject.SetActive(false);

            // HUD 테두리 글로우 해제
            UpdateHudBorderGlow(false);

            yield return null;
        }

        /// <summary>
        /// HUD 테두리 글로우 업데이트
        /// </summary>
        private void UpdateHudBorderGlow(bool isActive)
        {
            foreach (var image in _hudBorderImages)
            {
                if (image.CompareTag("HUDBorder"))
                {
                    if (isActive)
                    {
                        image.color = new Color(1f, 0.5f, 0f, 0.8f); // 따뜻한 오렌지색
                    }
                    else
                    {
                        image.color = Color.white; // 원래 색상
                    }
                }
            }
        }

        /// <summary>
        /// 스크린 쉐이크 효과
        /// </summary>
        private IEnumerator ScreenShake()
        {
            if (_mainCamera == null)
                yield break;

            Vector3 originalPos = _mainCamera.transform.position;
            float elapsed = 0f;

            while (elapsed < _screenShakeDuration)
            {
                elapsed += Time.deltaTime;
                float x = Random.Range(-1f, 1f) * _screenShakeIntensity;
                float y = Random.Range(-1f, 1f) * _screenShakeIntensity;

                _mainCamera.transform.position = originalPos + new Vector3(x, y, 0);
                yield return null;
            }

            _mainCamera.transform.position = originalPos;
        }

        /// <summary>
        /// 피버 모드 활성 상태 확인
        /// </summary>
        public bool IsFeverActive => _isFeverActive;

        /// <summary>
        /// 현재까지의 피버 활성화 횟수 반환
        /// </summary>
        public int GetFeverCount()
        {
            return _totalFeverActivations;
        }

        /// <summary>
        /// 점수 배율 계산 (피버 모드 2배)
        /// </summary>
        public float GetScoreMultiplier()
        {
            return _isFeverActive ? 2f : 1f;
        }

        /// <summary>
        /// 콤보 기반 피버 활성화 체크
        /// GameplayManager에서 호출
        /// </summary>
        public void CheckFeverActivation(int currentCombo)
        {
            if (currentCombo >= _feverActivationThreshold && !_isFeverActive)
            {
                ActivateFever();
            }
        }

        /// <summary>
        /// 피버 통계 초기화 (게임 재시작 시)
        /// </summary>
        public void ResetFeverForNewGame()
        {
            _isFeverActive = false;

            if (_feverActivationCoroutine != null)
                StopCoroutine(_feverActivationCoroutine);
            if (_feverDeactivationCoroutine != null)
                StopCoroutine(_feverDeactivationCoroutine);

            if (_feverBannerObject != null)
                _feverBannerObject.SetActive(false);

            if (_mainCamera != null)
                _mainCamera.backgroundColor = _originalCameraColor;
        }
    }
}

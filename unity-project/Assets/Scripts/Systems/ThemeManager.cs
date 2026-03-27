using UnityEngine;
using UnityEngine.Events;
using PumpNumber.Data;
using TMPro;

namespace PumpNumber.Systems
{
    /// <summary>
    /// 테마 관리 시스템 — 4가지 비주얼 테마 제공
    /// Space(우주), Ocean(바다), Forest(숲), Neon(네온)
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        [System.Serializable]
        public struct ThemeData
        {
            public ThemeType themeType;
            public Color bgGradientTop;      // 배경 그래디언트 상단
            public Color bgGradientBottom;   // 배경 그래디언트 하단
            public Color accentColor;        // HUD 강조 색상
            public Color particleColor;      // 파티클 색상
            public ParticleType particleType; // 파티클 종류
        }

        public enum ParticleType
        {
            Star,   // 별 (우주)
            Bubble, // 거품 (바다)
            Leaf,   // 잎 (숲)
            Neon    // 네온 (네온)
        }

        // === 싱글톤 ===
        private static ThemeManager _instance;
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ThemeManager>();
                return _instance;
            }
        }

        // === 테마 데이터 ===
        private ThemeData[] _themeDatas = new ThemeData[4];
        private ThemeData _currentThemeData;

        // === 컴포넌트 참조 ===
        private Camera _mainCamera;
        private ParticleSystem _particleSystem;
        private Image[] _hudAccentImages;

        // === 설정값 ===
        [SerializeField] private float _transitionDuration = 0.5f;
        [SerializeField] private ParticleSystem _particleSystemPrefab;

        // === 이벤트 ===
        public UnityEvent<ThemeType> OnThemeChanged;

        // === 상태 ===
        private float _transitionTimer = 0f;
        private Color _startBgTop;
        private Color _startBgBottom;
        private bool _isTransitioning = false;

        private void Awake()
        {
            // 싱글톤 설정
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 컴포넌트 캐싱
            _mainCamera = Camera.main;
            _hudAccentImages = FindObjectsOfType<Image>();

            // 테마 데이터 초기화
            InitializeThemeDatas();

            // 저장된 테마 로드 또는 기본값 로드
            int savedThemeIndex = PlayerPrefs.GetInt("SelectedTheme", (int)ThemeType.Space);
            ThemeType savedTheme = (ThemeType)savedThemeIndex;

            // 저장된 테마가 유효한지 확인
            if (savedTheme >= ThemeType.Space && savedTheme <= ThemeType.Neon)
                SetTheme(savedTheme, immediate: true);
            else
                SetTheme(ThemeType.Space, immediate: true);
        }

        private void InitializeThemeDatas()
        {
            // Space (우주) 테마
            _themeDatas[(int)ThemeType.Space] = new ThemeData
            {
                themeType = ThemeType.Space,
                bgGradientTop = HexToColor("#060812"),
                bgGradientBottom = HexToColor("#0a1028"),
                accentColor = HexToColor("#ffd700"),
                particleColor = HexToColor("#ffd700"),
                particleType = ParticleType.Star
            };

            // Ocean (바다) 테마
            _themeDatas[(int)ThemeType.Ocean] = new ThemeData
            {
                themeType = ThemeType.Ocean,
                bgGradientTop = HexToColor("#041828"),
                bgGradientBottom = HexToColor("#082038"),
                accentColor = HexToColor("#40e0d0"),
                particleColor = HexToColor("#40e0d0"),
                particleType = ParticleType.Bubble
            };

            // Forest (숲) 테마
            _themeDatas[(int)ThemeType.Forest] = new ThemeData
            {
                themeType = ThemeType.Forest,
                bgGradientTop = HexToColor("#041808"),
                bgGradientBottom = HexToColor("#082810"),
                accentColor = HexToColor("#60e060"),
                particleColor = HexToColor("#60e060"),
                particleType = ParticleType.Leaf
            };

            // Neon (네온) 테마
            _themeDatas[(int)ThemeType.Neon] = new ThemeData
            {
                themeType = ThemeType.Neon,
                bgGradientTop = HexToColor("#0a0418"),
                bgGradientBottom = HexToColor("#180828"),
                accentColor = HexToColor("#ff40ff"),
                particleColor = HexToColor("#ff40ff"),
                particleType = ParticleType.Neon
            };
        }

        private void Update()
        {
            if (_isTransitioning)
            {
                _transitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_transitionTimer / _transitionDuration);

                // 배경 색상 보간
                Color lerpedTop = Color.Lerp(_startBgTop, _currentThemeData.bgGradientTop, t);
                Color lerpedBottom = Color.Lerp(_startBgBottom, _currentThemeData.bgGradientBottom, t);

                _mainCamera.backgroundColor = Color.Lerp(lerpedTop, lerpedBottom, 0.5f);

                if (t >= 1f)
                {
                    _isTransitioning = false;
                }
            }
        }

        /// <summary>
        /// 테마 변경 (부드러운 전환)
        /// </summary>
        public void SetTheme(ThemeType themeType, bool immediate = false)
        {
            if (themeType < ThemeType.Space || themeType > ThemeType.Neon)
                return;

            ThemeData newTheme = _themeDatas[(int)themeType];

            // 배경 색상 전환 시작
            _startBgTop = _mainCamera.backgroundColor;
            _startBgBottom = _mainCamera.backgroundColor;
            _currentThemeData = newTheme;

            if (immediate)
            {
                _mainCamera.backgroundColor = Color.Lerp(newTheme.bgGradientTop, newTheme.bgGradientBottom, 0.5f);
                _isTransitioning = false;
            }
            else
            {
                _transitionTimer = 0f;
                _isTransitioning = true;
            }

            // 파티클 시스템 업데이트
            UpdateParticleSystem(newTheme);

            // HUD 강조 색상 업데이트
            UpdateHudAccentColors(newTheme);

            // 별자리 표시 (우주 테마만)
            UpdateConstellation(themeType == ThemeType.Space);

            // PlayerPrefs에 저장
            PlayerPrefs.SetInt("SelectedTheme", (int)themeType);
            PlayerPrefs.Save();

            // 이벤트 발생
            OnThemeChanged?.Invoke(themeType);

            Debug.Log($"[ThemeManager] 테마 변경: {themeType}");
        }

        /// <summary>
        /// 파티클 시스템 업데이트
        /// </summary>
        private void UpdateParticleSystem(ThemeData themeData)
        {
            if (_particleSystem == null)
                return;

            var mainModule = _particleSystem.main;
            mainModule.startColor = themeData.particleColor;

            // 파티클 종류별 모양 조정 (실제 구현은 파티클 프리팹에 따라 달라짐)
            var shapeModule = _particleSystem.shape;

            switch (themeData.particleType)
            {
                case ParticleType.Star:
                    // 별 모양 (Sphere 사용, 스케일 조정)
                    shapeModule.shapeType = ParticleSystemShapeType.Sphere;
                    break;
                case ParticleType.Bubble:
                    // 거품 (원형, Circle 사용)
                    shapeModule.shapeType = ParticleSystemShapeType.Circle;
                    break;
                case ParticleType.Leaf:
                    // 잎 (폴 모션 추가됨)
                    shapeModule.shapeType = ParticleSystemShapeType.Cone;
                    break;
                case ParticleType.Neon:
                    // 네온 (랜덤 색상 애니메이션)
                    mainModule.startColor = new ParticleSystem.MinMaxGradient(
                        new Gradient() // 실제로는 Dynamic 애니메이션
                    );
                    break;
            }
        }

        /// <summary>
        /// HUD 강조 색상 업데이트
        /// </summary>
        private void UpdateHudAccentColors(ThemeData themeData)
        {
            // 태그로 HUD 요소 찾기
            var hudElements = FindObjectsOfType<Image>();
            foreach (var element in hudElements)
            {
                if (element.CompareTag("HUDAccent"))
                {
                    element.color = themeData.accentColor;
                }
            }

            // TextMeshPro 텍스트도 업데이트
            var textElements = FindObjectsOfType<TextMeshProUGUI>();
            foreach (var text in textElements)
            {
                if (text.CompareTag("HUDAccent"))
                {
                    text.color = themeData.accentColor;
                }
            }
        }

        /// <summary>
        /// 별자리 표시 (우주 테마에만 표시)
        /// </summary>
        private void UpdateConstellation(bool showConstellation)
        {
            var constellationObject = GameObject.FindWithTag("Constellation");
            if (constellationObject != null)
            {
                constellationObject.SetActive(showConstellation);
            }
        }

        /// <summary>
        /// 현재 테마 데이터 반환
        /// </summary>
        public ThemeData GetCurrentTheme()
        {
            return _currentThemeData;
        }

        /// <summary>
        /// 특정 테마 데이터 반환
        /// </summary>
        public ThemeData GetThemeData(ThemeType themeType)
        {
            if (themeType < ThemeType.Space || themeType > ThemeType.Neon)
                return _themeDatas[(int)ThemeType.Space];

            return _themeDatas[(int)themeType];
        }

        /// <summary>
        /// Hex 색상 코드를 Color로 변환
        /// </summary>
        private Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            return Color.white;
        }
    }
}

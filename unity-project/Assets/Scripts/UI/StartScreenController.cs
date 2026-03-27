using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PumpNumber.Core;
using PumpNumber.Effects;

namespace PumpNumber.UI
{
    /// <summary>
    /// 시작 화면 컨트롤러 — 스크린샷 디자인 재현
    ///
    /// [구조]
    /// StartPanel (CanvasGroup)
    ///   ├─ ConstellationBackground  ← ConstellationBackground.cs 부착
    ///   ├─ StarParticles            ← ParticleSystem (twinkle)
    ///   ├─ MeteorParticles          ← ParticleSystem (shooting star trail)
    ///   ├─ CharacterRow (HorizontalLayoutGroup)
    ///   │   ├─ Face_Red   (Image)
    ///   │   ├─ Face_Orange (Image)
    ///   │   ├─ Face_Green  (Image)
    ///   │   ├─ Face_Blue   (Image)
    ///   │   └─ Face_Purple (Image)
    ///   ├─ TitleText (TMP) "빼봐영"
    ///   ├─ GemCharacter (Image)     ← GemCharacter.cs 부착
    ///   └─ StartButton (Button)     "시작하기"
    ///
    /// [Unity 에디터에서 설정하는 방법]
    /// 1. Canvas 아래에 StartPanel 오브젝트 생성
    /// 2. CharacterRow에 HorizontalLayoutGroup (spacing: 8) 추가
    /// 3. 각 Face Image의 크기: 52x52, FilterMode: Point
    /// 4. TitleText: TMP, 폰트 DotGothic16, 크기 68, 색상 #f08020
    /// 5. StartButton: Image 색상 #f0a030, 라운드 코너 14px
    /// 6. ConstellationBackground: LineRenderer로 별자리 선 그림
    /// 7. StarParticles / MeteorParticles: Particle System 설정
    /// </summary>
    public class StartScreenController : MonoBehaviour
    {
        [Header("=== UI 요소 ===")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private TMP_Text titleText;            // "빼봐영"
        [SerializeField] private TMP_Text bestScoreText;        // "BEST SCORE 12,450"
        [SerializeField] private Button startButton;

        [Header("=== 화면 상단 버튼 ===")]
        [SerializeField] private Button collectionButton;       // "컬렉션" 버튼 (좌상단)
        [SerializeField] private Button rankingButton;          // "랭킹" 버튼 (우상단)

        [Header("=== 5색 캐릭터 (빨/주/초/파/보) ===")]
        [SerializeField] private Image[] characterImages;       // 5개 Image 컴포넌트

        [Header("=== 보석 캐릭터 ===")]
        [SerializeField] private Image gemImage;

        [Header("=== 배경 파티클 ===")]
        [SerializeField] private ParticleSystem starParticles;
        [SerializeField] private ParticleSystem meteorParticles;

        [Header("=== 캐릭터 애니메이션 ===")]
        [SerializeField] private float bobSpeed = 1.5f;
        [SerializeField] private float bobAmount = 4f;
        [SerializeField] private float gemFloatSpeed = 2.5f;
        [SerializeField] private float gemFloatAmount = 6f;

        // 5색 캐릭터 설정 (스크린샷 기준)
        private readonly CharacterSetup[] characters = new CharacterSetup[]
        {
            // 빨강: 혀 내밀기
            new CharacterSetup {
                bodyColor = new Color(240/255f, 90/255f, 90/255f),
                cheekColor = new Color(255/255f, 160/255f, 160/255f, 0.3f),
                eyeStyle = 0, mouthStyle = 0
            },
            // 주황: 윙크
            new CharacterSetup {
                bodyColor = new Color(250/255f, 180/255f, 50/255f),
                cheekColor = new Color(255/255f, 200/255f, 120/255f, 0.3f),
                eyeStyle = 1, mouthStyle = 1
            },
            // 초록: 미소
            new CharacterSetup {
                bodyColor = new Color(80/255f, 200/255f, 100/255f),
                cheekColor = new Color(140/255f, 255/255f, 160/255f, 0.3f),
                eyeStyle = 2, mouthStyle = 2
            },
            // 파랑: 웃는 눈
            new CharacterSetup {
                bodyColor = new Color(80/255f, 160/255f, 240/255f),
                cheekColor = new Color(140/255f, 200/255f, 255/255f, 0.3f),
                eyeStyle = 3, mouthStyle = 4
            },
            // 보라: 반짝눈
            new CharacterSetup {
                bodyColor = new Color(200/255f, 130/255f, 220/255f),
                cheekColor = new Color(230/255f, 180/255f, 240/255f, 0.3f),
                eyeStyle = 4, mouthStyle = 3
            },
        };

        private RectTransform[] charRects;
        private RectTransform gemRect;
        private float[] charBobOffsets;

        private void Start()
        {
            startButton.onClick.AddListener(OnStartClick);

            // 화면 상단 버튼 이벤트 연결
            if (collectionButton)
                collectionButton.onClick.AddListener(OnCollectionButtonClicked);
            if (rankingButton)
                rankingButton.onClick.AddListener(OnRankingButtonClicked);

            GenerateCharacterSprites();
            GenerateGemSprite();
            SetupBestScore();

            // 캐릭터 밥 오프셋 (시차 애니메이션)
            charRects = new RectTransform[characterImages.Length];
            charBobOffsets = new float[characterImages.Length];
            for (int i = 0; i < characterImages.Length; i++)
            {
                charRects[i] = characterImages[i].GetComponent<RectTransform>();
                charBobOffsets[i] = i * 0.15f; // 0, 0.15, 0.3, 0.45, 0.6
            }

            if (gemImage)
                gemRect = gemImage.GetComponent<RectTransform>();

            // GameManager 이벤트 연결
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStart.AddListener(Hide);
                GameManager.Instance.OnGameOver.AddListener(ShowAfterDelay);
            }

            Show();
        }

        private void Update()
        {
            if (!startPanel.activeSelf) return;

            // 캐릭터 위아래 통통 애니메이션
            for (int i = 0; i < charRects.Length; i++)
            {
                if (charRects[i] == null) continue;
                float y = Mathf.Sin((Time.time + charBobOffsets[i]) * bobSpeed * Mathf.PI * 2f / 1.5f) * bobAmount;
                charRects[i].anchoredPosition = new Vector2(
                    charRects[i].anchoredPosition.x, y
                );
            }

            // 보석 플로팅
            if (gemRect != null)
            {
                float y = Mathf.Sin(Time.time * gemFloatSpeed * Mathf.PI * 2f / 2.5f) * gemFloatAmount;
                gemRect.anchoredPosition = new Vector2(
                    gemRect.anchoredPosition.x, y
                );
            }
        }

        /// <summary>
        /// 5색 캐릭터 스프라이트 생성 (코드로 픽셀아트 생성)
        /// </summary>
        private void GenerateCharacterSprites()
        {
            for (int i = 0; i < characters.Length && i < characterImages.Length; i++)
            {
                var setup = characters[i];
                Texture2D tex = PixelCharacters.CreateColorFace(
                    setup.bodyColor, setup.cheekColor,
                    setup.eyeStyle, setup.mouthStyle
                );
                characterImages[i].sprite = PixelCharacters.TextureToSprite(tex);
            }
        }

        /// <summary>
        /// 보석 캐릭터 스프라이트 생성
        /// </summary>
        private void GenerateGemSprite()
        {
            if (gemImage == null) return;
            Texture2D tex = PixelCharacters.CreateGemCharacter();
            gemImage.sprite = PixelCharacters.TextureToSprite(tex);
        }

        private void SetupBestScore()
        {
            int best = PlayerPrefs.GetInt("pumpBest", 0);
            if (bestScoreText)
                bestScoreText.text = $"BEST SCORE {best:N0}";
        }

        private void OnStartClick()
        {
            GameManager.Instance?.StartGame();
        }

        /// <summary>
        /// 컬렉션 화면 열기
        /// </summary>
        private void OnCollectionButtonClicked()
        {
            // TODO: 컬렉션 화면 열기 (SceneManager.LoadScene 또는 CollectionUIController 활성화)
            Debug.Log("컬렉션 화면 열기");
        }

        /// <summary>
        /// 랭킹 화면 열기
        /// </summary>
        private void OnRankingButtonClicked()
        {
            // TODO: 랭킹 화면 열기 (SceneManager.LoadScene 또는 RankingUIController 활성화)
            Debug.Log("랭킹 화면 열기");
        }

        public void Show()
        {
            startPanel.SetActive(true);
            SetupBestScore();

            // 파티클 시작
            if (starParticles) starParticles.Play();
            if (meteorParticles) meteorParticles.Play();
        }

        public void Hide()
        {
            startPanel.SetActive(false);

            // 파티클 정지
            if (starParticles) starParticles.Stop();
            if (meteorParticles) meteorParticles.Stop();
        }

        private void ShowAfterDelay()
        {
            // 게임오버 후 재도전/홈으로 버튼에서 처리
        }

        /// <summary>
        /// 캐릭터 설정 구조체
        /// </summary>
        [System.Serializable]
        private struct CharacterSetup
        {
            public Color bodyColor;
            public Color cheekColor;
            public int eyeStyle;    // 0~4
            public int mouthStyle;  // 0~4
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace PumpNumber.Systems
{
    /// <summary>
    /// 수집품 관리 시스템
    /// 캐릭터 스킨, 업적, 칭호 관리
    /// </summary>
    public class CollectionManager : MonoBehaviour
    {
        #region 클래스 정의
        /// <summary>
        /// 캐릭터 스킨
        /// </summary>
        [System.Serializable]
        public class CharacterSkin
        {
            public string id;                       // 고유 ID (예: "red_character")
            public string name;                     // 이름 (예: "빨강이")
            public Color bodyColor;                 // 캐릭터 색상
            public Color cheekColor;                // 볼 색상
            public bool isSpecial;                  // 특수 스킨 여부
            public UnlockCondition unlockCondition; // 해금 조건
            public int unlockThreshold;             // 해금 수치 (콤보, 점수 등)
            public bool isUnlocked;                 // 해금 여부
        }

        /// <summary>
        /// 업적
        /// </summary>
        [System.Serializable]
        public class Achievement
        {
            public string id;                       // 고유 ID
            public Sprite icon;                     // 아이콘
            public string title;                    // 제목
            [TextArea(2, 3)]
            public string description;              // 설명
            public int currentProgress;             // 현재 진행도
            public int maxProgress;                 // 최대 진행도
            public string reward;                   // 보상 (예: "골드 +1000")
            public bool isCompleted;                // 완료 여부
        }

        /// <summary>
        /// 플레이어 칭호
        /// </summary>
        [System.Serializable]
        public class PlayerTitle
        {
            public string id;                       // 고유 ID
            public string name;                     // 칭호 이름
            public Color titleColor;                // 칭호 색상
            public UnlockCondition unlockCondition; // 해금 조건
            public int unlockThreshold;             // 해금 수치
            public bool isUnlocked;                 // 해금 여부
            public bool isEquipped;                 // 장착 여부
        }

        /// <summary>
        /// 해금 조건 열거형
        /// </summary>
        public enum UnlockCondition
        {
            None,               // 기본 (모두 해금)
            FeverCount,         // 피버 타임 횟수
            MaxCombo,           // 최대 콤보
            GeniusCount,        // 천재 모드 클리어 수
            TotalScore,         // 누적 점수
            SRankCount,         // S랭크 달성 수
            FirstClear,         // 첫 클리어
            NightPlay,          // 밤 시간대 플레이
            AllAchievements     // 모든 업적 달성
        }
        #endregion

        #region 싱글톤
        private static CollectionManager instance;

        public static CollectionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<CollectionManager>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("CollectionManager");
                        instance = obj.AddComponent<CollectionManager>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        #endregion

        #region 직렬화 필드
        [Header("=== 캐릭터 스킨 (10개) ===")]
        [SerializeField] private CharacterSkin[] characterSkins;

        [Header("=== 업적 (7개) ===")]
        [SerializeField] private Achievement[] achievements;

        [Header("=== 플레이어 칭호 (7개) ===")]
        [SerializeField] private PlayerTitle[] playerTitles;
        #endregion

        #region 이벤트
        public delegate void SkinUnlockedDelegate(CharacterSkin skin);
        public event SkinUnlockedDelegate OnSkinUnlocked;

        public delegate void AchievementCompletedDelegate(Achievement achievement);
        public event AchievementCompletedDelegate OnAchievementCompleted;

        public delegate void TitleUnlockedDelegate(PlayerTitle title);
        public event TitleUnlockedDelegate OnTitleUnlocked;
        #endregion

        #region 내부 상태
        private Dictionary<string, CharacterSkin> skinDict;
        private Dictionary<string, Achievement> achievementDict;
        private Dictionary<string, PlayerTitle> titleDict;
        #endregion

        private void Start()
        {
            InitializeCollections();
            LoadCollectionData();
        }

        #region 초기화
        /// <summary>
        /// 수집품 초기화 (기본값 설정)
        /// </summary>
        private void InitializeCollections()
        {
            InitializeCharacterSkins();
            InitializeAchievements();
            InitializePlayerTitles();

            // 딕셔너리 생성
            skinDict = characterSkins.ToDictionary(s => s.id);
            achievementDict = achievements.ToDictionary(a => a.id);
            titleDict = playerTitles.ToDictionary(t => t.id);
        }

        /// <summary>
        /// 캐릭터 스킨 기본값 설정 (10개)
        /// </summary>
        private void InitializeCharacterSkins()
        {
            if (characterSkins == null || characterSkins.Length == 0)
            {
                characterSkins = new CharacterSkin[10];

                // 1. 빨강이 (기본)
                characterSkins[0] = new CharacterSkin
                {
                    id = "red_character",
                    name = "빨강이",
                    bodyColor = new Color(240/255f, 90/255f, 90/255f),
                    cheekColor = new Color(255/255f, 160/255f, 160/255f),
                    isSpecial = false,
                    unlockCondition = UnlockCondition.None,
                    unlockThreshold = 0,
                    isUnlocked = true
                };

                // 2. 주황이 (기본)
                characterSkins[1] = new CharacterSkin
                {
                    id = "orange_character",
                    name = "주황이",
                    bodyColor = new Color(250/255f, 180/255f, 50/255f),
                    cheekColor = new Color(255/255f, 200/255f, 120/255f),
                    isSpecial = false,
                    unlockCondition = UnlockCondition.None,
                    unlockThreshold = 0,
                    isUnlocked = true
                };

                // 3. 초록이 (기본)
                characterSkins[2] = new CharacterSkin
                {
                    id = "green_character",
                    name = "초록이",
                    bodyColor = new Color(80/255f, 200/255f, 100/255f),
                    cheekColor = new Color(140/255f, 255/255f, 160/255f),
                    isSpecial = false,
                    unlockCondition = UnlockCondition.None,
                    unlockThreshold = 0,
                    isUnlocked = true
                };

                // 4. 파랑이 (기본)
                characterSkins[3] = new CharacterSkin
                {
                    id = "blue_character",
                    name = "파랑이",
                    bodyColor = new Color(80/255f, 160/255f, 240/255f),
                    cheekColor = new Color(140/255f, 200/255f, 255/255f),
                    isSpecial = false,
                    unlockCondition = UnlockCondition.None,
                    unlockThreshold = 0,
                    isUnlocked = true
                };

                // 5. 보라 (기본)
                characterSkins[4] = new CharacterSkin
                {
                    id = "purple_character",
                    name = "보라",
                    bodyColor = new Color(200/255f, 130/255f, 220/255f),
                    cheekColor = new Color(230/255f, 180/255f, 240/255f),
                    isSpecial = false,
                    unlockCondition = UnlockCondition.None,
                    unlockThreshold = 0,
                    isUnlocked = true
                };

                // 6. 로봇 (특수)
                characterSkins[5] = new CharacterSkin
                {
                    id = "robot_character",
                    name = "로봇",
                    bodyColor = new Color(150/255f, 150/255f, 150/255f),
                    cheekColor = new Color(200/255f, 200/255f, 200/255f),
                    isSpecial = true,
                    unlockCondition = UnlockCondition.MaxCombo,
                    unlockThreshold = 50,
                    isUnlocked = false
                };

                // 7. 유령 (특수)
                characterSkins[6] = new CharacterSkin
                {
                    id = "ghost_character",
                    name = "유령",
                    bodyColor = new Color(0.9f, 0.9f, 0.95f),
                    cheekColor = new Color(1f, 0.6f, 0.6f),
                    isSpecial = true,
                    unlockCondition = UnlockCondition.NightPlay,
                    unlockThreshold = 10,
                    isUnlocked = false
                };

                // 8. 왕관 (특수)
                characterSkins[7] = new CharacterSkin
                {
                    id = "king_character",
                    name = "왕관",
                    bodyColor = new Color(1f, 0.9f, 0.4f),
                    cheekColor = new Color(1f, 0.95f, 0.7f),
                    isSpecial = true,
                    unlockCondition = UnlockCondition.SRankCount,
                    unlockThreshold = 5,
                    isUnlocked = false
                };

                // 9. 무지개 (특수)
                characterSkins[8] = new CharacterSkin
                {
                    id = "rainbow_character",
                    name = "무지개",
                    bodyColor = new Color(1f, 0.4f, 0.9f),
                    cheekColor = new Color(1f, 0.7f, 0.95f),
                    isSpecial = true,
                    unlockCondition = UnlockCondition.AllAchievements,
                    unlockThreshold = 0,
                    isUnlocked = false
                };

                // 10. ??? (비밀)
                characterSkins[9] = new CharacterSkin
                {
                    id = "secret_character",
                    name = "???",
                    bodyColor = new Color(0.3f, 0.3f, 0.3f),
                    cheekColor = new Color(0.5f, 0.5f, 0.5f),
                    isSpecial = true,
                    unlockCondition = UnlockCondition.FeverCount,
                    unlockThreshold = 100,
                    isUnlocked = false
                };
            }
        }

        /// <summary>
        /// 업적 기본값 설정 (7개)
        /// </summary>
        private void InitializeAchievements()
        {
            if (achievements == null || achievements.Length == 0)
            {
                achievements = new Achievement[7];

                achievements[0] = new Achievement
                {
                    id = "first_clear",
                    title = "첫 도전",
                    description = "게임을 처음 완료했어요!",
                    currentProgress = 0,
                    maxProgress = 1,
                    reward = "골드 +500",
                    isCompleted = false
                };

                achievements[1] = new Achievement
                {
                    id = "combo_20",
                    title = "콤보 마스터",
                    description = "20개 이상의 콤보를 달성하세요",
                    currentProgress = 0,
                    maxProgress = 20,
                    reward = "골드 +1000",
                    isCompleted = false
                };

                achievements[2] = new Achievement
                {
                    id = "score_10000",
                    title = "점수 사냥꾼",
                    description = "누적 10,000점을 달성하세요",
                    currentProgress = 0,
                    maxProgress = 10000,
                    reward = "프리미엄 스킨 +1",
                    isCompleted = false
                };

                achievements[3] = new Achievement
                {
                    id = "fever_10",
                    title = "피버 찬양자",
                    description = "피버 타임을 10번 달성하세요",
                    currentProgress = 0,
                    maxProgress = 10,
                    reward = "칭호 +1",
                    isCompleted = false
                };

                achievements[4] = new Achievement
                {
                    id = "genius_mode",
                    title = "천재 모드",
                    description = "천재 모드를 완료하세요",
                    currentProgress = 0,
                    maxProgress = 1,
                    reward = "골드 +2000",
                    isCompleted = false
                };

                achievements[5] = new Achievement
                {
                    id = "s_rank_5",
                    title = "S랭크 달성",
                    description = "S랭크를 5번 달성하세요",
                    currentProgress = 0,
                    maxProgress = 5,
                    reward = "골드 +1500",
                    isCompleted = false
                };

                achievements[6] = new Achievement
                {
                    id = "all_skins",
                    title = "모든 스킨 수집",
                    description = "모든 캐릭터 스킨을 해금하세요",
                    currentProgress = 0,
                    maxProgress = 10,
                    reward = "특수 칭호 +1",
                    isCompleted = false
                };
            }
        }

        /// <summary>
        /// 플레이어 칭호 기본값 설정 (7개)
        /// </summary>
        private void InitializePlayerTitles()
        {
            if (playerTitles == null || playerTitles.Length == 0)
            {
                playerTitles = new PlayerTitle[7];

                playerTitles[0] = new PlayerTitle
                {
                    id = "newbie",
                    name = "초보자",
                    titleColor = Color.white,
                    unlockCondition = UnlockCondition.None,
                    unlockThreshold = 0,
                    isUnlocked = true,
                    isEquipped = true
                };

                playerTitles[1] = new PlayerTitle
                {
                    id = "combo_master",
                    name = "콤보 마스터",
                    titleColor = new Color(1f, 0.9f, 0.4f),
                    unlockCondition = UnlockCondition.MaxCombo,
                    unlockThreshold = 30,
                    isUnlocked = false,
                    isEquipped = false
                };

                playerTitles[2] = new PlayerTitle
                {
                    id = "fever_king",
                    name = "피버 킹",
                    titleColor = new Color(1f, 0.4f, 0.4f),
                    unlockCondition = UnlockCondition.FeverCount,
                    unlockThreshold = 20,
                    isUnlocked = false,
                    isEquipped = false
                };

                playerTitles[3] = new PlayerTitle
                {
                    id = "genius",
                    name = "천재",
                    titleColor = new Color(0.5f, 0.7f, 1f),
                    unlockCondition = UnlockCondition.GeniusCount,
                    unlockThreshold = 5,
                    isUnlocked = false,
                    isEquipped = false
                };

                playerTitles[4] = new PlayerTitle
                {
                    id = "score_hunter",
                    name = "점수 사냥꾼",
                    titleColor = new Color(1f, 0.4f, 0.9f),
                    unlockCondition = UnlockCondition.TotalScore,
                    unlockThreshold = 50000,
                    isUnlocked = false,
                    isEquipped = false
                };

                playerTitles[5] = new PlayerTitle
                {
                    id = "legend",
                    name = "전설의 플레이어",
                    titleColor = new Color(1f, 0.85f, 0.2f),
                    unlockCondition = UnlockCondition.SRankCount,
                    unlockThreshold = 10,
                    isUnlocked = false,
                    isEquipped = false
                };

                playerTitles[6] = new PlayerTitle
                {
                    id = "perfect",
                    name = "완벽한 수집가",
                    titleColor = new Color(0.5f, 1f, 0.5f),
                    unlockCondition = UnlockCondition.AllAchievements,
                    unlockThreshold = 0,
                    isUnlocked = false,
                    isEquipped = false
                };
            }
        }
        #endregion

        #region 해금 체크
        /// <summary>
        /// 모든 해금 조건 확인 및 업데이트
        /// </summary>
        public void CheckUnlocks(int feverCount = 0, int maxCombo = 0, int geniusCount = 0,
                                 int totalScore = 0, int sRankCount = 0)
        {
            // 스킨 해금 확인
            foreach (var skin in characterSkins)
            {
                if (skin.isUnlocked) continue;

                if (CheckUnlockCondition(skin.unlockCondition, skin.unlockThreshold,
                                         feverCount, maxCombo, geniusCount, totalScore, sRankCount))
                {
                    UnlockSkin(skin.id);
                }
            }

            // 칭호 해금 확인
            foreach (var title in playerTitles)
            {
                if (title.isUnlocked) continue;

                if (CheckUnlockCondition(title.unlockCondition, title.unlockThreshold,
                                         feverCount, maxCombo, geniusCount, totalScore, sRankCount))
                {
                    UnlockTitle(title.id);
                }
            }
        }

        /// <summary>
        /// 해금 조건 확인
        /// </summary>
        private bool CheckUnlockCondition(UnlockCondition condition, int threshold,
                                         int feverCount, int maxCombo, int geniusCount,
                                         int totalScore, int sRankCount)
        {
            switch (condition)
            {
                case UnlockCondition.None:
                    return true;
                case UnlockCondition.FeverCount:
                    return feverCount >= threshold;
                case UnlockCondition.MaxCombo:
                    return maxCombo >= threshold;
                case UnlockCondition.GeniusCount:
                    return geniusCount >= threshold;
                case UnlockCondition.TotalScore:
                    return totalScore >= threshold;
                case UnlockCondition.SRankCount:
                    return sRankCount >= threshold;
                case UnlockCondition.FirstClear:
                    return threshold == 0;
                case UnlockCondition.NightPlay:
                    return System.DateTime.Now.Hour >= 22 || System.DateTime.Now.Hour < 6;
                case UnlockCondition.AllAchievements:
                    return achievements.All(a => a.isCompleted);
                default:
                    return false;
            }
        }
        #endregion

        #region 스킨 관리
        /// <summary>
        /// 스킨 해금
        /// </summary>
        public void UnlockSkin(string skinId)
        {
            if (skinDict.TryGetValue(skinId, out var skin))
            {
                if (!skin.isUnlocked)
                {
                    skin.isUnlocked = true;
                    OnSkinUnlocked?.Invoke(skin);
                    SaveCollectionData();
                }
            }
        }

        /// <summary>
        /// 스킨 정보 반환
        /// </summary>
        public CharacterSkin GetSkin(string skinId)
        {
            skinDict.TryGetValue(skinId, out var skin);
            return skin;
        }

        /// <summary>
        /// 해금된 모든 스킨 반환
        /// </summary>
        public List<CharacterSkin> GetUnlockedSkins()
        {
            return characterSkins.Where(s => s.isUnlocked).ToList();
        }
        #endregion

        #region 칭호 관리
        /// <summary>
        /// 칭호 해금
        /// </summary>
        private void UnlockTitle(string titleId)
        {
            if (titleDict.TryGetValue(titleId, out var title))
            {
                if (!title.isUnlocked)
                {
                    title.isUnlocked = true;
                    OnTitleUnlocked?.Invoke(title);
                    SaveCollectionData();
                }
            }
        }

        /// <summary>
        /// 칭호 장착
        /// </summary>
        public void EquipTitle(string titleId)
        {
            if (!titleDict.TryGetValue(titleId, out var title) || !title.isUnlocked)
                return;

            // 기존 칭호 장착 해제
            foreach (var t in playerTitles)
            {
                t.isEquipped = false;
            }

            title.isEquipped = true;
            SaveCollectionData();
        }

        /// <summary>
        /// 장착된 칭호 반환
        /// </summary>
        public PlayerTitle GetEquippedTitle()
        {
            return playerTitles.FirstOrDefault(t => t.isEquipped);
        }
        #endregion

        #region 업적 관리
        /// <summary>
        /// 업적 진행도 업데이트
        /// </summary>
        public void UpdateAchievementProgress(string achievementId, int progress)
        {
            if (achievementDict.TryGetValue(achievementId, out var achievement))
            {
                achievement.currentProgress = Mathf.Min(progress, achievement.maxProgress);

                if (!achievement.isCompleted && achievement.currentProgress >= achievement.maxProgress)
                {
                    achievement.isCompleted = true;
                    OnAchievementCompleted?.Invoke(achievement);
                }

                SaveCollectionData();
            }
        }

        /// <summary>
        /// 업적 정보 반환
        /// </summary>
        public Achievement GetAchievement(string achievementId)
        {
            achievementDict.TryGetValue(achievementId, out var achievement);
            return achievement;
        }
        #endregion

        #region 수집품 진행도
        /// <summary>
        /// 전체 수집품 진행도 반환 (0-1)
        /// </summary>
        public float GetCollectionProgress()
        {
            int totalItems = characterSkins.Length + playerTitles.Length;
            int unlockedItems = characterSkins.Count(s => s.isUnlocked) +
                                playerTitles.Count(t => t.isUnlocked);

            return totalItems > 0 ? (float)unlockedItems / totalItems : 0f;
        }

        /// <summary>
        /// 스킨 해금률 반환 (0-1)
        /// </summary>
        public float GetSkinProgress()
        {
            int unlockedCount = characterSkins.Count(s => s.isUnlocked);
            return characterSkins.Length > 0 ? (float)unlockedCount / characterSkins.Length : 0f;
        }

        /// <summary>
        /// 업적 완료률 반환 (0-1)
        /// </summary>
        public float GetAchievementProgress()
        {
            int completedCount = achievements.Count(a => a.isCompleted);
            return achievements.Length > 0 ? (float)completedCount / achievements.Length : 0f;
        }
        #endregion

        #region 저장 및 로드
        /// <summary>
        /// 수집품 데이터 저장 (PlayerPrefs 사용)
        /// </summary>
        private void SaveCollectionData()
        {
            // 스킨 데이터 저장
            for (int i = 0; i < characterSkins.Length; i++)
            {
                PlayerPrefs.SetInt($"skin_{characterSkins[i].id}_unlocked",
                    characterSkins[i].isUnlocked ? 1 : 0);
            }

            // 칭호 데이터 저장
            for (int i = 0; i < playerTitles.Length; i++)
            {
                PlayerPrefs.SetInt($"title_{playerTitles[i].id}_unlocked",
                    playerTitles[i].isUnlocked ? 1 : 0);
                PlayerPrefs.SetInt($"title_{playerTitles[i].id}_equipped",
                    playerTitles[i].isEquipped ? 1 : 0);
            }

            // 업적 데이터 저장
            for (int i = 0; i < achievements.Length; i++)
            {
                PlayerPrefs.SetInt($"achievement_{achievements[i].id}_progress",
                    achievements[i].currentProgress);
                PlayerPrefs.SetInt($"achievement_{achievements[i].id}_completed",
                    achievements[i].isCompleted ? 1 : 0);
            }

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 수집품 데이터 로드 (PlayerPrefs 사용)
        /// </summary>
        private void LoadCollectionData()
        {
            // 스킨 데이터 로드
            foreach (var skin in characterSkins)
            {
                skin.isUnlocked = PlayerPrefs.GetInt($"skin_{skin.id}_unlocked",
                    skin.unlockCondition == UnlockCondition.None ? 1 : 0) == 1;
            }

            // 칭호 데이터 로드
            foreach (var title in playerTitles)
            {
                title.isUnlocked = PlayerPrefs.GetInt($"title_{title.id}_unlocked",
                    title.unlockCondition == UnlockCondition.None ? 1 : 0) == 1;
                title.isEquipped = PlayerPrefs.GetInt($"title_{title.id}_equipped", 0) == 1;
            }

            // 업적 데이터 로드
            foreach (var achievement in achievements)
            {
                achievement.currentProgress = PlayerPrefs.GetInt(
                    $"achievement_{achievement.id}_progress", 0);
                achievement.isCompleted = PlayerPrefs.GetInt(
                    $"achievement_{achievement.id}_completed", 0) == 1;
            }
        }
        #endregion
    }
}

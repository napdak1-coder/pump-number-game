using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PumpNumber.Systems
{
    /// <summary>
    /// 랭킹 관리 시스템
    /// 주간 랭킹, 게임 센터/플레이 게임즈 통합, 온/오프라인 지원
    /// </summary>
    public class RankingManager : MonoBehaviour
    {
        #region 클래스 정의
        /// <summary>
        /// 랭킹 엔트리
        /// </summary>
        [System.Serializable]
        public class RankEntry
        {
            public int rank;                        // 순위
            public string playerName;               // 플레이어 이름
            public string title;                    // 칭호
            public int score;                       // 점수
            public int maxCombo;                    // 최대 콤보
            public Color characterColor;            // 캐릭터 색상
            public int rankChange;                  // 순위 변화 (-3, -1, 0, +1, +3 등)
        }

        /// <summary>
        /// 랭킹 탭 열거형
        /// </summary>
        public enum RankingTab
        {
            All,                                    // 전체 랭킹
            Friends,                                // 친구 랭킹
            Regional                               // 지역 랭킹
        }
        #endregion

        #region 싱글톤
        private static RankingManager instance;

        public static RankingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<RankingManager>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("RankingManager");
                        instance = obj.AddComponent<RankingManager>();
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
        [Header("=== 랭킹 설정 ===")]
        [SerializeField] private int maxRankingEntries = 100;
        [SerializeField] private bool useLocalDataInEditor = true;
        [SerializeField] private bool enableDebugLog = false;

        [Header("=== 리셋 설정 ===")]
        [SerializeField] private DayOfWeek weeklyResetDay = DayOfWeek.Monday;
        [SerializeField] private int weeklyResetHour = 0;
        #endregion

        #region 내부 상태
        private List<RankEntry> allRankings;
        private List<RankEntry> friendsRankings;
        private List<RankEntry> regionalRankings;
        private RankEntry myRank;
        private DateTime lastResetTime;
        private bool isLoading = false;

        // 게임 센터/플레이 게임즈 상태
        private bool isGameCenterInitialized = false;
        private bool isPlayGamesInitialized = false;
        #endregion

        #region 이벤트
        public delegate void RankingUpdatedDelegate(RankingTab tab);
        public event RankingUpdatedDelegate OnRankingUpdated;

        public delegate void MyRankUpdatedDelegate(RankEntry newRank);
        public event MyRankUpdatedDelegate OnMyRankUpdated;
        #endregion

        private void Start()
        {
            InitializeRankings();
            InitializeGameServicesSafely();
            CheckWeeklyReset();
            LoadLeaderboard();
        }

        #region 초기화
        /// <summary>
        /// 랭킹 초기화
        /// </summary>
        private void InitializeRankings()
        {
            allRankings = new List<RankEntry>();
            friendsRankings = new List<RankEntry>();
            regionalRankings = new List<RankEntry>();

            // 오프라인 개발용 모의 데이터
            GenerateMockData();

            lastResetTime = LoadLastResetTime();
        }

        /// <summary>
        /// 게임 서비스 초기화 (안전하게)
        /// </summary>
        private void InitializeGameServicesSafely()
        {
#if UNITY_IOS
            InitializeGameCenter();
#elif UNITY_ANDROID
            InitializePlayGames();
#endif
        }

        /// <summary>
        /// 게임 센터 초기화 (iOS)
        /// </summary>
#if UNITY_IOS
        private void InitializeGameCenter()
        {
            // GameKit을 통한 초기화
            // 실제 구현은 GameKit 래퍼 필요
            try
            {
                // TODO: GameKit 초기화 코드
                // UnityEngine.iOS.GameKit.GKLocalPlayer.Authenticate(...)
                isGameCenterInitialized = true;
                Debug.Log("[RankingManager] Game Center initialized (iOS)");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RankingManager] Failed to initialize Game Center: {ex.Message}");
                isGameCenterInitialized = false;
            }
        }
#endif

        /// <summary>
        /// 플레이 게임즈 초기화 (Android)
        /// </summary>
#if UNITY_ANDROID
        private void InitializePlayGames()
        {
            // Google Play Games 초기화
            try
            {
                // TODO: Play Games Services 초기화 코드
                // PlayGamesPlatform.InitializeInstance(...)
                // PlayGamesPlatform.DebugLogEnabled = enableDebugLog;
                // PlayGamesPlatform.Activate();
                isPlayGamesInitialized = true;
                Debug.Log("[RankingManager] Google Play Games initialized (Android)");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RankingManager] Failed to initialize Play Games: {ex.Message}");
                isPlayGamesInitialized = false;
            }
        }
#endif
        #endregion

        #region 모의 데이터
        /// <summary>
        /// 개발/오프라인용 모의 데이터 생성
        /// </summary>
        private void GenerateMockData()
        {
            allRankings.Clear();

            // 상위 10명 플레이어 데이터
            string[] playerNames = new string[]
            {
                "콤보마스터",
                "점수헌터",
                "피버킹",
                "천재모드",
                "무적의전사",
                "별무리",
                "빛의기사",
                "시간여행자",
                "요정의숲",
                "달의궁전"
            };

            Color[] characterColors = new Color[]
            {
                new Color(240/255f, 90/255f, 90/255f),   // 빨강
                new Color(250/255f, 180/255f, 50/255f),  // 주황
                new Color(80/255f, 200/255f, 100/255f),  // 초록
                new Color(80/255f, 160/255f, 240/255f),  // 파랑
                new Color(200/255f, 130/255f, 220/255f), // 보라
                new Color(150/255f, 150/255f, 150/255f), // 로봇
                new Color(0.9f, 0.9f, 0.95f),             // 유령
                new Color(1f, 0.9f, 0.4f),                // 왕관
                new Color(1f, 0.4f, 0.9f),                // 무지개
                new Color(0.3f, 0.3f, 0.3f)               // ???
            };

            string[] titles = new string[]
            {
                "전설의 플레이어",
                "천재",
                "피버 킹",
                "콤보 마스터",
                "완벽한 수집가",
                "점수 사냥꾼",
                "초보자",
                "천재",
                "전설의 플레이어",
                "완벽한 수집가"
            };

            // 랭킹 데이터 생성
            for (int i = 0; i < 10; i++)
            {
                allRankings.Add(new RankEntry
                {
                    rank = i + 1,
                    playerName = playerNames[i],
                    title = titles[i],
                    score = 50000 - (i * 3000) - UnityEngine.Random.Range(0, 1000),
                    maxCombo = 50 - (i * 3) - UnityEngine.Random.Range(0, 5),
                    characterColor = characterColors[i],
                    rankChange = UnityEngine.Random.Range(-3, 4)
                });
            }

            // 친구 랭킹 (상위 5명만)
            friendsRankings.AddRange(allRankings.Take(5));

            // 지역 랭킹 (상위 10명 동일)
            regionalRankings.AddRange(allRankings);
        }
        #endregion

        #region 랭킹 조회
        /// <summary>
        /// 전체 주간 랭킹 반환
        /// </summary>
        public List<RankEntry> GetWeeklyRanking(RankingTab tab = RankingTab.All)
        {
            switch (tab)
            {
                case RankingTab.Friends:
                    return new List<RankEntry>(friendsRankings);
                case RankingTab.Regional:
                    return new List<RankEntry>(regionalRankings);
                default:
                    return new List<RankEntry>(allRankings);
            }
        }

        /// <summary>
        /// 내 랭킹 정보 반환
        /// </summary>
        public RankEntry GetMyRank()
        {
            if (myRank == null)
            {
                // 기본값: 아직 랭킹에 없음
                myRank = new RankEntry
                {
                    rank = -1,
                    playerName = PlayerPrefs.GetString("playerName", "플레이어"),
                    title = "초보자",
                    score = 0,
                    maxCombo = 0,
                    characterColor = Color.white,
                    rankChange = 0
                };
            }
            return myRank;
        }

        /// <summary>
        /// 특정 랭크의 플레이어 정보 반환
        /// </summary>
        public RankEntry GetRankByPosition(int rank, RankingTab tab = RankingTab.All)
        {
            List<RankEntry> rankings = GetWeeklyRanking(tab);
            if (rank > 0 && rank <= rankings.Count)
            {
                return rankings[rank - 1];
            }
            return null;
        }

        /// <summary>
        /// 다음 리셋까지 남은 시간
        /// </summary>
        public TimeSpan GetTimeUntilReset()
        {
            DateTime nextReset = GetNextResetTime();
            TimeSpan remaining = nextReset - DateTime.Now;

            if (remaining.TotalSeconds < 0)
                return TimeSpan.Zero;

            return remaining;
        }

        /// <summary>
        /// 다음 리셋 시간 계산
        /// </summary>
        private DateTime GetNextResetTime()
        {
            DateTime now = DateTime.Now;
            DateTime nextReset = now.Date.AddHours(weeklyResetHour);

            // 이번 주 리셋 요일 찾기
            int currentDayOfWeek = (int)now.DayOfWeek;
            int resetDayOfWeek = (int)weeklyResetDay;

            int daysUntilReset = (resetDayOfWeek - currentDayOfWeek + 7) % 7;

            if (daysUntilReset == 0 && now.Hour >= weeklyResetHour)
            {
                daysUntilReset = 7;
            }

            nextReset = now.Date.AddDays(daysUntilReset).AddHours(weeklyResetHour);
            return nextReset;
        }

        /// <summary>
        /// 주간 리셋 확인
        /// </summary>
        private void CheckWeeklyReset()
        {
            DateTime now = DateTime.Now;

            if (now >= GetNextResetTime())
            {
                ResetWeeklyRanking();
            }
        }

        /// <summary>
        /// 주간 리셋 실행
        /// </summary>
        private void ResetWeeklyRanking()
        {
            // 지난주 상위 플레이어들의 순위 변화 계산
            for (int i = 0; i < allRankings.Count; i++)
            {
                // 순위 변화 무작위 생성 (실제로는 이전 주의 순위와 비교)
                allRankings[i].rankChange = UnityEngine.Random.Range(-5, 6);
            }

            lastResetTime = DateTime.Now;
            SaveLastResetTime();

            if (enableDebugLog)
                Debug.Log("[RankingManager] Weekly ranking reset completed");
        }
        #endregion

        #region 점수 제출
        /// <summary>
        /// 게임 점수 제출
        /// </summary>
        public void SubmitScore(int score)
        {
            if (score <= 0) return;

            string playerName = PlayerPrefs.GetString("playerName", "플레이어");

            // 로컬 저장
            SaveLocalScore(playerName, score);

            // 게임 서비스로 제출 시도
#if UNITY_IOS
            if (isGameCenterInitialized)
            {
                SubmitScoreToGameCenter(score);
            }
#elif UNITY_ANDROID
            if (isPlayGamesInitialized)
            {
                SubmitScoreToPlayGames(score);
            }
#endif

            if (enableDebugLog)
                Debug.Log($"[RankingManager] Score submitted: {score}");
        }

        /// <summary>
        /// 로컬에 점수 저장
        /// </summary>
        private void SaveLocalScore(string playerName, int score)
        {
            // 최고 점수 업데이트
            int bestScore = PlayerPrefs.GetInt("pumpBest", 0);
            if (score > bestScore)
            {
                PlayerPrefs.SetInt("pumpBest", score);
            }

            // 누적 점수 업데이트
            int totalScore = PlayerPrefs.GetInt("totalScore", 0);
            PlayerPrefs.SetInt("totalScore", totalScore + score);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 게임 센터로 점수 제출 (iOS)
        /// </summary>
#if UNITY_IOS
        private void SubmitScoreToGameCenter(int score)
        {
            try
            {
                // TODO: 실제 Game Center Leaderboard ID 설정
                string leaderboardID = "com.pumpnumber.weekly_ranking";

                // TODO: GameKit을 통해 점수 제출
                // GKScore scoreReporter = new GKScore(leaderboardID);
                // scoreReporter.Value = score;
                // GKScore.ReportScores(new[] { scoreReporter }, ...);

                if (enableDebugLog)
                    Debug.Log($"[RankingManager] Score submitted to Game Center: {score}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RankingManager] Failed to submit score to Game Center: {ex.Message}");
            }
        }
#endif

        /// <summary>
        /// 플레이 게임즈로 점수 제출 (Android)
        /// </summary>
#if UNITY_ANDROID
        private void SubmitScoreToPlayGames(int score)
        {
            try
            {
                // TODO: 실제 Play Games Leaderboard ID 설정
                string leaderboardID = "CgkI6OXR5p0CBAIQDQ";  // 예시 ID

                // TODO: Play Games Services를 통해 점수 제출
                // PlayGamesPlatform.Instance.ReportScore(score, leaderboardID, ...)

                if (enableDebugLog)
                    Debug.Log($"[RankingManager] Score submitted to Play Games: {score}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RankingManager] Failed to submit score to Play Games: {ex.Message}");
            }
        }
#endif
        #endregion

        #region 리더보드 로드
        /// <summary>
        /// 리더보드 로드 코루틴
        /// </summary>
        public void LoadLeaderboard()
        {
            StartCoroutine(LoadLeaderboardCoroutine());
        }

        /// <summary>
        /// 리더보드 로드 코루틴 (내부)
        /// </summary>
        private IEnumerator LoadLeaderboardCoroutine()
        {
            if (isLoading) yield break;

            isLoading = true;

            // 네트워크 요청 시뮬레이션
            yield return new WaitForSeconds(0.5f);

#if UNITY_IOS
            if (isGameCenterInitialized)
            {
                // TODO: Game Center에서 리더보드 로드
                yield return LoadGameCenterLeaderboard();
            }
#elif UNITY_ANDROID
            if (isPlayGamesInitialized)
            {
                // TODO: Play Games에서 리더보드 로드
                yield return LoadPlayGamesLeaderboard();
            }
#endif

            // 모의 데이터 사용 (온라인 실패 시)
            if (allRankings.Count == 0)
            {
                GenerateMockData();
            }

            isLoading = false;
            OnRankingUpdated?.Invoke(RankingTab.All);

            if (enableDebugLog)
                Debug.Log("[RankingManager] Leaderboard loaded");
        }

        /// <summary>
        /// Game Center 리더보드 로드 (iOS)
        /// </summary>
#if UNITY_IOS
        private IEnumerator LoadGameCenterLeaderboard()
        {
            // TODO: GKLeaderboard.LoadLeaderboards(...)
            yield return null;
        }
#endif

        /// <summary>
        /// Play Games 리더보드 로드 (Android)
        /// </summary>
#if UNITY_ANDROID
        private IEnumerator LoadPlayGamesLeaderboard()
        {
            // TODO: PlayGamesPlatform.Instance.LoadMoreScores(...)
            yield return null;
        }
#endif
        #endregion

        #region 저장 및 로드
        /// <summary>
        /// 마지막 리셋 시간 저장
        /// </summary>
        private void SaveLastResetTime()
        {
            PlayerPrefs.SetString("lastRankingReset", DateTime.Now.Ticks.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 마지막 리셋 시간 로드
        /// </summary>
        private DateTime LoadLastResetTime()
        {
            string ticksStr = PlayerPrefs.GetString("lastRankingReset", "0");
            if (long.TryParse(ticksStr, out long ticks) && ticks > 0)
            {
                return new DateTime(ticks);
            }
            return DateTime.Now;
        }
        #endregion

        #region 유틸리티
        /// <summary>
        /// 랭킹이 로드 중인지 반환
        /// </summary>
        public bool IsLoading()
        {
            return isLoading;
        }

        /// <summary>
        /// 주간 랭킹 리프레시 (강제)
        /// </summary>
        public void RefreshRanking()
        {
            LoadLeaderboard();
        }

        /// <summary>
        /// 시간 형식 포맷팅 (남은 시간 표시용)
        /// </summary>
        public string FormatTimeUntilReset()
        {
            TimeSpan remaining = GetTimeUntilReset();

            if (remaining.TotalSeconds <= 0)
                return "곧 리셋됩니다";

            if (remaining.TotalHours < 1)
                return $"{remaining.Minutes}분 {remaining.Seconds}초";

            if (remaining.TotalDays < 1)
                return $"{remaining.Hours}시간 {remaining.Minutes}분";

            return $"{(int)remaining.TotalDays}일 {remaining.Hours}시간";
        }
        #endregion
    }
}

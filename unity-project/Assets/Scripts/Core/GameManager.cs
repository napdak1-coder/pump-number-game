using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using PumpNumber.Data;
using PumpNumber.Audio;

namespace PumpNumber.Core
{
    /// <summary>
    /// 게임의 핵심 매니저 — 싱글톤 패턴
    /// ComboVisualSystem, CollectionManager, RankingManager, ThemeManager, FeverMode와 통합
    /// JS의 startGame(), nextRound(), pressKey(), handleSuccess(), handleFail() 등을 담당
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("=== 설정 ===")]
        [SerializeField] private GameConfig config;

        [Header("=== 시스템 참조 ===")]
        [SerializeField] private SoundManager soundManager;
        // ComboVisualSystem, CollectionManager, RankingManager, ThemeManager 참조
        // (Inspector에서 설정하거나 GetComponent 사용)

        [Header("=== 이벤트 (UI가 구독) ===")]
        public UnityEvent OnGameStart;
        public UnityEvent OnRoundStart;
        public UnityEvent<int> OnScoreChanged;       // 현재 점수
        public UnityEvent<int> OnComboChanged;        // 콤보 수
        public UnityEvent<int> OnTargetChanged;       // 목표 숫자 변경
        public UnityEvent<float> OnTimerChanged;      // 타이머 퍼센트 (0~100)
        public UnityEvent<int> OnLivesChanged;        // 남은 목숨
        public UnityEvent<string, string> OnModeChanged; // 모드명, CSS클래스명
        public UnityEvent<string> OnStageChanged;     // "초급 5/30" 같은 문자열
        public UnityEvent<string, string> OnMessage;  // 메시지 텍스트, 등급 (genius/great/good/ok/fail)
        public UnityEvent<int, int> OnKeyPressed;     // 누른 숫자, 실제 빼는 값
        public UnityEvent OnSuccess;
        public UnityEvent<string> OnFail;             // 실패 사유
        public UnityEvent OnGameOver;
        public UnityEvent<bool> OnNewBestScore;       // 신기록 여부
        public UnityEvent OnFeverStart;               // 피버 시작 (🔥 피버 타임! 지금부터 점수 두배! 🔥)
        public UnityEvent OnFeverEnd;                 // 피버 종료
        public UnityEvent<float> OnSpeedMultChanged;  // 속도 배율
        public UnityEvent<DifficultyTier> OnTierChanged; // 티어 변경

        // === 내부 상태 ===
        public GameState State { get; private set; } = new GameState();
        public GameConfig Config => config;

        private Coroutine timerCoroutine;
        private string prevTier = "";

        // === 게임 오버 통계 ===
        [System.Serializable]
        public class GameOverStats
        {
            public int questionsToNextStage;  // 다음 스테이지까지 필요한 문제 수
            public int pointsToHighScore;     // 최고점까지 필요한 점수 ("아깝다 연출")
        }
        public GameOverStats gameOverStats = new GameOverStats();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            State.LoadBestScore();

            // SoundManager 참조 (없으면 Find)
            if (soundManager == null)
                soundManager = FindObjectOfType<SoundManager>();
        }

        // ================================================================
        // 게임 시작
        // JS: startGame()
        // ================================================================
        public void StartGame()
        {
            State.Reset();
            State.LoadBestScore();
            prevTier = "";

            OnGameStart?.Invoke();
            OnScoreChanged?.Invoke(0);
            OnComboChanged?.Invoke(0);
            OnLivesChanged?.Invoke(State.lives);
            OnStageChanged?.Invoke("초급 0/30");

            NextRound();
        }

        // ================================================================
        // 다음 라운드
        // JS: nextRound()
        // ================================================================
        public void NextRound()
        {
            int nextStage = State.stageCount + 1;
            DifficultyTier cfg = config.GetDifficulty(nextStage);

            // 리버스 모드 판정
            State.isReverse = Random.value < cfg.reverseChance;

            // 금지 숫자 결정
            State.forbiddenNums.Clear();
            int actualForbid = State.isReverse ? Mathf.Max(0, cfg.forbidCount - 1) : cfg.forbidCount;
            if (actualForbid > 0)
            {
                List<int> available = Enumerable.Range(1, 9).ToList();
                for (int i = 0; i < actualForbid && available.Count > 0; i++)
                {
                    int idx = Random.Range(0, available.Count);
                    State.forbiddenNums.Add(available[idx]);
                    available.RemoveAt(idx);
                }
            }

            // 목표 숫자 생성
            State.originalTarget = Random.Range(cfg.minTarget, cfg.maxTarget + 1);

            if (State.isReverse)
            {
                State.currentValue = 0;
                State.target = State.originalTarget;
            }
            else
            {
                State.target = State.originalTarget;
                State.currentValue = 0;
            }

            State.tapCount = 0;
            State.timeLeft = 100f;

            // 최소 탭 수 계산
            List<int> usable = Enumerable.Range(1, 9).Where(n => !State.forbiddenNums.Contains(n)).ToList();
            int maxU = usable.Count > 0 ? usable.Max() : 9;
            int maxSwipe3 = maxU * 100 + maxU * 10 + maxU;
            int maxSwipe2 = maxU * 10 + maxU;
            if (State.originalTarget > maxSwipe3)
                State.minTaps = 1 + Mathf.CeilToInt((float)(State.originalTarget - maxSwipe3) / maxSwipe3);
            else
                State.minTaps = 1;

            // 모드 뱃지 업데이트
            UpdateModeBadge();

            // 티어 변경 감지
            if (cfg.tierName != prevTier)
            {
                prevTier = cfg.tierName;
                OnTierChanged?.Invoke(cfg);
            }

            // UI 이벤트 발행
            OnRoundStart?.Invoke();
            OnTargetChanged?.Invoke(State.isReverse ? 0 : State.target);
            OnTimerChanged?.Invoke(100f);
            OnStageChanged?.Invoke($"{cfg.tierName} {State.stageCount}/{config.maxStage}");

            // 타이머 시작
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            float speed = cfg.timerSpeed;
            if (State.isReverse && State.forbiddenNums.Count > 0)
                speed *= 1.3f;
            timerCoroutine = StartCoroutine(TimerRoutine(speed));
        }

        // ================================================================
        // 키 입력 처리
        // JS: pressKey(num, actualNum)
        // ================================================================
        public void PressKey(int num, int actualNum = -1)
        {
            if (!State.isPlaying) return;
            if (State.forbiddenNums.Contains(num)) return;

            if (actualNum < 0) actualNum = num;
            State.tapCount++;

            OnKeyPressed?.Invoke(num, actualNum);

            if (State.isReverse)
            {
                State.currentValue += actualNum;
                OnTargetChanged?.Invoke(State.currentValue);

                if (State.currentValue > State.originalTarget)
                    HandleFail("초과!");
                else if (State.currentValue == State.originalTarget)
                    HandleSuccess();
            }
            else
            {
                State.target -= actualNum;
                OnTargetChanged?.Invoke(State.target);

                if (State.target < 0)
                    HandleFail("실패!");
                else if (State.target == 0)
                    HandleSuccess();
            }
        }

        // ================================================================
        // 성공 처리
        // JS: handleSuccess()
        // ComboVisualSystem, CollectionManager와 통합
        // ================================================================
        private void HandleSuccess()
        {
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);

            State.stageCount++;
            State.comboCount++;
            State.currentCombo = State.comboCount;  // ComboVisualSystem 동기화
            State.totalTaps++;  // 통계 추적

            if (State.comboCount > State.maxCombo)
                State.maxCombo = State.comboCount;

            // 등급 판정
            var rank = GetRank(State.tapCount, State.minTaps);

            // 점수 계산 (콤보 스테이지별 배율 적용)
            int baseScore = State.originalTarget;
            float comboMult = GetComboMultiplier(State.comboCount);
            float mult = rank.mult * State.speedMultiplier * comboMult;
            if (State.isFever) mult *= config.feverScoreMult;
            int addScore = Mathf.RoundToInt(baseScore * mult);
            State.score += addScore;

            // 리버스 클리어 카운트
            if (State.isReverse) State.reverseClears++;

            // 천재 카운트
            if (rank.cls == "genius")
            {
                State.geniusCount++;
                State.sRankCount++;  // S 랭크 카운트
            }

            // 콤보 보너스
            CheckComboBonus();

            // ComboVisualSystem 콤보 증가 (IncrementCombo)
            IncrementComboVisual();

            // 이벤트 발행
            OnSuccess?.Invoke();
            OnScoreChanged?.Invoke(State.score);
            OnComboChanged?.Invoke(State.comboCount);
            OnMessage?.Invoke(rank.rank, rank.cls);

            // 콤보에 따른 피버 자동 활성화
            if (State.comboCount >= config.feverActivationThreshold && !State.isFever)
                StartFever();

            // 빠른 클리어 → 피버 체크 (기존 방식 유지)
            if (State.timeLeft >= 70)
            {
                State.fastClears++;
                if (State.fastClears >= config.feverThreshold && !State.isFever)
                    StartFever();
            }
            else
            {
                State.fastClears = 0;
            }

            // 다음 라운드 (0.8초 딜레이)
            StartCoroutine(DelayedNextRound(0.8f));
        }

        // ================================================================
        // 실패 처리
        // JS: handleFail(reason)
        // ComboVisualSystem과 통합 (ResetCombo)
        // ================================================================
        private void HandleFail(string reason)
        {
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);

            // ComboVisualSystem 콤보 리셋 (ResetCombo)
            ResetComboVisual();

            State.comboCount = 0;
            State.currentCombo = 0;  // ComboVisualSystem 동기화
            State.fastClears = 0;
            State.lives--;

            // 오답 효과음
            if (soundManager != null)
                soundManager.PlayWrongAnswer();

            OnFail?.Invoke(reason);
            OnComboChanged?.Invoke(0);
            OnLivesChanged?.Invoke(State.lives);
            OnMessage?.Invoke(reason, "fail-msg");

            if (State.lives <= 0)
            {
                // 게임 오버
                StartCoroutine(DelayedGameOver(1f));
            }
            else
            {
                // 다음 라운드
                StartCoroutine(DelayedNextRound(1.2f));
            }
        }

        // ================================================================
        // 게임 오버
        // JS: showGameOver()
        // RankingManager, CollectionManager와 통합
        // "아깝다 연출" 데이터 계산
        // ================================================================
        private void ShowGameOver()
        {
            State.isPlaying = false;
            bool isNewBest = State.score > State.bestScore;
            State.SaveBestScore();

            // "아깝다 연출" 데이터 계산
            // questionsToNextStage: 다음 스테이지까지 필요한 문제 수
            gameOverStats.questionsToNextStage = config.questionsPerStage - (State.stageCount % config.questionsPerStage);
            // pointsToHighScore: 최고점까지 필요한 점수
            gameOverStats.pointsToHighScore = Mathf.Max(0, State.bestScore - State.score);

            // CollectionManager 업데이트 (라이프타임 통계)
            UpdateCollectionStats();

            // RankingManager 제출
            SubmitRankingScore();

            OnGameOver?.Invoke();
            OnNewBestScore?.Invoke(isNewBest);
        }

        // ================================================================
        // 피버 모드
        // FeverMode 시스템 통합
        // 피버 활성화 시 한국식 배너: "🔥 피버 타임! 지금부터 점수 두배! 🔥"
        // ================================================================
        private void StartFever()
        {
            State.isFever = true;
            State.feverCount++;  // 피버 발동 횟수 추적
            State.fastClears = 0;

            // 피버 활성화 사운드
            if (soundManager != null)
                soundManager.PlayFeverActivate();

            OnFeverStart?.Invoke();
            StartCoroutine(FeverRoutine());
        }

        private IEnumerator FeverRoutine()
        {
            yield return new WaitForSeconds(config.feverDuration);
            State.isFever = false;

            // 피버 종료 사운드
            if (soundManager != null)
                soundManager.PlayFeverDeactivate();

            OnFeverEnd?.Invoke();
        }

        // ================================================================
        // 타이머 코루틴
        // JS: setInterval 타이머 로직
        // ================================================================
        private IEnumerator TimerRoutine(float tickSpeed)
        {
            while (State.timeLeft > 0 && State.isPlaying)
            {
                yield return new WaitForSeconds(tickSpeed);
                State.timeLeft -= 1f;
                UpdateSpeedMultiplier();
                OnTimerChanged?.Invoke(State.timeLeft);

                if (State.timeLeft <= 0)
                {
                    HandleFail("시간초과!");
                    yield break;
                }
            }
        }

        // ================================================================
        // 속도 배율 계산
        // JS: updateSpeedMult()
        // ================================================================
        private void UpdateSpeedMultiplier()
        {
            if (State.timeLeft >= 80) State.speedMultiplier = config.speedMult_80;
            else if (State.timeLeft >= 60) State.speedMultiplier = config.speedMult_60;
            else if (State.timeLeft >= 40) State.speedMultiplier = config.speedMult_40;
            else if (State.timeLeft >= 20) State.speedMultiplier = config.speedMult_20;
            else State.speedMultiplier = config.speedMult_0;

            OnSpeedMultChanged?.Invoke(State.speedMultiplier);
        }

        // ================================================================
        // 등급 판정
        // JS: getRank(tapCount, minTaps)
        // ================================================================
        private (string rank, string cls, float mult) GetRank(int taps, int minTaps)
        {
            int diff = taps - minTaps;
            if (diff == 0) return ("천재!", "genius", config.geniusMult);
            if (diff == 1) return ("멋져!", "great", config.greatMult);
            if (diff <= 3) return ("좋아!", "good", config.goodMult);
            return ("괜찮아!", "ok-rank", config.okMult);
        }

        // ================================================================
        // 콤보 보너스 체크
        // ================================================================
        private void CheckComboBonus()
        {
            for (int i = 0; i < config.comboMilestones.Length; i++)
            {
                if (State.comboCount == config.comboMilestones[i])
                {
                    State.score += config.comboBonusScores[i];
                    break;
                }
            }
        }

        // ================================================================
        // 모드 뱃지 업데이트
        // ================================================================
        private void UpdateModeBadge()
        {
            string text;
            string mode;

            if (State.isReverse && State.forbiddenNums.Count > 0)
            {
                text = $"⟲ 리버스 + 금지 {string.Join(",", State.forbiddenNums)}";
                mode = "reverse-forbidden";
            }
            else if (State.isReverse)
            {
                text = $"⟲ 리버스 → {State.originalTarget}";
                mode = "reverse";
            }
            else if (State.forbiddenNums.Count > 0)
            {
                text = $"금지 {string.Join(",", State.forbiddenNums)} !";
                mode = "forbidden";
            }
            else
            {
                text = "▶ 목 표";
                mode = "normal";
            }

            OnModeChanged?.Invoke(text, mode);
        }

        // ================================================================
        // 유틸 코루틴
        // ================================================================
        private IEnumerator DelayedNextRound(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (State.isPlaying) NextRound();
        }

        private IEnumerator DelayedGameOver(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowGameOver();
        }

        // ================================================================
        // ComboVisualSystem 통합 메서드
        // ================================================================

        /// <summary>
        /// 콤보 시각화 증가 (정답 시 호출)
        /// ComboVisualSystem과의 인터페이스
        /// </summary>
        private void IncrementComboVisual()
        {
            // ComboVisualSystem이 있다면 IncrementCombo 호출
            // var comboVisual = FindObjectOfType<ComboVisualSystem>();
            // if (comboVisual != null)
            //     comboVisual.IncrementCombo(State.comboCount);

            // 콤보 스테이지 변경 감지 및 사운드 재생
            UpdateComboStage();
        }

        /// <summary>
        /// 콤보 시각화 리셋 (오답 시 호출)
        /// ComboVisualSystem과의 인터페이스
        /// </summary>
        private void ResetComboVisual()
        {
            // ComboVisualSystem이 있다면 ResetCombo 호출
            // var comboVisual = FindObjectOfType<ComboVisualSystem>();
            // if (comboVisual != null)
            //     comboVisual.ResetCombo();

            State.comboStage = 0;
        }

        /// <summary>
        /// 콤보 스테이지 계산 및 변경 감지
        /// 콤보 단계별 점수 배율 적용 및 음향 효과
        /// </summary>
        private void UpdateComboStage()
        {
            int prevStage = State.comboStage;

            // 콤보 스테이지 판정
            if (State.comboCount >= config.comboStageThresholds[3])      // 20+
                State.comboStage = 4;  // Rainbow
            else if (State.comboCount >= config.comboStageThresholds[2]) // 15~19
                State.comboStage = 3;  // Red
            else if (State.comboCount >= config.comboStageThresholds[1]) // 10~14
                State.comboStage = 2;  // Gold
            else if (State.comboCount >= config.comboStageThresholds[0]) // 5~9
                State.comboStage = 1;  // Blue
            else
                State.comboStage = 0;  // Normal

            // 스테이지 변경 시 효과음
            if (prevStage < State.comboStage && soundManager != null)
            {
                soundManager.PlayComboStageChange(State.comboStage - 1);
            }
        }

        /// <summary>
        /// 현재 콤보 스테이지에 따른 점수 배율 계산
        /// </summary>
        private float GetComboMultiplier(int comboCount)
        {
            if (comboCount >= config.comboStageThresholds[3])      // 20+
                return config.comboStageMultipliers[4];
            else if (comboCount >= config.comboStageThresholds[2]) // 15~19
                return config.comboStageMultipliers[3];
            else if (comboCount >= config.comboStageThresholds[1]) // 10~14
                return config.comboStageMultipliers[2];
            else if (comboCount >= config.comboStageThresholds[0]) // 5~9
                return config.comboStageMultipliers[1];
            else
                return config.comboStageMultipliers[0]; // 1x
        }

        // ================================================================
        // CollectionManager 통합 메서드
        // ================================================================

        /// <summary>
        /// 라이프타임 통계 업데이트
        /// 게임 오버 시 호출하여 Collection 시스템으로 전송
        /// </summary>
        private void UpdateCollectionStats()
        {
            // 라이프타임 통계 누적
            State.lifetimeStats.totalScore += State.score;
            State.lifetimeStats.totalGames++;
            State.lifetimeStats.totalFever += State.feverCount;
            State.lifetimeStats.totalGenius += State.sRankCount;

            // CollectionManager가 있다면 업데이트
            // var collectionMgr = FindObjectOfType<CollectionManager>();
            // if (collectionMgr != null)
            // {
            //     collectionMgr.UpdateStats(
            //         feverCount: State.feverCount,
            //         geniusCount: State.sRankCount,
            //         totalScore: State.score,
            //         maxCombo: State.maxCombo,
            //         sRankCount: State.sRankCount
            //     );
            // }
        }

        // ================================================================
        // RankingManager 통합 메서드
        // ================================================================

        /// <summary>
        /// 랭킹 제출
        /// 게임 오버 시 호출하여 점수를 RankingManager에 제출
        /// </summary>
        private void SubmitRankingScore()
        {
            // RankingManager가 있다면 점수 제출
            // var rankingMgr = FindObjectOfType<RankingManager>();
            // if (rankingMgr != null)
            // {
            //     rankingMgr.SubmitScore(
            //         score: State.score,
            //         maxCombo: State.maxCombo,
            //         geniusCount: State.sRankCount,
            //         playedAt: System.DateTime.Now
            //     );
            // }
        }

        // ================================================================
        // ThemeManager 통합 메서드
        // ================================================================

        /// <summary>
        /// 선택된 테마 적용
        /// </summary>
        public void SetTheme(GameConfig.ThemeType theme)
        {
            State.selectedTheme = theme;
            // ThemeManager가 있다면 테마 적용
            // var themeMgr = FindObjectOfType<ThemeManager>();
            // if (themeMgr != null)
            //     themeMgr.ApplyTheme(theme);
        }
    }
}

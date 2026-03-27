using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using PumpNumber.Data;

namespace PumpNumber.Core
{
    /// <summary>
    /// 게임의 핵심 매니저 — 싱글톤 패턴
    /// JS의 startGame(), nextRound(), pressKey(), handleSuccess(), handleFail() 등을 담당
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("=== 설정 ===")]
        [SerializeField] private GameConfig config;

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
        public UnityEvent OnFeverStart;
        public UnityEvent OnFeverEnd;
        public UnityEvent<float> OnSpeedMultChanged;  // 속도 배율
        public UnityEvent<DifficultyTier> OnTierChanged; // 티어 변경

        // === 내부 상태 ===
        public GameState State { get; private set; } = new GameState();
        public GameConfig Config => config;

        private Coroutine timerCoroutine;
        private string prevTier = "";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            State.LoadBestScore();
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
        // ================================================================
        private void HandleSuccess()
        {
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);

            State.stageCount++;
            State.comboCount++;
            if (State.comboCount > State.maxCombo)
                State.maxCombo = State.comboCount;

            // 등급 판정
            var rank = GetRank(State.tapCount, State.minTaps);

            // 점수 계산
            int baseScore = State.originalTarget;
            float mult = rank.mult * State.speedMultiplier;
            if (State.isFever) mult *= config.feverScoreMult;
            int addScore = Mathf.RoundToInt(baseScore * mult);
            State.score += addScore;

            // 리버스 클리어 카운트
            if (State.isReverse) State.reverseClears++;

            // 천재 카운트
            if (rank.cls == "genius") State.geniusCount++;

            // 콤보 보너스
            CheckComboBonus();

            // 이벤트 발행
            OnSuccess?.Invoke();
            OnScoreChanged?.Invoke(State.score);
            OnComboChanged?.Invoke(State.comboCount);
            OnMessage?.Invoke(rank.rank, rank.cls);

            // 빠른 클리어 → 피버 체크
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
        // ================================================================
        private void HandleFail(string reason)
        {
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);

            State.comboCount = 0;
            State.fastClears = 0;
            State.lives--;

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
        // ================================================================
        private void ShowGameOver()
        {
            State.isPlaying = false;
            bool isNewBest = State.score > State.bestScore;
            State.SaveBestScore();

            OnGameOver?.Invoke();
            OnNewBestScore?.Invoke(isNewBest);
        }

        // ================================================================
        // 피버 모드
        // ================================================================
        private void StartFever()
        {
            State.isFever = true;
            State.fastClears = 0;
            OnFeverStart?.Invoke();
            StartCoroutine(FeverRoutine());
        }

        private IEnumerator FeverRoutine()
        {
            yield return new WaitForSeconds(config.feverDuration);
            State.isFever = false;
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
    }
}

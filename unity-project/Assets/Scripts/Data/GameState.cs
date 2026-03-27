using System;
using System.Collections.Generic;
using UnityEngine;

namespace PumpNumber.Data
{
    /// <summary>
    /// 게임 상태를 저장하는 클래스
    /// JS의 전역 변수들 (score, comboCount, lives 등)을 구조화한 것
    /// </summary>
    [Serializable]
    public class GameState
    {
        // === 점수 ===
        public int score;
        public int bestScore;
        public int comboCount;
        public int maxCombo;

        // === 진행 ===
        public int stageCount;
        public int currentLevel;
        public int lives;

        // === 현재 라운드 ===
        public int target;           // 남은 목표 숫자 (일반모드)
        public int originalTarget;   // 원래 목표 숫자
        public int currentValue;     // 현재 누적값 (리버스모드)
        public int tapCount;
        public int minTaps;

        // === 모드 ===
        public bool isReverse;
        public List<int> forbiddenNums = new List<int>();

        // === 타이머 ===
        public float timeLeft;       // 0~100 (퍼센트)
        public float speedMultiplier;

        // === 통계 ===
        public int geniusCount;
        public int reverseClears;

        // === 상태 플래그 ===
        public bool isPlaying;
        public bool isFever;
        public int fastClears;

        // === 테마 ===
        public string currentTheme = "peach"; // "dark", "lavender", "peach"

        /// <summary>
        /// 새 게임 시작 시 상태 초기화
        /// JS의 startGame() 내 초기화 로직과 동일
        /// </summary>
        public void Reset()
        {
            score = 0;
            comboCount = 0;
            maxCombo = 0;
            currentLevel = 1;
            stageCount = 0;
            lives = 3;
            geniusCount = 0;
            reverseClears = 0;
            tapCount = 0;
            fastClears = 0;
            isFever = false;
            isPlaying = true;
            speedMultiplier = 1f;
            forbiddenNums.Clear();
        }

        /// <summary>
        /// 최고 기록 로드/저장
        /// JS의 localStorage.getItem('pumpBest') 대체
        /// </summary>
        public void LoadBestScore()
        {
            bestScore = PlayerPrefs.GetInt("pumpBest", 0);
        }

        public void SaveBestScore()
        {
            if (score > bestScore)
            {
                bestScore = score;
                PlayerPrefs.SetInt("pumpBest", bestScore);
                PlayerPrefs.Save();
            }
        }
    }
}

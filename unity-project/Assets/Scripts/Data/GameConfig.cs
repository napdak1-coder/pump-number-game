using UnityEngine;

namespace PumpNumber.Data
{
    /// <summary>
    /// 난이도 티어 정보 — 스테이지별 설정값을 담는 구조체
    /// JS의 getDifficulty() 함수를 C#으로 변환한 것
    /// </summary>
    [System.Serializable]
    public struct DifficultyTier
    {
        public string tierName;   // "초급", "중급", "고급", "∞ 고급+"
        public Color tierColor;
        public int minTarget;
        public int maxTarget;
        public float timerSpeed;  // 타이머 틱 간격 (ms → 초 변환)
        public int forbidCount;   // 금지 숫자 개수
        public float reverseChance; // 리버스 모드 확률 (0~1)
    }

    /// <summary>
    /// 게임 전체 설정 — ScriptableObject로 에디터에서 조절 가능
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "PumpNumber/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== 기본 설정 ===")]
        public int maxLives = 3;
        public int maxStage = 30;
        public float baseTimerDuration = 10f; // 기본 타이머 (초)

        [Header("=== 점수 배율 ===")]
        public float geniusMult = 3f;   // 최소 탭 정확히 맞춤
        public float greatMult = 2f;    // 최소 탭 + 1
        public float goodMult = 1.2f;   // 최소 탭 + 2~3
        public float okMult = 1f;       // 나머지

        [Header("=== 속도 배율 (시간 남은 비율 기준) ===")]
        public float speedMult_80 = 3f;   // 80% 이상
        public float speedMult_60 = 2.5f; // 60~80%
        public float speedMult_40 = 2f;   // 40~60%
        public float speedMult_20 = 1.5f; // 20~40%
        public float speedMult_0  = 1f;   // 20% 미만

        [Header("=== 피버 모드 ===")]
        public int feverThreshold = 5; // 연속 빠른 클리어 횟수
        public float feverDuration = 8f;
        public float feverScoreMult = 2f;

        [Header("=== 콤보 보너스 ===")]
        public int[] comboMilestones = { 5, 10, 15, 20, 30, 50 };
        public int[] comboBonusScores = { 100, 250, 500, 800, 1200, 2000 };

        /// <summary>
        /// 스테이지 번호에 따른 난이도 반환
        /// JS의 getDifficulty(stage)와 1:1 대응
        /// </summary>
        public DifficultyTier GetDifficulty(int stage)
        {
            DifficultyTier tier = new DifficultyTier();

            // 초급 1~8: 워밍업
            if (stage <= 2)
            {
                tier.tierName = "초급"; tier.tierColor = HexColor("#44ffaa");
                tier.minTarget = 5; tier.maxTarget = 15;
                tier.timerSpeed = 0.052f; tier.forbidCount = 0; tier.reverseChance = 0f;
            }
            else if (stage <= 5)
            {
                tier.tierName = "초급"; tier.tierColor = HexColor("#44ffaa");
                tier.minTarget = 10; tier.maxTarget = 40;
                tier.timerSpeed = 0.048f; tier.forbidCount = 0; tier.reverseChance = 0f;
            }
            else if (stage <= 8)
            {
                tier.tierName = "초급"; tier.tierColor = HexColor("#44ffaa");
                tier.minTarget = 25; tier.maxTarget = 70;
                tier.timerSpeed = 0.044f; tier.forbidCount = 0; tier.reverseChance = 0f;
            }
            // 중급 9~18: 리버스/금지 개별 등장
            else if (stage <= 11)
            {
                tier.tierName = "중급"; tier.tierColor = HexColor("#ffd700");
                tier.minTarget = 40; tier.maxTarget = 99;
                tier.timerSpeed = 0.040f; tier.forbidCount = 0; tier.reverseChance = 0.1f;
            }
            else if (stage <= 14)
            {
                tier.tierName = "중급"; tier.tierColor = HexColor("#ffd700");
                tier.minTarget = 60; tier.maxTarget = 130;
                tier.timerSpeed = 0.035f; tier.forbidCount = 1; tier.reverseChance = 0.12f;
            }
            else if (stage <= 18)
            {
                tier.tierName = "중급"; tier.tierColor = HexColor("#ffd700");
                tier.minTarget = 80; tier.maxTarget = 180;
                tier.timerSpeed = 0.030f; tier.forbidCount = 1; tier.reverseChance = 0.18f;
            }
            // 고급 19~30: 세자리 + 동시 발동
            else if (stage <= 22)
            {
                tier.tierName = "고급"; tier.tierColor = HexColor("#ff44aa");
                tier.minTarget = 150; tier.maxTarget = 300;
                tier.timerSpeed = 0.026f; tier.forbidCount = 1; tier.reverseChance = 0.2f;
            }
            else if (stage <= 26)
            {
                tier.tierName = "고급"; tier.tierColor = HexColor("#ff44aa");
                tier.minTarget = 250; tier.maxTarget = 450;
                tier.timerSpeed = 0.022f; tier.forbidCount = 1; tier.reverseChance = 0.25f;
            }
            else if (stage <= 30)
            {
                tier.tierName = "고급"; tier.tierColor = HexColor("#ff44aa");
                tier.minTarget = 350; tier.maxTarget = 600;
                tier.timerSpeed = 0.020f; tier.forbidCount = 2; tier.reverseChance = 0.28f;
            }
            // 무한 31+
            else
            {
                int s = stage - 30;
                tier.tierName = "∞ 고급+"; tier.tierColor = HexColor("#ff2244");
                tier.minTarget = Mathf.Min(600 + s * 12, 900);
                tier.maxTarget = Mathf.Min(700 + s * 15, 999);
                tier.timerSpeed = Mathf.Max(0.014f, 0.020f - s * 0.0007f);
                tier.forbidCount = Mathf.Min(3, 2 + s / 5);
                tier.reverseChance = Mathf.Min(0.35f, 0.28f + s * 0.008f);
            }

            return tier;
        }

        private Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }
}

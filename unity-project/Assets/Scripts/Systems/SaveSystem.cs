using UnityEngine;
using System;
using System.Collections.Generic;
using PumpNumber.Data;

namespace PumpNumber.Systems
{
    /// <summary>
    /// 저장 시스템 — PlayerPrefs 기반 중앙집중식 데이터 관리
    /// JSON 직렬화를 사용한 구조화된 저장
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        /// <summary>
        /// 모든 저장 데이터를 담는 클래스
        /// </summary>
        [System.Serializable]
        public class SaveData
        {
            // === 점수 데이터 ===
            public int highScore = 0;                    // 최고점
            public long totalScore = 0;                  // 누적 점수
            public int maxComboEver = 0;                 // 최대 콤보

            // === 게임 통계 ===
            public int totalGames = 0;                   // 총 게임 횟수
            public int totalFeverCount = 0;              // 총 피버 활성화 횟수
            public int totalGeniusCount = 0;             // 총 Genius 판정 수
            public int totalSRankCount = 0;              // 총 S-Rank 달성 수

            // === 진행도 ===
            public bool firstClearDone = false;          // 첫 클리어 완료
            public bool nightPlayDone = false;           // 야간 플레이 완료

            // === 테마 & 스킨 ===
            public int selectedTheme = 0;                // 선택된 테마 (ThemeType 열거형)
            public string selectedSkinId = "default";    // 선택된 스킨 ID
            public string equippedTitleId = "";          // 장착한 칭호 ID

            // === 언락된 아이템 ===
            public List<string> unlockedSkinIds = new List<string>();       // 언락된 스킨 ID 목록
            public List<string> unlockedTitleIds = new List<string>();      // 언락된 칭호 ID 목록
            public List<string> completedAchievementIds = new List<string>(); // 완료한 도전과제 ID

            // === 주간 데이터 ===
            public int weeklyBestScore = 0;              // 이번주 최고점
            public string weeklyResetDate = "";          // 주간 리셋 날짜 (ISO 8601)
        }

        // === 싱글톤 ===
        private static SaveSystem _instance;
        public static SaveSystem Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<SaveSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveSystem");
                    _instance = go.AddComponent<SaveSystem>();
                }
                return _instance;
            }
        }

        // === 저장 데이터 ===
        private SaveData _saveData;
        private const string SAVE_KEY = "PumpNumberSaveData";

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

            // 저장 데이터 로드
            Load();
        }

        /// <summary>
        /// 저장 데이터를 PlayerPrefs에 저장
        /// JSON 직렬화 사용
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_saveData, prettyPrint: true);
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();

                Debug.Log("[SaveSystem] 게임 데이터가 저장되었습니다.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 저장 실패: {e.Message}");
            }
        }

        /// <summary>
        /// PlayerPrefs에서 저장 데이터를 로드
        /// 저장된 데이터가 없으면 기본값으로 초기화
        /// </summary>
        public void Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    _saveData = JsonUtility.FromJson<SaveData>(json);

                    // 리스트 재초기화 (JsonUtility가 리스트를 완벽히 처리하지 못할 수 있음)
                    if (_saveData.unlockedSkinIds == null)
                        _saveData.unlockedSkinIds = new List<string>();
                    if (_saveData.unlockedTitleIds == null)
                        _saveData.unlockedTitleIds = new List<string>();
                    if (_saveData.completedAchievementIds == null)
                        _saveData.completedAchievementIds = new List<string>();

                    Debug.Log("[SaveSystem] 저장된 게임 데이터를 로드했습니다.");
                }
                else
                {
                    // 새로운 저장 데이터 생성
                    _saveData = new SaveData();
                    _saveData.selectedTheme = (int)ThemeType.Space;
                    _saveData.weeklyResetDate = DateTime.Now.ToString("o");

                    Debug.Log("[SaveSystem] 새로운 저장 데이터를 생성했습니다.");
                }

                // 주간 리셋 체크
                ResetWeeklyIfNeeded();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 로드 실패: {e.Message}");
                _saveData = new SaveData();
            }
        }

        /// <summary>
        /// 현재 저장 데이터 반환
        /// </summary>
        public SaveData GetSaveData()
        {
            return _saveData;
        }

        /// <summary>
        /// 최고점 업데이트
        /// 새로운 최고점이면 true 반환
        /// </summary>
        public bool UpdateHighScore(int score)
        {
            bool isNewBest = score > _saveData.highScore;

            if (isNewBest)
            {
                _saveData.highScore = score;
                Debug.Log($"[SaveSystem] 새로운 최고점! {score}점");
            }

            _saveData.totalScore += score;
            _saveData.totalGames++;

            // 주간 최고점 업데이트
            if (score > _saveData.weeklyBestScore)
            {
                _saveData.weeklyBestScore = score;
            }

            Save();
            return isNewBest;
        }

        /// <summary>
        /// 게임 통계 업데이트
        /// </summary>
        public void UpdateStats(int combo, int geniusCount, int feverCount, bool sRank)
        {
            // 최대 콤보 업데이트
            if (combo > _saveData.maxComboEver)
            {
                _saveData.maxComboEver = combo;
                Debug.Log($"[SaveSystem] 새로운 최대 콤보! {combo}");
            }

            // 통계 누적
            _saveData.totalGeniusCount += geniusCount;
            _saveData.totalFeverCount += feverCount;

            if (sRank)
            {
                _saveData.totalSRankCount++;
                Debug.Log("[SaveSystem] S-Rank 달성!");
            }

            Save();
        }

        /// <summary>
        /// 선택된 테마 저장
        /// </summary>
        public void SetSelectedTheme(ThemeType themeType)
        {
            _saveData.selectedTheme = (int)themeType;
            Save();
            Debug.Log($"[SaveSystem] 선택된 테마: {themeType}");
        }

        /// <summary>
        /// 선택된 스킨 저장
        /// </summary>
        public void SetSelectedSkin(string skinId)
        {
            _saveData.selectedSkinId = skinId;
            Save();
            Debug.Log($"[SaveSystem] 선택된 스킨: {skinId}");
        }

        /// <summary>
        /// 스킨 언락
        /// </summary>
        public void UnlockSkin(string skinId)
        {
            if (!_saveData.unlockedSkinIds.Contains(skinId))
            {
                _saveData.unlockedSkinIds.Add(skinId);
                Save();
                Debug.Log($"[SaveSystem] 스킨 언락: {skinId}");
            }
        }

        /// <summary>
        /// 칭호 언락
        /// </summary>
        public void UnlockTitle(string titleId)
        {
            if (!_saveData.unlockedTitleIds.Contains(titleId))
            {
                _saveData.unlockedTitleIds.Add(titleId);
                Save();
                Debug.Log($"[SaveSystem] 칭호 언락: {titleId}");
            }
        }

        /// <summary>
        /// 칭호 장착
        /// </summary>
        public void EquipTitle(string titleId)
        {
            if (_saveData.unlockedTitleIds.Contains(titleId))
            {
                _saveData.equippedTitleId = titleId;
                Save();
                Debug.Log($"[SaveSystem] 칭호 장착: {titleId}");
            }
            else
            {
                Debug.LogWarning($"[SaveSystem] 미보유 칭호입니다: {titleId}");
            }
        }

        /// <summary>
        /// 도전과제 완료
        /// </summary>
        public void CompleteAchievement(string achievementId)
        {
            if (!_saveData.completedAchievementIds.Contains(achievementId))
            {
                _saveData.completedAchievementIds.Add(achievementId);
                Save();
                Debug.Log($"[SaveSystem] 도전과제 완료: {achievementId}");
            }
        }

        /// <summary>
        /// 첫 클리어 표시
        /// </summary>
        public void MarkFirstClear()
        {
            if (!_saveData.firstClearDone)
            {
                _saveData.firstClearDone = true;
                Save();
                Debug.Log("[SaveSystem] 첫 클리어 완료!");
            }
        }

        /// <summary>
        /// 야간 플레이 표시
        /// </summary>
        public void MarkNightPlay()
        {
            if (!_saveData.nightPlayDone)
            {
                _saveData.nightPlayDone = true;
                Save();
                Debug.Log("[SaveSystem] 야간 플레이 완료!");
            }
        }

        /// <summary>
        /// 필요시 주간 데이터 리셋
        /// 월요일마다 자동 리셋
        /// </summary>
        public void ResetWeeklyIfNeeded()
        {
            if (string.IsNullOrEmpty(_saveData.weeklyResetDate))
            {
                _saveData.weeklyResetDate = DateTime.Now.ToString("o");
                return;
            }

            try
            {
                DateTime lastResetDate = DateTime.Parse(_saveData.weeklyResetDate);
                DateTime today = DateTime.Now;

                // 월요일마다 리셋 (월요일 = 1)
                bool isNewWeek = today.DayOfWeek == DayOfWeek.Monday &&
                                 lastResetDate.DayOfWeek != DayOfWeek.Monday;

                if (isNewWeek || (today - lastResetDate).TotalDays >= 7)
                {
                    _saveData.weeklyBestScore = 0;
                    _saveData.weeklyResetDate = DateTime.Now.ToString("o");
                    Save();
                    Debug.Log("[SaveSystem] 주간 데이터가 리셋되었습니다.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 주간 리셋 체크 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 모든 데이터 삭제 (디버그/테스트용)
        /// </summary>
        public void DeleteAll()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            _saveData = new SaveData();
            Debug.LogWarning("[SaveSystem] 모든 저장 데이터가 삭제되었습니다!");
        }

        /// <summary>
        /// 게임 종료 시 저장
        /// </summary>
        private void OnApplicationQuit()
        {
            Save();
        }

        /// <summary>
        /// 포커스 잃을 때 저장 (모바일용)
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save();
            }
        }
    }
}

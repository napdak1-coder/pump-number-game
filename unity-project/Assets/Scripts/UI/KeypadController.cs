using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using PumpNumber.Core;

namespace PumpNumber.UI
{
    /// <summary>
    /// 키패드 컨트롤러 — 1~9 버튼 + 스와이프 입력
    /// JS의 keypad 버튼 + swipe 시스템을 담당
    /// </summary>
    public class KeypadController : MonoBehaviour
    {
        [Header("=== 키 버튼 (1~9 순서) ===")]
        [SerializeField] private List<Button> keyButtons = new List<Button>();
        [SerializeField] private List<TMP_Text> keyLabels = new List<TMP_Text>();

        [Header("=== OK/백스페이스 버튼 ===")]
        [SerializeField] private Button okButton;              // OK 버튼 (답 제출)
        [SerializeField] private Button backspaceButton;       // ← 버튼 (백스페이스/클리어)

        [Header("=== 입력 디스플레이 ===")]
        [SerializeField] private TMP_Text inputDisplayText;   // 입력한 숫자 표시

        [Header("=== 스와이프 ===")]
        [SerializeField] private TMP_Text swipePreviewText;
        [SerializeField] private float swipeThreshold = 30f; // 스와이프 감지 거리

        [Header("=== 스타일 ===")]
        [SerializeField] private Color normalColor = new Color(1f, 0.84f, 0f, 1f);      // #ffd700
        [SerializeField] private Color forbiddenColor = new Color(1f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private Color pressedColor = new Color(1f, 0.9f, 0.3f, 1f);

        [Header("=== 사운드 & 피드백 ===")]
        [SerializeField] private AudioClip keyPressSFX;        // 키 누르기 사운드
        [SerializeField] private float basePitch = 1f;         // 기본 피치
        private AudioSource audioSource;

        // 스와이프 상태
        private bool swipeActive = false;
        private List<int> swipePath = new List<int>();
        private Vector2 swipeStartPos;
        private int swipeStartButton = -1;

        private void Start()
        {
            // AudioSource 초기화
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // 키 버튼 이벤트 연결
            for (int i = 0; i < keyButtons.Count; i++)
            {
                int num = i + 1; // 1~9
                int idx = i;

                // 일반 탭
                keyButtons[i].onClick.AddListener(() => OnKeyTap(num));

                // 스와이프를 위한 EventTrigger 추가
                AddSwipeTrigger(keyButtons[i].gameObject, num);
            }

            // OK 버튼 (답 제출)
            if (okButton)
                okButton.onClick.AddListener(OnOKButtonPressed);

            // 백스페이스 버튼 (백스페이스/클리어)
            if (backspaceButton)
                backspaceButton.onClick.AddListener(OnBackspaceButtonPressed);

            // GameManager 이벤트 구독
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRoundStart.AddListener(ResetKeypad);
            }
        }

        /// <summary>
        /// 일반 탭 입력
        /// </summary>
        private void OnKeyTap(int num)
        {
            if (GameManager.Instance == null) return;
            if (!swipeActive) // 스와이프 중이 아닐 때만
            {
                GameManager.Instance.PressKey(num);
                AnimateButtonPress(num - 1);

                // 비주얼 피드백
                PlayKeyPressSound(num);
                PlayKeyPressFeedback(num - 1);
            }
        }

        /// <summary>
        /// OK 버튼 눌림 (답 제출)
        /// </summary>
        private void OnOKButtonPressed()
        {
            // TODO: GameManager.SubmitAnswer() 호출
            Debug.Log("OK 버튼 눌림 - 답 제출");

            // 비주얼 피드백
            if (okButton)
            {
                AnimateButtonPress(okButton.GetComponent<RectTransform>());
            }

            PlayKeyPressFeedback(okButton?.GetComponent<RectTransform>());
        }

        /// <summary>
        /// 백스페이스 버튼 눌림 (백스페이스/클리어)
        /// </summary>
        private void OnBackspaceButtonPressed()
        {
            // TODO: GameManager.Backspace() 또는 입력 클리어
            Debug.Log("← 버튼 눌림 - 백스페이스");

            // 비주얼 피드백
            if (backspaceButton)
            {
                AnimateButtonPress(backspaceButton.GetComponent<RectTransform>());
            }

            PlayKeyPressFeedback(backspaceButton?.GetComponent<RectTransform>());
        }

        /// <summary>
        /// 키 누르기 사운드 (콤보에 따라 피치 변함)
        /// </summary>
        private void PlayKeyPressSound(int num)
        {
            if (keyPressSFX == null || audioSource == null) return;

            int combo = GameManager.Instance?.State.combo ?? 0;
            float pitch = basePitch + (combo * 0.05f); // 콤보가 높을수록 피치 올라감
            pitch = Mathf.Clamp(pitch, 0.5f, 2f);

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(keyPressSFX);
        }

        /// <summary>
        /// 스와이프 시작
        /// </summary>
        private void OnSwipeBegin(int num, Vector2 pos)
        {
            swipeActive = true;
            swipePath.Clear();
            swipePath.Add(num);
            swipeStartPos = pos;
            swipeStartButton = num;
            UpdateSwipePreview();
            HighlightButton(num - 1, true);
        }

        /// <summary>
        /// 스와이프 중 (다른 버튼 위로 이동)
        /// </summary>
        private void OnSwipeMove(int num, Vector2 pos)
        {
            if (!swipeActive) return;
            if (swipePath.Count > 0 && swipePath[swipePath.Count - 1] != num)
            {
                if (swipePath.Count < 3) // 최대 3자리
                {
                    swipePath.Add(num);
                    UpdateSwipePreview();
                    HighlightButton(num - 1, true);
                }
            }
        }

        /// <summary>
        /// 스와이프 종료 → 합산 값으로 키 입력
        /// </summary>
        private void OnSwipeEnd()
        {
            if (!swipeActive) return;
            swipeActive = false;

            if (swipePath.Count >= 2)
            {
                // 스와이프 경로를 숫자로 합산 (예: [3,5] → 35, [1,2,3] → 123)
                int actualNum = 0;
                foreach (int n in swipePath)
                    actualNum = actualNum * 10 + n;

                // 첫 번째 숫자로 pressKey 호출 (actualNum은 합산 값)
                GameManager.Instance?.PressKey(swipePath[0], actualNum);
            }

            // 모든 하이라이트 해제
            for (int i = 0; i < keyButtons.Count; i++)
                HighlightButton(i, false);

            swipePath.Clear();
            if (swipePreviewText) swipePreviewText.gameObject.SetActive(false);
        }

        /// <summary>
        /// 스와이프 프리뷰 업데이트
        /// </summary>
        private void UpdateSwipePreview()
        {
            if (swipePreviewText == null) return;
            if (swipePath.Count >= 2)
            {
                int val = 0;
                foreach (int n in swipePath) val = val * 10 + n;
                swipePreviewText.text = $"-{val}";
                swipePreviewText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 라운드 시작 시 키패드 리셋
        /// </summary>
        private void ResetKeypad()
        {
            var state = GameManager.Instance?.State;
            if (state == null) return;

            for (int i = 0; i < keyButtons.Count; i++)
            {
                int num = i + 1;
                bool isForbidden = state.forbiddenNums.Contains(num);

                keyButtons[i].interactable = !isForbidden;
                if (keyLabels.Count > i)
                {
                    keyLabels[i].text = num.ToString();
                    keyLabels[i].color = isForbidden ? forbiddenColor : normalColor;
                }
            }
        }

        // ================================================================
        // 버튼 애니메이션
        // ================================================================

        /// <summary>
        /// 버튼 눌림 애니메이션 (인덱스로)
        /// </summary>
        private void AnimateButtonPress(int idx)
        {
            if (idx < 0 || idx >= keyButtons.Count) return;
            var rt = keyButtons[idx].GetComponent<RectTransform>();
            AnimateButtonPress(rt);
        }

        /// <summary>
        /// 버튼 눌림 애니메이션 (RectTransform)
        /// 비주얼 피드백: 변환 + 섀도우 감소
        /// </summary>
        private void AnimateButtonPress(RectTransform rt)
        {
            if (rt == null) return;

            LeanTween.cancel(rt.gameObject);
            rt.localScale = Vector3.one;

            // 스케일 다운
            LeanTween.scale(rt.gameObject, Vector3.one * 0.85f, 0.05f)
                .setEase(LeanTweenType.easeInQuad)
                .setOnComplete(() =>
                {
                    // 원래 크기로 복구
                    LeanTween.scale(rt.gameObject, Vector3.one, 0.1f)
                        .setEase(LeanTweenType.easeOutQuad);
                });

            // 슬라이트 다운 이동 (눌린 감각)
            var startPos = rt.anchoredPosition;
            LeanTween.moveY(rt, startPos.y - 2f, 0.05f)
                .setEase(LeanTweenType.easeInQuad)
                .setOnComplete(() =>
                {
                    LeanTween.moveY(rt, startPos.y, 0.1f)
                        .setEase(LeanTweenType.easeOutQuad);
                });
        }

        /// <summary>
        /// 키 누르기 촉각 피드백 (진동)
        /// </summary>
        private void PlayKeyPressFeedback(RectTransform rt)
        {
            if (rt == null) return;

            // 모바일 진동
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif

            // 추가 비주얼 피드백은 AnimateButtonPress에서 처리됨
        }

        private void HighlightButton(int idx, bool on)
        {
            if (idx < 0 || idx >= keyButtons.Count) return;
            var img = keyButtons[idx].GetComponent<Image>();
            if (img) img.color = on ? pressedColor : Color.white;
        }

        // ================================================================
        // 스와이프 이벤트 트리거 셋업
        // ================================================================
        private void AddSwipeTrigger(GameObject go, int num)
        {
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();

            // PointerDown
            var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) =>
            {
                var pData = (PointerEventData)data;
                OnSwipeBegin(num, pData.position);
            });
            trigger.triggers.Add(entryDown);

            // PointerEnter (스와이프 중 진입)
            var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener((data) =>
            {
                var pData = (PointerEventData)data;
                if (swipeActive) OnSwipeMove(num, pData.position);
            });
            trigger.triggers.Add(entryEnter);

            // PointerUp
            var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => OnSwipeEnd());
            trigger.triggers.Add(entryUp);
        }
    }
}

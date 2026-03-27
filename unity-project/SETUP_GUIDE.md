# 빼봐영 Unity 프로젝트 셋업 가이드

## 1. Unity 설치 (무료)

1. [Unity Hub 다운로드](https://unity.com/download)
2. Unity Hub에서 **Unity 2022.3 LTS** (Long Term Support) 설치
3. 설치 시 모듈 선택:
   - ✅ Android Build Support (Android SDK & NDK)
   - ✅ iOS Build Support (Mac에서만 가능)
   - ✅ Visual Studio / VS Code

## 2. 프로젝트 열기

1. Unity Hub → "Open" → 이 폴더(`pump-number-game-unity`) 선택
2. 프로젝트가 열리면 `Assets/Scenes/` 폴더에서 메인 씬 열기

## 3. 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Core/
│   │   └── GameManager.cs      ← 게임 핵심 로직 (싱글톤)
│   ├── Data/
│   │   ├── GameConfig.cs       ← 난이도/설정 (ScriptableObject)
│   │   └── GameState.cs        ← 게임 상태 데이터
│   ├── UI/
│   │   ├── HUDController.cs    ← 점수/콤보/타이머 표시
│   │   ├── KeypadController.cs ← 1~9 키패드 + 스와이프
│   │   ├── GameOverController.cs ← 게임오버 화면
│   │   └── StartScreenController.cs ← 시작 화면
│   ├── Effects/
│   │   ├── ParticleEffects.cs  ← 파티클/배경 효과
│   │   └── PixelCharacters.cs  ← 메롱 얼굴/강아지 생성
│   └── Audio/
│       └── SoundManager.cs     ← 8비트 효과음
├── Scenes/
├── Prefabs/
├── Fonts/          ← DotGothic16 폰트 파일
├── Sprites/        ← UI/캐릭터 스프라이트
├── Audio/          ← 효과음 파일
└── Resources/
```

## 4. 씬 구성 방법

Unity 에디터에서 빈 씬을 만들고 다음 순서로 오브젝트 배치:

### 4.1 매니저 오브젝트
- 빈 GameObject "GameManager" → `GameManager.cs` 부착
- 빈 GameObject "SoundManager" → `SoundManager.cs` + AudioSource 부착
- 빈 GameObject "ParticleEffects" → `ParticleEffects.cs` 부착

### 4.2 UI Canvas 구성
- Canvas (Screen Space - Overlay, Canvas Scaler: Scale With Screen Size, 1080x1920)
  - Panel "StartScreen" → `StartScreenController.cs`
  - Panel "HUD" → `HUDController.cs`
    - Text "Score", Text "Combo", Text "Stage", Text "Lives"
    - Text "TargetNumber" (큰 숫자)
    - Image "CircularTimer"
    - Text "ModeBadge"
    - Text "SpeedMult"
  - Panel "Keypad" → `KeypadController.cs`
    - 9개 Button (1~9)
  - Panel "GameOver" → `GameOverController.cs`
    - 각종 텍스트 + 재도전 버튼
  - Image "FlashOverlay" (화면 플래시)

### 4.3 GameConfig 생성
- Project 창에서 우클릭 → Create → PumpNumber → GameConfig
- GameManager의 Inspector에서 Config 필드에 드래그

## 5. Android 빌드

1. File → Build Settings → Platform을 Android로 Switch
2. Player Settings에서:
   - Package Name: `com.pumpnumber.bbaebyoyeong`
   - Minimum API Level: Android 7.0 (API 24)
   - Target API Level: Android 14 (API 34)
   - 화면 방향: Portrait Only
3. Build → APK 파일 생성

## 6. iOS 빌드 (Mac에서만)

1. File → Build Settings → Platform을 iOS로 Switch
2. Player Settings에서:
   - Bundle Identifier: `com.pumpnumber.bbaebyoyeong`
   - Target minimum iOS Version: 14.0
   - 화면 방향: Portrait Only
3. Build → Xcode 프로젝트 생성 → Xcode에서 Archive → App Store 업로드

## 7. Mac 없이 iOS 빌드 (클라우드)

1. [Codemagic](https://codemagic.io) 회원가입 (무료)
2. GitHub 레포 연결
3. Unity iOS 빌드 파이프라인 설정
4. 빌드 → IPA 다운로드 → App Store Connect에 업로드

## 8. JS→C# 매핑 참고

| JS (index.html) | C# (Unity) |
|---|---|
| `startGame()` | `GameManager.StartGame()` |
| `nextRound()` | `GameManager.NextRound()` |
| `pressKey(num, actualNum)` | `GameManager.PressKey(num, actualNum)` |
| `handleSuccess()` | `GameManager.HandleSuccess()` (private) |
| `handleFail()` | `GameManager.HandleFail()` (private) |
| `showGameOver()` | `GameOverController.Show()` |
| `getDifficulty(stage)` | `GameConfig.GetDifficulty(stage)` |
| `getRank(taps, minTaps)` | `GameManager.GetRank(taps, minTaps)` |
| `localStorage` | `PlayerPrefs` |
| `setInterval` | `Coroutine` / `InvokeRepeating` |
| `setTimeout` | `StartCoroutine` + `WaitForSeconds` |
| `document.createElement` | `Instantiate(prefab)` |
| CSS 애니메이션 | Unity Animator / DOTween / LeanTween |
| Canvas 2D (drawTauntFace) | `Texture2D.SetPixel()` → Sprite |
| Web Audio API | `AudioSource` + `AudioClip` |

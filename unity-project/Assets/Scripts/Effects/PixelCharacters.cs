using UnityEngine;

namespace PumpNumber.Effects
{
    /// <summary>
    /// 픽셀 캐릭터 생성 — 메롱 얼굴 + 5색 캐릭터 + 강아지 + 보석
    ///
    /// [사용법]
    /// // 기존 메롱 얼굴 (게임 내 HUD용)
    /// Texture2D face = PixelCharacters.CreateTauntFace(0, Color.red);
    ///
    /// // 5색 캐릭터 (시작화면용 — 빨/주/초/파/보)
    /// Texture2D colorFace = PixelCharacters.CreateColorFace(bodyColor, cheekColor, eyeStyle, mouthStyle);
    ///
    /// // 강아지
    /// Texture2D dog = PixelCharacters.CreatePixelDog(bodyColor, earColor, cheekColor, true);
    ///
    /// // 보석 캐릭터 (시작화면용)
    /// Texture2D gem = PixelCharacters.CreateGemCharacter();
    ///
    /// // Sprite로 변환
    /// Sprite sprite = PixelCharacters.TextureToSprite(face);
    /// </summary>
    public static class PixelCharacters
    {
        private static readonly Color O = new Color(0.16f, 0.16f, 0.16f, 1f); // #282828 외곽선
        private static readonly Color W = Color.white;

        // ================================================================
        // 5색 캐릭터 얼굴 (시작화면용, 14x14)
        // 스크린샷의 빨/주/초/파/보 캐릭터 재현
        // eyeStyle: 0=기본둥근, 1=윙크, 2=양쪽둥근, 3=웃는눈, 4=반짝
        // mouthStyle: 0=혀내밀기, 1=넓은웃음, 2=작은미소, 3=O입, 4=삐죽
        // ================================================================
        public static Texture2D CreateColorFace(Color bodyColor, Color cheekColor, int eyeStyle, int mouthStyle)
        {
            Texture2D tex = new Texture2D(14, 14);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clear = new Color[14 * 14];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            Color bc = bodyColor, ck = cheekColor;

            // --- 몸체 (둥근 사각형) ---
            // Unity Texture2D: y=0이 아래, y=13이 위
            // 상단 윤곽 (y=13, y=12)
            for (int i = 3; i <= 10; i++) Px(tex, i, 13, O);
            Px(tex, 2, 12, O); for (int i = 3; i <= 10; i++) Px(tex, i, 12, bc); Px(tex, 11, 12, O);
            // 본체 (y=3~11)
            for (int y = 3; y <= 11; y++)
            {
                Px(tex, 1, y, O);
                for (int i = 2; i <= 11; i++) Px(tex, i, y, bc);
                Px(tex, 12, y, O);
            }
            // 하단 윤곽
            Px(tex, 2, 2, O); for (int i = 3; i <= 10; i++) Px(tex, i, 2, bc); Px(tex, 11, 2, O);
            for (int i = 3; i <= 10; i++) Px(tex, i, 1, O);

            // --- 하이라이트 (좌상단) ---
            Px(tex, 3, 11, new Color(1f, 1f, 1f, 0.23f));
            Px(tex, 4, 11, new Color(1f, 1f, 1f, 0.16f));
            Px(tex, 3, 10, new Color(1f, 1f, 1f, 0.12f));

            // --- 볼 터치 ---
            Px(tex, 3, 6, ck); Px(tex, 3, 5, ck);
            Px(tex, 10, 6, ck); Px(tex, 10, 5, ck);

            // --- 눈 ---
            int es = eyeStyle % 5;
            if (es == 0) // 기본 둥근 눈
            {
                Px(tex, 4, 9, O); Px(tex, 5, 9, O);
                Px(tex, 4, 8, O); Px(tex, 5, 8, W);
                Px(tex, 8, 9, O); Px(tex, 9, 9, O);
                Px(tex, 8, 8, O); Px(tex, 9, 8, W);
            }
            else if (es == 1) // 윙크 (왼쪽 감은 눈)
            {
                Px(tex, 4, 8, O); Px(tex, 5, 8, O); Px(tex, 6, 8, O);
                Px(tex, 8, 9, O); Px(tex, 9, 9, O);
                Px(tex, 8, 8, O); Px(tex, 9, 8, W);
            }
            else if (es == 2) // 양쪽 둥근 큰 눈
            {
                Px(tex, 4, 9, O); Px(tex, 5, 9, O);
                Px(tex, 4, 8, W); Px(tex, 5, 8, O);
                Px(tex, 8, 9, O); Px(tex, 9, 9, O);
                Px(tex, 8, 8, W); Px(tex, 9, 8, O);
            }
            else if (es == 3) // 웃는 눈 (반달)
            {
                Px(tex, 4, 9, O); Px(tex, 5, 9, O); Px(tex, 6, 9, O); Px(tex, 5, 8, O);
                Px(tex, 8, 9, O); Px(tex, 9, 9, O); Px(tex, 10, 9, O); Px(tex, 9, 8, O);
            }
            else // 반짝 눈 (십자)
            {
                Px(tex, 4, 9, O); Px(tex, 5, 9, W); Px(tex, 6, 9, O);
                Px(tex, 5, 10, O); Px(tex, 5, 8, O);
                Px(tex, 8, 9, O); Px(tex, 9, 9, W); Px(tex, 10, 9, O);
                Px(tex, 9, 10, O); Px(tex, 9, 8, O);
            }

            // --- 입 ---
            int ms = mouthStyle % 5;
            Color tongue = new Color(1f, 0.39f, 0.39f);
            if (ms == 0) // 혀 내밀기
            {
                Px(tex, 5, 5, O); Px(tex, 6, 5, O); Px(tex, 7, 5, O); Px(tex, 8, 5, O);
                Px(tex, 6, 4, tongue); Px(tex, 7, 4, tongue);
                Px(tex, 6, 3, O); Px(tex, 7, 3, O);
            }
            else if (ms == 1) // 넓은 웃음
            {
                Px(tex, 4, 5, O);
                for (int i = 5; i <= 8; i++) Px(tex, i, 5, O);
                Px(tex, 9, 5, O);
                Px(tex, 5, 4, O); Px(tex, 8, 4, O);
            }
            else if (ms == 2) // 작은 미소
            {
                Px(tex, 6, 5, O); Px(tex, 7, 5, O);
                Px(tex, 5, 6, O); Px(tex, 8, 6, O);
            }
            else if (ms == 3) // O 입
            {
                Px(tex, 6, 6, O); Px(tex, 7, 6, O);
                Px(tex, 5, 5, O); Px(tex, 8, 5, O);
                Px(tex, 6, 4, O); Px(tex, 7, 4, O);
            }
            else // 삐죽 입
            {
                Px(tex, 5, 5, O); Px(tex, 6, 5, O); Px(tex, 7, 5, O); Px(tex, 8, 5, O); Px(tex, 9, 6, O);
            }

            tex.Apply();
            return tex;
        }

        // ================================================================
        // 보석 캐릭터 (16x16)
        // 시작화면의 보라색 보석 — 눈 + 볼 + 미소
        // ================================================================
        public static Texture2D CreateGemCharacter()
        {
            Texture2D tex = new Texture2D(16, 16);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clear = new Color[16 * 16];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            Color gm = new Color(160/255f, 80/255f, 200/255f);   // 메인 보라
            Color gd = new Color(120/255f, 50/255f, 160/255f);   // 어두운 보라
            Color gl = new Color(200/255f, 140/255f, 240/255f);  // 밝은 보라
            Color ck = new Color(1f, 0.59f, 0.78f, 0.31f);       // 볼

            // Y좌표: Unity에서 y=0이 아래, y=15가 위
            // 보석 상단 (꼭짓점)
            for (int i = 6; i <= 9; i++) Px(tex, i, 14, O);
            Px(tex, 5, 13, O); for (int i = 6; i <= 9; i++) Px(tex, i, 13, gl); Px(tex, 10, 13, O);
            Px(tex, 4, 12, O); for (int i = 5; i <= 10; i++) Px(tex, i, 12, gl); Px(tex, 11, 12, O);
            Px(tex, 3, 11, O); for (int i = 4; i <= 11; i++) Px(tex, i, 11, gm); Px(tex, 12, 11, O);

            // 본체
            for (int y = 5; y <= 10; y++)
            {
                Px(tex, 2, y, O);
                for (int i = 3; i <= 12; i++) Px(tex, i, y, gm);
                Px(tex, 13, y, O);
            }

            // 하이라이트
            Px(tex, 4, 10, gl); Px(tex, 5, 10, gl); Px(tex, 4, 9, gl);
            // 그림자
            Px(tex, 10, 10, gd); Px(tex, 11, 10, gd); Px(tex, 11, 9, gd);

            // 하단 (역삼각형)
            Px(tex, 3, 4, O); for (int i = 4; i <= 11; i++) Px(tex, i, 4, gd); Px(tex, 12, 4, O);
            Px(tex, 4, 3, O); for (int i = 5; i <= 10; i++) Px(tex, i, 3, gd); Px(tex, 11, 3, O);
            Px(tex, 5, 2, O); for (int i = 6; i <= 9; i++) Px(tex, i, 2, gd); Px(tex, 10, 2, O);
            for (int i = 6; i <= 9; i++) Px(tex, i, 1, O);

            // 눈 (y=8)
            Px(tex, 5, 8, O); Px(tex, 6, 8, O); Px(tex, 5, 7, O); Px(tex, 6, 7, W);
            Px(tex, 9, 8, O); Px(tex, 10, 8, O); Px(tex, 9, 7, O); Px(tex, 10, 7, W);

            // 볼
            Px(tex, 4, 6, ck); Px(tex, 11, 6, ck);

            // 입 (작은 미소)
            Px(tex, 7, 5, O); Px(tex, 8, 5, O);

            tex.Apply();
            return tex;
        }

        // ================================================================
        // 기존: 메롱 얼굴 (게임 내 HUD 캐릭터바용, 14x14)
        // ================================================================
        public static Texture2D CreateTauntFace(int style, Color faceColor)
        {
            Texture2D tex = new Texture2D(14, 14);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clear = new Color[14 * 14];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            Color fc = faceColor;
            int s = style % 5;

            // 공통 윤곽 (Y 뒤집힘)
            for (int i = 4; i <= 9; i++) Px(tex, i, 13, O);
            Px(tex, 3, 12, O);
            for (int i = 4; i <= 9; i++) Px(tex, i, 12, fc);
            Px(tex, 10, 12, O);
            for (int y = 4; y <= 11; y++)
            {
                Px(tex, 2, y, O);
                for (int i = 3; i <= 10; i++) Px(tex, i, y, fc);
                Px(tex, 11, y, O);
            }
            Px(tex, 3, 3, O);
            for (int i = 4; i <= 9; i++) Px(tex, i, 3, fc);
            Px(tex, 10, 3, O);
            for (int i = 4; i <= 9; i++) Px(tex, i, 2, O);

            Color tongue = new Color(1f, 0.42f, 0.42f, 1f);
            Color tongueDeep = new Color(1f, 0.25f, 0.25f, 1f);

            if (s == 0)
            {
                Px(tex, 4, 9, O); Px(tex, 5, 9, O); Px(tex, 6, 9, O);
                Px(tex, 8, 10, O); Px(tex, 9, 10, O);
                Px(tex, 8, 9, W); Px(tex, 9, 9, O);
                Px(tex, 8, 8, O); Px(tex, 9, 8, O);
                Px(tex, 5, 6, O); Px(tex, 6, 6, O); Px(tex, 7, 6, O); Px(tex, 8, 6, O);
                Px(tex, 5, 5, O); Px(tex, 6, 5, tongue); Px(tex, 7, 5, tongue); Px(tex, 8, 5, O);
                Px(tex, 6, 4, tongue); Px(tex, 7, 4, tongue);
                Px(tex, 6, 3, O); Px(tex, 7, 3, O);
            }
            else if (s == 1)
            {
                Px(tex, 4, 9, O); Px(tex, 5, 9, O); Px(tex, 6, 9, O);
                Px(tex, 8, 9, O); Px(tex, 9, 9, O); Px(tex, 10, 9, O);
                Px(tex, 3, 8, new Color(1f, 0.56f, 0.56f)); Px(tex, 10, 8, new Color(1f, 0.56f, 0.56f));
                for (int i = 5; i <= 9; i++) Px(tex, i, 6, O);
                Px(tex, 6, 5, O); Px(tex, 7, 5, new Color(1f, 0.33f, 0.33f)); Px(tex, 8, 5, O);
                Px(tex, 7, 4, new Color(1f, 0.33f, 0.33f));
                Px(tex, 7, 3, new Color(1f, 0.33f, 0.33f));
                Px(tex, 7, 2, O);
            }
            else if (s == 2)
            {
                Px(tex, 4, 10, O); Px(tex, 6, 10, O); Px(tex, 5, 9, O);
                Px(tex, 4, 8, O); Px(tex, 6, 8, O);
                Px(tex, 8, 10, O); Px(tex, 10, 10, O); Px(tex, 9, 9, O);
                Px(tex, 8, 8, O); Px(tex, 10, 8, O);
                for (int i = 5; i <= 9; i++) Px(tex, i, 6, O);
                Px(tex, 6, 5, tongue); Px(tex, 7, 5, tongue); Px(tex, 8, 5, tongue);
                Px(tex, 6, 4, O); Px(tex, 7, 4, tongueDeep); Px(tex, 8, 4, O);
                Px(tex, 7, 3, O);
            }
            else if (s == 3)
            {
                Px(tex, 4, 10, O); Px(tex, 5, 10, O); Px(tex, 6, 10, O);
                Px(tex, 4, 9, O); Px(tex, 6, 9, O);
                Px(tex, 8, 10, O); Px(tex, 9, 10, O); Px(tex, 10, 10, O);
                Px(tex, 8, 9, O); Px(tex, 10, 9, O);
                Px(tex, 3, 8, new Color(1f, 0.69f, 0.69f)); Px(tex, 10, 8, new Color(1f, 0.69f, 0.69f));
                Px(tex, 5, 6, O); Px(tex, 6, 6, O); Px(tex, 7, 6, O); Px(tex, 8, 6, O);
                Px(tex, 6, 5, new Color(1f, 0.44f, 0.44f)); Px(tex, 7, 5, new Color(1f, 0.44f, 0.44f));
                Px(tex, 5, 4, O); Px(tex, 6, 4, new Color(1f, 0.31f, 0.31f)); Px(tex, 7, 4, O);
            }
            else
            {
                Px(tex, 4, 10, O); Px(tex, 5, 10, O); Px(tex, 4, 9, W); Px(tex, 5, 9, O);
                Px(tex, 8, 10, O); Px(tex, 9, 10, O); Px(tex, 8, 9, W); Px(tex, 9, 9, O);
                Px(tex, 5, 6, O); Px(tex, 6, 6, O); Px(tex, 7, 6, O); Px(tex, 8, 6, O); Px(tex, 9, 7, O);
                Px(tex, 6, 5, tongue); Px(tex, 7, 5, tongue);
                Px(tex, 6, 4, O); Px(tex, 7, 4, O);
            }

            tex.Apply();
            return tex;
        }

        // ================================================================
        // 기존: 픽셀 강아지 (14x16)
        // ================================================================
        public static Texture2D CreatePixelDog(Color bodyColor, Color earColor, Color cheekColor, bool armsUp)
        {
            Texture2D tex = new Texture2D(14, 16);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clear = new Color[14 * 16];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            Color bc = bodyColor, ec = earColor, cc = cheekColor;

            // 귀
            Px(tex, 2, 15, O); Px(tex, 3, 15, O); Px(tex, 10, 15, O); Px(tex, 11, 15, O);
            Px(tex, 1, 14, O); Px(tex, 2, 14, ec); Px(tex, 3, 14, ec); Px(tex, 4, 14, O);
            Px(tex, 9, 14, O); Px(tex, 10, 14, ec); Px(tex, 11, 14, ec); Px(tex, 12, 14, O);
            Px(tex, 1, 13, O); Px(tex, 2, 13, ec); Px(tex, 3, 13, ec); Px(tex, 4, 13, O);
            Px(tex, 9, 13, O); Px(tex, 10, 13, ec); Px(tex, 11, 13, ec); Px(tex, 12, 13, O);

            for (int i = 3; i <= 10; i++) Px(tex, i, 12, O);

            Px(tex, 2, 11, O); for (int i = 3; i <= 10; i++) Px(tex, i, 11, bc); Px(tex, 11, 11, O);
            Px(tex, 2, 10, O); for (int i = 3; i <= 10; i++) Px(tex, i, 10, bc); Px(tex, 11, 10, O);
            Px(tex, 4, 10, W); Px(tex, 5, 10, O); Px(tex, 8, 10, W); Px(tex, 9, 10, O);
            Px(tex, 2, 9, O); for (int i = 3; i <= 10; i++) Px(tex, i, 9, bc); Px(tex, 11, 9, O);
            Px(tex, 6, 9, O); Px(tex, 7, 9, O); Px(tex, 3, 9, cc); Px(tex, 10, 9, cc);
            Px(tex, 2, 8, O); for (int i = 3; i <= 10; i++) Px(tex, i, 8, bc); Px(tex, 11, 8, O);

            for (int y = 4; y <= 7; y++)
            {
                Px(tex, 3, y, O);
                for (int i = 4; i <= 9; i++) Px(tex, i, y, bc);
                Px(tex, 10, y, O);
            }

            if (armsUp)
            {
                Px(tex, 2, 7, O); Px(tex, 3, 7, bc); Px(tex, 3, 8, O); Px(tex, 2, 8, bc);
                Px(tex, 10, 7, bc); Px(tex, 11, 7, O); Px(tex, 10, 8, O); Px(tex, 11, 8, bc);
            }
            else
            {
                Px(tex, 3, 6, O); Px(tex, 2, 5, O); Px(tex, 10, 6, O); Px(tex, 11, 5, O);
            }

            Px(tex, 4, 3, O); Px(tex, 5, 3, O); Px(tex, 8, 3, O); Px(tex, 9, 3, O);
            Px(tex, 3, 2, O); Px(tex, 4, 2, bc); Px(tex, 5, 2, O);
            Px(tex, 8, 2, O); Px(tex, 9, 2, bc); Px(tex, 10, 2, O);

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Texture2D → Sprite 변환
        /// </summary>
        public static Sprite TextureToSprite(Texture2D tex)
        {
            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                14f
            );
        }

        private static void Px(Texture2D tex, int x, int y, Color c)
        {
            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                tex.SetPixel(x, y, c);
        }
    }
}

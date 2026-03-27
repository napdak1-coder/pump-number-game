using UnityEngine;

namespace PumpNumber.Effects
{
    /// <summary>
    /// 픽셀 캐릭터 생성 — 메롱 얼굴 + 강아지
    /// JS의 drawTauntFace(), drawPixelDog()를 Texture2D로 변환
    /// Unity에서는 이걸 Sprite로 만들어서 Image/SpriteRenderer에 적용
    /// </summary>
    public static class PixelCharacters
    {
        private static readonly Color O = new Color(0.2f, 0.2f, 0.2f, 1f);  // 외곽선 #333
        private static readonly Color W = Color.white;

        /// <summary>
        /// 메롱 얼굴 생성 (14x14 픽셀)
        /// JS: drawTauntFace(canvas, style, mainColor)
        /// style: 0=윙크+혀, 1=양눈감고+긴혀, 2=XD+혀, 3=^^웃음+혀, 4=찡긋
        /// </summary>
        public static Texture2D CreateTauntFace(int style, Color faceColor)
        {
            Texture2D tex = new Texture2D(14, 14);
            tex.filterMode = FilterMode.Point; // 픽셀아트용
            tex.wrapMode = TextureWrapMode.Clamp;

            // 배경 투명
            Color[] clear = new Color[14 * 14];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            Color fc = faceColor;
            int s = style % 5;

            // 공통 얼굴 윤곽
            // 상단: 4~9 윤곽
            for (int i = 4; i <= 9; i++) Px(tex, i, 13, O);
            // 2번째 줄
            Px(tex, 3, 12, O);
            for (int i = 4; i <= 9; i++) Px(tex, i, 12, fc);
            Px(tex, 10, 12, O);
            // 3~10번째 줄 (몸통)
            for (int y = 4; y <= 11; y++)
            {
                Px(tex, 2, y, O);
                for (int i = 3; i <= 10; i++) Px(tex, i, y, fc);
                Px(tex, 11, y, O);
            }
            // 하단
            Px(tex, 3, 3, O);
            for (int i = 4; i <= 9; i++) Px(tex, i, 3, fc);
            Px(tex, 10, 3, O);
            for (int i = 4; i <= 9; i++) Px(tex, i, 2, O);

            // 표정별 눈/입 (Y좌표 뒤집힘 주의: Unity Texture2D는 아래가 0)
            Color tongue = new Color(1f, 0.42f, 0.42f, 1f); // #ff6b6b
            Color tongueDeep = new Color(1f, 0.25f, 0.25f, 1f); // #ff4040

            if (s == 0) // 윙크 + 혀
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
            else if (s == 1) // 양눈 감고 + 긴 혀
            {
                Px(tex, 4, 9, O); Px(tex, 5, 9, O); Px(tex, 6, 9, O);
                Px(tex, 8, 9, O); Px(tex, 9, 9, O); Px(tex, 10, 9, O);
                Px(tex, 3, 8, new Color(1f, 0.56f, 0.56f)); Px(tex, 10, 8, new Color(1f, 0.56f, 0.56f));
                Px(tex, 5, 6, O); Px(tex, 6, 6, O); Px(tex, 7, 6, O); Px(tex, 8, 6, O); Px(tex, 9, 6, O);
                Px(tex, 6, 5, O); Px(tex, 7, 5, new Color(1f, 0.33f, 0.33f)); Px(tex, 8, 5, O);
                Px(tex, 7, 4, new Color(1f, 0.33f, 0.33f));
                Px(tex, 7, 3, new Color(1f, 0.33f, 0.33f));
                Px(tex, 7, 2, O);
            }
            else if (s == 2) // XD + 혀
            {
                Px(tex, 4, 10, O); Px(tex, 6, 10, O); Px(tex, 5, 9, O);
                Px(tex, 4, 8, O); Px(tex, 6, 8, O);
                Px(tex, 8, 10, O); Px(tex, 10, 10, O); Px(tex, 9, 9, O);
                Px(tex, 8, 8, O); Px(tex, 10, 8, O);
                Px(tex, 5, 6, O); Px(tex, 6, 6, O); Px(tex, 7, 6, O); Px(tex, 8, 6, O); Px(tex, 9, 6, O);
                Px(tex, 6, 5, tongue); Px(tex, 7, 5, tongue); Px(tex, 8, 5, tongue);
                Px(tex, 6, 4, O); Px(tex, 7, 4, tongueDeep); Px(tex, 8, 4, O);
                Px(tex, 7, 3, O);
            }
            else if (s == 3) // ^^ 웃음 + 혀
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
            else // 찡긋
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

        /// <summary>
        /// 픽셀 강아지 생성 (14x16 픽셀)
        /// JS: drawPixelDog(canvas, bodyColor, earColor, cheekColor, armsUp)
        /// </summary>
        public static Texture2D CreatePixelDog(Color bodyColor, Color earColor, Color cheekColor, bool armsUp)
        {
            Texture2D tex = new Texture2D(14, 16);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] clear = new Color[14 * 16];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            Color bc = bodyColor, ec = earColor, cc = cheekColor;

            // 귀 (Y좌표 뒤집힘: 원본 y=0 → tex y=15)
            Px(tex, 2, 15, O); Px(tex, 3, 15, O); Px(tex, 10, 15, O); Px(tex, 11, 15, O);
            Px(tex, 1, 14, O); Px(tex, 2, 14, ec); Px(tex, 3, 14, ec); Px(tex, 4, 14, O);
            Px(tex, 9, 14, O); Px(tex, 10, 14, ec); Px(tex, 11, 14, ec); Px(tex, 12, 14, O);
            Px(tex, 1, 13, O); Px(tex, 2, 13, ec); Px(tex, 3, 13, ec); Px(tex, 4, 13, O);
            Px(tex, 9, 13, O); Px(tex, 10, 13, ec); Px(tex, 11, 13, ec); Px(tex, 12, 13, O);

            // 머리 윤곽
            for (int i = 3; i <= 10; i++) Px(tex, i, 12, O);

            // 얼굴
            Px(tex, 2, 11, O); for (int i = 3; i <= 10; i++) Px(tex, i, 11, bc); Px(tex, 11, 11, O);
            Px(tex, 2, 10, O); for (int i = 3; i <= 10; i++) Px(tex, i, 10, bc); Px(tex, 11, 10, O);
            Px(tex, 4, 10, W); Px(tex, 5, 10, O); Px(tex, 8, 10, W); Px(tex, 9, 10, O);
            Px(tex, 2, 9, O); for (int i = 3; i <= 10; i++) Px(tex, i, 9, bc); Px(tex, 11, 9, O);
            Px(tex, 6, 9, O); Px(tex, 7, 9, O); Px(tex, 3, 9, cc); Px(tex, 10, 9, cc);
            Px(tex, 2, 8, O); for (int i = 3; i <= 10; i++) Px(tex, i, 8, bc); Px(tex, 11, 8, O);

            // 몸통
            for (int y = 4; y <= 7; y++)
            {
                Px(tex, 3, y, O);
                for (int i = 4; i <= 9; i++) Px(tex, i, y, bc);
                Px(tex, 10, y, O);
            }

            // 팔
            if (armsUp)
            {
                Px(tex, 2, 7, O); Px(tex, 3, 7, bc); Px(tex, 3, 8, O); Px(tex, 2, 8, bc);
                Px(tex, 10, 7, bc); Px(tex, 11, 7, O); Px(tex, 10, 8, O); Px(tex, 11, 8, bc);
            }
            else
            {
                Px(tex, 3, 6, O); Px(tex, 2, 5, O); Px(tex, 10, 6, O); Px(tex, 11, 5, O);
            }

            // 다리
            Px(tex, 4, 3, O); Px(tex, 5, 3, O); Px(tex, 8, 3, O); Px(tex, 9, 3, O);
            Px(tex, 3, 2, O); Px(tex, 4, 2, bc); Px(tex, 5, 2, O);
            Px(tex, 8, 2, O); Px(tex, 9, 2, bc); Px(tex, 10, 2, O);

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Texture2D를 Sprite로 변환하는 헬퍼
        /// </summary>
        public static Sprite TextureToSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 14f);
        }

        private static void Px(Texture2D tex, int x, int y, Color c)
        {
            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                tex.SetPixel(x, y, c);
        }
    }
}

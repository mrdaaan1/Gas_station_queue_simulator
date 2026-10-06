using System;
using System.Collections.Generic;

namespace GasQueue.Intro
{
    public struct Box
    {
        public float x, y, w, h;
        public Box(float x, float y, float w, float h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public float cx => x + w / 2f;
        public float cy => y + h / 2f;
        public Box Fit(Func<Box, Box> f) => f(this);
    }

    /// <summary>Что нарисовать в кадре: картинка (или сплошной цвет, если Tex = −1) в прямоугольнике экрана.</summary>
    public struct Sprite
    {
        public int Tex;
        public float X, Y, W, H;
        /// <summary>Кусок картинки (0..1, Y сверху вниз).</summary>
        public float U, V, UW, VH;
        public float R, G, B, Alpha;
        public bool Clip;
        public float ClipX, ClipY, ClipW, ClipH;
    }

    /// <summary>
    /// Раскладка заставки в стиле обложки GTA VI и её анимация по времени. Чистые вычисления:
    /// Unity (<c>IntroSplash</c>) и превью (Tools/IntroArt) рисуют один и тот же список <see cref="Sprite"/>.
    /// </summary>
    public class IntroLayout
    {
        public const int PanelCount = 9;
        public const int TexLogo = 9, TexGlow = 10, TexShine = 11, TexLine0 = 12, TexTagline = 15, TexCount = 16;
        public static readonly string[] Lines = { "симулятор", "очереди", "на заправку" };
        public const string Tagline = "ВЫ В ОЧЕРЕДИ: 1 Ч 20 МИН";

        // Тайминг (секунды)
        const float FirstPanel = 0.25f, PanelStep = 0.15f;
        /// <summary>Порядок появления кадров: центральный (очередь под логотипом) — последним.</summary>
        static readonly int[] Order = { 0, 6, 3, 2, 8, 5, 1, 7, 4 };
        public static readonly float LogoTime = FirstPanel + PanelStep * PanelCount + 0.1f;
        public static readonly float FadeStart = LogoTime + 2.5f;
        /// <summary>Заставка закрыта чёрным — под ней можно показывать меню.</summary>
        public static readonly float MenuTime = FadeStart + 0.45f;
        /// <summary>Чёрный растаял поверх меню — заставка закончилась.</summary>
        public static readonly float EndTime = MenuTime + 0.45f;

        public float Width, Height;
        public Box[] Panels = new Box[PanelCount];
        public Box Logo, Tagline_;
        public Box[] LineBoxes = new Box[3];
        public float LineSize, LogoSize;

        /// <summary>Число на логотипе — «92» римскими цифрами, как «VI» у GTA.</summary>
        public const string Numeral = "XCII";
        /// <summary>Базовая линия цифр — в нижней части логотипа (доля высоты).</summary>
        public const float NumeralBaseline = 0.95f;

        /// <summary>Вертикальная «обложка» по центру экрана, по бокам — чёрное.</summary>
        public Box Poster;

        public static IntroLayout Compute(float w, float h)
        {
            var L = new IntroLayout { Width = w, Height = h };
            float ph = Math.Min(h * 0.96f, w * 0.96f / 0.8f), pw = (float)Math.Round(ph * 0.8f);
            L.Poster = new Box((float)Math.Round((w - pw) / 2f), (float)Math.Round((h - ph) / 2f), pw, (float)Math.Round(ph));
            float px = L.Poster.x, py = L.Poster.y;
            float g = Math.Max(3f, (float)Math.Round(ph * 0.011f));
            float m = g;
            float side = (float)Math.Round((pw - 2 * m - 2 * g) * 0.3f);
            float center = pw - 2 * m - 2 * g - 2 * side;
            float avail = ph - 2 * m;
            Column(L.Panels, 0, px + m, py + m, side, avail, g, 0.36f, 0.30f);
            Column(L.Panels, 3, px + m + side + g, py + m, center, avail, g, 0.27f, 0.40f);
            Column(L.Panels, 6, px + m + side + g + center + g, py + m, side, avail, g, 0.30f, 0.36f);

            // Логотип — поверх центрального кадра, заходит на соседние, как на обложке
            var c = L.Panels[4];
            float lw = pw * 0.86f, lh = lw * 0.62f;
            L.Logo = new Box(L.Poster.cx - lw / 2f, c.cy - lh / 2f, lw, lh);
            L.LogoSize = Math.Min(lw * 0.96f / Path.TextWidth(Numeral, 1f, 0.02f), lh * 1.3f);

            float widest = 0f;
            foreach (var s in Lines) widest = Math.Max(widest, Path.TextWidth(s, 1f));
            L.LineSize = Math.Min(lw * 0.72f / widest, lh * 0.25f);
            float lead = L.LineSize * 0.84f;
            float top = L.Logo.y + lh * 0.02f; // надпись сверху, нижняя строка заходит на цифры
            for (int i = 0; i < 3; i++)
                L.LineBoxes[i] = new Box(L.Logo.x - L.LineSize * 0.2f, top + i * lead - L.LineSize * 0.1f, lw + L.LineSize * 0.4f, L.LineSize * 1.2f);

            float ts = L.LineSize * 0.3f;
            float tw = Path.TextWidth(Tagline, ts, 0.12f) + ts * 1.2f;
            float numeralBottom = L.Logo.y + lh * NumeralBaseline;
            L.Tagline_ = new Box(L.Poster.cx - tw / 2f, Math.Min(numeralBottom + ts * 0.6f, py + ph - m - ts * 1.6f), tw, ts * 1.5f);
            return L;
        }

        static void Column(Box[] panels, int first, float x, float y, float w, float avail, float g, float f0, float f1)
        {
            float usable = avail - 2 * g;
            float h0 = (float)Math.Round(usable * f0), h1 = (float)Math.Round(usable * f1), h2 = usable - h0 - h1;
            panels[first] = new Box(x, y, w, h0);
            panels[first + 1] = new Box(x, y + h0 + g, w, h1);
            panels[first + 2] = new Box(x, y + h0 + g + h1 + g, w, h2);
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        static float OutCubic(float p) { p = 1f - p; return 1f - p * p * p; }

        static float OutBack(float p)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            p -= 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        public static float PanelTime(int panel)
        {
            int k = Array.IndexOf(Order, panel);
            return FirstPanel + k * PanelStep;
        }

        /// <summary>Когда картинка впервые нужна на экране (заставка не идёт дальше, пока её нет).</summary>
        public static float NeedTime(int tex)
        {
            if (tex < PanelCount) return PanelTime(tex);
            if (tex >= TexLine0 && tex < TexLine0 + 3) return LineTime(tex - TexLine0);
            if (tex == TexTagline) return LogoTime + 1f;
            return LogoTime - 0.15f;
        }

        /// <summary>Моменты «ударов» для звука: появление кадров, логотип, строки.</summary>
        public static float LineTime(int i) => LogoTime + 0.4f + i * 0.2f;

        /// <summary>Кадр анимации в момент t для экрана w×h (раскладка масштабируется, если окно поменяли).</summary>
        public static List<Sprite> Frame(IntroLayout L, float w, float h, float t)
        {
            var list = new List<Sprite>(48);
            // Окно поменяло размер — обложка масштабируется целиком, без искажений
            float fk = Math.Min(w / L.Width, h / L.Height);
            float fx = (w - L.Width * fk) / 2f, fy = (h - L.Height * fk) / 2f;
            Func<Box, Box> fit = bx => new Box(bx.x * fk + fx, bx.y * fk + fy, bx.w * fk, bx.h * fk);
            Solid(list, new Box(0, 0, w, h), 0f, 0f, 0f, 1f);
            if (t >= MenuTime)
            {
                // Меню уже под заставкой — чёрный тает
                list[0] = SolidSprite(new Box(0, 0, w, h), 0f, 0f, 0f, 1f - Clamp01((t - MenuTime) / (EndTime - MenuTime)));
                return list;
            }

            // Тряска после удара логотипа
            float shake = t > LogoTime ? (float)Math.Exp(-(t - LogoTime) * 9f) * h * 0.007f : 0f;
            float ox = shake * (float)Math.Sin(t * 91f), oy = shake * (float)Math.Cos(t * 73f);

            // Тёмная подложка обложки — видны промежутки между кадрами
            float posterIn = Clamp01((t - FirstPanel + 0.15f) / 0.3f);
            if (posterIn > 0f) Solid(list, Shift(L.Poster.Fit(fit), ox, oy), 0.09f, 0.04f, 0.125f, posterIn);

            for (int i = 0; i < PanelCount; i++)
            {
                float t0 = PanelTime(i);
                if (t < t0) continue;
                var b = Shift(L.Panels[i].Fit(fit), ox, oy);
                float p = Clamp01((t - t0) / 0.38f);
                float scale = 1f + 0.12f * (1f - OutCubic(p));
                var r = Scale(b, scale);
                // Медленный наезд камеры внутри кадра
                float zoom = 1f - 0.08f * Clamp01((t - t0) / 4.5f);
                float dirX = (i % 3 - 1) * 0.5f, dirY = (i / 3 - 1) * 0.5f;
                float u = (1f - zoom) * (0.5f + dirX * 0.6f), v = (1f - zoom) * (0.5f + dirY * 0.6f);
                var sp = new Sprite { Tex = i, X = r.x, Y = r.y, W = r.w, H = r.h, U = u, V = v, UW = zoom, VH = zoom, R = 1, G = 1, B = 1, Alpha = Clamp01((t - t0) / 0.1f) };
                // Кадр не вылезает за свои границы (кроме момента «выпрыгивания»)
                if (p >= 1f) { sp.Clip = true; sp.ClipX = b.x; sp.ClipY = b.y; sp.ClipW = b.w; sp.ClipH = b.h; }
                list.Add(sp);
                float flash = 0.85f * (1f - Clamp01((t - t0) / 0.28f));
                if (flash > 0f) Solid(list, r, 1f, 1f, 1f, flash);
            }

            // Логотип
            var logo = Shift(L.Logo.Fit(fit), ox, oy);
            float glowIn = Clamp01((t - (LogoTime - 0.15f)) / 0.5f);
            if (glowIn > 0f)
            {
                float pulse = 0.85f + 0.15f * (float)Math.Sin((t - LogoTime) * 4f);
                Tex(list, TexGlow, Scale(logo, 1.15f), glowIn * pulse);
            }
            if (t >= LogoTime)
            {
                float p = Clamp01((t - LogoTime) / 0.3f);
                float scale = 1.75f - 0.75f * OutCubic(p);
                Tex(list, TexLogo, Scale(logo, scale), Clamp01(p * 3f));
                float flash = 0.9f * (1f - Clamp01((t - LogoTime - 0.28f) / 0.3f));
                if (p >= 1f && flash > 0f) Tex(list, TexShine, logo, flash);
            }
            for (int i = 0; i < 3; i++)
            {
                float t0 = LineTime(i);
                if (t < t0) continue;
                float p = Clamp01((t - t0) / 0.22f);
                var b = Shift(L.LineBoxes[i].Fit(fit), ox, oy);
                Tex(list, TexLine0 + i, Scale(b, 2.3f - 1.3f * OutCubic(p)), Clamp01(p * 2.5f));
            }

            // Блик пробегает по «92»
            float shineT = LogoTime + 1.25f;
            if (t > shineT && t < shineT + 0.75f)
            {
                float p = (t - shineT) / 0.75f;
                float bw = logo.w * 0.03f;
                float cx = logo.x - logo.w * 0.1f + p * logo.w * 1.2f;
                float[] alphas = { 0.12f, 0.3f, 0.55f, 0.3f, 0.12f };
                for (int k = 0; k < alphas.Length; k++)
                {
                    var sp = TexSprite(TexShine, logo, alphas[k]);
                    sp.Clip = true;
                    sp.ClipX = cx + (k - 2.5f) * bw; sp.ClipY = logo.y; sp.ClipW = bw; sp.ClipH = logo.h;
                    list.Add(sp);
                }
            }

            float tagT = LogoTime + 1.0f;
            if (t > tagT)
            {
                float p = Clamp01((t - tagT) / 0.4f);
                var b = Shift(L.Tagline_.Fit(fit), ox, oy + (1f - OutCubic(p)) * h * 0.02f);
                Tex(list, TexTagline, b, p);
            }

            if (t > FadeStart) Solid(list, new Box(0, 0, w, h), 0f, 0f, 0f, Clamp01((t - FadeStart) / (MenuTime - FadeStart)));
            return list;
        }

        static Box Shift(Box b, float dx, float dy) => new Box(b.x + dx, b.y + dy, b.w, b.h);

        static Box Scale(Box b, float s) => new Box(b.cx - b.w * s / 2f, b.cy - b.h * s / 2f, b.w * s, b.h * s);

        static Sprite SolidSprite(Box b, float r, float g, float bl, float a) =>
            new Sprite { Tex = -1, X = b.x, Y = b.y, W = b.w, H = b.h, UW = 1, VH = 1, R = r, G = g, B = bl, Alpha = a };

        static void Solid(List<Sprite> list, Box b, float r, float g, float bl, float a) => list.Add(SolidSprite(b, r, g, bl, a));

        static Sprite TexSprite(int tex, Box b, float a) =>
            new Sprite { Tex = tex, X = b.x, Y = b.y, W = b.w, H = b.h, UW = 1, VH = 1, R = 1, G = 1, B = 1, Alpha = a };

        static void Tex(List<Sprite> list, int tex, Box b, float a) => list.Add(TexSprite(tex, b, a));
    }
}

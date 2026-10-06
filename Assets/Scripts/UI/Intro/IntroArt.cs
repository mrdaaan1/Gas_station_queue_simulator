using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GasQueue.Intro
{
    /// <summary>
    /// Картинки заставки в стиле обложки GTA VI: девять кадров-иллюстраций из жизни очереди и логотип
    /// «92» с надписью «симулятор очереди на заправку». Всё рисуется кодом (без UnityEngine — можно в отдельном потоке).
    /// </summary>
    public class IntroArt
    {
        public IntroLayout Layout;
        public Raster[] Images;

        /// <summary>Нарисовать всё для экрана w×h и дождаться (для превью). quality &lt; 1 — картинки меньше, Unity растянет.</summary>
        public static IntroArt Build(int w, int h, float quality)
        {
            var art = Start(w, h, quality, out var jobs);
            Task.WaitAll(jobs);
            return art;
        }

        /// <summary>
        /// Начать рисовать в фоне: раскладка готова сразу, картинки появляются в <see cref="Images"/> по мере готовности.
        /// jobs[i] — задача, рисующая картинку i (у свечения и блика та же задача, что у логотипа).
        /// Кадры запускаются в порядке появления на экране, чтобы анимация могла начаться, не дожидаясь логотипа.
        /// </summary>
        public static IntroArt Start(int w, int h, float quality, out Task[] jobs)
        {
            var L = IntroLayout.Compute(w, h);
            var art = new IntroArt { Layout = L, Images = new Raster[IntroLayout.TexCount] };
            jobs = new Task[IntroLayout.TexCount];
            var panels = new int[IntroLayout.PanelCount];
            for (int i = 0; i < panels.Length; i++) panels[i] = i;
            Array.Sort(panels, (a, b) => IntroLayout.PanelTime(a).CompareTo(IntroLayout.PanelTime(b)));
            foreach (int i in panels)
            {
                int k = i;
                var b = L.Panels[k];
                jobs[k] = Task.Run(() => art.Images[k] = Panel(k, Px(b.w * quality), Px(b.h * quality)));
            }
            var logo = Task.Run(() => BuildLogo(art, L, quality));
            jobs[IntroLayout.TexLogo] = jobs[IntroLayout.TexGlow] = jobs[IntroLayout.TexShine] = logo;
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                jobs[IntroLayout.TexLine0 + k] = Task.Run(() => BuildLine(art, L, quality, k));
            }
            jobs[IntroLayout.TexTagline] = Task.Run(() => BuildTagline(art, L, quality));
            return art;
        }

        static int Px(float v) => Math.Max(8, (int)Math.Round(v));

        public static Raster Panel(int index, int w, int h)
        {
            var cv = new Cv(w, h);
            switch (index)
            {
                case 0: Pump(cv); break;
                case 1: Gauge(cv); break;
                case 2: Canister(cv); break;
                case 3: SportsCar(cv); break;
                case 4: Queue(cv); break;
                case 5: Maybach(cv); break;
                case 6: Barrier(cv); break;
                case 7: Break(cv); break;
                default: Explosion(cv); break;
            }
            cv.Vignette();
            return cv.R;
        }

        // ---------- Палитра «Вайс-Сити» ----------
        static readonly C4 Ink = C4.Hex("#170a20");
        static readonly C4 Violet = C4.Hex("#5a2a8a"), Purple = C4.Hex("#3b1d6e"), Pink = C4.Hex("#ff4f9a"), Rose = C4.Hex("#ff7aa8");
        static readonly C4 Orange = C4.Hex("#ff8a3d"), Peach = C4.Hex("#ffb35c"), Sun = C4.Hex("#ffe36b"), Teal = C4.Hex("#2fc6d8");
        static readonly C4 Red = C4.Hex("#e2323e"), DarkRed = C4.Hex("#9c1a24");

        /// <summary>
        /// Холст в «условных единицах»: высота кадра — 100, X отсчитывается от середины (влево минус).
        /// Так одна и та же сцена подходит к кадрам разной ширины.
        /// </summary>
        class Cv
        {
            public readonly Raster R;
            public readonly float U, Cx;
            public float HalfW => R.Width / U / 2f;

            public Cv(int w, int h)
            {
                R = new Raster(w, h);
                U = h / 100f;
                Cx = w / 2f;
            }

            public float X(float x) => Cx + x * U;
            public float Y(float y) => y * U;
            public Path P(Path p) => p.Map(U, Cx, 0f);

            public void Fill(Path p, C4 c) => R.Fill(P(p), c);
            public void Fill(Path p, Paint paint) => R.Fill(P(p), paint);
            public void FillEvenOdd(Path p, Paint paint) => R.Fill(P(p), paint, true);
            public void Outlined(Path p, Paint fill, C4 outline, float width) => R.FillOutlined(P(p), fill, outline, width * U);

            public Paint Lin(float x0, float y0, float x1, float y1, params C4[] stops) => Paints.Linear(X(x0), Y(y0), X(x1), Y(y1), stops);
            public Paint Rad(float cx, float cy, float r, params C4[] stops) => Paints.Radial(X(cx), Y(cy), r * U, stops);
            public void Sky(params C4[] stops) => R.FillAll(Lin(0, 0, 0, 100, stops));

            public void Glow(float cx, float cy, float r, C4 c) => Fill(Path.Circle(cx, cy, r), Rad(cx, cy, r, c, c.A(c.a * 0.35f), c.A(0f)));

            public void Text(string s, float cx, float baseline, float size, C4 fill, float tracking = 0f)
            {
                R.Fill(Path.TextCentered(s, X(cx), Y(baseline), size * U, tracking), fill);
            }

            public void TextOutlined(string s, float cx, float baseline, float size, Paint fill, C4 outline, float width, float tracking = 0f, float rotate = 0f)
            {
                var p = Path.TextCentered(s, X(cx), Y(baseline), size * U, tracking);
                if (rotate != 0f) p = p.Rotate(rotate, X(cx), Y(baseline - size * 0.35f));
                R.FillOutlined(p, fill, outline, width * U);
            }

            /// <summary>Затемнение по краям кадра — «киношность».</summary>
            public void Vignette()
            {
                float w = R.Width, h = R.Height, cx = w / 2f, cy = h / 2f;
                float r = (float)Math.Sqrt(cx * cx + cy * cy);
                R.FillAll((x, y) =>
                {
                    float d = (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                    float a = d < 0.6f ? 0f : (d - 0.6f) / 0.4f;
                    return Ink.A(a * a * 0.55f);
                });
            }
        }

        // ---------- 0. Колонка «ЛУКАВОЙЛ» ----------
        static void Pump(Cv cv)
        {
            float hw = cv.HalfW;
            cv.Sky(Teal, C4.Hex("#8fe0d8"), C4.Hex("#ffc0cf"), Rose);
            cv.Glow(hw * 0.55f, 46, 60, C4.Hex("#fff3c4", 0.85f));
            cv.Fill(Path.Rect(-hw, 86, hw * 2, 14), C4.Hex("#3a2448"));
            cv.Fill(Path.Rect(-hw, 86, hw * 2, 1.2f), C4.Hex("#8a5a8a"));

            // Дальняя колонка — силуэтом
            var far = new Path().Add(Path.RoundRect(-11, 34, 22, 52, 3)).Add(Path.Rect(-13, 30, 26, 6)).Map(0.75f, hw * 0.62f, 22f);
            cv.Fill(far, C4.Hex("#c25b8a"));
            // Опора навеса
            cv.Fill(Path.Rect(-hw * 0.82f - 4, 16, 8, 70), cv.Lin(-hw * 0.82f - 4, 0, -hw * 0.82f + 4, 0, C4.Hex("#f4f0f6"), C4.Hex("#bdb0c8")));
            cv.Fill(Path.Rect(-hw * 0.82f - 4, 60, 8, 8), Red);

            // Навес с надписью
            cv.Fill(Path.Rect(-hw, 0, hw * 2, 15), cv.Lin(0, 0, 0, 15, C4.Hex("#ff4d55"), C4.Hex("#d3202e")));
            cv.Fill(Path.Rect(-hw, 15, hw * 2, 3), C4.White);
            cv.Fill(Path.Rect(-hw, 18, hw * 2, 2.5f), C4.Hex("#7a2a5a", 0.6f));
            cv.Text("ЛУКАВОЙЛ", 0, 11.2f, 9.5f, C4.White, 0.06f);

            // Колонка
            float x0 = -30, w = 44;
            cv.Outlined(Path.RoundRect(x0, 27, w, 62, 4), cv.Lin(x0, 0, x0 + w, 0, C4.Hex("#ffffff"), C4.Hex("#e4dce8"), C4.Hex("#b9aac6")), Ink, 0.7f);
            cv.Fill(Path.RoundRect(x0 - 2, 23, w + 4, 8, 2), cv.Lin(0, 23, 0, 31, C4.Hex("#ff5560"), DarkRed));
            cv.Fill(new Path().Add(Path.RoundRect(x0, 58, w, 31, 4)).Add(Path.Rect(x0, 58, w, 8)), cv.Lin(x0, 0, x0 + w, 0, C4.Hex("#ff4a55"), Red, DarkRed));
            cv.Fill(Path.Rect(x0 + 2, 30, 2.2f, 26), C4.White.A(0.8f));
            // Табло
            cv.Fill(Path.RoundRect(x0 + 5, 34, w - 10, 15, 2), C4.Hex("#1b1424"));
            cv.Text("00,00", x0 + w / 2, 45.5f, 8.5f, C4.Hex("#ffcf4a"), 0.04f);
            // Бумажка
            var paper = Path.Rect(x0 + 6, 50.5f, w - 12, 6.5f).Rotate(-4, x0 + w / 2, 53);
            cv.Fill(paper, C4.Hex("#fffbe8"));
            cv.R.Fill(Path.TextCentered("ТОЛЬКО 20 Л", cv.X(x0 + w / 2), cv.Y(55.6f), 4.2f * cv.U).Rotate(-4, cv.X(x0 + w / 2), cv.Y(53)), C4.Hex("#c41e2a"));
            cv.Text("АИ-92", x0 + w / 2, 77.5f, 11f, C4.White);
            cv.Fill(Path.Rect(x0 - 4, 87, w + 8, 4), C4.Hex("#2a1a33"));

            // Пистолет и шланг
            float hx = x0 + w;
            cv.Fill(Path.RoundRect(hx - 1, 47, 6, 14, 1.5f), C4.Hex("#2a1a33"));
            var nozzle = Path.Poly(hx + 1, 44, hx + 7, 43, hx + 8, 52, hx + 4, 55, hx + 2, 50).Add(Path.Capsule(hx + 6, 44, hx + 14, 41, 1.4f));
            cv.Outlined(nozzle, Paints.Solid(C4.Hex("#1a1a22")), C4.Hex("#000000", 0.4f), 0.3f);
            cv.Fill(Path.Stroke(Path.CubicPoints(hx + 4, 55, hx + 18, 70, hx + 22, 96, hx - 4, 92), 1.6f), C4.Hex("#151018"));
        }

        // ---------- 1. Датчик топлива на нуле ----------
        static void Gauge(Cv cv)
        {
            cv.R.FillAll(cv.Rad(0, 62, 110, C4.Hex("#5a2470"), C4.Hex("#1f0d2c"), C4.Hex("#0b0612")));
            float cx = 0, cy = 70, r = 46;
            cv.Glow(cx, cy, r + 18, C4.Hex("#ff3d8b", 0.55f));
            cv.Fill(Path.Circle(cx, cy, r), cv.Rad(cx, cy - 10, r, C4.Hex("#34203f"), C4.Hex("#120a1a")));
            cv.Fill(Path.Ring(cx, cy, r + 1.5f, 3.2f), cv.Lin(cx - r, 0, cx + r, 0, Orange, Pink, C4.Hex("#9a5cff")));
            for (int k = 0; k <= 8; k++)
            {
                float a = (195f + k * 150f / 8f) * (float)Math.PI / 180f;
                float r0 = k % 2 == 0 ? r - 11 : r - 7;
                var c = k <= 1 ? C4.Hex("#ff3b3b") : C4.Hex("#f4e9ff");
                cv.Fill(Path.Capsule(cx + (float)Math.Cos(a) * r0, cy + (float)Math.Sin(a) * r0, cx + (float)Math.Cos(a) * (r - 3.5f), cy + (float)Math.Sin(a) * (r - 3.5f), k % 2 == 0 ? 1.1f : 0.7f), c);
            }
            cv.Text("E", cx - 35, cy + 12, 10, C4.Hex("#ff3b3b"));
            cv.Text("F", cx + 35, cy + 12, 10, C4.Hex("#f4e9ff"));

            // Лампочка бензина
            float ix = cx, iy = cy - 22;
            cv.Glow(ix, iy, 16, C4.Hex("#ffb020", 0.8f));
            var icon = new Path().Add(Path.RoundRect(ix - 4.5f, iy - 6, 7, 12, 1)).Add(Path.Rect(ix - 5.5f, iy + 5, 9, 1.8f))
                .Add(Path.Capsule(ix + 2.5f, iy - 2, ix + 5.5f, iy - 0.5f, 0.6f)).Add(Path.Capsule(ix + 5.5f, iy - 0.5f, ix + 5.5f, iy + 4, 0.6f));
            cv.Fill(icon, C4.Hex("#ffc23a"));
            cv.Fill(Path.Rect(ix - 3, iy - 4.5f, 4, 3.2f), C4.Hex("#5a3a10"));

            // Стрелка лежит на «E»
            float na = 191f * (float)Math.PI / 180f;
            cv.Glow(cx + (float)Math.Cos(na) * 30, cy + (float)Math.Sin(na) * 30, 12, C4.Hex("#ff2e3e", 0.4f));
            cv.Fill(Path.Capsule(cx, cy, cx + (float)Math.Cos(na) * (r - 6), cy + (float)Math.Sin(na) * (r - 6), 1.4f), C4.Hex("#ff2e3e"));
            cv.Fill(Path.Circle(cx, cy, 5), cv.Rad(cx - 1, cy - 1, 5, C4.Hex("#6a5a78"), C4.Hex("#1a1020")));
            // Блик на стекле
            cv.Fill(Path.Poly(-hwOf(cv), 0, -hwOf(cv) + 30, 0, -hwOf(cv) + 70, 100, -hwOf(cv) + 52, 100), C4.White.A(0.05f));
            cv.Text("РЕЗЕРВ", cx, 96, 5.5f, C4.Hex("#ffb020"), 0.25f);
        }

        static float hwOf(Cv cv) => cv.HalfW;

        // ---------- 2. Канистра втридорога ----------
        static void Canister(Cv cv)
        {
            float hw = cv.HalfW;
            cv.R.Clear(C4.Hex("#ffd04a"));
            float cx = -6, cy = 54;
            for (int k = 0; k < 18; k++)
            {
                if (k % 2 == 1) continue;
                float a0 = k * 20f * (float)Math.PI / 180f, a1 = (k + 1) * 20f * (float)Math.PI / 180f;
                cv.Fill(Path.Poly(cx, cy, cx + (float)Math.Cos(a0) * 200, cy + (float)Math.Sin(a0) * 200, cx + (float)Math.Cos(a1) * 200, cy + (float)Math.Sin(a1) * 200), C4.Hex("#ffae2e"));
            }
            cv.Glow(cx, cy, 60, C4.Hex("#fff6c0", 0.7f));

            // Канистра (ручка — три прорези, правило чёт-нечет)
            var can = new Path().Add(Path.RoundRect(-25, 20, 50, 66, 6))
                .Add(Path.RoundRect(-19, 25, 10, 8, 3)).Add(Path.RoundRect(-5, 25, 10, 8, 3)).Add(Path.RoundRect(9, 25, 10, 8, 3));
            var spout = Path.RoundRect(-27, 9, 11, 14, 2);
            float rot = -10f;
            var body = can.Rotate(rot, 0, 52).Translate(cx, 0);
            var sp = spout.Rotate(rot, 0, 52).Translate(cx, 0);
            cv.Fill(body.Translate(3, 3), Ink.A(0.35f));
            cv.Outlined(sp, Paints.Solid(C4.Hex("#ffd23a")), Ink, 1f);
            cv.R.FillOutlined(cv.P(body), Paints.Solid(Ink), Ink, 1.2f * cv.U);
            cv.FillEvenOdd(body, cv.Lin(-30, 0, 30, 0, C4.Hex("#ff5a4a"), Red, DarkRed));
            // Выпуклый крест на боку
            var x = new Path().Add(Path.Capsule(-16, 42, 16, 79, 2.4f)).Add(Path.Capsule(16, 42, -16, 79, 2.4f)).Rotate(rot, 0, 52).Translate(cx, 0);
            cv.Fill(x.Translate(0.8f, 0.8f), C4.Hex("#7a0f18"));
            cv.Fill(x, C4.Hex("#ff6a5a"));
            cv.Fill(Path.Capsule(-21, 40, -21, 80, 1.3f).Rotate(rot, 0, 52).Translate(cx, 0), C4.White.A(0.45f));

            // Ценник
            var tag = Path.RoundRect(-16, -10, 32, 20, 2.5f).Rotate(9, 0, 0).Translate(hw * 0.52f, 72);
            cv.Outlined(tag, Paints.Solid(C4.Hex("#fffdf2")), Ink, 0.8f);
            cv.R.Fill(Path.TextCentered("ЦЕНА", cv.X(hw * 0.52f), cv.Y(69), 4.5f * cv.U, 0.1f).Rotate(9, cv.X(hw * 0.52f), cv.Y(72)), C4.Hex("#444444"));
            cv.R.Fill(Path.TextCentered("×3", cv.X(hw * 0.52f + 0.5f), cv.Y(79.5f), 11f * cv.U).Rotate(9, cv.X(hw * 0.52f), cv.Y(72)), C4.Hex("#d3202e"));
            cv.Fill(Path.Stroke(Path.CubicPoints(hw * 0.52f - 13, 63, hw * 0.4f, 55, hw * 0.2f, 52, 8, 50), 0.35f), Ink);
        }

        // ---------- 3. Спорткар на закате ----------
        static void SportsCar(Cv cv)
        {
            float hw = cv.HalfW;
            var sky = cv.Lin(0, 0, 0, 80, Purple, C4.Hex("#b03a8c"), C4.Hex("#ff6f61"), Peach);
            cv.R.FillAll(sky);
            float sx = hw * 0.35f, sy = 58, sr = 30;
            cv.Glow(sx, sy, 70, C4.Hex("#ffd36b", 0.5f));
            cv.Fill(Path.Circle(sx, sy, sr), cv.Lin(0, sy - sr, 0, sy + sr, Sun, C4.Hex("#ff6f91")));
            for (int i = 0; i < 6; i++)
            {
                float y = sy + 2 + i * 4.2f, th = 0.8f + i * 0.45f;
                cv.Fill(Path.Rect(sx - sr - 1, y, sr * 2 + 2, th), sky);
            }
            City(cv, 78, C4.Hex("#2a1240"), 7);
            cv.Fill(Path.Rect(-hw, 78, hw * 2, 22), cv.Lin(0, 78, 0, 100, C4.Hex("#3a1a4a"), C4.Hex("#160a20")));
            var rnd = new Random(3);
            for (int i = 0; i < 18; i++)
            {
                float y = 81 + (float)rnd.NextDouble() * 18, x = -hw + (float)rnd.NextDouble() * hw * 2, len = 10 + (float)rnd.NextDouble() * 40;
                cv.Fill(Path.Capsule(x, y, x + len, y, 0.25f + (float)rnd.NextDouble() * 0.3f), (i % 3 == 0 ? Pink : C4.White).A(0.35f));
            }

            float L = Math.Min(hw * 1.15f, 150f), x0 = -L * 0.62f, ground = 93f;
            // Полосы скорости позади
            for (int i = 0; i < 7; i++)
            {
                float y = ground - L * (0.05f + i * 0.045f);
                cv.Fill(Path.Capsule(x0 - L * (0.25f + 0.1f * (i % 3)), y, x0 - L * 0.03f, y, 0.45f), C4.White.A(0.55f));
            }
            SideCar(cv, x0, ground, L, false, cv.Lin(0, ground - L * 0.35f, 0, ground, C4.Hex("#ffb04a"), C4.Hex("#ff5a2a"), C4.Hex("#c41e2a"), C4.Hex("#6a0f20")), true);
        }

        /// <summary>Силуэт города: панельки с окнами.</summary>
        static void City(Cv cv, float ground, C4 color, int seed)
        {
            float hw = cv.HalfW;
            var rnd = new Random(seed);
            float x = -hw - 5;
            while (x < hw)
            {
                float w = 14 + (float)rnd.NextDouble() * 22, h = 10 + (float)rnd.NextDouble() * 26;
                cv.Fill(Path.Rect(x, ground - h, w, h + 1), color);
                for (float wy = ground - h + 3; wy < ground - 3; wy += 3.4f)
                    for (float wx = x + 2; wx < x + w - 2; wx += 3.2f)
                        if (rnd.NextDouble() < 0.28) cv.Fill(Path.Rect(wx, wy, 1.6f, 1.6f), C4.Hex("#ffcf6b", 0.55f + (float)rnd.NextDouble() * 0.4f));
                x += w + 1 + (float)rnd.NextDouble() * 6;
            }
        }

        /// <summary>
        /// Машина сбоку. Профиль задан от зада (0) к носу (1), высота — в долях длины.
        /// faceLeft — нос влево; sport — низкий спорткар с антикрылом, иначе длинный седан.
        /// </summary>
        static void SideCar(Cv cv, float x0, float ground, float L, bool faceLeft, Paint paint, bool sport)
        {
            float[] prof = sport
                ? new float[] { 0.02f, 0.06f, 0.0f, 0.12f, 0.01f, 0.2f, 0.09f, 0.215f, 0.22f, 0.225f, 0.31f, 0.235f, 0.42f, 0.335f, 0.55f, 0.345f, 0.66f, 0.255f, 0.82f, 0.21f, 0.965f, 0.165f, 1f, 0.12f, 0.985f, 0.065f, 0.9f, 0.055f, 0.1f, 0.055f }
                : new float[] { 0.005f, 0.07f, 0f, 0.19f, 0.02f, 0.225f, 0.2f, 0.235f, 0.27f, 0.25f, 0.38f, 0.325f, 0.62f, 0.33f, 0.72f, 0.255f, 0.93f, 0.225f, 0.995f, 0.2f, 1f, 0.1f, 0.985f, 0.065f, 0.9f, 0.055f, 0.1f, 0.055f };
            float[] glass = sport
                ? new float[] { 0.33f, 0.245f, 0.43f, 0.322f, 0.545f, 0.332f, 0.635f, 0.262f }
                : new float[] { 0.29f, 0.248f, 0.39f, 0.315f, 0.61f, 0.32f, 0.70f, 0.255f };
            float wr = sport ? 0.082f : 0.075f;
            float[] wheels = sport ? new[] { 0.2f, 0.8f } : new[] { 0.18f, 0.82f };

            Func<float, float> X = t => faceLeft ? x0 + (1f - t) * L : x0 + t * L;
            Func<float, float> Y = hgt => ground - hgt * L;
            Func<float[], Path> poly = pts =>
            {
                var p = new Path();
                for (int i = 0; i + 1 < pts.Length; i += 2) p.Line(X(pts[i]), Y(pts[i + 1]));
                return p.Close();
            };

            cv.Fill(Path.Ellipse(X(0.5f), ground + 0.5f, L * 0.55f, 2.2f), Ink.A(0.55f));
            var body = poly(prof);
            cv.Outlined(body, paint, Ink, 0.6f);
            // Блик по борту и линия дверей
            cv.Fill(Path.Capsule(X(0.04f), Y(0.165f), X(0.95f), Y(0.15f), 0.012f * L), C4.White.A(0.35f));
            cv.Fill(Path.Capsule(X(0.06f), Y(0.2f), X(0.9f), Y(0.19f), 0.004f * L), C4.White.A(0.25f));
            cv.Fill(Path.Capsule(X(sport ? 0.45f : 0.42f), Y(0.07f), X(sport ? 0.44f : 0.41f), Y(0.23f), 0.0025f * L), Ink.A(0.6f));
            if (!sport) cv.Fill(Path.Capsule(X(0.62f), Y(0.07f), X(0.62f), Y(0.24f), 0.0025f * L), Ink.A(0.6f));
            // Стёкла
            cv.Fill(poly(glass), cv.Lin(0, Y(0.33f), 0, Y(0.24f), C4.Hex("#5a4a9a"), C4.Hex("#1b1030")));
            cv.Fill(Path.Poly(X(glass[0] + 0.05f), Y(0.25f), X(glass[0] + 0.1f), Y(0.3f), X(glass[0] + 0.13f), Y(0.3f), X(glass[0] + 0.08f), Y(0.25f)), C4.White.A(0.3f));
            cv.Fill(Path.Capsule(X((glass[2] + glass[4]) / 2f - 0.02f), Y(0.33f), X((glass[0] + glass[6]) / 2f + 0.02f), Y(0.25f), 0.006f * L), Ink.A(0.85f));

            if (sport)
            {
                // Антикрыло
                cv.Outlined(Path.Capsule(X(0.015f), Y(0.265f), X(0.17f), Y(0.27f), 0.008f * L), Paints.Solid(C4.Hex("#1a1a22")), Ink, 0.3f);
                cv.Fill(Path.Capsule(X(0.06f), Y(0.225f), X(0.07f), Y(0.262f), 0.005f * L), C4.Hex("#1a1a22"));
                cv.Fill(Path.Capsule(X(0.13f), Y(0.228f), X(0.135f), Y(0.265f), 0.005f * L), C4.Hex("#1a1a22"));
            }
            // Фары и фонари
            cv.Fill(Path.Capsule(X(0.93f), Y(0.16f), X(0.985f), Y(0.135f), 0.012f * L), C4.Hex("#fff6d0"));
            cv.Fill(Path.Capsule(X(0.005f), Y(0.17f), X(0.03f), Y(0.17f), 0.013f * L), C4.Hex("#ff2a3a"));
            cv.Glow(X(0.01f), Y(0.17f), 0.06f * L, C4.Hex("#ff2a3a", 0.6f));

            foreach (float wx in wheels)
            {
                float cx = X(wx), cy = Y(wr);
                cv.Fill(Path.Circle(cx, cy, wr * L * 1.12f), Ink);
                cv.Fill(Path.Circle(cx, cy, wr * L), C4.Hex("#18141c"));
                cv.Fill(Path.Circle(cx, cy, wr * L * 0.66f), cv.Lin(cx, cy - wr * L, cx, cy + wr * L, C4.Hex("#f2f2f6"), C4.Hex("#9a98a8"), C4.Hex("#5a5866")));
                int spokes = sport ? 5 : 10;
                for (int k = 0; k < spokes; k++)
                {
                    float a = k * 2f * (float)Math.PI / spokes + 0.3f;
                    cv.Fill(Path.Capsule(cx + (float)Math.Cos(a) * wr * L * 0.18f, cy + (float)Math.Sin(a) * wr * L * 0.18f,
                        cx + (float)Math.Cos(a) * wr * L * 0.58f, cy + (float)Math.Sin(a) * wr * L * 0.58f, wr * L * (sport ? 0.09f : 0.05f)), C4.Hex("#2a2830"));
                }
                cv.Fill(Path.Circle(cx, cy, wr * L * 0.14f), C4.Hex("#d0d0d8"));
                if (sport) cv.Fill(Path.Rect(cx - wr * L * 0.5f, cy - wr * L * 0.42f, wr * L * 0.22f, wr * L * 0.3f), C4.Hex("#e01e2a"));
            }
        }

        // ---------- 4. Бесконечная очередь ----------
        static void Queue(Cv cv)
        {
            float hw = cv.HalfW;
            float vx = hw * 0.08f, vy = 44f;
            cv.R.FillAll(cv.Lin(0, 0, 0, vy, C4.Hex("#3b1d6e"), C4.Hex("#b0367e"), C4.Hex("#ff7a5c"), C4.Hex("#ffc46b")));
            cv.Glow(vx, vy - 2, 40, C4.Hex("#ffe08a", 0.8f));
            cv.Fill(Path.Circle(vx, vy - 1, 11), cv.Lin(0, vy - 12, 0, vy, C4.Hex("#fff3b0"), C4.Hex("#ffb35c")));
            cv.Fill(Path.Rect(-hw, vy, hw * 2, 100 - vy), C4.Hex("#2b1838"));
            // Дорога
            cv.Fill(Path.Poly(vx - 1.5f, vy, vx + 1.5f, vy, hw * 1.25f, 100, -hw * 0.95f, 100), cv.Lin(0, vy, 0, 100, C4.Hex("#6a4a6a"), C4.Hex("#3a2a48"), C4.Hex("#241830")));
            for (int i = 1; i < 14; i++)
            {
                float s0 = 1f / (1f + i * 0.9f), s1 = 1f / (1f + (i + 0.45f) * 0.9f);
                float ex = hw * 0.15f;
                cv.Fill(Path.Poly(vx + ex * s0 - 0.6f * s0, vy + 56 * s0, vx + ex * s0 + 0.6f * s0, vy + 56 * s0, vx + ex * s1 + 0.6f * s1, vy + 56 * s1, vx + ex * s1 - 0.6f * s1, vy + 56 * s1), C4.White.A(0.6f));
            }
            Facade(cv, -hw, vx - 10, vy, true, 11);
            Facade(cv, hw, vx + 12, vy, false, 12);

            // Стела вдали
            cv.Fill(Path.Rect(vx + 9, vy - 13, 0.8f, 13), C4.Hex("#2a1240"));
            cv.Fill(Path.Rect(vx + 5.5f, vy - 17, 8, 5), Red);

            // Машины очереди — от дальней к ближней
            var rnd = new Random(5);
            C4[] paints = { C4.Hex("#e3dccb"), C4.Hex("#3a6ea5"), C4.Hex("#b23a3a"), C4.Hex("#2e2e36"), C4.Hex("#d8d8d8"), C4.Hex("#6b8e23"), C4.Hex("#f2c81a"), C4.Hex("#8a6a4a") };
            var haze = C4.Hex("#d86a8a");
            for (int i = 16; i >= 0; i--)
            {
                float d = 1f + i * 0.85f, s = 1f / d;
                float cx = vx + hw * 0.42f * s, yb = vy + 56f * s;
                var paint = C4.Lerp(paints[rnd.Next(paints.Length)], haze, Math.Min(0.75f, (1f - s) * 0.85f));
                RearCar(cv, cx, yb, 44f * s, paint, Math.Min(1f, 0.3f + s));
            }
        }

        /// <summary>Стена панельки в перспективе: от края кадра (edgeX) к точке схода.</summary>
        static void Facade(Cv cv, float edgeX, float nearVpX, float vy, bool left, int seed)
        {
            float topNear = 2f, botNear = 100f, topFar = vy - 9f, botFar = vy + 1f;
            var wall = Path.Poly(edgeX, topNear, nearVpX, topFar, nearVpX, botFar, edgeX, botNear);
            cv.Fill(wall, cv.Lin(edgeX, 0, nearVpX, 0, C4.Hex(left ? "#3a1a52" : "#46205a"), C4.Hex("#7a3a6a")));
            var rnd = new Random(seed);
            // Окна: колонки по перспективе (ближе — шире)
            for (int c = 0; c < 18; c++)
            {
                float t0 = 1f - 1f / (1f + c * 0.35f), t1 = 1f - 1f / (1f + (c + 0.55f) * 0.35f);
                t0 /= 0.87f; t1 /= 0.87f;
                if (t1 > 1f) break;
                for (int r = 0; r < 9; r++)
                {
                    if (rnd.NextDouble() < 0.4) continue;
                    float f0 = 0.08f + r * 0.1f, f1 = f0 + 0.05f;
                    var p = new Path();
                    foreach (var (t, f) in new[] { (t0, f0), (t1, f0), (t1, f1), (t0, f1) })
                    {
                        float x = edgeX + (nearVpX - edgeX) * t;
                        float top = topNear + (topFar - topNear) * t, bot = botNear + (botFar - botNear) * t;
                        p.Line(x, top + (bot - top) * f);
                    }
                    cv.Fill(p.Close(), C4.Hex(rnd.NextDouble() < 0.5 ? "#ffcf6b" : "#ff9a6b", 0.35f + (float)rnd.NextDouble() * 0.5f));
                }
            }
        }

        /// <summary>Машина сзади: кузов, кабина, красные фонари.</summary>
        static void RearCar(Cv cv, float cx, float yb, float w, C4 paint, float lightAlpha)
        {
            var shade = C4.Lerp(paint, Ink, 0.45f);
            cv.Fill(Path.Ellipse(cx, yb, w * 0.55f, w * 0.05f), Ink.A(0.5f));
            cv.Fill(Path.Rect(cx - w * 0.44f, yb - w * 0.1f, w * 0.18f, w * 0.1f), Ink);
            cv.Fill(Path.Rect(cx + w * 0.26f, yb - w * 0.1f, w * 0.18f, w * 0.1f), Ink);
            cv.Fill(Path.Poly(cx - w * 0.38f, yb - w * 0.38f, cx - w * 0.3f, yb - w * 0.64f, cx + w * 0.3f, yb - w * 0.64f, cx + w * 0.38f, yb - w * 0.38f), shade);
            cv.Fill(Path.Poly(cx - w * 0.31f, yb - w * 0.4f, cx - w * 0.26f, yb - w * 0.6f, cx + w * 0.26f, yb - w * 0.6f, cx + w * 0.31f, yb - w * 0.4f), C4.Lerp(C4.Hex("#2a2040"), paint, 0.15f));
            cv.Fill(Path.RoundRect(cx - w * 0.5f, yb - w * 0.4f, w, w * 0.33f, w * 0.06f), cv.Lin(0, yb - w * 0.4f, 0, yb - w * 0.07f, paint, shade));
            cv.Fill(Path.Rect(cx - w * 0.1f, yb - w * 0.22f, w * 0.2f, w * 0.07f), C4.Hex("#f4f4f4"));
            var red = C4.Hex("#ff2a3a");
            cv.Fill(Path.Rect(cx - w * 0.47f, yb - w * 0.34f, w * 0.15f, w * 0.08f), red);
            cv.Fill(Path.Rect(cx + w * 0.32f, yb - w * 0.34f, w * 0.15f, w * 0.08f), red);
            cv.Glow(cx - w * 0.4f, yb - w * 0.3f, w * 0.3f, red.A(0.55f * lightAlpha));
            cv.Glow(cx + w * 0.4f, yb - w * 0.3f, w * 0.3f, red.A(0.55f * lightAlpha));
        }

        // ---------- 5. Депутат на «Майбахе» с мигалкой ----------
        static void Maybach(Cv cv)
        {
            float hw = cv.HalfW;
            cv.Sky(C4.Hex("#0b0a2a"), C4.Hex("#2a1550"), C4.Hex("#4a1f5e"), C4.Hex("#1a0c26"));
            var rnd = new Random(9);
            C4[] bokeh = { Pink, Teal, Sun, C4.Hex("#9a7aff") };
            for (int i = 0; i < 26; i++)
            {
                float x = -hw + (float)rnd.NextDouble() * hw * 2, y = 10 + (float)rnd.NextDouble() * 60, r = 2 + (float)rnd.NextDouble() * 6;
                cv.Glow(x, y, r, bokeh[i % bokeh.Length].A(0.35f));
            }
            City(cv, 80, C4.Hex("#150a26"), 21);
            cv.Fill(Path.Rect(-hw, 80, hw * 2, 20), cv.Lin(0, 80, 0, 100, C4.Hex("#1e1430"), C4.Hex("#0a0612")));

            float L = Math.Min(hw * 1.3f, 175f), x0 = -L * 0.42f, ground = 94f;
            // Мигалка заливает всё синим
            float lx = x0 + (1f - 0.55f) * L, ly = ground - 0.335f * L;
            cv.Glow(lx, ly, 70, C4.Hex("#2a7aff", 0.55f));
            for (int k = 0; k < 10; k++)
            {
                float a = (k * 36f + 8f) * (float)Math.PI / 180f;
                cv.Fill(Path.Poly(lx, ly, lx + (float)Math.Cos(a) * 120, ly + (float)Math.Sin(a) * 120, lx + (float)Math.Cos(a + 0.08f) * 120, ly + (float)Math.Sin(a + 0.08f) * 120), C4.Hex("#5aa8ff", 0.12f));
            }
            // Полосы скорости позади (машина едет влево)
            for (int i = 0; i < 6; i++)
            {
                float y = ground - L * (0.05f + i * 0.05f);
                cv.Fill(Path.Capsule(x0 + L * 1.03f, y, x0 + L * (1.2f + 0.08f * (i % 3)), y, 0.4f), C4.White.A(0.45f));
            }
            // Свет фар
            cv.Fill(Path.Poly(x0 + 0.01f * L, ground - 0.15f * L, -hw, ground - 0.3f * L, -hw, ground + 2), C4.Hex("#fff6d0", 0.12f));
            SideCar(cv, x0, ground, L, true, cv.Lin(0, ground - L * 0.34f, 0, ground, C4.Hex("#4a4a5a"), C4.Hex("#16161e"), C4.Hex("#050508")), false);
            // Хром и отражения мигалки
            cv.Fill(Path.Capsule(x0 + 0.03f * L, ground - 0.13f * L, x0 + 0.97f * L, ground - 0.13f * L, 0.003f * L), C4.Hex("#dcdcec"));
            cv.Fill(Path.Capsule(x0 + 0.1f * L, ground - 0.215f * L, x0 + 0.75f * L, ground - 0.225f * L, 0.004f * L), C4.Hex("#5aa8ff", 0.5f));
            cv.Fill(Path.Rect(x0 - 0.002f * L, ground - 0.19f * L, 0.012f * L, 0.08f * L), C4.Hex("#c8c8d8"));
            // Мигалка
            cv.Fill(Path.RoundRect(lx - 0.035f * L, ly - 0.03f * L, 0.07f * L, 0.03f * L, 0.01f * L), C4.Hex("#7ac0ff"));
            cv.Glow(lx, ly - 0.015f * L, 14, C4.Hex("#cfe6ff", 0.9f));
        }

        // ---------- 6. Шлагбаум «БЕНЗИНА НЕТ» ----------
        static void Barrier(Cv cv)
        {
            float hw = cv.HalfW;
            cv.Sky(C4.Hex("#4a2470"), C4.Hex("#c04a8a"), C4.Hex("#ff8a5c"), C4.Hex("#ffc46b"));
            cv.Glow(hw * 0.3f, 70, 50, C4.Hex("#ffe08a", 0.6f));
            City(cv, 84, C4.Hex("#5a2a6a"), 31);
            cv.Fill(Path.Rect(-hw, 84, hw * 2, 16), C4.Hex("#2b1838"));
            for (int i = 0; i < 6; i++) cv.Fill(Path.Rect(-hw + i * 22, 91, 11, 1.5f), C4.White.A(0.6f));

            // Будка
            float bx = -hw + 4;
            cv.Outlined(Path.Rect(bx, 34, 26, 52), cv.Lin(bx, 0, bx + 26, 0, C4.Hex("#f4f0f6"), C4.Hex("#b8a8c8")), Ink, 0.5f);
            cv.Fill(Path.Rect(bx - 2, 30, 30, 5), Red);
            cv.Fill(Path.Rect(bx + 3, 42, 20, 16), cv.Lin(0, 42, 0, 58, C4.Hex("#7ad8ff"), C4.Hex("#3a6ab0")));
            cv.Fill(Path.Rect(bx + 5, 46, 10, 9), C4.Hex("#fffbe8"));
            cv.Text("ЗАВОЗ", bx + 10, 50, 2.6f, C4.Hex("#c41e2a"));
            cv.Text("ЧЕРЕЗ ЧАС", bx + 10, 53.5f, 2.1f, C4.Hex("#333333"));

            // Стойка и стрела
            float px = bx + 30, py = 62;
            cv.Outlined(Path.Rect(px - 2.5f, py, 5, 24), Paints.Solid(C4.Hex("#ffcf3a")), Ink, 0.4f);
            float armEnd = hw + 5;
            cv.Outlined(Path.Capsule(px, py, armEnd, py, 2.2f), Paints.Solid(C4.White), Ink, 0.5f);
            for (float x = px + 5; x < armEnd; x += 12) cv.Fill(Path.Poly(x, py - 2.2f, x + 6, py - 2.2f, x + 4, py + 2.2f, x - 2, py + 2.2f), Red);
            cv.Fill(Path.Circle(px, py, 3.2f), C4.Hex("#2a1a33"));

            // Табличка
            float sx = (px + armEnd) / 2f + 2, sy = 66;
            cv.Fill(Path.Capsule(sx - 14, py + 1, sx - 13, sy + 1, 0.3f), Ink);
            cv.Fill(Path.Capsule(sx + 14, py + 1, sx + 13, sy + 1, 0.3f), Ink);
            var plate = Path.RoundRect(sx - 20, sy, 40, 19, 2).Rotate(3, sx, sy);
            cv.Outlined(plate, cv.Lin(0, sy, 0, sy + 19, C4.Hex("#ff4a55"), C4.Hex("#c41e2a")), C4.White, 0.9f);
            cv.R.Fill(Path.TextCentered("БЕНЗИНА", cv.X(sx), cv.Y(sy + 8), 6.4f * cv.U, 0.04f).Rotate(3, cv.X(sx), cv.Y(sy)), C4.White);
            cv.R.Fill(Path.TextCentered("НЕТ", cv.X(sx), cv.Y(sy + 16.8f), 8.5f * cv.U, 0.08f).Rotate(3, cv.X(sx), cv.Y(sy)), C4.Hex("#ffe36b"));
        }

        // ---------- 7. Касса: «ПЕРЕРЫВ» ----------
        static void Break(Cv cv)
        {
            float hw = cv.HalfW;
            cv.R.FillAll(cv.Lin(0, 0, 0, 100, C4.Hex("#3ac0bc"), C4.Hex("#1f8a96")));
            for (float x = -hw; x < hw; x += 8) cv.Fill(Path.Rect(x, 0, 0.5f, 100), C4.White.A(0.18f));
            for (float y = 0; y < 100; y += 8) cv.Fill(Path.Rect(-hw, y, hw * 2, 0.5f), C4.White.A(0.18f));
            cv.Glow(0, 0, 60, C4.Hex("#fff3c4", 0.45f));

            cv.Outlined(Path.RoundRect(-22, 3, 44, 11, 1.5f), Paints.Solid(C4.Hex("#1f4fb0")), C4.White, 0.6f);
            cv.Text("КАССА", 0, 11.8f, 7.5f, C4.White, 0.08f);
            cv.Fill(Path.RoundRect(-46, 17, 92, 62, 3), C4.Hex("#2c2c34"));
            for (float y = 21; y < 75; y += 3.2f) cv.Fill(Path.Rect(-42, y, 84, 2.1f), cv.Lin(-42, 0, 42, 0, C4.Hex("#e8e2d0"), C4.Hex("#c8bea8")));

            var paper = Path.Rect(-29, 27, 58, 36).Rotate(-5, 0, 45);
            cv.Fill(paper.Translate(1, 1.2f), Ink.A(0.3f));
            cv.Fill(paper, C4.Hex("#fffdf4"));
            cv.R.Fill(Path.TextCentered("ПЕРЕРЫВ", cv.X(0), cv.Y(43), 10 * cv.U).Rotate(-5, cv.X(0), cv.Y(45)), C4.Hex("#d3202e"));
            cv.R.Fill(Path.TextCentered("15 МИН", cv.X(0), cv.Y(56), 8 * cv.U).Rotate(-5, cv.X(0), cv.Y(45)), C4.Hex("#2a2a33"));
            cv.Fill(Path.Rect(-34, 24, 12, 4).Rotate(-40, -28, 26), C4.Hex("#ffe58a", 0.75f));
            cv.Fill(Path.Rect(22, 58, 12, 4).Rotate(-40, 28, 60), C4.Hex("#ffe58a", 0.75f));
            cv.Fill(Path.Rect(-52, 79, 104, 5), cv.Lin(0, 79, 0, 84, C4.Hex("#d8d8e0"), C4.Hex("#7a7a88")));

            // Часы
            float cx = Math.Min(hw - 12, 64), cy = 34;
            cv.Outlined(Path.Circle(cx, cy, 10), Paints.Solid(C4.White), C4.Hex("#2c2c34"), 1.2f);
            cv.Fill(Path.Capsule(cx, cy, cx, cy - 7, 0.6f), C4.Hex("#2c2c34"));
            cv.Fill(Path.Capsule(cx, cy, cx + 5, cy, 0.8f), C4.Hex("#2c2c34"));
        }

        // ---------- 8. БА-БАХ ----------
        static void Explosion(Cv cv)
        {
            float hw = cv.HalfW;
            float cx = 0, cy = 46;
            cv.R.FillAll(cv.Rad(cx, cy, 90, C4.Hex("#fff3a0"), C4.Hex("#ffb23a"), C4.Hex("#ff5a2a"), C4.Hex("#8a1a3a"), C4.Hex("#2a0a2a")));
            for (int k = 0; k < 14; k++)
            {
                float a = k * (float)Math.PI * 2f / 14f;
                cv.Fill(Path.Poly(cx, cy, cx + (float)Math.Cos(a) * 140, cy + (float)Math.Sin(a) * 140, cx + (float)Math.Cos(a + 0.1f) * 140, cy + (float)Math.Sin(a + 0.1f) * 140), C4.Hex("#fff6c0", 0.22f));
            }
            var rnd = new Random(77);
            // Дым → огонь → ядро
            Puffs(cv, rnd, cx, cy, 30, 46, 10, 18, C4.Hex("#3a1a2a"), C4.Hex("#5a2a3a"));
            Puffs(cv, rnd, cx, cy, 22, 30, 9, 15, C4.Hex("#d3202e"), C4.Hex("#ff5a2a"));
            Puffs(cv, rnd, cx, cy, 16, 18, 8, 12, Orange, C4.Hex("#ffb23a"));
            Puffs(cv, rnd, cx, cy, 10, 8, 7, 10, Sun, C4.Hex("#fff6d0"));
            // Обломки
            for (int i = 0; i < 12; i++)
            {
                float a = (float)(rnd.NextDouble() * Math.PI * 2), d = 34 + (float)rnd.NextDouble() * 30;
                float x = cx + (float)Math.Cos(a) * d, y = cy + (float)Math.Sin(a) * d * 0.8f, s = 1.5f + (float)rnd.NextDouble() * 3;
                cv.Fill(Path.Rect(x - s, y - s * 0.5f, s * 2, s).Rotate((float)rnd.NextDouble() * 180, x, y), Ink);
            }
            // Сорванный навес и колонка
            cv.Fill(Path.Rect(-hw - 5, 70, hw * 2 + 10, 7).Rotate(-7, 0, 73), Ink);
            cv.Fill(Path.Rect(-hw - 5, 76, hw * 2 + 10, 2).Rotate(-7, 0, 73), Red);
            cv.Fill(Path.Rect(-hw * 0.6f, 74, 4, 30).Rotate(8, -hw * 0.6f, 100), Ink);
            cv.Fill(Path.Rect(hw * 0.55f, 72, 4, 30).Rotate(-12, hw * 0.55f, 100), Ink);
            cv.Fill(Path.RoundRect(-8, 86, 16, 20, 2).Rotate(-25, 0, 95), Ink);
            cv.TextOutlined("БАХ!", 2, 30, 24, cv.Lin(0, 10, 0, 32, C4.Hex("#fff6a0"), C4.Hex("#ffd23a")), Ink, 1.6f, 0.02f, -9f);
        }

        static void Puffs(Cv cv, Random rnd, float cx, float cy, int count, float spread, float rMin, float rMax, C4 c0, C4 c1)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (float)(rnd.NextDouble() * Math.PI * 2), d = (float)Math.Sqrt(rnd.NextDouble()) * spread;
                float x = cx + (float)Math.Cos(a) * d, y = cy + (float)Math.Sin(a) * d * 0.75f;
                float r = rMin + (float)rnd.NextDouble() * (rMax - rMin);
                cv.Fill(Path.Circle(x, y, r), cv.Rad(x - r * 0.3f, y - r * 0.3f, r * 1.3f, c1, c0));
            }
        }

        // ---------- Логотип ----------
        public static void BuildLogo(IntroArt art, IntroLayout L, float q)
        {
            var b = L.Logo;
            int w = Px(b.w * q), h = Px(b.h * q);
            float size = L.LogoSize * q;
            float width = Path.TextWidth("92", size);
            float baseline = h / 2f + size * 0.36f;
            var digits = Path.Text("92", (w - width) / 2f, baseline, size, 0f);

            var logo = new Raster(w, h);
            float ow = size * 0.022f;
            // Тёмная обводка с тенью, светлый кант сверху-слева, градиент как у «VI»
            logo.FillOutlined(digits, Paints.Solid(Ink), Ink, ow, size * 0.02f, size * 0.025f);
            logo.Fill(digits.Translate(-size * 0.006f, -size * 0.006f), C4.Hex("#fff0f6"));
            logo.Fill(digits.Translate(size * 0.004f, size * 0.004f), Paints.Linear(w * 0.1f, 0, w * 0.9f, h,
                C4.Hex("#5a6cff"), C4.Hex("#b34cff"), C4.Hex("#ff4f9a"), C4.Hex("#ff8a5c"), C4.Hex("#ffd36b")));
            // Мягкий блик в верхней половине
            logo.Fill(digits.Translate(size * 0.004f, size * 0.004f), (x, y) => C4.White.A(Math.Max(0f, 0.28f - y / h * 0.5f)));
            art.Images[IntroLayout.TexLogo] = logo;

            var glow = logo.Silhouette(C4.Hex("#ff3d9a", 0.9f));
            glow.Blur(Math.Max(2, (int)(size * 0.05f)));
            art.Images[IntroLayout.TexGlow] = glow;
            art.Images[IntroLayout.TexShine] = logo.Silhouette(C4.White);

        }

        static void BuildLine(IntroArt art, IntroLayout L, float q, int i)
        {
            var lb = L.LineBoxes[i];
            int lw = Px(lb.w * q), lh = Px(lb.h * q);
            float s = L.LineSize * q;
            var r = new Raster(lw, lh);
            var p = Path.TextCentered(IntroLayout.Lines[i], lw / 2f, lh * 0.72f, s, 0.01f);
            r.FillOutlined(p, Paints.Linear(0, lh * 0.2f, 0, lh * 0.8f, C4.White, C4.Hex("#ffe6f0")), Ink, s * 0.055f, s * 0.03f, s * 0.04f);
            art.Images[IntroLayout.TexLine0 + i] = r;
        }

        static void BuildTagline(IntroArt art, IntroLayout L, float q)
        {
            var tb = L.Tagline_;
            int tw = Px(tb.w * q), th = Px(tb.h * q);
            float ts = L.LineSize * 0.3f * q;
            var tag = new Raster(tw, th);
            tag.FillOutlined(Path.TextCentered(IntroLayout.Tagline, tw / 2f, th * 0.75f, ts, 0.12f), Paints.Solid(C4.White), Ink, ts * 0.09f);
            art.Images[IntroLayout.TexTagline] = tag;
        }
    }
}

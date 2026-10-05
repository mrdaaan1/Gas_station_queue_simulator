using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «Хонда S2000» (2001) Суки из «Двойного форсажа»: розовый родстер без крыши.
    /// Над салоном — только рамка лобового стекла; за сиденьями чёрные дуги безопасности и мягкая
    /// крышка сложенного тента. Белые завитки на капоте и крыльях, пурпурные «пряди» с искорками на дверях,
    /// большое розовое крыло на треугольных стойках, серебристые многоспицевые диски.
    /// Руль слева. Салон: чёрная торпеда, руль «Спарко», цифровой щиток, сиденья и двери в розовом меху.
    /// Размеры: 4,14 × 1,75 × 1,29 м (с поднятым тентом), база 2,40 м.
    /// </summary>
    public class S2000Model : SportsCarModel
    {
        public const float AxleF = 1.27f, AxleR = -1.13f;
        public const float WheelR = 0.315f, Track = 0.74f;
        public const float DoorFront = 0.50f, DoorRear = -0.62f;
        const float Tonneau = -0.9f;

        public static readonly Vector3 DriverEyes = new Vector3(-0.36f, 1.0f, -0.56f);
        public static readonly Vector3 SteeringPos = new Vector3(-0.36f, 0.79f, -0.08f);
        public const float SteeringTilt = -62f;

        static Model cached;

        public static Model Get()
        {
            if (cached == null) cached = new S2000Model().Build();
            return cached;
        }

        public static S2000Model Shape() => new S2000Model();

        protected override Vector3 Eyes => DriverEyes;

        S2000Model()
        {
            Front = 2.07f; Rear = -2.07f;
            Cowl = 0.55f; Header = 0.02f; RoofRear = -1.0f; Deck = -1.2f;
            FrontRound = 1.55f; RearRound = -1.65f; FrontPow = 2.3f; RearPow = 3.0f;

            W0 = new Curve().Key(-2.07f, 0.85f).Key(-1.6f, 0.875f).Key(-1.13f, 0.88f).Key(-0.6f, 0.865f).Key(0f, 0.86f)
                .Key(0.6f, 0.862f).Key(1.27f, 0.872f).Key(1.7f, 0.86f).Key(2.07f, 0.83f);
            YBot = new Curve().Key(-2.07f, 0.34f).Key(-1.97f, 0.24f).Key(-1.8f, 0.17f).Key(-1.5f, 0.14f).Key(0f, 0.12f)
                .Key(1.6f, 0.12f).Key(1.95f, 0.10f).Key(2.03f, 0.12f).Key(2.07f, 0.19f);
            YMax = new Curve().Key(-2.07f, 0.52f).Key(-1.5f, 0.50f).Key(0f, 0.46f).Key(1.5f, 0.42f).Key(2.07f, 0.38f);
            YBelt = new Curve().Key(-2.07f, 0.80f).Key(-1.97f, 0.85f).Key(-1.75f, 0.885f).Key(-1.4f, 0.9f).Key(-1.0f, 0.895f)
                .Key(-0.4f, 0.885f).Key(0.2f, 0.865f).Key(Cowl, 0.83f).Key(0.9f, 0.78f).Key(1.27f, 0.745f).Key(1.6f, 0.70f)
                .Key(1.85f, 0.65f).Key(2.0f, 0.59f).Key(2.07f, 0.52f);
            FBelt = new Curve().Key(-2.07f, 0.8f).Key(-1.7f, 0.86f).Key(-1.2f, 0.92f).Key(-0.5f, 0.94f).Key(0.3f, 0.94f)
                .Key(0.7f, 0.88f).Key(1.1f, 0.84f).Key(2.07f, 0.8f);
            YTop = new Curve().Key(-2.07f, 0.81f).Key(-2.0f, 0.86f).Key(-1.85f, 0.895f).Key(-1.6f, 0.91f).Key(Deck, 0.912f, true)
                .Key(-1.1f, 1.0f).Key(RoofRear, 1.12f, true).Key(-0.5f, 1.15f).Key(Header, 1.13f, true).Key(0.3f, 1.0f)
                .Key(Cowl, 0.845f, true).Key(0.9f, 0.795f).Key(1.27f, 0.765f).Key(1.6f, 0.72f).Key(1.85f, 0.67f).Key(2.0f, 0.61f)
                .Key(2.07f, 0.53f);
            YRoofEdge = new Curve().Key(Deck, 0.902f, true).Key(-1.1f, 0.96f).Key(RoofRear, 1.08f, true).Key(-0.5f, 1.105f)
                .Key(Header, 1.085f, true).Key(0.3f, 0.97f).Key(Cowl, 0.83f, true);
            XRoofEdge = new Curve().Key(Deck, 0.81f, true).Key(-1.1f, 0.72f).Key(RoofRear, 0.64f, true).Key(-0.5f, 0.64f)
                .Key(Header, 0.64f, true).Key(Cowl, 0.81f, true);
            AngC = new Curve().Key(-2.07f, 160f).Key(-1.7f, 150f).Key(-1.45f, 115f).Key(-1.2f, 100f).Key(0.2f, 100f)
                .Key(0.6f, 135f).Key(0.9f, 155f).Key(2.07f, 160f);
            AngE0 = new Curve().Key(-2.07f, 172f).Key(-1.5f, 170f).Key(-1.25f, 158f).Key(-1.0f, 150f).Key(Header, 150f)
                .Key(0.3f, 158f).Key(Cowl, 170f).Key(2.07f, 172f);
            AngD0 = new Curve().Key(-1.5f, 140f).Key(-1.2f, 118f).Key(-0.8f, 112f).Key(0.3f, 112f).Key(Cowl, 120f);
            AngD1 = new Curve().Key(-1.5f, 150f).Key(-1.2f, 135f).Key(-0.8f, 122f).Key(0.3f, 122f).Key(Cowl, 130f);

            arches.Add(new Arch { z = AxleF, y = 0.315f, r = 0.365f, xMin = 0.56f });
            arches.Add(new Arch { z = AxleR, y = 0.315f, r = 0.375f, xMin = 0.56f });
            Prepare();
        }

        // ---------- Клетки кузова: крыши нет ----------

        protected override CellInfo Classify(float z, int seg, float v, Vector3 p, float side)
        {
            string node = "Shell", mat = "paint", inner = null;
            bool cabin = InCabin(z);
            if (seg == SegE)
            {
                if (z > Header && z < Cowl)
                {
                    // Лобовое стекло в чёрной рамке
                    float edge = Mathf.Min(z - Header, Cowl - z);
                    mat = v > 0.10f && edge > 0.035f ? "glass" : "black";
                }
                else if (z <= Header && z >= Header - 0.045f) mat = "black";   // верхняя перекладина рамки
                else if (cabin) mat = null;                                      // открытый верх
                if (z >= Cowl + 0.015f && z < 1.92f) node = "Hood";
                else if (z < Deck - 0.01f && z > -1.98f) node = "Trunk";
            }
            else if (seg == SegD && cabin)
            {
                // Боковых стёкол нет (опущены), остаются только стойки рамки
                mat = null;
            }
            bool lower = seg == SegB || seg == SegC;
            if (lower && z < DoorFront && z > DoorRear && p.y > 0.16f) node = side > 0 ? "DoorR" : "DoorL";
            if (seg <= SegC && z > 1.88f) node = "BumperF";
            if (seg <= SegC && z < -1.88f && p.y < 0.68f) node = "BumperR";
            if (mat != null && mat != "glass" && seg >= SegB && seg <= SegC && z < Cowl && z > Tonneau - 0.05f && p.y > 0.22f)
                inner = "int_door";
            if (mat == "black" && seg == SegE && z < Cowl) inner = "black"; // рамку стекла видно и изнутри
            return new CellInfo { node = node, mat = mat, innerMat = inner };
        }

        protected override Vector3 Pivot(string name)
        {
            switch (name)
            {
                case "Hood": return new Vector3(0f, 0.845f, Cowl + 0.02f);
                case "Roof": return new Vector3(0f, 1.13f, Header);
                case "Trunk": return new Vector3(0f, 0.91f, Deck);
                case "DoorL": return new Vector3(-0.86f, 0.55f, -0.05f);
                case "DoorR": return new Vector3(0.86f, 0.55f, -0.05f);
                case "BumperF": return new Vector3(0f, 0.3f, 1.97f);
                case "BumperR": return new Vector3(0f, 0.42f, -1.97f);
            }
            return Vector3.zero;
        }

        // ---------- Сборка ----------

        Model Build()
        {
            StartModel();
            Emit(N);
            ArchLiners(N("Shell"), "liner");
            foreach (float side in new[] { 1f, -1f })
            {
                Headlight(side);
                FrontFascia(side);
                Taillight(side);
                SideDetails(side);
                Livery(side);
                Mirror(side);
                DoorTopCap(side);
            }
            CenterDetails();
            Convertible();
            Wing();
            Interior();
            BuildWheels();
            // Рамка лобового стекла — «крыша» для поломок (её мнёт при ударах)
            N("Roof");
            return model;
        }

        static Frame Cup(Frame look) => new Frame { o = look.o, x = look.x, y = look.z, z = -look.y };

        // ---------- Перед ----------

        /// <summary>Вытянутая фара, уходящая вверх к крылу; снаружи — оранжевый поворотник.</summary>
        void Headlight(float side)
        {
            var node = body.Child(side < 0 ? "HeadlightL" : "HeadlightR", new Vector3(0.58f * side, 0.58f, 1.8f));
            (Vector3, Vector3) Ray(float a, float b)
            {
                float ang = Mathf.Lerp(6f, 66f, a) * Mathf.Deg2Rad;
                float yc = Mathf.Lerp(0.618f, 0.535f, a);
                float hh = Mathf.Lerp(0.04f, 0.03f, a) * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(2f * a - 1f), 6f)));
                return (new Vector3(0.32f, yc + (b - 0.5f) * 2f * hh, 1.5f), new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)));
            }
            var blink = node.Child(side < 0 ? "BlinkFL" : "BlinkFR");
            Patch(blink, "amber", 6, 5, (a, b) => Ray(a * 0.3f, b), 0.004f, side);
            Patch(node, "housing", 16, 5, (a, b) => Ray(0.3f + a * 0.7f, b), 0.003f, side);
            Patch(node, "lens", 22, 5, Ray, 0.018f, side);
            var rim = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                rim.Add(t < 0.5f ? Ray(t * 2f, 1f) : Ray(1f - (t - 0.5f) * 2f, 0f));
            }
            Ribbon(node, "black", rim, 0.009f, 0.004f, side);
            var f = node.WorldFrame();
            foreach (var (a, rad) in new[] { (0.5f, 0.03f), (0.78f, 0.024f) })
            {
                var r = Ray(a, 0.5f);
                OnBody(r.Item1, r.Item2, side, out var p, out var n);
                var cup = Cup(Frame.Look(f.ToLocal(p), f.DirToLocal(n), Vector3.up));
                Geo.Lathe(node.M("reflector"), cup, new[] { new Vector2(rad, 0.012f), new Vector2(rad * 0.75f, 0.005f), new Vector2(rad * 0.3f, 0.003f) }, 20, true, true);
                Geo.Lathe(node.M("lamp_glow"), cup, new[] { new Vector2(rad * 0.6f, 0.006f), new Vector2(rad * 0.45f, 0.013f), new Vector2(0f, 0.016f) }, 20);
            }
        }

        void FrontFascia(float side)
        {
            var bumper = N("BumperF");
            // «Рот» с горизонтальными перемычками в цвет кузова
            Patch(bumper, "grille", 12, 6, (a, b) =>
            {
                float x = a * 0.36f;
                float round = Mathf.Max(0f, (x - 0.28f) / 0.08f);
                return (new Vector3(x, Mathf.Lerp(0.2f + 0.03f * round * round, 0.355f - 0.04f * round * round, b), 1.5f), Vector3.forward);
            }, 0.003f, side);
            foreach (float y in new[] { 0.235f, 0.27f, 0.305f, 0.34f })
            {
                var bar = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 10; i++) bar.Add((new Vector3(i / 10f * 0.33f, y, 1.5f), Vector3.forward));
                Ribbon(bumper, "paint", bar, 0.012f, 0.008f, side);
            }
            // Нижние угловые воздухозаборники с вертикальными рёбрами
            Patch(bumper, "grille", 8, 4, (a, b) => (new Vector3(Mathf.Lerp(0.48f, 0.70f, a), Mathf.Lerp(0.16f, 0.29f, b) - 0.03f * a, 1.5f), Vector3.forward), 0.003f, side);
            for (int i = 0; i < 4; i++)
            {
                float x = 0.51f + i * 0.055f;
                var fin = new List<(Vector3, Vector3)>();
                for (int k = 0; k <= 4; k++) fin.Add((new Vector3(x, Mathf.Lerp(0.16f, 0.28f, k / 4f) - 0.03f * (x - 0.48f) / 0.22f, 1.5f), Vector3.forward));
                Ribbon(bumper, "paint", fin, 0.01f, 0.008f, side);
            }
            var lip = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 24; i++)
            {
                float ang = Mathf.Lerp(90f, 8f, i / 24f) * Mathf.Deg2Rad;
                lip.Add((new Vector3(0f, 0.125f, 1.5f), new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang))));
            }
            Ribbon(bumper, "black", lip, 0.028f, 0.004f, side);
            Patch(N("Shell"), "amber", 4, 2, (a, b) => (new Vector3(0.4f, Mathf.Lerp(0.58f, 0.61f, b), Mathf.Lerp(0.95f, 1.02f, a)), Vector3.right), 0.003f, side);
        }

        // ---------- Раскраска Суки ----------

        void Livery(float side)
        {
            // Белые завитки «ооо» по капоту
            var loops = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 240; i++)
            {
                float t = i / 240f, ang = t * Mathf.PI * 2f * 9f;
                float z = Mathf.Lerp(0.66f, 1.86f, t) + 0.05f * Mathf.Sin(ang);
                float x = 0.25f - 0.05f * t + 0.045f * Mathf.Cos(ang);
                loops.Add((new Vector3(x, 0.3f, z), Vector3.up));
            }
            Ribbon(N("Hood"), "stripe_white", loops, 0.007f, 0.0016f, side);
            // Завитки на переднем крыле и на задних крыльях
            foreach (var (z0, z1, y0) in new[] { (1.62f, 0.72f, 0.6f), (-0.7f, -1.7f, 0.66f) })
            {
                var w = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 150; i++)
                {
                    float t = i / 150f, ang = t * Mathf.PI * 2f * 5f;
                    float z = Mathf.Lerp(z0, z1, t) + 0.05f * Mathf.Sin(ang);
                    float y = y0 + 0.05f * Mathf.Cos(ang) - 0.04f * t;
                    w.Add((new Vector3(0.3f, y, z), Vector3.right));
                }
                Ribbon(N("Shell"), "stripe_white", w, 0.007f, 0.0016f, side);
            }
            // Пурпурные «пряди» на двери, уходящие назад на крыло
            var door = N(side < 0 ? "DoorL" : "DoorR");
            for (int i = 0; i < 6; i++)
            {
                float y0 = 0.5f + i * 0.045f, z0 = 0.35f - (i % 2) * 0.08f;
                float len = 0.75f + 0.2f * (i % 3);
                string mat = i % 2 == 0 ? "stripe_magenta" : "stripe_purple";
                float w = 0.045f - (i % 3) * 0.008f;
                Patch(door, mat, 14, 2, (a, b) =>
                {
                    float s = (b * 2f - 1f) * w * (1f - a * 0.9f);
                    float z = z0 - len * a;
                    float y = y0 + 0.05f * Mathf.Sin(a * Mathf.PI * 1.4f + i) + s;
                    return (new Vector3(0.3f, y, Mathf.Max(z, DoorRear + 0.02f)), Vector3.right);
                }, 0.0016f + i * 0.0002f, side);
            }
            // Белые искорки
            var dots = N("Shell");
            for (int i = 0; i < 14; i++)
            {
                float z = Mathf.Lerp(0.4f, -1.5f, (i * 0.618f) % 1f);
                float y = 0.45f + ((i * 0.37f) % 1f) * 0.32f;
                OnBody(new Vector3(0.3f, y, z), Vector3.right, side, out var p, out var n);
                var df = dots.WorldFrame();
                var lf = Frame.Look(df.ToLocal(p + n * 0.002f), df.DirToLocal(n), Vector3.up);
                Geo.Polygon(dots.M("stripe_white"), lf, Geo.Circle(0.008f + (i % 3) * 0.004f, 10), true);
            }
        }

        // ---------- Корма ----------

        void Taillight(float side)
        {
            var node = body.Child(side < 0 ? "TaillightL" : "TaillightR", new Vector3(0.6f * side, 0.75f, -2.0f));
            (Vector3, Vector3) Lamp(float a, float b)
            {
                float x = Mathf.Lerp(0.42f, 0.80f, a);
                float hh = 0.05f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(2f * a - 1f), 4f)));
                return (new Vector3(x, 0.75f + 0.015f * a + (b - 0.5f) * 2f * hh, -1.5f), Vector3.back);
            }
            Patch(node, "tail_smoke", 18, 5, Lamp, 0.004f, side);
            var blink = node.Child(side < 0 ? "BlinkRL" : "BlinkRR");
            Patch(blink, "amber", 4, 3, (a, b) => Lamp(0.06f + a * 0.14f, 0.2f + b * 0.6f), 0.006f, side);
            var f = node.WorldFrame();
            foreach (var (x, r) in new[] { (0.66f, 0.038f), (0.53f, 0.03f) })
            {
                OnBody(new Vector3(x, 0.757f, -1.5f), Vector3.back, side, out var p, out var n);
                var cup = Cup(Frame.Look(f.ToLocal(p + n * 0.005f), f.DirToLocal(n), Vector3.up));
                Geo.Lathe(node.M("tail_red"), cup, new[] { new Vector2(r, 0f), new Vector2(r * 0.9f, 0.006f), new Vector2(0f, 0.009f) }, 24);
            }
        }

        void CenterDetails()
        {
            var shell = N("Shell");
            var bumperF = N("BumperF");
            var bumperR = N("BumperR");
            Plate(bumperF, new Vector3(0f, 0.42f, 1.5f), Vector3.forward, "PlateFront");
            Plate(bumperR, new Vector3(0f, 0.5f, -1.5f), Vector3.back, "PlateRear");
            foreach (float side in new[] { 1f, -1f })
            {
                Patch(bumperR, "black", 6, 4, (a, b) => (new Vector3(a * 0.3f, Mathf.Lerp(0.43f, 0.57f, b), -1.5f), Vector3.back), 0.002f, side);
                Patch(bumperR, "black", 18, 3, (a, b) => (new Vector3(a * 0.78f, Mathf.Lerp(0.22f, 0.29f, b), -1.5f), Vector3.back), 0.002f, side);
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++) seam.Add((new Vector3(i / 16f * 0.8f, 0.82f, -1.5f), Vector3.back));
                Ribbon(N("Trunk"), "black", seam, 0.004f, 0.001f, side);
                // Стоп-сигнал — тёмная полоска на крышке багажника
                Patch(N("Trunk"), "tail_smoke", 8, 2, (a, b) => (new Vector3(a * 0.28f, 0.6f, Mathf.Lerp(-1.82f, -1.85f, b)), Vector3.up), 0.003f, side);
                var wiper = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 10; i++) wiper.Add((new Vector3(Mathf.Lerp(0.6f, 0.02f, i / 10f), 0.7f, Mathf.Lerp(0.49f, 0.465f, i / 10f)), Vector3.up));
                Ribbon(shell, "black", wiper, 0.016f, 0.008f, side);
                Patch(shell, "black", 12, 2, (a, b) => (new Vector3(a * 0.76f, 0.55f, Mathf.Lerp(Cowl + 0.003f, Cowl + 0.05f, b)), Vector3.up), 0.002f, side);
                OnBody(new Vector3(0.42f, 0.22f, -1.5f), Vector3.back, side, out var ep, out _);
                var ef = bumperR.WorldFrame();
                var pipe = new Frame { o = ef.ToLocal(new Vector3(ep.x, 0.22f, ep.z + 0.03f)), x = Vector3.right, y = Vector3.back, z = Vector3.up };
                Geo.Lathe(bumperR.M("chrome"), pipe, new[] { new Vector2(0.042f, -0.05f), new Vector2(0.042f, 0.08f), new Vector2(0.046f, 0.085f), new Vector2(0.04f, 0.09f) }, 24);
                Geo.Lathe(bumperR.M("int_black"), pipe, new[] { new Vector2(0.036f, 0.09f), new Vector2(0.036f, 0.0f), new Vector2(0f, 0.0f) }, 24, false, true);
            }
            Emblem(N("Hood"), new Vector3(0f, 0.4f, 1.9f), new Vector3(0f, 0.3f, 1f), 0.04f);
            Emblem(N("Trunk"), new Vector3(0f, 0.74f, -1.5f), Vector3.back, 0.04f);
            Geo.Box(shell.M("int_black"), Frame.Identity, new Vector3(0f, 0.38f, 1.1f), new Vector3(1.1f, 0.34f, 1.0f));
        }

        // ---------- Бока ----------

        void SideDetails(float side)
        {
            var door = N(side < 0 ? "DoorL" : "DoorR");
            var shell = N("Shell");
            List<(Vector3, Vector3)> Line(float z0, float y0, float z1, float y1, int n = 14)
            {
                var list = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    list.Add((new Vector3(0.3f, Mathf.Lerp(y0, y1, t), Mathf.Lerp(z0, z1, t)), Vector3.right));
                }
                return list;
            }
            Ribbon(door, "black", Line(DoorFront, 0.18f, DoorFront - 0.03f, 0.84f), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear, 0.18f, DoorRear + 0.02f, 0.87f), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear, 0.185f, DoorFront, 0.185f, 30), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear + 0.08f, 0.8f, DoorRear + 0.2f, 0.8f, 6), 0.022f, 0.002f, side);
            var hood = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float z = Mathf.Lerp(Cowl + 0.02f, 1.92f, i / 40f);
                var st = Sec(z);
                hood.Add((new Vector3(st.fb * st.w - 0.03f, st.belt - 0.15f, z), Vector3.up));
            }
            Ribbon(N("Hood"), "black", hood, 0.005f, 0.001f, side);
            Ribbon(shell, "black", Line(AxleR + 0.39f, 0.14f, AxleF - 0.37f, 0.14f, 30), 0.03f, 0.002f, side);
            if (side > 0)
            {
                var hatch = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 28; i++)
                {
                    float a = i / 28f * Mathf.PI * 2f;
                    hatch.Add((new Vector3(0.3f, 0.78f + Mathf.Sin(a) * 0.05f, -1.48f + Mathf.Cos(a) * 0.06f), Vector3.right));
                }
                Ribbon(shell, "black", hatch, 0.004f, 0.001f, side);
            }
        }

        /// <summary>Чёрный уплотнитель по верху двери и борта: закрывает щель между краской и обшивкой открытого салона.</summary>
        void DoorTopCap(float side)
        {
            var shell = N("Shell");
            var m = shell.M("black");
            var f = shell.WorldFrame();
            int prevA = -1, prevB = -1;
            const int n = 40;
            for (int i = 0; i <= n; i++)
            {
                float z = Mathf.Lerp(Cowl - 0.02f, Tonneau - 0.05f, i / (float)n);
                var outer = S(z, SegU[3]);
                outer.x *= side;
                var inner = new Vector3(outer.x - 0.05f * side, outer.y - 0.003f, z);
                int a = m.Add(f.ToLocal(outer + Vector3.up * 0.004f), Vector3.up);
                int b = m.Add(f.ToLocal(inner + Vector3.up * 0.004f), Vector3.up);
                if (i > 0) QuadFacing(m, prevA, prevB, b, a, Vector3.up);
                prevA = a; prevB = b;
            }
        }

        void Mirror(float side)
        {
            var door = N(side < 0 ? "DoorL" : "DoorR");
            const float mz = 0.36f;
            var st = Sec(mz);
            var basePt = new Vector3(st.fb * st.w * side, st.belt + 0.015f, mz);
            var df = door.WorldFrame();
            var center = basePt + new Vector3(0.12f * side, 0.07f, -0.035f);
            var mount = door.Child(side < 0 ? "MirrorL" : "MirrorR", df.ToLocal(center));
            Geo.RoundBox(mount.M("paint"), Frame.Identity, Vector3.zero, new Vector3(0.16f, 0.10f, 0.11f), 0.55f, 14);
            var from = df.ToLocal(basePt) - mount.pos;
            var to = new Vector3(-0.05f * side, -0.025f, 0.01f);
            Geo.RoundBox(mount.M("paint"), Frame.Look((from + to) * 0.5f, to - from, Vector3.up), Vector3.zero, new Vector3(0.06f, 0.028f, (to - from).magnitude + 0.03f), 0.4f, 6);
            float yaw = side < 0 ? -27f : 48f;
            var glass = mount.Child(side < 0 ? "MirrorGlassL" : "MirrorGlassR", new Vector3(0f, 0.002f, -0.056f), new Vector3(0f, yaw * 0.25f, 0f));
            Geo.Quad(glass.M("mirror"), Frame.Identity, 0.14f, 0.08f);
        }

        /// <summary>Открытый верх: дуги безопасности за сиденьями, крышка сложенного тента, перегородка.</summary>
        void Convertible()
        {
            var cab = body.Child("Topless");
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("black"), id, new Vector3(0f, 0.875f, (Tonneau + Deck) / 2f - 0.02f), new Vector3(1.52f, 0.035f, Tonneau - Deck + 0.08f), 0.3f, 8);
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.55f, Tonneau), new Vector3(1.5f, 0.66f, 0.03f));
            foreach (float x in new[] { -0.36f, 0.36f })
            {
                var arc = new List<Vector3>();
                for (int i = 0; i <= 16; i++)
                {
                    float a = Mathf.PI * i / 16f;
                    arc.Add(new Vector3(x + Mathf.Cos(a) * 0.2f, 0.87f + Mathf.Sin(a) * 0.2f, Tonneau - 0.02f));
                }
                Geo.Tube(cab.M("black"), id, arc, 0.035f, 10, false, Vector3.forward);
                Geo.RoundBox(cab.M("fur_pink"), id, new Vector3(x, 0.95f, Tonneau + 0.02f), new Vector3(0.26f, 0.14f, 0.03f), 0.5f, 8);
            }
        }

        /// <summary>Большое розовое крыло на треугольных серебристых стойках, торцы загнуты вниз.</summary>
        void Wing()
        {
            var trunk = N("Trunk");
            var f = trunk.WorldFrame();
            const float top = 1.1f, zc = -1.78f, half = 0.86f;
            var path = new List<Vector3>();
            for (int i = 0; i <= 24; i++) path.Add(f.ToLocal(new Vector3(Mathf.Lerp(-half, half, i / 24f), top, zc)));
            var foil = new List<Vector2>();
            const int nf = 16;
            for (int i = 0; i < nf * 2; i++)
            {
                float t = i < nf ? i / (float)nf : (2 * nf - i) / (float)nf;
                float xx = (1f - Mathf.Cos(t * Mathf.PI)) / 2f;
                float th = 0.35f * (0.2969f * Mathf.Sqrt(xx) - 0.126f * xx - 0.3516f * xx * xx + 0.2843f * xx * xx * xx - 0.1036f * xx * xx * xx * xx);
                float yy = (i < nf ? th : -th) * 0.26f;
                foil.Add(new Vector2((xx - 0.45f) * 0.26f, yy + xx * 0.03f));
            }
            Geo.Sweep(trunk.M("paint"), Frame.Identity, path, foil, false, Vector3.up, true);
            foreach (float side in new[] { -1f, 1f })
            {
                Geo.RoundBox(trunk.M("paint"), Frame.Identity, f.ToLocal(new Vector3(half * side, top - 0.05f, zc - 0.03f)), new Vector3(0.016f, 0.14f, 0.26f), 0.35f, 6);
                // Треугольная стойка из двух наклонных пластин
                var topP = f.ToLocal(new Vector3(0.3f * side, top - 0.01f, zc));
                foreach (float dz in new[] { 0.14f, -0.14f })
                {
                    var bottom = f.ToLocal(new Vector3(0.3f * side, 0.9f, zc + dz));
                    Geo.RoundBox(trunk.M("alloy"), Frame.Look((topP + bottom) * 0.5f, topP - bottom, Vector3.right), Vector3.zero, new Vector3(0.012f, 0.03f, (topP - bottom).magnitude + 0.02f), 0.3f, 6);
                }
            }
        }

        // ---------- Салон (руль слева, розовый мех) ----------

        void Interior()
        {
            var cab = body.Child("Interior");
            var id = Frame.Identity;
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.2f, -0.2f), new Vector3(1.5f, 0.03f, 1.4f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.42f, 0.56f), new Vector3(1.5f, 0.42f, 0.04f));
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.3f, -0.15f), new Vector3(0.28f, 0.22f, 1.3f), 0.25f, 8);
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.43f, -0.3f), new Vector3(0.26f, 0.08f, 0.8f), 0.2f, 8);
            // Рычаг с хромированным шаром в меховом чехле, ручник, кнопка закиси азота
            Geo.RoundBox(cab.M("fur_pink"), id, new Vector3(-0.02f, 0.5f, -0.08f), new Vector3(0.11f, 0.08f, 0.11f), 0.7f, 10);
            Geo.Cylinder(cab.M("chrome"), Frame.Euler(new Vector3(-0.02f, 0.5f, -0.08f), new Vector3(-8f, 0f, 0f)), 0.009f, 0.02f, 0.13f, 8);
            Geo.RoundBox(cab.M("chrome"), id, new Vector3(-0.02f, 0.64f, -0.065f), new Vector3(0.05f, 0.05f, 0.05f), 0.95f, 12);
            Geo.RoundBox(cab.M("int_black"), Frame.Euler(new Vector3(0.09f, 0.5f, -0.42f), new Vector3(-20f, 0f, 0f)), Vector3.zero, new Vector3(0.03f, 0.03f, 0.26f), 0.5f, 6);
            Geo.Cylinder(cab.M("tail_red"), new Frame { o = new Vector3(0f, 0.47f, -0.22f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.018f, 0f, 0.012f, 14);

            // Торпеда
            var prof = new[]
            {
                new Vector2(0.53f, 0.80f), new Vector2(0.40f, 0.83f), new Vector2(0.26f, 0.85f), new Vector2(0.17f, 0.85f),
                new Vector2(0.13f, 0.83f), new Vector2(0.12f, 0.78f), new Vector2(0.14f, 0.66f), new Vector2(0.19f, 0.54f), new Vector2(0.33f, 0.46f),
            };
            Vector2 Prof(float t)
            {
                float fi = t * (prof.Length - 1);
                int i = Mathf.Min(prof.Length - 2, (int)fi);
                float k = fi - i;
                var p0 = prof[Mathf.Max(0, i - 1)]; var p1 = prof[i]; var p2 = prof[i + 1]; var p3 = prof[Mathf.Min(prof.Length - 1, i + 2)];
                return 0.5f * (2f * p1 + (-p0 + p2) * k + (2f * p0 - 5f * p1 + 4f * p2 - p3) * k * k + (-p0 + 3f * p1 - 3f * p2 + p3) * k * k * k);
            }
            Geo.Surface(cab.M("int_black"), id, 40, 12, (a, b) =>
            {
                var q = Prof(b);
                return new Vector3(Mathf.Lerp(-0.74f, 0.74f, a), q.y, q.x);
            });
            foreach (float x in new[] { 0.6f, -0.62f, 0.12f })
            {
                Geo.RoundBox(cab.M("int_black"), id, new Vector3(x, 0.76f, 0.115f), new Vector3(0.12f, 0.06f, 0.02f), 0.25f, 6);
                Geo.RoundBox(cab.M("grille"), id, new Vector3(x, 0.76f, 0.108f), new Vector3(0.1f, 0.045f, 0.012f), 0.2f, 4);
            }
            // Три прибора посередине торпеды
            var pod = cab.Child("Pods");
            for (int i = 0; i < 3; i++)
                Gauge(pod, "Aux" + i, new Vector3(0.0f + i * 0.08f, 0.69f, 0.135f), 0.031f, true, "gauge_light", "int_black");
            // Цифровой щиток под козырьком
            var c = new Vector3(-0.36f, 0.82f, 0.06f);
            Vector3 Hood(float a, float b)
            {
                float th = Mathf.Lerp(-0.2f, Mathf.PI + 0.2f, a);
                return c + new Vector3(Mathf.Cos(th) * 0.22f, Mathf.Sin(th) * 0.22f * 0.4f + 0.03f, Mathf.Lerp(0.05f, -0.05f, b) - 0.02f * Mathf.Sin(th));
            }
            Geo.Surface(cab.M("int_black"), id, 30, 4, Hood, true);
            Geo.Surface(cab.M("int_black"), id, 30, 4, Hood, false);
            var cl = cab.Child("ClusterScreen", c + new Vector3(0f, -0.01f, 0.03f), SportsCarModel.LookEuler((c - DriverEyes).normalized));
            // Щиток как у настоящей S2000: дуга светодиодного тахометра, крупная скорость, столбик бензина
            Geo.RoundBox(cl.M("gauge_face"), id, new Vector3(0f, 0f, 0.008f), new Vector3(0.36f, 0.14f, 0.012f), 0.3f, 6);
            const int leds = 30;
            for (int i = 0; i < leds; i++)
            {
                float ang = Mathf.Lerp(162f, 18f, i / (float)(leds - 1)) * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                var tangent = new Vector3(-Mathf.Sin(ang), Mathf.Cos(ang), 0f);
                var led = cl.Child("Led" + i, new Vector3(0f, -0.075f, 0f) + radial * 0.13f);
                Geo.Box(led.M("led_off"), new Frame { o = new Vector3(0f, 0f, -0.002f), x = radial, y = tangent, z = Vector3.forward },
                    Vector3.zero, new Vector3(0.022f, 0.009f, 0.003f));
            }
            for (int i = 0; i < 8; i++)
            {
                var seg = cl.Child("FuelLed" + i, new Vector3(0.155f, -0.05f + i * 0.012f, 0f));
                Geo.Box(seg.M("led_off"), id, new Vector3(0f, 0f, -0.002f), new Vector3(0.02f, 0.008f, 0.003f));
            }
            var screen = cab.Child("RadioScreen", new Vector3(0.12f, 0.6f, 0.15f), new Vector3(10f, 0f, 0f));
            Geo.RoundBox(screen.M("int_black"), id, new Vector3(0f, 0f, 0.01f), new Vector3(0.2f, 0.06f, 0.02f), 0.2f, 6);
            Geo.Quad(screen.M("screen"), id, 0.16f, 0.035f);

            SteeringWheel();
            foreach (float side in new[] { -1f, 1f }) Seat(cab, side);
            foreach (float side in new[] { -1f, 1f })
            {
                var door = N(side < 0 ? "DoorL" : "DoorR");
                var df = door.WorldFrame();
                Geo.RoundBox(door.M("fur_pink"), id, df.ToLocal(new Vector3(0.78f * side, 0.55f, -0.08f)), new Vector3(0.04f, 0.26f, 0.7f), 0.35f, 8);
                Geo.RoundBox(door.M("int_black"), id, df.ToLocal(new Vector3(0.76f * side, 0.7f, -0.25f)), new Vector3(0.06f, 0.04f, 0.4f), 0.4f, 8);
            }
            // Салонное зеркало на рамке стекла
            var rm = cab.Child("RearMirror", new Vector3(0f, 1.065f, 0.06f), new Vector3(0f, 21f, 0f));
            Geo.Cylinder(rm.M("int_black"), id, 0.01f, 0f, 0.035f, 8);
            Geo.RoundBox(rm.M("int_black"), id, new Vector3(0f, 0f, 0.016f), new Vector3(0.24f, 0.065f, 0.035f), 0.5f, 10);
            Geo.Quad(rm.Child("RearMirrorGlass", new Vector3(0f, 0f, -0.0025f)).M("mirror"), id, 0.22f, 0.05f);
            Driver(0.72f, -0.66f, -14f);
        }

        /// <summary>Руль «Спарко»: чёрный, три спицы, жёлтый логотип, красные кнопки.</summary>
        void SteeringWheel()
        {
            var w = body.Child("SteeringWheel", SteeringPos, new Vector3(SteeringTilt, 0f, 0f));
            var id = Frame.Identity;
            const float R = 0.17f;
            Geo.Torus(w.M("leather_black"), id, R, 0.019f, 48, 10);
            foreach (var dir in new[] { new Vector3(1f, 0f, -0.2f).normalized, new Vector3(-1f, 0f, -0.2f).normalized, Vector3.back })
            {
                var f = Frame.Look(dir * R * 0.5f, dir, Vector3.up);
                Geo.RoundBox(w.M("int_black"), f, Vector3.zero, new Vector3(0.07f, 0.018f, R * 0.95f), 0.4f, 6);
            }
            Geo.RoundBox(w.M("int_black"), id, new Vector3(0f, 0.012f, 0f), new Vector3(0.12f, 0.04f, 0.1f), 0.5f, 10);
            Geo.Box(w.M("stripe_yellow"), id, new Vector3(0f, 0.033f, 0.01f), new Vector3(0.06f, 0.003f, 0.015f));
            foreach (float x in new[] { -0.08f, 0.08f })
                Geo.Cylinder(w.M("tail_red"), new Frame { o = new Vector3(x, 0.012f, -0.04f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.009f, 0f, 0.012f, 10);
            Geo.Cylinder(w.M("int_black"), id, 0.035f, -0.3f, -0.01f, 12);
        }

        /// <summary>Сиденье в бело-розовом меху.</summary>
        void Seat(ModelNode cab, float side)
        {
            float x = 0.36f * side;
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("fur_pink"), id, new Vector3(x, 0.3f, -0.5f), new Vector3(0.48f, 0.12f, 0.5f), 0.45f, 10);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(cab.M("fur_pink"), id, new Vector3(x + b * 0.2f, 0.36f, -0.52f), new Vector3(0.1f, 0.13f, 0.48f), 0.6f, 8);
            var back = cab.Child(side > 0 ? "SeatBackR" : "SeatBackL", new Vector3(x, 0.34f, -0.74f), new Vector3(-14f, 0f, 0f));
            Geo.RoundBox(back.M("fur_pink"), id, new Vector3(0f, 0.31f, -0.02f), new Vector3(0.48f, 0.62f, 0.13f), 0.45f, 10);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(back.M("fur_pink"), id, new Vector3(b * 0.2f, 0.27f, 0.05f), new Vector3(0.1f, 0.46f, 0.15f), 0.6f, 8);
            Geo.RoundBox(back.M("fur_pink"), id, new Vector3(0f, 0.66f, -0.01f), new Vector3(0.26f, 0.17f, 0.12f), 0.65f, 8);
        }

        void BuildWheels()
        {
            foreach (float side in new[] { -1f, 1f })
            foreach (bool front in new[] { true, false })
            {
                float z = front ? AxleF : AxleR;
                string tag = (front ? "F" : "R") + (side < 0 ? "L" : "R");
                var mount = model.root.Child((front ? "Steer" : "Mount") + tag, new Vector3(Track * side, WheelR, z));
                var wheel = mount.Child("Wheel" + tag, Vector3.zero, new Vector3(0f, 0f, 90f));
                var mesh = wheel.Child("Rim" + tag, Vector3.zero, side < 0 ? Vector3.zero : new Vector3(180f, 0f, 0f));
                WheelModel.FiveSpoke(mesh, WheelR, 0.235f, 12, 0.229f, "alloy", "chrome", 0.5f);
                WheelModel.Caliper(mount, side, 1.1f);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «Тоёта Супра» четвёртого поколения (1993–2002), праворульная японская версия.
    /// Размеры как у настоящей: 4,52 × 1,81 × 1,275 м, база 2,55 м, колёса 17".
    /// Вся модель — гладкие сетки, посчитанные кодом: кузов по сечениям (<see cref="CarBody"/>),
    /// фары с тремя линзами, четыре круглых фонаря с каждой стороны, «петля» антикрыла,
    /// пятиспицевые диски, салон с красной кожей и дополнительными приборами на стойке.
    /// </summary>
    public class SupraModel : SportsCarModel
    {
        public const float AxleF = 1.19f, AxleR = -1.36f;
        public const float WheelR = 0.322f, TrackF = 0.765f, TrackR = 0.775f;
        public const float DoorFront = 0.70f, DoorRear = -0.90f;
        const float SideRearPoint = -1.44f;

        static Model cached;

        /// <summary>Готовая модель (строится один раз, дальше переиспользуется).</summary>
        public static Model Get()
        {
            if (cached == null) cached = new SupraModel().Build();
            return cached;
        }

        /// <summary>Только форма кузова (без сборки сеток) — для поиска точек на поверхности.</summary>
        public static SupraModel Shape() => new SupraModel();

        SupraModel()
        {
            Front = 2.26f; Rear = -2.26f;
            Cowl = 0.50f; Header = -0.22f; RoofRear = -0.95f; Deck = -1.72f;
            FrontRound = 1.60f; RearRound = -1.80f; FrontPow = 2.3f; RearPow = 3.0f;

            W0 = new Curve().Key(-2.26f, 0.885f).Key(-1.8f, 0.9f).Key(-1.36f, 0.905f).Key(-0.9f, 0.895f).Key(-0.3f, 0.885f)
                .Key(0.4f, 0.885f).Key(0.9f, 0.89f).Key(1.19f, 0.895f).Key(1.6f, 0.888f).Key(2.26f, 0.86f);
            YBot = new Curve().Key(-2.26f, 0.40f).Key(-2.20f, 0.31f).Key(-2.10f, 0.27f).Key(-1.95f, 0.22f).Key(-1.75f, 0.17f)
                .Key(-1.4f, 0.15f).Key(0f, 0.14f).Key(1.6f, 0.14f).Key(2.0f, 0.125f).Key(2.15f, 0.14f).Key(2.22f, 0.18f).Key(2.26f, 0.24f);
            YMax = new Curve().Key(-2.26f, 0.56f).Key(-1.8f, 0.55f).Key(-1.0f, 0.50f).Key(0.5f, 0.47f).Key(1.6f, 0.46f).Key(2.26f, 0.42f);
            YBelt = new Curve().Key(-2.26f, 0.83f).Key(-2.18f, 0.90f).Key(-2.05f, 0.94f).Key(-1.85f, 0.962f).Key(-1.55f, 0.978f)
                .Key(-1.25f, 0.975f).Key(-0.9f, 0.962f).Key(-0.4f, 0.942f).Key(0f, 0.915f).Key(0.5f, 0.875f).Key(0.9f, 0.80f)
                .Key(1.19f, 0.77f).Key(1.5f, 0.735f).Key(1.8f, 0.69f).Key(2.0f, 0.645f).Key(2.12f, 0.60f).Key(2.2f, 0.545f).Key(2.26f, 0.47f);
            FBelt = new Curve().Key(-2.26f, 0.80f).Key(-2.0f, 0.84f).Key(-1.75f, 0.87f).Key(-1.3f, 0.915f).Key(-0.5f, 0.93f)
                .Key(0.3f, 0.93f).Key(0.6f, 0.89f).Key(1.0f, 0.86f).Key(2.26f, 0.80f);
            YTop = new Curve().Key(-2.26f, 0.835f).Key(-2.20f, 0.905f).Key(-2.12f, 0.945f).Key(-2.0f, 0.975f).Key(-1.86f, 0.99f)
                .Key(Deck, 0.995f, true).Key(-1.33f, 1.148f).Key(RoofRear, 1.255f, true).Key(-0.55f, 1.274f).Key(Header, 1.248f, true)
                .Key(0.14f, 1.08f).Key(Cowl, 0.89f, true).Key(0.9f, 0.84f).Key(1.19f, 0.805f).Key(1.5f, 0.765f).Key(1.8f, 0.715f)
                .Key(2.0f, 0.67f).Key(2.12f, 0.625f).Key(2.2f, 0.57f).Key(2.26f, 0.48f);
            YRoofEdge = new Curve().Key(Deck, 0.976f, true).Key(-1.33f, 1.10f).Key(RoofRear, 1.205f, true).Key(-0.55f, 1.226f)
                .Key(Header, 1.198f, true).Key(0.14f, 1.035f).Key(Cowl, 0.875f, true);
            XRoofEdge = new Curve().Key(Deck, 0.80f, true).Key(-1.33f, 0.70f).Key(RoofRear, 0.637f, true).Key(-0.55f, 0.645f)
                .Key(Header, 0.637f, true).Key(Cowl, 0.79f, true);
            AngC = new Curve().Key(-2.26f, 160f).Key(-1.85f, 150f).Key(-1.6f, 120f).Key(-1.3f, 100f).Key(0.2f, 100f).Key(0.55f, 135f)
                .Key(0.8f, 155f).Key(2.26f, 160f);
            AngE0 = new Curve().Key(-2.26f, 172f).Key(-1.75f, 170f).Key(-1.5f, 158f).Key(-0.95f, 150f).Key(-0.22f, 150f)
                .Key(0.2f, 158f).Key(0.5f, 170f).Key(2.26f, 172f);
            AngD0 = new Curve().Key(-1.75f, 140f).Key(-1.45f, 118f).Key(-1.0f, 112f).Key(0.3f, 112f).Key(0.5f, 120f);
            AngD1 = new Curve().Key(-1.75f, 150f).Key(-1.45f, 135f).Key(-1.0f, 122f).Key(0.3f, 122f).Key(0.5f, 130f);

            arches.Add(new Arch { z = AxleF, y = 0.325f, r = 0.372f, xMin = 0.56f });
            arches.Add(new Arch { z = AxleR, y = 0.325f, r = 0.382f, xMin = 0.56f });
            Prepare();
        }

        // ---------- Материалы и узлы клеток кузова ----------

        float SideWindowTop(float z)
        {
            if (z > -0.95f) return 0.86f;
            float k = Mathf.Clamp01((-0.95f - z) / (-0.95f - SideRearPoint));
            return Mathf.Lerp(0.86f, 0.08f, k * k * (3f - 2f * k) * 0.6f + k * 0.4f);
        }

        protected override CellInfo Classify(float z, int seg, float v, Vector3 p, float side)
        {
            string node = "Shell";
            string mat = "paint";
            string inner = null;
            bool cabin = InCabin(z);

            if (seg == SegE)
            {
                if (z > Header && z < Cowl)
                {
                    // Лобовое стекло с чёрной рамкой, по краям — стойки
                    float edge = Mathf.Min(z - Header, Cowl - z);
                    if (v > 0.10f && edge > 0.035f) mat = "glass";
                    else if (v > 0.055f && edge > 0.006f) mat = "black";
                }
                else if (z < RoofRear && z > Deck)
                {
                    // Заднее стекло-«пузырь»: книзу уже, по бокам — крылья кузова
                    float k = (RoofRear - z) / (RoofRear - Deck);
                    float vmin = Mathf.Lerp(0.12f, 0.36f, k * k);
                    float edge = Mathf.Min(RoofRear - z, z - Deck);
                    if (v > vmin && edge > 0.04f) mat = "glass";
                    else if (v > vmin - 0.05f && edge > 0.012f) mat = "black";
                }
                if (z >= Cowl + 0.015f && z < 2.03f) node = "Hood";
                else if (z <= Header && z >= RoofRear) node = "Roof";
                else if (z < Deck - 0.01f && z > -2.14f) node = "Trunk";
            }
            else if (seg == SegD && cabin)
            {
                float top = SideWindowTop(z);
                float rearPt = SideRearPoint;
                bool inZ = z < Cowl - 0.20f && z > rearPt;
                if (z > Cowl - 0.21f && z < Cowl - 0.01f && v < 0.92f) mat = "black";          // треугольник зеркала
                else if (inZ && v > 0.10f && v < top) mat = Mathf.Abs(z - (DoorRear - 0.04f)) < 0.018f ? "black" : "glass";
                else if (z < Cowl - 0.01f && z > rearPt - 0.03f && v > 0.05f && v < top + 0.07f) mat = "black";
            }

            bool lower = seg == SegB || seg == SegC || seg == SegD;
            if (lower && z < DoorFront && z > DoorRear && p.y > 0.18f) node = side > 0 ? "DoorR" : "DoorL";
            if (seg <= SegC && z > 2.0f) node = "BumperF";
            if (seg <= SegC && z < -2.02f && p.y < 0.72f) node = "BumperR";

            // Изнанка в салоне и в багажнике
            if (mat != "glass" && z < Cowl && z > Deck - 0.32f && !(seg == SegD && z < Deck + 0.04f))
                inner = seg == SegA ? "carpet" : seg == SegE || seg == SegD ? "int_roof" : "int_door";
            return new CellInfo { node = node, mat = mat, innerMat = inner };
        }

        // ---------- Сборка ----------

        protected override Vector3 Pivot(string name)
        {
            switch (name)
            {
                case "Hood": return new Vector3(0f, 0.89f, Cowl + 0.02f);
                case "Roof": return new Vector3(0f, 1.26f, -0.6f);
                case "Trunk": return new Vector3(0f, 0.99f, Deck);
                case "DoorL": return new Vector3(-0.88f, 0.6f, -0.1f);
                case "DoorR": return new Vector3(0.88f, 0.6f, -0.1f);
                case "BumperF": return new Vector3(0f, 0.35f, 2.1f);
                case "BumperR": return new Vector3(0f, 0.45f, -2.1f);
            }
            return Vector3.zero;
        }

        protected override Vector3 Eyes => DriverEyes;

        Model Build()
        {
            StartModel();
            Emit(N);
            SealFirewall(N("Shell"), Cowl - 0.03f, "int_black");
            ArchLiners(N("Shell"), "liner");
            for (int k = 0; k < 2; k++)
            {
                float side = k == 0 ? 1f : -1f;
                Headlight(side);
                FrontFascia(side);
                Taillight(side);
                SideDetails(side);
                Mirror(side);
            }
            CenterDetails();
            Wing();
            Interior();
            BuildWheels();
            return model;
        }

        // ---------- Перед ----------

        /// <summary>Фара-«капля» на углу: тёмный корпус, три круглых линзы, прозрачное стекло поверх.</summary>
        void Headlight(float side)
        {
            var node = body.Child(side < 0 ? "HeadlightL" : "HeadlightR", new Vector3(0.62f * side, 0.6f, 2.0f));
            (Vector3, Vector3) Ray(float a, float b)
            {
                // a: 0 — внешний конец (на боку), 1 — внутренний (на носу); b: низ → верх
                float ang = Mathf.Lerp(4f, 76f, a) * Mathf.Deg2Rad;
                float yc = Mathf.Lerp(0.627f, 0.548f, a);
                float hh = Mathf.Lerp(0.06f, 0.036f, a) * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(2f * a - 1f), 5f)));
                float y = yc + (b - 0.5f) * 2f * hh;
                return (new Vector3(0.36f, y, 1.66f), new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)));
            }
            Patch(node, "housing", 22, 6, Ray, 0.003f, side);
            Patch(node, "lens", 22, 6, Ray, 0.022f, side);
            // Хромированная окантовка
            var rim = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                rim.Add(t < 0.5f ? Ray(t * 2f, 1f) : Ray(1f - (t - 0.5f) * 2f, 0f));
            }
            Ribbon(node, "black", rim, 0.012f, 0.004f, side);
            // Три проектора
            var f = node.WorldFrame();
            foreach (float a in new[] { 0.2f, 0.5f, 0.78f })
            {
                var r = Ray(a, 0.5f);
                OnBody(r.Item1, r.Item2, side, out var p, out var n);
                var lf = Frame.Look(f.ToLocal(p), f.DirToLocal(n), Vector3.up);
                float rad = Mathf.Lerp(0.031f, 0.024f, a);
                var cup = new Frame { o = lf.o, x = lf.x, y = lf.z, z = -lf.y };
                Geo.Lathe(node.M("reflector"), cup, new[] { new Vector2(rad, 0.012f), new Vector2(rad * 0.75f, 0.005f), new Vector2(rad * 0.3f, 0.003f) }, 20, true, true);
                Geo.Torus(node.M("chrome"), cup.Mul(new Frame { o = new Vector3(0, 0.012f, 0), x = Vector3.right, y = Vector3.up, z = Vector3.forward }), rad, 0.0035f, 24, 6);
                Geo.Lathe(node.M("lamp_glow"), cup, new[] { new Vector2(rad * 0.62f, 0.006f), new Vector2(rad * 0.45f, 0.013f), new Vector2(0f, 0.016f) }, 20);
            }
        }

        /// <summary>Бампер: большой «рот», боковые воздухозаборники, поворотники, губа, номер, эмблема.</summary>
        void FrontFascia(float side)
        {
            var bumper = N("BumperF");
            // Половина центрального воздухозаборника (вторую рисует другая сторона)
            Patch(bumper, "grille", 14, 8, (a, b) =>
            {
                float x = a * 0.40f;
                float round = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Max(0f, (x - 0.30f) / 0.10f), 2f)));
                float lo = 0.215f + 0.03f * (1f - round), hi = 0.352f - 0.035f * (1f - round) + 0.01f * (1f - a);
                return (new Vector3(x, Mathf.Lerp(lo, hi, b), 1.6f), Vector3.forward);
            }, 0.002f, side);
            // Перемычки в «роте»
            foreach (float y in new[] { 0.255f, 0.29f, 0.325f })
            {
                var bar = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 12; i++) bar.Add((new Vector3(i / 12f * 0.37f, y, 1.6f), Vector3.forward));
                Ribbon(bumper, "int_black", bar, 0.008f, 0.006f, side);
            }
            // Боковой воздухозаборник
            Patch(bumper, "grille", 10, 6, (a, b) =>
            {
                float x = Mathf.Lerp(0.50f, 0.735f, a);
                float lo = 0.22f + 0.02f * a, hi = 0.33f - 0.035f * a * a;
                return (new Vector3(x, Mathf.Lerp(lo, hi, b), 1.6f), Vector3.forward);
            }, 0.002f, side);
            // Поворотник над ним
            var blink = bumper.Child(side < 0 ? "BlinkFL" : "BlinkFR", new Vector3(0.6f * side, 0.37f, 2.15f));
            Patch(blink, "amber", 8, 3, (a, b) => (new Vector3(Mathf.Lerp(0.52f, 0.70f, a), Mathf.Lerp(0.355f, 0.39f, b) - 0.012f * a, 1.6f), Vector3.forward), 0.004f, side);
            // Губа спойлера под бампером
            var lip = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 24; i++)
            {
                float ang = Mathf.Lerp(90f, 8f, i / 24f) * Mathf.Deg2Rad;
                lip.Add((new Vector3(0.0f, 0.15f, 1.55f), new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang))));
            }
            Ribbon(bumper, "black", lip, 0.035f, 0.004f, side);
            // Боковой габарит
            Patch(bumper, "amber", 4, 2, (a, b) => (new Vector3(0.5f, Mathf.Lerp(0.40f, 0.43f, b), Mathf.Lerp(1.74f, 1.84f, a)), Vector3.right), 0.003f, side);
        }

        // ---------- Корма ----------

        /// <summary>Четыре круглых фонаря на тёмной полосе: снаружи поворотник, два красных, внутри задний ход.</summary>
        void Taillight(float side)
        {
            var node = body.Child(side < 0 ? "TaillightL" : "TaillightR", new Vector3(0.48f * side, 0.785f, -2.2f));
            (Vector3, Vector3) Band(float a, float b)
            {
                float x = Mathf.Lerp(0.115f, 0.86f, a);
                float hh = 0.056f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(2f * a - 1f), 8f)));
                return (new Vector3(x, 0.785f + (b - 0.5f) * 2f * hh, -1.5f), Vector3.back);
            }
            Patch(node, "housing", 30, 6, Band, 0.003f, side);
            var f = node.WorldFrame();
            string[] mats = { "tail_clear", "tail_red", "tail_red", "amber" };
            float[] xs = { 0.235f, 0.405f, 0.575f, 0.745f };
            for (int i = 0; i < 4; i++)
            {
                OnBody(new Vector3(xs[i], 0.785f, -1.5f), Vector3.back, side, out var p, out var n);
                var lampNode = i == 3 ? node.Child(side < 0 ? "BlinkRL" : "BlinkRR") : node;
                var lf = lampNode.WorldFrame();
                var cup = Frame.Look(lf.ToLocal(p), lf.DirToLocal(n), Vector3.up);
                cup = new Frame { o = cup.o, x = cup.x, y = cup.z, z = -cup.y };
                const float r = 0.049f;
                var look = Frame.Look(Vector3.zero, n, Vector3.up);
                Geo.Torus(node.M("chrome"), new Frame { o = f.ToLocal(p + n * 0.007f), x = f.DirToLocal(look.x), y = f.DirToLocal(n), z = f.DirToLocal(look.y) }, r, 0.0045f, 32, 6);
                Geo.Lathe(lampNode.M(mats[i]), cup, new[]
                {
                    new Vector2(r, 0.004f), new Vector2(r * 0.95f, 0.011f), new Vector2(r * 0.7f, 0.016f), new Vector2(r * 0.35f, 0.019f), new Vector2(0f, 0.02f),
                }, 28);
                // Внутреннее кольцо — «глаз» в фонаре, как у настоящей
                Geo.Torus(lampNode.M(mats[i]), cup.Mul(new Frame { o = new Vector3(0, 0.021f, 0), x = Vector3.right, y = Vector3.up, z = Vector3.forward }), r * 0.42f, 0.003f, 20, 5);
            }
        }

        /// <summary>Детали посередине: номера, эмблемы, стоп-сигнал, нижняя юбка, выхлоп, щётки, замок багажника.</summary>
        void CenterDetails()
        {
            var shell = N("Shell");
            var bumperF = N("BumperF");
            var bumperR = N("BumperR");
            Plate(bumperF, new Vector3(0f, 0.405f, 1.6f), Vector3.forward, "PlateFront");
            Plate(bumperR, new Vector3(0f, 0.545f, -1.6f), Vector3.back, "PlateRear");
            // Ниша под номер сзади
            foreach (float side in new[] { 1f, -1f })
            {
                Patch(bumperR, "black", 6, 4, (a, b) => (new Vector3(a * 0.30f, Mathf.Lerp(0.47f, 0.62f, b), -1.6f), Vector3.back), 0.002f, side);
                // Нижняя чёрная юбка с диффузором
                Patch(bumperR, "black", 18, 3, (a, b) => (new Vector3(a * 0.80f, Mathf.Lerp(0.31f, 0.375f, b) + 0.02f * a * a, -1.6f), Vector3.back), 0.002f, side);
                // Катафоты по углам бампера
                Patch(bumperR, "tail_red", 4, 2, (a, b) => (new Vector3(Mathf.Lerp(0.66f, 0.78f, a), Mathf.Lerp(0.43f, 0.455f, b), -1.6f), Vector3.back), 0.003f, side);
                // Шов крышки багажника над фонарями
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++) seam.Add((new Vector3(i / 16f * 0.86f, 0.865f - 0.01f * (i / 16f), -1.6f), Vector3.back));
                Ribbon(N("Trunk"), "black", seam, 0.004f, 0.001f, side);
                // Стоп-сигнал на крышке багажника под стеклом
                Patch(N("Trunk"), "tail_red", 6, 2, (a, b) => (new Vector3(a * 0.2f, 0.6f, Mathf.Lerp(-1.775f, -1.79f, b)), Vector3.up), 0.003f, side);
            }
            // Эмблемы-овалы (без настоящего логотипа)
            Emblem(N("Hood"), new Vector3(0f, 0.5f, 2.0f), new Vector3(0f, 0.25f, 1f), 0.045f);
            Emblem(N("Trunk"), new Vector3(0f, 0.79f, -1.6f), Vector3.back, 0.04f);
            // Выхлоп слева
            OnBody(new Vector3(0.55f, 0.29f, -1.6f), Vector3.back, -1f, out var ep, out _);
            var ef = bumperR.WorldFrame();
            var pipe = new Frame { o = ef.ToLocal(new Vector3(ep.x, 0.29f, ep.z + 0.03f)), x = Vector3.right, y = Vector3.back, z = Vector3.up };
            Geo.Lathe(bumperR.M("chrome"), pipe, new[] { new Vector2(0.047f, -0.05f), new Vector2(0.047f, 0.09f), new Vector2(0.05f, 0.095f), new Vector2(0.044f, 0.1f) }, 24);
            Geo.Lathe(bumperR.M("int_black"), pipe, new[] { new Vector2(0.041f, 0.1f), new Vector2(0.041f, 0.0f), new Vector2(0f, 0.0f) }, 24, false, true);
            // Щётки стеклоочистителя на лобовом стекле
            foreach (var (x0, x1) in new[] { (0.62f, 0.02f), (-0.05f, -0.62f) })
            {
                var wiper = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 10; i++)
                {
                    float t = i / 10f;
                    float x = Mathf.Lerp(x0, x1, t);
                    wiper.Add((new Vector3(Mathf.Abs(x), 0.8f, Mathf.Lerp(0.43f, 0.405f, t)), Vector3.up));
                }
                Ribbon(shell, "black", wiper, 0.016f, 0.008f, x0 < 0f ? -1f : 1f);
            }
            // Чёрный жабо под лобовым стеклом
            foreach (float side in new[] { 1f, -1f })
                Patch(shell, "black", 12, 2, (a, b) => (new Vector3(a * 0.76f, 0.6f, Mathf.Lerp(Cowl + 0.003f, Cowl + 0.05f, b)), Vector3.up), 0.002f, side);
            // Подкапотное пространство — чтобы, когда капот сорвёт, не было видно пустоты
            Geo.Box(shell.M("int_black"), Frame.Identity, new Vector3(0f, 0.42f, 1.15f), new Vector3(1.2f, 0.38f, 1.2f));
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
            // Щели двери
            Ribbon(door, "black", Line(DoorFront, 0.2f, DoorFront - 0.03f, 0.87f), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear, 0.2f, DoorRear + 0.015f, 0.93f), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear, 0.205f, DoorFront, 0.205f, 30), 0.005f, 0.001f, side);
            // Ручка — утопленная щель у заднего края двери
            Ribbon(door, "black", Line(DoorRear + 0.08f, 0.865f, DoorRear + 0.22f, 0.862f, 6), 0.022f, 0.002f, side);
            Ribbon(door, "chrome", Line(DoorRear + 0.085f, 0.876f, DoorRear + 0.215f, 0.873f, 6), 0.004f, 0.003f, side);
            // Воздухозаборник перед задним колесом
            Patch(shell, "grille", 6, 4, (a, b) => (new Vector3(0.3f, Mathf.Lerp(0.30f, 0.41f, b), Mathf.Lerp(-0.94f, -0.83f, a) - 0.05f * b), Vector3.right), 0.002f, side);
            // Шов капота вдоль крыла и спереди
            Ribbon(N("Hood"), "black", Line(Cowl + 0.02f, 0f, 2.03f, 0f, 40).ConvertAll(r => HoodEdgeRay(r.Item1.z)), 0.005f, 0.001f, side);
            // Лючок бака справа
            if (side > 0)
            {
                var hatch = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 28; i++)
                {
                    float a = i / 28f * Mathf.PI * 2f;
                    hatch.Add((new Vector3(0.3f, 0.83f + Mathf.Sin(a) * 0.055f, -1.12f + Mathf.Cos(a) * 0.065f), Vector3.right));
                }
                Ribbon(shell, "black", hatch, 0.004f, 0.001f, side);
            }
            // Чёрная накладка порога
            Ribbon(shell, "black", Line(AxleR + 0.40f, 0.165f, AxleF - 0.39f, 0.165f, 30), 0.03f, 0.002f, side);
        }

        /// <summary>Край капота: чуть внутрь от линии крыла.</summary>
        (Vector3, Vector3) HoodEdgeRay(float z)
        {
            var st = Sec(z);
            float x = st.fb * st.w - 0.03f;
            return (new Vector3(x, st.belt - 0.15f, z), Vector3.up);
        }

        /// <summary>Боковое зеркало цвета кузова на тонкой ножке; стекло повёрнуто к водителю (он справа).</summary>
        void Mirror(float side)
        {
            var door = N(side < 0 ? "DoorL" : "DoorR");
            // Крепится к чёрному треугольнику в начале двери, на линии окон, и выносится наружу
            const float mz = 0.36f;
            var st = Sec(mz);
            var basePt = new Vector3(st.fb * st.w * side, st.belt + 0.015f, mz);
            var df = door.WorldFrame();
            var center = basePt + new Vector3(0.135f * side, 0.075f, -0.035f);
            var mount = door.Child(side < 0 ? "MirrorL" : "MirrorR", df.ToLocal(center));
            Geo.RoundBox(mount.M("paint"), Frame.Identity, Vector3.zero, new Vector3(0.165f, 0.10f, 0.10f), 0.55f, 14);
            // Ножка: от двери к корпусу, наклонная
            var stalkFrom = df.ToLocal(basePt) - mount.pos;
            var stalkTo = new Vector3(-0.05f * side, -0.025f, 0.01f);
            var sf = Frame.Look((stalkFrom + stalkTo) * 0.5f, stalkTo - stalkFrom, Vector3.up);
            Geo.RoundBox(mount.M("paint"), sf, Vector3.zero, new Vector3(0.06f, 0.028f, (stalkTo - stalkFrom).magnitude + 0.03f), 0.4f, 6);
            Geo.RoundBox(door.M("black"), Frame.Identity, df.ToLocal(basePt), new Vector3(0.05f, 0.03f, 0.09f), 0.4f, 6);
            // Стекло: смотрит назад, повёрнуто к водителю
            float yaw = side > 0 ? 27f : -48f;
            var glass = mount.Child(side < 0 ? "MirrorGlassL" : "MirrorGlassR", new Vector3(0f, 0.002f, -0.051f), new Vector3(0f, yaw * 0.25f, 0f));
            Geo.Quad(glass.M("mirror"), Frame.Identity, 0.145f, 0.08f);
        }


        // ---------- Салон (правый руль) ----------

        public static readonly Vector3 DriverEyes = new Vector3(0.37f, 1.065f, -0.86f);
        public static readonly Vector3 SteeringPos = new Vector3(0.37f, 0.83f, -0.30f);
        public const float SteeringTilt = -60f;

        void Interior()
        {
            var cab = body.Child("Interior");
            var id = Frame.Identity;
            // Пол, моторный щит, тоннель
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.225f, -0.45f), new Vector3(1.56f, 0.03f, 1.95f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.45f, 0.6f), new Vector3(1.56f, 0.45f, 0.04f));
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.31f, -0.3f), new Vector3(0.28f, 0.2f, 1.5f), 0.25f, 8);
            // Консоль с рычагом КПП и ручником
            Geo.RoundBox(cab.M("int_grey"), id, new Vector3(0f, 0.43f, -0.42f), new Vector3(0.25f, 0.08f, 0.8f), 0.2f, 8);
            var boot = new Frame { o = new Vector3(0.02f, 0.47f, -0.25f), x = Vector3.right, y = Vector3.up, z = Vector3.forward };
            Geo.Lathe(cab.M("leather_black"), boot, new[] { new Vector2(0.055f, 0f), new Vector2(0.04f, 0.035f), new Vector2(0.018f, 0.06f) }, 16);
            Geo.Cylinder(cab.M("chrome"), Frame.Euler(new Vector3(0.02f, 0.47f, -0.25f), new Vector3(-12f, 0f, 0f)), 0.008f, 0.04f, 0.17f, 8);
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(0.02f, 0.645f, -0.215f), new Vector3(0.05f, 0.05f, 0.05f), 0.9f, 10);
            Geo.RoundBox(cab.M("int_black"), Frame.Euler(new Vector3(-0.08f, 0.5f, -0.6f), new Vector3(-14f, 0f, 0f)), Vector3.zero, new Vector3(0.04f, 0.035f, 0.24f), 0.5f, 6);

            Dashboard(cab);
            Cluster(cab);
            ExtraGauges(cab);
            SteeringWheel(cab);
            foreach (float side in new[] { -1f, 1f }) Seat(cab, side);
            // Задние «детские» сиденья 2+2 и пол багажника
            foreach (float x in new[] { -0.34f, 0.34f })
            {
                Geo.RoundBox(cab.M("leather_red"), id, new Vector3(x, 0.40f, -1.33f), new Vector3(0.5f, 0.11f, 0.36f), 0.35f, 8);
                Geo.RoundBox(cab.M("leather_red"), Frame.Euler(new Vector3(x, 0.42f, -1.52f), new Vector3(-28f, 0f, 0f)), new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.42f, 0.1f), 0.35f, 8);
            }
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.58f, -1.88f), new Vector3(1.1f, 0.03f, 0.36f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.42f, -1.68f), new Vector3(1.3f, 0.32f, 0.04f));
            // Подлокотники на дверях
            foreach (float side in new[] { -1f, 1f })
            {
                var door = N(side < 0 ? "DoorL" : "DoorR");
                var df = door.WorldFrame();
                Geo.RoundBox(door.M("leather_red"), Frame.Identity, df.ToLocal(new Vector3(0.755f * side, 0.6f, -0.45f)), new Vector3(0.08f, 0.05f, 0.6f), 0.4f, 8);
                Geo.RoundBox(door.M("int_grey"), Frame.Identity, df.ToLocal(new Vector3(0.775f * side, 0.74f, -0.1f)), new Vector3(0.04f, 0.04f, 0.18f), 0.4f, 6);
            }
            // Салонное зеркало: стекло повёрнуто к водителю справа
            Geo.Cylinder(cab.M("int_black"), Frame.Euler(new Vector3(0f, 1.2f, -0.185f), Vector3.zero), 0.012f, -0.03f, 0.015f, 8);
            var rm = cab.Child("RearMirror", new Vector3(0f, 1.155f, -0.19f), new Vector3(0f, -21f, 0f));
            Geo.RoundBox(rm.M("int_black"), Frame.Identity, new Vector3(0f, 0f, 0.016f), new Vector3(0.27f, 0.075f, 0.035f), 0.5f, 10);
            var rmGlass = rm.Child("RearMirrorGlass", new Vector3(0f, 0f, -0.0025f));
            Geo.Quad(rmGlass.M("mirror"), Frame.Identity, 0.25f, 0.06f);
            // Козырьки
            foreach (float x in new[] { -0.38f, 0.38f })
                Geo.RoundBox(cab.M("int_roof"), Frame.Euler(new Vector3(x, 1.205f, -0.30f), new Vector3(-10f, 0f, 0f)), Vector3.zero, new Vector3(0.40f, 0.022f, 0.16f), 0.4f, 6);
            // Водитель (туловище и голову прячем при виде из салона, руки достраивает игра)
            Driver(0.78f, -0.95f, -16f);
        }

        /// <summary>Торпеда: верх в красной коже, лицевая часть серебристая, у пассажира — вентиляция и бардачок.</summary>
        void Dashboard(ModelNode cab)
        {
            var prof = new[]
            {
                new Vector2(0.47f, 0.862f), new Vector2(0.30f, 0.886f), new Vector2(0.12f, 0.9f), new Vector2(0.03f, 0.897f),
                new Vector2(-0.015f, 0.875f), new Vector2(-0.02f, 0.83f), new Vector2(0.0f, 0.70f), new Vector2(0.05f, 0.56f), new Vector2(0.2f, 0.48f),
            };
            Vector2 Prof(float t)
            {
                float f = t * (prof.Length - 1);
                int i = Mathf.Min(prof.Length - 2, (int)f);
                float k = f - i;
                var p0 = prof[Mathf.Max(0, i - 1)]; var p1 = prof[i]; var p2 = prof[i + 1]; var p3 = prof[Mathf.Min(prof.Length - 1, i + 2)];
                // Катмулл–Ром — гладко через все точки
                return 0.5f * (2f * p1 + (-p0 + p2) * k + (2f * p0 - 5f * p1 + 4f * p2 - p3) * k * k + (-p0 + 3f * p1 - 3f * p2 + p3) * k * k * k);
            }
            Vector3 P(float a, float b)
            {
                float x = Mathf.Lerp(-0.79f, 0.79f, a);
                var q = Prof(b);
                // У водителя торпеда «обнимает» его: выступает назад
                float wrap = 0.05f * Mathf.Exp(-Mathf.Pow((x - 0.37f) / 0.28f, 2f));
                return new Vector3(x, q.y, q.x - wrap * Mathf.Clamp01(b * 2f));
            }
            Geo.Surface(cab.M("leather_red"), Frame.Identity, 40, 10, P, false, (a, b) => b < 0.5f);
            Geo.Surface(cab.M("int_grey"), Frame.Identity, 40, 10, P, false, (a, b) => b >= 0.5f);
            // Дефлекторы и бардачок у пассажира (слева)
            foreach (float x in new[] { -0.62f, -0.12f })
                Geo.RoundBox(cab.M("int_black"), Frame.Identity, new Vector3(x, 0.80f, -0.012f), new Vector3(0.12f, 0.05f, 0.02f), 0.3f, 6);
            Geo.RoundBox(cab.M("int_black"), Frame.Identity, new Vector3(-0.42f, 0.66f, 0.004f), new Vector3(0.42f, 0.006f, 0.01f), 0.3f, 4);
            // Центральная консоль, развёрнутая к водителю
            var stack = cab.Child("CenterStack", new Vector3(0.03f, 0.68f, 0.0f), new Vector3(8f, -10f, 0f));
            Geo.RoundBox(stack.M("int_grey"), Frame.Identity, Vector3.zero, new Vector3(0.32f, 0.36f, 0.08f), 0.25f, 8);
            Geo.RoundBox(stack.M("int_black"), Frame.Identity, new Vector3(0f, 0.07f, -0.035f), new Vector3(0.22f, 0.07f, 0.02f), 0.2f, 6);
            var screen = stack.Child("RadioScreen", new Vector3(0f, 0.07f, -0.047f));
            Geo.Quad(screen.M("screen"), Frame.Identity, 0.16f, 0.04f);
            for (int i = 0; i < 3; i++)
                Geo.Cylinder(stack.M("chrome"), Frame.Euler(new Vector3(-0.08f + i * 0.08f, -0.06f, -0.04f), new Vector3(-90f, 0f, 0f)), 0.018f, 0f, 0.02f, 14);
            foreach (float x in new[] { -0.08f, 0.08f })
                Geo.RoundBox(stack.M("int_black"), Frame.Identity, new Vector3(x, 0.15f, -0.035f), new Vector3(0.1f, 0.04f, 0.02f), 0.3f, 6);
        }

        /// <summary>Щиток: козырёк-«капюшон», тахометр по центру, спидометр слева, топливо справа.</summary>
        void Cluster(ModelNode cab)
        {
            var c = new Vector3(0.37f, 0.87f, -0.075f);
            // Козырёк
            Vector3 Hood(float a, float b)
            {
                float th = Mathf.Lerp(-0.25f, Mathf.PI + 0.25f, a);
                float r = 0.235f;
                return c + new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r * 0.5f + 0.03f, Mathf.Lerp(0.06f, -0.06f, b) - 0.02f * Mathf.Sin(th));
            }
            Geo.Surface(cab.M("int_black"), Frame.Identity, 30, 4, Hood, true);
            Geo.Surface(cab.M("int_black"), Frame.Identity, 30, 4, Hood, false);
            Geo.RoundBox(cab.M("gauge_face"), Frame.Identity, c + new Vector3(0f, -0.005f, 0.05f), new Vector3(0.46f, 0.17f, 0.02f), 0.3f, 6);
            Gauge(cab, "Speed", c + new Vector3(-0.155f, -0.012f, 0.03f), 0.058f, true);
            Gauge(cab, "Tach", c + new Vector3(0f, -0.005f, 0.025f), 0.068f, true);
            Gauge(cab, "Fuel", c + new Vector3(0.155f, -0.012f, 0.03f), 0.05f, true);
            // Лампы бензина и мотора
            var fl = cab.Child("FuelLamp", c + new Vector3(0.155f, -0.045f, 0.016f));
            Geo.RoundBox(fl.M("lamp_off"), Frame.Identity, Vector3.zero, new Vector3(0.022f, 0.012f, 0.004f), 0.3f, 4);
            var el = cab.Child("EngineLamp", c + new Vector3(-0.155f, -0.05f, 0.016f));
            Geo.RoundBox(el.M("lamp_off"), Frame.Identity, Vector3.zero, new Vector3(0.022f, 0.012f, 0.004f), 0.3f, 4);
        }

        /// <summary>Дополнительные приборы с красной подсветкой: три на правой стойке и три на торпеде.</summary>
        void ExtraGauges(ModelNode cab)
        {
            var baseP = new Vector3(0.70f, 0.915f, 0.39f);
            var topP = new Vector3(0.6f, 1.17f, -0.17f);
            var pod = cab.Child("PillarPod");
            for (int i = 0; i < 3; i++)
            {
                var p = Vector3.Lerp(baseP, topP, 0.2f + i * 0.2f) + new Vector3(-0.085f, -0.015f, -0.05f);
                Geo.Cylinder(pod.M("int_black"), Frame.Look(p, (p - DriverEyes).normalized, Vector3.up).Mul(new Frame { o = Vector3.zero, x = Vector3.right, y = Vector3.forward, z = Vector3.down }), 0.036f, 0f, 0.06f, 18);
                Gauge(pod, "Pillar" + i, p - (p - DriverEyes).normalized * 0.003f, 0.031f, true, "gauge_glow", "int_black");
            }
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(-0.07f + i * 0.075f, 0.935f, 0.13f);
                Geo.Cylinder(pod.M("int_black"), Frame.Look(p, (p - DriverEyes).normalized, Vector3.up).Mul(new Frame { o = Vector3.zero, x = Vector3.right, y = Vector3.forward, z = Vector3.down }), 0.033f, 0f, 0.05f, 18);
                Gauge(pod, "Dash" + i, p - (p - DriverEyes).normalized * 0.003f, 0.029f, true, "gauge_glow", "int_black");
            }
        }

        /// <summary>Руль: чёрный обод с красной кожей снизу, три спицы, колонка к торпеде. Узел наклонён к водителю.</summary>
        void SteeringWheel(ModelNode cab)
        {
            var w = body.Child("SteeringWheel", SteeringPos, new Vector3(SteeringTilt, 0f, 0f));
            var id = Frame.Identity;
            const float R = 0.18f;
            Geo.Torus(w.M("leather_black"), id, R, 0.017f, 40, 10, 25f, 155f);
            Geo.Torus(w.M("leather_black"), id, R, 0.017f, 20, 10, 155f, 205f);
            Geo.Torus(w.M("leather_red"), id, R, 0.017f, 30, 10, 205f, 335f);
            Geo.Torus(w.M("leather_black"), id, R, 0.017f, 20, 10, 335f, 385f);
            foreach (var (dir, len) in new[] { (Vector3.right, R), (Vector3.left, R), (Vector3.back, R) })
            {
                var f = Frame.Look(dir * len * 0.5f, dir, Vector3.up);
                Geo.RoundBox(w.M("int_black"), f, Vector3.zero, new Vector3(0.045f, 0.018f, len * 0.95f), 0.4f, 6);
            }
            Geo.RoundBox(w.M("int_black"), id, new Vector3(0f, 0.012f, -0.005f), new Vector3(0.12f, 0.05f, 0.1f), 0.5f, 10);
            var emb = new Frame { o = new Vector3(0f, 0.038f, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward * 0.62f };
            Geo.Torus(w.M("chrome"), emb, 0.022f, 0.003f, 24, 5);
            Geo.Cylinder(w.M("int_black"), id, 0.035f, -0.3f, -0.01f, 12);
        }

        /// <summary>Ковшеобразное сиденье: красные подушки, чёрные боковины.</summary>
        void Seat(ModelNode cab, float side)
        {
            float x = 0.37f * side;
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(x, 0.30f, -0.8f), new Vector3(0.5f, 0.1f, 0.52f), 0.3f, 8);
            Geo.RoundBox(cab.M("leather_red"), id, new Vector3(x, 0.37f, -0.8f), new Vector3(0.34f, 0.08f, 0.5f), 0.4f, 8);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(cab.M("leather_black"), id, new Vector3(x + b * 0.2f, 0.385f, -0.82f), new Vector3(0.1f, 0.12f, 0.48f), 0.5f, 8);
            var back = cab.Child(side > 0 ? "SeatBackR" : "SeatBackL", new Vector3(x, 0.36f, -1.04f), new Vector3(-13f, 0f, 0f));
            Geo.RoundBox(back.M("leather_black"), id, new Vector3(0f, 0.31f, -0.02f), new Vector3(0.48f, 0.62f, 0.12f), 0.35f, 8);
            Geo.RoundBox(back.M("leather_red"), id, new Vector3(0f, 0.28f, 0.035f), new Vector3(0.3f, 0.48f, 0.06f), 0.4f, 8);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(back.M("leather_black"), id, new Vector3(b * 0.2f, 0.25f, 0.05f), new Vector3(0.09f, 0.46f, 0.14f), 0.5f, 8);
            Geo.RoundBox(back.M("leather_black"), id, new Vector3(0f, 0.68f, -0.01f), new Vector3(0.26f, 0.18f, 0.11f), 0.55f, 8);
        }

        // ---------- Антикрыло ----------

        /// <summary>Знаменитое «петлёй»: две стойки по краям переходят в крыло дугами.</summary>
        void Wing()
        {
            var trunk = N("Trunk");
            var f = trunk.WorldFrame();
            const float half = 0.655f, top = 1.155f, rr = 0.085f, zc = -1.985f;
            var path = new List<Vector3>();
            void Add(float x, float y) => path.Add(f.ToLocal(new Vector3(x, y, zc)));
            for (int i = 0; i <= 6; i++) Add(-half, Mathf.Lerp(0.945f, top - rr, i / 6f));
            for (int i = 1; i <= 10; i++)
            {
                float a = Mathf.PI - i / 10f * Mathf.PI / 2f;
                Add(-half + rr + Mathf.Cos(a) * rr, top - rr + Mathf.Sin(a) * rr);
            }
            for (int i = 1; i < 20; i++) Add(Mathf.Lerp(-half + rr, half - rr, i / 20f), top);
            for (int i = 0; i <= 10; i++)
            {
                float a = Mathf.PI / 2f - i / 10f * Mathf.PI / 2f;
                Add(half - rr + Mathf.Cos(a) * rr, top - rr + Mathf.Sin(a) * rr);
            }
            for (int i = 1; i <= 6; i++) Add(half, Mathf.Lerp(top - rr, 0.945f, i / 6f));
            // Профиль крыла: каплевидный, задняя кромка тонкая и чуть выше передней
            var foil = new List<Vector2>();
            const int nf = 18;
            for (int i = 0; i < nf * 2; i++)
            {
                float t = i < nf ? i / (float)nf : (2 * nf - i) / (float)nf;
                float xx = (1f - Mathf.Cos(t * Mathf.PI)) / 2f; // 0 — передняя кромка, 1 — задняя
                float th = 0.42f * (0.2969f * Mathf.Sqrt(xx) - 0.126f * xx - 0.3516f * xx * xx + 0.2843f * xx * xx * xx - 0.1036f * xx * xx * xx * xx);
                float yy = (i < nf ? th : -th) * 0.22f;
                float cx = (xx - 0.45f) * 0.22f;
                foil.Add(new Vector2(cx, yy + xx * 0.012f));
            }
            int count = path.Count;
            // Начальная «правая» ось = −Z: хорда крыла вдоль машины, толщина — поперёк пути
            Geo.Sweep(trunk.M("paint"), Frame.Identity, path, foil, false, Vector3.left, true,
                t => Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(Mathf.Min(t, 1f - t) * 4f)));
        }

        void BuildWheels()
        {
            foreach (float side in new[] { -1f, 1f })
            foreach (bool front in new[] { true, false })
            {
                float x = (front ? TrackF : TrackR) * side;
                float z = front ? AxleF : AxleR;
                string tag = (front ? "F" : "R") + (side < 0 ? "L" : "R");
                var mount = model.root.Child((front ? "Steer" : "Mount") + tag, new Vector3(x, WheelR, z));
                var wheel = mount.Child("Wheel" + tag, Vector3.zero, new Vector3(0f, 0f, 90f));
                // Колёсный узел крутится вокруг своей оси Y; лицевая сторона диска — наружу
                var mesh = wheel.Child("Rim" + tag, Vector3.zero, side < 0 ? Vector3.zero : new Vector3(180f, 0f, 0f));
                WheelModel.FiveSpoke(mesh, WheelR, 0.235f);
                WheelModel.Caliper(mount, side);
            }
        }
    }

    /// <summary>Модель — дерево узлов с сетками.</summary>
    public class Model
    {
        public readonly ModelNode root = new ModelNode { name = "Root" };
    }
}

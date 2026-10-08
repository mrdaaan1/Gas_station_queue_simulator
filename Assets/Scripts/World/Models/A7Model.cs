using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «Audi A7 Sportback» (C8, 2018–) — серебристый пятидверный фастбэк.
    /// Размеры как у настоящего: 4,97 × 1,91 × 1,42 м, база 2,93 м, колёса 20" (255/40).
    /// Длинный капот, крыша плавно стекает до короткой крышки багажника с выдвижным спойлером, безрамочные двери,
    /// острая линия плеч над колёсами, шестигранная решётка Singleframe с четырьмя кольцами, узкие фары с сегментами ДХО,
    /// сквозная светодиодная полоса между фонарями, диффузор с трапециями выхлопа.
    /// Салон светлый: бежевая кожа, чёрный верх торпеды, два экрана MMI, цифровой щиток, руль слева.
    /// </summary>
    public class A7Model : SportsCarModel
    {
        public const float AxleF = 1.49f, AxleR = -1.44f;
        public const float WheelR = 0.356f, Track = 0.80f;
        const float W = 0.955f;
        const float FrontDoorF = 0.96f, DoorSplit = -0.33f, RearDoorR = -1.20f;

        public static readonly Vector3 DriverEyes = new Vector3(-0.37f, 1.13f, -0.30f);
        public static readonly Vector3 SteeringPos = new Vector3(-0.37f, 0.83f, 0.17f);
        public const float SteeringTilt = -66f;

        static Model cached;

        public static Model Get()
        {
            if (cached == null) cached = new A7Model().Build();
            return cached;
        }

        public static A7Model Shape() => new A7Model();

        protected override Vector3 Eyes => DriverEyes;

        A7Model()
        {
            Front = 2.48f; Rear = -2.49f;
            Deck = -2.02f; RoofRear = -0.80f; Header = 0.38f; Cowl = 0.97f;
            FrontRound = 2.02f; RearRound = -2.12f; FrontPow = 2.4f; RearPow = 3.0f;
            RockerFrac = 0.90f; RockerAngle = 35f;

            W0 = new Curve().Key(-2.49f, 0.90f).Key(-1.44f, W).Key(0f, 0.945f).Key(1.49f, W).Key(2.48f, 0.90f);
            YBot = new Curve().Key(-2.49f, 0.30f).Key(-2.42f, 0.22f).Key(-2.2f, 0.19f).Key(-1.8f, 0.18f).Key(0f, 0.17f).Key(1.8f, 0.18f).Key(2.3f, 0.18f).Key(2.48f, 0.26f);
            // Самое широкое место — «плечо» над колёсами: борт ниже выпуклый
            YMax = new Curve().Key(-2.49f, 0.58f).Key(-2.3f, 0.64f).Key(-1.44f, 0.68f).Key(0f, 0.62f).Key(1.49f, 0.66f).Key(2.3f, 0.62f).Key(2.48f, 0.52f);
            // Линия окон: почти ровная, к корме поднимается; на носу — кромка крыла, опускается к фарам
            YBelt = new Curve().Key(-2.49f, 0.92f).Key(-2.40f, 1.01f).Key(Deck, 1.06f, true).Key(-1.6f, 1.035f).Key(-0.6f, 1.0f).Key(0.5f, 0.985f)
                .Key(Cowl, 0.975f, true).Key(1.4f, 0.93f).Key(1.9f, 0.875f).Key(2.2f, 0.82f).Key(2.36f, 0.77f).Key(2.44f, 0.72f).Key(2.48f, 0.62f);
            FBelt = new Curve().Key(-2.49f, 0.90f).Key(-1.6f, 0.885f).Key(0.6f, 0.885f).Key(Cowl, 0.89f, true).Key(2.0f, 0.90f).Key(2.48f, 0.90f);
            // Осевая линия верха: короткая крышка багажника, пологое заднее стекло, крыша, лобовое, длинный капот
            YTop = new Curve().Key(-2.49f, 0.94f).Key(-2.42f, 1.035f).Key(-2.30f, 1.06f).Key(Deck, 1.085f, true).Key(-1.6f, 1.205f)
                .Key(-1.2f, 1.30f).Key(RoofRear, 1.375f).Key(-0.55f, 1.405f).Key(-0.25f, 1.42f).Key(0.1f, 1.41f).Key(Header, 1.37f, true)
                .Key(0.7f, 1.17f).Key(Cowl, 1.0f, true).Key(1.3f, 0.965f).Key(1.8f, 0.915f).Key(2.15f, 0.865f).Key(2.33f, 0.83f)
                .Key(2.42f, 0.79f).Key(2.46f, 0.745f).Key(2.48f, 0.66f);
            YRoofEdge = new Curve().Key(Deck, 1.06f, true).Key(-1.75f, 1.12f).Key(-1.3f, 1.258f).Key(RoofRear, 1.352f).Key(-0.55f, 1.378f).Key(-0.2f, 1.388f)
                .Key(Header, 1.345f, true).Key(0.7f, 1.14f).Key(Cowl, 0.975f, true);
            XRoofEdge = new Curve().Key(Deck, 0.80f, true).Key(-1.75f, 0.76f).Key(-1.3f, 0.70f).Key(RoofRear, 0.67f).Key(-0.3f, 0.66f)
                .Key(Header, 0.675f, true).Key(0.7f, 0.76f).Key(Cowl, 0.845f, true);
            AngC = new Curve().Key(-2.49f, 100f).Key(2.48f, 100f);
            AngE0 = new Curve().Key(-2.49f, 160f).Key(Deck, 150f).Key(-1.3f, 142f).Key(RoofRear, 140f).Key(Header, 140f).Key(Cowl, 155f).Key(1.4f, 162f).Key(2.48f, 166f);
            AngD0 = new Curve().Key(-2.49f, 104f).Key(2.48f, 104f);
            AngD1 = new Curve().Key(-2.49f, 122f).Key(2.48f, 122f);

            arches.Add(new Arch { z = AxleF, y = WheelR, r = 0.415f, xMin = 0.64f });
            arches.Add(new Arch { z = AxleR, y = WheelR, r = 0.415f, xMin = 0.64f });
            Prepare();
        }

        // ---------- Клетки кузова ----------

        static bool Between(float z, float a, float b) => z > Mathf.Min(a, b) && z < Mathf.Max(a, b);

        /// <summary>Заднее боковое стекло сужается к корме: его низ поднимается к краю крыши.</summary>
        const float WinTip = -1.68f;

        protected override CellInfo Classify(float z, int seg, float v, Vector3 p, float side)
        {
            string node = "Shell", mat = "paint", inner = null;
            if (seg == SegA) mat = "chassis";
            if (seg == SegE)
            {
                if (z > Header && z < Cowl)
                {
                    float edge = Mathf.Min(z - Header, Cowl - z);
                    if (v > 0.06f && edge > 0.03f) mat = "glass";
                    else if (v > 0.035f && edge > 0.006f) mat = "black";
                }
                else if (z > Deck && z < RoofRear)
                {
                    // Пологое заднее стекло фастбэка в чёрной рамке
                    float edge = Mathf.Min(z - Deck, RoofRear - 0.12f - z);
                    // Стекло уже кузова: по бокам широкие стойки C цвета кузова
                    if (v > 0.30f && edge > 0.05f) mat = "glass";
                    else if (v > 0.26f && edge > 0.012f) mat = "black";
                }
                if (z > Cowl + 0.015f && z < 2.30f) node = "Hood";
                else if (z < Header && z >= RoofRear) node = "Roof";
                else if (z < RoofRear) node = "Tailgate"; // подъёмная дверь вместе со стеклом
            }
            else if (seg == SegD && InCabin(z))
            {
                // Безрамочные двери: окно передней двери, задней и заднее боковое «треугольником». Стойка B чёрная.
                bool win = Between(z, -0.27f, 0.80f) || Between(z, -1.06f, -0.40f) || Between(z, WinTip, -1.12f);
                if (win && v > 0.025f && v < 0.975f) mat = "glass";
                else if (v > 0.03f && z < 0.90f && z > WinTip - 0.03f) mat = "black";
                else if (z >= 0.90f && v > 0.03f) mat = "black"; // стойка A
            }
            bool lower = seg == SegB || seg == SegC || seg == SegD;
            if (lower && p.y > 0.30f)
            {
                if (Between(z, DoorSplit, FrontDoorF)) node = side > 0 ? "DoorFR" : "DoorFL";
                else if (Between(z, RearDoorR, DoorSplit)) node = side > 0 ? "DoorRR" : "DoorRL";
            }
            bool bumperRow = seg <= SegB || (seg == SegC && v < 0.30f);
            if (z < -2.28f && seg >= SegB && !bumperRow) node = "Tailgate";
            if (bumperRow && z > 2.16f) node = "BumperF";
            if (bumperRow && z < -2.14f) node = "BumperR";
            if (mat != "glass" && z < Cowl) inner = seg == SegA ? "carpet" : seg >= SegD ? "int_roof_light" : "int_beige";
            return new CellInfo { node = node, mat = mat, innerMat = inner };
        }

        protected override Vector3 Pivot(string name)
        {
            switch (name)
            {
                case "Hood": return new Vector3(0f, 1.0f, Cowl + 0.02f);
                case "Roof": return new Vector3(0f, 1.40f, -0.2f);
                case "Tailgate": return new Vector3(0f, 1.36f, -0.82f); // петли у края крыши — дверь поднимается вместе со стеклом
                case "DoorFL": return new Vector3(-W, 0.7f, FrontDoorF);
                case "DoorFR": return new Vector3(W, 0.7f, FrontDoorF);
                case "DoorRL": return new Vector3(-W, 0.7f, DoorSplit);
                case "DoorRR": return new Vector3(W, 0.7f, DoorSplit);
                case "BumperF": return new Vector3(0f, 0.4f, 2.35f);
                case "BumperR": return new Vector3(0f, 0.45f, -2.35f);
            }
            return Vector3.zero;
        }

        // ---------- Сборка ----------

        Model Build()
        {
            StartModel();
            Emit(N, 210f);
            foreach (var n in new[] { "Hood", "Roof", "Tailgate", "DoorFL", "DoorFR", "DoorRL", "DoorRR", "BumperF", "BumperR" }) N(n);
            SealFirewall(N("Shell"), Cowl - 0.03f, "int_black");
            ArchLiners(N("Shell"), "liner");
            foreach (float side in new[] { -1f, 1f })
            {
                Headlight(side);
                SideDetails(side);
                Mirror(side);
                Taillight(side);
                foreach (var a in arches) ArchLip(a, side);
            }
            FrontEnd();
            Wipers();
            RearDetails();
            Chassis();
            Interior();
            BuildWheels();
            return model;
        }

        /// <summary>Кромка арки: тонкий отбортованный край цвета кузова — скрывает ступеньки клеток по краю выреза.</summary>
        void ArchLip(Arch a, float side)
        {
            var m = N("Shell").M("paint");
            float yb = 0.30f;
            float th0 = Mathf.Asin(Mathf.Clamp((yb - a.y) / a.r, -1f, 1f));
            float th1 = Mathf.PI - th0;
            Vector3 P(float u, float b)
            {
                float th = Mathf.Lerp(th0, th1, u);
                float phi = b * Mathf.PI * 2f;
                float cs = Mathf.Cos(phi), sn = Mathf.Sin(phi);
                float r = a.r + 0.012f + Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), 0.5f) * 0.016f;
                float x = W - 0.02f + Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.5f) * 0.02f;
                // бок кузова у арки чуть уже полуширины — берём реальную ширину на этой высоте
                var st = Sec(a.z + Mathf.Cos(th) * a.r);
                float hw = HalfWidthAt(st, a.y + Mathf.Sin(th) * a.r);
                if (hw > 0f) x = Mathf.Min(x, hw + 0.004f + Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.5f) * 0.008f);
                return new Vector3(side * x, a.y + Mathf.Sin(th) * r, a.z + Mathf.Cos(th) * r);
            }
            Geo.Surface(m, Frame.Identity, 48, 10, P, side < 0);
        }

        static Frame Facing(Vector3 o, Vector3 n)
        {
            var lf = Frame.Look(o, n, Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up);
            return new Frame { o = lf.o, x = lf.x, y = lf.z, z = -lf.y };
        }

        Vector3 FrontPoint(float x, float y, out Vector3 n)
        {
            OnBody(new Vector3(Mathf.Abs(x), y, 1.9f), Vector3.forward, Mathf.Sign(x + 1e-6f), out var p, out n);
            return p;
        }

        Vector3 RearPoint(float x, float y, out Vector3 n)
        {
            OnBody(new Vector3(Mathf.Abs(x), y, -1.9f), Vector3.back, Mathf.Sign(x + 1e-6f), out var p, out n);
            return p;
        }

        Vector3 SidePoint(float side, float y, float z, out Vector3 n)
        {
            OnBody(new Vector3(0.5f, y, z), Vector3.right, side, out var p, out n);
            return p;
        }

        /// <summary>Деталь на кузове спереди/сзади: x от x0 до x1 (правая половина), высота от yb(t) до yt(t).</summary>
        void Lamp(ModelNode node, string mat, float x0, float x1, System.Func<float, float> yb, System.Func<float, float> yt,
            float offset, float side, bool rear = false, int na = 16, int nb = 6)
        {
            float z0 = rear ? -1.9f : 1.9f;
            var dir = rear ? Vector3.back : Vector3.forward;
            Patch(node, mat, na, nb, (a, b) => (new Vector3(Mathf.Lerp(x0, x1, a), Mathf.Lerp(yb(a), yt(a), b), z0), dir), offset, side);
        }

        static List<(Vector3, Vector3)> Outline(float x0, float x1, System.Func<float, float> yb, System.Func<float, float> yt, bool rear, int n = 14)
        {
            float z0 = rear ? -1.9f : 1.9f;
            var dir = rear ? Vector3.back : Vector3.forward;
            var list = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= n; i++) { float t = i / (float)n; list.Add((new Vector3(Mathf.Lerp(x0, x1, t), yb(t), z0), dir)); }
            for (int i = 1; i <= 4; i++) list.Add((new Vector3(x1, Mathf.Lerp(yb(1f), yt(1f), i / 4f), z0), dir));
            for (int i = n - 1; i >= 0; i--) { float t = i / (float)n; list.Add((new Vector3(Mathf.Lerp(x0, x1, t), yt(t), z0), dir)); }
            for (int i = 1; i <= 4; i++) list.Add((new Vector3(x0, Mathf.Lerp(yt(0f), yb(0f), i / 4f), z0), dir));
            return list;
        }

        static List<(Vector3, Vector3)> Line(Vector3 a, Vector3 b, Vector3 dir, int n = 12)
        {
            var list = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= n; i++) list.Add((Vector3.Lerp(a, b, i / (float)n), dir));
            return list;
        }

        // ---------- Перед ----------

        /// <summary>
        /// Узкая фара под кромкой капота: внутренний конец ниже и острее, наружный заворачивает на крыло.
        /// Внизу — 12 вертикальных сегментов ДХО, над ними две линзы, сверху тонкая светящаяся бровь.
        /// </summary>
        void Headlight(float side)
        {
            var c = FrontPoint(0.70f * side, 0.71f, out _);
            var node = body.Child(side < 0 ? "HeadlightL" : "HeadlightR", c);
            System.Func<float, float> yb = t => Mathf.Lerp(0.665f, 0.685f, t), yt = t => Mathf.Lerp(0.715f, 0.775f, Mathf.Sqrt(t));
            Lamp(node, "housing", 0.50f, 0.90f, yb, yt, 0.004f, side);
            Lamp(node, "lens", 0.50f, 0.90f, yb, yt, 0.018f, side);
            Ribbon(node, "black", Outline(0.50f, 0.90f, yb, yt, false), 0.006f, 0.012f, side);
            // Сегменты ДХО: короткие светящиеся планки по низу фары
            for (int i = 0; i < 12; i++)
            {
                float t = 0.04f + i * 0.075f;
                float x = Mathf.Lerp(0.50f, 0.90f, t);
                float y0 = yb(t) + 0.006f;
                Ribbon(node, "lamp_glow", Line(new Vector3(x, y0, 1.9f), new Vector3(x + 0.004f, y0 + 0.022f, 1.9f), Vector3.forward, 2), 0.011f, 0.014f, side);
            }
            var brow = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 16; i++) { float t = i / 16f; brow.Add((new Vector3(Mathf.Lerp(0.53f, 0.89f, t), yt(t) - 0.008f, 1.9f), Vector3.forward)); }
            Ribbon(node, "lamp_glow", brow, 0.004f, 0.014f, side);
            var f = node.WorldFrame();
            foreach (var (x, r) in new[] { (0.66f, 0.016f), (0.74f, 0.016f), (0.82f, 0.014f) })
            {
                float t = (x - 0.50f) / 0.40f;
                var p = FrontPoint(x * side, (yb(t) + yt(t)) * 0.5f + 0.008f, out var n);
                var rf = Facing(f.ToLocal(p + n * 0.008f), f.DirToLocal(n));
                Geo.Torus(node.M("chrome"), rf, r, 0.003f, 24, 5);
                Geo.Lathe(node.M("glass_dark"), rf, new[] { new Vector2(r * 0.95f, 0.001f), new Vector2(r * 0.6f, 0.006f), new Vector2(0f, 0.008f) }, 20);
            }
            var blink = node.Child(side < 0 ? "BlinkFL" : "BlinkFR");
            Lamp(blink, "amber", 0.80f, 0.895f, t => yt(Mathf.Lerp(0.75f, 1f, t)) - 0.02f, t => yt(Mathf.Lerp(0.75f, 1f, t)) - 0.01f, 0.015f, side, false, 6, 2);
        }

        /// <summary>
        /// Решётка Singleframe: широкий шестиугольник в чёрной рамке, горизонтальные планки, четыре кольца;
        /// боковые заборники-трапеции, губа и номер.
        /// </summary>
        void FrontEnd()
        {
            var bf = N("BumperF");
            System.Func<float, float> gx = t => Mathf.Lerp(0f, 0.475f, t);
            System.Func<float, float> gt = t => { float x = gx(t); return x < 0.40f ? 0.735f : Mathf.Lerp(0.735f, 0.60f, (x - 0.40f) / 0.075f); };
            System.Func<float, float> gb = t => { float x = gx(t); return x < 0.37f ? 0.31f : Mathf.Lerp(0.31f, 0.56f, (x - 0.37f) / 0.105f); };
            foreach (float side in new[] { -1f, 1f })
            {
                Lamp(bf, "grille", 0f, 0.475f, gb, gt, 0.006f, side, false, 18, 10);
                Ribbon(bf, "black_satin", Outline(0f, 0.475f, gb, gt, false, 20), 0.022f, 0.012f, side);
                // Горизонтальные планки
                for (int i = 1; i <= 7; i++)
                {
                    float y = Mathf.Lerp(0.32f, 0.725f, i / 8f);
                    float xe = y > 0.60f ? Mathf.Lerp(0.40f, 0.47f, (0.735f - y) / 0.135f) : Mathf.Lerp(0.37f, 0.47f, (y - 0.31f) / 0.25f);
                    var rib = new List<(Vector3, Vector3)>();
                    for (int k = 0; k <= 10; k++) rib.Add((new Vector3(Mathf.Lerp(0.0f, xe - 0.012f, k / 10f), y, 1.9f), Vector3.forward));
                    Ribbon(bf, "housing", rib, 0.010f, 0.012f, side);
                }
                // Боковые заборники: трапеция, внутри — две планки
                System.Func<float, float> sb = t => Mathf.Lerp(0.25f, 0.24f, t), st = t => Mathf.Lerp(0.43f, 0.50f, t);
                Lamp(bf, "grille", 0.56f, 0.86f, sb, st, 0.004f, side, false, 12, 6);
                Ribbon(bf, "black_satin", Outline(0.56f, 0.86f, sb, st, false), 0.014f, 0.008f, side);
                for (int i = 1; i <= 2; i++)
                {
                    var rib = new List<(Vector3, Vector3)>();
                    for (int k = 0; k <= 8; k++) { float t = k / 8f; rib.Add((new Vector3(Mathf.Lerp(0.57f, 0.85f, t), Mathf.Lerp(sb(t), st(t), i / 3f), 1.9f), Vector3.forward)); }
                    Ribbon(bf, "housing", rib, 0.010f, 0.008f, side);
                }
                // Серебристое «лезвие» под заборником
                var blade = new List<(Vector3, Vector3)>();
                for (int k = 0; k <= 10; k++) blade.Add((new Vector3(Mathf.Lerp(0.50f, 0.88f, k / 10f), 0.215f, 1.9f), Vector3.forward));
                Ribbon(bf, "int_grey", blade, 0.016f, 0.008f, side);
            }
            // Губа под бампером
            var bfr = bf.WorldFrame();
            Geo.RoundBox(bf.M("black"), Frame.Identity, bfr.ToLocal(new Vector3(0f, 0.19f, 2.36f)), new Vector3(1.55f, 0.03f, 0.16f), 0.3f, 8);
            // Четыре кольца над номером
            var hp = FrontPoint(0f, 0.655f, out var hn);
            var rings = bf.Child("Rings", bfr.ToLocal(hp + hn * 0.014f));
            var rf = Facing(Vector3.zero, bfr.DirToLocal(hn));
            for (int i = 0; i < 4; i++)
                Geo.Torus(rings.M("chrome"), rf.Mul(new Frame { o = new Vector3((i - 1.5f) * 0.062f, 0f, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }), 0.040f, 0.0055f, 32, 6);
            var pp = FrontPoint(0f, 0.45f, out var pn);
            PlateAt(bf, pp + pn * 0.02f, pn, "PlateFront");

            // Капот: шов по краю крыльев и две «силовые» линии
            foreach (float side in new[] { -1f, 1f })
            {
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++)
                {
                    float z = Mathf.Lerp(Cowl + 0.03f, 2.30f, i / 24f);
                    seam.Add((new Vector3(Mathf.Lerp(0.80f, 0.86f, i / 24f), 0.9f, z), Vector3.up));
                }
                Ribbon(N("Hood"), "black", seam, 0.005f, 0.001f, side);
                var rib = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++) rib.Add((new Vector3(Mathf.Lerp(0.40f, 0.36f, i / 24f), 0.9f, Mathf.Lerp(Cowl + 0.10f, 2.25f, i / 24f)), Vector3.up));
                Ribbon(N("Hood"), "paint", rib, 0.026f, 0.004f, side);
            }
            var cross = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 16; i++) cross.Add((new Vector3(Mathf.Lerp(0f, 0.86f, i / 16f), 0.78f, 1.9f), Vector3.forward));
            Ribbon(N("Hood"), "black", cross, 0.005f, 0.002f, 1f);
            Ribbon(N("Hood"), "black", cross, 0.005f, 0.002f, -1f);
        }

        void Wipers()
        {
            var shell = N("Shell");
            foreach (var (x0, x1, side) in new[] { (0.62f, 0.04f, -1f), (0.06f, 0.62f, 1f) })
            {
                var arm = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++)
                {
                    float t = i / 16f;
                    float z = Cowl - 0.05f - 0.08f * (side < 0 ? 1f - t : t);
                    arm.Add((new Vector3(Mathf.Lerp(x0, x1, t), 1.0f, z), Vector3.up));
                }
                Ribbon(shell, "black", arm, 0.020f, 0.010f, side);
                Ribbon(shell, "black_satin", arm, 0.009f, 0.019f, side);
            }
        }

        // ---------- Бока ----------

        void SideDetails(float side)
        {
            var shell = N("Shell");
            List<(Vector3, Vector3)> L(float z0, float y0, float z1, float y1, int n = 16)
            {
                var list = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    list.Add((new Vector3(0.5f, Mathf.Lerp(y0, y1, t), Mathf.Lerp(z0, z1, t)), Vector3.right));
                }
                return list;
            }
            string fd = side > 0 ? "DoorFR" : "DoorFL", rd = side > 0 ? "DoorRR" : "DoorRL";
            // Щели дверей (задняя дверь заходит над аркой)
            Ribbon(N(fd), "black", L(FrontDoorF, 0.30f, FrontDoorF - 0.04f, 0.97f), 0.005f, 0.001f, side);
            Ribbon(N(fd), "black", L(DoorSplit, 0.30f, DoorSplit, 0.99f), 0.005f, 0.001f, side);
            Ribbon(N(rd), "black", L(RearDoorR, 0.62f, RearDoorR + 0.04f, 1.02f), 0.005f, 0.001f, side);
            var arcCut = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.Lerp(Mathf.PI * 0.62f, Mathf.PI * 0.95f, i / 12f);
                arcCut.Add((new Vector3(0.5f, WheelR + Mathf.Sin(a) * 0.47f, AxleR + Mathf.Cos(a) * -0.47f), Vector3.right));
            }
            Ribbon(N(rd), "black", arcCut, 0.005f, 0.001f, side);
            Ribbon(N(fd), "black", L(DoorSplit, 0.305f, FrontDoorF, 0.305f, 30), 0.005f, 0.001f, side);
            Ribbon(N(rd), "black", L(-1.0f, 0.305f, DoorSplit, 0.305f, 20), 0.005f, 0.001f, side);
            // Линия плеч — острая кромка вдоль всего борта (светлая полоска ловит блик) и линия порога, поднимающаяся к корме
            Ribbon(shell, "paint", L(2.15f, 0.80f, -2.25f, 0.84f, 80), 0.014f, 0.003f, side);
            Ribbon(shell, "paint", L(1.05f, 0.36f, -0.98f, 0.40f, 40), 0.03f, 0.003f, side);
            // Хромированная окантовка окон
            var dlo = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 50; i++)
            {
                float z = Mathf.Lerp(0.84f, WinTip + 0.02f, i / 50f);
                dlo.Add((new Vector3(0.5f, YBelt[z] + 0.006f, z), Vector3.right));
            }
            Ribbon(shell, "chrome", dlo, 0.010f, 0.004f, side);
            // Ручки цвета кузова
            foreach (var (door, z) in new[] { (fd, DoorSplit + 0.20f), (rd, RearDoorR + 0.20f) })
            {
                var p = SidePoint(side, 0.905f, z, out var n);
                var dn = N(door);
                Geo.RoundBox(dn.M("paint"), Frame.Identity, dn.WorldFrame().ToLocal(p + n * 0.010f), new Vector3(0.02f, 0.026f, 0.15f), 0.45f, 8);
            }
            // Пороги: чёрная накладка
            Geo.RoundBox(shell.M("black"), Frame.Identity, new Vector3(0.925f * side, 0.24f, -0.0f), new Vector3(0.08f, 0.05f, 2.2f), 0.35f, 8);
            // Лючок бака справа сзади
            if (side > 0)
            {
                var hatch = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 28; i++)
                {
                    float a = i / 28f * Mathf.PI * 2f;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    float y = 0.90f + Mathf.Sign(sa) * Mathf.Pow(Mathf.Abs(sa), 0.5f) * 0.06f;
                    float z = -1.66f + Mathf.Sign(ca) * Mathf.Pow(Mathf.Abs(ca), 0.5f) * 0.085f;
                    hatch.Add((new Vector3(0.5f, y, z), Vector3.right));
                }
                Ribbon(shell, "black", hatch, 0.004f, 0.001f, side);
            }
        }

        /// <summary>Зеркало на двери у стойки A: корпус цвета кузова, чёрная ножка, полоса поворотника.</summary>
        void Mirror(float side)
        {
            var door = N(side < 0 ? "DoorFL" : "DoorFR");
            var df = door.WorldFrame();
            var basePt = new Vector3(W * 0.93f * side, 1.0f, 0.78f);
            var center = basePt + new Vector3(0.13f * side, 0.05f, -0.03f);
            var mount = door.Child(side < 0 ? "MirrorL" : "MirrorR", df.ToLocal(center));
            var size = new Vector3(0.22f, 0.13f, 0.11f);
            Geo.RoundBox(mount.M("paint"), Frame.Identity, Vector3.zero, size, 0.5f, 14);
            Geo.RoundBox(mount.M("amber"), Frame.Identity, new Vector3(0.03f * side, -0.045f, 0.03f), new Vector3(0.14f, 0.012f, 0.05f), 0.4f, 6);
            var from = df.ToLocal(basePt) - mount.pos;
            var to = new Vector3(-0.05f * side, -0.035f, 0.01f);
            Geo.RoundBox(mount.M("black"), Frame.Look((from + to) * 0.5f, to - from, Vector3.up), Vector3.zero, new Vector3(0.05f, 0.024f, (to - from).magnitude + 0.04f), 0.4f, 6);
            MirrorGlass(mount, side < 0 ? "MirrorGlassL" : "MirrorGlassR", size);
        }

        // ---------- Корма ----------

        /// <summary>Фонарь под кромкой спойлера: тёмно-красный, вертикальные светодиодные сегменты, заворачивает на крыло.</summary>
        void Taillight(float side)
        {
            var c = RearPoint(0.70f * side, 0.90f, out _);
            var node = body.Child(side < 0 ? "TaillightL" : "TaillightR", c);
            System.Func<float, float> yb = t => Mathf.Lerp(0.86f, 0.845f, t), yt = t => Mathf.Lerp(0.955f, 0.93f, t * t);
            Lamp(node, "black", 0.40f, 0.93f, yb, yt, 0.004f, side, true);
            Lamp(node, "tail_smoke", 0.40f, 0.93f, t => yb(t) + 0.006f, t => yt(t) - 0.006f, 0.014f, side, true);
            for (int i = 0; i < 13; i++)
            {
                float t = 0.05f + i * 0.072f;
                float x = Mathf.Lerp(0.40f, 0.93f, t);
                Ribbon(node, "tail_red", Line(new Vector3(x, yb(t) + 0.015f, -1.9f), new Vector3(x, yt(t) - 0.016f, -1.9f), Vector3.back, 2), 0.012f, 0.018f, side);
            }
            var led = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 14; i++) { float t = i / 14f; led.Add((new Vector3(Mathf.Lerp(0.42f, 0.92f, t), yt(t) - 0.010f, -1.9f), Vector3.back)); }
            Ribbon(node, "tail_red", led, 0.006f, 0.019f, side);
            var blink = node.Child(side < 0 ? "BlinkRL" : "BlinkRR");
            Lamp(blink, "amber", 0.42f, 0.60f, t => yb(Mathf.Lerp(0.04f, 0.38f, t)) + 0.008f, t => yb(Mathf.Lerp(0.04f, 0.38f, t)) + 0.016f, 0.019f, side, true, 6, 2);
        }

        /// <summary>Сквозная полоса между фонарями, кромка спойлера, номер на бампере, диффузор и трапеции выхлопа.</summary>
        void RearDetails()
        {
            var tail = N("Tailgate");
            foreach (float side in new[] { -1f, 1f })
            {
                var bar = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 14; i++) bar.Add((new Vector3(Mathf.Lerp(0f, 0.42f, i / 14f), 0.915f, -1.9f), Vector3.back));
                Ribbon(tail, "black", bar, 0.030f, 0.005f, side);
                Ribbon(tail, "tail_red", bar, 0.007f, 0.009f, side);
                // Кромка выдвижного спойлера
                var lip = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 20; i++) lip.Add((new Vector3(Mathf.Lerp(0f, 0.86f, i / 20f), 1.2f, -2.33f), Vector3.up));
                Ribbon(tail, "black", lip, 0.005f, 0.001f, side);
                // Щели двери багажника по бокам
                Ribbon(tail, "black", Line(new Vector3(0.93f, 0.82f, -1.9f), new Vector3(0.93f, 1.0f, -1.9f), Vector3.back, 8), 0.005f, 0.002f, side);
            }
            // Эмблема-кольца на двери багажника
            var tf = tail.WorldFrame();
            var ep = RearPoint(0f, 0.985f, out var en);
            if (ep.z > -2.6f)
            {
                var rings = tail.Child("RingsRear", tf.ToLocal(ep + en * 0.006f));
                var rf = Facing(Vector3.zero, tf.DirToLocal(en));
                for (int i = 0; i < 4; i++)
                    Geo.Torus(rings.M("chrome"), rf.Mul(new Frame { o = new Vector3((i - 1.5f) * 0.044f, 0f, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }), 0.028f, 0.0042f, 28, 5);
            }
            var roof = N("Roof");
            var rfr = roof.WorldFrame();
            // Антенна-«плавник»
            Geo.RoundBox(roof.M("black"), Frame.Identity, rfr.ToLocal(new Vector3(0f, 1.335f, -1.02f)), new Vector3(0.065f, 0.06f, 0.19f), 0.55f, 12);

            var br = N("BumperR");
            var bfr = br.WorldFrame();
            var pp = RearPoint(0f, 0.62f, out var pn);
            PlateAt(br, pp + pn * 0.014f, pn, "PlateRear");
            foreach (float side in new[] { -1f, 1f })
            {
                // Диффузор: чёрный низ с вертикальными рёбрами
                Lamp(br, "black", 0f, 0.86f, t => 0.22f, t => 0.36f, 0.004f, side, true, 14, 3);
                for (int k = 0; k < 3; k++)
                {
                    float x = 0.08f + k * 0.12f;
                    Ribbon(br, "black_satin", Line(new Vector3(x, 0.23f, -1.9f), new Vector3(x, 0.35f, -1.9f), Vector3.back, 3), 0.012f, 0.012f, side);
                }
                // Трапеции выхлопа: хромированная рамка, чёрная середина
                System.Func<float, float> eb = t => 0.265f, et = t => 0.335f - 0.01f * t;
                Lamp(br, "int_black", 0.50f, 0.80f, eb, et, 0.012f, side, true, 8, 3);
                Ribbon(br, "chrome", Outline(0.50f, 0.80f, eb, et, true, 10), 0.014f, 0.016f, side);
                // Отражатели по краям
                Lamp(br, "tail_red", 0.80f, 0.90f, t => 0.47f, t => 0.49f, 0.006f, side, true, 4, 2);
                // Хромированная планка над диффузором
                var strip = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++) strip.Add((new Vector3(Mathf.Lerp(0f, 0.84f, i / 16f), 0.375f, -1.9f), Vector3.back));
                // Рельеф двери багажника под фонарями
                var crease = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++) crease.Add((new Vector3(Mathf.Lerp(0f, 0.80f, i / 16f), 0.80f, -1.9f), Vector3.back));
                Ribbon(N("Tailgate"), "paint", crease, 0.02f, 0.004f, side);
                Ribbon(br, "chrome", strip, 0.010f, 0.008f, side);
            }
        }

        void Chassis()
        {
            var m = N("Shell").M("chassis");
            foreach (float z in new[] { AxleF, AxleR })
            {
                var af = new Frame { o = new Vector3(0f, WheelR, z), x = Vector3.up, y = Vector3.right, z = Vector3.forward };
                Geo.Cylinder(m, af, 0.045f, -0.7f, 0.7f, 12);
                Geo.RoundBox(m, Frame.Identity, new Vector3(0.03f, WheelR, z), new Vector3(0.3f, 0.18f, 0.24f), 0.6f, 8);
            }
        }

        // ---------- Салон (руль слева, бежевая кожа) ----------

        void Interior()
        {
            var cab = body.Child("Interior");
            var id = Frame.Identity;
            Geo.Box(cab.M("carpet_beige"), id, new Vector3(0f, 0.25f, -0.6f), new Vector3(1.7f, 0.03f, 2.9f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.58f, 0.92f), new Vector3(1.6f, 0.62f, 0.04f));
            // Торпеда: чёрный верх, светлый низ, полоса «рояльного лака» с серебристой рамкой
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.95f, 0.77f), new Vector3(1.78f, 0.14f, 0.42f), 0.3f, 10);
            Geo.RoundBox(cab.M("leather_cream"), id, new Vector3(0f, 0.72f, 0.73f), new Vector3(1.74f, 0.30f, 0.36f), 0.3f, 8);
            Geo.RoundBox(cab.M("piano_black"), id, new Vector3(0.18f, 0.84f, 0.545f), new Vector3(1.05f, 0.10f, 0.03f), 0.3f, 6);
            Geo.RoundBox(cab.M("chrome"), id, new Vector3(0.18f, 0.788f, 0.54f), new Vector3(1.07f, 0.006f, 0.032f), 0.3f, 4);
            Geo.RoundBox(cab.M("chrome"), id, new Vector3(0.18f, 0.892f, 0.54f), new Vector3(1.07f, 0.006f, 0.032f), 0.3f, 4);
            // Щелевые дефлекторы во всю ширину
            Geo.Box(cab.M("grille"), id, new Vector3(0.18f, 0.915f, 0.552f), new Vector3(0.90f, 0.03f, 0.02f));
            foreach (float x in new[] { -0.78f, 0.78f }) Geo.Box(cab.M("grille"), id, new Vector3(x, 0.92f, 0.56f), new Vector3(0.12f, 0.05f, 0.02f));

            // Цифровой щиток за рулём под козырьком
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(-0.37f, 1.04f, 0.57f), new Vector3(0.46f, 0.06f, 0.16f), 0.4f, 8);
            var clusterPos = new Vector3(-0.37f, 0.965f, 0.525f);
            var cluster = cab.Child("ClusterScreen", clusterPos, LookEuler(clusterPos - Eyes));
            Geo.RoundBox(cluster.Child("ClusterBezel").M("piano_black"), id, new Vector3(0f, 0f, 0.012f), new Vector3(0.42f, 0.15f, 0.02f), 0.2f, 6);
            Geo.Quad(cluster.M("screen_blue"), id, 0.39f, 0.13f);

            // Верхний экран MMI (радио) — в чёрной полосе, развёрнут к водителю
            var mmiPos = new Vector3(0.0f, 0.845f, 0.52f);
            var info = cab.Child("RadioScreen", mmiPos, LookEuler(mmiPos - Eyes));
            Geo.RoundBox(info.Child("RadioBezel").M("piano_black"), id, new Vector3(0f, 0f, 0.01f), new Vector3(0.27f, 0.12f, 0.02f), 0.15f, 6);
            Geo.Quad(info.M("screen_blue"), id, 0.25f, 0.105f);
            // Нижний экран климата на консоли
            var clim = cab.Child("ClimateScreen", new Vector3(0f, 0.68f, 0.52f), new Vector3(-35f, 0f, 0f));
            Geo.RoundBox(clim.M("piano_black"), id, new Vector3(0f, 0f, 0.01f), new Vector3(0.23f, 0.12f, 0.02f), 0.15f, 6);
            Geo.Quad(clim.M("screen_blue"), id, 0.21f, 0.10f);

            // Центральная консоль с деревом, селектор, подлокотник
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.45f, -0.05f), new Vector3(0.28f, 0.36f, 1.1f), 0.25f, 8);
            Geo.RoundBox(cab.M("wood_dark"), id, new Vector3(0f, 0.635f, 0.05f), new Vector3(0.24f, 0.02f, 0.46f), 0.3f, 6);
            Geo.RoundBox(cab.M("chrome"), id, new Vector3(0f, 0.632f, 0.05f), new Vector3(0.255f, 0.012f, 0.475f), 0.3f, 6);
            Geo.RoundBox(cab.M("leather_cream"), id, new Vector3(0f, 0.66f, -0.42f), new Vector3(0.26f, 0.07f, 0.36f), 0.45f, 8);
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(-0.06f, 0.69f, 0.22f), new Vector3(0.07f, 0.09f, 0.11f), 0.5f, 8);

            SteeringWheel();
            foreach (float x in new[] { -0.37f, 0.37f }) Seat(cab, x, -0.42f, x < 0 ? "SeatBackL" : "SeatBackR", 0.52f);
            // Задний диван (под пологой крышей — ниже)
            Geo.RoundBox(cab.M("leather_cream"), id, new Vector3(0f, 0.42f, -1.20f), new Vector3(1.42f, 0.13f, 0.50f), 0.3f, 8);
            var rb = cab.Child("RearBack", new Vector3(0f, 0.46f, -1.45f), new Vector3(-26f, 0f, 0f));
            Geo.RoundBox(rb.M("leather_cream"), id, new Vector3(0f, 0.28f, 0f), new Vector3(1.42f, 0.56f, 0.13f), 0.3f, 8);
            Geo.Box(cab.M("carpet_beige"), id, new Vector3(0f, 0.62f, -1.95f), new Vector3(1.6f, 0.03f, 0.5f));
            // Двери: подлокотник и полоса дерева
            foreach (var door in new[] { "DoorFL", "DoorFR", "DoorRL", "DoorRR" })
            {
                float side = door.EndsWith("L") ? -1f : 1f;
                float z = door.StartsWith("DoorF") ? 0.30f : -0.78f;
                var dn = N(door);
                var df = dn.WorldFrame();
                Geo.RoundBox(dn.M("leather_cream"), id, df.ToLocal(new Vector3(0.86f * side, 0.68f, z)), new Vector3(0.09f, 0.05f, 0.48f), 0.4f, 8);
                Geo.RoundBox(dn.M("wood_dark"), id, df.ToLocal(new Vector3(0.87f * side, 0.85f, z + 0.05f)), new Vector3(0.02f, 0.03f, 0.55f), 0.3f, 6);
            }
            // Салонное зеркало
            Geo.Cylinder(cab.M("int_black"), new Frame { o = new Vector3(0f, 1.33f, 0.47f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.012f, -0.04f, 0.02f, 8);
            var rm = cab.Child("RearMirror", new Vector3(0f, 1.28f, 0.45f), new Vector3(0f, 19f, 0f));
            Geo.RoundBox(rm.M("int_black"), id, new Vector3(0f, 0f, 0.016f), new Vector3(0.27f, 0.075f, 0.035f), 0.55f, 10);
            Geo.Quad(rm.Child("RearMirrorGlass", new Vector3(0f, 0f, -0.0025f)).M("mirror"), id, 0.25f, 0.06f);
            Driver(0.86f, -0.50f, -14f);
        }

        void SteeringWheel()
        {
            var w = body.Child("SteeringWheel", SteeringPos, new Vector3(SteeringTilt, 0f, 0f));
            var id = Frame.Identity;
            const float R = 0.185f;
            Geo.Torus(w.M("leather_black"), id, R, 0.017f, 48, 10);
            foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.back })
            {
                var f = Frame.Look(dir * R * 0.5f, dir, Vector3.up);
                Geo.RoundBox(w.M("int_black"), f, Vector3.zero, new Vector3(0.05f, 0.02f, R * 0.95f), 0.4f, 6);
                Geo.RoundBox(w.M("int_grey"), f, new Vector3(0f, 0.011f, 0.02f), new Vector3(0.012f, 0.004f, R * 0.6f), 0.4f, 4);
            }
            Geo.RoundBox(w.M("leather_black"), id, new Vector3(0f, 0.015f, -0.005f), new Vector3(0.14f, 0.05f, 0.10f), 0.5f, 10);
            Geo.Cylinder(w.M("int_black"), id, 0.035f, -0.3f, -0.01f, 12);
        }

        void Seat(ModelNode cab, float x, float z, string backName, float width)
        {
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("leather_cream"), id, new Vector3(x, 0.44f, z), new Vector3(width, 0.13f, 0.54f), 0.3f, 8);
            var back = cab.Child(backName, new Vector3(x, 0.48f, z - 0.28f), new Vector3(-18f, 0f, 0f));
            Geo.RoundBox(back.M("leather_cream"), id, new Vector3(0f, 0.30f, 0f), new Vector3(width, 0.60f, 0.13f), 0.3f, 8);
            Geo.RoundBox(back.M("leather_cream"), id, new Vector3(0f, 0.68f, -0.01f), new Vector3(0.28f, 0.16f, 0.11f), 0.55f, 8);
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
                WheelModel.FiveSpoke(mesh, WheelR, 0.255f, 10, 0.258f, "alloy", "rim_black", 0.62f);
                WheelModel.Caliper(mount, side, 1.2f);
            }
        }
    }
}

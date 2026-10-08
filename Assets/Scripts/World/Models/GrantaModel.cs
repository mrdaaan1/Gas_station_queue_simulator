using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «Лада Гранта Sport» (седан, 2011–2018) — чёрная, заниженная, с обвесом: губа-сплиттер, юбки, спойлер на крышке
    /// багажника, диффузор и два выхлопа, противотуманки, тонировка, 17" чёрные диски с точёными спицами и жёлтыми суппортами.
    /// Размеры как у настоящей: 4,26 × 1,70 м, база 2,48 м, высота с занижением 1,44 м.
    /// Салон чёрный с красным: ткань с красными вставками, красная строчка, метка на руле, стрелочные приборы.
    /// </summary>
    public class GrantaModel : SportsCarModel
    {
        public const float AxleF = 1.30f, AxleR = -1.176f;
        public const float WheelR = 0.308f, Track = 0.715f;
        const float W = 0.85f;
        const float FrontDoorF = 0.88f, DoorSplit = -0.22f, RearDoorR = -1.40f;
        const float WinTip = -1.37f;
        /// <summary>Салон сдвинут вперёд: у Гранты лобовое начинается почти над передним колесом, капот короткий.</summary>
        const float DZ = 0.30f;

        public static readonly Vector3 DriverEyes = new Vector3(-0.36f, 1.11f, -0.28f);
        public static readonly Vector3 SteeringPos = new Vector3(-0.36f, 0.81f, 0.14f);
        public const float SteeringTilt = -64f;

        static Model cached;

        public static Model Get()
        {
            if (cached == null) cached = new GrantaModel().Build();
            return cached;
        }

        public static GrantaModel Shape() => new GrantaModel();

        Vector3? eyesLocal;
        protected override Vector3 Eyes => eyesLocal ?? DriverEyes;

        GrantaModel()
        {
            Front = 2.36f; Rear = -2.13f;
            Deck = -1.45f; RoofRear = -0.85f; Header = 0.10f; Cowl = 0.90f;
            FrontRound = 1.95f; RearRound = -1.92f; FrontPow = 3.2f; RearPow = 3.6f;
            RockerFrac = 0.90f; RockerAngle = 35f;

            W0 = new Curve().Key(-2.13f, 0.82f).Key(AxleR, W).Key(0f, 0.845f).Key(AxleF, W).Key(2.13f, 0.83f).Key(2.36f, 0.83f);
            // Занижена: низ порогов и юбок почти у земли
            YBot = new Curve().Key(-2.13f, 0.26f).Key(-2.05f, 0.17f).Key(-1.6f, 0.13f).Key(0f, 0.12f).Key(1.6f, 0.13f).Key(2.0f, 0.13f).Key(2.25f, 0.14f).Key(2.36f, 0.22f);
            YMax = new Curve().Key(-2.13f, 0.55f).Key(-1.8f, 0.60f).Key(AxleR, 0.62f).Key(0f, 0.58f).Key(AxleF, 0.60f).Key(1.9f, 0.58f).Key(2.13f, 0.50f).Key(2.30f, 0.40f).Key(2.36f, 0.34f);
            // Высокая линия окон, поднимается к корме — «клин»; на носу — высокое крыло, круто опускается к фарам
            YBelt = new Curve().Key(-2.13f, 0.95f).Key(-2.10f, 1.0f).Key(Deck, 1.02f, true).Key(-1.2f, 1.0f).Key(-0.6f, 0.965f).Key(0.2f, 0.93f)
                .Key(Cowl, 0.905f, true).Key(1.3f, 0.90f).Key(1.7f, 0.895f).Key(1.95f, 0.88f).Key(2.05f, 0.855f).Key(2.10f, 0.80f).Key(2.13f, 0.66f).Key(2.17f, 0.60f).Key(2.28f, 0.565f).Key(2.33f, 0.50f).Key(2.36f, 0.40f);
            FBelt = new Curve().Key(-2.13f, 0.88f).Key(0f, 0.875f).Key(Cowl, 0.88f, true).Key(2.13f, 0.89f).Key(2.36f, 0.92f);
            // Осевая верха: высокий короткий багажник, крутое заднее стекло, длинная ровная крыша, длинное лобовое, короткий капот
            YTop = new Curve().Key(-2.13f, 0.98f).Key(-2.11f, 1.045f).Key(-2.04f, 1.07f).Key(-1.95f, 1.07f).Key(-1.75f, 1.065f).Key(Deck, 1.07f, true)
                .Key(-1.2f, 1.22f).Key(-1.0f, 1.33f).Key(RoofRear, 1.385f).Key(-0.5f, 1.43f).Key(-0.2f, 1.44f).Key(0.0f, 1.435f).Key(Header, 1.42f, true)
                .Key(0.4f, 1.25f).Key(0.7f, 1.07f).Key(Cowl, 0.99f, true).Key(1.2f, 0.98f).Key(1.6f, 0.955f).Key(1.9f, 0.925f).Key(2.02f, 0.905f)
                .Key(2.08f, 0.875f).Key(2.115f, 0.80f).Key(2.135f, 0.68f).Key(2.17f, 0.625f).Key(2.28f, 0.59f).Key(2.33f, 0.52f).Key(2.36f, 0.40f);
            YRoofEdge = new Curve().Key(Deck, 1.02f, true).Key(-1.25f, 1.15f).Key(-1.0f, 1.27f).Key(RoofRear, 1.33f).Key(-0.4f, 1.385f)
                .Key(Header, 1.375f, true).Key(0.45f, 1.20f).Key(0.7f, 1.03f).Key(Cowl, 0.905f, true);
            XRoofEdge = new Curve().Key(Deck, 0.72f, true).Key(-1.2f, 0.66f).Key(RoofRear, 0.63f).Key(-0.4f, 0.62f)
                .Key(Header, 0.63f, true).Key(0.5f, 0.70f).Key(Cowl, 0.765f, true);
            AngC = new Curve().Key(-2.13f, 100f).Key(2.13f, 100f);
            AngE0 = new Curve().Key(-2.13f, 160f).Key(Deck, 150f).Key(-1.3f, 140f).Key(RoofRear, 138f).Key(Header, 138f).Key(Cowl, 155f).Key(1.4f, 162f).Key(2.13f, 166f);
            AngD0 = new Curve().Key(-2.13f, 102f).Key(2.13f, 102f);
            AngD1 = new Curve().Key(-2.13f, 118f).Key(2.13f, 118f);

            arches.Add(new Arch { z = AxleF, y = WheelR, r = 0.335f, xMin = 0.58f });
            arches.Add(new Arch { z = AxleR, y = WheelR, r = 0.335f, xMin = 0.58f });
            Prepare();
        }

        // ---------- Клетки кузова ----------

        static bool Between(float z, float a, float b) => z > Mathf.Min(a, b) && z < Mathf.Max(a, b);

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
                    float edge = Mathf.Min(z - Deck, RoofRear - z);
                    if (v > 0.12f && edge > 0.035f) mat = "glass";
                    else if (v > 0.08f && edge > 0.010f) mat = "black";
                }
                if (z > Cowl + 0.015f && z < 2.06f) node = "Hood";
                else if (z < Header && z >= RoofRear) node = "Roof";
                else if (z < Deck - 0.01f) node = "Tailgate"; // крышка багажника
            }
            else if (seg == SegD && InCabin(z))
            {
                // Окно передней двери, задней двери и треугольная форточка в задней двери. Стойка B и рамки — чёрные,
                // толстая стойка C — цвета кузова
                bool win = Between(z, -0.18f, 0.80f) || Between(z, -1.02f, -0.28f) || Between(z, WinTip, -1.075f);
                if (win && v > 0.05f && v < 0.95f) mat = "glass";
                else if (v > 0.03f && z < 0.86f && z > WinTip - 0.03f) mat = "black";
            }
            bool lower = seg == SegB || seg == SegC || seg == SegD;
            if (lower && p.y > 0.24f)
            {
                if (Between(z, DoorSplit, FrontDoorF)) node = side > 0 ? "DoorFR" : "DoorFL";
                else if (Between(z, RearDoorR, DoorSplit)) node = side > 0 ? "DoorRR" : "DoorRL";
            }
            bool bumperRow = seg <= SegB || (seg == SegC && v < 0.32f);
            if (z < -1.98f && seg >= SegB && !bumperRow && (seg == SegE || p.x < 0.41f)) node = "Tailgate";   // вертикальная стенка крышки между фонарями
            if ((bumperRow && z > 1.80f) || z > 2.14f) node = "BumperF";   // бампер выступает вперёд под фарами
            if (bumperRow && z < -1.82f) node = "BumperR";
            if (mat != "glass" && z < Cowl) inner = seg == SegA ? "carpet" : seg >= SegD ? "int_roof" : "int_door";
            return new CellInfo { node = node, mat = mat, innerMat = inner };
        }

        protected override Vector3 Pivot(string name)
        {
            switch (name)
            {
                case "Hood": return new Vector3(0f, 0.98f, Cowl + 0.02f);
                case "Roof": return new Vector3(0f, 1.43f, -0.4f);
                case "Tailgate": return new Vector3(0f, 1.07f, Deck); // петли крышки багажника — у заднего стекла
                case "DoorFL": return new Vector3(-W, 0.65f, FrontDoorF);
                case "DoorFR": return new Vector3(W, 0.65f, FrontDoorF);
                case "DoorRL": return new Vector3(-W, 0.65f, DoorSplit);
                case "DoorRR": return new Vector3(W, 0.65f, DoorSplit);
                case "BumperF": return new Vector3(0f, 0.38f, 2.0f);
                case "BumperR": return new Vector3(0f, 0.42f, -2.0f);
            }
            return Vector3.zero;
        }

        // ---------- Сборка ----------

        Model Build()
        {
            StartModel();
            Emit(N, 200f);
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

        void ArchLip(Arch a, float side)
        {
            var m = N("Shell").M("paint");
            float yb = 0.22f;
            float th0 = Mathf.Asin(Mathf.Clamp((yb - a.y) / a.r, -1f, 1f));
            float th1 = Mathf.PI - th0;
            Vector3 P(float u, float b)
            {
                float th = Mathf.Lerp(th0, th1, u);
                float phi = b * Mathf.PI * 2f;
                float cs = Mathf.Cos(phi), sn = Mathf.Sin(phi);
                float r = a.r + 0.012f + Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), 0.5f) * 0.016f;
                float x = W - 0.02f + Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.5f) * 0.02f;
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
            OnBody(new Vector3(Mathf.Abs(x), y, 1.6f), Vector3.forward, Mathf.Sign(x + 1e-6f), out var p, out n);
            return p;
        }

        Vector3 RearPoint(float x, float y, out Vector3 n)
        {
            OnBody(new Vector3(Mathf.Abs(x), y, -1.6f), Vector3.back, Mathf.Sign(x + 1e-6f), out var p, out n);
            return p;
        }

        Vector3 SidePoint(float side, float y, float z, out Vector3 n)
        {
            OnBody(new Vector3(0.4f, y, z), Vector3.right, side, out var p, out n);
            return p;
        }

        void Lamp(ModelNode node, string mat, float x0, float x1, System.Func<float, float> yb, System.Func<float, float> yt,
            float offset, float side, bool rear = false, int na = 16, int nb = 6)
        {
            float z0 = rear ? -1.6f : 1.6f;
            var dir = rear ? Vector3.back : Vector3.forward;
            Patch(node, mat, na, nb, (a, b) => (new Vector3(Mathf.Lerp(x0, x1, a), Mathf.Lerp(yb(a), yt(a), b), z0), dir), offset, side);
        }

        static List<(Vector3, Vector3)> Outline(float x0, float x1, System.Func<float, float> yb, System.Func<float, float> yt, bool rear, int n = 14)
        {
            float z0 = rear ? -1.6f : 1.6f;
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

        /// <summary>Большая фара, заходящая на крыло: хромированные чаши ближнего и дальнего, линза, поворотник у угла.</summary>
        void Headlight(float side)
        {
            var c = FrontPoint(0.60f * side, 0.76f, out _);
            var node = body.Child(side < 0 ? "HeadlightL" : "HeadlightR", c);
            System.Func<float, float> yb = t => Mathf.Lerp(0.655f, 0.765f, t * t), yt = t => Mathf.Lerp(0.805f, 0.875f, Mathf.Sqrt(t));
            Lamp(node, "reflector", 0.37f, 0.72f, yb, yt, 0.004f, side);
            Lamp(node, "lens", 0.37f, 0.72f, yb, yt, 0.02f, side);
            Ribbon(node, "chrome", Outline(0.37f, 0.72f, yb, yt, false), 0.005f, 0.012f, side);
            var f = node.WorldFrame();
            foreach (var (x, r) in new[] { (0.46f, 0.046f), (0.59f, 0.042f) })
            {
                float t = (x - 0.37f) / 0.35f;
                var p = FrontPoint(x * side, (yb(t) + yt(t)) * 0.5f, out var n);
                var rf = Facing(f.ToLocal(p + n * 0.006f), f.DirToLocal(n));
                Geo.Lathe(node.M("chrome"), rf, new[] { new Vector2(r, 0f), new Vector2(r * 0.65f, -0.012f), new Vector2(0f, -0.016f) }, 24, true, true);
                Geo.Torus(node.M("housing"), rf, r * 0.45f, 0.004f, 20, 5);
                Geo.Lathe(node.M("glass_dark"), rf, new[] { new Vector2(r * 0.42f, 0.0f), new Vector2(r * 0.28f, 0.008f), new Vector2(0f, 0.011f) }, 20);
            }
            var brow = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 14; i++) { float t = i / 14f; brow.Add((new Vector3(Mathf.Lerp(0.39f, 0.70f, t), yb(t * 0.9f) + 0.012f, 1.6f), Vector3.forward)); }
            Ribbon(node, "housing", brow, 0.010f, 0.014f, side);
            var blink = node.Child(side < 0 ? "BlinkFL" : "BlinkFR");
            Lamp(blink, "amber", 0.64f, 0.71f, t => yt(Mathf.Lerp(0.75f, 0.95f, t)) - 0.030f, t => yt(Mathf.Lerp(0.75f, 0.95f, t)) - 0.012f, 0.022f, side, false, 6, 2);
        }

        /// <summary>
        /// Узкая верхняя решётка с хромированной планкой и «ладьёй», большой нижний заборник с сеткой и номером,
        /// круглые противотуманки в чёрных карманах, губа-сплиттер с плавниками.
        /// </summary>
        void FrontEnd()
        {
            var bf = N("BumperF");
            var bfr = bf.WorldFrame();
            foreach (float side in new[] { -1f, 1f })
            {
                // Верхняя решётка между фарами
                System.Func<float, float> ub = t => Mathf.Lerp(0.705f, 0.68f, t * t), ut = t => Mathf.Lerp(0.815f, 0.80f, t * t);
                Lamp(N("Hood"), "grille", 0f, 0.37f, ub, ut, 0.005f, side, false, 12, 4);
                Ribbon(N("Hood"), "chrome", Outline(0f, 0.37f, t => ub(t) + 0.035f, t => ub(t) + 0.045f, false, 10), 0.006f, 0.010f, side);
                Ribbon(N("Hood"), "black_satin", Outline(0f, 0.37f, ub, ut, false, 12), 0.012f, 0.009f, side);
                // Нижний заборник-трапеция с сеткой
                System.Func<float, float> lb = t => 0.20f, lt = t => Mathf.Lerp(0.40f, 0.36f, t * t);
                Lamp(bf, "grille", 0f, 0.47f, lb, lt, 0.004f, side, false, 14, 8);
                Ribbon(bf, "black_satin", Outline(0f, 0.47f, lb, lt, false, 14), 0.020f, 0.008f, side);
                for (int i = 1; i <= 5; i++)
                {
                    var rib = new List<(Vector3, Vector3)>();
                    float y = Mathf.Lerp(0.215f, 0.385f, i / 6f);
                    for (int k = 0; k <= 10; k++) rib.Add((new Vector3(Mathf.Lerp(0f, 0.45f, k / 10f), y, 1.6f), Vector3.forward));
                    Ribbon(bf, "housing", rib, 0.006f, 0.010f, side);
                }
                // Противотуманка в круглом кармане
                var fp = FrontPoint(0.66f * side, 0.33f, out var fn);
                var pocket = bf.Child(side < 0 ? "FogL" : "FogR", bfr.ToLocal(fp));
                var pf = Facing(bfr.DirToLocal(fn) * 0.004f, bfr.DirToLocal(fn));
                Geo.Lathe(pocket.M("black"), pf, new[] { new Vector2(0.095f, 0.004f), new Vector2(0.075f, -0.012f), new Vector2(0.0f, -0.02f) }, 28, true, true);
                Geo.Torus(pocket.M("chrome"), pf.Mul(new Frame { o = new Vector3(0f, 0.004f, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }), 0.062f, 0.005f, 28, 6);
                Geo.Lathe(pocket.M("reflector"), pf, new[] { new Vector2(0.058f, 0f), new Vector2(0.03f, -0.012f), new Vector2(0f, -0.015f) }, 24, true, true);
                Geo.Lathe(pocket.M("lens"), pf, new[] { new Vector2(0.058f, 0.002f), new Vector2(0.04f, 0.010f), new Vector2(0f, 0.014f) }, 24);
                // Плавник сплиттера
                var fin = new List<(Vector3, Vector3)>();
                for (int k = 0; k <= 4; k++) fin.Add((new Vector3(0.55f + 0.08f * k / 4f, Mathf.Lerp(0.16f, 0.25f, k / 4f), 1.6f), Vector3.forward));
                Ribbon(bf, "black", fin, 0.012f, 0.03f, side);
            }
            // Губа-сплиттер: широкая чёрная пластина, выступает вперёд
            foreach (float side in new[] { -1f, 1f })
            {
                // Губа-сплиттер повторяет низ бампера и выступает на 4 см
                Lamp(bf, "black", 0f, 0.80f, t => 0.135f, t => 0.175f, 0.04f, side, false, 16, 2);
                Lamp(bf, "black", 0f, 0.80f, t => 0.125f, t => 0.14f, 0.055f, side, false, 16, 1);
            }
            // «Ладья» на решётке
            var hood = N("Hood");
            var hf = hood.WorldFrame();
            var lp = FrontPoint(0f, 0.76f, out var ln);
            var badge = hood.Child("Ladya", hf.ToLocal(lp + ln * 0.014f));
            var bfF = Facing(Vector3.zero, hf.DirToLocal(ln));
            var oval = bfF.Mul(new Frame { o = Vector3.zero, x = Vector3.right * 1.45f, y = Vector3.up, z = Vector3.forward });
            Geo.Torus(badge.M("chrome"), oval, 0.036f, 0.005f, 32, 6);
            Geo.Lathe(badge.M("black"), oval, new[] { new Vector2(0.033f, -0.002f), new Vector2(0f, 0.0f) }, 24);
            Geo.RoundBox(badge.M("chrome"), Frame.Identity, Vector3.zero + hf.DirToLocal(ln) * 0.004f, new Vector3(0.05f, 0.022f, 0.006f), 0.6f, 8);
            // «Sport» — красная плашка справа на решётке
            var sp = FrontPoint(0.22f, 0.725f, out var sn);
            Geo.RoundBox(hood.M("tail_red"), Frame.Identity, hf.ToLocal(sp + sn * 0.012f), new Vector3(0.07f, 0.014f, 0.006f), 0.4f, 4);
            var pp = FrontPoint(0f, 0.50f, out var pn);
            PlateAt(bf, pp + pn * 0.02f, pn, "PlateFront");

            foreach (float side in new[] { -1f, 1f })
            {
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++) seam.Add((new Vector3(Mathf.Lerp(0.70f, 0.76f, i / 24f), 0.9f, Mathf.Lerp(Cowl + 0.03f, 1.98f, i / 24f)), Vector3.up));
                Ribbon(N("Hood"), "black", seam, 0.005f, 0.001f, side);
                var rib = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++) rib.Add((new Vector3(Mathf.Lerp(0.30f, 0.24f, i / 24f), 0.9f, Mathf.Lerp(Cowl + 0.10f, 1.95f, i / 24f)), Vector3.up));
                Ribbon(N("Hood"), "paint", rib, 0.024f, 0.004f, side);
            }
        }

        void Wipers()
        {
            var shell = N("Shell");
            foreach (var (x0, x1, side) in new[] { (0.58f, 0.04f, -1f), (0.06f, 0.56f, 1f) })
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
                    list.Add((new Vector3(0.4f, Mathf.Lerp(y0, y1, t), Mathf.Lerp(z0, z1, t)), Vector3.right));
                }
                return list;
            }
            string fd = side > 0 ? "DoorFR" : "DoorFL", rd = side > 0 ? "DoorRR" : "DoorRL";
            Ribbon(N(fd), "black", L(FrontDoorF, 0.24f, FrontDoorF - 0.03f, 0.90f), 0.005f, 0.001f, side);
            Ribbon(N(fd), "black", L(DoorSplit, 0.24f, DoorSplit, 0.95f), 0.005f, 0.001f, side);
            Ribbon(N(rd), "black", L(RearDoorR + 0.02f, 0.62f, RearDoorR, 1.0f), 0.005f, 0.001f, side);
            var arcCut = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.Lerp(Mathf.PI * 0.55f, Mathf.PI * 0.95f, i / 12f);
                arcCut.Add((new Vector3(0.4f, WheelR + Mathf.Sin(a) * 0.40f, AxleR + Mathf.Cos(a) * -0.40f), Vector3.right));
            }
            Ribbon(N(rd), "black", arcCut, 0.005f, 0.001f, side);
            Ribbon(N(fd), "black", L(DoorSplit, 0.245f, FrontDoorF, 0.245f, 30), 0.005f, 0.001f, side);
            Ribbon(N(rd), "black", L(-0.85f, 0.245f, DoorSplit, 0.245f, 20), 0.005f, 0.001f, side);
            // Линия плеч над ручками и нижний рельеф дверей, поднимающийся к корме
            Ribbon(shell, "paint", L(1.95f, 0.76f, -2.05f, 0.83f, 80), 0.012f, 0.003f, side);
            Ribbon(shell, "paint", L(0.95f, 0.38f, -0.85f, 0.46f, 40), 0.022f, 0.004f, side);
            // Ручки — хром
            foreach (var (door, z) in new[] { (fd, DoorSplit + 0.18f), (rd, RearDoorR + 0.22f) })
            {
                var p = SidePoint(side, 0.80f, z, out var n);
                var dn = N(door);
                Geo.RoundBox(dn.M("chrome"), Frame.Identity, dn.WorldFrame().ToLocal(p + n * 0.012f), new Vector3(0.02f, 0.03f, 0.13f), 0.45f, 8);
            }
            // Юбки: широкий порог цвета кузова почти до земли, под ним чёрная кромка
            Geo.RoundBox(shell.M("paint"), Frame.Identity, new Vector3(0.80f * side, 0.20f, -0.06f), new Vector3(0.12f, 0.12f, 1.50f), 0.45f, 10);
            Geo.RoundBox(shell.M("black"), Frame.Identity, new Vector3(0.82f * side, 0.135f, -0.06f), new Vector3(0.10f, 0.02f, 1.46f), 0.4f, 6);
            // Повторитель поворотника на крыле
            var rp = SidePoint(side, 0.80f, 0.95f, out var rn);
            var rep = N("Shell").Child(side < 0 ? "RepeaterL" : "RepeaterR", rp + rn * 0.004f);
            Geo.RoundBox(rep.M("amber"), Frame.Identity, Vector3.zero, new Vector3(0.012f, 0.018f, 0.05f), 0.5f, 6);
            // Лючок бака справа
            if (side > 0)
            {
                var hatch = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 28; i++)
                {
                    float a = i / 28f * Mathf.PI * 2f;
                    hatch.Add((new Vector3(0.4f, 0.88f + Mathf.Sin(a) * 0.065f, -1.64f + Mathf.Cos(a) * 0.065f), Vector3.right));
                }
                Ribbon(shell, "black", hatch, 0.004f, 0.001f, side);
            }
        }

        void Mirror(float side)
        {
            var door = N(side < 0 ? "DoorFL" : "DoorFR");
            var df = door.WorldFrame();
            var basePt = new Vector3(W * 0.92f * side, 0.93f, 0.78f);
            var center = basePt + new Vector3(0.12f * side, 0.05f, -0.03f);
            var mount = door.Child(side < 0 ? "MirrorL" : "MirrorR", df.ToLocal(center));
            var size = new Vector3(0.20f, 0.12f, 0.10f);
            Geo.RoundBox(mount.M("paint"), Frame.Identity, Vector3.zero, size, 0.5f, 14);
            var from = df.ToLocal(basePt) - mount.pos;
            var to = new Vector3(-0.05f * side, -0.03f, 0.01f);
            Geo.RoundBox(mount.M("black"), Frame.Look((from + to) * 0.5f, to - from, Vector3.up), Vector3.zero, new Vector3(0.05f, 0.024f, (to - from).magnitude + 0.04f), 0.4f, 6);
            MirrorGlass(mount, side < 0 ? "MirrorGlassL" : "MirrorGlassR", size);
        }

        // ---------- Корма ----------

        /// <summary>Большой фонарь, заходящий на крыло: красный верх, светлая секция заднего хода внизу у крышки, хромированная окантовка.</summary>
        void Taillight(float side)
        {
            // Фонарь как у настоящей Гранты: высокий блок на углу сбоку от крышки багажника (на задней стенке, не на боку),
            // верх и наружная часть — красные, внизу у крышки — светлая секция заднего хода с поперечными рёбрами
            var c = RearPoint(0.58f * side, 0.86f, out _);
            var node = body.Child(side < 0 ? "TaillightL" : "TaillightR", c);
            const float X0 = 0.43f, X1 = 0.685f;
            System.Func<float, float> yb = t => Mathf.Lerp(0.705f, 0.765f, t), yt = t => Mathf.Lerp(1.035f, 1.00f, t);
            System.Func<float, float> H(float k) => t => Mathf.Lerp(yb(t), yt(t), k);
            Lamp(node, "black", X0, X1, yb, yt, 0.004f, side, true, 10, 10);
            Lamp(node, "tail_red", X0, X1, H(0.40f), t => yt(t) - 0.006f, 0.016f, side, true, 10, 6);
            Lamp(node, "tail_clear", X0 + 0.006f, 0.60f, t => yb(t) + 0.008f, H(0.38f), 0.016f, side, true, 8, 4);
            Lamp(node, "tail_red", 0.60f, X1, t => yb(t) + 0.006f, H(0.40f), 0.016f, side, true, 4, 4);
            for (int i = 1; i <= 2; i++)
            {
                var rib = new List<(Vector3, Vector3)>();
                for (int k = 0; k <= 6; k++) { float t = Mathf.Lerp(0.02f, 0.68f, k / 6f); rib.Add((new Vector3(Mathf.Lerp(X0, X1, t), H(0.13f * i)(t), -1.6f), Vector3.back)); }
                Ribbon(node, "int_grey", rib, 0.004f, 0.018f, side);
            }
            Ribbon(node, "black", Outline(X0, X1, yb, yt, true, 10), 0.006f, 0.017f, side);
            var blink = node.Child(side < 0 ? "BlinkRL" : "BlinkRR");
            Lamp(blink, "amber", 0.605f, 0.68f, t => yb(t) + 0.012f, H(0.36f), 0.018f, side, true, 4, 2);
        }

        /// <summary>Крышка багажника со спойлером, «ладья» и шильдик, бампер с номером, диффузор и два круглых выхлопа.</summary>
        void RearDetails()
        {
            var tail = N("Tailgate");
            var tf = tail.WorldFrame();
            // Спойлер на кромке крышки
            Geo.RoundBox(tail.M("paint"), Frame.Euler(tf.ToLocal(new Vector3(0f, 1.085f, -2.06f)), new Vector3(-12f, 0f, 0f)), Vector3.zero, new Vector3(1.30f, 0.022f, 0.12f), 0.45f, 10);
            foreach (float side in new[] { -1f, 1f })
            {
                // Шов крышки: вниз вдоль фонаря до бампера и по низу крышки
                Ribbon(tail, "black", Line(new Vector3(0.41f, 0.60f, -1.6f), new Vector3(0.41f, 1.04f, -1.6f), Vector3.back, 12), 0.005f, 0.002f, side);
                Ribbon(tail, "black", Line(new Vector3(0f, 0.60f, -1.6f), new Vector3(0.41f, 0.60f, -1.6f), Vector3.back, 10), 0.005f, 0.002f, side);
                // Рельеф крышки над номером
                Ribbon(tail, "paint", Line(new Vector3(0f, 0.86f, -1.6f), new Vector3(0.38f, 0.875f, -1.6f), Vector3.back, 10), 0.016f, 0.004f, side);
                // «Бедро»: складка от фонаря над задним колесом к двери
                var hip = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++)
                {
                    float t = i / 24f;
                    float z = Mathf.Lerp(-2.0f, -0.9f, t);
                    float y = 0.70f - 0.06f * t * t;
                    hip.Add((new Vector3(0.4f, y, z), Vector3.right));
                }
                Ribbon(N("Shell"), "paint", hip, 0.02f, 0.005f, side);
            }
            var ep = RearPoint(0f, 0.96f, out var en);
            var badge = tail.Child("LadyaRear", tf.ToLocal(ep + en * 0.006f));
            var rf = Facing(Vector3.zero, tf.DirToLocal(en));
            var oval = rf.Mul(new Frame { o = Vector3.zero, x = Vector3.right * 1.45f, y = Vector3.up, z = Vector3.forward });
            Geo.Torus(badge.M("chrome"), oval, 0.032f, 0.0045f, 28, 6);
            Geo.Lathe(badge.M("black"), oval, new[] { new Vector2(0.029f, -0.002f), new Vector2(0f, 0f) }, 24);
            var gp = RearPoint(0.27f, 0.66f, out var gn);
            var lp = RearPoint(-0.30f, 0.66f, out var lpn);
            Geo.RoundBox(tail.M("chrome"), Frame.Identity, tf.ToLocal(lp + lpn * 0.006f), new Vector3(0.06f, 0.013f, 0.005f), 0.4f, 4);   // «LADA»
            Geo.RoundBox(tail.M("chrome"), Frame.Identity, tf.ToLocal(gp + gn * 0.006f), new Vector3(0.11f, 0.015f, 0.005f), 0.4f, 4);
            Geo.RoundBox(tail.M("tail_red"), Frame.Identity, tf.ToLocal(gp + gn * 0.006f + new Vector3(0.03f, -0.018f, 0f)), new Vector3(0.05f, 0.010f, 0.005f), 0.4f, 4);

            var roof = N("Roof");
            var rfr = roof.WorldFrame();
            Geo.Cylinder(roof.M("black"), new Frame { o = rfr.ToLocal(new Vector3(0f, 1.425f, -0.62f)), x = Vector3.right, y = Vector3.Lerp(Vector3.up, Vector3.back, 0.45f).normalized, z = Vector3.forward }, 0.004f, 0f, 0.42f, 6);

            var br = N("BumperR");
            var bfr = br.WorldFrame();
            var pp = RearPoint(0f, 0.50f, out var pn);
            PlateAt(br, pp + pn * 0.014f, pn, "PlateRear");
            foreach (float side in new[] { -1f, 1f })
            {
                Lamp(br, "black", 0f, 0.78f, t => 0.17f, t => 0.30f, 0.004f, side, true, 14, 3);
                for (int k = 0; k < 4; k++)
                {
                    float x = 0.06f + k * 0.09f;
                    Ribbon(br, "black_satin", Line(new Vector3(x, 0.17f, -1.6f), new Vector3(x, 0.30f, -1.6f), Vector3.back, 3), 0.010f, 0.016f, side);
                }
                Lamp(br, "tail_red", 0.66f, 0.78f, t => 0.42f, t => 0.44f, 0.006f, side, true, 4, 2);
                // Круглый выхлоп с хромированным срезом
                var xp = RearPoint(0.58f * side, 0.215f, out var xn);
                var ef = new Frame { o = bfr.ToLocal(xp), x = Vector3.right, y = bfr.DirToLocal(xn), z = Vector3.Cross(Vector3.right, bfr.DirToLocal(xn)) };
                Geo.Lathe(br.M("chrome"), ef, new[] { new Vector2(0.042f, -0.06f), new Vector2(0.042f, 0.05f), new Vector2(0.046f, 0.056f), new Vector2(0.036f, 0.06f) }, 24);
                Geo.Lathe(br.M("int_black"), ef, new[] { new Vector2(0.036f, 0.06f), new Vector2(0.036f, -0.02f), new Vector2(0f, -0.02f) }, 24, false, true);
            }
        }

        void Chassis()
        {
            var m = N("Shell").M("chassis");
            foreach (float z in new[] { AxleF, AxleR })
            {
                var af = new Frame { o = new Vector3(0f, WheelR, z), x = Vector3.up, y = Vector3.right, z = Vector3.forward };
                Geo.Cylinder(m, af, 0.04f, -0.62f, 0.62f, 12);
                Geo.RoundBox(m, Frame.Identity, new Vector3(0.03f, WheelR, z), new Vector3(0.26f, 0.16f, 0.22f), 0.6f, 8);
            }
        }

        // ---------- Салон: чёрный с красным, руль слева ----------

        void Interior()
        {
            var cab = body.Child("Interior", new Vector3(0f, 0f, DZ));
            var id = Frame.Identity;
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.20f, -0.75f), new Vector3(1.5f, 0.03f, 2.3f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.52f, 0.58f), new Vector3(1.45f, 0.62f, 0.04f));
            // Красная окантовка ковриков
            foreach (float x in new[] { -0.36f, 0.36f })
                Geo.RoundBox(cab.M("stitch_red"), id, new Vector3(x, 0.218f, -0.08f), new Vector3(0.48f, 0.004f, 0.62f), 0.1f, 4);
            foreach (float x in new[] { -0.36f, 0.36f })
                Geo.Box(cab.M("carpet"), id, new Vector3(x, 0.221f, -0.08f), new Vector3(0.46f, 0.004f, 0.60f));
            // Торпеда: гладкий тёмный пластик, козырёк щитка
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.86f, 0.40f), new Vector3(1.62f, 0.18f, 0.44f), 0.35f, 10);
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.66f, 0.38f), new Vector3(1.56f, 0.26f, 0.36f), 0.3f, 8);
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(-0.36f, 0.975f, 0.17f), new Vector3(0.42f, 0.05f, 0.16f), 0.45f, 8);
            // Серебристые накладки «под карбон» по сторонам торпеды
            foreach (float x in new[] { -0.62f, 0.52f })
                Geo.RoundBox(cab.M("int_grey"), Frame.Euler(new Vector3(x, 0.80f, 0.19f), new Vector3(0f, 0f, x < 0 ? 8f : -8f)), Vector3.zero, new Vector3(0.36f, 0.025f, 0.02f), 0.4f, 6);
            // Круглые дефлекторы по краям
            foreach (float x in new[] { -0.70f, 0.70f })
            {
                var vf = new Frame { o = new Vector3(x, 0.86f, 0.175f), x = Vector3.right, y = Vector3.back, z = Vector3.up };
                Geo.Torus(cab.M("int_grey"), vf, 0.045f, 0.007f, 28, 6);
                Geo.Lathe(cab.M("grille"), vf, new[] { new Vector2(0.04f, 0f), new Vector2(0.03f, -0.01f), new Vector2(0f, -0.006f) }, 20);
            }
            // Щиток: два больших прибора с красными кольцами, указатель бензина между ними
            var c = new Vector3(-0.36f, 0.90f, 0.15f);
            eyesLocal = DriverEyes - new Vector3(0f, 0f, DZ);   // приборы — в системе салона
            Geo.RoundBox(cab.M("gauge_face"), id, c + new Vector3(0f, 0f, 0.03f), new Vector3(0.38f, 0.15f, 0.02f), 0.4f, 6);
            Gauge(cab, "Speed", c + new Vector3(-0.095f, 0f, 0.01f), 0.062f, true, "gauge_face", "white");
            Gauge(cab, "Tach", c + new Vector3(0.095f, 0f, 0.01f), 0.062f, true, "gauge_face", "white");
            Gauge(cab, "Fuel", c + new Vector3(0f, -0.035f, 0.012f), 0.026f, false, "gauge_face", "white");
            foreach (float dx in new[] { -0.095f, 0.095f })
            {
                var g = c + new Vector3(dx, 0f, 0.008f);
                var away = (g - DriverEyes).normalized;
                var ring = Frame.Look(g, away, Vector3.up);
                Geo.Torus(cab.M("tail_red"), new Frame { o = ring.o, x = ring.x, y = ring.z, z = -ring.y }, 0.066f, 0.0025f, 36, 4);
            }
            eyesLocal = null;
            var fl = cab.Child("FuelLamp", c + new Vector3(0.03f, -0.06f, 0.005f));
            Geo.RoundBox(fl.M("lamp_off"), id, Vector3.zero, new Vector3(0.02f, 0.01f, 0.004f), 0.3f, 4);
            var el = cab.Child("EngineLamp", c + new Vector3(-0.03f, -0.06f, 0.005f));
            Geo.RoundBox(el.M("lamp_off"), id, Vector3.zero, new Vector3(0.02f, 0.01f, 0.004f), 0.3f, 4);

            // Центральная консоль: дефлекторы, магнитола, климат
            var stack = cab.Child("CenterStack", new Vector3(0f, 0.72f, 0.18f), new Vector3(10f, 0f, 0f));
            Geo.RoundBox(stack.M("int_grey"), id, Vector3.zero, new Vector3(0.26f, 0.36f, 0.05f), 0.25f, 8);
            foreach (float x in new[] { -0.06f, 0.06f })
                Geo.RoundBox(stack.M("grille"), id, new Vector3(x, 0.13f, -0.028f), new Vector3(0.09f, 0.045f, 0.012f), 0.25f, 6);
            var screen = stack.Child("RadioScreen", new Vector3(0f, 0.05f, -0.03f));
            Geo.Quad(screen.M("screen"), id, 0.14f, 0.035f);
            Geo.Cylinder(stack.M("chrome"), Frame.Euler(new Vector3(0f, -0.01f, -0.03f), new Vector3(-90f, 0f, 0f)), 0.016f, 0f, 0.014f, 16);
            for (int i = 0; i < 3; i++)
                Geo.Cylinder(stack.M("int_black"), Frame.Euler(new Vector3(-0.07f + i * 0.07f, -0.12f, -0.03f), new Vector3(-90f, 0f, 0f)), 0.022f, 0f, 0.016f, 16);
            // Тоннель: рычаг КПП с хромированной ручкой, ручник, подстаканники
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.36f, -0.35f), new Vector3(0.24f, 0.30f, 1.0f), 0.3f, 8);
            Geo.Cylinder(cab.M("leather_black"), new Frame { o = new Vector3(0f, 0.50f, -0.08f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.035f, 0f, 0.10f, 12);
            var knob = new List<Vector2>();
            for (int i = 0; i <= 8; i++) { float a = Mathf.Lerp(-Mathf.PI / 2f, Mathf.PI / 2f, i / 8f); knob.Add(new Vector2(Mathf.Cos(a) * 0.028f, Mathf.Sin(a) * 0.028f)); }
            knob.Reverse();
            Geo.Lathe(cab.M("chrome"), new Frame { o = new Vector3(0f, 0.63f, -0.08f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, knob, 16);
            Geo.Cylinder(cab.M("int_grey"), new Frame { o = new Vector3(0f, 0.55f, -0.08f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.008f, 0f, 0.07f, 8);
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(0f, 0.53f, -0.40f), new Vector3(0.04f, 0.04f, 0.26f), 0.5f, 6);

            SteeringWheel();
            foreach (float x in new[] { -0.36f, 0.36f }) Seat(cab, x, -0.75f, x < 0 ? "SeatBackL" : "SeatBackR", 0.50f);
            Geo.RoundBox(cab.M("cloth_dark"), id, new Vector3(0f, 0.38f, -1.35f), new Vector3(1.30f, 0.12f, 0.46f), 0.3f, 8);
            var rb = cab.Child("RearBack", new Vector3(0f, 0.42f, -1.58f), new Vector3(-24f, 0f, 0f));
            Geo.RoundBox(rb.M("cloth_dark"), id, new Vector3(0f, 0.28f, 0f), new Vector3(1.30f, 0.56f, 0.12f), 0.3f, 8);
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.98f, -1.78f), new Vector3(1.3f, 0.02f, 0.30f));
            foreach (var door in new[] { "DoorFL", "DoorFR", "DoorRL", "DoorRR" })
            {
                float side = door.EndsWith("L") ? -1f : 1f;
                float z = (door.StartsWith("DoorF") ? -0.05f : -0.95f) + DZ;
                var dn = N(door);
                var df = dn.WorldFrame();
                Geo.RoundBox(dn.M("int_black"), id, df.ToLocal(new Vector3(0.76f * side, 0.62f, z)), new Vector3(0.08f, 0.05f, 0.42f), 0.4f, 8);
                Geo.RoundBox(dn.M("int_grey"), id, df.ToLocal(new Vector3(0.775f * side, 0.79f, z + 0.12f)), new Vector3(0.02f, 0.035f, 0.10f), 0.4f, 6);
            }
            Geo.Cylinder(cab.M("int_black"), new Frame { o = new Vector3(0f, 1.33f, -0.10f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.012f, -0.04f, 0.02f, 8);
            var rm = cab.Child("RearMirror", new Vector3(0f, 1.28f, -0.12f), new Vector3(0f, 20f, 0f));
            Geo.RoundBox(rm.M("int_black"), id, new Vector3(0f, 0f, 0.016f), new Vector3(0.25f, 0.07f, 0.035f), 0.55f, 10);
            Geo.Quad(rm.Child("RearMirrorGlass", new Vector3(0f, 0f, -0.0025f)).M("mirror"), id, 0.23f, 0.055f);
            Driver(0.84f, -0.85f + DZ, -14f);
        }

        void SteeringWheel()
        {
            var w = body.Child("SteeringWheel", SteeringPos, new Vector3(SteeringTilt, 0f, 0f));
            var id = Frame.Identity;
            const float R = 0.18f;
            Geo.Torus(w.M("leather_black"), id, R, 0.016f, 48, 10);
            foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.back })
            {
                var f = Frame.Look(dir * R * 0.5f, dir, Vector3.up);
                Geo.RoundBox(w.M("int_black"), f, Vector3.zero, new Vector3(0.05f, 0.02f, R * 0.95f), 0.4f, 6);
            }
            Geo.RoundBox(w.M("leather_black"), id, new Vector3(0f, 0.015f, -0.01f), new Vector3(0.15f, 0.05f, 0.12f), 0.5f, 10);
            Geo.Cylinder(w.M("int_black"), id, 0.035f, -0.3f, -0.01f, 12);
        }

        void Seat(ModelNode cab, float x, float z, string backName, float width)
        {
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("cloth_dark"), id, new Vector3(x, 0.40f, z), new Vector3(width, 0.13f, 0.52f), 0.3f, 8);
            var back = cab.Child(backName, new Vector3(x, 0.44f, z - 0.27f), new Vector3(-18f, 0f, 0f));
            Geo.RoundBox(back.M("cloth_dark"), id, new Vector3(0f, 0.30f, 0f), new Vector3(width, 0.60f, 0.13f), 0.3f, 8);
            Geo.RoundBox(back.M("cloth_dark"), id, new Vector3(0f, 0.68f, -0.01f), new Vector3(0.26f, 0.16f, 0.11f), 0.55f, 8);
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
                WheelModel.FiveSpoke(mesh, WheelR, 0.205f, 10, 0.216f, "alloy", "rim_black", 0.6f);
                WheelModel.Caliper(mount, side, 1.0f);
            }
        }
    }
}

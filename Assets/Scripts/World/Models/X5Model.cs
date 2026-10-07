using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «BMW X5 M» (E70, 2009–2013) — кроссовер Давидыча в золотом хромированном камуфляже.
    /// Размеры как у настоящего: 4,85 × 1,93 × 1,76 м, база 2,93 м, колёса 20".
    /// Кузов по сечениям (<see cref="CarBody"/>), наклонное лобовое и заднее стёкла, три боковых окна,
    /// двойные «почки» и ангельские глазки, жабры на крыльях, рейлинги, спойлер на крыше, четыре выхлопа,
    /// дымчатые фонари, чёрный салон с цифровым щитком и экраном iDrive. На капоте и передних дверях — наклейка-питбуль.
    /// Весь кузов — материал «paint» (в игре подставляется золотой камуфляж).
    /// </summary>
    public class X5Model : SportsCarModel
    {
        public const float AxleF = 1.47f, AxleR = -1.46f;
        public const float WheelR = 0.37f, Track = 0.80f;
        const float W = 0.965f;
        const float FrontDoorF = 0.88f, DoorSplit = -0.17f, RearDoorR = -1.18f;
        // Салон сдвинут относительно «Гелика»: лобовое стекло ниже и дальше от водителя
        const float DZ = 0.15f, DY = -0.05f;

        public static readonly Vector3 DriverEyes = new Vector3(-0.38f, 1.40f, -0.20f);
        public static readonly Vector3 SteeringPos = new Vector3(-0.38f, 1.04f, 0.30f);
        public const float SteeringTilt = -62f;

        static Model cached;

        public static Model Get()
        {
            if (cached == null) cached = new X5Model().Build();
            return cached;
        }

        public static X5Model Shape() => new X5Model();

        protected override Vector3 Eyes => DriverEyes;

        X5Model()
        {
            Front = 2.43f; Rear = -2.43f;
            Deck = -2.36f; RoofRear = -1.97f; Header = 0.05f; Cowl = 0.95f;
            FrontRound = 1.90f; RearRound = -2.15f; FrontPow = 2.6f; RearPow = 3.2f;
            RockerFrac = 0.90f; RockerAngle = 35f;

            W0 = new Curve().Key(-2.43f, 0.95f).Key(-1.4f, W).Key(0f, 0.96f).Key(1.5f, W).Key(2.43f, 0.93f);
            YBot = new Curve().Key(-2.43f, 0.40f).Key(-2.3f, 0.34f).Key(-1.6f, 0.30f).Key(0f, 0.29f).Key(1.6f, 0.30f).Key(2.3f, 0.30f).Key(2.43f, 0.34f);
            YMax = new Curve().Key(-2.43f, 0.60f).Key(-2.2f, 0.64f).Key(2.2f, 0.64f).Key(2.43f, 0.52f);
            // Линия плеч; на носу опускается — верх «морды» скруглён к фарам и почкам
            YBelt = new Curve().Key(-2.43f, 1.06f).Key(Deck, 1.14f).Key(-2.1f, 1.18f).Key(-1.6f, 1.19f).Key(-0.2f, 1.15f).Key(0.6f, 1.13f)
                .Key(Cowl, 1.10f, true).Key(1.4f, 1.07f).Key(1.9f, 1.04f).Key(2.2f, 1.0f).Key(2.3f, 0.95f).Key(2.37f, 0.88f).Key(2.41f, 0.78f).Key(2.43f, 0.66f);
            FBelt = new Curve().Key(-2.43f, 0.92f).Key(-1.0f, 0.935f).Key(0.6f, 0.935f).Key(Cowl, 0.92f, true).Key(2.0f, 0.91f).Key(2.43f, 0.90f);
            YTop = new Curve().Key(-2.43f, 1.10f).Key(Deck, 1.24f, true).Key(-2.2f, 1.42f).Key(RoofRear, 1.66f, true).Key(-1.4f, 1.73f)
                .Key(-0.6f, 1.765f).Key(Header, 1.735f, true).Key(0.5f, 1.46f).Key(Cowl, 1.21f, true).Key(1.4f, 1.20f)
                .Key(1.9f, 1.17f).Key(2.2f, 1.12f).Key(2.3f, 1.06f).Key(2.37f, 0.98f).Key(2.41f, 0.88f).Key(2.43f, 0.70f);
            YRoofEdge = new Curve().Key(Deck, 1.20f, true).Key(-2.2f, 1.38f).Key(RoofRear, 1.63f, true).Key(-0.6f, 1.72f)
                .Key(Header, 1.69f, true).Key(0.5f, 1.43f).Key(Cowl, 1.10f, true);
            XRoofEdge = new Curve().Key(Deck, 0.74f, true).Key(-2.2f, 0.72f).Key(RoofRear, 0.68f, true).Key(-0.6f, 0.735f)
                .Key(Header, 0.72f, true).Key(0.5f, 0.80f).Key(Cowl, 0.883f, true);
            AngC = new Curve().Key(-2.43f, 98f).Key(2.43f, 98f);
            AngE0 = new Curve().Key(-2.43f, 150f).Key(Deck, 150f).Key(RoofRear, 125f).Key(Header, 125f).Key(Cowl, 150f).Key(1.4f, 160f).Key(2.43f, 165f);
            AngD0 = new Curve().Key(-2.43f, 100f).Key(2.43f, 100f);
            AngD1 = new Curve().Key(-2.43f, 115f).Key(2.43f, 115f);

            arches.Add(new Arch { z = AxleF, y = WheelR, r = 0.46f, xMin = 0.64f });
            arches.Add(new Arch { z = AxleR, y = WheelR, r = 0.46f, xMin = 0.64f });
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
                    // Лобовое стекло с чёрной рамкой
                    float edge = Mathf.Min(z - Header, Cowl - z);
                    if (v > 0.07f && edge > 0.03f) mat = "glass";
                    else if (v > 0.04f && edge > 0.006f) mat = "black";
                }
                else if (z > Deck && z < RoofRear)
                {
                    // Заднее стекло — наклонное, в чёрной рамке
                    float edge = Mathf.Min(z - Deck, RoofRear - z);
                    if (v > 0.10f && edge > 0.035f) mat = "glass";
                    else if (v > 0.06f && edge > 0.008f) mat = "black";
                }
                if (z > Cowl + 0.015f && z < 2.28f) node = "Hood";
                else if (z < Header && z >= RoofRear) node = "Roof";
                else if (z < RoofRear) node = "Tailgate";
            }
            else if (seg == SegD && InCabin(z))
            {
                // Три окна: передняя дверь, задняя дверь, заднее боковое. Стойки B и C — чёрные.
                bool win = Between(z, -0.12f, 0.78f) || Between(z, -1.12f, -0.24f) || Between(z, -1.92f, -1.26f);
                bool frame = Between(z, -0.16f, 0.88f) || Between(z, -1.16f, -0.20f) || Between(z, -1.96f, -1.22f) || Between(z, -0.24f, -0.12f)
                    || Between(z, -1.26f, -1.12f) || z < -1.92f;
                if (win && v > 0.07f && v < 0.93f) mat = "glass";
                else if (frame && v > 0.03f) mat = "black";
                else if (z > 0.78f && v > 0.03f) mat = "black"; // треугольник у зеркала и стойка A
            }
            bool lower = seg == SegB || seg == SegC || seg == SegD;
            if (lower && p.y > 0.42f)
            {
                if (Between(z, DoorSplit, FrontDoorF)) node = side > 0 ? "DoorFR" : "DoorFL";
                else if (Between(z, RearDoorR, DoorSplit)) node = side > 0 ? "DoorRR" : "DoorRL";
            }
            // Бамперы — часть кузова (форма общая), но отдельные узлы: мнутся и отваливаются. Граница — по ряду клеток
            // (сегмент и доля v), а не по высоте: так шов идёт ровной линией вдоль кузова, без «лесенки».
            bool bumperRow = seg <= SegB || (seg == SegC && v < 0.22f);
            if (z < -2.30f && seg >= SegB && !bumperRow) node = "Tailgate";
            if (bumperRow && z > 2.12f) node = "BumperF";
            if (bumperRow && z < -2.12f) node = "BumperR";
            if (mat != "glass" && z < Cowl) inner = seg == SegA ? "carpet" : seg >= SegD ? "int_roof" : "int_door";
            return new CellInfo { node = node, mat = mat, innerMat = inner };
        }

        protected override Vector3 Pivot(string name)
        {
            switch (name)
            {
                case "Hood": return new Vector3(0f, 1.15f, Cowl + 0.02f);
                case "Roof": return new Vector3(0f, 1.74f, -0.8f);
                case "Tailgate": return new Vector3(0f, 1.64f, -1.99f); // подъёмная дверь багажника — петли сверху
                case "DoorFL": return new Vector3(-W, 0.9f, FrontDoorF);
                case "DoorFR": return new Vector3(W, 0.9f, FrontDoorF);
                case "DoorRL": return new Vector3(-W, 0.9f, DoorSplit);
                case "DoorRR": return new Vector3(W, 0.9f, DoorSplit);
                case "BumperF": return new Vector3(0f, 0.5f, 2.3f);
                case "BumperR": return new Vector3(0f, 0.5f, -2.3f);
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
                foreach (var a in arches) Flare(a, side);
            }
            FrontEnd();
            Wipers();
            RearDetails();
            Pitbulls();
            Chassis();
            Interior();
            BuildWheels();
            Triplanar(model.root);
            return model;
        }

        /// <summary>
        /// Камуфляж без растяжения: UV деталей цвета кузова считаем по положению в метрах одной косой проекцией. Плитка текстуры — 2 м.
        /// </summary>
        static void Triplanar(ModelNode node)
        {
            int k = node.mats.IndexOf("paint");
            if (k >= 0)
            {
                var m = node.meshes[k];
                var f = node.WorldFrame();
                for (int i = 0; i < m.v.Count; i++)
                {
                    var p = f.P(m.v[i]);
                    // Одна косая проекция на все грани: ни одна плоскость не вырождается и нет швов между гранями
                    var uv = new Vector2(0.8f * p.x + 0.6f * p.z, p.y + 0.5f * p.x - 0.4f * p.z);
                    m.uv[i] = uv * 0.5f;
                }
            }
            foreach (var c in node.children) Triplanar(c);
        }

        /// <summary>Расширитель арки: «бровь» цвета кузова по дуге над колесом.</summary>
        void Flare(Arch a, float side)
        {
            var m = N("Shell").M("paint");
            float yb = 0.34f;
            float th0 = Mathf.Asin(Mathf.Clamp((yb - a.y) / a.r, -1f, 1f));
            float th1 = Mathf.PI - th0;
            Vector3 P(float u, float b)
            {
                float th = Mathf.Lerp(th0, th1, u);
                float phi = b * Mathf.PI * 2f;
                float cs = Mathf.Cos(phi), sn = Mathf.Sin(phi);
                float rad = 0.025f + Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), 0.45f) * 0.03f;
                float outw = 0.008f + Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.45f) * 0.03f;
                float r = a.r + rad;
                return new Vector3(side * (W + outw - 0.01f), a.y + Mathf.Sin(th) * r, a.z + Mathf.Cos(th) * r);
            }
            Geo.Surface(m, Frame.Identity, 40, 14, P, side < 0);
        }

        static Frame Facing(Vector3 o, Vector3 n)
        {
            // Система с осью Y по нормали (для тел вращения «лицом» наружу)
            var lf = Frame.Look(o, n, Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up);
            return new Frame { o = lf.o, x = lf.x, y = lf.z, z = -lf.y };
        }

        static Frame Fy(float h) => new Frame { o = new Vector3(0f, h, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward };

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

        // ---------- Перед ----------

        /// <summary>
        /// Деталь, лежащая на кузове спереди или сзади: от x0 до x1 (правая половина, side −1 — зеркально),
        /// по высоте от yb(t) до yt(t), t — доля от x0 к x1. Повторяет изгиб кузова, заворачивает за угол.
        /// </summary>
        void Lamp(ModelNode node, string mat, float x0, float x1, System.Func<float, float> yb, System.Func<float, float> yt,
            float offset, float side, bool rear = false, int na = 16, int nb = 6)
        {
            float z0 = rear ? -1.9f : 1.9f;
            var dir = rear ? Vector3.back : Vector3.forward;
            Patch(node, mat, na, nb, (a, b) => (new Vector3(Mathf.Lerp(x0, x1, a), Mathf.Lerp(yb(a), yt(a), b), z0), dir), offset, side);
        }

        /// <summary>Контур детали (для рамок и светящихся полос): лучи по кругу вдоль её краёв.</summary>
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

        /// <summary>
        /// Фара-«клин» от почки до крыла: верх ровный под кромкой капота, низ поднимается к углу.
        /// Внутри два «ангельских глаза» (светящиеся кольца) с отражателями, по верху — светодиодная бровь, снизу — поворотник.
        /// </summary>
        void Headlight(float side)
        {
            var c = FrontPoint(0.62f * side, 0.92f, out _);
            var node = body.Child(side < 0 ? "HeadlightL" : "HeadlightR", c);
            System.Func<float, float> yb = t => Mathf.Lerp(0.835f, 0.945f, t * t), yt = t => Mathf.Lerp(0.985f, 1.0f, t);
            Lamp(node, "housing", 0.40f, 0.86f, yb, yt, 0.004f, side);
            Lamp(node, "lens", 0.40f, 0.86f, yb, yt, 0.022f, side);
            Ribbon(node, "chrome", Outline(0.40f, 0.86f, yb, yt, false), 0.008f, 0.012f, side);
            var f = node.WorldFrame();
            foreach (var (x, r) in new[] { (0.50f, 0.046f), (0.675f, 0.036f) })
            {
                float t = (x - 0.40f) / 0.46f;
                var p = FrontPoint(x * side, (yb(t) + yt(t)) * 0.5f - 0.004f, out var n);
                var rf = Facing(f.ToLocal(p + n * 0.012f), f.DirToLocal(n));
                Geo.Torus(node.M("lamp_glow"), rf, r, 0.0055f, 36, 6);
                Geo.Lathe(node.M("reflector"), rf, new[] { new Vector2(r - 0.004f, 0f), new Vector2(r * 0.6f, -0.006f), new Vector2(0f, -0.008f) }, 24, true, true);
                // Линза-проектор: тёмный стеклянный купол внутри кольца, с хромированной оправой
                Geo.Torus(node.M("chrome"), rf.Mul(Fy(0.004f)), r * 0.62f, 0.004f, 28, 6);
                Geo.Lathe(node.M("glass_dark"), rf, new[] { new Vector2(r * 0.58f, 0.002f), new Vector2(r * 0.45f, 0.012f), new Vector2(r * 0.25f, 0.018f), new Vector2(0f, 0.02f) }, 28);
            }
            var brow = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 16; i++) { float t = i / 16f; brow.Add((new Vector3(Mathf.Lerp(0.43f, 0.84f, t), yt(t) - 0.013f, 1.9f), Vector3.forward)); }
            Ribbon(node, "lamp_glow", brow, 0.007f, 0.016f, side);
            var blink = node.Child(side < 0 ? "BlinkFL" : "BlinkFR");
            Lamp(blink, "amber", 0.62f, 0.85f, t => yb(Mathf.Lerp(0.48f, 0.98f, t)) + 0.006f, t => yb(Mathf.Lerp(0.48f, 0.98f, t)) + 0.022f, 0.016f, side, false, 10, 2);
        }

        /// <summary>Двойные «почки» (чёрные, с рёбрами и рамкой), эмблема, бампер М: воздухозаборники с лезвием цвета кузова, губа, номер.</summary>
        void FrontEnd()
        {
            var shell = N("Shell");
            var id = Frame.Identity;
            System.Func<float, float> kb = t => t < 0.75f ? 0.765f : Mathf.Lerp(0.765f, 0.84f, (t - 0.75f) / 0.25f);
            System.Func<float, float> kt = t => 0.968f - 0.012f * t;
            foreach (float side in new[] { -1f, 1f })
            {
                var g = shell.Child(side < 0 ? "KidneyL" : "KidneyR");
                Lamp(g, "grille", 0.03f, 0.36f, kb, kt, 0.006f, side, false, 14, 8);
                Ribbon(g, "black", Outline(0.03f, 0.36f, kb, kt, false), 0.022f, 0.016f, side);
                for (int i = 1; i <= 6; i++)
                {
                    float y = Mathf.Lerp(0.775f, 0.958f, i / 7f);
                    float xe = y > 0.84f ? 0.35f : Mathf.Lerp(0.28f, 0.35f, (y - 0.765f) / 0.075f);
                    var rib = new List<(Vector3, Vector3)>();
                    for (int k = 0; k <= 8; k++) rib.Add((new Vector3(Mathf.Lerp(0.04f, xe, k / 8f), y, 1.9f), Vector3.forward));
                    Ribbon(g, "int_grey", rib, 0.007f, 0.012f, side);
                }
            }
            // Эмблема на кромке капота
            var ep = FrontPoint(0f, 1.035f, out var en);
            var em = shell.Child("Roundel", ep + en * 0.004f);
            var ef = Facing(Vector3.zero, en);
            Geo.Torus(em.M("chrome"), ef, 0.036f, 0.005f, 28, 6);
            Geo.Lathe(em.M("black"), ef, new[] { new Vector2(0.033f, 0f), new Vector2(0.025f, 0.004f), new Vector2(0f, 0.005f) }, 24);

            // Шов капота по краю крыльев и силовые «рёбра»
            foreach (float side in new[] { -1f, 1f })
            {
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++) seam.Add((new Vector3(0.74f, 0.9f, Mathf.Lerp(Cowl + 0.03f, 2.2f, i / 24f)), Vector3.up));
                Ribbon(N("Hood"), "black", seam, 0.006f, 0.001f, side);
                var rib = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++) rib.Add((new Vector3(Mathf.Lerp(0.30f, 0.22f, i / 24f), 0.9f, Mathf.Lerp(Cowl + 0.12f, 2.15f, i / 24f)), Vector3.up));
                Ribbon(N("Hood"), "paint", rib, 0.03f, 0.006f, side);
            }
            var cross = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 16; i++) cross.Add((new Vector3(Mathf.Lerp(0f, 0.74f, i / 16f), 1.0f, 1.9f), Vector3.forward));
            Ribbon(N("Hood"), "black", cross, 0.006f, 0.002f, 1f);
            Ribbon(N("Hood"), "black", cross, 0.006f, 0.002f, -1f);

            // Бампер М: центральный заборник под номером, огромные боковые с «лезвием» цвета кузова, чёрная губа
            var bf = N("BumperF");
            foreach (float side in new[] { -1f, 1f })
            {
                Lamp(bf, "grille", 0f, 0.44f, t => 0.37f, t => 0.60f - 0.03f * t * t, 0.004f, side, false, 8, 6);
                for (int i = 1; i <= 3; i++)
                {
                    var rib = new List<(Vector3, Vector3)>();
                    for (int k = 0; k <= 8; k++) rib.Add((new Vector3(Mathf.Lerp(0f, 0.43f, k / 8f), 0.37f + i * 0.058f, 1.9f), Vector3.forward));
                    Ribbon(bf, "housing", rib, 0.012f, 0.008f, side);
                }
                System.Func<float, float> sb = t => 0.36f + 0.02f * t, st = t => 0.67f - 0.09f * t * t;
                Lamp(bf, "grille", 0.55f, 0.88f, sb, st, 0.004f, side, false, 12, 6);
                Ribbon(bf, "black", Outline(0.55f, 0.88f, sb, st, false), 0.016f, 0.008f, side);
                for (int i = 1; i <= 3; i++)
                {
                    var rib = new List<(Vector3, Vector3)>();
                    for (int k = 0; k <= 8; k++) rib.Add((new Vector3(Mathf.Lerp(0.56f, 0.86f, k / 8f), 0.36f + i * 0.07f, 1.9f), Vector3.forward));
                    Ribbon(bf, "housing", rib, 0.012f, 0.008f, side);
                }
                var blade = new List<(Vector3, Vector3)>();
                for (int k = 0; k <= 6; k++) blade.Add((new Vector3(0.70f, Mathf.Lerp(0.37f, 0.64f, k / 6f), 1.9f), Vector3.forward));
                Ribbon(bf, "paint", blade, 0.035f, 0.012f, side);
            }
            var bfr = bf.WorldFrame();
            Geo.RoundBox(bf.M("black"), id, bfr.ToLocal(new Vector3(0f, 0.32f, 2.33f)), new Vector3(1.62f, 0.035f, 0.18f), 0.3f, 8);
            var pp = FrontPoint(0f, 0.50f, out var pn);
            PlateAt(bf, pp + pn * 0.016f, pn, "PlateFront");
        }

        /// <summary>Дворники: два чёрных поводка с щётками, лежат у основания лобового стекла.</summary>
        void Wipers()
        {
            var shell = N("Shell");
            // Ribbon кладёт точки на половину side, поэтому x — по модулю, а сторона — знаком
            foreach (var (x0, x1, side) in new[] { (0.62f, 0.04f, -1f), (0.06f, 0.64f, 1f) })
            {
                var arm = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++)
                {
                    float t = i / 16f;
                    float x = Mathf.Lerp(x0, x1, t);
                    float z = Cowl - 0.05f - 0.06f * (side < 0 ? 1f - t : t);   // чуть поднимается по стеклу к концу щётки
                    arm.Add((new Vector3(x, 1.0f, z), Vector3.up));
                }
                Ribbon(shell, "black", arm, 0.022f, 0.012f, side);
                Ribbon(shell, "black_satin", arm, 0.010f, 0.022f, side);
            }
        }

        // ---------- Бока ----------

        void SideDetails(float side)
        {
            var shell = N("Shell");
            List<(Vector3, Vector3)> Line(float z0, float y0, float z1, float y1, int n = 16)
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
            // Щели дверей
            Ribbon(N(fd), "black", Line(FrontDoorF, 0.36f, FrontDoorF, 1.15f), 0.006f, 0.001f, side);
            Ribbon(N(fd), "black", Line(DoorSplit, 0.36f, DoorSplit, 1.15f), 0.006f, 0.001f, side);
            Ribbon(N(rd), "black", Line(RearDoorR, 0.36f, RearDoorR, 1.18f), 0.006f, 0.001f, side);
            Ribbon(N(fd), "black", Line(DoorSplit, 0.365f, FrontDoorF, 0.365f, 30), 0.006f, 0.001f, side);
            Ribbon(N(rd), "black", Line(RearDoorR, 0.365f, DoorSplit, 0.365f, 20), 0.006f, 0.001f, side);
            // Линия характера вдоль борта и нижний молдинг порога
            Ribbon(shell, "black", Line(2.0f, 0.93f, -2.15f, 0.95f, 60), 0.012f, 0.003f, side);
            Ribbon(shell, "black", Line(1.0f, 0.36f, -1.0f, 0.36f, 30), 0.05f, 0.004f, side);
            // Ручки: цвета кузова
            foreach (var (door, z) in new[] { (fd, DoorSplit + 0.20f), (rd, RearDoorR + 0.20f) })
            {
                var p = SidePoint(side, 1.03f, z, out var n);
                var dn = N(door);
                Geo.RoundBox(dn.M("paint"), Frame.Identity, dn.WorldFrame().ToLocal(p + n * 0.012f), new Vector3(0.022f, 0.03f, 0.16f), 0.45f, 8);
            }
            // «Изгиб Хофмайстера»: низ заднего бокового окна у стойки C загибается вверх — гладкая чёрная накладка поверх стекла
            Patch(shell, "black", 16, 4, (a, b) =>
            {
                float z = Mathf.Lerp(-1.50f, -1.93f, a);
                float yb = YBelt[z] + 0.005f;
                float k = Mathf.SmoothStep(0f, 1f, a);
                return (new Vector3(0.5f, Mathf.Lerp(yb, yb + 0.02f + 0.32f * k, b), z), Vector3.right);
            }, 0.004f, side);
            // Жабры М на переднем крыле за колесом
            for (int k = 0; k < 4; k++)
            {
                var gl = Line(1.03f, 0.74f + k * 0.035f, 0.93f, 0.76f + k * 0.035f, 6);
                Ribbon(shell, k == 0 ? "chrome" : "black", gl, 0.02f, 0.003f, side);
            }
            // Рейлинги на крыше: тонкие чёрные полосы
            var rail = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float z = Mathf.Lerp(-1.85f, -0.08f, i / 40f);
                rail.Add((new Vector3(0.58f, 1.2f, z), Vector3.up));
            }
            Ribbon(N("Roof"), "black", rail, 0.04f, 0.014f, side);
            Ribbon(N("Roof"), "int_grey", rail, 0.012f, 0.022f, side);
            // Пороги: чёрная накладка
            Geo.RoundBox(shell.M("black"), Frame.Identity, new Vector3(0.94f * side, 0.34f, -0.1f), new Vector3(0.1f, 0.06f, 2.3f), 0.35f, 8);
            // Лючок бака справа сзади
            if (side > 0)
            {
                var hatch = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++)
                {
                    float a = i / 24f * Mathf.PI * 2f;
                    float y = 0.98f + Mathf.Sin(a) * 0.075f, z = -1.70f + Mathf.Cos(a) * 0.075f;
                    hatch.Add((new Vector3(0.5f, y, z), Vector3.right));
                }
                Ribbon(shell, "black", hatch, 0.005f, 0.001f, side);
            }
        }

        /// <summary>Зеркало цвета кузова на двери у стойки; стекло повёрнуто к водителю (он слева).</summary>
        void Mirror(float side)
        {
            var door = N(side < 0 ? "DoorFL" : "DoorFR");
            var df = door.WorldFrame();
            var basePt = new Vector3(W * 0.97f * side, 1.20f, 0.72f);
            var center = basePt + new Vector3(0.14f * side, 0.06f, -0.02f);
            var mount = door.Child(side < 0 ? "MirrorL" : "MirrorR", df.ToLocal(center));
            Geo.RoundBox(mount.M("paint"), Frame.Identity, Vector3.zero, new Vector3(0.24f, 0.16f, 0.12f), 0.45f, 14);
            var from = df.ToLocal(basePt) - mount.pos;
            var to = new Vector3(-0.06f * side, -0.04f, 0.01f);
            Geo.RoundBox(mount.M("black"), Frame.Look((from + to) * 0.5f, to - from, Vector3.up), Vector3.zero, new Vector3(0.05f, 0.028f, (to - from).magnitude + 0.04f), 0.4f, 6);
            MirrorGlass(mount, side < 0 ? "MirrorGlassL" : "MirrorGlassR", new Vector3(0.24f, 0.16f, 0.12f));
        }

        // ---------- Корма ----------

        /// <summary>Широкий дымчатый фонарь, заворачивающий за угол на крыло: красная вставка, светодиодная полоса, янтарный поворотник.</summary>
        void Taillight(float side)
        {
            var c = RearPoint(0.70f * side, 1.07f, out _);
            var node = body.Child(side < 0 ? "TaillightL" : "TaillightR", c);
            System.Func<float, float> yb = t => Mathf.Lerp(0.99f, 1.03f, t), yt = t => Mathf.Lerp(1.15f, 1.12f, t * t);
            Lamp(node, "black", 0.40f, 0.89f, yb, yt, 0.004f, side, true);
            Lamp(node, "tail_smoke", 0.40f, 0.89f, t => yb(t) + 0.008f, t => yt(t) - 0.008f, 0.016f, side, true);
            Lamp(node, "tail_red", 0.50f, 0.87f, t => yb(t) + 0.03f, t => yb(t) + 0.065f, 0.02f, side, true, 12, 2);
            var led = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 14; i++) { float t = i / 14f; led.Add((new Vector3(Mathf.Lerp(0.46f, 0.87f, t), yt(Mathf.Lerp(0.12f, 0.96f, t)) - 0.03f, -1.9f), Vector3.back)); }
            Ribbon(node, "lamp_glow", led, 0.008f, 0.021f, side);
            var blink = node.Child(side < 0 ? "BlinkRL" : "BlinkRR");
            Lamp(blink, "amber", 0.42f, 0.49f, t => 1.02f, t => 1.12f, 0.02f, side, true, 3, 2);
        }

        /// <summary>Дверь багажника: номер, хромированная планка, шов; спойлер на крыше со стоп-сигналом; бампер с диффузором и четырьмя выхлопами.</summary>
        void RearDetails()
        {
            var tail = N("Tailgate");
            var tf = tail.WorldFrame();
            var pp = RearPoint(0f, 0.88f, out var pn);
            PlateAt(tail, pp + pn * 0.012f, pn, "PlateRear");
            var strip = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 12; i++) strip.Add((new Vector3(Mathf.Lerp(0f, 0.36f, i / 12f), 0.985f, -1.9f), Vector3.back));
            Ribbon(tail, "chrome", strip, 0.018f, 0.006f, 1f);
            Ribbon(tail, "chrome", strip, 0.018f, 0.006f, -1f);
            foreach (float side in new[] { -1f, 1f })
            {
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++) seam.Add((new Vector3(0.38f, Mathf.Lerp(0.73f, 0.99f, i / 16f), -1.9f), Vector3.back));
                Ribbon(tail, "black", seam, 0.006f, 0.002f, side);
            }
            var roof = N("Roof");
            var rf = roof.WorldFrame();
            // Антенна-«плавник» на крыше у задней кромки
            Geo.RoundBox(roof.M("black"), Frame.Identity, rf.ToLocal(new Vector3(0f, 1.79f, -1.70f)), new Vector3(0.07f, 0.07f, 0.20f), 0.55f, 12);
            Geo.RoundBox(roof.M("black"), Frame.Identity, rf.ToLocal(new Vector3(0f, 1.765f, -1.68f)), new Vector3(0.085f, 0.02f, 0.25f), 0.5f, 8);
            Geo.RoundBox(roof.M("paint"), Frame.Identity, rf.ToLocal(new Vector3(0f, 1.68f, -2.01f)), new Vector3(1.30f, 0.045f, 0.17f), 0.4f, 10);
            Geo.Box(roof.M("tail_red"), Frame.Identity, rf.ToLocal(new Vector3(0f, 1.68f, -2.098f)), new Vector3(0.56f, 0.014f, 0.006f));

            var br = N("BumperR");
            var bfr = br.WorldFrame();
            foreach (float side in new[] { -1f, 1f })
            {
                Lamp(br, "black", 0f, 0.82f, t => 0.33f, t => 0.44f, 0.004f, side, true, 12, 3);
                Lamp(br, "tail_red", 0.74f, 0.88f, t => 0.56f, t => 0.585f, 0.008f, side, true, 6, 2);
                foreach (float dx in new[] { 0.40f, 0.60f })
                {
                    var p = RearPoint(dx * side, 0.40f, out var n);
                    var ef = new Frame { o = bfr.ToLocal(p), x = Vector3.right, y = bfr.DirToLocal(n), z = Vector3.Cross(Vector3.right, bfr.DirToLocal(n)) };
                    Geo.Lathe(br.M("chrome"), ef, new[] { new Vector2(0.046f, -0.05f), new Vector2(0.046f, 0.03f), new Vector2(0.05f, 0.034f), new Vector2(0.04f, 0.04f) }, 20);
                    Geo.Lathe(br.M("int_black"), ef, new[] { new Vector2(0.04f, 0.04f), new Vector2(0.04f, -0.03f), new Vector2(0f, -0.03f) }, 20, false, true);
                }
            }
        }

        // ---------- Наклейки ----------

        /// <summary>Круглая наклейка-питбуль: диск, лежащий на кузове (повторяет его кривизну), UV — по кругу картинки.</summary>
        void Pitbulls()
        {
            const bool Doors = true;
            var hood = N("Hood");
            // На капоте: смотрит вперёд (верх картинки — вперёд)
            Disc(hood, new Vector3(0f, 0.9f, 1.55f), Vector3.up, Vector3.right, Vector3.forward, 0.25f);
            if (!Doors) return;
            foreach (float side in new[] { -1f, 1f })
            {
                var door = N(side > 0 ? "DoorFR" : "DoorFL");
                // На двери: смотрим на борт снаружи; «право» картинки — к корме у правого борта и к носу у левого
                var u = side > 0 ? Vector3.forward : Vector3.back;
                Disc(door, new Vector3(0.3f * side, 0.80f, 0.50f), new Vector3(side, 0f, 0f), u, Vector3.up, 0.17f);
            }
        }

        /// <summary>
        /// Диск радиуса r вокруг точки c: лучи из c ± по осям (au, av) в направлении dir ищут поверхность кузова,
        /// вершины ложатся на неё. UV: u — вдоль au, v — вдоль av (картинка вписана в круг).
        /// </summary>
        void Disc(ModelNode node, Vector3 c, Vector3 dir, Vector3 au, Vector3 av, float r)
        {
            const int rings = 5, segs = 28;
            var m = node.M("decal_pit");
            var f = node.WorldFrame();
            const float offset = 0.004f;
            int Vert(float dx, float dy, out Vector3 nrm)
            {
                var o = c + au * dx + av * dy;
                HitPoint(o, dir, out var pos, out nrm);
                var uv = new Vector2(0.5f + dx / r * 0.485f, 0.5f + dy / r * 0.485f);
                return m.Add(f.ToLocal(pos + nrm * offset), f.DirToLocal(nrm), uv);
            }
            int center = Vert(0f, 0f, out var cn);
            var ids = new int[rings + 1, segs];
            var nr = new Vector3[rings + 1, segs];
            for (int i = 1; i <= rings; i++)
            for (int j = 0; j < segs; j++)
            {
                float a = j / (float)segs * Mathf.PI * 2f;
                float rr = r * i / rings;
                ids[i, j] = Vert(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, out nr[i, j]);
            }
            for (int j = 0; j < segs; j++)
            {
                int j2 = (j + 1) % segs;
                var a = m.v[center]; var b = m.v[ids[1, j]]; var d = m.v[ids[1, j2]];
                var nrm = f.DirToLocal(cn + nr[1, j] + nr[1, j2]);
                if (Vector3.Dot(Vector3.Cross(b - a, d - a), nrm) >= 0f) m.Tri(center, ids[1, j], ids[1, j2]);
                else m.Tri(center, ids[1, j2], ids[1, j]);
            }
            for (int i = 1; i < rings; i++)
            for (int j = 0; j < segs; j++)
            {
                int j2 = (j + 1) % segs;
                QuadFacing(m, ids[i, j], ids[i, j2], ids[i + 1, j2], ids[i + 1, j], f.DirToLocal(nr[i, j] + nr[i + 1, j2]));
            }
        }

        /// <summary>Мосты под кузовом — чтобы снизу не было пустоты.</summary>
        void Chassis()
        {
            var m = N("Shell").M("chassis");
            foreach (float z in new[] { AxleF, AxleR })
            {
                var af = new Frame { o = new Vector3(0f, WheelR, z), x = Vector3.up, y = Vector3.right, z = Vector3.forward };
                Geo.Cylinder(m, af, 0.05f, -0.7f, 0.7f, 12);
                Geo.RoundBox(m, Frame.Identity, new Vector3(0.03f, WheelR, z), new Vector3(0.3f, 0.2f, 0.24f), 0.6f, 8);
            }
        }

        // ---------- Салон (руль слева, чёрная кожа) ----------

        void Interior()
        {
            var cab = body.Child("Interior");
            var id = Frame.Identity;
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.46f, -0.7f), new Vector3(1.7f, 0.03f, 2.9f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.78f + DY, 0.84f + DZ), new Vector3(1.6f, 0.54f, 0.04f));
            // Торпеда: широкая полка, под ней — «колени»
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(0f, 1.03f, 0.62f + DZ), new Vector3(1.78f, 0.28f, 0.42f), 0.22f, 10);
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.80f, 0.64f + DZ), new Vector3(1.6f, 0.26f, 0.36f), 0.25f, 8);
            Geo.RoundBox(cab.M("int_grey"), id, new Vector3(0f, 0.98f, 0.408f + DZ), new Vector3(1.7f, 0.04f, 0.02f), 0.3f, 6);

            // Щиток прибора: козырёк и цифровой экран, повёрнутые к водителю
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(-0.38f, 1.20f, 0.66f), new Vector3(0.52f, 0.10f, 0.22f), 0.35f, 8);
            var cluster = cab.Child("ClusterScreen", new Vector3(-0.38f, 1.145f, 0.555f), new Vector3(-16f, 0f, 0f));
            Geo.RoundBox(cluster.Child("ClusterBezel").M("gauge_face"), id, new Vector3(0f, 0f, 0.012f), new Vector3(0.50f, 0.17f, 0.02f), 0.2f, 6);
            Geo.Quad(cluster.M("screen_blue"), id, 0.46f, 0.14f);
            foreach (float x in new[] { -0.11f, 0.11f })
                Geo.Torus(cluster.M("white"), new Frame { o = new Vector3(x, 0f, -0.002f), x = Vector3.right, y = Vector3.back, z = Vector3.up }, 0.05f, 0.003f, 32, 4);

            // Экран iDrive на торпеде по центру (на нём радио)
            var info = cab.Child("RadioScreen", new Vector3(0.0f, 1.215f, 0.62f), new Vector3(-8f, 0f, 0f));
            Geo.RoundBox(info.Child("RadioBezel").M("gauge_face"), id, new Vector3(0f, 0f, 0.012f), new Vector3(0.28f, 0.15f, 0.02f), 0.2f, 6);
            Geo.Quad(info.M("screen_blue"), id, 0.25f, 0.12f);
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 1.18f, 0.67f), new Vector3(0.12f, 0.1f, 0.12f), 0.4f, 6);

            // Круглые дефлекторы: по краям и по центру
            foreach (float x in new[] { -0.80f, -0.2f, 0.2f, 0.80f })
            {
                var vf = new Frame { o = new Vector3(x, 1.06f, 0.56f), x = Vector3.right, y = Vector3.back, z = Vector3.up };
                Geo.Torus(cab.M("chrome"), vf, 0.052f, 0.008f, 32, 6);
                Geo.Lathe(cab.M("int_black"), vf, new[] { new Vector2(0.046f, 0f), new Vector2(0.03f, -0.01f), new Vector2(0f, -0.006f) }, 20);
                for (int k = 0; k < 5; k++)
                {
                    float a = k / 5f * 180f;
                    Geo.Box(cab.M("int_grey"), vf.Mul(Frame.Euler(new Vector3(0f, -0.004f, 0f), new Vector3(0f, a, 0f))), Vector3.zero, new Vector3(0.08f, 0.004f, 0.01f));
                }
            }
            // Центральная консоль: высокая, с рычагом, подлокотник
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.78f, -0.15f), new Vector3(0.30f, 0.42f, 1.15f), 0.25f, 8);
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(0f, 1.0f, -0.55f), new Vector3(0.28f, 0.06f, 0.4f), 0.4f, 8);
            Geo.RoundBox(cab.M("int_grey"), id, new Vector3(0f, 0.99f, 0.1f), new Vector3(0.22f, 0.02f, 0.4f), 0.3f, 6);
            Geo.Cylinder(cab.M("chrome"), new Frame { o = new Vector3(0f, 0.98f, 0.18f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.014f, 0f, 0.12f, 10);

            SteeringWheel(cab);
            foreach (float x in new[] { -0.38f, 0.38f }) Seat(cab, x, -0.35f, x < 0 ? "SeatBackL" : "SeatBackR", 0.54f);
            // Задний диван
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(0f, 0.76f, -1.28f), new Vector3(1.5f, 0.14f, 0.52f), 0.3f, 8);
            var rb = cab.Child("RearBack", new Vector3(0f, 0.8f, -1.56f), new Vector3(-14f, 0f, 0f));
            Geo.RoundBox(rb.M("leather_black"), id, new Vector3(0f, 0.34f, 0f), new Vector3(1.5f, 0.68f, 0.14f), 0.3f, 8);
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.74f, -1.95f), new Vector3(1.6f, 0.03f, 0.55f));
            // Двери: подлокотники
            foreach (var door in new[] { "DoorFL", "DoorFR", "DoorRL", "DoorRR" })
            {
                float side = door.EndsWith("L") ? -1f : 1f;
                float z = door.StartsWith("DoorF") ? 0.33f : -0.66f;
                var dn = N(door);
                var df = dn.WorldFrame();
                Geo.RoundBox(dn.M("leather_black"), id, df.ToLocal(new Vector3(0.88f * side, 0.98f, z)), new Vector3(0.09f, 0.06f, 0.5f), 0.4f, 8);
            }
            // Салонное зеркало, повёрнутое к водителю слева
            Geo.Cylinder(cab.M("int_black"), new Frame { o = new Vector3(0f, 1.6f, 0.29f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.012f, -0.04f, 0.02f, 8);
            var rm = cab.Child("RearMirror", new Vector3(0f, 1.55f, 0.27f), new Vector3(0f, 21f, 0f));
            Geo.RoundBox(rm.M("int_black"), id, new Vector3(0f, 0f, 0.016f), new Vector3(0.28f, 0.08f, 0.035f), 0.5f, 10);
            Geo.Quad(rm.Child("RearMirrorGlass", new Vector3(0f, 0f, -0.0025f)).M("mirror"), id, 0.26f, 0.065f);
            // Потолок: светлый, с люком
            foreach (float x in new[] { -0.4f, 0.4f })
                Geo.RoundBox(cab.M("int_roof"), Frame.Euler(new Vector3(x, 1.69f, -0.05f), new Vector3(-3f, 0f, 0f)), Vector3.zero, new Vector3(0.42f, 0.022f, 0.17f), 0.4f, 6);
            Driver(1.12f, -0.42f, -10f);
        }

        void SteeringWheel(ModelNode cab)
        {
            var w = body.Child("SteeringWheel", SteeringPos, new Vector3(SteeringTilt, 0f, 0f));
            var id = Frame.Identity;
            const float R = 0.18f;
            Geo.Torus(w.M("leather_black"), id, R, 0.019f, 48, 10);
            foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.back })
            {
                var f = Frame.Look(dir * R * 0.5f, dir, Vector3.up);
                Geo.RoundBox(w.M("int_black"), f, Vector3.zero, new Vector3(0.05f, 0.02f, R * 0.95f), 0.4f, 6);
                Geo.RoundBox(w.M("int_grey"), f, new Vector3(0f, 0.011f, 0.02f), new Vector3(0.03f, 0.004f, R * 0.5f), 0.4f, 4);
            }
            Geo.RoundBox(w.M("leather_black"), id, new Vector3(0f, 0.015f, -0.005f), new Vector3(0.13f, 0.05f, 0.11f), 0.5f, 10);
            Geo.Torus(w.M("chrome"), new Frame { o = new Vector3(0f, 0.042f, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }, 0.024f, 0.004f, 24, 5);
            Geo.Cylinder(w.M("int_black"), id, 0.035f, -0.3f, -0.01f, 12);
        }

        /// <summary>Кресло: сиденье с валиками, спинка с подголовником (узел спинки наклонён).</summary>
        void Seat(ModelNode cab, float x, float z, string backName, float width)
        {
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(x, 0.76f, z), new Vector3(width, 0.15f, 0.56f), 0.3f, 8);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(cab.M("leather_black"), id, new Vector3(x + b * (width / 2f - 0.04f), 0.79f, z - 0.02f), new Vector3(0.09f, 0.15f, 0.54f), 0.5f, 8);
            var back = cab.Child(backName, new Vector3(x, 0.81f, z - 0.3f), new Vector3(-14f, 0f, 0f));
            Geo.RoundBox(back.M("leather_black"), id, new Vector3(0f, 0.35f, 0f), new Vector3(width, 0.7f, 0.14f), 0.3f, 8);
            for (int i = 0; i < 4; i++)
                Geo.Box(back.M("int_grey"), id, new Vector3(0f, 0.15f + i * 0.12f, 0.072f), new Vector3(width * 0.6f, 0.004f, 0.004f));
            Geo.RoundBox(back.M("leather_black"), id, new Vector3(0f, 0.8f, -0.01f), new Vector3(0.3f, 0.2f, 0.12f), 0.55f, 8);
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
                WheelModel.FiveSpoke(mesh, WheelR, 0.295f, 10, 0.262f, "alloy", "chrome", 0.62f); // 20" двойные спицы, как на фото
                WheelModel.Caliper(mount, side, 1.25f);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «Скайлайн GT-R» R34 (1999–2002) в раскраске из «Двойного форсажа»: серебристый, две синие полосы
    /// по капоту и бамперу, наклонные синие полосы на боках, высокое синее крыло на стойках.
    /// Праворульный. Размеры: 4,60 × 1,785 × 1,36 м, база 2,665 м, колёса 18".
    /// Угловатое купе со «ступенькой» багажника: фары-трапеции, большой воздухозаборник с интеркулером,
    /// по два круглых фонаря с каждой стороны, хромированные шестиспицевые диски.
    /// </summary>
    public class SkylineModel : SportsCarModel
    {
        public const float AxleF = 1.38f, AxleR = -1.285f;
        public const float WheelR = 0.325f, TrackF = 0.75f, TrackR = 0.745f;
        public const float DoorFront = 0.62f, DoorRear = -0.95f;
        const float SideRearPoint = -1.30f;

        public static readonly Vector3 DriverEyes = new Vector3(0.37f, 1.12f, -0.80f);
        public static readonly Vector3 SteeringPos = new Vector3(0.37f, 0.875f, -0.25f);
        public const float SteeringTilt = -62f;

        static Model cached;

        public static Model Get()
        {
            if (cached == null) cached = new SkylineModel().Build();
            return cached;
        }

        public static SkylineModel Shape() => new SkylineModel();

        protected override Vector3 Eyes => DriverEyes;

        SkylineModel()
        {
            Front = 2.30f; Rear = -2.30f;
            Cowl = 0.42f; Header = -0.30f; RoofRear = -1.00f; Deck = -1.58f;
            FrontRound = 1.85f; RearRound = -1.95f; FrontPow = 3.0f; RearPow = 4.0f;
            RockerFrac = 0.9f; RockerAngle = 10f;

            W0 = new Curve().Key(-2.30f, 0.88f).Key(-1.6f, 0.895f).Key(-1.28f, 0.9f).Key(-0.8f, 0.885f).Key(0f, 0.875f)
                .Key(0.8f, 0.88f).Key(1.38f, 0.893f).Key(1.9f, 0.885f).Key(2.30f, 0.87f);
            YBot = new Curve().Key(-2.30f, 0.36f).Key(-2.2f, 0.26f).Key(-2.05f, 0.20f).Key(-1.8f, 0.16f).Key(-1.0f, 0.14f)
                .Key(1.6f, 0.14f).Key(2.05f, 0.12f).Key(2.2f, 0.14f).Key(2.30f, 0.22f);
            YMax = new Curve().Key(-2.30f, 0.58f).Key(-1.6f, 0.55f).Key(-0.5f, 0.50f).Key(1.5f, 0.48f).Key(2.30f, 0.46f);
            YBelt = new Curve().Key(-2.30f, 0.915f).Key(-2.15f, 0.975f).Key(-1.9f, 0.99f).Key(-1.58f, 0.985f).Key(-1.2f, 0.955f)
                .Key(-0.6f, 0.925f).Key(0f, 0.90f).Key(Cowl, 0.865f).Key(0.9f, 0.80f).Key(1.38f, 0.76f).Key(1.6f, 0.75f)
                .Key(2.0f, 0.725f).Key(2.2f, 0.70f).Key(2.30f, 0.655f);
            FBelt = new Curve().Key(-2.30f, 0.86f).Key(-1.9f, 0.89f).Key(-1.4f, 0.93f).Key(0.2f, 0.94f).Key(0.6f, 0.9f)
                .Key(1.0f, 0.87f).Key(2.30f, 0.86f);
            YTop = new Curve().Key(-2.30f, 0.925f).Key(-2.25f, 0.95f).Key(-2.15f, 0.99f).Key(-1.9f, 1.008f).Key(Deck, 1.005f, true)
                .Key(-1.30f, 1.16f).Key(RoofRear, 1.335f, true).Key(-0.65f, 1.36f).Key(Header, 1.335f, true).Key(0.05f, 1.12f)
                .Key(Cowl, 0.875f, true).Key(0.9f, 0.83f).Key(1.38f, 0.79f).Key(1.6f, 0.775f).Key(2.0f, 0.745f).Key(2.2f, 0.72f)
                .Key(2.30f, 0.67f);
            YRoofEdge = new Curve().Key(Deck, 0.986f, true).Key(-1.30f, 1.13f).Key(RoofRear, 1.285f, true).Key(-0.65f, 1.31f)
                .Key(Header, 1.285f, true).Key(0.05f, 1.07f).Key(Cowl, 0.865f, true);
            XRoofEdge = new Curve().Key(Deck, 0.80f, true).Key(-1.30f, 0.70f).Key(RoofRear, 0.64f, true).Key(-0.65f, 0.645f)
                .Key(Header, 0.64f, true).Key(Cowl, 0.81f, true);
            AngC = new Curve().Key(-2.30f, 160f).Key(-1.9f, 150f).Key(-1.6f, 115f).Key(-1.3f, 100f).Key(0.1f, 100f)
                .Key(0.45f, 135f).Key(0.8f, 150f).Key(2.30f, 155f);
            AngE0 = new Curve().Key(-2.30f, 172f).Key(-1.6f, 170f).Key(-1.4f, 155f).Key(-1.0f, 150f).Key(-0.3f, 150f)
                .Key(0.1f, 158f).Key(Cowl, 170f).Key(2.30f, 172f);
            AngD0 = new Curve().Key(-1.6f, 135f).Key(-1.3f, 112f).Key(0.2f, 108f).Key(Cowl, 118f);
            AngD1 = new Curve().Key(-1.6f, 145f).Key(-1.3f, 125f).Key(0.2f, 118f).Key(Cowl, 128f);

            arches.Add(new Arch { z = AxleF, y = 0.33f, r = 0.375f, xMin = 0.57f });
            arches.Add(new Arch { z = AxleR, y = 0.33f, r = 0.385f, xMin = 0.57f });
            Prepare();
        }

        // ---------- Клетки кузова ----------

        float SideWindowTop(float z)
        {
            if (z > -0.92f) return 0.86f;
            float k = Mathf.Clamp01((-0.92f - z) / (-0.92f - SideRearPoint));
            return Mathf.Lerp(0.86f, 0.1f, k * k * (3f - 2f * k) * 0.5f + k * 0.5f);
        }

        protected override CellInfo Classify(float z, int seg, float v, Vector3 p, float side)
        {
            string node = "Shell", mat = "paint", inner = null;
            bool cabin = InCabin(z);
            if (seg == SegE)
            {
                if (z > Header && z < Cowl)
                {
                    float edge = Mathf.Min(z - Header, Cowl - z);
                    if (v > 0.10f && edge > 0.035f) mat = "glass";
                    else if (v > 0.055f && edge > 0.006f) mat = "black";
                }
                else if (z < RoofRear && z > Deck)
                {
                    // Заднее стекло над «ступенькой» багажника
                    float k = (RoofRear - z) / (RoofRear - Deck);
                    float vmin = Mathf.Lerp(0.13f, 0.24f, k);
                    float edge = Mathf.Min(RoofRear - z, z - Deck);
                    if (v > vmin && edge > 0.04f) mat = "glass";
                    else if (v > vmin - 0.05f && edge > 0.012f) mat = "black";
                }
                if (z >= Cowl + 0.015f && z < 2.2f) node = "Hood";
                else if (z <= Header && z >= RoofRear) node = "Roof";
                else if (z < Deck - 0.01f && z > -2.24f) node = "Trunk";
            }
            else if (seg == SegD && cabin)
            {
                float top = SideWindowTop(z);
                bool inZ = z < Cowl - 0.19f && z > SideRearPoint;
                if (z > Cowl - 0.20f && z < Cowl - 0.01f && v < 0.92f) mat = "black";
                else if (inZ && v > 0.10f && v < top) mat = Mathf.Abs(z - (DoorRear - 0.02f)) < 0.02f ? "black" : "glass";
                else if (z < Cowl - 0.01f && z > SideRearPoint - 0.03f && v > 0.05f && v < top + 0.07f) mat = "black";
            }
            bool lower = seg == SegB || seg == SegC || seg == SegD;
            if (lower && z < DoorFront && z > DoorRear && p.y > 0.18f) node = side > 0 ? "DoorR" : "DoorL";
            if (seg <= SegC && z > 2.05f) node = "BumperF";
            if (seg <= SegC && z < -2.08f && p.y < 0.75f) node = "BumperR";
            if (mat != "glass" && z < Cowl && z > Deck - 0.3f && !(seg == SegD && z < Deck + 0.04f))
                inner = seg == SegA ? "carpet" : seg == SegE || seg == SegD ? "int_roof" : "int_door";
            return new CellInfo { node = node, mat = mat, innerMat = inner };
        }

        protected override Vector3 Pivot(string name)
        {
            switch (name)
            {
                case "Hood": return new Vector3(0f, 0.875f, Cowl + 0.02f);
                case "Roof": return new Vector3(0f, 1.35f, -0.65f);
                case "Trunk": return new Vector3(0f, 1.0f, Deck);
                case "DoorL": return new Vector3(-0.87f, 0.6f, -0.15f);
                case "DoorR": return new Vector3(0.87f, 0.6f, -0.15f);
                case "BumperF": return new Vector3(0f, 0.35f, 2.15f);
                case "BumperR": return new Vector3(0f, 0.45f, -2.15f);
            }
            return Vector3.zero;
        }

        // ---------- Сборка ----------

        Model Build()
        {
            StartModel();
            Emit(N);
            SealFirewall(N("Shell"), Cowl - 0.03f, "int_black");
            ArchLiners(N("Shell"), "liner");
            foreach (float side in new[] { 1f, -1f })
            {
                Headlight(side);
                FrontFascia(side);
                Taillights(side);
                SideDetails(side);
                Livery(side);
                Mirror(side);
            }
            CenterDetails();
            Wing();
            Interior();
            BuildWheels();
            return model;
        }

        static Frame Cup(Frame look) => new Frame { o = look.o, x = look.x, y = look.z, z = -look.y };

        // ---------- Перед ----------

        /// <summary>Фара-трапеция: внутренний край у решётки, внешний заходит на крыло. Круглый проектор и отражатель.</summary>
        void Headlight(float side)
        {
            var node = body.Child(side < 0 ? "HeadlightL" : "HeadlightR", new Vector3(0.62f * side, 0.66f, 2.05f));
            (Vector3, Vector3) Ray(float a, float b)
            {
                float ang = Mathf.Lerp(6f, 74f, a) * Mathf.Deg2Rad;
                float top = Mathf.Lerp(0.722f, 0.69f, a), bot = Mathf.Lerp(0.655f, 0.595f, a);
                float y = Mathf.Lerp(bot, top, b);
                return (new Vector3(0.30f, y, 1.75f), new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)));
            }
            Patch(node, "housing", 20, 6, Ray, 0.003f, side);
            Patch(node, "lens", 20, 6, Ray, 0.02f, side);
            var rim = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                rim.Add(t < 0.5f ? Ray(t * 2f, 1f) : Ray(1f - (t - 0.5f) * 2f, 0f));
            }
            rim.Add(Ray(0f, 1f));
            Ribbon(node, "black", rim, 0.012f, 0.004f, side);
            var f = node.WorldFrame();
            // Круглый проектор (внутри) и большой отражатель (снаружи)
            foreach (var (a, rad) in new[] { (0.72f, 0.03f), (0.38f, 0.036f) })
            {
                var r = Ray(a, 0.5f);
                OnBody(r.Item1, r.Item2, side, out var p, out var n);
                var cup = Cup(Frame.Look(f.ToLocal(p), f.DirToLocal(n), Vector3.up));
                Geo.Lathe(node.M("reflector"), cup, new[] { new Vector2(rad, 0.012f), new Vector2(rad * 0.75f, 0.005f), new Vector2(rad * 0.3f, 0.003f) }, 20, true, true);
                Geo.Torus(node.M("chrome"), cup.Mul(new Frame { o = new Vector3(0, 0.012f, 0), x = Vector3.right, y = Vector3.up, z = Vector3.forward }), rad, 0.0035f, 24, 6);
                Geo.Lathe(node.M("lamp_glow"), cup, new[] { new Vector2(rad * 0.6f, 0.006f), new Vector2(rad * 0.45f, 0.013f), new Vector2(0f, 0.016f) }, 20);
            }
        }

        void FrontFascia(float side)
        {
            var bumper = N("BumperF");
            // Верхняя решётка между фарами
            Patch(bumper, "grille", 10, 5, (a, b) =>
            {
                float x = a * 0.31f;
                float shrink = Mathf.Max(0f, (x - 0.26f) / 0.05f);
                return (new Vector3(x, Mathf.Lerp(0.505f + 0.02f * shrink, 0.605f - 0.015f * shrink, b), 1.6f), Vector3.forward);
            }, 0.003f, side);
            // Большой нижний воздухозаборник
            Patch(bumper, "grille", 14, 8, (a, b) =>
            {
                float x = a * 0.43f;
                float shrink = Mathf.Max(0f, (x - 0.36f) / 0.07f);
                return (new Vector3(x, Mathf.Lerp(0.235f + 0.03f * shrink, 0.43f - 0.03f * shrink, b), 1.6f), Vector3.forward);
            }, 0.003f, side);
            // Интеркулер: серебристая «рамка-овал» и рёбра
            var ring = new List<(Vector3, Vector3)>();
            foreach (var (x, y) in new[] { (0f, 0.39f), (0.27f, 0.39f), (0.31f, 0.365f), (0.31f, 0.305f), (0.27f, 0.28f), (0f, 0.28f) })
                ring.Add((new Vector3(x, y, 1.6f), Vector3.forward));
            Ribbon(bumper, "int_grey", ring, 0.012f, 0.007f, side);
            for (int i = 0; i < 5; i++)
            {
                var fin = new List<(Vector3, Vector3)>();
                for (int k = 0; k <= 6; k++) fin.Add((new Vector3(k / 6f * 0.29f, 0.297f + i * 0.019f, 1.6f), Vector3.forward));
                Ribbon(bumper, "rim_inner", fin, 0.004f, 0.005f, side);
            }
            // Боковые воздухозаборники и поворотники
            Patch(bumper, "grille", 8, 4, (a, b) => (new Vector3(Mathf.Lerp(0.55f, 0.72f, a), Mathf.Lerp(0.22f, 0.30f, b), 1.6f), Vector3.forward), 0.003f, side);
            var blink = bumper.Child(side < 0 ? "BlinkFL" : "BlinkFR", new Vector3(0.63f * side, 0.39f, 2.2f));
            Patch(blink, "amber", 6, 3, (a, b) => (new Vector3(Mathf.Lerp(0.56f, 0.70f, a), Mathf.Lerp(0.365f, 0.415f, b), 1.6f), Vector3.forward), 0.004f, side);
            // Губа
            var lip = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 24; i++)
            {
                float ang = Mathf.Lerp(90f, 6f, i / 24f) * Mathf.Deg2Rad;
                lip.Add((new Vector3(0f, 0.145f, 1.7f), new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang))));
            }
            Ribbon(bumper, "black", lip, 0.03f, 0.004f, side);
            // Боковой габарит на крыле
            Patch(N("Shell"), "amber", 4, 2, (a, b) => (new Vector3(0.4f, Mathf.Lerp(0.6f, 0.63f, b), Mathf.Lerp(1.74f, 1.82f, a)), Vector3.right), 0.003f, side);
        }

        // ---------- Раскраска «Двойного форсажа» ----------

        void Livery(float side)
        {
            // Две полосы по капоту и бамперу (между ними — серебристая)
            Patch(N("Hood"), "stripe", 4, 40, (a, b) => (new Vector3(Mathf.Lerp(0.055f, 0.285f, a), 0.4f, Mathf.Lerp(Cowl + 0.03f, 2.2f, b)), Vector3.up), 0.0015f, side);
            Patch(N("BumperF"), "stripe", 4, 24, (a, b) => (new Vector3(Mathf.Lerp(0.055f, 0.285f, a), Mathf.Lerp(0.15f, 0.69f, b), 1.6f), Vector3.forward), 0.0015f, side);
            // Наклонные полосы внизу дверей (голубые) и большие на заднем крыле (синие)
            for (int i = 0; i < 8; i++)
            {
                float z0 = 1.05f - i * 0.2f;
                float h = Mathf.Lerp(0.09f, 0.3f, i / 7f);
                float w = 0.085f + i * 0.004f;
                StripeBar(z0, 0.19f, h, w, 0.16f, "stripe", side);
            }
            for (int i = 0; i < 3; i++)
            {
                float z0 = -0.68f - i * 0.3f;
                StripeBar(z0, 0.6f + i * 0.02f, 0.3f - i * 0.03f, 0.12f, 0.3f, "stripe_dark", side);
            }
        }

        /// <summary>Наклонная полоса на борту: от (z0, y0) вверх на h со сдвигом назад на slant, ширина w.</summary>
        void StripeBar(float z0, float y0, float h, float w, float slant, string mat, float side)
        {
            var node = N("Shell");
            Patch(node, mat, 3, 6, (a, b) =>
            {
                float y = y0 + b * h;
                float z = z0 - a * w - b * slant;
                // На арках полоса просто обрывается (луч уходит в арку): поднимаем её край
                foreach (var ar in arches)
                {
                    float dz = z - ar.z, dy = y - ar.y;
                    if (dz * dz + dy * dy < (ar.r + 0.02f) * (ar.r + 0.02f)) y = ar.y + Mathf.Sqrt(Mathf.Max(0f, (ar.r + 0.02f) * (ar.r + 0.02f) - dz * dz));
                }
                return (new Vector3(0.3f, y, z), Vector3.right);
            }, 0.0015f, side);
        }

        // ---------- Корма ----------

        /// <summary>По два круглых фонаря: снаружи большой красный, внутри поменьше — поворотник.</summary>
        void Taillights(float side)
        {
            var node = body.Child(side < 0 ? "TaillightL" : "TaillightR", new Vector3(0.62f * side, 0.8f, -2.25f));
            var f = node.WorldFrame();
            foreach (var (x, r, mat) in new[] { (0.71f, 0.072f, "tail_red"), (0.53f, 0.06f, "amber") })
            {
                OnBody(new Vector3(x, 0.80f, -1.6f), Vector3.back, side, out var p, out var n);
                var lampNode = mat == "amber" ? node.Child(side < 0 ? "BlinkRL" : "BlinkRR") : node;
                var lf = lampNode.WorldFrame();
                var cup = Cup(Frame.Look(lf.ToLocal(p), lf.DirToLocal(n), Vector3.up));
                var look = Frame.Look(Vector3.zero, n, Vector3.up);
                Geo.Torus(node.M("black"), new Frame { o = f.ToLocal(p + n * 0.006f), x = f.DirToLocal(look.x), y = f.DirToLocal(n), z = f.DirToLocal(look.y) }, r + 0.006f, 0.008f, 32, 6);
                Geo.Lathe(lampNode.M(mat), cup, new[]
                {
                    new Vector2(r, 0.004f), new Vector2(r * 0.95f, 0.012f), new Vector2(r * 0.7f, 0.018f), new Vector2(r * 0.35f, 0.021f), new Vector2(0f, 0.022f),
                }, 28);
                Geo.Torus(lampNode.M(mat == "amber" ? "amber" : "tail_red"), cup.Mul(new Frame { o = new Vector3(0, 0.023f, 0), x = Vector3.right, y = Vector3.up, z = Vector3.forward }), r * 0.45f, 0.004f, 20, 5);
            }
        }

        void CenterDetails()
        {
            var shell = N("Shell");
            var bumperF = N("BumperF");
            var bumperR = N("BumperR");
            Plate(bumperF, new Vector3(0f, 0.335f, 1.6f), Vector3.forward, "PlateFront");
            Plate(bumperR, new Vector3(0f, 0.53f, -1.6f), Vector3.back, "PlateRear");
            foreach (float side in new[] { 1f, -1f })
            {
                Patch(bumperR, "black", 6, 4, (a, b) => (new Vector3(a * 0.3f, Mathf.Lerp(0.46f, 0.60f, b), -1.6f), Vector3.back), 0.002f, side);
                Patch(bumperR, "black", 18, 3, (a, b) => (new Vector3(a * 0.82f, Mathf.Lerp(0.29f, 0.36f, b) + 0.02f * a * a, -1.6f), Vector3.back), 0.002f, side);
                Patch(bumperR, "tail_red", 4, 2, (a, b) => (new Vector3(Mathf.Lerp(0.62f, 0.76f, a), Mathf.Lerp(0.42f, 0.445f, b), -1.6f), Vector3.back), 0.003f, side);
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++) seam.Add((new Vector3(i / 16f * 0.84f, 0.895f, -1.6f), Vector3.back));
                Ribbon(N("Trunk"), "black", seam, 0.004f, 0.001f, side);
                // Шов крышки багажника сверху (вдоль заднего стекла)
                var top = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 12; i++) top.Add((new Vector3(i / 12f * 0.7f, 0.6f, Deck - 0.04f), Vector3.up));
                Ribbon(N("Trunk"), "black", top, 0.004f, 0.001f, side);
                foreach (var (x0, x1) in new[] { (0.62f, 0.02f) })
                {
                    var wiper = new List<(Vector3, Vector3)>();
                    for (int i = 0; i <= 10; i++)
                    {
                        float t = i / 10f;
                        wiper.Add((new Vector3(Mathf.Lerp(x0, x1, t), 0.8f, Mathf.Lerp(0.36f, 0.335f, t)), Vector3.up));
                    }
                    Ribbon(shell, "black", wiper, 0.016f, 0.008f, side);
                }
                Patch(shell, "black", 12, 2, (a, b) => (new Vector3(a * 0.78f, 0.6f, Mathf.Lerp(Cowl + 0.003f, Cowl + 0.05f, b)), Vector3.up), 0.002f, side);
            }
            // Красная эмблема на решётке и на корме
            OnBody(new Vector3(0f, 0.555f, 1.6f), Vector3.forward, 1f, out var gp, out var gn);
            var bf = bumperF.WorldFrame();
            Geo.RoundBox(bumperF.M("tail_red"), Frame.Look(bf.ToLocal(gp + gn * 0.008f), gn, Vector3.up), Vector3.zero, new Vector3(0.09f, 0.03f, 0.01f), 0.3f, 6);
            Geo.RoundBox(bumperF.M("chrome"), Frame.Look(bf.ToLocal(gp + gn * 0.006f), gn, Vector3.up), Vector3.zero, new Vector3(0.62f, 0.008f, 0.008f), 0.3f, 4);
            Emblem(N("Trunk"), new Vector3(0f, 0.80f, -1.6f), Vector3.back, 0.045f);
            // Выхлоп справа — толстая титановая труба
            OnBody(new Vector3(0.45f, 0.27f, -1.6f), Vector3.back, 1f, out var ep, out _);
            var ef = bumperR.WorldFrame();
            var pipe = new Frame { o = ef.ToLocal(new Vector3(ep.x, 0.27f, ep.z + 0.03f)), x = Vector3.right, y = Vector3.back, z = Vector3.up };
            Geo.Lathe(bumperR.M("chrome"), pipe, new[] { new Vector2(0.058f, -0.05f), new Vector2(0.058f, 0.11f), new Vector2(0.062f, 0.115f), new Vector2(0.054f, 0.12f) }, 24);
            Geo.Lathe(bumperR.M("int_black"), pipe, new[] { new Vector2(0.051f, 0.12f), new Vector2(0.051f, 0.0f), new Vector2(0f, 0.0f) }, 24, false, true);
            Geo.Box(shell.M("int_black"), Frame.Identity, new Vector3(0f, 0.42f, 1.3f), new Vector3(1.2f, 0.38f, 1.4f));
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
            Ribbon(door, "black", Line(DoorFront, 0.2f, DoorFront - 0.02f, 0.86f), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear, 0.2f, DoorRear + 0.01f, 0.93f), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear, 0.205f, DoorFront, 0.205f, 30), 0.005f, 0.001f, side);
            // Ручка-скоба
            Ribbon(door, "black", Line(DoorRear + 0.1f, 0.835f, DoorRear + 0.25f, 0.835f, 6), 0.03f, 0.002f, side);
            Ribbon(door, "chrome", Line(DoorRear + 0.1f, 0.85f, DoorRear + 0.25f, 0.85f, 6), 0.006f, 0.006f, side);
            // Шов капота
            var hood = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float z = Mathf.Lerp(Cowl + 0.02f, 2.2f, i / 40f);
                var st = Sec(z);
                hood.Add((new Vector3(st.fb * st.w - 0.03f, st.belt - 0.15f, z), Vector3.up));
            }
            Ribbon(N("Hood"), "black", hood, 0.005f, 0.001f, side);
            if (side > 0)
            {
                var hatch = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 28; i++)
                {
                    float a = i / 28f * Mathf.PI * 2f;
                    hatch.Add((new Vector3(0.3f, 0.84f + Mathf.Sin(a) * 0.06f, -1.62f + Mathf.Cos(a) * 0.07f), Vector3.right));
                }
                Ribbon(shell, "black", hatch, 0.004f, 0.001f, side);
            }
            Ribbon(shell, "black", Line(AxleR + 0.41f, 0.16f, AxleF - 0.39f, 0.16f, 30), 0.03f, 0.002f, side);
        }

        void Mirror(float side)
        {
            var door = N(side < 0 ? "DoorL" : "DoorR");
            const float mz = 0.29f;
            var st = Sec(mz);
            var basePt = new Vector3(st.fb * st.w * side, st.belt + 0.015f, mz);
            var df = door.WorldFrame();
            var center = basePt + new Vector3(0.13f * side, 0.075f, -0.035f);
            var mount = door.Child(side < 0 ? "MirrorL" : "MirrorR", df.ToLocal(center));
            Geo.RoundBox(mount.M("paint"), Frame.Identity, Vector3.zero, new Vector3(0.17f, 0.105f, 0.11f), 0.4f, 14);
            var from = df.ToLocal(basePt) - mount.pos;
            var to = new Vector3(-0.05f * side, -0.025f, 0.01f);
            Geo.RoundBox(mount.M("paint"), Frame.Look((from + to) * 0.5f, to - from, Vector3.up), Vector3.zero, new Vector3(0.06f, 0.03f, (to - from).magnitude + 0.03f), 0.4f, 6);
            Geo.RoundBox(door.M("black"), Frame.Identity, df.ToLocal(basePt), new Vector3(0.05f, 0.03f, 0.09f), 0.4f, 6);
            float yaw = side > 0 ? 27f : -48f;
            var glass = mount.Child(side < 0 ? "MirrorGlassL" : "MirrorGlassR", new Vector3(0f, 0.002f, -0.056f), new Vector3(0f, yaw * 0.25f, 0f));
            Geo.Quad(glass.M("mirror"), Frame.Identity, 0.15f, 0.085f);
        }

        /// <summary>Высокое синее крыло: профиль-лопасть с торцевыми пластинами на двух чёрных стойках.</summary>
        void Wing()
        {
            var trunk = N("Trunk");
            var f = trunk.WorldFrame();
            const float top = 1.33f, zc = -1.97f, half = 0.80f;
            var path = new List<Vector3>();
            for (int i = 0; i <= 24; i++) path.Add(f.ToLocal(new Vector3(Mathf.Lerp(-half, half, i / 24f), top, zc)));
            var foil = new List<Vector2>();
            const int nf = 16;
            for (int i = 0; i < nf * 2; i++)
            {
                float t = i < nf ? i / (float)nf : (2 * nf - i) / (float)nf;
                float xx = (1f - Mathf.Cos(t * Mathf.PI)) / 2f;
                float th = 0.35f * (0.2969f * Mathf.Sqrt(xx) - 0.126f * xx - 0.3516f * xx * xx + 0.2843f * xx * xx * xx - 0.1036f * xx * xx * xx * xx);
                float yy = (i < nf ? th : -th) * 0.27f;
                foil.Add(new Vector2((xx - 0.45f) * 0.27f, yy + xx * 0.03f));
            }
            // Хорда вдоль машины: начальная «правая» ось должна смотреть назад (−Z)
            Geo.Sweep(trunk.M("wing_blue"), Frame.Identity, path, foil, false, Vector3.up, true);
            foreach (float side in new[] { -1f, 1f })
            {
                // Торцевые пластины
                Geo.RoundBox(trunk.M("wing_blue"), Frame.Identity, f.ToLocal(new Vector3(half * side, top - 0.03f, zc - 0.02f)), new Vector3(0.012f, 0.17f, 0.32f), 0.25f, 6);
                // Стойки: от крышки багажника вверх и чуть назад
                var a = f.ToLocal(new Vector3(0.36f * side, 0.98f, -1.86f));
                var b = f.ToLocal(new Vector3(0.36f * side, top - 0.01f, zc + 0.03f));
                Geo.RoundBox(trunk.M("black"), Frame.Look((a + b) * 0.5f, b - a, Vector3.forward), Vector3.zero, new Vector3(0.012f, 0.1f, (b - a).magnitude), 0.3f, 6);
            }
        }

        // ---------- Салон (правый руль, синий руль MOMO) ----------

        void Interior()
        {
            var cab = body.Child("Interior");
            var id = Frame.Identity;
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.225f, -0.5f), new Vector3(1.56f, 0.03f, 1.85f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.47f, 0.5f), new Vector3(1.56f, 0.48f, 0.04f));
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.31f, -0.3f), new Vector3(0.28f, 0.2f, 1.4f), 0.25f, 8);
            Geo.RoundBox(cab.M("int_grey"), id, new Vector3(0f, 0.44f, -0.40f), new Vector3(0.25f, 0.08f, 0.75f), 0.2f, 8);
            Geo.Lathe(cab.M("leather_black"), new Frame { o = new Vector3(0.02f, 0.48f, -0.22f), x = Vector3.right, y = Vector3.up, z = Vector3.forward },
                new[] { new Vector2(0.06f, 0f), new Vector2(0.042f, 0.04f), new Vector2(0.018f, 0.075f) }, 16);
            Geo.Cylinder(cab.M("chrome"), Frame.Euler(new Vector3(0.02f, 0.48f, -0.22f), new Vector3(-12f, 0f, 0f)), 0.008f, 0.05f, 0.19f, 8);
            Geo.RoundBox(cab.M("chrome"), id, new Vector3(0.02f, 0.675f, -0.18f), new Vector3(0.045f, 0.06f, 0.045f), 0.8f, 10);
            Geo.RoundBox(cab.M("int_black"), Frame.Euler(new Vector3(-0.08f, 0.5f, -0.6f), new Vector3(-14f, 0f, 0f)), Vector3.zero, new Vector3(0.04f, 0.035f, 0.24f), 0.5f, 6);

            // Торпеда: вся чёрная, щиток «капюшоном» у водителя, центральная консоль с экраном MFD наверху
            var prof = new[]
            {
                new Vector2(0.39f, 0.885f), new Vector2(0.24f, 0.925f), new Vector2(0.08f, 0.94f), new Vector2(-0.01f, 0.935f),
                new Vector2(-0.05f, 0.91f), new Vector2(-0.055f, 0.86f), new Vector2(-0.03f, 0.72f), new Vector2(0.02f, 0.58f), new Vector2(0.18f, 0.5f),
            };
            Vector2 Prof(float t)
            {
                float fi = t * (prof.Length - 1);
                int i = Mathf.Min(prof.Length - 2, (int)fi);
                float k = fi - i;
                var p0 = prof[Mathf.Max(0, i - 1)]; var p1 = prof[i]; var p2 = prof[i + 1]; var p3 = prof[Mathf.Min(prof.Length - 1, i + 2)];
                return 0.5f * (2f * p1 + (-p0 + p2) * k + (2f * p0 - 5f * p1 + 4f * p2 - p3) * k * k + (-p0 + 3f * p1 - 3f * p2 + p3) * k * k * k);
            }
            Vector3 Dash(float a, float b)
            {
                float x = Mathf.Lerp(-0.78f, 0.78f, a);
                var q = Prof(b);
                return new Vector3(x, q.y, q.x);
            }
            Geo.Surface(cab.M("int_black"), id, 40, 12, Dash);
            foreach (float x in new[] { -0.70f, 0.72f })
                Vent(cab, new Vector3(x, 0.84f, -0.06f), 0.12f, 0.08f);
            // Центральная консоль с экраном
            var stack = cab.Child("CenterStack", new Vector3(0.02f, 0.72f, -0.04f), new Vector3(6f, -8f, 0f));
            Geo.RoundBox(stack.M("int_grey"), id, Vector3.zero, new Vector3(0.3f, 0.34f, 0.08f), 0.25f, 8);
            foreach (float x in new[] { -0.07f, 0.07f }) Vent(stack, new Vector3(x, 0.11f, -0.045f), 0.12f, 0.06f);
            Geo.RoundBox(stack.M("int_black"), id, new Vector3(0f, -0.02f, -0.042f), new Vector3(0.24f, 0.06f, 0.02f), 0.2f, 6);
            var screen = stack.Child("RadioScreen", new Vector3(0f, -0.02f, -0.054f));
            Geo.Quad(screen.M("screen"), id, 0.18f, 0.04f);
            for (int i = 0; i < 3; i++)
                Geo.Cylinder(stack.M("chrome"), Frame.Euler(new Vector3(-0.08f + i * 0.08f, -0.11f, -0.045f), new Vector3(-90f, 0f, 0f)), 0.016f, 0f, 0.02f, 14);
            // Экран «MFD» наверху торпеды — фирменная деталь R34
            var mfd = cab.Child("Mfd", new Vector3(0.02f, 0.975f, 0.02f), new Vector3(-8f, -8f, 0f));
            Geo.RoundBox(mfd.M("int_black"), id, Vector3.zero, new Vector3(0.2f, 0.12f, 0.08f), 0.25f, 8);
            Geo.Quad(mfd.Child("MfdScreen", new Vector3(0f, 0f, -0.041f)).M("screen_amber"), id, 0.16f, 0.09f);

            Cluster(cab);
            SteeringWheel(cab);
            foreach (float side in new[] { -1f, 1f }) Seat(cab, side);
            foreach (float x in new[] { -0.34f, 0.34f })
            {
                Geo.RoundBox(cab.M("cloth_dark"), id, new Vector3(x, 0.4f, -1.3f), new Vector3(0.5f, 0.11f, 0.4f), 0.35f, 8);
                Geo.RoundBox(cab.M("cloth_dark"), Frame.Euler(new Vector3(x, 0.42f, -1.5f), new Vector3(-24f, 0f, 0f)), new Vector3(0f, 0.22f, 0f), new Vector3(0.5f, 0.46f, 0.1f), 0.35f, 8);
            }
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.6f, -1.62f), new Vector3(1.3f, 0.02f, 0.25f));
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.6f, -1.98f), new Vector3(1.2f, 0.03f, 0.5f));
            foreach (float side in new[] { -1f, 1f })
            {
                var door = N(side < 0 ? "DoorL" : "DoorR");
                var df = door.WorldFrame();
                Geo.RoundBox(door.M("leather_black"), id, df.ToLocal(new Vector3(0.75f * side, 0.6f, -0.45f)), new Vector3(0.08f, 0.05f, 0.6f), 0.4f, 8);
                Geo.RoundBox(door.M("int_grey"), id, df.ToLocal(new Vector3(0.77f * side, 0.76f, -0.1f)), new Vector3(0.04f, 0.04f, 0.16f), 0.4f, 6);
            }
            Geo.Cylinder(cab.M("int_black"), Frame.Euler(new Vector3(0f, 1.285f, -0.27f), Vector3.zero), 0.012f, -0.03f, 0.015f, 8);
            var rm = cab.Child("RearMirror", new Vector3(0f, 1.235f, -0.275f), new Vector3(0f, -21f, 0f));
            Geo.RoundBox(rm.M("int_black"), id, new Vector3(0f, 0f, 0.016f), new Vector3(0.27f, 0.075f, 0.035f), 0.5f, 10);
            Geo.Quad(rm.Child("RearMirrorGlass", new Vector3(0f, 0f, -0.0025f)).M("mirror"), id, 0.25f, 0.06f);
            foreach (float x in new[] { -0.38f, 0.38f })
                Geo.RoundBox(cab.M("int_roof"), Frame.Euler(new Vector3(x, 1.29f, -0.36f), new Vector3(-10f, 0f, 0f)), Vector3.zero, new Vector3(0.40f, 0.022f, 0.16f), 0.4f, 6);
            Driver(0.83f, -0.9f, -14f);
        }

        void Vent(ModelNode n, Vector3 c, float w, float h)
        {
            Geo.RoundBox(n.M("int_grey"), Frame.Identity, c, new Vector3(w, h, 0.02f), 0.25f, 6);
            Geo.RoundBox(n.M("grille"), Frame.Identity, c + new Vector3(0f, 0f, -0.006f), new Vector3(w * 0.85f, h * 0.75f, 0.012f), 0.2f, 4);
            for (int i = 0; i < 4; i++)
                Geo.Box(n.M("int_black"), Frame.Identity, c + new Vector3(0f, (i - 1.5f) * h * 0.18f, -0.014f), new Vector3(w * 0.8f, 0.004f, 0.006f));
        }

        /// <summary>Щиток R34: тахометр по центру, спидометр справа, мелкие приборы слева; стрелки — как в игре.</summary>
        void Cluster(ModelNode cab)
        {
            var c = new Vector3(0.37f, 0.905f, -0.09f);
            Vector3 Hood(float a, float b)
            {
                float th = Mathf.Lerp(-0.2f, Mathf.PI + 0.2f, a);
                float r = 0.24f;
                return c + new Vector3(Mathf.Cos(th) * r, Mathf.Sin(th) * r * 0.45f + 0.035f, Mathf.Lerp(0.06f, -0.05f, b) - 0.02f * Mathf.Sin(th));
            }
            Geo.Surface(cab.M("int_black"), Frame.Identity, 30, 4, Hood, true);
            Geo.Surface(cab.M("int_black"), Frame.Identity, 30, 4, Hood, false);
            Geo.RoundBox(cab.M("gauge_face"), Frame.Identity, c + new Vector3(0f, -0.005f, 0.05f), new Vector3(0.46f, 0.17f, 0.02f), 0.3f, 6);
            Gauge(cab, "Fuel", c + new Vector3(-0.16f, -0.015f, 0.03f), 0.045f, true, "gauge_light", "int_black");
            Gauge(cab, "Tach", c + new Vector3(0f, -0.005f, 0.025f), 0.07f, true, "gauge_light", "int_black");
            Gauge(cab, "Speed", c + new Vector3(0.155f, -0.01f, 0.03f), 0.06f, true, "gauge_light", "int_black");
            var fl = cab.Child("FuelLamp", c + new Vector3(-0.16f, -0.05f, 0.016f));
            Geo.RoundBox(fl.M("lamp_off"), Frame.Identity, Vector3.zero, new Vector3(0.022f, 0.012f, 0.004f), 0.3f, 4);
            var el = cab.Child("EngineLamp", c + new Vector3(0.155f, -0.06f, 0.016f));
            Geo.RoundBox(el.M("lamp_off"), Frame.Identity, Vector3.zero, new Vector3(0.022f, 0.012f, 0.004f), 0.3f, 4);
        }

        /// <summary>Спортивный руль: синие кожаные верх и спицы, чёрный низ, красная кнопка в центре.</summary>
        void SteeringWheel(ModelNode cab)
        {
            var w = body.Child("SteeringWheel", SteeringPos, new Vector3(SteeringTilt, 0f, 0f));
            var id = Frame.Identity;
            const float R = 0.175f;
            Geo.Torus(w.M("leather_blue"), id, R, 0.018f, 40, 10, 20f, 160f);
            Geo.Torus(w.M("leather_black"), id, R, 0.018f, 50, 10, 160f, 380f);
            foreach (var dir in new[] { new Vector3(1f, 0f, -0.25f).normalized, new Vector3(-1f, 0f, -0.25f).normalized, Vector3.back })
            {
                var f = Frame.Look(dir * R * 0.5f, dir, Vector3.up);
                Geo.RoundBox(w.M("leather_blue"), f, Vector3.zero, new Vector3(0.07f, 0.02f, R * 0.95f), 0.45f, 6);
            }
            Geo.Lathe(w.M("leather_blue"), new Frame { o = Vector3.zero, x = Vector3.right, y = Vector3.up, z = Vector3.forward },
                new[] { new Vector2(0.07f, 0f), new Vector2(0.065f, 0.02f), new Vector2(0f, 0.025f) }, 24);
            Geo.Lathe(w.M("tail_red"), new Frame { o = new Vector3(0f, 0.025f, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward },
                new[] { new Vector2(0.014f, 0f), new Vector2(0.012f, 0.006f), new Vector2(0f, 0.008f) }, 16);
            Geo.Cylinder(w.M("int_black"), id, 0.035f, -0.3f, -0.01f, 12);
        }

        /// <summary>Спортивное сиденье в чёрной ткани с «точками».</summary>
        void Seat(ModelNode cab, float side)
        {
            float x = 0.37f * side;
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(x, 0.31f, -0.78f), new Vector3(0.5f, 0.1f, 0.52f), 0.3f, 8);
            Geo.RoundBox(cab.M("cloth_dark"), id, new Vector3(x, 0.38f, -0.78f), new Vector3(0.34f, 0.08f, 0.5f), 0.4f, 8);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(cab.M("leather_black"), id, new Vector3(x + b * 0.2f, 0.395f, -0.8f), new Vector3(0.1f, 0.12f, 0.48f), 0.5f, 8);
            var back = cab.Child(side > 0 ? "SeatBackR" : "SeatBackL", new Vector3(x, 0.37f, -1.02f), new Vector3(-13f, 0f, 0f));
            Geo.RoundBox(back.M("leather_black"), id, new Vector3(0f, 0.33f, -0.02f), new Vector3(0.48f, 0.66f, 0.12f), 0.35f, 8);
            Geo.RoundBox(back.M("cloth_dark"), id, new Vector3(0f, 0.3f, 0.035f), new Vector3(0.3f, 0.5f, 0.06f), 0.4f, 8);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(back.M("leather_black"), id, new Vector3(b * 0.2f, 0.27f, 0.05f), new Vector3(0.09f, 0.48f, 0.14f), 0.5f, 8);
            Geo.RoundBox(back.M("leather_black"), id, new Vector3(0f, 0.72f, -0.01f), new Vector3(0.27f, 0.18f, 0.11f), 0.55f, 8);
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
                var mesh = wheel.Child("Rim" + tag, Vector3.zero, side < 0 ? Vector3.zero : new Vector3(180f, 0f, 0f));
                WheelModel.FiveSpoke(mesh, WheelR, 0.255f, 6, 0.229f, "chrome", "chrome", 1.05f);
                WheelModel.Caliper(mount, side, 1.1f);
            }
        }
    }
}

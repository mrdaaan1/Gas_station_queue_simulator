using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «Мазда RX-7» FD (1993) Доминика из первого «Форсажа»: красная, широкий обвес, серебристые «когти»
    /// на капоте и боках, серебристая полоса по носу, боковой воздухозаборник с зелёным акцентом,
    /// чёрное крыло на стойках с красными торцами, многоспицевые хромированные диски 19".
    /// Руль слева (американская машина). Салон: чёрная торпеда, красная оплётка руля, бежевая кожа,
    /// карбоновые подиумы с приборами на торпеде и коробочка с кнопкой закиси азота.
    /// Размеры: 4,30 × 1,83 (с обвесом) × 1,23 м, база 2,425 м.
    /// </summary>
    public class Rx7Model : SportsCarModel
    {
        public const float AxleF = 1.22f, AxleR = -1.205f;
        public const float WheelR = 0.32f, Track = 0.755f;
        public const float DoorFront = 0.66f, DoorRear = -0.86f;
        const float SideRearPoint = -1.36f;

        public static readonly Vector3 DriverEyes = new Vector3(-0.37f, 1.02f, -0.80f);
        public static readonly Vector3 SteeringPos = new Vector3(-0.37f, 0.80f, -0.26f);
        public const float SteeringTilt = -60f;

        static Model cached;

        public static Model Get()
        {
            if (cached == null) cached = new Rx7Model().Build();
            return cached;
        }

        public static Rx7Model Shape() => new Rx7Model();

        protected override Vector3 Eyes => DriverEyes;

        Rx7Model()
        {
            Front = 2.15f; Rear = -2.15f;
            Cowl = 0.45f; Header = -0.30f; RoofRear = -0.95f; Deck = -1.62f;
            FrontRound = 1.62f; RearRound = -1.75f; FrontPow = 2.4f; RearPow = 3.0f;

            W0 = new Curve().Key(-2.15f, 0.88f).Key(-1.7f, 0.905f).Key(-1.2f, 0.915f).Key(-0.7f, 0.89f).Key(0f, 0.875f)
                .Key(0.6f, 0.88f).Key(1.22f, 0.905f).Key(1.7f, 0.895f).Key(2.15f, 0.86f);
            YBot = new Curve().Key(-2.15f, 0.36f).Key(-2.05f, 0.25f).Key(-1.9f, 0.19f).Key(-1.6f, 0.15f).Key(0f, 0.12f)
                .Key(1.7f, 0.12f).Key(2.0f, 0.10f).Key(2.1f, 0.12f).Key(2.15f, 0.20f);
            YMax = new Curve().Key(-2.15f, 0.54f).Key(-1.6f, 0.52f).Key(0f, 0.46f).Key(1.6f, 0.42f).Key(2.15f, 0.38f);
            YBelt = new Curve().Key(-2.15f, 0.82f).Key(-2.05f, 0.87f).Key(-1.85f, 0.905f).Key(-1.55f, 0.93f).Key(-1.2f, 0.94f)
                .Key(-0.8f, 0.915f).Key(-0.3f, 0.895f).Key(0.1f, 0.87f).Key(Cowl, 0.83f).Key(0.8f, 0.765f).Key(1.22f, 0.74f)
                .Key(1.55f, 0.70f).Key(1.85f, 0.645f).Key(2.02f, 0.59f).Key(2.1f, 0.54f).Key(2.15f, 0.46f);
            FBelt = new Curve().Key(-2.15f, 0.8f).Key(-1.8f, 0.86f).Key(-1.3f, 0.9f).Key(-0.5f, 0.93f).Key(0.3f, 0.93f)
                .Key(0.6f, 0.88f).Key(1.0f, 0.84f).Key(2.15f, 0.8f);
            YTop = new Curve().Key(-2.15f, 0.83f).Key(-2.08f, 0.885f).Key(-1.95f, 0.925f).Key(-1.8f, 0.945f).Key(Deck, 0.952f, true)
                .Key(-1.28f, 1.10f).Key(RoofRear, 1.21f, true).Key(-0.62f, 1.232f).Key(Header, 1.205f, true).Key(0.08f, 1.03f)
                .Key(Cowl, 0.845f, true).Key(0.8f, 0.785f).Key(1.22f, 0.758f).Key(1.55f, 0.717f).Key(1.85f, 0.663f)
                .Key(2.02f, 0.607f).Key(2.1f, 0.557f).Key(2.15f, 0.47f);
            YRoofEdge = new Curve().Key(Deck, 0.93f, true).Key(-1.28f, 1.06f).Key(RoofRear, 1.165f, true).Key(-0.62f, 1.185f)
                .Key(Header, 1.16f, true).Key(0.08f, 0.99f).Key(Cowl, 0.83f, true);
            XRoofEdge = new Curve().Key(Deck, 0.79f, true).Key(-1.28f, 0.70f).Key(RoofRear, 0.63f, true).Key(-0.62f, 0.64f)
                .Key(Header, 0.63f, true).Key(Cowl, 0.81f, true);
            AngC = new Curve().Key(-2.15f, 160f).Key(-1.8f, 150f).Key(-1.55f, 120f).Key(-1.25f, 100f).Key(0.15f, 100f)
                .Key(0.5f, 135f).Key(0.8f, 155f).Key(2.15f, 160f);
            AngE0 = new Curve().Key(-2.15f, 172f).Key(-1.65f, 170f).Key(-1.45f, 158f).Key(-0.95f, 150f).Key(-0.3f, 150f)
                .Key(0.15f, 158f).Key(Cowl, 170f).Key(2.15f, 172f);
            AngD0 = new Curve().Key(-1.65f, 140f).Key(-1.4f, 118f).Key(-1.0f, 112f).Key(0.25f, 112f).Key(Cowl, 120f);
            AngD1 = new Curve().Key(-1.65f, 150f).Key(-1.4f, 135f).Key(-1.0f, 122f).Key(0.25f, 122f).Key(Cowl, 130f);

            arches.Add(new Arch { z = AxleF, y = 0.32f, r = 0.37f, xMin = 0.56f });
            arches.Add(new Arch { z = AxleR, y = 0.32f, r = 0.38f, xMin = 0.56f });
            Prepare();
        }

        // ---------- Клетки кузова ----------

        float SideWindowTop(float z)
        {
            if (z > -0.92f) return 0.86f;
            float k = Mathf.Clamp01((-0.92f - z) / (-0.92f - SideRearPoint));
            return Mathf.Lerp(0.86f, 0.08f, k * k * (3f - 2f * k) * 0.6f + k * 0.4f);
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
                    // Выпуклое заднее стекло
                    float k = (RoofRear - z) / (RoofRear - Deck);
                    float vmin = Mathf.Lerp(0.12f, 0.33f, k * k);
                    float edge = Mathf.Min(RoofRear - z, z - Deck);
                    if (v > vmin && edge > 0.04f) mat = "glass";
                    else if (v > vmin - 0.05f && edge > 0.012f) mat = "black";
                }
                if (z >= Cowl + 0.015f && z < 1.98f) node = "Hood";
                else if (z <= Header && z >= RoofRear) node = "Roof";
                else if (z < Deck - 0.01f && z > -2.06f) node = "Trunk";
            }
            else if (seg == SegD && cabin)
            {
                float top = SideWindowTop(z);
                bool inZ = z < Cowl - 0.19f && z > SideRearPoint;
                if (z > Cowl - 0.20f && z < Cowl - 0.01f && v < 0.92f) mat = "black";
                else if (inZ && v > 0.10f && v < top) mat = Mathf.Abs(z - (DoorRear - 0.03f)) < 0.018f ? "black" : "glass";
                else if (z < Cowl - 0.01f && z > SideRearPoint - 0.03f && v > 0.05f && v < top + 0.07f) mat = "black";
            }
            bool lower = seg == SegB || seg == SegC || seg == SegD;
            if (lower && z < DoorFront && z > DoorRear && p.y > 0.16f) node = side > 0 ? "DoorR" : "DoorL";
            if (seg <= SegC && z > 1.95f) node = "BumperF";
            if (seg <= SegC && z < -1.95f && p.y < 0.70f) node = "BumperR";
            if (mat != "glass" && z < Cowl && z > Deck - 0.3f && !(seg == SegD && z < Deck + 0.04f))
                inner = seg == SegA ? "carpet" : seg == SegE || seg == SegD ? "int_roof" : "int_door";
            return new CellInfo { node = node, mat = mat, innerMat = inner };
        }

        protected override Vector3 Pivot(string name)
        {
            switch (name)
            {
                case "Hood": return new Vector3(0f, 0.845f, Cowl + 0.02f);
                case "Roof": return new Vector3(0f, 1.22f, -0.62f);
                case "Trunk": return new Vector3(0f, 0.95f, Deck);
                case "DoorL": return new Vector3(-0.87f, 0.55f, -0.1f);
                case "DoorR": return new Vector3(0.87f, 0.55f, -0.1f);
                case "BumperF": return new Vector3(0f, 0.3f, 2.05f);
                case "BumperR": return new Vector3(0f, 0.42f, -2.05f);
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

        /// <summary>Вытянутая фара под прозрачным колпаком на углу крыла (вместо «лупоглазых» подъёмных).</summary>
        void Headlight(float side)
        {
            var node = body.Child(side < 0 ? "HeadlightL" : "HeadlightR", new Vector3(0.6f * side, 0.58f, 1.85f));
            (Vector3, Vector3) Ray(float a, float b)
            {
                float ang = Mathf.Lerp(8f, 64f, a) * Mathf.Deg2Rad;
                float yc = Mathf.Lerp(0.612f, 0.528f, a);
                float hh = Mathf.Lerp(0.046f, 0.027f, a) * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(2f * a - 1f), 5f)));
                return (new Vector3(0.33f, yc + (b - 0.5f) * 2f * hh, 1.58f), new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)));
            }
            Patch(node, "housing", 22, 6, Ray, 0.003f, side);
            Patch(node, "lens", 22, 6, Ray, 0.02f, side);
            var rim = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                rim.Add(t < 0.5f ? Ray(t * 2f, 1f) : Ray(1f - (t - 0.5f) * 2f, 0f));
            }
            Ribbon(node, "black", rim, 0.01f, 0.004f, side);
            var f = node.WorldFrame();
            foreach (var (a, rad) in new[] { (0.3f, 0.032f), (0.62f, 0.026f) })
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
            // Большой «рот» обвеса
            Patch(bumper, "grille", 14, 8, (a, b) =>
            {
                float x = a * 0.46f;
                float round = Mathf.Max(0f, (x - 0.36f) / 0.10f);
                float lo = 0.16f + 0.04f * round * round, hi = 0.355f - 0.05f * round * round;
                return (new Vector3(x, Mathf.Lerp(lo, hi, b), 1.5f), Vector3.forward);
            }, 0.003f, side);
            foreach (float y in new[] { 0.22f, 0.29f })
            {
                var bar = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 12; i++) bar.Add((new Vector3(i / 12f * 0.42f, y, 1.5f), Vector3.forward));
                Ribbon(bumper, "int_black", bar, 0.008f, 0.007f, side);
            }
            // Противотуманки
            Patch(bumper, "lamp_glow", 6, 3, (a, b) => (new Vector3(Mathf.Lerp(0.56f, 0.70f, a), Mathf.Lerp(0.235f, 0.29f, b), 1.5f), Vector3.forward), 0.004f, side);
            var blink = bumper.Child(side < 0 ? "BlinkFL" : "BlinkFR", new Vector3(0.62f * side, 0.33f, 2.1f));
            Patch(blink, "amber", 6, 2, (a, b) => (new Vector3(Mathf.Lerp(0.57f, 0.69f, a), Mathf.Lerp(0.315f, 0.34f, b), 1.5f), Vector3.forward), 0.004f, side);
            var lip = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 24; i++)
            {
                float ang = Mathf.Lerp(90f, 6f, i / 24f) * Mathf.Deg2Rad;
                lip.Add((new Vector3(0f, 0.13f, 1.5f), new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang))));
            }
            Ribbon(bumper, "black", lip, 0.03f, 0.004f, side);
        }

        // ---------- Раскраска Доминика ----------

        /// <summary>Серебристый «коготь»: сужающаяся полоса по лучам. ray(t, s): t — вдоль (0 у широкого конца), s — поперёк −1…1.</summary>
        void Claw(ModelNode node, string mat, System.Func<float, float, (Vector3, Vector3)> ray, float side)
        {
            Patch(node, mat, 12, 2, (a, b) => ray(a, (b * 2f - 1f) * (1f - a * 0.92f)), 0.0016f, side);
        }

        void Livery(float side)
        {
            var hood = N("Hood");
            // Широкая серебристая полоса по носу (на капоте и над «ртом»)
            Patch(hood, "stripe_silver", 3, 16, (a, b) => (new Vector3(a * 0.2f, 0.3f, Mathf.Lerp(1.45f, 1.97f, b)), Vector3.up), 0.0016f, side);
            Patch(N("BumperF"), "stripe_silver", 3, 6, (a, b) => (new Vector3(a * 0.2f, Mathf.Lerp(0.37f, 0.47f, b), 1.5f), Vector3.forward), 0.0016f, side);
            // «Когти» по капоту: от носа назад, расходясь веером
            for (int i = 0; i < 7; i++)
            {
                float x0 = 0.2f + i * 0.075f;
                float z0 = 1.92f - i * 0.04f;
                float len = 0.55f + 0.12f * (i % 3);
                float w = 0.045f - i * 0.002f;
                float drift = 0.04f + i * 0.02f;
                Claw(hood, "stripe_silver", (t, s) => (new Vector3(x0 + drift * t + s * w, 0.3f, z0 - len * t), Vector3.up), side);
            }
            // «Когти» по бокам: от переднего крыла назад, слегка вверх
            var shell = N("Shell");
            string door = side < 0 ? "DoorL" : "DoorR";
            for (int i = 0; i < 9; i++)
            {
                float y0 = 0.40f + i * 0.04f;
                float z0 = 1.80f - (i % 3) * 0.06f;
                float len = 1.1f + 0.35f * ((i * 7) % 3) / 2f + (i % 2) * 0.4f;
                float w = 0.05f - (i % 3) * 0.008f;
                Claw(shell, "stripe_silver", (t, s) =>
                {
                    float z = z0 - len * t, y = y0 + 0.08f * t + s * w;
                    foreach (var ar in arches)
                    {
                        float dz = z - ar.z, dy = y - ar.y, rr = ar.r + 0.03f;
                        if (dz * dz + dy * dy < rr * rr) y = ar.y + Mathf.Sqrt(Mathf.Max(0f, rr * rr - dz * dz));
                    }
                    return (new Vector3(0.3f, y, z), Vector3.right);
                }, side);
            }
            // Боковой воздухозаборник на двери с зелёным акцентом
            var dn = N(door);
            Patch(dn, "grille", 6, 5, (a, b) => (new Vector3(0.3f, Mathf.Lerp(0.38f, 0.58f, b), Mathf.Lerp(-0.62f, -0.30f, a) - 0.05f * b), Vector3.right), 0.003f, side);
            var frame = new List<(Vector3, Vector3)>();
            foreach (var (z, y) in new[] { (-0.62f, 0.38f), (-0.30f, 0.38f), (-0.35f, 0.58f), (-0.67f, 0.58f), (-0.62f, 0.38f) })
                frame.Add((new Vector3(0.3f, y, z), Vector3.right));
            Ribbon(dn, "chrome", frame, 0.012f, 0.004f, side);
            // Зелёные «языки пламени» за воздухозаборником
            for (int i = 0; i < 3; i++)
            {
                float y0 = 0.44f + i * 0.045f, z0 = -0.30f - i * 0.03f;
                Claw(shell, "stripe_green", (t, s2) => (new Vector3(0.3f, y0 + 0.03f * t + s2 * 0.022f, z0 - (0.38f + i * 0.08f) * t), Vector3.right), side);
            }
            // Жёлтая тонкая полоса вдоль борта
            var pin = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 30; i++) pin.Add((new Vector3(0.3f, 0.62f + 0.03f * i / 30f, Mathf.Lerp(0.9f, -0.2f, i / 30f)), Vector3.right));
            Ribbon(shell, "stripe_yellow", pin, 0.008f, 0.0016f, side);
        }

        // ---------- Корма ----------

        void Taillights(float side)
        {
            var node = body.Child(side < 0 ? "TaillightL" : "TaillightR", new Vector3(0.55f * side, 0.77f, -2.1f));
            Patch(node, "housing", 24, 5, (a, b) => (new Vector3(a * 0.80f, Mathf.Lerp(0.72f, 0.825f, b) - 0.02f * a * a, -1.5f), Vector3.back), 0.003f, side);
            var f = node.WorldFrame();
            foreach (var (x, r, mat) in new[] { (0.66f, 0.052f, "tail_red"), (0.49f, 0.05f, "tail_red"), (0.32f, 0.04f, "amber") })
            {
                OnBody(new Vector3(x, 0.772f - 0.02f * (x / 0.8f) * (x / 0.8f), -1.5f), Vector3.back, side, out var p, out var n);
                var lampNode = mat == "amber" ? node.Child(side < 0 ? "BlinkRL" : "BlinkRR") : node;
                var lf = lampNode.WorldFrame();
                var cup = Cup(Frame.Look(lf.ToLocal(p + n * 0.003f), lf.DirToLocal(n), Vector3.up));
                Geo.Lathe(lampNode.M(mat), cup, new[] { new Vector2(r, 0.002f), new Vector2(r * 0.9f, 0.01f), new Vector2(r * 0.5f, 0.015f), new Vector2(0f, 0.017f) }, 28);
                var look = Frame.Look(Vector3.zero, n, Vector3.up);
                Geo.Torus(node.M("chrome"), new Frame { o = f.ToLocal(p + n * 0.006f), x = f.DirToLocal(look.x), y = f.DirToLocal(n), z = f.DirToLocal(look.y) }, r, 0.0035f, 28, 6);
            }
        }

        void CenterDetails()
        {
            var shell = N("Shell");
            var bumperF = N("BumperF");
            var bumperR = N("BumperR");
            Plate(bumperF, new Vector3(0f, 0.255f, 1.5f), Vector3.forward, "PlateFront");
            Plate(bumperR, new Vector3(0f, 0.5f, -1.5f), Vector3.back, "PlateRear");
            foreach (float side in new[] { 1f, -1f })
            {
                Patch(bumperR, "black", 6, 4, (a, b) => (new Vector3(a * 0.3f, Mathf.Lerp(0.43f, 0.57f, b), -1.5f), Vector3.back), 0.002f, side);
                Patch(bumperR, "black", 18, 3, (a, b) => (new Vector3(a * 0.80f, Mathf.Lerp(0.25f, 0.33f, b), -1.5f), Vector3.back), 0.002f, side);
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 16; i++) seam.Add((new Vector3(i / 16f * 0.8f, 0.85f, -1.5f), Vector3.back));
                Ribbon(N("Trunk"), "black", seam, 0.004f, 0.001f, side);
                var wiper = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 10; i++) wiper.Add((new Vector3(Mathf.Lerp(0.62f, 0.02f, i / 10f), 0.7f, Mathf.Lerp(0.38f, 0.355f, i / 10f)), Vector3.up));
                Ribbon(shell, "black", wiper, 0.016f, 0.008f, side);
                Patch(shell, "black", 12, 2, (a, b) => (new Vector3(a * 0.78f, 0.55f, Mathf.Lerp(Cowl + 0.003f, Cowl + 0.05f, b)), Vector3.up), 0.002f, side);
                // Двойной выхлоп посередине
                OnBody(new Vector3(0.12f, 0.24f, -1.5f), Vector3.back, side, out var ep, out _);
                var ef = bumperR.WorldFrame();
                var pipe = new Frame { o = ef.ToLocal(new Vector3(ep.x, 0.24f, ep.z + 0.03f)), x = Vector3.right, y = Vector3.back, z = Vector3.up };
                Geo.Lathe(bumperR.M("chrome"), pipe, new[] { new Vector2(0.048f, -0.05f), new Vector2(0.048f, 0.09f), new Vector2(0.052f, 0.095f), new Vector2(0.045f, 0.1f) }, 24);
                Geo.Lathe(bumperR.M("int_black"), pipe, new[] { new Vector2(0.042f, 0.1f), new Vector2(0.042f, 0.0f), new Vector2(0f, 0.0f) }, 24, false, true);
            }
            Emblem(N("Hood"), new Vector3(0f, 0.4f, 1.9f), new Vector3(0f, 0.3f, 1f), 0.04f);
            Emblem(N("Trunk"), new Vector3(0f, 0.79f, -1.5f), Vector3.back, 0.04f);
            Geo.Box(shell.M("int_black"), Frame.Identity, new Vector3(0f, 0.38f, 1.1f), new Vector3(1.1f, 0.34f, 1.1f));
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
            Ribbon(door, "black", Line(DoorFront, 0.18f, DoorFront - 0.03f, 0.82f), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear, 0.18f, DoorRear + 0.02f, 0.9f), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear, 0.185f, DoorFront, 0.185f, 30), 0.005f, 0.001f, side);
            Ribbon(door, "black", Line(DoorRear + 0.08f, 0.82f, DoorRear + 0.2f, 0.82f, 6), 0.022f, 0.002f, side);
            var hood = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float z = Mathf.Lerp(Cowl + 0.02f, 1.98f, i / 40f);
                var st = Sec(z);
                hood.Add((new Vector3(st.fb * st.w - 0.03f, st.belt - 0.15f, z), Vector3.up));
            }
            Ribbon(N("Hood"), "black", hood, 0.005f, 0.001f, side);
            // Пороги обвеса
            Ribbon(shell, "black", Line(AxleR + 0.40f, 0.14f, AxleF - 0.38f, 0.14f, 30), 0.03f, 0.002f, side);
            if (side > 0)
            {
                var hatch = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 28; i++)
                {
                    float a = i / 28f * Mathf.PI * 2f;
                    hatch.Add((new Vector3(0.3f, 0.8f + Mathf.Sin(a) * 0.055f, -1.55f + Mathf.Cos(a) * 0.065f), Vector3.right));
                }
                Ribbon(shell, "black", hatch, 0.004f, 0.001f, side);
            }
        }

        void Mirror(float side)
        {
            var door = N(side < 0 ? "DoorL" : "DoorR");
            const float mz = 0.32f;
            var st = Sec(mz);
            var basePt = new Vector3(st.fb * st.w * side, st.belt + 0.015f, mz);
            var df = door.WorldFrame();
            var center = basePt + new Vector3(0.13f * side, 0.07f, -0.035f);
            var mount = door.Child(side < 0 ? "MirrorL" : "MirrorR", df.ToLocal(center));
            Geo.RoundBox(mount.M("paint"), Frame.Identity, Vector3.zero, new Vector3(0.16f, 0.10f, 0.11f), 0.5f, 14);
            var from = df.ToLocal(basePt) - mount.pos;
            var to = new Vector3(-0.05f * side, -0.025f, 0.01f);
            Geo.RoundBox(mount.M("paint"), Frame.Look((from + to) * 0.5f, to - from, Vector3.up), Vector3.zero, new Vector3(0.06f, 0.028f, (to - from).magnitude + 0.03f), 0.4f, 6);
            Geo.RoundBox(door.M("black"), Frame.Identity, df.ToLocal(basePt), new Vector3(0.05f, 0.03f, 0.09f), 0.4f, 6);
            float yaw = side < 0 ? -27f : 48f;
            var glass = mount.Child(side < 0 ? "MirrorGlassL" : "MirrorGlassR", new Vector3(0f, 0.002f, -0.056f), new Vector3(0f, yaw * 0.25f, 0f));
            Geo.Quad(glass.M("mirror"), Frame.Identity, 0.14f, 0.08f);
        }

        /// <summary>Чёрное крыло на высоких стойках, торцевые пластины в цвет кузова.</summary>
        void Wing()
        {
            var trunk = N("Trunk");
            var f = trunk.WorldFrame();
            const float top = 1.15f, zc = -1.86f, half = 0.80f;
            var path = new List<Vector3>();
            for (int i = 0; i <= 24; i++) path.Add(f.ToLocal(new Vector3(Mathf.Lerp(-half, half, i / 24f), top, zc)));
            var foil = new List<Vector2>();
            const int nf = 16;
            for (int i = 0; i < nf * 2; i++)
            {
                float t = i < nf ? i / (float)nf : (2 * nf - i) / (float)nf;
                float xx = (1f - Mathf.Cos(t * Mathf.PI)) / 2f;
                float th = 0.35f * (0.2969f * Mathf.Sqrt(xx) - 0.126f * xx - 0.3516f * xx * xx + 0.2843f * xx * xx * xx - 0.1036f * xx * xx * xx * xx);
                float yy = (i < nf ? th : -th) * 0.28f;
                foil.Add(new Vector2((xx - 0.45f) * 0.28f, yy + xx * 0.035f));
            }
            Geo.Sweep(trunk.M("black_satin"), Frame.Identity, path, foil, false, Vector3.up, true);
            foreach (float side in new[] { -1f, 1f })
            {
                Geo.RoundBox(trunk.M("paint"), Frame.Identity, f.ToLocal(new Vector3(half * side, top - 0.02f, zc - 0.02f)), new Vector3(0.014f, 0.17f, 0.34f), 0.25f, 6);
                var a = f.ToLocal(new Vector3(0.42f * side, 0.94f, -1.76f));
                var b = f.ToLocal(new Vector3(0.42f * side, top - 0.01f, zc + 0.03f));
                Geo.RoundBox(trunk.M("black"), Frame.Look((a + b) * 0.5f, b - a, Vector3.forward), Vector3.zero, new Vector3(0.012f, 0.09f, (b - a).magnitude), 0.3f, 6);
            }
        }

        // ---------- Салон (руль слева) ----------

        void Interior()
        {
            var cab = body.Child("Interior");
            var id = Frame.Identity;
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.205f, -0.5f), new Vector3(1.56f, 0.03f, 1.85f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.42f, 0.52f), new Vector3(1.56f, 0.42f, 0.04f));
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.29f, -0.3f), new Vector3(0.28f, 0.2f, 1.4f), 0.25f, 8);
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.42f, -0.42f), new Vector3(0.25f, 0.08f, 0.75f), 0.2f, 8);
            // Рычаг КПП в красной изоленте и ручник
            Geo.Lathe(cab.M("leather_black"), new Frame { o = new Vector3(-0.02f, 0.46f, -0.24f), x = Vector3.right, y = Vector3.up, z = Vector3.forward },
                new[] { new Vector2(0.055f, 0f), new Vector2(0.04f, 0.04f), new Vector2(0.018f, 0.07f) }, 16);
            Geo.Cylinder(cab.M("leather_red"), Frame.Euler(new Vector3(-0.02f, 0.46f, -0.24f), new Vector3(-12f, 0f, 0f)), 0.016f, 0.05f, 0.2f, 10);
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(-0.02f, 0.665f, -0.2f), new Vector3(0.045f, 0.05f, 0.045f), 0.8f, 10);
            Geo.RoundBox(cab.M("leather_black"), Frame.Euler(new Vector3(0.08f, 0.48f, -0.6f), new Vector3(-14f, 0f, 0f)), Vector3.zero, new Vector3(0.04f, 0.035f, 0.24f), 0.5f, 6);
            // Коробочка закиси азота с красной кнопкой
            var nos = cab.Child("NosBox", new Vector3(0.13f, 0.52f, -0.08f), new Vector3(-20f, -15f, 0f));
            Geo.RoundBox(nos.M("int_grey"), id, Vector3.zero, new Vector3(0.09f, 0.09f, 0.06f), 0.2f, 6);
            Geo.Box(nos.M("tail_red"), id, new Vector3(0.035f, 0f, 0f), new Vector3(0.02f, 0.09f, 0.062f));
            Geo.Cylinder(nos.M("chrome"), new Frame { o = new Vector3(-0.01f, 0f, -0.03f), x = Vector3.right, y = Vector3.back, z = Vector3.up }, 0.016f, 0f, 0.02f, 14);

            // Торпеда чёрная; щиток у водителя слева
            var prof = new[]
            {
                new Vector2(0.42f, 0.83f), new Vector2(0.26f, 0.85f), new Vector2(0.1f, 0.86f), new Vector2(0.01f, 0.855f),
                new Vector2(-0.03f, 0.83f), new Vector2(-0.035f, 0.78f), new Vector2(-0.01f, 0.66f), new Vector2(0.03f, 0.54f), new Vector2(0.18f, 0.46f),
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
                return new Vector3(Mathf.Lerp(-0.77f, 0.77f, a), q.y, q.x);
            });
            foreach (float x in new[] { 0.68f, -0.70f }) Vent(cab, new Vector3(x, 0.77f, -0.04f), 0.11f, 0.07f);
            var stack = cab.Child("CenterStack", new Vector3(-0.02f, 0.66f, -0.03f), new Vector3(6f, 8f, 0f));
            Geo.RoundBox(stack.M("int_black"), id, Vector3.zero, new Vector3(0.28f, 0.3f, 0.08f), 0.25f, 8);
            Vent(stack, new Vector3(0f, 0.1f, -0.045f), 0.18f, 0.06f);
            for (int i = 0; i < 3; i++)
                Geo.Cylinder(stack.M("int_grey"), Frame.Euler(new Vector3(-0.08f + i * 0.08f, 0.02f, -0.045f), new Vector3(-90f, 0f, 0f)), 0.022f, 0f, 0.02f, 14);
            Geo.RoundBox(stack.M("int_grey"), id, new Vector3(0f, -0.07f, -0.042f), new Vector3(0.2f, 0.05f, 0.02f), 0.2f, 6);
            var screen = stack.Child("RadioScreen", new Vector3(0f, -0.07f, -0.054f));
            Geo.Quad(screen.M("screen"), id, 0.15f, 0.03f);

            Cluster(cab);
            ExtraGauges(cab);
            SteeringWheel(cab);
            foreach (float side in new[] { -1f, 1f }) Seat(cab, side);
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.5f, -1.5f), new Vector3(1.3f, 0.03f, 0.7f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.35f, -1.15f), new Vector3(1.3f, 0.3f, 0.04f));
            foreach (float side in new[] { -1f, 1f })
            {
                var door = N(side < 0 ? "DoorL" : "DoorR");
                var df = door.WorldFrame();
                Geo.RoundBox(door.M("leather_tan"), id, df.ToLocal(new Vector3(0.79f * side, 0.45f, -0.15f)), new Vector3(0.04f, 0.3f, 0.7f), 0.3f, 8);
                Geo.RoundBox(door.M("leather_black"), id, df.ToLocal(new Vector3(0.75f * side, 0.6f, -0.4f)), new Vector3(0.08f, 0.05f, 0.55f), 0.4f, 8);
            }
            Geo.Cylinder(cab.M("int_black"), Frame.Euler(new Vector3(0f, 1.16f, -0.27f), Vector3.zero), 0.012f, -0.03f, 0.015f, 8);
            var rm = cab.Child("RearMirror", new Vector3(0f, 1.11f, -0.275f), new Vector3(0f, 21f, 0f));
            Geo.RoundBox(rm.M("int_black"), id, new Vector3(0f, 0f, 0.016f), new Vector3(0.26f, 0.07f, 0.035f), 0.5f, 10);
            Geo.Quad(rm.Child("RearMirrorGlass", new Vector3(0f, 0f, -0.0025f)).M("mirror"), id, 0.24f, 0.055f);
            foreach (float x in new[] { -0.38f, 0.38f })
                Geo.RoundBox(cab.M("int_roof"), Frame.Euler(new Vector3(x, 1.16f, -0.36f), new Vector3(-10f, 0f, 0f)), Vector3.zero, new Vector3(0.40f, 0.022f, 0.16f), 0.4f, 6);
            Driver(0.73f, -0.9f, -15f);
        }

        void Vent(ModelNode n, Vector3 c, float w, float h)
        {
            Geo.RoundBox(n.M("int_black"), Frame.Identity, c, new Vector3(w, h, 0.02f), 0.25f, 6);
            Geo.RoundBox(n.M("grille"), Frame.Identity, c + new Vector3(0f, 0f, -0.006f), new Vector3(w * 0.85f, h * 0.75f, 0.012f), 0.2f, 4);
            for (int i = 0; i < 4; i++)
                Geo.Box(n.M("int_grey"), Frame.Identity, c + new Vector3(0f, (i - 1.5f) * h * 0.18f, -0.014f), new Vector3(w * 0.8f, 0.004f, 0.006f));
        }

        void Cluster(ModelNode cab)
        {
            var c = new Vector3(-0.37f, 0.80f, -0.07f);
            Vector3 Hood(float a, float b)
            {
                float th = Mathf.Lerp(-0.2f, Mathf.PI + 0.2f, a);
                return c + new Vector3(Mathf.Cos(th) * 0.24f, Mathf.Sin(th) * 0.24f * 0.45f + 0.035f, Mathf.Lerp(0.06f, -0.05f, b) - 0.02f * Mathf.Sin(th));
            }
            Geo.Surface(cab.M("int_black"), Frame.Identity, 30, 4, Hood, true);
            Geo.Surface(cab.M("int_black"), Frame.Identity, 30, 4, Hood, false);
            Geo.RoundBox(cab.M("gauge_face"), Frame.Identity, c + new Vector3(0f, -0.005f, 0.05f), new Vector3(0.46f, 0.16f, 0.02f), 0.3f, 6);
            Gauge(cab, "Speed", c + new Vector3(-0.16f, -0.012f, 0.03f), 0.055f, true);
            Gauge(cab, "Tach", c + new Vector3(0f, -0.005f, 0.025f), 0.068f, true);
            Gauge(cab, "Fuel", c + new Vector3(0.16f, -0.012f, 0.03f), 0.047f, true);
            var fl = cab.Child("FuelLamp", c + new Vector3(0.16f, -0.05f, 0.016f));
            Geo.RoundBox(fl.M("lamp_off"), Frame.Identity, Vector3.zero, new Vector3(0.022f, 0.012f, 0.004f), 0.3f, 4);
            var el = cab.Child("EngineLamp", c + new Vector3(-0.16f, -0.055f, 0.016f));
            Geo.RoundBox(el.M("lamp_off"), Frame.Identity, Vector3.zero, new Vector3(0.022f, 0.012f, 0.004f), 0.3f, 4);
        }

        /// <summary>Карбоновые подиумы с приборами на торпеде: два посередине и три у пассажира.</summary>
        void ExtraGauges(ModelNode cab)
        {
            var pod = cab.Child("Pods");
            void Pod(Vector3 from, int count, float step, string prefix)
            {
                var len = count * step + 0.03f;
                Geo.RoundBox(pod.M("carbon"), Frame.Identity, from + new Vector3(step * (count - 1) / 2f, -0.005f, 0.03f), new Vector3(len, 0.08f, 0.09f), 0.45f, 10);
                for (int i = 0; i < count; i++)
                {
                    var p = from + new Vector3(step * i, 0.01f, -0.012f);
                    Gauge(pod, prefix + i, p, 0.03f, true, "gauge_light", "int_black");
                }
            }
            Pod(new Vector3(-0.08f, 0.9f, 0.1f), 2, 0.075f, "Center");
            Pod(new Vector3(0.3f, 0.9f, 0.12f), 3, 0.08f, "Right");
        }

        void SteeringWheel(ModelNode cab)
        {
            var w = body.Child("SteeringWheel", SteeringPos, new Vector3(SteeringTilt, 0f, 0f));
            var id = Frame.Identity;
            const float R = 0.18f;
            // Красная оплётка на весь обод
            Geo.Torus(w.M("leather_red"), id, R, 0.021f, 48, 10);
            foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.back })
            {
                var f = Frame.Look(dir * R * 0.5f, dir, Vector3.up);
                Geo.RoundBox(w.M("int_black"), f, Vector3.zero, new Vector3(0.05f, 0.02f, R * 0.95f), 0.4f, 6);
            }
            Geo.RoundBox(w.M("int_black"), id, new Vector3(0f, 0.015f, -0.01f), new Vector3(0.14f, 0.055f, 0.11f), 0.5f, 10);
            Geo.Torus(w.M("chrome"), new Frame { o = new Vector3(0f, 0.043f, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward * 0.62f }, 0.022f, 0.003f, 24, 5);
            Geo.Cylinder(w.M("int_black"), id, 0.035f, -0.3f, -0.01f, 12);
        }

        /// <summary>Бежевая кожа с чёрными боковинами.</summary>
        void Seat(ModelNode cab, float side)
        {
            float x = 0.37f * side;
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("leather_tan"), id, new Vector3(x, 0.29f, -0.78f), new Vector3(0.5f, 0.1f, 0.52f), 0.3f, 8);
            Geo.RoundBox(cab.M("leather_tan"), id, new Vector3(x, 0.355f, -0.78f), new Vector3(0.34f, 0.07f, 0.5f), 0.4f, 8);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(cab.M("leather_tan"), id, new Vector3(x + b * 0.2f, 0.37f, -0.8f), new Vector3(0.1f, 0.12f, 0.48f), 0.5f, 8);
            var back = cab.Child(side > 0 ? "SeatBackR" : "SeatBackL", new Vector3(x, 0.35f, -1.02f), new Vector3(-14f, 0f, 0f));
            Geo.RoundBox(back.M("leather_tan"), id, new Vector3(0f, 0.31f, -0.02f), new Vector3(0.48f, 0.62f, 0.12f), 0.35f, 8);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(back.M("leather_tan"), id, new Vector3(b * 0.2f, 0.26f, 0.05f), new Vector3(0.09f, 0.46f, 0.14f), 0.5f, 8);
            Geo.RoundBox(back.M("leather_tan"), id, new Vector3(0f, 0.67f, -0.01f), new Vector3(0.26f, 0.17f, 0.11f), 0.55f, 8);
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
                // Много тонких спиц — «сеточка», как у дисков Доминика
                WheelModel.FiveSpoke(mesh, WheelR, 0.25f, 16, 0.241f, "chrome", "chrome", 0.32f);
                WheelModel.Caliper(mount, side, 1.15f);
            }
        }
    }
}

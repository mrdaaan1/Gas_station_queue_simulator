using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «Гелик» 2025 (W465, версия «63»): квадратный кузов с почти вертикальными стенками и плоской крышей,
    /// круглые фары со светодиодным кольцом, решётка с вертикальными рёбрами, поворотники на крыльях,
    /// расширители арок, подножки, боковой выхлоп, запаска на задней двери, 22" колёса.
    /// Салон: два широких экрана под одним стеклом, круглые «турбинки», синяя подсветка, руль слева.
    /// Размеры: 4,62 м кузов (+ запаска), 1,86 м (1,98 с расширителями), высота 1,97 м, база 2,89 м.
    /// </summary>
    public class GelikModel : SportsCarModel
    {
        public const float AxleF = 1.51f, AxleR = -1.38f;
        public const float WheelR = 0.43f, Track = 0.83f;
        const float W = 0.925f;
        const float FrontDoorF = 0.93f, DoorSplit = -0.27f, RearDoorR = -1.05f;

        public static readonly Vector3 DriverEyes = new Vector3(-0.42f, 1.55f, -0.38f);
        public static readonly Vector3 SteeringPos = new Vector3(-0.42f, 1.16f, 0.13f);
        public const float SteeringTilt = -66f;

        static Model cached;

        public static Model Get()
        {
            if (cached == null) cached = new GelikModel().Build();
            return cached;
        }

        public static GelikModel Shape() => new GelikModel();

        protected override Vector3 Eyes => DriverEyes;

        GelikModel()
        {
            Front = 2.33f; Rear = -2.31f;
            Deck = -2.30f; RoofRear = -2.29f; Header = 0.52f; Cowl = 0.80f;
            FrontRound = 2.21f; RearRound = -2.19f; FrontPow = 6f; RearPow = 6f;
            RockerFrac = 0.96f; RockerAngle = 45f;

            W0 = new Curve().Key(-2.31f, W).Key(2.33f, W);
            YBot = new Curve().Key(-2.31f, 0.5f).Key(-2.2f, 0.47f).Key(2.2f, 0.47f).Key(2.33f, 0.5f);
            YMax = new Curve().Key(-2.31f, 0.6f).Key(2.33f, 0.6f);
            YBelt = new Curve().Key(-2.31f, 1.27f).Key(0.5f, 1.27f).Key(Cowl, 1.235f).Key(1.5f, 1.185f).Key(2.0f, 1.16f).Key(2.33f, 1.145f);
            FBelt = new Curve().Key(-2.31f, 0.985f).Key(2.33f, 0.985f);
            YTop = new Curve().Key(-2.31f, 1.94f).Key(-0.8f, 1.955f).Key(Header, 1.915f, true).Key(Cowl, 1.25f, true)
                .Key(1.5f, 1.2f).Key(2.0f, 1.175f).Key(2.33f, 1.16f);
            YRoofEdge = new Curve().Key(-2.31f, 1.905f).Key(Header, 1.885f, true).Key(Cowl, 1.235f, true);
            XRoofEdge = new Curve().Key(-2.31f, 0.86f).Key(Header, 0.86f, true).Key(Cowl, 0.911f, true);
            AngC = new Curve().Key(-2.31f, 90f).Key(2.33f, 90f);
            AngE0 = new Curve().Key(-2.31f, 140f).Key(Cowl, 140f).Key(2.33f, 145f);
            AngD0 = new Curve().Key(-2.31f, 96f).Key(2.33f, 96f);
            AngD1 = new Curve().Key(-2.31f, 98f).Key(2.33f, 98f);

            arches.Add(new Arch { z = AxleF, y = WheelR, r = 0.49f, xMin = 0.64f });
            arches.Add(new Arch { z = AxleR, y = WheelR, r = 0.49f, xMin = 0.64f });
            Prepare();
        }

        // ---------- Клетки кузова ----------

        static bool Between(float z, float a, float b) => z > Mathf.Min(a, b) && z < Mathf.Max(a, b);

        protected override CellInfo Classify(float z, int seg, float v, Vector3 p, float side)
        {
            string node = "Shell", mat = "paint", inner = null;
            if (seg == SegE)
            {
                if (z > Header && z < Cowl)
                {
                    float edge = Mathf.Min(z - Header, Cowl - z);
                    if (v > 0.07f && edge > 0.03f) mat = "glass";
                    else if (v > 0.04f && edge > 0.006f) mat = "black";
                }
                if (z > Cowl + 0.015f && z < 2.27f) node = "Hood";
                else if (z < Header && z > -2.2f) node = "Roof";
            }
            else if (seg == SegD && InCabin(z) && z > -2.2f)
            {
                // Три окна: передняя дверь, задняя дверь, заднее боковое. Стойка B — чёрная.
                bool win = Between(z, -0.20f, 0.70f) || Between(z, -0.98f, -0.34f) || Between(z, -2.06f, -1.15f);
                bool frame = Between(z, -0.23f, 0.76f) || Between(z, -1.01f, -0.31f) || Between(z, -2.09f, -1.12f) || Between(z, -0.34f, -0.20f);
                if (win && v > 0.07f && v < 0.93f) mat = "glass";
                else if (frame && v > 0.03f) mat = "black";
            }
            bool lower = seg == SegB || seg == SegC || seg == SegD;
            if (lower && p.y > 0.5f)
            {
                if (Between(z, DoorSplit, FrontDoorF)) node = side > 0 ? "DoorFR" : "DoorFL";
                else if (Between(z, RearDoorR, DoorSplit)) node = side > 0 ? "DoorRR" : "DoorRL";
            }
            if (z < -2.22f && seg >= SegB) node = "Tailgate";
            if (mat != "glass" && z < Cowl) inner = seg == SegA ? "carpet" : seg >= SegD ? "int_roof" : "int_door";
            return new CellInfo { node = node, mat = mat, innerMat = inner };
        }

        protected override Vector3 Pivot(string name)
        {
            switch (name)
            {
                case "Hood": return new Vector3(0f, 1.25f, Cowl + 0.02f);
                case "Roof": return new Vector3(0f, 1.94f, -0.8f);
                case "Tailgate": return new Vector3(-0.88f, 1.2f, -2.31f); // петли задней двери — слева
                case "DoorFL": return new Vector3(-W, 0.9f, FrontDoorF);
                case "DoorFR": return new Vector3(W, 0.9f, FrontDoorF);
                case "DoorRL": return new Vector3(-W, 0.9f, DoorSplit);
                case "DoorRR": return new Vector3(W, 0.9f, DoorSplit);
                case "BumperF": return new Vector3(0f, 0.6f, 2.38f);
                case "BumperR": return new Vector3(0f, 0.6f, -2.38f);
            }
            return Vector3.zero;
        }

        // ---------- Сборка ----------

        Model Build()
        {
            StartModel();
            Emit(N, 200f);
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
            RearDetails();
            Chassis();
            Interior();
            BuildWheels();
            return model;
        }

        static Frame Facing(Vector3 o, Vector3 n)
        {
            // Система с осью Y по нормали (для тел вращения «лицом» наружу)
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

        // ---------- Перед ----------

        /// <summary>Круглая фара: хромовый ободок, светодиодное кольцо, линза-проектор, выпуклое стекло.</summary>
        void Headlight(float side)
        {
            var p = FrontPoint(0.695f * side, 0.995f, out var n);
            var node = body.Child(side < 0 ? "HeadlightL" : "HeadlightR", p);
            var f = Facing(Vector3.zero, n);
            Geo.Torus(node.M("chrome"), f.Mul(Fy(0.006f)), 0.113f, 0.011f, 40, 8);
            Geo.Lathe(node.M("housing"), f, new[] { new Vector2(0.105f, 0.006f), new Vector2(0.1f, -0.01f), new Vector2(0.06f, -0.025f), new Vector2(0f, -0.03f) }, 32, true, true);
            Geo.Torus(node.M("lamp_glow"), f.Mul(Fy(0.002f)), 0.083f, 0.006f, 40, 6);
            Geo.Lathe(node.M("reflector"), f, new[] { new Vector2(0.055f, -0.004f), new Vector2(0.04f, -0.014f), new Vector2(0f, -0.018f) }, 24, true, true);
            Geo.Lathe(node.M("lamp_glow"), f, new[] { new Vector2(0.034f, -0.006f), new Vector2(0.026f, 0.008f), new Vector2(0f, 0.013f) }, 24);
            // Светодиодная полоса поперёк (у новой модели)
            Geo.Box(node.M("lamp_glow"), f, new Vector3(0f, 0.004f, -0.045f), new Vector3(0.12f, 0.004f, 0.006f));
            Geo.Lathe(node.M("lens"), f, new[] { new Vector2(0.108f, 0.006f), new Vector2(0.09f, 0.022f), new Vector2(0.05f, 0.032f), new Vector2(0f, 0.035f) }, 32);
            // Поворотник-«бочонок» на крыле
            var blink = body.Child(side < 0 ? "BlinkFL" : "BlinkFR", new Vector3(0.80f * side, 1.175f, 2.13f));
            Geo.RoundBox(blink.M("black"), Frame.Identity, new Vector3(0f, -0.005f, -0.03f), new Vector3(0.15f, 0.05f, 0.13f), 0.5f, 8);
            Geo.RoundBox(blink.M("amber"), Frame.Identity, new Vector3(0f, 0.012f, 0.005f), new Vector3(0.13f, 0.045f, 0.1f), 0.55f, 10);
        }

        static Frame Fy(float h) => new Frame { o = new Vector3(0f, h, 0f), x = Vector3.right, y = Vector3.up, z = Vector3.forward };

        /// <summary>Решётка с вертикальными рёбрами, круглая эмблема, бампер с воздухозаборниками и номером.</summary>
        void FrontEnd()
        {
            var shell = N("Shell");
            var c = FrontPoint(0f, 0.93f, out var n);
            var g = shell.Child("Grille", c + n * 0.004f);
            var id = Frame.Identity;
            Geo.RoundBox(g.M("grille"), id, new Vector3(0f, 0f, 0.008f), new Vector3(0.86f, 0.36f, 0.03f), 0.12f, 6);
            // Хромированная рамка
            foreach (float y in new[] { -0.18f, 0.18f })
                Geo.RoundBox(g.M("chrome"), id, new Vector3(0f, y, 0.022f), new Vector3(0.88f, 0.014f, 0.016f), 0.4f, 6);
            foreach (float x in new[] { -0.43f, 0.43f })
                Geo.RoundBox(g.M("chrome"), id, new Vector3(x, 0f, 0.022f), new Vector3(0.014f, 0.37f, 0.016f), 0.4f, 6);
            // Вертикальные рёбра
            for (int i = 0; i < 15; i++)
            {
                float x = Mathf.Lerp(-0.39f, 0.39f, i / 14f);
                if (Mathf.Abs(x) < 0.09f) continue; // за эмблемой
                Geo.RoundBox(g.M("chrome"), id, new Vector3(x, 0f, 0.026f), new Vector3(0.011f, 0.33f, 0.03f), 0.45f, 6);
            }
            // Эмблема: кольцо и диск
            var ef = new Frame { o = new Vector3(0f, 0f, 0.04f), x = Vector3.right, y = Vector3.forward, z = Vector3.down };
            Geo.Torus(g.M("chrome"), ef, 0.088f, 0.01f, 40, 8);
            Geo.Lathe(g.M("black"), ef, new[] { new Vector2(0.08f, -0.005f), new Vector2(0.06f, 0.004f), new Vector2(0f, 0.006f) }, 32);
            Geo.Torus(g.M("chrome"), new Frame { o = new Vector3(0f, 0f, 0.048f), x = Vector3.right, y = Vector3.forward, z = Vector3.down }, 0.03f, 0.006f, 24, 6);
            // Шов капота по краю крыльев
            foreach (float side in new[] { -1f, 1f })
            {
                var seam = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++) seam.Add((new Vector3(0.66f, 0.9f, Mathf.Lerp(Cowl + 0.03f, 2.3f, i / 24f)), Vector3.up));
                Ribbon(N("Hood"), "black", seam, 0.006f, 0.001f, side);
            }
            var cross = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 16; i++) cross.Add((new Vector3(Mathf.Lerp(0f, 0.66f, i / 16f), 0.9f, 2.29f), Vector3.up));
            Ribbon(N("Hood"), "black", cross, 0.006f, 0.001f, 1f);
            Ribbon(N("Hood"), "black", cross, 0.006f, 0.001f, -1f);

            // Бампер
            var bf = N("BumperF");
            var bfr = bf.WorldFrame();
            Geo.RoundBox(bf.M("paint"), id, bfr.ToLocal(new Vector3(0f, 0.63f, 2.38f)), new Vector3(1.92f, 0.3f, 0.25f), 0.22f, 12);
            Geo.RoundBox(bf.M("black"), id, bfr.ToLocal(new Vector3(0f, 0.475f, 2.385f)), new Vector3(1.78f, 0.08f, 0.24f), 0.3f, 8);
            Geo.Box(bf.M("grille"), id, bfr.ToLocal(new Vector3(0f, 0.545f, 2.502f)), new Vector3(0.62f, 0.07f, 0.012f));
            foreach (float side in new[] { -1f, 1f })
            {
                Geo.Box(bf.M("grille"), id, bfr.ToLocal(new Vector3(0.6f * side, 0.62f, 2.502f)), new Vector3(0.34f, 0.15f, 0.012f));
                for (int i = 0; i < 3; i++)
                    Geo.Box(bf.M("int_grey"), id, bfr.ToLocal(new Vector3(0.6f * side, 0.58f + i * 0.04f, 2.508f)), new Vector3(0.32f, 0.01f, 0.01f));
                Geo.RoundBox(bf.M("black"), id, bfr.ToLocal(new Vector3(0.36f * side, 0.62f, 2.5f)), new Vector3(0.05f, 0.24f, 0.03f), 0.4f, 6);
            }
            PlateAt(bf, new Vector3(0f, 0.69f, 2.512f), Vector3.forward, "PlateFront");
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
            Ribbon(N(fd), "black", Line(FrontDoorF, 0.5f, FrontDoorF, 1.26f), 0.006f, 0.001f, side);
            Ribbon(N(fd), "black", Line(DoorSplit, 0.5f, DoorSplit, 1.26f), 0.006f, 0.001f, side);
            Ribbon(N(rd), "black", Line(RearDoorR, 0.5f, RearDoorR, 1.26f), 0.006f, 0.001f, side);
            Ribbon(N(fd), "black", Line(DoorSplit, 0.505f, FrontDoorF, 0.505f, 30), 0.006f, 0.001f, side);
            Ribbon(N(rd), "black", Line(RearDoorR, 0.505f, DoorSplit, 0.505f, 20), 0.006f, 0.001f, side);
            // Молдинг посередине борта и тонкая хромированная полоса внизу дверей
            Ribbon(shell, "black", Line(2.12f, 1.02f, -2.16f, 1.02f, 60), 0.045f, 0.004f, side);
            Ribbon(N(fd), "chrome", Line(DoorSplit + 0.01f, 0.6f, FrontDoorF - 0.01f, 0.6f, 20), 0.012f, 0.003f, side);
            Ribbon(N(rd), "chrome", Line(RearDoorR + 0.01f, 0.6f, DoorSplit - 0.01f, 0.6f, 14), 0.012f, 0.003f, side);
            // Ручки (кнопочные) и открытые петли — фирменная деталь
            foreach (var (door, z) in new[] { (fd, DoorSplit + 0.2f), (rd, RearDoorR + 0.2f) })
            {
                var p = SidePoint(side, 1.13f, z, out var n);
                var dn = N(door);
                Geo.RoundBox(dn.M("chrome"), Frame.Identity, dn.WorldFrame().ToLocal(p + n * 0.015f), new Vector3(0.025f, 0.035f, 0.17f), 0.45f, 8);
            }
            foreach (var (door, z) in new[] { (fd, FrontDoorF - 0.012f), (rd, DoorSplit - 0.012f) })
            foreach (float y in new[] { 0.72f, 1.12f })
            {
                var p = SidePoint(side, y, z, out var n);
                var dn = N(door);
                Geo.RoundBox(dn.M("black"), Frame.Identity, dn.WorldFrame().ToLocal(p + n * 0.012f), new Vector3(0.03f, 0.075f, 0.06f), 0.4f, 6);
            }
            // Водостоки по краю крыши
            var rail = new List<(Vector3, Vector3)>();
            for (int i = 0; i <= 40; i++)
            {
                float z = Mathf.Lerp(Header - 0.02f, -2.22f, i / 40f);
                rail.Add((new Vector3(0f, 1.6f, z), new Vector3(0.86f, 0.3f, 0f)));
            }
            Ribbon(shell, "black", rail, 0.03f, 0.006f, side);
            // Подножка
            Geo.RoundBox(shell.M("black"), Frame.Identity, new Vector3(0.99f * side, 0.405f, -0.1f), new Vector3(0.2f, 0.055f, 1.74f), 0.3f, 8);
            for (int i = 0; i < 3; i++)
                Geo.RoundBox(shell.M("int_grey"), Frame.Identity, new Vector3((0.94f + i * 0.045f) * side, 0.435f, -0.1f), new Vector3(0.018f, 0.008f, 1.68f), 0.4f, 4);
            // Боковой выхлоп перед задним колесом: две трубы
            foreach (float z in new[] { -0.79f, -0.89f })
            {
                var pf = new Frame { o = new Vector3(0.82f * side, 0.365f, z), x = Vector3.forward, y = new Vector3(side, 0f, 0f), z = new Vector3(0f, side, 0f) };
                pf = new Frame { o = pf.o, x = Vector3.forward, y = new Vector3(side, 0f, 0f), z = Vector3.Cross(Vector3.forward, new Vector3(side, 0f, 0f)) };
                Geo.Lathe(shell.M("chrome"), pf, new[] { new Vector2(0.042f, -0.05f), new Vector2(0.042f, 0.2f), new Vector2(0.046f, 0.205f), new Vector2(0.038f, 0.21f) }, 20);
                Geo.Lathe(shell.M("int_black"), pf, new[] { new Vector2(0.036f, 0.21f), new Vector2(0.036f, 0.1f), new Vector2(0f, 0.1f) }, 20, false, true);
            }
            // Лючок бака справа сзади
            if (side > 0)
            {
                var hatch = new List<(Vector3, Vector3)>();
                for (int i = 0; i <= 24; i++)
                {
                    float a = i / 24f * Mathf.PI * 2f;
                    float y = 1.12f + Mathf.Sin(a) * 0.075f, z = -1.75f + Mathf.Cos(a) * 0.075f;
                    hatch.Add((new Vector3(0.5f, y, z), Vector3.right));
                }
                Ribbon(shell, "black", hatch, 0.005f, 0.001f, side);
            }
        }

        /// <summary>Расширитель арки: толстая «бровь» цвета кузова по дуге над колесом.</summary>
        void Flare(Arch a, float side)
        {
            var m = N("Shell").M("paint");
            float yb = 0.47f;
            float th0 = Mathf.Asin(Mathf.Clamp((yb - 0.03f - a.y) / a.r, -1f, 1f));
            float th1 = Mathf.PI - th0;
            Vector3 P(float u, float b)
            {
                float th = Mathf.Lerp(th0, th1, u);
                float phi = b * Mathf.PI * 2f;
                float cs = Mathf.Cos(phi), sn = Mathf.Sin(phi);
                float rad = 0.04f + Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), 0.45f) * 0.05f;
                float outw = 0.015f + Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.45f) * 0.045f;
                float r = a.r + rad;
                return new Vector3(side * (W + outw), a.y + Mathf.Sin(th) * r, a.z + Mathf.Cos(th) * r);
            }
            Geo.Surface(m, Frame.Identity, 40, 16, P, side < 0);
        }

        /// <summary>Большое чёрное зеркало на двери у стойки; стекло повёрнуто к водителю (он слева).</summary>
        void Mirror(float side)
        {
            var door = N(side < 0 ? "DoorFL" : "DoorFR");
            var df = door.WorldFrame();
            var basePt = new Vector3((W * 0.985f) * side, 1.29f, 0.68f);
            var center = basePt + new Vector3(0.16f * side, 0.08f, -0.03f);
            var mount = door.Child(side < 0 ? "MirrorL" : "MirrorR", df.ToLocal(center));
            Geo.RoundBox(mount.M("black"), Frame.Identity, Vector3.zero, new Vector3(0.22f, 0.15f, 0.11f), 0.45f, 14);
            var from = df.ToLocal(basePt) - mount.pos;
            var to = new Vector3(-0.07f * side, -0.04f, 0.01f);
            Geo.RoundBox(mount.M("black"), Frame.Look((from + to) * 0.5f, to - from, Vector3.up), Vector3.zero, new Vector3(0.05f, 0.028f, (to - from).magnitude + 0.04f), 0.4f, 6);
            MirrorGlass(mount, side < 0 ? "MirrorGlassL" : "MirrorGlassR", new Vector3(0.22f, 0.15f, 0.11f));
        }

        // ---------- Корма ----------

        void Taillight(float side)
        {
            var p = RearPoint(0.80f * side, 0.93f, out var n);
            var node = body.Child(side < 0 ? "TaillightL" : "TaillightR", p);
            var f = Frame.Look(Vector3.zero, n, Vector3.up);
            Geo.RoundBox(node.M("black"), f, new Vector3(0f, 0f, 0.005f), new Vector3(0.27f, 0.105f, 0.03f), 0.3f, 8);
            Geo.RoundBox(node.M("tail_red"), f, new Vector3(0f, 0.005f, 0.016f), new Vector3(0.25f, 0.075f, 0.02f), 0.3f, 8);
            Geo.Box(node.M("lamp_glow"), f, new Vector3(0f, -0.022f, 0.027f), new Vector3(0.22f, 0.006f, 0.004f));
            var blink = node.Child(side < 0 ? "BlinkRL" : "BlinkRR");
            Geo.Box(blink.M("amber"), f, new Vector3(-0.07f * side, 0.018f, 0.027f), new Vector3(0.08f, 0.02f, 0.004f));
        }

        /// <summary>Задняя дверь: стекло, запаска в чёрном кожухе, петли слева, ручка справа; бампер с номером; стоп-сигнал на крыше.</summary>
        void RearDetails()
        {
            var tail = N("Tailgate");
            var tf = tail.WorldFrame();
            foreach (float side in new[] { -1f, 1f })
            {
                Patch(tail, "glass_dark", 12, 8, (a, b) => (new Vector3(a * 0.62f, Mathf.Lerp(1.42f, 1.84f, b), -1.9f), Vector3.back), 0.003f, side);
                var frame = new List<(Vector3, Vector3)>();
                foreach (var (x, y) in new[] { (0f, 1.85f), (0.63f, 1.85f), (0.63f, 1.41f), (0f, 1.41f) })
                    frame.Add((new Vector3(x, y, -1.9f), Vector3.back));
                Ribbon(tail, "black", frame, 0.025f, 0.004f, side);
            }
            // Запаска
            var c = RearPoint(-0.02f, 1.25f, out var n);
            var spare = tail.Child("Spare", tf.ToLocal(c));
            var sf = Facing(Vector3.zero, n);
            Geo.Lathe(spare.M("black_satin"), sf, new[]
            {
                new Vector2(0.39f, 0f), new Vector2(0.415f, 0.03f), new Vector2(0.425f, 0.18f), new Vector2(0.41f, 0.225f),
                new Vector2(0.38f, 0.24f), new Vector2(0.2f, 0.245f), new Vector2(0f, 0.248f),
            }, 48);
            Geo.Torus(spare.M("rim_inner"), sf.Mul(Fy(0.243f)), 0.33f, 0.008f, 48, 6);
            Geo.Torus(spare.M("chrome"), sf.Mul(Fy(0.25f)), 0.075f, 0.009f, 36, 6);
            Geo.Torus(spare.M("chrome"), sf.Mul(Fy(0.252f)), 0.025f, 0.006f, 20, 6);
            // Петли слева, ручка справа
            foreach (float y in new[] { 0.85f, 1.6f })
            {
                var hp = RearPoint(-0.88f, y, out var hn);
                Geo.RoundBox(tail.M("black"), Frame.Identity, tf.ToLocal(hp + hn * 0.012f), new Vector3(0.06f, 0.08f, 0.03f), 0.4f, 6);
            }
            var hp2 = RearPoint(0.55f, 1.06f, out var hn2);
            Geo.RoundBox(tail.M("chrome"), Frame.Identity, tf.ToLocal(hp2 + hn2 * 0.015f), new Vector3(0.15f, 0.03f, 0.025f), 0.45f, 8);
            // Стоп-сигнал на крыше
            var roof = N("Roof");
            var rf = roof.WorldFrame();
            Geo.RoundBox(roof.M("black"), Frame.Identity, rf.ToLocal(new Vector3(0f, 1.945f, -2.27f)), new Vector3(1.3f, 0.035f, 0.1f), 0.4f, 8);
            Geo.Box(roof.M("tail_red"), Frame.Identity, rf.ToLocal(new Vector3(0f, 1.935f, -2.321f)), new Vector3(0.4f, 0.016f, 0.006f));

            // Бампер
            var br = N("BumperR");
            var bfr = br.WorldFrame();
            var id = Frame.Identity;
            Geo.RoundBox(br.M("paint"), id, bfr.ToLocal(new Vector3(0f, 0.63f, -2.38f)), new Vector3(1.92f, 0.3f, 0.25f), 0.22f, 12);
            Geo.RoundBox(br.M("black"), id, bfr.ToLocal(new Vector3(0f, 0.475f, -2.385f)), new Vector3(1.78f, 0.08f, 0.24f), 0.3f, 8);
            foreach (float side in new[] { -1f, 1f })
            {
                Geo.RoundBox(br.M("black"), id, bfr.ToLocal(new Vector3(0.36f * side, 0.62f, -2.5f)), new Vector3(0.05f, 0.24f, 0.03f), 0.4f, 6);
                Geo.Box(br.M("tail_red"), id, bfr.ToLocal(new Vector3(0.74f * side, 0.69f, -2.506f)), new Vector3(0.2f, 0.022f, 0.006f));
            }
            PlateAt(br, new Vector3(0f, 0.69f, -2.512f), Vector3.back, "PlateRear");
        }

        /// <summary>Рама, мосты и бак под высоким кузовом — чтобы снизу не было пустоты.</summary>
        void Chassis()
        {
            var shell = N("Shell");
            var m = shell.M("chassis");
            var id = Frame.Identity;
            foreach (float x in new[] { -0.52f, 0.52f })
                Geo.Box(m, id, new Vector3(x, 0.4f, 0f), new Vector3(0.1f, 0.14f, 4.3f));
            foreach (float z in new[] { AxleF, AxleR })
            {
                var af = new Frame { o = new Vector3(0f, WheelR, z), x = Vector3.up, y = Vector3.right, z = Vector3.forward };
                Geo.Cylinder(m, af, 0.06f, -0.72f, 0.72f, 12);
                Geo.RoundBox(m, id, new Vector3(0.05f, WheelR, z), new Vector3(0.3f, 0.26f, 0.26f), 0.6f, 8);
            }
            Geo.Box(m, id, new Vector3(0f, 0.44f, -1.9f), new Vector3(1.0f, 0.18f, 0.5f));
            Geo.Box(m, id, new Vector3(0f, 0.44f, 1.95f), new Vector3(1.4f, 0.14f, 0.5f));
            Geo.Box(shell.M("int_black"), id, new Vector3(0f, 0.85f, 1.6f), new Vector3(1.5f, 0.6f, 1.2f)); // подкапотное
        }

        // ---------- Салон (руль слева) ----------

        void Interior()
        {
            var cab = body.Child("Interior");
            var id = Frame.Identity;
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.62f, -0.6f), new Vector3(1.7f, 0.03f, 2.9f));
            Geo.Box(cab.M("int_black"), id, new Vector3(0f, 0.92f, 0.84f), new Vector3(1.6f, 0.54f, 0.04f));
            // Торпеда: ровная полка, алюминиевая полоса и синяя подсветка
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(0f, 1.08f, 0.62f), new Vector3(1.76f, 0.3f, 0.42f), 0.22f, 10);
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.86f, 0.64f), new Vector3(1.6f, 0.24f, 0.36f), 0.25f, 8);
            Geo.RoundBox(cab.M("int_grey"), id, new Vector3(0f, 1.02f, 0.408f), new Vector3(1.7f, 0.05f, 0.02f), 0.3f, 6);
            Geo.Box(cab.M("ambient"), id, new Vector3(0f, 1.064f, 0.405f), new Vector3(1.72f, 0.007f, 0.012f));
            // Два экрана под одним стеклом
            var scr = cab.Child("Screens", new Vector3(-0.28f, 1.26f, 0.44f), new Vector3(-12f, 0f, 0f));
            Geo.RoundBox(scr.M("gauge_face"), id, Vector3.zero, new Vector3(1.0f, 0.19f, 0.03f), 0.15f, 6);
            var cluster = scr.Child("ClusterScreen", new Vector3(-0.245f, 0f, -0.0165f));
            Geo.Quad(cluster.M("screen_blue"), id, 0.45f, 0.16f);
            var info = scr.Child("RadioScreen", new Vector3(0.245f, 0f, -0.0165f));
            Geo.Quad(info.M("screen_blue"), id, 0.45f, 0.16f);
            // «Приборы» на экране: два кольца
            foreach (float x in new[] { -0.13f, 0.13f })
                Geo.Torus(cluster.M("white"), new Frame { o = new Vector3(x, 0f, -0.002f), x = Vector3.right, y = Vector3.back, z = Vector3.up }, 0.055f, 0.003f, 32, 4);
            // Круглые «турбинки»: две посередине и по краям
            foreach (float x in new[] { -0.15f, 0.05f, -0.79f, 0.79f })
            {
                var vf = new Frame { o = new Vector3(x, 1.13f, 0.405f), x = Vector3.right, y = Vector3.back, z = Vector3.up };
                Geo.Torus(cab.M("chrome"), vf, 0.058f, 0.009f, 32, 6);
                Geo.Torus(cab.M("ambient"), vf.Mul(Fy(-0.004f)), 0.047f, 0.004f, 28, 4);
                Geo.Lathe(cab.M("int_black"), vf, new[] { new Vector2(0.05f, 0f), new Vector2(0.03f, -0.01f), new Vector2(0f, -0.006f) }, 20);
                for (int k = 0; k < 6; k++)
                {
                    float a = k / 6f * 180f;
                    Geo.Box(cab.M("int_grey"), vf.Mul(Frame.Euler(new Vector3(0f, -0.004f, 0f), new Vector3(0f, a, 0f))), Vector3.zero, new Vector3(0.09f, 0.004f, 0.012f));
                }
            }
            // Поручень у пассажира
            Geo.RoundBox(cab.M("int_grey"), id, new Vector3(0.47f, 1.18f, 0.39f), new Vector3(0.44f, 0.03f, 0.035f), 0.5f, 8);
            foreach (float x in new[] { 0.27f, 0.67f })
                Geo.RoundBox(cab.M("int_grey"), id, new Vector3(x, 1.15f, 0.41f), new Vector3(0.03f, 0.06f, 0.04f), 0.5f, 6);
            // Высокая консоль между сиденьями
            Geo.RoundBox(cab.M("int_black"), id, new Vector3(0f, 0.86f, -0.2f), new Vector3(0.28f, 0.44f, 1.25f), 0.25f, 8);
            Geo.RoundBox(cab.M("int_grey"), id, new Vector3(0f, 1.085f, -0.2f), new Vector3(0.24f, 0.02f, 1.1f), 0.3f, 6);
            Geo.Box(cab.M("ambient"), id, new Vector3(0.141f, 1.0f, -0.2f), new Vector3(0.004f, 0.006f, 1.1f));
            Geo.Box(cab.M("ambient"), id, new Vector3(-0.141f, 1.0f, -0.2f), new Vector3(0.004f, 0.006f, 1.1f));

            SteeringWheel(cab);
            foreach (float x in new[] { -0.42f, 0.42f }) Seat(cab, x, -0.5f, x < 0 ? "SeatBackL" : "SeatBackR", 0.56f);
            // Задний диван
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(0f, 0.9f, -1.4f), new Vector3(1.5f, 0.14f, 0.52f), 0.3f, 8);
            var rb = cab.Child("RearBack", new Vector3(0f, 0.95f, -1.68f), new Vector3(-12f, 0f, 0f));
            Geo.RoundBox(rb.M("leather_black"), id, new Vector3(0f, 0.34f, 0f), new Vector3(1.5f, 0.68f, 0.14f), 0.3f, 8);
            Geo.Box(cab.M("carpet"), id, new Vector3(0f, 0.8f, -2.02f), new Vector3(1.6f, 0.03f, 0.5f));
            // Двери: подлокотники и синяя подсветка
            foreach (var door in new[] { "DoorFL", "DoorFR", "DoorRL", "DoorRR" })
            {
                float side = door.EndsWith("L") ? -1f : 1f;
                float z = door.StartsWith("DoorF") ? 0.33f : -0.66f;
                var dn = N(door);
                var df = dn.WorldFrame();
                Geo.RoundBox(dn.M("leather_black"), id, df.ToLocal(new Vector3(0.85f * side, 1.03f, z)), new Vector3(0.09f, 0.06f, 0.5f), 0.4f, 8);
                Geo.Box(dn.M("ambient"), id, df.ToLocal(new Vector3(0.885f * side, 1.17f, z)), new Vector3(0.006f, 0.008f, 0.55f));
            }
            // Салонное зеркало, повёрнутое к водителю слева
            Geo.Cylinder(cab.M("int_black"), Frame.Identity.Mul(new Frame { o = new Vector3(0f, 1.86f, 0.44f), x = Vector3.right, y = Vector3.up, z = Vector3.forward }), 0.012f, -0.04f, 0.02f, 8);
            var rm = cab.Child("RearMirror", new Vector3(0f, 1.8f, 0.44f), new Vector3(0f, 21f, 0f));
            Geo.RoundBox(rm.M("int_black"), id, new Vector3(0f, 0f, 0.016f), new Vector3(0.28f, 0.08f, 0.035f), 0.5f, 10);
            Geo.Quad(rm.Child("RearMirrorGlass", new Vector3(0f, 0f, -0.0025f)).M("mirror"), id, 0.26f, 0.065f);
            foreach (float x in new[] { -0.42f, 0.42f })
                Geo.RoundBox(cab.M("int_roof"), Frame.Euler(new Vector3(x, 1.87f, 0.42f), new Vector3(-8f, 0f, 0f)), Vector3.zero, new Vector3(0.42f, 0.022f, 0.17f), 0.4f, 6);
            Driver(1.27f, -0.6f, -10f);
        }

        void SteeringWheel(ModelNode cab)
        {
            var w = body.Child("SteeringWheel", SteeringPos, new Vector3(SteeringTilt, 0f, 0f));
            var id = Frame.Identity;
            const float R = 0.19f;
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

        /// <summary>Кресло: сиденье, спинка с подголовником (узел спинки наклонён).</summary>
        void Seat(ModelNode cab, float x, float z, string backName, float width)
        {
            var id = Frame.Identity;
            Geo.RoundBox(cab.M("leather_black"), id, new Vector3(x, 0.9f, z), new Vector3(width, 0.15f, 0.56f), 0.3f, 8);
            foreach (float b in new[] { -1f, 1f })
                Geo.RoundBox(cab.M("leather_black"), id, new Vector3(x + b * (width / 2f - 0.04f), 0.93f, z - 0.02f), new Vector3(0.09f, 0.15f, 0.54f), 0.5f, 8);
            var back = cab.Child(backName, new Vector3(x, 0.95f, z - 0.3f), new Vector3(-12f, 0f, 0f));
            Geo.RoundBox(back.M("leather_black"), id, new Vector3(0f, 0.35f, 0f), new Vector3(width, 0.7f, 0.14f), 0.3f, 8);
            // Стёжка «ромбами» — тонкие серые полосы
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
                WheelModel.FiveSpoke(mesh, WheelR, 0.295f, 10, 0.279f, "rim_black", "chrome", 0.7f);
                WheelModel.Caliper(mount, side, 1.3f);
            }
        }
    }
}

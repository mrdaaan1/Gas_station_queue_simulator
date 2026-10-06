using System.Collections.Generic;
using UnityEngine;
using L = GasQueue.TolyattiLayout;

namespace GasQueue
{
    /// <summary>
    /// Строит Автозаводский район Тольятти для уличной гонки (<see cref="TolyattiLayout"/>):
    /// улицы с разметкой, кольца с травяными островами, панельки 9–16 этажей, осенние деревья, фонари,
    /// ЛЭП на Обводном, узнаваемые места по скринам (храм у первого кольца, «Миндаль», ТЦ «Бегемот»,
    /// перекрёсток со светофорами у Тополиной, «Стоматология» и заправка у второго кольца,
    /// дублёр Льва Яшина с парковкой и ТЦ «Мадагаскар» с лемуром на фасаде), перекрытия боковых улиц,
    /// старт на Обводном и финиш в кармане.
    /// </summary>
    public static class TolyattiBuilder
    {
        static readonly Color Grass = Shapes.Hex("#86895a");
        static readonly Color GrassDark = Shapes.Hex("#6f7a48");
        static readonly Color Walk = Shapes.Hex("#a9a6a0");
        static readonly Color Curb = Shapes.Hex("#c4c2bb");
        static readonly Color PaintWhite = Shapes.Hex("#e9e8e2");
        static readonly Color Metal = Shapes.Hex("#7d8287");
        static readonly Color Roof = Shapes.Hex("#4a4a4c");

        /// <summary>Улица для зазоров: ось и полуширина (с тротуарами).</summary>
        struct Street
        {
            public LanePath line;
            public float half;
        }

        static readonly List<Street> streets = new List<Street>();
        /// <summary>Перекрёстки и кольца: тут не рисуем разметку, тротуары, фонари и деревья.</summary>
        static readonly List<(Vector3 c, float r)> junctions = new List<(Vector3, float)>();
        /// <summary>Места, занятые ориентирами, — дома тут не ставим.</summary>
        static readonly List<(Vector3 c, float r)> reserved = new List<(Vector3, float)>();
        static Material asphalt;
        static Transform props;

        public static RaceTrackBuilder.Result Build(Transform root, RaceTrack track)
        {
            streets.Clear();
            junctions.Clear();
            reserved.Clear();
            var g = Shapes.Group("Tolyatti", root);
            props = Shapes.Group("TolyattiCars", root); // машины не склеиваем: у них свои скрипты
            asphalt = MeshFactory.Textured(TextureFactory.Asphalt, "TltAsphalt");

            var dOf = L.OfitDir;
            var dY = (L.YuzhB - L.YuzhA).normalized;
            var jy = RaceTrack.Intersect(L.OfitA, dOf, L.YuzhA, dY);
            junctions.Add((jy, 26f));
            junctions.Add((L.OfitOkt70, 30f));
            junctions.Add((L.Ring1, L.RingOuter + 3f));
            junctions.Add((L.Topolinaya, 11f));
            junctions.Add((L.Ring2, L.RingOuter + 3f));

            Ground(g);
            BuildStreets(g, track);
            Ring(g, L.Ring1, true);
            Ring(g, L.Ring2, false);
            Pocket(g, track);
            Pylons(g);
            Landmarks(g);
            Buildings(g, track);
            Closures(g);
            var lights = StartGate(g, track);
            FinishGate(g, track);
            TurnSigns(g, track);

            // Тысячи неподвижных кубиков — склеиваем в большие сетки, иначе тормозит
            StaticBatchingUtility.Combine(g.gameObject);
            return new RaceTrackBuilder.Result { lights = lights };
        }

        // ---------- Земля и улицы ----------

        static void Ground(Transform g)
        {
            var c = (L.M(0f, 0f) + L.M(2000f, 942f)) / 2f;
            Shapes.Make(PrimitiveType.Plane, g, new Vector3(c.x, -0.03f, c.z), new Vector3(330f, 1f, 220f), Grass, name: "Earth");
        }

        static bool InJunction(Vector3 p, float extra = 0f)
        {
            foreach (var j in junctions)
                if ((p - j.c).sqrMagnitude < (j.r + extra) * (j.r + extra)) return true;
            return false;
        }

        static List<Vector3> Line(params Vector3[] pts) => new List<Vector3>(pts);

        static void BuildStreets(Transform g, RaceTrack track)
        {
            var dOf = L.OfitDir;
            var rOf = new Vector3(dOf.z, 0f, -dOf.x);
            var d70 = L.Okt70Dir;

            // Обводное + Офицерская: две проезжие части по 3 полосы, между ними газон с фонарями
            var ofit = Line(L.OfitA - dOf * 330f, L.OfitB + dOf * 150f);
            float carriage = L.OfitMedian / 2f + L.OfitLanes / 2f;
            foreach (float side in new[] { -1f, 1f })
            {
                Road(g, ofit, side * carriage, L.OfitLanes, 0.03f);
                Dashes(g, ofit, side * carriage - 1.75f, 3f, 9f);
                Dashes(g, ofit, side * carriage + 1.75f, 3f, 9f);
                Solid(g, ofit, side * (L.OfitMedian / 2f + L.OfitLanes - 0.4f));
            }
            Strip(g, ofit, 0f, L.OfitMedian, 0.05f, GrassDark, 0f);
            AddStreet(ofit, L.OfitMedian / 2f + L.OfitLanes + 3f);
            // Дублёр Офицерской (северо-западнее, за широким газоном)
            var jy = junctions[0].c;
            var dub = Line(jy - dOf * 40f + rOf * L.DublyorOffset, L.OfitOkt70 - dOf * 70f + rOf * L.DublyorOffset);
            Road(g, dub, 0f, 7f, 0.025f);
            AddStreet(dub, 6f);
            Sidewalks(g, ofit, L.OfitMedian / 2f + L.OfitLanes + 4.5f, 2.5f, true, false);

            // Южное шоссе
            var yuzh = Line(L.YuzhA, L.YuzhB);
            foreach (float side in new[] { -1f, 1f })
            {
                Road(g, yuzh, side * 8.5f, 11f, 0.02f);
                Dashes(g, yuzh, side * 8.5f, 3f, 9f);
            }
            Strip(g, yuzh, 0f, 6f, 0.045f, GrassDark, 0f);
            AddStreet(yuzh, L.YuzhWidth / 2f + 3f);

            // 70 лет Октября: 4 полосы, двойная сплошная, тротуары, фонари с двух сторон
            var okt = Line(L.Okt70A - d70 * 90f, L.Ring2 - d70 * 20f);
            Road(g, okt, 0f, L.StreetWidth, 0.03f);
            Solid(g, okt, -0.15f);
            Solid(g, okt, 0.15f);
            Dashes(g, okt, -3.5f, 3f, 9f);
            Dashes(g, okt, 3.5f, 3f, 9f);
            AddStreet(okt, L.StreetWidth / 2f + 6f);
            Sidewalks(g, okt, L.StreetWidth / 2f + 5f, 3f, true, true);

            // Боковые улицы
            SideStreet(g, Line(L.Ring1North[0], L.Ring1North[1], L.Ring1));
            SideStreet(g, Line(L.Ring1, L.Ring1South[0], L.Ring1South[1]));
            SideStreet(g, Line(L.TopolinayaNorth[0], L.TopolinayaNorth[1], L.Topolinaya, L.TopolinayaSouth[0], L.TopolinayaSouth[1]));
            SideStreet(g, Line(L.Ring2, L.Ring2SouthWest));
            SideStreet(g, Line(L.Ring2, L.Ring2NorthEast));

            // Льва Яшина
            var yash = Line(L.Ring2South, L.Ring2, L.OnYashina(700f));
            Road(g, yash, 0f, L.YashinaWidth, 0.03f);
            Solid(g, yash, -0.15f);
            Solid(g, yash, 0.15f);
            Dashes(g, yash, -3.7f, 3f, 9f);
            Dashes(g, yash, 3.7f, 3f, 9f);
            AddStreet(yash, L.YashinaWidth / 2f + 4f);
            Sidewalks(g, yash, L.YashinaWidth / 2f + 5f, 3f, true, true, onlyOneSide: true); // слева — парковка и карман

            // Зебры: у Тополиной (жёлто-белые) и на Льва Яшина у кольца
            Zebra(g, L.Topolinaya - d70 * 13f, d70, L.StreetWidth);
            Zebra(g, L.Topolinaya + d70 * 13f, d70, L.StreetWidth);
            Zebra(g, L.OnYashina(60f), L.YashinaDir, L.YashinaWidth);
            TrafficLights(g, L.Topolinaya, d70);
        }

        static void SideStreet(Transform g, List<Vector3> pts)
        {
            Road(g, pts, 0f, L.StreetWidth, 0.02f);
            Solid(g, pts, 0f);
            AddStreet(pts, L.StreetWidth / 2f + 6f);
            Sidewalks(g, pts, L.StreetWidth / 2f + 5f, 3f, true, true);
        }

        static void AddStreet(List<Vector3> pts, float half) =>
            streets.Add(new Street { line = new LanePath("Street", 1f, pts, false), half = half });

        /// <summary>Асфальт вдоль ломаной со смещением offset.</summary>
        static void Road(Transform g, List<Vector3> pts, float offset, float width, float y)
        {
            MeshFactory.MeshObject("Road", g, RaceTrackBuilder.Ribbon(pts, offset - width / 2f, offset + width / 2f, 4f), new Vector3(0f, y, 0f), Vector3.zero, asphalt);
        }

        static void Strip(Transform g, List<Vector3> pts, float offset, float width, float y, Color color, float skipExtra)
        {
            foreach (var piece in Pieces(pts, skipExtra))
                MeshFactory.MeshObject("Strip", g, RaceTrackBuilder.Ribbon(piece, offset - width / 2f, offset + width / 2f, 2f), new Vector3(0f, y, 0f), Vector3.zero, Shapes.Mat(color));
        }

        static void Solid(Transform g, List<Vector3> pts, float offset)
        {
            foreach (var piece in Pieces(pts, 0f))
                MeshFactory.MeshObject("Line", g, RaceTrackBuilder.Ribbon(piece, offset - 0.07f, offset + 0.07f, 1f), new Vector3(0f, 0.045f, 0f), Vector3.zero, Shapes.Mat(PaintWhite));
        }

        /// <summary>Куски ломаной вне перекрёстков (через 3 м).</summary>
        static List<List<Vector3>> Pieces(List<Vector3> pts, float extra)
        {
            var path = new LanePath("Piece", 1f, pts, false);
            var result = new List<List<Vector3>>();
            List<Vector3> cur = null;
            for (float s = 0f; s <= path.Length; s += 3f)
            {
                var p = path.PointAt(Mathf.Min(s, path.Length));
                if (InJunction(p, extra))
                {
                    if (cur != null && cur.Count > 1) result.Add(cur);
                    cur = null;
                    continue;
                }
                if (cur == null) cur = new List<Vector3>();
                cur.Add(p);
            }
            if (cur != null && cur.Count > 1) result.Add(cur);
            return result;
        }

        /// <summary>Прерывистая линия одной сеткой (сотни штрихов — один объект).</summary>
        static void Dashes(Transform g, List<Vector3> pts, float offset, float dash, float gap)
        {
            var path = new LanePath("Dash", 1f, pts, false);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (float s = 2f; s < path.Length - dash; s += dash + gap)
            {
                var p = path.PointAt(s);
                if (InJunction(p)) continue;
                var t = path.TangentAt(s);
                var r = path.RightAt(s);
                var a = p + r * offset;
                int b = verts.Count;
                verts.Add(a - r * 0.07f);
                verts.Add(a - r * 0.07f + t * dash);
                verts.Add(a + r * 0.07f + t * dash);
                verts.Add(a + r * 0.07f);
                tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            if (verts.Count == 0) return;
            var mesh = new Mesh { name = "Dashes", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MeshFactory.MeshObject("Dashes", g, mesh, new Vector3(0f, 0.045f, 0f), Vector3.zero, Shapes.Mat(PaintWhite));
        }

        /// <summary>Тротуары, фонари и деревья вдоль улицы.</summary>
        static void Sidewalks(Transform g, List<Vector3> pts, float offset, float width, bool lamps, bool trees, bool onlyOneSide = false)
        {
            var sides = onlyOneSide ? new[] { Mathf.Sign(offset) } : new[] { -1f, 1f };
            float off = Mathf.Abs(offset);
            var path = new LanePath("Walk", 1f, pts, false);
            var rnd = new System.Random(pts.Count * 31 + (int)off);
            foreach (float side in sides)
            {
                foreach (var piece in Pieces(pts, 2f))
                    MeshFactory.MeshObject("Sidewalk", g, RaceTrackBuilder.Ribbon(piece, side * off - width / 2f, side * off + width / 2f, 2f), new Vector3(0f, 0.04f, 0f), Vector3.zero, Shapes.Mat(Walk));
                // Бордюр у края дороги
                float curb = off - 4.4f;
                foreach (var piece in Pieces(pts, 0f))
                    MeshFactory.MeshObject("Curb", g, RaceTrackBuilder.Ribbon(piece, side * curb - 0.12f, side * curb + 0.12f, 1f), new Vector3(0f, 0.09f, 0f), Vector3.zero, Shapes.Mat(Curb));
                for (float s = 10f + (side > 0 ? 17f : 0f); s < path.Length; s += 36f)
                {
                    var p = path.PointAt(s);
                    if (InJunction(p, 4f)) continue;
                    var r = path.RightAt(s);
                    if (lamps) Lamp(g, p + r * side * (off - 3.2f), -r * side);
                }
                if (!trees) continue;
                for (float s = 6f; s < path.Length; s += 9f + (float)rnd.NextDouble() * 8f)
                {
                    var p = path.PointAt(s);
                    if (InJunction(p, 6f) || rnd.NextDouble() < 0.3) continue;
                    var r = path.RightAt(s);
                    float lat = off + 3f + (float)rnd.NextDouble() * 9f;
                    Tree(g, p + r * side * lat, rnd);
                }
            }
        }

        /// <summary>Фонарь с изогнутой консолью над дорогой (toRoad — направление к дороге).</summary>
        static void Lamp(Transform g, Vector3 at, Vector3 toRoad)
        {
            float yaw = Mathf.Atan2(toRoad.x, toRoad.z) * Mathf.Rad2Deg;
            var l = Shapes.Group("Lamp", g, at, new Vector3(0f, yaw, 0f));
            Shapes.Make(PrimitiveType.Cylinder, l, new Vector3(0f, 5f, 0f), new Vector3(0.2f, 5f, 0.2f), Metal);
            Shapes.Box(l, new Vector3(0f, 9.9f, 0.9f), new Vector3(0.1f, 0.1f, 1.9f), Metal, new Vector3(-12f, 0f, 0f));
            Shapes.Box(l, new Vector3(0f, 10.1f, 1.9f), new Vector3(0.32f, 0.14f, 0.7f), Shapes.Hex("#d9d6c8"));
            Obstacles.AddBox(at, 0.35f, 0.35f, "фонарный столб");
        }

        static readonly Color[] Leaves =
        {
            Shapes.Hex("#d9a521"), Shapes.Hex("#c9781e"), Shapes.Hex("#b2862f"), Shapes.Hex("#6f8a3a"), Shapes.Hex("#8f6a2a"), Shapes.Hex("#e0b83a"),
        };

        /// <summary>Осеннее дерево: берёза (белый ствол) или тополь/клён.</summary>
        static void Tree(Transform g, Vector3 at, System.Random rnd)
        {
            bool birch = rnd.NextDouble() < 0.35;
            float h = 6f + (float)rnd.NextDouble() * 6f;
            var t = Shapes.Group("Tree", g, at);
            Shapes.Make(PrimitiveType.Cylinder, t, new Vector3(0f, h * 0.35f, 0f), new Vector3(0.28f, h * 0.35f, 0.28f), birch ? Shapes.Hex("#e8e4dc") : Shapes.Hex("#5b4630"));
            var leaf = Leaves[rnd.Next(Leaves.Length)];
            float w = birch ? 2.6f : 3.6f + (float)rnd.NextDouble() * 1.5f;
            Shapes.Make(PrimitiveType.Sphere, t, new Vector3(0f, h * 0.72f, 0f), new Vector3(w, h * 0.55f, w), leaf);
            if (!birch) Shapes.Make(PrimitiveType.Sphere, t, new Vector3(0.6f, h * 0.55f, 0.4f), new Vector3(w * 0.8f, h * 0.35f, w * 0.8f), Leaves[rnd.Next(Leaves.Length)]);
            Obstacles.AddBox(at, 0.5f, 0.5f, "дерево");
        }

        static void Zebra(Transform g, Vector3 at, Vector3 along, float width)
        {
            var across = new Vector3(along.z, 0f, -along.x);
            float yaw = Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg;
            int k = 0;
            for (float x = -width / 2f + 0.5f; x < width / 2f - 0.3f; x += 1.0f, k++)
                Shapes.Box(g, at + across * x + Vector3.up * 0.05f, new Vector3(0.5f, 0.01f, 3.5f), k % 2 == 0 ? Shapes.Hex("#e8d23a") : PaintWhite, new Vector3(0f, yaw, 0f), "Zebra");
        }

        /// <summary>Светофоры на Г-образных опорах над дорогой (у Тополиной), горят зелёным.</summary>
        static void TrafficLights(Transform g, Vector3 c, Vector3 along)
        {
            var across = new Vector3(along.z, 0f, -along.x);
            foreach (float side in new[] { -1f, 1f })
            {
                var at = c - along * side * 16f + across * side * (L.StreetWidth / 2f + 1.5f);
                float yaw = Mathf.Atan2(-across.x * side, -across.z * side) * Mathf.Rad2Deg;
                var pole = Shapes.Group("TrafficLight", g, at, new Vector3(0f, yaw, 0f));
                Shapes.Make(PrimitiveType.Cylinder, pole, new Vector3(0f, 3.5f, 0f), new Vector3(0.22f, 3.5f, 0.22f), Metal);
                Shapes.Box(pole, new Vector3(0f, 6.6f, 3.2f), new Vector3(0.14f, 0.14f, 6.4f), Metal);
                var box = Shapes.Group("Lights", pole, new Vector3(0f, 5.9f, 5.5f));
                Shapes.Box(box, Vector3.zero, new Vector3(0.4f, 1.1f, 0.3f), Shapes.Hex("#1f2124"));
                Shapes.Make(PrimitiveType.Sphere, box, new Vector3(0f, -0.33f, 0.16f * side), new Vector3(0.22f, 0.22f, 0.1f), Shapes.Hex("#38ff5a"));
                Shapes.Box(pole, new Vector3(0f, 6.3f, 1.5f), new Vector3(0.05f, 0.7f, 0.7f), Shapes.Hex("#2a6fd6"), name: "CrossingSign");
                Obstacles.AddBox(at, 0.4f, 0.4f, "светофор");
            }
        }

        // ---------- Кольца ----------

        static void Ring(Transform g, Vector3 c, bool billboards)
        {
            MeshFactory.MeshObject("RingRoad", g, Annulus(L.RingIsland - 0.5f, L.RingOuter, 64), c + Vector3.up * 0.035f, Vector3.zero, asphalt);
            Shapes.Make(PrimitiveType.Cylinder, g, c + Vector3.up * 0.06f, new Vector3(L.RingIsland * 2f, 0.06f, L.RingIsland * 2f), Grass, name: "Island");
            MeshFactory.MeshObject("IslandCurb", g, Annulus(L.RingIsland - 0.3f, L.RingIsland, 64), c + Vector3.up * 0.13f, Vector3.zero, Shapes.Mat(Curb));
            // Остров — твёрдый (по кругу стоят бордюр и знаки), машины его объезжают
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Obstacles.Add(new Obb(c + d * (L.RingIsland - 3f), new Vector3(-d.z, 0f, d.x), 6f, 2f * Mathf.PI * (L.RingIsland - 3f) / 12f + 0.5f), "кольцевой остров");
            }
            if (billboards)
            {
                // Два больших щита на одной опоре (как на скрине кольца)
                var pole = Shapes.Group("Billboards", g, c);
                Shapes.Make(PrimitiveType.Cylinder, pole, new Vector3(0f, 4f, 0f), new Vector3(0.6f, 4f, 0.6f), Metal);
                for (int i = 0; i < 2; i++)
                {
                    var b = Shapes.Group("Board", pole, Vector3.zero, new Vector3(0f, i * 180f + 30f, 0f));
                    Shapes.Box(b, new Vector3(-3.4f, 9f, 0.5f), new Vector3(6.4f, 3.2f, 0.25f), i == 0 ? Shapes.Hex("#2a3d8f") : Shapes.Hex("#e8e8ee"));
                    var t = Fonts.WorldText(b, new Vector3(-3.4f, 9f, 0.35f), i == 0 ? "Свободная касса\nна соседней АЗС*" : "ГОНКА\nпо 70 лет Октября", i == 0 ? Color.white : Shapes.Hex("#c8312b"), 0.08f);
                    t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }
            // Красно-белые стрелки на острове против каждого въезда
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 10f) * Mathf.Deg2Rad;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Chevron(g, c + d * (L.RingIsland - 1.2f), -d);
            }
        }

        static void Chevron(Transform g, Vector3 at, Vector3 face)
        {
            float yaw = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg;
            var s = Shapes.Group("Chevron", g, at, new Vector3(0f, yaw, 0f));
            Shapes.Box(s, new Vector3(0f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.1f), Metal);
            Shapes.Box(s, new Vector3(0f, 1.4f, 0.06f), new Vector3(1.6f, 0.5f, 0.04f), Color.white);
            for (int k = 0; k < 3; k++)
                Shapes.Box(s, new Vector3(-0.5f + k * 0.5f, 1.4f, 0.09f), new Vector3(0.18f, 0.42f, 0.02f), Shapes.Hex("#d22b25"), new Vector3(0f, 0f, 30f));
        }

        static Mesh Annulus(float r0, float r1, int n)
        {
            var verts = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i <= n; i++)
            {
                float a = i * 2f * Mathf.PI / n;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts.Add(d * r0);
                verts.Add(d * r1);
                uv.Add(new Vector2(d.x * r0, d.z * r0) / 4f);
                uv.Add(new Vector2(d.x * r1, d.z * r1) / 4f);
                if (i == 0) continue;
                int b = (i - 1) * 2;
                tris.AddRange(new[] { b, b + 3, b + 1, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = "Annulus" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            // Обход мог дать нормали вниз — тогда разворачиваем треугольники
            if (mesh.normals.Length > 0 && mesh.normals[0].y < 0f)
            {
                tris.Reverse();
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------- Карман (дублёр Льва Яшина) ----------

        static void Pocket(Transform g, RaceTrack track)
        {
            var center = track.CenterPath;
            float s0 = center.Project(L.OnYashina(L.PocketEnterFrom - 15f), out _);
            var pts = new List<Vector3>();
            for (float s = s0; s <= center.Length; s += 3f) pts.Add(center.PointAt(s));
            pts.Add(center.End + L.YashinaDir * 20f);
            Road(g, pts, 0f, L.PocketWidth + 1f, 0.036f);
            // Парковка между Льва Яшина и дублёром: машины ёлочкой
            var dir = L.YashinaDir;
            var park = Line(L.OnYashina(L.PocketEnterTo + 10f, L.ParkingOffset), L.OnYashina(L.PocketEnd - 10f, L.ParkingOffset));
            Road(g, park, 0f, 5f, 0.034f);
            Strip(g, Line(L.OnYashina(L.PocketEnterTo + 5f, 9.2f), L.OnYashina(L.PocketEnd, 9.2f)), 0f, 1.6f, 0.06f, GrassDark, 0f);
            var rnd = new System.Random(5);
            for (float d = L.PocketEnterTo + 16f; d < L.PocketEnd - 14f; d += 3.1f)
            {
                if (rnd.NextDouble() < 0.25) continue;
                var at = L.OnYashina(d, L.ParkingOffset);
                ParkedCar(at, Quaternion.AngleAxis(-35f, Vector3.up) * -L.YashinaLeft, rnd);
            }
            // Тротуар и пандус к магазинам слева
            Strip(g, Line(L.OnYashina(L.PocketEnterTo, L.PocketOffset + 7.5f), L.OnYashina(L.PocketEnd + 20f, L.PocketOffset + 7.5f)), 0f, 6f, 0.05f, Walk, 0f);
            reserved.Add((L.OnYashina(280f, 14f), 160f));
        }

        static void ParkedCar(Vector3 at, Vector3 forward, System.Random rnd)
        {
            var model = (CarModel)rnd.Next(0, 4);
            var paint = CarFactory.Paints[rnd.Next(CarFactory.Paints.Length)];
            var car = CarFactory.Build("Parked", paint, model, false);
            car.transform.SetParent(props, false);
            car.transform.position = at;
            car.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            Obstacles.Add(new Obb(at, forward, car.width, car.length), "припаркованная машина");
        }

        // ---------- ЛЭП на Обводном ----------

        static void Pylons(Transform g)
        {
            var d = L.OfitDir;
            var r = new Vector3(d.z, 0f, -d.x);
            var jy = junctions[0].c;
            Vector3 prev = default;
            for (int i = 0; i < 5; i++)
            {
                var at = jy - d * (60f + i * 120f) + r * 58f;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                var p = Shapes.Group("Pylon", g, at, new Vector3(0f, yaw, 0f));
                // Решётчатая башня: четыре наклонные стойки, поперечины, траверсы
                for (int k = 0; k < 4; k++)
                {
                    float sx = k % 2 == 0 ? -1f : 1f, sz = k < 2 ? -1f : 1f;
                    Shapes.Box(p, new Vector3(sx * 1.6f, 14f, sz * 1.6f), new Vector3(0.25f, 28.5f, 0.25f), Metal, new Vector3(sz * 5f, 0f, -sx * 5f));
                }
                for (int k = 0; k < 6; k++) Shapes.Box(p, new Vector3(0f, 4f + k * 4.5f, 0f), new Vector3(3.6f - k * 0.45f, 0.15f, 3.6f - k * 0.45f), Metal);
                Shapes.Box(p, new Vector3(0f, 22f, 0f), new Vector3(12f, 0.35f, 0.5f), Metal);
                Shapes.Box(p, new Vector3(0f, 27f, 0f), new Vector3(8f, 0.35f, 0.5f), Metal);
                Obstacles.AddBox(at, 4f, 4f, "опора ЛЭП");
                if (i > 0)
                    foreach (var wx in new[] { -5.6f, 5.6f, -3.6f, 3.6f })
                    {
                        float y = Mathf.Abs(wx) > 5f ? 21.6f : 26.6f;
                        var a = prev + r * wx + Vector3.up * y;
                        var b = at + r * wx + Vector3.up * y;
                        var mid = (a + b) / 2f - Vector3.up * 2.5f;
                        Wire(g, a, mid);
                        Wire(g, mid, b);
                    }
                prev = at;
            }
        }

        static void Wire(Transform g, Vector3 a, Vector3 b)
        {
            var w = Shapes.Box(g, (a + b) / 2f, new Vector3(0.05f, 0.05f, Vector3.Distance(a, b)), Shapes.Hex("#2b2b2b"), name: "Wire");
            w.transform.rotation = Quaternion.LookRotation(b - a);
        }

        // ---------- Ориентиры ----------

        static Material Facade(string wall, string glass, int seed) =>
            MeshFactory.Textured(TextureFactory.Facade(Shapes.Hex(wall), Shapes.Hex(glass), seed), "TltFacade" + seed);

        /// <summary>Коробка-дом, повёрнутый вдоль dir; front — сторона, смотрящая на дорогу (для вывески).</summary>
        static Transform House(Transform g, Vector3 center, Vector3 dir, float length, float depth, float height, Material mat, Color roof)
        {
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var t = Shapes.Group("House", g, center, new Vector3(0f, yaw, 0f));
            MeshFactory.MeshObject("Walls", t, MeshFactory.BoxUV(new Vector3(depth, height, length), 12f), new Vector3(0f, height / 2f, 0f), Vector3.zero, mat);
            Shapes.Box(t, new Vector3(0f, height + 0.25f, 0f), new Vector3(depth + 0.3f, 0.5f, length + 0.3f), roof, name: "Roof");
            Obstacles.Add(new Obb(center, dir, depth, length), "дом");
            return t;
        }

        /// <summary>Вывеска на стене: side — сторона дома (−1 — левая, +1 — правая по X дома).</summary>
        static void Sign(Transform house, float side, float depth, float y, float z, string text, Color bg, Color fg, float w, float h, float size)
        {
            var s = Shapes.Group("Sign", house, new Vector3(side * (depth / 2f + 0.15f), y, z), new Vector3(0f, side * -90f, 0f));
            Shapes.Box(s, Vector3.zero, new Vector3(w, h, 0.2f), bg);
            var t = Fonts.WorldText(s, new Vector3(0f, 0f, -0.12f), text, fg, size);
            t.transform.localRotation = Quaternion.identity;
        }

        static void Landmarks(Transform g)
        {
            var d70 = L.Okt70Dir;
            var north70 = new Vector3(-d70.z, 0f, d70.x); // слева по ходу на восток

            // Храм Иоанна Кронштадтского — у первого кольца, северо-восточнее
            Church(g, L.M(705f, 625f), d70);
            reserved.Add((L.M(705f, 625f), 45f));

            // «Миндаль» (зелёный продуктовый) — слева после кольца
            var almond = L.OnOkt70(905f) + north70 * 48f;
            var h = House(g, almond, d70, 46f, 22f, 8f, Shapes.Mat(Shapes.Hex("#1f9a5c")), Roof);
            Sign(h, 1f, 22f, 6f, 6f, "МИНДАЛЬ", Shapes.Hex("#1f9a5c"), Color.white, 18f, 2.6f, 0.2f);
            Sign(h, 1f, 22f, 6f, -14f, "DNS", Shapes.Hex("#f26a1b"), Color.white, 7f, 2.4f, 0.2f);
            reserved.Add((almond, 40f));

            // ТЦ «Бегемот» — справа у Тополиной
            var hippo = L.OnOkt70(1075f) - north70 * 52f;
            h = House(g, hippo, d70, 60f, 26f, 11f, Facade("#e7d9df", "#5a4a6a", 41), Shapes.Hex("#3a3a3e"));
            Sign(h, -1f, 26f, 8.5f, 8f, "БЕГЕМОТ", Shapes.Hex("#d8336a"), Color.white, 20f, 3f, 0.22f);
            Sign(h, -1f, 26f, 8.5f, -18f, "5", Shapes.Hex("#e1251b"), Color.white, 3f, 3f, 0.3f);
            reserved.Add((hippo, 45f));

            // Красный киоск и синяя остановка в начале 70 лет Октября (слева)
            var kiosk = L.OnOkt70(260f) + north70 * 14f;
            House(g, kiosk, d70, 7f, 4f, 3f, Shapes.Mat(Shapes.Hex("#d93a2a")), Shapes.Hex("#2a2a2e"));
            var stop = L.OnOkt70(300f) + north70 * 11.5f;
            var st = House(g, stop, d70, 6f, 2f, 2.6f, Shapes.Mat(Shapes.Hex("#2f6fa8")), Shapes.Hex("#24507a"));

            // Жёлтая «Стоматология» — у второго кольца слева
            var dent = L.OnOkt70(1790f) + north70 * 58f;
            h = House(g, dent, d70, 40f, 20f, 12f, Facade("#e8b830", "#3a4b5c", 43), Roof);
            Sign(h, 1f, 20f, 10.5f, 0f, "СТОМАТОЛОГИЯ", Shapes.Hex("#2b5fb8"), Color.white, 22f, 1.8f, 0.15f);
            reserved.Add((dent, 35f));

            // Заправка у второго кольца (синий навес) — и там очередь
            GasStation(g, L.M(1915f, 912f));

            // Большие магазины за Южным шоссе у старта
            var big = House(g, L.M(985f, 70f), Vector3.forward, 120f, 150f, 14f, Facade("#e6e6e2", "#8a9aa8", 44), Shapes.Hex("#5a5e62"));
            Sign(big, -1f, 150f, 11f, 0f, "ЛЕМАНА ПРО", Shapes.Hex("#2f9e3f"), Color.white, 30f, 4f, 0.3f);
            reserved.Add((L.M(985f, 70f), 110f));
            var big2 = House(g, L.M(1215f, 80f), Vector3.forward, 170f, 110f, 15f, Facade("#ececec", "#8a9aa8", 45), Shapes.Hex("#5a5e62"));
            Sign(big2, -1f, 110f, 11f, 30f, "АШАН", Shapes.Hex("#e2231a"), Color.white, 24f, 4f, 0.3f);
            Sign(big2, -1f, 110f, 11f, -40f, "HOFF", Shapes.Hex("#e2231a"), Color.white, 16f, 4f, 0.3f);
            reserved.Add((L.M(1215f, 80f), 120f));

            // Яркое здание у Офицерской (справа на втором скрине) и кирпичный ряд магазинов у поворота
            var dOf = L.OfitDir;
            var rOf = new Vector3(dOf.z, 0f, -dOf.x);
            var jy = junctions[0].c;
            var colorful = jy - dOf * -0f + dOf * 110f + rOf * 62f;
            ColorfulBlock(g, colorful, dOf);
            reserved.Add((colorful, 50f));
            var brick = L.OfitOkt70 - dOf * 170f + rOf * 52f;
            h = House(g, brick, dOf, 130f, 16f, 8f, Facade("#9a4b35", "#2f3b48", 46), Shapes.Hex("#6a3a2a"));
            for (int i = 0; i < 6; i++)
                Sign(h, -1f, 16f, 5.5f, -55f + i * 20f, i % 2 == 0 ? "АВТОЗАПЧАСТИ" : "ШИНЫ · ДИСКИ", i % 3 == 0 ? Shapes.Hex("#1f4fb0") : Shapes.Hex("#d9541e"), Color.white, 14f, 2.2f, 0.12f);
            reserved.Add((brick, 75f));
            var rnd = new System.Random(9);
            for (int i = 0; i < 14; i++)
            {
                if (rnd.NextDouble() < 0.3) continue;
                var at = brick - rOf * 16f + dOf * (-55f + i * 8f);
                ParkedCar(at, Quaternion.AngleAxis(90f, Vector3.up) * dOf, rnd);
            }

            Madagascar(g);
            Domovoy(g);
        }

        static void Church(Transform g, Vector3 at, Vector3 dir)
        {
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var c = Shapes.Group("Church", g, at, new Vector3(0f, yaw, 0f));
            var white = Shapes.Hex("#f2efe6");
            var gold = Shapes.Hex("#e0b43a");
            Shapes.Box(c, new Vector3(0f, 7f, 0f), new Vector3(18f, 14f, 24f), white);
            Shapes.Box(c, new Vector3(0f, 14.5f, 0f), new Vector3(12f, 1.5f, 12f), Shapes.Hex("#5a7a6a"));
            Shapes.Make(PrimitiveType.Cylinder, c, new Vector3(0f, 17.5f, 0f), new Vector3(7f, 3f, 7f), white);
            Shapes.Make(PrimitiveType.Sphere, c, new Vector3(0f, 22.5f, 0f), new Vector3(7.4f, 8f, 7.4f), gold);
            Shapes.Box(c, new Vector3(0f, 28f, 0f), new Vector3(0.25f, 3f, 0.25f), gold);
            Shapes.Box(c, new Vector3(0f, 28.6f, 0f), new Vector3(1.4f, 0.22f, 0.22f), gold);
            // Колокольня
            Shapes.Box(c, new Vector3(0f, 12f, -17f), new Vector3(7f, 24f, 7f), white);
            Shapes.Make(PrimitiveType.Sphere, c, new Vector3(0f, 26.5f, -17f), new Vector3(4.2f, 5f, 4.2f), gold);
            Shapes.Box(c, new Vector3(0f, 30f, -17f), new Vector3(0.2f, 2.5f, 0.2f), gold);
            foreach (var p in new[] { new Vector3(-6f, 0f, 8f), new Vector3(6f, 0f, 8f), new Vector3(-6f, 0f, -6f), new Vector3(6f, 0f, -6f) })
                Shapes.Make(PrimitiveType.Sphere, c, p + new Vector3(0f, 16f, 0f), new Vector3(2.6f, 3.2f, 2.6f), gold);
            Obstacles.Add(new Obb(at, dir, 18f, 40f), "храм");
        }

        static void ColorfulBlock(Transform g, Vector3 at, Vector3 dir)
        {
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var c = Shapes.Group("Colorful", g, at, new Vector3(0f, yaw, 0f));
            string[] colors = { "#f2c81a", "#e2402a", "#2f7fd6", "#f4f4f0", "#e86a2a", "#7fbf3a" };
            for (int i = 0; i < 10; i++)
                Shapes.Box(c, new Vector3(0f, 6f, -27f + i * 6f), new Vector3(20f, 12f, 6.05f), Shapes.Hex(colors[i % colors.Length]));
            Shapes.Box(c, new Vector3(0f, 12.3f, 0f), new Vector3(20.4f, 0.6f, 60.4f), Roof);
            var t = Fonts.WorldText(c, new Vector3(-10.2f, 10.5f, 10f), "PARKING 24", Color.white, 0.25f);
            t.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Obstacles.Add(new Obb(at, dir, 20f, 60f), "дом");
        }

        static void GasStation(Transform g, Vector3 at)
        {
            var st = Shapes.Group("GasStation", g, at);
            var blue = Shapes.Hex("#1d4fb5");
            foreach (var p in new[] { new Vector3(-8f, 0f, -5f), new Vector3(8f, 0f, -5f), new Vector3(-8f, 0f, 5f), new Vector3(8f, 0f, 5f) })
            {
                Shapes.Box(st, p + Vector3.up * 2.8f, new Vector3(0.5f, 5.6f, 0.5f), Shapes.Hex("#e8e8ee"));
                Obstacles.AddBox(at + p, 0.5f, 0.5f, "опора навеса");
            }
            Shapes.Box(st, new Vector3(0f, 6f, 0f), new Vector3(22f, 1f, 16f), blue);
            var t = Fonts.WorldText(st, new Vector3(0f, 6f, -8.1f), "ГАЗНЕФТЬ", Color.white, 0.18f);
            t.transform.localRotation = Quaternion.identity;
            Shapes.Box(st, new Vector3(0f, 3f, 16f), new Vector3(20f, 6f, 10f), Shapes.Hex("#e9edf2"));
            Obstacles.AddBox(at + new Vector3(0f, 0f, 16f), 20f, 10f, "магазин заправки");
            MeshFactory.MeshObject("Lot", st, MeshFactory.Quad(34f, 40f, new Vector2(4f, 4f)), new Vector3(0f, 0.033f, 4f), Vector3.zero, asphalt);
            // И тут очередь: весь город стоит за бензином
            var rnd = new System.Random(77);
            for (int i = 0; i < 7; i++)
                ParkedCar(at + new Vector3(-14f, 0f, -16f - i * 6f), Vector3.forward, rnd);
            reserved.Add((at, 40f));
        }

        // ---------- ТЦ «Мадагаскар» и «Домовой» ----------

        static void Madagascar(Transform g)
        {
            var dir = L.YashinaDir;
            var left = L.YashinaLeft;
            float depth = 60f, length = 110f, height = 22f;
            float front = L.PocketOffset + 11f; // фасад — за тротуаром
            var center = L.OnYashina(L.FinishDist + 15f, front + depth / 2f);
            var house = House(g, center, dir, length, depth, height, Shapes.Mat(Shapes.Hex("#8fc79a")), Shapes.Hex("#5a6a5a"));
            // Фасад с лемуром — отдельная картинка на передней стене (смотрит на карман)
            var tex = MadagascarFacade();
            var mat = new Material(Shapes.Mat(Color.white)) { name = "Madagascar", mainTexture = tex, color = Color.white };
            var quad = FacadeQuad(length, height);
            var facePos = center - left * (depth / 2f + 0.06f);
            float yaw = Mathf.Atan2(-left.x, -left.z) * Mathf.Rad2Deg;
            // Квад смотрит в −Z локально; разворачиваем лицом к дороге
            MeshFactory.MeshObject("MadagascarFacade", g, quad, facePos, new Vector3(0f, yaw + 180f, 0f), mat);
            // Козырёк над входом
            Shapes.Box(house, new Vector3(-depth / 2f - 1.5f, 4.2f, -8f), new Vector3(3f, 0.3f, 18f), Shapes.Hex("#e6a21a"));
        }

        static Mesh FacadeQuad(float width, float height)
        {
            var mesh = new Mesh { name = "FacadeQuad" };
            mesh.vertices = new[] { new Vector3(-width / 2f, 0f, 0f), new Vector3(-width / 2f, height, 0f), new Vector3(width / 2f, height, 0f), new Vector3(width / 2f, 0f, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Фасад «Мадагаскара» рисуем кодом той же рисовалкой, что и заставку: зелёные панели,
        /// коричневый лемур на хвосте, красные буквы «ТОРГОВЫЙ ЦЕНТР», баннеры и витрины внизу.
        /// </summary>
        static Texture2D MadagascarFacade()
        {
            const int W = 1024, H = 205;
            var r = new Intro.Raster(W, H);
            r.FillAll(Intro.Paints.Linear(0, 0, 0, H, Intro.C4.Hex("#9fd3a8"), Intro.C4.Hex("#7fbf8c")));
            for (int x = 0; x < W; x += 24) r.Fill(Intro.Path.Rect(x, 0, 1.5f, H), Intro.C4.Hex("#6fae7c", 0.7f));
            r.Fill(Intro.Path.Rect(0, 0, W, 8), Intro.C4.Hex("#e6a21a"));
            // Витрины внизу
            r.Fill(Intro.Path.Rect(0, H - 40, W, 40), Intro.C4.Hex("#2c3a44"));
            for (int x = 6; x < W; x += 52) r.Fill(Intro.Path.Rect(x, H - 36, 44, 32), Intro.C4.Hex("#6f9ab0"));
            r.Fill(Intro.Path.Rect(0, H - 50, W, 10), Intro.C4.Hex("#f2f2ee"));
            r.Fill(Intro.Path.Text("МАДАГАСКАР", 380, H - 41, 9.5f, 0.1f), Intro.C4.Hex("#1f7a3a"));
            // Лемур: тело, голова с ушами, длинный хвост спиралью, лапы
            var brown = Intro.C4.Hex("#7a3b2a");
            float lx = 120, ly = 95;
            r.Fill(Intro.Path.Ellipse(lx, ly, 26, 40).Rotate(-25, lx, ly), brown);
            r.Fill(Intro.Path.Circle(lx + 20, ly - 48, 15), brown);
            r.Fill(Intro.Path.Ellipse(lx + 8, ly - 62, 5, 8), brown);
            r.Fill(Intro.Path.Ellipse(lx + 32, ly - 62, 5, 8), brown);
            r.Fill(Intro.Path.Capsule(lx + 10, ly - 20, lx + 60, ly - 40, 5), brown);
            r.Fill(Intro.Path.Capsule(lx - 10, ly - 10, lx - 50, ly - 45, 5), brown);
            r.Fill(Intro.Path.Capsule(lx - 5, ly + 30, lx + 40, ly + 55, 6), brown);
            r.Fill(Intro.Path.Capsule(lx - 15, ly + 30, lx - 40, ly + 60, 6), brown);
            var tail = new List<Intro.V2>();
            for (int i = 0; i <= 60; i++)
            {
                float t = i / 60f, a = t * 9f, rad = 70f * (1f - t * 0.85f);
                tail.Add(new Intro.V2(lx - 10 + 90 * t + Mathf.Cos(a) * rad * 0.35f, ly + 20 - 10 * t + Mathf.Sin(a) * rad * 0.35f));
            }
            r.Fill(Intro.Path.Stroke(tail, 5f), brown);
            // Красные буквы и баннеры
            r.Fill(Intro.Path.Text("ТОРГОВЫЙ ЦЕНТР", 520, 40, 26f, 0.02f), Intro.C4.Hex("#d81f26"));
            r.Fill(Intro.Path.RoundRect(560, 60, 90, 70, 3), Intro.C4.Hex("#f26a1b"));
            r.Fill(Intro.Path.Text("DNS", 572, 105, 30f), Intro.C4.White);
            r.Fill(Intro.Path.RoundRect(670, 70, 60, 60, 3), Intro.C4.Hex("#e8e8f0"));
            r.Fill(Intro.Path.RoundRect(750, 70, 60, 60, 3), Intro.C4.Hex("#d23a3a"));
            r.Fill(Intro.Path.RoundRect(830, 70, 60, 60, 3), Intro.C4.Hex("#4a6ac8"));
            r.Fill(Intro.Path.RoundRect(0, H - 60, 200, 22, 3), Intro.C4.Hex("#3a3a3e"));
            r.Fill(Intro.Path.Text("БЕРИ IPHONE", 14, H - 43, 14f), Intro.C4.White);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "MadagascarFacade", wrapMode = TextureWrapMode.Clamp, anisoLevel = 4 };
            // Только верхний уровень: мелкие копии (mipmaps) Unity досчитает сама в Apply.
            // (LoadRawTextureData ждёт данные сразу для всех уровней — и падал.)
            tex.SetPixelData(r.ToRgba32(true), 0);
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>Жёлтое здание с магазинами («Домовой», «Сбербанк») и панельная башня над ним — по левую руку в кармане.</summary>
        static void Domovoy(Transform g)
        {
            var dir = L.YashinaDir;
            float front = L.PocketOffset + 11f;
            var center = L.OnYashina(225f, front + 10f);
            var h = House(g, center, dir, 90f, 20f, 7f, Shapes.Mat(Shapes.Hex("#f0b43a")), Shapes.Hex("#8a3a2a"));
            // Вывески смотрят на карман (вправо по X дома — к дороге)
            Sign(h, 1f, 20f, 5f, -30f, "ДОМОВОЙ", Shapes.Hex("#1b1d22"), Shapes.Hex("#e0b43a"), 14f, 2f, 0.14f);
            Sign(h, 1f, 20f, 5f, 0f, "МЯСО", Shapes.Hex("#c8312b"), Color.white, 10f, 2f, 0.14f);
            Sign(h, 1f, 20f, 5f, 28f, "СБЕРБАНК", Shapes.Hex("#21a038"), Color.white, 14f, 2f, 0.14f);
            House(g, L.OnYashina(225f, front + 22f), dir, 80f, 14f, 30f, Facade("#e6e2da", "#3a4b5c", 47), Roof);
        }

        // ---------- Панельки ----------

        static void Buildings(Transform g, RaceTrack track)
        {
            var rnd = new System.Random(31);
            Material[] facades =
            {
                Facade("#d9d4c7", "#40566b", 51), Facade("#c9cdd2", "#3a4b5c", 52), Facade("#e1d5b8", "#47586a", 53),
                Facade("#a8553f", "#2f3b48", 54), Facade("#b9c4cc", "#33475a", 55), Facade("#9c4a36", "#3a3f48", 56),
            };
            int[] floors = { 5, 9, 9, 9, 10, 12, 14, 16, 16 };
            // Дома рядами вдоль улиц (первый и второй ряд), как в квартале
            foreach (var st in streets.ToArray())
            {
                var line = st.line;
                foreach (float side in new[] { -1f, 1f })
                    foreach (float row in new[] { 0f, 55f })
                    {
                        float s = 10f + (float)rnd.NextDouble() * 20f;
                        while (s < line.Length - 20f)
                        {
                            bool tower = rnd.NextDouble() < 0.25;
                            float len = tower ? 24f : 40f + (float)rnd.NextDouble() * 70f;
                            float depth = tower ? 24f : 13f;
                            float setback = st.half + 16f + row + (float)rnd.NextDouble() * 14f + depth / 2f;
                            float sMid = s + len / 2f;
                            s += len + 14f + (float)rnd.NextDouble() * 30f;
                            if (sMid > line.Length) break;
                            var dir = line.TangentAt(sMid);
                            var center = line.PointAt(sMid) + line.RightAt(sMid) * side * setback;
                            if (!Clear(center, dir, len, depth, track)) continue;
                            int fl = tower ? 16 : floors[rnd.Next(floors.Length)];
                            House(g, center, dir, len, depth, fl * 2.9f, facades[rnd.Next(facades.Length)], Roof);
                            reserved.Add((center, Mathf.Max(len, depth) * 0.5f));
                        }
                    }
            }
        }

        /// <summary>Дом не залезает на улицы, кольца, трассу и ориентиры.</summary>
        static bool Clear(Vector3 center, Vector3 dir, float len, float depth, RaceTrack track)
        {
            var right = new Vector3(dir.z, 0f, -dir.x);
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var p = center + dir * (i * len / 2f) + right * (j * depth / 2f);
                    foreach (var st in streets)
                    {
                        st.line.Project(p, out float lat);
                        if (Mathf.Abs(lat) < st.half + 8f) return false;
                    }
                    track.CenterPath.Project(p, out float tl);
                    if (Mathf.Abs(tl) < 22f) return false;
                    foreach (var c in new[] { L.Ring1, L.Ring2 })
                        if ((p - c).sqrMagnitude < (L.RingOuter + 25f) * (L.RingOuter + 25f)) return false;
                    foreach (var r in reserved)
                        if ((p - r.c).sqrMagnitude < (r.r + 6f) * (r.r + 6f)) return false;
                }
            // Только на кусочке карты, который мы строим
            var a = L.M(-100f, -250f);
            var b = L.M(2100f, 1050f);
            return center.x > a.x && center.x < b.x && center.z < a.z && center.z > b.z;
        }

        // ---------- Перекрытия, старт, финиш, указатели ----------

        static void Closures(Transform g)
        {
            foreach (var cl in L.Closures()) BlockLine(g, cl.mid, cl.across, cl.width, true);
        }

        /// <summary>Ряд бетонных блоков поперёк улицы (сквозь них не проехать) и транспарант «ПРОЕЗД ЗАКРЫТ».</summary>
        static void BlockLine(Transform g, Vector3 mid, Vector3 across, float width, bool banner)
        {
            var red = Shapes.Mat(RaceTrackBuilder.BlockRed);
            var white = Shapes.Mat(RaceTrackBuilder.BlockWhite);
            var cube = RaceTrackBuilder.BlockMesh();
            float yaw = Mathf.Atan2(across.x, across.z) * Mathf.Rad2Deg;
            int k = 0;
            for (float x = -width / 2f; x < width / 2f - 0.1f; x += 2.4f, k++)
            {
                var p = mid + across * (x + 1.2f);
                var go = MeshFactory.MeshObject("Block", g, cube, p, new Vector3(0f, yaw, 0f), k % 2 == 0 ? red : white);
                go.transform.localScale = new Vector3(0.6f, 0.85f, 2.45f);
            }
            Obstacles.Add(new Obb(mid, across, 0.7f, width), "перекрытие");
            if (!banner) return;
            var b = Shapes.Group("Closed", g, mid, new Vector3(0f, yaw - 90f, 0f));
            Shapes.Box(b, new Vector3(-3.6f, 1.6f, 0f), new Vector3(0.12f, 3.2f, 0.12f), RaceTrackBuilder.Steel);
            Shapes.Box(b, new Vector3(3.6f, 1.6f, 0f), new Vector3(0.12f, 3.2f, 0.12f), RaceTrackBuilder.Steel);
            Shapes.Box(b, new Vector3(0f, 2.4f, 0f), new Vector3(7.4f, 1.3f, 0.08f), Shapes.Hex("#c8312b"));
            foreach (float face in new[] { -1f, 1f })
            {
                var t = Fonts.WorldText(b, new Vector3(0f, 2.4f, 0.06f * face), "ПРОЕЗД ЗАКРЫТ\nидёт гонка", Color.white, 0.045f);
                t.transform.localRotation = Quaternion.Euler(0f, face > 0f ? 180f : 0f, 0f);
            }
        }

        static Renderer[] StartGate(Transform g, RaceTrack track)
        {
            var center = track.CenterPath;
            float s = track.StartLineS;
            var p = center.PointAt(s);
            var t = center.TangentAt(s);
            var right = center.RightAt(s);
            float yaw = Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg;
            float span = L.OfitLanes / 2f + 0.9f;
            var arch = Shapes.Group("StartArch", g, p, new Vector3(0f, yaw, 0f));
            foreach (float side in new[] { -1f, 1f })
            {
                Shapes.Box(arch, new Vector3(side * span, 3.2f, 0f), new Vector3(0.4f, 6.4f, 0.4f), RaceTrackBuilder.Steel);
                Obstacles.Add(new Obb(p + right * side * span, t, 0.4f, 0.4f), "стойка арки");
            }
            Shapes.Box(arch, new Vector3(0f, 6.6f, 0f), new Vector3(span * 2f + 0.4f, 1.3f, 0.35f), Shapes.Hex("#1b1d22"));
            var title = Fonts.WorldText(arch, new Vector3(-2.2f, 6.6f, -0.2f), "СТАРТ · ТОЛЬЯТТИ", Color.white, 0.1f);
            title.transform.localRotation = Quaternion.identity;
            var lights = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var lamp = Shapes.Make(PrimitiveType.Sphere, arch, new Vector3(1.8f + i * 1.1f, 6.6f, -0.25f), new Vector3(0.7f, 0.7f, 0.25f), RaceTrackBuilder.LightOff, name: "StartLight");
                lights[i] = lamp.GetComponent<Renderer>();
            }
            Checkers(arch, span - 0.6f);
            // Позади решётки наша проезжая часть перекрыта
            BlockLine(g, center.PointAt(1f), right, L.OfitLanes + 1f, false);
            // Лампы светофора меняют материал — их не склеиваем
            foreach (var l in lights) l.transform.SetParent(props, true);
            return lights;
        }

        static void Checkers(Transform arch, float half)
        {
            int squares = Mathf.CeilToInt(half * 2f / 0.87f);
            for (int i = 0; i < squares; i++)
                for (int j = 0; j < 2; j++)
                    if ((i + j) % 2 == 0)
                        Shapes.Box(arch, new Vector3(-half + 0.45f + i * 0.87f, 0.05f, j * 0.6f - 0.3f), new Vector3(0.87f, 0.01f, 0.6f), Color.white);
        }

        static void FinishGate(Transform g, RaceTrack track)
        {
            var p = (track.FinishA + track.FinishB) / 2f;
            var t = track.CenterPath.TangentAt(track.FinishS);
            float yaw = Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg;
            float span = L.PocketWidth / 2f + 1.2f;
            var arch = Shapes.Group("FinishArch", g, p, new Vector3(0f, yaw, 0f));
            var right = new Vector3(t.z, 0f, -t.x);
            foreach (float side in new[] { -1f, 1f })
            {
                Shapes.Box(arch, new Vector3(side * span, 3.5f, 0f), new Vector3(0.45f, 7f, 0.45f), RaceTrackBuilder.Steel);
                Obstacles.Add(new Obb(p + right * side * span, t, 0.45f, 0.45f), "стойка финиша");
            }
            Shapes.Box(arch, new Vector3(0f, 7.3f, 0f), new Vector3(span * 2f + 0.5f, 1.5f, 0.4f), Shapes.Hex("#1b1d22"));
            var text = Fonts.WorldText(arch, new Vector3(0f, 7.3f, -0.25f), "ФИНИШ", Color.white, 0.14f);
            text.transform.localRotation = Quaternion.identity;
            Checkers(arch, span - 0.6f);
        }

        /// <summary>Указатели маршрута: перед поворотом на 70 лет Октября, на кольцах и перед карманом.</summary>
        static void TurnSigns(Transform g, RaceTrack track)
        {
            foreach (var h in track.Hints)
            {
                if (h.s >= track.FinishS - 1f) continue; // финиш и так видно — арка
                // Щит заранее, но не на перекрёстке и не на кольце
                Vector3 p = default, t = default, r = default, at = default;
                bool found = false;
                foreach (float lead in new[] { 120f, 80f, 55f })
                {
                    float s = Mathf.Max(0f, h.s - lead);
                    p = track.CenterPath.PointAt(s);
                    t = track.CenterPath.TangentAt(s);
                    r = track.CenterPath.RightAt(s);
                    at = p + r * 8.5f;
                    if (!InJunction(at)) { found = true; break; }
                }
                if (!found) continue;
                var b = Shapes.Group("RouteSign", g, at, new Vector3(0f, Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg, 0f));
                Shapes.Box(b, new Vector3(0f, 1.7f, 0f), new Vector3(0.15f, 3.4f, 0.15f), RaceTrackBuilder.Steel);
                Shapes.Box(b, new Vector3(0f, 3.9f, 0f), new Vector3(5.6f, 1.6f, 0.12f), Shapes.Hex("#1f4fb0"));
                var text = Fonts.WorldText(b, new Vector3(0f, 3.9f, -0.08f), h.text.Replace(" — ", "\n"), Color.white, 0.03f);
                text.transform.localRotation = Quaternion.identity;
                Obstacles.AddBox(at, 0.3f, 0.3f, "указатель");
            }
        }
    }
}

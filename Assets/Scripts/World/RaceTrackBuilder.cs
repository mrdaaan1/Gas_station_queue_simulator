using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Строит трассу гонки (см. <see cref="RaceLayout"/>): асфальт, разметку, красно-белые бетонные блоки
    /// вдоль обочин (твёрдые — в <see cref="Obstacles"/>), стартовую арку со светофором, щит заправки
    /// у последнего поворота и финишную арку на главной дороге.
    /// </summary>
    public static class RaceTrackBuilder
    {
        public class Result
        {
            /// <summary>Три лампы светофора на старте (слева направо).</summary>
            public Renderer[] lights;
        }

        public static readonly Color BlockRed = Shapes.Hex("#c7362f");
        public static readonly Color BlockWhite = Shapes.Hex("#eeeeea");
        static readonly Color Paint = Shapes.Hex("#ecebe4");
        public static readonly Color Steel = Shapes.Hex("#5d6166");
        public static readonly Color LightOff = Shapes.Hex("#2a1210");
        public static readonly Color LightRed = Shapes.Hex("#ff2a1a");
        public static readonly Color LightGreen = Shapes.Hex("#38ff5a");

        public static Result Build(Transform root)
        {
            var g = Shapes.Group("RaceTrack", root);
            var center = RaceLayout.Center();

            var asphalt = MeshFactory.Textured(TextureFactory.Asphalt, "TrackAsphalt");
            MeshFactory.MeshObject("TrackRoad", g, Ribbon(center, -RaceLayout.HalfWidth - 0.6f, RaceLayout.HalfWidth + 0.6f, 4f), new Vector3(0f, 0.012f, 0f), Vector3.zero, asphalt);

            var paint = Shapes.Mat(Paint);
            // Сплошные линии по краям и прерывистая посередине
            foreach (float side in new[] { -1f, 1f })
            {
                float off = side * (RaceLayout.HalfWidth - 1.1f);
                MeshFactory.MeshObject("EdgeLine", g, Ribbon(center, off - 0.08f, off + 0.08f, 1f), new Vector3(0f, 0.022f, 0f), Vector3.zero, paint);
            }
            var dashes = MeshFactory.Quad(0.14f, 3f, Vector2.one);
            var probe = new LanePath("TrackCenter", 1f, center, false);
            for (float s = 4f; s < probe.Length - 2f; s += 9f)
            {
                var p = probe.PointAt(s);
                var t = probe.TangentAt(s);
                MeshFactory.MeshObject("Dash", g, dashes, p + Vector3.up * 0.022f, new Vector3(0f, Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg, 0f), paint);
            }

            BuildBarriers(g, center);
            BuildHouses(g, probe);
            var result = new Result { lights = BuildStart(g, probe) };
            BuildFuelSign(g);
            BuildFinish(root);
            BuildClosure(root);
            BuildRoadSigns(root);
            return result;
        }

        /// <summary>Лента вдоль ломаной от смещения a до b (вправо +), лицом вверх.</summary>
        public static Mesh Ribbon(List<Vector3> center, float a, float b, float uvMeters)
        {
            var left = RaceLayout.Offset(center, a);
            var right = RaceLayout.Offset(center, b);
            var verts = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            float along = 0f;
            for (int i = 0; i < center.Count; i++)
            {
                if (i > 0) along += Vector3.Distance(center[i], center[i - 1]);
                verts.Add(left[i]);
                verts.Add(right[i]);
                uv.Add(new Vector2(a / uvMeters, along / uvMeters));
                uv.Add(new Vector2(b / uvMeters, along / uvMeters));
                if (i == 0) continue;
                int l0 = (i - 1) * 2, r0 = l0 + 1, l1 = i * 2, r1 = l1 + 1;
                tris.Add(l0); tris.Add(l1); tris.Add(r1);
                tris.Add(l0); tris.Add(r1); tris.Add(r0);
            }
            var mesh = new Mesh { name = "Ribbon" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Бетонные блоки по обеим сторонам: через один красный и белый. Сквозь них не проехать.</summary>
        static void BuildBarriers(Transform g, List<Vector3> center)
        {
            var walls = Shapes.Group("Barriers", g);
            var red = Shapes.Mat(BlockRed);
            var white = Shapes.Mat(BlockWhite);
            var cube = BlockMesh();
            // Позади стартовой решётки трасса закрыта поперёк
            var start = center[0];
            var along = (center[1] - center[0]).normalized;
            var across = new Vector3(along.z, 0f, -along.x);
            for (float x = -RaceLayout.HalfWidth; x < RaceLayout.HalfWidth; x += 2.4f)
            {
                var mid = start + across * (x + 1.2f) - along * 0.4f;
                var go = MeshFactory.MeshObject("Block", walls, cube, mid, new Vector3(0f, Mathf.Atan2(across.x, across.z) * Mathf.Rad2Deg, 0f), x < 0f ? red : white);
                go.transform.localScale = new Vector3(0.6f, 0.85f, 2.45f);
                Obstacles.Add(new Obb(mid, across, 0.6f, 2.4f), "бетонный блок");
            }
            foreach (float side in new[] { -1f, 1f })
            {
                var line = new LanePath("Wall", 1f, RaceLayout.Offset(center, side * (RaceLayout.HalfWidth + 0.35f)), false);
                const float block = 2.4f;
                int k = 0;
                for (float s = 0f; s < line.Length - 0.5f; s += block, k++)
                {
                    float len = Mathf.Min(block, line.Length - s);
                    var a = line.PointAt(s);
                    var b = line.PointAt(s + len);
                    var mid = (a + b) / 2f;
                    var dir = b - a;
                    if (dir.sqrMagnitude < 0.01f) continue;
                    float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    var go = MeshFactory.MeshObject("Block", walls, cube, mid, new Vector3(0f, yaw, 0f), k % 2 == 0 ? red : white);
                    go.transform.localScale = new Vector3(0.6f, 0.85f, dir.magnitude + 0.05f);
                    Obstacles.Add(new Obb(mid, dir, 0.6f, dir.magnitude), "бетонный блок");
                }
            }
        }

        /// <summary>Панельки вдоль трассы — гонка всё-таки городская. Ставим только там, где дом не задевает трассу.</summary>
        static void BuildHouses(Transform g, LanePath center)
        {
            var houses = Shapes.Group("TrackHouses", g);
            var rnd = new System.Random(23);
            Material[] facades =
            {
                MeshFactory.Textured(TextureFactory.Facade(Shapes.Hex("#d8cfbf"), Shapes.Hex("#40566b"), 5), "TrackFacade1"),
                MeshFactory.Textured(TextureFactory.Facade(Shapes.Hex("#c9ced3"), Shapes.Hex("#3a4b5c"), 6), "TrackFacade2"),
                MeshFactory.Textured(TextureFactory.Facade(Shapes.Hex("#e1d5b8"), Shapes.Hex("#47586a"), 7), "TrackFacade3"),
            };
            var roof = Shapes.Hex("#4a4a4c");
            var corners = RaceLayout.Corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                var a = corners[i];
                var b = corners[i + 1];
                var dir = (b - a).normalized;
                float len = Vector3.Distance(a, b);
                var right = new Vector3(dir.z, 0f, -dir.x);
                bool alongX = Mathf.Abs(dir.x) > 0.5f;
                foreach (float side in new[] { -1f, 1f })
                {
                    float t = 6f + (float)rnd.NextDouble() * 10f;
                    while (t < len - 10f)
                    {
                        float blockLen = Mathf.Min(28f + (float)rnd.NextDouble() * 34f, len - t);
                        const float depth = 13f;
                        var mid = a + dir * (t + blockLen / 2f) + right * side * (17f + depth / 2f);
                        t += blockLen + 10f + (float)rnd.NextDouble() * 16f;
                        if (blockLen < 14f) continue;
                        float sx = alongX ? blockLen : depth, sz = alongX ? depth : blockLen;
                        if (!Clear(center, mid, sx, sz)) continue;
                        float height = (5 + rnd.Next(0, 10)) * 3f;
                        var mat = facades[rnd.Next(facades.Length)];
                        MeshFactory.MeshObject("Panelka", houses, MeshFactory.BoxUV(new Vector3(sx, height, sz), 12f), mid + Vector3.up * height / 2f, Vector3.zero, mat);
                        Shapes.Box(houses, mid + Vector3.up * (height + 0.3f), new Vector3(sx + 0.3f, 0.6f, sz + 0.3f), roof, name: "Roof");
                        Obstacles.AddBox(mid, sx, sz, "дом");
                    }
                }
            }
        }

        /// <summary>Дом не задевает трассу (с запасом) и не стоит на главной дороге города.</summary>
        static bool Clear(LanePath center, Vector3 mid, float sx, float sz)
        {
            for (int ix = -1; ix <= 1; ix++)
                for (int iz = -1; iz <= 1; iz++)
                {
                    var p = mid + new Vector3(ix * sx / 2f, 0f, iz * sz / 2f);
                    center.Project(p, out float lat);
                    if (Mathf.Abs(lat) < RaceLayout.HalfWidth + 7f) return false;
                    if (p.z > RaceLayout.JoinZ - 18f && p.x > -40f) return false;
                }
            return true;
        }

        static Mesh blockMesh;

        /// <summary>Единичный блок-«нью-джерси»: широкое основание, узкий верх.</summary>
        public static Mesh BlockMesh()
        {
            if (blockMesh != null) return blockMesh;
            var profile = new List<Vector2> { new Vector2(-0.5f, 0f), new Vector2(-0.5f, 0.2f), new Vector2(-0.22f, 0.45f), new Vector2(-0.18f, 1f),
                new Vector2(0.18f, 1f), new Vector2(0.22f, 0.45f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0f) };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            // Боковые грани профиля, вытянутые по Z от −0.5 до 0.5
            for (int i = 0; i < profile.Count - 1; i++)
            {
                var p = profile[i];
                var q = profile[i + 1];
                int b = verts.Count;
                verts.Add(new Vector3(p.x, p.y, -0.5f)); verts.Add(new Vector3(q.x, q.y, -0.5f));
                verts.Add(new Vector3(q.x, q.y, 0.5f)); verts.Add(new Vector3(p.x, p.y, 0.5f));
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }
            // Торцы
            foreach (float z in new[] { -0.5f, 0.5f })
            {
                int b = verts.Count;
                foreach (var p in profile) verts.Add(new Vector3(p.x, p.y, z));
                for (int i = 1; i < profile.Count - 1; i++)
                {
                    if (z < 0f) { tris.Add(b); tris.Add(b + i); tris.Add(b + i + 1); }
                    else { tris.Add(b); tris.Add(b + i + 1); tris.Add(b + i); }
                }
            }
            blockMesh = new Mesh { name = "Block" };
            blockMesh.SetVertices(verts);
            blockMesh.SetTriangles(tris, 0);
            blockMesh.RecalculateNormals();
            blockMesh.RecalculateBounds();
            return blockMesh;
        }

        /// <summary>Стартовая арка с надписью и тремя лампами, шахматная линия на асфальте.</summary>
        static Renderer[] BuildStart(Transform g, LanePath center)
        {
            float s = RaceLayout.StartLineS;
            var p = center.PointAt(s);
            var t = center.TangentAt(s);
            float yaw = Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg;
            var arch = Shapes.Group("StartArch", g, p, new Vector3(0f, yaw, 0f));
            float span = RaceLayout.HalfWidth + 1.2f;
            foreach (float side in new[] { -1f, 1f })
                Shapes.Box(arch, new Vector3(side * span, 3f, 0f), new Vector3(0.4f, 6f, 0.4f), Steel);
            Shapes.Box(arch, new Vector3(0f, 6.3f, 0f), new Vector3(span * 2f + 0.4f, 1.3f, 0.35f), Shapes.Hex("#1b1d22"));
            var title = Fonts.WorldText(arch, new Vector3(-2f, 6.3f, -0.2f), "СТАРТ", Color.white, 0.13f);
            title.transform.localRotation = Quaternion.identity;
            // Светофор: три лампы
            var lights = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var lamp = Shapes.Make(PrimitiveType.Sphere, arch, new Vector3(1.3f + i * 1.1f, 6.3f, -0.25f), new Vector3(0.7f, 0.7f, 0.25f), LightOff, name: "StartLight");
                lights[i] = lamp.GetComponent<Renderer>();
            }
            // Шахматная линия
            int squares = Mathf.CeilToInt(RaceLayout.HalfWidth * 2f / 0.87f) - 1;
            for (int i = 0; i < squares; i++)
                for (int j = 0; j < 2; j++)
                    if ((i + j) % 2 == 0)
                        Shapes.Box(arch, new Vector3(-RaceLayout.HalfWidth + 0.45f + i * 0.87f, 0.02f, j * 0.6f - 0.3f), new Vector3(0.87f, 0.01f, 0.6f), Color.white);
            foreach (float side in new[] { -1f, 1f })
                Obstacles.Add(new Obb(p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(side * span, 0f, 0f), t, 0.4f, 0.4f), "стойка арки");

            // Рекламные щиты на стартовой прямой
            for (int i = 0; i < 3; i++)
            {
                var at = center.PointAt(80f + i * 70f);
                var tt = center.TangentAt(80f + i * 70f);
                var right = new Vector3(tt.z, 0f, -tt.x);
                var board = Shapes.Group("Banner", g, at + right * (RaceLayout.HalfWidth + 1.6f), new Vector3(0f, Mathf.Atan2(tt.x, tt.z) * Mathf.Rad2Deg + 90f, 0f));
                Shapes.Box(board, new Vector3(0f, 1.6f, 0f), new Vector3(7f, 1.6f, 0.15f), i == 1 ? Shapes.Hex("#c8312b") : Shapes.Hex("#1d2a44"));
                var text = Fonts.WorldText(board, new Vector3(0f, 1.6f, -0.1f), i == 1 ? CityBuilder.Brand + " — бензин есть!*" : "САМАЯ БЫСТРАЯ ГОНКА", Color.white, i == 1 ? 0.05f : 0.06f);
                text.transform.localRotation = Quaternion.identity;
            }
            return lights;
        }

        /// <summary>Перед последним поворотом — щит заправки: туда сейчас поедут все.</summary>
        static void BuildFuelSign(Transform g)
        {
            var corner = RaceLayout.Corners[RaceLayout.FuelSignalCorner];
            var board = Shapes.Group("FuelSign", g, corner + new Vector3(9.5f, 0f, -9.5f), new Vector3(0f, 135f, 0f));
            Shapes.Make(PrimitiveType.Cylinder, board, new Vector3(0f, 2f, 0f), new Vector3(0.25f, 2f, 0.25f), Steel);
            Shapes.Box(board, new Vector3(0f, 5f, 0f), new Vector3(6f, 2.6f, 0.2f), Shapes.Hex("#c8312b"));
            var text = Fonts.WorldText(board, new Vector3(0f, 5f, -0.12f), CityBuilder.Brand + "\nзаправка через 500 м", Color.white, 0.05f);
            text.transform.localRotation = Quaternion.identity;
            Obstacles.AddBox(board.position, 0.6f, 0.6f, "щит заправки");
        }

        /// <summary>
        /// Улица перекрыта сразу за въездом на заправку: блоки и высокий забор с транспарантом.
        /// К финишу можно проехать только через территорию заправки и выезд.
        /// </summary>
        static void BuildClosure(Transform root)
        {
            var g = Shapes.Group("RoadClosure", root);
            var red = Shapes.Mat(BlockRed);
            var white = Shapes.Mat(BlockWhite);
            var cube = BlockMesh();
            float z = RaceLayout.ClosureZ, x0 = RaceLayout.ClosureMinX, x1 = RaceLayout.ClosureMaxX;
            int k = 0;
            for (float x = x0; x < x1 - 0.1f; x += 2.4f, k++)
            {
                var go = MeshFactory.MeshObject("Block", g, cube, new Vector3(x + 1.2f, 0f, z), new Vector3(0f, 90f, 0f), k % 2 == 0 ? red : white);
                go.transform.localScale = new Vector3(0.6f, 0.85f, 2.45f);
            }
            Obstacles.AddBox(new Vector3((x0 + x1) / 2f, 0f, z), x1 - x0, 0.7f, "забор «Проезд закрыт»");
            // Забор над блоками: транспаранты на столбах
            for (float x = x0 + 2f; x < x1; x += 8f)
            {
                float w = Mathf.Min(7.6f, x1 - x + 1.6f);
                Shapes.Box(g, new Vector3(x - 1.6f, 1.9f, z + 0.15f), new Vector3(0.12f, 2.2f, 0.12f), Steel);
                Shapes.Box(g, new Vector3(x - 1.6f + w / 2f, 2.1f, z + 0.15f), new Vector3(w, 1.5f, 0.08f), Shapes.Hex("#c8312b"));
                var text = Fonts.WorldText(g, new Vector3(x - 1.6f + w / 2f, 2.1f, z + 0.08f), "ПРОЕЗД ЗАКРЫТ\nвсе на заправку", Color.white, 0.05f);
                text.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>Щиты вдоль главной дороги: сколько до заправки и что заправиться обязательно.</summary>
        static void BuildRoadSigns(Transform root)
        {
            var g = Shapes.Group("RaceSigns", root);
            float[] at = { -420f, -280f, -160f, -80f };
            foreach (float z in at)
            {
                int meters = Mathf.RoundToInt((CityLayout.EntranceMinZ - z) / 10f) * 10;
                var sign = Shapes.Group("FuelSign", g, new Vector3(12.9f, 0f, z));
                Shapes.Box(sign, new Vector3(0f, 1.6f, 0f), new Vector3(0.15f, 3.2f, 0.15f), Steel);
                Shapes.Box(sign, new Vector3(-0.6f, 3.6f, 0f), new Vector3(3.4f, 1.6f, 0.12f), Shapes.Hex("#c8312b"));
                var text = Fonts.WorldText(sign, new Vector3(-0.6f, 3.6f, -0.08f),
                    $"{CityBuilder.Brand}\nзаправка через {meters} м", Color.white, 0.03f);
                text.transform.localRotation = Quaternion.identity;
                Obstacles.AddBox(new Vector3(12.9f, 0f, z), 0.3f, 0.3f, "щит заправки");
            }
            // У самого въезда — над очередью
            var gate = Shapes.Group("EntranceSign", g, new Vector3(11.8f, 0f, CityLayout.EntranceMinZ - 6f));
            Shapes.Box(gate, new Vector3(0f, 2f, 0f), new Vector3(0.18f, 4f, 0.18f), Steel);
            Shapes.Box(gate, new Vector3(-2.2f, 4.6f, 0f), new Vector3(5.2f, 1.5f, 0.12f), Shapes.Hex("#f2c81a"));
            Obstacles.AddBox(gate.position, 0.3f, 0.3f, "щит заправки");
            var t2 = Fonts.WorldText(gate, new Vector3(-2.2f, 4.6f, -0.08f), "ГОНЩИКИ — В ОЧЕРЕДЬ\nна заправку >>>", Shapes.Hex("#1b1d22"), 0.04f);
            t2.transform.localRotation = Quaternion.identity;
        }

        /// <summary>Финишная арка поперёк главной дороги за заправкой.</summary>
        static void BuildFinish(Transform root)
        {
            var arch = Shapes.Group("FinishArch", root, new Vector3(0f, 0f, RaceLayout.FinishZ));
            foreach (float x in new[] { -11.9f, 11.9f })
            {
                Shapes.Box(arch, new Vector3(x, 3.5f, 0f), new Vector3(0.45f, 7f, 0.45f), Steel);
                Obstacles.AddBox(new Vector3(x, 0f, RaceLayout.FinishZ), 0.45f, 0.45f, "стойка финиша");
            }
            Shapes.Box(arch, new Vector3(0f, 7.3f, 0f), new Vector3(24.3f, 1.5f, 0.4f), Shapes.Hex("#1b1d22"));
            var text = Fonts.WorldText(arch, new Vector3(0f, 7.3f, -0.25f), "ФИНИШ", Color.white, 0.16f);
            text.transform.localRotation = Quaternion.identity;
            for (int i = 0; i < 24; i++)
                for (int j = 0; j < 2; j++)
                    if ((i + j) % 2 == 0)
                        Shapes.Box(arch, new Vector3(-10.5f + 0.44f + i * 0.875f, 0.03f, j * 0.6f - 0.3f), new Vector3(0.875f, 0.01f, 0.6f), Color.white);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Строит город: многополосную дорогу с разметкой, тротуары, панельки, фонари, деревья
    /// и заправку на прилегающей территории (въезд, шлагбаум, 4 колонки под навесом, стела, магазин с кассой).
    /// Заодно регистрирует твёрдые препятствия в <see cref="Obstacles"/>.
    /// </summary>
    public static class CityBuilder
    {
        public struct Result
        {
            public Barrier barrier;
            public PriceBoard priceBoard;
            public HumanRig cashier;
        }

        public const string Brand = "ЛУКАВОЙЛ";

        static readonly Color Red = Shapes.Hex("#c8312b");
        static readonly Color White = Shapes.Hex("#f1f0ea");
        static readonly Color Grey = Shapes.Hex("#9a9d9f");
        static readonly Color Dark = Shapes.Hex("#2a2a2e");
        static readonly Color Metal = Shapes.Hex("#7d8287");

        public static Result Build(Transform root)
        {
            Obstacles.Clear();
            LampHeads.Clear();
            SetupLighting(root);
            BuildGround(root);
            BuildStreetFurniture(root);
            BuildBuildings(root);
            BuildCareBanner(root);
            return BuildStation(root);
        }

        static void SetupLighting(Transform root)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            sunGo.transform.rotation = Quaternion.Euler(42f, -50f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.color = Shapes.Hex("#fff1dc");
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            var sky = Resources.Load<Material>("GasQueueGenerated/Sky");
            if (sky != null) RenderSettings.skybox = sky; // небо, которое точно есть в сборке

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Shapes.Hex("#b4c8de");
            RenderSettings.ambientEquatorColor = Shapes.Hex("#9a9a94");
            RenderSettings.ambientGroundColor = Shapes.Hex("#55574f");

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Shapes.Hex("#c9d3dc");
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 520f;
        }

        // ---------- Земля, дорога, тротуары ----------

        static void BuildGround(Transform root)
        {
            var g = Shapes.Group("Ground", root);
            float len = CityLayout.RoadEndZ - CityLayout.RoadStartZ;
            float midZ = (CityLayout.RoadEndZ + CityLayout.RoadStartZ) / 2f;

            Shapes.Make(PrimitiveType.Plane, g, new Vector3(0, -0.03f, midZ), new Vector3(140, 1, 140), Shapes.Hex("#7d7f6c"), name: "Earth");

            var roadMat = MeshFactory.Textured(TextureFactory.Road, "Road");
            MeshFactory.MeshObject("Road", g, MeshFactory.Quad(21f, len, new Vector2(21f, 12f)), new Vector3(0, 0.01f, midZ), Vector3.zero, roadMat);

            var walk = MeshFactory.Textured(TextureFactory.Sidewalk, "Sidewalk");
            var asphalt = MeshFactory.Textured(TextureFactory.Asphalt, "Asphalt");
            var curb = Shapes.Hex("#b7b5ae");

            // Левый тротуар — сплошной
            Strip(g, walk, -12.25f, 3.5f, CityLayout.RoadStartZ, CityLayout.RoadEndZ, 0.05f, new Vector2(2f, 2f));
            Shapes.Box(g, new Vector3(-10.55f, 0.07f, midZ), new Vector3(0.15f, 0.14f, len), curb, name: "Curb");

            // Правый тротуар — с разрывами на въезд и выезд
            float[,] parts = { { CityLayout.RoadStartZ, CityLayout.EntranceMinZ }, { CityLayout.EntranceMaxZ, CityLayout.ExitMinZ }, { CityLayout.ExitMaxZ, CityLayout.RoadEndZ } };
            for (int i = 0; i < 3; i++)
            {
                float z0 = parts[i, 0], z1 = parts[i, 1];
                Strip(g, walk, 12.25f, 3.5f, z0, z1, 0.05f, new Vector2(2f, 2f));
                Shapes.Box(g, new Vector3(10.55f, 0.07f, (z0 + z1) / 2f), new Vector3(0.15f, 0.14f, z1 - z0), curb, name: "Curb");
            }
            // Съезды на заправку
            Strip(g, asphalt, 12.25f, 3.5f, CityLayout.EntranceMinZ, CityLayout.EntranceMaxZ, 0.02f, new Vector2(4f, 4f));
            Strip(g, asphalt, 12.25f, 3.5f, CityLayout.ExitMinZ, CityLayout.ExitMaxZ, 0.02f, new Vector2(4f, 4f));

            // Территория заправки и служебный проезд
            float lotW = CityLayout.LotMaxX - CityLayout.LotMinX, lotL = CityLayout.LotMaxZ - CityLayout.LotMinZ;
            MeshFactory.MeshObject("Lot", g, MeshFactory.Quad(lotW, lotL, new Vector2(4f, 4f)),
                new Vector3((CityLayout.LotMinX + CityLayout.LotMaxX) / 2f, 0.02f, (CityLayout.LotMinZ + CityLayout.LotMaxZ) / 2f), Vector3.zero, asphalt);
            Strip(g, asphalt, CityLayout.TankerLaneX, 5f, CityLayout.RoadStartZ, CityLayout.RoadEndZ, 0.015f, new Vector2(4f, 4f));

            // Разметка на территории: стоп-линия у шлагбаума и стрелки
            var paint = Shapes.Hex("#e8e8e1");
            Shapes.Box(g, new Vector3(20.8f, 0.03f, CityLayout.BarrierZ - 0.6f), new Vector3(6.6f, 0.01f, 0.35f), paint, name: "StopLine");
            Arrow(g, new Vector3(13f, 0.03f, -28f), 50f, paint);
            Arrow(g, new Vector3(20.5f, 0.03f, -7f), 0f, paint);
            Arrow(g, new Vector3(18f, 0.03f, 25f), -45f, paint);
        }

        static void Strip(Transform parent, Material mat, float x, float width, float z0, float z1, float y, Vector2 uv)
        {
            MeshFactory.MeshObject("Strip", parent, MeshFactory.Quad(width, z1 - z0, uv), new Vector3(x, y, (z0 + z1) / 2f), Vector3.zero, mat);
        }

        static void Arrow(Transform parent, Vector3 at, float yaw, Color color)
        {
            var a = Shapes.Group("Arrow", parent, at, new Vector3(0, yaw, 0));
            Shapes.Box(a, new Vector3(0, 0, -0.6f), new Vector3(0.3f, 0.01f, 2.2f), color);
            Shapes.Box(a, new Vector3(-0.35f, 0, 0.55f), new Vector3(0.25f, 0.01f, 1.0f), color, new Vector3(0, 40, 0));
            Shapes.Box(a, new Vector3(0.35f, 0, 0.55f), new Vector3(0.25f, 0.01f, 1.0f), color, new Vector3(0, -40, 0));
        }

        // ---------- Фонари и деревья вдоль дороги ----------

        static void BuildStreetFurniture(Transform root)
        {
            var g = Shapes.Group("Street", root);
            var rnd = new System.Random(11);
            for (float z = CityLayout.RoadStartZ + 10f; z < CityLayout.RoadEndZ; z += 36f)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    if (side > 0 && InStationGap(z, 4f)) continue;
                    Lamp(g, new Vector3(11.3f * side, 0, z), side);
                }
            }
            var trunk = Shapes.Hex("#6b4a2f");
            Color[] leaves = { Shapes.Hex("#4c7a3a"), Shapes.Hex("#3f6b34"), Shapes.Hex("#5d8a43") };
            for (float z = CityLayout.RoadStartZ + 20f; z < CityLayout.RoadEndZ; z += 18f)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    if (side > 0 && (InStationGap(z, 5f) || (z > CityLayout.LotMinZ - 15f && z < CityLayout.LotMaxZ + 5f))) continue;
                    if (side > 0 && Mathf.Abs(z - CityLayout.ShashlikZ) < 12f) continue; // там мангал
                    if (rnd.NextDouble() < 0.2) continue;
                    var pos = new Vector3(13.2f * side, 0, z + (float)rnd.NextDouble() * 4f);
                    float h = 4f + (float)rnd.NextDouble() * 3f;
                    var t = Shapes.Group("Tree", g, pos);
                    Shapes.Make(PrimitiveType.Cylinder, t, new Vector3(0, h * 0.3f, 0), new Vector3(0.3f, h * 0.3f, 0.3f), trunk);
                    Shapes.Make(PrimitiveType.Sphere, t, new Vector3(0, h * 0.78f, 0), new Vector3(h * 0.5f, h * 0.55f, h * 0.5f), leaves[rnd.Next(leaves.Length)]);
                    Obstacles.AddBox(pos, 0.4f, 0.4f, "дерево");
                }
            }
        }

        static bool InStationGap(float z, float margin) =>
            (z > CityLayout.EntranceMinZ - margin && z < CityLayout.EntranceMaxZ + margin) ||
            (z > CityLayout.ExitMinZ - margin && z < CityLayout.ExitMaxZ + margin);

        /// <summary>Плафоны уличных фонарей (мировые координаты) — ночью у ближайших к игроку зажигается свет (<see cref="DayNight"/>).</summary>
        public static readonly List<Vector3> LampHeads = new List<Vector3>();
        /// <summary>Цвет плафона: свой, чтобы ночью светились только плафоны.</summary>
        public static readonly Color LampGlass = Shapes.Hex("#e2dcca");

        static void Lamp(Transform parent, Vector3 at, float side)
        {
            var l = Shapes.Group("Lamp", parent, at);
            Shapes.Make(PrimitiveType.Cylinder, l, new Vector3(0, 4.5f, 0), new Vector3(0.18f, 4.5f, 0.18f), Metal);
            Shapes.Box(l, new Vector3(-1.2f * side, 8.9f, 0), new Vector3(2.4f, 0.1f, 0.12f), Metal);
            Shapes.Box(l, new Vector3(-2.3f * side, 8.8f, 0), new Vector3(0.6f, 0.15f, 0.3f), LampGlass);
            LampHeads.Add(l.position + new Vector3(-2.3f * side, 8.6f, 0f));
            Obstacles.AddBox(at, 0.3f, 0.3f, "фонарный столб");
        }

        // ---------- Панельки ----------

        static void BuildBuildings(Transform root)
        {
            var g = Shapes.Group("Buildings", root);
            var rnd = new System.Random(17);
            Material[] facades =
            {
                MeshFactory.Textured(TextureFactory.Facade(Shapes.Hex("#d8cfbf"), Shapes.Hex("#40566b"), 1), "Facade1"),
                MeshFactory.Textured(TextureFactory.Facade(Shapes.Hex("#c9ced3"), Shapes.Hex("#3a4b5c"), 2), "Facade2"),
                MeshFactory.Textured(TextureFactory.Facade(Shapes.Hex("#e1d5b8"), Shapes.Hex("#47586a"), 3), "Facade3"),
                MeshFactory.Textured(TextureFactory.Facade(Shapes.Hex("#b9c4cc"), Shapes.Hex("#33475a"), 4), "Facade4"),
            };
            var roof = Shapes.Hex("#4a4a4c");

            // Слева — сплошной ряд домов
            Row(g, rnd, facades, roof, -32f, -18f, CityLayout.RoadStartZ, CityLayout.RoadEndZ);
            // Справа — дома до и после заправки, чтобы её было видно издалека
            // Справа до заправки — с разрывом под шашлычную у дороги
            Row(g, rnd, facades, roof, 18f, 32f, CityLayout.RoadStartZ, CityLayout.ShashlikZ - 24f);
            Row(g, rnd, facades, roof, 18f, 32f, CityLayout.ShashlikZ + 24f, -70f);
            Row(g, rnd, facades, roof, 18f, 32f, 70f, CityLayout.RoadEndZ);
            // За служебным проездом
            Row(g, rnd, facades, roof, 52f, 66f, -150f, 160f);
        }

        static void Row(Transform parent, System.Random rnd, Material[] facades, Color roof, float x0, float x1, float z0, float z1)
        {
            float z = z0;
            while (z < z1 - 20f)
            {
                float length = 36f + (float)rnd.NextDouble() * 40f;
                if (z + length > z1) length = z1 - z;
                float floors = 5 + rnd.Next(0, 12);
                float height = floors * 3f;
                float depth = x1 - x0;
                var center = new Vector3((x0 + x1) / 2f, height / 2f, z + length / 2f);
                var mat = facades[rnd.Next(facades.Length)];
                MeshFactory.MeshObject("Panelka", parent, MeshFactory.BoxUV(new Vector3(depth, height, length), 12f), center, Vector3.zero, mat);
                Shapes.Box(parent, new Vector3(center.x, height + 0.3f, center.z), new Vector3(depth + 0.3f, 0.6f, length + 0.3f), roof, name: "Roof");
                Obstacles.AddBox(center, depth, length, "дом");
                z += length + 12f + (float)rnd.NextDouble() * 18f;
            }
        }

        // ---------- Заправка ----------

        static Result BuildStation(Transform root)
        {
            var st = Shapes.Group("GasStation", root);

            BuildFences(st);
            var barrier = BuildBarrier(st);

            // Островки с колонками
            for (int i = 0; i < CityLayout.IslandX.Length; i++)
            {
                float ix = CityLayout.IslandX[i];
                var island = Shapes.Group($"Island{i + 1}", st, new Vector3(ix, 0f, CityLayout.IslandZ));
                Shapes.Box(island, new Vector3(0, 0.1f, 0), new Vector3(1.2f, 0.2f, 6f), Grey, name: "Curb");
                Shapes.Box(island, new Vector3(0, 0.12f, 0), new Vector3(1.0f, 0.22f, 5.8f), Shapes.Hex("#d8d4c8"), name: "Top");
                // Колонка-раздатчик
                Shapes.Box(island, new Vector3(0, 1.05f, 0), new Vector3(0.75f, 1.7f, 1.2f), White, name: "Dispenser");
                Shapes.Box(island, new Vector3(0, 2.0f, 0), new Vector3(0.78f, 0.3f, 1.22f), Red, name: "DispenserTop");
                foreach (float side in new[] { -1f, 1f })
                {
                    int number = i * 2 + (side < 0 ? 1 : 2);
                    Shapes.Box(island, new Vector3(0.38f * side, 1.35f, 0), new Vector3(0.02f, 0.4f, 0.7f), Dark, name: "Display");
                    var label = Fonts.WorldText(island, new Vector3(0.4f * side, 1.95f, 0f), number.ToString(), White, 0.06f);
                    label.transform.localRotation = Quaternion.Euler(0, side < 0 ? 90f : -90f, 0);
                    Shapes.Box(island, new Vector3(0.42f * side, 0.9f, 0.35f), new Vector3(0.08f, 0.25f, 0.1f), Dark, name: "Nozzle");
                    Shapes.Box(island, new Vector3(0.42f * side, 0.9f, -0.35f), new Vector3(0.08f, 0.25f, 0.1f), Shapes.Hex("#2f7d32"), name: "Nozzle");
                }
                // Стойки навеса стоят на островках
                foreach (float dz in new[] { -2.4f, 2.4f })
                    Shapes.Box(island, new Vector3(0, 2.7f, dz), new Vector3(0.35f, 5.2f, 0.35f), White, name: "Pillar");
                Obstacles.AddBox(new Vector3(ix, 0, CityLayout.IslandZ), 1.2f, 6f, "островок колонки");
            }

            // Навес с фирменной полосой
            var canopy = Shapes.Group("Canopy", st, new Vector3(26f, 5.6f, 2f));
            Shapes.Box(canopy, Vector3.zero, new Vector3(18f, 0.4f, 13f), White, name: "Roof");
            Shapes.Box(canopy, new Vector3(0, 0.05f, 0), new Vector3(18.3f, 0.75f, 13.3f), Red, name: "Fascia");
            Shapes.Box(canopy, new Vector3(0, -0.25f, 0), new Vector3(18.35f, 0.12f, 13.35f), White, name: "FasciaStripe");
            var front = Fonts.WorldText(canopy, new Vector3(-1.2f, 0.1f, -6.7f), Brand, White, 0.11f);
            front.fontStyle = FontStyle.Bold;
            Chevrons(canopy, new Vector3(3.6f, 0.1f, -6.68f), 0f, 0.55f);
            var side1 = Fonts.WorldText(canopy, new Vector3(-9.2f, 0.1f, 1.2f), Brand, White, 0.11f);
            side1.fontStyle = FontStyle.Bold;
            side1.transform.localRotation = Quaternion.Euler(0, 90, 0);
            Chevrons(canopy, new Vector3(-9.18f, 0.1f, -3.6f), 90f, 0.55f);

            var board = BuildStela(st);

            // Картонка «БЕНЗИНА НЕТ» у шлагбаума — появляется, когда бензин кончился
            var sold = Shapes.Group("SoldOutSign", st, new Vector3(20.8f, 0f, CityLayout.BarrierZ - 2.6f));
            Shapes.Box(sold, new Vector3(-0.6f, 0.6f, 0), new Vector3(0.05f, 1.2f, 0.05f), Shapes.Hex("#6b4a2f"));
            Shapes.Box(sold, new Vector3(0.6f, 0.6f, 0), new Vector3(0.05f, 1.2f, 0.05f), Shapes.Hex("#6b4a2f"));
            Shapes.Box(sold, new Vector3(0, 1.25f, 0), new Vector3(1.6f, 0.7f, 0.03f), Shapes.Hex("#c9a46a"), new Vector3(0, 0, 3f));
            var soldText = Fonts.WorldText(sold, new Vector3(0, 1.25f, -0.03f), "БЕНЗИНА\nНЕТ!!!", Shapes.Hex("#1a1a1a"), 0.045f);
            soldText.transform.localRotation = Quaternion.Euler(0, 0, 3f);
            board.soldOutSign = sold.gameObject;
            sold.gameObject.SetActive(false);
            var cashier = BuildShop(st);

            // Служебная колонка «для своих» — в дальнем углу, чёрная с золотом
            var vip = Shapes.Group("VipPump", st, CityLayout.VipSpot + new Vector3(0f, 0f, 2.4f));
            Shapes.Box(vip, new Vector3(0, 0.1f, 0), new Vector3(3f, 0.2f, 1.1f), Grey, name: "Island");
            Shapes.Box(vip, new Vector3(0, 1.05f, 0), new Vector3(1.1f, 1.7f, 0.7f), Shapes.Hex("#141416"), name: "Dispenser");
            Shapes.Box(vip, new Vector3(0, 2.0f, 0), new Vector3(1.12f, 0.3f, 0.72f), Shapes.Hex("#c9a23a"), name: "DispenserTop");
            var vipLabel = Fonts.WorldText(vip, new Vector3(0, 1.45f, -0.37f), "СЛУЖЕБНАЯ\nдля своих", Shapes.Hex("#c9a23a"), 0.03f);
            vipLabel.transform.localRotation = Quaternion.Euler(0, 0, 0);
            Obstacles.AddBox(vip.position, 3f, 1.1f, "служебная колонка");

            BuildPropane(st);

            // Указатели въезда и выезда
            Sign(st, new Vector3(12.8f, 0, CityLayout.EntranceMinZ - 2f), "ВЪЕЗД", Shapes.Hex("#1f5fbf"));
            Sign(st, new Vector3(12.8f, 0, CityLayout.ExitMaxZ + 2f), "ВЫЕЗД", Shapes.Hex("#1f5fbf"));

            return new Result { barrier = barrier, priceBoard = board, cashier = cashier };
        }

        /// <summary>
        /// АГЗС «пропан-бутан»: две белые цистерны с красной полосой за сеткой, жёлтые трубы,
        /// синяя колонка под синим полукруглым навесом. Очереди тут нет — на газу мало кто ездит.
        /// </summary>
        static void BuildPropane(Transform st)
        {
            var blue = Shapes.Hex("#1f4fb8");
            var yellow = Shapes.Hex("#e8c21a");
            var tankWhite = Shapes.Hex("#eeeeea");
            var g = Shapes.Group("Propane", st);

            // Площадка
            Shapes.Box(g, new Vector3(34.5f, 0.02f, -27.5f), new Vector3(16f, 0.04f, 15f), Shapes.Hex("#8f8b84"), name: "Pad");

            // Цистерны на синих опорах
            foreach (float z in new[] { -33.6f, -30.9f })
            {
                var tank = Shapes.Group("Tank", g, new Vector3(39.5f, 1.15f, z));
                Shapes.Make(PrimitiveType.Cylinder, tank, Vector3.zero, new Vector3(1.9f, 2.3f, 1.9f), tankWhite, new Vector3(0, 0, 90f), "Body");
                Shapes.Make(PrimitiveType.Sphere, tank, new Vector3(-2.3f, 0, 0), new Vector3(0.9f, 1.9f, 1.9f), tankWhite, name: "Cap");
                Shapes.Make(PrimitiveType.Sphere, tank, new Vector3(2.3f, 0, 0), new Vector3(0.9f, 1.9f, 1.9f), tankWhite, name: "Cap");
                // Полоса и надписи — на стороне, которую видно из очереди (север)
                Shapes.Box(tank, new Vector3(0f, 0f, 0.96f), new Vector3(4.2f, 0.18f, 0.02f), Red, name: "Stripe");
                var label = Fonts.WorldText(tank, new Vector3(0f, 0.35f, 0.97f), "ПРОПАН-БУТАН", Shapes.Hex("#1a1a1a"), 0.045f);
                label.fontStyle = FontStyle.Bold;
                label.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                var warn = Fonts.WorldText(tank, new Vector3(-0.9f, -0.32f, 0.97f), "ОГНЕОПАСНО", Red, 0.022f);
                warn.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                Shapes.Box(tank, new Vector3(0f, 1.0f, 0f), new Vector3(0.6f, 0.25f, 0.6f), Shapes.Hex("#bdbdb6"), name: "Hatch");
                foreach (float x in new[] { -1.6f, 1.6f })
                    Shapes.Box(tank, new Vector3(x, -0.85f, 0f), new Vector3(0.3f, 0.6f, 1.3f), blue, name: "Support");
            }
            // Трубы к колонке
            Shapes.Box(g, new Vector3(35.6f, 0.5f, -30.9f), new Vector3(3.6f, 0.08f, 0.08f), yellow, name: "Pipe");
            Shapes.Box(g, new Vector3(33.8f, 0.5f, -30.65f), new Vector3(0.08f, 0.08f, 0.5f), yellow, name: "Pipe");
            Shapes.Box(g, new Vector3(32.3f, 0.5f, -30.4f), new Vector3(3.0f, 0.08f, 0.08f), Shapes.Hex("#f0f0f0"), name: "Pipe");
            Shapes.Make(PrimitiveType.Cylinder, g, new Vector3(36.1f, 0.85f, -32.2f), new Vector3(0.08f, 0.7f, 0.08f), yellow, name: "Pipe");
            foreach (float x in new[] { 34.6f, 36.6f })
                Shapes.Make(PrimitiveType.Cylinder, g, new Vector3(x, 0.5f, -30.9f), new Vector3(0.22f, 0.04f, 0.22f), Red, new Vector3(0, 0, 90f), "Valve");

            // Сетчатое ограждение вокруг цистерн
            var mesh = Shapes.Hex("#6d7470");
            // Ограда: столбики и тонкие перекладины — цистерны за ней видно
            void Net(Vector3 a, Vector3 b)
            {
                var c = (a + b) / 2f;
                float sx = Mathf.Max(0.04f, Mathf.Abs(b.x - a.x)), sz = Mathf.Max(0.04f, Mathf.Abs(b.z - a.z));
                foreach (float y in new[] { 0.35f, 1.0f, 1.7f })
                    Shapes.Box(g, new Vector3(c.x, y, c.z), new Vector3(sx, 0.04f, sz), mesh, name: "Rail");
                float len = Vector3.Distance(a, b);
                int posts = Mathf.Max(2, Mathf.CeilToInt(len / 1.5f) + 1);
                for (int i = 0; i < posts; i++)
                {
                    var p = Vector3.Lerp(a, b, i / (float)(posts - 1));
                    Shapes.Box(g, new Vector3(p.x, 0.9f, p.z), new Vector3(0.06f, 1.8f, 0.06f), mesh, name: "Post");
                }
                Obstacles.AddBox(c, Mathf.Max(0.2f, sx), Mathf.Max(0.2f, sz), "ограждение цистерн");
            }
            Net(new Vector3(36.4f, 0, -35.4f), new Vector3(36.4f, 0, -29.3f));
            Net(new Vector3(36.4f, 0, -29.3f), new Vector3(42.8f, 0, -29.3f));

            // Колонка и навес
            var d = CityLayout.GasDispenser;
            Shapes.Box(g, new Vector3(d.x, 0.1f, d.z), new Vector3(2.4f, 0.2f, 1.0f), Grey, name: "Island");
            Shapes.Box(g, new Vector3(d.x, 1.0f, d.z), new Vector3(0.8f, 1.6f, 0.55f), blue, name: "GasDispenser");
            Shapes.Box(g, new Vector3(d.x, 1.45f, d.z + 0.28f), new Vector3(0.55f, 0.35f, 0.02f), Shapes.Hex("#16181c"), name: "Display");
            var price = Fonts.WorldText(g, new Vector3(d.x, 1.45f, d.z + 0.3f), "ГАЗ 26.90", Shapes.Hex("#ff5a3c"), 0.016f);
            price.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            Shapes.Box(g, new Vector3(d.x + 0.45f, 0.95f, d.z + 0.1f), new Vector3(0.08f, 0.25f, 0.1f), Dark, name: "Nozzle");
            Obstacles.AddBox(d, 2.4f, 1.0f, "газовая колонка");
            foreach (float x in new[] { d.x - 2.6f, d.x + 2.6f })
            {
                Shapes.Box(g, new Vector3(x, 1.8f, d.z - 0.4f), new Vector3(0.18f, 3.6f, 0.18f), blue, name: "CanopyPost");
                Obstacles.AddBox(new Vector3(x, 0, d.z - 0.4f), 0.25f, 0.25f, "стойка навеса");
            }
            // Полукруглый навес из нескольких наклонных пластин
            for (int i = 0; i < 5; i++)
            {
                float a = -50f + i * 25f;
                float r = 2.4f;
                var pos = new Vector3(d.x, 3.4f + Mathf.Cos(a * Mathf.Deg2Rad) * r * 0.35f, d.z + 1.2f + Mathf.Sin(a * Mathf.Deg2Rad) * r);
                Shapes.Box(g, pos, new Vector3(6.2f, 0.06f, 1.1f), blue, new Vector3(-a * 0.7f, 0, 0), "Canopy");
            }
            var sign = Fonts.WorldText(g, new Vector3(d.x, 4.35f, d.z - 1.3f), "АГЗС  ПРОПАН", Shapes.Hex("#ffffff"), 0.05f);
            sign.fontStyle = FontStyle.Bold;
            Shapes.Box(g, new Vector3(d.x, 4.35f, d.z - 1.25f), new Vector3(4.2f, 0.6f, 0.05f), blue, name: "SignBoard");

            // Оператор АГЗС
            var op = HumanRig.Build("Gas operator", g, new HumanRig.Look
            {
                shirt = blue, pants = Shapes.Hex("#2b2f3a"), skin = Shapes.Hex("#d9a47c"),
                hair = Shapes.Hex("#6b6b6b"), shoes = Shapes.Hex("#1b1b1b"),
            });
            op.transform.position = new Vector3(d.x + 2.0f, 0f, d.z - 1.4f);
            op.transform.rotation = Quaternion.Euler(0, -30f, 0);
            Obstacles.AddBox(op.transform.position, 0.5f, 0.5f, "оператор");
        }

        static void BuildFences(Transform st)
        {
            var fence = Shapes.Hex("#4f5a52");
            var bush = Shapes.Hex("#3f6b34");
            float x0 = CityLayout.LotMinX, x1 = CityLayout.LotMaxX, z0 = CityLayout.LotMinZ, z1 = CityLayout.LotMaxZ;

            void Fence(Vector3 a, Vector3 b)
            {
                var c = (a + b) / 2f;
                float sx = Mathf.Max(0.12f, Mathf.Abs(b.x - a.x)), sz = Mathf.Max(0.12f, Mathf.Abs(b.z - a.z));
                if (sx < 0.2f && sz < 0.2f) return;
                Shapes.Box(st, new Vector3(c.x, 0.5f, c.z), new Vector3(sx, 1f, sz), fence, name: "Fence");
                Obstacles.AddBox(c, sx, sz, "забор", 1f); // метровый — перепрыгивается
            }

            // Вдоль тротуара — с проёмами въезда и выезда
            Fence(new Vector3(x0, 0, z0), new Vector3(x0, 0, CityLayout.EntranceMinZ));
            Fence(new Vector3(x0, 0, CityLayout.EntranceMaxZ), new Vector3(x0, 0, CityLayout.ExitMinZ));
            Fence(new Vector3(x0, 0, CityLayout.ExitMaxZ), new Vector3(x0, 0, z1));
            Fence(new Vector3(x0, 0, z0), new Vector3(x1, 0, z0));
            Fence(new Vector3(x0, 0, z1), new Vector3(x1, 0, z1));
            Fence(new Vector3(x1, 0, z0), new Vector3(x1, 0, z1));

            // Газон-разделитель перед колонками, в нём проезд со шлагбаумом
            float bz = CityLayout.BarrierZ;
            float gapL = CityLayout.BarrierPostX - CityLayout.BarrierArmLength - 0.2f;
            // Второй проём — у магазина: по нему уезжают с газовой колонки
            float gasGap0 = CityLayout.GasExitX - 1.3f;
            foreach (var (a, b) in new[] { (x0, gapL), (CityLayout.BarrierPostX + 0.4f, gasGap0) })
            {
                var c = new Vector3((a + b) / 2f, 0, bz);
                Shapes.Box(st, new Vector3(c.x, 0.12f, bz), new Vector3(b - a, 0.24f, 1.2f), Grey, name: "Divider");
                Shapes.Box(st, new Vector3(c.x, 0.5f, bz), new Vector3(b - a - 0.3f, 0.6f, 0.8f), bush, name: "Bushes");
                Obstacles.AddBox(c, b - a, 1.2f, "газон");
            }
        }

        static Barrier BuildBarrier(Transform st)
        {
            var at = new Vector3(CityLayout.BarrierPostX, 0f, CityLayout.BarrierZ);
            var root = Shapes.Group("Barrier", st, at);
            Shapes.Box(root, new Vector3(0, 0.55f, 0), new Vector3(0.3f, 1.1f, 0.3f), Dark, name: "Post");
            Obstacles.AddBox(at, 0.35f, 0.35f, "стойка шлагбаума");
            var arm = Shapes.Group("ArmPivot", root, new Vector3(0, 1.0f, 0));
            const int stripes = 9;
            float seg = CityLayout.BarrierArmLength / stripes;
            for (int i = 0; i < stripes; i++)
                Shapes.Box(arm, new Vector3(-seg * (i + 0.5f), 0, 0), new Vector3(seg, 0.13f, 0.1f), i % 2 == 0 ? Red : White);
            var barrier = root.gameObject.AddComponent<Barrier>();
            barrier.arm = arm;
            barrier.armLength = CityLayout.BarrierArmLength;
            return barrier;
        }

        static PriceBoard BuildStela(Transform st)
        {
            var at = new Vector3(12.6f, 0f, CityLayout.EntranceMinZ - 10f);
            var s = Shapes.Group("Stela", st, at);
            Shapes.Box(s, new Vector3(0, 0.3f, 0), new Vector3(2.6f, 0.6f, 0.9f), Grey, name: "Base");
            Shapes.Box(s, new Vector3(0, 4.6f, 0), new Vector3(2.2f, 8f, 0.5f), White, name: "Body");
            Shapes.Box(s, new Vector3(0, 7.3f, 0), new Vector3(2.24f, 2.6f, 0.54f), Red, name: "Top");
            var title = Fonts.WorldText(s, new Vector3(0, 7.55f, -0.29f), "ЛУКА\nВОЙЛ", White, 0.09f);
            title.fontStyle = FontStyle.Bold;
            Chevrons(s, new Vector3(0, 6.45f, -0.28f), 0f, 0.35f);
            // Сзади — только логотип, без цен
            var back = Fonts.WorldText(s, new Vector3(0, 7.55f, 0.29f), "ЛУКА\nВОЙЛ", White, 0.09f);
            back.fontStyle = FontStyle.Bold;
            back.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Obstacles.AddBox(at, 2.6f, 0.9f, "стела");

            var rows = new TextMesh[4];
            for (int i = 0; i < 4; i++)
            {
                float y = 5.3f - i * 1.05f;
                Shapes.Box(s, new Vector3(0, y, -0.26f), new Vector3(1.9f, 0.8f, 0.02f), Shapes.Hex("#151515"), name: "PriceRow");
                rows[i] = Fonts.WorldText(s, new Vector3(0, y, -0.28f), "", Shapes.Hex("#ff5a3c"), 0.07f);
            }
            var board = s.gameObject.AddComponent<PriceBoard>();
            board.rows = rows;
            return board;
        }

        /// <summary>Три белых «шеврона» — фирменный знак ЛУКАВОЙЛа (на красном фоне).</summary>
        static void Chevrons(Transform parent, Vector3 at, float yaw, float size)
        {
            var g = Shapes.Group("Chevrons", parent, at, new Vector3(0, yaw, 0));
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * size * 0.55f;
                Shapes.Box(g, new Vector3(x, size * 0.22f, 0), new Vector3(size * 0.14f, size * 0.55f, 0.02f), White, new Vector3(0, 0, -35f), "Chevron");
                Shapes.Box(g, new Vector3(x, -size * 0.22f, 0), new Vector3(size * 0.14f, size * 0.55f, 0.02f), White, new Vector3(0, 0, 35f), "Chevron");
            }
        }

        static void Sign(Transform parent, Vector3 at, string text, Color color)
        {
            var s = Shapes.Group("Sign", parent, at);
            Shapes.Make(PrimitiveType.Cylinder, s, new Vector3(0, 1.3f, 0), new Vector3(0.08f, 1.3f, 0.08f), Metal);
            Shapes.Box(s, new Vector3(0, 2.7f, 0), new Vector3(1.3f, 0.6f, 0.05f), color);
            Fonts.WorldText(s, new Vector3(0, 2.7f, -0.03f), text, White, 0.045f);
            Obstacles.AddBox(at, 0.2f, 0.2f, "знак");
        }

        // ---------- Магазин с кассой ----------

        /// <summary>Листок А4 на скотче, смотрит наружу (к −X), надпись от руки.</summary>
        static void Paper(Transform parent, Vector3 at, float size, string text, float charSize)
        {
            var p = Shapes.Group("Paper", parent, at, new Vector3(0f, 90f, 0f));
            Shapes.Box(p, Vector3.zero, new Vector3(size * 0.7f, size, 0.01f), Shapes.Hex("#fbfbf6"), name: "Sheet");
            Shapes.Box(p, new Vector3(0f, size * 0.5f, -0.006f), new Vector3(size * 0.3f, 0.06f, 0.005f), Shapes.Hex("#e8dfa0"), name: "Tape");
            var t = Fonts.WorldText(p, new Vector3(0f, 0f, -0.012f), text, Shapes.Hex("#1f2a7a"), charSize);
            t.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Банкомат «СБЕРКАССА»: зелёный, с экраном и клавиатурой. Стоит у фасада внутри магазина,
        /// экраном в зал. Тут снимают наличные, потому что терминал на кассе «временно» не работает.
        /// </summary>
        static void BuildAtm(Transform shop)
        {
            var green = Shapes.Hex("#21a038");
            var atm = Shapes.Group("Atm", shop, CityLayout.AtmSpot, new Vector3(0f, 90f, 0f)); // вперёд (+Z) — в зал, к +X
            Shapes.Box(atm, new Vector3(0f, 0.85f, 0f), new Vector3(0.85f, 1.7f, 0.6f), green, name: "Body");
            Shapes.Box(atm, new Vector3(0f, 1.82f, 0f), new Vector3(0.9f, 0.3f, 0.65f), Shapes.Hex("#f2f2ee"), name: "Top");
            var title = Fonts.WorldText(atm, new Vector3(0f, 1.82f, 0.33f), "СБЕРКАССА", green, 0.022f);
            title.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Shapes.Box(atm, new Vector3(0f, 1.35f, 0.29f), new Vector3(0.5f, 0.36f, 0.04f), Shapes.Hex("#1d3a5a"), name: "Screen");
            var screen = Fonts.WorldText(atm, new Vector3(0f, 1.35f, 0.315f), "Вставьте\nкарту", Shapes.Hex("#9fe0b0"), 0.014f);
            screen.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Shapes.Box(atm, new Vector3(0f, 1.02f, 0.33f), new Vector3(0.4f, 0.06f, 0.18f), Shapes.Hex("#3a3a3e"), new Vector3(20f, 0f, 0f), "Keypad");
            Shapes.Box(atm, new Vector3(0f, 0.82f, 0.31f), new Vector3(0.36f, 0.05f, 0.02f), Shapes.Hex("#111111"), name: "CashSlot");
            Obstacles.AddBox(CityLayout.AtmSpot, 0.6f, 0.85f, "банкомат");
        }

        /// <summary>
        /// Растяжки над дорогой: перед заправкой (игрок появляется на Z ≈ −142 — растяжка впереди, на −70)
        /// и после выезда — уже на прощание. Плюс большой щит у самого въезда.
        /// </summary>
        static void BuildCareBanner(Transform root)
        {
            var g = Shapes.Group("CareBanner", root);
            Stretch(g, -70f, Brand + " — С ЗАБОТОЙ О ВАС", null);
            Stretch(g, 150f, Brand + " — СПАСИБО, ЧТО ВЫБРАЛИ НАС", "Выбора у вас всё равно не было");

            // Щит у въезда: смотрит навстречу очереди, видно издалека
            var board = Shapes.Group("CareBillboard", g, new Vector3(18.5f, 0f, -48f));
            foreach (float x in new[] { -3.2f, 3.2f })
            {
                Shapes.Box(board, new Vector3(x, 3f, 0f), new Vector3(0.3f, 6f, 0.3f), Metal);
                Obstacles.AddBox(board.position + new Vector3(x, 0f, 0f), 0.35f, 0.35f, "опора щита");
            }
            Shapes.Box(board, new Vector3(0f, 7.2f, 0f), new Vector3(8.4f, 3.6f, 0.2f), Red, name: "Board");
            Shapes.Box(board, new Vector3(0f, 7.2f, -0.11f), new Vector3(7.9f, 3.1f, 0.02f), White, name: "BoardInner");
            var t1 = Fonts.WorldText(board, new Vector3(0f, 7.9f, -0.13f), Brand, Red, 0.14f);
            t1.transform.localRotation = Quaternion.identity;
            var t2 = Fonts.WorldText(board, new Vector3(0f, 6.6f, -0.13f), "С ЗАБОТОЙ О ВАС", Shapes.Hex("#2a2a2e"), 0.075f);
            t2.transform.localRotation = Quaternion.identity;
        }

        /// <summary>Растяжка поперёк всей дороги на двух столбах, надписи с обеих сторон.</summary>
        static void Stretch(Transform g, float z, string title, string small)
        {
            foreach (float x in new[] { -12.4f, 12.4f })
            {
                Shapes.Make(PrimitiveType.Cylinder, g, new Vector3(x, 4.4f, z), new Vector3(0.22f, 4.4f, 0.22f), Metal);
                Obstacles.AddBox(new Vector3(x, 0f, z), 0.3f, 0.3f, "опора растяжки");
            }
            Shapes.Box(g, new Vector3(0f, 7.6f, z), new Vector3(24.8f, 2.2f, 0.06f), Red, name: "Banner");
            Shapes.Box(g, new Vector3(0f, 6.6f, z), new Vector3(24.8f, 0.2f, 0.07f), White, name: "BannerStripe");
            foreach (float face in new[] { -1f, 1f })
            {
                var t = Fonts.WorldText(g, new Vector3(0f, 7.75f, z + 0.05f * face), title, White, 0.13f);
                t.transform.localRotation = Quaternion.Euler(0f, face > 0f ? 180f : 0f, 0f);
                if (string.IsNullOrEmpty(small)) continue;
                var sm = Fonts.WorldText(g, new Vector3(0f, 7.0f, z + 0.05f * face), small, Shapes.Hex("#ffe0dc"), 0.045f);
                sm.transform.localRotation = Quaternion.Euler(0f, face > 0f ? 180f : 0f, 0f);
            }
        }

        static HumanRig BuildShop(Transform st)
        {
            float x0 = CityLayout.ShopMinX, x1 = CityLayout.ShopMaxX, z0 = CityLayout.ShopMinZ, z1 = CityLayout.ShopMaxZ;
            const float h = 3.6f, t = 0.2f;
            var wall = Shapes.Hex("#ebe6da");
            var glass = Shapes.Hex("#3b5266");
            var shop = Shapes.Group("Shop", st);

            void Wall(float ax, float az, float bx, float bz)
            {
                var c = new Vector3((ax + bx) / 2f, h / 2f, (az + bz) / 2f);
                float sx = Mathf.Max(t, Mathf.Abs(bx - ax)), sz = Mathf.Max(t, Mathf.Abs(bz - az));
                Shapes.Box(shop, c, new Vector3(sx, h, sz), wall, name: "Wall");
                Obstacles.AddBox(new Vector3(c.x, 0, c.z), sx, sz, "стена магазина");
            }

            float d0 = CityLayout.ShopDoorZ - 1f, d1 = CityLayout.ShopDoorZ + 1f;
            Wall(x0, z0, x0, d0); // фасад с дверью
            Wall(x0, d1, x0, z1);
            Wall(x1, z0, x1, z1);
            Wall(x0, z0, x1, z0);
            Wall(x0, z1, x1, z1);
            Shapes.Box(shop, new Vector3(x0, h - 0.3f, CityLayout.ShopDoorZ), new Vector3(t, 0.6f, 2f), wall, name: "Lintel");

            // Витрины, вывеска, крыша
            Shapes.Box(shop, new Vector3(x0 - 0.11f, 1.6f, (z0 + d0) / 2f), new Vector3(0.02f, 1.8f, d0 - z0 - 1f), glass, name: "Window");
            Shapes.Box(shop, new Vector3(x0 - 0.11f, 1.6f, (d1 + z1) / 2f), new Vector3(0.02f, 1.8f, z1 - d1 - 1f), glass, name: "Window");
            Shapes.Box(shop, new Vector3((x0 + x1) / 2f, h + 0.15f, (z0 + z1) / 2f), new Vector3(x1 - x0 + 0.4f, 0.3f, z1 - z0 + 0.4f), Red, name: "Roof");
            var sign = Fonts.WorldText(shop, new Vector3(x0 - 0.15f, h - 0.6f, CityLayout.ShopDoorZ + 4.5f), "КАССА · МАГАЗИН", White, 0.06f);
            sign.transform.localRotation = Quaternion.Euler(0, 90, 0);
            Shapes.Box(shop, new Vector3(x0 - 0.12f, h - 0.6f, CityLayout.ShopDoorZ + 4.5f), new Vector3(0.02f, 0.5f, 4.2f), Red, name: "SignBack");

            // Внутри: пол, прилавок, полки, холодильник, свет
            Shapes.Box(shop, new Vector3((x0 + x1) / 2f, 0.025f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.03f, z1 - z0), Shapes.Hex("#cfc8b8"), name: "Floor");
            var counter = new Vector3(40.3f, 0.5f, CityLayout.CashierSpot.z);
            Shapes.Box(shop, counter, new Vector3(0.7f, 1f, 3.2f), Shapes.Hex("#8a5a3b"), name: "Counter");
            Shapes.Box(shop, counter + new Vector3(0, 0.52f, 0), new Vector3(0.8f, 0.05f, 3.3f), Shapes.Hex("#3a3a3a"), name: "CounterTop");
            Shapes.Box(shop, counter + new Vector3(0, 0.75f, 0.9f), new Vector3(0.3f, 0.4f, 0.35f), Dark, name: "Register");
            Obstacles.AddBox(new Vector3(counter.x, 0, counter.z), 0.7f, 3.2f, "прилавок");
            foreach (float z in new[] { -4.5f, 10.5f })
            {
                var shelf = new Vector3(39.5f, 0.9f, z);
                Shapes.Box(shop, shelf, new Vector3(4.5f, 1.8f, 0.6f), Shapes.Hex("#a8a49a"), name: "Shelf");
                for (int k = 0; k < 8; k++)
                    Shapes.Box(shop, shelf + new Vector3(-2f + k * 0.55f, 0.25f + (k % 3) * 0.45f, -0.35f), new Vector3(0.3f, 0.3f, 0.12f),
                        CarFactory.Paints[k % CarFactory.Paints.Length], name: "Goods");
                Obstacles.AddBox(new Vector3(shelf.x, 0, shelf.z), 4.5f, 0.6f, "полка");
            }
            Shapes.Box(shop, new Vector3(42f, 1f, -1.5f), new Vector3(0.7f, 2f, 1.6f), Shapes.Hex("#d8e6ee"), name: "Fridge");
            Obstacles.AddBox(new Vector3(42f, 0, -1.5f), 0.7f, 1.6f, "холодильник");

            BuildAtm(shop);
            // Бумажки «терминал не работает» — на прилавке и на витрине у входа
            Paper(shop, new Vector3(39.93f, 1.3f, 3.7f), 0.75f, "ТЕРМИНАЛ\nНЕ РАБОТАЕТ\nтолько наличные", 0.012f);
            Paper(shop, new Vector3(x0 - 0.13f, 1.45f, CityLayout.ShopDoorZ - 4.2f), 0.9f, "КАРТЫ\nНЕ ПРИНИМАЕМ!\nбанкомат внутри", 0.014f);

            var lightGo = new GameObject("ShopLight");
            lightGo.transform.SetParent(shop, false);
            lightGo.transform.position = new Vector3(39.5f, 3.2f, 3f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 12f;
            l.intensity = 1.6f;
            l.color = Shapes.Hex("#fff4e0");

            // Кассир за прилавком
            var look = new HumanRig.Look
            {
                shirt = Red,
                pants = Shapes.Hex("#2b2f3a"),
                skin = Shapes.Hex("#f1c7a3"),
                hair = Shapes.Hex("#8b3a1e"),
                shoes = Shapes.Hex("#1b1b1b"),
            };
            var cashier = HumanRig.Build("Cashier", shop, look);
            cashier.transform.position = CityLayout.CashierSpot;
            cashier.transform.rotation = Quaternion.Euler(0, -90f, 0);
            return cashier;
        }
    }
}

using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Строит сцену прототипа: дорога, заправка с колонкой, шлагбаум, табло, деревья, свет.
    /// Дорога идёт вдоль оси Z, колонка в точке Z = 0, очередь стоит в сторону минуса.
    /// </summary>
    public static class WorldBuilder
    {
        public const float PumpZ = 0f;
        public const float QueueLaneX = 0f;
        public const float OppositeLaneX = -4.5f;
        public const float TankerLaneX = 8.5f;

        public struct Result
        {
            public Barrier barrier;
            public PriceBoard priceBoard;
            public float barrierZ;
        }

        public static Result Build(Transform root, float carSpacing)
        {
            SetupLighting(root);

            var ground = Shapes.Group("Ground", root);
            var asphalt = Shapes.Hex("#3b3d40");
            var grass = Shapes.Hex("#6f8f4e");
            var white = Shapes.Hex("#e9e9e2");

            Shapes.Make(PrimitiveType.Plane, ground, new Vector3(0, -0.02f, -100f), new Vector3(120, 1, 120), grass, name: "Grass");
            Shapes.Box(ground, new Vector3(1.75f, 0f, -100f), new Vector3(18.5f, 0.04f, 700f), asphalt, name: "Road");
            Shapes.Box(ground, new Vector3(13f, 0.01f, 2f), new Vector3(14f, 0.04f, 34f), Shapes.Hex("#8d8f8c"), name: "Forecourt");
            for (float z = -440f; z < 240f; z += 8f)
            {
                if (z > -14f && z < 14f) continue; // на заправке разметки нет
                Shapes.Box(ground, new Vector3(-2.25f, 0.025f, z), new Vector3(0.15f, 0.02f, 3.5f), white, name: "Dash");
                Shapes.Box(ground, new Vector3(4.6f, 0.025f, z), new Vector3(0.15f, 0.02f, 3.5f), white, name: "Dash");
            }

            var result = BuildStation(root, carSpacing);
            BuildTrees(root);
            return result;
        }

        static void SetupLighting(Transform root)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = Shapes.Hex("#fff1dc");
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Shapes.Hex("#a9c4e0");
            RenderSettings.ambientEquatorColor = Shapes.Hex("#8f9a88");
            RenderSettings.ambientGroundColor = Shapes.Hex("#4d5243");

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Shapes.Hex("#c4d3df");
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 380f;
        }

        static Result BuildStation(Transform root, float carSpacing)
        {
            var st = Shapes.Group("GasStation", root);
            var red = Shapes.Hex("#c8312b");
            var white = Shapes.Hex("#f1f0ea");
            var grey = Shapes.Hex("#9a9d9f");
            var dark = Shapes.Hex("#2a2a2e");

            // Навес над колонками
            foreach (float x in new[] { -3.4f, 6.6f })
            foreach (float z in new[] { -5f, 5f })
                Shapes.Box(st, new Vector3(x, 2.6f, z), new Vector3(0.4f, 5.2f, 0.4f), white, name: "Pillar");
            Shapes.Box(st, new Vector3(1.6f, 5.4f, 0f), new Vector3(11.5f, 0.5f, 12.5f), white, name: "Canopy");
            Shapes.Box(st, new Vector3(1.6f, 5.4f, 0f), new Vector3(11.6f, 0.25f, 12.6f), red, name: "CanopyStripe");

            // Островок с колонкой справа от машины
            Shapes.Box(st, new Vector3(2.9f, 0.1f, 0f), new Vector3(1.4f, 0.2f, 5.5f), grey, name: "Island");
            var pump = Shapes.Group("Pump", st, new Vector3(2.9f, 0.2f, 0f));
            Shapes.Box(pump, new Vector3(0, 0.8f, 0), new Vector3(0.7f, 1.6f, 1.0f), white);
            Shapes.Box(pump, new Vector3(0, 1.7f, 0), new Vector3(0.72f, 0.25f, 1.02f), red);
            Shapes.Box(pump, new Vector3(-0.36f, 1.1f, 0), new Vector3(0.02f, 0.35f, 0.6f), dark, name: "Display");
            Shapes.Box(pump, new Vector3(-0.4f, 0.7f, 0.3f), new Vector3(0.1f, 0.25f, 0.08f), dark, name: "Nozzle");
            var pumpLabel = Fonts.WorldText(pump, new Vector3(-0.38f, 1.42f, 0f), "95", red, 0.04f);
            pumpLabel.transform.localRotation = Quaternion.Euler(0, 90, 0);

            // Шлагбаум между колонкой и очередью
            float barrierZ = WorldBuilder.PumpZ - carSpacing / 2f;
            var barrierRoot = Shapes.Group("Barrier", st, new Vector3(2.4f, 0f, barrierZ));
            Shapes.Box(barrierRoot, new Vector3(0, 0.55f, 0), new Vector3(0.3f, 1.1f, 0.3f), dark, name: "Post");
            var armPivot = Shapes.Group("ArmPivot", barrierRoot, new Vector3(0, 1.0f, 0));
            const float armLen = 4.8f;
            int stripes = 8;
            for (int i = 0; i < stripes; i++)
            {
                float seg = armLen / stripes;
                Shapes.Box(armPivot, new Vector3(-seg * (i + 0.5f), 0, 0), new Vector3(seg, 0.13f, 0.1f), i % 2 == 0 ? red : white);
            }
            var barrier = barrierRoot.gameObject.AddComponent<Barrier>();
            barrier.arm = armPivot;

            // Касса
            var shop = Shapes.Group("Shop", st, new Vector3(15f, 0f, 3f));
            Shapes.Box(shop, new Vector3(0, 1.6f, 0), new Vector3(6f, 3.2f, 9f), Shapes.Hex("#d9d2c0"));
            Shapes.Box(shop, new Vector3(0, 3.3f, 0), new Vector3(6.4f, 0.3f, 9.4f), red);
            Shapes.Box(shop, new Vector3(-3.01f, 1.5f, 1.5f), new Vector3(0.05f, 1.4f, 3.5f), Shapes.Hex("#33424f"), name: "Window");
            Shapes.Box(shop, new Vector3(-3.01f, 1.1f, -2.4f), new Vector3(0.05f, 2.2f, 1.2f), dark, name: "Door");
            var shopSign = Fonts.WorldText(shop, new Vector3(-3.05f, 2.8f, 0f), "КАССА", white, 0.08f);
            shopSign.transform.localRotation = Quaternion.Euler(0, 90, 0);

            // Табло с ценами — видно из очереди
            var board = Shapes.Group("PriceBoard", st, new Vector3(12.5f, 0f, -16f));
            Shapes.Box(board, new Vector3(0, 2.5f, 0), new Vector3(0.3f, 5f, 0.3f), grey);
            Shapes.Box(board, new Vector3(0, 5.6f, 0), new Vector3(2.8f, 2.4f, 0.25f), dark);
            Shapes.Box(board, new Vector3(0, 7.0f, 0), new Vector3(2.8f, 0.5f, 0.27f), red);
            var boardText = Fonts.WorldText(board, new Vector3(0, 5.6f, -0.14f), "", Shapes.Hex("#ffb52e"), 0.09f);
            var priceBoard = board.gameObject.AddComponent<PriceBoard>();
            priceBoard.text = boardText;
            var title = Fonts.WorldText(board, new Vector3(0, 7.0f, -0.15f), "ТОПЛИВО", white, 0.07f);
            title.fontStyle = FontStyle.Bold;

            return new Result { barrier = barrier, priceBoard = priceBoard, barrierZ = barrierZ };
        }

        static void BuildTrees(Transform root)
        {
            var trees = Shapes.Group("Trees", root);
            var rnd = new System.Random(42);
            var trunk = Shapes.Hex("#6b4a2f");
            Color[] leaves = { Shapes.Hex("#4c7a3a"), Shapes.Hex("#3f6b34"), Shapes.Hex("#5d8a43") };
            for (int i = 0; i < 170; i++)
            {
                float side = rnd.NextDouble() < 0.5 ? -1f : 1f;
                float x = side < 0 ? -12f - (float)rnd.NextDouble() * 70f : 14f + (float)rnd.NextDouble() * 70f;
                float z = -420f + (float)rnd.NextDouble() * 640f;
                if (x > 0f && x < 26f && z > -22f && z < 16f) continue; // не сажаем деревья на заправку
                float h = 3f + (float)rnd.NextDouble() * 4f;
                var t = Shapes.Group("Tree", trees, new Vector3(x, 0, z), new Vector3(0, (float)rnd.NextDouble() * 360f, 0));
                Shapes.Make(PrimitiveType.Cylinder, t, new Vector3(0, h * 0.3f, 0), new Vector3(0.35f, h * 0.3f, 0.35f), trunk);
                var leaf = leaves[rnd.Next(leaves.Length)];
                if (rnd.NextDouble() < 0.5)
                    Shapes.Make(PrimitiveType.Sphere, t, new Vector3(0, h * 0.75f, 0), new Vector3(h * 0.55f, h * 0.6f, h * 0.55f), leaf);
                else
                    Shapes.Box(t, new Vector3(0, h * 0.75f, 0), new Vector3(h * 0.45f, h * 0.55f, h * 0.45f), leaf, new Vector3(0, 45, 0));
            }
        }
    }
}

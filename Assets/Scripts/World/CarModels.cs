using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    public enum CarModel { Vaz2107, Rio, Niva, Gazelle }

    /// <summary>
    /// Машины NPC «по мотивам» того, что стоит в российских очередях: «семёрка», «Рио»-такси, «Нива», «Газель».
    /// Кузов строится как призма по силуэту сбоку (капот, лобовое, багажник), сверху — тонированная кабина.
    /// </summary>
    public static class CarModels
    {
        static readonly Color Tire = Shapes.Hex("#1b1b1b");
        static readonly Color Rim = Shapes.Hex("#9da3a8");
        static readonly Color Chrome = Shapes.Hex("#c4c8cc");
        static readonly Color Black = Shapes.Hex("#1e1e20");
        static readonly Color Plastic = Shapes.Hex("#2b2b2e");
        static readonly Color HeadLight = Shapes.Hex("#fff6d5");
        static readonly Color TailLight = Shapes.Hex("#b5160f");
        static readonly Color Amber = Shapes.Hex("#e08a1e");
        static readonly Color Seat = Shapes.Hex("#3d3530");
        static readonly Color Skin = Shapes.Hex("#e0ac85");
        static readonly Color PlateWhite = Shapes.Hex("#f4f4f0");
        static readonly Color TaxiYellow = Shapes.Hex("#f2c200");

        public static readonly Color[] ClassicPaints =
        {
            Shapes.Hex("#e9e6dc"), Shapes.Hex("#7a1f2b"), Shapes.Hex("#2f5d8a"), Shapes.Hex("#3d6b3a"), Shapes.Hex("#c9a23a"),
            Shapes.Hex("#5a2d5c"), Shapes.Hex("#8a5a2b"), Shapes.Hex("#4f8f8a"), Shapes.Hex("#9a9a92"), Shapes.Hex("#c0392b"),
        };
        public static readonly Color[] ModernPaints =
        {
            Shapes.Hex("#e8e8e8"), Shapes.Hex("#1e1e22"), Shapes.Hex("#8c9196"), Shapes.Hex("#2a3f6b"), Shapes.Hex("#6b1a1a"), Shapes.Hex("#d9d4c7"),
        };

        static readonly string PlateLetters = "АВЕКМНОРСТУХ";
        static readonly string[] Regions = { "77", "97", "177", "199", "777", "50", "750", "190", "799" };

        class Spec
        {
            public float length, halfWidth, wheelR, axleFront, axleRear, track;
            public Vector2[] body, cabin;
            public float bodyTopShrink = 0.03f, cabinBottom, cabinTop;
            public float roofY, seatZ, dashZ;
            public float lightY, bumperY;
        }

        static Spec Get(CarModel m)
        {
            switch (m)
            {
                case CarModel.Rio:
                    return new Spec
                    {
                        length = 4.4f, halfWidth = 0.87f, wheelR = 0.31f, axleFront = 1.32f, axleRear = -1.28f, track = 0.75f,
                        body = new[] { V(-2.2f, 0.32f), V(2.2f, 0.32f), V(2.22f, 0.62f), V(2.05f, 0.8f), V(0.95f, 0.95f), V(-1.55f, 1.0f), V(-2.18f, 0.92f) },
                        cabin = new[] { V(-1.6f, 0.99f), V(0.95f, 0.95f), V(0.05f, 1.46f), V(-0.9f, 1.47f) },
                        cabinBottom = 0.82f, cabinTop = 0.66f, roofY = 1.47f, seatZ = -0.45f, dashZ = 0.55f, lightY = 0.74f, bumperY = 0.42f,
                    };
                case CarModel.Niva:
                    return new Spec
                    {
                        length = 3.74f, halfWidth = 0.84f, wheelR = 0.37f, axleFront = 1.11f, axleRear = -1.11f, track = 0.72f,
                        body = new[] { V(-1.87f, 0.42f), V(1.87f, 0.42f), V(1.88f, 0.98f), V(0.9f, 1.05f), V(-1.87f, 1.07f) },
                        cabin = new[] { V(-1.86f, 1.07f), V(0.9f, 1.05f), V(0.42f, 1.62f), V(-1.84f, 1.63f) },
                        cabinBottom = 0.8f, cabinTop = 0.72f, roofY = 1.63f, seatZ = -0.35f, dashZ = 0.6f, lightY = 0.82f, bumperY = 0.5f,
                    };
                case CarModel.Gazelle:
                    return new Spec
                    {
                        length = 5.5f, halfWidth = 1.0f, wheelR = 0.37f, axleFront = 1.85f, axleRear = -1.35f, track = 0.84f,
                        body = new[] { V(-2.75f, 0.42f), V(2.75f, 0.42f), V(2.78f, 1.05f), V(2.25f, 1.25f), V(-2.75f, 1.27f) },
                        cabin = new[] { V(-2.75f, 1.27f), V(2.25f, 1.25f), V(1.75f, 2.05f), V(1.3f, 2.17f), V(-2.75f, 2.22f) },
                        cabinBottom = 0.98f, cabinTop = 0.92f, roofY = 2.22f, seatZ = 1.05f, dashZ = 1.9f, lightY = 0.95f, bumperY = 0.5f,
                    };
                default: // ВАЗ-2107
                    return new Spec
                    {
                        length = 4.14f, halfWidth = 0.84f, wheelR = 0.3f, axleFront = 1.33f, axleRear = -1.1f, track = 0.71f,
                        // «Коробка»: высокий плоский капот, вертикальный зад, почти прямые стойки
                        body = new[] { V(-2.07f, 0.3f), V(2.07f, 0.3f), V(2.08f, 0.85f), V(1.0f, 0.93f), V(-1.3f, 0.95f), V(-2.08f, 0.93f) },
                        cabin = new[] { V(-1.25f, 0.95f), V(0.98f, 0.93f), V(0.42f, 1.41f), V(-0.95f, 1.42f) },
                        cabinBottom = 0.8f, cabinTop = 0.73f, roofY = 1.43f, seatZ = -0.35f, dashZ = 0.65f, lightY = 0.66f, bumperY = 0.4f,
                    };
            }
        }

        static Vector2 V(float z, float y) => new Vector2(z, y);

        /// <summary>Случайная машина для очереди. В очереди много «семёрок» и такси.</summary>
        public static CarModel Random(out bool taxi)
        {
            float r = UnityEngine.Random.value;
            taxi = false;
            if (r < 0.45f) return CarModel.Vaz2107;
            if (r < 0.7f)
            {
                taxi = UnityEngine.Random.value < 0.35f;
                return CarModel.Rio;
            }
            if (r < 0.9f) return CarModel.Niva;
            return CarModel.Gazelle;
        }

        public static Color RandomPaint(CarModel m, bool taxi)
        {
            if (taxi) return TaxiYellow;
            if (m == CarModel.Gazelle) return UnityEngine.Random.value < 0.7f ? Shapes.Hex("#f2f2ee") : Shapes.Hex("#f2c200");
            var list = m == CarModel.Rio ? ModernPaints : ClassicPaints;
            return list[UnityEngine.Random.Range(0, list.Length)];
        }

        public static CarVisual Build(string name, CarModel model, Color paint, bool taxi)
        {
            var spec = Get(model);
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var body = Shapes.Group("Body", root);
            visual.body = body;
            visual.length = spec.length;
            visual.width = spec.halfWidth * 2f + 0.06f;
            visual.height = spec.roofY;
            visual.driverDoorLocal = new Vector3(-spec.halfWidth - 0.6f, 0f, spec.seatZ);
            visual.fuelCapLocal = new Vector3(spec.halfWidth + 0.03f, spec.lightY + 0.15f, spec.axleRear - 0.2f);

            var paintMat = Shapes.Mat(paint);
            float half = spec.length / 2f;

            // Кузов и кабина
            MeshFactory.MeshObject("Shell", body, MeshFactory.Prism(spec.body, spec.halfWidth, spec.halfWidth - spec.bodyTopShrink),
                Vector3.zero, Vector3.zero, paintMat);
            MeshFactory.MeshObject("Cabin", body, MeshFactory.Prism(spec.cabin, spec.cabinBottom, spec.cabinTop),
                Vector3.zero, Vector3.zero, MeshFactory.TintedGlass);
            Pillars(body, spec, paint);

            // Колёса и арки
            AddWheels(root, visual, spec);

            // Салон: сиденья, торпеда, руль, водитель
            float floor = spec.body[0].y + 0.15f;
            float beltY = spec.cabin[0].y;
            foreach (float x in new[] { -0.36f, 0.36f })
            {
                Shapes.Box(body, new Vector3(x, beltY - 0.25f, spec.seatZ), new Vector3(0.5f, 0.14f, 0.5f), Seat, name: "Seat");
                Shapes.Box(body, new Vector3(x, beltY + 0.08f, spec.seatZ - 0.3f), new Vector3(0.5f, 0.6f, 0.12f), Seat, new Vector3(-10, 0, 0), "SeatBack");
            }
            Shapes.Box(body, new Vector3(0, beltY - 0.05f, spec.dashZ), new Vector3(spec.cabinBottom * 1.9f, 0.2f, 0.35f), Plastic, name: "Dashboard");
            visual.steeringWheel = CarFactory.SteeringWheelFor(body, new Vector3(-0.36f, beltY + 0.05f, spec.dashZ - 0.3f));
            var look = HumanRig.RandomLook();
            visual.driverTorso = Shapes.Box(body, new Vector3(-0.36f, beltY - 0.02f, spec.seatZ - 0.08f), new Vector3(0.44f, 0.5f, 0.26f), look.shirt, name: "Torso").transform;
            visual.driverHead = Shapes.Make(PrimitiveType.Sphere, body, new Vector3(-0.36f, Mathf.Min(beltY + 0.36f, spec.roofY - 0.17f), spec.seatZ - 0.05f),
                Vector3.one * 0.25f, look.skin, name: "Head").transform;
            Shapes.Make(PrimitiveType.Sphere, visual.driverHead, new Vector3(0, 0.25f, -0.08f), new Vector3(1.03f, 0.75f, 1.03f), look.hair, name: "Hair");

            // Лицо машины: решётка, фары, бамперы, фонари, номера
            switch (model)
            {
                case CarModel.Vaz2107: Front2107(body, visual, spec); break;
                case CarModel.Rio: FrontRio(body, visual, spec, paint); break;
                case CarModel.Niva: FrontNiva(body, visual, spec); break;
                case CarModel.Gazelle: FrontGazelle(body, visual, spec); break;
            }
            Rear(body, visual, spec, model);
            Plates(body, spec);
            SideDetails(body, spec, model);
            if (model == CarModel.Vaz2107) Extras2107(body, spec);
            if (model == CarModel.Niva) ExtrasNiva(body, spec);
            if (model == CarModel.Rio) ExtrasRio(body, spec);

            if (taxi) TaxiDress(body, spec);
            if (model == CarModel.Gazelle && UnityEngine.Random.value < 0.6f) RouteSign(body, spec);
            return visual;
        }

        // ---------- Части ----------

        /// <summary>Стойки кабины цвета кузова и крыша — чтобы стекло было «в рамке».</summary>
        static void Pillars(Transform body, Spec s, Color paint)
        {
            var c = s.cabin;
            int n = c.Length;
            // Нижний задний, нижний передний, верх передний ... — определяем передний и задний верх по z
            Vector2 baseFront = c[1], baseRear = c[0];
            Vector2 topFront = c[2], topRear = c[n - 1];
            float side = s.cabinTop + 0.01f;
            foreach (float sign in new[] { -1f, 1f })
            {
                float xb = sign * (s.cabinBottom + 0.01f), xt = sign * side;
                Beam(body, new Vector3(xb, baseFront.y, baseFront.x), new Vector3(xt, topFront.y, topFront.x), 0.07f, paint, "APillar");
                Beam(body, new Vector3(xb, baseRear.y, baseRear.x), new Vector3(xt, topRear.y, topRear.x), 0.12f, paint, "CPillar");
                float bz = (baseFront.x + baseRear.x) / 2f - 0.1f;
                Beam(body, new Vector3(xb, baseRear.y, bz), new Vector3(xt, topRear.y, bz), 0.08f, paint, "BPillar");
                Beam(body, new Vector3(xt, topRear.y, topRear.x), new Vector3(xt, topFront.y, topFront.x), 0.06f, paint, "RoofRail");
            }
            // Крыша
            var roofProfile = new[] { V(topRear.x, topRear.y - 0.01f), V(topFront.x, topFront.y - 0.01f), V(topFront.x - 0.02f, topFront.y + 0.03f), V(topRear.x + 0.02f, topRear.y + 0.03f) };
            MeshFactory.MeshObject("Roof", body, MeshFactory.Prism(roofProfile, s.cabinTop + 0.02f, s.cabinTop), Vector3.zero, Vector3.zero, Shapes.Mat(paint));
        }

        /// <summary>Брусок между двумя точками.</summary>
        public static Transform Beam(Transform parent, Vector3 a, Vector3 b, float thickness, Color color, string name)
        {
            var go = Shapes.Box(parent, (a + b) / 2f, new Vector3(thickness, thickness, Vector3.Distance(a, b)), color, name: name);
            go.transform.localRotation = Quaternion.LookRotation(b - a, Vector3.up);
            return go.transform;
        }

        static void AddWheels(Transform root, CarVisual visual, Spec s)
        {
            float track = s.halfWidth - 0.04f; // колёса чуть выступают из кузова
            foreach (float x in new[] { -track, track })
            foreach (float z in new[] { s.axleRear, s.axleFront })
            {
                Transform mount = root;
                var pos = new Vector3(x, s.wheelR, z);
                if (z > 0f)
                {
                    mount = Shapes.Group("SteerPivot", root, pos);
                    visual.frontSteer.Add(mount);
                    pos = Vector3.zero;
                }
                var pivot = Shapes.Group("Wheel", mount, pos, new Vector3(0, 0, 90));
                float d = s.wheelR * 2f;
                Shapes.Make(PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(d, 0.11f, d), Tire);
                Shapes.Make(PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(d * 0.58f, 0.115f, d * 0.58f), Rim);
                Shapes.Box(pivot, Vector3.zero, new Vector3(0.07f, 0.12f, d * 0.75f), Shapes.Hex("#6c7176"));
                visual.wheels.Add(pivot);
                // Тёмная «ниша» колёсной арки на боку кузова
                float side = Mathf.Sign(x) * (s.halfWidth + 0.004f);
                Shapes.Box(root.Find("Body"), new Vector3(side, s.wheelR + s.wheelR * 0.55f, z), new Vector3(0.01f, s.wheelR * 0.9f, d * 1.2f), Black, name: "WheelWell");
            }
        }

        static void Front2107(Transform body, CarVisual v, Spec s)
        {
            float z = s.length / 2f;
            // Хромированная решётка с горизонтальными планками
            Shapes.Box(body, new Vector3(0, 0.66f, z + 0.015f), new Vector3(0.78f, 0.24f, 0.03f), Chrome, name: "Grille");
            for (int i = 0; i < 4; i++)
                Shapes.Box(body, new Vector3(0, 0.57f + i * 0.06f, z + 0.035f), new Vector3(0.74f, 0.015f, 0.01f), Black, name: "GrilleBar");
            // Прямоугольные фары с оранжевыми поворотниками по краям
            foreach (float x in new[] { -0.58f, 0.58f })
            {
                Shapes.Box(body, new Vector3(x, 0.66f, z + 0.01f), new Vector3(0.36f, 0.22f, 0.04f), Chrome, name: "LampFrame");
                v.headlights.Add(Shapes.Box(body, new Vector3(x, 0.66f, z + 0.03f), new Vector3(0.3f, 0.17f, 0.02f), HeadLight, name: "Headlight").transform);
            }
            Blinkers(body, v, s, new Vector3(0.79f, 0.66f, z + 0.02f), new Vector3(0.79f, 0.68f, -z - 0.02f));
            // Бампер: хром + чёрные клыки
            v.bumperFront = Shapes.Box(body, new Vector3(0, s.bumperY, z + 0.06f), new Vector3(1.72f, 0.11f, 0.08f), Chrome, name: "BumperFront").transform;
            foreach (float x in new[] { -0.45f, 0.45f })
                Shapes.Box(v.bumperFront, new Vector3(x / 1.72f, 0f, 0.6f), new Vector3(0.07f, 1.4f, 1f), Black, name: "Fang");
        }

        static void FrontRio(Transform body, CarVisual v, Spec s, Color paint)
        {
            float z = s.length / 2f;
            Shapes.Box(body, new Vector3(0, 0.6f, z + 0.03f), new Vector3(0.62f, 0.14f, 0.03f), Black, name: "Grille");
            Shapes.Box(body, new Vector3(0, 0.4f, z + 0.03f), new Vector3(0.9f, 0.12f, 0.03f), Black, name: "Intake");
            foreach (float x in new[] { -0.62f, 0.62f })
                v.headlights.Add(Shapes.Box(body, new Vector3(x, s.lightY, z - 0.06f), new Vector3(0.38f, 0.1f, 0.12f), HeadLight,
                    new Vector3(0, x > 0 ? -18f : 18f, 0), "Headlight").transform);
            Blinkers(body, v, s, new Vector3(0.78f, s.lightY, z - 0.12f), new Vector3(0.74f, 0.86f, -z + 0.02f));
            // Бампер в цвет кузова
            v.bumperFront = Shapes.Box(body, new Vector3(0, s.bumperY, z + 0.02f), new Vector3(1.74f, 0.2f, 0.08f), paint, name: "BumperFront").transform;
        }

        static void FrontNiva(Transform body, CarVisual v, Spec s)
        {
            float z = s.length / 2f;
            Shapes.Box(body, new Vector3(0, 0.82f, z + 0.015f), new Vector3(1.1f, 0.22f, 0.03f), Black, name: "Grille");
            foreach (float x in new[] { -0.6f, 0.6f })
            {
                var lamp = Shapes.Make(PrimitiveType.Cylinder, body, new Vector3(x, s.lightY, z + 0.02f), new Vector3(0.2f, 0.02f, 0.2f), HeadLight,
                    new Vector3(90, 0, 0), "Headlight");
                v.headlights.Add(lamp.transform);
                Shapes.Make(PrimitiveType.Cylinder, body, new Vector3(x, s.lightY, z + 0.01f), new Vector3(0.24f, 0.015f, 0.24f), Chrome, new Vector3(90, 0, 0), "LampRing");
            }
            Blinkers(body, v, s, new Vector3(0.6f, 0.62f, z + 0.02f), new Vector3(0.78f, 0.75f, -z - 0.02f));
            v.bumperFront = Shapes.Box(body, new Vector3(0, s.bumperY, z + 0.06f), new Vector3(1.7f, 0.14f, 0.1f), Black, name: "BumperFront").transform;
        }

        static void FrontGazelle(Transform body, CarVisual v, Spec s)
        {
            float z = s.length / 2f;
            Shapes.Box(body, new Vector3(0, 0.82f, z + 0.03f), new Vector3(0.9f, 0.3f, 0.03f), Plastic, name: "Grille");
            foreach (float x in new[] { -0.72f, 0.72f })
                v.headlights.Add(Shapes.Box(body, new Vector3(x, s.lightY, z + 0.02f), new Vector3(0.36f, 0.2f, 0.04f), HeadLight, name: "Headlight").transform);
            Blinkers(body, v, s, new Vector3(0.95f, s.lightY, z), new Vector3(0.95f, 0.75f, -z - 0.02f));
            v.bumperFront = Shapes.Box(body, new Vector3(0, s.bumperY, z + 0.06f), new Vector3(2.02f, 0.2f, 0.1f), Plastic, name: "BumperFront").transform;
        }

        static void Rear(Transform body, CarVisual v, Spec s, CarModel model)
        {
            float z = -s.length / 2f;
            float tailY = model == CarModel.Gazelle ? 0.75f : model == CarModel.Rio ? 0.86f : s.lightY + 0.04f;
            float tailW = model == CarModel.Vaz2107 ? 0.5f : 0.3f;
            foreach (float x in new[] { -1f, 1f })
            {
                float px = x * (s.halfWidth - tailW / 2f - 0.04f);
                v.taillights.Add(Shapes.Box(body, new Vector3(px, tailY, z - 0.015f), new Vector3(tailW, 0.18f, 0.03f), TailLight, name: "Taillight").transform);
                if (model == CarModel.Vaz2107)
                    Shapes.Box(body, new Vector3(px, tailY - 0.06f, z - 0.03f), new Vector3(tailW, 0.05f, 0.01f), Amber, name: "TailAmber");
            }
            var bumperColor = model == CarModel.Vaz2107 ? Chrome : model == CarModel.Rio ? Shapes.Hex("#2b2b2e") : Black;
            v.bumperRear = Shapes.Box(body, new Vector3(0, s.bumperY, z - 0.06f), new Vector3(s.halfWidth * 2f + 0.02f, 0.13f, 0.08f), bumperColor, name: "BumperRear").transform;
        }

        static void Blinkers(Transform body, CarVisual v, Spec s, Vector3 front, Vector3 rear)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                var f = Shapes.Box(body, new Vector3(front.x * side, front.y, front.z), new Vector3(0.09f, 0.08f, 0.04f), Shapes.Hex("#7a4a10"), name: "Blinker");
                var r = Shapes.Box(body, new Vector3(rear.x * side, rear.y, rear.z), new Vector3(0.09f, 0.08f, 0.04f), Shapes.Hex("#7a4a10"), name: "Blinker");
                var list = side < 0 ? v.leftBlinkers : v.rightBlinkers;
                list.Add(f.GetComponent<Renderer>());
                list.Add(r.GetComponent<Renderer>());
            }
        }

        /// <summary>Номер в российском формате: «А123ВС 77».</summary>
        public static string RandomPlate()
        {
            char L() => PlateLetters[UnityEngine.Random.Range(0, PlateLetters.Length)];
            return $"{L()}{UnityEngine.Random.Range(1, 1000):000}{L()}{L()} {Regions[UnityEngine.Random.Range(0, Regions.Length)]}";
        }

        static void Plates(Transform body, Spec s) => Plates(body, s.length, s.bumperY, RandomPlate());

        public static void Plates(Transform body, float length, float bumperY, string number)
        {
            float half = length / 2f;
            foreach (float end in new[] { 1f, -1f })
            {
                float y = end > 0 ? bumperY + 0.12f : bumperY + 0.2f;
                var plate = Shapes.Group("Plate", body, new Vector3(0, y, (half + 0.08f) * end), new Vector3(0, end > 0 ? 180f : 0f, 0));
                Shapes.Box(plate, Vector3.zero, new Vector3(0.52f, 0.12f, 0.015f), PlateWhite);
                Shapes.Box(plate, new Vector3(0.21f, 0, -0.009f), new Vector3(0.002f, 0.1f, 0.002f), Black, name: "RegionLine");
                Fonts.WorldText(plate, new Vector3(-0.03f, 0, -0.01f), number, Black, 0.0105f);
            }
        }

        static void SideDetails(Transform body, Spec s, CarModel model)
        {
            float side = s.halfWidth + 0.005f;
            float y = model == CarModel.Gazelle ? 0.85f : s.body[0].y + 0.3f;
            foreach (float sign in new[] { -1f, 1f })
            {
                // Молдинг
                Shapes.Box(body, new Vector3(sign * side, y, 0), new Vector3(0.015f, 0.05f, s.length * 0.75f), Black, name: "Molding");
                // Ручки дверей
                float hy = s.cabin[0].y - 0.08f;
                foreach (float z in new[] { s.seatZ + 0.35f, s.seatZ - 0.7f })
                    Shapes.Box(body, new Vector3(sign * (side + 0.005f), hy, z), new Vector3(0.02f, 0.03f, 0.13f), Chrome, name: "Handle");
                // Зеркала
                var front = s.cabin[1];
                Shapes.Box(body, new Vector3(sign * (s.cabinBottom + 0.12f), front.y + 0.1f, front.x - 0.12f), new Vector3(0.14f, 0.1f, 0.06f), Black, name: "Mirror");
            }
        }

        /// <summary>«Семёрка»: хромированные окантовки окон и водостоки на крыше, брызговики, хромированные колпаки.</summary>
        static void Extras2107(Transform body, Spec s)
        {
            var c = s.cabin;
            foreach (float sign in new[] { -1f, 1f })
            {
                // Хром по нижней кромке окон и по краю крыши
                Beam(body, new Vector3(sign * (s.cabinBottom + 0.015f), c[0].y + 0.01f, c[0].x), new Vector3(sign * (s.cabinBottom + 0.015f), c[1].y + 0.01f, c[1].x), 0.03f, Chrome, "BeltChrome");
                Beam(body, new Vector3(sign * (s.cabinTop + 0.02f), c[3].y - 0.01f, c[3].x), new Vector3(sign * (s.cabinTop + 0.02f), c[2].y - 0.01f, c[2].x), 0.035f, Chrome, "DripRail");
                // Брызговики за колёсами
                foreach (float z in new[] { s.axleFront, s.axleRear })
                    Shapes.Box(body, new Vector3(sign * (s.halfWidth - 0.08f), 0.2f, z - s.wheelR - 0.12f), new Vector3(0.22f, 0.3f, 0.02f), Black, name: "Mudflap");
            }
            // Хромированные колпаки
            foreach (var w in body.parent.GetComponentsInChildren<Transform>())
                if (w.name == "Wheel")
                    Shapes.Make(PrimitiveType.Cylinder, w, Vector3.zero, new Vector3(s.wheelR * 1.05f, 0.118f, s.wheelR * 1.05f), Chrome, name: "Hubcap");
        }

        /// <summary>«Нива»: чёрные расширители арок и багажник на крыше.</summary>
        static void ExtrasNiva(Transform body, Spec s)
        {
            foreach (float sign in new[] { -1f, 1f })
            foreach (float z in new[] { s.axleFront, s.axleRear })
                Shapes.Box(body, new Vector3(sign * (s.halfWidth + 0.03f), s.wheelR + 0.32f, z), new Vector3(0.08f, 0.1f, s.wheelR * 2.5f), Black, name: "FenderFlare");
            float roof = s.roofY + 0.06f;
            var front = s.cabin[2].x - 0.1f;
            var rear = s.cabin[s.cabin.Length - 1].x + 0.1f;
            foreach (float sign in new[] { -1f, 1f })
                Beam(body, new Vector3(sign * (s.cabinTop - 0.05f), roof, rear), new Vector3(sign * (s.cabinTop - 0.05f), roof, front), 0.05f, Black, "RoofRail");
            for (int i = 0; i < 3; i++)
            {
                float z = Mathf.Lerp(rear + 0.2f, front - 0.2f, i / 2f);
                Shapes.Box(body, new Vector3(0, roof + 0.03f, z), new Vector3(s.cabinTop * 2f - 0.05f, 0.03f, 0.05f), Black, name: "RoofBar");
            }
        }

        /// <summary>«Рио»: чёрный низ бамперов и порогов, узкий фонарь через всю корму.</summary>
        static void ExtrasRio(Transform body, Spec s)
        {
            float half = s.length / 2f;
            Shapes.Box(body, new Vector3(0, 0.36f, half + 0.03f), new Vector3(1.5f, 0.08f, 0.03f), Black, name: "LowerTrim");
            Shapes.Box(body, new Vector3(0, 0.88f, -half - 0.005f), new Vector3(1.2f, 0.04f, 0.02f), TailLight, name: "TailBar");
            foreach (float sign in new[] { -1f, 1f })
                Shapes.Box(body, new Vector3(sign * (s.halfWidth + 0.005f), 0.36f, 0), new Vector3(0.02f, 0.08f, s.length * 0.55f), Black, name: "Sill");
        }

        /// <summary>Такси: шашечки на дверях и «гребешок» на крыше.</summary>
        static void TaxiDress(Transform body, Spec s)
        {
            float side = s.halfWidth + 0.008f;
            foreach (float sign in new[] { -1f, 1f })
                for (int i = 0; i < 8; i++)
                for (int row = 0; row < 2; row++)
                {
                    if ((i + row) % 2 != 0) continue;
                    Shapes.Box(body, new Vector3(sign * side, 0.6f + row * 0.07f, -0.9f + i * 0.07f), new Vector3(0.01f, 0.07f, 0.07f), Black, name: "Checker");
                }
            var top = s.cabin[2];
            var roofSign = Shapes.Group("TaxiSign", body, new Vector3(0, s.roofY + 0.1f, (top.x + s.cabin[s.cabin.Length - 1].x) / 2f));
            Shapes.Box(roofSign, Vector3.zero, new Vector3(0.6f, 0.16f, 0.22f), TaxiYellow);
            for (int i = 0; i < 6; i++)
                Shapes.Box(roofSign, new Vector3(-0.25f + i * 0.1f, 0.04f * ((i % 2) * 2 - 1), 0), new Vector3(0.05f, 0.05f, 0.23f), Black, name: "Checker");
            var t1 = Fonts.WorldText(roofSign, new Vector3(0, -0.02f, -0.12f), "ТАКСИ", Black, 0.012f);
            var t2 = Fonts.WorldText(roofSign, new Vector3(0, -0.02f, 0.12f), "ТАКСИ", Black, 0.012f);
            t2.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }

        /// <summary>Маршрутка: табличка с номером маршрута в лобовом стекле.</summary>
        static void RouteSign(Transform body, Spec s)
        {
            var c = s.cabin;
            var at = new Vector3(0.5f, (c[1].y + c[2].y) / 2f - 0.1f, (c[1].x + c[2].x) / 2f - 0.12f);
            var sign = Shapes.Group("Route", body, at, new Vector3(0, 180, 0));
            Shapes.Box(sign, Vector3.zero, new Vector3(0.45f, 0.2f, 0.01f), PlateWhite);
            Fonts.WorldText(sign, new Vector3(0, 0, -0.01f), $"№{UnityEngine.Random.Range(1, 600)}", Shapes.Hex("#c0201a"), 0.02f);
        }
    }
}

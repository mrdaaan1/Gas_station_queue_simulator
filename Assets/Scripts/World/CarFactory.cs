using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    public enum CarShape { Sedan, Hatchback, Van }

    /// <summary>Ссылки на подвижные и ломающиеся части собранной машины.</summary>
    public class CarVisual : MonoBehaviour
    {
        public float length;
        public float width = 1.86f;
        public float height;
        public readonly List<Transform> wheels = new List<Transform>();
        public readonly List<Transform> frontSteer = new List<Transform>();
        public Transform body;
        public Transform fuelNeedle;
        public Transform speedNeedle;
        public Transform steeringWheel;
        public Transform driverEyes;
        public Transform driverHead;
        public Transform driverTorso;
        public Renderer fuelLamp;
        public Renderer engineLamp;
        public TextMesh radioDisplay;

        // Части, которые мнутся и отваливаются при ударах
        public Transform bumperFront, bumperRear;
        public readonly List<Transform> headlights = new List<Transform>();
        public readonly List<Transform> taillights = new List<Transform>();
        public Transform frontPanel, rearPanel;

        public readonly List<Renderer> leftBlinkers = new List<Renderer>();
        public readonly List<Renderer> rightBlinkers = new List<Renderer>();

        /// <summary>Где водитель выходит из машины (в локальных координатах, слева).</summary>
        public Vector3 driverDoorLocal = new Vector3(-1.45f, 0f, -0.45f);
        /// <summary>Лючок бензобака — справа сзади.</summary>
        public Vector3 fuelCapLocal = new Vector3(0.93f, 0.85f, -1.3f);

        /// <summary>Поворотник: −1 левый, +1 правый, 0 выключен.</summary>
        public int blinker;

        float bounce;
        int shownBlinker;
        bool blinkOn;

        static readonly Color BlinkOff = Shapes.Hex("#7a4a10");
        static readonly Color BlinkOn = Shapes.Hex("#ffa500");

        /// <summary>Крутим колёса по пройденному пути.</summary>
        public void Roll(float distance)
        {
            if (Mathf.Abs(distance) < 0.0001f) return;
            float deg = distance / (2 * Mathf.PI * 0.33f) * 360f;
            foreach (var w in wheels) w.Rotate(Vector3.up, deg, Space.Self);
        }

        public void Steer(float angle)
        {
            foreach (var s in frontSteer) s.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        /// <summary>Машина «подпрыгивает» — водитель возмущается.</summary>
        public void Bounce() => bounce = 1f;

        void Update()
        {
            if (bounce > 0f && body != null)
            {
                bounce = Mathf.Max(0f, bounce - Time.deltaTime * 2.5f);
                body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bounce * Mathf.PI * 3f)) * 0.12f * bounce, 0f);
            }

            bool on = blinker != 0 && Mathf.Repeat(Time.time, 0.8f) < 0.45f;
            if (on == blinkOn && blinker == shownBlinker) return;
            blinkOn = on;
            shownBlinker = blinker;
            foreach (var r in leftBlinkers) if (r != null) r.sharedMaterial = Shapes.Mat(on && blinker < 0 ? BlinkOn : BlinkOff);
            foreach (var r in rightBlinkers) if (r != null) r.sharedMaterial = Shapes.Mat(on && blinker > 0 ? BlinkOn : BlinkOff);
        }
    }

    /// <summary>Собирает low-poly машинку из примитивов. Вперёд — ось +Z.</summary>
    public static class CarFactory
    {
        static readonly Color Dark = Shapes.Hex("#2a2a2e");
        static readonly Color Tire = Shapes.Hex("#1b1b1b");
        static readonly Color Chrome = Shapes.Hex("#b8bcc2");
        static readonly Color Glass = Shapes.Hex("#33424f");
        static readonly Color Seat = Shapes.Hex("#4a3b32");
        static readonly Color Skin = Shapes.Hex("#e0ac85");

        public static readonly Color[] Paints =
        {
            Shapes.Hex("#d9d4c7"), Shapes.Hex("#a83232"), Shapes.Hex("#2f5d8a"), Shapes.Hex("#3d6b3a"),
            Shapes.Hex("#c9a23a"), Shapes.Hex("#6b6f75"), Shapes.Hex("#1e1e22"), Shapes.Hex("#7a2f5a"),
            Shapes.Hex("#e8e8e8"), Shapes.Hex("#8a5a2b"), Shapes.Hex("#4f8f8a"),
        };

        static readonly Color[] Shirts =
        {
            Shapes.Hex("#3b5998"), Shapes.Hex("#555555"), Shapes.Hex("#8b2e2e"), Shapes.Hex("#2e6b4f"), Shapes.Hex("#d2b48c"),
        };

        public static CarVisual Build(string name, Color paint, CarShape shape, bool isPlayer)
        {
            if (isPlayer) return BuildPlayer(name, paint);
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var body = Shapes.Group("Body", root);
            visual.body = body;

            float length = shape == CarShape.Van ? 4.7f : shape == CarShape.Hatchback ? 3.8f : 4.3f;
            float bodyTop = shape == CarShape.Van ? 1.25f : 0.95f;
            float roofY = shape == CarShape.Van ? 2.05f : 1.5f;
            float half = length / 2f;
            visual.length = length;
            visual.height = roofY;

            // Кузов
            float bodyH = bodyTop - 0.3f;
            Shapes.Box(body, new Vector3(0, 0.3f + bodyH / 2f, 0), new Vector3(1.8f, bodyH, length), paint, name: "Shell");
            AddBumpersAndLights(body, visual, half);
            AddWheels(root, visual, half);

            // Салон: открытый каркас, чтобы было видно водителя и чтобы игрок видел мир из машины
            float cabFront = shape == CarShape.Van ? half - 1.0f : 0.7f;
            float cabRear = shape == CarShape.Sedan ? -1.25f : -half + 0.2f;
            float cabLen = cabFront - cabRear;
            float cabMid = (cabFront + cabRear) / 2f;
            float pillarH = roofY - bodyTop;
            Shapes.Box(body, new Vector3(0, roofY, cabMid), new Vector3(1.66f, 0.07f, cabLen + 0.1f), paint, name: "Roof");
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                Shapes.Box(body, new Vector3(x, bodyTop + pillarH / 2f, cabFront - 0.15f), new Vector3(0.07f, pillarH, 0.07f), paint, new Vector3(-18, 0, 0));
                Shapes.Box(body, new Vector3(x, bodyTop + pillarH / 2f, cabMid - 0.1f), new Vector3(0.07f, pillarH, 0.07f), paint);
                Shapes.Box(body, new Vector3(x, bodyTop + pillarH / 2f, cabRear + 0.05f), new Vector3(0.07f, pillarH, 0.1f), paint);
            }
            // Заднее стекло — тёмное, как тонировка
            Shapes.Box(body, new Vector3(0, bodyTop + pillarH / 2f, cabRear + 0.02f), new Vector3(1.55f, pillarH * 0.95f, 0.03f), Glass, name: "RearGlass");

            // Приборная панель и сиденья
            float dashZ = cabFront - 0.25f;
            Shapes.Box(body, new Vector3(0, bodyTop + 0.06f, dashZ), new Vector3(1.6f, 0.22f, 0.4f), Dark, name: "Dashboard");
            Shapes.Box(body, new Vector3(0, roofY - 0.12f, cabFront - 0.25f), new Vector3(0.24f, 0.07f, 0.03f), Dark, name: "Mirror");
            float seatZ = cabFront - 1.25f;
            visual.driverDoorLocal = new Vector3(-1.45f, 0f, seatZ);
            foreach (float x in new[] { -0.4f, 0.4f })
            {
                Shapes.Box(body, new Vector3(x, bodyTop - 0.2f, seatZ), new Vector3(0.55f, 0.14f, 0.55f), Seat);
                Shapes.Box(body, new Vector3(x, bodyTop + 0.12f, seatZ - 0.3f), new Vector3(0.55f, 0.65f, 0.12f), Seat, new Vector3(-10, 0, 0));
            }

            visual.steeringWheel = SteeringWheel(body, new Vector3(-0.4f, bodyTop + 0.08f, dashZ - 0.3f));

            {
                // Водитель: голова и туловище — чтобы было видно, что в машине кто-то сидит
                var shirt = Shirts[Random.Range(0, Shirts.Length)];
                visual.driverTorso = Shapes.Box(body, new Vector3(-0.4f, bodyTop - 0.02f, seatZ - 0.08f), new Vector3(0.46f, 0.5f, 0.28f), shirt, name: "Torso").transform;
                visual.driverHead = Shapes.Make(PrimitiveType.Sphere, body, new Vector3(-0.4f, bodyTop + 0.36f, seatZ - 0.05f),
                    Vector3.one * 0.27f, Skin, name: "Head").transform;
            }

            return visual;
        }

        static void AddBumpersAndLights(Transform body, CarVisual visual, float half)
        {
            visual.bumperFront = Shapes.Box(body, new Vector3(0, 0.42f, half + 0.03f), new Vector3(1.86f, 0.22f, 0.12f), Chrome, name: "BumperFront").transform;
            visual.bumperRear = Shapes.Box(body, new Vector3(0, 0.42f, -half - 0.03f), new Vector3(1.86f, 0.22f, 0.12f), Chrome, name: "BumperRear").transform;
            foreach (float x in new[] { -0.6f, 0.6f })
            {
                visual.headlights.Add(Shapes.Box(body, new Vector3(x, 0.72f, half + 0.01f), new Vector3(0.36f, 0.14f, 0.04f), Shapes.Hex("#fff3c4"), name: "Headlight").transform);
                visual.taillights.Add(Shapes.Box(body, new Vector3(x, 0.72f, -half - 0.01f), new Vector3(0.36f, 0.12f, 0.04f), Shapes.Hex("#c0201a"), name: "Taillight").transform);
            }
            // Поворотники по углам
            foreach (float side in new[] { -1f, 1f })
            foreach (float end in new[] { -1f, 1f })
            {
                var r = Shapes.Box(body, new Vector3(0.86f * side, 0.72f, (half + 0.02f) * end), new Vector3(0.12f, 0.1f, 0.04f),
                    Shapes.Hex("#7a4a10"), name: "Blinker").GetComponent<Renderer>();
                (side < 0 ? visual.leftBlinkers : visual.rightBlinkers).Add(r);
            }
        }

        static void AddWheels(Transform root, CarVisual visual, float half)
        {
            foreach (float x in new[] { -0.86f, 0.86f })
            foreach (float z in new[] { -half + 0.85f, half - 0.85f })
            {
                // Передние колёса висят на отдельном шарнире, чтобы поворачиваться
                Transform mount = root;
                var pos = new Vector3(x, 0.33f, z);
                if (z > 0f)
                {
                    mount = Shapes.Group("SteerPivot", root, pos);
                    visual.frontSteer.Add(mount);
                    pos = Vector3.zero;
                }
                var pivot = Shapes.Group("Wheel", mount, pos, new Vector3(0, 0, 90));
                Shapes.Make(PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(0.66f, 0.12f, 0.66f), Tire);
                Shapes.Make(PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(0.36f, 0.125f, 0.36f), Chrome);
                Shapes.Box(pivot, Vector3.zero, new Vector3(0.08f, 0.13f, 0.5f), Dark); // спица, чтобы было видно вращение
                visual.wheels.Add(pivot);
            }
        }

        /// <summary>Руль: обод-кольцо, три спицы и колонка, уходящая в торпеду. Наклонён к водителю.</summary>
        static Transform SteeringWheel(Transform parent, Vector3 pos)
        {
            var pivot = Shapes.Group("SteeringWheel", parent, pos, new Vector3(-65f, 0, 0));
            MeshFactory.MeshObject("Rim", pivot, MeshFactory.Torus(0.18f, 0.018f), Vector3.zero, Vector3.zero, Shapes.Mat(Tire));
            Shapes.Box(pivot, Vector3.zero, new Vector3(0.34f, 0.015f, 0.035f), Dark, name: "Spoke");
            Shapes.Box(pivot, new Vector3(0, 0, -0.085f), new Vector3(0.035f, 0.015f, 0.17f), Dark, name: "Spoke");
            Shapes.Make(PrimitiveType.Cylinder, pivot, new Vector3(0, 0.005f, 0), new Vector3(0.09f, 0.015f, 0.09f), Dark, name: "Hub");
            Shapes.Make(PrimitiveType.Cylinder, pivot, new Vector3(0, -0.14f, 0), new Vector3(0.05f, 0.13f, 0.05f), Tire, name: "Column");
            return pivot;
        }

        /// <summary>
        /// Машина игрока: кузов собран из отдельных панелей, чтобы внутри был настоящий салон —
        /// пол, двери, стёкла, торпеда с приборами, руль, сиденья, магнитола, зеркала.
        /// </summary>
        static CarVisual BuildPlayer(string name, Color paint)
        {
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var body = Shapes.Group("Body", root);
            visual.body = body;

            const float length = 4.3f, half = length / 2f;
            const float floorY = 0.6f, beltY = 1.0f, roofY = 1.6f;
            const float cabFront = 0.95f, cabRear = -1.75f; // от основания лобового стекла до заднего стекла
            const float windTopZ = 0.3f;                    // где лобовое стекло упирается в крышу
            visual.length = length;
            visual.height = roofY;

            var trim = Shapes.Hex("#26262a");
            var plastic = Shapes.Hex("#3a3a40");
            var seat = Shapes.Hex("#5c4c3e");
            var headliner = Shapes.Hex("#b9b3a6");

            // Низ кузова, капот, багажник, двери
            Shapes.Box(body, new Vector3(0, (0.3f + floorY) / 2f, 0), new Vector3(1.8f, floorY - 0.3f, length), paint, name: "Underbody");
            Shapes.Box(body, new Vector3(0, floorY + 0.01f, (cabFront + cabRear) / 2f), new Vector3(1.62f, 0.02f, cabFront - cabRear), trim, name: "Floor");
            visual.frontPanel = Shapes.Box(body, new Vector3(0, (floorY + 0.95f) / 2f, (cabFront + half) / 2f), new Vector3(1.8f, 0.95f - floorY, half - cabFront), paint, name: "Hood").transform;
            visual.rearPanel = Shapes.Box(body, new Vector3(0, (floorY + 0.98f) / 2f, (cabRear - half) / 2f), new Vector3(1.8f, 0.98f - floorY, half + cabRear), paint, name: "Trunk").transform;
            foreach (float side in new[] { -1f, 1f })
            {
                float x = 0.86f * side;
                Shapes.Box(body, new Vector3(x, (floorY + beltY) / 2f, (cabFront + cabRear) / 2f), new Vector3(0.08f, beltY - floorY, cabFront - cabRear), paint, name: "Door");
                Shapes.Box(body, new Vector3(x - 0.05f * side, beltY - 0.02f, (cabFront + cabRear) / 2f), new Vector3(0.06f, 0.05f, cabFront - cabRear), trim, name: "DoorTrim");
                Shapes.Box(body, new Vector3(x - 0.05f * side, floorY + 0.25f, -0.4f), new Vector3(0.04f, 0.3f, 1.4f), plastic, name: "DoorCard");
                // Боковые стёкла и стойки
                var sideGlass = Shapes.Box(body, new Vector3(x, (beltY + roofY) / 2f, (windTopZ + cabRear) / 2f), new Vector3(0.02f, roofY - beltY, windTopZ - cabRear), Color.white, name: "SideGlass");
                sideGlass.GetComponent<Renderer>().sharedMaterial = MeshFactory.Glass;
                Shapes.Box(body, new Vector3(x, (beltY + roofY) / 2f, -0.75f), new Vector3(0.08f, roofY - beltY, 0.1f), paint, name: "BPillar");
                Shapes.Box(body, new Vector3(x, (beltY + roofY) / 2f, cabRear + 0.08f), new Vector3(0.08f, roofY - beltY, 0.16f), paint, name: "CPillar");
                // Боковое зеркало снаружи
                Shapes.Box(body, new Vector3(0.98f * side, beltY + 0.08f, cabFront - 0.15f), new Vector3(0.1f, 0.13f, 0.2f), paint, name: "SideMirror");
                Shapes.Box(body, new Vector3(0.98f * side, beltY + 0.08f, cabFront - 0.255f), new Vector3(0.08f, 0.11f, 0.01f), Chrome, name: "SideMirrorGlass");
            }

            // Лобовое стекло и передние стойки — под наклоном
            float rise = roofY - 0.95f, run = cabFront - windTopZ;
            float tilt = Mathf.Atan2(run, rise) * Mathf.Rad2Deg;
            float windLen = Mathf.Sqrt(rise * rise + run * run);
            var windPos = new Vector3(0, (0.95f + roofY) / 2f, (cabFront + windTopZ) / 2f);
            var windshield = Shapes.Box(body, windPos, new Vector3(1.62f, windLen, 0.02f), Color.white, new Vector3(-tilt, 0, 0), "Windshield");
            windshield.GetComponent<Renderer>().sharedMaterial = MeshFactory.Glass;
            foreach (float side in new[] { -1f, 1f })
                Shapes.Box(body, new Vector3(0.84f * side, windPos.y, windPos.z), new Vector3(0.07f, windLen, 0.07f), paint, new Vector3(-tilt, 0, 0), "APillar");
            var rearGlass = Shapes.Box(body, new Vector3(0, (0.98f + roofY) / 2f, cabRear - 0.1f), new Vector3(1.62f, roofY - 0.98f, 0.02f), Color.white, new Vector3(20f, 0, 0), "RearGlass");
            rearGlass.GetComponent<Renderer>().sharedMaterial = MeshFactory.Glass;

            // Крыша и потолок
            Shapes.Box(body, new Vector3(0, roofY, (windTopZ + cabRear) / 2f - 0.05f), new Vector3(1.74f, 0.06f, windTopZ - cabRear + 0.1f), paint, name: "Roof");
            Shapes.Box(body, new Vector3(0, roofY - 0.035f, (windTopZ + cabRear) / 2f - 0.05f), new Vector3(1.62f, 0.01f, windTopZ - cabRear), headliner, name: "Headliner");
            foreach (float x in new[] { -0.4f, 0.4f })
                Shapes.Box(body, new Vector3(x, roofY - 0.06f, windTopZ - 0.08f), new Vector3(0.42f, 0.02f, 0.16f), headliner, new Vector3(-8, 0, 0), "SunVisor");

            // Торпеда
            Shapes.Box(body, new Vector3(0, 0.88f, 0.6f), new Vector3(1.64f, 0.36f, 0.5f), plastic, name: "Dashboard");
            Shapes.Box(body, new Vector3(0, 1.07f, 0.62f), new Vector3(1.64f, 0.04f, 0.46f), trim, name: "DashTop");
            Shapes.Box(body, new Vector3(0.45f, 0.86f, 0.355f), new Vector3(0.5f, 0.16f, 0.02f), trim, name: "Glovebox");

            // Щиток приборов за рулём: козырёк, спидометр, датчик топлива, лампы
            const float gx = -0.38f, gy = 1.0f, gz = 0.35f;
            Shapes.Box(body, new Vector3(gx, gy, gz + 0.005f), new Vector3(0.5f, 0.17f, 0.02f), Shapes.Hex("#111114"), name: "Cluster");
            Shapes.Box(body, new Vector3(gx, gy + 0.1f, gz + 0.04f), new Vector3(0.54f, 0.04f, 0.14f), plastic, new Vector3(-12, 0, 0), "ClusterVisor");
            var face = new Vector3(-90, 0, 0);
            var dial = Shapes.Hex("#f2efe6");
            Shapes.Make(PrimitiveType.Cylinder, body, new Vector3(gx - 0.1f, gy, gz - 0.01f), new Vector3(0.15f, 0.006f, 0.15f), dial, face, "Speedometer");
            Shapes.Make(PrimitiveType.Cylinder, body, new Vector3(gx + 0.12f, gy, gz - 0.01f), new Vector3(0.11f, 0.006f, 0.11f), dial, face, "FuelGauge");
            Shapes.Box(body, new Vector3(gx + 0.12f - 0.035f, gy + 0.03f, gz - 0.019f), new Vector3(0.016f, 0.016f, 0.003f), Shapes.Hex("#d02020"), name: "EmptyMark");
            visual.speedNeedle = Needle(body, new Vector3(gx - 0.1f, gy, gz - 0.022f), 0.06f);
            visual.fuelNeedle = Needle(body, new Vector3(gx + 0.12f, gy, gz - 0.022f), 0.045f);
            visual.fuelLamp = Shapes.Box(body, new Vector3(gx + 0.12f, gy - 0.045f, gz - 0.019f), new Vector3(0.025f, 0.012f, 0.003f), Shapes.Hex("#3a2a10"), name: "FuelLamp").GetComponent<Renderer>();
            visual.engineLamp = Shapes.Box(body, new Vector3(gx - 0.1f, gy - 0.055f, gz - 0.019f), new Vector3(0.025f, 0.012f, 0.003f), Shapes.Hex("#3a2a10"), name: "EngineLamp").GetComponent<Renderer>();

            visual.steeringWheel = SteeringWheel(body, new Vector3(gx, 0.98f, 0.1f));

            // Центральная консоль с магнитолой и рычаг КПП
            Shapes.Box(body, new Vector3(0, 0.78f, 0.25f), new Vector3(0.32f, 0.36f, 0.2f), plastic, name: "Console");
            Shapes.Box(body, new Vector3(0, 0.92f, 0.145f), new Vector3(0.26f, 0.07f, 0.01f), Shapes.Hex("#0c1a12"), name: "RadioScreen");
            visual.radioDisplay = Fonts.WorldText(body, new Vector3(0, 0.92f, 0.138f), "", Shapes.Hex("#7dffa8"), 0.0042f);
            Shapes.Box(body, new Vector3(0, 0.66f, -0.3f), new Vector3(0.22f, 0.12f, 0.7f), plastic, name: "Tunnel");
            Shapes.Make(PrimitiveType.Cylinder, body, new Vector3(0, 0.8f, -0.05f), new Vector3(0.025f, 0.12f, 0.025f), trim, new Vector3(-10, 0, 0), "GearStick");
            Shapes.Make(PrimitiveType.Sphere, body, new Vector3(0, 0.93f, -0.07f), Vector3.one * 0.06f, trim, name: "GearKnob");

            // Сиденья: два передних и задний диван
            foreach (float x in new[] { -0.38f, 0.38f })
            {
                Shapes.Box(body, new Vector3(x, floorY + 0.14f, -0.55f), new Vector3(0.52f, 0.14f, 0.52f), seat, name: "Seat");
                Shapes.Box(body, new Vector3(x, floorY + 0.45f, -0.85f), new Vector3(0.52f, 0.7f, 0.12f), seat, new Vector3(-12, 0, 0), "SeatBack");
                Shapes.Box(body, new Vector3(x, floorY + 0.86f, -0.94f), new Vector3(0.28f, 0.18f, 0.1f), seat, new Vector3(-12, 0, 0), "Headrest");
            }
            Shapes.Box(body, new Vector3(0, floorY + 0.14f, -1.4f), new Vector3(1.5f, 0.14f, 0.5f), seat, name: "RearSeat");
            Shapes.Box(body, new Vector3(0, floorY + 0.45f, -1.66f), new Vector3(1.5f, 0.62f, 0.12f), seat, new Vector3(-10, 0, 0), "RearSeatBack");

            // Салонное зеркало
            Shapes.Make(PrimitiveType.Cylinder, body, new Vector3(0, roofY - 0.08f, windTopZ + 0.02f), new Vector3(0.02f, 0.04f, 0.02f), trim, name: "MirrorMount");
            Shapes.Box(body, new Vector3(0, roofY - 0.14f, windTopZ + 0.02f), new Vector3(0.26f, 0.07f, 0.03f), trim, name: "Mirror");
            Shapes.Box(body, new Vector3(0, roofY - 0.14f, windTopZ + 0.004f), new Vector3(0.24f, 0.055f, 0.003f), Chrome, name: "MirrorGlass");

            AddBumpersAndLights(body, visual, half);
            AddWheels(root, visual, half);

            visual.driverEyes = Shapes.Group("DriverEyes", root, new Vector3(-0.38f, 1.37f, -0.5f));
            return visual;
        }

        static Transform Needle(Transform parent, Vector3 pos, float len)
        {
            var pivot = Shapes.Group("Needle", parent, pos);
            Shapes.Box(pivot, new Vector3(0, len / 2f, 0), new Vector3(0.008f, len, 0.004f), Shapes.Hex("#d02020"));
            return pivot;
        }

        public static CarShape RandomShape()
        {
            float r = Random.value;
            return r < 0.6f ? CarShape.Sedan : r < 0.85f ? CarShape.Hatchback : CarShape.Van;
        }

        /// <summary>Бензовоз — приезжает с завозом.</summary>
        public static Transform BuildTanker()
        {
            var root = new GameObject("Tanker").transform;
            var orange = Shapes.Hex("#e07b1a");
            Shapes.Box(root, new Vector3(0, 1.3f, 3.2f), new Vector3(2.3f, 2.2f, 2.0f), Shapes.Hex("#c63a2b"), name: "Cab");
            Shapes.Box(root, new Vector3(0, 1.75f, 4.21f), new Vector3(2.0f, 0.8f, 0.03f), Glass);
            Shapes.Box(root, new Vector3(0, 0.75f, -0.8f), new Vector3(1.6f, 0.3f, 8.0f), Dark, name: "Frame");
            Shapes.Make(PrimitiveType.Cylinder, root, new Vector3(0, 2.0f, -1.3f), new Vector3(2.2f, 3.4f, 2.2f), orange, new Vector3(90, 0, 0), "Tank");
            foreach (float side in new[] { -1f, 1f })
            {
                var label = Fonts.WorldText(root, new Vector3(1.12f * side, 2.0f, -1.3f), "ОГНЕОПАСНО", Shapes.Hex("#1a1a1a"), 0.07f);
                label.transform.localRotation = Quaternion.Euler(0, -90f * side, 0);
            }
            foreach (float x in new[] { -1.05f, 1.05f })
            foreach (float z in new[] { 3.0f, -2.0f, -3.4f })
                Shapes.Make(PrimitiveType.Cylinder, root, new Vector3(x, 0.5f, z), new Vector3(1.0f, 0.18f, 1.0f), Tire, new Vector3(0, 0, 90));
            return root;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
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
        public Transform passengerHead, passengerTorso;          // пассажир справа (есть не у всех)
        public Transform mirrorRear, mirrorLeft, mirrorRight; // стёкла зеркал машины игрока (CarMirrors)
        public Transform driverTorso;
        public Transform leftHand, rightHand, leftArm, rightArm;
        public Renderer fuelLamp;
        public Renderer engineLamp;
        public TextMesh radioDisplay;

        // Части, которые мнутся и отваливаются при ударах
        public Transform bumperFront, bumperRear;
        public readonly List<Transform> headlights = new List<Transform>();
        public readonly List<Transform> taillights = new List<Transform>();
        public Transform frontPanel, rearPanel;
        public readonly List<Transform> doors = new List<Transform>();
        public Transform roof;

        public readonly List<Renderer> leftBlinkers = new List<Renderer>();
        public readonly List<Renderer> rightBlinkers = new List<Renderer>();

        /// <summary>Где водитель выходит из машины (в локальных координатах, слева).</summary>
        public Vector3 driverDoorLocal = new Vector3(-1.45f, 0f, -0.45f);
        /// <summary>Лючок бензобака — справа сзади.</summary>
        public Vector3 fuelCapLocal = new Vector3(0.93f, 0.85f, -1.3f);

        /// <summary>Поворотник: −1 левый, +1 правый, 0 выключен.</summary>
        public int blinker;

        // Особенности модели (у «Жигулей» — значения по умолчанию)
        public bool rightHandDrive;
        public bool sporty;
        /// <summary>Высота капота и багажника — на них можно залезть (−1 — считать от высоты машины).</summary>
        public float hoodTop = -1f;
        public float wheelRadius = 0.33f;
        /// <summary>Максимальная скорость (м/с), разгон (м/с²), шкала спидометра.</summary>
        public float maxSpeed = 16f, accel = 3.6f, speedoMaxKmh = 120f;
        /// <summary>Наклон руля к водителю (градусы вокруг X).</summary>
        public float steeringTilt = -65f;
        public Transform tachNeedle;
        /// <summary>Плечи водителя (локально в кузове) — от них тянутся руки к рулю.</summary>
        public Vector3 shoulderL = new Vector3(-0.6f, 1.18f, -0.55f), shoulderR = new Vector3(-0.16f, 1.18f, -0.55f);
        /// <summary>Материалы поворотника (если null — простые цвета).</summary>
        public Material blinkOffMat, blinkOnMat;

        float bounce;
        int shownBlinker;
        bool blinkOn;

        static readonly Color BlinkOff = Shapes.Hex("#7a4a10");
        static readonly Color BlinkOn = Shapes.Hex("#ffa500");

        /// <summary>Крутим колёса по пройденному пути.</summary>
        public void Roll(float distance)
        {
            if (Mathf.Abs(distance) < 0.0001f) return;
            float deg = distance / (2 * Mathf.PI * wheelRadius) * 360f;
            foreach (var w in wheels) w.Rotate(Vector3.up, deg, Space.Self);
        }

        /// <summary>Тянем руки водителя от плеч к кистям на руле.</summary>
        public void UpdateArms()
        {
            if (leftArm == null || body == null) return;
            UpdateArm(leftArm, leftHand, shoulderL);
            UpdateArm(rightArm, rightHand, shoulderR);
        }

        void UpdateArm(Transform arm, Transform hand, Vector3 shoulderLocal)
        {
            var a = body.TransformPoint(shoulderLocal);
            var b = hand.position;
            arm.position = (a + b) / 2f;
            arm.rotation = Quaternion.LookRotation(b - a, transform.up);
            arm.localScale = new Vector3(0.1f, 0.1f, Vector3.Distance(a, b));
        }

        /// <summary>Показать/спрятать водителя: голову и туловище прячем при виде из салона, всего — когда он вышел.</summary>
        public void SetDriverVisible(bool body, bool arms)
        {
            if (driverHead != null && driverHead.gameObject.activeSelf != body) driverHead.gameObject.SetActive(body);
            if (driverTorso != null && driverTorso.gameObject.activeSelf != body) driverTorso.gameObject.SetActive(body);
            foreach (var t in new[] { leftArm, rightArm, leftHand, rightHand })
                if (t != null && t.gameObject.activeSelf != arms) t.gameObject.SetActive(arms);
        }

        Material BlinkMat(bool lit)
        {
            if (blinkOffMat == null) return Shapes.Mat(lit ? BlinkOn : BlinkOff);
            if (!lit) return blinkOffMat;
            if (blinkOnMat == null)
            {
                blinkOnMat = new Material(blinkOffMat) { name = "BlinkOn", color = BlinkOn };
                blinkOnMat.EnableKeyword("_EMISSION");
                if (blinkOnMat.HasProperty("_EmissionColor")) blinkOnMat.SetColor("_EmissionColor", BlinkOn * 1.6f);
            }
            return blinkOnMat;
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
            foreach (var r in leftBlinkers) if (r != null) r.sharedMaterial = BlinkMat(on && blinker < 0);
            foreach (var r in rightBlinkers) if (r != null) r.sharedMaterial = BlinkMat(on && blinker > 0);
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

        /// <summary>Машина игрока (с полным салоном) или NPC одной из моделей <see cref="CarModels"/>.</summary>
        public static CarVisual Build(string name, Color paint, CarModel model, bool isPlayer, bool taxi = false, ServiceKind service = ServiceKind.None)
        {
            if (isPlayer) return BuildPlayer(name, paint);
            return CarModels.Build(name, model, paint, taxi, service);
        }

        /// <summary>«Лицо» машины игрока в стиле ВАЗ-2107: хромированная решётка, прямоугольные фары, хромированные бамперы.</summary>
        static void AddBumpersAndLights(Transform body, CarVisual visual, float half)
        {
            var black = Shapes.Hex("#1e1e20");
            Shapes.Box(body, new Vector3(0, 0.78f, half + 0.015f), new Vector3(0.8f, 0.24f, 0.03f), Chrome, name: "Grille");
            for (int i = 0; i < 4; i++)
                Shapes.Box(body, new Vector3(0, 0.69f + i * 0.06f, half + 0.035f), new Vector3(0.76f, 0.015f, 0.01f), black, name: "GrilleBar");
            foreach (float x in new[] { -0.6f, 0.6f })
            {
                Shapes.Box(body, new Vector3(x, 0.78f, half + 0.01f), new Vector3(0.38f, 0.22f, 0.04f), Chrome, name: "LampFrame");
                visual.headlights.Add(Shapes.Box(body, new Vector3(x, 0.78f, half + 0.03f), new Vector3(0.32f, 0.17f, 0.02f), Shapes.Hex("#fff6d5"), name: "Headlight").transform);
                visual.taillights.Add(Shapes.Box(body, new Vector3(x * 1.05f, 0.8f, -half - 0.015f), new Vector3(0.52f, 0.18f, 0.03f), Shapes.Hex("#b5160f"), name: "Taillight").transform);
                Shapes.Box(body, new Vector3(x * 1.05f, 0.73f, -half - 0.03f), new Vector3(0.52f, 0.05f, 0.01f), Shapes.Hex("#e08a1e"), name: "TailAmber");
            }
            visual.bumperFront = Shapes.Box(body, new Vector3(0, 0.45f, half + 0.06f), new Vector3(1.86f, 0.12f, 0.08f), Chrome, name: "BumperFront").transform;
            visual.bumperRear = Shapes.Box(body, new Vector3(0, 0.45f, -half - 0.06f), new Vector3(1.86f, 0.12f, 0.08f), Chrome, name: "BumperRear").transform;
            foreach (var bumper in new[] { visual.bumperFront, visual.bumperRear })
            foreach (float x in new[] { -0.45f, 0.45f })
                Shapes.Box(bumper, new Vector3(x / 1.86f, 0f, 0.6f), new Vector3(0.07f, 1.4f, 1f), black, name: "Fang");
            // Поворотники по углам
            foreach (float side in new[] { -1f, 1f })
            foreach (float end in new[] { -1f, 1f })
            {
                var r = Shapes.Box(body, new Vector3(0.86f * side, 0.78f, (half + 0.02f) * end), new Vector3(0.1f, 0.1f, 0.04f),
                    Shapes.Hex("#7a4a10"), name: "Blinker").GetComponent<Renderer>();
                (side < 0 ? visual.leftBlinkers : visual.rightBlinkers).Add(r);
            }
            CarModels.Plates(body, half * 2f, 0.45f, "О777ЧЕ 77");
            // Молдинги и ручки
            foreach (float sign in new[] { -1f, 1f })
            {
                Shapes.Box(body, new Vector3(sign * 0.905f, 0.72f, 0), new Vector3(0.015f, 0.05f, half * 1.6f), black, name: "Molding");
                foreach (float z in new[] { 0.1f, -1.0f })
                    Shapes.Box(body, new Vector3(sign * 0.91f, 0.9f, z), new Vector3(0.02f, 0.03f, 0.13f), Chrome, name: "Handle");
            }
        }

        /// <summary>Водитель в машине игрока: туловище и голова (их прячем при виде из салона) и руки на руле.</summary>
        static void AddPlayerDriver(Transform body, CarVisual visual)
        {
            var jacket = Shapes.Hex("#e0752d");
            var skin = Shapes.Hex("#e8b48f");
            visual.driverTorso = Shapes.Box(body, new Vector3(-0.38f, 1.0f, -0.62f), new Vector3(0.44f, 0.5f, 0.26f), jacket, name: "Torso").transform;
            visual.driverHead = Shapes.Make(PrimitiveType.Sphere, body, new Vector3(-0.38f, 1.36f, -0.56f), new Vector3(0.24f, 0.27f, 0.25f), skin, name: "Head").transform;
            Shapes.Make(PrimitiveType.Sphere, visual.driverHead, new Vector3(0, 0.2f, -0.08f), new Vector3(1.05f, 0.75f, 1.05f), Shapes.Hex("#3a2717"), name: "Hair");
            // Кисти держатся за обод руля (крутятся вместе с ним), руки тянутся к ним от плеч
            foreach (float side in new[] { -1f, 1f })
            {
                var hand = Shapes.Group(side < 0 ? "HandL" : "HandR", visual.steeringWheel, new Vector3(0.17f * side, 0.02f, 0.03f));
                Shapes.Make(PrimitiveType.Sphere, hand, Vector3.zero, new Vector3(0.08f, 0.07f, 0.1f), skin, name: "Fist");
                var arm = Shapes.Box(body, Vector3.zero, new Vector3(0.1f, 0.1f, 1f), jacket, name: side < 0 ? "ArmL" : "ArmR").transform;
                if (side < 0) { visual.leftHand = hand; visual.leftArm = arm; }
                else { visual.rightHand = hand; visual.rightArm = arm; }
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
        public static Transform SteeringWheelFor(Transform parent, Vector3 pos) => SteeringWheel(parent, pos);

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
                visual.doors.Add(Shapes.Box(body, new Vector3(x, (floorY + beltY) / 2f, (cabFront + cabRear) / 2f), new Vector3(0.08f, beltY - floorY, cabFront - cabRear), paint, name: "Door").transform);
                Shapes.Box(body, new Vector3(x - 0.05f * side, beltY - 0.02f, (cabFront + cabRear) / 2f), new Vector3(0.06f, 0.05f, cabFront - cabRear), trim, name: "DoorTrim");
                Shapes.Box(body, new Vector3(x - 0.05f * side, floorY + 0.25f, -0.4f), new Vector3(0.04f, 0.3f, 1.4f), plastic, name: "DoorCard");
                // Боковые стёкла и стойки
                var sideGlass = Shapes.Box(body, new Vector3(x, (beltY + roofY) / 2f, (windTopZ + cabRear) / 2f), new Vector3(0.02f, roofY - beltY, windTopZ - cabRear), Color.white, name: "SideGlass");
                sideGlass.GetComponent<Renderer>().sharedMaterial = MeshFactory.Glass;
                Shapes.Box(body, new Vector3(x, (beltY + roofY) / 2f, -0.75f), new Vector3(0.08f, roofY - beltY, 0.1f), paint, name: "BPillar");
                Shapes.Box(body, new Vector3(x, (beltY + roofY) / 2f, cabRear + 0.08f), new Vector3(0.08f, roofY - beltY, 0.16f), paint, name: "CPillar");
                // Боковое зеркало снаружи: повёрнуто к водителю (он сидит слева), стекло смотрит назад
                Shapes.Box(body, new Vector3(0.93f * side, beltY + 0.04f, cabFront - 0.12f), new Vector3(0.1f, 0.05f, 0.08f), trim, name: "SideMirrorArm");
                var mirror = Shapes.Group(side < 0 ? "LeftMirror" : "RightMirror", body,
                    new Vector3(1.06f * side, beltY + 0.1f, cabFront - 0.12f), new Vector3(0f, side < 0 ? -27f : 48f, 0f));
                Shapes.Box(mirror, new Vector3(0f, 0f, 0.035f), new Vector3(0.24f, 0.16f, 0.07f), paint, name: "SideMirror");
                var sideGlassMirror = Shapes.Make(PrimitiveType.Quad, mirror, new Vector3(0f, 0f, -0.002f), new Vector3(0.21f, 0.135f, 1f), Chrome, name: "SideMirrorGlass").transform;
                if (side < 0) visual.mirrorLeft = sideGlassMirror; else visual.mirrorRight = sideGlassMirror;
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
            visual.roof = Shapes.Box(body, new Vector3(0, roofY, (windTopZ + cabRear) / 2f - 0.05f), new Vector3(1.74f, 0.06f, windTopZ - cabRear + 0.1f), paint, name: "Roof").transform;
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
            var inner = Shapes.Group("RearMirror", body, new Vector3(0, roofY - 0.15f, windTopZ + 0.02f), new Vector3(0f, 22f, 0f));
            Shapes.Box(inner, new Vector3(0f, 0f, 0.018f), new Vector3(0.31f, 0.09f, 0.035f), trim, name: "Mirror");
            visual.mirrorRear = Shapes.Make(PrimitiveType.Quad, inner, new Vector3(0f, 0f, -0.001f), new Vector3(0.29f, 0.075f, 1f), Chrome, name: "MirrorGlass").transform;

            AddBumpersAndLights(body, visual, half);
            AddWheels(root, visual, half);
            AddPlayerDriver(body, visual);

            visual.driverEyes = Shapes.Group("DriverEyes", root, new Vector3(-0.38f, 1.37f, -0.5f));
            return visual;
        }

        static Transform Needle(Transform parent, Vector3 pos, float len)
        {
            var pivot = Shapes.Group("Needle", parent, pos);
            Shapes.Box(pivot, new Vector3(0, len / 2f, 0), new Vector3(0.008f, len, 0.004f), Shapes.Hex("#d02020"));
            return pivot;
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

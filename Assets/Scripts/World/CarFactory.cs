using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    public enum CarShape { Sedan, Hatchback, Van }

    /// <summary>Ссылки на подвижные части собранной машины.</summary>
    public class CarVisual : MonoBehaviour
    {
        public float length;
        public float height;
        public readonly List<Transform> wheels = new List<Transform>();
        public Transform body;
        public Transform fuelNeedle;
        public Transform speedNeedle;
        public Transform steeringWheel;
        public Transform driverEyes;
        public Transform driverHead;

        float bounce;

        /// <summary>Крутим колёса по пройденному пути.</summary>
        public void Roll(float distance)
        {
            if (Mathf.Abs(distance) < 0.0001f) return;
            float deg = distance / (2 * Mathf.PI * 0.33f) * 360f;
            foreach (var w in wheels) w.Rotate(Vector3.up, deg, Space.Self);
        }

        /// <summary>Машина «подпрыгивает» — водитель возмущается.</summary>
        public void Bounce() => bounce = 1f;

        void Update()
        {
            if (bounce <= 0f || body == null) return;
            bounce = Mathf.Max(0f, bounce - Time.deltaTime * 2.5f);
            body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bounce * Mathf.PI * 3f)) * 0.12f * bounce, 0f);
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
            Shapes.Box(body, new Vector3(0, 0.42f, half + 0.03f), new Vector3(1.86f, 0.22f, 0.12f), Chrome, name: "BumperFront");
            Shapes.Box(body, new Vector3(0, 0.42f, -half - 0.03f), new Vector3(1.86f, 0.22f, 0.12f), Chrome, name: "BumperRear");
            foreach (float x in new[] { -0.6f, 0.6f })
            {
                Shapes.Box(body, new Vector3(x, 0.72f, half + 0.01f), new Vector3(0.36f, 0.14f, 0.04f), Shapes.Hex("#fff3c4"));
                Shapes.Box(body, new Vector3(x, 0.72f, -half - 0.01f), new Vector3(0.36f, 0.12f, 0.04f), Shapes.Hex("#c0201a"));
            }

            // Колёса
            foreach (float x in new[] { -0.86f, 0.86f })
            foreach (float z in new[] { -half + 0.85f, half - 0.85f })
            {
                var pivot = Shapes.Group("Wheel", root, new Vector3(x, 0.33f, z), new Vector3(0, 0, 90));
                Shapes.Make(PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(0.66f, 0.12f, 0.66f), Tire);
                Shapes.Make(PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(0.36f, 0.125f, 0.36f), Chrome);
                Shapes.Box(pivot, Vector3.zero, new Vector3(0.08f, 0.13f, 0.5f), Dark); // спица, чтобы было видно вращение
                visual.wheels.Add(pivot);
            }

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
            foreach (float x in new[] { -0.4f, 0.4f })
            {
                Shapes.Box(body, new Vector3(x, bodyTop - 0.2f, seatZ), new Vector3(0.55f, 0.14f, 0.55f), Seat);
                Shapes.Box(body, new Vector3(x, bodyTop + 0.12f, seatZ - 0.3f), new Vector3(0.55f, 0.65f, 0.12f), Seat, new Vector3(-10, 0, 0));
            }

            // Руль
            var wheelPivot = Shapes.Group("SteeringWheel", body, new Vector3(-0.4f, bodyTop + 0.2f, dashZ - 0.3f), new Vector3(60, 0, 0));
            Shapes.Make(PrimitiveType.Cylinder, wheelPivot, Vector3.zero, new Vector3(0.38f, 0.015f, 0.38f), Tire);
            Shapes.Make(PrimitiveType.Cylinder, wheelPivot, new Vector3(0, 0.01f, 0), new Vector3(0.3f, 0.015f, 0.3f), Dark);
            Shapes.Box(wheelPivot, new Vector3(0, 0.02f, 0), new Vector3(0.34f, 0.02f, 0.04f), Tire);
            Shapes.Make(PrimitiveType.Cylinder, wheelPivot, new Vector3(0, -0.15f, 0), new Vector3(0.05f, 0.15f, 0.05f), Tire);
            visual.steeringWheel = wheelPivot;

            if (isPlayer)
            {
                // Приборы: спидометр и датчик топлива, повёрнуты к водителю
                var face = new Vector3(-90, 0, 0);
                float gy = bodyTop + 0.12f, gz = dashZ - 0.21f;
                Shapes.Make(PrimitiveType.Cylinder, body, new Vector3(-0.08f, gy, gz), new Vector3(0.17f, 0.01f, 0.17f), Shapes.Hex("#f2efe6"), face, "Speedometer");
                Shapes.Make(PrimitiveType.Cylinder, body, new Vector3(0.16f, gy, gz), new Vector3(0.13f, 0.01f, 0.13f), Shapes.Hex("#f2efe6"), face, "FuelGauge");
                // Красная зона «пусто» на датчике топлива
                Shapes.Box(body, new Vector3(0.16f - 0.045f, gy + 0.03f, gz - 0.012f), new Vector3(0.02f, 0.02f, 0.003f), Shapes.Hex("#d02020"));
                visual.speedNeedle = Needle(body, new Vector3(-0.08f, gy, gz - 0.015f), 0.07f);
                visual.fuelNeedle = Needle(body, new Vector3(0.16f, gy, gz - 0.015f), 0.055f);
                visual.driverEyes = Shapes.Group("DriverEyes", root, new Vector3(-0.4f, bodyTop + 0.4f, seatZ - 0.05f));
            }
            else
            {
                // Водитель: голова и туловище — чтобы было видно, что в машине кто-то сидит
                var shirt = Shirts[Random.Range(0, Shirts.Length)];
                Shapes.Box(body, new Vector3(-0.4f, bodyTop - 0.02f, seatZ - 0.08f), new Vector3(0.46f, 0.5f, 0.28f), shirt, name: "Torso");
                visual.driverHead = Shapes.Make(PrimitiveType.Sphere, body, new Vector3(-0.4f, bodyTop + 0.36f, seatZ - 0.05f),
                    Vector3.one * 0.27f, Skin, name: "Head").transform;
            }

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

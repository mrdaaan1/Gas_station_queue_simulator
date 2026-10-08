using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// «Болид» Академика: евроконтейнер на 1100 л (зелёный, кверху расширяется, скруглённые углы, толстый обод,
    /// рёбра жёсткости, ручки, петли от крышки — самой крышки нет) на картинговой раме с четырьмя колёсами.
    /// Из бака торчит Академик в полнолицевом шлеме: салатовый с белой полосой, тёмный визор.
    /// Как и спорткары — чистые данные (<see cref="ModelKit"/>), в игру переносит <see cref="ModelSpawner"/>.
    /// Материал «paint» — краска бака. Вперёд — +Z.
    /// </summary>
    public static class AkademikModel
    {
        public const float WheelR = 0.17f;
        public const float WheelX = 0.68f, WheelZ = 0.6f;
        const float Bottom = 0.32f, Height = 1.0f;
        const float HxBottom = 0.52f, HxTop = 0.6f, HzBottom = 0.63f, HzTop = 0.71f;
        const float Squareness = 0.16f; // показатель суперэллипса сечения: меньше — квадратнее

        static Model cached;

        public static Model Get()
        {
            if (cached != null) return cached;
            var model = new Model();
            var body = model.root.Child("Bin");
            BuildBin(body);
            BuildRider(body.Child("Rider", new Vector3(0f, Bottom + Height + 0.2f, 0.1f)));
            BuildChassis(model.root);
            BuildWheels(model.root);
            cached = model;
            return model;
        }

        static float Hx(float v) => Mathf.Lerp(HxBottom, HxTop, v);
        static float Hz(float v) => Mathf.Lerp(HzBottom, HzTop, v);

        /// <summary>Точка контура сечения (суперэллипс) по углу 0…1, на высоте v (0 — дно, 1 — верх), с отступом наружу.</summary>
        static Vector3 Rim(float u, float v, float grow = 0f)
        {
            float a = u * Mathf.PI * 2f;
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            float x = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), Squareness);
            float z = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), Squareness);
            return new Vector3(x * (Hx(v) + grow), Bottom + v * Height, z * (Hz(v) + grow));
        }

        static void BuildBin(ModelNode node)
        {
            var f = Frame.Identity;
            var paint = node.M("paint");
            var inner = node.M("bin_inner");
            // Стенки снаружи и изнутри (толщина 3 см), дно изнутри и снаружи
            Geo.Surface(paint, f, 96, 10, (u, v) => Rim(u, v), true);
            Geo.Surface(inner, f, 96, 6, (u, v) => Rim(u, v, -0.03f) + Vector3.up * 0.0f, false);
            Geo.Surface(inner, f, 48, 4, (u, w) =>
            {
                var p = Rim(u, 0.03f, -0.03f);
                return new Vector3(p.x * w, p.y, p.z * w);
            }, false);
            Geo.Surface(paint, f, 48, 3, (u, w) =>
            {
                var p = Rim(u, 0f);
                return new Vector3(p.x * w, p.y, p.z * w);
            }, true);

            // Толстый обод по верху
            var top = new List<Vector3>();
            for (int i = 0; i < 96; i++) top.Add(Rim(i / 96f, 1f, 0.012f) + Vector3.up * 0.01f);
            Geo.Sweep(paint, f, top, Geo.Circle(0.035f, 10, 0.045f), true);
            // Уступ-пояс под ободом (бак там чуть шире)
            var belt = new List<Vector3>();
            for (int i = 0; i < 96; i++) belt.Add(Rim(i / 96f, 0.84f, 0.012f));
            Geo.Sweep(paint, f, belt, Geo.Circle(0.045f, 8, 0.018f), true);
            // Нижний пояс над рамой
            var low = new List<Vector3>();
            for (int i = 0; i < 96; i++) low.Add(Rim(i / 96f, 0.04f, 0.008f));
            Geo.Sweep(node.M("paint_dark"), f, low, Geo.Circle(0.03f, 8, 0.016f), true);

            // Вертикальные рёбра жёсткости: по два на боках, по одному спереди и сзади, по наклону стенки
            float lean = Mathf.Atan2(HxTop - HxBottom, Height) * Mathf.Rad2Deg;
            float leanZ = Mathf.Atan2(HzTop - HzBottom, Height) * Mathf.Rad2Deg;
            foreach (float side in new[] { -1f, 1f })
            {
                foreach (float z in new[] { -0.3f, 0.3f })
                {
                    var c = new Vector3(side * (Hx(0.42f) + 0.012f), Bottom + 0.42f * Height, z);
                    Geo.RoundBox(paint, Frame.Euler(c, new Vector3(0f, 0f, -lean * side)), Vector3.zero, new Vector3(0.05f, 0.72f, 0.08f), 0.35f, 8);
                }
                var cz = new Vector3(0f, Bottom + 0.42f * Height, side * (Hz(0.42f) + 0.012f));
                Geo.RoundBox(paint, Frame.Euler(cz, new Vector3(leanZ * side, 0f, 0f)), Vector3.zero, new Vector3(0.09f, 0.72f, 0.05f), 0.35f, 8);
                // Ниша под ручку и сама ручка спереди и сзади
                foreach (float x in new[] { -0.32f, 0.32f })
                {
                    var hc = new Vector3(x, Bottom + 0.9f * Height, side * (Hz(0.9f) + 0.03f));
                    Geo.RoundBox(node.M("paint_dark"), Frame.Euler(hc, Vector3.zero), Vector3.zero, new Vector3(0.24f, 0.05f, 0.05f), 0.5f, 8);
                }
            }

            // Петли от крышки сзади вверху: кронштейны и ось
            var hinge = node.M("black_satin");
            float hz = -(HzTop + 0.06f);
            foreach (float x in new[] { -0.42f, -0.14f, 0.14f, 0.42f })
                Geo.RoundBox(hinge, f, new Vector3(x, Bottom + Height + 0.02f, hz + 0.02f), new Vector3(0.07f, 0.1f, 0.1f), 0.4f, 6);
            Geo.Cylinder(hinge, Frame.Euler(new Vector3(0f, Bottom + Height + 0.04f, hz), new Vector3(0f, 0f, 90f)), 0.022f, -0.5f, 0.5f, 10);

            // Номер-наклейка «1100 L» не нужна — вместо неё белая полоса-отражатель спереди
            Geo.RoundBox(node.M("plate"), f, new Vector3(0f, Bottom + 0.62f * Height, Hz(0.62f) + 0.012f), new Vector3(0.36f, 0.08f, 0.012f), 0.3f, 6);
        }

        static void BuildRider(ModelNode node)
        {
            var f = Frame.Identity;
            // Плечи в тёмной куртке чуть выглядывают над краем
            Geo.RoundBox(node.M("jacket_dark"), f, new Vector3(0f, -0.25f, -0.02f), new Vector3(0.46f, 0.2f, 0.26f), 0.55f, 10);
            Geo.Cylinder(node.M("jacket_dark"), f, 0.065f, -0.2f, -0.1f, 14);

            // Полнолицевой шлем: эллипсоид, снизу срезан; белая полоса по центру сверху, тёмный визор спереди
            const float rx = 0.145f, ry = 0.155f, rz = 0.168f;
            Vector3 S(float u, float w, float grow)
            {
                float th = Mathf.Lerp(-0.38f, 1f, u) * Mathf.PI / 2f; // снизу срез
                float ph = w * Mathf.PI * 2f;
                return new Vector3((rx + grow) * Mathf.Cos(th) * Mathf.Sin(ph), (ry + grow) * Mathf.Sin(th), (rz + grow) * Mathf.Cos(th) * Mathf.Cos(ph));
            }
            // Точка шлема по углам: th — широта (вверх), ph — долгота (0 — вперёд)
            Vector3 A(float th, float ph, float grow) =>
                new Vector3((rx + grow) * Mathf.Cos(th) * Mathf.Sin(ph), (ry + grow) * Mathf.Sin(th), (rz + grow) * Mathf.Cos(th) * Mathf.Cos(ph));
            Geo.Surface(node.M("helmet_lime"), f, 40, 80, (u, w) => S(u, w, 0f), true);
            // Белая полоса через макушку — отдельной лентой поверх, ровными краями
            Geo.Surface(node.M("helmet_white"), f, 6, 48, (u, w) =>
            {
                float x = Mathf.Lerp(-0.042f, 0.042f, u) / rx;
                float k = Mathf.Sqrt(1f - x * x);
                float a = Mathf.Lerp(0.5f, Mathf.PI + 0.35f, w);
                return new Vector3(x * (rx + 0.002f), (ry + 0.002f) * k * Mathf.Sin(a), (rz + 0.002f) * k * Mathf.Cos(a));
            }, false);
            // Тёмный визор спереди
            Geo.Surface(node.M("visor"), f, 10, 36, (u, w) => A(Mathf.Lerp(-0.34f, 0.3f, u), Mathf.Lerp(-1.05f, 1.05f, w), 0.006f), true);
            // Низ шлема — тёмная окантовка
            var ring = new List<Vector3>();
            for (int i = 0; i < 32; i++) ring.Add(S(0f, i / 32f, 0f));
            Geo.Sweep(node.M("black_satin"), f, ring, Geo.Circle(0.012f, 6), true);
            // Петли визора по бокам
            foreach (float side in new[] { -1f, 1f })
                Geo.RoundBox(node.M("black_satin"), f, new Vector3(side * (rx + 0.002f), 0.01f, 0.03f), new Vector3(0.012f, 0.035f, 0.035f), 0.5f, 6);
        }

        static void BuildChassis(ModelNode root)
        {
            var m = root.M("chassis");
            var f = Frame.Identity;
            // Рама из трубы под баком и две оси
            float y = WheelR;
            var frame = new List<Vector3>
            {
                new Vector3(-0.42f, Bottom - 0.04f, -0.62f), new Vector3(0.42f, Bottom - 0.04f, -0.62f),
                new Vector3(0.42f, Bottom - 0.04f, 0.62f), new Vector3(-0.42f, Bottom - 0.04f, 0.62f),
            };
            Geo.Tube(m, f, frame, 0.025f, 8, true);
            foreach (float z in new[] { -WheelZ, WheelZ })
            {
                Geo.Cylinder(m, Frame.Euler(new Vector3(0f, y, z), new Vector3(0f, 0f, 90f)), 0.022f, -WheelX + 0.05f, WheelX - 0.05f, 10);
                // Стойки от оси к раме
                foreach (float x in new[] { -0.42f, 0.42f })
                    Geo.Tube(m, f, new[] { new Vector3(x, y, z), new Vector3(x, Bottom - 0.04f, z * 1.03f) }, 0.02f, 8);
            }
        }

        static void BuildWheels(ModelNode root)
        {
            foreach (float side in new[] { -1f, 1f })
            foreach (bool front in new[] { true, false })
            {
                string tag = (front ? "F" : "R") + (side < 0 ? "L" : "R");
                var mount = root.Child("Mount" + tag, new Vector3(WheelX * side, WheelR, front ? WheelZ : -WheelZ));
                var wheel = mount.Child("Wheel" + tag, Vector3.zero, new Vector3(0f, 0f, 90f));
                var mesh = wheel.Child("Rim" + tag, Vector3.zero, side < 0 ? Vector3.zero : new Vector3(180f, 0f, 0f));
                // Картинговое колесо: широкая низкая шина и маленький диск
                WheelModel.FiveSpoke(mesh, WheelR, 0.19f, 6, 0.112f, "alloy", "chrome", 1.2f);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Режим «Самая быстрая гонка»: трасса к югу от города. Старт на длинной прямой, пять поворотов
    /// за бетонными отбойниками, потом трасса вливается в главную дорогу с юга (X ≈ 7, Z = −560) —
    /// и все едут прямиком в ту самую очередь на заправку. Финиш — на главной дороге за заправкой.
    /// Две полосы по 3,5 м: левая на главной дороге становится средним рядом, правая — рядом очереди.
    /// </summary>
    public static class RaceLayout
    {
        public const float LaneOffset = 1.75f;
        public const float HalfWidth = 5.2f;
        public const float CornerRadius = 18f;
        /// <summary>Где трасса заканчивается и начинается главная дорога.</summary>
        public const float JoinZ = -560f;
        /// <summary>Линия финиша поперёк главной дороги, за выездом с заправки.</summary>
        public const float FinishZ = 300f;
        /// <summary>Скорость на прямых для соперников, м/с (≈ 170 км/ч).</summary>
        public const float TopSpeed = 47f;
        /// <summary>Боковое ускорение в поворотах, м/с² — чем больше, тем быстрее проходят повороты.</summary>
        public const float CornerGrip = 14f;
        public const float Braking = 9f;

        /// <summary>Ломаная оси трассы (до скругления). Поворот, после которого загорается лампочка бензина, — последний.</summary>
        public static readonly Vector3[] Corners =
        {
            P(-420f, -640f), // старт, едем на восток
            P(-140f, -640f), // налево — на север
            P(-140f, -600f), // направо — на восток
            P(-60f, -600f),  // направо — на юг
            P(-60f, -700f),  // налево — на восток
            P(7f, -700f),    // налево — на север, к городу
            P(7f, JoinZ),
        };

        /// <summary>Индекс поворота, где загорается лампочка бензина (последний, перед выездом на главную).</summary>
        public const int FuelSignalCorner = 5;

        /// <summary>Расстояние от старта по оси трассы, где стоит стартовая линия (позади неё — решётка).</summary>
        public const float StartLineS = 40f;

        static Vector3 P(float x, float z) => new Vector3(x, 0f, z);

        /// <summary>Ось трассы со скруглёнными поворотами, шаг ~2 м на дугах.</summary>
        public static List<Vector3> Center()
        {
            var pts = new List<Vector3> { Corners[0] };
            for (int i = 1; i < Corners.Length - 1; i++)
            {
                var c = Corners[i];
                var d0 = (c - Corners[i - 1]).normalized;
                var d1 = (Corners[i + 1] - c).normalized;
                float angle = Vector3.Angle(d0, d1) * Mathf.Deg2Rad;
                float t = CornerRadius * Mathf.Tan(angle / 2f);
                var a = c - d0 * t;
                var b = c + d1 * t;
                pts.Add(a);
                int n = Mathf.Max(4, Mathf.CeilToInt(CornerRadius * angle / 2f));
                for (int k = 1; k < n; k++)
                {
                    // Квадратичная кривая через угол — почти дуга окружности
                    float u = k / (float)n;
                    pts.Add((1 - u) * (1 - u) * a + 2 * (1 - u) * u * c + u * u * b);
                }
                pts.Add(b);
            }
            pts.Add(Corners[Corners.Length - 1]);
            return pts;
        }

        /// <summary>Ломаная, сдвинутая вбок (+ вправо по ходу).</summary>
        public static List<Vector3> Offset(List<Vector3> pts, float offset)
        {
            var result = new List<Vector3>(pts.Count);
            for (int i = 0; i < pts.Count; i++)
            {
                var t0 = i > 0 ? (pts[i] - pts[i - 1]).normalized : Vector3.zero;
                var t1 = i < pts.Count - 1 ? (pts[i + 1] - pts[i]).normalized : Vector3.zero;
                var t = (t0 + t1).normalized;
                var right = new Vector3(t.z, 0f, -t.x);
                // На повороте сдвиг чуть больше, чтобы ширина полосы не сужалась
                float scale = t0 != Vector3.zero && t1 != Vector3.zero ? 1f / Mathf.Max(0.5f, Vector3.Dot(t, t1)) : 1f;
                result.Add(pts[i] + right * offset * scale);
            }
            return result;
        }

        /// <summary>Полоса трассы, продолженная по главной дороге до заправки (дальше машина встаёт в очередь).</summary>
        public static LanePath Lane(bool right)
        {
            float off = right ? LaneOffset : -LaneOffset;
            var pts = Offset(Center(), off);
            float x = right ? CityLayout.LaneQueue : CityLayout.LaneMiddle;
            pts.Add(new Vector3(x, 0f, JoinZ + 15f));
            return new LanePath(right ? "RaceRight" : "RaceLeft", TopSpeed, pts, false);
        }

        /// <summary>Обычные машины на трассе: по правой полосе, на главной дороге — в левый ряд и дальше по городу.</summary>
        public static LanePath TrafficLane()
        {
            var pts = Offset(Center(), LaneOffset);
            pts.Add(new Vector3(CityLayout.LaneMiddle, 0f, JoinZ + 25f));
            pts.Add(new Vector3(CityLayout.LaneLeft, 0f, JoinZ + 55f));
            pts.Add(new Vector3(CityLayout.LaneLeft, 0f, CityLayout.RoadEndZ));
            return new LanePath("RaceTraffic", 12f, pts, false);
        }

        /// <summary>Длина оси трассы до точки, ближайшей к углу (для сигнала «бензин» и подсчёта мест).</summary>
        public static float CenterS(Vector3 p)
        {
            var path = new LanePath("RaceCenter", TopSpeed, Center(), false);
            return path.Project(p, out _);
        }
    }

    /// <summary>
    /// Профиль скорости вдоль маршрута: на прямых — максимум, перед поворотом соперник заранее тормозит.
    /// Считается один раз по кривизне ломаной.
    /// </summary>
    public class SpeedProfile
    {
        const float Step = 2f;
        readonly float[] v;

        public SpeedProfile(LanePath path, float top, float grip, float braking)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(path.Length / Step) + 1);
            v = new float[n];
            for (int i = 0; i < n; i++)
            {
                float s = i * Step;
                var a = path.TangentAt(Mathf.Max(0f, s - 3f));
                var b = path.TangentAt(Mathf.Min(path.Length, s + 3f));
                float angle = Vector3.Angle(a, b) * Mathf.Deg2Rad;
                float curvature = angle / 6f;
                v[i] = curvature > 0.0005f ? Mathf.Min(top, Mathf.Sqrt(grip / curvature)) : top;
            }
            // Тормозим заранее: скорость в точке не больше, чем позволяет остановиться до следующей
            for (int i = n - 2; i >= 0; i--)
                v[i] = Mathf.Min(v[i], Mathf.Sqrt(v[i + 1] * v[i + 1] + 2f * braking * Step));
        }

        public float At(float s)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt(s / Step), 0, v.Length - 1);
            return v[i];
        }

        /// <summary>Прямой участок впереди (можно идти на обгон).</summary>
        public bool StraightAhead(float s, float distance, float top)
        {
            for (float d = 0f; d <= distance; d += Step * 2f)
                if (At(s + d) < top * 0.92f) return false;
            return true;
        }
    }
}

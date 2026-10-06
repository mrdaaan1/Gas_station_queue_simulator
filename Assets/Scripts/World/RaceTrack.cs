using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Описание трассы для гонки: ось, полосы соперников, профили скорости, старт и финиш.
    /// Две трассы: «Самая быстрая гонка» (<see cref="RaceLayout"/>, в конце — очередь на заправку)
    /// и уличная гонка по Тольятти (<see cref="TolyattiLayout"/>, финиш у «Тольятти Молла»).
    /// </summary>
    public class RaceTrack
    {
        public string Name;
        /// <summary>Трасса кончается очередью на заправку (классическая гонка). Иначе — финишная линия.</summary>
        public bool FuelStop;
        public List<Vector3> Center;
        public LanePath CenterPath, Left, Right, Traffic;
        public float StartLineS = 40f;
        public float TopSpeed = RaceLayout.TopSpeed, CornerGrip = RaceLayout.CornerGrip, Braking = RaceLayout.Braking;
        public float LaneOffset = RaceLayout.LaneOffset;

        /// <summary>Финишная линия (для уличной гонки): от A до B поперёк дороги.</summary>
        public Vector3 FinishA, FinishB;
        /// <summary>Расстояние до финиша по оси трассы.</summary>
        public float FinishS;

        /// <summary>Подсказки по маршруту: на каком расстоянии по оси что будет («налево на 70 лет Октября»).</summary>
        public readonly List<(float s, string text)> Hints = new List<(float, string)>();

        /// <summary>Сейчас выбранная трасса (задаёт GameBootstrap; в обычной игре — null).</summary>
        public static RaceTrack Current;

        public static RaceTrack Classic()
        {
            var center = RaceLayout.Center();
            return new RaceTrack
            {
                Name = "Самая быстрая гонка",
                FuelStop = true,
                Center = center,
                CenterPath = new LanePath("RaceCenter", 1f, center, false),
                Left = RaceLayout.Lane(false),
                Right = RaceLayout.Lane(true),
                Traffic = RaceLayout.TrafficLane(),
                StartLineS = RaceLayout.StartLineS,
            };
        }

        /// <summary>Отрезок from→to пересёк финишную линию (в сторону движения).</summary>
        public bool CrossedFinish(Vector3 from, Vector3 to)
        {
            var ab = FinishB - FinishA;
            var n = new Vector3(-ab.z, 0f, ab.x); // нормаль к линии
            float d0 = Vector3.Dot(from - FinishA, n), d1 = Vector3.Dot(to - FinishA, n);
            if (Mathf.Sign(d0) == Mathf.Sign(d1) || Mathf.Approximately(d0, d1)) return false;
            // Точка пересечения — между стойками
            var p = from + (to - from) * (d0 / (d0 - d1));
            float t = Vector3.Dot(p - FinishA, ab) / ab.sqrMagnitude;
            if (t < -0.05f || t > 1.05f) return false;
            // В сторону движения по трассе
            return Vector3.Dot(to - from, CenterPath.TangentAt(FinishS)) > 0f;
        }

        /// <summary>Ближайшая подсказка впереди (не дальше 600 м).</summary>
        public string HintAt(float s, out float meters)
        {
            meters = 0f;
            foreach (var h in Hints)
            {
                if (h.s < s - 5f) continue;
                meters = h.s - s;
                return meters < 600f ? h.text : null;
            }
            return null;
        }

        // ---------- Построение оси ----------

        /// <summary>Точки дуги окружности (углы в градусах, против часовой — по возрастанию; в мире X — восток, Z — север).</summary>
        public static void AddArc(List<Vector3> pts, Vector3 c, float r, float a0, float a1, float step = 2.5f)
        {
            float len = Mathf.Abs(a1 - a0) * Mathf.Deg2Rad * r;
            int n = Mathf.Max(2, Mathf.CeilToInt(len / step));
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad;
                pts.Add(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r);
            }
        }

        /// <summary>Кривая Эрмита от p0 (направление t0) к p1 (направление t1) — плавный въезд и съезд.</summary>
        public static void AddHermite(List<Vector3> pts, Vector3 p0, Vector3 t0, Vector3 p1, Vector3 t1, float step = 2f)
        {
            float len = Vector3.Distance(p0, p1);
            var m0 = t0.normalized * len;
            var m1 = t1.normalized * len;
            int n = Mathf.Max(3, Mathf.CeilToInt(len * 1.2f / step));
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n, t2 = t * t, t3 = t2 * t;
                pts.Add((2 * t3 - 3 * t2 + 1) * p0 + (t3 - 2 * t2 + t) * m0 + (-2 * t3 + 3 * t2) * p1 + (t3 - t2) * m1);
            }
        }

        /// <summary>Поворот на перекрёстке: от прямой a→corner к corner→b дугой радиуса r.</summary>
        public static void AddCorner(List<Vector3> pts, Vector3 from, Vector3 corner, Vector3 to, float r)
        {
            var d0 = (corner - from).normalized;
            var d1 = (to - corner).normalized;
            float angle = Vector3.Angle(d0, d1) * Mathf.Deg2Rad;
            float t = r * Mathf.Tan(angle / 2f);
            var a = corner - d0 * t;
            var b = corner + d1 * t;
            pts.Add(a);
            int n = Mathf.Max(4, Mathf.CeilToInt(r * angle / 2f));
            for (int k = 1; k < n; k++)
            {
                float u = k / (float)n;
                // Квадратичная кривая через угол — почти дуга
                pts.Add((1 - u) * (1 - u) * a + 2 * (1 - u) * u * corner + u * u * b);
            }
            pts.Add(b);
        }

        /// <summary>Пересечение прямых p + d·t и q + e·u на плоскости XZ.</summary>
        public static Vector3 Intersect(Vector3 p, Vector3 d, Vector3 q, Vector3 e)
        {
            float den = d.x * e.z - d.z * e.x;
            if (Mathf.Abs(den) < 1e-6f) return p;
            float t = ((q.x - p.x) * e.z - (q.z - p.z) * e.x) / den;
            return p + d * t;
        }

        /// <summary>Убрать точки ближе 0,5 м друг к другу (на стыках кусков оси).</summary>
        public static List<Vector3> Clean(List<Vector3> pts)
        {
            var result = new List<Vector3>();
            foreach (var p in pts)
                if (result.Count == 0 || (p - result[result.Count - 1]).sqrMagnitude > 0.25f) result.Add(new Vector3(p.x, 0f, p.z));
            return result;
        }
    }
}

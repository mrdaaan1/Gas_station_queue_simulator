using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Маршрут, по которому едут машины NPC: ломаная по земле (Y = 0) с плавными поворотами.
    /// s — пройденное расстояние вдоль маршрута в метрах.
    /// </summary>
    public class LanePath
    {
        public readonly string name;
        public readonly float speedLimit;
        public float Length { get; }

        readonly Vector3[] points;
        readonly float[] cumulative;

        public LanePath(string name, float speedLimit, IList<Vector3> controlPoints, bool smooth = true)
        {
            this.name = name;
            this.speedLimit = speedLimit;
            points = smooth ? Smooth(controlPoints) : ToArray(controlPoints);
            cumulative = new float[points.Length];
            for (int i = 1; i < points.Length; i++)
                cumulative[i] = cumulative[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            Length = cumulative[points.Length - 1];
        }

        public Vector3 Start => points[0];
        public Vector3 End => points[points.Length - 1];

        int SegmentAt(float s)
        {
            if (s <= 0f) return 0;
            if (s >= Length) return points.Length - 2;
            int lo = 0, hi = points.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (cumulative[mid] <= s) lo = mid; else hi = mid;
            }
            return lo;
        }

        public Vector3 PointAt(float s)
        {
            int i = SegmentAt(s);
            float segLen = cumulative[i + 1] - cumulative[i];
            float t = segLen > 0.0001f ? Mathf.Clamp01((s - cumulative[i]) / segLen) : 0f;
            return Vector3.Lerp(points[i], points[i + 1], t);
        }

        public Vector3 TangentAt(float s)
        {
            int i = SegmentAt(s);
            var d = points[i + 1] - points[i];
            d.y = 0f;
            return d.sqrMagnitude > 0.000001f ? d.normalized : Vector3.forward;
        }

        /// <summary>Вектор «вправо» от направления движения.</summary>
        public Vector3 RightAt(float s)
        {
            var t = TangentAt(s);
            return new Vector3(t.z, 0f, -t.x);
        }

        /// <summary>Ближайшая точка маршрута: возвращает s, а в lateral — смещение вправо (+) или влево (−).</summary>
        public float Project(Vector3 p, out float lateral)
        {
            float bestS = 0f, bestD = float.MaxValue;
            lateral = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = points[i];
                var ab = points[i + 1] - a;
                ab.y = 0f;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 0.000001f ? Mathf.Clamp01(Vector3.Dot(new Vector3(p.x - a.x, 0f, p.z - a.z), ab) / len2) : 0f;
                var q = a + ab * t;
                float dx = p.x - q.x, dz = p.z - q.z;
                float d = dx * dx + dz * dz;
                if (d < bestD)
                {
                    bestD = d;
                    bestS = cumulative[i] + Mathf.Sqrt(len2) * t;
                    var right = len2 > 0.000001f ? new Vector3(ab.z, 0f, -ab.x) / Mathf.Sqrt(len2) : Vector3.right;
                    lateral = dx * right.x + dz * right.z;
                }
            }
            return bestS;
        }

        static Vector3[] ToArray(IList<Vector3> pts)
        {
            var arr = new Vector3[pts.Count];
            for (int i = 0; i < pts.Count; i++) arr[i] = new Vector3(pts[i].x, 0f, pts[i].z);
            return arr;
        }

        /// <summary>Скругляет углы ломаной (Catmull-Rom). Длинные прямые остаются прямыми.</summary>
        static Vector3[] Smooth(IList<Vector3> pts)
        {
            if (pts.Count < 3) return ToArray(pts);
            var result = new List<Vector3> { Flat(pts[0]) };
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p1 = Flat(pts[i]);
                var p2 = Flat(pts[i + 1]);
                float len = Vector3.Distance(p1, p2);
                // Соседние точки подтягиваем на длину текущего отрезка, иначе длинная прямая даёт петлю
                var p0 = Near(p1, Flat(pts[Mathf.Max(0, i - 1)]), len);
                var p3 = Near(p2, Flat(pts[Mathf.Min(pts.Count - 1, i + 2)]), len);
                int steps = Mathf.Clamp(Mathf.CeilToInt(len / 1.5f), 1, 12);
                if (len > 30f) steps = 1; // длинный прямой участок дороги
                for (int k = 1; k <= steps; k++)
                {
                    float t = k / (float)steps;
                    result.Add(steps == 1 ? p2 : CatmullRom(p0, p1, p2, p3, t));
                }
            }
            return result.ToArray();
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        static Vector3 Near(Vector3 from, Vector3 to, float maxDist)
        {
            var d = to - from;
            float m = Mathf.Sqrt(d.sqrMagnitude);
            return m > maxDist && m > 0.0001f ? from + d / m * maxDist : to;
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }

    /// <summary>
    /// Повёрнутый прямоугольник на земле (вид сверху) — «габарит» машины, столба, стены.
    /// Координаты: x — мировой X, y — мировой Z.
    /// </summary>
    public struct Obb
    {
        public Vector2 center;
        public Vector2 forward; // единичный
        public Vector2 half;    // x — полуширина (вправо), y — полудлина (вперёд)

        public Vector2 Right => new Vector2(forward.y, -forward.x);

        public Obb(Vector3 position, Vector3 fwd, float width, float length)
        {
            center = new Vector2(position.x, position.z);
            var f = new Vector2(fwd.x, fwd.z);
            forward = f.sqrMagnitude > 0.000001f ? f.normalized : new Vector2(0f, 1f);
            half = new Vector2(width / 2f, length / 2f);
        }

        public static Obb Axis(Vector3 center, float sizeX, float sizeZ) =>
            new Obb(center, Vector3.forward, sizeX, sizeZ);

        float Radius(Vector2 axis) =>
            half.x * Mathf.Abs(Vector2.Dot(Right, axis)) + half.y * Mathf.Abs(Vector2.Dot(forward, axis));

        /// <summary>
        /// Пересекаются ли a и b. mtv — на сколько сдвинуть a, чтобы они перестали пересекаться.
        /// </summary>
        public static bool Overlap(Obb a, Obb b, out Vector2 mtv)
        {
            mtv = Vector2.zero;
            float best = float.MaxValue;
            Vector2 bestAxis = Vector2.zero;
            var d = a.center - b.center;
            for (int i = 0; i < 4; i++)
            {
                var axis = i == 0 ? a.forward : i == 1 ? a.Right : i == 2 ? b.forward : b.Right;
                float dist = Vector2.Dot(d, axis);
                float overlap = a.Radius(axis) + b.Radius(axis) - Mathf.Abs(dist);
                if (overlap <= 0f) return false;
                if (overlap < best)
                {
                    best = overlap;
                    bestAxis = dist < 0f ? -axis : axis;
                }
            }
            mtv = bestAxis * best;
            return true;
        }

        public static bool Overlap(Obb a, Obb b) => Overlap(a, b, out _);

        /// <summary>Вытолкнуть круг (пешеход) из прямоугольника. Возвращает сдвиг круга.</summary>
        public bool PushCircle(Vector2 p, float radius, out Vector2 push)
        {
            var rel = p - center;
            float lx = Vector2.Dot(rel, Right), lz = Vector2.Dot(rel, forward);
            float cx = Mathf.Clamp(lx, -half.x, half.x), cz = Mathf.Clamp(lz, -half.y, half.y);
            var closest = center + Right * cx + forward * cz;
            var diff = p - closest;
            float dist = diff.magnitude;
            push = Vector2.zero;
            if (dist >= radius) return false;
            if (dist < 0.0001f)
            {
                // Центр круга внутри прямоугольника — выталкиваем к ближайшей стороне
                float ox = half.x - Mathf.Abs(lx), oz = half.y - Mathf.Abs(lz);
                push = ox < oz ? Right * Mathf.Sign(lx == 0 ? 1 : lx) * (ox + radius) : forward * Mathf.Sign(lz == 0 ? 1 : lz) * (oz + radius);
                return true;
            }
            push = diff / dist * (radius - dist);
            return true;
        }

        /// <summary>
        /// Взгляд «датчиком» вперёд: на каком расстоянии (вдоль forward от передней кромки «me»)
        /// начинается other в коридоре шириной corridorHalf. Возвращает false, если other не в коридоре.
        /// </summary>
        public static bool AheadDistance(Obb me, Obb other, float corridorHalf, float range, out float distance)
        {
            distance = float.MaxValue;
            var r = me.Right;
            var f = me.forward;
            var oc = other.center - me.center;
            var or = other.Right * other.half.x;
            var of = other.forward * other.half.y;

            float minL = float.MaxValue, maxL = float.MinValue, minF = float.MaxValue, maxF = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var c = oc + (i < 2 ? or : -or) + (i % 2 == 0 ? of : -of);
                float l = Vector2.Dot(c, r), fw = Vector2.Dot(c, f);
                if (l < minL) minL = l;
                if (l > maxL) maxL = l;
                if (fw < minF) minF = fw;
                if (fw > maxF) maxF = fw;
            }
            if (maxL < -corridorHalf || minL > corridorHalf) return false; // не в нашей полосе
            if (maxF < me.half.y - 0.5f) return false;                       // сзади или рядом
            float gap = minF - me.half.y;
            if (gap > range) return false;
            distance = Mathf.Max(0f, gap);
            return true;
        }
    }
}

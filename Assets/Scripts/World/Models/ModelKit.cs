using System;
using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Сетка одной детали (один материал): вершины, нормали, UV, треугольники.
    /// Чистые данные без Unity-объектов — так модель можно собрать и проверить вне редактора.
    /// </summary>
    public class MeshData
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();
        /// <summary>Запечённое затенение по вершинам (0 — тень, 1 — светло), если модель из Blender; иначе null.</summary>
        public List<float> ao;

        public int Add(Vector3 p, Vector3 normal, Vector2 texcoord = default)
        {
            v.Add(p);
            n.Add(normal);
            uv.Add(texcoord);
            return v.Count - 1;
        }

        public void Tri(int a, int b, int c)
        {
            t.Add(a); t.Add(b); t.Add(c);
        }

        public void Quad(int a, int b, int c, int d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }
    }

    /// <summary>Узел модели: своё положение и поворот, свои сетки по материалам, дочерние узлы.</summary>
    public class ModelNode
    {
        public string name;
        public ModelNode parent;
        public Vector3 pos, euler;
        public readonly List<string> mats = new List<string>();
        public readonly List<MeshData> meshes = new List<MeshData>();
        public readonly List<ModelNode> children = new List<ModelNode>();
        /// <summary>Нормали готовые (модель из файла Blender) — не пересчитывать при создании сетки.</summary>
        public bool keepNormals;

        public MeshData M(string mat)
        {
            int i = mats.IndexOf(mat);
            if (i >= 0) return meshes[i];
            mats.Add(mat);
            var m = new MeshData();
            meshes.Add(m);
            return m;
        }

        public ModelNode Child(string childName, Vector3 localPos = default, Vector3 localEuler = default)
        {
            var c = new ModelNode { name = childName, parent = this, pos = localPos, euler = localEuler };
            children.Add(c);
            return c;
        }

        public ModelNode Find(string target)
        {
            if (name == target) return this;
            foreach (var c in children)
            {
                var f = c.Find(target);
                if (f != null) return f;
            }
            return null;
        }

        /// <summary>Мировая (относительно корня модели) система координат узла.</summary>
        public Frame WorldFrame()
        {
            var f = Frame.Euler(pos, euler);
            return parent == null ? f : parent.WorldFrame().Mul(f);
        }
    }

    /// <summary>Система координат: начало и три оси. Поворот задаётся как в Unity (Euler: Z, потом X, потом Y).</summary>
    public struct Frame
    {
        public Vector3 o, x, y, z;

        public static Frame Identity => new Frame { o = Vector3.zero, x = Vector3.right, y = Vector3.up, z = Vector3.forward };

        public static Frame Euler(Vector3 pos, Vector3 euler)
        {
            return new Frame { o = pos, x = Rot(euler, Vector3.right), y = Rot(euler, Vector3.up), z = Rot(euler, Vector3.forward) };
        }

        /// <summary>Система координат с осью z вдоль forward и y по возможности вверх.</summary>
        public static Frame Look(Vector3 pos, Vector3 forward, Vector3 upHint)
        {
            var z = forward.normalized;
            var x = Vector3.Cross(upHint, z);
            if (x.sqrMagnitude < 1e-8f) x = Vector3.Cross(Vector3.right, z);
            x = x.normalized;
            var y = Vector3.Cross(z, x);
            return new Frame { o = pos, x = x, y = y, z = z };
        }

        public static Vector3 Rot(Vector3 euler, Vector3 v)
        {
            float d = Mathf.Deg2Rad;
            float cz = Mathf.Cos(euler.z * d), sz = Mathf.Sin(euler.z * d);
            v = new Vector3(v.x * cz - v.y * sz, v.x * sz + v.y * cz, v.z);
            float cx = Mathf.Cos(euler.x * d), sx = Mathf.Sin(euler.x * d);
            v = new Vector3(v.x, v.y * cx - v.z * sx, v.y * sx + v.z * cx);
            float cy = Mathf.Cos(euler.y * d), sy = Mathf.Sin(euler.y * d);
            return new Vector3(v.x * cy + v.z * sy, v.y, -v.x * sy + v.z * cy);
        }

        public Vector3 P(Vector3 local) => o + x * local.x + y * local.y + z * local.z;
        public Vector3 P(float lx, float ly, float lz) => o + x * lx + y * ly + z * lz;
        public Vector3 D(Vector3 local) => x * local.x + y * local.y + z * local.z;
        public Frame Mul(Frame child) => new Frame { o = P(child.o), x = D(child.x), y = D(child.y), z = D(child.z) };

        /// <summary>Перевод точки из системы модели в эту систему (оси ортонормированы).</summary>
        public Vector3 ToLocal(Vector3 p)
        {
            var d = p - o;
            return new Vector3(Vector3.Dot(d, x), Vector3.Dot(d, y), Vector3.Dot(d, z));
        }

        public Vector3 DirToLocal(Vector3 dir) => new Vector3(Vector3.Dot(dir, x), Vector3.Dot(dir, y), Vector3.Dot(dir, z));
    }

    /// <summary>Гладкая кривая y(x) по ключам. Ключ-«излом» (corner) даёт острый угол — для линий кузова.</summary>
    public class Curve
    {
        readonly List<float> xs = new List<float>();
        readonly List<float> ys = new List<float>();
        readonly List<bool> corner = new List<bool>();
        float[] m;

        public Curve Key(float x, float y, bool isCorner = false)
        {
            xs.Add(x);
            ys.Add(y);
            corner.Add(isCorner);
            m = null;
            return this;
        }

        void Prepare()
        {
            int k = xs.Count;
            // Ключи можно задавать в любом порядке
            for (int i = 1; i < k; i++)
                for (int j = i; j > 0 && xs[j] < xs[j - 1]; j--)
                {
                    (xs[j], xs[j - 1]) = (xs[j - 1], xs[j]);
                    (ys[j], ys[j - 1]) = (ys[j - 1], ys[j]);
                    (corner[j], corner[j - 1]) = (corner[j - 1], corner[j]);
                }
            // Монотонная кубическая интерполяция (Фрича–Карлсон): без «перелётов» между ключами
            var d = new float[Math.Max(1, k - 1)];
            for (int i = 0; i < k - 1; i++) d[i] = (ys[i + 1] - ys[i]) / (xs[i + 1] - xs[i]);
            m = new float[k * 2]; // для каждого ключа: наклон слева и справа
            for (int i = 0; i < k; i++)
            {
                float left = i > 0 ? d[i - 1] : (k > 1 ? d[0] : 0f);
                float right = i < k - 1 ? d[i] : left;
                float s;
                if (corner[i]) { m[i * 2] = left; m[i * 2 + 1] = right; continue; }
                if (i == 0) s = right;
                else if (i == k - 1) s = left;
                else if (left * right <= 0f) s = 0f;
                else s = 2f / (1f / left + 1f / right) * 0.5f + (left + right) * 0.25f; // смесь гармонического и среднего
                if (i > 0 && i < k - 1 && left * right > 0f)
                {
                    float lim = 3f * Mathf.Min(Mathf.Abs(left), Mathf.Abs(right));
                    if (Mathf.Abs(s) > lim) s = Mathf.Sign(s) * lim;
                }
                m[i * 2] = s;
                m[i * 2 + 1] = s;
            }
        }

        public float this[float x] => Eval(x);

        public float Eval(float x)
        {
            if (m == null) Prepare();
            int k = xs.Count;
            if (k == 1) return ys[0];
            if (x <= xs[0]) return ys[0];
            if (x >= xs[k - 1]) return ys[k - 1];
            int lo = 0, hi = k - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (xs[mid] > x) hi = mid; else lo = mid;
            }
            float h = xs[hi] - xs[lo];
            float t = (x - xs[lo]) / h;
            float t2 = t * t, t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * ys[lo] + (t3 - 2 * t2 + t) * h * m[lo * 2 + 1] +
                   (-2 * t3 + 3 * t2) * ys[hi] + (t3 - t2) * h * m[hi * 2];
        }
    }

    /// <summary>Построители форм: поверхности по формуле, тела вращения, трубки, скруглённые коробки.</summary>
    public static class Geo
    {
        public static Vector2 Dir(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));

        /// <summary>Кубический сегмент Эрмита между двумя точками с заданными касательными.</summary>
        public static Vector2 Hermite(Vector2 p0, Vector2 m0, Vector2 p1, Vector2 m1, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * p0 + (t3 - 2 * t2 + t) * m0 + (-2 * t3 + 3 * t2) * p1 + (t3 - t2) * m1;
        }

        /// <summary>
        /// Поверхность по формуле P(u, v), u и v от 0 до 1. Нормали — по производным формулы,
        /// поэтому стыки соседних кусков одной гладкой поверхности не видны.
        /// outward — куда смотрит лицевая сторона (если null — как получится из порядка u, v; flip разворачивает).
        /// </summary>
        public static void Surface(MeshData m, Frame f, int nu, int nv, Func<float, float, Vector3> P, bool flip = false,
            Func<float, float, bool> keep = null, float uvScale = 1f)
        {
            var idx = new int[nu + 1, nv + 1];
            for (int i = 0; i <= nu; i++)
            for (int j = 0; j <= nv; j++)
            {
                float u = i / (float)nu, w = j / (float)nv;
                var p = P(u, w);
                var nrm = NormalAt(P, u, w, flip);
                idx[i, j] = m.Add(f.P(p), f.D(nrm), new Vector2(u * uvScale, w * uvScale));
            }
            for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
            {
                if (keep != null && !keep((i + 0.5f) / nu, (j + 0.5f) / nv)) continue;
                if (flip) m.Quad(idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i + 1, j]);
                else m.Quad(idx[i, j], idx[i + 1, j], idx[i + 1, j + 1], idx[i, j + 1]);
            }
        }

        public static Vector3 NormalAt(Func<float, float, Vector3> P, float u, float w, bool flip)
        {
            const float e = 0.002f;
            float u0 = Mathf.Max(0f, u - e), u1 = Mathf.Min(1f, u + e);
            float w0 = Mathf.Max(0f, w - e), w1 = Mathf.Min(1f, w + e);
            var du = P(u1, w) - P(u0, w);
            var dw = P(u, w1) - P(u, w0);
            var nrm = Vector3.Cross(du, dw);
            if (nrm.sqrMagnitude < 1e-14f)
            {
                // Вырожденная точка (полюс): берём соседнюю
                float uu = u < 0.5f ? u + 0.02f : u - 0.02f, ww = w < 0.5f ? w + 0.02f : w - 0.02f;
                du = P(Mathf.Min(1f, uu + e), ww) - P(Mathf.Max(0f, uu - e), ww);
                dw = P(uu, Mathf.Min(1f, ww + e)) - P(uu, Mathf.Max(0f, ww - e));
                nrm = Vector3.Cross(du, dw);
            }
            nrm = nrm.normalized;
            return flip ? -nrm : nrm;
        }

        /// <summary>
        /// Тело вращения вокруг оси Y системы f. profile — точки (радиус, высота) по порядку;
        /// smooth — сглаживать нормали между отрезками профиля (иначе гранёный профиль, как у фаски).
        /// inward — нормали внутрь (внутренняя сторона обода, трубы).
        /// </summary>
        public static void Lathe(MeshData m, Frame f, IList<Vector2> profile, int segments, bool smooth = true, bool inward = false,
            float a0 = 0f, float a1 = 360f)
        {
            int k = profile.Count;
            // Нормали профиля в плоскости (r, h)
            var segN = new Vector2[k - 1];
            for (int i = 0; i < k - 1; i++)
            {
                var d = profile[i + 1] - profile[i];
                var nn = new Vector2(d.y, -d.x);
                segN[i] = nn.sqrMagnitude > 1e-12f ? nn.normalized : new Vector2(1, 0);
                if (inward) segN[i] = -segN[i];
            }
            bool full = Mathf.Abs(a1 - a0) >= 359.9f;
            for (int s = 0; s < k - 1; s++)
            {
                var start = new int[segments + 1, 2];
                for (int a = 0; a <= segments; a++)
                {
                    float ang = Mathf.Lerp(a0, a1, a / (float)segments) * Mathf.Deg2Rad;
                    float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                    for (int e = 0; e < 2; e++)
                    {
                        int pi = s + e;
                        var pr = profile[pi];
                        Vector2 nn = segN[s];
                        if (smooth)
                        {
                            var sum = Vector2.zero;
                            if (pi > 0) sum += segN[pi - 1];
                            if (pi < k - 1) sum += segN[pi];
                            nn = sum.sqrMagnitude > 1e-12f ? sum.normalized : segN[s];
                        }
                        var p = new Vector3(pr.x * c, pr.y, pr.x * sn);
                        var nrm = new Vector3(nn.x * c, nn.y, nn.x * sn);
                        start[a, e] = m.Add(f.P(p), f.D(nrm), new Vector2(a / (float)segments, pi / (float)(k - 1)));
                    }
                }
                for (int a = 0; a < segments; a++)
                {
                    int b = a + 1;
                    if (b > segments) b = full ? 0 : segments;
                    if (!inward) m.Quad(start[a, 0], start[a, 1], start[b, 1], start[b, 0]);
                    else m.Quad(start[a, 0], start[b, 0], start[b, 1], start[a, 1]);
                }
            }
        }

        /// <summary>Трубка вдоль ломаной. closed — замкнутое кольцо.</summary>
        public static void Tube(MeshData m, Frame f, IList<Vector3> path, float radius, int sides = 8, bool closed = false, Vector3 upHint = default)
        {
            Sweep(m, f, path, Circle(radius, sides), closed, upHint, true);
        }

        public static List<Vector2> Circle(float r, int sides, float rx = -1f)
        {
            var list = new List<Vector2>();
            if (rx < 0) rx = r;
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                list.Add(new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * r));
            }
            return list;
        }

        /// <summary>
        /// Протянуть замкнутое сечение (точки в плоскости «вбок, вверх») вдоль пути. Сечение можно масштабировать по пути (scale).
        /// Концы закрываются плоскими крышками, если путь не замкнут.
        /// </summary>
        public static void Sweep(MeshData m, Frame f, IList<Vector3> path, IList<Vector2> section, bool closed, Vector3 upHint = default,
            bool smooth = true, Func<float, float> scale = null, bool caps = true)
        {
            if (upHint == default) upHint = Vector3.up;
            int n = path.Count, k = section.Count;
            var rings = new Vector3[n, k];
            var normals = new Vector3[n, k];
            var right = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 tan;
                if (closed) tan = path[(i + 1) % n] - path[(i - 1 + n) % n];
                else tan = path[Mathf.Min(n - 1, i + 1)] - path[Mathf.Max(0, i - 1)];
                tan = tan.normalized;
                // Перенос системы вдоль пути, чтобы сечение не перекручивалось
                if (i == 0)
                {
                    right = Vector3.Cross(upHint, tan);
                    if (right.sqrMagnitude < 1e-8f) right = Vector3.Cross(Vector3.forward, tan);
                    right = right.normalized;
                }
                else
                {
                    right = (right - tan * Vector3.Dot(right, tan));
                    if (right.sqrMagnitude < 1e-8f) right = Vector3.Cross(upHint, tan);
                    right = right.normalized;
                }
                var up = Vector3.Cross(tan, right);
                float sc = scale != null ? scale(n > 1 ? i / (float)(n - 1) : 0f) : 1f;
                for (int j = 0; j < k; j++)
                {
                    var s = section[j] * sc;
                    rings[i, j] = path[i] + right * s.x + up * s.y;
                    // Нормаль сечения: перпендикуляр к его контуру
                    var a = section[(j - 1 + k) % k];
                    var b = section[(j + 1) % k];
                    var d = b - a;
                    var nn = new Vector2(d.y, -d.x).normalized;
                    normals[i, j] = (right * nn.x + up * nn.y).normalized;
                }
            }
            // Ориентация: нормали наружу — проверяем по площади сечения
            float area = 0f;
            for (int j = 0; j < k; j++)
            {
                var a = section[j];
                var b = section[(j + 1) % k];
                area += a.x * b.y - b.x * a.y;
            }
            bool ccw = area > 0f;
            if (!ccw)
                for (int i = 0; i < n; i++) for (int j = 0; j < k; j++) normals[i, j] = -normals[i, j];

            var idx = new int[n, k];
            if (smooth)
                for (int i = 0; i < n; i++)
                for (int j = 0; j < k; j++)
                    idx[i, j] = m.Add(f.P(rings[i, j]), f.D(normals[i, j]), new Vector2(j / (float)k, i / (float)Mathf.Max(1, n - 1)));
            int last = closed ? n : n - 1;
            for (int i = 0; i < last; i++)
            {
                int i2 = (i + 1) % n;
                for (int j = 0; j < k; j++)
                {
                    int j2 = (j + 1) % k;
                    if (smooth)
                    {
                        if (ccw) m.Quad(idx[i, j], idx[i, j2], idx[i2, j2], idx[i2, j]);
                        else m.Quad(idx[i, j], idx[i2, j], idx[i2, j2], idx[i, j2]);
                    }
                    else
                    {
                        var a = rings[i, j]; var b = rings[i, j2]; var c = rings[i2, j2]; var d = rings[i2, j];
                        var nn = Vector3.Cross(b - a, d - a).normalized;
                        if (!ccw) nn = -nn;
                        // Нормаль грани наружу от оси пути
                        if (Vector3.Dot(nn, (a + c) * 0.5f - (path[i] + path[i2]) * 0.5f) < 0f) nn = -nn;
                        int ia = m.Add(f.P(a), f.D(nn)), ib = m.Add(f.P(b), f.D(nn)), ic = m.Add(f.P(c), f.D(nn)), id = m.Add(f.P(d), f.D(nn));
                        if (Vector3.Dot(Vector3.Cross(b - a, c - a), nn) > 0f) m.Quad(ia, ib, ic, id);
                        else m.Quad(ia, id, ic, ib);
                    }
                }
            }
            if (!closed && caps)
            {
                Cap(m, f, rings, 0, k, -(path[Mathf.Min(1, n - 1)] - path[0]).normalized);
                Cap(m, f, rings, n - 1, k, (path[n - 1] - path[Mathf.Max(0, n - 2)]).normalized);
            }
        }

        static void Cap(MeshData m, Frame f, Vector3[,] rings, int i, int k, Vector3 normal)
        {
            var c = Vector3.zero;
            for (int j = 0; j < k; j++) c += rings[i, j];
            c /= k;
            int ci = m.Add(f.P(c), f.D(normal));
            var ids = new int[k];
            for (int j = 0; j < k; j++) ids[j] = m.Add(f.P(rings[i, j]), f.D(normal));
            for (int j = 0; j < k; j++)
            {
                int j2 = (j + 1) % k;
                var a = rings[i, j]; var b = rings[i, j2];
                if (Vector3.Dot(Vector3.Cross(a - c, b - c), normal) > 0f) m.Tri(ci, ids[j], ids[j2]);
                else m.Tri(ci, ids[j2], ids[j]);
            }
        }

        /// <summary>
        /// Скруглённая «подушка» (суперэллипсоид): size — габариты, round — от 0 (кирпич) до 1 (эллипсоид).
        /// Годится для сидений, корпусов зеркал, кнопок, накладок.
        /// </summary>
        public static void RoundBox(MeshData m, Frame f, Vector3 center, Vector3 size, float round = 0.3f, int seg = 12)
        {
            float e = Mathf.Max(0.08f, round);
            var h = size * 0.5f;
            float Sp(float c) => Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e);
            Vector3 P(float u, float w)
            {
                float th = (u - 0.5f) * Mathf.PI;          // широта
                float ph = w * Mathf.PI * 2f;              // долгота
                float ct = Mathf.Cos(th), st = Mathf.Sin(th), cp = Mathf.Cos(ph), sp = Mathf.Sin(ph);
                return center + new Vector3(h.x * Sp(ct) * Sp(cp), h.y * Sp(st), h.z * Sp(ct) * Sp(sp));
            }
            // Нормали суперэллипсоида считаем по градиенту неявной формы — так рёбра не «плывут»
            Vector3 N(Vector3 p)
            {
                var q = p - center;
                float g = 2f / e;
                float ax = Mathf.Abs(q.x) / h.x, ay = Mathf.Abs(q.y) / h.y, az = Mathf.Abs(q.z) / h.z;
                var nn = new Vector3(Mathf.Sign(q.x) * Mathf.Pow(ax + 1e-6f, g - 1f) / h.x,
                    Mathf.Sign(q.y) * Mathf.Pow(ay + 1e-6f, g - 1f) / h.y,
                    Mathf.Sign(q.z) * Mathf.Pow(az + 1e-6f, g - 1f) / h.z);
                return nn.sqrMagnitude > 1e-12f ? nn.normalized : Vector3.up;
            }
            int nu = seg, nv = seg * 2;
            var idx = new int[nu + 1, nv + 1];
            for (int i = 0; i <= nu; i++)
            for (int j = 0; j <= nv; j++)
            {
                var p = P(i / (float)nu, j / (float)nv);
                var nn = N(p);
                if (i == 0) nn = Vector3.down;
                if (i == nu) nn = Vector3.up;
                idx[i, j] = m.Add(f.P(p), f.D(nn), new Vector2(j / (float)nv, i / (float)nu));
            }
            for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
                m.Quad(idx[i, j], idx[i + 1, j], idx[i + 1, j + 1], idx[i, j + 1]);
        }

        /// <summary>Обычная коробка с плоскими гранями.</summary>
        public static void Box(MeshData m, Frame f, Vector3 center, Vector3 size)
        {
            var h = size * 0.5f;
            void Face(Vector3 nrm, Vector3 a, Vector3 b)
            {
                var c = center + Vector3.Scale(nrm, h);
                var ha = Vector3.Scale(a, h); var hb = Vector3.Scale(b, h);
                var fn = f.D(nrm);
                int i0 = m.Add(f.P(c - ha - hb), fn, new Vector2(0, 0));
                int i1 = m.Add(f.P(c + ha - hb), fn, new Vector2(1, 0));
                int i2 = m.Add(f.P(c + ha + hb), fn, new Vector2(1, 1));
                int i3 = m.Add(f.P(c - ha + hb), fn, new Vector2(0, 1));
                // a × b = нормаль → обход против часовой, если смотреть снаружи; в Unity лицевая сторона — по часовой
                m.Quad(i0, i3, i2, i1);
            }
            Face(Vector3.right, Vector3.forward, Vector3.up);
            Face(Vector3.left, Vector3.back, Vector3.up);
            Face(Vector3.up, Vector3.right, Vector3.forward);
            Face(Vector3.down, Vector3.left, Vector3.forward);
            Face(Vector3.forward, Vector3.left, Vector3.up);
            Face(Vector3.back, Vector3.right, Vector3.up);
        }

        /// <summary>Плоский многоугольник (выпуклый или звёздный от центра) в плоскости XY системы f, смотрит в −Z (как Quad в Unity).</summary>
        public static void Polygon(MeshData m, Frame f, IList<Vector2> pts, bool facePlusZ = false)
        {
            var c = Vector2.zero;
            foreach (var p in pts) c += p;
            c /= pts.Count;
            var nrm = f.D(facePlusZ ? Vector3.forward : Vector3.back);
            int ci = m.Add(f.P(new Vector3(c.x, c.y, 0)), nrm, new Vector2(0.5f, 0.5f));
            var ids = new int[pts.Count];
            for (int i = 0; i < pts.Count; i++) ids[i] = m.Add(f.P(new Vector3(pts[i].x, pts[i].y, 0)), nrm, new Vector2(0.5f + pts[i].x, 0.5f + pts[i].y));
            for (int i = 0; i < pts.Count; i++)
            {
                int j = (i + 1) % pts.Count;
                // Точки против часовой в плоскости XY; лицевая сторона в Unity — по cross(b − a, c − a)
                if (facePlusZ) m.Tri(ci, ids[i], ids[j]);
                else m.Tri(ci, ids[j], ids[i]);
            }
        }

        /// <summary>Прямоугольник с UV 0…1 (стекло зеркала): в плоскости XY, смотрит в −Z, как Quad в Unity.</summary>
        public static void Quad(MeshData m, Frame f, float w, float h)
        {
            var nrm = f.D(Vector3.back);
            int a = m.Add(f.P(-w / 2, -h / 2, 0), nrm, new Vector2(0, 0));
            int b = m.Add(f.P(w / 2, -h / 2, 0), nrm, new Vector2(1, 0));
            int c = m.Add(f.P(w / 2, h / 2, 0), nrm, new Vector2(1, 1));
            int d = m.Add(f.P(-w / 2, h / 2, 0), nrm, new Vector2(0, 1));
            m.Quad(a, d, c, b);
        }

        /// <summary>Цилиндр вдоль оси Y системы f: от y0 до y1, с крышками.</summary>
        public static void Cylinder(MeshData m, Frame f, float r, float y0, float y1, int seg = 20, bool caps = true)
        {
            Lathe(m, f, new[] { new Vector2(r, y0), new Vector2(r, y1) }, seg, false);
            if (!caps) return;
            Lathe(m, f, new[] { new Vector2(0, y1), new Vector2(r, y1) }, seg, false, true);
            Lathe(m, f, new[] { new Vector2(r, y0), new Vector2(0, y0) }, seg, false, true);
        }

        /// <summary>Тор (бублик) вокруг оси Y системы f, можно дугой от a0 до a1 градусов.</summary>
        public static void Torus(MeshData m, Frame f, float radius, float thickness, int seg = 32, int sides = 8, float a0 = 0f, float a1 = 360f)
        {
            var path = new List<Vector3>();
            bool full = Mathf.Abs(a1 - a0) >= 359.9f;
            int count = full ? seg : seg + 1;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad;
                path.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            Sweep(m, f, path, Circle(thickness, sides), full, Vector3.up);
        }
    }
}

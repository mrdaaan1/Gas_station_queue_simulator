using System;
using System.Collections.Generic;

namespace GasQueue.Intro
{
    /// <summary>Цвет RGBA в долях 0..1 (без UnityEngine — заставку можно рисовать в отдельном потоке и в превью).</summary>
    public struct C4
    {
        public float r, g, b, a;
        public C4(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }

        public static C4 Hex(string hex, float a = 1f)
        {
            if (hex[0] == '#') hex = hex.Substring(1);
            int v = Convert.ToInt32(hex, 16);
            return new C4(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, a);
        }

        public C4 A(float alpha) => new C4(r, g, b, alpha);
        public static C4 Lerp(C4 x, C4 y, float t) => new C4(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t);
        public static readonly C4 White = new C4(1, 1, 1), Black = new C4(0, 0, 0), Clear = new C4(0, 0, 0, 0);
    }

    /// <summary>Заливка: цвет в точке (x, y) в пикселях.</summary>
    public delegate C4 Paint(float x, float y);

    public static class Paints
    {
        public static Paint Solid(C4 c) => (x, y) => c;

        /// <summary>Линейный градиент от (x0, y0) к (x1, y1) по равномерно расставленным цветам.</summary>
        public static Paint Linear(float x0, float y0, float x1, float y1, params C4[] stops)
        {
            float dx = x1 - x0, dy = y1 - y0, len2 = Math.Max(1e-6f, dx * dx + dy * dy);
            return (x, y) => Stops(stops, ((x - x0) * dx + (y - y0) * dy) / len2);
        }

        public static Paint Radial(float cx, float cy, float r, params C4[] stops)
        {
            return (x, y) => Stops(stops, (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r);
        }

        public static C4 Stops(C4[] stops, float t)
        {
            if (t <= 0f) return stops[0];
            if (t >= 1f) return stops[stops.Length - 1];
            float f = t * (stops.Length - 1);
            int i = (int)f;
            return C4.Lerp(stops[i], stops[i + 1], f - i);
        }
    }

    public struct V2
    {
        public float x, y;
        public V2(float x, float y) { this.x = x; this.y = y; }
    }

    /// <summary>Контур из ломаных (кривые сразу разбиваются на отрезки). Y вниз, как на картинке.</summary>
    public class Path
    {
        public readonly List<List<V2>> Contours = new List<List<V2>>();
        List<V2> cur;

        public Path Move(float x, float y)
        {
            cur = new List<V2> { new V2(x, y) };
            Contours.Add(cur);
            return this;
        }

        public Path Line(float x, float y)
        {
            if (cur == null) return Move(x, y);
            cur.Add(new V2(x, y));
            return this;
        }

        public Path Quad(float cx, float cy, float x, float y, int n = 10)
        {
            var p = cur[cur.Count - 1];
            for (int k = 1; k <= n; k++)
            {
                float t = k / (float)n, u = 1 - t;
                cur.Add(new V2(u * u * p.x + 2 * u * t * cx + t * t * x, u * u * p.y + 2 * u * t * cy + t * t * y));
            }
            return this;
        }

        public Path Cubic(float c1x, float c1y, float c2x, float c2y, float x, float y, int n = 14)
        {
            var p = cur[cur.Count - 1];
            for (int k = 1; k <= n; k++)
            {
                float t = k / (float)n, u = 1 - t;
                cur.Add(new V2(u * u * u * p.x + 3 * u * u * t * c1x + 3 * u * t * t * c2x + t * t * t * x,
                               u * u * u * p.y + 3 * u * u * t * c1y + 3 * u * t * t * c2y + t * t * t * y));
            }
            return this;
        }

        public Path Close()
        {
            cur = null;
            return this;
        }

        public Path Add(Path other)
        {
            foreach (var c in other.Contours) Contours.Add(new List<V2>(c));
            cur = null;
            return this;
        }

        /// <summary>Новый контур: точка (x, y) → (x·s + ox, y·sy + oy).</summary>
        public Path Map(float s, float ox, float oy, float sy = float.NaN)
        {
            if (float.IsNaN(sy)) sy = s;
            var p = new Path();
            foreach (var c in Contours)
            {
                var n = new List<V2>(c.Count);
                foreach (var v in c) n.Add(new V2(v.x * s + ox, v.y * sy + oy));
                p.Contours.Add(n);
            }
            return p;
        }

        public Path Translate(float dx, float dy) => Map(1f, dx, dy);

        public Path Rotate(float degrees, float cx, float cy)
        {
            float a = degrees * (float)Math.PI / 180f, cs = (float)Math.Cos(a), sn = (float)Math.Sin(a);
            var p = new Path();
            foreach (var c in Contours)
            {
                var n = new List<V2>(c.Count);
                foreach (var v in c)
                {
                    float x = v.x - cx, y = v.y - cy;
                    n.Add(new V2(cx + x * cs - y * sn, cy + x * sn + y * cs));
                }
                p.Contours.Add(n);
            }
            return p;
        }

        public void Bounds(out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = float.MaxValue;
            x1 = y1 = float.MinValue;
            foreach (var c in Contours)
                foreach (var v in c)
                {
                    if (v.x < x0) x0 = v.x;
                    if (v.y < y0) y0 = v.y;
                    if (v.x > x1) x1 = v.x;
                    if (v.y > y1) y1 = v.y;
                }
        }

        // ---------- Фигуры ----------

        public static Path Poly(params float[] xy)
        {
            var p = new Path().Move(xy[0], xy[1]);
            for (int i = 2; i + 1 < xy.Length; i += 2) p.Line(xy[i], xy[i + 1]);
            return p.Close();
        }

        public static Path Rect(float x, float y, float w, float h) => Poly(x, y, x + w, y, x + w, y + h, x, y + h);

        public static Path RoundRect(float x, float y, float w, float h, float r)
        {
            r = Math.Min(r, Math.Min(w, h) / 2f);
            return new Path().Move(x + r, y).Line(x + w - r, y).Quad(x + w, y, x + w, y + r, 6)
                .Line(x + w, y + h - r).Quad(x + w, y + h, x + w - r, y + h, 6)
                .Line(x + r, y + h).Quad(x, y + h, x, y + h - r, 6)
                .Line(x, y + r).Quad(x, y, x + r, y, 6).Close();
        }

        public static Path Ellipse(float cx, float cy, float rx, float ry, int n = 48)
        {
            var p = new Path();
            for (int i = 0; i < n; i++)
            {
                float a = i * 2f * (float)Math.PI / n;
                p.Line(cx + rx * (float)Math.Cos(a), cy + ry * (float)Math.Sin(a));
            }
            return p.Close();
        }

        public static Path Circle(float cx, float cy, float r) => Ellipse(cx, cy, r, r, Math.Max(16, Math.Min(96, (int)(r * 1.2f))));

        /// <summary>Кольцо (для правила «ненулевой обмотки» внутренний контур идёт в обратную сторону).</summary>
        public static Path Ring(float cx, float cy, float r, float thickness)
        {
            var p = Circle(cx, cy, r);
            var inner = Circle(cx, cy, r - thickness).Contours[0];
            inner.Reverse();
            p.Contours.Add(inner);
            return p;
        }

        /// <summary>Отрезок толщиной 2r со скруглёнными концами.</summary>
        public static Path Capsule(float x0, float y0, float x1, float y1, float r)
        {
            float dx = x1 - x0, dy = y1 - y0, len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-4f) return Circle(x0, y0, r);
            float a = (float)Math.Atan2(dy, dx);
            var p = new Path();
            const int n = 10;
            // Полукруг вокруг конца, потом полукруг вокруг начала — у всех капсул одинаковый обход
            for (int i = 0; i <= n; i++)
            {
                float t = a - (float)Math.PI / 2f + i * (float)Math.PI / n;
                p.Line(x1 + (float)Math.Cos(t) * r, y1 + (float)Math.Sin(t) * r);
            }
            for (int i = 0; i <= n; i++)
            {
                float t = a + (float)Math.PI / 2f + i * (float)Math.PI / n;
                p.Line(x0 + (float)Math.Cos(t) * r, y0 + (float)Math.Sin(t) * r);
            }
            return p.Close();
        }

        /// <summary>Линия по точкам толщиной 2r (цепочка капсул).</summary>
        public static Path Stroke(IList<V2> pts, float r)
        {
            var p = new Path();
            for (int i = 0; i + 1 < pts.Count; i++) p.Add(Capsule(pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y, r));
            return p;
        }

        /// <summary>Дуга окружности толщиной 2r (углы в градусах, по часовой на экране).</summary>
        public static Path Arc(float cx, float cy, float radius, float a0, float a1, float r, int n = 40)
        {
            var pts = new List<V2>();
            for (int i = 0; i <= n; i++)
            {
                float a = (a0 + (a1 - a0) * i / n) * (float)Math.PI / 180f;
                pts.Add(new V2(cx + radius * (float)Math.Cos(a), cy + radius * (float)Math.Sin(a)));
            }
            return Stroke(pts, r);
        }

        public static List<V2> CubicPoints(float x0, float y0, float c1x, float c1y, float c2x, float c2y, float x1, float y1, int n = 20)
        {
            var p = new Path().Move(x0, y0).Cubic(c1x, c1y, c2x, c2y, x1, y1, n);
            return p.Contours[0];
        }

        // ---------- Текст ----------

        /// <summary>Ширина строки шрифтом Rubik Black в пикселях.</summary>
        public static float TextWidth(string s, float size, float tracking = 0f)
        {
            float w = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                if (!IntroGlyphs.Rubik.TryGetValue(s[i], out var g)) g = IntroGlyphs.Rubik['?'];
                w += g.Advance * size + (i < s.Length - 1 ? tracking * size : 0f);
            }
            return w;
        }

        /// <summary>Строка шрифтом Rubik Black: x — левый край, baseline — базовая линия, slant — наклон (курсив).</summary>
        public static Path Text(string s, float x, float baseline, float size, float tracking = 0f, float slant = 0f)
        {
            var p = new Path();
            float pen = x;
            foreach (char ch in s)
            {
                if (!IntroGlyphs.Rubik.TryGetValue(ch, out var g)) g = IntroGlyphs.Rubik['?'];
                foreach (var c in g.Contours)
                {
                    var n = new List<V2>(c.Length / 2);
                    for (int i = 0; i + 1 < c.Length; i += 2)
                        n.Add(new V2(pen + (c[i] + c[i + 1] * slant) * size, baseline - c[i + 1] * size));
                    p.Contours.Add(n);
                }
                pen += (g.Advance + tracking) * size;
            }
            return p;
        }

        /// <summary>Строка по центру относительно cx.</summary>
        public static Path TextCentered(string s, float cx, float baseline, float size, float tracking = 0f, float slant = 0f)
        {
            return Text(s, cx - TextWidth(s, size, tracking) / 2f, baseline, size, tracking, slant);
        }
    }

    /// <summary>
    /// Картинка в памяти с заливкой контуров и сглаживанием (4 подстроки на пиксель, точное покрытие по горизонтали).
    /// Хранит цвет с premultiplied alpha.
    /// </summary>
    public class Raster
    {
        public readonly int Width, Height;
        public readonly float[] Px;
        const int Sub = 4;

        public Raster(int w, int h)
        {
            Width = Math.Max(1, w);
            Height = Math.Max(1, h);
            Px = new float[Width * Height * 4];
        }

        public void Clear(C4 c) => FillAll(Paints.Solid(c));

        public void FillAll(Paint paint)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Blend((y * Width + x) * 4, paint(x + 0.5f, y + 0.5f), 1f);
        }

        public void Fill(Path path, C4 c) => Fill(path, Paints.Solid(c));

        struct Edge
        {
            public float x0, y0, x1, y1;
            public int dir;
        }

        public void Fill(Path path, Paint paint, bool evenOdd = false)
        {
            var edges = new List<Edge>();
            float minY = float.MaxValue, maxY = float.MinValue, minX = float.MaxValue, maxX = float.MinValue;
            foreach (var c in path.Contours)
            {
                for (int i = 0; i < c.Count; i++)
                {
                    var a = c[i];
                    var b = c[(i + 1) % c.Count];
                    if (a.x < minX) minX = a.x;
                    if (a.x > maxX) maxX = a.x;
                    if (a.y == b.y) continue;
                    var e = a.y < b.y ? new Edge { x0 = a.x, y0 = a.y, x1 = b.x, y1 = b.y, dir = 1 } : new Edge { x0 = b.x, y0 = b.y, x1 = a.x, y1 = a.y, dir = -1 };
                    edges.Add(e);
                    if (e.y0 < minY) minY = e.y0;
                    if (e.y1 > maxY) maxY = e.y1;
                }
            }
            if (edges.Count == 0) return;
            int row0 = Math.Max(0, (int)Math.Floor(minY)), row1 = Math.Min(Height - 1, (int)Math.Ceiling(maxY));
            int col0 = Math.Max(0, (int)Math.Floor(minX)), col1 = Math.Min(Width - 1, (int)Math.Ceiling(maxX));
            if (row0 > row1 || col0 > col1) return;

            // Рёбра по строкам: в каждой строке проверяем только «свои»
            int rows = row1 - row0 + 1;
            var buckets = new List<int>[rows];
            for (int i = 0; i < edges.Count; i++)
            {
                int r0 = Math.Max(row0, (int)Math.Floor(edges[i].y0)), r1 = Math.Min(row1, (int)Math.Floor(edges[i].y1));
                for (int r = r0; r <= r1; r++)
                {
                    var list = buckets[r - row0] ?? (buckets[r - row0] = new List<int>());
                    list.Add(i);
                }
            }

            int cols = col1 - col0 + 1;
            var cov = new float[cols + 2];
            var xs = new List<float>();
            var ds = new List<int>();
            var order = new List<int>();
            for (int row = row0; row <= row1; row++)
            {
                var list = buckets[row - row0];
                if (list == null) continue;
                Array.Clear(cov, 0, cov.Length);
                bool any = false;
                for (int s = 0; s < Sub; s++)
                {
                    float sy = row + (s + 0.5f) / Sub;
                    xs.Clear();
                    ds.Clear();
                    foreach (int i in list)
                    {
                        var e = edges[i];
                        if (sy < e.y0 || sy >= e.y1) continue;
                        xs.Add(e.x0 + (sy - e.y0) * (e.x1 - e.x0) / (e.y1 - e.y0));
                        ds.Add(e.dir);
                    }
                    if (xs.Count < 2) continue;
                    order.Clear();
                    for (int i = 0; i < xs.Count; i++) order.Add(i);
                    order.Sort((p, q) => xs[p].CompareTo(xs[q]));
                    int wind = 0;
                    for (int k = 0; k < order.Count - 1; k++)
                    {
                        int i = order[k];
                        wind += evenOdd ? 1 : ds[i];
                        bool inside = evenOdd ? (wind & 1) == 1 : wind != 0;
                        if (!inside) continue;
                        AddSpan(cov, col0, cols, xs[i], xs[order[k + 1]], 1f / Sub);
                        any = true;
                    }
                }
                if (!any) continue;
                for (int cx = 0; cx < cols; cx++)
                {
                    float a = cov[cx];
                    if (a <= 0.002f) continue;
                    int x = col0 + cx;
                    Blend((row * Width + x) * 4, paint(x + 0.5f, row + 0.5f), Math.Min(1f, a));
                }
            }
        }

        static void AddSpan(float[] cov, int col0, int cols, float xa, float xb, float weight)
        {
            xa -= col0;
            xb -= col0;
            if (xb <= 0f || xa >= cols) return;
            if (xa < 0f) xa = 0f;
            if (xb > cols) xb = cols;
            int ia = (int)xa, ib = (int)xb;
            if (ia == ib)
            {
                cov[ia] += (xb - xa) * weight;
                return;
            }
            cov[ia] += (ia + 1 - xa) * weight;
            for (int i = ia + 1; i < ib; i++) cov[i] += weight;
            if (ib < cols) cov[ib] += (xb - ib) * weight;
        }

        void Blend(int i, C4 c, float coverage)
        {
            float a = c.a * coverage;
            if (a <= 0f) return;
            float k = 1f - a;
            Px[i] = c.r * a + Px[i] * k;
            Px[i + 1] = c.g * a + Px[i + 1] * k;
            Px[i + 2] = c.b * a + Px[i + 2] * k;
            Px[i + 3] = a + Px[i + 3] * k;
        }

        /// <summary>Залить с обводкой: обводка — те же контуры, сдвинутые по кругу (быстро и с круглыми углами).</summary>
        public void FillOutlined(Path path, Paint fill, C4 outline, float width, float shadowDx = 0f, float shadowDy = 0f)
        {
            if (width > 0f)
            {
                // Столько сдвигов, чтобы углы обводки отклонялись от круга меньше чем на 0,3 пикселя
                int n = Math.Max(8, (int)Math.Ceiling(Math.PI / Math.Acos(Math.Max(-1.0, 1.0 - 0.3 / width))));
                if (shadowDx != 0f || shadowDy != 0f)
                    for (int i = 0; i < n; i++)
                    {
                        float a = i * 2f * (float)Math.PI / n;
                        Fill(path.Translate(shadowDx + (float)Math.Cos(a) * width, shadowDy + (float)Math.Sin(a) * width), outline);
                    }
                for (int i = 0; i < n; i++)
                {
                    float a = i * 2f * (float)Math.PI / n;
                    Fill(path.Translate((float)Math.Cos(a) * width, (float)Math.Sin(a) * width), outline);
                }
                if (width > 3f)
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 2f * (float)Math.PI / 8;
                        Fill(path.Translate((float)Math.Cos(a) * width * 0.5f, (float)Math.Sin(a) * width * 0.5f), outline);
                    }
            }
            Fill(path, fill);
        }

        /// <summary>Наложить другую картинку (того же размера или меньше) со сдвигом и прозрачностью.</summary>
        public void Draw(Raster src, int dx, int dy, float opacity = 1f)
        {
            for (int y = 0; y < src.Height; y++)
            {
                int ty = y + dy;
                if (ty < 0 || ty >= Height) continue;
                for (int x = 0; x < src.Width; x++)
                {
                    int tx = x + dx;
                    if (tx < 0 || tx >= Width) continue;
                    int si = (y * src.Width + x) * 4, ti = (ty * Width + tx) * 4;
                    float a = src.Px[si + 3] * opacity;
                    if (a <= 0f) continue;
                    float k = 1f - a;
                    Px[ti] = src.Px[si] * opacity + Px[ti] * k;
                    Px[ti + 1] = src.Px[si + 1] * opacity + Px[ti + 1] * k;
                    Px[ti + 2] = src.Px[si + 2] * opacity + Px[ti + 2] * k;
                    Px[ti + 3] = a + Px[ti + 3] * k;
                }
            }
        }

        /// <summary>Размытие (три прохода «коробкой» по X и Y — почти гаусс).</summary>
        public void Blur(int radius)
        {
            if (radius < 1) return;
            var tmp = new float[Px.Length];
            for (int pass = 0; pass < 3; pass++)
            {
                BoxPass(Px, tmp, Width, Height, radius, true);
                BoxPass(tmp, Px, Width, Height, radius, false);
            }
        }

        static void BoxPass(float[] src, float[] dst, int w, int h, int r, bool horizontal)
        {
            int len = horizontal ? w : h, lines = horizontal ? h : w;
            float inv = 1f / (2 * r + 1);
            var sum = new float[4];
            for (int line = 0; line < lines; line++)
            {
                for (int c = 0; c < 4; c++) sum[c] = 0f;
                for (int k = -r; k <= r; k++)
                {
                    int i = Index(line, Clamp(k, len), horizontal, w);
                    for (int c = 0; c < 4; c++) sum[c] += src[i + c];
                }
                for (int p = 0; p < len; p++)
                {
                    int o = Index(line, p, horizontal, w);
                    for (int c = 0; c < 4; c++) dst[o + c] = sum[c] * inv;
                    int add = Index(line, Clamp(p + r + 1, len), horizontal, w), rem = Index(line, Clamp(p - r, len), horizontal, w);
                    for (int c = 0; c < 4; c++) sum[c] += src[add + c] - src[rem + c];
                }
            }
        }

        static int Clamp(int v, int len) => v < 0 ? 0 : v >= len ? len - 1 : v;
        static int Index(int line, int p, bool horizontal, int w) => (horizontal ? line * w + p : p * w + line) * 4;

        /// <summary>Залить всё, где есть хоть что-то, одним цветом, сохранив прозрачность (силуэт для блика и свечения).</summary>
        public Raster Silhouette(C4 c)
        {
            var r = new Raster(Width, Height);
            for (int i = 0; i < Px.Length; i += 4)
            {
                float a = Px[i + 3] * c.a;
                r.Px[i] = c.r * a;
                r.Px[i + 1] = c.g * a;
                r.Px[i + 2] = c.b * a;
                r.Px[i + 3] = a;
            }
            return r;
        }

        /// <summary>Байты RGBA32 снизу вверх (как ждёт Texture2D.LoadRawTextureData), без premultiply.</summary>
        public byte[] ToRgba32(bool flipY)
        {
            var bytes = new byte[Width * Height * 4];
            for (int y = 0; y < Height; y++)
            {
                int sy = flipY ? Height - 1 - y : y;
                for (int x = 0; x < Width; x++)
                {
                    int s = (sy * Width + x) * 4, d = (y * Width + x) * 4;
                    float a = Px[s + 3];
                    float inv = a > 1e-5f ? 1f / a : 0f;
                    bytes[d] = ToByte(Px[s] * inv);
                    bytes[d + 1] = ToByte(Px[s + 1] * inv);
                    bytes[d + 2] = ToByte(Px[s + 2] * inv);
                    bytes[d + 3] = ToByte(a);
                }
            }
            return bytes;
        }

        static byte ToByte(float v) => (byte)(v <= 0f ? 0 : v >= 1f ? 255 : (int)(v * 255f + 0.5f));
    }
}

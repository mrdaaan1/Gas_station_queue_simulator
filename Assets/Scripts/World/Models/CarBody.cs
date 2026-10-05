using System;
using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Гладкий кузов спортивной машины, заданный сечениями вдоль длины (ось Z, перёд — +Z).
    /// Каждое сечение — половинка контура от середины днища до середины крыши из пяти кривых:
    /// A — днище, B — порог и низ двери, C — борт до линии окон, D — боковое стекло и стойки, E — крыша/капот/багажник.
    /// Все размеры — функции от Z (<see cref="Curve"/>), поэтому одной формулой описывается и нос, и салон, и корма.
    /// Описание конкретной машины — в наследнике (например, <see cref="SupraModel"/>).
    /// </summary>
    public abstract class CarBody
    {
        // ---------- Описание формы (задаёт наследник) ----------

        public float Front = 2.26f, Rear = -2.26f;
        /// <summary>Основание лобового стекла, край крыши над ним, край крыши над задним стеклом, начало багажника.</summary>
        public float Cowl, Header, RoofRear, Deck;

        protected Curve W0, YBot, YMax, YBelt, FBelt, YTop, YRoofEdge, XRoofEdge;
        protected Curve AngC, AngD0, AngD1, AngE0;
        /// <summary>Где начинается скругление носа и кормы в плане и насколько оно «квадратное».</summary>
        protected float FrontRound = 1.62f, RearRound = -1.78f, FrontPow = 2.3f, RearPow = 3.2f;
        /// <summary>Нижний угол порога (доля полуширины).</summary>

        public struct Arch
        {
            public float z, y, r, xMin;
        }

        public readonly List<Arch> arches = new List<Arch>();

        // ---------- Сечение ----------

        public struct Station
        {
            public float z, w, ybot, ymax, belt, fb, yc, xr, yr, angC, angD0, angD1, angE0;
        }

        // Доли параметра u для пяти кривых сечения
        public static readonly float[] SegU = { 0f, 0.10f, 0.30f, 0.52f, 0.74f, 1f };
        public const int SegA = 0, SegB = 1, SegC = 2, SegD = 3, SegE = 4;

        Station[] lut;
        const int LutSize = 2400;

        protected void Prepare()
        {
            lut = new Station[LutSize + 1];
            for (int i = 0; i <= LutSize; i++) lut[i] = Compute(Mathf.Lerp(Rear, Front, i / (float)LutSize));
        }

        public bool InCabin(float z) => z < Cowl && z > Deck;

        Station Compute(float z)
        {
            var s = new Station { z = z };
            float w = W0[z];
            if (z > FrontRound)
            {
                float k = Mathf.Clamp01((z - FrontRound) / (Front - FrontRound));
                w *= Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(k, FrontPow)), 1f / FrontPow);
            }
            else if (z < RearRound)
            {
                float k = Mathf.Clamp01((RearRound - z) / (RearRound - Rear));
                w *= Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(k, RearPow)), 1f / RearPow);
            }
            s.w = Mathf.Max(0f, w);
            s.ybot = YBot[z];
            s.ymax = YMax[z];
            s.belt = YBelt[z];
            s.fb = FBelt[z];
            s.yc = YTop[z];
            s.angC = AngC[z];
            s.angE0 = AngE0[z];
            s.angD0 = AngD0[z];
            s.angD1 = AngD1[z];
            if (InCabin(z))
            {
                // Край крыши (верх бокового стекла): у основания стёкол сходится с линией окон
                s.yr = YRoofEdge[z];
                float xr = XRoofEdge[z];
                s.xr = Mathf.Min(xr / s.w, s.fb);
            }
            else
            {
                s.yr = s.belt;
                s.xr = s.fb;
            }
            return s;
        }

        public Station Sec(float z)
        {
            float f = Mathf.Clamp01((z - Rear) / (Front - Rear)) * LutSize;
            int i = Mathf.Min(LutSize - 1, (int)f);
            float t = f - i;
            var a = lut[i];
            var b = lut[i + 1];
            return new Station
            {
                z = z,
                w = a.w + (b.w - a.w) * t,
                ybot = a.ybot + (b.ybot - a.ybot) * t,
                ymax = a.ymax + (b.ymax - a.ymax) * t,
                belt = a.belt + (b.belt - a.belt) * t,
                fb = a.fb + (b.fb - a.fb) * t,
                yc = a.yc + (b.yc - a.yc) * t,
                xr = a.xr + (b.xr - a.xr) * t,
                yr = a.yr + (b.yr - a.yr) * t,
                angC = a.angC + (b.angC - a.angC) * t,
                angD0 = a.angD0 + (b.angD0 - a.angD0) * t,
                angD1 = a.angD1 + (b.angD1 - a.angD1) * t,
                angE0 = a.angE0 + (b.angE0 - a.angE0) * t,
            };
        }

        /// <summary>Точка контура (x, y) на сечении: x — в долях полуширины (потом умножается на w).</summary>
        public Vector2 EvalNorm(in Station s, int seg, float v)
        {
            Vector2 p0, p1;
            float a0, a1;
            switch (seg)
            {
                case SegA: p0 = new Vector2(0f, s.ybot); p1 = new Vector2(RockerFrac, s.ybot + 0.004f); a0 = 0f; a1 = RockerAngle; break;
                case SegB: p0 = new Vector2(RockerFrac, s.ybot + 0.004f); p1 = new Vector2(1f, s.ymax); a0 = RockerAngle; a1 = 90f; break;
                case SegC: p0 = new Vector2(1f, s.ymax); p1 = new Vector2(s.fb, s.belt); a0 = 90f; a1 = s.angC; break;
                case SegD: p0 = new Vector2(s.fb, s.belt); p1 = new Vector2(s.xr, s.yr); a0 = s.angD0; a1 = s.angD1; break;
                default: p0 = new Vector2(s.xr, s.yr); p1 = new Vector2(0f, s.yc); a0 = s.angE0; a1 = 180f; break;
            }
            // В нормированных координатах x делим на полуширину, а y — в метрах; чтобы углы
            // касательных выглядели как в метрах, растягиваем x на типичную полуширину.
            const float sx = 0.9f;
            var q0 = new Vector2(p0.x * sx, p0.y);
            var q1 = new Vector2(p1.x * sx, p1.y);
            float chord = (q1 - q0).magnitude;
            if (chord < 1e-6f) return p0;
            float k = seg == SegB ? 1.25f : 1.15f;
            float l0 = chord * k, l1 = chord * k;
            if (seg == SegA) { l0 = chord; l1 = chord; }
            if (seg == SegB) { l0 = chord * 0.9f; l1 = chord * 1.1f; } // скругление порога: низ двери «заваливается» под машину
            var d0 = Geo.Dir(a0);
            var d1 = Geo.Dir(a1);
            // Не даём кривой «перелететь» выше конечной точки (иначе на крыше и капоте посередине ямка)
            float dy = q1.y - q0.y;
            if (seg == SegE && d0.y > 0f && dy > 0f) l0 = Mathf.Min(l0, 2.4f * dy / d0.y);
            if (seg == SegE && dy <= 0f) l0 = Mathf.Min(l0, chord * 0.6f);
            var r = Geo.Hermite(q0, d0 * l0, q1, d1 * l1, v);
            return new Vector2(r.x / sx, r.y);
        }

        /// <summary>Нижний угол порога (доля полуширины) и угол, под которым днище переходит в порог.</summary>
        protected float RockerFrac = 0.88f, RockerAngle = 1f;

        public static void SegOf(float u, out int seg, out float v)
        {
            seg = SegE;
            for (int i = 0; i < 5; i++)
                if (u <= SegU[i + 1]) { seg = i; break; }
            v = Mathf.Clamp01((u - SegU[seg]) / (SegU[seg + 1] - SegU[seg]));
        }

        /// <summary>Точка поверхности правой половины по (z, u).</summary>
        public Vector3 S(float z, float u)
        {
            var st = Sec(z);
            SegOf(u, out int seg, out float v);
            var p = EvalNorm(st, seg, v);
            return new Vector3(p.x * st.w, p.y, z);
        }

        Vector3 SSeg(float z, int seg, float v)
        {
            var st = Sec(z);
            var p = EvalNorm(st, seg, v);
            return new Vector3(p.x * st.w, p.y, z);
        }

        /// <summary>Границы куска поверхности по Z (изломы — у основания стёкол и у края багажника).</summary>
        public float[] PatchBounds => new[] { Rear, Deck, RoofRear, Header, Cowl, Front };

        public int PatchOf(float z)
        {
            var b = PatchBounds;
            for (int i = 0; i < b.Length - 2; i++) if (z < b[i + 1]) return i;
            return b.Length - 2;
        }

        /// <summary>Нормаль правой половины в точке (z, сегмент, v); производные не выходят за кусок и сегмент — так остаются изломы.</summary>
        public Vector3 NormalSeg(float z, int seg, float v, int patch)
        {
            var b = PatchBounds;
            float z0 = b[patch], z1 = b[patch + 1];
            const float ez = 0.004f, ev = 0.004f;
            float za = Mathf.Max(z0, z - ez), zb = Mathf.Min(z1, z + ez);
            float va = Mathf.Max(0f, v - ev), vb = Mathf.Min(1f, v + ev);
            var du = SSeg(z, seg, vb) - SSeg(z, seg, va);
            var dz = SSeg(zb, seg, v) - SSeg(za, seg, v);
            if (du.sqrMagnitude < 1e-12f)
            {
                // Вырожденный сегмент (боковое стекло сжалось в точку у капота): берём соседний
                int other = seg == SegD ? SegE : seg;
                du = SSeg(z, other, 0.02f) - SSeg(z, other, 0f);
                if (du.sqrMagnitude < 1e-12f) du = SSeg(z, SegC, 1f) - SSeg(z, SegC, 0.98f);
            }
            if (dz.sqrMagnitude < 1e-12f) dz = Vector3.forward;
            var n = Vector3.Cross(du, dz);
            if (n.sqrMagnitude < 1e-14f) return Vector3.right;
            return n.normalized;
        }

        public Vector3 Normal(float z, float u)
        {
            SegOf(u, out int seg, out float v);
            return NormalSeg(z, seg, v, PatchOf(z));
        }

        // ---------- Поиск точки на поверхности ----------

        /// <summary>Параметр u на сечении, где контур проходит на высоте y (контур идёт снизу вверх).</summary>
        public float UAtY(in Station st, float y)
        {
            float lo = SegU[1], hi = 1f;
            if (y <= st.ybot + 0.004f) return Mathf.Lerp(0f, SegU[1], Mathf.Clamp01((y - st.ybot) / 0.004f));
            for (int it = 0; it < 26; it++)
            {
                float mid = (lo + hi) * 0.5f;
                SegOf(mid, out int seg, out float v);
                if (EvalNorm(st, seg, v).y < y) lo = mid; else hi = mid;
            }
            return (lo + hi) * 0.5f;
        }

        public float HalfWidthAt(in Station st, float y)
        {
            if (y < st.ybot || y > st.yc) return -1f;
            float u = UAtY(st, y);
            SegOf(u, out int seg, out float v);
            return EvalNorm(st, seg, v).x * st.w;
        }

        public bool Inside(Vector3 p)
        {
            if (p.z <= Rear || p.z >= Front) return false;
            var st = Sec(p.z);
            float hw = HalfWidthAt(st, p.y);
            return hw > 0f && Mathf.Abs(p.x) < hw;
        }

        /// <summary>Луч изнутри кузова наружу: где он пересекает поверхность. Возвращает (z, u) правой половины.</summary>
        public bool Hit(Vector3 origin, Vector3 dir, out float z, out float u)
        {
            dir = dir.normalized;
            float lo = 0f, hi = 3.5f;
            z = 0f; u = 0f;
            if (!Inside(origin)) return false;
            for (int it = 0; it < 28; it++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Inside(origin + dir * mid)) lo = mid; else hi = mid;
            }
            var p = origin + dir * hi;
            z = Mathf.Clamp(p.z, Rear + 1e-6f, Front - 1e-6f);
            var st = Sec(z);
            u = UAtY(st, Mathf.Clamp(p.y, st.ybot, st.yc));
            return true;
        }

        /// <summary>Точка и нормаль поверхности; side = −1 — левая половина (зеркально).</summary>
        public void At(float z, float u, float side, out Vector3 pos, out Vector3 nrm)
        {
            pos = S(z, u);
            nrm = Normal(z, u);
            if (side < 0f)
            {
                pos.x = -pos.x;
                nrm.x = -nrm.x;
            }
        }

        public bool HitPoint(Vector3 origin, Vector3 dir, out Vector3 pos, out Vector3 nrm)
        {
            float side = origin.x < 0f || (Mathf.Abs(origin.x) < 1e-5f && dir.x < 0f) ? -1f : 1f;
            var o = new Vector3(Mathf.Abs(origin.x), origin.y, origin.z);
            var d = new Vector3(dir.x * side, dir.y, dir.z);
            if (!Hit(o, d, out float z, out float u))
            {
                pos = origin;
                nrm = Vector3.up;
                return false;
            }
            At(z, u, side, out pos, out nrm);
            return true;
        }

        // ---------- Сборка сетки ----------

        /// <summary>Куда относится клетка поверхности: в какой узел и каким материалом; null — дырка (арка колеса).</summary>
        public struct CellInfo
        {
            public string node, mat;
            /// <summary>Внутренняя обшивка салона (материал изнанки) — или null.</summary>
            public string innerMat;
        }

        protected abstract CellInfo Classify(float z, int seg, float v, Vector3 p, float side);

        static readonly int[] SegCount = { 5, 12, 14, 12, 18 };

        /// <summary>Сетка станций по Z на куске [a, b]: гуще у краёв машины и у колёсных арок.</summary>
        List<float> Stations(float a, float b, int count)
        {
            float Density(float z)
            {
                float d = 1f;
                d += 3.5f * Mathf.Exp(-Sq((z - Front) / 0.22f)) + 3.5f * Mathf.Exp(-Sq((z - Rear) / 0.22f));
                foreach (var ar in arches) d += 1.3f * Mathf.Exp(-Sq((Mathf.Abs(z - ar.z) - ar.r) / 0.12f));
                return d;
            }
            const int fine = 2000;
            var cum = new float[fine + 1];
            for (int i = 1; i <= fine; i++)
            {
                float z = Mathf.Lerp(a, b, (i - 0.5f) / fine);
                cum[i] = cum[i - 1] + Density(z);
            }
            var list = new List<float>();
            int j = 0;
            for (int k = 0; k <= count; k++)
            {
                float target = cum[fine] * k / count;
                while (j < fine && cum[j + 1] < target) j++;
                float f = j >= fine ? 1f : (target - cum[j]) / Mathf.Max(1e-6f, cum[j + 1] - cum[j]);
                list.Add(Mathf.Lerp(a, b, (j + f) / fine));
            }
            list[0] = a;
            list[count] = b;
            return list;
        }

        static float Sq(float x) => x * x;

        bool InArch(float z, float y, float x, out Arch hit)
        {
            foreach (var a in arches)
            {
                if (x < a.xMin) continue;
                float dz = z - a.z, dy = y - a.y;
                if (dz * dz + dy * dy < a.r * a.r)
                {
                    hit = a;
                    return true;
                }
            }
            hit = default;
            return false;
        }

        /// <summary>Строит обе половины кузова в узлы модели (узел выбирает Classify).</summary>
        public void Emit(Func<string, ModelNode> nodeByName, float totalStations = 230f)
        {
            var b = PatchBounds;
            for (int patch = 0; patch < b.Length - 1; patch++)
            {
                float len = b[patch + 1] - b[patch];
                int count = Mathf.Max(6, Mathf.RoundToInt(totalStations * len / (Front - Rear) * (patch == 0 || patch == b.Length - 2 ? 1.25f : 1f)));
                var zs = Stations(b[patch], b[patch + 1], count);
                EmitPatch(patch, zs, nodeByName);
            }
        }

        void EmitPatch(int patch, List<float> zs, Func<string, ModelNode> nodeByName)
        {
            int ni = zs.Count;
            // Глобальная сетка параметров по u (сегменты подряд, границы общие)
            var segStart = new int[6];
            int nj = 0;
            for (int s = 0; s < 5; s++)
            {
                segStart[s] = nj;
                nj += SegCount[s];
            }
            segStart[5] = nj;
            // Параметры вершин (после подгонки к краю арок): z и u
            var pz = new float[ni, nj + 1];
            var pu = new float[ni, nj + 1];
            var pos = new Vector3[ni, nj + 1];
            for (int i = 0; i < ni; i++)
            for (int j = 0; j <= nj; j++)
            {
                int s = 0;
                while (s < 4 && j > segStart[s + 1]) s++;
                float v = (j - segStart[s]) / (float)SegCount[s];
                pz[i, j] = zs[i];
                pu[i, j] = SegU[s] + v * (SegU[s + 1] - SegU[s]);
                pos[i, j] = S(zs[i], pu[i, j]);
            }

            // Какие клетки вырезаны (арки колёс)
            var cut = new bool[ni - 1, nj];
            for (int i = 0; i < ni - 1; i++)
            for (int j = 0; j < nj; j++)
            {
                var c = (pos[i, j] + pos[i + 1, j] + pos[i, j + 1] + pos[i + 1, j + 1]) * 0.25f;
                SegOf((pu[i, j] + pu[i, j + 1]) * 0.5f, out int seg, out _);
                cut[i, j] = seg <= SegC && InArch(c.z, c.y, c.x, out _);
            }

            // Вершины на краю выреза (рядом и вырезанная, и целая клетка) кладём точно на окружность арки —
            // край получается ровным, без «лесенки»
            var snapZ = new float[ni, nj + 1];
            var snapU = new float[ni, nj + 1];
            var snap = new bool[ni, nj + 1];
            for (int i = 0; i < ni; i++)
            for (int j = 0; j <= nj; j++)
            {
                if (pu[i, j] < SegU[1] + 1e-4f || pu[i, j] > SegU[3] + 1e-4f) continue; // только борт (B и C)
                bool anyCut = false, anyKept = false;
                for (int di = -1; di <= 0; di++)
                for (int dj = -1; dj <= 0; dj++)
                {
                    int ci = i + di, cj = j + dj;
                    if (ci < 0 || cj < 0 || ci >= ni - 1 || cj >= nj) continue;
                    if (cut[ci, cj]) anyCut = true; else anyKept = true;
                }
                if (!anyCut || !anyKept) continue;
                var p = pos[i, j];
                Arch arch = arches[0];
                float bestD = float.MaxValue;
                foreach (var a in arches)
                {
                    float d = Mathf.Abs(new Vector2(p.z - a.z, p.y - a.y).magnitude - a.r);
                    if (d < bestD) { bestD = d; arch = a; }
                }
                var dir = new Vector2(p.z - arch.z, p.y - arch.y);
                dir = dir.sqrMagnitude > 1e-8f ? dir.normalized : new Vector2(0f, 1f);
                float tz = arch.z + dir.x * arch.r, ty = arch.y + dir.y * arch.r;
                var st = Sec(tz);
                if (ty < st.ybot + 0.004f) ty = st.ybot + 0.004f;
                snapZ[i, j] = tz;
                snapU[i, j] = UAtY(st, ty);
                snap[i, j] = true;
            }
            for (int i = 0; i < ni; i++)
            for (int j = 0; j <= nj; j++)
            {
                if (!snap[i, j]) continue;
                pz[i, j] = snapZ[i, j];
                pu[i, j] = snapU[i, j];
                pos[i, j] = S(pz[i, j], pu[i, j]);
            }

            // Клетки → узлы и материалы, обе половины
            foreach (float side in new[] { 1f, -1f })
            {
                var cache = new Dictionary<(MeshData, int, int, int, bool), int>();
                for (int i = 0; i < ni - 1; i++)
                for (int j = 0; j < nj; j++)
                {
                    if (cut[i, j]) continue;
                    int seg = 0;
                    while (seg < 4 && j >= segStart[seg + 1]) seg++;
                    var c = (pos[i, j] + pos[i + 1, j] + pos[i, j + 1] + pos[i + 1, j + 1]) * 0.25f;
                    // Пустые (сжатые в линию) клетки не рисуем
                    var diag = Vector3.Cross(pos[i + 1, j + 1] - pos[i, j], pos[i + 1, j] - pos[i, j + 1]);
                    if (diag.sqrMagnitude < 1e-12f) continue;
                    float vc = ((pu[i, j] + pu[i, j + 1]) * 0.5f - SegU[seg]) / (SegU[seg + 1] - SegU[seg]);
                    float zc = (zs[i] + zs[i + 1]) * 0.5f;
                    var info = Classify(zc, seg, vc, new Vector3(c.x * side, c.y, c.z), side);
                    if (info.mat == null) continue;
                    var node = nodeByName(info.node);
                    EmitQuad(node, node.M(info.mat), i, j, seg, side, patch, pz, pu, cache, false);
                    if (info.innerMat != null)
                    {
                        // Обшивка — отдельный узел без теней: иначе она в 1–2 см под краской «пятнает» кузов тенью
                        var innerNode = node.children.Find(ch => ch.name == node.name + "Inner") ?? node.Child(node.name + "Inner");
                        EmitQuad(innerNode, innerNode.M(info.innerMat), i, j, seg, side, patch, pz, pu, cache, true);
                    }
                }
            }
        }

        void EmitQuad(ModelNode node, MeshData m, int i, int j, int seg, float side, int patch, float[,] pz, float[,] pu,
            Dictionary<(MeshData, int, int, int, bool), int> cache, bool inner)
        {
            int V(int a, int b)
            {
                var key = (m, a, b, seg, inner);
                if (cache.TryGetValue(key, out int idx)) return idx;
                float z = pz[a, b];
                float u = pu[a, b];
                float v = Mathf.Clamp01((u - SegU[seg]) / (SegU[seg + 1] - SegU[seg]));
                var p = S(z, u); // точное положение (вершина у арки могла уйти в соседний сегмент)
                var nrm = NormalSeg(z, seg, v, patch);
                if (inner)
                {
                    p -= nrm * 0.015f;
                    nrm = -nrm;
                }
                p.x *= side;
                nrm.x *= side;
                var origin = node.WorldFrame();
                idx = m.Add(origin.ToLocal(p), origin.DirToLocal(nrm), new Vector2(z, u));
                cache[key] = idx;
                return idx;
            }
            int a0 = V(i, j), a1 = V(i, j + 1), a2 = V(i + 1, j + 1), a3 = V(i + 1, j);
            bool flip = (side < 0f) != inner;
            if (!flip) m.Quad(a0, a1, a2, a3);
            else m.Quad(a0, a3, a2, a1);
        }


        // ---------- Детали на поверхности кузова ----------

        /// <summary>Добавить четырёхугольник так, чтобы лицевая сторона смотрела вдоль нормали.</summary>
        public static void QuadFacing(MeshData m, int a, int b, int c, int d, Vector3 normal)
        {
            var pa = m.v[a]; var pb = m.v[b]; var pc = m.v[c];
            if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), normal) >= 0f) m.Quad(a, b, c, d);
            else m.Quad(a, d, c, b);
        }

        /// <summary>
        /// Накладка, повторяющая поверхность кузова (фара, решётка, стекло фонаря). ray(a, b) — луч изнутри
        /// правой половины к поверхности; side −1 — зеркально на левую сторону. offset — подъём над кузовом.
        /// </summary>
        public void Patch(ModelNode node, string mat, int na, int nb, Func<float, float, (Vector3 o, Vector3 d)> ray, float offset, float side)
        {
            var m = node.M(mat);
            var f = node.WorldFrame();
            var idx = new int[na + 1, nb + 1];
            var nrm = new Vector3[na + 1, nb + 1];
            for (int i = 0; i <= na; i++)
            for (int j = 0; j <= nb; j++)
            {
                var r = ray(i / (float)na, j / (float)nb);
                Hit(r.o, r.d, out float z, out float u);
                At(z, u, side, out var p, out var n);
                nrm[i, j] = n;
                idx[i, j] = m.Add(f.ToLocal(p + n * offset), f.DirToLocal(n), new Vector2(i / (float)na, j / (float)nb));
            }
            for (int i = 0; i < na; i++)
            for (int j = 0; j < nb; j++)
                QuadFacing(m, idx[i, j], idx[i + 1, j], idx[i + 1, j + 1], idx[i, j + 1], f.DirToLocal(nrm[i, j] + nrm[i + 1, j + 1]));
        }

        /// <summary>Точка на кузове по лучу (правая половина) с зеркалированием на сторону side.</summary>
        public bool OnBody(Vector3 o, Vector3 d, float side, out Vector3 p, out Vector3 n)
        {
            bool ok = Hit(o, d, out float z, out float u);
            At(z, u, side, out p, out n);
            return ok;
        }

        /// <summary>Узкая полоска по поверхности (щели между панелями, молдинги). points — лучи к кузову по порядку.</summary>
        public void Ribbon(ModelNode node, string mat, IList<(Vector3 o, Vector3 d)> rays, float width, float offset, float side)
        {
            var m = node.M(mat);
            var f = node.WorldFrame();
            int k = rays.Count;
            var pts = new Vector3[k];
            var ns = new Vector3[k];
            for (int i = 0; i < k; i++) OnBody(rays[i].o, rays[i].d, side, out pts[i], out ns[i]);
            int prevA = -1, prevB = -1;
            for (int i = 0; i < k; i++)
            {
                var t = pts[Mathf.Min(k - 1, i + 1)] - pts[Mathf.Max(0, i - 1)];
                var across = Vector3.Cross(ns[i], t).normalized * (width / 2f);
                var c = pts[i] + ns[i] * offset;
                int a = m.Add(f.ToLocal(c - across), f.DirToLocal(ns[i]));
                int b = m.Add(f.ToLocal(c + across), f.DirToLocal(ns[i]));
                if (i > 0) QuadFacing(m, prevA, prevB, b, a, f.DirToLocal(ns[i]));
                prevA = a; prevB = b;
            }
        }

        /// <summary>Подкрылки: свод над колесом и стенка изнутри, чтобы сквозь арку не было видно пустоты.</summary>
        public void ArchLiners(ModelNode node, string mat)
        {
            var m = node.M(mat);
            var f = node.WorldFrame();
            foreach (var a in arches)
            foreach (float side in new[] { 1f, -1f })
            {
                float r = a.r + 0.012f;
                float yb = Sec(a.z).ybot;
                float th0 = Mathf.Asin(Mathf.Clamp((yb - 0.02f - a.y) / r, -1f, 1f));
                float th1 = Mathf.PI - th0;
                const int segs = 36;
                var outer = new int[segs + 1];
                var inner = new int[segs + 1];
                var wallIdx = new int[segs + 1];
                for (int i = 0; i <= segs; i++)
                {
                    float th = Mathf.Lerp(th0, th1, i / (float)segs);
                    float z = a.z + Mathf.Cos(th) * r, y = a.y + Mathf.Sin(th) * r;
                    var st = Sec(z);
                    float xo = HalfWidthAt(st, Mathf.Clamp(y, st.ybot + 0.005f, st.yc - 0.005f)) - 0.006f;
                    if (xo < a.xMin + 0.02f) xo = a.xMin + 0.02f;
                    var radial = new Vector3(0f, Mathf.Sin(th), Mathf.Cos(th));
                    var nIn = -radial;
                    outer[i] = m.Add(f.ToLocal(new Vector3(xo * side, y, z)), f.DirToLocal(nIn));
                    inner[i] = m.Add(f.ToLocal(new Vector3(a.xMin * side, y, z)), f.DirToLocal(nIn));
                    wallIdx[i] = m.Add(f.ToLocal(new Vector3(a.xMin * side, y, z)), f.DirToLocal(new Vector3(side, 0f, 0f)));
                }
                for (int i = 0; i < segs; i++)
                {
                    var n = f.DirToLocal(-new Vector3(0f, Mathf.Sin(Mathf.Lerp(th0, th1, (i + 0.5f) / segs)), Mathf.Cos(Mathf.Lerp(th0, th1, (i + 0.5f) / segs))));
                    QuadFacing(m, outer[i], outer[i + 1], inner[i + 1], inner[i], n);
                }
                // Стенка: веер из центра арки
                int c = m.Add(f.ToLocal(new Vector3(a.xMin * side, Mathf.Max(a.y - 0.05f, yb), a.z)), f.DirToLocal(new Vector3(side, 0f, 0f)));
                for (int i = 0; i < segs; i++)
                {
                    var pa = m.v[c]; var pb = m.v[wallIdx[i]]; var pc = m.v[wallIdx[i + 1]];
                    if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), f.DirToLocal(new Vector3(side, 0f, 0f))) >= 0f) m.Tri(c, wallIdx[i], wallIdx[i + 1]);
                    else m.Tri(c, wallIdx[i + 1], wallIdx[i]);
                }
            }
        }

        protected static float SegLen(int seg) => SegU[seg + 1] - SegU[seg];
    }
}

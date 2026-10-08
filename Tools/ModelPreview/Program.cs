using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GasQueue;
using UnityEngine;

// Выгружает модель машины в JSON для просмотра в браузере (three.js). Координаты Unity (левые)
// переводим в правые, отражая Z: заодно меняется и обход треугольников.
static class Program
{
    static int Main(string[] args)
    {
        string which = args.Length > 0 ? args[0] : "supra";
        if (which == "debug") { Debug(); return 0; }
        if (which == "dev") { Deviation(args[1]); return 0; }
        if (which == "normals") { Normals(args.Length > 1 ? args[1] : "supra"); return 0; }
        if (which == "ray") { Rays(args); return 0; }
        if (which == "poke") { Poke(args.Length > 1 ? args[1] : "supra"); return 0; }
        if (which == "tree") { Tree(args[1], args[2]); return 0; }
        string outPath = args.Length > 1 ? args[1] : "model.json";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Model model = which.EndsWith(".bytes") ? ModelFile.Read(File.ReadAllBytes(which)) : which switch
        {
            "x5" => X5Model.Get(),
            "akademik" => AkademikModel.Get(),
            "gelik" => GelikModel.Get(),
            "skyline" => SkylineModel.Get(),
            "rx7" => Rx7Model.Get(),
            "s2000" => S2000Model.Get(),
            _ => SupraModel.Get(),
        };
        Console.WriteLine($"build {sw.ElapsedMilliseconds} ms");
        var sb = new StringBuilder();
        sb.Append("{\"meshes\":[");
        bool first = true;
        int tris = 0, verts = 0;
        void Walk(ModelNode n)
        {
            var f = n.WorldFrame();
            for (int k = 0; k < n.meshes.Count; k++)
            {
                var m = n.meshes[k];
                if (m.t.Count == 0) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"node\":\"").Append(n.name).Append("\",\"mat\":\"").Append(n.mats[k]).Append("\",\"p\":[");
                for (int i = 0; i < m.v.Count; i++)
                {
                    var p = f.P(m.v[i]);
                    if (i > 0) sb.Append(',');
                    sb.Append(F(p.x)).Append(',').Append(F(p.y)).Append(',').Append(F(-p.z));
                }
                sb.Append("],\"n\":[");
                for (int i = 0; i < m.n.Count; i++)
                {
                    var q = f.D(m.n[i]);
                    if (i > 0) sb.Append(',');
                    sb.Append(F(q.x)).Append(',').Append(F(q.y)).Append(',').Append(F(-q.z));
                }
                sb.Append("],\"uv\":[");
                for (int i = 0; i < m.uv.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(F(m.uv[i].x)).Append(',').Append(F(m.uv[i].y));
                }
                if (m.ao != null)
                {
                    sb.Append("],\"ao\":[");
                    for (int i = 0; i < m.ao.Count; i++) { if (i > 0) sb.Append(','); sb.Append(F(m.ao[i])); }
                }
                sb.Append("],\"i\":[");
                for (int i = 0; i < m.t.Count; i += 3)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(m.t[i]).Append(',').Append(m.t[i + 2]).Append(',').Append(m.t[i + 1]);
                }
                sb.Append("]}");
                tris += m.t.Count / 3;
                verts += m.v.Count;
            }
            foreach (var c in n.children) Walk(c);
        }
        Walk(model.root);
        sb.Append("]}");
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine($"triangles {tris}, vertices {verts}");
        return 0;
    }

    static Model ByName(string which) => which switch
    {
        "x5" => X5Model.Get(),
            "gelik" => GelikModel.Get(),
        "skyline" => SkylineModel.Get(),
        "rx7" => Rx7Model.Get(),
        "s2000" => S2000Model.Get(),
        _ => SupraModel.Get(),
    };

    // Ищем испорченные нормали (нулевые, NaN) — в Unity такие пиксели горят белым или чёрным
    static void Normals(string which)
    {
        var model = ByName(which);
        var bad = new Dictionary<string, int>();
        var where = new Dictionary<string, Vector3>();
        void Walk(ModelNode n)
        {
            var f = n.WorldFrame();
            for (int k = 0; k < n.meshes.Count; k++)
            {
                var m = n.meshes[k];
                if (m.n.Count != m.v.Count || m.uv.Count != m.v.Count) Console.WriteLine($"COUNT {n.name}:{n.mats[k]} v={m.v.Count} n={m.n.Count} uv={m.uv.Count}");
                int vc = 0; foreach (var mm in n.meshes) vc += mm.v.Count;
                if (k == 0 && vc > 65000) Console.WriteLine($"BIG {n.name} {vc}");
                for (int ti = 0; ti < m.t.Count; ti += 3)
                {
                    var n0 = m.n[m.t[ti]]; var n1 = m.n[m.t[ti + 1]]; var n2 = m.n[m.t[ti + 2]];
                    float md = Mathf.Min(Vector3.Dot(n0, n1), Mathf.Min(Vector3.Dot(n1, n2), Vector3.Dot(n0, n2)));
                    var fn = Vector3.Cross(m.v[m.t[ti + 1]] - m.v[m.t[ti]], m.v[m.t[ti + 2]] - m.v[m.t[ti]]);
                    bool against = fn.sqrMagnitude > 1e-12f && Vector3.Dot(fn.normalized, (n0 + n1 + n2).normalized) < -0.2f;
                    if (md < 0f || against)
                    {
                        string key = n.name + ":" + n.mats[k] + (md < 0f ? " opposite-normals" : " normal-vs-winding");
                        bad[key] = bad.TryGetValue(key, out int c) ? c + 1 : 1;
                        where[key] = f.P(m.v[m.t[ti]]);
                    }
                }
                foreach (int ti in m.t) if (ti < 0 || ti >= m.v.Count) { Console.WriteLine($"INDEX {n.name}:{n.mats[k]} {ti}/{m.v.Count}"); break; }
                for (int ti = 0; ti < m.t.Count; ti += 3)
                {
                    var A = m.v[m.t[ti]]; var B = m.v[m.t[ti + 1]]; var C = m.v[m.t[ti + 2]];
                    float L = Mathf.Max((A - B).magnitude, Mathf.Max((B - C).magnitude, (A - C).magnitude));
                    if (L > 1.0f && n.name == "Interior") { Console.WriteLine($"LONG {n.name}:{n.mats[k]} {L:F2} {f.P(A)} {f.P(B)} {f.P(C)}"); }
                }
                for (int ui = 0; ui < m.uv.Count; ui++)
                {
                    var q = m.uv[ui];
                    if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsInfinity(q.x) || float.IsInfinity(q.y) || Math.Abs(q.x) > 1000f || Math.Abs(q.y) > 1000f)
                    {
                        string key = n.name + ":" + n.mats[k] + " BAD-UV";
                        bad[key] = bad.TryGetValue(key, out int c) ? c + 1 : 1;
                        where[key] = new Vector3(q.x, q.y, 0);
                    }
                }
                var used = new HashSet<int>(m.t);
                foreach (int i in used)
                {
                    var q = m.n[i];
                    float len = q.magnitude;
                    bool nan = float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(m.v[i].x) || float.IsNaN(m.v[i].y) || float.IsNaN(m.v[i].z);
                    if (nan || len < 0.5f || len > 1.5f)
                    {
                        string key = n.name + ":" + n.mats[k] + (nan ? " NaN" : $" len~{Math.Round(len, 1)}");
                        bad[key] = bad.TryGetValue(key, out int c) ? c + 1 : 1;
                        where[key] = f.P(m.v[i]);
                    }
                }
            }
            foreach (var c in n.children) Walk(c);
        }
        Walk(model.root);
        foreach (var kv in bad) Console.WriteLine($"{kv.Key}: {kv.Value}  e.g. {where[kv.Key]}");
        Console.WriteLine($"bad groups: {bad.Count}");
    }

    // Насколько заданные нормали расходятся с нормалями по треугольникам (как Mesh.RecalculateNormals)
    static void Deviation(string which)
    {
        var model = ByName(which);
        void Walk(ModelNode n)
        {
            var f = n.WorldFrame();
            for (int k = 0; k < n.meshes.Count; k++)
            {
                var m = n.meshes[k];
                if (m.t.Count == 0) continue;
                var acc = new Vector3[m.v.Count];
                for (int i = 0; i < m.t.Count; i += 3)
                {
                    var fn = Vector3.Cross(m.v[m.t[i + 1]] - m.v[m.t[i]], m.v[m.t[i + 2]] - m.v[m.t[i]]);
                    acc[m.t[i]] += fn; acc[m.t[i + 1]] += fn; acc[m.t[i + 2]] += fn;
                }
                int bad = 0, worse = 0; float worst = 1f; Vector3 at = Vector3.zero;
                for (int i = 0; i < m.v.Count; i++)
                {
                    if (acc[i].sqrMagnitude < 1e-20f) continue;
                    float d = Vector3.Dot(m.n[i].normalized, acc[i].normalized);
                    if (d < 0.7f) bad++;
                    if (d < 0f) worse++;
                    if (d < worst) { worst = d; at = f.P(m.v[i]); }
                }
                if (bad > 0) Console.WriteLine($"{n.name}:{n.mats[k]} verts {m.v.Count} dev>45° {bad} opposite {worse} worst {worst:F2} at {at}");
            }
            foreach (var c in n.children) Walk(c);
        }
        Walk(model.root);
    }

    // Сравнение: точка на формуле кузова и пересечение луча с готовой сеткой (ищем «утонувшие» накладки)
    static void Debug()
    {
        var shape = SupraModel.Shape();
        var model = SupraModel.Get();
        var tris = new List<(Vector3, Vector3, Vector3, string)>();
        void Walk(ModelNode n)
        {
            var f = n.WorldFrame();
            for (int k = 0; k < n.meshes.Count; k++)
            {
                var m = n.meshes[k];
                for (int i = 0; i < m.t.Count; i += 3)
                    tris.Add((f.P(m.v[m.t[i]]), f.P(m.v[m.t[i + 1]]), f.P(m.v[m.t[i + 2]]), n.name + ":" + n.mats[k]));
            }
            foreach (var c in n.children) Walk(c);
        }
        Walk(model.root);
        foreach (var (y, zo, dz) in new[] { (0.55f, -1.6f, -1f), (0.3f, 1.6f, 1f) })
        foreach (float x in new[] { 0f, 0.005f, 0.02f, 0.05f, 0.1f, 0.2f })
        {
            var o = new Vector3(x, y, zo);
            var d = new Vector3(0, 0, dz);
            shape.Hit(o, d, out float z, out float u);
            var p = shape.S(z, u);
            string hits = "";
            foreach (var (a, b, c, name) in tris)
            {
                if (!RayTri(o, d, a, b, c, out float t)) continue;
                if (t > 0.3f) hits += $" {name}@{zo + dz * t:F4}";
            }
            Console.WriteLine($"y={y} x={x}: formula z={p.z:F4} x={p.x:F4} | mesh:{hits}");
        }
    }

    // Ищем детали салона, торчащие сквозь кузов: вершина внутреннего материала снаружи формы
    static void Poke(string name)
    {
        CarBody shape = name == "x5" ? X5Model.Shape() : name == "gelik" ? GelikModel.Shape() : name == "skyline" ? (CarBody)SkylineModel.Shape() : name == "rx7" ? Rx7Model.Shape() : name == "s2000" ? S2000Model.Shape() : SupraModel.Shape();
        var model = name == "x5" ? X5Model.Get() : name == "gelik" ? GelikModel.Get() : name == "skyline" ? SkylineModel.Get() : name == "rx7" ? Rx7Model.Get() : name == "s2000" ? S2000Model.Get() : SupraModel.Get();
        var inner = new HashSet<string> { "carpet", "int_black", "int_grey", "int_door", "int_roof", "leather_red", "leather_black", "gauge_face", "gauge_glow", "jacket", "skin", "hair", "screen", "white", "needle", "lamp_off", "screen_blue", "ambient", "cloth_dark", "leather_blue", "gauge_light", "screen_amber", "leather_tan", "carbon", "fur_pink" };
        var stats = new Dictionary<string, (int n, float worst, Vector3 at)>();
        void Walk(ModelNode n)
        {
            var f = n.WorldFrame();
            for (int k = 0; k < n.meshes.Count; k++)
            {
                if (!inner.Contains(n.mats[k])) continue;
                foreach (var lv in n.meshes[k].v)
                {
                    var p = f.P(lv);
                    if (shape.Inside(p)) continue;
                    // насколько снаружи: ищем ближайшую точку внутри по направлению к оси
                    float d = 0f;
                    var c = new Vector3(0, 0.7f, p.z);
                    var dir = (c - p).normalized;
                    for (float t = 0.002f; t < 0.3f; t += 0.002f) if (shape.Inside(p + dir * t)) { d = t; break; }
                    string key = n.name + ":" + n.mats[k];
                    stats.TryGetValue(key, out var s);
                    if (d > s.worst) s = (s.n + 1, d, p); else s.n++;
                    stats[key] = s;
                }
            }
            foreach (var c in n.children) Walk(c);
        }
        Walk(model.root);
        foreach (var kv in stats) Console.WriteLine($"{kv.Key}: {kv.Value.n} verts outside, worst {kv.Value.worst * 100:F1} cm at {kv.Value.at}");
    }

    // Что видит глаз водителя в пикселе скриншота Unity: ray <model> eyeX eyeY eyeZ pitch W H fov px,py ...
    static void Rays(string[] a)
    {
        var model = ByName(a[1]);
        var eye = new Vector3(float.Parse(a[2], CultureInfo.InvariantCulture), float.Parse(a[3], CultureInfo.InvariantCulture), float.Parse(a[4], CultureInfo.InvariantCulture));
        float pitch = float.Parse(a[5], CultureInfo.InvariantCulture) * Mathf.Deg2Rad;
        float W = float.Parse(a[6]), H = float.Parse(a[7]), fov = float.Parse(a[8], CultureInfo.InvariantCulture);
        var tris = new List<(Vector3, Vector3, Vector3, string)>();
        void Walk(ModelNode n)
        {
            var f = n.WorldFrame();
            for (int k = 0; k < n.meshes.Count; k++)
            {
                var m = n.meshes[k];
                for (int i = 0; i < m.t.Count; i += 3)
                    tris.Add((f.P(m.v[m.t[i]]), f.P(m.v[m.t[i + 1]]), f.P(m.v[m.t[i + 2]]), n.name + ":" + n.mats[k]));
            }
            foreach (var c in n.children) Walk(c);
        }
        Walk(model.root);
        float th = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        var fwd = new Vector3(0, -Mathf.Sin(pitch), Mathf.Cos(pitch));
        var up = new Vector3(0, Mathf.Cos(pitch), Mathf.Sin(pitch));
        for (int i = 9; i < a.Length; i++)
        {
            var xy = a[i].Split(',');
            float px = float.Parse(xy[0]), py = float.Parse(xy[1]);
            float sx = (px - W / 2) / (H / 2) * th, sy = (H / 2 - py) / (H / 2) * th;
            var d = (fwd + Vector3.right * sx + up * sy).normalized;
            var hits = new List<(float, string)>();
            foreach (var (p0, p1, p2, name) in tris)
                if (RayTri(eye, d, p0, p1, p2, out float t))
                {
                    bool front = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), d) < 0;
                    hits.Add((t, (front ? "F " : "b ") + name));
                }
            hits.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            var sb = new StringBuilder($"{a[i]} dir {d}: ");
            for (int k = 0; k < Math.Min(5, hits.Count); k++) sb.Append($" [{hits[k].Item1:F2} {hits[k].Item2}]");
            Console.WriteLine(sb);
        }
    }

    static bool RayTri(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c, out float t)
    {
        t = 0;
        var e1 = b - a; var e2 = c - a;
        var p = Vector3.Cross(d, e2);
        float det = Vector3.Dot(e1, p);
        if (Math.Abs(det) < 1e-9f) return false;
        float inv = 1f / det;
        var s = o - a;
        float u = Vector3.Dot(s, p) * inv;
        if (u < 0 || u > 1) return false;
        var q = Vector3.Cross(s, e1);
        float v = Vector3.Dot(d, q) * inv;
        if (v < 0 || u + v > 1) return false;
        t = Vector3.Dot(e2, q) * inv;
        return t > 0;
    }

    // Дерево узлов в координатах Unity (локальные положения, повороты, сетки) — для Blender (Tools/Blender/x5_build.py)
    static void Tree(string which, string outPath)
    {
        var model = ByName(which);
        var sb = new StringBuilder("{\"nodes\":[");
        int count = 0;
        void Walk(ModelNode n, int parent)
        {
            int me = count++;
            if (me > 0) sb.Append(',');
            sb.Append("{\"name\":\"").Append(n.name).Append("\",\"parent\":").Append(parent)
              .Append(",\"pos\":[").Append(F(n.pos.x)).Append(',').Append(F(n.pos.y)).Append(',').Append(F(n.pos.z))
              .Append("],\"euler\":[").Append(F(n.euler.x)).Append(',').Append(F(n.euler.y)).Append(',').Append(F(n.euler.z)).Append("],\"meshes\":[");
            bool first = true;
            for (int k = 0; k < n.meshes.Count; k++)
            {
                var m = n.meshes[k];
                if (m.t.Count == 0) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"mat\":\"").Append(n.mats[k]).Append("\",\"v\":[");
                for (int i = 0; i < m.v.Count; i++) { if (i > 0) sb.Append(','); sb.Append(F(m.v[i].x)).Append(',').Append(F(m.v[i].y)).Append(',').Append(F(m.v[i].z)); }
                sb.Append("],\"n\":[");
                for (int i = 0; i < m.n.Count; i++) { if (i > 0) sb.Append(','); sb.Append(F(m.n[i].x)).Append(',').Append(F(m.n[i].y)).Append(',').Append(F(m.n[i].z)); }
                sb.Append("],\"uv\":[");
                for (int i = 0; i < m.uv.Count; i++) { if (i > 0) sb.Append(','); sb.Append(F(m.uv[i].x)).Append(',').Append(F(m.uv[i].y)); }
                sb.Append("],\"t\":[");
                for (int i = 0; i < m.t.Count; i++) { if (i > 0) sb.Append(','); sb.Append(m.t[i]); }
                sb.Append("]}");
            }
            sb.Append("]}");
            foreach (var c in n.children) Walk(c, me);
        }
        Walk(model.root, -1);
        sb.Append("]}");
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine($"nodes {count}");
    }

    static string F(float v) => float.IsNaN(v) ? "0" : v.ToString("0.#####", CultureInfo.InvariantCulture);
}

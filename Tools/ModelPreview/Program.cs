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
        string outPath = args.Length > 1 ? args[1] : "model.json";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Model model = which switch
        {
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

    static string F(float v) => float.IsNaN(v) ? "0" : v.ToString("0.#####", CultureInfo.InvariantCulture);
}

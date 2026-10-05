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

    static string F(float v) => float.IsNaN(v) ? "0" : v.ToString("0.#####", CultureInfo.InvariantCulture);
}

using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Временная отладка белых пятен в салоне спорткара (клавиша K).
    /// 1 — «прицел»: в центре экрана крестик, сверху надпись, какая деталь машины под ним.
    /// 2 — видна только машина игрока, вокруг пурпурный фон: если белое станет пурпурным — это дыра.
    /// </summary>
    public class CabinDebug : MonoBehaviour
    {
        static readonly string[] Names =
        {
            "0 — как обычно",
            "1 — прицел: наведите крестик на белое и сделайте скриншот",
            "2 — только машина на пурпурном фоне",
        };

        class MeshInfo
        {
            public Vector3[] v;
            public int[][] subs;
        }

        int mode;
        string label = "";
        float nextRay;
        readonly Dictionary<Mesh, MeshInfo> cache = new Dictionary<Mesh, MeshInfo>();
        readonly List<Renderer> hidden = new List<Renderer>();
        CameraClearFlags savedClear;
        Color savedBg;
        bool savedFog;

        void Update()
        {
            if (GameInput.DebugCabinPressed)
            {
                SetMode((mode + 1) % Names.Length);
                if (GameManager.Instance != null) GameManager.Instance.ShowMessage("Отладка салона (K): " + Names[mode], 6f);
            }
            if (mode == 1 && Time.unscaledTime >= nextRay)
            {
                nextRay = Time.unscaledTime + 0.2f;
                label = Probe();
            }
        }

        void SetMode(int m)
        {
            if (mode == 2) Restore();
            mode = m;
            if (mode == 2) Isolate();
        }

        void Isolate()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                savedClear = cam.clearFlags;
                savedBg = cam.backgroundColor;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.magenta;
            }
            savedFog = RenderSettings.fog;
            RenderSettings.fog = false;
            hidden.Clear();
            foreach (var r in FindObjectsOfType<Renderer>())
                if (r.enabled && !r.transform.IsChildOf(transform))
                {
                    r.enabled = false;
                    hidden.Add(r);
                }
        }

        void Restore()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = savedClear;
                cam.backgroundColor = savedBg;
            }
            RenderSettings.fog = savedFog;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
        }

        /// <summary>Луч из центра камеры по треугольникам всех деталей машины: первые три попадания.</summary>
        string Probe()
        {
            var cam = Camera.main;
            if (cam == null) return "";
            var o = cam.transform.position;
            var d = cam.transform.forward;
            var hits = new List<(float t, string text)>();
            foreach (var mf in GetComponentsInChildren<MeshFilter>())
            {
                var r = mf.GetComponent<Renderer>();
                var mesh = mf.sharedMesh;
                if (r == null || !r.enabled || mesh == null || !mesh.isReadable) continue;
                if (!cache.TryGetValue(mesh, out var info))
                {
                    info = new MeshInfo { v = mesh.vertices, subs = new int[mesh.subMeshCount][] };
                    for (int s = 0; s < mesh.subMeshCount; s++) info.subs[s] = mesh.GetTriangles(s);
                    cache[mesh] = info;
                }
                var t = mf.transform;
                var mats = r.sharedMaterials;
                for (int s = 0; s < info.subs.Length; s++)
                {
                    var tri = info.subs[s];
                    for (int i = 0; i + 2 < tri.Length; i += 3)
                    {
                        var a = t.TransformPoint(info.v[tri[i]]);
                        var b = t.TransformPoint(info.v[tri[i + 1]]);
                        var c = t.TransformPoint(info.v[tri[i + 2]]);
                        if (!RayTri(o, d, a, b, c, out float dist)) continue;
                        bool front = Vector3.Dot(Vector3.Cross(b - a, c - a), d) < 0f;
                        string mat = s < mats.Length && mats[s] != null ? mats[s].name : "?";
                        hits.Add((dist, $"{t.name} / {mat} / {dist:F2} м / {(front ? "лицо" : "изнанка")}"));
                    }
                }
            }
            if (hits.Count == 0) return "под прицелом нет деталей машины";
            hits.Sort((x, y) => x.t.CompareTo(y.t));
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < Mathf.Min(3, hits.Count); k++) sb.Append(k + 1).Append(") ").Append(hits[k].text).Append('\n');
            return sb.ToString();
        }

        static bool RayTri(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            t = 0f;
            var e1 = b - a;
            var e2 = c - a;
            var p = Vector3.Cross(d, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-10f) return false;
            float inv = 1f / det;
            var s = o - a;
            float u = Vector3.Dot(s, p) * inv;
            if (u < 0f || u > 1f) return false;
            var q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(d, q) * inv;
            if (v < 0f || u + v > 1f) return false;
            t = Vector3.Dot(e2, q) * inv;
            return t > 0f;
        }

        void OnGUI()
        {
            if (mode != 1) return;
            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            var old = GUI.color;
            GUI.color = Color.red;
            GUI.DrawTexture(new Rect(cx - 12, cy - 1, 24, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1, cy - 12, 2, 24), Texture2D.whiteTexture);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(cx - 330, 60, 660, 76), Texture2D.whiteTexture);
            GUI.color = old;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 17 };
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(cx - 320, 64, 640, 72), label, style);
        }

        void OnDestroy()
        {
            if (mode == 2) Restore();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GasQueue
{
    /// <summary>Формы, которых нет среди стандартных примитивов Unity: кольцо (обод руля) и прозрачное стекло.</summary>
    public static class MeshFactory
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
        static Material glass;

        /// <summary>Тор (бублик) в плоскости XZ: radius — радиус кольца, thickness — радиус трубки.</summary>
        public static Mesh Torus(float radius, float thickness, int segments = 28, int sides = 10)
        {
            string key = $"torus_{radius}_{thickness}_{segments}_{sides}";
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                for (int j = 0; j <= sides; j++)
                {
                    float b = j / (float)sides * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b));
                    verts.Add(center + n * thickness);
                    normals.Add(n);
                }
            }
            for (int i = 0; i < segments; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * (sides + 1) + j, b = a + sides + 1;
                tris.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
            }

            var mesh = new Mesh { name = key };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            cache[key] = mesh;
            return mesh;
        }

        /// <summary>Горизонтальный прямоугольник (смотрит вверх) с UV в метрах / uvMeters.</summary>
        public static Mesh Quad(float sizeX, float sizeZ, Vector2 uvMeters)
        {
            float hx = sizeX / 2f, hz = sizeZ / 2f;
            var mesh = new Mesh { name = "Quad" };
            mesh.vertices = new[] { new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(hx, 0, -hz) };
            mesh.uv = new[]
            {
                new Vector2(0, 0), new Vector2(0, sizeZ / uvMeters.y),
                new Vector2(sizeX / uvMeters.x, sizeZ / uvMeters.y), new Vector2(sizeX / uvMeters.x, 0),
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Коробка, у которой текстура на каждой грани повторяется каждые uvMeters метров (для фасадов).</summary>
        public static Mesh BoxUV(Vector3 size, float uvMeters)
        {
            var h = size / 2f;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            void Face(Vector3 n, Vector3 u, Vector3 v, float w, float hgt)
            {
                var c = Vector3.Scale(n, h);
                int start = verts.Count;
                verts.Add(c - u * (w / 2f) - v * (hgt / 2f));
                verts.Add(c - u * (w / 2f) + v * (hgt / 2f));
                verts.Add(c + u * (w / 2f) + v * (hgt / 2f));
                verts.Add(c + u * (w / 2f) - v * (hgt / 2f));
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, hgt / uvMeters));
                uvs.Add(new Vector2(w / uvMeters, hgt / uvMeters));
                uvs.Add(new Vector2(w / uvMeters, 0));
                for (int i = 0; i < 4; i++) normals.Add(n);
                tris.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            // v × u смотрит наружу (по нормали) — так грань видна снаружи
            Face(Vector3.forward, Vector3.left, Vector3.up, size.x, size.y);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z, size.y);
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z);

            var mesh = new Mesh { name = "BoxUV" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Призма по выпуклому силуэту сбоку: profile — точки (z, y), ширина плавно меняется
        /// от halfWidthBottom внизу до halfWidthTop вверху (так кузов «заваливается» к крыше).
        /// Плоское затенение — каждая грань со своими нормалями.
        /// </summary>
        public static Mesh Prism(IList<Vector2> profile, float halfWidthBottom, float halfWidthTop)
        {
            float yMin = float.MaxValue, yMax = float.MinValue;
            var center = Vector3.zero;
            foreach (var p in profile)
            {
                yMin = Mathf.Min(yMin, p.y);
                yMax = Mathf.Max(yMax, p.y);
                center += new Vector3(0f, p.y, p.x);
            }
            center /= profile.Count;
            float W(float y) => Mathf.Lerp(halfWidthBottom, halfWidthTop, (y - yMin) / Mathf.Max(0.001f, yMax - yMin));
            Vector3 L(int i) => new Vector3(-W(profile[i].y), profile[i].y, profile[i].x);
            Vector3 R(int i) => new Vector3(W(profile[i].y), profile[i].y, profile[i].x);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                var n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-10f) return;
                n.Normalize();
                if (Vector3.Dot(n, (a + b + c) / 3f - center) < 0f)
                {
                    (b, c) = (c, b);
                    n = -n;
                }
                int i0 = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                normals.Add(n); normals.Add(n); normals.Add(n);
                tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
            }

            int count = profile.Count;
            for (int i = 1; i < count - 1; i++)
            {
                Tri(L(0), L(i), L(i + 1));
                Tri(R(0), R(i), R(i + 1));
            }
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                Tri(L(i), L(j), R(j));
                Tri(L(i), R(j), R(i));
            }

            var mesh = new Mesh { name = "Prism" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Material tintedGlass;

        /// <summary>Тонированное стекло машин NPC: водителя видно, но темновато.</summary>
        public static Material TintedGlass
        {
            get
            {
                if (tintedGlass != null) return tintedGlass;
                tintedGlass = new Material(Glass) { name = "TintedGlass", color = new Color(0.18f, 0.24f, 0.3f, 0.55f) };
                return tintedGlass;
            }
        }

        public static Material Textured(Texture2D tex, string name)
        {
            var m = new Material(Shapes.Mat(Color.white)) { name = name, mainTexture = tex, color = Color.white };
            return m;
        }

        public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Vector3 pos, Vector3 euler, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>
        /// Полупрозрачный материал стекла. Переключает стандартный шейдер в режим прозрачности;
        /// если шейдер другой (например, URP) — пытается сделать то же через его свойства.
        /// </summary>
        public static Material Glass
        {
            get
            {
                if (glass != null) return glass;
                glass = new Material(Shapes.Mat(Color.white)) { name = "Glass", color = new Color(0.65f, 0.8f, 0.9f, 0.16f) };
                if (glass.HasProperty("_Mode")) glass.SetFloat("_Mode", 3f);       // Built-in Standard: Transparent
                if (glass.HasProperty("_Surface")) glass.SetFloat("_Surface", 1f); // URP Lit: Transparent
                glass.SetOverrideTag("RenderType", "Transparent");
                glass.SetInt("_SrcBlend", (int)BlendMode.One);
                glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                glass.SetInt("_ZWrite", 0);
                glass.DisableKeyword("_ALPHATEST_ON");
                glass.DisableKeyword("_ALPHABLEND_ON");
                glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (glass.HasProperty("_Glossiness")) glass.SetFloat("_Glossiness", 0.9f);
                if (glass.HasProperty("_Smoothness")) glass.SetFloat("_Smoothness", 0.9f);
                glass.renderQueue = (int)RenderQueue.Transparent;
                return glass;
            }
        }
    }
}

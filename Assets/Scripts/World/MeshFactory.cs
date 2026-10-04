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

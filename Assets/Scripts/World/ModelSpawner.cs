using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GasQueue
{
    /// <summary>
    /// Превращает модель из <see cref="ModelKit"/> (чистые данные) в объекты Unity.
    /// Сетки строятся один раз и переиспользуются всеми экземплярами (у каждой машины свои только материалы).
    /// </summary>
    public static class ModelSpawner
    {
        static readonly Dictionary<ModelNode, Mesh> meshes = new Dictionary<ModelNode, Mesh>();

        /// <summary>Создать дерево объектов под parent. Возвращает узлы по именам.</summary>
        public static Dictionary<string, Transform> Spawn(ModelNode node, Transform parent, Func<string, Material> material)
        {
            var map = new Dictionary<string, Transform>();
            Build(node, parent, material, map);
            return map;
        }

        static void Build(ModelNode node, Transform parent, Func<string, Material> material, Dictionary<string, Transform> map)
        {
            var go = new GameObject(node.name);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = node.pos;
            t.localRotation = Quaternion.Euler(node.euler);
            if (!map.ContainsKey(node.name)) map[node.name] = t;

            var mesh = MeshOf(node);
            if (mesh != null)
            {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                var mats = new List<Material>();
                for (int i = 0; i < node.mats.Count; i++)
                    if (node.meshes[i].t.Count > 0) mats.Add(material(node.mats[i]));
                r.sharedMaterials = mats.ToArray();
                // Внутренняя обшивка кузова не отбрасывает тень (иначе пятна на краске)
                if (node.name.EndsWith("Inner")) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var c in node.children) Build(c, t, material, map);
        }

        static Mesh MeshOf(ModelNode node)
        {
            // Сетку могли уничтожить при выходе из Play (если домен не перезагружается) — тогда строим заново
            if (meshes.TryGetValue(node, out var cached) && cached != null) return cached;
            Mesh mesh = null;
            int total = 0, subs = 0;
            foreach (var m in node.meshes)
            {
                if (m.t.Count == 0) continue;
                total += m.v.Count;
                subs++;
            }
            if (subs > 0)
            {
                var v = new List<Vector3>(total);
                var nrm = new List<Vector3>(total);
                var uv = new List<Vector2>(total);
                var tris = new List<int[]>();
                foreach (var m in node.meshes)
                {
                    if (m.t.Count == 0) continue;
                    int start = v.Count;
                    v.AddRange(m.v);
                    nrm.AddRange(m.n);
                    uv.AddRange(m.uv);
                    var t = new int[m.t.Count];
                    for (int i = 0; i < t.Length; i++) t[i] = m.t[i] + start;
                    tris.Add(t);
                }
                mesh = new Mesh { name = node.name };
                if (v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(v);
                mesh.SetUVs(0, uv);
                mesh.subMeshCount = tris.Count;
                for (int i = 0; i < tris.Count; i++) mesh.SetTriangles(tris[i], i);
                // Нормали считает сам Unity по треугольникам: с расчётными нормалями модели на изогнутых
                // поверхностях (торпеда, стойки) в салоне вылезали чисто белые и чёрные пятна.
                if (node.keepNormals) mesh.SetNormals(nrm); // модель из Blender: сглаживание и кромки уже посчитаны
                else mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
            meshes[node] = mesh;
            return mesh;
        }
    }

    /// <summary>Материалы спорткаров по ключам из моделей: глянцевая краска, хром, кожа, светящиеся фонари.</summary>
    public static class CarMaterials
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Material glowBase;

        public static Material Get(string key, Color paint, float paintSmoothness = 0.82f)
        {
            string id = key == "paint" ? "paint" + ColorUtility.ToHtmlStringRGB(paint) + paintSmoothness : key;
            if (cache.TryGetValue(id, out var m) && m != null) return m;
            m = Create(key, paint, paintSmoothness);
            m.name = "Car_" + id;
            cache[id] = m;
            return m;
        }

        static Material Create(string key, Color paint, float paintSmoothness)
        {
            switch (key)
            {
                case "paint": return Surface(paint, paintSmoothness, paintSmoothness < 0.7f ? 0.45f : 0.25f);
                case "gold_camo": return GoldCamo();
                case "decal_pit": return Decal("Decals/pitbull");
                case "stripe": return Surface(Hex("#2f7fe0"), 0.85f, 0.15f);
                case "stripe_dark": return Surface(Hex("#1d3fae"), 0.85f, 0.15f);
                case "wing_blue": return Surface(Hex("#2347c4"), 0.85f, 0.2f);
                case "cloth_dark": return Surface(Hex("#1e1f22"), 0.05f, 0f);
                case "leather_blue": return Surface(Hex("#2456c8"), 0.22f, 0f);
                case "gauge_light": return Glow(Hex("#d8d8d4"), Hex("#3a3a36"));
                case "screen_amber": return Glow(Hex("#2a1a05"), Hex("#c88a20") * 0.7f);
                case "stripe_silver": return Surface(Hex("#d9dde2"), 0.85f, 0.6f);
                case "stripe_green": return Surface(Hex("#3fdc4a"), 0.6f, 0f);
                case "stripe_yellow": return Surface(Hex("#f2c81a"), 0.6f, 0f);
                case "leather_tan": return Surface(Hex("#c49a6c"), 0.2f, 0f);
                case "leather_cream": return Surface(Hex("#d9ccb4"), 0.22f, 0f);
                case "int_beige": return Surface(Hex("#cbbfa8"), 0.1f, 0f);
                case "carpet_beige": return Surface(Hex("#8a7a63"), 0f, 0f);
                case "int_roof_light": return Surface(Hex("#cfc8bb"), 0.05f, 0f);
                case "piano_black": return Surface(Hex("#050506"), 0.92f, 0f);
                case "wood_dark": return Surface(Hex("#4a3a30"), 0.55f, 0f);
                case "alloy_machined": return Surface(Hex("#d2d5d9"), 0.85f, 0.8f);
                case "rim_gunmetal": return Surface(Hex("#3a3d42"), 0.6f, 0.6f);
                case "mater_rust": return Textured("Textures/mater_rust", 0.12f);
                case "mater_blue": return Textured("Textures/mater_blue", 0.3f);
                case "mater_green": return Textured("Textures/mater_green", 0.35f);
                case "mater_stripes": return Textured("Textures/mater_stripes", 0.3f);
                case "mater_eye": return Textured("Textures/mater_eye", 0.85f);
                case "eye_white": return Surface(Hex("#f4f1e6"), 0.85f, 0f);
                case "teeth": return Surface(Hex("#eee2bf"), 0.5f, 0f);
                case "mouth": return Surface(Hex("#1e0b07"), 0.1f, 0f);
                case "tongue": return Surface(Hex("#8a2c2a"), 0.5f, 0f);
                case "rust_dark": return Surface(Hex("#3d2416"), 0.1f, 0f);
                case "seat_brown": return Surface(Hex("#6b3e26"), 0.3f, 0f);
                case "steel_dark": return Surface(Hex("#3b3f39"), 0.3f, 0.3f);
                case "cloth_red": return Surface(Hex("#a3141c"), 0.08f, 0f);
                case "stitch_red": return Surface(Hex("#c41620"), 0.3f, 0f);
                case "caliper_yellow": return Surface(Hex("#f2c414"), 0.6f, 0.1f);
                case "carbon": return Surface(Hex("#2b2d31"), 0.45f, 0.2f);
                case "stripe_white": return Surface(Hex("#f7f7f7"), 0.7f, 0f);
                case "stripe_magenta": return Surface(Hex("#c2188a"), 0.75f, 0f);
                case "stripe_purple": return Surface(Hex("#6b1f8e"), 0.75f, 0f);
                case "fur_pink": return Surface(Hex("#f3d6dc"), 0f, 0f);
                case "tail_smoke": return Glow(Hex("#3a0c10"), Hex("#2a0004"));
                case "glass_dark": return Surface(Hex("#0e1216"), 0.92f, 0.3f);
                case "chassis": return Surface(Hex("#1b1b1c"), 0.15f, 0f);
                case "rim_black": return Surface(Hex("#151517"), 0.6f, 0.4f);
                case "black_satin": return Surface(Hex("#1c1d20"), 0.45f, 0.2f);
                case "bin_inner": return Surface(Hex("#16291a"), 0.1f, 0f);
                case "paint_dark": return Surface(Hex("#1f3d1e"), 0.35f, 0f);
                case "helmet_lime": return Surface(Hex("#b7dc3c"), 0.8f, 0.05f);
                case "helmet_white": return Surface(Hex("#f3f3ef"), 0.8f, 0.05f);
                case "visor": return Surface(Hex("#1a2230"), 0.95f, 0.6f);
                case "jacket_dark": return Surface(Hex("#24262b"), 0.1f, 0f);
                case "screen_blue": return Glow(Hex("#0a1838"), Hex("#1a3a8a"));
                case "ambient": return Glow(Hex("#2a5cff"), Hex("#2a5cff") * 1.4f);
                case "glass": return CarGlass(new Color(0.12f, 0.16f, 0.2f, 0.45f));
                case "screen_dark": return Glow(Hex("#05080c"), Hex("#0a1424")); // экран почти чёрный, цифры светятся сами
                case "glass_tint": return CarGlass(new Color(0.03f, 0.04f, 0.05f, 0.62f)); // тонировка: темнее снаружи, но изнутри дорогу видно
                case "lens": return new Material(MeshFactory.Glass) { color = new Color(0.85f, 0.9f, 0.95f, 0.12f) };
                case "black": return Surface(Hex("#0b0b0d"), 0.25f, 0f);
                case "liner": return Surface(Hex("#0c0c0c"), 0.05f, 0f);
                case "grille": return Surface(Hex("#0d0d0f"), 0.2f, 0f);
                case "rubber": return Surface(Hex("#151515"), 0.12f, 0f);
                case "tread": return Surface(Hex("#0a0a0a"), 0.05f, 0f);
                case "chrome": return Surface(Hex("#e8ecf0"), 0.92f, 0.85f);
                case "mirror": return Surface(Hex("#cfd6de"), 0.95f, 0.9f);
                case "alloy": return Surface(Hex("#cfd2d6"), 0.8f, 0.75f);
                case "rim_inner": return Surface(Hex("#74787d"), 0.5f, 0.6f);
                case "disc": return Surface(Hex("#6a6c70"), 0.5f, 0.7f);
                case "caliper": return Surface(Hex("#b5161b"), 0.6f, 0.1f);
                case "housing": return Surface(Hex("#202328"), 0.75f, 0.3f);
                case "reflector": return Surface(Hex("#f0f2f4"), 0.95f, 0.9f);
                case "lamp_glow": return Glow(Hex("#fff7e0"), Hex("#ffe9b8") * 0.6f);
                case "amber": return Glow(Hex("#ff8a1c"), Hex("#4a2000"));
                case "tail_red": return Glow(Hex("#d0141c"), Hex("#5a0005"));
                case "tail_clear": return Surface(Hex("#e2e8ee"), 0.85f, 0.3f);
                case "plate": return Surface(Hex("#f4f4f2"), 0.4f, 0f);
                case "int_door": return Surface(Hex("#1d1d20"), 0.06f, 0f);
                case "int_roof": return Surface(Hex("#3c3c41"), 0.05f, 0f);
                case "carpet": return Surface(Hex("#161616"), 0f, 0f);
                case "leather_red": return Surface(Hex("#9e1219"), 0.22f, 0f);
                case "leather_black": return Surface(Hex("#141416"), 0.18f, 0f);
                case "int_black": return Surface(Hex("#1a1a1d"), 0.06f, 0f);
                case "int_grey": return Surface(Hex("#8b8e93"), 0.35f, 0.3f);
                case "gauge_face": return Surface(Hex("#0a0a0b"), 0.25f, 0f);
                case "led_off": return Surface(Hex("#2a1a0c"), 0.2f, 0f);
                case "led_on": return Glow(Hex("#ffb020"), Hex("#ff9a10") * 1.3f);
                case "led_red": return Glow(Hex("#ff3020"), Hex("#ff2010") * 1.3f);
                case "gauge_glow": return Glow(Hex("#ff4a1a"), Hex("#ff3000") * 0.9f);
                case "white": return Glow(Hex("#f2f2f2"), Hex("#606060"));
                case "needle": return Glow(Hex("#ff3320"), Hex("#801500"));
                case "screen": return Glow(Hex("#0c1a12"), Hex("#0c2a18"));
                case "lamp_off": return Surface(Hex("#3a2a10"), 0.3f, 0f);
                case "skin": return Surface(Hex("#e2b08a"), 0.1f, 0f);
                case "hair": return Surface(Hex("#3a2717"), 0.1f, 0f);
                case "jacket": return Surface(Hex("#2a2c33"), 0.1f, 0f);
                default: return Surface(Color.magenta, 0.3f, 0f);
            }
        }

        static Color Hex(string h) => Shapes.Hex(h);


        /// <summary>Золотой хромированный камуфляж: текстура-«пятна» с высоким блеском. UV кузова — (z в метрах, доля контура), поэтому тайлинг растянут.</summary>
        static Material GoldCamo()
        {
            var m = Surface(Color.white, 0.88f, 0.55f); // хром: блеск высокий, металлик не до конца — иначе без отражений будет чёрным
            m.mainTexture = TextureFactory.GoldCamo;
            m.mainTextureScale = Vector2.one; // UV деталей кузова X5 считает X5Model.Triplanar (плитка 2 м)
            return m;
        }

        /// <summary>Материал с текстурой из Resources (ржавчина, краска Мэтра); UV уже в метрах — плитка задаётся моделью.</summary>
        static Material Textured(string path, float smoothness)
        {
            var m = Surface(Color.white, smoothness, 0f);
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null) m.mainTexture = tex;
            else m.color = Hex("#7a4a2a");
            return m;
        }

        /// <summary>Наклейка из Resources (полностью непрозрачная, форма круга задаётся сеткой).</summary>
        static Material Decal(string path)
        {
            var m = Surface(Color.white, 0.5f, 0f);
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null) m.mainTexture = tex;
            else m.color = Hex("#202020");
            return m;
        }

        /// <summary>
        /// Стекло кузова: обычная прозрачность (отражение тоже ослабляется прозрачностью) и без зеркального
        /// отражения неба. Иначе под острым углом (низ лобового, у стоек) стекло горело белым и закрывало торпеду.
        /// </summary>
        public static Material CarGlass(Color c)
        {
            var saved = Resources.Load<Material>("GasQueueGenerated/CarGlass"); // в сборке — с нужным вариантом шейдера
            var m = new Material(saved != null ? saved : MeshFactory.Glass) { color = c };
            SetupCarGlass(m);
            return m;
        }

        public static void SetupCarGlass(Material m)
        {
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 2f); // Built-in Standard: Fade
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            if (m.HasProperty("_GlossyReflections")) m.SetFloat("_GlossyReflections", 0f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.6f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.6f);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        static Material Surface(Color c, float smoothness, float metallic)
        {
            var m = new Material(Shapes.Mat(Color.white)) { color = c };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }

        /// <summary>Светится сам по себе (фонари, подсветка приборов). Основа — материал из Resources, чтобы вариант шейдера с подсветкой попал в сборку.</summary>
        static Material Glow(Color c, Color emission)
        {
            if (glowBase == null) glowBase = Resources.Load<Material>("GasQueueGenerated/Glow");
            var m = glowBase != null ? new Material(glowBase) { color = c } : Surface(c, 0.6f, 0f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.7f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.7f);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }
    }
}

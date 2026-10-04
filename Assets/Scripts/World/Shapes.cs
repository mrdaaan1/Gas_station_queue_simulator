using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Помощник для сборки всего мира из примитивов Unity (кубы, цилиндры, сферы).
    /// Это временная графика прототипа: потом заменим на low-poly модели.
    /// </summary>
    public static class Shapes
    {
        static Material baseMaterial;
        static readonly Dictionary<Color, Material> cache = new Dictionary<Color, Material>();

        public static Material Mat(Color color)
        {
            if (cache.TryGetValue(color, out var m) && m != null) return m;
            if (baseMaterial == null)
            {
                // Берём материал по умолчанию у примитива: так он подходит под любой рендер-пайплайн.
                var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMaterial = probe.GetComponent<Renderer>().sharedMaterial;
                Object.Destroy(probe);
            }
            m = new Material(baseMaterial) { color = color };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            cache[color] = m;
            return m;
        }

        public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale,
            Color color, Vector3 localEuler = default, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Color color, Vector3 euler = default, string name = null)
            => Make(PrimitiveType.Cube, parent, pos, size, color, euler, name);

        /// <summary>Пустой объект-контейнер.</summary>
        public static Transform Group(string name, Transform parent, Vector3 localPos = default, Vector3 localEuler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            return go.transform;
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }

    public static class Fonts
    {
        static Font font;

        public static Font Default
        {
            get
            {
                if (font == null)
                {
#if UNITY_2022_2_OR_NEWER
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                }
                return font;
            }
        }

        static Material depthTested;

        /// <summary>
        /// Материал шрифта, который прячется за стенами и крышей (свой шейдер GasQueue/WorldText).
        /// Стандартный материал шрифта Unity рисуется поверх всего — надписи просвечивали сквозь предметы.
        /// </summary>
        public static Material DepthTested
        {
            get
            {
                if (depthTested != null) return depthTested;
                var shader = Shader.Find("GasQueue/WorldText");
                if (shader == null) return Default.material; // на всякий случай — хоть как-то, но видно
                depthTested = new Material(shader) { name = "WorldTextDepth", mainTexture = Default.material.mainTexture };
                // Динамический шрифт иногда пересобирает текстуру с буквами — подхватываем новую
                Font.textureRebuilt += f =>
                {
                    if (f == Default && depthTested != null) depthTested.mainTexture = f.material.mainTexture;
                };
                return depthTested;
            }
        }

        /// <summary>
        /// Текст в 3D-мире. Вывески и табло прячутся за предметами; реплики над машинами (onTop)
        /// видны всегда, чтобы их можно было прочитать.
        /// </summary>
        public static TextMesh WorldText(Transform parent, Vector3 localPos, string text, Color color, float size = 0.05f,
            TextAnchor anchor = TextAnchor.MiddleCenter, bool onTop = false)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Default;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            go.GetComponent<MeshRenderer>().sharedMaterial = onTop ? Default.material : DepthTested;
            return tm;
        }
    }
}

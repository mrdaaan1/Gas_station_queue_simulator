using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Спортивные колёса: шина с округлыми боковинами, полированная закраина обода, спицы,
    /// колпачок, гайки, тормозной диск за спицами и суппорт (суппорт не крутится вместе с колесом).
    /// Ось колеса — Y узла, лицевая сторона диска — +Y.
    /// </summary>
    public static class WheelModel
    {
        /// <summary>Пятиспицевый литой диск 17" как у «Супры» и шина.</summary>
        public static void FiveSpoke(ModelNode node, float tireR, float width, int spokes = 5, float rimR = 0.216f,
            string spokeMatName = "alloy", string lipMat = "chrome", float spokeWidth = 1f)
        {
            var f = Frame.Identity;
            float sc = rimR / 0.216f; // размеры ступицы, спиц и тормоза — пропорционально диску
            float hw = width / 2f;
            // Шина
            var tire = node.M("rubber");
            float sw = tireR - rimR;
            Geo.Lathe(tire, f, new[]
            {
                new Vector2(rimR + 0.006f, -hw + 0.017f), new Vector2(rimR + sw * 0.4f, -hw + 0.005f), new Vector2(rimR + sw * 0.75f, -hw),
                new Vector2(tireR - 0.009f, -hw + 0.005f), new Vector2(tireR - 0.002f, -hw + 0.017f), new Vector2(tireR, -hw + 0.032f),
                new Vector2(tireR, hw - 0.032f), new Vector2(tireR - 0.002f, hw - 0.017f), new Vector2(tireR - 0.009f, hw - 0.005f),
                new Vector2(rimR + sw * 0.75f, hw), new Vector2(rimR + sw * 0.4f, hw - 0.005f), new Vector2(rimR + 0.006f, hw - 0.017f),
            }, 40);
            // Канавки протектора — тёмные кольца чуть утоплены
            foreach (float h in new[] { -0.045f, 0f, 0.045f })
                Geo.Lathe(node.M("tread"), f, new[] { new Vector2(tireR + 0.0006f, h - 0.006f), new Vector2(tireR + 0.0006f, h + 0.006f) }, 40, false);

            // Обод: полированная закраина и внутренняя «бочка», видная между спиц
            float face = hw - 0.012f;
            Geo.Lathe(node.M(lipMat), f, new[]
            {
                new Vector2(rimR - 0.016f, face - 0.012f), new Vector2(rimR - 0.004f, face - 0.002f), new Vector2(rimR + 0.006f, face + 0.002f),
                new Vector2(rimR + 0.010f, face + 0.007f), new Vector2(rimR + 0.006f, face + 0.012f), new Vector2(rimR - 0.006f, face + 0.012f),
                new Vector2(rimR - 0.016f, face + 0.004f),
            }, 40);
            Geo.Lathe(node.M("rim_inner"), f, new[] { new Vector2(rimR - 0.016f, face - 0.012f), new Vector2(rimR - 0.016f, -hw + 0.01f) }, 36, false, true);
            Geo.Lathe(node.M("rim_inner"), f, new[] { new Vector2(rimR - 0.016f, -hw + 0.01f), new Vector2(0.09f * sc, -hw + 0.03f) }, 36, false, true);

            // Спицы: от ступицы к ободу расширяются, лицо слегка вогнуто (ступица утоплена)
            var spokeMat = node.M(spokeMatName);
            for (int k = 0; k < spokes; k++)
            {
                float ang = k / (float)spokes * Mathf.PI * 2f + Mathf.PI / 2f;
                var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                var perp = new Vector3(-dir.z, 0f, dir.x);
                Vector3 P(float s, float c)
                {
                    float r = Mathf.Lerp(0.05f * sc, rimR - 0.008f, s);
                    float half = (Mathf.Lerp(0.024f, 0.034f, s) + 0.02f * Mathf.Pow(s, 6f)) * sc * spokeWidth;
                    float top = Mathf.Lerp(face - 0.006f, face + 0.006f, s) - 0.006f * Mathf.Sin(s * Mathf.PI);
                    const float thick = 0.034f;
                    float phi = c * Mathf.PI * 2f;
                    float cs = Mathf.Cos(phi), sn = Mathf.Sin(phi);
                    float a = Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), 0.45f) * half;
                    float d = Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.6f) * thick / 2f;
                    return dir * r + perp * a + Vector3.up * (top - thick / 2f + d);
                }
                Geo.Surface(spokeMat, f, 14, 20, P, false);
            }

            // Ступица с колпачком и гайками
            Geo.Lathe(node.M(spokeMatName), f, new[]
            {
                new Vector2(0.075f * sc, face - 0.03f), new Vector2(0.075f * sc, face - 0.005f), new Vector2(0.066f * sc, face + 0.002f),
                new Vector2(0.05f * sc, face + 0.004f), new Vector2(0.034f, face + 0.004f),
            }, 32);
            Geo.Lathe(node.M("chrome"), f, new[]
            {
                new Vector2(0.034f, face + 0.004f), new Vector2(0.031f, face + 0.009f), new Vector2(0.02f, face + 0.012f), new Vector2(0f, face + 0.013f),
            }, 28);
            for (int k = 0; k < 5; k++)
            {
                float ang = (k + 0.5f) / 5f * Mathf.PI * 2f + Mathf.PI / 2f;
                var c = new Vector3(Mathf.Cos(ang) * 0.046f * sc, 0f, Mathf.Sin(ang) * 0.046f * sc);
                var lf = new Frame { o = c, x = Vector3.right, y = Vector3.up, z = Vector3.forward };
                Geo.Cylinder(node.M("chrome"), lf, 0.0075f, face - 0.004f, face + 0.009f, 6);
            }

            // Тормозной диск с вентиляцией и «колокол»
            Geo.Lathe(node.M("disc"), f, new[]
            {
                new Vector2(0.09f * sc, -0.002f), new Vector2(0.165f * sc, -0.002f), new Vector2(0.165f * sc, -0.03f), new Vector2(0.09f * sc, -0.03f),
            }, 36, false);
            Geo.Lathe(node.M("rim_inner"), f, new[] { new Vector2(0.09f * sc, -0.002f), new Vector2(0.09f * sc, 0.03f), new Vector2(0.05f * sc, 0.03f) }, 24, false);
        }

        /// <summary>Суппорт — висит на неподвижном узле колеса (поворотный кулак), сзади вверху.</summary>
        public static void Caliper(ModelNode mount, float side, float scale = 1f)
        {
            var m = mount.M("caliper");
            float a = 150f * Mathf.Deg2Rad;
            var pos = new Vector3(side * 0.006f, Mathf.Sin(a) * 0.15f * scale, Mathf.Cos(a) * 0.15f * scale);
            var tangent = new Vector3(0f, Mathf.Cos(a), -Mathf.Sin(a));
            var f = Frame.Look(pos, tangent, new Vector3(side, 0f, 0f));
            Geo.RoundBox(m, f, Vector3.zero, new Vector3(0.075f, 0.05f, 0.14f) * scale, 0.35f, 8);
        }
    }
}

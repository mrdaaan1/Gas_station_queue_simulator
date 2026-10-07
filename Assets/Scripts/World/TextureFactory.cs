using UnityEngine;

namespace GasQueue
{
    /// <summary>Текстуры, нарисованные кодом: разметка дороги, плитка, фасады с окнами.</summary>
    public static class TextureFactory
    {
        static Texture2D road, sidewalk, asphalt;

        static Texture2D New(int w, int h, string name)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
            };
        }

        static Color32 Noise(Color baseColor, System.Random rnd, float amount)
        {
            float n = ((float)rnd.NextDouble() - 0.5f) * amount;
            return new Color(baseColor.r + n, baseColor.g + n, baseColor.b + n, 1f);
        }

        /// <summary>
        /// Дорога на 6 полос (21 м поперёк, 12 м вдоль на одну текстуру):
        /// двойная сплошная по центру, прерывистые между полосами, сплошные по краям.
        /// </summary>
        public static Texture2D Road
        {
            get
            {
                if (road != null) return road;
                const int W = 512, H = 128;
                const float metersX = 21f, metersZ = 12f;
                road = New(W, H, "Road");
                var px = new Color32[W * H];
                var rnd = new System.Random(5);
                var baseColor = Shapes.Hex("#3d3f42");
                var paint = new Color32(232, 232, 225, 255);
                for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float mx = x / (float)W * metersX - metersX / 2f; // −10.5 … 10.5
                    float mz = y / (float)H * metersZ;
                    var c = Noise(baseColor, rnd, 0.05f);
                    float ax = Mathf.Abs(mx);
                    bool line =
                        (ax > 0.06f && ax < 0.2f) ||                                // двойная сплошная
                        (ax > 10.05f && ax < 10.2f) ||                              // край дороги
                        ((Mathf.Abs(ax - 3.5f) < 0.07f || Mathf.Abs(ax - 7f) < 0.07f) && mz < 3f); // прерывистые
                    px[y * W + x] = line ? paint : c;
                }
                road.SetPixels32(px);
                road.Apply(true);
                return road;
            }
        }

        /// <summary>Тротуарная плитка 1×1 м (текстура на 2×2 м).</summary>
        public static Texture2D Sidewalk
        {
            get
            {
                if (sidewalk != null) return sidewalk;
                const int S = 128;
                sidewalk = New(S, S, "Sidewalk");
                var px = new Color32[S * S];
                var rnd = new System.Random(8);
                var tile = Shapes.Hex("#9c9a94");
                var seam = Shapes.Hex("#6f6d68");
                for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    bool isSeam = x % 64 < 2 || y % 64 < 2;
                    px[y * S + x] = isSeam ? (Color32)seam : Noise(tile, rnd, 0.06f);
                }
                sidewalk.SetPixels32(px);
                sidewalk.Apply(true);
                return sidewalk;
            }
        }

        /// <summary>Асфальт заправки — светлее и ровнее дорожного (текстура на 4×4 м).</summary>
        public static Texture2D Asphalt
        {
            get
            {
                if (asphalt != null) return asphalt;
                const int S = 64;
                asphalt = New(S, S, "Asphalt");
                var px = new Color32[S * S];
                var rnd = new System.Random(9);
                var c = Shapes.Hex("#55575a");
                for (int i = 0; i < px.Length; i++) px[i] = Noise(c, rnd, 0.07f);
                asphalt.SetPixels32(px);
                asphalt.Apply(true);
                return asphalt;
            }
        }

        static Texture2D goldCamo;

        /// <summary>
        /// Золотой хромированный камуфляж: крупные угловатые пятна четырёх оттенков (от светлого золота до тёмно-коричневого),
        /// бесшовная (ячейки Вороного с заворотом по краям), чуть «блестит» полосками отражений.
        /// </summary>
        public static Texture2D GoldCamo
        {
            get
            {
                if (goldCamo != null) return goldCamo;
                const int S = 256, N = 16;
                goldCamo = New(S, S, "GoldCamo");
                var rnd = new System.Random(70);
                var pts = new Vector2[N];
                var tone = new int[N];
                for (int i = 0; i < N; i++)
                {
                    pts[i] = new Vector2((float)rnd.NextDouble() * S, (float)rnd.NextDouble() * S);
                    double r = rnd.NextDouble();
                    tone[i] = r < 0.38 ? 0 : r < 0.70 ? 1 : r < 0.90 ? 2 : 3;
                }
                var palette = new[] { Shapes.Hex("#ffd440"), Shapes.Hex("#eea810"), Shapes.Hex("#b87008"), Shapes.Hex("#683c06") };
                var px = new Color32[S * S];
                for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float best = float.MaxValue;
                    int bi = 0;
                    for (int i = 0; i < N; i++)
                    {
                        float dx = Mathf.Abs(x - pts[i].x), dy = Mathf.Abs(y - pts[i].y);
                        if (dx > S / 2f) dx = S - dx;
                        if (dy > S / 2f) dy = S - dy;
                        // Немного растянутые по диагонали ячейки — пятна «рваные», как у армейского камуфляжа
                        float d = dx * dx + dy * dy + 0.6f * dx * dy;
                        if (d < best) { best = d; bi = i; }
                    }
                    var c = palette[tone[bi]];
                    // Блик хрома: пологие диагональные полосы света и тени поверх пятен
                    float sheen = Mathf.Sin((x + y * 0.5f) / S * Mathf.PI * 4f) * 0.04f;
                    px[y * S + x] = new Color(Mathf.Clamp01(c.r + sheen), Mathf.Clamp01(c.g + sheen), Mathf.Clamp01(c.b + sheen * 0.8f), 1f);
                }
                goldCamo.SetPixels32(px);
                goldCamo.Apply(true);
                return goldCamo;
            }
        }

        /// <summary>Фасад панельки: одна «ячейка» 3×3 м — стена и окно с рамой. Иногда окно светится или с балконом.</summary>
        public static Texture2D Facade(Color wall, Color glass, int seed)
        {
            const int S = 64, cells = 4; // 4×4 окна в одной текстуре, чтобы окна были разными
            var tex = New(S * cells, S * cells, "Facade");
            var px = new Color32[S * cells * S * cells];
            var rnd = new System.Random(seed);
            var frame = Shapes.Hex("#e8e4dc");
            var seam = wall * 0.85f;
            for (int cy = 0; cy < cells; cy++)
            for (int cx = 0; cx < cells; cx++)
            {
                double r = rnd.NextDouble();
                var win = r < 0.15 ? Shapes.Hex("#e9d79a") : r < 0.3 ? glass * 0.7f : glass;
                bool balcony = rnd.NextDouble() < 0.25;
                for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    Color c;
                    bool panelSeam = x < 1 || y < 1;
                    bool inWindow = x >= 16 && x < 48 && y >= 22 && y < 52;
                    bool inFrame = x >= 14 && x < 50 && y >= 20 && y < 54;
                    if (panelSeam) c = seam;
                    else if (inWindow) c = (x == 32 || y == 40) ? frame : win;
                    else if (inFrame) c = frame;
                    else if (balcony && y >= 8 && y < 22 && x >= 8 && x < 56) c = wall * 0.75f;
                    else c = wall;
                    px[(cy * S + y) * S * cells + cx * S + x] = Noise(c, rnd, 0.025f);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }
    }
}

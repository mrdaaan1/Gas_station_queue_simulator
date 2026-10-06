using System;
using System.Diagnostics;
using System.IO;
using Path = GasQueue.Intro.Path;
using IOPath = System.IO.Path;
using System.IO.Compression;
using GasQueue.Intro;

// Превью заставки: каждый кадр коллажа отдельно и кадры анимации в заданные моменты.
//   dotnet run -- <папка> [ширина] [высота] [время1 время2 ...]
static class Program
{
    static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : "intro_out";
        int w = args.Length > 1 ? int.Parse(args[1]) : 1920, h = args.Length > 2 ? int.Parse(args[2]) : 1080;
        Directory.CreateDirectory(dir);
        var sw = Stopwatch.StartNew();
        var art = IntroArt.Build(w, h, 1f);
        Console.WriteLine($"нарисовано за {sw.ElapsedMilliseconds} мс");
        if (Environment.GetEnvironmentVariable("INTRO_PROFILE") == "1")
        {
            var L = IntroLayout.Compute(w, h);
            for (int i = 0; i < IntroLayout.PanelCount; i++)
            {
                sw.Restart();
                IntroArt.Panel(i, (int)L.Panels[i].w, (int)L.Panels[i].h);
                Console.WriteLine($"  кадр {i}: {sw.ElapsedMilliseconds} мс");
            }
            sw.Restart();
            IntroArt.BuildLogo(new IntroArt { Images = new Raster[IntroLayout.TexCount] }, L, 1f);
            Console.WriteLine($"  логотип: {sw.ElapsedMilliseconds} мс");
        }
        for (int i = 0; i < art.Images.Length; i++) Png.Save(IOPath.Combine(dir, $"tex{i:00}.png"), art.Images[i]);
        for (int k = 3; k < args.Length; k++)
        {
            float t = float.Parse(args[k], System.Globalization.CultureInfo.InvariantCulture);
            var frame = Compose(art, w, h, t);
            Png.Save(IOPath.Combine(dir, $"frame_{t:0.00}.png".Replace(',', '.')), frame);
        }
    }

    /// <summary>То же, что делает IntroSplash.OnGUI в Unity, только в картинку.</summary>
    static Raster Compose(IntroArt art, int w, int h, float t)
    {
        var dst = new Raster(w, h);
        foreach (var s in IntroLayout.Frame(art.Layout, w, h, t))
        {
            Raster src = s.Tex >= 0 ? art.Images[s.Tex] : null;
            int x0 = Math.Max(0, (int)Math.Floor(s.X)), y0 = Math.Max(0, (int)Math.Floor(s.Y));
            int x1 = Math.Min(w, (int)Math.Ceiling(s.X + s.W)), y1 = Math.Min(h, (int)Math.Ceiling(s.Y + s.H));
            if (s.Clip)
            {
                x0 = Math.Max(x0, (int)s.ClipX); y0 = Math.Max(y0, (int)s.ClipY);
                x1 = Math.Min(x1, (int)(s.ClipX + s.ClipW)); y1 = Math.Min(y1, (int)(s.ClipY + s.ClipH));
            }
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    float r = s.R, g = s.G, b = s.B, a = s.Alpha;
                    if (src != null)
                    {
                        float u = s.U + (x + 0.5f - s.X) / s.W * s.UW, v = s.V + (y + 0.5f - s.Y) / s.H * s.VH;
                        Sample(src, u, v, out float sr, out float sg, out float sb, out float sa);
                        // premultiplied → обычный цвет
                        if (sa <= 1e-5f) continue;
                        r *= sr / sa; g *= sg / sa; b *= sb / sa; a *= sa;
                    }
                    int i = (y * w + x) * 4;
                    float k = 1f - a;
                    dst.Px[i] = r * a + dst.Px[i] * k;
                    dst.Px[i + 1] = g * a + dst.Px[i + 1] * k;
                    dst.Px[i + 2] = b * a + dst.Px[i + 2] * k;
                    dst.Px[i + 3] = a + dst.Px[i + 3] * k;
                }
        }
        return dst;
    }

    static void Sample(Raster r, float u, float v, out float cr, out float cg, out float cb, out float ca)
    {
        float fx = u * r.Width - 0.5f, fy = v * r.Height - 0.5f;
        int x = (int)Math.Floor(fx), y = (int)Math.Floor(fy);
        float tx = fx - x, ty = fy - y;
        cr = cg = cb = ca = 0f;
        for (int j = 0; j < 2; j++)
            for (int i = 0; i < 2; i++)
            {
                int px = Math.Clamp(x + i, 0, r.Width - 1), py = Math.Clamp(y + j, 0, r.Height - 1);
                float wgt = (i == 0 ? 1 - tx : tx) * (j == 0 ? 1 - ty : ty);
                int o = (py * r.Width + px) * 4;
                cr += r.Px[o] * wgt; cg += r.Px[o + 1] * wgt; cb += r.Px[o + 2] * wgt; ca += r.Px[o + 3] * wgt;
            }
    }
}

static class Png
{
    public static void Save(string path, Raster r)
    {
        var rgba = r.ToRgba32(false);
        using var raw = new MemoryStream();
        for (int y = 0; y < r.Height; y++)
        {
            raw.WriteByte(0);
            raw.Write(rgba, y * r.Width * 4, r.Width * 4);
        }
        using var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.Fastest, true)) { raw.Position = 0; raw.CopyTo(zs); }
        using var f = File.Create(path);
        f.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var hdr = new byte[13];
        BE(hdr, 0, r.Width); BE(hdr, 4, r.Height); hdr[8] = 8; hdr[9] = 6;
        Chunk(f, "IHDR", hdr);
        Chunk(f, "IDAT", z.ToArray());
        Chunk(f, "IEND", Array.Empty<byte>());
    }

    static void BE(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, data.Length); s.Write(len);
        var td = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) td[i] = (byte)type[i];
        Array.Copy(data, 0, td, 4, data.Length);
        s.Write(td);
        var crc = new byte[4]; BE(crc, 0, (int)Crc(td)); s.Write(crc);
    }

    static uint Crc(byte[] d)
    {
        uint c = 0xffffffff;
        foreach (byte b in d)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xedb88320 ^ (c >> 1) : c >> 1;
        }
        return c ^ 0xffffffff;
    }
}

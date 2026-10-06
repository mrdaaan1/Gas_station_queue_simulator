#!/usr/bin/env python3
"""
Достаёт контуры букв из шрифта Rubik (вес 900, лицензия SIL OFL 1.1) и пишет их в
Assets/Scripts/UI/Intro/IntroGlyphs.cs — заставка рисует надписи сама, без файла шрифта.

    pip install fonttools
    curl -LO "https://raw.githubusercontent.com/google/fonts/main/ofl/rubik/Rubik%5Bwght%5D.ttf"
    python3 Tools/IntroArt/make_glyphs.py "Rubik[wght].ttf"
"""
import sys, os
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools.pens.recordingPen import DecomposingRecordingPen

CHARS = (" 0123456789-.,:!?«»×%/№"
         "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ"
         "абвгдеёжзийклмнопрстуфхцчшщъыьэюя"
         "ABCDEFGHIJKLMNOPQRSTUVWXYZ")

def flatten(rec, upm):
    contours, cur, start = [], [], None
    def pt(p): return (p[0] / upm, p[1] / upm)
    last = None
    for op, args in rec.value:
        if op == "moveTo":
            cur = [pt(args[0])]; last = args[0]
        elif op == "lineTo":
            cur.append(pt(args[0])); last = args[0]
        elif op == "qCurveTo":
            # TrueType: цепочка off-curve точек, последняя — on-curve (или None — замкнутый контур без on-точек)
            pts = list(args)
            if pts[-1] is None:
                pts = pts[:-1]
                last = ((pts[-1][0] + pts[0][0]) / 2, (pts[-1][1] + pts[0][1]) / 2)
                cur = [pt(last)]
                pts = pts + [last]
            p0 = last
            for i in range(len(pts) - 1):
                c = pts[i]
                p1 = pts[i + 1] if i == len(pts) - 2 else ((pts[i][0] + pts[i + 1][0]) / 2, (pts[i][1] + pts[i + 1][1]) / 2)
                n = 6
                for k in range(1, n + 1):
                    t = k / n
                    x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * c[0] + t * t * p1[0]
                    y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * c[1] + t * t * p1[1]
                    cur.append(pt((x, y)))
                p0 = p1
            last = p0
        elif op == "curveTo":
            c1, c2, p1 = args
            p0 = last
            n = 8
            for k in range(1, n + 1):
                t = k / n
                x = (1-t)**3*p0[0] + 3*(1-t)**2*t*c1[0] + 3*(1-t)*t*t*c2[0] + t**3*p1[0]
                y = (1-t)**3*p0[1] + 3*(1-t)**2*t*c1[1] + 3*(1-t)*t*t*c2[1] + t**3*p1[1]
                cur.append(pt((x, y)))
            last = p1
        elif op in ("closePath", "endPath"):
            if len(cur) > 2:
                if cur[0] == cur[-1]: cur.pop()
                contours.append(cur)
            cur = []
    return contours

def main():
    font = TTFont(sys.argv[1])
    if "fvar" in font:
        font = instantiateVariableFont(font, {"wght": 900})
    upm = font["head"].unitsPerEm
    cmap = font.getBestCmap()
    gs = font.getGlyphSet()
    hmtx = font["hmtx"]
    out = []
    for ch in CHARS:
        name = cmap.get(ord(ch))
        if name is None:
            print("нет буквы", ch); continue
        rec = DecomposingRecordingPen(gs); gs[name].draw(rec)
        adv = hmtx[name][0] / upm
        cs = flatten(rec, upm)
        body = ", ".join("new float[] {" + ",".join(f"{x:.3f}f,{y:.3f}f" for x, y in c) + "}" for c in cs)
        esc = ch.replace("\\", "\\\\").replace("'", "\\'")
        out.append(f"            {{ '{esc}', new Glyph({adv:.3f}f, new float[][] {{ {body} }}) }},")
    path = os.path.join(os.path.dirname(__file__), "../../Assets/Scripts/UI/Intro/IntroGlyphs.cs")
    with open(path, "w", encoding="utf-8") as f:
        f.write("""// Сгенерировано Tools/IntroArt/make_glyphs.py — не править руками.
// Контуры букв шрифта Rubik Black (c) The Rubik Project Authors, SIL Open Font License 1.1.
using System.Collections.Generic;

namespace GasQueue.Intro
{
    public sealed class Glyph
    {
        /// <summary>Ширина буквы и контуры в долях кегля, Y вверх от базовой линии.</summary>
        public readonly float Advance;
        public readonly float[][] Contours;
        public Glyph(float advance, float[][] contours) { Advance = advance; Contours = contours; }
    }

    public static class IntroGlyphs
    {
        public static readonly Dictionary<char, Glyph> Rubik = new Dictionary<char, Glyph>
        {
""" + "\n".join(out) + """
        };
    }
}
""")
    print("ok", len(out), "букв")

main()

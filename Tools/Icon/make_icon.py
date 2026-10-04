"""Иконка приложения: колонка «ЛУКАВОЙЛ» с табличкой «БЕНЗИНА НЕТ» и очередь машин.
Запуск: python3 Tools/Icon/make_icon.py  (нужен Pillow). Результат: Assets/Icons/AppIcon.png и Tools/Icon/AppIcon.icns"""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

S = 1024
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FONT = next((f for f in ["/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                         "/System/Library/Fonts/Supplemental/Arial Bold.ttf"] if os.path.exists(f)), None)

def font(size):
    return ImageFont.truetype(FONT, size) if FONT else ImageFont.load_default()

def hexc(h, a=255):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (a,)

art = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(art)

# Небо и дорога
horizon = 600
for y in range(S):
    t = y / horizon
    if y < horizon:
        c = tuple(int(a + (b - a) * t) for a, b in zip(hexc("#4f8fd1")[:3], hexc("#cfe6f5")[:3]))
    else:
        c = hexc("#3a3d44")[:3]
    d.line([(0, y), (S, y)], fill=c + (255,))
# Дома на горизонте
for x, w, h, col in [(60, 150, 210, "#e9dcc0"), (200, 120, 300, "#d5d9de"), (310, 170, 240, "#efe3c8"), (470, 120, 330, "#cfd4da")]:
    d.rectangle([x, horizon - h, x + w, horizon], fill=hexc(col))
    for wy in range(horizon - h + 25, horizon - 20, 42):
        for wx in range(x + 18, x + w - 20, 34):
            d.rectangle([wx, wy, wx + 16, wy + 20], fill=hexc("#7a95b0"))
# Тротуар и разметка
d.rectangle([0, horizon, S, horizon + 26], fill=hexc("#9aa0a6"))
for x in range(-40, S, 150):
    d.rectangle([x, 905, x + 80, 920], fill=hexc("#f2f2f2"))

# Колонка
px0, px1, py0, py1 = 610, 860, 300, 860
d.rounded_rectangle([px0, py0, px1, py1], 26, fill=hexc("#d32f2f"))
d.rounded_rectangle([px0 + 30, py0 + 120, px1 - 30, py0 + 300], 16, fill=hexc("#f4f4f4"))
d.rectangle([px0 + 55, py0 + 150, px1 - 55, py0 + 225], fill=hexc("#16181c"))
d.text(((px0 + px1) / 2, py0 + 188), "0,00", font=font(58), fill=hexc("#ffd23f"), anchor="mm")
d.text(((px0 + px1) / 2, py0 + 262), "АИ-95", font=font(40), fill=hexc("#d32f2f"), anchor="mm")
# Шланг и пистолет
d.arc([px0 - 150, py0 + 300, px0 + 60, py0 + 560], 90, 270, fill=hexc("#111111"), width=26)
d.rounded_rectangle([px0 - 70, py0 + 300, px0 + 8, py0 + 345], 10, fill=hexc("#222222"))
d.rectangle([px0 - 8, py0 + 310, px0 + 20, py0 + 335], fill=hexc("#222222"))

# Табличка «БЕНЗИНА НЕТ» над колонкой
sign = Image.new("RGBA", (460, 170), (0, 0, 0, 0))
sd = ImageDraw.Draw(sign)
sd.rounded_rectangle([0, 0, 459, 169], 22, fill=hexc("#ffffff"), outline=hexc("#d32f2f"), width=14)
sd.text((230, 60), "БЕНЗИНА", font=font(64), fill=hexc("#d32f2f"), anchor="mm")
sd.text((230, 125), "НЕТ", font=font(66), fill=hexc("#16181c"), anchor="mm")
sign = sign.rotate(-8, resample=Image.BICUBIC, expand=True)
art.alpha_composite(sign, (470, 120))

# Очередь машин (вид сбоку) — едут к колонке
def car(x, y, w, body, cabin=None, taxi=False):
    h = int(w * 0.25)
    cabin = cabin or body
    # кабина
    d.polygon([(x + w * 0.24, y), (x + w * 0.34, y - h * 0.95), (x + w * 0.70, y - h * 0.95), (x + w * 0.80, y)], fill=hexc(cabin))
    d.polygon([(x + w * 0.30, y - 6), (x + w * 0.37, y - h * 0.82), (x + w * 0.50, y - h * 0.82), (x + w * 0.50, y - 6)], fill=hexc("#9cc3dc"))
    d.polygon([(x + w * 0.53, y - 6), (x + w * 0.53, y - h * 0.82), (x + w * 0.67, y - h * 0.82), (x + w * 0.74, y - 6)], fill=hexc("#9cc3dc"))
    d.rounded_rectangle([x, y - 4, x + w, y + h], 14, fill=hexc(body))
    d.rectangle([x + w - 16, y + 8, x + w, y + 26], fill=hexc("#ffe680"))
    d.rectangle([x, y + 8, x + 14, y + 26], fill=hexc("#c62828"))
    if taxi:
        d.rectangle([x + w * 0.45, y - h * 0.95 - 22, x + w * 0.59, y - h * 0.95], fill=hexc("#ffd23f"))
        for i in range(4):
            d.rectangle([x + w * 0.3 + i * 30, y + h * 0.45, x + w * 0.3 + i * 30 + 15, y + h * 0.6], fill=hexc("#16181c"))
    r = int(w * 0.11)
    for cx in (x + w * 0.2, x + w * 0.8):
        d.ellipse([cx - r, y + h - r, cx + r, y + h + r], fill=hexc("#1b1b1b"))
        d.ellipse([cx - r * 0.45, y + h - r * 0.45, cx + r * 0.45, y + h + r * 0.45], fill=hexc("#bdbdbd"))

car(-150, 735, 330, "#2f6db5")
car(160, 725, 330, "#ffcc33", taxi=True)
car(420, 760, 300, "#ece7da")

# Скругление в стиле иконок macOS: «сквиркл» с полями и мягкой тенью
margin, radius = 100, 185
mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle([margin, margin, S - margin, S - margin], radius, fill=255)
art_crop = Image.new("RGBA", (S, S), (0, 0, 0, 0))
inner = art.resize((S - 2 * margin, S - 2 * margin), Image.LANCZOS)
art_crop.paste(inner, (margin, margin))
art_crop.putalpha(Image.composite(art_crop.getchannel("A"), Image.new("L", (S, S), 0), mask))

shadow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
ImageDraw.Draw(shadow).rounded_rectangle([margin, margin + 14, S - margin, S - margin + 14], radius, fill=(0, 0, 0, 110))
shadow = shadow.filter(ImageFilter.GaussianBlur(18))
icon = Image.alpha_composite(shadow, art_crop)

os.makedirs(os.path.join(ROOT, "Assets", "Icons"), exist_ok=True)
icon.save(os.path.join(ROOT, "Assets", "Icons", "AppIcon.png"))
icon.save(os.path.join(ROOT, "Tools", "Icon", "AppIcon.icns"))
print("ok")

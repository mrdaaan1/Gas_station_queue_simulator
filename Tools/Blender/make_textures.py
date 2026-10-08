# Текстуры Мэтра: ржавчина (коричневая), синяя краска с ржавыми пятнами, зелёная панель двери, глаз.
# Бесшовные (шум из периодических волн), 512 px. Кладутся в Assets/Resources/Textures — их берёт и игра, и Blender.
# Запуск: Blender --background --factory-startup --python make_textures.py -- <папка>
import bpy, math, os, sys
import numpy as np

out = sys.argv[sys.argv.index("--") + 1]
os.makedirs(out, exist_ok=True)
S = 512
rng = np.random.default_rng(113)
yy, xx = np.mgrid[0:S, 0:S] / S

def noise(octaves=6, base=2, gain=0.55, seed=0):
    """Бесшовный шум: сумма волн с целыми частотами (плитка без швов)."""
    r = np.random.default_rng(seed)
    acc = np.zeros((S, S))
    amp, total = 1.0, 0.0
    f = base
    for _ in range(octaves):
        for _ in range(6):
            kx, ky = r.integers(-f, f + 1), r.integers(-f, f + 1)
            if kx == 0 and ky == 0:
                kx = f
            ph = r.random() * 2 * math.pi
            acc += amp * np.sin(2 * math.pi * (kx * xx + ky * yy) + ph)
            total += amp
        amp *= gain
        f *= 2
    acc /= total
    return (acc - acc.min()) / (acc.max() - acc.min())

def hexc(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])

def mix(a, b, t):
    t = np.clip(t, 0, 1)[..., None]
    return a * (1 - t) + b * t

def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)

grain = noise(3, 64, 0.6, 7)
n1, n2, n3 = noise(6, 2, 0.55, 1), noise(6, 3, 0.6, 2), noise(5, 4, 0.5, 3)

def rust(base_seed=0):
    """Ржавчина: коричневая основа, оранжевые пятна, тёмные раковины, мелкое зерно."""
    c = mix(hexc("#4e2412"), hexc("#73361a"), n1)
    c = mix(c, hexc("#9a4f24"), smooth(0.6, 0.85, n2) * 0.8)
    c = mix(c, hexc("#2a130a"), smooth(0.62, 0.9, n3) * 0.85)
    c *= (0.86 + 0.28 * grain)[..., None]
    return c

def paint(base, light, cover):
    """Старая краска с ржавыми проплешинами; cover — доля краски (0..1)."""
    c = mix(hexc(base), hexc(light), smooth(0.3, 0.9, n3) * 0.6)
    c *= (0.92 + 0.16 * grain)[..., None]
    patch = noise(6, 2, 0.6, 11)
    edge = smooth(cover - 0.06, cover + 0.02, patch)            # где ржавчина проела краску
    halo = smooth(cover - 0.16, cover - 0.04, patch) * (1 - edge)  # подтёки вокруг пятна
    c = mix(c, rust(), edge)
    c = mix(c, c * hexc("#c08a60") * 1.1, halo * 0.6)
    return c

def save(name, rgb):
    img = bpy.data.images.new(name, S, S, alpha=False)
    px = np.ones((S, S, 4))
    px[..., :3] = np.clip(rgb, 0, 1)
    img.pixels = px.ravel()
    img.filepath_raw = os.path.join(out, name + ".png")
    img.file_format = 'PNG'
    img.save()
    print("SAVED", img.filepath_raw)

save("mater_rust", rust())
save("mater_blue", paint("#62808f", "#8aa1ad", 0.70))
save("mater_green", paint("#7cab70", "#a3c891", 0.76))

# Задний бампер: жёлто-чёрные косые полосы, облезлые и ржавые
band = ((xx + yy) * 6) % 1.0
stripes = mix(hexc("#d9b51c"), hexc("#161310"), smooth(0.47, 0.53, band) * (1 - smooth(0.97, 1.0, band)))
stripes *= (0.9 + 0.2 * grain)[..., None]
wear = smooth(0.58, 0.66, noise(6, 3, 0.6, 21))
save("mater_stripes", mix(stripes, rust(), wear))

# Глаз: белок, зелёная радужка с тёмной каймой и лучами, зрачок, два блика. UV — квадрат 0..1 вокруг глаза.
cx, cy = 0.5, 0.5
dx, dy = xx - cx, yy - cy
r = np.sqrt(dx * dx + dy * dy)
ang = np.arctan2(dy, dx)
eye = np.ones((S, S, 3)) * hexc("#f4f1e6")
eye *= (1 - 0.12 * smooth(0.25, 0.5, r))[..., None]              # белок темнеет к краю
iris_r, pupil_r = 0.20, 0.085
fib = 0.5 + 0.5 * np.sin(ang * 46 + 3 * np.sin(ang * 7))
iris = mix(hexc("#2f6b25"), hexc("#8fbf3e"), smooth(pupil_r, iris_r, r) * (0.6 + 0.4 * fib))
iris = mix(iris, hexc("#c7a03a"), smooth(iris_r * 0.75, pupil_r, r) * 0.55)   # золотистое кольцо у зрачка
iris = mix(iris, hexc("#1d3a14"), smooth(iris_r * 0.85, iris_r, r))            # тёмная кайма
eye = mix(eye, iris, smooth(iris_r + 0.006, iris_r - 0.004, r))
eye = mix(eye, hexc("#070806"), smooth(pupil_r + 0.004, pupil_r - 0.004, r))
for hx, hy, hr in ((0.045, 0.05, 0.03), (-0.04, -0.045, 0.012)):
    hrr = np.sqrt((dx - hx) ** 2 + (dy - hy) ** 2)
    eye = mix(eye, np.ones(3), smooth(hr, hr * 0.5, hrr))
save("mater_eye", eye)

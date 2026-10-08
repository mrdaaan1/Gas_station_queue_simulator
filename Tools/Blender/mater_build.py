# Мэтр (эвакуатор из «Тачек») целиком в Blender и выгрузка в игру.
# Модель строится «клетками» под Subdivision (мягкие мультяшные формы) прямо в координатах Unity по узлам,
# имена узлов — как у остальных машин (Body, Hood, DoorFL…, SteerFL/WheelFL/RimFL…, SteeringWheel, GaugeSpeed…),
# поэтому игра собирает её тем же кодом (SportsCars.BuildMater). Ржавчина, краска, глаз и полосы — текстуры
# из Assets/Resources/Textures (готовит make_textures.py), UV — проекцией по положению (плитка ~1,7 м).
# Запуск:
#   Blender --background --factory-startup --python mater_build.py -- Mater.bytes mater.blend Assets/Resources/Textures
import bpy, bmesh, math, os, sys
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
out_bytes, out_blend, tex_dir = argv[0], argv[1], argv[2]
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gq_materials, gq_export
from gq_export import ub, unity_local
from gq_geom import mat_index, subd_into, cage, torus, sweep, lathe

gq_materials.tex_dir = os.path.abspath(tex_dir)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------- Размеры (м, Unity: x вправо, y вверх, z вперёд) ----------
AXLE_F, AXLE_R = 1.45, -1.25
TIRE_R = 0.42
TRACK_F, TRACK_R = 0.80, 0.90          # сзади — центр наружного колеса из пары
DUAL = 0.25                            # расстояние между колёсами сдвоенной пары
EYES = (-0.40, 1.62, -0.15)            # глаза водителя (вид из кабины)
STEER = (-0.40, 1.22, 0.10)
STEER_TILT = -45

# ---------- Узлы ----------
nodes = []

def node(name, parent=None, pos=(0, 0, 0), euler=(0, 0, 0)):
    me = bpy.data.meshes.new(name)
    ob = bpy.data.objects.new(name, me)
    scene.collection.objects.link(ob)
    ob["gq_index"] = len(nodes)
    ob["gq_name"] = name
    ob["gq_pos"] = list(pos)
    ob["gq_euler"] = list(euler)
    if parent is not None:
        ob.parent = parent
    ob.matrix_basis = unity_local(pos, euler)
    nodes.append(ob)
    return ob

def look_euler(d):
    """Углы Эйлера (как в Unity), поворачивающие +Z в направление d."""
    l = math.sqrt(d[0] ** 2 + d[1] ** 2 + d[2] ** 2)
    x, y, z = d[0] / l, d[1] / l, d[2] / l
    return (-math.degrees(math.asin(max(-1, min(1, y)))), math.degrees(math.atan2(x, z)), 0.0)

def sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])

def build(ob, fn, level=2):
    """Добавить в узел детали, построенные fn(bm, M); M(key) — номер материала узла."""
    me = ob.data
    def run(bm):
        fn(bm, lambda key: mat_index(me, key))
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)   # у тел вращения на оси — совпадающие вершины
    subd_into(me, run, level)

def facing(bm, faces, want):
    """Развернуть плоскую деталь лицом в сторону want (Unity)."""
    bm.normal_update()
    w = Vector(ub(want))
    for f in faces:
        if f.normal.dot(w) < 0:
            f.normal_flip()

def disc(bm, c, rx, ry, mat, bulge=0.0, power=2.0, rings=8, seg=48, normal=(0, 0, 1)):
    """Плоский скруглённый диск (суперэллипс) в плоскости XY Unity с выпуклостью bulge по +Z; лицом по normal."""
    center = bm.verts.new(ub((c[0], c[1], c[2] + bulge)))
    prev = None
    faces = []
    for i in range(1, rings + 1):
        t = i / rings
        ring = []
        for k in range(seg):
            a = 2 * math.pi * k / seg
            ca, sa = math.cos(a), math.sin(a)
            sx = math.copysign(abs(ca) ** (2 / power), ca)
            sy = math.copysign(abs(sa) ** (2 / power), sa)
            z = c[2] + bulge * (1 - t * t)
            ring.append(bm.verts.new(ub((c[0] + sx * rx * t, c[1] + sy * ry * t, z))))
        for k in range(seg):
            k2 = (k + 1) % seg
            if prev is None:
                faces.append(bm.faces.new((center, ring[k], ring[k2])))
            else:
                faces.append(bm.faces.new((prev[k], ring[k], ring[k2], prev[k2])))
        prev = ring
    for f in faces:
        f.material_index = mat
        f.tag = True
        f.smooth = True
    facing(bm, faces, normal)
    return faces

def loft(bm, pts, profile, mat, up=(0, 1, 0)):
    """Протяжка замкнутого сечения profile [(a, b)] вдоль точек pts: a — вбок, b — «вверх» (поперёк пути)."""
    n = len(pts)
    upv = Vector(up)
    rings = []
    for i, p in enumerate(pts):
        p = Vector(p)
        t = (Vector(pts[min(n - 1, i + 1)]) - Vector(pts[max(0, i - 1)])).normalized()
        side = t.cross(upv).normalized()
        u2 = side.cross(t).normalized()
        rings.append([bm.verts.new(ub(p + side * a + u2 * b)) for a, b in profile])
    m = len(profile)
    faces = []
    for i in range(n - 1):
        r0, r1 = rings[i], rings[i + 1]
        for j in range(m):
            faces.append(bm.faces.new((r0[j], r1[j], r1[(j + 1) % m], r0[(j + 1) % m])))
    faces.append(bm.faces.new(list(reversed(rings[0]))))
    faces.append(bm.faces.new(rings[-1]))
    for f in faces:
        f.material_index = mat
        f.tag = True
    bmesh.ops.recalc_face_normals(bm, faces=faces)

def crown(width, height, thick, n=7):
    """Сечение крыла: выпуклая «горбушка» шириной width, высотой height, толщиной thick."""
    top = [(-width / 2 + width * i / (n - 1), 0.0) for i in range(n)]
    top = [(a, height * (1 - (2 * a / width) ** 2)) for a, _ in top]
    bot = [(a * 0.96, b - thick) for a, b in reversed(top)]
    return top + bot

def rrect(w, h, n=2):
    """Скруглённый прямоугольник для протяжки (бампер, балки)."""
    return [(-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)]

def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)

# ---------- Дерево ----------
root = node("Root")
body = node("Body", root)
shell = node("Shell", body)

# ---------- Рама, мосты, подножки ----------
def chassis(bm, M):
    ch = M("chassis")
    for s in (-1, 1):
        cage(bm, (s * 0.42, 0.52, -0.05), (0.10, 0.14, 4.25), ch, cuts=1)
    cage(bm, (0, 0.44, AXLE_F), (1.30, 0.09, 0.10), ch, cuts=1)                       # передняя балка
    cage(bm, (0, 0.56, -1.95), (0.94, 0.08, 0.08), ch, cuts=1)
    cage(bm, (0, 0.60, 1.95), (0.94, 0.10, 0.10), ch, cuts=1)
    cage(bm, (0, 0.62, 1.0), (0.66, 0.40, 1.3), ch, cuts=1)                           # мотор снизу
build(shell, chassis, 1)

def axles(bm, M):
    ch = M("chassis")
    lathe(bm, [(0.065, -0.80), (0.065, 0.80)], ch, 16, (0, TIRE_R, AXLE_R), "x", inside=(0, 0))
    lathe(bm, [(0.0, -0.17), (0.16, -0.1), (0.17, 0.0), (0.16, 0.1), (0.0, 0.17)], ch, 24, (0, TIRE_R, AXLE_R), "z", inside=(0, 0))
    lathe(bm, [(0.15, -0.32), (0.15, 0.32)], M("rust_dark"), 24, (0.66, 0.56, -0.05), "z", inside=(0, 0))          # бак под кабиной
    lathe(bm, [(0.035, -1.2), (0.035, 0.5)], ch, 12, (-0.55, 0.36, -1.2), "z", inside=(0, 0))                      # выхлоп
build(shell, axles, 0)

def running_boards(bm, M):
    r = M("mater_rust")
    for s in (-1, 1):
        cage(bm, (s * 0.85, 0.585, -0.06), (0.26, 0.05, 0.95), r, cuts=1)
build(shell, running_boards, 1)

# ---------- Передние крылья ----------
FENDER_F = [(2.13, 0.64), (2.14, 0.78), (2.08, 0.92), (1.95, 1.01), (1.75, 1.06), (1.45, 1.09), (1.15, 1.06),
            (0.92, 0.96), (0.72, 0.80), (0.52, 0.66), (0.36, 0.60), (0.28, 0.59)]
def front_fenders(bm, M):
    for s in (-1, 1):
        pts = [(s * 0.75, y, z) for z, y in FENDER_F]
        loft(bm, pts, crown(0.56, 0.11, 0.04), M("mater_rust"))
        # внутренний щиток между крылом и капотом
        cage(bm, (s * 0.50, 0.86, 1.30), (0.06, 0.38, 1.55), M("rust_dark"), cuts=1)
build(shell, front_fenders, 2)

# ---------- Капот и «лицо»: верхняя губа, зубы, рот ----------
hood = node("Hood", body, (0, 1.36, 0.55))
HOOD_LOCAL = (0, -1.36, -0.55)
def L(p):  # мировые координаты Unity → локальные узла капота
    return (p[0] + HOOD_LOCAL[0], p[1] + HOOD_LOCAL[1], p[2] + HOOD_LOCAL[2])

def hood_shape(u, h, w, p):
    # верх капота чуть опускается к носу, нос скруглён; низ передней части выдвинут — «нависающая губа»
    if h > 0:
        p.y -= 0.07 * smoothstep(-0.2, 1.0, w)
        p.y += 0.025 * (1 - u * u)
    if w > 0.6 and h < 0:
        p.z += 0.04 * (-h)
    return p

def hood_parts(bm, M):
    r = M("mater_rust")
    cage(bm, L((0, 1.07, 1.36)), (0.96, 0.62, 1.62), r, cuts=3, shape=hood_shape)
    # верхняя губа — толстый валик над ртом
    lip = [(x, 0.865 + 0.02 * (abs(x) / 0.46) ** 2, 2.225 - 0.06 * (abs(x) / 0.46) ** 2) for x in [i / 10 * 0.92 - 0.46 for i in range(11)]]
    loft(bm, [L(p) for p in lip], rrect(0.11, 0.10), r)
    # решётка-«усы»: тёмные щели над губой
    for k, y in enumerate((0.97, 1.03, 1.09)):
        cage(bm, L((0, y, 2.205 - 0.01 * k)), (0.62 - 0.08 * k, 0.026, 0.03), M("rust_dark"), cuts=1)
    # нос — ровная пластина посередине
    cage(bm, L((0, 1.18, 2.17)), (0.30, 0.10, 0.04), r, cuts=1)
build(hood, hood_parts, 2)

def teeth(bm, M):
    t = M("teeth")
    for s in (-1, 1):
        cage(bm, L((s * 0.078, 0.745, 2.235)), (0.14, 0.21, 0.05), t, cuts=1,
             shape=lambda u, h, w, p: Vector((p.x * (1 - 0.12 * max(0, -h)), p.y, p.z - 0.01 * max(0, -h))))
        cage(bm, L((s * 0.25, 0.79, 2.21)), (0.09, 0.11, 0.04), t, cuts=1)
build(hood, teeth, 1)

# Рот: тёмная полость между губой и бампером
def mouth(bm, M):
    cage(bm, (0, 0.68, 2.08), (1.0, 0.38, 0.26), M("mouth"), cuts=1)
    cage(bm, (0, 0.53, 2.12), (0.70, 0.06, 0.20), M("tongue"), cuts=1)
build(shell, mouth, 1)

# ---------- Бампер-«челюсть» ----------
bumper_f = node("BumperF", body, (0, 0.62, 2.25))
def jaw(bm, M):
    pts = []
    for i in range(17):
        x = -1.0 + 2.0 * i / 16
        a = abs(x)
        y = 0.50 + 0.34 * a ** 2.2 + 0.03 * a           # концы загнуты вверх — широкая улыбка
        z = 2.33 - 0.30 * a ** 2.0
        pts.append((x, y - 0.62, z - 2.25))
    loft(bm, pts, [(-0.055, -0.08), (0.055, -0.08), (0.06, 0.0), (0.055, 0.085), (-0.05, 0.085), (-0.06, 0.0)], M("mater_rust"))
    for s in (-1, 1):                                   # нижние зубки на «челюсти»
        cage(bm, (s * 0.14, 0.60 - 0.62, 2.325 - 2.25), (0.065, 0.065, 0.03), M("teeth"), cuts=1)
    for s in (-1, 1):                                   # кронштейны к раме
        cage(bm, (s * 0.42, 0.52 - 0.62, 2.12 - 2.25), (0.08, 0.08, 0.30), M("chassis"), cuts=1)
build(bumper_f, jaw, 2)

# ---------- Фары: справа целая, слева пустая ржавая чашка ----------
for side, name in ((1, "HeadlightR"), (-1, "HeadlightL")):
    hl = node(name, body, (side * 0.75, 1.10, 2.01))
    def lamp(bm, M, side=side):
        lathe(bm, [(0.0, -0.10), (0.06, -0.095), (0.095, -0.05), (0.108, 0.02), (0.112, 0.05), (0.098, 0.062)], M("mater_rust"), 32, (0, 0, 0), "z", inside=(0, 0))
        if side > 0:
            lathe(bm, [(0.094, 0.05), (0.07, 0.068), (0.035, 0.078), (0.0, 0.080)], M("lens"), 32, (0, 0, 0), "z", inside=(0, 0))
            lathe(bm, [(0.09, 0.045), (0.05, 0.0), (0.0, -0.02)], M("reflector"), 32, (0, 0, 0), "z", inside=(0, -0.1))
        else:
            lathe(bm, [(0.094, 0.055), (0.06, 0.01), (0.0, -0.02)], M("mouth"), 32, (0, 0, 0), "z", inside=(0, -0.1))
    build(hl, lamp, 1)
    bl = node("BlinkFR" if side > 0 else "BlinkFL", hl, (0, -0.17, 0.08))
    build(bl, lambda bm, M: lathe(bm, [(0.0, -0.01), (0.03, 0.0), (0.028, 0.012), (0.0, 0.02)], M("amber"), 16, (0, 0, 0), "z", inside=(0, -0.05)), 0)

# ---------- Кабина ----------
def cab(bm, M):
    b, r = M("mater_blue"), M("mater_rust")
    cage(bm, (0, 0.66, 0.05), (1.50, 0.06, 1.0), M("rust_dark"), cuts=1)                     # пол
    cage(bm, (0, 1.30, 0.50), (1.54, 0.18, 0.16), r, cuts=2)                                 # передок под стеклом
    cage(bm, (0, 0.95, 0.52), (1.50, 0.58, 0.08), r, cuts=1)                                 # моторный щит
    cage(bm, (0, 1.30, -0.44), (1.54, 1.38, 0.09), r, cuts=2)                                # задняя стенка
    for s in (-1, 1):
        cage(bm, (s * 0.73, 1.64, 0.51), (0.10, 0.58, 0.10), r, pitch=-6, cuts=1)             # стойки A
        cage(bm, (s * 0.73, 1.64, -0.42), (0.11, 0.60, 0.12), r, cuts=1)                     # задние стойки
        cage(bm, (s * 0.78, 1.00, -0.42), (0.09, 0.76, 0.10), r, cuts=1)                     # задний край проёма
build(shell, cab, 2)

def glass_parts(bm, M):
    g = M("glass")
    cage(bm, (0, 1.66, -0.495), (0.86, 0.28, 0.02), g, cuts=0)                               # заднее окно
build(shell, glass_parts, 0)

roof = node("Roof", body, (0, 1.95, 0.0))
def roof_parts(bm, M):
    r = M("mater_rust")
    cage(bm, (0, 0.03, 0.04), (1.58, 0.13, 1.06), r, cuts=3,
         shape=lambda u, h, w, p: Vector((p.x, p.y + (0.04 * (1 - u * u) * (1 - w * w) if h > 0 else 0), p.z)))
    # козырёк над стеклом; посередине нижний край опускается — «бровь» между глазами
    cage(bm, (0, -0.02, 0.60), (1.54, 0.08, 0.20), r, cuts=3,
         shape=lambda u, h, w, p: Vector((p.x, p.y - (0.035 * (1 - abs(u)) ** 2 if h < 0 else 0) - 0.02 * u * u, p.z)))
    for x in (-0.33, -0.11, 0.11, 0.33):                                                       # габаритные огни на козырьке
        lathe(bm, [(0.0, 0.0), (0.035, 0.0), (0.03, 0.02), (0.015, 0.032), (0.0, 0.035)], M("amber"), 16, (x, 0.02, 0.62), "y", inside=(0, -0.05))
build(roof, roof_parts, 2)

# Глаза на лобовом: белки (одна выпуклая поверхность), радужки — отдельными дисками с текстурой.
# Всё смотрит только наружу: изнутри грани не рисуются, водитель видит дорогу.
eyes = node("Eyes", body, (0, 1.60, 0.56))
build(eyes, lambda bm, M: disc(bm, (0, 0, 0), 0.68, 0.255, M("eye_white"), bulge=0.035, power=3.2, rings=10, seg=64), 0)
EYE_R = 0.15
for side, name in ((-1, "EyeL"), (1, "EyeR")):
    e = node(name, eyes, (side * 0.205, -0.035, 0.034))
    build(e, lambda bm, M: disc(bm, (0, 0, 0), EYE_R, EYE_R, M("mater_eye"), bulge=0.008, rings=8, seg=48), 0)

# ---------- Двери ----------
DOOR_HINGE_Z = 0.50
for side, name in ((-1, "DoorFL"), (1, "DoorFR")):
    d = node(name, body, (side * 0.78, 1.0, DOOR_HINGE_Z))
    def door(bm, M, side=side):
        b = M("mater_blue")
        cage(bm, (0, 0.0, -0.46), (0.08, 0.76, 0.90), b, cuts=2)                                 # дверь
        cage(bm, (side * 0.042, 0.02, -0.46), (0.02, 0.42, 0.62), M("mater_green"), cuts=2)       # зелёная панель
        cage(bm, (side * 0.05, 0.25, -0.80), (0.03, 0.03, 0.12), M("mater_rust"), cuts=1)          # ручка
        cage(bm, (0, 0.64, -0.46), (0.025, 0.52, 0.82), M("glass"), cuts=0)                       # стекло
        cage(bm, (0, 0.39, -0.46), (0.09, 0.04, 0.90), b, cuts=1)                                 # кромка
    build(d, door, 2)
    # Надпись на двери — узел, развёрнутый наружу (+Z узла — внутрь кабины)
    node("DoorText" + name[-1], d, (side * 0.056, 0.02, -0.46), (0, -90 * side, 0))
    # Зеркало: высокий прямоугольник на кронштейнах у стойки
    mount = node("MirrorL" if side < 0 else "MirrorR", d, (side * 0.27, 0.72, -0.08))
    def mirror(bm, M, side=side):
        r = M("mater_rust")
        cage(bm, (0, 0, 0), (0.20, 0.34, 0.05), r, cuts=1)
        sweep(bm, [(-side * 0.25, -0.25, 0.0), (-side * 0.10, -0.22, 0.0), (0, -0.14, 0.0)], 0.03, 0.03, r)
        sweep(bm, [(-side * 0.25, 0.08, 0.0), (-side * 0.10, 0.10, 0.0), (0, 0.10, 0.0)], 0.025, 0.025, r)
    build(mount, mirror, 1)
    mg = node("MirrorGlassL" if side < 0 else "MirrorGlassR", mount, (0, 0, -0.027))
    build(mg, lambda bm, M: disc(bm, (0, 0, 0), 0.085, 0.15, M("mirror"), power=6, rings=2, seg=32, normal=(0, 0, -1)), 0)

# ---------- Кузов-платформа сзади ----------
def bed(bm, M):
    b = M("mater_blue")
    cage(bm, (0, 0.90, -1.28), (1.72, 0.08, 1.56), b, cuts=2)
    for s in (-1, 1):
        cage(bm, (s * 0.86, 1.03, -1.28), (0.07, 0.30, 1.56), b, cuts=2)
    cage(bm, (0, 1.03, -0.52), (1.72, 0.30, 0.06), b, cuts=2)
build(shell, bed, 2)

REAR_FENDER = [(-0.55, 0.66), (-0.66, 0.86), (-0.84, 1.00), (-1.05, 1.07), (-1.25, 1.09), (-1.45, 1.07), (-1.66, 1.00),
               (-1.84, 0.86), (-1.96, 0.66)]
def rear_fenders(bm, M):
    for s in (-1, 1):
        loft(bm, [(s * 0.80, y, z) for z, y in REAR_FENDER], crown(0.52, 0.10, 0.04), M("mater_blue"))
build(shell, rear_fenders, 2)

tail = node("Tailgate", body, (0, 0.90, -2.06))
build(tail, lambda bm, M: cage(bm, (0, 0.14, 0), (1.72, 0.30, 0.06), M("mater_blue"), cuts=2), 2)

for side, name in ((-1, "TaillightL"), (1, "TaillightR")):
    tl = node(name, body, (side * 0.80, 0.78, -2.09))
    build(tl, lambda bm, M: lathe(bm, [(0.0, 0.0), (0.06, 0.0), (0.058, -0.02), (0.035, -0.032), (0.0, -0.035)], M("tail_red"), 20, (0, 0, 0), "z", inside=(0, 0.05)), 0)
    build(tl, lambda bm, M: cage(bm, (0, 0, 0.03), (0.16, 0.16, 0.05), M("mater_rust"), cuts=1), 1)
    bl = node("BlinkRL" if side < 0 else "BlinkRR", tl, (-side * 0.14, 0, 0))
    build(bl, lambda bm, M: lathe(bm, [(0.0, 0.0), (0.035, 0.0), (0.03, -0.02), (0.0, -0.025)], M("amber"), 16, (0, 0, 0), "z", inside=(0, 0.05)), 0)

bumper_r = node("BumperR", body, (0, 0.56, -2.18))
def rear_bumper(bm, M):
    cage(bm, (0, 0, 0), (1.84, 0.19, 0.12), M("mater_stripes"), cuts=2)
    for s in (-1, 1):
        sweep(bm, [(s * 0.30, -0.02, -0.07), (s * 0.30, -0.10, -0.10), (s * 0.30, -0.06, -0.15)], 0.03, 0.03, M("chassis"))
build(bumper_r, rear_bumper, 1)
plate = node("PlateRear", bumper_r, (0, 0.0, -0.068))
build(plate, lambda bm, M: cage(bm, (0, 0, 0), (0.30, 0.15, 0.012), M("plate"), cuts=1), 1)

# ---------- Кран ----------
def crane(bm, M):
    r = M("mater_rust")
    sweep(bm, [(0, 0.94, -0.80), (0, 1.6, -0.88), (0, 2.25, -0.95)], 0.16, 0.16, r)                          # мачта
    for s in (-1, 1):
        sweep(bm, [(s * 0.72, 0.95, -0.60), (s * 0.40, 1.6, -0.78), (s * 0.12, 2.15, -0.93)], 0.07, 0.07, r)  # раскосы
    sweep(bm, [(-0.42, 2.20, -0.93), (0.42, 2.20, -0.93)], 0.08, 0.08, r)                                      # перекладина
    sweep(bm, [(0, 2.24, -0.90), (0, 1.95, -1.65), (0, 1.66, -2.38)], 0.15, 0.17, r)                          # стрела
    lathe(bm, [(0.07, -0.06), (0.08, -0.04), (0.08, 0.04), (0.07, 0.06)], r, 24, (0, 1.62, -2.42), "x", inside=(0, 0))        # блок
    # лебёдка: барабан с тросом
    lathe(bm, [(0.17, -0.34), (0.17, -0.30), (0.13, -0.29), (0.13, 0.29), (0.17, 0.30), (0.17, 0.34)], r, 32, (0, 1.12, -1.32), "x", inside=(0, 0))
    lathe(bm, [(0.145, -0.28), (0.145, 0.28)], M("chassis"), 32, (0, 1.12, -1.32), "x", inside=(0, 0))
    for s in (-1, 1):
        cage(bm, (s * 0.38, 1.03, -1.32), (0.05, 0.26, 0.16), r, cuts=1)
    # маячок на перекладине
    lathe(bm, [(0.0, 0.0), (0.075, 0.0), (0.075, 0.03)], M("chassis"), 24, (0, 2.24, -0.93), "y", inside=(0.03, 0.015))
    lathe(bm, [(0.065, 0.03), (0.066, 0.10), (0.045, 0.15), (0.0, 0.165)], M("amber"), 24, (0, 2.24, -0.93), "y", inside=(0, 0.05))
build(shell, crane, 1)

def cables(bm, M):
    c = M("chassis")
    sweep(bm, [(0, 1.27, -1.33), (0, 1.47, -1.87), (0, 1.68, -2.40)], 0.018, 0.018, c)
    sweep(bm, [(0, 1.56, -2.48), (0, 1.30, -2.48), (0, 1.07, -2.48)], 0.016, 0.016, c)
    cage(bm, (0, 1.05, -2.48), (0.07, 0.10, 0.07), M("mater_rust"), cuts=1)
    hook = [(0, 1.00, -2.48), (0, 0.92, -2.48), (0, 0.86, -2.45), (0, 0.85, -2.39), (0, 0.89, -2.36), (0, 0.93, -2.37)]
    sweep(bm, hook, 0.035, 0.03, M("mater_rust"))
build(shell, cables, 1)

# ---------- Салон ----------
inter = node("Interior", body)
def interior(bm, M):
    seat = M("seat_brown")
    cage(bm, (0, 0.96, -0.18), (1.36, 0.16, 0.50), seat, cuts=2)                                # диван
    cage(bm, (0, 1.30, -0.36), (1.36, 0.56, 0.14), seat, cuts=2, pitch=-12)
    cage(bm, (0, 0.80, -0.18), (1.30, 0.20, 0.44), M("rust_dark"), cuts=1)
    cage(bm, (0, 1.24, 0.42), (1.48, 0.20, 0.18), M("mater_blue"), cuts=2)                       # торпеда цвета кузова
    cage(bm, (0, 1.06, 0.46), (1.40, 0.24, 0.10), M("rust_dark"), cuts=1)
    cage(bm, (0, 1.36, 0.46), (1.50, 0.05, 0.14), M("rust_dark"), cuts=1)
    sweep(bm, [(0.12, 0.70, 0.22), (0.06, 0.95, 0.15), (0.02, 1.16, 0.06)], 0.018, 0.018, M("chrome"))  # рычаг
    lathe(bm, [(0.0, -0.03), (0.03, -0.015), (0.032, 0.0), (0.03, 0.015), (0.0, 0.03)], M("int_black"), 16, (0.02, 1.18, 0.05), "y", inside=(0, 0))
    cage(bm, (0, 1.89, 0.0), (1.40, 0.02, 0.88), M("int_roof"), cuts=0)                          # потолок
build(inter, interior, 2)

def gauge(name, center, r):
    g = node("Gauge" + name, inter, center, look_euler(sub(center, EYES)))
    def face(bm, M):
        disc(bm, (0, 0, 0), r, r, M("gauge_face"), rings=2, seg=32, normal=(0, 0, -1))
        for i in range(13):
            a = math.radians(210 - 240 * i / 12)
            x, y = math.cos(a) * r * 0.8, math.sin(a) * r * 0.8
            cage(bm, (x, y, -0.002), (0.004, 0.004 + (0.012 if i % 2 == 0 else 0.006), 0.001), M("white"), cuts=0, roll=math.degrees(a) - 90)
        lathe(bm, [(r + 0.002, 0.004), (r + 0.010, 0.0), (r + 0.008, -0.008), (r - 0.001, -0.006)], M("chrome"), 32, (0, 0, 0), "z", inside=(r + 0.004, -0.002))
    build(g, face, 0)
    n = node(name + "Needle", g, (0, 0, -0.004))
    build(n, lambda bm, M: cage(bm, (0, r * 0.38, 0), (0.0035, r * 0.85, 0.002), M("needle"), cuts=0), 0)
    return g

gauge("Speed", (-0.50, 1.25, 0.325), 0.06)
gauge("Fuel", (-0.29, 1.25, 0.325), 0.045)
for name, x in (("FuelLamp", -0.29), ("EngineLamp", -0.50)):
    lp = node(name, inter, (x, 1.18, 0.328))
    build(lp, lambda bm, M: cage(bm, (0, 0, 0), (0.025, 0.012, 0.003), M("lamp_off"), cuts=0), 0)
radio = node("RadioScreen", inter, (0.08, 1.24, 0.328), look_euler(sub((0.08, 1.24, 0.328), EYES)))
build(radio, lambda bm, M: disc(bm, (0, 0, 0), 0.09, 0.025, M("screen"), power=8, rings=2, seg=32, normal=(0, 0, -1)), 0)
rb = node("RadioBezel", radio)
build(rb, lambda bm, M: cage(bm, (0, 0, 0.008), (0.21, 0.07, 0.014), M("chrome"), cuts=1), 1)

rm = node("RearMirror", inter, (0, 1.82, 0.40), (0, 15, 0))
build(rm, lambda bm, M: cage(bm, (0, 0, 0.016), (0.26, 0.075, 0.03), M("int_black"), cuts=1), 1)
rmg = node("RearMirrorGlass", rm, (0, 0, -0.0025))
build(rmg, lambda bm, M: disc(bm, (0, 0, 0), 0.12, 0.03, M("mirror"), power=8, rings=2, seg=32, normal=(0, 0, -1)), 0)

# Руль: большой тонкий обод, три спицы, колонка
sw = node("SteeringWheel", body, STEER, (STEER_TILT, 0, 0))
def wheel(bm, M):
    blk = M("int_black")
    torus(bm, (0, 0, 0), 0.22, 0.013, blk, 64, 8)
    for a in (0, 120, 240):
        ra = math.radians(a + 90)
        sweep(bm, [(0, 0.0, 0), (math.cos(ra) * 0.215, 0.0, math.sin(ra) * 0.215)], 0.02, 0.008, blk)
    lathe(bm, [(0.0, 0.03), (0.04, 0.025), (0.045, 0.0), (0.03, -0.02)], M("chrome"), 24, (0, 0, 0), "y", inside=(0, 0))
    lathe(bm, [(0.03, -0.45), (0.03, -0.02)], blk, 12, (0, 0, 0), "y", inside=(0, -0.2))
build(sw, wheel, 0)

torso = node("DriverTorso", body, (EYES[0], 1.30, -0.30), (-10, 0, 0))
build(torso, lambda bm, M: cage(bm, (0, 0, 0), (0.42, 0.50, 0.24), M("jacket"), cuts=1), 2)
head = node("DriverHead", body, (EYES[0], EYES[1] + 0.02, EYES[2] - 0.05))
def head_parts(bm, M):
    cage(bm, (0, 0, 0), (0.19, 0.24, 0.22), M("skin"), cuts=1)
    cage(bm, (0, 0.06, -0.025), (0.20, 0.15, 0.21), M("hair"), cuts=1)
build(head, head_parts, 2)

# ---------- Колёса: широкие шины с крупным протектором, стальные диски с ржавой ступицей ----------
def tire(bm, M, h0):
    """Шина вокруг оси Y узла, центр по оси — h0 (наружу — +Y)."""
    prof = [(0.255, 0.10), (0.30, 0.122), (0.36, 0.126), (0.398, 0.114), (0.414, 0.088), (0.418, 0.045),
            (0.418, -0.045), (0.414, -0.088), (0.398, -0.114), (0.36, -0.126), (0.30, -0.122), (0.255, -0.10)]
    lathe(bm, [(r, h + h0) for r, h in prof], M("rubber"), 64, (0, 0, 0), "y", inside=(0.34, h0))
    for k in range(30):                                 # шашки протектора, вперемешку со сдвигом
        a = 2 * math.pi * k / 30
        for hs in (-1, 1):
            off = 0.035 if (k % 2 == 0) == (hs > 0) else 0.055
            c = (TIRE_R * math.sin(a), h0 + hs * off, TIRE_R * math.cos(a))
            cage(bm, c, (0.06, 0.05, 0.022), M("tread"), cuts=0, yaw=math.degrees(a))

def steel_wheel(bm, M, h0, deep=False):
    d = 0.04 if deep else 0.0
    lathe(bm, [(0.258, 0.10 + h0), (0.25, 0.085 + h0), (0.235, 0.06 + h0 - d), (0.20, 0.035 + h0 - d), (0.15, 0.05 + h0 - d),
               (0.115, 0.085 + h0 - d), (0.095, 0.095 + h0 - d)], M("steel_dark"), 48, (0, 0, 0), "y", inside=(0.17, h0 - 0.1))
    lathe(bm, [(0.258, 0.10 + h0), (0.255, -0.10 + h0), (0.15, -0.10 + h0)], M("chassis"), 48, (0, 0, 0), "y", inside=(0.30, h0))
    lathe(bm, [(0.095, 0.095 + h0 - d), (0.085, 0.13 + h0 - d), (0.06, 0.155 + h0 - d), (0.03, 0.165 + h0 - d), (0.0, 0.168 + h0 - d)],
          M("rust_dark"), 32, (0, 0, 0), "y", inside=(0, h0 - d))
    for k in range(8):                                  # гайки
        a = 2 * math.pi * (k + 0.5) / 8
        lathe(bm, [(0.0, 0.112 + h0 - d), (0.013, 0.104 + h0 - d), (0.013, 0.09 + h0 - d)], M("rust_dark"), 8,
              (0.108 * math.cos(a), 0, 0.108 * math.sin(a)), "y", inside=(0, h0 - 0.05))
    for k in range(6):                                  # окна в диске
        a = 2 * math.pi * k / 6
        cage(bm, (0.19 * math.cos(a), 0.046 + h0 - d, 0.19 * math.sin(a)), (0.05, 0.006, 0.03), M("mouth"), cuts=0, yaw=-math.degrees(a))

for side in (-1, 1):
    for front in (True, False):
        tag = ("F" if front else "R") + ("L" if side < 0 else "R")
        mount = node(("Steer" if front else "Mount") + tag, root, (side * (TRACK_F if front else TRACK_R), TIRE_R, AXLE_F if front else AXLE_R))
        w = node("Wheel" + tag, mount, (0, 0, 0), (0, 0, 90))
        rim = node("Rim" + tag, w, (0, 0, 0), (0, 0, 0) if side < 0 else (180, 0, 0))
        def wheel_parts(bm, M, front=front):
            tire(bm, M, 0.0)
            steel_wheel(bm, M, 0.0, deep=not front)
            if not front:                               # сдвоенные задние колёса
                tire(bm, M, -DUAL)
                steel_wheel(bm, M, -DUAL)
        build(rim, wheel_parts, 0)
print("built", len(nodes), "nodes")

# ---------- Сглаживание и UV ----------
bpy.context.view_layer.update()
PROJ = {"mater_rust", "mater_blue", "mater_green"}
for ob in nodes:
    me = ob.data
    if not me.polygons:
        continue
    for p in me.polygons:
        p.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(48))
    uvl = me.uv_layers.get("UV") or me.uv_layers.new(name="UV")
    mw = ob.matrix_world
    names = [m.name if m else "" for m in me.materials]
    for p in me.polygons:
        key = names[p.material_index] if p.material_index < len(names) else ""
        for li in p.loop_indices:
            v = me.vertices[me.loops[li].vertex_index].co
            if key in PROJ or key == "mater_stripes":
                w = mw @ v
                x, y, z = w.x, w.z, w.y                 # Blender → Unity
                if key == "mater_stripes":
                    uv = (x * 0.9, y * 0.9 + z * 0.3)
                else:
                    uv = ((0.8 * x + 0.6 * z) * 0.6, (y + 0.5 * x - 0.4 * z) * 0.6)
            elif key == "mater_eye":
                uv = (v.x / EYE_R * 0.25 + 0.5, v.z / EYE_R * 0.25 + 0.5)
            elif key == "mirror":
                uv = (v.x / 0.17 + 0.5, v.z / 0.30 + 0.5)
            else:
                uv = (0.0, 0.0)
            uvl.data[li].uv = uv

# ---------- Выгрузка ----------
gq_export.write_gqm(nodes, out_bytes)

bpy.ops.mesh.primitive_plane_add(size=30, location=(0, 0, 0))
for name, loc, energy in [("KeyLight", (4, 5, 6), 800), ("FillLight", (-5, -3, 4), 400)]:
    ld = bpy.data.lights.new(name, 'AREA')
    ld.size = 4
    ld.energy = energy
    lo = bpy.data.objects.new(name, ld)
    lo.location = loc
    lo.rotation_euler = (Vector((0, 0, 0.8)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(lo)
os.makedirs(os.path.dirname(os.path.abspath(out_blend)), exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(out_blend))
print("SAVED", out_blend)

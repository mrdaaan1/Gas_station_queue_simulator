# Доводка машины из кода в Blender и выгрузка в игру (X5 Давидыча, Audi A7).
#   1) Импорт дерева узлов модели из кода (ModelPreview.dll tree x5 x5_tree.json): каждый узел — объект Blender
#      с тем же шарниром (капот, двери, бамперы, колёса…), координаты Unity → Blender: (x, y, z) → (x, z, y).
#   2) Доводка: панели кузова получают толщину и скруглённые кромки (видны щели между панелями), стёкла и линзы —
#      объём; колёса строятся заново (шина, диск со своим рисунком спиц, тормоз, суппорт), салон — кресла, руль,
#      детали торпеды и дверей; сглаживание с острыми кромками. Отличия машин — в CARS ниже.
#   3) Выгрузка в Assets/Resources/Models/<Машина>.bytes (формат GQM1, см. Assets/Scripts/World/Models/ModelFile.cs)
#      и сохранение сцены .blend, чтобы её можно было открыть и покрутить.
# Запуск:
#   Blender --background --factory-startup --python car_build.py -- x5 x5_tree.json X5.bytes x5.blend [camo.png] [decal.png]
#   PAINT=#b8bcc1 Blender --background --factory-startup --python car_build.py -- a7 a7_tree.json A7.bytes a7.blend
import bpy, bmesh, gzip, io, json, math, os, struct, sys
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
car, argv = argv[0], argv[1:]
tree_path, out_bytes, out_blend = argv[0], argv[1], argv[2]
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gq_materials
gq_materials.camo_path = argv[3] if len(argv) > 3 else None
gq_materials.decal_path = argv[4] if len(argv) > 4 else None

# ---------- Отличия машин ----------
# Колесо: радиус обода R, полуширина HW, радиус шины TR, тормозной диск, рисунок спиц, материалы диска.
# Салон: высота подушек, ряды кресел, материалы кожи, руль, детали торпеды и дверей.
CARS = {
    "x5": dict(
        R=0.262, HW=0.1475, TR=0.370, disc=0.197, bell=0.115, spokes="y", face="alloy", side="alloy",
        caliper="caliper", cal_r=(0.128, 0.212), cal_w=0.042,
        seat_y=0.755, seat_z=-0.35, seat_x=0.38, seat_w=0.54, rear_z=-1.28, rear_xs=(-0.48, 0.0, 0.48), rear_w=0.46, rear_back_w=1.5,
        seat_old="leather_black", front_cut=(-0.72, 0.0, 0.95), rear_cut=(-1.60, -0.98, 0.92),
        insert="leather_tan", bolster="leather_black", wheel="m", idrive=True, dash=True,
        door_z=(0.42, -0.62), door_y=(1.07, 0.62, 0.80), door_x=0.865, door_mat="int_black",
    ),
    "a7": dict(
        R=0.258, HW=0.1275, TR=0.356, disc=0.187, bell=0.110, spokes="v", face="alloy_machined", side="rim_gunmetal",
        caliper="rim_black", cal_r=(0.122, 0.198), cal_w=0.036,
        seat_y=0.44, seat_z=-0.42, seat_x=0.37, seat_w=0.52, rear_z=-1.20, back_h=0.88, rear_head=0.58, rear_xs=(-0.46, 0.0, 0.46), rear_w=0.44, rear_back_w=1.42,
        seat_old="leather_cream", front_cut=(-0.80, -0.05, 0.62), rear_cut=(-1.52, -0.9, 0.60),
        insert="leather_cream", bolster="leather_cream", piping="int_beige", wheel="audi", idrive=False, dash=False,
        door_z=(0.30, -0.78), door_y=(0.80, 0.50, 0.60), door_x=0.855, door_mat="int_beige",
    ),
}
C = CARS[car]

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------- Unity ↔ Blender ----------
P = Matrix(((1, 0, 0, 0), (0, 0, 1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))  # меняет местами Y и Z, сам себе обратный

def ub(v):  # точка/направление Unity → Blender
    return (v[0], v[2], v[1])

def unity_rot(e):
    """Поворот как Frame.Rot в игре: сначала Z, потом X, потом Y (градусы)."""
    d = math.pi / 180
    def rot(v):
        x, y, z = v
        cz, sz = math.cos(e[2] * d), math.sin(e[2] * d)
        x, y = x * cz - y * sz, x * sz + y * cz
        cx, sx = math.cos(e[0] * d), math.sin(e[0] * d)
        y, z = y * cx - z * sx, y * sx + z * cx
        cy, sy = math.cos(e[1] * d), math.sin(e[1] * d)
        return (x * cy + z * sy, y, -x * sy + z * cy)
    cols = [rot((1, 0, 0)), rot((0, 1, 0)), rot((0, 0, 1))]
    return Matrix(((cols[0][0], cols[1][0], cols[2][0]), (cols[0][1], cols[1][1], cols[2][1]), (cols[0][2], cols[1][2], cols[2][2])))

def unity_local(pos, euler):
    m = unity_rot(euler).to_4x4()
    m.translation = Vector(pos)
    return P @ m @ P

# ---------- Импорт ----------
tree = json.load(open(tree_path))
objs = []
for idx, nd in enumerate(tree["nodes"]):
    me = None
    if nd["meshes"]:
        me = bpy.data.meshes.new(nd["name"])
        verts, faces, fmat, uvs = [], [], [], []
        good = bad = 0
        for mi, m in enumerate(nd["meshes"]):
            base = len(verts)
            v, n, uv, t = m["v"], m["n"], m["uv"], m["t"]
            for i in range(0, len(v), 3):
                verts.append(ub(v[i:i + 3]))
            for i in range(0, len(t), 3):
                a, b, c = t[i], t[i + 1], t[i + 2]
                # Unity → Blender: отражение меняет обход, поэтому (a, c, b)
                faces.append((base + a, base + c, base + b))
                fmat.append(mi)
                uvs.append([(uv[a * 2], uv[a * 2 + 1]), (uv[c * 2], uv[c * 2 + 1]), (uv[b * 2], uv[b * 2 + 1])])
                # Проверка обхода по нормали из кода
                pa, pb, pc = Vector(verts[base + a]), Vector(verts[base + c]), Vector(verts[base + b])
                nn = Vector(ub(n[a * 3:a * 3 + 3])) + Vector(ub(n[b * 3:b * 3 + 3])) + Vector(ub(n[c * 3:c * 3 + 3]))
                d = (pb - pa).cross(pc - pa).dot(nn)
                if d > 0: good += 1
                elif d < 0: bad += 1
        me.from_pydata(verts, [], faces)
        uvl = me.uv_layers.new(name="UV")
        for poly, fuv in zip(me.polygons, uvs):
            poly.material_index = fmat[poly.index]
            for k, li in enumerate(poly.loop_indices):
                uvl.data[li].uv = fuv[k]
        for m in nd["meshes"]:
            me.materials.append(gq_materials.get_mat(m["mat"]))
        me.validate(clean_customdata=False)
        if bad > good:
            print("FLIP", nd["name"], good, bad)
            me.flip_normals()
    ob = bpy.data.objects.new(nd["name"], me)
    ob["gq_index"] = idx
    ob["gq_name"] = nd["name"]
    ob["gq_pos"] = nd["pos"]
    ob["gq_euler"] = nd["euler"]
    scene.collection.objects.link(ob)
    if nd["parent"] >= 0:
        ob.parent = objs[nd["parent"]]
    ob.matrix_basis = unity_local(nd["pos"], nd["euler"])
    if me is None:
        ob.empty_display_size = 0.05
    objs.append(ob)
by_name = {o["gq_name"]: o for o in objs}
print("imported", len(objs))

# ---------- Сглаживание ----------
for ob in objs:
    if ob.type != 'MESH':
        continue
    for p in ob.data.polygons:
        p.use_smooth = True
    ob.data.set_sharp_from_angle(angle=math.radians(38))

# ---------- Панели кузова: толщина и скруглённые кромки ----------
PANELS = ["Shell", "Hood", "Roof", "Tailgate", "DoorFL", "DoorFR", "DoorRL", "DoorRR", "BumperF", "BumperR"]
OUTER = {"paint", "black", "glass"}  # толщину получают только наружные поверхности (не оси, подкрылки, обшивка)

def clean(me):
    """Склеить совпадающие вершины и убрать вырожденные грани (нос и корма сходятся в точку — иначе толщина даёт шипы)."""
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0004)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.0004)
    bad = [f for f in bm.faces if f.calc_area() < 1e-8]
    if bad:
        bmesh.ops.delete(bm, geom=bad, context='FACES')
    bm.to_mesh(me)
    bm.free()

for name in PANELS:
    ob = by_name.get(name)
    if ob is None or ob.type != 'MESH':
        continue
    clean(ob.data)
    ob.data.set_sharp_from_angle(angle=math.radians(38))
    outer_idx = {i for i, m in enumerate(ob.data.materials) if m and m.name in OUTER}
    vg = ob.vertex_groups.new(name="outer")
    vg.add(sorted({v for p in ob.data.polygons if p.material_index in outer_idx for v in p.vertices}), 1.0, 'REPLACE')
    so = ob.modifiers.new("Thickness", 'SOLIDIFY')
    so.thickness = 0.010
    so.offset = -1.0
    so.use_rim = True
    so.use_even_offset = False
    so.use_quality_normals = True
    so.vertex_group = "outer"
    so.thickness_vertex_group = 0.0
    bv = ob.modifiers.new("Edges", 'BEVEL')
    bv.width = 0.0035
    bv.segments = 2
    bv.limit_method = 'ANGLE'
    bv.angle_limit = math.radians(50)
    bv.harden_normals = False
    wn = ob.modifiers.new("Normals", 'WEIGHTED_NORMAL')
    wn.keep_sharp = True

# Линзы фар и фонарей — объёмное стекло
for name in ["HeadlightL", "HeadlightR", "TaillightL", "TaillightR"]:
    ob = by_name.get(name)
    if ob is not None and ob.type == 'MESH':
        clean(ob.data)
        so = ob.modifiers.new("Thickness", 'SOLIDIFY')
        so.thickness = 0.004
        so.offset = -1.0
        so.use_quality_normals = True

# ---------- Диски: 20" Y-образные двойные спицы ----------
def mat_index(me, key):
    for i, m in enumerate(me.materials):
        if m and m.name == key:
            return i
    me.materials.append(gq_materials.get_mat(key))
    return len(me.materials) - 1

def build_rim(me):
    """Новый диск в локальных координатах узла Rim (ось колеса — Z Blender, лицо диска — +Z)."""
    bm = bmesh.new()
    bm.from_mesh(me)
    # Колесо строим заново целиком: шина 275/40 R20, диск, тормоз
    bmesh.ops.delete(bm, geom=list(bm.faces), context='FACES')
    bmesh.ops.delete(bm, geom=list(bm.verts), context='VERTS')
    i_alloy, i_chrome, i_inner, i_black = (mat_index(me, k) for k in (C["face"], "chrome", "rim_inner", "black"))
    i_side = mat_index(me, C["side"])
    i_rubber, i_tread, i_disc = (mat_index(me, k) for k in ("rubber", "tread", "disc"))
    R, HW = C["R"], C["HW"]
    TR = C["TR"]
    face = HW - 0.014

    def face_out(f, inside):
        """Развернуть грань лицом от точки inside = (радиус, z) в сечении. В Unity видна только лицевая сторона."""
        f.normal_update()
        c = f.calc_center_median()
        rr = math.hypot(c.x, c.y)
        radial = Vector((c.x / rr, c.y / rr, 0)) if rr > 1e-6 else Vector((0, 0, 0))
        d = radial * (rr - inside[0]) + Vector((0, 0, c.z - inside[1]))
        if f.normal.dot(d) < 0:
            f.normal_flip()

    def lathe(profile, mat, seg=72, inside=None):
        """Тело вращения вокруг Z: профиль (радиус, z); inside — точка сечения, от которой грани смотрят наружу."""
        rings = []
        for k in range(seg):
            a = 2 * math.pi * k / seg
            rings.append([bm.verts.new((r * math.cos(a), r * math.sin(a), z)) for r, z in profile])
        for k in range(seg):
            r0, r1 = rings[k], rings[(k + 1) % seg]
            for j in range(len(profile) - 1):
                f = bm.faces.new((r0[j], r1[j], r1[j + 1], r0[j + 1]))
                f.material_index = mat
                f.smooth = True
                if inside is not None:
                    face_out(f, inside)

    # ---- Шина: боковины с выпуклостью, скруглённое плечо, 4 продольные канавки, шашки на плечах ----
    seg = 160
    prof = []
    for k in range(9):                       # боковина снаружи (лицо, +Z): от обода к плечу
        t = k / 8
        prof.append((R + 0.006 + t * (TR - 0.018 - R - 0.006), HW - 0.022 + 0.022 * math.sin(t * math.pi * 0.9)))
    for k in range(1, 6):                    # плечо
        a = k / 5 * math.pi / 2
        prof.append((TR - 0.018 + 0.018 * math.sin(a), HW - 0.004 - 0.022 * (1 - math.cos(a))))
    tread_z0 = HW - 0.026
    nt = 22
    for k in range(1, nt):                   # беговая дорожка
        prof.append((TR, tread_z0 - 2 * tread_z0 * k / nt))
    n_front = len(prof)
    for r, z in reversed(prof[:n_front - nt + 1]):   # зеркально — внутренняя сторона
        prof.append((r, -z))
    grooves = tuple(g * HW / 0.1475 for g in (-0.072, -0.026, 0.026, 0.072))
    rings = []
    for k in range(seg):
        a = 2 * math.pi * k / seg
        ring = []
        for j, (r, z) in enumerate(prof):
            rr = r
            if abs(r - TR) < 1e-6:
                if any(abs(z - g) < 0.007 for g in grooves):
                    rr -= 0.008                                        # продольная канавка
                elif abs(z) > 0.09 * HW / 0.1475 and ((k + (z * 40)) % 6) < 2:
                    rr -= 0.006                                        # поперечные прорези на плечах
            ring.append(bm.verts.new((rr * math.cos(a), rr * math.sin(a), z)))
        rings.append(ring)
    for k in range(seg):
        r0, r1 = rings[k], rings[(k + 1) % seg]
        for j in range(len(prof) - 1):
            f = bm.faces.new((r0[j], r1[j], r1[j + 1], r0[j + 1]))
            on_tread = abs(prof[j][0] - TR) < 1e-6 and abs(prof[j + 1][0] - TR) < 1e-6
            f.material_index = i_tread if on_tread else i_rubber
            f.smooth = True
            face_out(f, ((R + TR) / 2, 0.0))   # наружу от середины сечения шины

    # ---- Тормоз: вентилируемый диск Ø 395 мм с перфорацией и тёмным «колоколом» ----
    D, B = C["disc"], C["bell"]
    lathe([(B, -0.022), (D, -0.022), (D, -0.050), (B, -0.050), (B, -0.022)], i_disc, 96, inside=((B + D) / 2, -0.036))
    lathe([(B, -0.024), (0.072, -0.010), (0.072, 0.040), (0.050, 0.040)], i_inner, 64, inside=(0.0, -0.08))
    for ring_r, n_holes in ((B + 0.25 * (D - B), 24), (B + 0.5 * (D - B), 24), (B + 0.75 * (D - B), 24)):
        for k in range(n_holes):
            a = 2 * math.pi * (k + ring_r * 37) / n_holes
            bmesh.ops.create_circle(bm, cap_ends=True, segments=8, radius=0.0042,
                                    matrix=Matrix.Translation((ring_r * math.cos(a), ring_r * math.sin(a), -0.0215)))
    for f in bm.faces:
        if not f.tag and len(f.verts) == 8:
            f.material_index = i_black
            f.tag = True

    # Полированная закраина обода и «бочка»
    lathe([(R - 0.020, face - 0.010), (R - 0.006, face + 0.002), (R + 0.006, face + 0.004), (R + 0.010, face - 0.004), (R + 0.004, face - 0.012)], i_chrome, inside=(R, face - 0.03))
    lathe([(R - 0.012, -HW + 0.012), (R - 0.016, face - 0.012), (R - 0.020, face - 0.010)], i_inner, inside=(R + 0.08, 0.0))   # бочка — видна изнутри
    lathe([(0.10, -HW + 0.03), (R - 0.012, -HW + 0.012)], i_inner, inside=(0.18, -0.6))                                    # задняя стенка — лицом вперёд
    # Ступица, колпачок с кольцом
    lathe([(0.0, face - 0.004), (0.060, face - 0.010), (0.075, face - 0.030)], i_alloy, 48, inside=(0.0, face - 0.08))
    lathe([(0.0, face + 0.006), (0.030, face + 0.004), (0.034, face - 0.004)], i_black, 36, inside=(0.0, face - 0.05))
    lathe([(0.034, face - 0.004), (0.036, face + 0.003), (0.040, face + 0.0), (0.040, face - 0.006)], i_chrome, 36, inside=(0.030, face - 0.02))

    def spoke(a0, a1, r0, r1, w0, w1, depth):
        """Спица: коробка от радиуса r0 (угол a0) к r1 (угол a1), ширина w0→w1, лицо вогнуто к ступице."""
        p0 = Vector((r0 * math.cos(a0), r0 * math.sin(a0), 0))
        p1 = Vector((r1 * math.cos(a1), r1 * math.sin(a1), 0))
        d = (p1 - p0).normalized()
        s = Vector((-d.y, d.x, 0))
        z0, z1 = face - 0.020, face - 0.002   # ступица утоплена — диск «глубокий»
        vs = []
        for p, w, z in ((p0, w0, z0), (p1, w1, z1)):
            for sx, sz in ((-1, 0), (1, 0), (1, -1), (-1, -1)):
                q = p + s * (w / 2 * sx) + Vector((0, 0, z + sz * depth))
                vs.append(bm.verts.new(q))
        a, b = vs[:4], vs[4:]
        quads = [(a[0], a[1], b[1], b[0]), (a[1], a[2], b[2], b[1]), (a[2], a[3], b[3], b[2]), (a[3], a[0], b[0], b[3]),
                 (a[3], a[2], a[1], a[0]), (b[0], b[1], b[2], b[3])]
        new_faces = []
        for qi, q in enumerate(quads):
            f = bm.faces.new(q)
            f.material_index = i_alloy if qi == 0 else i_side   # лицо спицы — точёное, бока — тёмные (у X5 всё одно)
            new_faces.append(f)
        bmesh.ops.recalc_face_normals(bm, faces=new_faces)
        edges = list({e for f in new_faces for e in f.edges})
        bmesh.ops.bevel(bm, geom=edges, offset=0.0025, segments=2, profile=0.5, affect='EDGES', clamp_overlap=True)

    for k in range(5):
        a = 2 * math.pi * k / 5 + math.pi / 2
        if C["spokes"] == "y":
            spoke(a, a, 0.050, 0.152, 0.066, 0.052, 0.034)              # ствол — литой, широкий
            for side in (-1, 1):                                          # две ветки к ободу
                spoke(a, a + side * math.radians(12), 0.138, R - 0.014, 0.040, 0.034, 0.030)
        else:
            for side in (-1, 1):                                          # «V»: две спицы расходятся от ступицы к ободу
                spoke(a + side * math.radians(3.5), a + side * math.radians(11), 0.050, R - 0.014, 0.040, 0.036, 0.032)
        # Гайка
        an = a + math.pi / 5
        c = Vector((0.046 * math.cos(an), 0.046 * math.sin(an), face - 0.006))
        res = bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=0.0085, radius2=0.0085, depth=0.012,
                                    matrix=Matrix.Translation(c))
        for v in res["verts"]:
            for f in v.link_faces:
                f.material_index = i_chrome
    for f in bm.faces:
        if f.material_index not in (i_alloy, i_side, i_chrome, i_inner, i_black):
            continue
        f.smooth = True
    bm.to_mesh(me)
    bm.free()
    me.set_sharp_from_angle(angle=math.radians(40))

def build_caliper(me, side):
    """Большой шестипоршневой суппорт: дуга вокруг диска сверху-сзади, охватывает его с обеих сторон.
    Система узла Mount/Steer (не крутится): ось колеса — X Unity, наружу — side."""
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.delete(bm, geom=list(bm.faces), context='FACES')
    bmesh.ops.delete(bm, geom=list(bm.verts), context='VERTS')
    bm.to_mesh(me)
    bm.free()
    cal = mat_index(me, C["caliper"])
    r_in, r_out = C["cal_r"]
    cw = C["cal_w"]
    disc_x = -0.036 * side                     # диск — внутри от спиц
    def build(b):
        n = 10
        a0, a1 = math.radians(118), math.radians(178)   # сверху-сзади (угол от +Z Unity к +Y)
        rings = []
        for i in range(n + 1):
            a = a0 + (a1 - a0) * i / n
            ring = []
            for (dx, r) in ((-cw, r_in), (cw, r_in), (cw, r_out), (-cw, r_out)):
                x = disc_x + dx
                y, z = r * math.sin(a), r * math.cos(a)
                ring.append(b.verts.new(ub((x, y, z))))
            rings.append(ring)
        for i in range(n):
            for j in range(4):
                f = b.faces.new((rings[i][j], rings[i + 1][j], rings[i + 1][(j + 1) % 4], rings[i][(j + 1) % 4]))
                f.material_index = cal
        for ring in (rings[0], rings[-1]):
            f = b.faces.new(ring)
            f.material_index = cal
        bmesh.ops.recalc_face_normals(b, faces=b.faces[:])
    subd_into(me, build, level=2)

for tag in ("FL", "FR", "RL", "RR"):
    ob = by_name.get("Rim" + tag)
    if ob is not None and ob.type == 'MESH':
        build_rim(ob.data)

# ---------- Салон: спортивные кресла, задний диван, руль М ----------
# Детали строятся «клетками» (коробки с разрезами), которые сглаживает Subdivision — получаются мягкие формы кожи.
# Координаты задаются как в Unity (x вправо, y вверх, z вперёд) в системе узла; в Blender — (x, z, y).

def subd_into(target_me, build, level=2):
    """Построить клетку build(bm), сгладить Subdivision и добавить в сетку target_me (индексы материалов — её)."""
    bm = bmesh.new()
    build(bm)
    tmp = bpy.data.meshes.new("tmp")
    for m in target_me.materials:   # материалы — до to_mesh, иначе номера материалов обнуляются
        tmp.materials.append(m)
    bm.to_mesh(tmp)
    bm.free()
    ob = bpy.data.objects.new("tmp", tmp)
    scene.collection.objects.link(ob)
    md = ob.modifiers.new("S", 'SUBSURF')
    md.levels = level
    md.render_levels = level
    ev = ob.evaluated_get(bpy.context.evaluated_depsgraph_get())
    out = bpy.data.meshes.new_from_object(ev)
    bpy.data.objects.remove(ob)
    bpy.data.meshes.remove(tmp)
    tb = bmesh.new()
    tb.from_mesh(target_me)
    tb.from_mesh(out)
    for f in tb.faces:
        f.smooth = True
    tb.to_mesh(target_me)
    tb.free()
    bpy.data.meshes.remove(out)

def cage(bm, c, size, mat, yaw=0.0, pitch=0.0, cuts=2, shape=None):
    """Коробка-клетка: центр c и размеры size в координатах Unity узла; yaw — поворот вокруг вертикали (градусы),
    pitch — наклон вокруг поперечной оси; shape(u, v, w, p) может сдвинуть вершину (u, v, w — от −1 до 1 по осям)."""
    res = bmesh.ops.create_cube(bm, size=1.0)
    verts = res["verts"]
    edges = list({e for v in verts for e in v.link_edges})
    if cuts:
        bmesh.ops.subdivide_edges(bm, edges=edges, cuts=cuts, use_grid_fill=True)
    new_verts = list({v for f in bm.faces if not f.tag for v in f.verts})
    for f in bm.faces:
        if not f.tag:
            f.tag = True
            f.material_index = mat
    yr, pr = math.radians(yaw), math.radians(pitch)
    for v in new_verts:
        u, w_, h = v.co.x * 2, v.co.y * 2, v.co.z * 2       # create_cube: −0.5…0.5 → −1…1 (Blender: x, y=вперёд, z=вверх)
        p = Vector((u * size[0] / 2, h * size[1] / 2, w_ * size[2] / 2))  # Unity: x, y (вверх), z (вперёд)
        if shape:
            p = shape(u, h, w_, p)
        # наклон вокруг X (Unity), потом поворот вокруг вертикали
        y, z = p.y * math.cos(pr) - p.z * math.sin(pr), p.y * math.sin(pr) + p.z * math.cos(pr)
        x, z = p.x * math.cos(yr) + z * math.sin(yr), -p.x * math.sin(yr) + z * math.cos(yr)
        v.co = Vector(ub((x + c[0], y + c[1], z + c[2])))
    bmesh.ops.recalc_face_normals(bm, faces=[f for f in bm.faces])

def torus(bm, c, R, r, mat, seg=24, sides=6):
    """Тор в плоскости XZ Unity (ось — вверх по Y узла), центр c в координатах Unity."""
    rings = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        col = []
        for j in range(sides):
            b = 2 * math.pi * j / sides
            rr = R + r * math.cos(b)
            col.append(bm.verts.new(ub((c[0] + rr * math.cos(a), c[1] + r * math.sin(b), c[2] + rr * math.sin(a)))))
        rings.append(col)
    faces = []
    for i in range(seg):
        for j in range(sides):
            f = bm.faces.new((rings[i][j], rings[(i + 1) % seg][j], rings[(i + 1) % seg][(j + 1) % sides], rings[i][(j + 1) % sides]))
            f.material_index = mat
            f.tag = True
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)

def remove_faces(me, keep_fn):
    bm = bmesh.new()
    bm.from_mesh(me)
    kill = [f for f in bm.faces if not keep_fn(f)]
    bmesh.ops.delete(bm, geom=kill, context='FACES')
    bm.to_mesh(me)
    bm.free()

def unity_centroid(f):
    c = f.calc_center_median()
    return (c.x, c.z, c.y)

def cushion_shape(u, h, w, p):
    # верх сиденья чуть проседает в середине, передний край скруглён вниз
    if h > 0:
        p.y -= 0.012 * (1 - abs(u)) * (1 - abs(w))
    return p

def seat_bottom(bm, me, x, z, width):
    tan, blk = mat_index(me, C["insert"]), mat_index(me, C["bolster"])
    y = C["seat_y"]
    cage(bm, (x, y, z), (width * 0.56, 0.11, 0.52), tan, shape=cushion_shape)                        # вставка
    for s in (-1, 1):
        cage(bm, (x + s * (width * 0.5 - 0.06), y + 0.03, z - 0.01), (0.12, 0.17, 0.54), blk, yaw=s * 4)     # валики
    cage(bm, (x, y - 0.055, z + 0.02), (width, 0.06, 0.56), blk, cuts=1)                                    # основание
    if C.get("piping"):                                                                                      # стёжка поперёк вставки
        pip = mat_index(me, C["piping"])
        for k in range(4):
            cage(bm, (x, y + 0.053, z - 0.18 + k * 0.12), (width * 0.5, 0.008, 0.008), pip, cuts=0)

def seat_back(bm, me, width):
    """Спинка в системе узла SeatBack (низ — у шарнира, вверх по y, лицо — +z)."""
    tan, blk, ch = mat_index(me, C["insert"]), mat_index(me, C["bolster"]), mat_index(me, "chrome")
    cage(bm, (0, 0.33, 0.0), (width * 0.56, 0.58, 0.09), tan)
    for s in (-1, 1):
        cage(bm, (s * (width * 0.5 - 0.06), 0.31, 0.02), (0.12, 0.58, 0.16), blk, yaw=-s * 14)
    cage(bm, (0, 0.33, -0.05), (width, 0.66, 0.08), blk, cuts=1)                       # задняя панель
    cage(bm, (0, 0.62, 0.01), (width * 0.9, 0.08, 0.12), blk)                           # плечи
    cage(bm, (0, 0.82, -0.01), (0.27, 0.18, 0.11), blk)                                 # подголовник
    if C.get("piping"):
        pip = mat_index(me, C["piping"])
        for k in range(5):
            cage(bm, (0, 0.12 + k * 0.1, 0.048), (width * 0.5, 0.008, 0.008), pip, cuts=0)
    for s in (-1, 1):                                                                    # хромированные стойки
        bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.006, radius2=0.006, depth=0.08,
                              matrix=Matrix.Translation(Vector(ub((s * 0.07, 0.70, -0.01)))))
    for f in bm.faces:
        if not f.tag:
            f.tag = True
            f.material_index = ch
    if C.get("back_h"):                       # у седана спинки ниже — иначе подголовник упирается в крышу
        for v in bm.verts:
            v.co.z *= C["back_h"]

def rebuild_seats():
    inter = by_name["Interior"].data
    def keep(f):
        mname = inter.materials[f.material_index].name if f.material_index < len(inter.materials) else ""
        if mname != C["seat_old"]:
            return True
        x, y, z = unity_centroid(f)
        z0, z1, ymax = C["front_cut"]
        front = z0 < z < z1 and abs(abs(x) - C["seat_x"]) < 0.33 and y < ymax
        z0, z1, ymax = C["rear_cut"]
        rear = z0 < z < z1 and abs(x) < 0.82 and y < ymax
        return not (front or rear)
    remove_faces(inter, keep)
    sx = C["seat_x"]
    for x in (-sx, sx):
        subd_into(inter, lambda bm, x=x: seat_bottom(bm, inter, x, C["seat_z"], C["seat_w"]))
    # задний диван: три места
    for x in C["rear_xs"]:
        subd_into(inter, lambda bm, x=x: seat_bottom(bm, inter, x, C["rear_z"], C["rear_w"]))
    for name in ("SeatBackL", "SeatBackR"):
        me = by_name[name].data
        remove_faces(me, lambda f: False)
        subd_into(me, lambda bm, me=me: seat_back(bm, me, C["seat_w"]))
    rb = by_name["RearBack"].data
    remove_faces(rb, lambda f: False)
    def rear_back(bm):
        tan, blk = mat_index(rb, C["insert"]), mat_index(rb, C["bolster"])
        for x in C["rear_xs"]:
            cage(bm, (x, 0.33, 0.0), (0.26, 0.56, 0.09), tan)
            for s in (-1, 1):
                cage(bm, (x + s * 0.17, 0.32, 0.015), (0.10, 0.56, 0.13), blk, yaw=-s * 8)
            cage(bm, (x, C.get("rear_head", 0.76), -0.01), (0.25, 0.15, 0.10), blk)
        cage(bm, (0, 0.33, -0.05), (C["rear_back_w"], 0.66, 0.08), blk, cuts=1)
        if C.get("back_h"):
            for v in bm.verts:
                v.co.z *= C["back_h"]
    subd_into(rb, rear_back)

def rebuild_wheel():
    """Руль М: толстый обод, три спицы, крупная подушка, трёхцветная полоска. Ось — Y узла (Blender Z), к водителю +Y."""
    me = by_name["SteeringWheel"].data
    remove_faces(me, lambda f: False)
    blk, lth, grey, ch = (mat_index(me, k) for k in ("int_black", "leather_black", "int_grey", "chrome"))
    st1, st2, st3 = (mat_index(me, k) for k in ("stripe", "stripe_dark", "leather_red"))
    bm = bmesh.new()
    R, seg, sides = 0.18, 72, 14
    ring = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        col = []
        for j in range(sides):
            b = 2 * math.pi * j / sides
            r = R + 0.016 * math.cos(b)            # сечение — овал: толще вдоль оси
            h = 0.021 * math.sin(b)
            col.append(bm.verts.new(ub((r * math.cos(a), h, r * math.sin(a)))))
        ring.append(col)
    for i in range(seg):
        a = 360 * (i + 0.5) / seg
        # снизу (−z в Unity — к коленям) полоска М: голубой, синий, красный
        mat = lth
        d = (a - 270 + 540) % 360 - 180
        if C["wheel"] == "m":
            if abs(d) < 2.5: mat = st2
            elif -7.5 < d < -2.5: mat = st1
            elif 2.5 < d < 7.5: mat = st3
        for j in range(sides):
            f = bm.faces.new((ring[i][j], ring[(i + 1) % seg][j], ring[(i + 1) % seg][(j + 1) % sides], ring[i][(j + 1) % sides]))
            f.material_index = mat
            f.smooth = True
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for f in bm.faces:
        f.tag = True
    tmp = bpy.data.meshes.new("ring")
    for m in me.materials:
        tmp.materials.append(m)
    bm.to_mesh(tmp)
    bm.free()
    tb = bmesh.new()
    tb.from_mesh(me)
    tb.from_mesh(tmp)
    tb.to_mesh(me)
    tb.free()
    bpy.data.meshes.remove(tmp)

    def parts_audi(bm):
        """Руль Audi: восьмиугольная подушка с кольцами, горизонтальные спицы с серебристой рамкой, двойная нижняя спица."""
        cage(bm, (0, 0.022, -0.004), (0.14, 0.06, 0.105), lth, shape=lambda u, h, w, p: Vector((p.x * (1 - 0.18 * max(0, -w)), p.y, p.z)))
        for s in (-1, 1):
            cage(bm, (s * 0.115, 0.012, -0.004), (0.11, 0.024, 0.06), blk, cuts=1)          # спица
            cage(bm, (s * 0.112, 0.026, -0.004), (0.085, 0.004, 0.048), ch, cuts=1)         # серебристая рамка
            cage(bm, (s * 0.112, 0.029, -0.004), (0.07, 0.004, 0.036), blk, cuts=1)         # кнопки
            cage(bm, (s * 0.04, 0.008, -0.10), (0.014, 0.02, 0.11), ch, cuts=1, yaw=-s * 12)  # нижняя спица — серебристая «V»
        cage(bm, (0, -0.15, -0.0), (0.07, 0.30, 0.07), blk, cuts=1)                       # колонка
        for k in range(4):                                                                  # кольца на подушке
            torus(bm, ((k - 1.5) * 0.017, 0.054, 0.006), 0.0115, 0.0017, ch)

    def parts(bm):
        cage(bm, (0, 0.022, -0.008), (0.135, 0.06, 0.115), lth)                          # подушка
        cage(bm, (0, 0.05, -0.008), (0.10, 0.012, 0.08), blk, cuts=1)                    # накладка
        for s in (-1, 1):
            cage(bm, (s * 0.11, 0.01, -0.012), (0.12, 0.022, 0.05), blk, cuts=1)          # боковые спицы
            cage(bm, (s * 0.11, 0.022, -0.012), (0.07, 0.006, 0.012), grey, cuts=1)       # кнопки
        for s in (-1, 1):
            cage(bm, (s * 0.022, 0.006, -0.12), (0.018, 0.018, 0.12), blk, cuts=1)        # нижняя «двойная» спица
        cage(bm, (0, -0.15, -0.0), (0.07, 0.30, 0.07), blk, cuts=1)                       # колонка
    subd_into(me, parts_audi if C["wheel"] == "audi" else parts)
    me.set_sharp_from_angle(angle=math.radians(50))

def add_idrive():
    me = by_name["Interior"].data
    ch, blk = mat_index(me, "chrome"), mat_index(me, "int_black")
    bm = bmesh.new()
    bm.from_mesh(me)
    for r, h, y, m in ((0.034, 0.018, 1.005, ch), (0.026, 0.008, 1.018, blk)):
        res = bmesh.ops.create_cone(bm, cap_ends=True, segments=32, radius1=r, radius2=r, depth=h,
                                    matrix=Matrix.Translation(Vector(ub((0.0, y, -0.30)))))
        for v in res["verts"]:
            for f in v.link_faces:
                f.material_index = m
                f.smooth = True
    bm.to_mesh(me)
    bm.free()

def dark_screens():
    """Экраны щитка и iDrive — почти чёрное стекло (цифры и радио игра рисует поверх светящимся текстом)."""
    for name in ("ClusterScreen", "RadioScreen", "ClimateScreen"):
        ob = by_name.get(name)
        if ob is None or ob.type != 'MESH':
            continue
        for i, m in enumerate(ob.data.materials):
            if m and m.name == "screen_blue":
                ob.data.materials[i] = gq_materials.get_mat("screen_dark")

def dash_details():
    """Центральная консоль: панель с двумя прямоугольными дефлекторами, блок климата с кнопками, аварийка."""
    me = by_name["Interior"].data
    blk, grey, ch, face, grille = (mat_index(me, k) for k in ("int_black", "int_grey", "chrome", "gauge_face", "grille"))
    red = mat_index(me, "tail_red")
    def build(bm):
        cage(bm, (0, 0.93, 0.575), (0.32, 0.30, 0.05), blk, cuts=1)                     # панель консоли
        for x in (-0.075, 0.075):
            cage(bm, (x, 1.04, 0.548), (0.12, 0.055, 0.012), grille, cuts=1)              # дефлекторы
            for k in range(3):
                cage(bm, (x, 1.025 + k * 0.016, 0.544), (0.112, 0.004, 0.006), ch, cuts=0)
        cage(bm, (0, 0.995, 0.546), (0.02, 0.016, 0.01), red, cuts=0)                     # аварийка
        cage(bm, (0, 0.90, 0.547), (0.27, 0.075, 0.012), face, cuts=1)                    # климат
        for i in range(6):
            cage(bm, (-0.1 + i * 0.04, 0.875, 0.541), (0.026, 0.012, 0.008), grey, cuts=0)
        for x in (-0.11, 0.11):                                                            # крутилки температуры
            res = bmesh.ops.create_cone(bm, cap_ends=True, segments=20, radius1=0.016, radius2=0.016, depth=0.018,
                                        matrix=Matrix.Translation(Vector(ub((x, 0.915, 0.538)))) @ Matrix.Rotation(math.pi / 2, 4, 'X'))
            for v in res["verts"]:
                for f in v.link_faces:
                    f.material_index = ch
                    f.tag = True
    subd_into(me, build, level=1)

def door_details():
    """Обивка дверей: хромированная ручка, решётка динамика, карман."""
    bpy.context.view_layer.update()
    for name in ("DoorFL", "DoorFR", "DoorRL", "DoorRR"):
        ob = by_name.get(name)
        if ob is None:
            continue
        me = ob.data
        side = -1 if name.endswith("L") else 1
        front = name.startswith("DoorF")
        z = C["door_z"][0] if front else C["door_z"][1]
        yh, ys, yp = C["door_y"]
        dx = C["door_x"]
        ch, grille, blk = (mat_index(me, k) for k in ("chrome", "grille", C["door_mat"]))
        inv = ob.matrix_world.inverted()
        def build(bm):
            cage(bm, (dx * side, yh, z + 0.12), (0.02, 0.03, 0.12), ch, cuts=1)                  # ручка
            cage(bm, ((dx + 0.015) * side, ys, z + 0.05), (0.02, 0.17, 0.17), grille, cuts=1)     # динамик
            cage(bm, ((dx + 0.01) * side, yp, z - 0.05), (0.035, 0.07, 0.38), blk, cuts=1)        # карман
            bmesh.ops.transform(bm, matrix=inv, verts=bm.verts[:])
        subd_into(me, build, level=1)

rebuild_seats()
rebuild_wheel()
if C["idrive"]:
    add_idrive()
dark_screens()
if C["dash"]:
    dash_details()
door_details()
print("interior rebuilt")

for tag in ("FL", "FR", "RL", "RR"):
    ob = by_name.get(("Steer" if tag[0] == "F" else "Mount") + tag)
    if ob is not None and ob.type == 'MESH':
        build_caliper(ob.data, -1 if tag[1] == "L" else 1)
print("calipers rebuilt")


# ---------- Запекание затенения (AO) в вершины ----------
# Тень в щелях панелей, арках, под ручками, в салоне. Модификаторы применяются (выгружается то же самое),
# затенение пишется в цветовой атрибут «AO» по углам граней; в игре его показывает шейдер GasQueue/CarAO.
def bake_ao():
    deps = bpy.context.evaluated_depsgraph_get()
    meshes = [o for o in objs if o.type == 'MESH']
    for ob in meshes:
        new = bpy.data.meshes.new_from_object(ob.evaluated_get(deps), preserve_all_data_layers=True, depsgraph=deps)
        ob.modifiers.clear()
        ob.vertex_groups.clear()
        old = ob.data
        ob.data = new
        if old.users == 0:
            bpy.data.meshes.remove(old)
    for ob in meshes:
        attr = ob.data.color_attributes.new("AO", 'BYTE_COLOR', 'CORNER')
        ob.data.color_attributes.active_color = attr
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = int(os.environ.get("AO_SAMPLES", "96"))
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences
        prefs.compute_device_type = 'METAL'
        prefs.get_devices()
        for d in prefs.devices:
            d.use = True
        scene.cycles.device = 'GPU'
    except Exception:
        pass
    if scene.world is None:
        scene.world = bpy.data.worlds.new("World")
    scene.world.light_settings.distance = 0.5        # дальность затенения, м
    for o in bpy.context.view_layer.objects:
        o.select_set(o in meshes)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.bake(type='AO', target='VERTEX_COLORS')
    vals = [c.color[0] for c in meshes[0].data.color_attributes["AO"].data]
    print("AO baked; sample mean", round(sum(vals) / max(1, len(vals)), 3))

# Затенение в игре пока выключено: шейдер с ним гасил отражения хрома (см. CLAUDE.md). Включить: AO=1
WITH_AO = os.environ.get("AO") == "1"
if WITH_AO:
    bake_ao()

# ---------- Выгрузка в игру ----------
def write_string(f, s):
    b = s.encode("utf-8")
    n = len(b)
    while True:  # длина как в BinaryWriter (7-битная)
        if n < 0x80:
            f.write(bytes([n]))
            break
        f.write(bytes([(n & 0x7F) | 0x80]))
        n >>= 7
    f.write(b)

deps = bpy.context.evaluated_depsgraph_get()
buf = io.BytesIO()
buf.write(b"GQM2" if WITH_AO else b"GQM1")
order = sorted(objs, key=lambda o: o["gq_index"])
buf.write(struct.pack("<i", len(order)))
tris_total = 0
for ob in order:
    write_string(buf, ob["gq_name"])
    parent = ob.parent["gq_index"] if ob.parent else -1
    buf.write(struct.pack("<i", parent))
    buf.write(struct.pack("<3f", *ob["gq_pos"]))
    buf.write(struct.pack("<3f", *ob["gq_euler"]))
    if ob.type != 'MESH':
        buf.write(struct.pack("<i", 0))
        continue
    ev = ob.evaluated_get(deps)
    me = ev.to_mesh()
    me.calc_loop_triangles()
    normals = [cn.vector.copy() for cn in me.corner_normals]
    uvl = me.uv_layers.get("UV") or (me.uv_layers[0] if me.uv_layers else None)
    aol = me.color_attributes.get("AO")
    groups = {}
    for lt in me.loop_triangles:
        groups.setdefault(lt.material_index, []).append(lt)
    meshes = []
    for mi, lts in sorted(groups.items()):
        key = me.materials[mi].name if mi < len(me.materials) and me.materials[mi] else "black"
        vmap, vdata, idx = {}, [], []
        for lt in lts:
            tri = []
            for li in lt.loops:
                vi = me.loops[li].vertex_index
                n = normals[li]
                uv = tuple(uvl.data[li].uv) if uvl else (0.0, 0.0)
                ao = aol.data[li].color[0] if aol is not None and aol.domain == 'CORNER' else (aol.data[vi].color[0] if aol is not None else 1.0)
                aob = max(0, min(255, int(round(ao * 255))))
                k = (vi, round(n.x, 3), round(n.y, 3), round(n.z, 3), round(uv[0], 4), round(uv[1], 4), aob)
                j = vmap.get(k)
                if j is None:
                    j = len(vdata)
                    vmap[k] = j
                    co = me.vertices[vi].co
                    vdata.append((co.x, co.z, co.y, n.x, n.z, n.y, uv[0], uv[1], aob))  # Blender → Unity
                tri.append(j)
            idx.extend((tri[0], tri[2], tri[1]))  # обратно к обходу Unity
        meshes.append((key, vdata, idx))
        tris_total += len(idx) // 3
    buf.write(struct.pack("<i", len(meshes)))
    for key, vdata, idx in meshes:
        write_string(buf, key)
        buf.write(struct.pack("<i", len(vdata)))
        for v in vdata:
            if WITH_AO:
                buf.write(struct.pack("<8fB", *v))
            else:
                buf.write(struct.pack("<8f", *v[:8]))
        buf.write(struct.pack("<i", len(idx)))
        buf.write(struct.pack("<%di" % len(idx), *idx))
    ev.to_mesh_clear()

os.makedirs(os.path.dirname(os.path.abspath(out_bytes)), exist_ok=True)
with open(out_bytes, "wb") as f:
    f.write(gzip.compress(buf.getvalue(), 9))
print("EXPORTED", out_bytes, "triangles", tris_total, "bytes", os.path.getsize(out_bytes))

# ---------- Сцена для просмотра ----------
root = order[0]
bpy.ops.mesh.primitive_plane_add(size=30, location=(0, 0, 0))
bpy.context.active_object.name = "Ground"
for name, loc, energy in [("KeyLight", (4, 5, 6), 800), ("FillLight", (-5, -3, 4), 400)]:
    ld = bpy.data.lights.new(name, 'AREA')
    ld.size = 4
    ld.energy = energy
    lo = bpy.data.objects.new(name, ld)
    lo.location = loc
    lo.rotation_euler = (Vector((0, 0, 0.8)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(lo)
bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(out_blend))
print("SAVED", out_blend)

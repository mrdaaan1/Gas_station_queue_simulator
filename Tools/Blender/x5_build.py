# Доводка X5 в Blender и выгрузка в игру.
#   1) Импорт дерева узлов модели из кода (ModelPreview.dll tree x5 x5_tree.json): каждый узел — объект Blender
#      с тем же шарниром (капот, двери, бамперы, колёса…), координаты Unity → Blender: (x, y, z) → (x, z, y).
#   2) Доводка: панели кузова получают толщину и скруглённые кромки (видны щели между панелями), стёкла и линзы —
#      объём; диски заменяются на детальные 20" с Y-образными двойными спицами; сглаживание с острыми кромками.
#   3) Выгрузка в Assets/Resources/Models/X5.bytes (формат GQM1, см. Assets/Scripts/World/Models/ModelFile.cs)
#      и сохранение сцены .blend, чтобы её можно было открыть и покрутить.
# Запуск:
#   Blender --background --factory-startup --python x5_build.py -- x5_tree.json X5.bytes x5.blend [camo.png] [decal.png]
import bpy, bmesh, gzip, io, json, math, os, struct, sys
from mathutils import Matrix, Vector

argv = sys.argv[sys.argv.index("--") + 1:]
tree_path, out_bytes, out_blend = argv[0], argv[1], argv[2]
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gq_materials
gq_materials.camo_path = argv[3] if len(argv) > 3 else None
gq_materials.decal_path = argv[4] if len(argv) > 4 else None

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
    # Убираем старый обод, спицы и ступицу (шина, протектор и тормозной диск остаются)
    old = {mat_index(me, k) for k in ("alloy", "chrome", "rim_inner")}
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index in old], context='FACES')
    i_alloy, i_chrome, i_inner, i_black = (mat_index(me, k) for k in ("alloy", "chrome", "rim_inner", "black"))
    R, HW = 0.262, 0.1475
    face = HW - 0.014

    def lathe(profile, mat, seg=72):
        """Тело вращения вокруг Z: профиль (радиус, z)."""
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

    # Полированная закраина обода и «бочка»
    lathe([(R - 0.020, face - 0.010), (R - 0.006, face + 0.002), (R + 0.006, face + 0.004), (R + 0.010, face - 0.004), (R + 0.004, face - 0.012)], i_chrome)
    lathe([(R - 0.012, -HW + 0.012), (R - 0.016, face - 0.012), (R - 0.020, face - 0.010)], i_inner)
    lathe([(0.10, -HW + 0.03), (R - 0.012, -HW + 0.012)], i_inner)
    # Ступица, колпачок с кольцом
    lathe([(0.0, face - 0.004), (0.060, face - 0.010), (0.075, face - 0.030)], i_alloy, 48)
    lathe([(0.0, face + 0.006), (0.030, face + 0.004), (0.034, face - 0.004)], i_black, 36)
    lathe([(0.034, face - 0.004), (0.036, face + 0.003), (0.040, face + 0.0), (0.040, face - 0.006)], i_chrome, 36)

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
        for q in quads:
            f = bm.faces.new(q)
            f.material_index = i_alloy
            new_faces.append(f)
        bmesh.ops.recalc_face_normals(bm, faces=new_faces)
        edges = list({e for f in new_faces for e in f.edges})
        bmesh.ops.bevel(bm, geom=edges, offset=0.0025, segments=2, profile=0.5, affect='EDGES', clamp_overlap=True)

    for k in range(5):
        a = 2 * math.pi * k / 5 + math.pi / 2
        spoke(a, a, 0.055, 0.150, 0.046, 0.036, 0.030)              # ствол
        for side in (-1, 1):                                          # две ветки к ободу
            spoke(a, a + side * math.radians(13), 0.140, R - 0.016, 0.026, 0.022, 0.026)
        # Гайка
        an = a + math.pi / 5
        c = Vector((0.046 * math.cos(an), 0.046 * math.sin(an), face - 0.006))
        bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=0.0085, radius2=0.0085, depth=0.012,
                              matrix=Matrix.Translation(c))
    for f in bm.faces:
        if f.material_index not in (i_alloy, i_chrome, i_inner, i_black):
            continue
        f.smooth = True
    bm.to_mesh(me)
    bm.free()
    me.set_sharp_from_angle(angle=math.radians(40))

for tag in ("FL", "FR", "RL", "RR"):
    ob = by_name.get("Rim" + tag)
    if ob is not None and ob.type == 'MESH':
        build_rim(ob.data)

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
buf.write(b"GQM1")
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
                k = (vi, round(n.x, 3), round(n.y, 3), round(n.z, 3), round(uv[0], 4), round(uv[1], 4))
                j = vmap.get(k)
                if j is None:
                    j = len(vdata)
                    vmap[k] = j
                    co = me.vertices[vi].co
                    vdata.append((co.x, co.z, co.y, n.x, n.z, n.y, uv[0], uv[1]))  # Blender → Unity
                tri.append(j)
            idx.extend((tri[0], tri[2], tri[1]))  # обратно к обходу Unity
        meshes.append((key, vdata, idx))
        tris_total += len(idx) // 3
    buf.write(struct.pack("<i", len(meshes)))
    for key, vdata, idx in meshes:
        write_string(buf, key)
        buf.write(struct.pack("<i", len(vdata)))
        for v in vdata:
            buf.write(struct.pack("<8f", *v))
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

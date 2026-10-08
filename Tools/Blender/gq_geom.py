# Построение деталей в Blender (общее для car_build.py и mater_build.py).
# Координаты деталей задаются как в Unity (x вправо, y вверх, z вперёд) в системе узла; в Blender — (x, z, y).
# «Клетки» — коробки с разрезами, которые сглаживает Subdivision: получаются мягкие формы (кожа, мультяшный кузов).
import bpy, bmesh, math
from mathutils import Matrix, Vector
import gq_materials
from gq_export import ub

def mat_index(me, key):
    for i, m in enumerate(me.materials):
        if m and m.name == key:
            return i
    me.materials.append(gq_materials.get_mat(key))
    return len(me.materials) - 1

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
    bpy.context.scene.collection.objects.link(ob)
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

def cage(bm, c, size, mat, yaw=0.0, pitch=0.0, cuts=2, shape=None, roll=0.0):
    """Коробка-клетка: центр c и размеры size в координатах Unity узла; yaw — поворот вокруг вертикали (градусы),
    pitch — наклон вокруг поперечной оси, roll — крен вокруг продольной; shape(u, v, w, p) может сдвинуть вершину (u, v, w — от −1 до 1 по осям)."""
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
    yr, pr, rr = math.radians(yaw), math.radians(pitch), math.radians(roll)
    for v in new_verts:
        u, w_, h = v.co.x * 2, v.co.y * 2, v.co.z * 2       # create_cube: −0.5…0.5 → −1…1 (Blender: x, y=вперёд, z=вверх)
        p = Vector((u * size[0] / 2, h * size[1] / 2, w_ * size[2] / 2))  # Unity: x, y (вверх), z (вперёд)
        if shape:
            p = shape(u, h, w_, p)
        # крен вокруг Z, наклон вокруг X (Unity), потом поворот вокруг вертикали
        if rr:
            p = Vector((p.x * math.cos(rr) - p.y * math.sin(rr), p.x * math.sin(rr) + p.y * math.cos(rr), p.z))
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


def resample(pts, step):
    """Частые сечения вдоль ломаной (иначе Subdivision стягивает длинный брус в «весло») + у концов по короткому."""
    out = [Vector(pts[0])]
    for i in range(len(pts) - 1):
        a, b = Vector(pts[i]), Vector(pts[i + 1])
        n = max(1, int(math.ceil((b - a).length / step)))
        for k in range(1, n + 1):
            out.append(a.lerp(b, k / n))
    if len(out) >= 2:
        e = min(step * 0.25, (out[1] - out[0]).length * 0.3)
        out.insert(1, out[0] + (out[1] - out[0]).normalized() * e)
        out.insert(len(out) - 1, out[-1] + (out[-2] - out[-1]).normalized() * e)
    return [tuple(p) for p in out]

def sweep(bm, pts, w, h, mat, up=(0, 1, 0), closed=False, round_=True):
    """Брус сечением w × h (Unity: w — поперёк вбок, h — по up), протянутый по точкам pts. Под Subdivision — гладкий жгут."""
    pts = resample(pts, max(w, h) * 0.8) if not closed else pts
    ring = []
    n = len(pts)
    upv = Vector(up)
    for i, p in enumerate(pts):
        p = Vector(p)
        a = Vector(pts[max(0, i - 1)]) if not closed else Vector(pts[(i - 1) % n])
        b = Vector(pts[min(n - 1, i + 1)]) if not closed else Vector(pts[(i + 1) % n])
        t = (b - a).normalized()
        side = t.cross(upv)
        if side.length < 1e-6:
            side = t.cross(Vector((0, 0, 1)))
        side.normalize()
        u2 = side.cross(t).normalized()
        corners = []
        for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            q = p + side * (w / 2 * sx) + u2 * (h / 2 * sy)
            corners.append(bm.verts.new(ub(q)))
        ring.append(corners)
    faces = []
    m = n if closed else n - 1
    for i in range(m):
        r0, r1 = ring[i], ring[(i + 1) % n]
        for j in range(4):
            faces.append(bm.faces.new((r0[j], r1[j], r1[(j + 1) % 4], r0[(j + 1) % 4])))
    if not closed:
        faces.append(bm.faces.new(list(reversed(ring[0]))))
        faces.append(bm.faces.new(ring[-1]))
    for f in faces:
        f.material_index = mat
        f.tag = True
    bmesh.ops.recalc_face_normals(bm, faces=faces)

def lathe(bm, profile, mat, seg=48, center=(0, 0, 0), axis="z", cap=False, inside=None):
    """Тело вращения: профиль (радиус, высота) вокруг оси узла (Unity axis: "x", "y" или "z"), центр в Unity.
    inside — точка сечения (радиус, высота), от которой грани смотрят наружу (в игре видна только лицевая сторона)."""
    rings = []
    for k in range(seg):
        a = 2 * math.pi * k / seg
        ca, sa = math.cos(a), math.sin(a)
        ring = []
        for r, h in profile:
            if axis == "z":
                p = (r * ca, r * sa, h)
            elif axis == "y":
                p = (r * ca, h, r * sa)
            else:
                p = (h, r * ca, r * sa)
            ring.append(bm.verts.new(ub((p[0] + center[0], p[1] + center[1], p[2] + center[2]))))
        rings.append(ring)
    faces = []
    for k in range(seg):
        r0, r1 = rings[k], rings[(k + 1) % seg]
        for j in range(len(profile) - 1):
            try:
                faces.append(bm.faces.new((r0[j], r1[j], r1[j + 1], r0[j + 1])))
            except ValueError:
                pass
    for f in faces:
        f.material_index = mat
        f.tag = True
        f.smooth = True
    if inside is None:
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        return faces
    # сечение грани: радиус от оси и высота вдоль оси (в координатах Unity)
    ax = {"x": 0, "y": 1, "z": 2}[axis]
    for f in faces:
        f.normal_update()
        cb = f.calc_center_median()
        cu = [cb.x - center[0], cb.z - center[1], cb.y - center[2]]
        nu = [f.normal.x, f.normal.z, f.normal.y]
        h = cu[ax]
        rv = [0.0 if i == ax else cu[i] for i in range(3)]
        rl = math.sqrt(sum(c * c for c in rv))
        radial = [c / rl for c in rv] if rl > 1e-7 else [0.0, 0.0, 0.0]
        d = [radial[i] * (rl - inside[0]) for i in range(3)]
        d[ax] += h - inside[1]
        if sum(nu[i] * d[i] for i in range(3)) < 0:
            f.normal_flip()
    return faces

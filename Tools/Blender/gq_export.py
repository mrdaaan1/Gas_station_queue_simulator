# Выгрузка модели из Blender в игру: формат GQM1/GQM2 + gzip (читает Assets/Scripts/World/Models/ModelFile.cs).
# Общее для car_build.py (машины из кода) и mater_build.py (Мэтр целиком из Blender).
# Координаты: Unity (x, y, z) ↔ Blender (x, z, y); поворот узла — как Frame.Rot в игре.
import bpy, gzip, io, math, os, struct
from mathutils import Matrix, Vector

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

def write_gqm(objs, out_bytes, WITH_AO=False):
    """Выгрузить узлы (объекты с gq_index/gq_name/gq_pos/gq_euler) в формат GQM1 (GQM2 — с затенением)."""
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

    return order

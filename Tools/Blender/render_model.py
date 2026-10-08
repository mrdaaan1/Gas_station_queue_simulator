# Рендер модели машины в Blender (фотореалистично, с отражениями) — для проверки формы и материалов.
# Модель — JSON из Tools/ModelPreview (ModelPreview.dll <машина> model.json).
# Запуск:
#   Blender --background --factory-startup --python render_model.py -- model.json out_dir [camo.png] [decal.png] [engine]
# Результат: out_dir/<ракурс>.png и out_dir/grid.png (6 ракурсов).
import bpy, bmesh, json, math, os, sys
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
model_path, out_dir = argv[0], argv[1]
camo_path = argv[2] if len(argv) > 2 and argv[2] != "-" else None
decal_path = argv[3] if len(argv) > 3 and argv[3] != "-" else None
engine = argv[4] if len(argv) > 4 else "CYCLES"
os.makedirs(out_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gq_materials
gq_materials.camo_path, gq_materials.decal_path = camo_path, decal_path
gq_materials.tex_dir = os.environ.get("TEXDIR")   # текстуры Мэтра
get_mat, principled = gq_materials.get_mat, gq_materials.principled

# ---------- Геометрия ----------
# JSON: координаты Unity с Z, перевёрнутым для three.js. В Blender: X → X, Y (вперёд) = −z_json, Z (вверх) = y.
data = json.load(open(model_path))
car = bpy.data.objects.new("Car", None)
scene.collection.objects.link(car)
hide = [h for h in os.environ.get("HIDE", "").split(",") if h]
for k, m in enumerate(data["meshes"]):
    if any(m["node"].startswith(h) for h in hide):
        continue
    p, idx = m["p"], m["i"]
    verts = [(p[i], -p[i + 2], p[i + 1]) for i in range(0, len(p), 3)]
    faces = [(idx[i], idx[i + 1], idx[i + 2]) for i in range(0, len(idx), 3)]
    me = bpy.data.meshes.new(f"{m['node']}_{m['mat']}")
    me.from_pydata(verts, [], faces)
    if m.get("uv"):
        uvl = me.uv_layers.new(name="UV")
        uv = m["uv"]
        for poly in me.polygons:
            for li in poly.loop_indices:
                vi = me.loops[li].vertex_index
                uvl.data[li].uv = (uv[vi * 2], uv[vi * 2 + 1])
    me.validate()
    for poly in me.polygons:
        poly.use_smooth = True
    if os.environ.get("AOVIEW") and m.get("ao"):
        # Проверка запечённого затенения: модель окрашена значениями AO
        ao = m["ao"]
        ca = me.color_attributes.new("AO", 'FLOAT_COLOR', 'POINT')
        for i, c in enumerate(ca.data):
            c.color = (ao[i], ao[i], ao[i], 1.0)
        if "aoview" not in bpy.data.materials:
            am = bpy.data.materials.new("aoview")
            am.use_nodes = True
            nt = am.node_tree
            attr = nt.nodes.new("ShaderNodeAttribute")
            attr.attribute_name = "AO"
            em = nt.nodes.new("ShaderNodeEmission")
            nt.links.new(attr.outputs["Color"], em.inputs["Color"])
            nt.links.new(em.outputs["Emission"], nt.nodes["Material Output"].inputs["Surface"])
        me.materials.append(bpy.data.materials["aoview"])
    else:
        me.materials.append(get_mat(m["mat"]))
    ob = bpy.data.objects.new(me.name, me)
    ob.parent = car
    scene.collection.objects.link(ob)
    # Сглаживание по углу: острые рёбра (решётки, рамки) остаются острыми
    try:
        ob.data.set_sharp_from_angle(angle=math.radians(40))
    except Exception:
        pass

# ---------- Сцена: земля, небо, солнце ----------
bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
ground = bpy.context.active_object
ground.data.materials.append(principled("ground", '#5c5f63', 0.8, 0.0))

world = bpy.data.worlds.new("World")
scene.world = world
world.use_nodes = True
wn = world.node_tree
sky = wn.nodes.new("ShaderNodeTexSky")
try:
    sky.sky_type = 'MULTIPLE_SCATTERING'
except Exception:
    try:
        sky.sky_type = 'NISHITA'
    except Exception:
        pass
try:
    sky.sun_elevation = math.radians(28)
    sky.sun_rotation = math.radians(140)
except Exception:
    pass
bg = wn.nodes["Background"]
bg.inputs["Strength"].default_value = 0.12
wn.links.new(sky.outputs["Color"], bg.inputs["Color"])

sun_data = bpy.data.lights.new("Sun", 'SUN')
sun_data.energy = 2.5
sun_data.angle = math.radians(2)
sun = bpy.data.objects.new("Sun", sun_data)
sun.rotation_euler = (math.radians(55), 0, math.radians(140))
scene.collection.objects.link(sun)

# Студийные «софтбоксы» сверху и сбоку — чтобы хром было чем отражать
# Софтбоксы видны в отражениях (как в фотостудии): длинные яркие полосы на хроме
for name, loc, size, energy in [("BoxTop", (0, 0, 5), 5, 900), ("BoxL", (-7, 3, 2.2), 5, 900), ("BoxR", (7, -3, 2.2), 5, 900), ("BoxF", (2, 8, 2.5), 4, 500)]:
    ld = bpy.data.lights.new(name, 'AREA')
    ld.shape = 'RECTANGLE'
    ld.size = size
    ld.size_y = size * 0.35
    ld.energy = energy
    lo = bpy.data.objects.new(name, ld)
    lo.location = loc
    scene.collection.objects.link(lo)
    try:
        lo.visible_glossy = True
    except Exception:
        pass
    d = Vector((0, 0, 0.8)) - Vector(loc)
    lo.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()

# Свет в салоне (в игре салон освещён общим светом сцены, а в студии сквозь тонировку темно)
if os.environ.get("CABLIGHT"):
    ld = bpy.data.lights.new("Cabin", 'AREA')
    ld.size = 1.2
    ld.energy = 60
    lo = bpy.data.objects.new("Cabin", ld)
    lo.location = (0, -0.4, 1.62)
    scene.collection.objects.link(lo)

# ---------- Рендер ----------
scene.render.engine = 'BLENDER_EEVEE' if engine == 'EEVEE' else 'CYCLES'
if scene.render.engine == 'CYCLES':
    scene.cycles.samples = int(os.environ.get("SAMPLES", "48"))
    scene.cycles.use_denoising = True
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences
        prefs.compute_device_type = 'METAL'
        prefs.get_devices()
        for d in prefs.devices:
            d.use = True
        scene.cycles.device = 'GPU'
    except Exception:
        pass
scene.render.resolution_x = int(os.environ.get("W", "700"))
scene.render.resolution_y = int(os.environ.get("H", "430"))
scene.render.film_transparent = False
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'AgX - Punchy'

cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

def unity(v):  # точка в координатах Unity → Blender
    return Vector((v[0], v[2], v[1]))

VIEWS = [
    ("fl", (5.2, 1.6, 5.0), (0, 0.6, 0), 32), ("rl", (-5.0, 1.7, -5.2), (0, 0.6, -0.2), 32),
    ("side", (8.5, 0.9, 0), (0, 0.7, 0), 30), ("top", (2.6, 3.6, -1.6), (0, 0.9, 0.2), 45),
    ("front", (0, 1.0, 8), (0, 0.75, 0), 24), ("rear", (0, 1.1, -8), (0, 0.8, 0), 24),
    ("cab", (-0.38, 1.40, -0.30), (-0.25, 1.15, 1.5), 75), ("cab2", (0.55, 1.45, -1.3), (-0.3, 1.0, 0.6), 80), ("cabtop", (0.9, 3.2, -1.6), (0, 0.9, 0.1), 48),
    ("wheel", (2.2, 0.55, 2.4), (0.8, 0.37, 1.47), 26), ("wheel2", (1.9, 0.30, 0.1), (0.85, 0.37, 1.47), 30),
    ("photo", (-4.6, 3.0, 3.6), (0, 0.55, 0.1), 30), ("photorear", (-3.2, 1.5, -4.6), (0, 0.6, -0.3), 32), ("photorear2", (-1.6, 0.9, -5.2), (0.1, 0.65, -0.6), 34),
    ("roofside", (3.2, 1.75, 0.6), (0.5, 1.25, -0.3), 30),
]
# Своё место водителя (у седана глаза ниже, чем у X5): CAB="x,y,z" CABAT="x,y,z"
if os.environ.get("CAB"):
    cab = tuple(float(c) for c in os.environ["CAB"].split(","))
    cab_at = tuple(float(c) for c in os.environ.get("CABAT", "%f,%f,1.5" % (cab[0] + 0.1, cab[1] - 0.25)).split(","))
    VIEWS = [(n, cab, cab_at, f) if n == "cab" else (n, p, a, f) for n, p, a, f in VIEWS]
only = os.environ.get("VIEWS")
files = []
for name, pos, at, fov in VIEWS:
    if only and name not in only.split(","):
        continue
    cam.location = unity(pos)
    d = unity(at) - unity(pos)
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    # fov в Unity/three — вертикальный
    cam_data.sensor_fit = 'VERTICAL'
    cam_data.angle = math.radians(fov)
    cam_data.clip_start = 0.02
    scene.render.filepath = os.path.join(out_dir, name + ".png")
    bpy.ops.render.render(write_still=True)
    files.append(scene.render.filepath)

# Сетка 2×3 из готовых кадров
if len(files) == 6 and not only:
    w, h = scene.render.resolution_x, scene.render.resolution_y
    grid = bpy.data.images.new("grid", w * 2, h * 3)
    pix = [0.0] * (w * 2 * h * 3 * 4)
    for i, f in enumerate(files):
        im = bpy.data.images.load(f)
        src = list(im.pixels)
        col, row = i % 2, 2 - i // 2
        for y in range(h):
            s0 = y * w * 4
            d0 = ((row * h + y) * w * 2 + col * w) * 4
            pix[d0:d0 + w * 4] = src[s0:s0 + w * 4]
    grid.pixels = pix
    grid.filepath_raw = os.path.join(out_dir, "grid.png")
    grid.file_format = 'PNG'
    grid.save()
print("RENDER DONE", files)

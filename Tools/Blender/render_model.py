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

# ---------- Материалы (ключи как в CarMaterials) ----------
def hexc(h, a=1.0):
    h = h.lstrip('#')
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [x ** 2.2 for x in c] + [a]  # sRGB → линейный

mats = {}

def principled(name, color, rough=0.5, metal=0.0, emit=None, emit_strength=1.0, alpha=None, transmission=0.0, coat=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = hexc(color)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if coat:
        b.inputs["Coat Weight"].default_value = coat
        b.inputs["Coat Roughness"].default_value = 0.03
    if emit:
        b.inputs["Emission Color"].default_value = hexc(emit)
        b.inputs["Emission Strength"].default_value = emit_strength
    if transmission:
        b.inputs["Transmission Weight"].default_value = transmission
    if alpha is not None:
        b.inputs["Alpha"].default_value = alpha
        try:
            m.surface_render_method = 'BLENDED'
        except Exception:
            pass
    return m

def image_mat(name, path, rough, metal, coat=0.0):
    m = principled(name, '#ffffff', rough, metal, coat=coat)
    nt = m.node_tree
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(path)
    nt.links.new(tex.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    return m

SPEC = {
    'paint': ('#d8a53f', 0.15, 1.0), 'glass': ('#1d2a33', 0.03, 0.0), 'glass_dark': ('#0b0f13', 0.05, 0.0),
    'lens': ('#e8f0f6', 0.02, 0.0), 'black': ('#0b0b0d', 0.45, 0.0), 'grille': ('#0d0d0f', 0.7, 0.0),
    'rubber': ('#141414', 0.85, 0.0), 'tread': ('#0a0a0a', 0.95, 0.0), 'chrome': ('#e4e8ec', 0.06, 1.0),
    'alloy': ('#c9ccd0', 0.22, 1.0), 'rim_inner': ('#7d8186', 0.4, 1.0), 'disc': ('#6a6c70', 0.45, 1.0),
    'caliper': ('#b5161b', 0.35, 0.0), 'housing': ('#25282d', 0.3, 0.6), 'reflector': ('#f0f2f4', 0.05, 1.0),
    'plate': ('#f4f4f4', 0.4, 0.0), 'int_door': ('#1c1c1f', 0.8, 0.0), 'int_roof': ('#3c3c41', 0.9, 0.0),
    'carpet': ('#151515', 1.0, 0.0), 'leather_black': ('#141416', 0.5, 0.0), 'int_black': ('#1a1a1d', 0.7, 0.0),
    'int_grey': ('#8b8e93', 0.4, 0.6), 'gauge_face': ('#0a0a0b', 0.4, 0.0), 'liner': ('#0c0c0c', 0.95, 0.0),
    'mirror': ('#dfe6ee', 0.03, 1.0), 'skin': ('#e2b08a', 0.6, 0.0), 'hair': ('#3a2717', 0.8, 0.0),
    'jacket': ('#2a2c33', 0.8, 0.0), 'chassis': ('#1b1b1c', 0.8, 0.0), 'black_satin': ('#1c1d20', 0.5, 0.2),
    'rim_black': ('#151517', 0.35, 0.5),
}
EMIT = {
    'lamp_glow': ('#fff7e0', '#fff2c8', 3.0), 'amber': ('#ff8a1c', '#ff7a00', 0.6), 'tail_red': ('#c8121a', '#ff1010', 1.0),
    'tail_smoke': ('#3a0c10', '#300004', 0.5), 'screen_blue': ('#0a1838', '#1a3a8a', 2.0), 'white': ('#f2f2f2', '#ffffff', 0.5),
    'ambient': ('#2a5cff', '#2a5cff', 2.0), 'needle': ('#ff3320', '#ff2200', 1.0),
}

def get_mat(key):
    if key in mats:
        return mats[key]
    if key == 'paint' and camo_path:
        m = image_mat(key, camo_path, 0.08, 1.0, coat=0.6)    # хромированная плёнка: металл с зеркальным блеском
    elif key == 'decal_pit' and decal_path:
        m = image_mat(key, decal_path, 0.35, 0.0)
    elif key in ('glass', 'glass_dark'):
        c, r, _ = SPEC[key]
        m = principled(key, c, r, 0.0, transmission=0.6 if key == 'glass' else 0.2)
    elif key == 'lens':
        m = principled(key, '#e8f0f6', 0.02, 0.0, transmission=1.0)
    elif key in EMIT:
        c, e, s = EMIT[key]
        m = principled(key, c, 0.3, 0.0, emit=e, emit_strength=s)
    elif key in SPEC:
        c, r, mt = SPEC[key]
        m = principled(key, c, r, mt)
    else:
        m = principled(key, '#ff00ff', 0.5, 0.0)
    mats[key] = m
    return m

# ---------- Геометрия ----------
# JSON: координаты Unity с Z, перевёрнутым для three.js. В Blender: X → X, Y (вперёд) = −z_json, Z (вверх) = y.
data = json.load(open(model_path))
car = bpy.data.objects.new("Car", None)
scene.collection.objects.link(car)
for k, m in enumerate(data["meshes"]):
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
ground.data.materials.append(principled("ground", '#8a8d90', 0.85, 0.0))

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
bg.inputs["Strength"].default_value = 0.35
wn.links.new(sky.outputs["Color"], bg.inputs["Color"])

sun_data = bpy.data.lights.new("Sun", 'SUN')
sun_data.energy = 3.5
sun_data.angle = math.radians(2)
sun = bpy.data.objects.new("Sun", sun_data)
sun.rotation_euler = (math.radians(55), 0, math.radians(140))
scene.collection.objects.link(sun)

# Студийные «софтбоксы» сверху и сбоку — чтобы хром было чем отражать
for name, loc, size, energy in [("BoxTop", (0, 0, 6), 6, 600), ("BoxL", (-6, 2, 2.5), 4, 300), ("BoxR", (6, -2, 2.5), 4, 300)]:
    ld = bpy.data.lights.new(name, 'AREA')
    ld.size = size
    ld.energy = energy
    lo = bpy.data.objects.new(name, ld)
    lo.location = loc
    scene.collection.objects.link(lo)
    d = Vector((0, 0, 0.8)) - Vector(loc)
    lo.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()

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

cam_data = bpy.data.cameras.new("Cam")
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

def unity(v):  # точка в координатах Unity → Blender
    return Vector((v[0], v[2], v[1]))

VIEWS = [
    ("fl", (5.2, 1.6, 5.0), (0, 0.6, 0), 32), ("rl", (-5.0, 1.7, -5.2), (0, 0.6, -0.2), 32),
    ("side", (8.5, 0.9, 0), (0, 0.7, 0), 30), ("top", (2.6, 3.6, -1.6), (0, 0.9, 0.2), 45),
    ("front", (0, 1.0, 8), (0, 0.75, 0), 24), ("cab", (-0.38, 1.40, -0.20), (-0.3, 1.2, 1.5), 72),
]
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
if len(files) == 6:
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

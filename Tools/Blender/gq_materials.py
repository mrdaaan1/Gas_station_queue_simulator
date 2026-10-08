# Материалы машин для Blender — по ключам, как CarMaterials в игре. Общие для render_model.py и car_build.py.
import bpy, os

camo_path = None
# Папка с текстурами Мэтра (Assets/Resources/Textures): ржавчина, краска, глаз, полосы
tex_dir = None
TEXTURED = {'mater_rust': 0.8, 'mater_blue': 0.6, 'mater_green': 0.55, 'mater_eye': 0.12, 'mater_stripes': 0.6}
# Цвет кузова без камуфляжа (A7 — серебристый «Флорет»): PAINT=#b8bcc1
paint_color = os.environ.get("PAINT")
decal_path = None

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
    'leather_tan': ('#c49a6c', 0.5, 0.0), 'leather_cream': ('#d9ccb4', 0.5, 0.0), 'int_beige': ('#cbbfa8', 0.7, 0.0),
    'carpet_beige': ('#8a7a63', 1.0, 0.0), 'int_roof_light': ('#cfc8bb', 0.9, 0.0), 'piano_black': ('#050506', 0.06, 0.0),
    'wood_dark': ('#4a3a30', 0.35, 0.0), 'eye_white': ('#f4f1e6', 0.12, 0.0), 'teeth': ('#eee2bf', 0.35, 0.0),
    'mouth': ('#1e0b07', 0.8, 0.0), 'rust_dark': ('#3d2416', 0.85, 0.0), 'tongue': ('#8a2c2a', 0.4, 0.0), 'cloth_dark': ('#1e1f22', 0.9, 0.0), 'cloth_red': ('#a3141c', 0.85, 0.0), 'stitch_red': ('#c41620', 0.6, 0.0), 'caliper_yellow': ('#f2c414', 0.35, 0.0), 'tail_clear': ('#e2e8ee', 0.1, 0.3), 'seat_brown': ('#6b3e26', 0.55, 0.0), 'steel_dark': ('#3b3f39', 0.6, 0.3), 'lamp_off': ('#3a2a10', 0.5, 0.0), 'alloy_machined': ('#d2d5d9', 0.18, 1.0), 'rim_gunmetal': ('#3a3d42', 0.35, 0.8), 'leather_red': ('#9e1219', 0.5, 0.0), 'stripe': ('#2f7fe0', 0.35, 0.0), 'stripe_dark': ('#1d3fae', 0.35, 0.0),
}
EMIT = {
    'lamp_glow': ('#fff7e0', '#fff2c8', 3.0), 'amber': ('#ff8a1c', '#ff7a00', 0.6), 'tail_red': ('#c8121a', '#ff1010', 1.0),
    'tail_smoke': ('#3a0c10', '#300004', 0.5), 'screen_blue': ('#0a1838', '#1a3a8a', 2.0), 'white': ('#f2f2f2', '#ffffff', 0.5),
    'ambient': ('#2a5cff', '#2a5cff', 2.0), 'screen': ('#0c1a12', '#0c2a18', 1.0), 'screen_dark': ('#05080c', '#0a1424', 1.0), 'needle': ('#ff3320', '#ff2200', 1.0),
}

def get_mat(key):
    if key in mats:
        return mats[key]
    if key in TEXTURED and tex_dir:
        import os as _os
        m = image_mat(key, _os.path.join(tex_dir, key + ".png"), TEXTURED[key], 0.0)
    elif key == 'paint' and paint_color:
        m = principled(key, paint_color, 0.22, float(os.environ.get("PAINT_METAL", "0.85")), coat=1.0)   # металлик под лаком
    elif key == 'paint' and camo_path:
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


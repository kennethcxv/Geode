"""Golf Simulator checkout kit -> Geode FBX.

Converts the authored Golf checkout GLBs (Assets/checkout/glb/*.glb in the Golf-Simulator repo) into Unity-ready FBX
files under Geode/Assets/GeodeEmpire/Models/Checkout, extracts their embedded textures to
Geode/Assets/GeodeEmpire/Textures/Checkout, and writes checkout_kit.json: every material (base colour, texture,
metallic, roughness, emission), every node with its authored extras (anchors, sockets, drawer well contracts) and
the glTF-space transforms, so the Unity kit builder can rebuild materials and rig references without string lookups
at runtime.

Run headlessly:
    ./Tools/blender.sh --background --python Tools/Blender/import_golf_checkout.py [-- --golf <path>]

Deterministic: no wall-clock state, same input -> same output.
"""
import bpy, os, sys, json, struct, math

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
GOLF = os.path.expanduser("~/Documents/GitHub/Golf-Simulator")
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if "--golf" in argv: GOLF = argv[argv.index("--golf") + 1]
GLB_DIR = os.path.join(GOLF, "Assets", "checkout", "glb")
OUT_MODELS = os.path.join(REPO, "Geode", "Assets", "GeodeEmpire", "Models", "Checkout")
OUT_TEX = os.path.join(REPO, "Geode", "Assets", "GeodeEmpire", "Textures", "Checkout")
os.makedirs(OUT_MODELS, exist_ok=True); os.makedirs(OUT_TEX, exist_ok=True)

# the register-critical kit (§18.2); the scanner, printer, loose receipt and the retired 20-unit coin are not in the shipping flow
STEMS = [
    "checkout_counter", "pos_monitor", "payment_terminal", "cash_drawer", "payment_card", "shopping_bag",
    "customer_display", "cash_handoff_stack",
    "cash_bill_1", "cash_bill_5", "cash_bill_10", "cash_bill_20", "cash_bill_50",
    "cash_coin_01", "cash_coin_05", "cash_coin_05_sheet01", "cash_coin_10", "cash_coin_25", "cash_coin_50",
]

def log(msg): print(f"[golf-kit] {msg}", flush=True)

def read_glb_json(path):
    with open(path, "rb") as f: data = f.read()
    magic, ver, length = struct.unpack("<III", data[:12]); off = 12
    while off < length:
        clen, ctype = struct.unpack("<II", data[off:off + 8]); off += 8
        chunk = data[off:off + clen]; off += clen
        if ctype == 0x4E4F534A: return json.loads(chunk)
    return None

def gltf_node_index(js):
    """name -> (translation, rotation, scale, extras, parentName, meshBounds) straight from the glTF (Y-up, metres)."""
    nodes = js.get("nodes", []); meshes = js.get("meshes", []); acc = js.get("accessors", [])
    parent = {}
    for i, n in enumerate(nodes):
        for c in n.get("children", []): parent[c] = i
    out = {}
    for i, n in enumerate(nodes):
        b = None
        if "mesh" in n:
            mn = [1e9] * 3; mx = [-1e9] * 3
            for p in meshes[n["mesh"]]["primitives"]:
                a = acc[p["attributes"]["POSITION"]]
                for k in range(3): mn[k] = min(mn[k], a["min"][k]); mx[k] = max(mx[k], a["max"][k])
            b = {"min": mn, "max": mx}
        out[n.get("name", f"node{i}")] = {
            "translation": n.get("translation", [0, 0, 0]), "rotation": n.get("rotation", [0, 0, 0, 1]),
            "scale": n.get("scale", [1, 1, 1]), "extras": n.get("extras", {}),
            "parent": nodes[parent[i]].get("name") if i in parent else None, "bounds": b,
            "materials": [js["materials"][p["material"]].get("name") for p in meshes[n["mesh"]]["primitives"] if "material" in p] if "mesh" in n else [],
        }
    return out

def material_info(mat):
    info = {"name": mat.name, "baseColor": [1, 1, 1, 1], "texture": None, "normalTexture": None, "metallic": 0.0,
            "roughness": 0.5, "emission": [0, 0, 0], "emissionStrength": 0.0, "alpha": 1.0, "blend": "OPAQUE"}
    if not mat.use_nodes: return info
    bsdf = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None: return info
    def linked_image(sock):
        if not sock.is_linked: return None
        node = sock.links[0].from_node
        # the importer may route through a separate/normal-map node
        for _ in range(3):
            if node.type == "TEX_IMAGE": return node.image.name if node.image else None
            if node.inputs and any(i.is_linked for i in node.inputs):
                node = next(i for i in node.inputs if i.is_linked).links[0].from_node
            else: break
        return None
    info["baseColor"] = list(bsdf.inputs["Base Color"].default_value)
    info["texture"] = linked_image(bsdf.inputs["Base Color"])
    info["metallic"] = float(bsdf.inputs["Metallic"].default_value)
    info["roughness"] = float(bsdf.inputs["Roughness"].default_value)
    info["normalTexture"] = linked_image(bsdf.inputs["Normal"])
    em = bsdf.inputs.get("Emission Color")
    if em is not None:
        info["emission"] = list(em.default_value)[:3]
        info["emissionStrength"] = float(bsdf.inputs["Emission Strength"].default_value)
    info["alpha"] = float(bsdf.inputs["Alpha"].default_value)
    info["blend"] = getattr(mat, "surface_render_method", "OPAQUE") if hasattr(mat, "surface_render_method") else getattr(mat, "blend_method", "OPAQUE")
    return info

saved_images = {}
def save_image(img):
    if img.name in saved_images: return saved_images[img.name]
    base = os.path.splitext(img.name)[0]
    path = os.path.join(OUT_TEX, base + ".png")
    img.file_format = "PNG"
    try:
        img.save(filepath=path)
    except TypeError:
        img.filepath_raw = path; img.save()
    saved_images[img.name] = base + ".png"
    return saved_images[img.name]

def export_fbx(path):
    if os.path.exists(path): os.remove(path)
    bpy.ops.object.select_all(action="SELECT")
    common = dict(filepath=path, use_selection=True, object_types={"MESH", "EMPTY"}, apply_unit_scale=True,
                  apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE",
                  use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode="STRIP",
                  use_custom_props=True)
    try:
        r = bpy.ops.export_scene.fbx(**common)
    except AttributeError:
        r = bpy.ops.wm.fbx_export(filepath=path, selected_objects_only=True)
    if "FINISHED" not in r: raise RuntimeError(f"FBX export failed for {path}: {r}")

manifest = {"source": "Golf-Simulator Assets/checkout/glb (commit ab3850c4)", "models": {}}
for stem in STEMS:
    glb = os.path.join(GLB_DIR, stem + ".glb")
    if not os.path.exists(glb): log(f"MISSING {glb}"); continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    js = read_glb_json(glb)
    bpy.ops.import_scene.gltf(filepath=glb, import_shading="NORMALS", merge_vertices=False)
    # rest pose only: the kit ships drawer open/close clips, and an imported object left on an animated frame would
    # export in that pose (the cash drawer came through permanently open). Clear the animation and put every node whose
    # authored glTF translation is the origin back on its parent origin.
    gltf_json = read_glb_json(glb)
    zero_nodes = {n.get("name") for n in gltf_json.get("nodes", [])
                  if [round(v, 6) for v in n.get("translation", [0, 0, 0])] == [0, 0, 0]}
    for ob in bpy.data.objects:
        if ob.animation_data:
            ob.animation_data_clear()
        if ob.name in zero_nodes:
            ob.location = (0.0, 0.0, 0.0)
    for act in list(bpy.data.actions):
        bpy.data.actions.remove(act)
    bpy.context.view_layer.update()
    # materials + textures
    mats = []
    for mat in bpy.data.materials:
        if mat.users == 0: continue
        info = material_info(mat)
        for key in ("texture", "normalTexture"):
            if info[key]:
                img = bpy.data.images.get(info[key])
                info[key] = save_image(img) if img is not None else None
        mats.append(info)
    # objects: names, extras (custom props) and the parent chain
    objs = []
    for ob in bpy.data.objects:
        extras = {}
        for k in ob.keys():
            if k.startswith("_"): continue
            v = ob[k]
            try: json.dumps(v); extras[k] = v
            except TypeError: extras[k] = str(v)
        objs.append({"name": ob.name, "type": ob.type, "parent": ob.parent.name if ob.parent else None, "extras": extras,
                     "materials": [s.material.name for s in ob.material_slots if s.material] if ob.type == "MESH" else []})
    gltf_nodes = gltf_node_index(js)
    root_extras = next((n.get("extras", {}) for n in js.get("nodes", []) if n.get("name") == stem), {})
    out = os.path.join(OUT_MODELS, stem + ".fbx")
    export_fbx(out)
    manifest["models"][stem] = {"fbx": os.path.relpath(out, REPO), "rootExtras": root_extras, "materials": mats,
                                "objects": objs, "gltfNodes": gltf_nodes}
    log(f"{stem}: {len(objs)} objects, {len(mats)} materials -> {os.path.basename(out)}")

with open(os.path.join(OUT_MODELS, "checkout_kit.json"), "w") as f:
    json.dump(manifest, f, indent=1, sort_keys=True)
log(f"textures: {sorted(saved_images.values())}")
log("DONE")

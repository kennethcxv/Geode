"""
Geode - Blender asset-generation pipeline smoke test.

Run headlessly:
    ./Tools/blender.sh --background --python Tools/Blender/smoke_test.py

Steps:
  1. Clear the scene.
  2. Create a low-poly icosphere.
  3. Procedurally deform it into an irregular rock.
  4. Recalculate normals so every face points outward.
  5. Assign a simple rock-like material.
  6. Apply transforms and rest the rock on Z = 0.
  7. Ensure Tools/Blender/Output exists.
  8. Save Tools/Blender/Output/blender_smoke_test.blend
  9. Export Tools/Blender/Output/blender_smoke_test.fbx

The script verifies both files on disk and exits non-zero on any failure,
so the Blender process exit code reflects the test result.
"""

import os
import random
import sys
import traceback

import bmesh
import bpy
from mathutils import Vector, noise

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUTPUT_DIR = os.path.join(SCRIPT_DIR, "Output")
BLEND_PATH = os.path.join(OUTPUT_DIR, "blender_smoke_test.blend")
FBX_PATH = os.path.join(OUTPUT_DIR, "blender_smoke_test.fbx")

SEED = 1337
ROCK_NAME = "SmokeTestRock"


def log(msg):
    print(f"[smoke_test] {msg}", flush=True)


def fail(msg):
    print(f"[smoke_test] FAIL: {msg}", flush=True)
    sys.exit(1)


# ---------------------------------------------------------------------------
# 1. Clear the scene
# ---------------------------------------------------------------------------
def clear_scene():
    # Load an empty startup file (no default cube/camera/light) ...
    bpy.ops.wm.read_homefile(use_empty=True)
    # ... then remove anything that still lingers so the saved .blend only
    # contains the rock.
    for collection in (bpy.data.objects, bpy.data.meshes, bpy.data.materials,
                       bpy.data.images, bpy.data.cameras, bpy.data.lights):
        for block in list(collection):
            collection.remove(block)
    log(f"Scene cleared: {len(bpy.data.objects)} objects remain")


# ---------------------------------------------------------------------------
# 2. Low-poly icosphere
# ---------------------------------------------------------------------------
def create_icosphere():
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=(0, 0, 0))
    obj = bpy.context.active_object
    if obj is None or obj.type != "MESH":
        fail("Icosphere was not created")
    obj.name = ROCK_NAME
    obj.data.name = ROCK_NAME + "_Mesh"
    log(f"Icosphere created: {len(obj.data.vertices)} verts, {len(obj.data.polygons)} faces")
    return obj


# ---------------------------------------------------------------------------
# 3. Procedural rock deformation
# ---------------------------------------------------------------------------
def deform_into_rock(obj):
    random.seed(SEED)
    mesh = obj.data
    offset = Vector((random.uniform(-100, 100) for _ in range(3)))

    for v in mesh.vertices:
        p = v.co
        n = p.normalized()

        # Layered noise: broad lumps + finer chips.
        low = noise.noise((p * 1.2) + offset)
        high = noise.noise((p * 3.5) + offset * 0.5)
        displacement = 0.28 * low + 0.10 * high

        # Random per-vertex jitter breaks up the icosphere's regularity.
        jitter = Vector((random.uniform(-0.04, 0.04) for _ in range(3)))

        v.co = p + n * displacement + jitter

    # Squash so it reads as a rock rather than a ball, and flatten the base a bit.
    obj.scale = (1.35, 1.0, 0.72)
    for v in mesh.vertices:
        if v.co.z < -0.55:
            v.co.z = -0.55 + (v.co.z + 0.55) * 0.35

    mesh.update()
    mesh.validate()

    # Low-poly look: flat shading.
    for poly in mesh.polygons:
        poly.use_smooth = False

    log("Rock deformation applied")


# ---------------------------------------------------------------------------
# 4. Sensible normals
# ---------------------------------------------------------------------------
def fix_normals(obj):
    """Make face normals consistent and outward-facing, and check the result."""
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    non_manifold = [e for e in bm.edges if not e.is_manifold]
    if non_manifold:
        bm.free()
        fail(f"Mesh has {len(non_manifold)} non-manifold edges")

    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

    # The rock is centred on the origin at this point, so an outward normal
    # must point roughly the same way as the face centre.
    inward = sum(1 for poly in mesh.polygons if poly.normal.dot(poly.center) < 0.0)
    if inward:
        fail(f"{inward}/{len(mesh.polygons)} faces point inward after recalc")
    log(f"Normals recalculated: {len(mesh.polygons)} faces, all outward, manifold")


# ---------------------------------------------------------------------------
# 5. Simple rock material
# ---------------------------------------------------------------------------
def assign_material(obj):
    mat = bpy.data.materials.new("RockMaterial")
    # Blender 5.x materials are node-based by default; 'use_nodes' is
    # deprecated (slated for removal in 6.0), so only touch it on old builds.
    if mat.node_tree is None:
        mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links

    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        fail("Principled BSDF node missing from new material")

    bsdf.inputs["Base Color"].default_value = (0.36, 0.33, 0.30, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.92

    # Light procedural colour variation via noise -> mix into base colour.
    tex = nodes.new("ShaderNodeTexNoise")
    tex.inputs["Scale"].default_value = 6.0
    tex.inputs["Detail"].default_value = 4.0
    tex.location = (-600, 200)

    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.location = (-350, 200)
    ramp.color_ramp.elements[0].color = (0.22, 0.20, 0.18, 1.0)
    ramp.color_ramp.elements[1].color = (0.48, 0.45, 0.41, 1.0)

    links.new(tex.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])

    # Viewport / solid-mode fallback colour (also what a plain FBX carries).
    mat.diffuse_color = (0.36, 0.33, 0.30, 1.0)
    mat.roughness = 0.92

    obj.data.materials.clear()
    obj.data.materials.append(mat)
    log(f"Material assigned: {mat.name}")


# ---------------------------------------------------------------------------
# 6. Apply transforms, rest on Z = 0
# ---------------------------------------------------------------------------
def apply_transforms(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # Sit the rock on the ground plane (origin at base is Unity-friendly).
    min_z = min(v.co.z for v in obj.data.vertices)
    for v in obj.data.vertices:
        v.co.z -= min_z
    obj.data.update()
    if any(abs(s - 1.0) > 1e-6 for s in obj.scale):
        fail(f"Scale not applied: {tuple(obj.scale)}")
    base_z = min(v.co.z for v in obj.data.vertices)
    if abs(base_z) > 1e-5:
        fail(f"Rock base not at Z=0: {base_z}")
    log(f"Transforms applied; scale={tuple(round(s, 3) for s in obj.scale)}; base_z={base_z:.6f}")


# ---------------------------------------------------------------------------
# 7-9. Output
# ---------------------------------------------------------------------------
def ensure_output_dir():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    log(f"Output dir: {OUTPUT_DIR}")


def save_blend():
    for path in (BLEND_PATH, BLEND_PATH + "1"):
        if os.path.exists(path):
            os.remove(path)
    result = bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH, compress=False)
    if "FINISHED" not in result:
        fail(f"save_as_mainfile returned {result}")
    log(f"Saved .blend: {BLEND_PATH}")


def export_fbx(obj):
    if os.path.exists(FBX_PATH):
        os.remove(FBX_PATH)

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    common = dict(
        filepath=FBX_PATH,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )

    result = None
    try:
        result = bpy.ops.export_scene.fbx(**common)
        exporter = "export_scene.fbx (Python add-on)"
    except AttributeError:
        # Blender 5.x ships a native C++ exporter; fall back to it if the
        # legacy add-on operator is unavailable.
        result = bpy.ops.wm.fbx_export(filepath=FBX_PATH, selected_objects_only=True)
        exporter = "wm.fbx_export (native)"

    if "FINISHED" not in result:
        fail(f"FBX export returned {result}")
    log(f"Exported FBX via {exporter}: {FBX_PATH}")


def verify_outputs():
    ok = True
    for path in (BLEND_PATH, FBX_PATH):
        if not os.path.isfile(path):
            log(f"MISSING: {path}")
            ok = False
            continue
        size = os.path.getsize(path)
        if size == 0:
            log(f"EMPTY: {path}")
            ok = False
            continue
        log(f"OK ({size} bytes): {path}")

    # Sanity-check the FBX header (binary FBX starts with this magic string).
    with open(FBX_PATH, "rb") as f:
        head = f.read(32)
    if not head.startswith(b"Kaydara FBX Binary"):
        log(f"FBX header unexpected: {head!r}")
        ok = False

    with open(BLEND_PATH, "rb") as f:
        head = f.read(7)
    if head != b"BLENDER":
        log(f".blend header unexpected: {head!r}")
        ok = False

    if not ok:
        fail("Output verification failed")


def main():
    log(f"Blender {bpy.app.version_string} ({bpy.app.build_platform.decode()})")
    log(f"Python {sys.version.split()[0]}")
    log(f"Background mode: {bpy.app.background}")

    clear_scene()
    rock = create_icosphere()
    deform_into_rock(rock)
    fix_normals(rock)
    assign_material(rock)
    apply_transforms(rock)
    ensure_output_dir()
    save_blend()
    export_fbx(rock)
    verify_outputs()

    log(f"Final mesh: {len(rock.data.vertices)} verts, {len(rock.data.polygons)} faces")
    log("SMOKE TEST PASSED")


if __name__ == "__main__":
    try:
        main()
    except SystemExit:
        raise
    except Exception:
        traceback.print_exc()
        fail("Unhandled exception")

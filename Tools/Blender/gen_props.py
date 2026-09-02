"""
Geode Empire - workshop prop generator (headless bpy).

Run:
    ./Tools/blender.sh --background --python Tools/Blender/gen_props.py

Writes one FBX per prop to Geode/Assets/GeodeEmpire/Models/Props/.
All props: metres, origin at base centre (unless noted), +Z up in Blender (=> +Y in Unity),
low-poly with small bevels, box-projected UVs (0.5 m per tile), deterministic.
"""

import math
import os
import random
import sys
import traceback

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import geode_blender_lib as lib  # noqa: E402

TAG = "gen_props"
OUT_DIR = os.path.join(lib.UNITY_ASSETS, "Models", "Props")
UV_SCALE = 2.0  # 1 tile = 0.5 m


def box(bm, size, center, bevel=0.0):
    b = lib.bm_box(size, center)
    if bevel > 0:
        lib.bm_bevel(b, bevel, segments=1)
    lib.bm_append(bm, b)


def cyl(bm, radius, height, center, segments=16, radius_top=None, matrix=None, bevel=0.0):
    c = lib.bm_cylinder(radius, height, segments=segments, center=(0, 0, 0), radius_top=radius_top)
    if bevel > 0:
        lib.bm_bevel(c, bevel, segments=1)
    m = Matrix.Translation(center)
    if matrix is not None:
        m = m @ matrix
    lib.bm_append(bm, c, m)


def torus(bm, major, minor, center, seg_major=28, seg_minor=10, squash=1.0):
    verts, faces = [], []
    for i in range(seg_major):
        a = 2 * math.pi * i / seg_major
        ca, sa = math.cos(a), math.sin(a)
        for j in range(seg_minor):
            b = 2 * math.pi * j / seg_minor
            r = major + minor * math.cos(b)
            verts.append((center[0] + r * ca, center[1] + r * sa, center[2] + minor * math.sin(b) * squash))
    for i in range(seg_major):
        for j in range(seg_minor):
            i2 = (i + 1) % seg_major
            j2 = (j + 1) % seg_minor
            faces.append((i * seg_minor + j, i2 * seg_minor + j, i2 * seg_minor + j2, i * seg_minor + j2))
    t = lib.bm_from_pydata(verts, faces)
    lib.bm_append(bm, t)


# ---------------------------------------------------------------------------
# Props
# ---------------------------------------------------------------------------
def hammer(rng):
    bm = bmesh.new()
    # handle along +Z, origin at handle bottom
    cyl(bm, 0.016, 0.3, (0, 0, 0), segments=10, radius_top=0.014)
    cyl(bm, 0.02, 0.05, (0, 0, 0.0), segments=10, radius_top=0.017)  # grip flare
    # head (mallet-like engineer's hammer): box with bevel, oriented along X
    head = lib.bm_box((0.11, 0.036, 0.036), (0, 0, 0.315))
    lib.bm_bevel(head, 0.005, segments=1)
    lib.bm_append(bm, head)
    return bm, False


def chisel(rng, fine=False):
    bm = bmesh.new()
    length = 0.22 if not fine else 0.24
    r = 0.011 if not fine else 0.008
    # tip at origin: tapered wedge, then octagonal shank, then mushroom head at the top
    tip = lib.bm_cylinder(0.002, 0.03, segments=8, radius_top=r)
    lib.bm_append(bm, tip, Matrix.Translation((0, 0, 0)))
    # flatten tip into a blade
    for v in bm.verts:
        if v.co.z < 0.001:
            v.co.x *= 4.0
            v.co.y *= 0.25
    cyl(bm, r, length - 0.06, (0, 0, 0.03), segments=8)
    if fine:
        cyl(bm, r * 1.6, 0.03, (0, 0, 0.11), segments=8)  # grip ring
    cyl(bm, r * 1.5, 0.03, (0, 0, length - 0.03), segments=10, radius_top=r * 1.3)
    return bm, False


def workbench(rng):
    bm = bmesh.new()
    box(bm, (1.8, 0.75, 0.07), (0, 0, 0.865), bevel=0.006)
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (0.08, 0.08, 0.83), (sx * 0.82, sy * 0.3, 0.415), bevel=0.004)
    box(bm, (1.64, 0.06, 0.1), (0, 0.3, 0.2))
    box(bm, (1.64, 0.06, 0.1), (0, -0.3, 0.2))
    box(bm, (0.06, 0.6, 0.1), (-0.82, 0, 0.2))
    box(bm, (0.06, 0.6, 0.1), (0.82, 0, 0.2))
    box(bm, (1.6, 0.6, 0.03), (0, 0, 0.26))  # lower shelf
    return bm, False


def cradle(rng):
    """Sandbag ring that holds a rock steady on the bench. Origin at base."""
    bm = bmesh.new()
    torus(bm, 0.085, 0.035, (0, 0, 0.032), squash=0.85)
    # rubber pad underneath
    cyl(bm, 0.13, 0.012, (0, 0, 0), segments=20, bevel=0.003)
    return bm, True


def crate_body(rng):
    bm = bmesh.new()
    w, d, h = 0.62, 0.46, 0.34
    t = 0.018
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (0.04, 0.04, h), (sx * (w / 2 - 0.02), sy * (d / 2 - 0.02), h / 2), bevel=0.003)
    # slats: 3 per long side, 3 per short side, bottom boards
    for i in range(3):
        z = 0.05 + i * 0.115
        for sy in (-1, 1):
            box(bm, (w, t, 0.085), (0, sy * (d / 2 - t / 2), z + 0.0425), bevel=0.002)
        for sx in (-1, 1):
            box(bm, (t, d - 0.08, 0.085), (sx * (w / 2 - t / 2), 0, z + 0.0425), bevel=0.002)
    for i in range(5):
        box(bm, (w - 0.08, 0.07, t), (0, -d / 2 + 0.07 + i * 0.08, t / 2), bevel=0.002)
    # straw bed (soft lumpy fill) - separate low disc
    straw = lib.bm_cylinder(0.26, 0.05, segments=14, center=(0, 0, t))
    for v in straw.verts:
        v.co.x *= 1.1
        v.co.y *= 0.8
        if v.co.z > t + 0.01:
            v.co.z += rng.uniform(-0.012, 0.018)
    lib.bm_append(bm, straw)
    return bm, False


def crate_lid(rng):
    bm = bmesh.new()
    w, d, t = 0.62, 0.46, 0.018
    for i in range(4):
        box(bm, (w, 0.105, t), (0, -d / 2 + 0.055 + i * 0.115, t / 2), bevel=0.002)
    box(bm, (0.05, d - 0.02, t), (-0.22, 0, t + t / 2), bevel=0.002)
    box(bm, (0.05, d - 0.02, t), (0.22, 0, t + t / 2), bevel=0.002)
    return bm, False


def shelf_unit(rng):
    bm = bmesh.new()
    w, d, h = 1.0, 0.36, 1.85
    for sx in (-1, 1):
        box(bm, (0.04, d, h), (sx * (w / 2 - 0.02), 0, h / 2), bevel=0.003)
    for i in range(4):
        box(bm, (w - 0.08, d, 0.03), (0, 0, 0.12 + i * 0.55), bevel=0.002)
    box(bm, (w - 0.08, 0.02, h - 0.1), (0, d / 2 - 0.01, h / 2), bevel=0.0)  # back
    return bm, False


def display_cabinet(rng):
    bm = bmesh.new()
    w, d, h = 1.3, 0.42, 1.75
    box(bm, (w, d, 0.12), (0, 0, 0.06), bevel=0.004)             # plinth
    for sx in (-1, 1):
        box(bm, (0.04, d, h - 0.12), (sx * (w / 2 - 0.02), 0, 0.12 + (h - 0.12) / 2), bevel=0.003)
    box(bm, (w, d, 0.05), (0, 0, h - 0.025), bevel=0.004)         # top
    box(bm, (w - 0.08, 0.025, h - 0.17), (0, d / 2 - 0.0125, 0.12 + (h - 0.17) / 2))  # back
    for i in range(3):
        box(bm, (w - 0.08, d - 0.05, 0.03), (0, 0.02, 0.2 + i * 0.5), bevel=0.002)      # shelves (3 rows)
    # front lip rails
    for i in range(3):
        box(bm, (w - 0.08, 0.02, 0.03), (0, -d / 2 + 0.02, 0.2 + i * 0.5 + 0.03))
    return bm, False


def scale_station(rng):
    bm = bmesh.new()
    box(bm, (0.34, 0.3, 0.045), (0, 0, 0.0225), bevel=0.006)
    cyl(bm, 0.115, 0.012, (0, -0.02, 0.045), segments=24, bevel=0.003)
    disp = lib.bm_box((0.2, 0.03, 0.09), (0, 0.12, 0.09))
    lib.bm_bevel(disp, 0.004, segments=1)
    lib.bm_transform(disp, Matrix.Translation((0, 0.12, 0.09)) @ Matrix.Rotation(math.radians(-25), 4, "X") @ Matrix.Translation((0, -0.12, -0.09)))
    lib.bm_append(bm, disp)
    return bm, False


def tablet(rng):
    bm = bmesh.new()
    slab = lib.bm_box((0.26, 0.012, 0.18), (0, 0, 0.09))
    lib.bm_bevel(slab, 0.004, segments=2)
    lib.bm_transform(slab, Matrix.Rotation(math.radians(-20), 4, "X"))
    lib.bm_append(bm, slab)
    # stand wedge
    box(bm, (0.16, 0.06, 0.02), (0, 0.04, 0.01))
    box(bm, (0.16, 0.02, 0.1), (0, 0.075, 0.05))
    lib.bm_origin_to_base(bm)
    return bm, False


def tray(rng):
    bm = bmesh.new()
    w, d, h, t = 0.5, 0.36, 0.07, 0.012
    box(bm, (w, d, t), (0, 0, t / 2), bevel=0.002)
    box(bm, (w, t, h), (0, d / 2 - t / 2, h / 2), bevel=0.002)
    box(bm, (w, t, h), (0, -d / 2 + t / 2, h / 2), bevel=0.002)
    box(bm, (t, d, h), (w / 2 - t / 2, 0, h / 2), bevel=0.002)
    box(bm, (t, d, h), (-w / 2 + t / 2, 0, h / 2), bevel=0.002)
    return bm, False


def bucket(rng):
    bm = bmesh.new()
    outer = lib.bm_cylinder(0.12, 0.3, segments=18, radius_top=0.145)
    lib.bm_append(bm, outer)
    inner = lib.bm_cylinder(0.105, 0.29, segments=18, radius_top=0.13, center=(0, 0, 0.02))
    for f in inner.faces:
        f.normal_flip()
    lib.bm_append(bm, inner)
    return bm, True


def stool(rng):
    bm = bmesh.new()
    cyl(bm, 0.17, 0.04, (0, 0, 0.62), segments=18, bevel=0.006)
    for i in range(4):
        a = math.radians(45 + 90 * i)
        leg = lib.bm_cylinder(0.018, 0.63, segments=8)
        m = Matrix.Translation((math.cos(a) * 0.12, math.sin(a) * 0.12, 0)) @ Matrix.Rotation(math.radians(6), 4, (-math.sin(a), math.cos(a), 0))
        lib.bm_append(bm, leg, m)
    return bm, False


def task_lamp(rng):
    bm = bmesh.new()
    cyl(bm, 0.09, 0.025, (0, 0, 0), segments=18, bevel=0.004)
    arm1 = lib.bm_cylinder(0.01, 0.42, segments=8)
    lib.bm_append(bm, arm1, Matrix.Translation((0, 0, 0.02)) @ Matrix.Rotation(math.radians(-25), 4, "X"))
    joint = lib.bm_icosphere(0.02, 1, center=(0, 0.177, 0.4))
    lib.bm_append(bm, joint)
    arm2 = lib.bm_cylinder(0.01, 0.34, segments=8)
    lib.bm_append(bm, arm2, Matrix.Translation((0, 0.177, 0.4)) @ Matrix.Rotation(math.radians(115), 4, "X"))
    shade = lib.bm_cylinder(0.035, 0.14, segments=18, radius_top=0.09)
    for f in shade.faces:
        pass
    lib.bm_append(bm, shade, Matrix.Translation((0, 0.49, 0.29)) @ Matrix.Rotation(math.radians(140), 4, "X"))
    return bm, True


def pegboard(rng):
    bm = bmesh.new()
    box(bm, (1.2, 0.02, 0.8), (0, 0, 0.4), bevel=0.003)
    for i in range(6):
        for j in range(3):
            cyl(bm, 0.006, 0.05, (-0.5 + i * 0.2, -0.01, 0.15 + j * 0.25), segments=6, matrix=Matrix.Rotation(math.radians(90), 4, "X"))
    return bm, False


def window_frame(rng):
    bm = bmesh.new()
    w, h, t = 1.2, 1.0, 0.08
    box(bm, (w, t, 0.06), (0, 0, 0.03), bevel=0.003)
    box(bm, (w, t, 0.06), (0, 0, h - 0.03), bevel=0.003)
    box(bm, (0.06, t, h), (-w / 2 + 0.03, 0, h / 2), bevel=0.003)
    box(bm, (0.06, t, h), (w / 2 - 0.03, 0, h / 2), bevel=0.003)
    box(bm, (0.04, 0.04, h), (0, 0, h / 2))
    box(bm, (w, 0.04, 0.04), (0, 0, h / 2))
    box(bm, (w + 0.16, 0.12, 0.05), (0, 0.0, -0.025), bevel=0.004)  # sill
    return bm, False


def door(rng):
    bm = bmesh.new()
    w, h = 0.9, 2.05
    box(bm, (w, 0.05, h), (0, 0, h / 2), bevel=0.004)
    for i in range(2):
        box(bm, (w - 0.2, 0.012, 0.75), (0, -0.03, 0.55 + i * 0.95), bevel=0.003)
    box(bm, (0.08, 0.1, 2.15), (-w / 2 - 0.04, 0, 2.15 / 2))
    box(bm, (0.08, 0.1, 2.15), (w / 2 + 0.04, 0, 2.15 / 2))
    box(bm, (w + 0.16, 0.1, 0.08), (0, 0, 2.15 - 0.04))
    cyl(bm, 0.012, 0.09, (w / 2 - 0.1, -0.03, 1.0), segments=8, matrix=Matrix.Rotation(math.radians(90), 4, "X"))
    box(bm, (0.1, 0.02, 0.025), (w / 2 - 0.15, -0.12, 1.0), bevel=0.004)
    return bm, False


def saw_teaser(rng):
    """A covered machine under a tarp: future precision saw station."""
    bm = bmesh.new()
    box(bm, (1.0, 0.62, 0.08), (0, 0, 0.04), bevel=0.01)
    body = lib.bm_box((0.9, 0.55, 0.95), (0, 0, 0.55))
    lib.bm_bevel(body, 0.06, segments=2)
    lib.bm_append(bm, body)
    for _ in range(5):
        r = rng.uniform(0.12, 0.24)
        s = lib.bm_icosphere(r, 2, center=(rng.uniform(-0.3, 0.3), rng.uniform(-0.15, 0.15), 1.0 + rng.uniform(-0.05, 0.1)))
        lib.bm_append(bm, s)
    return bm, True


def pallet(rng):
    bm = bmesh.new()
    for i in range(3):
        box(bm, (0.1, 0.8, 0.08), (-0.55 + i * 0.55, 0, 0.06), bevel=0.003)
    for i in range(5):
        box(bm, (1.2, 0.1, 0.02), (0, -0.35 + i * 0.175, 0.11), bevel=0.002)
    for i in range(3):
        box(bm, (1.2, 0.1, 0.02), (0, -0.35 + i * 0.35, 0.01), bevel=0.002)
    return bm, False


def cardboard_box(rng):
    bm = bmesh.new()
    b = lib.bm_box((0.5, 0.4, 0.35), (0, 0, 0.175))
    lib.bm_bevel(b, 0.008, segments=1)
    lib.bm_append(bm, b)
    box(bm, (0.5, 0.004, 0.36), (0, 0, 0.18))  # tape line seam (thin)
    return bm, False


def label_stand(rng):
    bm = bmesh.new()
    card = lib.bm_box((0.07, 0.003, 0.035), (0, 0, 0.03))
    lib.bm_transform(card, Matrix.Rotation(math.radians(-18), 4, "X"))
    lib.bm_append(bm, card)
    box(bm, (0.04, 0.02, 0.01), (0, 0.006, 0.005))
    return bm, False


PROPS = [
    ("prop_hammer", hammer, 201),
    ("prop_chisel", lambda r: chisel(r, False), 202),
    ("prop_chisel_fine", lambda r: chisel(r, True), 203),
    ("prop_workbench", workbench, 204),
    ("prop_cradle", cradle, 205),
    ("prop_crate_body", crate_body, 206),
    ("prop_crate_lid", crate_lid, 207),
    ("prop_shelf_unit", shelf_unit, 208),
    ("prop_display_cabinet", display_cabinet, 209),
    ("prop_scale_station", scale_station, 210),
    ("prop_tablet", tablet, 211),
    ("prop_tray", tray, 212),
    ("prop_bucket", bucket, 213),
    ("prop_stool", stool, 214),
    ("prop_task_lamp", task_lamp, 215),
    ("prop_pegboard", pegboard, 216),
    ("prop_window_frame", window_frame, 217),
    ("prop_door", door, 218),
    ("prop_saw_teaser", saw_teaser, 219),
    ("prop_pallet", pallet, 220),
    ("prop_cardboard_box", cardboard_box, 221),
    ("prop_label_stand", label_stand, 222),
]


def build_all():
    lib.reset_scene()
    lib.ensure_dir(OUT_DIR)
    built = []
    for name, builder, seed in PROPS:
        rng = random.Random(seed)
        bm, smooth = builder(rng)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        lib.bm_box_uv(bm, scale=UV_SCALE)
        if name not in ("prop_chisel", "prop_chisel_fine", "prop_tablet"):
            lib.bm_origin_to_base(bm, center_xy=(name not in ("prop_hammer",)))
        else:
            lib.bm_origin_to_base(bm, center_xy=True)
        lib.validate_bmesh(TAG, name, bm, require_manifold=False)
        obj = lib.object_from_bmesh(name, bm, smooth=smooth)
        lib.apply_transforms(obj)
        mesh = obj.data
        xs = [v.co.x for v in mesh.vertices]
        ys = [v.co.y for v in mesh.vertices]
        zs = [v.co.z for v in mesh.vertices]
        lib.log(TAG, f"{name}: {len(mesh.vertices)} verts / {len(mesh.polygons)} faces  size={max(xs)-min(xs):.2f}x{max(ys)-min(ys):.2f}x{max(zs)-min(zs):.2f} base_z={min(zs):.4f}")
        if len(mesh.vertices) > 6000:
            lib.fail(TAG, f"{name}: too dense ({len(mesh.vertices)} verts)")
        lib.export_fbx([obj], os.path.join(OUT_DIR, name + ".fbx"), tag=TAG)
        built.append(name)
        bpy.data.objects.remove(obj)
        bpy.data.meshes.remove(mesh)
    return built


def main():
    try:
        built = build_all()
    except SystemExit:
        raise
    except Exception:
        traceback.print_exc()
        sys.exit(1)
    lib.log(TAG, f"OK - {len(built)} props exported to {os.path.relpath(OUT_DIR, lib.REPO_ROOT)}")


if __name__ == "__main__":
    main()

"""
Geode Empire - crystal archetype mesh generator.

Run headlessly:
    ./Tools/blender.sh --background --python Tools/Blender/gen_crystals.py

Produces one FBX per archetype under Geode/Assets/GeodeEmpire/Models/Crystals/.
Every archetype:
  * grows along +Z in Blender (=> +Y in Unity after axis conversion),
  * has its origin at the base centre (attachment point),
  * is roughly 1 unit tall / <= 1 unit wide so Unity can scale it uniformly,
  * is flat shaded with small bevels so facets catch light,
  * has box-projected UVs.
Deterministic: fixed seeds, no dependence on session state.
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

TAG = "gen_crystals"
OUT_DIR = os.path.join(lib.UNITY_ASSETS, "Models", "Crystals")


# ---------------------------------------------------------------------------
# Archetypes (each returns a bmesh, +Z up, base near z=0)
# ---------------------------------------------------------------------------
def quartz_point(rng, radius=0.18, prism_h=0.68, term_h=0.32, ditrigonal=0.14, term_alt=0.07,
                 jitter_deg=5.0, apex_off=0.05):
    n = 6
    radii = [radius * (1 + ditrigonal * (1 if i % 2 == 0 else -1)) for i in range(n)]
    angs = [math.radians(60 * i + rng.uniform(-jitter_deg, jitter_deg)) for i in range(n)]
    verts = []
    for i in range(n):
        verts.append((radii[i] * math.cos(angs[i]), radii[i] * math.sin(angs[i]), 0.0))
    for i in range(n):
        z = prism_h + (term_alt if i % 2 == 0 else -term_alt) * rng.uniform(0.6, 1.2)
        verts.append((radii[i] * math.cos(angs[i]), radii[i] * math.sin(angs[i]), z))
    verts.append((rng.uniform(-apex_off, apex_off) * radius, rng.uniform(-apex_off, apex_off) * radius,
                  prism_h + term_h))
    faces = [tuple(reversed(range(n)))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
        faces.append((n + i, n + j, 2 * n))
    return lib.bm_from_pydata(verts, faces)


def cube(rng, size=0.6):
    bm = lib.bm_box(size, center=(0, 0, size / 2))
    # slight non-cubic distortion so instances look less mechanical
    m = Matrix.Diagonal((rng.uniform(0.92, 1.08), rng.uniform(0.92, 1.08), rng.uniform(0.95, 1.05), 1.0))
    lib.bm_transform(bm, m)
    return bm


def octahedron(rng, size=0.55):
    s = size
    pts = [(s, 0, 0), (-s, 0, 0), (0, s, 0), (0, -s, 0), (0, 0, s), (0, 0, -s)]
    bm = lib.bm_convex_hull(pts)
    bm.faces.ensure_lookup_table()
    lib.bm_orient_face_down(bm, bm.faces[0])
    return bm


def rhombohedron(rng, edge=0.55, alpha_deg=78.0):
    a_ang = math.radians(alpha_deg)
    ca, sa = math.cos(a_ang), math.sin(a_ang)
    a = Vector((1, 0, 0))
    b = Vector((ca, sa, 0))
    cy = (ca - ca * ca) / sa
    cz = math.sqrt(max(1e-6, 1 - ca * ca - cy * cy))
    c = Vector((ca, cy, cz))
    o = Vector((0, 0, 0))
    pts = [o, a, b, c, a + b, a + c, b + c, a + b + c]
    pts = [p * edge for p in pts]
    return lib.bm_convex_hull(pts)


def dogtooth(rng, base_r=0.09, ring_r=0.19, ring_z=0.28, height=1.0, alt=0.06):
    n = 6
    verts = []
    for i in range(n):
        a = math.radians(60 * i)
        verts.append((base_r * math.cos(a), base_r * math.sin(a), 0.0))
    for i in range(n):
        a = math.radians(60 * i + 30)
        z = ring_z + (alt if i % 2 == 0 else -alt)
        verts.append((ring_r * math.cos(a), ring_r * math.sin(a), z))
    verts.append((0.0, 0.0, height))
    faces = [tuple(reversed(range(n)))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
        faces.append((n + i, n + j, 2 * n))
    return lib.bm_from_pydata(verts, faces)


def blade(rng, width=0.36, thick=0.09, body_h=0.78, tip_h=1.0):
    hw, ht = width / 2, thick / 2
    verts = [
        (-hw, -ht, 0), (hw, -ht, 0), (hw, ht, 0), (-hw, ht, 0),
        (-hw, -ht, body_h), (hw, -ht, body_h), (hw, ht, body_h), (-hw, ht, body_h),
        (-hw * 0.7, 0, tip_h), (hw * 0.7, 0, tip_h),
    ]
    faces = [
        (0, 3, 2, 1), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7),
        (4, 5, 9, 8), (6, 7, 8, 9), (5, 6, 9), (7, 4, 8),
    ]
    return lib.bm_from_pydata(verts, faces)


def needle(rng):
    return quartz_point(rng, radius=0.035, prism_h=0.9, term_h=0.1, ditrigonal=0.0, term_alt=0.0, jitter_deg=0)


def pyritohedron(rng, size=0.32):
    phi = (1 + math.sqrt(5)) / 2
    pts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                pts.append((sx, sy, sz))
    for s1 in (-1, 1):
        for s2 in (-1, 1):
            pts.append((0, s1 / phi, s2 * phi))
            pts.append((s1 / phi, s2 * phi, 0))
            pts.append((s1 * phi, 0, s2 / phi))
    pts = [(p[0] * size, p[1] * size, p[2] * size) for p in pts]
    bm = lib.bm_convex_hull(pts)
    bm.faces.ensure_lookup_table()
    lib.bm_orient_face_down(bm, bm.faces[0])
    return bm


def druzy_tile(rng, disc_r=0.5, count=16):
    bm = lib.bm_cylinder(disc_r, 0.05, segments=14)
    for _ in range(count):
        r = rng.uniform(0.0, disc_r * 0.82)
        a = rng.uniform(0, 2 * math.pi)
        px, py = r * math.cos(a), r * math.sin(a)
        h = rng.uniform(0.12, 0.3)
        w = rng.uniform(0.06, 0.11)
        pt = quartz_point(rng, radius=w, prism_h=h * 0.65, term_h=h * 0.35)
        tilt = Matrix.Rotation(math.radians(rng.uniform(0, 28)), 4, "X") @ Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z")
        m = Matrix.Translation((px, py, 0.03)) @ Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z") @ tilt
        lib.bm_append(bm, pt, m)
    return bm


def quartz_cluster(rng, count=5):
    bm = lib.bm_cylinder(0.22, 0.04, segments=10)
    for i in range(count):
        r = rng.uniform(0.0, 0.14)
        a = rng.uniform(0, 2 * math.pi)
        h = rng.uniform(0.45, 1.0) if i == 0 else rng.uniform(0.3, 0.7)
        w = rng.uniform(0.09, 0.15)
        pt = quartz_point(rng, radius=w, prism_h=h * 0.68, term_h=h * 0.32)
        tilt = Matrix.Rotation(math.radians(rng.uniform(0, 35)), 4, "X") @ Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z")
        m = Matrix.Translation((r * math.cos(a), r * math.sin(a), 0.02)) @ Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z") @ tilt
        lib.bm_append(bm, pt, m)
    return bm


def botryoidal(rng, disc_r=0.5, count=7):
    bm = lib.bm_cylinder(disc_r, 0.04, segments=14)
    for _ in range(count):
        r = rng.uniform(0.0, disc_r * 0.6)
        a = rng.uniform(0, 2 * math.pi)
        rad = rng.uniform(0.16, 0.3)
        sph = lib.bm_icosphere(rad, subdivisions=2, center=(r * math.cos(a), r * math.sin(a), rad * 0.55))
        lib.bm_append(bm, sph)
    return bm


def aragonite_spray(rng, count=13):
    bm = lib.bm_icosphere(0.09, subdivisions=1, center=(0, 0, 0.06))
    for i in range(count):
        length = rng.uniform(0.45, 1.0)
        nd = quartz_point(rng, radius=0.04, prism_h=length * 0.9, term_h=length * 0.1, ditrigonal=0.0, term_alt=0.0, jitter_deg=0)
        # direction within ~70 degrees of +Z
        theta = math.radians(rng.uniform(8, 70))
        phi = rng.uniform(0, 2 * math.pi)
        d = Vector((math.sin(theta) * math.cos(phi), math.sin(theta) * math.sin(phi), math.cos(theta)))
        rot = Vector((0, 0, 1)).rotation_difference(d).to_matrix().to_4x4()
        lib.bm_append(bm, nd, Matrix.Translation((0, 0, 0.05)) @ rot)
    return bm


def calcite_nailhead(rng):
    """Squat hexagonal calcite with flat rhombohedral cap."""
    return quartz_point(rng, radius=0.3, prism_h=0.4, term_h=0.18, ditrigonal=0.05, term_alt=0.09, jitter_deg=3)


ARCHETYPES = [
    # (name, builder, bevel width, seed, smooth)
    ("crystal_quartz_point", quartz_point, 0.012, 101, False),
    ("crystal_quartz_stubby", lambda r: quartz_point(r, radius=0.3, prism_h=0.45, term_h=0.35, ditrigonal=0.1), 0.014, 102, False),
    ("crystal_quartz_cluster", quartz_cluster, 0.008, 103, False),
    ("crystal_cube", cube, 0.02, 104, False),
    ("crystal_octahedron", octahedron, 0.018, 105, False),
    ("crystal_rhomb", rhombohedron, 0.016, 106, False),
    ("crystal_dogtooth", dogtooth, 0.008, 107, False),
    ("crystal_nailhead", calcite_nailhead, 0.012, 108, False),
    ("crystal_blade", blade, 0.008, 109, False),
    ("crystal_needle", needle, 0.0, 110, False),
    ("crystal_pyritohedron", pyritohedron, 0.014, 111, False),
    ("crystal_druzy_tile", druzy_tile, 0.0, 112, False),
    ("crystal_botryoidal", botryoidal, 0.0, 113, True),
    ("crystal_aragonite_spray", aragonite_spray, 0.0, 114, False),
]


def build_all():
    lib.reset_scene()
    lib.ensure_dir(OUT_DIR)
    built = []
    for name, builder, bevel, seed, smooth in ARCHETYPES:
        rng = random.Random(seed)
        bm = builder(rng)
        if bevel > 0:
            lib.bm_bevel(bm, bevel, segments=1)
        lib.bm_box_uv(bm, scale=1.0)
        lib.bm_origin_to_base(bm)
        # compound tiles are intentionally overlapping (non-manifold) - skip that check for them
        lib.validate_bmesh(TAG, name, bm, require_manifold=name not in (
            "crystal_druzy_tile", "crystal_botryoidal", "crystal_aragonite_spray", "crystal_quartz_cluster"))
        obj = lib.object_from_bmesh(name, bm, smooth=smooth)
        lib.apply_transforms(obj)
        mesh = obj.data
        zs = [v.co.z for v in mesh.vertices]
        xs = [v.co.x for v in mesh.vertices]
        ys = [v.co.y for v in mesh.vertices]
        lib.log(TAG, f"{name}: {len(mesh.vertices)} verts / {len(mesh.polygons)} faces, "
                     f"h={max(zs):.3f} w={max(xs)-min(xs):.3f}x{max(ys)-min(ys):.3f} base_z={min(zs):.4f}")
        if abs(min(zs)) > 1e-5:
            lib.fail(TAG, f"{name}: base not at z=0")
        if len(mesh.vertices) > 900:
            lib.fail(TAG, f"{name}: too dense ({len(mesh.vertices)} verts)")
        lib.export_fbx([obj], os.path.join(OUT_DIR, name + ".fbx"), tag=TAG)
        built.append(name)
        # keep scene small: remove after export
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
    lib.log(TAG, f"OK - {len(built)} crystal archetypes exported to {os.path.relpath(OUT_DIR, lib.REPO_ROOT)}")


if __name__ == "__main__":
    main()

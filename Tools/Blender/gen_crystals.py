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
OUT_DIR = os.environ.get("CRYSTAL_OUT", os.path.join(lib.UNITY_ASSETS, "Models", "Crystals"))


# ---------------------------------------------------------------------------
# Archetypes (each returns a bmesh, +Z up, base near z=0)
# ---------------------------------------------------------------------------
def quartz_point(rng, radius=0.18, prism_h=0.68, term_h=0.32, ditrigonal=0.14, term_alt=0.07,
                 jitter_deg=5.0, apex_off=0.05, striations=True):
    """Hexagonal prism with a six-face termination. The prism carries faint horizontal striations (real quartz
    prism faces are striated at right angles to the c-axis), so the faces catch light in bands instead of reading
    as one flat polygon."""
    n = 6
    radii = [radius * (1 + ditrigonal * (1 if i % 2 == 0 else -1)) for i in range(n)]
    angs = [math.radians(60 * i + rng.uniform(-jitter_deg, jitter_deg)) for i in range(n)]
    rings = 7 if striations and prism_h > 0.3 else 1
    verts = []
    for k in range(rings + 1):
        t = k / rings
        z = prism_h * t
        stri = 1.0 + (0.006 * (1 if k % 2 == 0 else -1) if 0 < k < rings and striations else 0.0)
        for i in range(n):
            zz = z if k < rings else prism_h + (term_alt if i % 2 == 0 else -term_alt) * rng.uniform(0.6, 1.2)
            verts.append((radii[i] * stri * math.cos(angs[i]), radii[i] * stri * math.sin(angs[i]), zz))
    verts.append((rng.uniform(-apex_off, apex_off) * radius, rng.uniform(-apex_off, apex_off) * radius,
                  prism_h + term_h))
    apex = len(verts) - 1
    faces = [tuple(reversed(range(n)))]
    for k in range(rings):
        for i in range(n):
            j = (i + 1) % n
            faces.append((k * n + i, k * n + j, (k + 1) * n + j, (k + 1) * n + i))
    top = rings * n
    for i in range(n):
        j = (i + 1) % n
        faces.append((top + i, top + j, apex))
    return lib.bm_from_pydata(verts, faces)


def cube(rng, size=0.6, corner=0.12):
    """Cube with modified corners (small octahedral faces at the corners, as fluorite and pyrite grow) and a
    slight non-cubic distortion so instances look less mechanical."""
    bm = lib.bm_box(size, center=(0, 0, size / 2))
    if corner > 0:
        bmesh.ops.bevel(bm, geom=list(bm.verts), offset=size * corner, offset_type="OFFSET", segments=1,
                        profile=0.5, affect="VERTICES", clamp_overlap=True)
    m = Matrix.Diagonal((rng.uniform(0.92, 1.08), rng.uniform(0.92, 1.08), rng.uniform(0.95, 1.05), 1.0))
    lib.bm_transform(bm, m)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
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
    return quartz_point(rng, radius=0.035, prism_h=0.9, term_h=0.1, ditrigonal=0.0, term_alt=0.0, jitter_deg=0, striations=False)


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


def druzy_tile(rng, disc_r=0.5, count=30):
    bm = lib.bm_cylinder(disc_r, 0.05, segments=14)
    for _ in range(count):
        r = rng.uniform(0.0, disc_r * 0.82)
        a = rng.uniform(0, 2 * math.pi)
        px, py = r * math.cos(a), r * math.sin(a)
        h = rng.uniform(0.1, 0.32)
        w = rng.uniform(0.05, 0.11)
        pt = quartz_point(rng, radius=w, prism_h=h * 0.65, term_h=h * 0.35, striations=False)
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


def botryoidal(rng, disc_r=0.5, count=11):
    bm = lib.bm_cylinder(disc_r, 0.04, segments=24)
    for _ in range(count):
        r = rng.uniform(0.0, disc_r * 0.62)
        a = rng.uniform(0, 2 * math.pi)
        rad = rng.uniform(0.13, 0.3)
        sph = lib.bm_icosphere(rad, subdivisions=3, center=(r * math.cos(a), r * math.sin(a), rad * 0.5))
        for v in sph.verts:
            v.co.z = rad * 0.5 + (v.co.z - rad * 0.5) * rng.uniform(0.85, 1.0) if False else v.co.z
        lib.bm_append(bm, sph)
    return bm


def aragonite_spray(rng, count=13):
    bm = lib.bm_icosphere(0.09, subdivisions=1, center=(0, 0, 0.06))
    for i in range(count):
        length = rng.uniform(0.45, 1.0)
        nd = quartz_point(rng, radius=0.04, prism_h=length * 0.9, term_h=length * 0.1, ditrigonal=0.0, term_alt=0.0, jitter_deg=0, striations=False)
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


def tabular_plate(rng, width=0.9, thick=0.12, height=0.9, chamfer=0.12):
    """Wulfenite: a thin square tabular crystal standing on one edge, bevelled square outline."""
    hw, ht = width / 2, thick / 2
    c = chamfer
    verts = []
    # outline of the square face in the XZ plane (Y is the thin axis), corners chamfered
    outline = [(-hw + c, 0.0), (hw - c, 0.0), (hw, c), (hw, height - c), (hw - c, height), (-hw + c, height), (-hw, height - c), (-hw, c)]
    for y in (-ht, ht):
        for (x, z) in outline:
            verts.append((x, y, z))
    n = len(outline)
    faces = [tuple(range(n))[::-1], tuple(range(n, 2 * n))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    bm = lib.bm_from_pydata(verts, faces)
    # slight pyramidal doming of the two flat faces so they catch light as facets
    for v in bm.verts:
        d = 1.0 - min(1.0, (abs(v.co.x) / hw) * 0.5 + (abs(v.co.z - height / 2) / (height / 2)) * 0.5)
        v.co.y += math.copysign(ht * 0.35 * d, v.co.y)
    return bm


def dodecahedron(rng, size=0.5):
    """Garnet: rhombic dodecahedron (12 rhombic faces), resting on one face."""
    pts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                pts.append((sx, sy, sz))
    for s in (-2, 2):
        pts.append((s, 0, 0)); pts.append((0, s, 0)); pts.append((0, 0, s))
    k = size / 2.0
    pts = [(p[0] * k * rng.uniform(0.97, 1.03), p[1] * k * rng.uniform(0.97, 1.03), p[2] * k) for p in pts]
    bm = lib.bm_convex_hull(pts)
    bm.faces.ensure_lookup_table()
    lib.bm_orient_face_down(bm, bm.faces[0])
    return bm


def trigonal_prism(rng, radius=0.14, height=1.0, sides=12, bulge=0.22, term=0.08):
    """Tourmaline: a striated prism with a rounded-triangular section and a flat, slightly pyramidal termination."""
    verts = []
    for i in range(sides):
        a = 2 * math.pi * i / sides
        r = radius * (1.0 + bulge * math.cos(3 * a)) * (1.0 + 0.06 * (i % 2))   # striation ridges
        verts.append((r * math.cos(a), r * math.sin(a), 0.0))
    for i in range(sides):
        a = 2 * math.pi * i / sides
        r = radius * (1.0 + bulge * math.cos(3 * a)) * (1.0 + 0.06 * (i % 2))
        verts.append((r * math.cos(a), r * math.sin(a), height - term))
    verts.append((0.0, 0.0, height))
    faces = [tuple(reversed(range(sides)))]
    for i in range(sides):
        j = (i + 1) % sides
        faces.append((i, j, sides + j, sides + i))
        faces.append((sides + i, sides + j, 2 * sides))
    return lib.bm_from_pydata(verts, faces)


def fishtail(rng):
    """Selenite: a swallowtail twin, two blades leaning apart from a shared base."""
    bm = lib.bm_cylinder(0.16, 0.03, segments=8)
    for sgn in (-1, 1):
        b = blade(rng, width=0.3, thick=0.08, body_h=0.8, tip_h=1.0)
        m = Matrix.Translation((sgn * 0.05, 0.0, 0.02)) @ Matrix.Rotation(math.radians(sgn * 22), 4, "Y")
        lib.bm_append(bm, b, m)
    return bm



# ---------------------------------------------------------------------------
# V5 habits: six more archetypes so the new families are new shapes, not recolours
# ---------------------------------------------------------------------------
def barrel_prism(rng, radius=0.32, height=0.62):
    """Vanadinite / mimetite: short hexagonal prism with slightly convex (barrel) sides and a flat pinacoid top."""
    n = 6
    prof = [(0.0, 0.88), (0.12, 0.97), (0.5, 1.03), (0.88, 0.97), (1.0, 0.86)]
    rings = []
    angs = [math.radians(60 * i + rng.uniform(-3, 3)) for i in range(n)]
    for (t, k) in prof:
        rings.append([(radius * k * math.cos(a), radius * k * math.sin(a), height * t) for a in angs])
    verts, faces = [], []
    for ring in rings:
        verts.extend(ring)
    for r in range(len(rings) - 1):
        for i in range(n):
            j = (i + 1) % n
            faces.append((r * n + i, r * n + j, (r + 1) * n + j, (r + 1) * n + i))
    faces.append(tuple(reversed(range(n))))
    top = (len(rings) - 1) * n
    faces.append(tuple(range(top, top + n)))
    return lib.bm_from_pydata(verts, faces)


def rosette(rng, count=8):
    """Azurite / barite rose: thin blades radiating from a centre, each leaning outward like petals."""
    bm = lib.bm_icosphere(0.1, subdivisions=1, center=(0, 0, 0.08))
    for i in range(count):
        a = 2 * math.pi * i / count + rng.uniform(-0.15, 0.15)
        b = blade(rng, width=rng.uniform(0.26, 0.36), thick=0.06, body_h=rng.uniform(0.6, 0.85), tip_h=rng.uniform(0.85, 1.0))
        tilt = Matrix.Rotation(math.radians(rng.uniform(25, 55)), 4, "X")
        m = Matrix.Rotation(a, 4, "Z") @ Matrix.Translation((0, 0.04, 0.02)) @ tilt
        lib.bm_append(bm, b, m)
    return bm


def tetragonal_pyramid(rng, radius=0.27, prism_h=0.55, height=1.0):
    """Apophyllite: square prism capped by a steep four-face pyramid, corners slightly truncated."""
    n = 4
    angs = [math.radians(45 + 90 * i) for i in range(n)]
    verts = [(radius * math.cos(a), radius * math.sin(a), 0.0) for a in angs]
    verts += [(radius * math.cos(a), radius * math.sin(a), prism_h) for a in angs]
    verts.append((rng.uniform(-0.02, 0.02), rng.uniform(-0.02, 0.02), height))
    faces = [tuple(reversed(range(n)))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
        faces.append((n + i, n + j, 2 * n))
    bm = lib.bm_from_pydata(verts, faces)
    return bm


def tetrahedron(rng, size=0.4):
    """Chalcopyrite sphenoid: a tetrahedron resting on one face, edges softened."""
    s = size
    pts = [(s, s, s), (s, -s, -s), (-s, s, -s), (-s, -s, s)]
    bm = lib.bm_convex_hull(pts)
    bm.faces.ensure_lookup_table()
    lib.bm_orient_face_down(bm, bm.faces[0])
    m = Matrix.Diagonal((rng.uniform(0.9, 1.1), rng.uniform(0.9, 1.1), 1.0, 1.0))
    lib.bm_transform(bm, m)
    return bm


def sheaf(rng, blades=7):
    """Stilbite bow-tie: two fans of thin blades diverging from a pinched waist."""
    bm = lib.bm_cylinder(0.08, 0.04, segments=10)
    for side in (-1, 1):
        for i in range(blades):
            b = blade(rng, width=0.14, thick=0.05, body_h=0.75, tip_h=0.9)
            spread = math.radians(-32 + 64 * i / max(1, blades - 1))
            m = Matrix.Translation((0, 0, 0.03)) @ Matrix.Rotation(math.radians(side * 12), 4, "Y") @ Matrix.Rotation(spread, 4, "X") @ Matrix.Translation((side * 0.03, 0, 0))
            lib.bm_append(bm, b, m)
    return bm


def hopper_cube(rng, size=0.6, depth=0.2, inner=0.55):
    """Halite / bismuth hopper: a cube whose five free faces are stepped recesses (the edges grew faster than the
    face centres), built explicitly: outer rim square -> sloped step -> recessed inner square."""
    h = size / 2
    verts, faces = [], []
    # the resting face (bottom) stays a flat square
    base = [(-h, -h, 0), (h, -h, 0), (h, h, 0), (-h, h, 0)]
    b0 = len(verts); verts += base
    faces.append((b0, b0 + 3, b0 + 2, b0 + 1))
    # five hopper faces: normal n, and two in-plane axes u, v; corners at z from 0..size
    cz = h   # centre height
    spec = [((0, 0, 1), (1, 0, 0), (0, 1, 0)), ((1, 0, 0), (0, 1, 0), (0, 0, 1)), ((-1, 0, 0), (0, -1, 0), (0, 0, 1)),
            ((0, 1, 0), (-1, 0, 0), (0, 0, 1)), ((0, -1, 0), (1, 0, 0), (0, 0, 1))]
    import mathutils
    for (n, u, v) in spec:
        n = mathutils.Vector(n); u = mathutils.Vector(u); v = mathutils.Vector(v)
        c = mathutils.Vector((0, 0, cz)) + n * h
        outer = [c + u * (-h) + v * (-h), c + u * h + v * (-h), c + u * h + v * h, c + u * (-h) + v * h]
        ci = c - n * (size * depth)
        k = h * inner
        innerq = [ci + u * (-k) + v * (-k), ci + u * k + v * (-k), ci + u * k + v * k, ci + u * (-k) + v * k]
        o0 = len(verts); verts += [tuple(p) for p in outer]
        i0 = len(verts); verts += [tuple(p) for p in innerq]
        for a in range(4):
            b = (a + 1) % 4
            faces.append((o0 + a, o0 + b, i0 + b, i0 + a))
        faces.append((i0 + 0, i0 + 1, i0 + 2, i0 + 3))
    bm = lib.bm_from_pydata(verts, faces)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    m = Matrix.Diagonal((rng.uniform(0.94, 1.06), rng.uniform(0.94, 1.06), 1.0, 1.0))
    lib.bm_transform(bm, m)
    return bm


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
    ("crystal_tabular_plate", tabular_plate, 0.01, 115, False),
    ("crystal_dodecahedron", dodecahedron, 0.012, 116, False),
    ("crystal_trigonal_prism", trigonal_prism, 0.006, 117, False),
    ("crystal_fishtail", fishtail, 0.006, 118, False),
    # V5 habits
    ("crystal_barrel_prism", barrel_prism, 0.012, 119, False),
    ("crystal_rosette", rosette, 0.004, 120, False),
    ("crystal_tetragonal", tetragonal_pyramid, 0.012, 121, False),
    ("crystal_tetrahedron", tetrahedron, 0.02, 122, False),
    ("crystal_sheaf", sheaf, 0.003, 123, False),
    ("crystal_hopper", hopper_cube, 0.008, 124, False),
]


def build_all():
    lib.reset_scene()
    lib.ensure_dir(OUT_DIR)
    built = []
    for name, builder, bevel, seed, smooth in ARCHETYPES:
        rng = random.Random(seed)
        bm = builder(rng)
        if bevel > 0:
            lib.bm_bevel(bm, bevel, segments=2 if len(bm.verts) < 200 else 1)
        lib.bm_box_uv(bm, scale=1.0)
        lib.bm_origin_to_base(bm)
        # compound tiles are intentionally overlapping (non-manifold) - skip that check for them
        lib.validate_bmesh(TAG, name, bm, require_manifold=name not in (
            "crystal_druzy_tile", "crystal_botryoidal", "crystal_aragonite_spray", "crystal_quartz_cluster", "crystal_fishtail",
            "crystal_rosette", "crystal_sheaf"))
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
        if len(mesh.vertices) > 2600:
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

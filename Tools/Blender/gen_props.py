"""
Geode Empire - workshop prop generator (headless bpy), V5 hero-quality rebuild.

Run:
    ./Tools/blender.sh --background --python Tools/Blender/gen_props.py [-- only prop_a,prop_b]

Writes one FBX per prop to Geode/Assets/GeodeEmpire/Models/Props/.
Conventions (all props): metres, origin at the base centre unless noted, +Z up in Blender (=> +Y in Unity),
operator side is -Y. Curved parts carry enough segments to read as curves at arm's length, machined edges are
bevelled with 2-3 segments, faces are smooth with sharp edges marked by angle so the FBX carries split normals,
material slots separate wood / metal / rubber / paint / glass, box-projected UVs (0.5 m per tile), deterministic.

Collision: a builder may return `hq.ColBox` proxies; they export as COL_<n> children that the Unity scene builder
turns into BoxColliders (no MeshCollider needed for a 10k-triangle machine).
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
import hq  # noqa: E402
from hq import T, R, S, ColBox  # noqa: E402

TAG = "gen_props"
OUT_DIR = os.path.join(lib.UNITY_ASSETS, "Models", "Props")
UV_SCALE = 2.0  # 1 tile = 0.5 m


def add(bm, part, matrix=None, mat=0, sharp=32.0, flat=False):
    return hq.add(bm, part, matrix, mat=mat, sharp_deg=sharp, flat=flat)


def box(bm, size, center, bevel=0.0, mat=0, segments=2, matrix=None):
    add(bm, hq.rbox(size, center, bevel=bevel, segments=segments), matrix, mat=mat)


def cyl(bm, radius, height, center, segments=32, radius_top=None, matrix=None, bevel=0.0, mat=0, bsegs=2):
    c = hq.cyl(radius, height, segments=segments, center=(0, 0, 0), radius_top=radius_top, bevel=bevel, bsegs=bsegs)
    m = T(*center)
    if matrix is not None:
        m = m @ matrix
    add(bm, c, m, mat=mat)


def cols_box(size, center):
    return [ColBox(size, center)]


# ---------------------------------------------------------------------------
# Hero tools
# ---------------------------------------------------------------------------
def hammer(rng):
    """Two-pound cross-peen hammer. Hickory handle along +Z from the grip end (origin), oval section swelling toward
    the hand; forged head across X at z=0.312 (striking face +X, cross-peen -X, 0.132 m end to end); the handle
    shows through the eye under a steel wedge. Slot 0 hickory, slot 1 steel."""
    bm = bmesh.new()
    prof = [(0, 0), (0.0125, 0), (0.0158, 0.008), (0.0168, 0.035), (0.016, 0.09), (0.0142, 0.17), (0.0128, 0.25),
            (0.0122, 0.292), (0.0122, 0.331), (0, 0.331)]
    handle = hq.lathe(prof, segments=28)
    for v in handle.verts:
        v.co.y *= 1.32
    add(bm, handle, mat=0)
    body = hq.rbox((0.078, 0.034, 0.036), (0.007, 0, 0.312), bevel=0.0045, segments=3)
    for v in body.verts:
        w = 0.88 + 0.12 * min(1.0, abs(v.co.x - 0.007) / 0.03)
        v.co.y *= w
        v.co.z = 0.312 + (v.co.z - 0.312) * w
    add(bm, body, mat=1)
    face = hq.lathe([(0, 0), (0.0155, 0), (0.0175, 0.004), (0.0175, 0.017), (0.0165, 0.021), (0.011, 0.0235), (0, 0.024)], segments=32)
    add(bm, face, T(0.043, 0, 0.312) @ R(90, "Y"), mat=1)
    peen = hq.loft([hq.ring_rrect(0.036, 0.034, 0.005, 0.0, 16), hq.ring_rrect(0.022, 0.032, 0.004, 0.02, 16),
                    hq.ring_rrect(0.005, 0.03, 0.002, 0.036, 16)])
    add(bm, peen, T(-0.03, 0, 0.312) @ R(-90, "Y"), mat=1, sharp=40)
    box(bm, (0.018, 0.0045, 0.005), (0.007, 0, 0.3305), bevel=0.001, mat=1)
    return bm, None


def lump_hammer(rng):
    """Four-pound lump hammer for the splitting wedge: a squat double-faced head on a short hickory handle. Same
    conventions as the cross-peen hammer: handle along +Z from the grip end (origin), head centre at z 0.29,
    striking faces along +/-X, half-length 0.05."""
    bm = bmesh.new()
    handle = hq.lathe([(0.016, 0), (0.018, 0.02), (0.015, 0.1), (0.014, 0.2), (0.016, 0.255), (0.013, 0.29), (0.013, 0.306)], segments=28, close_bottom=True, close_top=True)
    for v in handle.verts:
        v.co.y *= 0.72
    add(bm, handle, mat=0)
    head = hq.rbox((0.1, 0.038, 0.042), (0, 0, 0.29), bevel=0.006, segments=3)
    add(bm, head, mat=1)
    for sx in (-1, 1):
        add(bm, hq.lathe([(0, 0), (0.017, 0), (0.019, 0.003), (0.019, 0.006), (0, 0.006)], segments=32), T(sx * 0.05, 0, 0.29) @ R(sx * 90, "Y"), mat=1)   # domed faces
    add(bm, hq.rbox((0.012, 0.02, 0.006), (0, 0, 0.308), bevel=0.001, segments=1), mat=1)   # wedge in the eye
    return bm, None


def chisel(rng, fine=False):
    """Cold chisel: origin at the cutting edge, +Z up the shank. Ground edge widening into a flat blade, octagonal
    shank, mushroomed striking cap. The fine chisel is longer, slimmer and carries a knurled grip band."""
    bm = bmesh.new()
    length = 0.17 if not fine else 0.19
    r = 0.0075 if not fine else 0.0062
    n = 16
    edge_w = 0.019 if not fine else 0.012
    rings = [hq.ring_ellipse(edge_w / 2, 0.0008, 0.0, n), hq.ring_ellipse(edge_w / 2 * 0.98, 0.0035, 0.006, n),
             hq.ring_ellipse(edge_w / 2 * 0.9, 0.0058, 0.02, n), hq.ring_oct(r, 0.034, n)]
    add(bm, hq.loft(rings, close_top=False), mat=0, sharp=40)
    top = length - 0.022
    shank = [hq.ring_oct(r, 0.034, n), hq.ring_oct(r, top, n)]
    if fine:
        shank = [hq.ring_oct(r, 0.034, n), hq.ring_oct(r, 0.075, n), hq.ring_oct(r * 1.35, 0.082, n), hq.ring_oct(r * 1.35, 0.108, n),
                 hq.ring_oct(r, 0.115, n), hq.ring_oct(r, top, n)]
    add(bm, hq.loft(shank, close_bottom=False, close_top=False), mat=0, sharp=30)
    cap = hq.lathe([(r * 0.98, 0), (r * 1.1, 0.003), (r * 1.24, 0.01), (r * 1.2, 0.017), (r * 0.85, 0.0215), (0, 0.022)], segments=24)
    add(bm, cap, T(0, 0, top), mat=0)
    if fine:
        knurl = hq.cyl(r * 1.36, 0.024, segments=48, center=(0, 0, 0.083))
        for v in knurl.verts:
            a = math.atan2(v.co.y, v.co.x)
            s = 1.0 + 0.035 * math.cos(a * 24)
            v.co.x *= s
            v.co.y *= s
        add(bm, knurl, mat=0, sharp=60)
    return bm, None


def wedge(rng):
    """Splitting wedge: hardened steel, tip at the origin, 0.17 m up +Z, chamfered flanks and a mushroomed head."""
    bm = bmesh.new()
    body = hq.loft([hq.ring_rrect(0.007, 0.04, 0.002, 0.0, 16), hq.ring_rrect(0.026, 0.046, 0.004, 0.09, 16),
                    hq.ring_rrect(0.034, 0.048, 0.005, 0.13, 16)])
    add(bm, body, mat=0, sharp=40)
    head = hq.loft([hq.ring_rrect(0.034, 0.048, 0.005, 0.13, 16), hq.ring_rrect(0.04, 0.054, 0.007, 0.145, 16),
                    hq.ring_rrect(0.041, 0.055, 0.008, 0.162, 16), hq.ring_rrect(0.034, 0.048, 0.008, 0.17, 16)], close_bottom=False)
    add(bm, head, mat=0, sharp=40)
    return bm, None


def loupe(rng):
    """Folding jeweller's loupe: brass lens barrel on a folding arm and a rounded grip. Origin at the base of the
    grip (+Z up); the lens axis is +Y (toward the player when raised). Lens disc is slot 1."""
    bm = bmesh.new()
    box(bm, (0.019, 0.012, 0.076), (0, 0, 0.038), bevel=0.0055, segments=3, mat=0)
    box(bm, (0.008, 0.008, 0.026), (0, 0, 0.088), bevel=0.0025, segments=2, mat=0)
    cyl(bm, 0.0035, 0.016, (0, 0, 0.084), segments=16, matrix=R(90, "Y") @ T(0, 0, -0.008), mat=0)   # pivot rivet
    barrel = hq.lathe([(0.0205, 0), (0.0218, 0.0015), (0.0218, 0.0125), (0.0205, 0.014), (0.0172, 0.014), (0.0165, 0.0125),
                       (0.0165, 0.0015), (0.0172, 0)], segments=48, loop=True)
    add(bm, barrel, T(0, -0.007, 0.112) @ R(-90, "X"), mat=0)
    lens = hq.lathe([(0, 0), (0.0164, 0), (0.0164, 0.003), (0, 0.003)], segments=48)
    add(bm, lens, T(0, -0.0015, 0.112) @ R(-90, "X"), mat=1)
    return bm, None


def brush(rng):
    """Scrub brush: domed hardwood back (slot 0) and a stiff bristle block (slot 1). Origin at base, bristles down."""
    bm = bmesh.new()
    back = hq.rbox((0.16, 0.062, 0.03), (0, 0, 0.035), bevel=0.011, segments=3)
    for v in back.verts:
        if v.co.z > 0.04:
            v.co.z += 0.006 * (1.0 - (v.co.x / 0.08) ** 2) * (1.0 - (v.co.y / 0.031) ** 2)
    add(bm, back, mat=0)
    for i in range(7):
        for j in range(3):
            x = -0.066 + i * 0.022
            y = -0.02 + j * 0.02
            cyl(bm, 0.0075, 0.022, (x, y, 0.0), segments=10, mat=1, radius_top=0.0065)
    return bm, None


# ---------------------------------------------------------------------------
# Cracking bench
# ---------------------------------------------------------------------------
def workbench(rng):
    """Heavy joiner's bench 1.8 x 0.75, top at 0.9: laminated hardwood slab with a rounded front edge, chamfered
    legs, aprons and stretchers bolted with hex bolts, a slatted lower shelf."""
    bm = bmesh.new()
    box(bm, (1.8, 0.75, 0.07), (0, 0, 0.865), bevel=0.008, segments=3)
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (0.09, 0.09, 0.83), (sx * 0.8, sy * 0.29, 0.415), bevel=0.008, segments=2)
    for sy in (-1, 1):
        box(bm, (1.62, 0.04, 0.11), (0, sy * 0.265, 0.775), bevel=0.003)
        box(bm, (1.62, 0.05, 0.08), (0, sy * 0.29, 0.22), bevel=0.003)
    for sx in (-1, 1):
        box(bm, (0.04, 0.58, 0.11), (sx * 0.755, 0, 0.775), bevel=0.003)
        box(bm, (0.05, 0.58, 0.08), (sx * 0.8, 0, 0.22), bevel=0.003)
    for i in range(5):
        box(bm, (1.5, 0.1, 0.022), (0, -0.24 + i * 0.12, 0.271), bevel=0.003)
    for sx in (-1, 1):
        for sy in (-1, 1):
            for z in (0.745, 0.805):
                add(bm, hq.hex_bolt(0.009, 0.006), T(sx * 0.8, sy * (0.265 + 0.02), z) @ R(-sy * 90, "X"), mat=1)
            add(bm, hq.hex_bolt(0.009, 0.006), T(sx * 0.8, sy * (0.29 + 0.025), 0.22) @ R(-sy * 90, "X"), mat=1)
    cols = [ColBox((1.8, 0.75, 0.07), (0, 0, 0.865)), ColBox((1.62, 0.62, 0.11), (0, 0, 0.775)), ColBox((1.5, 0.6, 0.075), (0, 0, 0.245))]
    for sx in (-1, 1):
        for sy in (-1, 1):
            cols.append(ColBox((0.09, 0.09, 0.83), (sx * 0.8, sy * 0.29, 0.415)))
    return bm, cols


def cradle(rng):
    """Leather sandbag ring on a rubber pad: the rock sits in the hollow. Slot 0 leather, slot 1 rubber. Origin at base."""
    bm = bmesh.new()
    bag = hq.torus(0.085, 0.036, seg_major=64, seg_minor=24, center=(0, 0, 0.036), squash=0.82)
    hq.displace(bag, 0.0045, 28.0, seed=5, octaves=2)
    hq.displace(bag, 0.002, 90.0, seed=9, octaves=1)
    for v in bag.verts:
        if v.co.z < 0.012:
            v.co.z = 0.012 + (v.co.z - 0.012) * 0.3   # flattened where it rests
    add(bm, bag, mat=0, sharp=70)
    seam = hq.tube([(0.121 * math.cos(a), 0.121 * math.sin(a), 0.036 + 0.004 * math.sin(a * 7)) for a in [i / 64 * math.tau for i in range(65)]], 0.0022, segments=8)
    add(bm, seam, mat=0, sharp=70)
    pad = hq.lathe([(0, 0), (0.128, 0), (0.132, 0.004), (0.132, 0.01), (0.128, 0.013), (0, 0.013)], segments=64)
    add(bm, pad, mat=1)
    return bm, None


def heavy_cradle(rng):
    """Heavy cracking cradle: 0.54 m steel plate, three rubber-capped posts standing OUTSIDE a wide sandbag ring
    (slot 1) so a big rock is held by the bag and steadied by the posts. Origin at base; ring inner radius ~0.09."""
    bm = bmesh.new()
    box(bm, (0.54, 0.54, 0.02), (0, 0, 0.01), bevel=0.005, segments=3, mat=0)
    for i in range(3):
        a = math.radians(90 + 120 * i)
        x, y = 0.225 * math.cos(a), 0.225 * math.sin(a)
        cyl(bm, 0.02, 0.1, (x, y, 0.02), segments=32, mat=0)
        cyl(bm, 0.031, 0.022, (x, y, 0.118), segments=32, bevel=0.005, mat=2)
        add(bm, hq.hex_bolt(0.008, 0.005), T(x, y, 0.02), mat=0)
    for sx in (-1, 1):
        for sy in (-1, 1):
            add(bm, hq.hex_bolt(0.009, 0.006), T(sx * 0.245, sy * 0.245, 0.02), mat=0)
    ring = hq.torus(0.14, 0.05, seg_major=72, seg_minor=24, center=(0, 0, 0.068), squash=0.8)
    hq.displace(ring, 0.005, 22.0, seed=7, octaves=2)
    for v in ring.verts:
        if v.co.z < 0.03:
            v.co.z = 0.03 + (v.co.z - 0.03) * 0.3
    add(bm, ring, mat=1, sharp=70)
    return bm, None


def crate_body(rng):
    """Shipping crate 0.62 x 0.46 x 0.34: slats with eased edges on corner cleats, bottom boards, a straw bed."""
    bm = bmesh.new()
    w, d, h, t = 0.62, 0.46, 0.34, 0.018
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (0.04, 0.04, h), (sx * (w / 2 - 0.02), sy * (d / 2 - 0.02), h / 2), bevel=0.004, segments=2)
    for i in range(3):
        z = 0.05 + i * 0.115
        for sy in (-1, 1):
            box(bm, (w, t, 0.085), (0, sy * (d / 2 - t / 2), z + 0.0425), bevel=0.003)
        for sx in (-1, 1):
            box(bm, (t, d - 0.08, 0.085), (sx * (w / 2 - t / 2), 0, z + 0.0425), bevel=0.003)
    for i in range(5):
        box(bm, (w - 0.08, 0.07, t), (0, -d / 2 + 0.07 + i * 0.08, t / 2), bevel=0.003)
    straw = hq.lathe([(0, 0), (0.27, 0), (0.29, 0.02), (0.26, 0.05), (0.14, 0.062), (0, 0.06)], segments=40, center=(0, 0, t))
    for v in straw.verts:
        v.co.x *= 1.05
        v.co.y *= 0.76
    hq.displace(straw, 0.012, 18.0, seed=3, octaves=2, mask=lambda v: 1.0 if v.co.z > t + 0.01 else 0.0)
    add(bm, straw, mat=1, sharp=75)
    return bm, None


def crate_lid(rng):
    bm = bmesh.new()
    w, d, t = 0.62, 0.46, 0.018
    for i in range(4):
        box(bm, (w, 0.105, t), (0, -d / 2 + 0.055 + i * 0.115, t / 2), bevel=0.003)
    box(bm, (0.05, d - 0.02, t), (-0.22, 0, t + t / 2), bevel=0.003)
    box(bm, (0.05, d - 0.02, t), (0.22, 0, t + t / 2), bevel=0.003)
    return bm, None


# ---------------------------------------------------------------------------
# Storage / furniture
# ---------------------------------------------------------------------------
def shelf_unit(rng):
    """Utility shelf 1.0 x 0.36 x 1.85: eased uprights, four shelves on cleats, a back panel."""
    bm = bmesh.new()
    w, d, h = 1.0, 0.36, 1.85
    for sx in (-1, 1):
        box(bm, (0.04, d, h), (sx * (w / 2 - 0.02), 0, h / 2), bevel=0.004, segments=2)
    for i in range(4):
        z = 0.12 + i * 0.55
        box(bm, (w - 0.08, d, 0.03), (0, 0, z), bevel=0.003)
        for sx in (-1, 1):
            box(bm, (0.02, d - 0.04, 0.03), (sx * (w / 2 - 0.05), 0, z - 0.03), bevel=0.002)
    box(bm, (w - 0.08, 0.02, h - 0.1), (0, d / 2 - 0.01, h / 2))
    cols = [ColBox((0.04, d, h), (-(w / 2 - 0.02), 0, h / 2)), ColBox((0.04, d, h), (w / 2 - 0.02, 0, h / 2)), ColBox((w - 0.08, 0.02, h - 0.1), (0, d / 2 - 0.01, h / 2))]
    for i in range(4):
        cols.append(ColBox((w - 0.08, d, 0.03), (0, 0, 0.12 + i * 0.55)))
    return bm, cols


def display_cabinet(rng):
    """Collection cabinet 1.3 x 0.5 x 1.75: plinth, stiles, crown, back panel, three shelves (tops at 0.215 / 0.715 /
    1.215) with front lips, and LED strip housings (slot 1 emissive) under each shelf above."""
    bm = bmesh.new()
    w, d, h = 1.3, 0.5, 1.75
    box(bm, (w, d, 0.12), (0, 0, 0.06), bevel=0.006, segments=3)
    box(bm, (w + 0.02, d + 0.02, 0.025), (0, 0, 0.1325), bevel=0.004)             # plinth cap
    for sx in (-1, 1):
        box(bm, (0.045, d, h - 0.12), (sx * (w / 2 - 0.0225), 0, 0.12 + (h - 0.12) / 2), bevel=0.004, segments=2)
    box(bm, (w, d, 0.05), (0, 0, h - 0.025), bevel=0.005, segments=3)
    box(bm, (w + 0.04, d + 0.04, 0.03), (0, 0, h + 0.015), bevel=0.006, segments=3)   # crown
    box(bm, (w - 0.09, 0.025, h - 0.17), (0, d / 2 - 0.0125, 0.12 + (h - 0.17) / 2))
    for i in range(3):
        z = 0.2 + i * 0.5
        box(bm, (w - 0.09, d - 0.05, 0.03), (0, 0.02, z), bevel=0.003)
        box(bm, (w - 0.09, 0.02, 0.03), (0, -d / 2 + 0.02, z + 0.03), bevel=0.003)
        strip_z = z + 0.5 - 0.015 - 0.012 if i < 2 else h - 0.05 - 0.012
        strip = hq.rbox((w - 0.2, 0.03, 0.012), (0, -0.06, strip_z), bevel=0.003)
        add(bm, strip, mat=1)
    cols = [ColBox((w + 0.02, d + 0.02, 0.145), (0, 0, 0.0725)), ColBox((0.045, d, h - 0.12), (-(w / 2 - 0.0225), 0, 0.12 + (h - 0.12) / 2)),
            ColBox((0.045, d, h - 0.12), (w / 2 - 0.0225, 0, 0.12 + (h - 0.12) / 2)), ColBox((w + 0.04, d + 0.04, 0.08), (0, 0, h - 0.01)),
            ColBox((w - 0.09, 0.025, h - 0.17), (0, d / 2 - 0.0125, 0.12 + (h - 0.17) / 2))]
    for i in range(3):
        z = 0.2 + i * 0.5
        cols.append(ColBox((w - 0.09, d - 0.05, 0.03), (0, 0.02, z)))
        cols.append(ColBox((w - 0.09, 0.02, 0.03), (0, -d / 2 + 0.02, z + 0.03)))
    return bm, cols


def rock_rack(rng):
    """Material storage rack 1.2 x 0.45 x 1.5: angle-iron uprights, three lipped shelves, cross-braced back."""
    bm = bmesh.new()
    L = [(-0.02, -0.02), (0.02, -0.02), (0.02, -0.012), (-0.012, -0.012), (-0.012, 0.02), (-0.02, 0.02)]
    for x in (-0.58, 0.58):
        for y in (-0.21, 0.21):
            prof = [(a * (1 if x > 0 else -1), b * (1 if y > 0 else -1)) for (a, b) in L]
            add(bm, hq.extrude_profile(prof, 1.5, axis="Z", center=(x, y, 0.75)), mat=0, sharp=40)
    for z in (0.12, 0.6, 1.08):
        box(bm, (1.2, 0.45, 0.025), (0, 0, z), bevel=0.002)
        box(bm, (1.2, 0.02, 0.06), (0, -0.215, z + 0.03), bevel=0.002)
        box(bm, (1.2, 0.02, 0.04), (0, 0.215, z + 0.02), bevel=0.002)
        for x in (-0.58, 0.58):
            for y in (-0.21, 0.21):
                add(bm, hq.hex_bolt(0.006, 0.004), T(x + (0.03 if x < 0 else -0.03), y, z + 0.0125), mat=0)
    for sgn in (-1, 1):
        brace = hq.rbox((0.03, 0.006, 1.42), (0, 0, 0), bevel=0.001)
        add(bm, brace, T(0, 0.222, 0.75) @ R(sgn * 38, "Y"), mat=0)
    box(bm, (1.2, 0.015, 0.4), (0, 0.215, 1.3))
    cols = [ColBox((1.2, 0.02, 0.4), (0, 0.215, 1.3))]
    for x in (-0.58, 0.58):
        for y in (-0.21, 0.21):
            cols.append(ColBox((0.04, 0.04, 1.5), (x, y, 0.75)))
    for z in (0.12, 0.6, 1.08):
        cols.append(ColBox((1.2, 0.45, 0.025), (0, 0, z)))
        cols.append(ColBox((1.2, 0.02, 0.06), (0, -0.215, z + 0.03)))
        cols.append(ColBox((1.2, 0.02, 0.04), (0, 0.215, z + 0.02)))
    return bm, cols


def scale_station(rng):
    """Bench parcel scale: rounded body 0.48 x 0.44, stainless platform 0.44 x 0.4 (slot 2) with its top at z=0.054,
    tilted display pod at the back with a screen (slot 1) and three buttons. Origin at the base centre; the
    platform is centred at y=-0.01, the pod at +Y (away from the operator)."""
    bm = bmesh.new()
    box(bm, (0.48, 0.44, 0.04), (0, 0, 0.02), bevel=0.01, segments=3, mat=0)
    plat = hq.rbox((0.44, 0.4, 0.012), (0, -0.01, 0.044), bevel=0.004, segments=2)
    add(bm, plat, mat=2)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl(bm, 0.016, 0.006, (sx * 0.17, -0.01 + sy * 0.15, 0.038), segments=20, mat=3)   # load cells
    pod = hq.rbox((0.21, 0.03, 0.09), (0, 0, 0), bevel=0.008, segments=3)
    add(bm, pod, T(0, 0.2, 0.085) @ R(-25, "X"), mat=0)
    screen = hq.rbox((0.15, 0.004, 0.045), (0, 0, 0.012), bevel=0.001)
    add(bm, screen, T(0, 0.186, 0.085) @ R(-25, "X"), mat=1)
    for i in range(3):
        b = hq.lathe([(0, 0), (0.007, 0), (0.007, 0.003), (0.006, 0.004), (0, 0.004)], segments=20)
        add(bm, b, T(-0.05 + i * 0.05, 0.186, 0.052) @ R(-25 - 90, "X"), mat=3)
    box(bm, (0.14, 0.02, 0.05), (0, 0.205, 0.045), bevel=0.004, mat=0)
    for sx in (-1, 1):
        for sy in (-1, 1):
            cyl(bm, 0.012, 0.006, (sx * 0.22, sy * 0.2, -0.004), segments=20, mat=3)
    bm = _lift(bm, 0.004)
    return bm, [ColBox((0.48, 0.44, 0.044), (0, 0, 0.022)), ColBox((0.44, 0.4, 0.012), (0, -0.01, 0.048)), ColBox((0.21, 0.03, 0.09), (0, 0.198, 0.085))]


def _lift(bm, dz):
    for v in bm.verts:
        v.co.z += dz
    return bm


def tablet(rng):
    """Shop tablet on a folding stand: rounded slab, camera bump (slot 1), stand wedge. Origin at base."""
    bm = bmesh.new()
    slab = hq.rbox((0.26, 0.011, 0.18), (0, 0, 0.09), bevel=0.005, segments=3)
    add(bm, slab, R(-20, "X"), mat=0)
    cam = hq.lathe([(0, 0), (0.006, 0), (0.006, 0.0015), (0, 0.0015)], segments=20)
    add(bm, cam, R(-20, "X") @ T(0.105, 0.0055, 0.16) @ R(-90, "X"), mat=1)
    box(bm, (0.16, 0.06, 0.02), (0, 0.04, 0.01), bevel=0.003, mat=0)
    box(bm, (0.16, 0.02, 0.1), (0, 0.075, 0.05), bevel=0.003, mat=0)
    lib.bm_origin_to_base(bm)
    return bm, None


def tray(rng):
    """Moulded tray 0.5 x 0.36 x 0.07 with rounded corners, a rolled rim and a thick floor. Origin at base."""
    bm = bmesh.new()
    w, d, h, t = 0.5, 0.36, 0.07, 0.012
    outer = [hq.ring_rrect(w, d, 0.04, 0.0, 32), hq.ring_rrect(w, d, 0.04, h - 0.012, 32), hq.ring_rrect(w + 0.012, d + 0.012, 0.046, h - 0.004, 32),
             hq.ring_rrect(w + 0.012, d + 0.012, 0.046, h + 0.002, 32), hq.ring_rrect(w - 0.012, d - 0.012, 0.034, h + 0.002, 32),
             hq.ring_rrect(w - 2 * t, d - 2 * t, 0.028, h - 0.006, 32), hq.ring_rrect(w - 2 * t, d - 2 * t, 0.028, t, 32)]
    add(bm, hq.loft(outer, close_bottom=True, close_top=True), mat=0, sharp=45)
    cols = [ColBox((w - 0.02, d - 0.02, t), (0, 0, t / 2)), ColBox((w + 0.012, 0.024, h), (0, -d / 2 + 0.006, h / 2)), ColBox((w + 0.012, 0.024, h), (0, d / 2 - 0.006, h / 2)),
            ColBox((0.024, d + 0.012, h), (-w / 2 + 0.006, 0, h / 2)), ColBox((0.024, d + 0.012, h), (w / 2 - 0.006, 0, h / 2))]
    return bm, cols


def bucket(rng):
    """Plastic bucket with a rolled rim, a wire bail and a grip. Origin at base."""
    bm = bmesh.new()
    body = hq.lathe([(0, 0), (0.108, 0), (0.118, 0.008), (0.143, 0.295), (0.152, 0.302), (0.152, 0.312), (0.145, 0.315), (0.136, 0.312),
                     (0.134, 0.3), (0.107, 0.028), (0.0, 0.028)], segments=48)
    add(bm, body, mat=0)
    pts = hq.arc_points((0, 0, 0.29), 0.165, 12, 168, 28, plane="XZ")
    add(bm, hq.tube(pts, 0.0035, segments=10), mat=1)
    for sx in (-1, 1):
        box(bm, (0.02, 0.012, 0.03), (sx * 0.152, 0, 0.3), bevel=0.002, mat=1)   # ear tabs on the rim
    add(bm, hq.cyl(0.011, 0.07, segments=16, center=(0, 0, -0.035)), T(0, 0, 0.29 + 0.165) @ R(90, "Y"), mat=0)
    return bm, [ColBox((0.29, 0.29, 0.315), (0, 0, 0.1575))]


def stool(rng):
    """Shop stool: dished round seat, four turned legs splayed 6 degrees, two rings of rungs."""
    bm = bmesh.new()
    seat = hq.lathe([(0, 0), (0.15, 0), (0.17, 0.008), (0.175, 0.025), (0.168, 0.04), (0.12, 0.036), (0, 0.032)], segments=48, center=(0, 0, 0.62))
    add(bm, seat, mat=0)
    leg_prof = [(0, 0), (0.016, 0), (0.019, 0.02), (0.017, 0.2), (0.022, 0.24), (0.024, 0.3), (0.02, 0.36), (0.018, 0.56), (0.016, 0.61), (0.014, 0.63), (0, 0.63)]
    for i in range(4):
        a = math.radians(45 + 90 * i)
        leg = hq.lathe(leg_prof, segments=20)
        m = T(math.cos(a) * 0.125, math.sin(a) * 0.125, 0) @ Matrix.Rotation(math.radians(6), 4, (-math.sin(a), math.cos(a), 0))
        add(bm, leg, m, mat=0)
    for z, rr in ((0.2, 0.135), (0.42, 0.125)):
        for i in range(4):
            a0 = math.radians(45 + 90 * i)
            a1 = a0 + math.radians(90)
            p0 = (math.cos(a0) * rr, math.sin(a0) * rr, z)
            p1 = (math.cos(a1) * rr, math.sin(a1) * rr, z)
            add(bm, hq.tube([p0, p1], 0.009, segments=12), mat=0)
    return bm, [ColBox((0.36, 0.36, 0.66), (0, 0, 0.33))]


def task_lamp(rng):
    """Architect's task lamp: weighted base, two-section sprung arm with knuckle joints, conical shade with a white
    inner (slot 1) and a bulb (slot 2). Origin at the base centre; the shade hangs forward over +Y."""
    bm = bmesh.new()
    base = hq.lathe([(0, 0), (0.085, 0), (0.092, 0.006), (0.092, 0.02), (0.085, 0.027), (0.03, 0.03), (0.026, 0.06), (0, 0.06)], segments=48)
    add(bm, base, mat=0)
    j0 = Vector((0, 0, 0.06))
    j1 = Vector((0, 0.18, 0.42))
    j2 = Vector((0, 0.44, 0.31))
    for a, b in ((j0, j1), (j1, j2)):
        d = (b - a).normalized()
        side = d.cross(Vector((1, 0, 0))).normalized()
        for s in (-1, 1):
            add(bm, hq.tube([tuple(a + side * (s * 0.011)), tuple(b + side * (s * 0.011))], 0.005, segments=12), mat=0)
        # spring: a coil pair alongside the arm
        spring = [tuple(a + d * (0.06 + 0.2 * k / 28) + Vector((0.016, 0, 0)) + side * (0.012 * math.sin(k * 1.2)) + Vector((0, 0, 0)) ) for k in range(29)]
        add(bm, hq.tube(spring, 0.0022, segments=8), mat=0, sharp=70)
    for j in (j0 + Vector((0, 0, 0.0)), j1, j2):
        add(bm, hq.cyl(0.02, 0.04, segments=24, center=(0, 0, -0.02), bevel=0.004), T(*j) @ R(90, "Y"), mat=0)
        add(bm, hq.knob(0.011, 0.012, segments=20, ridges=10), T(*j) @ T(0.02, 0, 0) @ R(90, "Y"), mat=3)
    shade = hq.lathe([(0.035, 0), (0.04, 0.0), (0.095, 0.13), (0.1, 0.14), (0.097, 0.144), (0.09, 0.14), (0.036, 0.012), (0.032, 0.012)], segments=48, loop=True)
    sm = T(*j2) @ R(140, "X")
    add(bm, shade, sm, mat=0)
    inner = hq.lathe([(0.033, 0.013), (0.089, 0.139), (0.001, 0.139), (0.001, 0.013)], segments=48, loop=True)
    add(bm, inner, sm, mat=1)
    add(bm, hq.uv_sphere(0.022, segments=24, rings=12), sm @ T(0, 0, 0.055), mat=2)
    # cord: runs along the arms into the base, then out the back
    cord = [tuple(j2 + Vector((-0.018, 0.0, -0.01)))] + [tuple(j2 + (j1 - j2) * (k / 6) + Vector((-0.018, 0, 0))) for k in range(1, 7)] \
        + [tuple(j1 + (j0 - j1) * (k / 6) + Vector((-0.018, 0, 0))) for k in range(1, 7)] + [(-0.018, -0.02, 0.03), (-0.018, -0.09, 0.006), (-0.018, -0.2, 0.004)]
    add(bm, hq.tube(cord, 0.003, segments=8), mat=3, sharp=70)
    return bm, [ColBox((0.19, 0.19, 0.03), (0, 0, 0.015))]


def pegboard(rng):
    """Tool board 1.2 x 0.8: hardboard with a 25 mm hole grid (dark inset discs, slot 1) on a batten frame, steel hooks."""
    bm = bmesh.new()
    box(bm, (1.2, 0.012, 0.8), (0, 0, 0.4), bevel=0.002, mat=0)
    for sx in (-1, 1):
        box(bm, (0.04, 0.02, 0.8), (sx * 0.58, 0.016, 0.4), bevel=0.003, mat=0)
    box(bm, (1.12, 0.02, 0.04), (0, 0.016, 0.02), bevel=0.003, mat=0)
    box(bm, (1.12, 0.02, 0.04), (0, 0.016, 0.78), bevel=0.003, mat=0)
    for i in range(43):
        for j in range(29):
            x, z = -0.525 + i * 0.025, 0.05 + j * 0.025
            hole = lib.bm_from_pydata([(0.0032 * math.cos(a), 0.0032 * math.sin(a), 0.0) for a in [k * math.tau / 6 for k in range(6)]], [(0, 1, 2, 3, 4, 5)])
            add(bm, hole, T(x, -0.0065, z) @ R(90, "X"), mat=1, flat=True)
    for (x, z) in ((-0.5, 0.65), (-0.3, 0.65), (0.35, 0.6), (0.5, 0.4), (-0.45, 0.3)):
        hook = hq.tube([(x, -0.004, z), (x, -0.02, z), (x, -0.045, z - 0.004), (x, -0.05, z - 0.03)], 0.003, segments=10)
        add(bm, hook, mat=2)
    return bm, [ColBox((1.2, 0.034, 0.8), (0, 0.011, 0.4))]


def window_frame(rng):
    """Painted window 1.2 x 1.0: frame with an outer architrave, a cross of muntins, a deep sill. Front is -Y."""
    bm = bmesh.new()
    w, h, t = 1.2, 1.0, 0.08
    for (size, c) in (((w, t, 0.07), (0, 0, 0.035)), ((w, t, 0.07), (0, 0, h - 0.035)), ((0.07, t, h), (-w / 2 + 0.035, 0, h / 2)), ((0.07, t, h), (w / 2 - 0.035, 0, h / 2))):
        box(bm, size, c, bevel=0.005, segments=3)
    box(bm, (0.035, 0.04, h - 0.14), (0, 0, h / 2), bevel=0.004)
    box(bm, (w - 0.14, 0.04, 0.035), (0, 0, h / 2), bevel=0.004)
    # sill and apron stand proud of the wall face (local y < -0.04 is in front of the frame's thickness)
    box(bm, (w + 0.16, 0.09, 0.045), (0, -0.085, -0.0225), bevel=0.006, segments=3)
    box(bm, (w + 0.1, 0.03, 0.05), (0, -0.055, -0.07), bevel=0.004)     # apron under the sill
    return bm, None


def door(rng):
    """Four-panel painted door 0.9 x 2.05 in an architraved frame: raised panels, three hinges and a lever handle on a
    rosette (slot 1 brass). Origin at the base centre of the leaf; the room is -Y."""
    bm = bmesh.new()
    w, h = 0.9, 2.05
    box(bm, (w, 0.045, h), (0, 0, h / 2), bevel=0.004, segments=2)
    for (px, pz, pw, ph) in ((-0.2, 0.42, 0.28, 0.6), (0.2, 0.42, 0.28, 0.6), (-0.2, 1.4, 0.28, 0.85), (0.2, 1.4, 0.28, 0.85)):
        panel = hq.rbox((pw, 0.02, ph), (px, -0.028, pz), bevel=0.008, segments=3)
        add(bm, panel, mat=0)
        rim = hq.rbox((pw + 0.05, 0.012, ph + 0.05), (px, -0.026, pz), bevel=0.005, segments=2)
        add(bm, rim, mat=0)
    for sx in (-1, 1):
        box(bm, (0.09, 0.11, 2.15), (sx * (w / 2 + 0.045), 0, 2.15 / 2), bevel=0.006, segments=3)
    box(bm, (w + 0.18, 0.11, 0.09), (0, 0, 2.15 - 0.045), bevel=0.006, segments=3)
    for z in (0.3, 1.0, 1.75):
        box(bm, (0.012, 0.09, 0.1), (-w / 2 - 0.004, -0.02, z), bevel=0.002, mat=1)
        cyl(bm, 0.008, 0.11, (-w / 2 - 0.004, -0.02, z - 0.055), segments=16, mat=1)
    add(bm, hq.lathe([(0, 0), (0.03, 0), (0.032, 0.004), (0.028, 0.008), (0, 0.008)], segments=32), T(w / 2 - 0.09, -0.0225, 1.0) @ R(90, "X"), mat=1)
    lever = hq.tube([(w / 2 - 0.09, -0.03, 1.0), (w / 2 - 0.09, -0.07, 1.0), (w / 2 - 0.1, -0.078, 1.0), (w / 2 - 0.2, -0.08, 1.0)], 0.0095, segments=16)
    add(bm, lever, mat=1)
    box(bm, (0.28, 0.012, 0.22), (0, -0.028, 0.11), bevel=0.002, mat=1)     # kick plate
    return bm, [ColBox((w + 0.18, 0.11, 2.15), (0, 0, 2.15 / 2))]


def saw_teaser(rng):
    """The locked saw under a canvas dust cover (V6): the cloth follows the machine under it (cabinet, the blade
    guard's hump, the motor's peak at the back left), wrinkled, with a pleated skirt to the floor, a rope round the
    middle and a paper tag. Slot 0 canvas, slot 1 rope, slot 2 tag."""
    bm = bmesh.new()
    def ring(w, h, r, z, dx=0.0, dy=0.0):
        return [(x + dx, y + dy, zz) for (x, y, zz) in hq.ring_rrect(w, h, r, z, 44)]
    rings = [ring(1.18, 0.7, 0.05, 0.0), ring(1.16, 0.68, 0.05, 0.5), ring(1.14, 0.66, 0.07, 0.86),
             ring(1.02, 0.56, 0.12, 1.0, -0.04, 0.03), ring(0.8, 0.42, 0.14, 1.16, -0.1, 0.06),
             ring(0.56, 0.3, 0.12, 1.3, -0.2, 0.08), ring(0.34, 0.2, 0.09, 1.4, -0.3, 0.1), ring(0.18, 0.12, 0.05, 1.45, -0.36, 0.1)]
    body = hq.loft(rings, close_bottom=True, close_top=True)
    # the guard's hump on the right of the peak: a second ridge pushed up through the cloth
    for v in body.verts:
        dd = math.hypot((v.co.x - 0.05) / 0.28, (v.co.y - 0.05) / 0.16)
        if v.co.z > 0.95:
            v.co.z += max(0.0, 1.0 - dd) * 0.22 * min(1.0, (v.co.z - 0.95) / 0.2)
    hq.displace(body, 0.02, 4.0, seed=11, octaves=2, mask=lambda v: min(1.0, max(0.0, (v.co.z - 0.15) / 0.5)))
    hq.displace(body, 0.006, 18.0, seed=13, octaves=1)
    # hanging folds: pleats round the skirt that fade out toward the top where the cloth is stretched over the machine
    for v in body.verts:
        if v.co.z < 0.86:
            ang = math.atan2(v.co.y, v.co.x)
            fade = 1.0 - v.co.z / 0.86
            k = 0.02 * math.sin(ang * 11.0 + v.co.z * 3.0) * fade
            r = math.hypot(v.co.x, v.co.y)
            if r > 1e-4:
                v.co.x += v.co.x / r * k
                v.co.y += v.co.y / r * k
    add(bm, body, mat=0, sharp=85)
    rope = [(0.59 * math.cos(a) * 1.02, 0.35 * math.sin(a) * 1.02, 0.62 + 0.015 * math.sin(a * 3)) for a in [i / 64 * math.tau for i in range(65)]]
    add(bm, hq.tube(rope, 0.008, segments=10), mat=1, sharp=70)
    add(bm, hq.rbox((0.07, 0.002, 0.045), (0.2, -0.36, 0.585), bevel=0.001, segments=1), mat=2)   # paper tag under the rope
    return bm, [ColBox((1.18, 0.7, 1.45), (0, 0, 0.725))]


def pallet(rng):
    """Stringer pallet 1.2 x 0.8 x 0.12: three stringers, five deck boards, three bottom boards, nail heads."""
    bm = bmesh.new()
    for i in range(3):
        box(bm, (0.1, 0.8, 0.08), (-0.55 + i * 0.55, 0, 0.06), bevel=0.004)
    for i in range(5):
        y = -0.35 + i * 0.175
        box(bm, (1.2, 0.1, 0.02), (0, y, 0.11), bevel=0.003)
        for j in range(3):
            for k in (-1, 1):
                cyl(bm, 0.004, 0.002, (-0.55 + j * 0.55 + k * 0.03, y, 0.12), segments=8, mat=1)
    for i in range(3):
        box(bm, (1.2, 0.1, 0.02), (0, -0.35 + i * 0.35, 0.01), bevel=0.003)
    return bm, [ColBox((1.2, 0.8, 0.12), (0, 0, 0.06))]


def cardboard_box(rng):
    """Cardboard carton 0.5 x 0.4 x 0.35: soft edges, a slightly bowed lid with a tape strip (slot 1)."""
    bm = bmesh.new()
    b = hq.rbox((0.5, 0.4, 0.35), (0, 0, 0.175), bevel=0.01, segments=3)
    for v in b.verts:
        if v.co.z > 0.3:
            v.co.z += 0.008 * (1.0 - (v.co.x / 0.25) ** 2)
    add(bm, b, mat=0, sharp=45)
    box(bm, (0.5 + 0.002, 0.05, 0.36), (0, 0, 0.18), mat=1)
    box(bm, (0.5, 0.004, 0.36 + 0.002), (0, 0, 0.18), mat=0)   # flap seam
    return bm, None


def label_stand(rng):
    """Small folded card on a stand: card (slot 0) leaning 18 degrees."""
    bm = bmesh.new()
    card = hq.rbox((0.07, 0.003, 0.036), (0, 0, 0.03), bevel=0.0012)
    add(bm, card, R(-18, "X"), mat=0)
    box(bm, (0.04, 0.02, 0.01), (0, 0.006, 0.005), bevel=0.002, mat=0)
    return bm, None


def pendant_lamp(rng):
    """Industrial pendant: ceiling rose, cord, socket, spun-steel shade with a white inner (slot 1). Origin at the
    ceiling attachment (top); the shade opening is 0.86 m below it."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0.03, 0), (0.034, -0.01), (0.03, -0.025), (0, -0.025), (0, 0)], segments=32, loop=True), mat=0)
    cord = [(0, 0, -0.02), (0.002, 0.001, -0.2), (0.0, 0.003, -0.4), (0, 0, -0.55)]
    add(bm, hq.tube(hq.bezier(cord[0], cord[1], cord[2], cord[3], 12), 0.004, segments=10), mat=0)
    add(bm, hq.lathe([(0, 0), (0.022, 0), (0.026, -0.01), (0.026, -0.06), (0.02, -0.07), (0, -0.07)], segments=32, center=(0, 0, -0.55)), mat=0)
    shade = hq.lathe([(0.02, -0.62), (0.05, -0.64), (0.07, -0.68), (0.18, -0.82), (0.2, -0.85), (0.202, -0.86), (0.196, -0.862), (0.19, -0.855),
                      (0.17, -0.82), (0.064, -0.685), (0.045, -0.65), (0.02, -0.635)], segments=48, loop=True)
    add(bm, shade, mat=0)
    inner = hq.lathe([(0.0, -0.636), (0.02, -0.636), (0.045, -0.651), (0.064, -0.686), (0.17, -0.821), (0.19, -0.856), (0.001, -0.856)], segments=48, loop=True)
    add(bm, inner, mat=1)
    return bm, None


def wall_shelf(rng):
    """Wall shelf 0.9 x 0.24: board with a rounded front edge on two pressed-steel brackets (slot 1)."""
    bm = bmesh.new()
    box(bm, (0.9, 0.24, 0.028), (0, 0, 0.014), bevel=0.006, segments=3)
    Lp = [(-0.015, -0.2), (0.015, -0.2), (0.015, -0.006), (0.015, 0.006), (0.015, 0.0), (0.2, 0.0), (0.2, 0.0), (0.2, -0.003), (0.015, -0.003), (0.015, -0.2)]
    for sx in (-1, 1):
        prof = [(-0.1, -0.003), (0.1, -0.003), (0.1, -0.009), (-0.088, -0.009), (-0.088, -0.19), (-0.1, -0.19)]
        br = hq.extrude_profile(prof, 0.03, axis="X", center=(sx * 0.36, 0.0, 0.0))
        add(bm, br, mat=1, sharp=40)
        gusset = hq.extrude_profile([(-0.085, -0.012), (0.09, -0.012), (-0.085, -0.17)], 0.006, axis="X", center=(sx * 0.36, 0.0, 0.0))
        add(bm, gusset, mat=1, sharp=40)
    return bm, None


def jar(rng):
    """Glass storage jar with shoulders and a screw lid (slot 1)."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0, 0), (0.04, 0), (0.046, 0.006), (0.047, 0.1), (0.044, 0.118), (0.034, 0.128), (0.033, 0.132), (0, 0.132)], segments=48), mat=0)
    lid = hq.lathe([(0, 0), (0.034, 0), (0.036, 0.003), (0.036, 0.016), (0.034, 0.019), (0, 0.019)], segments=48, center=(0, 0, 0.131))
    for v in lid.verts:
        if 0.003 < v.co.z - 0.131 < 0.016:
            a = math.atan2(v.co.y, v.co.x)
            s = 1.0 + 0.02 * math.cos(a * 24)
            v.co.x *= s
            v.co.y *= s
    add(bm, lid, mat=1, sharp=60)
    return bm, None


def rock_bin(rng):
    """Wooden scrap bin 0.6 x 0.45 x 0.32 with corner posts, full of loose rocks (slot 1)."""
    bm = bmesh.new()
    w, d, h, t = 0.6, 0.45, 0.32, 0.02
    box(bm, (w, d, t), (0, 0, t / 2))
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (0.035, 0.035, h + 0.03), (sx * (w / 2 - 0.0175), sy * (d / 2 - 0.0175), (h + 0.03) / 2), bevel=0.003)
    box(bm, (w, t, h), (0, d / 2 - t / 2, h / 2), bevel=0.003)
    box(bm, (w, t, h), (0, -d / 2 + t / 2, h / 2), bevel=0.003)
    box(bm, (t, d, h), (w / 2 - t / 2, 0, h / 2), bevel=0.003)
    box(bm, (t, d, h), (-w / 2 + t / 2, 0, h / 2), bevel=0.003)
    for i in range(40):
        r = rng.uniform(0.035, 0.07)
        s = lib.bm_icosphere(r, 2, center=(0, 0, 0))
        m = Matrix.Diagonal((rng.uniform(0.8, 1.3), rng.uniform(0.8, 1.2), rng.uniform(0.7, 1.0), 1.0))
        lib.bm_transform(s, m)
        hq.displace(s, r * 0.18, 9.0 / r * 0.3, seed=rng.randint(1, 500), octaves=2)
        zc = 0.08 + rng.uniform(0.0, 0.2) if i < 28 else 0.24 + rng.uniform(0.0, 0.06)   # heaped toward the top
        add(bm, s, T(rng.uniform(-0.23, 0.23), rng.uniform(-0.16, 0.16), zc) @ R(rng.uniform(0, 360), "Z"), mat=1, sharp=50)
    return bm, [ColBox((w, d, h + 0.03), (0, 0, (h + 0.03) / 2))]


def extinguisher(rng):
    """Fire extinguisher: domed steel cylinder (slot 0 red), valve head, squeeze lever, gauge, hose with a nozzle."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0, 0), (0.05, 0), (0.07, 0.01), (0.076, 0.03), (0.076, 0.4), (0.072, 0.43), (0.05, 0.45), (0.025, 0.46), (0, 0.46)], segments=48), mat=0)
    add(bm, hq.lathe([(0, 0), (0.03, 0), (0.03, 0.03), (0.02, 0.035), (0.02, 0.05), (0, 0.05)], segments=32, center=(0, 0, 0.46)), mat=1)
    box(bm, (0.03, 0.15, 0.02), (0, 0.045, 0.52), bevel=0.005, segments=3, mat=1)
    lever = hq.rbox((0.026, 0.15, 0.014), (0, 0.055, 0.0), bevel=0.004, segments=2)
    add(bm, lever, T(0, 0, 0.54) @ R(-10, "X"), mat=1)
    add(bm, hq.lathe([(0, 0), (0.017, 0), (0.018, 0.008), (0.014, 0.012), (0, 0.012)], segments=24), T(0.028, 0, 0.5) @ R(90, "Y"), mat=1)
    # hose and label on the room side (+Y); the bracket plate on the wall side (-Y)
    hose = hq.bezier((0.03, 0.03, 0.48), (0.1, 0.06, 0.4), (0.085, 0.08, 0.15), (0.06, 0.06, 0.1), 20)
    add(bm, hq.tube(hose, 0.011, segments=14), mat=2)
    add(bm, hq.lathe([(0, 0), (0.012, 0), (0.02, 0.06), (0.019, 0.065), (0, 0.065)], segments=24, center=(0, 0, 0)), T(0.06, 0.06, 0.1) @ R(180, "X"), mat=1)
    box(bm, (0.04, 0.06, 0.06), (0, -0.0785, 0.32), mat=2)   # bracket plate on the wall side
    box(bm, (0.12, 0.002, 0.12), (-0.01, 0.077, 0.25), mat=3)   # label
    return bm, [ColBox((0.16, 0.16, 0.58), (0, 0, 0.29))]


def poster_frame(rng):
    """Picture frame 0.62 x 0.62: mitred moulding with a rounded outer edge and a backing board."""
    bm = bmesh.new()
    w, h, t = 0.62, 0.62, 0.025
    for (size, c) in (((w, t, 0.035), (0, 0, h - 0.0175)), ((w, t, 0.035), (0, 0, 0.0175)), ((0.035, t, h), (-w / 2 + 0.0175, 0, h / 2)), ((0.035, t, h), (w / 2 - 0.0175, 0, h / 2))):
        box(bm, size, c, bevel=0.005, segments=3)
    box(bm, (w - 0.02, 0.008, h - 0.02), (0, 0.006, h / 2))
    return bm, None


def wall_clock(rng):
    """Wall clock: spun bezel, dial (slot 1), hands (slot 2) on a centre pin. Faces -Y."""
    bm = bmesh.new()
    bez = hq.lathe([(0.0, 0), (0.15, 0), (0.16, -0.01), (0.162, -0.03), (0.152, -0.035), (0.142, -0.032), (0.142, -0.006), (0, -0.006)], segments=64, loop=True)
    add(bm, bez, R(-90, "X"), mat=0)
    add(bm, hq.lathe([(0, 0), (0.142, 0), (0.142, 0.002), (0, 0.002)], segments=64), T(0, -0.008, 0) @ R(90, "X"), mat=1)
    for i in range(12):
        a = math.radians(i * 30)
        tick = hq.rbox((0.006 if i % 3 else 0.01, 0.002, 0.018 if i % 3 else 0.026), (0, 0, 0.128), bevel=0.0005)
        add(bm, tick, T(0, -0.011, 0) @ Matrix.Rotation(-a, 4, "Y"), mat=2)
    box(bm, (0.008, 0.003, 0.1), (0, -0.012, 0.045), mat=2)
    box(bm, (0.007, 0.003, 0.07), (0.025, -0.015, 0.02), mat=2, matrix=None)
    cyl(bm, 0.006, 0.006, (0, -0.011, 0), segments=16, matrix=R(90, "X"), mat=2)
    return bm, None


def broom(rng):
    """Corn broom: turned handle, sewn head, splayed bristle tufts (slot 1)."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0, 0), (0.011, 0), (0.013, 0.01), (0.012, 1.15), (0.013, 1.19), (0.011, 1.2), (0, 1.2)], segments=20, center=(0, 0, 0.22)), mat=0)
    box(bm, (0.26, 0.055, 0.06), (0, 0, 0.21), bevel=0.008, segments=3, mat=1)
    box(bm, (0.24, 0.05, 0.012), (0, 0, 0.16), bevel=0.003, mat=2)
    for i in range(11):
        x = -0.11 + i * 0.022
        tuft = hq.rbox((0.02, 0.04, 0.17), (x, 0, 0.09), bevel=0.004, segments=2)
        for v in tuft.verts:
            v.co.x += (x / 0.12) * 0.04 * (1.0 - v.co.z / 0.18)
        add(bm, tuft, mat=1, sharp=50)
    return bm, None


def sign_board(rng):
    """Sign plank 0.5 x 0.14: eased edges (scaled non-uniformly by the scene builder to fit its text)."""
    bm = bmesh.new()
    box(bm, (0.5, 0.02, 0.14), (0, 0, 0.07), bevel=0.004, segments=2)
    return bm, None


# ---------------------------------------------------------------------------
# Retail shop fixtures
# ---------------------------------------------------------------------------
def shop_case(rng):
    """Wall display case 1.8 x 0.42 x 1.55: dark frame, plinth, crown, two lit shelves (tops at 0.55 / 1.05), LED
    strip housings (slot 1 emissive). Front is -Y. Origin at base centre."""
    bm = bmesh.new()
    w, d, h = 1.8, 0.5, 1.55
    t = 0.035
    box(bm, (w, d, 0.1), (0, 0, 0.05), bevel=0.006, segments=3)
    box(bm, (w + 0.02, d + 0.02, 0.02), (0, 0, 0.11), bevel=0.004)
    box(bm, (t, d, h), (-w / 2 + t / 2, 0, h / 2), bevel=0.004, segments=2)
    box(bm, (t, d, h), (w / 2 - t / 2, 0, h / 2), bevel=0.004, segments=2)
    box(bm, (w, 0.025, h), (0, d / 2 - 0.0125, h / 2))
    box(bm, (w, d, t), (0, 0, h - t / 2), bevel=0.004, segments=3)
    box(bm, (w + 0.05, d + 0.04, 0.03), (0, 0, h + 0.015), bevel=0.006, segments=3)
    for z in (0.55, 1.05):
        box(bm, (w - 2 * t, d - 0.06, 0.025), (0, 0.03, z - 0.0125), bevel=0.003)
        box(bm, (w - 2 * t, 0.012, 0.03), (0, -d / 2 + 0.03, z + 0.015), bevel=0.002)
    for z in (h - t - 0.012, 1.05 - 0.025 - 0.012):
        add(bm, hq.rbox((w - 2 * t - 0.1, 0.03, 0.012), (0, -0.05, z), bevel=0.003), mat=1)
    cols = [ColBox((w + 0.02, d + 0.02, 0.12), (0, 0, 0.06)), ColBox((t, d, h), (-w / 2 + t / 2, 0, h / 2)), ColBox((t, d, h), (w / 2 - t / 2, 0, h / 2)),
            ColBox((w, 0.025, h), (0, d / 2 - 0.0125, h / 2)), ColBox((w + 0.05, d + 0.04, 0.065), (0, 0, h + 0.0))]
    for z in (0.55, 1.05):
        cols.append(ColBox((w - 2 * t, d - 0.06, 0.025), (0, 0.03, z - 0.0125)))
        cols.append(ColBox((w - 2 * t, 0.012, 0.03), (0, -d / 2 + 0.03, z + 0.015)))
    return bm, cols


def shop_table(rng):
    """Island display table 1.6 x 0.9, top at 0.86 with a felt inlay (slot 1), panelled plinth, kick."""
    bm = bmesh.new()
    box(bm, (1.6, 0.9, 0.05), (0, 0, 0.835), bevel=0.008, segments=3)
    add(bm, hq.rbox((1.54, 0.84, 0.012), (0, 0, 0.866), bevel=0.002), mat=1)
    box(bm, (1.3, 0.7, 0.75), (0, 0, 0.405), bevel=0.006, segments=2)
    for sy in (-1, 1):
        for px in (-0.42, 0.0, 0.42):
            add(bm, hq.rbox((0.34, 0.012, 0.5), (px, sy * 0.354, 0.42), bevel=0.004, segments=2), mat=0)
    for sx in (-1, 1):
        add(bm, hq.rbox((0.012, 0.5, 0.5), (sx * 0.654, 0, 0.42), bevel=0.004, segments=2), mat=0)
    box(bm, (1.56, 0.86, 0.03), (0, 0, 0.075), bevel=0.003)
    return bm, [ColBox((1.6, 0.9, 0.86), (0, 0, 0.43))]


def counter(rng):
    """Checkout counter 1.3 x 0.55 x 0.95: rounded worktop, panelled customer front (slot 1 paint), toe kick."""
    bm = bmesh.new()
    box(bm, (1.3, 0.55, 0.05), (0, 0, 0.925), bevel=0.01, segments=3)
    box(bm, (1.26, 0.5, 0.86), (0, 0, 0.43), bevel=0.005, segments=2)
    box(bm, (1.24, 0.46, 0.05), (0, 0, 0.03))
    for px in (-0.42, 0.0, 0.42):
        add(bm, hq.rbox((0.34, 0.014, 0.6), (px, -0.257, 0.5), bevel=0.005, segments=3), mat=1)
    box(bm, (1.3, 0.04, 0.1), (0, -0.275, 0.85), bevel=0.006, segments=2)
    return bm, [ColBox((1.3, 0.55, 0.95), (0, 0, 0.475))]


def register(rng):
    """POS register: cash drawer with a pull, sloped body, keypad (slot 2 keys), screen on a post (slot 1)."""
    bm = bmesh.new()
    # the cash drawer housing is open at the front: the drawer itself is prop_register_drawer, slid by the register
    box(bm, (0.32, 0.36, 0.09), (0, 0.02, 0.045), bevel=0.006, segments=3)
    body = hq.rbox((0.28, 0.22, 0.12), (0, 0.04, 0.15), bevel=0.006, segments=3)
    for v in body.verts:
        if v.co.z > 0.16 and v.co.y < 0.04:
            v.co.z -= 0.05 * (0.04 - v.co.y) / 0.11
    add(bm, body, mat=0)
    for i in range(4):
        for j in range(3):
            ky = -0.055 + j * 0.03
            key = hq.rbox((0.02, 0.02, 0.008), (0, 0, 0.002), bevel=0.002)
            add(bm, key, T(-0.09 + i * 0.03, ky, 0.211 - 0.05 * (0.04 - ky) / 0.11) @ R(-24.4, "X"), mat=2)
    screen = hq.rbox((0.21, 0.014, 0.12), (0, 0.11, 0.265), bevel=0.004, segments=2)
    add(bm, screen, mat=0)
    add(bm, hq.rbox((0.19, 0.003, 0.1), (0, 0.102, 0.265), bevel=0.001), mat=1)
    box(bm, (0.04, 0.03, 0.06), (0, 0.11, 0.22), bevel=0.004, mat=0)
    return bm, None


def register_drawer(rng):
    """Cash drawer for prop_register: a tray with coin cups and a note well, a front plate with a pull (slot 2). Origin at
    the drawer's rear centre at the housing floor; it slides along -Y (Unity -Z) to open."""
    bm = bmesh.new()
    box(bm, (0.29, 0.33, 0.012), (0, -0.165, 0.008), bevel=0.003, segments=2)
    for sx in (-1, 1):
        box(bm, (0.012, 0.33, 0.05), (sx * 0.14, -0.165, 0.03), bevel=0.002)
    box(bm, (0.29, 0.012, 0.05), (0, -0.006, 0.03), bevel=0.002)
    for i in range(4):
        add(bm, hq.lathe([(0, 0), (0.028, 0), (0.028, 0.03), (0, 0.03)], segments=24), T(-0.1 + i * 0.066, -0.06, 0.014), mat=1)     # coin cups
    for i in range(3):
        box(bm, (0.08, 0.14, 0.006), (-0.09 + i * 0.09, -0.24, 0.02), bevel=0.001, mat=1)                                           # note wells
        # a stack of notes in each well, held by the well's spring clip (slot 3), and a label tab on the divider (slot 2)
        for k in range(5):
            add(bm, hq.rbox((0.068, 0.13, 0.0006), (-0.09 + i * 0.09, -0.24, 0.024 + k * 0.0009), bevel=0.0002, segments=1), R(rng.uniform(-2, 2), "Z"), mat=3)
        box(bm, (0.05, 0.004, 0.02), (-0.09 + i * 0.09, -0.312, 0.035), bevel=0.001, mat=2)
        box(bm, (0.012, 0.13, 0.01), (-0.09 + i * 0.09, -0.24, 0.032), bevel=0.001, mat=1)
    for i in range(4):
        for k in range(rng.randint(3, 9)):
            add(bm, hq.cyl(0.0105 + 0.002 * (i % 2), 0.0018, segments=18, center=(0, 0, 0)), T(-0.1 + i * 0.066 + rng.uniform(-0.008, 0.008), -0.06 + rng.uniform(-0.008, 0.008), 0.016 + k * 0.0019), mat=4)   # coins
    box(bm, (0.3, 0.012, 0.075), (0, -0.336, 0.04), bevel=0.004, segments=2)                                                         # front plate
    box(bm, (0.08, 0.008, 0.012), (0, -0.346, 0.04), bevel=0.003, mat=2)                                                             # pull
    return bm, None


def card_reader(rng):
    """Counter card terminal (V6 checkout): a sloped body on a swivel mount, a display (slot 1), a keypad (slot 2),
    the card slot at the bottom front, and a contactless mark on the top. Origin at the base centre; the display faces
    the customer (-Y)."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0, 0), (0.05, 0), (0.052, 0.006), (0.048, 0.012), (0.02, 0.014), (0, 0.014)], segments=32), mat=0)   # mount foot
    add(bm, hq.cyl(0.018, 0.03, segments=20, center=(0, 0, 0.014)), mat=2)                                              # swivel post
    body = hq.rbox((0.09, 0.15, 0.05), (0, 0.0, 0.07), bevel=0.006, segments=3)
    for v in body.verts:
        if v.co.z > 0.07:
            v.co.z += 0.05 * (v.co.y + 0.075) / 0.15                                                                   # slopes up toward the back
    add(bm, body, mat=0)
    add(bm, hq.rbox((0.074, 0.05, 0.003), (0, 0.035, 0.117), bevel=0.001, segments=1), R(-18.4, "X") @ T(0, 0, 0.0), mat=1)   # display
    for i in range(3):
        for j in range(4):
            add(bm, hq.rbox((0.018, 0.012, 0.004), (0, 0, 0), bevel=0.001, segments=1), T(-0.024 + i * 0.024, -0.052 + j * 0.017, 0.093 + 0.05 * (-0.052 + j * 0.017 + 0.075) / 0.15) @ R(-18.4, "X"), mat=2)
    add(bm, hq.rbox((0.06, 0.02, 0.004), (0, -0.07, 0.048), bevel=0.001, segments=1), mat=3)                             # card slot (dark)
    add(bm, hq.torus(0.008, 0.0015, seg_major=20, seg_minor=6, center=(0.03, 0.06, 0.128)), mat=2)                        # contactless mark
    return bm, None


def bank_card(rng):
    """Payment card 85.6 x 54 x 0.8 mm, a chip (slot 1) and a stripe. Origin at the centre; lies in XY, face up."""
    bm = bmesh.new()
    add(bm, hq.rbox((0.0856, 0.054, 0.0008), (0, 0, 0.0004), bevel=0.0003, segments=1), mat=0)
    add(bm, hq.rbox((0.011, 0.009, 0.0004), (-0.026, 0.008, 0.001), bevel=0.0001, segments=1), mat=1)
    add(bm, hq.rbox((0.0856, 0.008, 0.0002), (0, 0.018, 0.0009), bevel=0.0001, segments=1), mat=2)
    return bm, None


def banknotes(rng):
    """A few folded notes held together (slot 0 paper, slot 1 print band). Origin at the centre of the bundle base."""
    bm = bmesh.new()
    for k in range(4):
        note = hq.rbox((0.15, 0.068, 0.0005), (0, 0, 0.0005 + k * 0.0007), bevel=0.0002, segments=1)
        for v in note.verts:
            v.co.z += 0.003 * math.sin((v.co.x + 0.075) / 0.15 * math.pi) * (1.0 + 0.4 * k)   # a slight curl
        add(bm, note, R(rng.uniform(-6, 6), "Z"), mat=0)
    add(bm, hq.rbox((0.15, 0.02, 0.0004), (0, 0.0, 0.0032), bevel=0.0001, segments=1), mat=1)
    return bm, None


def paper_bag(rng):
    """Kraft paper bag for small pieces: a soft box with a rolled top, creased sides. Origin at the base centre; scaled to
    the piece by the checkout."""
    bm = bmesh.new()
    body = hq.loft([hq.ring_rrect(0.2, 0.12, 0.02, 0.0, 32), hq.ring_rrect(0.2, 0.12, 0.025, 0.14, 32), hq.ring_rrect(0.19, 0.1, 0.03, 0.26, 32), hq.ring_rrect(0.16, 0.05, 0.02, 0.3, 32)], close_bottom=True, close_top=False)
    hq.displace(body, 0.004, 14.0, seed=51, octaves=1)
    add(bm, body, mat=0, sharp=70)
    add(bm, hq.rbox((0.17, 0.06, 0.018), (0, 0, 0.305), bevel=0.008, segments=2), mat=0)   # rolled top
    return bm, [ColBox((0.2, 0.12, 0.31), (0, 0, 0.155))]


def gift_box(rng):
    """Open presentation box with tissue paper for medium pieces (slot 0 card, slot 1 tissue). Origin at the base centre."""
    bm = bmesh.new()
    box(bm, (0.3, 0.24, 0.01), (0, 0, 0.005), bevel=0.002, segments=1)
    for sx in (-1, 1):
        box(bm, (0.01, 0.24, 0.16), (sx * 0.145, 0, 0.08), bevel=0.002, segments=1)
    for sy in (-1, 1):
        box(bm, (0.3, 0.01, 0.16), (0, sy * 0.115, 0.08), bevel=0.002, segments=1)
    tissue = hq.loft([hq.ring_rrect(0.27, 0.21, 0.03, 0.012, 32), hq.ring_rrect(0.29, 0.23, 0.03, 0.1, 32), hq.ring_rrect(0.34, 0.28, 0.04, 0.19, 32)], close_bottom=True, close_top=False)
    hq.displace(tissue, 0.012, 9.0, seed=53, octaves=2)
    add(bm, tissue, mat=1, sharp=80)
    return bm, [ColBox((0.3, 0.24, 0.16), (0, 0, 0.08))]


def price_card(rng):
    """Easel price card 9 x 6 cm (printed face slot 1) on a folded stand. Origin at base."""
    bm = bmesh.new()
    card = hq.rbox((0.09, 0.004, 0.06), (0, -0.01, 0.045), bevel=0.0015)
    add(bm, card, R(-15, "X"), mat=1)
    box(bm, (0.05, 0.03, 0.006), (0, 0.005, 0.003), bevel=0.0015, mat=0)
    return bm, None


# ---------------------------------------------------------------------------
# Lapidary saw bay
# ---------------------------------------------------------------------------
# ---- 14-inch trim saw ---------------------------------------------------------------------------------------
# Layout (Blender: X = feed axis, +Y away from the operator, Z up; Unity: X, Z, Y). Blade plane XZ at y=SAW_BLADE_Y,
# arbor at z=SAW_ARBOR_Z. The carriage vise rides the rails on the operator side (y < blade) and feeds toward -X;
# the rock is clamped by its near half between two jaws that squeeze ALONG X, and overhangs across the blade plane,
# so nothing on the carriage ever crosses the blade. Behind the blade the arbor runs to a compact pillow-block
# bearing whose underside sits above the tallest rock the blade can pass (arbor - flange radius), the motor and
# belt live higher and further back, the hood covers the top half of the blade only.
SAW_BLADE_R = 0.178      # 14-inch diamond blade
SAW_BLADE_Y = 0.05       # blade plane (Unity z)
SAW_RAIL_TOP = 0.915
SAW_SLED_TOP = 0.02
SAW_ARBOR_Z = SAW_RAIL_TOP + SAW_SLED_TOP + SAW_BLADE_R    # 1.113: the rim just reaches the sled top
SAW_FLANGE_R = 0.032


def saw_station(rng):
    return saw_station_sized(rng, SAW_BLADE_R, SAW_FLANGE_R, 1.1, 0.64)


def saw_station_large(rng):
    """24-inch slab saw for the Stage-3 heavy bay: the same design scaled up (blade 0.3 m, a 4.5 cm flange, a longer
    cabinet with a wider pan), so the arbor passes a rock up to arbor - flange = 25 cm high."""
    return saw_station_sized(rng, SLAB_BLADE_R, SLAB_FLANGE_R, 1.1, 0.7)


SLAB_BLADE_R = 0.3
SLAB_FLANGE_R = 0.045


def saw_station_sized(rng, blade_r, flange_r, cab_len, cab_depth):
    """14-inch trim saw on a steel cabinet. Slot 0 painted steel, slot 1 coolant surface, slot 2 clear guard,
    slot 3 rubber, slot 4 red control, slot 5 bare steel, slot 6 meter dial (white), slot 7 cast iron (motor, bearing
    housings, pedestal, pulleys), slot 8 aluminium (blade guard, hood bracket), slot 9 dark plastic (switch box, junction
    box, cable, terminal cover), slot 10 nameplate. Cabinet 1.1 x 0.64, origin at base centre; rails top z=0.915 at
    y -0.136 and -0.276 (operator side of the blade, under the vise's shoes); blade plane y=0.05.
    V6: every part reads as its own material (a cast motor, an aluminium guard, a painted cabinet, plastic controls),
    with the manufactured detail a used shop saw carries: cooling fins, a fan cowl and terminal box on the motor, a cable
    run, a folded sheet guard with a viewing window, a nameplate, panel trim lines and a drain."""
    bm = bmesh.new()
    arbor_z = SAW_RAIL_TOP + SAW_SLED_TOP + blade_r
    hx, hy = cab_len / 2, cab_depth / 2
    box(bm, (cab_len, cab_depth, 0.8), (0, 0, 0.42), bevel=0.016, segments=3)
    # panel trim: a raised seam line round each side panel (sheet-metal panels overlap the frame)
    for sx in (-1, 1):
        add(bm, hq.rbox((0.004, cab_depth - 0.1, 0.004), (sx * (hx + 0.001), 0, 0.72), bevel=0.001, segments=1), mat=0)
        add(bm, hq.rbox((0.004, cab_depth - 0.1, 0.004), (sx * (hx + 0.001), 0, 0.12), bevel=0.001, segments=1), mat=0)
    # nameplate on the front, above the door: a small stamped plate on two rivets
    add(bm, hq.rbox((0.16, 0.003, 0.05), (0.05, -hy - 0.0015, 0.66), bevel=0.001, segments=1), mat=10)
    for sx in (-0.07, 0.07):
        add(bm, hq.lathe([(0, 0), (0.003, 0), (0.003, 0.002), (0, 0.002)], segments=12), T(0.05 + sx, -hy - 0.003, 0.66) @ R(-90, "X"), mat=5)
    for sx in (-1, 1):
        for sy in (-1, 1):
            add(bm, hq.lathe([(0, 0), (0.03, 0), (0.032, 0.01), (0.028, 0.02), (0, 0.02)], segments=24), T(sx * (hx - 0.07), sy * (hy - 0.07), 0.0), mat=3)
    # front door with a recessed panel line, pull and hinges
    add(bm, hq.rbox((0.5, 0.01, 0.5), (0, -0.318, 0.32), bevel=0.004, segments=2), mat=0)
    add(bm, hq.tube([(0.2, -0.324, 0.33), (0.2, -0.345, 0.33), (0.2, -0.348, 0.33), (0.2, -0.348, 0.42), (0.2, -0.345, 0.42), (0.2, -0.324, 0.42)], 0.007, segments=12), mat=5)
    for z in (0.15, 0.5):
        add(bm, hq.cyl(0.008, 0.05, segments=12, center=(0, 0, -0.025)), T(-0.26, -0.323, z), mat=5)
    # coolant pan with a rolled lip, the coolant surface, drain spigot with a valve
    L, D = cab_len, cab_depth
    pan = hq.loft([hq.ring_rrect(L + 0.02, D + 0.02, 0.05, 0.82, 32), hq.ring_rrect(L + 0.02, D + 0.02, 0.05, 0.875, 32), hq.ring_rrect(L + 0.04, D + 0.04, 0.055, 0.885, 32),
                   hq.ring_rrect(L, D, 0.045, 0.885, 32), hq.ring_rrect(L - 0.04, D - 0.04, 0.04, 0.83, 32)])
    add(bm, pan, mat=0, sharp=45)
    add(bm, hq.loft([hq.ring_rrect(L - 0.04, D - 0.04, 0.04, 0.86, 32), hq.ring_rrect(L - 0.04, D - 0.04, 0.04, 0.868, 32)]), mat=1, sharp=45)
    add(bm, hq.lathe([(0, 0), (0.012, 0), (0.012, 0.05), (0, 0.05)], segments=16), T(hx - 0.05, hy + 0.01, 0.83) @ R(-90, "X"), mat=5)
    add(bm, hq.knob(0.012, 0.012, segments=16), T(hx - 0.05, hy + 0.07, 0.83) @ R(-90, "X"), mat=3)
    # rails on stanchions, operator side of the blade, under the vise's shoes (the vise centre sits at blade - 0.106)
    for y in (SAW_BLADE_Y - 0.106 - 0.08, SAW_BLADE_Y - 0.106 - 0.22):
        box(bm, (cab_len, 0.04, 0.03), (0.0, y, 0.9), bevel=0.005, segments=3, mat=5)
        for sx in (-(hx - 0.1), 0.0, hx - 0.1):
            box(bm, (0.04, 0.05, 0.03), (sx, y, 0.87), bevel=0.003)
    # arbor: shaft from the blade back to a pillow block bearing, belt pulley behind it
    add(bm, hq.cyl(0.014, 0.23, segments=32, center=(0, 0, 0)), T(0, SAW_BLADE_Y + 0.22, arbor_z) @ R(90, "X"), mat=5)   # shaft: from just before the blade back to the pulley
    add(bm, hq.lathe([(0, 0), (flange_r - 0.002, 0), (flange_r, 0.003), (flange_r, 0.012), (flange_r - 0.004, 0.015), (0, 0.015)], segments=48), T(0, SAW_BLADE_Y + 0.018, arbor_z) @ R(90, "X"), mat=5)
    # pillow block (cast iron): housing no deeper below the arbor than the flange radius, so a rock the blade can pass clears it
    pb = hq.rbox((0.09, 0.08, 2 * flange_r), (0, 0, 0), bevel=0.006, segments=3)
    add(bm, pb, T(0, SAW_BLADE_Y + 0.15, arbor_z), mat=7)
    add(bm, hq.lathe([(0, 0), (0.03, 0), (0.03, 0.01), (0, 0.01)], segments=32), T(0, SAW_BLADE_Y + 0.105, arbor_z) @ R(90, "X"), mat=7)   # bearing cap
    add(bm, hq.lathe([(0, 0), (0.008, 0), (0.008, 0.012), (0.004, 0.016), (0, 0.016)], segments=12), T(0, SAW_BLADE_Y + 0.15, arbor_z + flange_r + 0.005) , mat=5)   # grease nipple
    for sx in (-1, 1):
        add(bm, hq.hex_bolt(0.006, 0.005), T(sx * 0.03, SAW_BLADE_Y + 0.15, arbor_z + flange_r), mat=5)
    # bearing pedestal rising from the cabinet behind the pan, its foot outside the rock path (cast, with a web)
    box(bm, (0.09, 0.08, 0.2), (0, SAW_BLADE_Y + 0.15, arbor_z + 0.1 + flange_r), bevel=0.006, segments=2, mat=7)
    add(bm, hq.rbox((0.03, 0.06, 0.16), (0, SAW_BLADE_Y + 0.2, arbor_z + 0.1 + flange_r), bevel=0.003, segments=1), mat=7)
    box(bm, (0.5, 0.06, 0.05), (-0.12, SAW_BLADE_Y + 0.15, arbor_z + 0.2 + flange_r + 0.025), bevel=0.006, segments=2, mat=7)   # top beam to the motor bracket
    add(bm, hq.lathe([(0, 0), (0.05, 0), (0.052, 0.004), (0.052, 0.028), (0.05, 0.032), (0, 0.032)], segments=48), T(0, SAW_BLADE_Y + 0.235, arbor_z) @ R(90, "X"), mat=7)   # arbor pulley
    # pulley groove and the belt itself (rubber) between the arbor and motor pulleys
    add(bm, hq.torus(0.052, 0.004, seg_major=48, seg_minor=8), T(0, SAW_BLADE_Y + 0.251, arbor_z) @ R(90, "X"), mat=3)
    # blade guard (aluminium sheet): a folded channel over the top half of the blade, 3 mm walls with a rolled lip,
    # a viewing window on the operator side, hanging from the top beam on a bracket
    hood = hq.lathe([(blade_r + 0.012, 0.0), (blade_r + 0.03, 0.0), (blade_r + 0.033, 0.003), (blade_r + 0.033, 0.05), (blade_r + 0.03, 0.053), (blade_r + 0.012, 0.053), (blade_r + 0.012, 0.05), (blade_r + 0.03, 0.05), (blade_r + 0.03, 0.003), (blade_r + 0.012, 0.003)], segments=96, loop=True)
    lower = [v for v in hood.verts if math.degrees(math.atan2(v.co.y, v.co.x)) % 360 > 172 or math.degrees(math.atan2(v.co.y, v.co.x)) % 360 < 8]
    bmesh.ops.delete(hood, geom=lower, context="VERTS")
    add(bm, hood, T(0, SAW_BLADE_Y + 0.027, arbor_z) @ R(90, "X"), mat=8, sharp=45)
    # side cheeks of the guard (operator side and back), thin plates with a window on the operator side
    for sgn, y in ((-1, SAW_BLADE_Y + 0.027 - 0.0015), (1, SAW_BLADE_Y + 0.027 + 0.053 + 0.0015)):
        cheek = hq.lathe([(blade_r - 0.03, 0.0), (blade_r + 0.03, 0.0), (blade_r + 0.03, 0.003), (blade_r - 0.03, 0.003)], segments=96, loop=True)
        low = [v for v in cheek.verts if math.degrees(math.atan2(v.co.y, v.co.x)) % 360 > 175 or math.degrees(math.atan2(v.co.y, v.co.x)) % 360 < 5]
        bmesh.ops.delete(cheek, geom=low, context="VERTS")
        add(bm, cheek, T(0, y, arbor_z) @ R(90, "X"), mat=8, sharp=45)
    add(bm, hq.rbox((0.09, 0.002, 0.05), (0.03, SAW_BLADE_Y + 0.027 - 0.004, arbor_z + blade_r * 0.55), bevel=0.001, segments=1), mat=2)   # window
    box(bm, (0.04, 0.054, 0.06), (0, SAW_BLADE_Y, arbor_z + blade_r + 0.06), bevel=0.004, segments=2, mat=8)                                    # guard hanger
    box(bm, (0.04, 0.16, 0.03), (0, SAW_BLADE_Y + 0.08, arbor_z + blade_r + 0.075), bevel=0.004, segments=2, mat=8)
    for y in (SAW_BLADE_Y + 0.03, SAW_BLADE_Y + 0.13):
        add(bm, hq.hex_bolt(0.005, 0.004), T(0, y, arbor_z + blade_r + 0.09), mat=5)
    # motor (cast frame) high and back on a bracket: a finned body, end bells, a fan cowl with slots at the back, a
    # terminal box with a cable gland, feet on a cradle; belt cover from the motor pulley down to the arbor pulley
    mx, my, mz = -0.62, SAW_BLADE_Y + 0.235, arbor_z + 0.16
    motor = hq.lathe([(0, 0), (0.09, 0), (0.1, 0.012), (0.1, 0.05), (0.1, 0.22), (0.1, 0.258), (0.088, 0.27), (0.05, 0.275), (0, 0.275)], segments=56)
    add(bm, motor, T(mx, my, mz) @ R(90, "Y"), mat=7, sharp=50)
    for k in range(9):
        fin = hq.lathe([(0.1, 0), (0.111, 0), (0.111, 0.003), (0.1, 0.003)], segments=56, loop=True)
        add(bm, fin, T(mx, my, mz) @ R(90, "Y") @ T(0, 0, 0.06 + k * 0.018), mat=7, sharp=60)
    for z in (0.02, 0.25):   # end bell bolts
        for a in range(4):
            ang = math.radians(45 + 90 * a)
            add(bm, hq.hex_bolt(0.004, 0.004), T(mx, my, mz) @ R(90, "Y") @ T(0.085 * math.cos(ang), 0.085 * math.sin(ang), z) , mat=5)
    cowl = hq.lathe([(0, 0), (0.07, 0), (0.095, 0.012), (0.098, 0.03), (0.09, 0.034), (0, 0.034)], segments=48)
    add(bm, cowl, T(mx, my, mz) @ R(90, "Y") @ T(0, 0, -0.03), mat=9, sharp=50)          # fan cowl (pressed steel, painted black)
    for a in range(12):   # cowl slots
        ang = math.radians(30 * a)
        add(bm, hq.rbox((0.014, 0.004, 0.003), (0, 0, 0), bevel=0.0005, segments=1), T(mx, my, mz) @ R(90, "Y") @ T(0.06 * math.cos(ang), 0.06 * math.sin(ang), -0.032) @ R(math.degrees(ang), "Z"), mat=9)
    box(bm, (0.1, 0.09, 0.055), (mx + 0.14, my, mz + 0.115), bevel=0.005, segments=2, mat=9)                            # terminal box on top
    add(bm, hq.rbox((0.098, 0.088, 0.004), (mx + 0.14, my, mz + 0.145), bevel=0.001, segments=1), mat=9)                 # its lid seam
    add(bm, hq.rbox((0.06, 0.003, 0.03), (mx + 0.02, my - 0.101, mz + 0.02), bevel=0.001, segments=1), mat=10)          # motor nameplate
    add(bm, hq.cyl(0.009, 0.02, segments=16, center=(0, 0, 0)), T(mx + 0.19, my, mz + 0.115) @ R(90, "Y"), mat=9)        # cable gland
    cable = hq.bezier((mx + 0.2, my, mz + 0.115), (mx + 0.3, my, mz + 0.1), (mx + 0.22, my - 0.2, arbor_z - 0.3), (-0.5, -0.33, 0.78), 26)
    add(bm, hq.tube(cable, 0.005, segments=10), mat=9)                                                                      # cable down to the switch box
    box(bm, (0.3, 0.14, 0.04), (-0.5, my, arbor_z + 0.03), bevel=0.006, segments=2, mat=7)                                # motor cradle
    for sx in (-0.6, -0.4):
        for sy in (-0.05, 0.05):
            add(bm, hq.hex_bolt(0.005, 0.004), T(sx, my + sy, arbor_z + 0.05), mat=5)
    box(bm, (0.04, 0.06, 0.3), (-0.5, SAW_BLADE_Y + 0.15, arbor_z - 0.12), bevel=0.005, segments=2, mat=7)                # bracket post to the pan
    add(bm, hq.rbox((0.42, 0.05, 0.22), (0, 0, 0), bevel=0.012, segments=3), T(-0.22, my, arbor_z + 0.08) @ R(-20, "Y"), mat=0)   # belt cover
    add(bm, hq.lathe([(0, 0), (0.04, 0), (0.04, 0.03), (0, 0.03)], segments=32), T(-0.44, my, mz) @ R(90, "X"), mat=7)  # motor pulley
    # switch box on the cabinet face (plastic housing): rocker, red stop, the load meter dial (the needle is prop_saw_needle)
    box(bm, (0.18, 0.06, 0.16), (-0.42, -0.35, 0.7), bevel=0.006, segments=3, mat=9)
    add(bm, hq.rbox((0.03, 0.01, 0.04), (0, 0, 0), bevel=0.003), T(-0.47, -0.385, 0.73), mat=3)
    add(bm, hq.lathe([(0, 0), (0.016, 0), (0.018, 0.006), (0.016, 0.012), (0, 0.012)], segments=24), T(-0.47, -0.382, 0.68) @ R(-90, "X"), mat=4)
    add(bm, hq.lathe([(0, 0), (0.03, 0), (0.032, 0.003), (0.032, 0.012), (0.029, 0.014), (0, 0.014)], segments=48), T(-0.385, -0.394, 0.71) @ R(-90, "X"), mat=5)   # meter bezel
    add(bm, hq.lathe([(0, 0), (0.026, 0), (0.026, 0.002), (0, 0.002)], segments=48), T(-0.385, -0.394, 0.71) @ R(-90, "X"), mat=6)                            # dial face
    # coolant feed: a riser from the pump in the cabinet, a valve body on it (lever is prop_saw_valve), hose to the nozzle at the top of the blade
    add(bm, hq.cyl(0.007, 0.5, segments=14, center=(0, 0, 0)), T(0.42, 0.24, 0.885), mat=5)
    add(bm, hq.lathe([(0, 0), (0.014, 0), (0.014, 0.03), (0, 0.03)], segments=20), T(0.42, 0.24, 1.15), mat=5)         # valve body
    hose = hq.bezier((0.42, 0.24, 1.385), (0.42, 0.24, 1.5), (0.12, SAW_BLADE_Y + 0.02, 1.48), (0.0, SAW_BLADE_Y, arbor_z + blade_r + 0.105), 28)
    add(bm, hq.tube(hose, 0.006, segments=12), mat=3)
    add(bm, hq.lathe([(0, 0), (0.008, 0), (0.008, 0.03), (0.004, 0.035), (0, 0.035)], segments=16), T(0, SAW_BLADE_Y, arbor_z + blade_r + 0.105) @ R(180, "X"), mat=5)  # nozzle, pointing down at the rim
    # side table on the -X side: a steel shelf on two brackets, where the cut pieces are laid out (top z=0.85)
    tx = -(hx + 0.25)
    box(bm, (0.55, 0.75, 0.02), (tx, 0.0, 0.84), bevel=0.004, segments=2)
    box(bm, (0.55, 0.03, 0.03), (tx, -0.36, 0.815), bevel=0.003)
    box(bm, (0.55, 0.03, 0.03), (tx, 0.36, 0.815), bevel=0.003)
    for y in (-0.25, 0.25):
        brace = hq.rbox((0.03, 0.03, 0.46), (0, 0, 0), bevel=0.003)
        add(bm, brace, T(tx + 0.06, y, 0.65) @ R(-38, "Y"), mat=0)
        box(bm, (0.04, 0.05, 0.1), (-hx, y, 0.45), bevel=0.004)
    for x in (tx - 0.2, tx + 0.18):
        for y in (-0.25, 0.25):
            add(bm, hq.hex_bolt(0.006, 0.004), T(x, y, 0.85), mat=5)
    # splash guard behind the blade, in a frame
    add(bm, hq.rbox((0.6, 0.006, 0.42), (0, 0.3, 1.1), bevel=0.002), mat=2)
    for sx in (-1, 1):
        box(bm, (0.02, 0.02, 0.44), (sx * 0.3, 0.3, 1.1), bevel=0.003)
    box(bm, (0.62, 0.02, 0.02), (0, 0.3, 1.32), bevel=0.003)
    cols = [ColBox((cab_len + 0.04, cab_depth + 0.04, 0.885), (0, 0, 0.4425)), ColBox((0.55, 0.77, 0.05), (tx, 0.0, 0.83)),
            ColBox((0.09, 0.08, 0.26), (0, SAW_BLADE_Y + 0.15, arbor_z + 0.1)), ColBox((0.5, 0.06, 0.05), (-0.12, SAW_BLADE_Y + 0.15, arbor_z + 0.2 + flange_r + 0.025)),
            ColBox((0.28, 0.28, 0.26), (-0.62, SAW_BLADE_Y + 0.235, arbor_z + 0.16)), ColBox((0.42, 0.05, 0.22), (-0.22, SAW_BLADE_Y + 0.235, arbor_z + 0.08)),
            ColBox((0.62, 0.02, 0.44), (0, 0.3, 1.1)), ColBox((0.18, 0.06, 0.16), (-0.42, -0.35, 0.7)), ColBox((0.04, 0.16, 0.06), (0, SAW_BLADE_Y + 0.08, arbor_z + blade_r + 0.06))]
    return bm, cols


def saw_blade(rng):
    return saw_blade_sized(rng, SAW_BLADE_R, SAW_FLANGE_R)


def saw_blade_large(rng):
    """24-inch diamond blade for the slab saw."""
    return saw_blade_sized(rng, SLAB_BLADE_R, SLAB_FLANGE_R)


def saw_blade_sized(rng, blade_r, flange_r):
    """Diamond blade, 356 mm (14 in): thin plate with a raised diamond rim (slot 2 dark segment), flanges and an
    arbor nut (slot 1). Origin at the axle; spins about its own Y (Unity Z)."""
    bm = bmesh.new()
    disc = hq.lathe([(0, 0), (blade_r - 0.008, 0), (blade_r - 0.008, 0.0016), (0, 0.0016)], segments=128, center=(0, 0, -0.0008))
    add(bm, disc, R(90, "X"), mat=0)
    # segmented diamond rim: 24 segments with gullets between them, slightly wider than the plate (the kerf)
    seg = 24
    for k in range(seg):
        a0 = 360.0 / seg * k
        piece = hq.lathe([(blade_r - 0.01, -0.0013), (blade_r, -0.0013), (blade_r, 0.0013), (blade_r - 0.01, 0.0013)], segments=144, loop=True)
        # the lathe makes a full ring; keep only this segment's arc by deleting the rest (a 2.2 degree gullet between segments)
        keep0, keep1 = a0, a0 + 360.0 / seg - 1.1
        drop = [v for v in piece.verts if not (keep0 <= (math.degrees(math.atan2(v.co.y, v.co.x)) % 360) <= keep1)]
        bmesh.ops.delete(piece, geom=drop, context="VERTS")
        if len(piece.verts) > 0:
            add(bm, piece, R(90, "X"), mat=2, sharp=60)
    # printed label on the operator-facing side: one thin ring (a brand band; no text at this size)
    add(bm, hq.lathe([(blade_r * 0.5, 0.0009), (blade_r * 0.56, 0.0009), (blade_r * 0.56, 0.0012), (blade_r * 0.5, 0.0012)], segments=96, loop=True), R(90, "X"), mat=3)
    for s in (-1, 1):
        add(bm, hq.lathe([(0, 0), (flange_r - 0.003, 0), (flange_r, 0.003), (flange_r, 0.008), (flange_r - 0.003, 0.01), (0, 0.01)], segments=48), R(90, "X") @ T(0, 0, s * 0.001 - (0.01 if s < 0 else 0)), mat=1)
    add(bm, hq.hex_bolt(0.012, 0.009), R(90, "X") @ T(0, 0, 0.011), mat=1)
    return bm, None


SAW_VISE_LEN = 0.42       # sled length along X
SAW_VISE_DEPTH = 0.2      # jaw depth along Y (all of it on the operator side of the blade)
SAW_SLED_NEAR = -0.25     # the sled plate runs from here (operator side) to +0.1: its shoes ride rails at y -0.08 and -0.22
SAW_FIXED_JAW_X = -0.16   # fixed jaw pad face, vise-local X (leading side)


def saw_vise(rng):
    """Carriage vise: sled plate with rail shoes, a fixed jaw at -X with a rubber pad (slot 1), bare steel screw and bosses (slot 2), a lead screw along X
    (jaws 9 cm tall: they grip a rock's lower half and leave it in view from the operator's side)
    to a bearing block at +X (the wheel is prop_saw_wheel, the moving jaw prop_saw_jaw). The jaws squeeze along the
    feed axis and hold the rock's near half; the whole vise sits on the operator side of the blade.
    0.42 (X) x 0.2 (Y), origin at base centre."""
    bm = bmesh.new()
    sled_depth = 0.1 - SAW_SLED_NEAR
    box(bm, (SAW_VISE_LEN, sled_depth, 0.02), (0, (0.1 + SAW_SLED_NEAR) * 0.5, 0.01), bevel=0.004, segments=3, mat=3)   # aluminium sled plate
    for y in (-0.08, -0.22):
        box(bm, (SAW_VISE_LEN, 0.05, 0.03), (0, y, 0.035), bevel=0.004, segments=2)          # rail shoes, on the operator side of the jaws
    # fixed jaw: a stout plate, its pad facing +X
    box(bm, (0.026, SAW_VISE_DEPTH - 0.02, 0.09), (SAW_FIXED_JAW_X - 0.013, 0, 0.065), bevel=0.004, segments=3)
    add(bm, hq.rbox((0.008, SAW_VISE_DEPTH - 0.04, 0.07), (SAW_FIXED_JAW_X + 0.004, 0, 0.065), bevel=0.002), mat=1)
    for sy in (-1, 1):
        box(bm, (0.05, 0.02, 0.06), (SAW_FIXED_JAW_X - 0.04, sy * 0.06, 0.05), bevel=0.003)   # gussets
    # lead screw along X from the fixed jaw side to the bearing block at +X, wheel boss outside it
    box(bm, (0.03, 0.08, 0.09), (0.195, 0, 0.065), bevel=0.004, segments=2)
    add(bm, hq.cyl(0.008, 0.36, segments=20, center=(0, 0, -0.18)), T(0.02, 0, 0.06) @ R(90, "Y"), mat=2)
    for k in range(14):
        add(bm, hq.torus(0.0082, 0.0012, seg_major=20, seg_minor=6), T(-0.1 + k * 0.02, 0, 0.06) @ R(90, "Y"), mat=2, sharp=70)
    add(bm, hq.cyl(0.012, 0.03, segments=24, center=(0, 0, -0.015)), T(0.225, 0, 0.06) @ R(90, "Y"), mat=2)   # wheel boss
    for sx in (-0.15, 0.15):
        for sy in (-0.06, 0.06):
            add(bm, hq.hex_bolt(0.006, 0.004), T(sx, sy, 0.02), mat=2)
    return bm, [ColBox((SAW_VISE_LEN, 0.1 - SAW_SLED_NEAR, 0.02), (0, (0.1 + SAW_SLED_NEAR) * 0.5, 0.01)), ColBox((SAW_VISE_LEN, 0.05, 0.03), (0, -0.08, 0.035)), ColBox((SAW_VISE_LEN, 0.05, 0.03), (0, -0.22, 0.035)),
                ColBox((0.034, SAW_VISE_DEPTH - 0.02, 0.09), (SAW_FIXED_JAW_X - 0.009, 0, 0.065)), ColBox((0.03, 0.08, 0.09), (0.195, 0, 0.065))]


def saw_jaw(rng):
    """Moving vise jaw with a rubber pad (slot 1), bare steel screw and bosses (slot 2) on its -X face and a bronze nut riding the lead screw. Origin at
    base centre; slides along X."""
    bm = bmesh.new()
    box(bm, (0.026, SAW_VISE_DEPTH - 0.02, 0.09), (0.013, 0, 0.065), bevel=0.004, segments=3)
    add(bm, hq.rbox((0.008, SAW_VISE_DEPTH - 0.04, 0.07), (-0.004, 0, 0.065), bevel=0.002), mat=1)
    box(bm, (0.04, 0.09, 0.02), (0.02, 0, 0.01), bevel=0.003)
    add(bm, hq.cyl(0.011, 0.03, segments=24, center=(0, 0, -0.015)), T(0.028, 0, 0.06) @ R(90, "Y"), mat=2)
    return bm, None


def saw_wheel(rng):
    """Vise handwheel: three-spoke wheel with a turned rim and a revolving handle; axis along X (it turns about X).
    Origin at the hub centre."""
    bm = bmesh.new()
    add(bm, hq.handwheel(0.045, 0.006, spokes=3, hub_r=0.014, seg_major=64), R(90, "Y"), mat=0)
    add(bm, hq.knob(0.006, 0.024, segments=16), T(0.006, 0.034, 0) @ R(90, "Y"), mat=1)
    return bm, None


def saw_needle(rng):
    """Ammeter needle: a tapered pointer on a hub, lying in the dial plane (XZ, facing -Y). Rotates about Y.
    Origin at the pivot; the needle points +Z at rest."""
    bm = bmesh.new()
    add(bm, hq.cyl(0.004, 0.003, segments=20, center=(0, 0, 0)), R(90, "X"), mat=0)
    add(bm, hq.rbox((0.0022, 0.0012, 0.024), (0, -0.0015, 0.012), bevel=0.0004, segments=1), mat=0)
    add(bm, hq.rbox((0.0015, 0.0012, 0.006), (0, -0.0015, 0.027), bevel=0.0003, segments=1), mat=0)
    return bm, None


def saw_valve(rng):
    """Coolant valve lever: a quarter-turn handle on the riser; turns about Z. Origin at the stem; closed lies along +X."""
    bm = bmesh.new()
    add(bm, hq.cyl(0.006, 0.014, segments=16, center=(0, 0, 0.007)), mat=0)
    add(bm, hq.rbox((0.05, 0.01, 0.006), (0.025, 0, 0.012), bevel=0.002, segments=2), mat=1)
    return bm, None


# ---- geode cracker (chain splitter) --------------------------------------------------------------------------
def cracker(rng):
    """Geode cracker on a stand: a soil-pipe-cutter-style chain splitter. Slot 0 painted steel, slot 1 bare steel,
    slot 2 rubber, slot 3 dial face. A 0.5 x 0.42 base plate on a welded stand (top of the plate z=0.9), two rubber
    V-rails the rock rests on (crest z=0.94, 0.16 apart along X), a rear post carrying the chain anchor and the
    ratchet head with a pressure gauge; the lever is prop_cracker_lever (pivot on the head), the chain is built at
    runtime round the rock. Origin at base centre; operator at -Y."""
    bm = bmesh.new()
    # stand: four legs, a shelf, the top plate
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (0.04, 0.04, 0.87), (sx * 0.22, sy * 0.18, 0.435), bevel=0.004, segments=2)
    box(bm, (0.5, 0.42, 0.03), (0, 0, 0.885), bevel=0.005, segments=3)
    box(bm, (0.46, 0.38, 0.02), (0, 0, 0.3), bevel=0.004, segments=2)
    for sy in (-1, 1):
        box(bm, (0.5, 0.03, 0.04), (0, sy * 0.185, 0.85), bevel=0.003)
    # rubber V-rails: the rock sits across them, the chain plane between
    for sx in (-1, 1):
        add(bm, hq.rbox((0.03, 0.34, 0.04), (0, 0, 0), bevel=0.006, segments=2), T(sx * 0.08, 0, 0.92) @ R(sx * 18, "Y"), mat=2)
    # rear post and the ratchet head: a boxy housing with the chain anchor drum and a gauge on its face
    box(bm, (0.08, 0.08, 0.46), (0, 0.17, 1.13), bevel=0.006, segments=3)
    box(bm, (0.16, 0.12, 0.12), (0, 0.12, 1.38), bevel=0.008, segments=3)
    add(bm, hq.lathe([(0, 0), (0.028, 0), (0.03, 0.004), (0.03, 0.03), (0.028, 0.034), (0, 0.034)], segments=40), T(0, 0.055, 1.36) @ R(90, "X"), mat=1)   # anchor drum
    add(bm, hq.cyl(0.006, 0.05, segments=12, center=(0, 0, 0)), T(0.055, 0.06, 1.36) @ R(90, "X"), mat=1)                                               # ratchet pawl pin
    add(bm, hq.lathe([(0, 0), (0.026, 0), (0.028, 0.003), (0.028, 0.01), (0.025, 0.012), (0, 0.012)], segments=40), T(-0.045, 0.058, 1.4) @ R(-90, "X"), mat=1)   # gauge bezel
    add(bm, hq.lathe([(0, 0), (0.022, 0), (0.022, 0.002), (0, 0.002)], segments=40), T(-0.045, 0.057, 1.4) @ R(-90, "X"), mat=3)                             # gauge face
    box(bm, (0.03, 0.03, 0.05), (0.065, 0.12, 1.33), bevel=0.003)                                                                                     # lever pivot boss
    add(bm, hq.cyl(0.012, 0.06, segments=20, center=(0, 0, -0.03)), T(0.08, 0.12, 1.33) @ R(90, "Y"), mat=1)                                           # lever pivot pin
    for sx in (-1, 1):
        add(bm, hq.hex_bolt(0.006, 0.005), T(sx * 0.06, 0.12, 1.445), mat=1)
    for sx in (-1, 1):
        for sy in (-1, 1):
            add(bm, hq.hex_bolt(0.007, 0.005), T(sx * 0.21, sy * 0.17, 0.9), mat=1)
    cols = [ColBox((0.5, 0.42, 0.03), (0, 0, 0.885)), ColBox((0.46, 0.38, 0.02), (0, 0, 0.3)), ColBox((0.08, 0.08, 0.46), (0, 0.17, 1.13)), ColBox((0.16, 0.12, 0.12), (0, 0.12, 1.38)),
            ColBox((0.03, 0.34, 0.05), (-0.08, 0, 0.92)), ColBox((0.03, 0.34, 0.05), (0.08, 0, 0.92))]
    for sx in (-1, 1):
        for sy in (-1, 1):
            cols.append(ColBox((0.04, 0.04, 0.87), (sx * 0.22, sy * 0.18, 0.435)))
    return bm, cols


def cracker_lever(rng):
    """Ratchet lever for the geode cracker: a steel bar with a rubber grip, pivot at the origin, lying along +X and
    rising toward +Z at rest; pumps about Y."""
    bm = bmesh.new()
    add(bm, hq.cyl(0.011, 0.02, segments=20, center=(0, 0, -0.01)), R(90, "X"), mat=1)                        # hub
    add(bm, hq.rbox((0.34, 0.02, 0.028), (0.17, 0, 0), bevel=0.004, segments=2), R(-35, "Y"), mat=0)          # bar
    # grip on the end of the bar (rotated about the pivot with the bar, so it never floats beside the head)
    add(bm, hq.lathe([(0, 0), (0.013, 0), (0.015, 0.004), (0.015, 0.1), (0.013, 0.104), (0, 0.104)], segments=24), R(-35, "Y") @ T(0.24, 0, 0) @ R(90, "Y"), mat=2)   # grip
    return bm, None


def plinth(rng):
    """Gallery plinth 0.42 x 0.42 x 1.0 for the Stage-3 curated display: a lacquered column on a steel foot with a
    felt top (slot 1) and a brass plaque rail (slot 2). Origin at base; a specimen stands on the top (z=1.0)."""
    bm = bmesh.new()
    box(bm, (0.46, 0.46, 0.02), (0, 0, 0.01), bevel=0.004, segments=2, mat=2)
    col = hq.rbox((0.4, 0.4, 0.94), (0, 0, 0.49), bevel=0.012, segments=3)
    add(bm, col, mat=0)
    box(bm, (0.42, 0.42, 0.03), (0, 0, 0.985), bevel=0.005, segments=3, mat=0)
    add(bm, hq.rbox((0.38, 0.38, 0.006), (0, 0, 1.0), bevel=0.001), mat=1)
    add(bm, hq.rbox((0.16, 0.012, 0.05), (0, -0.216, 0.9), bevel=0.002), mat=2)   # plaque
    return bm, [ColBox((0.46, 0.46, 1.0), (0, 0, 0.5))]


def uv_lamp(rng):
    """Longwave UV inspection lamp on a bench stand: a black tube housing with a purple lens (slot 1) on a hinged arm.
    Origin at the base; the lamp head points down at -Y and z 0.35."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0, 0), (0.07, 0), (0.072, 0.008), (0.06, 0.018), (0, 0.018)], segments=40), mat=0)
    add(bm, hq.cyl(0.012, 0.3, segments=20, center=(0, 0, 0.018)), mat=0)
    add(bm, hq.rbox((0.04, 0.04, 0.05), (0, 0, 0.32), bevel=0.006, segments=2), mat=0)
    head = hq.rbox((0.22, 0.07, 0.05), (0, -0.09, 0.33), bevel=0.008, segments=3)
    add(bm, head, mat=0)
    add(bm, hq.rbox((0.19, 0.055, 0.004), (0, -0.09, 0.305), bevel=0.001), mat=1)
    add(bm, hq.knob(0.01, 0.014, segments=16, ridges=8), T(0.08, -0.05, 0.36), mat=0)
    return bm, [ColBox((0.14, 0.14, 0.02), (0, 0, 0.01))]


def polish_lap(rng):
    """Flat lap machine: steel cabinet on rubber feet, round splash pan with a rolled lip and a drain spout, drip
    tank (slot 1 plastic) with a valve and a feed tube, a rocker switch, a finned motor housing out the right side
    and a splash guard behind the pan, so the machine reads as driven rather than as a box with a disc on it.
    Platen (prop_polish_disc) centre at (0, 0, 0.76). Origin at base centre; operator at -Y."""
    bm = bmesh.new()
    box(bm, (0.52, 0.46, 0.7), (0, 0, 0.37), bevel=0.012, segments=3)
    for sx in (-1, 1):
        for sy in (-1, 1):
            add(bm, hq.lathe([(0, 0), (0.024, 0), (0.026, 0.01), (0.022, 0.02), (0, 0.02)], segments=24), T(sx * 0.22, sy * 0.19, 0.0), mat=2)
    box(bm, (0.54, 0.48, 0.05), (0, 0, 0.745), bevel=0.008, segments=3)
    # the pan floor sits 3 mm proud of the cabinet top (coplanar faces z-fought into a camouflage pattern)
    pan = hq.lathe([(0.16, 0.75), (0.215, 0.75), (0.225, 0.762), (0.225, 0.795), (0.215, 0.805), (0.205, 0.805), (0.2, 0.795), (0.2, 0.773), (0.16, 0.773)], segments=64, loop=True)
    add(bm, pan, mat=0)
    add(bm, hq.lathe([(0, 0), (0.165, 0), (0.165, 0.02), (0, 0.02)], segments=64, center=(0, 0, 0.75)), mat=0)
    add(bm, hq.lathe([(0, 0), (0.012, 0), (0.012, 0.06), (0, 0.06)], segments=16), T(0.0, -0.21, 0.76) @ R(90, "X"), mat=0)      # drain spout
    tank = hq.lathe([(0, 0), (0.032, 0), (0.036, 0.006), (0.036, 0.15), (0.03, 0.16), (0.014, 0.165), (0.014, 0.18), (0, 0.18)], segments=32, center=(0, 0, 0))
    add(bm, tank, T(-0.19, 0.16, 0.77), mat=1)
    box(bm, (0.05, 0.03, 0.006), (-0.19, 0.16, 0.77), bevel=0.002, mat=0)
    add(bm, hq.knob(0.009, 0.01, segments=16), T(-0.19, 0.196, 0.8) @ R(-90, "X"), mat=2)
    feed = hq.bezier((-0.19, 0.16, 0.775), (-0.16, 0.12, 0.775), (-0.05, 0.03, 0.83), (0.0, 0.0, 0.8), 16)
    add(bm, hq.tube(feed, 0.004, segments=10), mat=1)
    box(bm, (0.1, 0.05, 0.1), (0.19, -0.2, 0.66), bevel=0.006, segments=3)
    add(bm, hq.rbox((0.03, 0.01, 0.04), (0, 0, 0), bevel=0.003), T(0.19, -0.228, 0.66), mat=2)

    # motor: a finned drum out the right-hand side with a belt cover and a cable gland, so the lap looks driven
    add(bm, hq.lathe([(0, 0), (0.105, 0), (0.11, 0.01), (0.11, 0.26), (0.105, 0.27), (0, 0.27)], segments=32),
        T(0.24, 0.0, 0.42) @ R(90, "Y"), mat=0)
    for i in range(6):
        add(bm, hq.lathe([(0.11, 0), (0.122, 0), (0.122, 0.014), (0.11, 0.014)], segments=32, loop=True),
            T(0.285 + i * 0.036, 0.0, 0.42) @ R(90, "Y"), mat=0)
    add(bm, hq.lathe([(0, 0), (0.062, 0), (0.064, 0.008), (0.06, 0.016), (0, 0.016)], segments=24),
        T(0.51, 0.0, 0.42) @ R(90, "Y"), mat=0)                                         # end bell
    add(bm, hq.rbox((0.05, 0.05, 0.05), (0.30, 0.09, 0.31), bevel=0.006, segments=1), mat=0)   # terminal box
    add(bm, hq.tube(hq.bezier((0.30, 0.11, 0.30), (0.30, 0.16, 0.22), (0.16, 0.22, 0.12), (0.05, 0.23, 0.02)), 0.008, segments=10), mat=1)   # supply cable

    # a maker's plate on the front face
    add(bm, hq.rbox((0.13, 0.004, 0.045), (0.0, -0.232, 0.55), bevel=0.002, segments=1), mat=2)
    return bm, [ColBox((0.54, 0.48, 0.77), (0, 0, 0.385)), ColBox((0.45, 0.45, 0.035), (0, 0, 0.7675)),
                ColBox((0.08, 0.08, 0.18), (-0.19, 0.16, 0.86)), ColBox((0.30, 0.24, 0.26), (0.36, 0.0, 0.42))]


def polish_disc(rng):
    """Lap platen: aluminium master lap with a chamfer (slot 0) and a diamond pad (slot 1). Origin at base centre."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0, 0), (0.146, 0), (0.15, 0.004), (0.15, 0.014), (0.148, 0.016), (0, 0.016)], segments=72), mat=0)
    add(bm, hq.lathe([(0, 0), (0.148, 0), (0.148, 0.006), (0, 0.006)], segments=72, center=(0, 0, 0.019)), mat=1)
    add(bm, hq.hex_bolt(0.012, 0.006), T(0, 0, 0.025), mat=0)
    return bm, None


def wash_tub(rng):
    """Cleaning station: a deep stainless utility sink on a steel stand under a splashback, with a wall-mounted
    mixer, a coiled hose and a spray gun on its rim, and a drain hose below. Slot 0 stand (steel), slot 1 water
    surface, slot 2 sink and splashback (stainless), slot 3 tap and valves (brass), slot 4 the hose and the gun's
    grip (dark rubber/plastic). Tub rim at 0.83, water at 0.706 (a shallow rinse basin), splashback top at 1.42.
    0.64 x 0.5, origin at base centre; operator at -Y, wall at +Y."""
    bm = bmesh.new()
    for x in (-0.28, 0.28):
        for y in (-0.2, 0.2):
            box(bm, (0.035, 0.035, 0.6), (x, y, 0.3), bevel=0.004, segments=2)
    for sy in (-1, 1):
        box(bm, (0.6, 0.035, 0.04), (0, sy * 0.2, 0.58), bevel=0.003)
        box(bm, (0.6, 0.035, 0.03), (0, sy * 0.2, 0.15), bevel=0.003)
    for sx in (-1, 1):
        box(bm, (0.035, 0.4, 0.04), (sx * 0.28, 0, 0.58), bevel=0.003)
        box(bm, (0.035, 0.4, 0.03), (sx * 0.28, 0, 0.15), bevel=0.003)
    for i in range(4):
        box(bm, (0.5, 0.07, 0.015), (0, -0.15 + i * 0.1, 0.17), bevel=0.002)
    w, d = 0.6, 0.46
    sink = hq.loft([hq.ring_rrect(w - 0.06, d - 0.06, 0.05, 0.62, 32), hq.ring_rrect(w, d, 0.06, 0.66, 32), hq.ring_rrect(w, d, 0.06, 0.82, 32),
                    hq.ring_rrect(w + 0.02, d + 0.02, 0.07, 0.83, 32), hq.ring_rrect(w - 0.04, d - 0.04, 0.05, 0.83, 32),
                    hq.ring_rrect(w - 0.05, d - 0.05, 0.045, 0.81, 32), hq.ring_rrect(w - 0.1, d - 0.1, 0.04, 0.65, 32)])
    add(bm, sink, mat=2, sharp=45)
    # a few centimetres of rinse water in the bottom of the sink, so a rock stands proud of it and can be seen
    add(bm, hq.loft([hq.ring_rrect(w - 0.088, d - 0.088, 0.042, 0.7, 32), hq.ring_rrect(w - 0.088, d - 0.088, 0.042, 0.706, 32)]), mat=1, sharp=45)
    # splashback: a stainless panel up the wall behind the sink with a folded top lip, so the wall reads as a
    # working wet area rather than a tub parked against boards
    add(bm, hq.rbox((0.94, 0.014, 0.52), (0, 0.235, 1.08), bevel=0.004, segments=2), mat=2)
    add(bm, hq.rbox((0.94, 0.045, 0.022), (0, 0.22, 1.335), bevel=0.005, segments=2), mat=2)    # top lip, folded forward
    add(bm, hq.rbox((0.94, 0.06, 0.022), (0, 0.21, 0.83), bevel=0.005, segments=2), mat=2)      # bottom return onto the rim

    # wall mixer: a body across the splashback, two cross handles, a swan neck reaching over the basin
    add(bm, hq.cyl(0.016, 0.24, segments=20), T(-0.12, 0.207, 0.99) @ R(90, "Y"), mat=3)
    for sx in (-1, 1):
        add(bm, hq.lathe([(0, 0), (0.02, 0), (0.022, 0.005), (0.016, 0.022), (0.011, 0.03), (0, 0.03)], segments=20),
            T(sx * 0.12, 0.19, 0.99) @ R(-90, "X"), mat=3)
        for a in (0, 90):
            add(bm, hq.rbox((0.046, 0.009, 0.009), (0, 0, 0), bevel=0.002, segments=1),
                T(sx * 0.12, 0.155, 0.99) @ R(a, "Y"), mat=3)
    # a swan neck that clears the rim and reaches the middle of the basin, so the tap reads from across the room
    neck = hq.bezier((0.0, 0.205, 1.0), (0.0, 0.205, 1.3), (0.0, -0.06, 1.28), (0.0, -0.02, 1.03), 24)
    add(bm, hq.tube(neck, 0.011, segments=16), mat=3)
    add(bm, hq.lathe([(0, 0), (0.014, 0), (0.016, 0.011), (0.013, 0.022), (0, 0.022)], segments=20), T(0.0, -0.02, 1.01), mat=3)

    # spray gun parked on the right rim, on a coiled hose down to the wall union: the reference's hero tool
    add(bm, hq.lathe([(0, 0), (0.02, 0), (0.022, 0.006), (0.018, 0.05), (0.012, 0.055), (0, 0.055)], segments=20),
        T(0.34, 0.19, 1.06) @ R(-90, "X"), mat=3)                                              # hose union on the splashback
    coil = hq.bezier((0.34, 0.15, 1.06), (0.44, 0.02, 1.0), (0.2, -0.04, 0.96), (0.29, 0.03, 0.86), 26)
    add(bm, hq.tube(coil, 0.010, segments=12), mat=4)
    add(bm, hq.rbox((0.038, 0.032, 0.11), (0.29, 0.03, 0.9), bevel=0.009, segments=2), mat=4)   # grip
    add(bm, hq.cyl(0.016, 0.13, segments=20, center=(0, 0, 0)), T(0.29, -0.01, 0.955) @ R(70, "X"), mat=3)   # barrel
    add(bm, hq.lathe([(0, 0), (0.018, 0), (0.02, 0.008), (0.008, 0.016), (0, 0.016)], segments=20),
        T(0.29, -0.05, 1.0) @ R(70, "X"), mat=3)                                               # nozzle head
    add(bm, hq.rbox((0.01, 0.03, 0.03), (0.272, 0.015, 0.935), bevel=0.004, segments=1), mat=4) # trigger

    # drain hose under the sink
    hose = hq.bezier((0.0, 0.05, 0.63), (0.0, 0.05, 0.5), (0.2, 0.15, 0.35), (0.25, 0.24, 0.02), 20)
    add(bm, hq.tube(hose, 0.012, segments=12), mat=4)
    cols = [ColBox((0.62, 0.48, 0.05), (0, 0, 0.585)), ColBox((0.52, 0.42, 0.04), (0, 0, 0.16)), ColBox((w - 0.1, d - 0.1, 0.04), (0, 0, 0.64)),
            ColBox((w + 0.02, 0.05, 0.2), (0, -d / 2 + 0.005, 0.74)), ColBox((w + 0.02, 0.05, 0.2), (0, d / 2 - 0.005, 0.74)),
            ColBox((0.05, d, 0.2), (-w / 2 + 0.005, 0, 0.74)), ColBox((0.05, d, 0.2), (w / 2 - 0.005, 0, 0.74))]
    for x in (-0.28, 0.28):
        for y in (-0.2, 0.2):
            cols.append(ColBox((0.035, 0.035, 0.6), (x, y, 0.3)))
    cols.append(ColBox((0.94, 0.02, 0.52), (0, 0.235, 1.08)))
    return bm, cols


# ---------------------------------------------------------------------------
# Customer (unchanged V4 mannequin: customer art is out of V5 scope)
# ---------------------------------------------------------------------------
def customer_parts(rng):
    """V6 customer figure (about 1.75 m): a lofted torso with shoulders and a collared jacket, a pelvis with a belt,
    two-segment legs (LegL/R with a ShinL/R child at the knee) and arms (ArmL/R with a ForearmL/R child at the elbow),
    hands with thumbs, shoes with soles, a skull with a jaw, ears, eyes, brows, nose and mouth. Slots: 0 jacket,
    1 trousers, 2 skin, 3 hair / shoes / dark details. Parts are (name, bmesh, location, parent) with the child
    locations relative to their parent; Customer.cs finds them by name."""
    parts = []
    def usph(radius, segments=24, rings=14, center=(0, 0, 0), squash=1.0):
        # hq.uv_sphere sits on its centre point (base at z); this one is centred like the icosphere
        return hq.uv_sphere(radius, segments=segments, rings=rings, center=(center[0], center[1], center[2] - radius * squash), squash=squash)
    def slot(bm, i):
        for f in bm.faces:
            f.material_index = i
        # cutting a cap out of a sphere leaves vertices with no faces behind: drop them
        loose = [v for v in bm.verts if not v.link_faces]
        if loose:
            bmesh.ops.delete(bm, geom=loose, context="VERTS")
        return bm
    def rr(w, h, r, z, dx=0.0, dy=0.0):
        return [(x + dx, y + dy, zz) for (x, y, zz) in hq.ring_rrect(w, h, r, z, 28)]

    # ---- torso (pivot at the waist, z 0.95): waist, chest, shoulders, neck base; a jacket with collar, placket and buttons
    torso = hq.loft([rr(0.31, 0.2, 0.09, 0.0), rr(0.34, 0.22, 0.1, 0.14), rr(0.38, 0.24, 0.11, 0.3), rr(0.43, 0.25, 0.12, 0.46),
                     rr(0.42, 0.24, 0.12, 0.53), rr(0.26, 0.19, 0.09, 0.59), rr(0.14, 0.14, 0.06, 0.62)], close_bottom=True, close_top=True)
    hq.displace(torso, 0.004, 9.0, seed=31, octaves=1)
    slot(torso, 0)
    for sx in (-1, 1):
        cap = usph(0.078, segments=20, rings=12, center=(sx * 0.19, 0.0, 0.5), squash=0.85)
        lib.bm_append(torso, slot(cap, 0))
    collar = hq.loft([rr(0.2, 0.17, 0.07, 0.55), rr(0.24, 0.2, 0.08, 0.6), rr(0.2, 0.17, 0.07, 0.65)], close_bottom=False, close_top=False)
    lib.bm_append(torso, slot(collar, 0))
    lib.bm_append(torso, slot(hq.rbox((0.018, 0.006, 0.44), (0.0, -0.126, 0.24), bevel=0.001, segments=1), 0))     # placket
    for z in (0.12, 0.24, 0.36):
        lib.bm_append(torso, slot(hq.cyl(0.008, 0.004, segments=12, center=(0.018, -0.13, z)), 3))                   # buttons
    parts.append(("Torso", torso, (0, 0, 0.95), None))

    # ---- hips / pelvis (pivot 0.95) with a belt
    hips = hq.loft([rr(0.33, 0.22, 0.1, -0.22), rr(0.35, 0.24, 0.11, -0.1), rr(0.32, 0.21, 0.09, 0.0), rr(0.31, 0.2, 0.09, 0.03)], close_bottom=True, close_top=True)
    slot(hips, 1)
    belt = hq.loft([rr(0.325, 0.215, 0.095, -0.01), rr(0.33, 0.22, 0.095, 0.02)], close_bottom=False, close_top=False)
    lib.bm_append(hips, slot(belt, 3))
    lib.bm_append(hips, slot(hq.rbox((0.03, 0.006, 0.024), (0.0, -0.115, 0.005), bevel=0.002, segments=1), 3))         # buckle
    parts.append(("Hips", hips, (0, 0, 0.95), None))

    # ---- legs: thigh (pivot at the hip 0.9) and shin (child at the knee) with a cuffed trouser leg and a shoe with a sole
    for name, x in (("LegL", -0.095), ("LegR", 0.095)):
        thigh = hq.loft([hq.ring_ellipse(0.085, 0.09, 0.0, 20), hq.ring_ellipse(0.08, 0.085, -0.2, 20), hq.ring_ellipse(0.068, 0.07, -0.44, 20)], close_bottom=True, close_top=True)
        slot(thigh, 1)
        lib.bm_append(thigh, slot(usph(0.064, segments=18, rings=10, center=(0, 0, -0.45)), 1))               # knee
        parts.append((name, thigh, (x, 0, 0.9), None))
        shin = hq.loft([hq.ring_ellipse(0.06, 0.066, 0.0, 20), hq.ring_ellipse(0.055, 0.06, -0.2, 20), hq.ring_ellipse(0.048, 0.05, -0.36, 20), hq.ring_ellipse(0.05, 0.052, -0.39, 20)], close_bottom=True, close_top=True)
        slot(shin, 1)
        shoe = hq.rbox((0.095, 0.25, 0.06), (0.0, -0.045, -0.41), bevel=0.02, segments=3)
        for v in shoe.verts:
            if v.co.y < -0.12:
                v.co.z += 0.012 * (-0.12 - v.co.y) / 0.05    # the toe turns up a little
            if v.co.z < -0.43 and v.co.y < -0.1:
                v.co.x *= 0.92
        lib.bm_append(shin, slot(shoe, 3))
        lib.bm_append(shin, slot(hq.rbox((0.1, 0.26, 0.014), (0.0, -0.045, -0.437), bevel=0.003, segments=1), 3))    # sole
        # exported flat with its pivot at the knee (FBX child transforms come through in the root's frame on
        # import); the scene builder parents it under the thigh
        parts.append(("Shin" + name[-1], shin, (x, 0, 0.44), None))

    # ---- arms: upper arm (pivot at the shoulder 1.45) and forearm (child at the elbow) with a cuff and a hand
    for name, x in (("ArmL", -0.245), ("ArmR", 0.245)):
        upper = hq.loft([hq.ring_ellipse(0.058, 0.06, 0.0, 16), hq.ring_ellipse(0.052, 0.054, -0.16, 16), hq.ring_ellipse(0.046, 0.048, -0.29, 16)], close_bottom=True, close_top=True)
        slot(upper, 0)
        lib.bm_append(upper, slot(usph(0.045, segments=16, rings=10, center=(0, 0, -0.3)), 0))              # elbow
        parts.append((name, upper, (x, 0, 1.45), None))
        fore = hq.loft([hq.ring_ellipse(0.044, 0.046, 0.0, 16), hq.ring_ellipse(0.04, 0.042, -0.14, 16), hq.ring_ellipse(0.036, 0.038, -0.26, 16), hq.ring_ellipse(0.04, 0.042, -0.28, 16)], close_bottom=True, close_top=True)
        slot(fore, 0)
        hand = hq.rbox((0.075, 0.035, 0.09), (0.0, 0.0, -0.335), bevel=0.012, segments=2)
        lib.bm_append(fore, slot(hand, 2))
        sgn = -1 if name.endswith("L") else 1
        thumb = hq.rbox((0.022, 0.03, 0.045), (sgn * -0.04, -0.012, -0.31), bevel=0.006, segments=1)
        lib.bm_append(fore, slot(thumb, 2))
        parts.append(("Forearm" + name[-1], fore, (x, 0, 1.14), None))

    # ---- head (pivot at the neck base 1.53): skull with a jaw, neck, ears, eyes, brows, nose, mouth
    head = usph(0.092, segments=28, rings=18, center=(0, 0, 0.14))
    for v in head.verts:
        v.co.y = (v.co.y) * 1.12 + 0.005
        v.co.z = 0.145 + (v.co.z - 0.145) * 1.18
        if v.co.z < 0.13:                                   # the jaw narrows and comes forward
            k = (0.13 - v.co.z) / 0.09
            v.co.x *= 1.0 - 0.28 * k
            v.co.y -= 0.02 * k
    slot(head, 2)
    lib.bm_append(head, slot(hq.cyl(0.046, 0.09, segments=14, center=(0, 0.005, 0.045)), 2))                       # neck
    for sx in (-1, 1):
        ear = usph(0.022, segments=12, rings=8, center=(sx * 0.098, 0.01, 0.145), squash=1.0)
        for v in ear.verts:
            v.co.x = sx * 0.098 + (v.co.x - sx * 0.098) * 0.45
        lib.bm_append(head, slot(ear, 2))
        lib.bm_append(head, slot(usph(0.012, segments=12, rings=8, center=(sx * 0.036, -0.088, 0.158)), 3))   # eye
        lib.bm_append(head, slot(hq.rbox((0.034, 0.008, 0.006), (sx * 0.037, -0.094, 0.182), bevel=0.001, segments=1), 3))   # brow
    nose = hq.rbox((0.022, 0.03, 0.038), (0.0, -0.104, 0.138), bevel=0.006, segments=2)
    for v in nose.verts:
        if v.co.z > 0.145:
            v.co.y += 0.012 * (v.co.z - 0.145) / 0.02       # the bridge slopes back
    lib.bm_append(head, slot(nose, 2))
    lib.bm_append(head, slot(hq.rbox((0.034, 0.004, 0.005), (0.0, -0.096, 0.104), bevel=0.001, segments=1), 3))   # mouth
    parts.append(("Head", head, (0, 0, 1.53), None))

    # ---- hair variants and hats: the customer enables one (seeded), so the crowd is not one figure
    hair = usph(0.112, segments=26, rings=16, center=(0, 0.008, 0.16))
    hair_v = [v for v in hair.verts if v.co.z < 0.13 or (v.co.y < -0.05 and v.co.z < 0.19)]
    bmesh.ops.delete(hair, geom=hair_v, context="VERTS")
    hq.displace(hair, 0.006, 25.0, seed=41, octaves=1)
    parts.append(("HairShort", slot(hair, 3), (0, 0, 1.53), None))
    hair2 = usph(0.118, segments=26, rings=16, center=(0, 0.016, 0.155))
    hair2_v = [v for v in hair2.verts if (v.co.y < -0.04 and v.co.z < 0.2) or v.co.z < 0.04]
    bmesh.ops.delete(hair2, geom=hair2_v, context="VERTS")
    for v in hair2.verts:
        if v.co.y > 0.03:
            v.co.z -= 0.08 * (v.co.y - 0.03) / 0.1           # falls to the collar at the back
    hq.displace(hair2, 0.007, 22.0, seed=43, octaves=1)
    parts.append(("HairLong", slot(hair2, 3), (0, 0, 1.53), None))
    cap = usph(0.122, segments=26, rings=16, center=(0, 0.0, 0.165))
    cap_v = [v for v in cap.verts if v.co.z < 0.165]
    bmesh.ops.delete(cap, geom=cap_v, context="VERTS")
    peak = hq.rbox((0.15, 0.1, 0.012), (0, -0.14, 0.17), bevel=0.004, segments=2)
    for v in peak.verts:
        if v.co.y < -0.16:
            v.co.z -= 0.02 * (-0.16 - v.co.y) / 0.03
    lib.bm_append(cap, peak)
    lib.bm_append(cap, hq.cyl(0.01, 0.006, segments=10, center=(0, 0, 0.287)))                                     # button on top
    parts.append(("Cap", slot(cap, 1), (0, 0, 1.53), None))
    beanie = usph(0.124, segments=26, rings=16, center=(0, 0.0, 0.16))
    beanie_v = [v for v in beanie.verts if v.co.z < 0.135]
    bmesh.ops.delete(beanie, geom=beanie_v, context="VERTS")
    for v in beanie.verts:
        v.co.z = 0.135 + (v.co.z - 0.135) * 1.15
    brim = hq.loft([hq.ring_ellipse(0.125, 0.13, 0.135, 26), hq.ring_ellipse(0.128, 0.133, 0.165, 26)], close_bottom=False, close_top=False)
    lib.bm_append(beanie, brim)
    parts.append(("Beanie", slot(beanie, 1), (0, 0, 1.53), None))
    coat = hq.loft([rr(0.36, 0.25, 0.1, -0.42), rr(0.38, 0.26, 0.11, -0.3), rr(0.36, 0.24, 0.1, -0.2), rr(0.33, 0.22, 0.1, -0.02)], close_bottom=True, close_top=False)
    parts.append(("CoatTail", slot(coat, 0), (0, 0, 0.95), None))
    return parts


def build_customer():
    name = "prop_customer"
    rng = random.Random(240)
    root = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(root)
    objs = [root]
    by_name = {}
    for part_name, bm, loc, parent_name in customer_parts(rng):
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        hq.mark_sharp_by_angle(bm, 40.0)
        lib.bm_box_uv(bm, scale=UV_SCALE)
        lib.validate_bmesh(TAG, name + "/" + part_name, bm, require_manifold=False)
        obj = lib.object_from_bmesh(part_name, bm, smooth=None)
        mesh = obj.data
        for slot in range(4):
            mesh.materials.append(bpy.data.materials.new(f"customer_slot{slot}"))
        obj.parent = by_name[parent_name] if parent_name else root
        obj.location = loc
        by_name[part_name] = obj
        objs.append(obj)
    lib.export_fbx(objs, os.path.join(OUT_DIR, name + ".fbx"), tag=TAG)
    for o in objs[1:]:
        m = o.data
        bpy.data.objects.remove(o)
        bpy.data.meshes.remove(m)
    bpy.data.objects.remove(root)
    lib.log(TAG, f"{name}: jointed customer exported")


# ---------------------------------------------------------------------------------------------
# Back of house: the office desk, the storage racks and the goods-in gear the reference pack shows
# ---------------------------------------------------------------------------------------------
def office_desk(rng):
    """Office desk 1.5 x 0.72, top at 0.75: a solid worktop on a drawer pedestal and a steel leg frame
    (slot 1), with a modesty panel and a cable tray. Origin at the base centre."""
    bm = bmesh.new()
    box(bm, (1.5, 0.72, 0.038), (0, 0, 0.731), bevel=0.006, segments=3)
    # drawer pedestal on the left, three fronts with routed pulls
    box(bm, (0.42, 0.62, 0.63), (-0.5, 0.02, 0.4), bevel=0.005, segments=2)
    for i, dz in enumerate((0.19, 0.4, 0.61)):
        add(bm, hq.rbox((0.38, 0.016, 0.185), (-0.5, -0.295, dz), bevel=0.004, segments=2), mat=0)
        add(bm, hq.rbox((0.16, 0.014, 0.014), (-0.5, -0.307, dz + 0.07), bevel=0.004), mat=1)
    # steel leg frame on the right
    for lx in (0.62, 0.3):
        add(bm, hq.rbox((0.045, 0.045, 0.71), (lx, 0.28, 0.355), bevel=0.004), mat=1)
        add(bm, hq.rbox((0.045, 0.045, 0.71), (lx, -0.28, 0.355), bevel=0.004), mat=1)
    add(bm, hq.rbox((0.42, 0.04, 0.04), (0.46, 0.28, 0.68), bevel=0.004), mat=1)
    add(bm, hq.rbox((0.42, 0.04, 0.04), (0.46, -0.28, 0.68), bevel=0.004), mat=1)
    add(bm, hq.rbox((0.04, 0.6, 0.04), (0.62, 0, 0.06), bevel=0.004), mat=1)
    add(bm, hq.rbox((0.04, 0.6, 0.04), (0.3, 0, 0.06), bevel=0.004), mat=1)
    # modesty panel and cable tray at the back
    box(bm, (0.9, 0.018, 0.3), (0.26, 0.31, 0.5), bevel=0.003)
    add(bm, hq.rbox((0.8, 0.09, 0.05), (0.26, 0.26, 0.68), bevel=0.006), mat=1)
    return bm, [ColBox((1.5, 0.72, 0.76), (0, 0, 0.38))]


def laptop(rng):
    """Open laptop, 15 inch: milled aluminium base with a keyboard deck and trackpad, a hinged lid at 105
    degrees carrying the screen (slot 1) in a bezel. Origin at the base centre; the screen faces -Y."""
    bm = bmesh.new()
    base = hq.rbox((0.345, 0.245, 0.017), (0, 0, 0.0085), bevel=0.004, segments=3)
    add(bm, base, mat=0)
    # keyboard well and trackpad
    add(bm, hq.rbox((0.29, 0.115, 0.004), (0, 0.03, 0.0185), bevel=0.001), mat=2)
    for r in range(5):
        for c in range(14):
            kx = -0.135 + c * 0.0208
            ky = -0.018 + r * 0.0215
            add(bm, hq.rbox((0.0175, 0.0175, 0.0035), (kx, ky, 0.0205), bevel=0.0008), mat=2)
    add(bm, hq.rbox((0.105, 0.07, 0.002), (0, -0.075, 0.018), bevel=0.002), mat=2)
    # hinge and lid, laid back 15 degrees past upright
    add(bm, hq.cyl(0.006, 0.30, segments=24, center=(0, 0.121, 0.012)), T(0, 0, 0) @ R(90, "Y"), mat=1)
    lid = bmesh.new()
    add(lid, hq.rbox((0.345, 0.225, 0.011), (0, 0, 0.1125), bevel=0.003, segments=3), mat=0)
    add(lid, hq.rbox((0.313, 0.196, 0.002), (0, -0.007, 0.1125), bevel=0.001), mat=1)
    lid_m = T(0, 0.121, 0.014) @ R(-105, "X")
    add(bm, lid, lid_m, mat=0)
    lid.free()
    return bm, [ColBox((0.35, 0.25, 0.02), (0, 0, 0.01)), ColBox((0.35, 0.06, 0.24), (0, 0.16, 0.13))]


def letter_tray(rng):
    """Stacked pair of desk letter trays with riser posts and a label plate (slot 1). Origin at the base centre."""
    bm = bmesh.new()
    for i, z in enumerate((0.0, 0.085)):
        box(bm, (0.32, 0.24, 0.008), (0, 0, z + 0.028), bevel=0.002)
        add(bm, hq.rbox((0.32, 0.012, 0.05), (0, 0.114, z + 0.055), bevel=0.002), mat=0)
        for sx in (-1, 1):
            add(bm, hq.rbox((0.012, 0.24, 0.05), (sx * 0.154, 0, z + 0.055), bevel=0.002), mat=0)
        add(bm, hq.rbox((0.32, 0.012, 0.022), (0, -0.114, z + 0.04), bevel=0.002), mat=0)
        add(bm, hq.rbox((0.13, 0.004, 0.026), (0, -0.121, z + 0.042), bevel=0.001), mat=1)
        for sx in (-1, 1):
            for sy in (-1, 1):
                add(bm, hq.cyl(0.007, 0.028 if i == 0 else 0.057, segments=14,
                               center=(sx * 0.145, sy * 0.105, (z + 0.014) if i == 0 else (z - 0.0045))), mat=0)
    return bm, [ColBox((0.33, 0.25, 0.14), (0, 0, 0.07))]


def cork_board(rng):
    """Framed cork board 0.9 x 0.6 (slot 1 cork) with pinned notes and invoices (slot 2 paper). Wall prop in the
    pegboard convention: it stands in XZ with its face toward -Y and its origin at the bottom centre."""
    bm = bmesh.new()
    W, H = 0.9, 0.6
    for sx in (-1, 1):
        add(bm, hq.rbox((0.03, 0.028, H), (sx * (W / 2 - 0.015), 0.014, H / 2), bevel=0.003), mat=0)
    for sz in (0.015, H - 0.015):
        add(bm, hq.rbox((W, 0.028, 0.03), (0, 0.014, sz), bevel=0.003), mat=0)
    add(bm, hq.rbox((W - 0.03, 0.014, H - 0.03), (0, 0.021, H / 2), bevel=0.002), mat=1)
    r = random.Random(4211)
    for i in range(7):
        w = r.uniform(0.08, 0.14); h = r.uniform(0.09, 0.15)
        px = r.uniform(-0.33, 0.33); pz = r.uniform(0.1, H - 0.1)
        add(bm, hq.rbox((w, 0.0018, h), (0, 0, 0), bevel=0.0006), T(px, 0.0125, pz) @ R(r.uniform(-6, 6), "Y"), mat=2)
        add(bm, hq.cyl(0.004, 0.008, segments=10, center=(0, 0, 0)),
            T(px + w * 0.3, 0.008, pz + h * 0.36) @ R(90, "X"), mat=0)
    return bm, [ColBox((W, 0.032, H), (0, 0.016, H / 2))]


def mug(rng):
    """Enamel mug with a handle. Origin at the base centre."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0.0, 0.0), (0.038, 0.0), (0.041, 0.006), (0.043, 0.045), (0.045, 0.094),
                      (0.0455, 0.098), (0.0425, 0.098), (0.0405, 0.05), (0.038, 0.008), (0.0, 0.008)],
                     segments=36, loop=True), mat=0)
    add(bm, hq.torus(0.028, 0.0055, seg_major=28, seg_minor=10, center=(0.058, 0, 0.055), sweep=250.0),
        T(0, 0, 0) @ R(90, "X"), mat=0)
    return bm, [ColBox((0.095, 0.09, 0.1), (0.008, 0, 0.05))]


def pallet_jack(rng):
    """Hand pallet truck: two forks on load rollers, a hydraulic pump body and a steering handle raised to
    about 60 degrees. Origin between the forks at the floor; forks run along +Y."""
    bm = bmesh.new()
    for sx in (-1, 1):
        add(bm, hq.rbox((0.16, 1.15, 0.05), (sx * 0.17, 0.42, 0.06), bevel=0.006), mat=0)
        add(bm, hq.rbox((0.14, 0.2, 0.03), (sx * 0.17, 0.95, 0.045), bevel=0.004), mat=0)
        add(bm, hq.cyl(0.038, 0.05, segments=20, center=(sx * 0.17, 0.95, 0.038)), T(0, 0, 0) @ R(90, "Y"), mat=1)
    add(bm, hq.rbox((0.46, 0.16, 0.12), (0, -0.17, 0.12), bevel=0.008), mat=0)
    add(bm, hq.cyl(0.075, 0.34, segments=28, center=(0, -0.2, 0.3)), mat=0)
    for sx in (-1, 1):
        add(bm, hq.cyl(0.085, 0.06, segments=24, center=(sx * 0.19, -0.24, 0.085)), T(0, 0, 0) @ R(90, "Y"), mat=1)
    handle = bmesh.new()
    add(handle, hq.rbox((0.06, 0.055, 0.86), (0, 0, 0.43), bevel=0.006), mat=0)
    add(handle, hq.cyl(0.022, 0.34, segments=20, center=(0, 0, 0.87)), T(0, 0, 0) @ R(90, "Y"), mat=2)
    add(bm, handle, T(0, -0.24, 0.4) @ R(-28, "X"), mat=0)
    handle.free()
    return bm, [ColBox((0.5, 1.5, 0.16), (0, 0.32, 0.08)), ColBox((0.5, 0.3, 1.1), (0, -0.34, 0.55))]



# ---------------------------------------------------------------------------------------------------
# M7 retail pass (R06 showroom). Slot order is fixed by the scene builder's material lists, so every
# generator below numbers its slots 0 = carcass / body, 1 = the second surface, 2 = the accent.
# ---------------------------------------------------------------------------------------------------

def display_wall(rng):
    """Tall retail shelving 1.5 x 0.4 x 2.25: a dark carcass with a panelled back (slot 1), four open shelves
    with a bull-nosed front edge, LED strip housings under the three upper shelves (slot 2 emissive), a plinth
    and a crown. Front is -Y. Origin at the base centre."""
    bm = bmesh.new()
    W, D, H = 1.5, 0.4, 2.25
    t = 0.032
    SHELVES = (0.42, 0.87, 1.32, 1.77)
    # plinth and its shadow gap
    box(bm, (W, D, 0.09), (0, 0.005, 0.045), bevel=0.005, segments=2)
    box(bm, (W - 0.06, D - 0.04, 0.03), (0, 0.005, 0.105), bevel=0.003)
    # side pilasters, back panel, crown
    for sx in (-1, 1):
        box(bm, (t, D, H - 0.12), (sx * (W / 2 - t / 2), 0.005, 0.12 + (H - 0.12) / 2), bevel=0.004, segments=2)
    add(bm, hq.rbox((W - 2 * t, 0.016, H - 0.16), (0, D / 2 - 0.008, 0.12 + (H - 0.16) / 2), bevel=0.002), mat=1)
    for sz in (0.12, 0.6, 1.05, 1.5, 1.95):   # the back panel's rails read as boarding, not a flat sheet
        add(bm, hq.rbox((W - 2 * t, 0.006, 0.02), (0, D / 2 - 0.019, sz), bevel=0.002), mat=1)
    box(bm, (W, D, t), (0, 0.005, H - t / 2), bevel=0.004, segments=2)
    box(bm, (W + 0.05, D + 0.035, 0.05), (0, 0.005, H + 0.025), bevel=0.008, segments=3)
    box(bm, (W + 0.03, D + 0.02, 0.018), (0, 0.005, H - 0.035), bevel=0.004)
    # shelves: a board, a bull-nose front lip, and a light housing on the underside of the one above
    for i, sz in enumerate(SHELVES):
        box(bm, (W - 2 * t, D - 0.03, 0.028), (0, 0.02, sz - 0.014), bevel=0.004, segments=2)
        box(bm, (W - 2 * t, 0.024, 0.042), (0, -D / 2 + 0.027, sz - 0.006), bevel=0.008, segments=3)
        for sx in (-1, 1):   # shelf pins, seated in the pilaster and running across the shelf's underside
            add(bm, hq.cyl(0.005, 0.05, segments=10), T(sx * (W / 2 - t - 0.005), 0.16, sz - 0.024) @ R(90, "X"), mat=2)
    for sz in SHELVES[1:] + (H - t,):
        add(bm, hq.rbox((W - 2 * t - 0.09, 0.035, 0.016), (0, -0.045, sz - 0.036), bevel=0.004), mat=0)
        add(bm, hq.rbox((W - 2 * t - 0.11, 0.022, 0.006), (0, -0.045, sz - 0.046), bevel=0.002), mat=2)
    cols = [ColBox((W, D, 0.12), (0, 0.005, 0.06))]
    for sx in (-1, 1):
        cols.append(ColBox((t, D, H - 0.12), (sx * (W / 2 - t / 2), 0.005, 0.12 + (H - 0.12) / 2)))
    cols.append(ColBox((W - 2 * t, 0.02, H - 0.16), (0, D / 2 - 0.01, 0.12 + (H - 0.16) / 2)))
    cols.append(ColBox((W + 0.05, D + 0.035, 0.13), (0, 0.005, H - 0.01)))
    for sz in SHELVES:
        cols.append(ColBox((W - 2 * t, D - 0.03, 0.03), (0, 0.02, sz - 0.015)))
    return bm, cols


def glass_counter(rng):
    """Island display counter 1.45 x 0.8 x 0.95: a panelled wood plinth, glazed sides and top (slot 1) between
    four corner posts, an internal glass shelf, and a brass lock plate and kick strip (slot 2). The top surface is
    at 0.95. Origin at the base centre.

    The width is a circulation number, not a styling one: the showroom is 3.96 m of usable floor between the
    partition and the wall case, and a customer has to be able to pass a standing player on either side of the
    island, so the island may not take more than 1.46 m of it."""
    bm = bmesh.new()
    W, D, H = 1.45, 0.8, 0.95
    post = 0.05
    base_h, glass_h = 0.30, 0.60
    # plinth: a kick, a panelled body and a nosing the glass sits on
    box(bm, (W - 0.06, D - 0.06, 0.05), (0, 0, 0.025), bevel=0.004)
    box(bm, (W - 0.02, D - 0.02, base_h - 0.09), (0, 0, 0.05 + (base_h - 0.09) / 2), bevel=0.006, segments=2)
    for sy in (-1, 1):
        for px in (-0.42, 0.0, 0.42):
            add(bm, hq.rbox((0.37, 0.012, 0.15), (px, sy * (D / 2 - 0.017), 0.155), bevel=0.004, segments=2), mat=0)
    for sx in (-1, 1):
        add(bm, hq.rbox((0.012, 0.5, 0.15), (sx * (W / 2 - 0.017), 0, 0.155), bevel=0.004, segments=2), mat=0)
    box(bm, (W, D, 0.04), (0, 0, base_h - 0.02), bevel=0.006, segments=3)
    # corner posts and the top frame
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(bm, (post, post, glass_h), (sx * (W / 2 - post / 2), sy * (D / 2 - post / 2), base_h + glass_h / 2),
                bevel=0.005, segments=2)
    for sy in (-1, 1):
        box(bm, (W, post, 0.055), (0, sy * (D / 2 - post / 2), H - 0.0275), bevel=0.006, segments=3)
    for sx in (-1, 1):
        box(bm, (post, D - 2 * post, 0.055), (sx * (W / 2 - post / 2), 0, H - 0.0275), bevel=0.006, segments=3)
    # glazing: four panes, an internal shelf and the top light
    gw, gd = W - 2 * post, D - 2 * post
    for sy in (-1, 1):
        add(bm, hq.rbox((gw, 0.008, glass_h - 0.01), (0, sy * (D / 2 - post / 2), base_h + glass_h / 2)), mat=1)
    for sx in (-1, 1):
        add(bm, hq.rbox((0.008, gd, glass_h - 0.01), (sx * (W / 2 - post / 2), 0, base_h + glass_h / 2)), mat=1)
    add(bm, hq.rbox((gw - 0.02, gd - 0.02, 0.01), (0, 0, base_h + 0.28)), mat=1)
    add(bm, hq.rbox((W - 2 * post - 0.01, D - 2 * post - 0.01, 0.012), (0, 0, H - 0.061)), mat=1)
    # brass: the lock escutcheon on the customer side, and a kick strip along the plinth
    add(bm, hq.cyl(0.019, 0.008, segments=20), T(0, -D / 2 + post / 2, base_h + 0.22) @ R(90, "X"), mat=2)
    add(bm, hq.cyl(0.005, 0.01, segments=10), T(0, -D / 2 + post / 2 - 0.006, base_h + 0.22) @ R(90, "X"), mat=2)
    add(bm, hq.rbox((W - 0.14, 0.01, 0.016), (0, -D / 2 + 0.004, 0.09), bevel=0.003), mat=2)
    return bm, [ColBox((W, D, H), (0, 0, H / 2))]


def shop_plant(rng):
    """Potted shop plant, 1.15 tall: a tapered glazed pot (slot 0) over dark soil (slot 1) with a fan of
    strap leaves (slot 2). Origin at the base centre."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0, 0), (0.15, 0), (0.155, 0.012), (0.175, 0.2), (0.19, 0.33), (0.2, 0.36),
                      (0.192, 0.365), (0.182, 0.34), (0.167, 0.21), (0.147, 0.02), (0, 0.02)],
                     segments=40, loop=True), mat=0)
    add(bm, hq.lathe([(0, 0.325), (0.16, 0.335), (0.178, 0.345)], segments=32, close_top=False), mat=1)
    # leaves: a strap tapering to a point, bowed outward, in three tiers around the pot
    for tier, (n, tilt, length, phase) in enumerate(((6, 50.0, 0.34, 0.0), (5, 32.0, 0.52, 30.0), (4, 14.0, 0.68, 60.0))):
        for i in range(n):
            a = 360.0 * i / n + phase + rng.uniform(-7.0, 7.0)
            L = length * rng.uniform(0.9, 1.08)
            rings = []
            for k in range(9):
                u = k / 8.0
                # bow: the leaf arcs out and then droops at the tip
                r = 0.055 * (1.0 - u * u * 0.9) + 0.004
                bend = math.radians(tilt) * (0.35 + 0.65 * u)
                x = math.sin(bend) * L * u
                z = math.cos(bend) * L * u
                rings.append([(x, -r, 0.34 + z), (x + 0.006 * (1 - u), 0.0, 0.345 + z),
                              (x, r, 0.34 + z), (x - 0.006 * (1 - u), 0.0, 0.335 + z)])
            leaf = hq.loft(rings, close_bottom=True, close_top=True)
            add(bm, leaf, R(a, "Z"), mat=2, sharp=70)
    return bm, [ColBox((0.4, 0.4, 0.38), (0, 0, 0.19))]


def logo_mountains(rng):
    """The Geode Emporium mark: three overlapping peaks in relief with inlaid caps (slot 1), 1.3 x 0.52.
    Wall prop in the pegboard convention: it stands in XZ with its face toward -Y, origin at the bottom centre."""
    bm = bmesh.new()
    peaks = ((-0.40, 0.30, 0.34), (0.02, 0.46, 0.52), (0.42, 0.28, 0.32))
    for i, (cx, half, h) in enumerate(peaks):
        body = hq.extrude_profile([(cx - half, 0.0), (cx + half, 0.0), (cx, h)], 0.045, axis="Y", center=(0, 0, 0))
        add(bm, body, mat=0, sharp=70)
        # snow cap: the top fifth of the peak, proud of the face
        cut = 0.30
        hw = half * cut
        cap = hq.extrude_profile([(cx - hw, h * (1 - cut)), (cx + hw, h * (1 - cut)), (cx, h)], 0.052, axis="Y",
                                 center=(0, -0.004, 0))
        add(bm, cap, mat=1, sharp=70)
    add(bm, hq.rbox((1.3, 0.022, 0.016), (0, 0.012, 0.0), bevel=0.004), mat=0)   # a ground rule under the range
    return bm, None


def pendant_globe(rng):
    """Retail pendant: a brass ceiling rose and rod carrying a spun cup and an opal globe (slot 1). Origin at the
    ceiling attachment (top); the globe centre is 0.62 m below it."""
    bm = bmesh.new()
    add(bm, hq.lathe([(0.035, 0), (0.04, -0.012), (0.036, -0.03), (0.012, -0.034), (0, -0.034), (0, 0)],
                     segments=32, loop=True), mat=0)
    add(bm, hq.cyl(0.008, 0.46, segments=16, center=(0, 0, -0.5)), mat=0)
    add(bm, hq.lathe([(0, -0.5), (0.02, -0.5), (0.075, -0.545), (0.082, -0.56), (0.078, -0.564),
                      (0.068, -0.55), (0.018, -0.508), (0, -0.508)], segments=36, loop=True), mat=0)
    add(bm, hq.uv_sphere(0.115, segments=32, rings=18, center=(0, 0, -0.62), squash=0.88), mat=1)
    add(bm, hq.torus(0.086, 0.008, seg_major=32, seg_minor=10, center=(0, 0, -0.556)), mat=0)
    return bm, None


def display_riser(rng):
    """Display riser 0.22 x 0.22 x 0.075: a bevelled block with a felt top (slot 1), the black base a shop stands
    a hero piece on. Origin at the base centre."""
    bm = bmesh.new()
    box(bm, (0.22, 0.22, 0.07), (0, 0, 0.035), bevel=0.008, segments=3)
    add(bm, hq.rbox((0.2, 0.2, 0.008), (0, 0, 0.073), bevel=0.002), mat=1)
    return bm, [ColBox((0.22, 0.22, 0.077), (0, 0, 0.0385))]


PROPS = [
    ("prop_display_wall", display_wall, 281),
    ("prop_glass_counter", glass_counter, 282),
    ("prop_shop_plant", shop_plant, 283),
    ("prop_logo_mountains", logo_mountains, 284),
    ("prop_pendant_globe", pendant_globe, 285),
    ("prop_display_riser", display_riser, 286),
    ("prop_office_desk", office_desk, 271),
    ("prop_laptop", laptop, 272),
    ("prop_letter_tray", letter_tray, 273),
    ("prop_cork_board", cork_board, 274),
    ("prop_mug", mug, 275),
    ("prop_pallet_jack", pallet_jack, 276),
    ("prop_hammer", hammer, 201),
    ("prop_chisel", lambda r: chisel(r, False), 202),
    ("prop_chisel_fine", lambda r: chisel(r, True), 203),
    ("prop_lump_hammer", lump_hammer, 257),
    ("prop_loupe", loupe, 232),
    ("prop_shop_case", shop_case, 233),
    ("prop_shop_table", shop_table, 234),
    ("prop_counter", counter, 235),
    ("prop_register", register, 236),
    ("prop_price_card", price_card, 237),
    ("prop_card_reader", card_reader, 263),
    ("prop_bank_card", bank_card, 264),
    ("prop_banknotes", banknotes, 265),
    ("prop_paper_bag", paper_bag, 266),
    ("prop_gift_box", gift_box, 267),
    ("prop_register_drawer", register_drawer, 258),
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
    ("prop_pendant_lamp", pendant_lamp, 223),
    ("prop_wall_shelf", wall_shelf, 224),
    ("prop_jar", jar, 225),
    ("prop_rock_bin", rock_bin, 226),
    ("prop_extinguisher", extinguisher, 227),
    ("prop_poster_frame", poster_frame, 228),
    ("prop_wall_clock", wall_clock, 229),
    ("prop_broom", broom, 230),
    ("prop_sign_board", sign_board, 231),
    ("prop_saw_station", saw_station, 241),
    ("prop_saw_blade", saw_blade, 242),
    ("prop_saw_vise", saw_vise, 243),
    ("prop_saw_jaw", saw_jaw, 244),
    ("prop_saw_wheel", saw_wheel, 252),
    ("prop_saw_needle", saw_needle, 253),
    ("prop_saw_valve", saw_valve, 254),
    ("prop_cracker", cracker, 255),
    ("prop_cracker_lever", cracker_lever, 256),
    ("prop_saw_station_large", saw_station_large, 259),
    ("prop_saw_blade_large", saw_blade_large, 260),
    ("prop_plinth", plinth, 261),
    ("prop_uv_lamp", uv_lamp, 262),
    ("prop_polish_lap", polish_lap, 245),
    ("prop_polish_disc", polish_disc, 246),
    ("prop_wash_tub", wash_tub, 247),
    ("prop_brush", brush, 248),
    ("prop_rock_rack", rock_rack, 249),
    ("prop_heavy_cradle", heavy_cradle, 250),
    ("prop_wedge", wedge, 251),
]

# props whose origin is not the base centre (tool tips, ceiling attachment, blade axle)
KEEP_ORIGIN = {"prop_pendant_globe", "prop_pendant_lamp", "prop_saw_blade", "prop_saw_blade_large", "prop_chisel", "prop_chisel_fine", "prop_wedge", "prop_wall_clock", "prop_saw_wheel", "prop_saw_needle", "prop_saw_valve", "prop_cracker_lever", "prop_register_drawer"}
KEEP_XY = {"prop_logo_mountains", "prop_cork_board", "prop_laptop", "prop_letter_tray", "prop_mug", "prop_hammer", "prop_lump_hammer", "prop_loupe", "prop_price_card", "prop_tablet", "prop_label_stand", "prop_scale_station", "prop_pegboard", "prop_window_frame", "prop_task_lamp"}
MAX_VERTS = 40000


def build_all(only=None):
    lib.reset_scene()
    lib.ensure_dir(OUT_DIR)
    built = []
    for name, builder, seed in PROPS:
        if only and name not in only:
            continue
        rng = random.Random(seed)
        bm, cols = builder(rng)
        lib.bm_box_uv(bm, scale=UV_SCALE)
        if name in KEEP_ORIGIN:
            pass
        elif name in KEEP_XY:
            lib.bm_origin_to_base(bm, center_xy=False)
        else:
            lib.bm_origin_to_base(bm, center_xy=True)
        lib.validate_bmesh(TAG, name, bm, require_manifold=False)
        # Unity numbers submeshes by the order a slot FIRST appears in the face list, so faces are sorted by slot
        # (and every slot up to the highest must be used) for the scene builder's material lists to line up
        used = sorted(set(f.material_index for f in bm.faces))
        if used != list(range(len(used))):
            lib.fail(TAG, f"{name}: material slots must be contiguous from 0, got {used}")
        hq.bake_wear(bm)   # V6 wear masks for the worn-surface materials (vertex colour: edges / cavities / up); subdivides panels
        bm.faces.sort(key=lambda f: f.material_index)
        bm.faces.index_update()
        obj = lib.object_from_bmesh(name, bm, smooth=None)
        lib.apply_transforms(obj)
        hq.add_weighted_normals(obj)
        mesh = obj.data
        max_slot = max((poly.material_index for poly in mesh.polygons), default=0)
        for slot in range(max_slot + 1):
            mesh.materials.append(bpy.data.materials.new(f"{name}_slot{slot}"))
        xs = [v.co.x for v in mesh.vertices]
        ys = [v.co.y for v in mesh.vertices]
        zs = [v.co.z for v in mesh.vertices]
        tris = sum(len(p.vertices) - 2 for p in mesh.polygons)
        lib.log(TAG, f"{name}: {len(mesh.vertices)} verts / {tris} tris  size={max(xs)-min(xs):.3f}x{max(ys)-min(ys):.3f}x{max(zs)-min(zs):.3f} "
                     f"base_z={min(zs):.4f} slots={max_slot + 1} cols={len(cols) if cols else 0}")
        if len(mesh.vertices) > MAX_VERTS:
            lib.fail(TAG, f"{name}: too dense ({len(mesh.vertices)} verts)")
        objs = [obj]
        if cols:
            for co in hq.col_objects(name, cols):
                co.parent = obj
                objs.append(co)
        lib.export_fbx(objs, os.path.join(OUT_DIR, name + ".fbx"), tag=TAG)
        built.append(name)
        for o in objs:
            m = o.data
            bpy.data.objects.remove(o)
            bpy.data.meshes.remove(m)
    if not only or "prop_customer" in only:
        build_customer()
        built.append("prop_customer")
    return built


def main():
    global OUT_DIR
    only = None
    if "--" in sys.argv:
        rest = sys.argv[sys.argv.index("--") + 1:]
        while len(rest) >= 2:
            key, val = rest[0], rest[1]
            rest = rest[2:]
            if key == "only":
                only = set(val.split(","))
            elif key == "out":
                OUT_DIR = val   # staging folder (copied into Assets/ once the Editor is idle)
    try:
        built = build_all(only)
    except SystemExit:
        raise
    except Exception:
        traceback.print_exc()
        sys.exit(1)
    lib.log(TAG, f"OK - {len(built)} props exported to {os.path.relpath(OUT_DIR, lib.REPO_ROOT)}")


if __name__ == "__main__":
    main()

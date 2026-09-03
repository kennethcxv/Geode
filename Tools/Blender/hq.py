"""
High-quality mesh helpers for Geode Empire hero props (headless bpy, Blender 4.1+/5.x).

Everything here is deterministic and works in --background mode. The helpers build bmesh parts that carry:
  * smooth faces with SHARP EDGES marked by angle (Smooth-by-Angle), so the FBX carries split normals and
    curved surfaces shade as curves while machined edges stay crisp;
  * real bevels (2-3 segments) where a real object has a rounded edge;
  * enough radial resolution that a cylinder reads as round at arm's length (48 segments for anything the
    player holds, 24-32 for background parts);
  * per-face material slots (material_index) so metal / rubber / paint / wood separate in Unity.

Collision proxies: a builder may return simple axis-aligned boxes (`col_boxes`) that the Unity scene builder
turns into BoxColliders, so a 12k-triangle saw does not need a 12k-triangle MeshCollider.
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector, noise

import geode_blender_lib as lib

TWO_PI = math.pi * 2.0


# ---------------------------------------------------------------------------
# Finishing: smoothing / sharp edges / materials / append
# ---------------------------------------------------------------------------
def mark_sharp_by_angle(bm, angle_deg=32.0):
    """Faces smooth, edges sharper than the limit marked sharp (the exporter then splits normals there)."""
    limit = math.radians(angle_deg)
    bm.normal_update()
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if not e.is_manifold:
            e.smooth = True
            continue
        try:
            e.smooth = e.calc_face_angle(0.0) <= limit
        except ValueError:
            e.smooth = True
    return bm


def set_material(bm, index):
    for f in bm.faces:
        f.material_index = index
    return bm


def add(dst, part, matrix=None, mat=0, sharp_deg=32.0, flat=False):
    """Finish a part (materials + smoothing) and append it to `dst`. Frees `part`."""
    bmesh.ops.recalc_face_normals(part, faces=part.faces)
    set_material(part, mat)
    if flat:
        for f in part.faces:
            f.smooth = False
    else:
        mark_sharp_by_angle(part, sharp_deg)
    # bm_append copies smooth flags via mesh round-trip (faces + edges), so the marks survive
    return lib.bm_append(dst, part, matrix)


def T(x=0.0, y=0.0, z=0.0):
    return Matrix.Translation((x, y, z))


def R(deg, axis):
    return Matrix.Rotation(math.radians(deg), 4, axis)


def S(x, y=None, z=None):
    if y is None:
        y = x
    if z is None:
        z = x
    return Matrix.Diagonal((x, y, z, 1.0))


# ---------------------------------------------------------------------------
# Primitives
# ---------------------------------------------------------------------------
def rbox(size, center=(0, 0, 0), bevel=0.0, segments=2):
    """Box with rounded edges (bevel width, segments)."""
    b = lib.bm_box(size, center)
    if bevel > 0:
        bmesh.ops.bevel(b, geom=list(b.edges), offset=min(bevel, min(size) * 0.45), offset_type="OFFSET",
                        segments=segments, profile=0.5, affect="EDGES", clamp_overlap=True, loop_slide=True)
        bmesh.ops.recalc_face_normals(b, faces=b.faces)
    return b


def cyl(radius, height, segments=48, center=(0, 0, 0), radius_top=None, bevel=0.0, bsegs=2, cap=True):
    """Cylinder / cone frustum along +Z from center, optional rounded rims."""
    c = lib.bm_cylinder(radius, height, segments=segments, center=center, cap=cap, radius_top=radius_top)
    if bevel > 0 and cap:
        rim = [e for e in c.edges if not all(abs(v.co.z - center[2]) > 1e-6 and abs(v.co.z - center[2] - height) > 1e-6 for v in e.verts)]
        rim = [e for e in c.edges if abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-6]
        bmesh.ops.bevel(c, geom=rim, offset=min(bevel, height * 0.45, radius * 0.45), offset_type="OFFSET",
                        segments=bsegs, profile=0.5, affect="EDGES", clamp_overlap=True, loop_slide=True)
        bmesh.ops.recalc_face_normals(c, faces=c.faces)
    return c


def lathe(profile, segments=48, center=(0, 0, 0), close_bottom=True, close_top=True, loop=False):
    """Revolve a profile of (r, z) pairs (bottom -> top) around Z. r == 0 makes a pole vertex.
    loop=True joins the last profile point back to the first (a closed section: rings, tyres, bezels)."""
    verts, faces = [], []
    rings = []
    cx, cy, cz = center
    if loop:
        profile = list(profile) + [profile[0]]
        close_bottom = close_top = False
    for (r, z) in profile:
        if r <= 1e-6:
            verts.append((cx, cy, cz + z))
            rings.append((len(verts) - 1, None))
        else:
            base = len(verts)
            for i in range(segments):
                a = TWO_PI * i / segments
                verts.append((cx + r * math.cos(a), cy + r * math.sin(a), cz + z))
            rings.append((base, segments))
    for k in range(len(rings) - 1):
        (b0, n0), (b1, n1) = rings[k], rings[k + 1]
        if n0 is None and n1 is None:
            continue
        if n0 is None:
            for i in range(n1):
                j = (i + 1) % n1
                faces.append((b0, b1 + i, b1 + j))
        elif n1 is None:
            for i in range(n0):
                j = (i + 1) % n0
                faces.append((b0 + i, b1, b0 + j))
        else:
            for i in range(n0):
                j = (i + 1) % n0
                faces.append((b0 + i, b0 + j, b1 + j, b1 + i))
    b_first, n_first = rings[0]
    if close_bottom and n_first is not None:
        faces.append(tuple(reversed(range(b_first, b_first + n_first))))
    b_last, n_last = rings[-1]
    if close_top and n_last is not None:
        faces.append(tuple(range(b_last, b_last + n_last)))
    bm = lib.bm_from_pydata(verts, faces)
    if loop:
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def ring_oct(r, z, n=16):
    """Octagon ring (apothem r) sampled with n points: vertices and edge midpoints alternate (n = 16)."""
    pts = []
    for i in range(n):
        a = TWO_PI * i / n
        rr = r / math.cos(math.pi / 8) if i % 2 == 0 else r
        pts.append((rr * math.cos(a), rr * math.sin(a), z))
    return pts


def sphere(radius, subdiv=3, center=(0, 0, 0)):
    return lib.bm_icosphere(radius, subdiv, center)


def uv_sphere(radius, segments=32, rings=16, center=(0, 0, 0), squash=1.0):
    prof = []
    for k in range(rings + 1):
        t = k / rings
        a = -math.pi / 2 + t * math.pi
        prof.append((radius * math.cos(a), radius * math.sin(a) * squash + radius * squash))
    return lathe(prof, segments=segments, center=center)


def torus(major, minor, seg_major=48, seg_minor=16, center=(0, 0, 0), squash=1.0, sweep=360.0):
    verts, faces = [], []
    closed = sweep >= 359.9
    n_maj = seg_major if closed else seg_major + 1
    for i in range(n_maj):
        a = math.radians(sweep) * i / seg_major
        ca, sa = math.cos(a), math.sin(a)
        for j in range(seg_minor):
            b = TWO_PI * j / seg_minor
            r = major + minor * math.cos(b)
            verts.append((center[0] + r * ca, center[1] + r * sa, center[2] + minor * math.sin(b) * squash))
    for i in range(seg_major):
        i2 = (i + 1) % n_maj
        for j in range(seg_minor):
            j2 = (j + 1) % seg_minor
            faces.append((i * seg_minor + j, i2 * seg_minor + j, i2 * seg_minor + j2, i * seg_minor + j2))
    if not closed:
        faces.append(tuple(reversed(range(seg_minor))))
        e = seg_major * seg_minor
        faces.append(tuple(range(e, e + seg_minor)))
    bm = lib.bm_from_pydata(verts, faces)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def _frames(points):
    """Parallel-transport frames along a polyline: list of (origin, tangent, u, v)."""
    pts = [Vector(p) for p in points]
    tangents = []
    for i in range(len(pts)):
        if i == 0:
            t = pts[1] - pts[0]
        elif i == len(pts) - 1:
            t = pts[-1] - pts[-2]
        else:
            t = (pts[i + 1] - pts[i]).normalized() + (pts[i] - pts[i - 1]).normalized()
        tangents.append(t.normalized())
    t0 = tangents[0]
    ref = Vector((0, 0, 1)) if abs(t0.z) < 0.9 else Vector((1, 0, 0))
    u = (ref - t0 * ref.dot(t0)).normalized()
    frames = []
    for i, t in enumerate(tangents):
        if i > 0:
            prev = tangents[i - 1]
            axis = prev.cross(t)
            if axis.length > 1e-6:
                ang = math.acos(max(-1.0, min(1.0, prev.dot(t))))
                u = Matrix.Rotation(ang, 3, axis.normalized()) @ u
            u = (u - t * u.dot(t)).normalized()
        v = t.cross(u).normalized()
        frames.append((pts[i], t, u, v))
    return frames


def tube(points, radius, segments=16, cap=True, radius_fn=None):
    """A round tube swept along a polyline (hoses, cords, pipes, bent rods). Rounded joints via dense points."""
    frames = _frames(points)
    verts, faces = [], []
    n = len(frames)
    for k, (o, t, u, v) in enumerate(frames):
        r = radius if radius_fn is None else radius_fn(k / max(1, n - 1))
        for i in range(segments):
            a = TWO_PI * i / segments
            verts.append(tuple(o + u * (r * math.cos(a)) + v * (r * math.sin(a))))
    for k in range(n - 1):
        b0, b1 = k * segments, (k + 1) * segments
        for i in range(segments):
            j = (i + 1) % segments
            faces.append((b0 + i, b0 + j, b1 + j, b1 + i))
    if cap:
        faces.append(tuple(reversed(range(segments))))
        e = (n - 1) * segments
        faces.append(tuple(range(e, e + segments)))
    bm = lib.bm_from_pydata(verts, faces)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def arc_points(center, radius, a0_deg, a1_deg, n, plane="XZ"):
    pts = []
    for i in range(n + 1):
        a = math.radians(a0_deg + (a1_deg - a0_deg) * i / n)
        c, s = math.cos(a), math.sin(a)
        if plane == "XZ":
            pts.append((center[0] + radius * c, center[1], center[2] + radius * s))
        elif plane == "YZ":
            pts.append((center[0], center[1] + radius * c, center[2] + radius * s))
        else:
            pts.append((center[0] + radius * c, center[1] + radius * s, center[2]))
    return pts


def bezier(p0, p1, p2, p3, n=16):
    out = []
    P = [Vector(p) for p in (p0, p1, p2, p3)]
    for i in range(n + 1):
        t = i / n
        mt = 1 - t
        out.append(tuple(P[0] * mt ** 3 + P[1] * (3 * mt * mt * t) + P[2] * (3 * mt * t * t) + P[3] * t ** 3))
    return out


def extrude_profile(profile2d, length, axis="Y", center=(0, 0, 0), close=True):
    """Extrude a closed 2D outline (list of (a, b)) along an axis. For axis Y the outline lives in XZ."""
    n = len(profile2d)
    verts, faces = [], []
    h = length / 2
    for side in (-h, h):
        for (a, b) in profile2d:
            if axis == "Y":
                verts.append((center[0] + a, center[1] + side, center[2] + b))
            elif axis == "X":
                verts.append((center[0] + side, center[1] + a, center[2] + b))
            else:
                verts.append((center[0] + a, center[1] + b, center[2] + side))
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    if close:
        faces.append(tuple(reversed(range(n))))
        faces.append(tuple(range(n, 2 * n)))
    bm = lib.bm_from_pydata(verts, faces)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def loft(rings, close_bottom=True, close_top=True):
    """Skin a list of rings (each a list of 3D points, all the same length) into a tube; optional caps."""
    n = len(rings[0])
    verts, faces = [], []
    for ring in rings:
        assert len(ring) == n
        verts.extend(tuple(p) for p in ring)
    for k in range(len(rings) - 1):
        b0, b1 = k * n, (k + 1) * n
        for i in range(n):
            j = (i + 1) % n
            faces.append((b0 + i, b0 + j, b1 + j, b1 + i))
    if close_bottom:
        faces.append(tuple(reversed(range(n))))
    if close_top:
        e = (len(rings) - 1) * n
        faces.append(tuple(range(e, e + n)))
    bm = lib.bm_from_pydata(verts, faces)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def ring_ellipse(rx, ry, z, n=16, phase=0.0):
    return [(rx * math.cos(TWO_PI * i / n + phase), ry * math.sin(TWO_PI * i / n + phase), z) for i in range(n)]


def ring_rrect(w, h, r, z, n=16):
    """A rounded-rectangle ring with exactly n points (n multiple of 4) at height z."""
    pts = rounded_rect(w, h, r, n=max(1, n // 4 - 1))
    # rounded_rect returns 4*(k+1) points for k = n//4 - 1  ->  n points
    return [(x, y, z) for (x, y) in pts]


def rounded_rect(w, h, r, n=6, center=(0.0, 0.0)):
    """Closed 2D outline of a rectangle with rounded corners (for extrude_profile)."""
    cx, cy = center
    hw, hh = w / 2, h / 2
    pts = []
    corners = [(hw - r, hh - r, 0), (-hw + r, hh - r, 90), (-hw + r, -hh + r, 180), (hw - r, -hh + r, 270)]
    for (x, y, a0) in corners:
        for i in range(n + 1):
            a = math.radians(a0 + 90 * i / n)
            pts.append((cx + x + r * math.cos(a), cy + y + r * math.sin(a)))
    return pts


# ---------------------------------------------------------------------------
# Deformers
# ---------------------------------------------------------------------------
def displace(bm, amplitude, frequency, seed=0, along_normal=True, octaves=2, mask=None):
    """Perlin displacement (deterministic: mathutils.noise with a seed offset)."""
    bm.normal_update()
    off = Vector((seed * 7.31, seed * 3.17, seed * 11.9))
    for v in bm.verts:
        p = v.co * frequency + off
        n = 0.0
        amp = 1.0
        for o in range(octaves):
            n += noise.noise(p * (2 ** o)) * amp
            amp *= 0.5
        d = amplitude * n
        if mask is not None:
            d *= mask(v)
        v.co += (v.normal if along_normal else Vector((0, 0, 1))) * d
    bm.normal_update()
    return bm


def taper(bm, axis="Z", z0=0.0, z1=1.0, s0=1.0, s1=1.0, about=(0.0, 0.0)):
    """Scale XY (about a point) as a linear function of Z."""
    for v in bm.verts:
        t = 0.0 if z1 == z0 else max(0.0, min(1.0, (v.co.z - z0) / (z1 - z0)))
        s = s0 + (s1 - s0) * t
        v.co.x = about[0] + (v.co.x - about[0]) * s
        v.co.y = about[1] + (v.co.y - about[1]) * s
    return bm


# ---------------------------------------------------------------------------
# Compound helpers (hardware)
# ---------------------------------------------------------------------------
def hex_bolt(radius, head_h, shank_len=0.0, segments=6):
    """Hex head with a chamfer, optional shank below. Origin at the head's underside."""
    bm = bmesh.new()
    head = cyl(radius, head_h, segments=segments, center=(0, 0, 0))
    bmesh.ops.bevel(head, geom=[e for e in head.edges if abs(e.verts[0].co.z - head_h) < 1e-6 and abs(e.verts[1].co.z - head_h) < 1e-6],
                    offset=radius * 0.18, segments=1, profile=0.5, affect="EDGES", clamp_overlap=True)
    lib.bm_append(bm, head)
    if shank_len > 0:
        lib.bm_append(bm, cyl(radius * 0.55, shank_len, segments=12, center=(0, 0, -shank_len)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def knob(radius, height, segments=32, ridges=0):
    """Round control knob: domed top, optional ridged grip."""
    prof = [(0, 0), (radius, 0), (radius, height * 0.7), (radius * 0.92, height * 0.9), (radius * 0.6, height), (0, height)]
    k = lathe(prof, segments=segments)
    if ridges > 0:
        for v in k.verts:
            if 0.05 * height < v.co.z < 0.75 * height:
                a = math.atan2(v.co.y, v.co.x)
                s = 1.0 + 0.05 * math.cos(a * ridges)
                v.co.x *= s
                v.co.y *= s
    return k


def handwheel(radius, rim_r, spokes=3, hub_r=None, seg_major=48):
    bm = bmesh.new()
    lib.bm_append(bm, torus(radius, rim_r, seg_major=seg_major, seg_minor=14))
    hub_r = hub_r or rim_r * 2.2
    lib.bm_append(bm, cyl(hub_r, rim_r * 2.2, segments=24, center=(0, 0, -rim_r * 1.1), bevel=rim_r * 0.3))
    for i in range(spokes):
        a = TWO_PI * i / spokes
        sp = cyl(rim_r * 0.8, radius - rim_r * 0.5, segments=12)
        m = Matrix.Rotation(a, 4, "Z") @ Matrix.Rotation(math.radians(90), 4, "Y")
        lib.bm_append(bm, sp, m)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


# ---------------------------------------------------------------------------
# Collision proxies + export
# ---------------------------------------------------------------------------
class ColBox:
    """Axis-aligned collision box in prop-local metres (Blender axes: +Z up)."""

    def __init__(self, size, center):
        self.size = size
        self.center = center


def col_objects(name, boxes):
    """Turn ColBox list into tiny cube mesh objects named COL_<i> (Unity converts them to BoxColliders)."""
    objs = []
    for i, b in enumerate(boxes):
        bm = lib.bm_box(b.size, b.center)
        obj = lib.object_from_bmesh(f"COL_{i}", bm)
        lib.apply_transforms(obj)
        objs.append(obj)
    return objs


def add_weighted_normals(obj, keep_sharp=True):
    """Weighted Normal modifier: big flat faces dominate the normal across small bevels (clean hard-surface shading)."""
    mod = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL")
    mod.keep_sharp = keep_sharp
    mod.weight = 60
    mod.mode = "FACE_AREA_WITH_ANGLE"
    return mod


def bounds(bm):
    xs = [v.co.x for v in bm.verts]
    ys = [v.co.y for v in bm.verts]
    zs = [v.co.z for v in bm.verts]
    return (min(xs), max(xs)), (min(ys), max(ys)), (min(zs), max(zs))

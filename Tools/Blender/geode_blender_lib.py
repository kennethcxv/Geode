"""
Shared helpers for Geode Empire headless Blender (bpy) asset generators.

Every generator under Tools/Blender/ imports this module.  All helpers are
deterministic (no wall-clock, no session state) and safe to run in
`--background` mode.

Conventions enforced here:
  * 1 Blender unit = 1 metre.
  * Object origin at the base (min Z) unless a generator asks otherwise.
  * Transforms applied before export (identity loc/rot/scale).
  * FBX export with -Z forward / Y up, unit scale applied, FBX_SCALE_ALL.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(TOOLS_DIR, "..", ".."))
UNITY_ASSETS = os.path.join(REPO_ROOT, "Geode", "Assets", "GeodeEmpire")
OUTPUT_DIR = os.path.join(TOOLS_DIR, "Output")


def log(tag, msg):
    print(f"[{tag}] {msg}", flush=True)


def fail(tag, msg):
    print(f"[{tag}] FAIL: {msg}", flush=True)
    sys.exit(1)


def ensure_dir(path):
    os.makedirs(path, exist_ok=True)
    return path


# ---------------------------------------------------------------------------
# Scene
# ---------------------------------------------------------------------------
def reset_scene():
    bpy.ops.wm.read_homefile(use_empty=True)
    for coll in (bpy.data.objects, bpy.data.meshes, bpy.data.materials,
                 bpy.data.images, bpy.data.cameras, bpy.data.lights):
        for block in list(coll):
            coll.remove(block)


def object_from_bmesh(name, bm, smooth=False):
    """Turn a bmesh into a linked scene object. Frees the bmesh."""
    for f in bm.faces:
        f.smooth = smooth
    mesh = bpy.data.meshes.new(name + "_Mesh")
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


# ---------------------------------------------------------------------------
# bmesh construction helpers
# ---------------------------------------------------------------------------
def bm_from_pydata(verts, faces):
    bm = bmesh.new()
    vs = [bm.verts.new(Vector(v)) for v in verts]
    bm.verts.ensure_lookup_table()
    for f in faces:
        try:
            bm.faces.new([vs[i] for i in f])
        except ValueError:
            pass  # duplicate face
    bm.normal_update()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def bm_convex_hull(points):
    bm = bmesh.new()
    for p in points:
        bm.verts.new(Vector(p))
    bmesh.ops.convex_hull(bm, input=list(bm.verts))
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context="VERTS")
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    return bm


def bm_box(size, center=(0, 0, 0)):
    sx, sy, sz = (size, size, size) if isinstance(size, (int, float)) else size
    cx, cy, cz = center
    hx, hy, hz = sx / 2, sy / 2, sz / 2
    verts = [
        (cx - hx, cy - hy, cz - hz), (cx + hx, cy - hy, cz - hz),
        (cx + hx, cy + hy, cz - hz), (cx - hx, cy + hy, cz - hz),
        (cx - hx, cy - hy, cz + hz), (cx + hx, cy - hy, cz + hz),
        (cx + hx, cy + hy, cz + hz), (cx - hx, cy + hy, cz + hz),
    ]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5),
             (2, 3, 7, 6), (3, 0, 4, 7)]
    return bm_from_pydata(verts, faces)


def bm_cylinder(radius, height, segments=16, center=(0, 0, 0), cap=True, radius_top=None):
    if radius_top is None:
        radius_top = radius
    cx, cy, cz = center
    verts = []
    for i in range(segments):
        a = 2 * math.pi * i / segments
        verts.append((cx + radius * math.cos(a), cy + radius * math.sin(a), cz))
    for i in range(segments):
        a = 2 * math.pi * i / segments
        verts.append((cx + radius_top * math.cos(a), cy + radius_top * math.sin(a), cz + height))
    faces = []
    for i in range(segments):
        j = (i + 1) % segments
        faces.append((i, j, segments + j, segments + i))
    if cap:
        faces.append(tuple(reversed(range(segments))))
        faces.append(tuple(range(segments, 2 * segments)))
    return bm_from_pydata(verts, faces)


def bm_icosphere(radius, subdivisions=2, center=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=radius)
    bmesh.ops.translate(bm, verts=list(bm.verts), vec=Vector(center))
    bm.normal_update()
    return bm


def bm_append(dst, src, matrix=None):
    """Append all geometry of `src` (a bmesh) into `dst`, transformed by matrix. Frees src."""
    tmp = bpy.data.meshes.new("_tmp_append")
    src.to_mesh(tmp)
    src.free()
    n0 = len(dst.verts)
    dst.from_mesh(tmp)
    bpy.data.meshes.remove(tmp)
    dst.verts.ensure_lookup_table()
    new_verts = [dst.verts[i] for i in range(n0, len(dst.verts))]
    if matrix is not None:
        bmesh.ops.transform(dst, matrix=matrix, verts=new_verts)
    dst.normal_update()
    return new_verts


def bm_transform(bm, matrix):
    bmesh.ops.transform(bm, matrix=matrix, verts=list(bm.verts))
    bm.normal_update()
    return bm


def bm_bevel(bm, width, segments=1, angle_limit_deg=28.0):
    """Bevel only edges sharper than the angle limit (keeps flat/coplanar areas clean)."""
    limit = math.radians(angle_limit_deg)
    edges = []
    for e in bm.edges:
        if not e.is_manifold:
            continue
        try:
            if e.calc_face_angle(0.0) > limit:
                edges.append(e)
        except ValueError:
            continue
    if edges:
        bmesh.ops.bevel(bm, geom=edges, offset=width, offset_type="OFFSET",
                        segments=segments, profile=0.5, affect="EDGES",
                        clamp_overlap=True, loop_slide=True)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    return bm


def bm_box_uv(bm, scale=1.0):
    """Cheap tri-planar box projection UVs, fine for procedural/tiling materials."""
    uv_layer = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for loop in f.loops:
            co = loop.vert.co
            if az >= ax and az >= ay:
                u, v = co.x, co.y
            elif ax >= ay:
                u, v = co.y, co.z
            else:
                u, v = co.x, co.z
            loop[uv_layer].uv = (u * scale, v * scale)
    return bm


def bm_origin_to_base(bm, center_xy=True):
    """Translate geometry so min Z == 0 and (optionally) the XY bbox centre is at the origin."""
    xs = [v.co.x for v in bm.verts]
    ys = [v.co.y for v in bm.verts]
    zs = [v.co.z for v in bm.verts]
    dx = -(min(xs) + max(xs)) / 2 if center_xy else 0.0
    dy = -(min(ys) + max(ys)) / 2 if center_xy else 0.0
    dz = -min(zs)
    bmesh.ops.translate(bm, verts=list(bm.verts), vec=Vector((dx, dy, dz)))
    return bm


def bm_orient_face_down(bm, face):
    """Rotate geometry so `face`'s normal points to -Z (object rests on that face)."""
    n = face.normal.normalized()
    rot = n.rotation_difference(Vector((0, 0, -1)))
    bmesh.ops.transform(bm, matrix=rot.to_matrix().to_4x4(), verts=list(bm.verts))
    bm.normal_update()
    return bm


def bm_stats(bm):
    return f"{len(bm.verts)} verts / {len(bm.faces)} faces"


def validate_bmesh(tag, name, bm, require_manifold=True):
    if len(bm.verts) == 0 or len(bm.faces) == 0:
        fail(tag, f"{name}: empty mesh")
    if require_manifold:
        bad = [e for e in bm.edges if not e.is_manifold]
        if bad:
            fail(tag, f"{name}: {len(bad)} non-manifold edges")
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        fail(tag, f"{name}: {len(loose)} loose verts")


# ---------------------------------------------------------------------------
# Export
# ---------------------------------------------------------------------------
def apply_transforms(obj):
    obj.matrix_world = Matrix.Identity(4)


def export_fbx(objs, path, tag="export"):
    ensure_dir(os.path.dirname(path))
    if os.path.exists(path):
        os.remove(path)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    common = dict(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    try:
        result = bpy.ops.export_scene.fbx(**common)
        exporter = "export_scene.fbx"
    except AttributeError:
        result = bpy.ops.wm.fbx_export(filepath=path, selected_objects_only=True)
        exporter = "wm.fbx_export"
    if "FINISHED" not in result:
        fail(tag, f"FBX export returned {result} for {path}")
    if not os.path.isfile(path) or os.path.getsize(path) < 64:
        fail(tag, f"FBX missing/empty: {path}")
    with open(path, "rb") as fh:
        if not fh.read(18).startswith(b"Kaydara FBX Binary"):
            fail(tag, f"FBX header invalid: {path}")
    log(tag, f"exported {os.path.relpath(path, REPO_ROOT)} via {exporter} ({os.path.getsize(path)} bytes)")

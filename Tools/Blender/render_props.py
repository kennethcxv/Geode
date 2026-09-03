"""
Prop review renders (headless Workbench): imports every FBX in a folder and renders two 3/4 views of each with
studio lighting, cavity shading and per-slot colours, so geometry can be judged before it reaches Unity.

    ./Tools/blender.sh --background --python Tools/Blender/render_props.py -- in <fbx folder> out <png folder> [only a,b] [size 640]
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import geode_blender_lib as lib  # noqa: E402

SLOT_COLOURS = [(0.62, 0.6, 0.56, 1), (0.25, 0.45, 0.8, 1), (0.85, 0.5, 0.2, 1), (0.3, 0.65, 0.35, 1), (0.8, 0.25, 0.25, 1), (0.6, 0.3, 0.7, 1)]


def setup_scene(size):
    lib.reset_scene()
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x = size
    sc.render.resolution_y = int(size * 0.7)
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "MATERIAL"
    sh.show_cavity = True
    sh.cavity_type = "BOTH"
    sh.curvature_ridge_factor = 1.0
    sh.curvature_valley_factor = 1.0
    sh.show_shadows = True
    sh.show_object_outline = False
    sc.display.render_aa = "8"
    sc.world = bpy.data.worlds.new("W")
    sc.world.color = (0.18, 0.18, 0.2)
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 45
    cam = bpy.data.objects.new("Cam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def frame(cam, objs, azimuth_deg, elevation_deg, pad=1.25):
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector((min(lo.x, w.x), min(lo.y, w.y), min(lo.z, w.z)))
            hi = Vector((max(hi.x, w.x), max(hi.y, w.y), max(hi.z, w.z)))
    centre = (lo + hi) / 2
    radius = max((hi - lo).length / 2, 0.02)
    fov = cam.data.angle
    dist = radius * pad / math.sin(fov / 2)
    a, e = math.radians(azimuth_deg), math.radians(elevation_deg)
    d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = centre + d * dist
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam.data.clip_start = dist * 0.05
    cam.data.clip_end = dist * 4


def render_folder(in_dir, out_dir, only=None, size=640):
    lib.ensure_dir(out_dir)
    files = sorted(f for f in os.listdir(in_dir) if f.lower().endswith(".fbx"))
    for f in files:
        name = f[:-4]
        if only and name not in only:
            continue
        cam = setup_scene(size)
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(in_dir, f))
        objs = [o for o in bpy.data.objects if o not in before and o.type == "MESH" and not o.name.startswith("COL_")]
        for o in [o for o in bpy.data.objects if o.name.startswith("COL_")]:
            o.hide_render = True
        for o in objs:
            for i, slot in enumerate(o.material_slots):
                if slot.material is None:
                    slot.material = bpy.data.materials.new(f"slot{i}")
                slot.material.diffuse_color = SLOT_COLOURS[i % len(SLOT_COLOURS)]
                slot.material.roughness = 0.5
        if not objs:
            lib.log("render", f"{name}: no mesh")
            continue
        for k, (az, el) in enumerate(((-38, 24), (142, 18))):
            frame(cam, objs, az, el)
            bpy.context.scene.render.filepath = os.path.join(out_dir, f"{name}_{k}.png")
            bpy.ops.render.render(write_still=True)
        lib.log("render", f"{name}: {sum(len(o.data.polygons) for o in objs)} polys rendered")


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    kv = dict(zip(args[0::2], args[1::2]))
    in_dir = kv.get("in", os.path.join(lib.UNITY_ASSETS, "Models", "Props"))
    out_dir = kv.get("out", os.path.join(lib.OUTPUT_DIR, "renders"))
    only = set(kv["only"].split(",")) if "only" in kv else None
    render_folder(in_dir, out_dir, only, int(kv.get("size", "640")))


if __name__ == "__main__":
    main()

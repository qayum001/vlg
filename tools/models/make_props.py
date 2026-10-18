"""Low-poly skill and item props for vlg, built procedurally and exported as GLB (no external assets).

Run headless (does not touch an open Blender session):
  "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Blender\\blender.exe" --background --factory-startup --python tools/models/make_props.py
Writes game/assets/models/<name>.glb. Units: meters, origin at the base center unless noted, front faces -Y in
Blender (= +Z in Godot). Sizes match the placeholder primitives they replace (SkillVisuals scales some at runtime).
"""
import math
import os

import bpy

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "game", "assets", "models")


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, color, metallic=0.0, roughness=0.6, emission=None, strength=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = strength
    return m


def add(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)
    return obj


def cube(size, loc, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    if bevel > 0:
        m = o.modifiers.new("bevel", "BEVEL")
        m.width = bevel
        m.segments = 2
    return add(o, material)


def cyl(radius, depth, loc, material, verts=24, rot=(0, 0, 0), top=None):
    if top is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc, rotation=rot)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=top, depth=depth, location=loc, rotation=rot)
    return add(bpy.context.active_object, material)


def torus(major, minor, loc, material, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=24, minor_segments=8,
                                     location=loc, rotation=rot)
    return add(bpy.context.active_object, material)


def export(name):
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = name
    bpy.ops.object.shade_flat()
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, name + ".glb"), export_format="GLB", use_selection=True,
                              export_apply=True)
    print("wrote", name)


FRONT = (math.pi / 2, 0, 0)  # cylinder axis along -Y (front)


def speaker(name="speaker", w=0.9, h=1.3, d=0.8, cones=2):
    reset()
    body = mat("cabinet", (0.08, 0.05, 0.12), roughness=0.8)
    cone = mat("cone", (0.02, 0.02, 0.03), roughness=0.4)
    rim = mat("rim", (0.3, 0.95, 1.0), emission=(0.3, 0.95, 1.0), strength=3.0)
    trim = mat("trim", (0.6, 0.2, 1.0), emission=(0.6, 0.2, 1.0), strength=1.5)
    cube((w, d, h), (0, 0, h / 2), body, bevel=0.03)
    cube((w + 0.04, d + 0.04, 0.05), (0, 0, h - 0.02), trim)
    step = h / (cones + 0.6)
    for i in range(cones):
        z = h - step * (i + 0.8)
        r = min(w, step) * 0.38
        cyl(r * 1.08, 0.04, (0, -d / 2 - 0.01, z), rim, rot=FRONT)
        cyl(r, 0.06, (0, -d / 2 - 0.02, z), cone, rot=FRONT)
        cyl(r * 0.3, 0.08, (0, -d / 2 - 0.04, z), rim, verts=12, rot=FRONT)
    export(name)


def vinyl():
    reset()
    disc = mat("vinyl", (0.03, 0.02, 0.05), metallic=0.3, roughness=0.15)
    label = mat("label", (1.0, 0.3, 0.75), emission=(1.0, 0.2, 0.7), strength=2.0)
    hole = mat("hole", (0.0, 0.0, 0.0))
    cyl(1.0, 0.05, (0, 0, 0), disc, verts=32)
    for r in (0.8, 0.62):
        torus(r, 0.008, (0, 0, 0.026), mat("groove", (0.12, 0.1, 0.16), roughness=0.3))
    cyl(0.38, 0.07, (0, 0, 0), label, verts=24)
    cyl(0.05, 0.09, (0, 0, 0), hole, verts=10)
    export("vinyl")


def cowbell():
    reset()
    chrome = mat("chrome", (0.85, 0.85, 0.9), metallic=1.0, roughness=0.2)
    glow = mat("bellglow", (1.0, 0.85, 0.3), emission=(1.0, 0.75, 0.2), strength=1.5)
    bell = cyl(0.55, 1.0, (0, 0, 0.5), chrome, verts=4, top=0.3)
    bell.rotation_euler = (0, 0, math.pi / 4)
    cyl(0.56, 0.06, (0, 0, 0.03), glow, verts=4).rotation_euler = (0, 0, math.pi / 4)
    torus(0.16, 0.04, (0, 0, 1.12), chrome, rot=(math.pi / 2, 0, 0))
    export("cowbell")


def boombox():
    reset()
    body = mat("boombody", (0.12, 0.06, 0.18), roughness=0.5)
    cone = mat("boomcone", (0.02, 0.02, 0.03), roughness=0.4)
    pink = mat("boompink", (1.0, 0.3, 0.75), emission=(1.0, 0.25, 0.7), strength=2.5)
    chrome = mat("boomchrome", (0.8, 0.8, 0.85), metallic=1.0, roughness=0.25)
    cube((0.9, 0.3, 0.45), (0, 0, 0.225), body, bevel=0.02)
    for x in (-0.25, 0.25):
        cyl(0.15, 0.04, (x, -0.16, 0.22), pink, rot=FRONT)
        cyl(0.13, 0.06, (x, -0.17, 0.22), cone, rot=FRONT)
    cube((0.2, 0.05, 0.1), (0, -0.16, 0.3), pink)
    cube((0.6, 0.04, 0.04), (0, 0, 0.6), chrome)
    for x in (-0.3, 0.3):
        cube((0.04, 0.04, 0.16), (x, 0, 0.52), chrome)
    cyl(0.012, 0.5, (0.35, 0.05, 0.7), chrome, verts=6, rot=(0, 0.4, 0))
    export("boombox")


def toy_drone():
    reset()
    shell = mat("droneshell", (0.95, 0.95, 1.0), roughness=0.35)
    arm = mat("dronearm", (0.15, 0.15, 0.2), roughness=0.5)
    rotor = mat("dronerotor", (0.3, 0.95, 1.0), emission=(0.3, 0.95, 1.0), strength=1.5)
    eye = mat("droneeye", (1.0, 0.2, 0.3), emission=(1.0, 0.2, 0.3), strength=4.0)
    cube((0.3, 0.3, 0.12), (0, 0, 0.1), shell, bevel=0.03)
    cube((0.08, 0.04, 0.04), (0, -0.16, 0.1), eye)
    for sx, sy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        o = cube((0.42, 0.04, 0.03), (sx * 0.15, sy * 0.15, 0.12), arm)
        o.rotation_euler = (0, 0, math.atan2(sy, sx))
        cyl(0.13, 0.015, (sx * 0.3, sy * 0.3, 0.16), rotor, verts=16)
    export("toy_drone")


def shell():
    reset()
    metal = mat("shellmetal", (0.3, 0.33, 0.25), metallic=0.7, roughness=0.5)
    band = mat("shellband", (1.0, 0.5, 0.15), emission=(1.0, 0.4, 0.1), strength=1.5)
    cyl(0.12, 0.4, (0, 0, 0.3), metal, verts=12)
    cyl(0.12, 0.2, (0, 0, 0.6), metal, verts=12, top=0.0)
    cyl(0.125, 0.05, (0, 0, 0.42), band, verts=12)
    for i in range(4):
        a = i * math.pi / 2
        o = cube((0.02, 0.18, 0.16), (math.cos(a) * 0.12, math.sin(a) * 0.12, 0.08), metal)
        o.rotation_euler = (0, 0, a + math.pi / 2)
    export("mortar_shell")


if __name__ == "__main__":
    speaker()
    speaker("subwoofer", w=1.6, h=1.6, d=1.3, cones=1)
    vinyl()
    cowbell()
    boombox()
    toy_drone()
    shell()

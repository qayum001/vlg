# bpy layer: objects, materials, normals, collision helpers. All objects keep identity transforms
# (geometry is written in world space), so BVH trees from evaluated objects are in world coords.
import bmesh
import bpy
from math import pi
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

from . import geom


def srgb(c):
    f = lambda v: v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    return (f(c[0]), f(c[1]), f(c[2]), 1.0)


def principled(m):
    # Look up by type: node names are localized.
    return next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')


def _sock(node, ident, out=False):
    # ShaderNodeMix has duplicate socket names per data type; identifiers are unique.
    return next(s for s in (node.outputs if out else node.inputs) if s.identifier == ident)


class Builder:
    """Creates datablocks in one collection. Every datablock name starts with `prefix` so clear() finds them."""

    def __init__(self, coll_name, prefix):
        self.coll_name = coll_name
        self.prefix = prefix
        self.mats = {}

    @property
    def coll(self):
        c = bpy.data.collections.get(self.coll_name)
        if c is None:
            c = bpy.data.collections.new(self.coll_name)
            bpy.context.scene.collection.children.link(c)
        return c

    def clear(self):
        c = bpy.data.collections.get(self.coll_name)
        if c:
            for o in list(c.objects):
                bpy.data.objects.remove(o, do_unlink=True)
        for blocks in (bpy.data.meshes, bpy.data.materials, bpy.data.lights, bpy.data.armatures):
            for d in list(blocks):
                if d.name.startswith(self.prefix) and d.users == 0:
                    blocks.remove(d)
        self.mats = {}

    # ---------------------------------------------------------------- materials
    def mat(self, key, color, rough=0.6, metal=0.0, emit=None, strength=0.0):
        m = bpy.data.materials.new(self.prefix + key)
        m.use_nodes = True
        p = principled(m)
        p.inputs['Base Color'].default_value = srgb(color)
        p.inputs['Roughness'].default_value = rough
        p.inputs['Metallic'].default_value = metal
        if emit:
            p.inputs['Emission Color'].default_value = srgb(emit)
            p.inputs['Emission Strength'].default_value = strength
        m.diffuse_color = srgb(color)
        self.mats[key] = m
        return m

    def ramp_mat(self, key, stops, uv_axis=None, rough=0.4, highlight=None, viewport=(0.9, 0.9, 0.9)):
        # Flat-color bands from UV: uv_axis None -> distance from (0.5, 0.5); 'v' -> UV.y.
        # highlight=(u, v, r) adds a white dot. Constant interpolation keeps edges crisp.
        m = bpy.data.materials.new(self.prefix + key)
        m.use_nodes = True
        nt = m.node_tree
        p = principled(m)
        p.inputs['Roughness'].default_value = rough
        tc = nt.nodes.new('ShaderNodeTexCoord')
        if uv_axis == 'v':
            sep = nt.nodes.new('ShaderNodeSeparateXYZ')
            nt.links.new(tc.outputs['UV'], sep.inputs[0])
            fac = sep.outputs['Y']
        else:
            d = nt.nodes.new('ShaderNodeVectorMath')
            d.operation = 'DISTANCE'
            nt.links.new(tc.outputs['UV'], d.inputs[0])
            d.inputs[1].default_value = (0.5, 0.5, 0.0)
            fac = d.outputs['Value']
        ramp = nt.nodes.new('ShaderNodeValToRGB')
        ramp.color_ramp.interpolation = 'CONSTANT'
        nt.links.new(fac, ramp.inputs['Fac'])
        els = ramp.color_ramp.elements
        els[0].position = stops[0][0]
        els[0].color = srgb(stops[0][1])
        els[1].position = stops[-1][0]
        els[1].color = srgb(stops[-1][1])
        for pos, col in stops[1:-1]:
            e = els.new(pos)
            e.color = srgb(col)
        color_out = ramp.outputs['Color']
        if highlight:
            hx, hy, hr = highlight
            d2 = nt.nodes.new('ShaderNodeVectorMath')
            d2.operation = 'DISTANCE'
            nt.links.new(tc.outputs['UV'], d2.inputs[0])
            d2.inputs[1].default_value = (hx, hy, 0.0)
            lt = nt.nodes.new('ShaderNodeMath')
            lt.operation = 'LESS_THAN'
            lt.inputs[1].default_value = hr
            nt.links.new(d2.outputs['Value'], lt.inputs[0])
            mix = nt.nodes.new('ShaderNodeMix')
            mix.data_type = 'RGBA'
            nt.links.new(lt.outputs['Value'], _sock(mix, 'Factor_Float'))
            nt.links.new(color_out, _sock(mix, 'A_Color'))
            _sock(mix, 'B_Color').default_value = (1, 1, 1, 1)
            color_out = _sock(mix, 'Result_Color', True)
        nt.links.new(color_out, p.inputs['Base Color'])
        m.diffuse_color = srgb(viewport)
        self.mats[key] = m
        return m

    # ---------------------------------------------------------------- objects
    def mesh(self, name, verts, faces, mats, mat_idx=None, uvs=None, flat=True):
        me = bpy.data.meshes.new(self.prefix + name)
        me.from_pydata([tuple(v) for v in verts], [], [tuple(f) for f in faces])
        for m in mats:
            me.materials.append(m)
        if mat_idx is not None:
            me.polygons.foreach_set('material_index', mat_idx)
        if uvs is not None:
            lay = me.uv_layers.new(name="UV")
            for lp in me.loops:
                lay.data[lp.index].uv = uvs[lp.vertex_index]
        if flat:
            me.shade_flat()
        else:
            me.shade_smooth()
        me.update()
        ob = bpy.data.objects.new(name, me)
        self.coll.objects.link(ob)
        return ob

    def tube(self, name, path, radii, mat, n=6, normals=None, caps=('round', 'round'), sq=2.0, rot=0.0):
        v, f = geom.sweep(path, radii, n=n, normals=normals, caps=caps, sq=sq, rot=rot)
        ob = self.mesh(name, v, f, [mat])
        orient_path(ob, path)
        return ob

    def loop(self, name, path, rw, rh, mat, n=4, hint=None):
        v, f = geom.sweep_loop(path, rw, rh, n=n, hint=hint)
        ob = self.mesh(name, v, f, [mat])
        orient_path(ob, path)
        return ob

    def light(self, name, kind, energy, color, loc, size=None, aim=None):
        ld = bpy.data.lights.new(self.prefix + name, kind)
        ld.energy = energy
        ld.color = color
        if size is not None:
            if kind == 'AREA':
                ld.size = size
            else:
                ld.shadow_soft_size = size
        o = bpy.data.objects.new(name, ld)
        o.location = loc
        if aim is not None:
            o.rotation_euler = (Vector(aim) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
        self.coll.objects.link(o)
        return o


# ---------------------------------------------------------------- normals
def flip(ob):
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()


def orient(ob, away):
    # away(center) -> vector pointing outward at that face; flips all faces if most area points inward.
    s = 0.0
    for p in ob.data.polygons:
        s += p.normal.dot(away(p.center)) * p.area
    if s < 0:
        flip(ob)


def orient_axis(ob, axis, c):
    # Outward = away from an axis line through c ('z' or 'x'), or from point c (any other axis value).
    if axis == 'z':
        orient(ob, lambda q: Vector((q.x - c[0], q.y - c[1], 0.0)))
    elif axis == 'x':
        orient(ob, lambda q: Vector((0.0, q.y - c[1], q.z - c[2])))
    else:
        orient(ob, lambda q: q - Vector(c))


def orient_path(ob, path):
    # Outward = away from the nearest centerline sample.
    kd = KDTree(len(path))
    for i, p in enumerate(path):
        kd.insert(p, i)
    kd.balance()
    orient(ob, lambda q: q - kd.find(q)[0])


def face_dir(ob, d):
    orient(ob, lambda q: d)


# ---------------------------------------------------------------- collision
def trees(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    return [BVHTree.FromObject(o, dg) for o in objs]


def push_out(p, tl, margin):
    # Moves p along the nearest face normal until it is `margin` outside every tree. Needs outward normals.
    for tr in tl:
        loc, nor, idx, dist = tr.find_nearest(p)
        if loc is None:
            continue
        d = (p - loc).dot(nor)
        if d < margin:
            p = p + nor * (margin - d)
    return p


def ray_front(tree, x, z, off):
    # Hit point seen from the front (-Y looking +Y), lifted `off` along the face normal. None on miss.
    loc, nor, idx, dist = tree.ray_cast(Vector((x, -10.0, z)), Vector((0, 1, 0)))
    if loc is None:
        return None
    return loc + nor * off


def back_y(tl, x, z):
    # Largest y hit from behind (+Y looking -Y) over all trees, or None.
    best = None
    for tr in tl:
        loc, nor, idx, dist = tr.ray_cast(Vector((x, 10.0, z)), Vector((0, -1, 0)))
        if loc is not None and (best is None or loc.y > best):
            best = loc.y
    return best


def tri_count(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    n = 0
    for o in objs:
        if o.type != 'MESH':
            continue
        e = o.evaluated_get(dg)
        m = e.to_mesh()
        m.calc_loop_triangles()
        n += len(m.loop_triangles)
        e.to_mesh_clear()
    return n

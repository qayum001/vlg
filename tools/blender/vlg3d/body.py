# Rig-ready low-poly humanoid base: skin-modifier skeleton -> one connected quad mesh with a ring at every
# joint, plus a separate low-res head. T-pose, faces -Y, feet at z=0. Joint positions are stored on the body
# object (custom prop "vlg_joints") for rigging later.
import json
import math
from dataclasses import dataclass, field

import bpy
import bmesh
from mathutils import Matrix, Vector

from . import geom


@dataclass
class BodyParams:
    height: float = 1.65         # sole to crown
    heads: float = 7.0           # height / head height
    shoulder_half: float = 0.165  # x of shoulder joints
    hip_half: float = 0.095      # x of hip joints; keep hip/thigh rx < hip_half so legs don't cross x=0
    female: bool = True
    fist: tuple = ()             # sides ('L'/'R') with a short, round fist instead of a flat hand
    # skin radii per joint, measured half-widths after subsurf. Vertical chains: (x, y); arms: (y, z).
    radii: dict = field(default_factory=lambda: {
        'pelvis': (0.150, 0.110), 'waist': (0.100, 0.076), 'chest': (0.145, 0.100), 'neck_base': (0.090, 0.070),
        'neck': (0.050, 0.052),
        'shoulder': (0.060, 0.058), 'upper_arm': (0.050, 0.048), 'elbow': (0.042, 0.042), 'forearm': (0.044, 0.040),
        'wrist': (0.030, 0.022), 'hand': (0.042, 0.014),
        'hip': (0.088, 0.090), 'thigh': (0.078, 0.085), 'knee': (0.056, 0.058), 'calf': (0.058, 0.062),
        'ankle': (0.040, 0.042), 'toe': (0.036, 0.024),
    })

    @property
    def head_h(self):
        return self.height / self.heads


# Catmull-Clark level 1 pulls a skin hull inward; radii are scaled up so final widths ~ the given radii.
RADIUS_GAIN = 1.1


def skeleton(p):
    # -> (names, coords, radii, edges). Proportions in head units (classic ~7-head figure).
    H, h = p.height, p.head_h
    sh_z = H - 1.49 * h
    chest_z = H - 2.05 * h
    waist_z = H - 2.75 * h
    hip_z = H - 3.55 * h
    knee_z = 0.285 * H
    ankle_z = 0.065
    upper_arm, fore_arm, hand = 1.25 * h, 1.05 * h, 0.72 * h
    r = p.radii
    j = {}
    j['pelvis'] = ((0.0, 0.0, hip_z + 0.04), r['pelvis'])
    j['waist'] = ((0.0, 0.0, waist_z), r['waist'])
    j['chest'] = ((0.0, -0.005, chest_z), r['chest'])
    j['neck_base'] = ((0.0, 0.005, sh_z + 0.035), r['neck_base'])
    j['neck'] = ((0.0, 0.012, H - 0.92 * h), r['neck'])
    for side, s in (('L', 1.0), ('R', -1.0)):
        x0 = s * p.shoulder_half
        j['shoulder.' + side] = ((x0, 0.0, sh_z), r['shoulder'])
        j['upper_arm.' + side] = ((x0 + s * 0.5 * upper_arm, 0.0, sh_z), r['upper_arm'])
        j['elbow.' + side] = ((x0 + s * upper_arm, 0.0, sh_z), r['elbow'])
        j['forearm.' + side] = ((x0 + s * (upper_arm + 0.4 * fore_arm), 0.0, sh_z), r['forearm'])
        j['wrist.' + side] = ((x0 + s * (upper_arm + fore_arm), 0.0, sh_z), r['wrist'])
        if side in p.fist:
            j['hand.' + side] = ((x0 + s * (upper_arm + fore_arm + 0.45 * hand), 0.0, sh_z), (0.030, 0.032))
        else:
            j['hand.' + side] = ((x0 + s * (upper_arm + fore_arm + hand), 0.0, sh_z - 0.01), r['hand'])
        hx = s * p.hip_half
        kx = s * (p.hip_half - 0.005)
        j['hip.' + side] = ((hx, 0.0, hip_z), r['hip'])
        j['thigh.' + side] = ((geom.lerp(hx, kx, 0.35), -0.004, geom.lerp(hip_z, knee_z, 0.35)), r['thigh'])
        j['knee.' + side] = ((kx, -0.01, knee_z), r['knee'])
        j['calf.' + side] = ((kx, 0.008, geom.lerp(knee_z, ankle_z, 0.3)), r['calf'])
        j['ankle.' + side] = ((kx, 0.01, ankle_z), r['ankle'])
        j['toe.' + side] = ((s * (p.hip_half - 0.002), -0.13, 0.03), r['toe'])
    edges = [('pelvis', 'waist'), ('waist', 'chest'), ('chest', 'neck_base'), ('neck_base', 'neck')]
    for side in ('L', 'R'):
        chain = [('neck_base', 'shoulder.'), ('shoulder.', 'upper_arm.'), ('upper_arm.', 'elbow.'),
                 ('elbow.', 'forearm.'), ('forearm.', 'wrist.'), ('wrist.', 'hand.'),
                 ('pelvis', 'hip.'), ('hip.', 'thigh.'), ('thigh.', 'knee.'), ('knee.', 'calf.'),
                 ('calf.', 'ankle.'), ('ankle.', 'toe.')]
        for a, b in chain:
            edges.append((a + side if a.endswith('.') else a, b + side))
    names = list(j)
    return names, [Vector(j[n][0]) for n in names], [j[n][1] for n in names], edges


def build_body(builder, p, mat, name="Body"):
    names, coords, radii, edges = skeleton(p)
    idx = {n: i for i, n in enumerate(names)}
    me = bpy.data.meshes.new(builder.prefix + name + "_skel")
    me.from_pydata([tuple(c) for c in coords], [(idx[a], idx[b]) for a, b in edges], [])
    skel = bpy.data.objects.new(name + "_skel", me)
    builder.coll.objects.link(skel)
    sk = skel.modifiers.new("Skin", 'SKIN')
    sk.use_smooth_shade = False
    if not me.skin_vertices:
        me.skin_vertices.new()
    for i, sv in enumerate(me.skin_vertices[0].data):
        sv.radius = (radii[i][0] * RADIUS_GAIN, radii[i][1] * RADIUS_GAIN)
        sv.use_root = names[i] == 'pelvis'
    ss = skel.modifiers.new("Subsurf", 'SUBSURF')
    ss.levels = 1
    ss.render_levels = 1
    dg = bpy.context.evaluated_depsgraph_get()
    dg.update()
    body_me = bpy.data.meshes.new_from_object(skel.evaluated_get(dg))
    body_me.name = builder.prefix + name
    bpy.data.objects.remove(skel, do_unlink=True)
    bpy.data.meshes.remove(me)
    # Skin hulls at branch joints come out asymmetric (cm-level) and sometimes inverted:
    # mirror the +X half onto -X, then fix normals.
    bm = bmesh.new()
    bm.from_mesh(body_me)
    if not p.fist:  # a one-sided fist is asymmetric by design; mirroring would overwrite it
        bmesh.ops.symmetrize(bm, input=bm.verts[:] + bm.edges[:] + bm.faces[:], direction='X', dist=1e-4)
        seam = [v for v in bm.verts if abs(v.co.x) < 1e-5]
        bmesh.ops.remove_doubles(bm, verts=seam, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(body_me)
    bm.free()
    body_me.materials.clear()
    body_me.materials.append(mat)
    body_me.shade_flat()
    ob = bpy.data.objects.new(name, body_me)
    builder.coll.objects.link(ob)
    ob["vlg_joints"] = json.dumps({n: list(coords[i]) for i, n in enumerate(names)})
    return ob


def joints(ob):
    return {k: Vector(v) for k, v in json.loads(ob["vlg_joints"]).items()}


# ---------------------------------------------------------------- head
def head_dims(p):
    # (center, a, b_front, b_back, c): ellipsoid half-axes; lower half is compressed by 0.92, so
    # crown-to-chin = 1.92 c = head height.
    h = p.head_h
    c = h / 1.92
    return Vector((0.0, -0.005, p.height - c)), 0.3277 * h, 0.404 * h, 0.4255 * h, c


def head_shape(p, u, v, w, d=0.0):
    # Unit-sphere direction (u, v, w) -> world point; d offsets the surface (hair shells).
    hc, a, bf, bb, c = head_dims(p)
    a += d
    b = (bb if v > 0 else bf) + d
    c += d
    x, y, z = a * u, b * v, c * w
    if w < 0.0:
        z *= 0.92
    if w < 0.15:
        t = geom.clamp((0.15 - w) / 1.15)
        x *= 1 - 0.26 * t ** 1.3
    if v > 0 and w < -0.1:
        t = geom.clamp((-0.1 - w) / 0.9)
        y *= 1 - 0.40 * t ** 1.2
    if w < 0.0:
        y -= 0.06 * p.head_h * geom.smoothstep(0.0, -1.0, w)
    if v < 0:
        y *= 1 - 0.10 * (-v) ** 3 * geom.smoothstep(0.6, -0.2, w)
    return hc + Vector((x, y, z))


def head_point(p, phi_deg, w, d=0.0):
    # phi: angle around Z (-90 = front/-Y, 90 = back, 0 = +X); w in [-1, 1] bottom..top.
    ph = math.radians(phi_deg)
    r = math.sqrt(max(0.0, 1 - w * w))
    return head_shape(p, r * math.cos(ph), r * math.sin(ph), w, d)


def build_head(builder, p, mat, name="Head", segs=(20, 14)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs[0], v_segments=segs[1], radius=1.0)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi / segs[0], 3, 'Z'))
    for v in bm.verts:
        v.co = head_shape(p, v.co.x, v.co.y, v.co.z)
    me = bpy.data.meshes.new(builder.prefix + name)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(mat)
    me.shade_flat()
    ob = bpy.data.objects.new(name, me)
    builder.coll.objects.link(ob)
    return ob

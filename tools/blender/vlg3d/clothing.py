# Clothing from the base body: copy -> inflate along normals -> cut with planes -> grow new rings from cut loops.
# The cloth reuses body topology (rig-friendly). Work happens on a bmesh; to_object() makes the final object.
import math

import bmesh
import bpy
from mathutils import Vector

from .scene import orient


def shell(body_ob, offset, keep_planes):
    # keep_planes: [(co, no)]; keeps the half-space (p - co) . no <= 0 of each plane. Returns a bmesh.
    bm = bmesh.new()
    bm.from_mesh(body_ob.data)
    bm.normal_update()
    moved = [(v, v.co + v.normal * offset) for v in bm.verts]
    for v, co in moved:
        v.co = co
    for co, no in keep_planes:
        co, no = Vector(co), Vector(no).normalized()
        geom_all = bm.verts[:] + bm.edges[:] + bm.faces[:]
        bmesh.ops.bisect_plane(bm, geom=geom_all, dist=1e-6, plane_co=co, plane_no=no, clear_outer=True)
    _drop_loose(bm)
    return bm


def _drop_loose(bm):
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context='VERTS')


def boundary_loops(bm):
    # Ordered vertex loops along boundary edges (edges with one face).
    bm.edges.ensure_lookup_table()
    bedges = {e for e in bm.edges if len(e.link_faces) == 1}
    loops = []
    while bedges:
        e = bedges.pop()
        start, cur = e.verts[0], e.verts[1]
        loop = [start]
        while cur is not start:
            loop.append(cur)
            nxt = next((f for f in cur.link_edges if f in bedges), None)
            if nxt is None:
                raise ValueError("open boundary chain")
            bedges.discard(nxt)
            cur = nxt.other_vert(cur)
        loops.append(loop)
    return loops


def pick_loop(loops, pred):
    # The single loop whose centroid satisfies pred(centroid).
    hits = [lp for lp in loops if pred(sum((v.co for v in lp), Vector()) / len(lp))]
    if len(hits) != 1:
        raise ValueError("expected one matching loop, got %d" % len(hits))
    return hits[0]


def orient_loop(loop, axis):
    # Rotate/reverse so the loop runs counter-clockwise around `axis` ('x' or 'z') starting at the lowest angle.
    c = sum((v.co for v in loop), Vector()) / len(loop)
    if axis == 'z':
        ang = lambda v: math.atan2(v.co.y - c.y, v.co.x - c.x)
    else:
        ang = lambda v: math.atan2(v.co.z - c.z, v.co.y - c.y)
    a = [ang(v) for v in loop]
    # signed area in the angle plane decides direction
    turn = sum(((a[(i + 1) % len(a)] - a[i] + math.pi) % (2 * math.pi)) - math.pi for i in range(len(a)))
    if turn < 0:
        loop = loop[::-1]
        a = a[::-1]
    i0 = min(range(len(a)), key=lambda i: a[i])
    return loop[i0:] + loop[:i0], c


def grow(bm, loop, rings):
    # Adds rings of new verts after `loop` (same count, same order) and bridges them with quads.
    # rings: list of lists of Vector. Returns the last ring's BMVerts.
    prev = loop
    n = len(loop)
    for pts in rings:
        if len(pts) != n:
            raise ValueError("ring size %d != loop size %d" % (len(pts), n))
        cur = [bm.verts.new(p) for p in pts]
        for j in range(n):
            j2 = (j + 1) % n
            bm.faces.new((prev[j], prev[j2], cur[j2], cur[j]))
        prev = cur
    return prev


def cap(bm, loop, center):
    pole = bm.verts.new(center)
    n = len(loop)
    for j in range(n):
        bm.faces.new((loop[j], loop[(j + 1) % n], pole))
    return pole


def away_from_bones(joints, bones):
    # -> f(point) = vector from the nearest bone segment to the point. bones: [(joint_a, joint_b)].
    segs = [(joints[a], joints[b]) for a, b in bones]

    def away(q):
        best, bp = None, None
        for a, b in segs:
            ab = b - a
            t = max(0.0, min(1.0, (q - a).dot(ab) / max(ab.length_squared, 1e-12)))
            p = a + ab * t
            d = (q - p).length_squared
            if best is None or d < best:
                best, bp = d, p
        return q - bp
    return away


def to_object(builder, bm, name, mats, away):
    # Makes winding consistent, then flips everything if most face area points toward the body (away(q) < 0).
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(builder.prefix + name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    me.shade_flat()
    ob = bpy.data.objects.new(name, me)
    builder.coll.objects.link(ob)
    orient(ob, away)
    return ob

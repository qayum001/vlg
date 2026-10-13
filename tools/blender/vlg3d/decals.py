# Face decals: flat meshes ray-cast onto a surface from the front (-Y), lifted by `off` to avoid z-fighting.
# Used for eyes, brows, lashes, lips on low-poly heads. Coordinates are world (x, z).
import math

from mathutils import Vector

from .scene import ray_front, face_dir


def _project(tree, x, z, off):
    p = ray_front(tree, x, z, off)
    if p is None:
        raise ValueError("decal point (%.4f, %.4f) misses the target surface" % (x, z))
    return p


def grid(builder, name, tree, cols, ends, uvf, mat, off):
    # cols: columns of (x, z) points, all the same length; ends: two corner points closed with triangle fans.
    # uvf(x, z) -> (u, v) per vertex.
    verts, uvs, faces, idx = [], [], [], {}
    for ci, col in enumerate(cols):
        for j, (x, z) in enumerate(col):
            idx[(ci, j)] = len(verts)
            verts.append(_project(tree, x, z, off))
            uvs.append(uvf(x, z))
    J = len(cols[0]) - 1
    e = []
    for (x, z) in ends:
        e.append(len(verts))
        verts.append(_project(tree, x, z, off))
        uvs.append(uvf(x, z))
    for ci in range(len(cols) - 1):
        for j in range(J):
            faces.append((idx[(ci, j)], idx[(ci + 1, j)], idx[(ci + 1, j + 1)], idx[(ci, j + 1)]))
    last = len(cols) - 1
    for j in range(J):
        faces.append((e[0], idx[(0, j)], idx[(0, j + 1)]))
        faces.append((e[1], idx[(last, j + 1)], idx[(last, j)]))
    ob = builder.mesh(name, verts, faces, [mat], uvs=uvs)
    face_dir(ob, Vector((0, -1, 0)))
    return ob


def strip(builder, name, tree, pts2, widths, mat, off):
    # Ribbon along a 2D (x, z) curve; widths[i] = (above, below) offsets along the curve's upward normal.
    verts, faces = [], []
    L = len(pts2)
    for i, (x, z) in enumerate(pts2):
        a = pts2[min(i + 1, L - 1)]
        b = pts2[max(i - 1, 0)]
        tx, tz = a[0] - b[0], a[1] - b[1]
        ln = math.hypot(tx, tz)
        tx, tz = tx / ln, tz / ln
        nx, nz = -tz, tx
        if nz < 0:
            nx, nz = -nx, -nz
        wu, wd = widths[i]
        verts.append(_project(tree, x + nx * wu, z + nz * wu, off))
        verts.append(_project(tree, x - nx * wd, z - nz * wd, off))
    for i in range(L - 1):
        faces.append((2 * i, 2 * i + 1, 2 * i + 3, 2 * i + 2))
    ob = builder.mesh(name, verts, faces, [mat])
    face_dir(ob, Vector((0, -1, 0)))
    return ob


def almond(cx, cz, half_w, up, lo, tilt, side, cols=8):
    # Almond outline as decal columns. side=+1: outer corner toward +X. up/lo/tilt: f(s), s in [-1, 1] (s=+1 outer).
    out = []
    for i in range(1, cols):
        s = -math.cos(math.pi * i / cols)
        out.append([(cx + side * s * half_w, cz + (up(s) if t > 0 else lo(s)) * t + tilt(s)) for t in (-1, 0, 1)])
    ends = [(cx - side * half_w, cz + tilt(-1)), (cx + side * half_w, cz + tilt(1))]
    return out, ends

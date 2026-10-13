# Pure geometry, no bpy. Builders return (verts, faces) lists for Builder.mesh().
import math
from math import sin, cos, pi, exp
from mathutils import Vector


def clamp(x, a=0.0, b=1.0):
    return max(a, min(b, x))


def lerp(a, b, t):
    return a + (b - a) * t


def smoothstep(e0, e1, x):
    t = clamp((x - e0) / (e1 - e0))
    return t * t * (3 - 2 * t)


def gauss(d2):
    return exp(-d2)


def spline(pts, seg=3):
    # Catmull-Rom through all points; ends extrapolated so the curve hits pts[0] and pts[-1].
    pts = [Vector(p) for p in pts]
    P = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for s in range(seg):
            t = s / seg
            t2 = t * t
            t3 = t2 * t
            out.append(0.5 * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                              + (3 * p1 - p0 - 3 * p2 + p3) * t3))
    out.append(pts[-1].copy())
    return out


def frames(path, normals=None):
    # Per-point (T, N, B). normals given: N = normal made perpendicular to T; else projection transport from Z up.
    L = len(path)
    T = [(path[min(i + 1, L - 1)] - path[max(i - 1, 0)]).normalized() for i in range(L)]
    N = []
    if normals:
        for i in range(L):
            n = normals[i] - T[i] * normals[i].dot(T[i])
            N.append(n.normalized())
    else:
        up = Vector((0, 0, 1)) if abs(T[0].z) < 0.9 else Vector((1, 0, 0))
        n = (up - T[0] * up.dot(T[0])).normalized()
        for i in range(L):
            n = (n - T[i] * n.dot(T[i])).normalized()
            N.append(n.copy())
    B = [T[i].cross(N[i]) for i in range(L)]
    return T, N, B


def ring_z(z, rx, df, db, n, cy=0.0, disp=None, rot=0.0):
    # Horizontal ring: half-ellipse depth df toward -Y (front), db toward +Y (back). disp(p, t, k) -> Vector.
    pts = []
    for k in range(n):
        t = 2 * pi * k / n + rot
        c, s = cos(t), sin(t)
        p = Vector((rx * c, cy + (db if s > 0 else df) * s, z))
        if disp:
            p = disp(p, t, k)
        pts.append(p)
    return pts


def loft(rings, closed=True):
    # Quads between consecutive rings of equal length. closed: ring is a loop.
    n = len(rings[0])
    verts = [v for r in rings for v in r]
    faces = []
    m = n if closed else n - 1
    for i in range(len(rings) - 1):
        a0, b0 = i * n, (i + 1) * n
        for j in range(m):
            j2 = (j + 1) % n
            faces.append((a0 + j, a0 + j2, b0 + j2, b0 + j))
    return verts, faces


def fan(verts, faces, ring_i, n, pole, start):
    # Triangle fan closing ring ring_i to a new pole vertex; winding matches loft() (start=True for ring 0).
    pi_ = len(verts)
    verts.append(pole)
    base = ring_i * n
    for j in range(n):
        j2 = (j + 1) % n
        faces.append((base + j2, base + j, pi_) if start else (base + j, base + j2, pi_))


def sweep(path, radii, n=6, normals=None, caps=('round', 'round'), sq=2.0, rot=0.0):
    # Tube along path. radii: list of (rw, rh) or f(s)->(rw, rh), s in [0,1]; rw along B, rh along N.
    # caps: 'round' (one 50deg ring + pole), 'flat' (pole at path end), None (open). sq: superellipse exponent.
    L = len(path)
    if callable(radii):
        radii = [radii(i / (L - 1)) for i in range(L)]
    T, N, B = frames(path, normals)

    def ring(P, Nn, Bn, rw, rh):
        out = []
        for k in range(n):
            t = 2 * pi * k / n + rot
            c, s = cos(t), sin(t)
            if sq != 2.0:
                c = math.copysign(abs(c) ** (2 / sq), c)
                s = math.copysign(abs(s) ** (2 / sq), s)
            out.append(P + Bn * (rw * c) + Nn * (rh * s))
        return out

    rings = [ring(path[i], N[i], B[i], *radii[i]) for i in range(L)]
    pre, post, poles = [], [], [None, None]
    for end in (0, 1):
        mode = caps[end]
        if not mode:
            continue
        i = 0 if end == 0 else L - 1
        d = -T[i] if end == 0 else T[i]
        rw, rh = radii[i]
        r = (rw + rh) * 0.5 if mode == 'round' else 0.0
        extra = []
        if mode == 'round':
            ar = math.radians(50)
            extra.append(ring(path[i] + d * (r * sin(ar)), N[i], B[i], rw * cos(ar), rh * cos(ar)))
        poles[end] = path[i] + d * r * 0.9
        if end == 0:
            pre = extra
        else:
            post = extra
    allr = pre + rings + post
    verts, faces = loft(allr)
    if poles[0] is not None:
        fan(verts, faces, 0, n, poles[0], True)
    if poles[1] is not None:
        fan(verts, faces, len(allr) - 1, n, poles[1], False)
    return verts, faces


def sweep_loop(path, rw, rh, n=4, hint=None, rot=pi / 4):
    # Closed tube (torus-like) along a closed path. hint(p) -> N direction (default Z up).
    L = len(path)
    rings = []
    for i in range(L):
        T = (path[(i + 1) % L] - path[i - 1]).normalized()
        h = hint(path[i]) if hint else Vector((0, 0, 1))
        Nn = (h - T * h.dot(T)).normalized()
        Bn = T.cross(Nn)
        rings.append([path[i] + Bn * (rw * cos(2 * pi * k / n + rot)) + Nn * (rh * sin(2 * pi * k / n + rot))
                      for k in range(n)])
    verts = [v for r in rings for v in r]
    faces = []
    for i in range(L):
        a0, b0 = i * n, ((i + 1) % L) * n
        for j in range(n):
            j2 = (j + 1) % n
            faces.append((a0 + j, a0 + j2, b0 + j2, b0 + j))
    return verts, faces


def mirror_x(pts):
    return [Vector((-p.x, p.y, p.z)) for p in pts]

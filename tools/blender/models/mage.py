# Mage v3 (low-poly, T-pose): base body + robe grown from the body + face decals, cape, hair, hat, staff.
# Spec: user request 2026-10-13 (black cape, mage hat, yellow hair, green eyes, staff in her right hand,
# purple robe dress with gold trim, heroic ~7-head proportions). Her right hand = -X.
import math
from math import cos, pi, sin

from mathutils import Matrix, Vector

from vlg3d import body, clothing, decals, geom, scene
from vlg3d.geom import gauss, lerp, smoothstep

P = body.BodyParams(fist=('R',))
HEM_Z = 0.13
CLOTH = 0.012          # robe offset from skin


def V(*a):
    return Vector(a)


def materials(b):
    b.mat('skin', (0.96, 0.79, 0.68), rough=0.7)
    b.mat('robe', (0.42, 0.20, 0.64), rough=0.8)
    b.mat('gold', (1.00, 0.78, 0.30), rough=0.35, metal=0.9)
    b.mat('cape', (0.05, 0.05, 0.065), rough=0.75)
    b.mat('lining', (0.22, 0.08, 0.30), rough=0.8)
    b.mat('hat', (0.06, 0.06, 0.075), rough=0.75)
    b.mat('band', (0.42, 0.20, 0.64), rough=0.7)
    b.mat('hair', (1.00, 0.83, 0.32), rough=0.6)
    b.mat('boot', (0.38, 0.22, 0.12), rough=0.7)
    b.mat('belt', (0.30, 0.17, 0.10), rough=0.7)
    b.mat('wood', (0.45, 0.28, 0.15), rough=0.8)
    b.mat('crystal', (0.45, 0.95, 1.00), rough=0.1, emit=(0.35, 0.95, 1.0), strength=6.0)
    b.mat('brow', (0.66, 0.45, 0.18), rough=0.7)
    b.mat('lash', (0.12, 0.07, 0.06), rough=0.7)
    b.ramp_mat('eye', [
        (0.000, (0.03, 0.03, 0.03)), (0.120, (0.05, 0.36, 0.15)),
        (0.205, (0.25, 0.75, 0.32)), (0.262, (0.95, 0.95, 0.94)), (1.000, (0.95, 0.95, 0.94))],
        rough=0.2, highlight=(0.40, 0.62, 0.06))
    b.ramp_mat('lips', [(0.0, (0.88, 0.55, 0.55)), (1.0, (0.88, 0.55, 0.55))], uv_axis='v', viewport=(0.88, 0.55, 0.55))


# ---------------------------------------------------------------- face
def face(b, head):
    tree = scene.trees([head])[0]
    hc = body.head_dims(P)[0]

    def on(x, z, out):
        return scene.ray_front(tree, hc.x + x, hc.z + z, 0.0) + V(0, -out, 0)

    verts = [on(0.0, -0.002, -0.002), on(0.0, -0.032, 0.016), on(0.011, -0.036, 0.002), on(-0.011, -0.036, 0.002),
             on(0.0, -0.040, 0.004), on(0.010, -0.014, -0.003), on(-0.010, -0.014, -0.003)]
    nose = b.mesh("Nose", verts, [(0, 1, 5), (0, 6, 1), (5, 1, 2), (6, 3, 1), (1, 4, 2), (1, 3, 4)], [b.mats['skin']])
    scene.face_dir(nose, V(0, -1, 0))

    hw, ir = 0.0160, 0.0074
    up = lambda s: 0.0085 * (1 - s * s) ** 0.6 * (1 - 0.15 * s)
    lo = lambda s: 0.0055 * (1 - s * s) ** 0.8
    tilt = lambda s: 0.0020 * s
    for side, nm in ((1, "L"), (-1, "R")):
        cx, cz = hc.x + side * 0.032, hc.z + 0.004
        cols, ends = decals.almond(cx, cz, hw, up, lo, tilt, side)
        uvf = lambda x, z, cx=cx, cz=cz: (0.5 + (x - cx) / (4 * ir), 0.5 + (z - cz - 0.0005) / (4 * ir))
        decals.grid(b, "Eye." + nm, tree, cols, ends, uvf, b.mats['eye'], 0.0012)
        pts, wid = [], []
        for i in range(7):
            s = -0.9 + 1.9 * i / 6
            pts.append((cx + side * s * hw, cz + up(s) + tilt(s)))
            wid.append((0.0007 + 0.0016 * smoothstep(-0.6, 1.0, s), 0.0004))
        pts.append((cx + side * (hw + 0.0035), cz + tilt(1) + 0.0030))
        wid.append((0.0006, 0.0003))
        decals.strip(b, "Lash." + nm, tree, pts, wid, b.mats['lash'], 0.0018)
        pts, wid = [], []
        for i in range(5):
            s = -0.9 + 2.0 * i / 4
            pts.append((cx + side * s * hw * 1.05, cz + 0.0200 + 0.0050 * (1 - ((s - 0.15) / 1.15) ** 2)))
            w = 0.0018 * (1 - 0.5 * smoothstep(-1.0, 1.2, s))
            wid.append((w, w))
        decals.strip(b, "Brow." + nm, tree, pts, wid, b.mats['brow'], 0.0015)
    cx, cz, lhw = hc.x, hc.z - 0.0635, 0.0150
    lup = lambda s: 0.0035 * (1 - s * s) ** 0.6
    llo = lambda s: 0.0045 * (1 - s * s) ** 0.8
    cols = [[(cx + s * lhw, cz + (lup(s) if t > 0 else llo(s)) * t) for t in (-1, 0, 1)] for s in (-0.6, 0.0, 0.6)]
    decals.grid(b, "Lips", tree, cols, [(cx - lhw, cz), (cx + lhw, cz)], lambda x, z: (0.5, 0.5), b.mats['lips'], 0.0015)


# ---------------------------------------------------------------- robe
def _ellipse_r(ang, rx, rf, rb):
    ry = rb if sin(ang) > 0 else rf
    return 1.0 / math.sqrt((cos(ang) / rx) ** 2 + (sin(ang) / ry) ** 2)


def build_robe(b, bod, J, bones):
    wx = J['wrist.L'].x - 0.012
    waist_z = J['waist'].z
    neck_z = J['neck'].z - 0.03  # above the shoulder tops, so the cut ring circles the neck
    bm = clothing.shell(bod, CLOTH, [(V(0, 0, neck_z), V(0, 0, 1)), (V(0, 0, waist_z), V(0, 0, -1)),
                                      (V(wx, 0, 0), V(1, 0, 0)), (V(-wx, 0, 0), V(-1, 0, 0))])
    sh_x, sh_z = J['shoulder.L'].x, J['shoulder.L'].z
    for v in bm.verts:  # sleeves loosen toward the wrist
        f = smoothstep(sh_x + 0.02, wx, abs(v.co.x))
        if f > 0:
            v.co.y *= 1 + 0.45 * f
            v.co.z = sh_z + (v.co.z - sh_z) * (1 + 0.45 * f)
    ch = J['chest']
    for v in bm.verts:  # bust
        if v.co.y < 0:
            g = sum(gauss(((v.co.x - s * 0.062) / 0.05) ** 2 + ((v.co.z - ch.z - 0.015) / 0.05) ** 2) for s in (-1, 1))
            v.co.y -= 0.03 * g * min(1.0, -v.co.y / 0.05)
    loops = clothing.boundary_loops(bm)
    body_tree = scene.trees([bod])
    out = {}

    # skirt from the waist cut: hips first, then A-line flare with alternating pleats
    wl, wc = clothing.orient_loop(clothing.pick_loop(loops, lambda c: abs(c.z - waist_z) < 0.04 and abs(c.x) < 0.1), 'z')
    base = [v.co.copy() for v in wl]
    out['waist'] = (base, wc)
    rings = []
    K = 7
    for k in range(1, K + 1):
        f = k / K
        z = lerp(waist_z, HEM_Z, f)
        ease = 1 - (1 - f) ** 1.6
        ring = []
        for j, p in enumerate(base):
            ang = math.atan2(p.y - wc.y, p.x - wc.x)
            r0 = math.hypot(p.x - wc.x, p.y - wc.y)
            r = lerp(r0, _ellipse_r(ang, 0.29, 0.23, 0.22), ease) + (0.014 if j % 2 else -0.006) * f
            q = V(wc.x + cos(ang) * r, wc.y + sin(ang) * r, z)
            ring.append(scene.push_out(q, body_tree, 0.015))
        rings.append(ring)
    hem = rings[-1]
    rings.append([V(wc.x + (p.x - wc.x) * 0.95, wc.y + (p.y - wc.y) * 0.95, p.z + 0.05) for p in hem])
    clothing.grow(bm, wl, rings)
    out['hem'] = (hem, wc)

    # bell cuffs from the wrist cuts, drooping on the underside
    out['cuffs'] = []
    for side in (1, -1):
        sl, sc = clothing.orient_loop(clothing.pick_loop(loops, lambda c, s=side: s * c.x > wx - 0.05), 'x')
        base = [v.co.copy() for v in sl]
        rings = []
        for k, (dx, gv, gh) in enumerate(((0.030, 0.028, 0.018), (0.060, 0.058, 0.038), (0.080, 0.080, 0.052))):
            ring = []
            for p in base:
                ang = math.atan2(p.z - sc.z, p.y - sc.y)
                r0 = math.hypot(p.y - sc.y, p.z - sc.z)
                droop = 0.075 * (k + 1) / 3 * max(0.0, -sin(ang)) ** 1.5
                ring.append(V(p.x + side * dx, sc.y + cos(ang) * (r0 + gh), sc.z + sin(ang) * (r0 + gv) - droop))
            rings.append(ring)
        end = rings[-1]
        ec = sum(end, Vector()) / len(end)
        rings.append([ec + (p - ec) * 0.88 - V(side * 0.010, 0, 0) for p in end])
        clothing.grow(bm, sl, rings)
        out['cuffs'].append((end, side))

    # collar: short turn-in so the inside is not visible
    cl, cc = clothing.orient_loop(clothing.pick_loop(loops, lambda c: c.z > neck_z - 0.03 and abs(c.x) < 0.1), 'z')
    out['collar'] = ([v.co.copy() for v in cl], cc)
    clothing.grow(bm, cl, [[cc + (v.co - cc) * 0.88 - V(0, 0, 0.02) for v in cl]])

    robe = clothing.to_object(b, bm, "Robe", [b.mats['robe']], clothing.away_from_bones(J, bones))
    _trims(b, out)
    return robe


def _radial_out(pts, c, d, axis='z'):
    res = []
    for p in pts:
        r = V(p.x - c.x, p.y - c.y, 0) if axis == 'z' else V(0, p.y - c.y, p.z - c.z)
        res.append(p + r.normalized() * d)
    return res


def _trims(b, out):
    hem, c = out['hem']
    b.loop("Trim.Hem", _radial_out([p + V(0, 0, 0.012) for p in hem], c, 0.004), 0.005, 0.011, b.mats['gold'])
    col, c = out['collar']
    b.loop("Trim.Collar", _radial_out(col, c, 0.003), 0.005, 0.006, b.mats['gold'])
    for end, side in out['cuffs']:
        ec = sum(end, Vector()) / len(end)
        path = [p - V(side * 0.008, 0, 0) for p in _radial_out(end, ec, 0.004, 'x')]
        b.loop("Trim.Cuff." + ('L' if side > 0 else 'R'), path, 0.009, 0.005, b.mats['gold'],
               hint=lambda p, ec=ec: V(0, p.y - ec.y, p.z - ec.z))
    wl, c = out['waist']
    belt = _radial_out(wl, c, 0.006)
    b.loop("Belt", belt, 0.006, 0.017, b.mats['belt'])
    fy = min(p.y for p in belt) - 0.006
    bz = c.z
    b.loop("Buckle", [V(0.016 * x, fy, bz + 0.019 * z) for x, z in ((1, 1), (-1, 1), (-1, -1), (1, -1))],
           0.004, 0.004, b.mats['gold'], hint=lambda p: V(0, -1, 0))


def build_boots(b, bod, J, bones):
    top = 0.27
    bm = clothing.shell(bod, 0.008, [(V(0, 0, top), V(0, 0, 1))])
    for v in bm.verts:
        v.co.z = max(v.co.z, 0.0)
    for lp in clothing.boundary_loops(bm):
        ol, c = clothing.orient_loop(lp, 'z')
        base = [v.co.copy() for v in ol]
        flare = [c + (p - c) * 1.18 + V(0, 0, 0.01) for p in base]
        fold = [c + (p - c) * 1.22 - V(0, 0, 0.035) for p in base]
        clothing.grow(bm, ol, [flare, fold])
    return clothing.to_object(b, bm, "Boots", [b.mats['boot']], clothing.away_from_bones(J, bones))


# ---------------------------------------------------------------- staff (through the right fist)
def build_staff(b, J):
    S = V(J['hand.R'].x, J['hand.R'].y, 0.0)
    arm_z = J['hand.R'].z
    pts, rad = [], []
    for z in (0.15, 0.40, 0.62, 0.85, 1.05, arm_z, 1.50, 1.60, 1.78):
        k = 1 - gauss(((z - arm_z) / 0.15) ** 2)
        pts.append(V(S.x + 0.005 * sin(z * 7) * k, S.y + 0.004 * sin(z * 5 + 1) * k, z))
        r = 0.016 + (0.003 if z in (0.62, 1.05, 1.60) else 0.0) + (0.002 if z == 1.78 else 0.0)
        rad.append((r, r))
    b.tube("Staff", pts, rad, b.mats['wood'], n=6, caps=('round', 'flat'))
    top, gz = 1.78, 1.875
    for k in range(3):
        a = math.radians(30 + 120 * k)
        d = V(cos(a), sin(a), 0)
        cp = [S + d * 0.010 + V(0, 0, top - 0.04), S + d * 0.030 + V(0, 0, top + 0.02), S + d * 0.042 + V(0, 0, gz - 0.01),
              S + d * 0.030 + V(0, 0, gz + 0.050), S + d * 0.010 + V(0, 0, gz + 0.080)]
        b.tube("Prong.%d" % k, cp, lambda s: (lerp(0.009, 0.0035, s),) * 2, b.mats['wood'], n=4)
    b.loop("Staff.Band", [S + V(0.0195 * cos(a), 0.0195 * sin(a), top - 0.035) for a in [2 * pi * i / 8 for i in range(8)]],
           0.005, 0.010, b.mats['gold'])
    c, r = S + V(0, 0, gz), 0.028
    verts = [c + V(0, 0, 0.075), c + V(0, 0, -0.055)]
    for zo in (0.022, -0.016):
        verts += [c + V(r * cos(2 * pi * k / 6), r * sin(2 * pi * k / 6), zo) for k in range(6)]
    faces = []
    for k in range(6):
        k2 = (k + 1) % 6
        faces += [(0, 2 + k, 2 + k2), (2 + k, 8 + k, 8 + k2, 2 + k2), (1, 8 + k2, 8 + k)]
    cr = b.mesh("Crystal", verts, faces, [b.mats['crystal']])
    scene.orient_axis(cr, 'p', tuple(c))
    b.light("Crystal.Light", 'POINT', 12.0, (0.4, 0.95, 1.0), c, size=0.02)


# ---------------------------------------------------------------- cape
CAPE_KEYS = [
    # z,     Rx,    Ry,    alpha (deg past the sides)
    (1.395, 0.165, 0.075, -18),
    (1.340, 0.192, 0.098, -28),
    (1.230, 0.200, 0.112, -6),
    (1.020, 0.220, 0.128, 6),
    (0.760, 0.258, 0.172, 10),
    (0.450, 0.320, 0.235, 14),
    (0.100, 0.420, 0.322, 17),
]
CM = 15


def build_cape(b, colliders):
    rings = []
    for (z, Rx, Ry, al) in CAPE_KEYS:
        ring = []
        for j in range(CM):
            u = j / (CM - 1)
            th = math.radians(-al + (180 + 2 * al) * u)
            ro = 0.034 * smoothstep(1.25, 0.15, z) * (1.0 if j % 2 else 0.0)
            zz = z - 0.035 * cos(th) ** 2 * smoothstep(1.30, 1.395, z)
            if z < 0.2:
                zz += 0.035 * (1.0 if j % 2 else 0.0)
            ring.append(V((Rx + ro) * cos(th), (Ry + ro) * sin(th), zz))
        rings.append(ring)
    tl = scene.trees(colliders)
    R = len(rings)
    for it in range(4):
        rings = [[scene.push_out(p, tl, 0.010) for p in r] for r in rings]
        if it < 3:
            new = [[p.copy() for p in r] for r in rings]
            for i in range(1, R - 1):
                for j in range(CM):
                    new[i][j] = rings[i][j].lerp((rings[i - 1][j] + rings[i + 1][j]) / 2, 0.3)
            rings = new
    v, f = geom.loft(rings, closed=False)
    ob = b.mesh("Cape", v, f, [b.mats['cape'], b.mats['lining']])
    scene.orient_axis(ob, 'z', (0, 0, 0))
    sm = ob.modifiers.new("Solidify", 'SOLIDIFY')
    sm.thickness, sm.offset, sm.material_offset, sm.material_offset_rim = 0.008, 1.0, 1, 0
    for p, nm in ((rings[0][0], "L"), (rings[0][-1], "R")):
        bmv, bmf = geom.sweep([p + V(0, 0.004, 0.008), p + V(0, -0.012, 0.008)], [(0.013, 0.013)] * 2, n=6, caps=('flat', 'flat'))
        cl = b.mesh("Clasp." + nm, bmv, bmf, [b.mats['gold']])
        scene.orient_path(cl, [p + V(0, 0.004, 0.008), p + V(0, -0.012, 0.008)])
    return ob


# ---------------------------------------------------------------- hair
def _hair_normal(p):
    hc = body.head_dims(P)[0]
    return (p - V(0, -0.02, min(p.z, hc.z))).normalized()


def _lock(b, name, cp, wfun, tl, margin):
    path = [Vector(p) for p in cp]
    for it in range(3):
        path = [path[0]] + [scene.push_out(p, tl, margin) for p in path[1:]]
    return b.tube(name, path, wfun, b.mats['hair'], n=4, normals=[_hair_normal(p) for p in path], caps=(None, 'round'))


def build_hair(b, head, bod, robe, cape):
    import bmesh
    import bpy
    hp = lambda phi, w, d: body.head_point(P, phi, w, d)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=12, radius=1.0)
    kill = []
    for f in bm.faces:
        c = f.calc_center_median()
        if (c.y < -0.2 and c.z < 0.45 - 0.5 * c.x * c.x) or c.z < -0.25 - 0.40 * smoothstep(0.0, 0.8, c.y):
            kill.append(f)
    bmesh.ops.delete(bm, geom=kill, context='FACES')
    for v in bm.verts:
        v.co = body.head_shape(P, v.co.x, v.co.y, v.co.z, 0.010)
    me = bpy.data.meshes.new(b.prefix + "haircap")
    bm.to_mesh(me)
    bm.free()
    cap = b.mesh("Hair.Cap", [v.co for v in me.vertices], [tuple(p.vertices) for p in me.polygons], [b.mats['hair']])
    bpy.data.meshes.remove(me)

    tl_head = scene.trees([head, cap])
    tl_all = scene.trees([head, cap, bod, robe, cape])
    for i, phi in enumerate((-128, -110, -92, -74, -56)):
        wt = (0.10, 0.24, 0.20, 0.26, 0.22)[i]
        cp = [hp(phi, 0.66, 0.012), hp(phi - 10, lerp(0.66, wt, 0.55), 0.020), hp(phi - 22, wt, 0.016)]
        _lock(b, "Hair.Bang.%d" % i, cp, lambda s: (lerp(0.030, 0.004, s ** 1.2), lerp(0.008, 0.002, s)), tl_head, 0.008)
    for i, phi in enumerate((-36, -18, -144, -162)):
        p2 = hp(phi, -0.30, 0.022)
        cp = [hp(phi, 0.52, 0.012), hp(phi, 0.10, 0.018), p2, V(p2.x * 0.98, p2.y - 0.004, 1.425 + 0.02 * (i % 2))]
        _lock(b, "Hair.Side.%d" % i, cp, lambda s: (lerp(0.032, 0.006, s ** 2.0), lerp(0.010, 0.003, s)), tl_all, 0.008)

    # back mass hugging the head, then over the cape with zigzag tips
    tl_back = scene.trees([cape, robe, bod, head])
    NU = 13
    phis = [lerp(-2, 182, j / (NU - 1)) for j in range(NU)]
    rings = [[hp(ph, w, 0.016) for ph in phis] for w in (0.55, 0.20, -0.15)]
    nape = [hp(90 + (ph - 90) * 0.85, -0.45, 0.020) for ph in phis]
    rings.append(nape)
    for z, kx in ((1.36, 1.10), (1.20, 1.40), (1.05, 1.60)):
        ring = []
        for j in range(NU):
            x = nape[j].x * kx
            by = scene.back_y(tl_back, x, z)
            zz = z
            if z < 1.1:
                edge = abs(j - (NU - 1) / 2) / ((NU - 1) / 2)
                zz += (0.06 if j % 2 else -0.02) + 0.10 * edge ** 2
            ring.append(V(x, max((by if by is not None else nape[j].y) + 0.012, nape[j].y), zz))
        rings.append(ring)
    for it in range(3):
        rings = [[scene.push_out(p, tl_all, 0.008) for p in r] for r in rings]
    v, f = geom.loft(rings, closed=False)
    curtain = b.mesh("Hair.Curtain", v, f, [b.mats['hair']])
    scene.orient_axis(curtain, 'z', (0, -0.02, 0))
    sm = curtain.modifiers.new("Solidify", 'SOLIDIFY')
    sm.thickness, sm.offset = 0.010, 1.0

    tl_top = scene.trees([head, cap, bod, robe, cape, curtain])
    tl_back2 = scene.trees([curtain, cape, robe, bod, head])
    n = 9
    for i in range(n):
        phi = lerp(10, 170, i / (n - 1))
        p2 = hp(90 + (phi - 90) * 0.85, -0.40, 0.030)
        edge = abs(i - (n - 1) / 2) / ((n - 1) / 2)
        ztip = 0.98 + 0.05 * (i % 2) + 0.10 * edge ** 2
        lower = []
        for z, kx in ((1.30, 1.25), (ztip, 1.60)):
            by = scene.back_y(tl_back2, p2.x * kx, z)
            lower.append(V(p2.x * kx, (by if by is not None else 0.12) + 0.010, z))
        cp = [hp(phi, 0.30, 0.024), hp(phi, 0.0, 0.028), p2] + lower
        _lock(b, "Hair.Back.%d" % i, cp, lambda s: (lerp(0.040, 0.006, s ** 1.6), lerp(0.010, 0.003, s)), tl_top, 0.010)


# ---------------------------------------------------------------- hat
def build_hat(b):
    hc = body.head_dims(P)[0]
    M = Matrix.Translation(V(0, hc.y + 0.003, hc.z + 0.069)) @ Matrix.Rotation(-0.08, 4, 'X') @ Matrix.Rotation(0.05, 4, 'Y')
    rings = []
    n = 20
    for f in (0.0, 0.5, 1.0):
        rx, ry = lerp(0.094, 0.275, f), lerp(0.110, 0.265, f)
        rings.append([M @ V(rx * cos(2 * pi * j / n), ry * sin(2 * pi * j / n),
                            -0.030 * f * f + 0.010 * f * f * sin(3 * 2 * pi * j / n + 0.5) + 0.004 * (1 if j % 2 else -1) * f * f)
                      for j in range(n)])
    v, f_ = geom.loft(rings)
    brim = b.mesh("Hat.Brim", v, f_, [b.mats['hat']])
    scene.face_dir(brim, V(0, 0, 1))
    sm = brim.modifiers.new("Solidify", 'SOLIDIFY')
    sm.thickness, sm.offset = 0.008, 0.0
    cpl = [V(0, 0, 0.0), V(0, 0.0, 0.13), V(0, 0.020, 0.25), V(0, 0.065, 0.335), V(0, 0.130, 0.375), V(0, 0.175, 0.355)]
    radk = [0.106, 0.078, 0.052, 0.031, 0.015, 0.004]
    pw = [M @ p for p in cpl]
    v, f_ = geom.sweep(pw, [(r * 1.08, r) for r in radk], n=10, caps=(None, 'flat'))
    cone = b.mesh("Hat.Cone", v, f_, [b.mats['hat']])
    scene.orient_path(cone, pw)
    up = M.to_3x3() @ V(0, 0, 1)
    b.loop("Hat.Band", [M @ V(0.1105 * cos(a), 0.1190 * sin(a), 0.024) for a in [2 * pi * i / 10 + pi / 10 for i in range(10)]],
           0.005, 0.014, b.mats['band'], hint=lambda p: up)
    fwd = M.to_3x3() @ V(0, -1, 0)
    b.loop("Hat.Buckle", [M @ V(0.015 * x, -0.126, 0.024 + 0.015 * z) for x, z in ((1, 1), (-1, 1), (-1, -1), (1, -1))],
           0.0035, 0.0035, b.mats['gold'], hint=lambda p: fwd)


# ---------------------------------------------------------------- stage
def stage(b):
    import bpy
    sc = bpy.context.scene
    cam = bpy.data.objects.get("Camera")
    if cam:
        cam.location = (-1.55, -3.6, 1.30)
        cam.rotation_euler = (V(0.0, 0.0, 1.02) - cam.location).to_track_quat('-Z', 'Y').to_euler()
        cam.data.lens = 50
    b.light("Stage.Key", 'AREA', 400, (1.0, 0.98, 0.95), (-2.2, -2.6, 3.0), size=2.0, aim=(0, 0, 1.1))
    b.light("Stage.Fill", 'AREA', 120, (0.85, 0.9, 1.0), (2.6, -1.8, 1.6), size=1.5, aim=(0, 0, 1.1))
    b.light("Stage.Rim", 'AREA', 300, (1.0, 0.95, 0.85), (1.2, 3.0, 2.6), size=1.5, aim=(0, 0, 1.1))
    sc.render.resolution_x, sc.render.resolution_y = 1080, 1440


def build(coll="Mage", prefix="mage_"):
    b = scene.Builder(coll, prefix)
    b.clear()
    materials(b)
    bod = body.build_body(b, P, b.mats['skin'])
    J = body.joints(bod)
    bones = body.skeleton(P)[3]
    head = body.build_head(b, P, b.mats['skin'])
    face(b, head)
    robe = build_robe(b, bod, J, bones)
    build_boots(b, bod, J, bones)
    build_staff(b, J)
    cape = build_cape(b, [robe, bod])
    build_hair(b, head, bod, robe, cape)
    build_hat(b)
    stage(b)
    return b

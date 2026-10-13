# Mage v4 (low-poly, T-pose). Target: generated reference 505
# (art/characters/ref/gen/vlg_mage_front_t2i_cn100_d100_s505_00001_.png, user pick 2026-10-13).
# Kept from the original spec: green eyes, cyan crystal, staff in her right hand (-X).
# From 505: black inner dress + wide belt + gold amulet, long purple open coat (replaces the black cape),
# purple bell sleeves, black knee boots, big drooping hat with a kinked tip, long hair over the chest.
import math
from math import cos, pi, sin

from mathutils import Matrix, Vector

from vlg3d import body, clothing, decals, geom, scene
from vlg3d.geom import gauss, lerp, smoothstep

P = body.BodyParams(heads=6.6, fist=('R',))
K = P.head_h / 0.2357    # head-relative sizes were tuned on a 0.2357 m head
CLOTH = 0.010            # dress offset from skin
DRESS_HEM = 0.48
BOOT_TOP = 0.46


def V(*a):
    return Vector(a)


def materials(b):
    b.mat('skin', (0.96, 0.79, 0.68), rough=0.7)
    b.mat('dress', (0.07, 0.06, 0.08), rough=0.75)
    b.mat('coat', (0.46, 0.20, 0.72), rough=0.75)
    b.mat('lining', (0.12, 0.07, 0.16), rough=0.8)
    b.mat('gold', (1.00, 0.78, 0.30), rough=0.35, metal=0.9)
    b.mat('hat', (0.06, 0.06, 0.07), rough=0.75)
    b.mat('hair', (1.00, 0.85, 0.25), rough=0.6)
    b.mat('boot', (0.07, 0.065, 0.07), rough=0.5)
    b.mat('belt', (0.55, 0.32, 0.15), rough=0.6)
    b.mat('wood', (0.40, 0.24, 0.14), rough=0.85)
    b.mat('crystal', (0.45, 0.95, 1.00), rough=0.1, emit=(0.35, 0.95, 1.0), strength=6.0)
    b.mat('brow', (0.70, 0.50, 0.15), rough=0.7)
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
        return scene.ray_front(tree, hc.x + x * K, hc.z + z * K, 0.0) + V(0, -out * K, 0)

    verts = [on(0.0, -0.002, -0.002), on(0.0, -0.032, 0.016), on(0.011, -0.036, 0.002), on(-0.011, -0.036, 0.002),
             on(0.0, -0.040, 0.004), on(0.010, -0.014, -0.003), on(-0.010, -0.014, -0.003)]
    nose = b.mesh("Nose", verts, [(0, 1, 5), (0, 6, 1), (5, 1, 2), (6, 3, 1), (1, 4, 2), (1, 3, 4)], [b.mats['skin']])
    scene.face_dir(nose, V(0, -1, 0))

    hw, ir = 0.0170 * K, 0.0080 * K
    up = lambda s: 0.0092 * K * (1 - s * s) ** 0.6 * (1 - 0.15 * s)
    lo = lambda s: 0.0058 * K * (1 - s * s) ** 0.8
    tilt = lambda s: 0.0020 * K * s
    for side, nm in ((1, "L"), (-1, "R")):
        cx, cz = hc.x + side * 0.033 * K, hc.z + 0.004 * K
        cols, ends = decals.almond(cx, cz, hw, up, lo, tilt, side)
        uvf = lambda x, z, cx=cx, cz=cz: (0.5 + (x - cx) / (4 * ir), 0.5 + (z - cz - 0.0005 * K) / (4 * ir))
        decals.grid(b, "Eye." + nm, tree, cols, ends, uvf, b.mats['eye'], 0.0012)
        pts, wid = [], []
        for i in range(7):
            s = -0.9 + 1.9 * i / 6
            pts.append((cx + side * s * hw, cz + up(s) + tilt(s)))
            wid.append(((0.0007 + 0.0016 * smoothstep(-0.6, 1.0, s)) * K, 0.0004 * K))
        pts.append((cx + side * (hw + 0.0035 * K), cz + tilt(1) + 0.0030 * K))
        wid.append((0.0006 * K, 0.0003 * K))
        decals.strip(b, "Lash." + nm, tree, pts, wid, b.mats['lash'], 0.0018)
        pts, wid = [], []
        for i in range(5):
            s = -0.9 + 2.0 * i / 4
            pts.append((cx + side * s * hw * 1.05, cz + (0.0205 + 0.0050 * (1 - ((s - 0.15) / 1.15) ** 2)) * K))
            w = 0.0016 * K * (1 - 0.5 * smoothstep(-1.0, 1.2, s))
            wid.append((w, w))
        decals.strip(b, "Brow." + nm, tree, pts, wid, b.mats['brow'], 0.0015)
    cx, cz, lhw = hc.x, hc.z - 0.0635 * K, 0.0140 * K
    lup = lambda s: 0.0032 * K * (1 - s * s) ** 0.6
    llo = lambda s: 0.0042 * K * (1 - s * s) ** 0.8
    cols = [[(cx + s * lhw, cz + (lup(s) if t > 0 else llo(s)) * t) for t in (-1, 0, 1)] for s in (-0.6, 0.0, 0.6)]
    decals.grid(b, "Lips", tree, cols, [(cx - lhw, cz), (cx + lhw, cz)], lambda x, z: (0.5, 0.5), b.mats['lips'], 0.0015)


# ---------------------------------------------------------------- dress (black bodice, purple bell sleeves)
def _ellipse_r(ang, rx, rf, rb):
    ry = rb if sin(ang) > 0 else rf
    return 1.0 / math.sqrt((cos(ang) / rx) ** 2 + (sin(ang) / ry) ** 2)


def build_dress(b, bod, J, bones):
    wx = J['wrist.L'].x - 0.012
    waist_z = J['waist'].z
    neck_z = J['neck'].z - 0.03  # above the shoulder tops, so the cut ring circles the neck
    bm = clothing.shell(bod, CLOTH, [(V(0, 0, neck_z), V(0, 0, 1)), (V(0, 0, waist_z), V(0, 0, -1)),
                                      (V(wx, 0, 0), V(1, 0, 0)), (V(-wx, 0, 0), V(-1, 0, 0))])
    sh_x, sh_z = J['shoulder.L'].x, J['shoulder.L'].z
    for v in bm.verts:  # sleeves loosen toward the wrist
        f = smoothstep(sh_x + 0.04, wx, abs(v.co.x))
        if f > 0:
            v.co.y *= 1 + 0.55 * f
            v.co.z = sh_z + (v.co.z - sh_z) * (1 + 0.55 * f)
    ch = J['chest']
    for v in bm.verts:  # bust
        if v.co.y < 0:
            g = sum(gauss(((v.co.x - s * 0.060) / 0.048) ** 2 + ((v.co.z - ch.z - 0.015) / 0.048) ** 2) for s in (-1, 1))
            v.co.y -= 0.028 * g * min(1.0, -v.co.y / 0.05)
    loops = clothing.boundary_loops(bm)
    body_tree = scene.trees([bod])

    # knee-length skirt from the waist cut
    wl, wc = clothing.orient_loop(clothing.pick_loop(loops, lambda c: abs(c.z - waist_z) < 0.04 and abs(c.x) < 0.1), 'z')
    base = [v.co.copy() for v in wl]
    rings = []
    N = 4
    for k in range(1, N + 1):
        f = k / N
        z = lerp(waist_z, DRESS_HEM, f)
        ease = 1 - (1 - f) ** 1.5
        ring = []
        for j, p in enumerate(base):
            ang = math.atan2(p.y - wc.y, p.x - wc.x)
            r0 = math.hypot(p.x - wc.x, p.y - wc.y)
            r = lerp(r0, _ellipse_r(ang, 0.20, 0.155, 0.16), ease) + (0.008 if j % 2 else -0.004) * f
            ring.append(scene.push_out(V(wc.x + cos(ang) * r, wc.y + sin(ang) * r, z), body_tree, 0.012))
        rings.append(ring)
    hem = rings[-1]
    rings.append([V(wc.x + (p.x - wc.x) * 0.94, wc.y + (p.y - wc.y) * 0.94, p.z + 0.04) for p in hem])
    clothing.grow(bm, wl, rings)

    # big bell sleeves, drooping on the underside
    for side in (1, -1):
        sl, sc = clothing.orient_loop(clothing.pick_loop(loops, lambda c, s=side: s * c.x > wx - 0.05), 'x')
        base = [v.co.copy() for v in sl]
        rings = []
        for k, (dx, gv, gh) in enumerate(((0.035, 0.035, 0.022), (0.070, 0.075, 0.048), (0.100, 0.110, 0.070))):
            ring = []
            for p in base:
                ang = math.atan2(p.z - sc.z, p.y - sc.y)
                r0 = math.hypot(p.y - sc.y, p.z - sc.z)
                droop = 0.10 * (k + 1) / 3 * max(0.0, -sin(ang)) ** 1.5
                ring.append(V(p.x + side * dx, sc.y + cos(ang) * (r0 + gh), sc.z + sin(ang) * (r0 + gv) - droop))
            rings.append(ring)
        end = rings[-1]
        ec = sum(end, Vector()) / len(end)
        rings.append([ec + (p - ec) * 0.86 - V(side * 0.012, 0, 0) for p in end])
        clothing.grow(bm, sl, rings)

    cl, cc = clothing.orient_loop(clothing.pick_loop(loops, lambda c: c.z > neck_z - 0.03 and abs(c.x) < 0.1), 'z')
    clothing.grow(bm, cl, [[cc + (v.co - cc) * 0.88 - V(0, 0, 0.02) for v in cl]])

    dress = clothing.to_object(b, bm, "Dress", [b.mats['dress'], b.mats['coat']], clothing.away_from_bones(J, bones))
    split = sh_x + 0.10  # 505: black upper sleeve, purple from mid upper arm
    dress.data.polygons.foreach_set('material_index', [1 if abs(p.center.x) > split else 0 for p in dress.data.polygons])
    dress.data.update()

    belt = [p + V(p.x - wc.x, p.y - wc.y, 0).normalized() * 0.007 for p in base]
    b.loop("Belt", belt, 0.007, 0.026, b.mats['belt'])
    fy = min(p.y for p in belt) - 0.007
    b.loop("Buckle", [V(0.022 * x, fy, wc.z + 0.024 * z) for x, z in ((1, 1), (-1, 1), (-1, -1), (1, -1))],
           0.005, 0.005, b.mats['gold'], hint=lambda p: V(0, -1, 0))
    tree = scene.trees([dress])[0]
    c = scene.ray_front(tree, 0.0, ch.z + 0.035, 0.012)
    s = 0.018
    verts = [c + V(0, 0, s * 1.3), c + V(s, 0, 0), c + V(0, 0, -s * 1.3), c + V(-s, 0, 0), c + V(0, -s * 0.6, 0)]
    am = b.mesh("Amulet", verts, [(4, 0, 1), (4, 1, 2), (4, 2, 3), (4, 3, 0), (0, 3, 2, 1)], [b.mats['gold']])
    scene.orient_axis(am, 'p', tuple(c + V(0, 0.01, 0)))
    return dress


def build_boots(b, bod, J, bones):
    bm = clothing.shell(bod, 0.009, [(V(0, 0, BOOT_TOP), V(0, 0, 1))])
    for v in bm.verts:
        v.co.z = max(v.co.z, 0.0)
    for lp in clothing.boundary_loops(bm):
        ol, c = clothing.orient_loop(lp, 'z')
        base = [v.co.copy() for v in ol]
        clothing.grow(bm, ol, [[c + (p - c) * 1.16 + V(0, 0, 0.012) for p in base],
                               [c + (p - c) * 1.20 - V(0, 0, 0.03) for p in base]])
    return clothing.to_object(b, bm, "Boots", [b.mats['boot']], clothing.away_from_bones(J, bones))


# ---------------------------------------------------------------- staff (gnarly, through the right fist)
def build_staff(b, J):
    S = V(J['hand.R'].x, J['hand.R'].y, 0.0)
    arm_z = J['hand.R'].z
    pts, rad = [], []
    for z in (0.04, 0.25, 0.48, 0.70, 0.92, 1.10, arm_z, 1.45, 1.62, 1.80):
        k = 1 - gauss(((z - arm_z) / 0.15) ** 2)
        pts.append(V(S.x + 0.010 * sin(z * 6) * k, S.y + 0.006 * sin(z * 4 + 1) * k, z))
        r = 0.022 + 0.004 * sin(z * 13) + (0.006 if z in (0.48, 1.10, 1.62) else 0.0) + 0.008 * smoothstep(1.6, 1.8, z)
        rad.append((r, r * 0.9))
    b.tube("Staff", pts, rad, b.mats['wood'], n=6, caps=('round', 'flat'))
    top, gz = 1.80, 1.93
    for k in range(4):
        a = math.radians(45 + 90 * k)
        d = V(cos(a), sin(a), 0)
        cp = [S + d * 0.015 + V(0, 0, top - 0.02), S + d * 0.040 + V(0, 0, top + 0.04), S + d * 0.034 + V(0, 0, gz - 0.02)]
        b.tube("Prong.%d" % k, cp, lambda s: (lerp(0.011, 0.005, s),) * 2, b.mats['wood'], n=4)
    c, r = S + V(0, 0, gz), 0.040
    verts = [c + V(0, 0, 0.095), c + V(0, 0, -0.065)]
    for zo in (0.030, -0.020):
        verts += [c + V(r * cos(2 * pi * k / 6), r * sin(2 * pi * k / 6), zo) for k in range(6)]
    faces = []
    for k in range(6):
        k2 = (k + 1) % 6
        faces += [(0, 2 + k, 2 + k2), (2 + k, 8 + k, 8 + k2, 2 + k2), (1, 8 + k2, 8 + k)]
    cr = b.mesh("Crystal", verts, faces, [b.mats['crystal']])
    scene.orient_axis(cr, 'p', tuple(c))
    b.light("Crystal.Light", 'POINT', 15.0, (0.4, 0.95, 1.0), c, size=0.03)


# ---------------------------------------------------------------- coat (open front, replaces the cape)
CM = 17
RIGHT_FLARE, LEFT_FLARE = 1.12, 1.0


def build_coat(b, J, colliders):
    sh_z, ch_z, wa_z, hip_z = J['shoulder.L'].z, J['chest'].z, J['waist'].z, J['hip.L'].z
    keys = [  # z, Rx, Ry, alpha (deg past the sides toward the front). 505: panels hang from under the arms.
        (sh_z - 0.09, 0.480, 0.130, 30),
        (ch_z - 0.05, 0.490, 0.140, 30),
        (wa_z, 0.495, 0.160, 28),
        (hip_z, 0.505, 0.200, 27),
        (0.70, 0.525, 0.240, 26),
        (0.42, 0.550, 0.270, 25),
        (0.10, 0.570, 0.300, 24),
    ]
    rings = []
    for i, (z, Rx, Ry, al) in enumerate(keys):
        ring = []
        for j in range(CM):
            u = j / (CM - 1)
            th = math.radians(-al + (180 + 2 * al) * u)
            ro = 0.028 * smoothstep(wa_z, 0.15, z) * (1.0 if j % 2 else 0.0)
            zz = z + (0.12, 0.05)[i] * max(0.0, sin(th)) if i < 2 else z
            if i == len(keys) - 1:
                zz += 0.035 * (1.0 if j % 2 else 0.0)
            rx = Rx * (RIGHT_FLARE if cos(th) < 0 else LEFT_FLARE)  # 505 flows wider on her right (-X)
            ring.append(V((rx + ro) * cos(th), (Ry + ro) * sin(th), zz))
        rings.append(ring)
    tl = scene.trees(colliders)
    R = len(rings)
    for it in range(4):
        rings = [[scene.push_out(p, tl, 0.012, max_move=0.06) for p in r] for r in rings]
        if it < 3:
            new = [[p.copy() for p in r] for r in rings]
            for i in range(1, R - 1):
                for j in range(CM):
                    new[i][j] = rings[i][j].lerp((rings[i - 1][j] + rings[i + 1][j]) / 2, 0.3)
            rings = new
    v, f = geom.loft(rings, closed=False)
    ob = b.mesh("Coat", v, f, [b.mats['coat'], b.mats['lining']])
    scene.orient_axis(ob, 'z', (0, 0, 0))
    sm = ob.modifiers.new("Solidify", 'SOLIDIFY')
    sm.thickness, sm.offset, sm.material_offset, sm.material_offset_rim = 0.008, 1.0, 1, 0
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


def build_hair(b, head, bod, dress, coat, J):
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
    tl_all = scene.trees([head, cap, bod, dress, coat])
    # center-parted bangs
    for i, (phi, sweep, wt) in enumerate(((-128, -16, 0.12), (-110, -18, 0.24), (-94, -12, 0.30),
                                          (-86, 12, 0.30), (-70, 18, 0.24), (-52, 16, 0.12))):
        cp = [hp(phi, 0.66, 0.012), hp(phi + sweep * 0.45, lerp(0.66, wt, 0.55), 0.020), hp(phi + sweep, wt, 0.016)]
        _lock(b, "Hair.Bang.%d" % i, cp, lambda s: (lerp(0.032, 0.004, s ** 1.2), lerp(0.009, 0.002, s)), tl_head, 0.008)
    # long locks over the chest
    sh_z, ch_z = J['shoulder.L'].z, J['chest'].z
    for i, (phi, out) in enumerate(((-46, 1.15), (-32, 1.05), (-18, 0.95), (-134, 1.15), (-148, 1.05), (-162, 0.95))):
        p2 = hp(phi, -0.35, 0.026)
        s = 1 if p2.x > 0 else -1
        cp = [hp(phi, 0.50, 0.012), hp(phi, 0.05, 0.020), p2,
              V(p2.x * out + s * 0.012, -0.10, sh_z + 0.04), V(p2.x * out + s * 0.02, -0.14, ch_z - 0.03 - 0.03 * (i % 3))]
        _lock(b, "Hair.Front.%d" % i, cp, lambda s: (lerp(0.046, 0.010, s ** 1.6), lerp(0.014, 0.004, s)), tl_all, 0.010)

    # back mass over the coat, to the waist, zigzag tips
    tl_back = scene.trees([coat, dress, bod, head])
    NU = 13
    phis = [lerp(-2, 182, j / (NU - 1)) for j in range(NU)]
    rings = [[hp(ph, w, 0.016) for ph in phis] for w in (0.55, 0.20, -0.15)]
    nape = [hp(90 + (ph - 90) * 0.85, -0.45, 0.020) for ph in phis]
    rings.append(nape)
    wa_z = J['waist'].z
    for z, kx in ((sh_z + 0.05, 1.10), (ch_z - 0.05, 1.45), (wa_z - 0.02, 1.75)):
        ring = []
        for j in range(NU):
            x = nape[j].x * kx
            by = scene.back_y(tl_back, x, z)
            zz = z
            if z < wa_z + 0.01:
                edge = abs(j - (NU - 1) / 2) / ((NU - 1) / 2)
                zz += (0.06 if j % 2 else -0.02) + 0.12 * edge ** 2
            ring.append(V(x, max((by if by is not None else nape[j].y) + 0.012, nape[j].y), zz))
        rings.append(ring)
    for it in range(3):
        rings = [[scene.push_out(p, tl_all, 0.008) for p in r] for r in rings]
    v, f = geom.loft(rings, closed=False)
    curtain = b.mesh("Hair.Curtain", v, f, [b.mats['hair']])
    scene.orient_axis(curtain, 'z', (0, -0.02, 0))
    sm = curtain.modifiers.new("Solidify", 'SOLIDIFY')
    sm.thickness, sm.offset = 0.010, 1.0

    tl_top = scene.trees([head, cap, bod, dress, coat, curtain])
    tl_back2 = scene.trees([curtain, coat, dress, bod, head])
    n = 9
    for i in range(n):
        phi = lerp(10, 170, i / (n - 1))
        p2 = hp(90 + (phi - 90) * 0.85, -0.40, 0.030)
        edge = abs(i - (n - 1) / 2) / ((n - 1) / 2)
        ztip = wa_z - 0.05 + 0.05 * (i % 2) + 0.12 * edge ** 2
        lower = []
        for z, kx in ((sh_z, 1.30), (ztip, 1.75)):
            by = scene.back_y(tl_back2, p2.x * kx, z)
            lower.append(V(p2.x * kx, (by if by is not None else 0.12) + 0.010, z))
        cp = [hp(phi, 0.30, 0.024), hp(phi, 0.0, 0.028), p2] + lower
        _lock(b, "Hair.Back.%d" % i, cp, lambda s: (lerp(0.042, 0.006, s ** 1.6), lerp(0.011, 0.003, s)), tl_top, 0.010)


# ---------------------------------------------------------------- hat (big drooping brim, kinked tip)
def build_hat(b):
    hc = body.head_dims(P)[0]
    M = Matrix.Translation(V(0, hc.y + 0.006, hc.z + 0.080 * K)) @ Matrix.Rotation(-0.16, 4, 'X')  # tilted back: brim clears the eyes
    rings = []
    n = 22
    for f in (0.0, 0.45, 1.0):
        rx, ry = lerp(0.100 * K, 0.36, f), lerp(0.116 * K, 0.325, f)
        rings.append([M @ V(rx * cos(2 * pi * j / n), ry * sin(2 * pi * j / n),
                            -0.070 * f * f + 0.012 * f * f * sin(2 * 2 * pi * j / n + 0.8) + 0.005 * (1 if j % 2 else -1) * f * f)
                      for j in range(n)])
    v, f_ = geom.loft(rings)
    brim = b.mesh("Hat.Brim", v, f_, [b.mats['hat']])
    scene.face_dir(brim, V(0, 0, 1))
    sm = brim.modifiers.new("Solidify", 'SOLIDIFY')
    sm.thickness, sm.offset = 0.008, 0.0
    cpl = [V(0, 0, 0.0), V(0, 0.005, 0.20), V(0.014, 0.015, 0.38), V(0.075, 0.02, 0.48), V(0.150, 0.015, 0.51),
           V(0.205, 0.005, 0.47)]
    radk = [0.112 * K, 0.082, 0.050, 0.030, 0.016, 0.004]
    pw = [M @ p for p in cpl]
    v, f_ = geom.sweep(pw, [(r * 1.08, r) for r in radk], n=10, caps=(None, 'flat'))
    cone = b.mesh("Hat.Cone", v, f_, [b.mats['hat']])
    scene.orient_path(cone, pw)


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
    dress = build_dress(b, bod, J, bones)
    build_boots(b, bod, J, bones)
    build_staff(b, J)
    coat = build_coat(b, J, [dress, bod])
    build_hair(b, head, bod, dress, coat, J)
    build_hat(b)
    stage(b)
    return b

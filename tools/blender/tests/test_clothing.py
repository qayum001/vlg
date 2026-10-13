import math
import unittest

import bpy
from mathutils import Vector

from vlg3d import body, clothing, scene


class ClothingTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.b = scene.Builder("TestCloth", "t_cloth_")
        cls.b.clear()
        cls.p = body.BodyParams()
        cls.body = body.build_body(cls.b, cls.p, cls.b.mat("skin", (0.9, 0.7, 0.6)))
        cls.J = body.joints(cls.body)
        cls.bones = body.skeleton(cls.p)[3]

    @classmethod
    def tearDownClass(cls):
        cls.b.clear()
        c = bpy.data.collections.get("TestCloth")
        if c:
            bpy.data.collections.remove(c)

    def bodice(self):
        J = self.J
        self.wx = J['wrist.L'].x - 0.01
        self.waist_z = J['waist'].z
        self.neck_z = J['neck'].z - 0.03
        planes = [(Vector((0, 0, self.neck_z)), Vector((0, 0, 1))), (Vector((0, 0, self.waist_z)), Vector((0, 0, -1))),
                  (Vector((self.wx, 0, 0)), Vector((1, 0, 0))), (Vector((-self.wx, 0, 0)), Vector((-1, 0, 0)))]
        return clothing.shell(self.body, 0.01, planes)

    def test_shell_respects_planes_and_offset(self):
        bm = self.bodice()
        for v in bm.verts:
            self.assertLessEqual(v.co.z, self.neck_z + 1e-5)
            self.assertGreaterEqual(v.co.z, self.waist_z - 1e-5)
            self.assertLessEqual(abs(v.co.x), self.wx + 1e-5)
        # inflated: the shell's chest is wider than the body's chest by about the offset
        ch = self.J['chest'].z
        sx = max(abs(v.co.x) for v in bm.verts if abs(v.co.z - ch) < 0.01 and abs(v.co.x) < 0.3)
        bx = max(abs(v.co.x) for v in self.body.data.vertices if abs(v.co.z - ch) < 0.01 and abs(v.co.x) < 0.3)
        self.assertAlmostEqual(sx - bx, 0.01, delta=0.004)
        bm.free()

    def test_bodice_has_four_cut_loops(self):
        bm = self.bodice()
        loops = clothing.boundary_loops(bm)
        self.assertEqual(len(loops), 4)
        waist = clothing.pick_loop(loops, lambda c: abs(c.z - self.waist_z) < 0.04 and abs(c.x) < 0.1)
        self.assertTrue(all(abs(v.co.z - self.waist_z) < 1e-4 for v in waist))
        with self.assertRaises(ValueError):
            clothing.pick_loop(loops, lambda c: True)
        bm.free()

    def test_orient_loop_is_counter_clockwise(self):
        bm = self.bodice()
        loops = clothing.boundary_loops(bm)
        wl, c = clothing.orient_loop(clothing.pick_loop(loops, lambda q: abs(q.z - self.waist_z) < 0.04), 'z')
        area = 0.0
        for i in range(len(wl)):
            a, b = wl[i].co - c, wl[(i + 1) % len(wl)].co - c
            area += a.x * b.y - a.y * b.x
        self.assertGreater(area, 0)
        bm.free()

    def test_grown_skirt_is_connected_and_outward(self):
        bm = self.bodice()
        loops = clothing.boundary_loops(bm)
        wl, c = clothing.orient_loop(clothing.pick_loop(loops, lambda q: abs(q.z - self.waist_z) < 0.04), 'z')
        base = [v.co.copy() for v in wl]
        rings = [[Vector((c.x + (p.x - c.x) * k, c.y + (p.y - c.y) * k, z)) for p in base]
                 for k, z in ((1.4, 0.8), (2.0, 0.4), (2.4, 0.15))]
        clothing.grow(bm, wl, rings)
        with self.assertRaises(ValueError):
            clothing.grow(bm, wl, [base[:-1]])
        ob = clothing.to_object(self.b, bm, "Dress", [self.b.mats['skin']], clothing.away_from_bones(self.J, self.bones))
        me = ob.data
        uses = {}
        for poly in me.polygons:
            for ek in poly.edge_keys:
                uses[ek] = uses.get(ek, 0) + 1
        self.assertTrue(all(n <= 2 for n in uses.values()))
        boundary = sum(1 for n in uses.values() if n == 1)
        self.assertEqual(boundary, len(base) + sum(len(lp) for lp in loops) - len(wl))  # hem + 3 untouched cuts
        away = clothing.away_from_bones(self.J, self.bones)
        out = sum(p.area for p in me.polygons if p.normal.dot(away(p.center)) > 0)
        self.assertGreater(out / sum(p.area for p in me.polygons), 0.9)


if __name__ == "__main__":
    unittest.main()

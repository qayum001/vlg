import unittest

import bmesh
import bpy
from mathutils.kdtree import KDTree

from vlg3d import body, scene


def islands(me):
    bm = bmesh.new()
    bm.from_mesh(me)
    seen, count = set(), 0
    for f in bm.faces:
        if f.index in seen:
            continue
        count += 1
        stack = [f]
        seen.add(f.index)
        while stack:
            cur = stack.pop()
            for e in cur.edges:
                for g in e.link_faces:
                    if g.index not in seen:
                        seen.add(g.index)
                        stack.append(g)
    non_manifold = sum(1 for e in bm.edges if len(e.link_faces) != 2)
    bm.free()
    return count, non_manifold


class BodyTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.b = scene.Builder("TestBody", "t_body_")
        cls.b.clear()
        cls.p = body.BodyParams()
        skin = cls.b.mat("skin", (0.9, 0.7, 0.6))
        cls.body = body.build_body(cls.b, cls.p, skin)
        cls.head = body.build_head(cls.b, cls.p, skin)

    @classmethod
    def tearDownClass(cls):
        cls.b.clear()
        c = bpy.data.collections.get("TestBody")
        if c:
            bpy.data.collections.remove(c)

    def test_one_closed_island(self):
        count, non_manifold = islands(self.body.data)
        self.assertEqual(count, 1)
        self.assertEqual(non_manifold, 0)

    def test_skeleton_helper_object_removed(self):
        self.assertNotIn("Body_skel", bpy.data.objects)

    def test_mirror_symmetric(self):
        vs = [v.co.copy() for v in self.body.data.vertices]
        kd = KDTree(len(vs))
        for i, v in enumerate(vs):
            kd.insert(v, i)
        kd.balance()
        worst = max(kd.find((-v.x, v.y, v.z))[2] for v in vs)
        self.assertLess(worst, 1e-4)

    def test_budget_and_placement(self):
        tris = scene.tri_count([self.body])
        self.assertTrue(1000 <= tris <= 4000, tris)
        zs = [v.co.z for v in self.body.data.vertices]
        self.assertTrue(0.0 <= min(zs) < 0.012, min(zs))
        hz = [v.co.z for v in self.head.data.vertices]
        self.assertAlmostEqual(max(hz), self.p.height, delta=0.005)
        self.assertAlmostEqual(min(hz), self.p.height - self.p.head_h, delta=0.01)

    def test_widths_follow_radii(self):
        # Guards RADIUS_GAIN: measured half-widths stay near the requested radii.
        J = body.joints(self.body)
        vs = [v.co for v in self.body.data.vertices]
        r = self.p.radii
        el = J['elbow.L']
        near = [v for v in vs if abs(v.x - el.x) < 0.012]
        half_y = (max(v.y for v in near) - min(v.y for v in near)) / 2
        self.assertAlmostEqual(half_y, r['elbow'][0], delta=0.25 * r['elbow'][0])
        ch = J['chest']
        near = [v for v in vs if abs(v.z - ch.z) < 0.012 and abs(v.x) < 0.3]
        half_x = (max(v.x for v in near) - min(v.x for v in near)) / 2
        self.assertAlmostEqual(half_x, r['chest'][0], delta=0.25 * r['chest'][0])

    def test_fist_body_is_closed_and_asymmetric(self):
        p = body.BodyParams(fist=('R',))
        ob = body.build_body(self.b, p, self.b.mats['skin'], name="FistBody")
        count, non_manifold = islands(ob.data)
        self.assertEqual((count, non_manifold), (1, 0))
        J = body.joints(ob)
        self.assertLess(abs(J['hand.R'].x), abs(J['hand.L'].x) - 0.05)

    def test_joints_stored(self):
        J = body.joints(self.body)
        for n in ('pelvis', 'neck', 'shoulder.L', 'hand.R', 'knee.L', 'toe.R'):
            self.assertIn(n, J)
        self.assertGreater(J['hand.L'].x, 0.7)
        self.assertLess(J['hand.R'].x, -0.7)


if __name__ == "__main__":
    unittest.main()

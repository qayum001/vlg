import unittest

import bpy

from models import mage
from vlg3d import body, scene
from tests.test_body import islands


class MageBuildTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.b = mage.build("TestMage", "t_mage_")
        cls.obs = list(cls.b.coll.objects)

    @classmethod
    def tearDownClass(cls):
        cls.b.clear()
        c = bpy.data.collections.get("TestMage")
        if c:
            bpy.data.collections.remove(c)

    def test_low_poly_budget(self):
        tris = scene.tri_count(self.obs)
        self.assertTrue(4000 <= tris <= 12000, tris)

    def test_key_parts_exist(self):
        names = {o.name for o in self.obs}
        for n in ("Body", "Head", "Dress", "Boots", "Coat", "Staff", "Crystal", "Hat.Cone", "Hair.Curtain", "Eye.L", "Eye.R",
                  "Belt", "Amulet"):
            self.assertIn(n, names)

    def test_dress_is_one_open_island(self):
        dress = self.b.coll.objects["Dress"]
        count, _ = islands(dress.data)
        self.assertEqual(count, 1)

    def test_staff_passes_through_right_fist(self):
        J = body.joints(self.b.coll.objects["Body"])
        staff = self.b.coll.objects["Staff"]
        near = [v.co for v in staff.data.vertices if abs(v.co.z - J['hand.R'].z) < 0.05]
        cx = sum(v.x for v in near) / len(near)
        self.assertAlmostEqual(cx, J['hand.R'].x, delta=0.015)

    def test_dress_sleeves_switch_to_coat_color(self):
        dress = self.b.coll.objects["Dress"]
        idx = {p.material_index for p in dress.data.polygons if abs(p.center.x) > 0.6}
        self.assertEqual(idx, {1})
        self.assertIn(0, {p.material_index for p in dress.data.polygons if abs(p.center.x) < 0.1})


if __name__ == "__main__":
    unittest.main()

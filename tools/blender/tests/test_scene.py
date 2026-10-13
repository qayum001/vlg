import unittest

import bpy
from mathutils import Vector

from vlg3d import geom, scene


def outward_score(ob, center):
    return sum(p.normal.dot(p.center - Vector(center)) * p.area for p in ob.data.polygons)


class BuilderTest(unittest.TestCase):
    def setUp(self):
        self.b = scene.Builder("TestColl", "t_vlg3d_")
        self.b.clear()

    def tearDown(self):
        self.b.clear()
        c = bpy.data.collections.get("TestColl")
        if c:
            bpy.data.collections.remove(c)

    def test_clear_removes_objects_and_data(self):
        m = self.b.mat("red", (1, 0, 0))
        self.b.mesh("Tri", [(0, 0, 0), (1, 0, 0), (0, 1, 0)], [(0, 1, 2)], [m])
        self.assertIn("Tri", bpy.data.objects)
        self.b.clear()
        self.assertNotIn("Tri", bpy.data.objects)
        self.assertNotIn("t_vlg3d_Tri", bpy.data.meshes)
        self.assertNotIn("t_vlg3d_red", bpy.data.materials)

    def test_mesh_is_flat_shaded_with_material(self):
        m = self.b.mat("grey", (0.5, 0.5, 0.5))
        ob = self.b.mesh("Quad", [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)], [(0, 1, 2, 3)], [m])
        self.assertFalse(ob.data.polygons[0].use_smooth)
        self.assertIs(ob.data.materials[0], m)

    def test_tube_normals_point_outward(self):
        m = self.b.mat("grey", (0.5, 0.5, 0.5))
        path = [Vector((0, 0, z)) for z in (0.0, 0.5, 1.0)]
        for caps in (('round', 'round'), (None, None)):
            ob = self.b.tube("Tube" + str(caps[0]), path, [(0.1, 0.1)] * 3, m, n=8, caps=caps)
            self.assertGreater(outward_score(ob, (0, 0, 0.5)), 0, caps)

    def test_orient_axis_flips_inward_faces(self):
        m = self.b.mat("grey", (0.5, 0.5, 0.5))
        rings = [geom.ring_z(z, 0.2, 0.2, 0.2, 8) for z in (0.0, 1.0)]
        v, f = geom.loft(rings)
        f = [tuple(reversed(x)) for x in f]
        ob = self.b.mesh("Inward", v, f, [m])
        self.assertLess(outward_score(ob, (0, 0, 0.5)), 0)
        scene.orient_axis(ob, 'z', (0, 0, 0))
        self.assertGreater(outward_score(ob, (0, 0, 0.5)), 0)

    def test_push_out_leaves_point_outside(self):
        m = self.b.mat("grey", (0.5, 0.5, 0.5))
        ob = self.b.tube("Pillar", [Vector((0, 0, -0.5)), Vector((0, 0, 0.5))], [(0.3, 0.3)] * 2, m,
                         n=12, caps=('flat', 'flat'))
        tl = scene.trees([ob])
        p = scene.push_out(Vector((0.1, 0.0, 0.0)), tl, 0.05)
        self.assertGreater(Vector((p.x, p.y)).length, 0.3)
        q = Vector((1.0, 0.0, 0.0))
        self.assertEqual(scene.push_out(q, tl, 0.05), q)
        deep = Vector((0.02, 0.0, 0.0))  # needs ~0.33 m to get out; larger than max_move -> left alone
        self.assertEqual(scene.push_out(deep, tl, 0.05, max_move=0.1), deep)
        near = Vector((0.27, 0.0, 0.0))
        self.assertGreater(scene.push_out(near, tl, 0.05, max_move=0.1).x, 0.3)

    def test_tri_count(self):
        m = self.b.mat("grey", (0.5, 0.5, 0.5))
        ob = self.b.mesh("Quad", [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)], [(0, 1, 2, 3)], [m])
        self.assertEqual(scene.tri_count([ob]), 2)


if __name__ == "__main__":
    unittest.main()
